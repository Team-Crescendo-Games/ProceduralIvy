using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace TeamCrescendo.ProceduralIvy
{
    public static class EditorMeshBuilder
    {
        private const int InitialArraySize = 4096;
        private static Vector3[] verts = new Vector3[InitialArraySize];
        private static Vector3[] normals = new Vector3[InitialArraySize];
        private static Vector2[] uvs = new Vector2[InitialArraySize];
        private static Color[] colors = new Color[InitialArraySize];
        private static int[] trisBranches = new int[InitialArraySize];

        private static readonly List<List<int>> TrisLeaves = new ();
        private static readonly List<Material> LeavesMaterials = new ();
        private static readonly List<List<int>> TypesByMat = new ();
        
        // cached math variables
        private static float angle;
        private static Vector3 rootPos;
        private static Quaternion rootRotInv;
        
        public static bool Build(InfoPool infoPool, Transform root, MeshRenderer mr, Mesh targetMesh)
        {
            if (infoPool == null || root == null || mr == null || targetMesh == null)
                throw new ArgumentNullException();
            
            InitializeMaterials(infoPool, mr);

            CalculateCounts(infoPool, out int requiredVerts, out int requiredBranchTris);

            long limit = infoPool.ivyParameters.buffer32Bits ? 2147483647L : 65535L;
            if (requiredVerts > limit)
            {
                Debug.Log($"Vertex count exceeds limit. Required: {requiredVerts}, Limit: {limit}");
                return false;
            }

            // resize arrays if necessary
            EnsureArrayCapacity(ref verts, requiredVerts);
            EnsureArrayCapacity(ref normals, requiredVerts);
            EnsureArrayCapacity(ref uvs, requiredVerts);
            EnsureArrayCapacity(ref colors, requiredVerts);
            EnsureArrayCapacity(ref trisBranches, requiredBranchTris);

            // clear lists
            foreach (var list in TrisLeaves) list.Clear();

            CacheMathVariables(infoPool, root);

            BuildGeometry(infoPool, root, out int finalVertCount, out int finalBranchTriCount);

            // update mesh
            targetMesh.Clear();
            if (infoPool.ivyParameters.buffer32Bits) 
                targetMesh.indexFormat = IndexFormat.UInt32;

            targetMesh.SetVertices(verts, 0, finalVertCount);
            targetMesh.SetNormals(normals, 0, finalVertCount);
            targetMesh.SetUVs(0, uvs, 0, finalVertCount);
            targetMesh.SetColors(colors, 0, finalVertCount);

            targetMesh.subMeshCount = LeavesMaterials.Count + 1;
            targetMesh.SetTriangles(trisBranches, 0, finalBranchTriCount, 0);

            for (var i = 0; i < LeavesMaterials.Count; i++)
                targetMesh.SetTriangles(TrisLeaves[i], i + 1);

            targetMesh.RecalculateBounds();
            targetMesh.RecalculateTangents();
            
            return true;
        }

        private static void InitializeMaterials(InfoPool infoPool, MeshRenderer mr)
        {
            TypesByMat.Clear();
            LeavesMaterials.Clear();

            if (infoPool.ivyParameters.generateLeaves)
            {
                //Check for repeated materials within prefabs
                for (var i = 0; i < infoPool.ivyParameters.leavesPrefabs.Length; i++)
                {
                    var materialExists = false;
                    for (var m = 0; m < LeavesMaterials.Count; m++)
                    {
                        if (LeavesMaterials[m] == infoPool.ivyParameters.leavesPrefabs[i]
                                .GetComponent<MeshRenderer>().sharedMaterial)
                        {
                            TypesByMat[m].Add(i);
                            materialExists = true;
                        }
                    }

                    if (!materialExists)
                    {
                        LeavesMaterials.Add(infoPool.ivyParameters.leavesPrefabs[i]
                            .GetComponent<MeshRenderer>().sharedMaterial);
                        TypesByMat.Add(new List<int>());
                        TypesByMat[^1].Add(i);
                    }
                }

                //Assign materials to the mesh renderer once collected from prefabs
                var materials = new Material[LeavesMaterials.Count + 1];
                for (var i = 0; i < materials.Length; i++)
                {
                    if (i == 0)
                        materials[i] = mr.sharedMaterial;
                    else
                        materials[i] = LeavesMaterials[i - 1];
                }

                mr.sharedMaterials = materials;
            }
            else
            {
                mr.sharedMaterials = new[] { infoPool.ivyParameters.branchesMaterial };
            }
            
            while (TrisLeaves.Count < LeavesMaterials.Count) TrisLeaves.Add(new List<int>());
        }
        
        private static void EnsureArrayCapacity<T>(ref T[] array, int requiredSize)
        {
            if (array == null || array.Length < requiredSize)
            {
                // Resize to required + 20% buffer to prevent constant resizing
                int newSize = Mathf.Max(requiredSize, (int)(array.Length * 1.5f));
                newSize = Mathf.Max(newSize, 4096); // Minimum floor
                Array.Resize(ref array, newSize);
            }
        }
        
        private static void CalculateCounts(InfoPool infoPool, out int vCount, out int tCount)
        {
            vCount = 0;
            tCount = 0;
            
            if (infoPool.ivyParameters.generateBranches)
            {
                //Count necessary verts and tris and make room in arrays. On this side, the branches
                for (var i = 0; i < infoPool.ivyContainer.branches.Count; i++)
                    if (infoPool.ivyContainer.branches[i].branchPoints.Count > 1)
                    {
                        vCount += (infoPool.ivyContainer.branches[i].branchPoints.Count - 1) *
                            (infoPool.ivyParameters.sides + 1) + 1;
                        tCount +=
                            (infoPool.ivyContainer.branches[i].branchPoints.Count - 2) * infoPool.ivyParameters.sides *
                            2 * 3 + infoPool.ivyParameters.sides * 3;
                    }
            }

            if (infoPool.ivyParameters.generateLeaves && infoPool.ivyParameters.leavesPrefabs.Length > 0)
            {
                //And on this side, the leaves, depending on the mesh of each prefab
                for (var i = 0; i < infoPool.ivyContainer.branches.Count; i++)
                {
                    if (infoPool.ivyContainer.branches[i].branchPoints.Count > 1)
                        for (var j = 0; j < infoPool.ivyContainer.branches[i].leaves.Count; j++)
                        {
                            var currentBranch = infoPool.ivyContainer.branches[i];
                            //BranchPoint currentBranchPoint = infoPool.ivyContainer.branches[i].branchPoints[j];
                            var leafMeshFilter = infoPool.ivyParameters
                                .leavesPrefabs[currentBranch.leaves[j].chosenLeave].GetComponent<MeshFilter>();
                            vCount += leafMeshFilter.sharedMesh.vertexCount;
                        }
                }
            }
        }

        private static void BuildGeometry(InfoPool infoPool, Transform ivyRootTransform, out int finalVertCount, out int finalBranchTriCount)
        {
            //These counters track where we are calculating vertices and triangles, since we calculate everything in one go, not branch by branch
            finalVertCount = 0;
            finalBranchTriCount = 0;

            //Iterate each branch and define the first vertex to write in the array, taken from vertCount updated in the previous iteration
            for (var b = 0; b < infoPool.ivyContainer.branches.Count; b++)
            {
                Random.InitState(b + infoPool.ivyParameters.randomSeed);
                var branch = infoPool.ivyContainer.branches[b];
                if (branch.branchPoints.Count > 1)
                {
                    //Store how many vertices the current branch has in this counter, to account for it in the next one and know which vertices to write
                    var lastVertCount = 0;
                    //Iterate each point of the branch up to the penultimate one
                    for (var p = 0; p < branch.branchPoints.Count; p++)
                    {
                        var branchPoint = branch.branchPoints[p];
                        branchPoint.verticesLoop = new List<RTVertexData>();

                        var centerVertexPosition =
                            branchPoint.point - rootPos;
                        centerVertexPosition = rootRotInv * centerVertexPosition;
                        var radius = CalculateRadius(infoPool, branchPoint.length,
                            branch.totalLenght);

                        branchPoint.radius = radius;

                        if (p != branch.branchPoints.Count - 1)
                        {
                            //In this array, the method puts firstVector at index 0 and ring rotation axis at index 1
                            var vectors = CalculateVectors(infoPool, ivyRootTransform, p, b);

                            branchPoint.firstVector = vectors[0];
                            branchPoint.axis = vectors[1];

                            for (var v = 0; v < infoPool.ivyParameters.sides + 1; v++)
                                if (infoPool.ivyParameters.generateBranches)
                                {
                                    //BranchPoint branchPoint = branch.branchPoints[p];
                                    var tipInfluence = GetTipInfluence(infoPool, branchPoint.length,
                                        branch.totalLenght);
                                    branch.branchPoints[p].radius = radius;

                                    var quat = Quaternion.AngleAxis(angle * v, vectors[1]);
                                    var direction = quat * vectors[0];
                                    //Exception for normal calculation if we have half geometry and 1 side
                                    if (infoPool.ivyParameters.halfgeom && infoPool.ivyParameters.sides == 1)
                                        normals[finalVertCount] = -branch.branchPoints[p]
                                            .grabVector;
                                    else
                                        normals[finalVertCount] = direction;

                                    var vertexForRuntime = direction * radius + centerVertexPosition;

                                    verts[finalVertCount] = direction * radius * tipInfluence +
                                                       branch.branchPoints[p].point;
                                    verts[finalVertCount] -= rootPos;
                                    verts[finalVertCount] = rootRotInv * verts[finalVertCount];
                                    
                                    uvs[finalVertCount] =
                                        new Vector2(
                                            branchPoint.length * infoPool.ivyParameters.uvScale.y +
                                            infoPool.ivyParameters.uvOffset.y - infoPool.ivyParameters.stepSize,
                                            1f / infoPool.ivyParameters.sides * v *
                                            infoPool.ivyParameters.uvScale.x + infoPool.ivyParameters.uvOffset.x);

                                    normals[finalVertCount] = rootRotInv * normals[finalVertCount];
                                    
                                    var vertexData = new RTVertexData(vertexForRuntime, normals[finalVertCount],
                                        uvs[finalVertCount], Vector2.zero, colors[finalVertCount]);
                                    branchPoint.verticesLoop.Add(vertexData);


                                    //Update these counters to know where we were writing in the array for the next pass
                                    finalVertCount++;
                                    lastVertCount++;
                                }
                        }
                        //If it's the last point, instead of calculating the ring, use the last point to write the last vertex of this branch
                        else
                        {
                            if (infoPool.ivyParameters.generateBranches)
                            {
                                verts[finalVertCount] = branch.branchPoints[p].point;
                                //Local space correction
                                verts[finalVertCount] -= rootPos;
                                verts[finalVertCount] = rootRotInv * verts[finalVertCount];

                                //Exception for normals in the case of half geometry and only 1 side
                                if (infoPool.ivyParameters.halfgeom && infoPool.ivyParameters.sides == 1)
                                    normals[finalVertCount] =
                                        -branch.branchPoints[p].grabVector;
                                else
                                    normals[finalVertCount] = Vector3.Normalize(
                                        branch.branchPoints[p].point -
                                        branch.branchPoints[p - 1].point);
                                uvs[finalVertCount] = new Vector2(
                                    branch.totalLenght *
                                    infoPool.ivyParameters.uvScale.y + infoPool.ivyParameters.uvOffset.y,
                                    0.5f * infoPool.ivyParameters.uvScale.x + infoPool.ivyParameters.uvOffset.x);

                                normals[finalVertCount] = rootRotInv * normals[finalVertCount];

                                var vertexForRuntime = centerVertexPosition;

                                var vertexData = new RTVertexData(vertexForRuntime, normals[finalVertCount],
                                    uvs[finalVertCount], Vector2.zero, colors[finalVertCount]);
                                branchPoint.verticesLoop.Add(vertexData);

                                //Update these counters to know where we were writing in the array for the next pass
                                finalVertCount++;
                                lastVertCount++;

                                //And after placing the last vertex, triangulate
                                TriangulateBranch(infoPool, b, ref finalBranchTriCount, finalVertCount, lastVertCount);
                            }
                        }
                    }
                }

                if (infoPool.ivyParameters.generateLeaves)
                    BuildLeaves(infoPool, b, ref finalVertCount);
            }
        }

        private static void CacheMathVariables(InfoPool infoPool, Transform ivyRootTransform)
        {
            if (!infoPool.ivyParameters.halfgeom)
                angle = Mathf.Rad2Deg * 2 * Mathf.PI / infoPool.ivyParameters.sides;
            else
                angle = Mathf.Rad2Deg * 2 * Mathf.PI / infoPool.ivyParameters.sides / 2;
            
            rootPos = ivyRootTransform.position;
            rootRotInv = Quaternion.Inverse(ivyRootTransform.rotation);
        }

        // this method is called branch by branch
        private static void BuildLeaves(InfoPool infoPool, int b, ref int vertCount)
        {
            for (var i = 0; i < LeavesMaterials.Count; i++)
            {
                Random.InitState(b + infoPool.ivyParameters.randomSeed + i);

                for (var j = 0; j < infoPool.ivyContainer.branches[b].leaves.Count; j++)
                {
                    var branch = infoPool.ivyContainer.branches[b];
                    var currentLeaf = branch.leaves[j];

                    //Now check if the leaf type at this point corresponds to the material we are iterating
                    if (TypesByMat[i].Contains(currentLeaf.chosenLeave))
                    {
                        currentLeaf.verticesLeaves = new List<RTVertexData>();
                        //See which leaf type corresponds to each point and grab that mesh
                        Mesh chosenLeaveMesh = infoPool.ivyParameters.leavesPrefabs[currentLeaf.chosenLeave]
                            .GetComponent<MeshFilter>().sharedMesh;
                        //Define the vertex where we need to start writing in the array
                        Vector3 left, forward;
                        Quaternion quat;
                        //Orientation calculations based on rotation options
                        if (!infoPool.ivyParameters.globalOrientation)
                        {
                            forward = currentLeaf.lpForward;
                            left = currentLeaf.left;
                        }
                        else
                        {
                            forward = infoPool.ivyParameters.globalRotation;
                            left = Vector3.Normalize(Vector3.Cross(infoPool.ivyParameters.globalRotation,
                                currentLeaf.lpUpward));
                        }

                        quat = Quaternion.LookRotation(currentLeaf.lpUpward, forward);
                        quat = Quaternion.AngleAxis(infoPool.ivyParameters.rotation.x, left) *
                               Quaternion.AngleAxis(infoPool.ivyParameters.rotation.y, currentLeaf.lpUpward) *
                               Quaternion.AngleAxis(infoPool.ivyParameters.rotation.z, forward) * quat;
                        quat =
                            Quaternion.AngleAxis(
                                Random.Range(-infoPool.ivyParameters.randomRotation.x,
                                    infoPool.ivyParameters.randomRotation.x), left) *
                            Quaternion.AngleAxis(
                                Random.Range(-infoPool.ivyParameters.randomRotation.y,
                                    infoPool.ivyParameters.randomRotation.y), currentLeaf.lpUpward) *
                            Quaternion.AngleAxis(
                                Random.Range(-infoPool.ivyParameters.randomRotation.z,
                                    infoPool.ivyParameters.randomRotation.z), forward) * quat;
                        quat = currentLeaf.forwarRot * quat;

                        var scale = Random.Range(infoPool.ivyParameters.minScale, infoPool.ivyParameters.maxScale);
                        scale *= Mathf.InverseLerp(branch.totalLenght,
                            branch.totalLenght - infoPool.ivyParameters.tipInfluence,
                            currentLeaf.lpLength);
                        
                        currentLeaf.leafScale = scale;
                        currentLeaf.leafRotation = quat;

                        //Put corresponding triangles into the array corresponding to the material being iterated
                        for (var t = 0; t < chosenLeaveMesh.triangles.Length; t++)
                        {
                            var triangle = chosenLeaveMesh.triangles[t] + vertCount;
                            TrisLeaves[i].Add(triangle);
                        }

                        //And vertices, normals and UVs, applying relevant transformations, updating counter to know where we are for next iteration
                        for (var v = 0; v < chosenLeaveMesh.vertexCount; v++)
                        {
                            var offset = left * infoPool.ivyParameters.offset.x +
                                         currentLeaf.lpUpward * infoPool.ivyParameters.offset.y +
                                         currentLeaf.lpForward * infoPool.ivyParameters.offset.z;

                            verts[vertCount] = quat * chosenLeaveMesh.vertices[v] * scale + currentLeaf.point + offset;
                            normals[vertCount] = quat * chosenLeaveMesh.normals[v];
                            uvs[vertCount] = chosenLeaveMesh.uv[v];
                            colors[vertCount] = chosenLeaveMesh.colors[v];

                            normals[vertCount] = rootRotInv * normals[vertCount];
                            verts[vertCount] -= rootPos;
                            verts[vertCount] = rootRotInv * verts[vertCount];

                            var vertexData = new RTVertexData(verts[vertCount], normals[vertCount], uvs[vertCount],
                                Vector2.zero, colors[vertCount]);
                            currentLeaf.verticesLeaves.Add(vertexData);

                            currentLeaf.leafCenter = currentLeaf.point - rootPos;
                            currentLeaf.leafCenter = rootRotInv * currentLeaf.leafCenter;

                            vertCount++;
                        }
                    }
                }
            }
        }
        
        //This calculates vectors for each ring calculation
        private static Vector3[] CalculateVectors(InfoPool infoPool, Transform ivyRootTransform, int p, int b)
        {
            //Declare ring's firstVector, the axis to rotate around, the rotation of each vertex
            Vector3 firstVector;
            Vector3 axis;
            //Define variables for the first point of the first branch
            if (b == 0 && p == 0)
            {
                axis = ivyRootTransform.up;
                //Exception for half geometry, so the arc aligns well with the ground
                if (!infoPool.ivyParameters.halfgeom)
                    firstVector = infoPool.ivyContainer.firstVertexVector;
                else
                    firstVector = Quaternion.AngleAxis(90f, axis) * infoPool.ivyContainer.firstVertexVector;
            }
            //For everything else, the axis is an interpolation of the previous and next segments to the point, and firstVector is a projection of grabVector onto the axis plane
            else
            {
                var branch = infoPool.ivyContainer.branches[b];
                if (p == 0)
                    axis = branch.branchPoints[1].point - branch.branchPoints[0].point;
                else
                    axis = Vector3.Normalize(Vector3.Lerp(
                        branch.branchPoints[p].point -
                        branch.branchPoints[p - 1].point,
                        branch.branchPoints[p + 1].point -
                        branch.branchPoints[p].point, 0.5f));
                
                if (!infoPool.ivyParameters.halfgeom)
                    firstVector = Vector3.Normalize(Vector3.ProjectOnPlane(branch.branchPoints[p].grabVector, axis));
                else
                    firstVector = Quaternion.AngleAxis(90f, axis) 
                                  * Vector3.Normalize(Vector3.ProjectOnPlane(branch.branchPoints[p].grabVector, axis));
            }

            return new [] { firstVector, axis };
        }

        //Radius calculation based on distance traveled by the branch at that point, it's not complex, skipping explanation
        private static float CalculateRadius(InfoPool infoPool, float lenght, float totalLenght)
        {
            var value = (Mathf.Sin(lenght * infoPool.ivyParameters.radiusVarFreq +
                                   infoPool.ivyParameters.radiusVarOffset) + 1f) / 2f;
            var radius = Mathf.Lerp(infoPool.ivyParameters.minRadius, infoPool.ivyParameters.maxRadius, value);

            return radius;
        }

        private static float GetTipInfluence(InfoPool infoPool, float lenght, float totalLenght)
        {
            if (lenght - 0.1f >= totalLenght - infoPool.ivyParameters.tipInfluence)
                return Mathf.InverseLerp(totalLenght, totalLenght - infoPool.ivyParameters.tipInfluence, lenght - 0.1f);
            return 1.0f;
        }

        //Triangulation algorithm, using branch point count, global triangle counter, global vertex counter, and vertex count of the last branch.
        private static void TriangulateBranch(InfoPool infoPool, int b, ref int triCount, int vertCount, int lastVertCount)
        {
            //Do a round for each branch point up to the penultimate one
            for (var round = 0; round < infoPool.ivyContainer.branches[b].branchPoints.Count - 2; round++)
            {
                //And for each round do a pass on each side of the branch
                for (var i = 0; i < infoPool.ivyParameters.sides; i++)
                {
                    //Assign indices to each slot in the tri array with the algorithm. To write in correct slots, add total vertices and subtract those from the last branch to start in the correct place
                    trisBranches[triCount] =
                        i + round * (infoPool.ivyParameters.sides + 1) + vertCount - lastVertCount;
                    trisBranches[triCount + 1] =
                        i + round * (infoPool.ivyParameters.sides + 1) + 1 + vertCount - lastVertCount;
                    trisBranches[triCount + 2] = i + round * (infoPool.ivyParameters.sides + 1) +
                        infoPool.ivyParameters.sides + 1 + vertCount - lastVertCount;

                    trisBranches[triCount + 3] =
                        i + round * (infoPool.ivyParameters.sides + 1) + 1 + vertCount - lastVertCount;
                    trisBranches[triCount + 4] = i + round * (infoPool.ivyParameters.sides + 1) +
                        infoPool.ivyParameters.sides + 2 + vertCount - lastVertCount;
                    trisBranches[triCount + 5] = i + round * (infoPool.ivyParameters.sides + 1) +
                        infoPool.ivyParameters.sides + 1 + vertCount - lastVertCount;
                    triCount += 6;
                }
            }

            //Here come the cap triangles
            for (int t = 0, c = 0; t < infoPool.ivyParameters.sides * 3; t += 3, c++)
            {
                trisBranches[triCount] = vertCount - 1;
                trisBranches[triCount + 1] = vertCount - 3 - c;
                trisBranches[triCount + 2] = vertCount - 2 - c;
                triCount += 3;
            }
        }
    }
}