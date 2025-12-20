using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

namespace TeamCrescendo.ProceduralIvy
{
    public static class EditorMeshBuilder
    {
        private struct LeafPrefabCache
        {
            public Material material;
            public int vertexCount;
            public int[] triangles;

            // Cached Arrays to avoid accessing Mesh properties in threads (Unity API restriction)
            public Vector3[] vertices;
            public Vector3[] normals;
            public Vector2[] uv;
            public Color32[] colors32;
        }

        private struct LeafData
        {
            public LeafPoint leaf;
            public int branchIndex;
        }

        private const int InitialArraySize = 4096;
        private static Vector3[] verts = new Vector3[InitialArraySize];
        private static Vector3[] normals = new Vector3[InitialArraySize];
        private static Vector2[] uvs = new Vector2[InitialArraySize];
        private static Color32[] colors = new Color32[InitialArraySize];
        private static int[] trisBranches = new int[InitialArraySize];

        private static readonly List<List<int>> trisLeaves = new ();
        private static readonly List<Material> uniqueMaterials = new ();
        private static readonly Dictionary<int, LeafPrefabCache> prefabCache = new ();

        private static readonly Dictionary<int, List<LeafData>> leavesByMaterialIndex = new ();

        private static float angleStep;
        private static Matrix4x4 worldToLocalMatrix;

        public static bool Build(IvyData ivyData, Transform root, MeshRenderer mr, Mesh targetMesh)
        {
            if (ivyData == null || root == null || mr == null || targetMesh == null)
                throw new ArgumentNullException();

            if (ivyData.ivyContainer.branches.Count == 0)
            {
                targetMesh.Clear();
                return false;
            }

            CachePrefabData(ivyData);

            InitializeMaterialsAndBuckets(ivyData, mr);

            CalculateCountsAndOffsets(ivyData, out int totalVerts, out int totalBranchTris,
                out int[] branchVertOffsets, out int[] branchTriOffsets, out Dictionary<int, int[]> leafVertOffsets);

            long limit = ivyData.ivyParameters.buffer32Bits ? Constants.VERTEX_LIMIT_32 : Constants.VERTEX_LIMIT_16;
            if (totalVerts > limit)
            {
                Debug.Log($"Vertex count exceeds limit. Required: {totalVerts}, Limit: {limit}");
                return false;
            }

            EnsureArrayCapacity(ref verts, totalVerts);
            EnsureArrayCapacity(ref normals, totalVerts);
            EnsureArrayCapacity(ref uvs, totalVerts);
            EnsureArrayCapacity(ref colors, totalVerts);
            EnsureArrayCapacity(ref trisBranches, totalBranchTris);

            // Ensure leaf triangle lists are ready
            foreach (var list in trisLeaves)
            {
                // Heuristic: estimate capacity to avoid resize. 
                // Actual count is hard to predict perfectly without another loop, but safe estimate is good enough.
                if (list.Capacity < totalVerts / 2) list.Capacity = totalVerts / 2;
            }

            // cache common vairables
            worldToLocalMatrix = root.worldToLocalMatrix;
            float totalAngle = ivyData.ivyParameters.halfgeom ? 180f : 360f;
            angleStep = totalAngle / ivyData.ivyParameters.sides;

            // We pass the pre-calculated offsets so threads know where to write
            BuildGeometryParallel(ivyData, root, branchVertOffsets, branchTriOffsets, leafVertOffsets);

            // update mesh
            targetMesh.Clear();
            targetMesh.indexFormat = ivyData.ivyParameters.buffer32Bits ? IndexFormat.UInt32 : IndexFormat.UInt16;

            targetMesh.SetVertices(verts, 0, totalVerts);
            targetMesh.SetNormals(normals, 0, totalVerts);
            targetMesh.SetUVs(0, uvs, 0, totalVerts);
            targetMesh.SetColors(colors, 0, totalVerts);

            targetMesh.subMeshCount = uniqueMaterials.Count + 1;
            targetMesh.SetTriangles(trisBranches, 0, totalBranchTris, 0);

            for (var i = 0; i < uniqueMaterials.Count; i++)
                targetMesh.SetTriangles(trisLeaves[i], i + 1);

            targetMesh.RecalculateBounds();
            targetMesh.RecalculateTangents();

            return true;
        }

