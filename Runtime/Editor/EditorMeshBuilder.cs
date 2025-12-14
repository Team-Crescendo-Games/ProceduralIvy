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
            public Color[] colors;
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
        private static Color[] colors = new Color[InitialArraySize];
        private static int[] trisBranches = new int[InitialArraySize];

        private static readonly List<List<int>> trisLeaves = new ();
        private static readonly List<Material> uniqueMaterials = new ();
        private static readonly Dictionary<int, LeafPrefabCache> prefabCache = new ();

        private static readonly Dictionary<int, List<LeafData>> leavesByMaterialIndex = new ();

        private static float angleStep;
        private static Matrix4x4 worldToLocalMatrix;

        public static bool Build(InfoPool infoPool, Transform root, MeshRenderer mr, Mesh targetMesh)
        {
            if (infoPool == null || root == null || mr == null || targetMesh == null)
                throw new ArgumentNullException();

            if (infoPool.ivyContainer.branches.Count == 0)
            {
                Debug.LogWarning("No branches found. Building into a null mesh.");
                targetMesh.Clear();
                return false;
            }

            CachePrefabData(infoPool);

            InitializeMaterialsAndBuckets(infoPool, mr);

            CalculateCountsAndOffsets(infoPool, out int totalVerts, out int totalBranchTris,
                out int[] branchVertOffsets, out int[] branchTriOffsets, out Dictionary<int, int[]> leafVertOffsets);

            long limit = infoPool.ivyParameters.buffer32Bits ? 2147483647L : 65535L;
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
            if (!infoPool.ivyParameters.halfgeom)
                angleStep = Mathf.Rad2Deg * 2 * Mathf.PI / infoPool.ivyParameters.sides;
            else
                angleStep = Mathf.Rad2Deg * 2 * Mathf.PI / infoPool.ivyParameters.sides / 2;

            // We pass the pre-calculated offsets so threads know where to write
            BuildGeometryParallel(infoPool, root, branchVertOffsets, branchTriOffsets, leafVertOffsets);

            // update mesh
            targetMesh.Clear();
            targetMesh.indexFormat = infoPool.ivyParameters.buffer32Bits ? IndexFormat.UInt32 : IndexFormat.UInt16;

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

        private static void CalculateCountsAndOffsets(InfoPool infoPool,
            out int totalVerts, out int totalBranchTris,
            out int[] branchVertOffsets, out int[] branchTriOffsets,
            out Dictionary<int, int[]> leafVertOffsets)
        {
            totalVerts = 0;
            totalBranchTris = 0;

            int branchCount = infoPool.ivyContainer.branches.Count;
            branchVertOffsets = new int[branchCount];
            branchTriOffsets = new int[branchCount];
            leafVertOffsets = new Dictionary<int, int[]>();

            int sidesPlusOne = infoPool.ivyParameters.sides + 1;
            int sidesTimesSix = infoPool.ivyParameters.sides * 6;
            int sidesTimesThree = infoPool.ivyParameters.sides * 3;

            if (infoPool.ivyParameters.generateBranches)
            {
                for (var i = 0; i < branchCount; i++)
                {
                    branchVertOffsets[i] = totalVerts;
                    branchTriOffsets[i] = totalBranchTris;

                    var branch = infoPool.ivyContainer.branches[i];
                    int pointCount = branch.branchPoints.Count;

                    if (pointCount > 1)
                    {
                        totalVerts += (pointCount - 1) * sidesPlusOne + 1;
                        totalBranchTris += (pointCount - 2) * sidesTimesSix + sidesTimesThree;
                    }
                }
            }

            if (infoPool.ivyParameters.generateLeaves)
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

        private static void BuildGeometryParallel(InfoPool infoPool, Transform root,
            int[] branchVertOffsets, int[] branchTriOffsets, Dictionary<int, int[]> leafVertOffsets)
        {
            var par = infoPool.ivyParameters;
            int sides = par.sides;
            int sidesPlusOne = sides + 1;
            bool generateBranches = par.generateBranches;
            bool halfGeom = par.halfgeom;
            Vector2 uvScale = par.uvScale;
            Vector2 uvOffset = par.uvOffset;
            float stepSize = par.stepSize;
            Vector3 rootPosition = root.position;
            Vector3 rootUp = root.up;

            if (generateBranches)
            {
                Parallel.For(0, infoPool.ivyContainer.branches.Count, b =>
                {
                    // Thread-Local Random (System.Random is necessary here)
                    System.Random rng = new System.Random(b + par.randomSeed);

                    var branch = infoPool.ivyContainer.branches[b];
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

                            // Note: List operations are not thread safe if accessed by multiple threads.
                            // Since we only access this specific branch in this thread, it is safe.
                            // However, we cannot reuse the existing List object if the main thread is reading it.
                            // Usually strictly safe to create new here.
                            branchPoint.verticesLoop = new List<RTVertexData>();

                            float radius = CalculateRadius(par, branchPoint.length, rng);
                            branchPoint.radius = radius;

                            // ... (Logic identical to before, just using currentVertBase) ...
                            if (p != pointCount - 1)
                            {
                                var vectors = CalculateVectors(infoPool, rootUp, p, b);
                                // Note: CalculateVectors reads other branches if b>0?? 
                                // Original code: checks p-1, p+1 within SAME branch. Safe.
                                // Branch 0 special case accesses 'firstVertexVector'. Safe (readonly).

                                branchPoint.firstVector = vectors[0];
                                branchPoint.axis = vectors[1];

                                float tipInfluence = GetTipInfluence(par, branchPoint.length, branch.totalLenght);

                                for (var v = 0; v < sidesPlusOne; v++)
                                {
                                    int absIndex = currentVertBase;

                                    var quat = Quaternion.AngleAxis(angleStep * v, vectors[1]);
                                    var direction = quat * vectors[0];

                                    Vector3 worldPos = direction * radius * tipInfluence + branchPoint.point;
                                    verts[absIndex] = worldToLocalMatrix.MultiplyPoint3x4(worldPos);

                                    Vector3 normalWorld;
                                    if (halfGeom && sides == 1) normalWorld = -branchPoint.grabVector;
                                    else normalWorld = direction;
                                    normals[absIndex] = worldToLocalMatrix.MultiplyVector(normalWorld);

                                    uvs[absIndex] = new Vector2(
                                        branchPoint.length * uvScale.y + uvOffset.y - stepSize,
                                        (1f / sides) * v * uvScale.x + uvOffset.x);

                                    var vertexForRuntime = direction * radius + (branchPoint.point - rootPosition);

                                    // Writing to the list is safe (thread local context)
                                    branchPoint.verticesLoop.Add(new RTVertexData(
                                        vertexForRuntime, normals[absIndex], uvs[absIndex], Vector2.zero,
                                        colors[absIndex]));

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
                                uvs[absIndex] = new Vector2(branch.totalLenght * uvScale.y + uvOffset.y,
                                    0.5f * uvScale.x + uvOffset.x);

                                var centerVertexPosition = worldToLocalMatrix.MultiplyPoint3x4(branchPoint.point);
                                branchPoint.verticesLoop.Add(new RTVertexData(
                                    centerVertexPosition, normals[absIndex], uvs[absIndex], Vector2.zero,
                                    colors[absIndex]));

                                currentVertBase++;
                                localVertCount++;

                                // Triangulate (Pure math, writes to shared array at disjoint indices -> Safe)
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
                        var branch = infoPool.ivyContainer.branches[leafData.branchIndex];
                        var cache = prefabCache[currentLeaf.chosenLeave];

                        // Re-init runtime list
                        currentLeaf.verticesLeaves = new List<RTVertexData>();

                        // --- Orientation Math (Same as before, using System.Random) ---
                        Vector3 left, forward;
                        if (!par.globalOrientation)
                        {
                            forward = currentLeaf.lpForward;
                            left = currentLeaf.left;
                        }
                        else
                        {
                            forward = par.globalRotation;
                            left = Vector3.Cross(par.globalRotation, currentLeaf.lpUpward).normalized;
                        }

                        Quaternion quat = Quaternion.LookRotation(currentLeaf.lpUpward, forward);
                        quat = Quaternion.AngleAxis(par.rotation.x, left) *
                               Quaternion.AngleAxis(par.rotation.y, currentLeaf.lpUpward) *
                               Quaternion.AngleAxis(par.rotation.z, forward) * quat;

                        // System.Random Range replacement
                        float rx = (float)(rng.NextDouble() * 2.0 - 1.0) * par.randomRotation.x;
                        float ry = (float)(rng.NextDouble() * 2.0 - 1.0) * par.randomRotation.y;
                        float rz = (float)(rng.NextDouble() * 2.0 - 1.0) * par.randomRotation.z;

                        quat = Quaternion.AngleAxis(rx, left) *
                               Quaternion.AngleAxis(ry, currentLeaf.lpUpward) *
                               Quaternion.AngleAxis(rz, forward) * quat;
                        quat = currentLeaf.forwarRot * quat;

                        float scale = par.minScale + (float)rng.NextDouble() * (par.maxScale - par.minScale);
                        scale *= Mathf.InverseLerp(branch.totalLenght, branch.totalLenght - par.tipInfluence,
                            currentLeaf.lpLength);

                        currentLeaf.leafScale = scale;
                        currentLeaf.leafRotation = quat;
                        currentLeaf.leafCenter = worldToLocalMatrix.MultiplyPoint3x4(currentLeaf.point);

                        Vector3 offset = left * par.offset.x + currentLeaf.lpUpward * par.offset.y +
                                         currentLeaf.lpForward * par.offset.z;

                        // --- Write Verts (Thread Safe due to offsets) ---
                        for (var v = 0; v < cache.vertexCount; v++)
                        {
                            int absIndex = vertStart + v;

                            // Transform World
                            Vector3 worldPos = (quat * cache.vertices[v] * scale) + currentLeaf.point + offset;
                            verts[absIndex] = worldToLocalMatrix.MultiplyPoint3x4(worldPos);
                            normals[absIndex] = worldToLocalMatrix.MultiplyVector(quat * cache.normals[v]);
                            uvs[absIndex] = cache.uv[v];
                            colors[absIndex] = (cache.colors != null && cache.colors.Length > v)
                                ? cache.colors[v]
                                : Color.white;

                            currentLeaf.verticesLeaves.Add(new RTVertexData(verts[absIndex], normals[absIndex],
                                uvs[absIndex], Vector2.zero, colors[absIndex]));
                        }

                        // --- Handle Triangles ---
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
        private static void CachePrefabData(InfoPool infoPool)
        {
            prefabCache.Clear();
            if (!infoPool.ivyParameters.generateLeaves) return;

            for (int i = 0; i < infoPool.ivyParameters.leavesPrefabs.Length; i++)
            {
                var go = infoPool.ivyParameters.leavesPrefabs[i];
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
                        colors = m.colors
                    };
                }
            }
        }

        private static float CalculateRadius(IvyParameters par, float length, System.Random rng)
        {
            float value = (Mathf.Sin(length * par.radiusVarFreq + par.radiusVarOffset) + 1f) * 0.5f;
            return Mathf.Lerp(par.minRadius, par.maxRadius, value);
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

        private static void InitializeMaterialsAndBuckets(InfoPool infoPool, MeshRenderer mr)
        {
            uniqueMaterials.Clear();
            foreach (var list in trisLeaves) list.Clear();
            leavesByMaterialIndex.Clear();
            if (infoPool.ivyParameters.generateLeaves)
            {
                var matToPrefabIndices = new Dictionary<Material, int>();
                for (int i = 0; i < infoPool.ivyParameters.leavesPrefabs.Length; i++)
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

                for (int b = 0; b < infoPool.ivyContainer.branches.Count; b++)
                {
                    var branch = infoPool.ivyContainer.branches[b];
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
                mr.sharedMaterials = new[] { infoPool.ivyParameters.branchesMaterial };
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

        private static Vector3[] CalculateVectors(InfoPool infoPool, Vector3 rootUp, int p, int b)
        {
            Vector3 firstVector, axis;
            var branch = infoPool.ivyContainer.branches[b];
            if (b == 0 && p == 0)
            {
                axis = rootUp;
                if (!infoPool.ivyParameters.halfgeom) firstVector = infoPool.ivyContainer.firstVertexVector;
                else firstVector = Quaternion.AngleAxis(90f, axis) * infoPool.ivyContainer.firstVertexVector;
            }
            else
            {
                if (p == 0) axis = branch.branchPoints[1].point - branch.branchPoints[0].point;
                else
                    axis = Vector3.Lerp(branch.branchPoints[p].point - branch.branchPoints[p - 1].point,
                        branch.branchPoints[p + 1].point - branch.branchPoints[p].point, 0.5f).normalized;
                if (!infoPool.ivyParameters.halfgeom)
                    firstVector = Vector3.ProjectOnPlane(branch.branchPoints[p].grabVector, axis).normalized;
                else
                    firstVector = Quaternion.AngleAxis(90f, axis) *
                                  Vector3.ProjectOnPlane(branch.branchPoints[p].grabVector, axis).normalized;
            }

            return new[] { firstVector, axis };
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