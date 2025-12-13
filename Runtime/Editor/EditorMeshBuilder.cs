using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace TeamCrescendo.ProceduralIvy
{
    public static class EditorMeshBuilder
    {
        // Cache structures to avoid GetComponent calls in loops
        private struct LeafPrefabCache
        {
            public Mesh mesh;
            public Material material;
            public int vertexCount;
            public int[] triangles;
        }

        private const int InitialArraySize = 4096;
        
        // Static buffers
        private static Vector3[] verts = new Vector3[InitialArraySize];
        private static Vector3[] normals = new Vector3[InitialArraySize];
        private static Vector2[] uvs = new Vector2[InitialArraySize];
        private static Color[] colors = new Color[InitialArraySize];
        private static int[] trisBranches = new int[InitialArraySize];

        // Helper lists
        private static readonly List<List<int>> TrisLeaves = new ();
        private static readonly List<Material> UniqueMaterials = new ();
        private static readonly Dictionary<int, LeafPrefabCache> PrefabCache = new ();
        
        // Optimization: Bucket leaves by material index to avoid O(N*M) looping
        private static readonly Dictionary<int, List<LeafData>> LeavesByMaterialIndex = new ();
        
        // Cached math variables
        private static float angleStep;
        private static Matrix4x4 worldToLocalMatrix;
        
        // Wrapper class to hold leaf reference + branch index context
        private struct LeafData
        {
            public LeafPoint leaf;
            public int branchIndex;
        }

        public static bool Build(InfoPool infoPool, Transform root, MeshRenderer mr, Mesh targetMesh)
        {
            if (infoPool == null || root == null || mr == null || targetMesh == null)
                throw new ArgumentNullException();

            // 1. Pre-cache prefab data to avoid GetComponent in loops
            CachePrefabData(infoPool);

            // 2. Setup Materials and Leaf Buckets
            InitializeMaterialsAndBuckets(infoPool, mr);

            // 3. Calculate exact counts
            CalculateCounts(infoPool, out int requiredVerts, out int requiredBranchTris);

            long limit = infoPool.ivyParameters.buffer32Bits ? 2147483647L : 65535L;
            if (requiredVerts > limit)
            {
                Debug.Log($"Vertex count exceeds limit. Required: {requiredVerts}, Limit: {limit}");
                return false;
            }

            // 4. Resize arrays only if needed
            EnsureArrayCapacity(ref verts, requiredVerts);
            EnsureArrayCapacity(ref normals, requiredVerts);
            EnsureArrayCapacity(ref uvs, requiredVerts);
            EnsureArrayCapacity(ref colors, requiredVerts);
            EnsureArrayCapacity(ref trisBranches, requiredBranchTris);

            // 5. Prepare Math Cache
            // Construct a matrix for world-to-local transformation. 
            // This is faster and cleaner than manual pos/rot subtraction in loops.
            worldToLocalMatrix = root.worldToLocalMatrix;
            
            if (!infoPool.ivyParameters.halfgeom)
                angleStep = Mathf.Rad2Deg * 2 * Mathf.PI / infoPool.ivyParameters.sides;
            else
                angleStep = Mathf.Rad2Deg * 2 * Mathf.PI / infoPool.ivyParameters.sides / 2;

            // 6. Build
            BuildGeometry(infoPool, root, out int finalVertCount, out int finalBranchTriCount);

            // 7. Apply to Mesh
            targetMesh.Clear();
            targetMesh.indexFormat = infoPool.ivyParameters.buffer32Bits ? IndexFormat.UInt32 : IndexFormat.UInt16;

            // Use the overload that accepts counts to avoid creating new sub-arrays
            targetMesh.SetVertices(verts, 0, finalVertCount);
            targetMesh.SetNormals(normals, 0, finalVertCount);
            targetMesh.SetUVs(0, uvs, 0, finalVertCount);
            targetMesh.SetColors(colors, 0, finalVertCount);

            targetMesh.subMeshCount = UniqueMaterials.Count + 1;
            targetMesh.SetTriangles(trisBranches, 0, finalBranchTriCount, 0);

            for (var i = 0; i < UniqueMaterials.Count; i++)
                targetMesh.SetTriangles(TrisLeaves[i], i + 1);

            targetMesh.RecalculateBounds();
            targetMesh.RecalculateTangents();

            return true;
        }

        private static void CachePrefabData(InfoPool infoPool)
        {
            PrefabCache.Clear();
            if (!infoPool.ivyParameters.generateLeaves) return;

            for (int i = 0; i < infoPool.ivyParameters.leavesPrefabs.Length; i++)
            {
                var go = infoPool.ivyParameters.leavesPrefabs[i];
                if (go == null) continue;

                var mf = go.GetComponent<MeshFilter>();
                var mr = go.GetComponent<MeshRenderer>();

                if (mf != null && mr != null && mf.sharedMesh != null)
                {
                    PrefabCache[i] = new LeafPrefabCache
                    {
                        mesh = mf.sharedMesh,
                        material = mr.sharedMaterial,
                        vertexCount = mf.sharedMesh.vertexCount,
                        triangles = mf.sharedMesh.triangles
                    };
                }
            }
        }

        private static void InitializeMaterialsAndBuckets(InfoPool infoPool, MeshRenderer mr)
        {
            UniqueMaterials.Clear();
            foreach (var list in TrisLeaves) list.Clear();
            LeavesByMaterialIndex.Clear();

            if (infoPool.ivyParameters.generateLeaves)
            {
                // Bucketing: Map Material -> List of Prefab Indices that use it
                var matToPrefabIndices = new Dictionary<Material, int>(); 
                // Note: We map to an index in our 'uniqueMaterials' list

                // 1. Identify unique materials from the prefabs
                for (int i = 0; i < infoPool.ivyParameters.leavesPrefabs.Length; i++)
                {
                    if (!PrefabCache.TryGetValue(i, out var cache)) continue;

                    if (!matToPrefabIndices.ContainsKey(cache.material))
                    {
                        UniqueMaterials.Add(cache.material);
                        matToPrefabIndices.Add(cache.material, UniqueMaterials.Count - 1);
                        
                        // Ensure lists exist
                        if (TrisLeaves.Count < UniqueMaterials.Count) 
                            TrisLeaves.Add(new List<int>());
                        
                        LeavesByMaterialIndex.Add(UniqueMaterials.Count - 1, new List<LeafData>());
                    }
                }

                // 2. Pre-sort all leaves into buckets based on the material index of their chosen prefab
                // This avoids the nested loop in BuildLeaves later
                for (int b = 0; b < infoPool.ivyContainer.branches.Count; b++)
                {
                    var branch = infoPool.ivyContainer.branches[b];
                    for (int l = 0; l < branch.leaves.Count; l++)
                    {
                        var leaf = branch.leaves[l];
                        if (PrefabCache.TryGetValue(leaf.chosenLeave, out var cache))
                        {
                            int matIndex = matToPrefabIndices[cache.material];
                            LeavesByMaterialIndex[matIndex].Add(new LeafData { leaf = leaf, branchIndex = b });
                        }
                    }
                }

                // Assign to Renderer
                var finalMaterials = new Material[UniqueMaterials.Count + 1];
                finalMaterials[0] = mr.sharedMaterial; // Branch material
                for (var i = 0; i < UniqueMaterials.Count; i++)
                    finalMaterials[i + 1] = UniqueMaterials[i];

                mr.sharedMaterials = finalMaterials;
            }
            else
            {
                mr.sharedMaterials = new[] { infoPool.ivyParameters.branchesMaterial };
            }
        }

        private static void CalculateCounts(InfoPool infoPool, out int vCount, out int tCount)
        {
            vCount = 0;
            tCount = 0;

            int sidesPlusOne = infoPool.ivyParameters.sides + 1;
            int sidesTimesSix = infoPool.ivyParameters.sides * 6;
            int sidesTimesThree = infoPool.ivyParameters.sides * 3;

            if (infoPool.ivyParameters.generateBranches)
            {
                for (var i = 0; i < infoPool.ivyContainer.branches.Count; i++)
                {
                    var branch = infoPool.ivyContainer.branches[i];
                    int pointCount = branch.branchPoints.Count;
                    
                    if (pointCount > 1)
                    {
                        vCount += (pointCount - 1) * sidesPlusOne + 1;
                        tCount += (pointCount - 2) * sidesTimesSix + sidesTimesThree;
                    }
                }
            }

            if (infoPool.ivyParameters.generateLeaves)
            {
                // Because we pre-cached, we don't need to loop everything here. 
                // We can iterate the bucketed list which is faster.
                foreach (var kvp in LeavesByMaterialIndex)
                {
                    foreach (var leafData in kvp.Value)
                    {
                        // Direct lookup from cache, no GetComponent
                        if (PrefabCache.TryGetValue(leafData.leaf.chosenLeave, out var cache))
                            vCount += cache.vertexCount;
                    }
                }
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

        private static void BuildGeometry(InfoPool infoPool, Transform root, out int finalVertCount, out int finalBranchTriCount)
        {
            finalVertCount = 0;
            finalBranchTriCount = 0;

            // Cache repeatedly accessed parameters
            var par = infoPool.ivyParameters;
            int sides = par.sides;
            int sidesPlusOne = sides + 1;
            bool generateBranches = par.generateBranches;
            bool halfGeom = par.halfgeom;
            Vector2 uvScale = par.uvScale;
            Vector2 uvOffset = par.uvOffset;
            float stepSize = par.stepSize;

            for (var b = 0; b < infoPool.ivyContainer.branches.Count; b++)
            {
                Random.InitState(b + par.randomSeed);
                var branch = infoPool.ivyContainer.branches[b];
                int pointCount = branch.branchPoints.Count;
                
                if (pointCount == 0) continue;

                int lastVertCount = 0;
                
                for (var p = 0; p < pointCount; p++)
                {
                    var branchPoint = branch.branchPoints[p];
                    
                    // Optimization: Avoid new allocation per frame/rebuild
                    if (branchPoint.verticesLoop == null)
                        branchPoint.verticesLoop = new List<RTVertexData>();
                    else
                        branchPoint.verticesLoop.Clear();

                    float radius = CalculateRadius(infoPool, branchPoint.length);
                    branchPoint.radius = radius;

                    // Not the last point
                    if (p != pointCount - 1)
                    {
                        var vectors = CalculateVectors(infoPool, root, p, b);
                        branchPoint.firstVector = vectors[0];
                        branchPoint.axis = vectors[1];

                        if (generateBranches)
                        {
                            float tipInfluence = GetTipInfluence(infoPool, branchPoint.length, branch.totalLenght);
                            
                            for (var v = 0; v < sidesPlusOne; v++)
                            {
                                var quat = Quaternion.AngleAxis(angleStep * v, vectors[1]);
                                var direction = quat * vectors[0];
                                
                                // Optimization: Calculate world pos then transform once using matrix
                                Vector3 worldPos = direction * radius * tipInfluence + branchPoint.point;
                                
                                // Apply matrix transform (World -> Local)
                                verts[finalVertCount] = worldToLocalMatrix.MultiplyPoint3x4(worldPos);

                                // Normals
                                Vector3 normalWorld;
                                if (halfGeom && sides == 1)
                                    normalWorld = -branchPoint.grabVector;
                                else
                                    normalWorld = direction;
                                
                                normals[finalVertCount] = worldToLocalMatrix.MultiplyVector(normalWorld);

                                uvs[finalVertCount] = new Vector2(
                                    branchPoint.length * uvScale.y + uvOffset.y - stepSize,
                                    (1f / sides) * v * uvScale.x + uvOffset.x);
                                
                                // Store runtime data
                                var vertexForRuntime = direction * radius + (branchPoint.point - root.position);
                                // Note: Logic for vertexForRuntime preserved from original, though it looks like it mixes spaces
                                
                                branchPoint.verticesLoop.Add(new RTVertexData(
                                    vertexForRuntime, 
                                    normals[finalVertCount],
                                    uvs[finalVertCount], 
                                    Vector2.zero, 
                                    colors[finalVertCount]));

                                finalVertCount++;
                                lastVertCount++;
                            }
                        }
                    }
                    // Last point (Tip)
                    else if (generateBranches)
                    {
                        verts[finalVertCount] = worldToLocalMatrix.MultiplyPoint3x4(branchPoint.point);

                        Vector3 normalWorld;
                        if (halfGeom && sides == 1)
                            normalWorld = -branchPoint.grabVector;
                        else
                            normalWorld = (branchPoint.point - branch.branchPoints[p - 1].point).normalized;

                        normals[finalVertCount] = worldToLocalMatrix.MultiplyVector(normalWorld);
                        
                        uvs[finalVertCount] = new Vector2(
                            branch.totalLenght * uvScale.y + uvOffset.y,
                            0.5f * uvScale.x + uvOffset.x);

                        // Store runtime data
                        // Original logic used centerVertexPosition logic here, replicating simplified version
                        var centerVertexPosition = worldToLocalMatrix.MultiplyPoint3x4(branchPoint.point);

                        branchPoint.verticesLoop.Add(new RTVertexData(
                            centerVertexPosition, 
                            normals[finalVertCount],
                            uvs[finalVertCount], 
                            Vector2.zero, 
                            colors[finalVertCount]));

                        finalVertCount++;
                        lastVertCount++;

                        TriangulateBranch(infoPool, b, ref finalBranchTriCount, finalVertCount, lastVertCount);
                    }
                }
            }

            if (par.generateLeaves)
                BuildLeaves(infoPool, ref finalVertCount);
        }

        private static void BuildLeaves(InfoPool infoPool, ref int vertCount)
        {
            var par = infoPool.ivyParameters;

            // Iterate by Material Index (optimized logic)
            for (var i = 0; i < UniqueMaterials.Count; i++)
            {
                if (!LeavesByMaterialIndex.TryGetValue(i, out var leavesInBucket)) continue;

                foreach (var leafData in leavesInBucket)
                {
                    // Re-init random state per leaf based on original logic logic
                    // Original: Random.InitState(b + seed + i)
                    // We need to preserve deterministic look, so we use stored branch index
                    Random.InitState(leafData.branchIndex + par.randomSeed + i);

                    var currentLeaf = leafData.leaf;
                    var branch = infoPool.ivyContainer.branches[leafData.branchIndex];

                    // Optimization: Reuse list
                    if (currentLeaf.verticesLeaves == null)
                        currentLeaf.verticesLeaves = new List<RTVertexData>();
                    else
                        currentLeaf.verticesLeaves.Clear();

                    // Fetch cached mesh data
                    var cache = PrefabCache[currentLeaf.chosenLeave];
                    
                    // --- Orientation Calculations ---
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
                    
                    // Combine rotations
                    quat = Quaternion.AngleAxis(par.rotation.x, left) *
                           Quaternion.AngleAxis(par.rotation.y, currentLeaf.lpUpward) *
                           Quaternion.AngleAxis(par.rotation.z, forward) * quat;

                    // Add random rotation
                    quat = Quaternion.AngleAxis(Random.Range(-par.randomRotation.x, par.randomRotation.x), left) *
                           Quaternion.AngleAxis(Random.Range(-par.randomRotation.y, par.randomRotation.y), currentLeaf.lpUpward) *
                           Quaternion.AngleAxis(Random.Range(-par.randomRotation.z, par.randomRotation.z), forward) * quat;
                    
                    quat = currentLeaf.forwarRot * quat;

                    float scale = Random.Range(par.minScale, par.maxScale);
                    scale *= Mathf.InverseLerp(branch.totalLenght, branch.totalLenght - par.tipInfluence, currentLeaf.lpLength);

                    currentLeaf.leafScale = scale;
                    currentLeaf.leafRotation = quat;

                    // --- Geometry Construction ---

                    // Add triangles (offset by current vertCount)
                    var cachedTris = cache.triangles;
                    int triLen = cachedTris.Length;
                    var targetList = TrisLeaves[i];

                    // Adding simple integers is fast, but EnsureCapacity on the list beforehand helps
                    if (targetList.Capacity < targetList.Count + triLen)
                        targetList.Capacity = targetList.Count + triLen + 512;

                    for (var t = 0; t < triLen; t++)
                        targetList.Add(cachedTris[t] + vertCount);

                    // Add Vertices
                    var meshVerts = cache.mesh.vertices;
                    var meshNormals = cache.mesh.normals;
                    var meshUVs = cache.mesh.uv;
                    var meshColors = cache.mesh.colors;
                    bool hasColors = meshColors != null && meshColors.Length == meshVerts.Length;

                    Vector3 offset = left * par.offset.x +
                                     currentLeaf.lpUpward * par.offset.y +
                                     currentLeaf.lpForward * par.offset.z;

                    for (var v = 0; v < cache.vertexCount; v++)
                    {
                        // Calc World Pos
                        Vector3 worldPos = (quat * meshVerts[v] * scale) + currentLeaf.point + offset;
                        
                        // Transform to Local
                        verts[vertCount] = worldToLocalMatrix.MultiplyPoint3x4(worldPos);
                        
                        // Transform Normal
                        normals[vertCount] = worldToLocalMatrix.MultiplyVector(quat * meshNormals[v]);
                        
                        uvs[vertCount] = meshUVs[v];
                        colors[vertCount] = hasColors ? meshColors[v] : Color.white;

                        // Runtime Data Storage
                        currentLeaf.verticesLeaves.Add(new RTVertexData(
                            verts[vertCount], 
                            normals[vertCount], 
                            uvs[vertCount],
                            Vector2.zero, 
                            colors[vertCount]));

                        vertCount++;
                    }
                    
                    currentLeaf.leafCenter = worldToLocalMatrix.MultiplyPoint3x4(currentLeaf.point);
                }
            }
        }
        
        private static Vector3[] CalculateVectors(InfoPool infoPool, Transform root, int p, int b)
        {
            Vector3 firstVector;
            Vector3 axis;
            var branch = infoPool.ivyContainer.branches[b];

            if (b == 0 && p == 0)
            {
                axis = root.up;
                if (!infoPool.ivyParameters.halfgeom)
                    firstVector = infoPool.ivyContainer.firstVertexVector;
                else
                    firstVector = Quaternion.AngleAxis(90f, axis) * infoPool.ivyContainer.firstVertexVector;
            }
            else
            {
                if (p == 0)
                    axis = branch.branchPoints[1].point - branch.branchPoints[0].point;
                else
                    axis = Vector3.Lerp(
                        branch.branchPoints[p].point - branch.branchPoints[p - 1].point,
                        branch.branchPoints[p + 1].point - branch.branchPoints[p].point, 
                        0.5f).normalized;
                
                if (!infoPool.ivyParameters.halfgeom)
                    firstVector = Vector3.ProjectOnPlane(branch.branchPoints[p].grabVector, axis).normalized;
                else
                    firstVector = Quaternion.AngleAxis(90f, axis) 
                                  * Vector3.ProjectOnPlane(branch.branchPoints[p].grabVector, axis).normalized;
            }

            return new [] { firstVector, axis };
        }

        private static float CalculateRadius(InfoPool infoPool, float length)
        {
            float value = (Mathf.Sin(length * infoPool.ivyParameters.radiusVarFreq +
                                   infoPool.ivyParameters.radiusVarOffset) + 1f) * 0.5f;
            return Mathf.Lerp(infoPool.ivyParameters.minRadius, infoPool.ivyParameters.maxRadius, value);
        }

        private static float GetTipInfluence(InfoPool infoPool, float length, float totalLength)
        {
            float distFromEnd = totalLength - length;
            if (distFromEnd <= infoPool.ivyParameters.tipInfluence)
                 return Mathf.InverseLerp(totalLength, totalLength - infoPool.ivyParameters.tipInfluence, length - 0.1f);
            
            return 1.0f;
        }

        private static void TriangulateBranch(InfoPool infoPool, int b, ref int triCount, int vertCount, int lastVertCount)
        {
            int sides = infoPool.ivyParameters.sides;
            int sidesPlusOne = sides + 1;
            int pointsToTriangulate = infoPool.ivyContainer.branches[b].branchPoints.Count - 2;

            for (var round = 0; round < pointsToTriangulate; round++)
            {
                int roundOffset = round * sidesPlusOne;
                int baseVertIndex = vertCount - lastVertCount;

                for (var i = 0; i < sides; i++)
                {
                    int currentBase = i + roundOffset + baseVertIndex;

                    trisBranches[triCount]     = currentBase;
                    trisBranches[triCount + 1] = currentBase + 1;
                    trisBranches[triCount + 2] = currentBase + sidesPlusOne;

                    trisBranches[triCount + 3] = currentBase + 1;
                    trisBranches[triCount + 4] = currentBase + sides + 2;
                    trisBranches[triCount + 5] = currentBase + sidesPlusOne;
                    
                    triCount += 6;
                }
            }

            // Caps
            for (int t = 0, c = 0; t < sides * 3; t += 3, c++)
            {
                trisBranches[triCount] = vertCount - 1;
                trisBranches[triCount + 1] = vertCount - 3 - c;
                trisBranches[triCount + 2] = vertCount - 2 - c;
                triCount += 3;
            }
        }
    }
}