        private static void CalculateCountsAndOffsets(IvyData ivyData,
            out int totalVerts, out int totalBranchTris,
            out int[] branchVertOffsets, out int[] branchTriOffsets,
            out Dictionary<int, int[]> leafVertOffsets)
        {
            totalVerts = 0;
            totalBranchTris = 0;

            int branchCount = ivyData.ivyContainer.branches.Count;
            branchVertOffsets = new int[branchCount];
            branchTriOffsets = new int[branchCount];
            leafVertOffsets = new Dictionary<int, int[]>();

            int sidesPlusOne = ivyData.ivyParameters.sides + 1;
            int sidesTimesSix = ivyData.ivyParameters.sides * 6;
            int sidesTimesThree = ivyData.ivyParameters.sides * 3;

            if (ivyData.ivyParameters.generateBranches)
            {
                for (var i = 0; i < branchCount; i++)
                {
                    branchVertOffsets[i] = totalVerts;
                    branchTriOffsets[i] = totalBranchTris;

                    var branch = ivyData.ivyContainer.branches[i];
                    int pointCount = branch.branchPoints.Count;

                    if (pointCount > 1)
                    {
                        totalVerts += (pointCount - 1) * sidesPlusOne + 1;
                        totalBranchTris += (pointCount - 2) * sidesTimesSix + sidesTimesThree;
                    }
                }
            }

            if (ivyData.ivyParameters.generateLeaves)
            {
                foreach (var kvp in leavesByMaterialIndex)
                {
                    int matIndex = kvp.Key;
                    var leavesList = kvp.Value;
                    int[] offsets = new int[leavesList.Count];

                    for (int i = 0; i < leavesList.Count; i++)
                    {
                        offsets[i] = totalVerts;
                        var leafData = leavesList[i];
                        if (prefabCache.TryGetValue(leafData.leaf.chosenLeave, out var cache))
                        {
                            totalVerts += cache.vertexCount;
                        }
                    }

                    leafVertOffsets[matIndex] = offsets;
                }
            }
        }

        private static void BuildGeometryParallel(IvyData ivyData, Transform root,
            int[] branchVertOffsets, int[] branchTriOffsets, Dictionary<int, int[]> leafVertOffsets)
        {
            var par = ivyData.ivyParameters;
            int sides = par.sides;
            int sidesPlusOne = sides + 1;
            bool generateBranches = par.generateBranches;
            bool halfGeom = par.halfgeom;
            Vector2 uvScale = par.uvScale;
            Vector2 uvOffset = par.uvOffset;
            float stepSize = par.stepSize;
            Vector3 rootUp = root.up;

            if (generateBranches)
            {
                Parallel.For(0, ivyData.ivyContainer.branches.Count, b =>
                {
                    var branch = ivyData.ivyContainer.branches[b];
                    int pointCount = branch.branchPoints.Count;

                    if (pointCount > 1)
                    {
                        int currentVertBase = branchVertOffsets[b];
                        int currentTriBase = branchTriOffsets[b];
                        int localVertCount = 0; // Relative to this branch

                        // Pre-allocate or clear the runtime list (thread-safe because 'branch' is unique per index b)
                        if (branch.branchPoints == null) return; // safety

                        for (var p = 0; p < pointCount; p++)
                        {
                            var branchPoint = branch.branchPoints[p];

                            float radius = CalculateRadius(par, branchPoint.length);
                            branchPoint.radius = radius;

                            if (p != pointCount - 1)
                            {
                                CalculateFirstVectorAndAxis(ivyData, rootUp, p, b, out branchPoint.firstVector, out branchPoint.axis);

                                float tipInfluence = GetTipInfluence(par, branchPoint.length, branch.totalLength);

                                for (var v = 0; v < sidesPlusOne; v++)
                                {
                                    int absIndex = currentVertBase;

                                    var quat = Quaternion.AngleAxis(angleStep * v, branchPoint.axis);
                                    var direction = quat * branchPoint.firstVector;

                                    Vector3 worldPos = direction * radius * tipInfluence + branchPoint.point;
                                    verts[absIndex] = worldToLocalMatrix.MultiplyPoint3x4(worldPos);

                                    Vector3 normalWorld;
                                    if (halfGeom && sides == 1) normalWorld = -branchPoint.grabVector;
                                    else normalWorld = direction;
                                    normals[absIndex] = worldToLocalMatrix.MultiplyVector(normalWorld);

                                    uvs[absIndex] = new Vector2(
                                        branchPoint.length * uvScale.y + uvOffset.y - stepSize,
                                        (1f / sides) * v * uvScale.x + uvOffset.x);

                                    currentVertBase++;
                                    localVertCount++;
                                }
                            }
                            else
                            {
                                // Tip Logic
                                int absIndex = currentVertBase;
                                verts[absIndex] = worldToLocalMatrix.MultiplyPoint3x4(branchPoint.point);

                                Vector3 normalWorld;
                                if (halfGeom && sides == 1) normalWorld = -branchPoint.grabVector;
                                else normalWorld = (branchPoint.point - branch.branchPoints[p - 1].point).normalized;

                                normals[absIndex] = worldToLocalMatrix.MultiplyVector(normalWorld);
                                uvs[absIndex] = new Vector2(branch.totalLength * uvScale.y + uvOffset.y,
                                    0.5f * uvScale.x + uvOffset.x);

                                currentVertBase++;
                                localVertCount++;

                                TriangulateBranchThreadSafe(par, branch, currentTriBase, currentVertBase, localVertCount);
                            }
                        }
                    }
                });
            }

            if (par.generateLeaves)
            {
                // We iterate over Materials (Serial), but process the List of Leaves for that material in Parallel
                foreach (var kvp in leavesByMaterialIndex)
                {
                    int matIndex = kvp.Key;
                    var leavesList = kvp.Value;
                    int[] offsets = leafVertOffsets[matIndex];
                    var targetTriList = trisLeaves[matIndex];

                    // ConcurrentBag is slow. Since we need to add to a List<int> for triangles, 
                    // and List is NOT thread safe, we have two options:
                    // 1. Lock (slow). 
                    // 2. Pre-calculate tri counts (complex).
                    // 3. Since Verts are 80% of work, we Parallelize Verts, but do Tris serially or with a fast lock.

                    // Hybrid approach: Calculate Verts in Parallel. Collect Tris in thread-local buffers and combine.
                    // Actually, for simplicity in Editor tools, a lock around the Tri list addition is often acceptable 
                    // IF the heavy math (Verts) is outside the lock.

                    object triListLock = new object();

                    Parallel.For(0, leavesList.Count, i =>
                    {
                        var leafData = leavesList[i];
                        int vertStart = offsets[i];

                        // Deterministic Random per leaf
                        System.Random rng = new System.Random(leafData.branchIndex + par.randomSeed + matIndex + i);

                        var currentLeaf = leafData.leaf;
                        var branch = ivyData.ivyContainer.branches[leafData.branchIndex];
                        var cache = prefabCache[currentLeaf.chosenLeave];

                        Quaternion localRot = ProceduralIvyCommon.CalculateLeafOrientation(par,
                            currentLeaf.lpForward, currentLeaf.lpUpward, 
                            rng, out Vector3 forward, out Vector3 left);

                        // scale is shrinked when closer to tip
                        float scale = par.minLeafScale + (float)rng.NextDouble() * (par.maxLeafScale - par.minLeafScale);
                        scale *= Mathf.InverseLerp(branch.totalLength, branch.totalLength - par.tipInfluence,
                            currentLeaf.lpLength);

                        currentLeaf.leafScale = scale;
                        currentLeaf.leafCenter = worldToLocalMatrix.MultiplyPoint3x4(currentLeaf.point);

                        Vector3 offset = left * par.leafOffset.x + currentLeaf.lpUpward * par.leafOffset.y 
                                                             + currentLeaf.lpForward * par.leafOffset.z;

                        // write verts with respect to offset (thread safe)
                        for (var v = 0; v < cache.vertexCount; v++)
                        {
                            int absIndex = vertStart + v;

                            // Transform World
                            Vector3 worldPos = (localRot * cache.vertices[v] * scale) + currentLeaf.point + offset;
                            verts[absIndex] = worldToLocalMatrix.MultiplyPoint3x4(worldPos);
                            normals[absIndex] = worldToLocalMatrix.MultiplyVector(localRot * cache.normals[v]);
                            uvs[absIndex] = cache.uv[v];
                            colors[absIndex] = (cache.colors32 != null && cache.colors32.Length > v)
                                ? cache.colors32[v]
                                : Color.white;
                        }

                        // We must add offsets to the triangles. 
                        // Since List.Add is not safe, we calculate them into a local array and lock-add.
                        // For massive leaf counts, this lock is a bottleneck, but better than single-threaded math.
                        int[] newTris = new int[cache.triangles.Length];
                        for (int t = 0; t < cache.triangles.Length; t++)
                        {
                            newTris[t] = cache.triangles[t] + vertStart;
                        }

                        lock (triListLock)
                        {
                            targetTriList.AddRange(newTris);
                        }
                    });
                }
            }
        }

        // --- Helper: Cache Prefab Data Including Arrays (Unity API not thread safe) ---
        private static void CachePrefabData(IvyData ivyData)
        {
            prefabCache.Clear();
            if (!ivyData.ivyParameters.generateLeaves) return;

            for (int i = 0; i < ivyData.ivyParameters.leavesPrefabs.Length; i++)
            {
                var go = ivyData.ivyParameters.leavesPrefabs[i];
                if (go == null) continue;
                var mf = go.GetComponent<MeshFilter>();
                var mr = go.GetComponent<MeshRenderer>();
                if (mf != null && mr != null && mf.sharedMesh != null)
                {
                    var m = mf.sharedMesh;
                    prefabCache[i] = new LeafPrefabCache
                    {
                        material = mr.sharedMaterial,
                        vertexCount = m.vertexCount,
                        triangles = m.triangles,
                        // Cache arrays for thread access
                        vertices = m.vertices,
                        normals = m.normals,
                        uv = m.uv,
                        colors32 = m.colors32
                    };
                }
            }
        }

        private static float CalculateRadius(IvyParameters par, float length)
        {
            float value = (Mathf.Sin(length * par.radiusVarFreq + par.radiusVarOffset) + 1f) * 0.5f;
            return Mathf.Lerp(par.minBranchRadius, par.maxBranchRadius, value);
        }

        private static void TriangulateBranchThreadSafe(IvyParameters par, BranchContainer branch, int triStartBase,
            int vertCount, int lastVertCount)
        {
            // Direct write to trisBranches using 'triStartBase' as offset
            int triIndex = triStartBase;
            int sides = par.sides;
            int sidesPlusOne = sides + 1;
            int pointsToTriangulate = branch.branchPoints.Count - 2;

            for (var round = 0; round < pointsToTriangulate; round++)
            {
                int roundOffset = round * sidesPlusOne;
                int baseVertIndex = vertCount - lastVertCount;

                for (var i = 0; i < sides; i++)
                {
                    int currentBase = i + roundOffset + baseVertIndex;

                    trisBranches[triIndex] = currentBase;
                    trisBranches[triIndex + 1] = currentBase + 1;
                    trisBranches[triIndex + 2] = currentBase + sidesPlusOne;

                    trisBranches[triIndex + 3] = currentBase + 1;
                    trisBranches[triIndex + 4] = currentBase + sides + 2;
                    trisBranches[triIndex + 5] = currentBase + sidesPlusOne;

                    triIndex += 6;
                }
            }

            // Caps
            for (int t = 0, c = 0; t < sides * 3; t += 3, c++)
            {
                trisBranches[triIndex] = vertCount - 1;
                trisBranches[triIndex + 1] = vertCount - 3 - c;
                trisBranches[triIndex + 2] = vertCount - 2 - c;
                triIndex += 3;
            }
        }

        private static void InitializeMaterialsAndBuckets(IvyData ivyData, MeshRenderer mr)
        {
            uniqueMaterials.Clear();
            foreach (var list in trisLeaves) list.Clear();
            leavesByMaterialIndex.Clear();
            if (ivyData.ivyParameters.generateLeaves)
            {
                var matToPrefabIndices = new Dictionary<Material, int>();
                for (int i = 0; i < ivyData.ivyParameters.leavesPrefabs.Length; i++)
                {
                    if (!prefabCache.TryGetValue(i, out var cache)) continue;
                    if (!matToPrefabIndices.ContainsKey(cache.material))
                    {
                        uniqueMaterials.Add(cache.material);
                        matToPrefabIndices.Add(cache.material, uniqueMaterials.Count - 1);
                        if (trisLeaves.Count < uniqueMaterials.Count) trisLeaves.Add(new List<int>());
                        leavesByMaterialIndex.Add(uniqueMaterials.Count - 1, new List<LeafData>());
                    }
                }

                for (int b = 0; b < ivyData.ivyContainer.branches.Count; b++)
                {
                    var branch = ivyData.ivyContainer.branches[b];
                    for (int l = 0; l < branch.leaves.Count; l++)
                    {
                        var leaf = branch.leaves[l];
                        if (prefabCache.TryGetValue(leaf.chosenLeave, out var cache))
                        {
                            int matIndex = matToPrefabIndices[cache.material];
                            leavesByMaterialIndex[matIndex].Add(new LeafData { leaf = leaf, branchIndex = b });
                        }
                    }
                }

                var finalMaterials = new Material[uniqueMaterials.Count + 1];
                finalMaterials[0] = mr.sharedMaterial;
                for (var i = 0; i < uniqueMaterials.Count; i++) finalMaterials[i + 1] = uniqueMaterials[i];
                mr.sharedMaterials = finalMaterials;
            }
            else
            {
                mr.sharedMaterials = new[] { ivyData.ivyParameters.branchesMaterial };
            }
        }

        private static void EnsureArrayCapacity<T>(ref T[] array, int requiredSize)
        {
            if (array == null || array.Length < requiredSize)
            {
                int newSize = Mathf.Max(requiredSize, (int)(array.Length * 1.5f));
                newSize = Mathf.Max(newSize, 4096);
                Array.Resize(ref array, newSize);
            }
        }

        private static void CalculateFirstVectorAndAxis(IvyData ivyData, Vector3 rootUp, int p, int b, out Vector3 firstVector, out Vector3 axis)
        {
            var branch = ivyData.ivyContainer.branches[b];
            if (b == 0 && p == 0)
            {
                axis = rootUp;
                if (!ivyData.ivyParameters.halfgeom) firstVector = ivyData.ivyContainer.firstVertexVector;
                else firstVector = Quaternion.AngleAxis(90f, axis) * ivyData.ivyContainer.firstVertexVector;
            }
            else
            {
                if (p == 0) axis = branch.branchPoints[1].point - branch.branchPoints[0].point;
                else
                    axis = Vector3.Lerp(branch.branchPoints[p].point - branch.branchPoints[p - 1].point,
                        branch.branchPoints[p + 1].point - branch.branchPoints[p].point, 0.5f).normalized;
                if (!ivyData.ivyParameters.halfgeom)
                    firstVector = Vector3.ProjectOnPlane(branch.branchPoints[p].grabVector, axis).normalized;
                else
                    firstVector = Quaternion.AngleAxis(90f, axis) *
                                  Vector3.ProjectOnPlane(branch.branchPoints[p].grabVector, axis).normalized;
            }
        }

        private static float GetTipInfluence(IvyParameters infoPool, float length, float totalLength)
        {
            float distFromEnd = totalLength - length;
            if (distFromEnd <= infoPool.tipInfluence)
                return Mathf.InverseLerp(totalLength, totalLength - infoPool.tipInfluence, length - 0.1f);
            return 1.0f;
        }
    }
}