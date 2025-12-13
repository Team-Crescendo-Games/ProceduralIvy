using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TeamCrescendo.ProceduralIvy
{
    public class EditorMeshBuilder : ScriptableObject
    {
        public InfoPool infoPool;
        private Transform ivyRootTransform;

        //The final mesh of the ivy as a whole
        public Mesh ivyMesh;
        public Vector3[] verts;
        private Vector3[] normals;
        private int[] trisBranches;
        private Vector2[] uvs;
        private Color[] vColor;
        
        public List<Material> leavesMaterials;

        public bool leavesDataInitialized;

        //Angle for the generation of each ring
        private float angle;

        //Dictionary used in lightmap UV generation
        private readonly Dictionary<int, int[]> branchesLeavesIndices = new();

        //Leaf triangles, divided by material of each leaf type to make submeshes. Could be done with arrays, but taking liberties given the complexity
        private List<List<int>> trisLeaves;

        //Here we store which leaf types correspond to each material
        public List<List<int>> typesByMat;

        //Here leaf structures are initialized before starting to generate the ivy and geometry
        public void InitLeavesData(Transform rootTransform, MeshRenderer mr)
        {
            if (rootTransform == null || mr == null)
            {
                Debug.LogWarning("Invalid root transform or mesh renderer: " + rootTransform + " " + mr);
                return;
            }

            ivyRootTransform = rootTransform;
            
            typesByMat = new List<List<int>>();
            leavesMaterials = new List<Material>();

            if (infoPool.ivyParameters.generateLeaves)
            {
                //Check for repeated materials within prefabs
                for (var i = 0; i < infoPool.ivyParameters.leavesPrefabs.Length; i++)
                {
                    var materialExists = false;
                    for (var m = 0; m < leavesMaterials.Count; m++)
                    {
                        if (leavesMaterials[m] == infoPool.ivyParameters.leavesPrefabs[i]
                                .GetComponent<MeshRenderer>().sharedMaterial)
                        {
                            typesByMat[m].Add(i);
                            materialExists = true;
                        }
                    }

                    if (!materialExists)
                    {
                        leavesMaterials.Add(infoPool.ivyParameters.leavesPrefabs[i]
                            .GetComponent<MeshRenderer>().sharedMaterial);
                        typesByMat.Add(new List<int>());
                        typesByMat[^1].Add(i);
                    }
                }

                //Assign materials to the mesh renderer once collected from prefabs
                var materials = new Material[leavesMaterials.Count + 1];
                for (var i = 0; i < materials.Length; i++)
                {
                    if (i == 0)
                        materials[i] = mr.sharedMaterial;
                    else
                        materials[i] = leavesMaterials[i - 1];
                }

                mr.sharedMaterials = materials;
            }
            else
            {
                mr.sharedMaterials = new [] { infoPool.ivyParameters.branchesMaterial };
            }

            leavesDataInitialized = true;
        }
        
        public void InitializeMeshBuilder()
        {
            //Reset leaf triangles in each iteration
            trisLeaves = new List<List<int>>();
            for (var i = 0; i < leavesMaterials.Count; i++)
                trisLeaves.Add(new List<int>());

            //Reset the mesh and define the number of materials
            ivyMesh.Clear();
            if (infoPool.ivyParameters.buffer32Bits) 
                ivyMesh.indexFormat = IndexFormat.UInt32;
            ivyMesh.name = "Ivy Mesh";
            ivyMesh.subMeshCount = leavesMaterials.Count + 1;
            //And also the dictionary used in lightmap UV creation
            branchesLeavesIndices.Clear();

            //These counters are to calculate how many slots are needed in vertex and tri arrays
            var vertCount = 0;
            var triBranchesCount = 0;
            if (infoPool.ivyParameters.generateBranches)
            {
                //Count necessary verts and tris and make room in arrays. On this side, the branches
                for (var i = 0; i < infoPool.ivyContainer.branches.Count; i++)
                    if (infoPool.ivyContainer.branches[i].branchPoints.Count > 1)
                    {
                        vertCount += (infoPool.ivyContainer.branches[i].branchPoints.Count - 1) *
                            (infoPool.ivyParameters.sides + 1) + 1;
                        triBranchesCount +=
                            (infoPool.ivyContainer.branches[i].branchPoints.Count - 2) * infoPool.ivyParameters.sides *
                            2 * 3 + infoPool.ivyParameters.sides * 3;
                    }
            }

            if (infoPool.ivyParameters.generateLeaves && infoPool.ivyParameters.leavesPrefabs.Length > 0)
            {
                //And on this side, the leaves, depending on the mesh of each prefab
                for (var i = 0; i < infoPool.ivyContainer.branches.Count; i++)
                    if (infoPool.ivyContainer.branches[i].branchPoints.Count > 1)
                        for (var j = 0; j < infoPool.ivyContainer.branches[i].leaves.Count; j++)
                        {
                            var currentBranch = infoPool.ivyContainer.branches[i];
                            //BranchPoint currentBranchPoint = infoPool.ivyContainer.branches[i].branchPoints[j];
                            var leafMeshFilter = infoPool.ivyParameters
                                .leavesPrefabs[currentBranch.leaves[j].chosenLeave].GetComponent<MeshFilter>();
                            vertCount += leafMeshFilter.sharedMesh.vertexCount;
                        }
            }

            // create arrays for all mesh data (except leaf triangles which are added on the fly, as they are lists)
            verts = new Vector3[vertCount];
            normals = new Vector3[vertCount];
            uvs = new Vector2[vertCount];
            vColor = new Color[vertCount];
            trisBranches = new int[Mathf.Max(triBranchesCount, 0)];

            // calculate angle
            if (!infoPool.ivyParameters.halfgeom)
                angle = Mathf.Rad2Deg * 2 * Mathf.PI / infoPool.ivyParameters.sides;
            else
                angle = Mathf.Rad2Deg * 2 * Mathf.PI / infoPool.ivyParameters.sides / 2;
        }

        //Here leaves are built, this method is called branch by branch
        private void BuildLeaves(int b, ref int vertCount)
        {
            for (var i = 0; i < leavesMaterials.Count; i++)
            {
                Random.InitState(b + infoPool.ivyParameters.randomSeed + i);

                for (var j = 0; j < infoPool.ivyContainer.branches[b].leaves.Count; j++)
                {
                    var currentLeaf = infoPool.ivyContainer.branches[b].leaves[j];

                    //Now check if the leaf type at this point corresponds to the material we are iterating
                    if (typesByMat[i].Contains(currentLeaf.chosenLeave))
                    {
                        currentLeaf.verticesLeaves = new List<RTVertexData>();
                        //See which leaf type corresponds to each point and grab that mesh
                        Mesh chosenLeaveMesh = infoPool.ivyParameters.leavesPrefabs[currentLeaf.chosenLeave]
                            .GetComponent<MeshFilter>().sharedMesh;
                        //Define the vertex where we need to start writing in the array
                        var firstVertex = vertCount;
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

                        //Here the scale, which is simple, including tip influence
                        var scale = Random.Range(infoPool.ivyParameters.minScale, infoPool.ivyParameters.maxScale);
                        currentLeaf.leafScale = scale;

                        scale *= Mathf.InverseLerp(infoPool.ivyContainer.branches[b].totalLenght,
                            infoPool.ivyContainer.branches[b].totalLenght - infoPool.ivyParameters.tipInfluence,
                            currentLeaf.lpLength);

                        currentLeaf.leafRotation = quat;
                        currentLeaf.dstScale = scale;

                        //Put corresponding triangles into the array corresponding to the material being iterated
                        for (var t = 0; t < chosenLeaveMesh.triangles.Length; t++)
                        {
                            var triangle = chosenLeaveMesh.triangles[t] + vertCount;
                            trisLeaves[i].Add(triangle);
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
                            vColor[vertCount] = chosenLeaveMesh.colors[v];

                            normals[vertCount] = Quaternion.Inverse(ivyRootTransform.rotation) *
                                                 normals[vertCount];
                            verts[vertCount] -= ivyRootTransform.position;
                            verts[vertCount] = Quaternion.Inverse(ivyRootTransform.rotation) *
                                               verts[vertCount];

                            var vertexData = new RTVertexData(verts[vertCount], normals[vertCount], uvs[vertCount],
                                Vector2.zero, vColor[vertCount]);
                            currentLeaf.verticesLeaves.Add(vertexData);

                            currentLeaf.leafCenter = currentLeaf.point - ivyRootTransform.position;
                            currentLeaf.leafCenter =
                                Quaternion.Inverse(ivyRootTransform.rotation) *
                                currentLeaf.leafCenter;

                            vertCount++;
                        }

                        //Write the index where we stopped into the dictionary to later transform UVs of each element according to its real dimension
                        var fromTo = new int[2] { firstVertex, vertCount - 1 };
                        branchesLeavesIndices.Add(branchesLeavesIndices.Count, fromTo);
                    }
                }
            }
        }

        public void BuildGeometry()
        {
            if (leavesDataInitialized)
            {
                InitializeMeshBuilder();
                //These counters track where we are calculating vertices and triangles, since we calculate everything in one go, not branch by branch
                var vertCount = 0;
                var triBranchesCount = 0;

                //Iterate each branch and define the first vertex to write in the array, taken from vertCount updated in the previous iteration
                for (var b = 0; b < infoPool.ivyContainer.branches.Count; b++)
                {
                    var firstVertex = vertCount;
                    Random.InitState(b + infoPool.ivyParameters.randomSeed);
                    if (infoPool.ivyContainer.branches[b].branchPoints.Count > 1)
                    {
                        //Store how many vertices the current branch has in this counter, to account for it in the next one and know which vertices to write
                        var lastVertCount = 0;
                        //Iterate each point of the branch up to the penultimate one
                        for (var p = 0; p < infoPool.ivyContainer.branches[b].branchPoints.Count; p++)
                        {
                            var branchPoint = infoPool.ivyContainer.branches[b].branchPoints[p];
                            branchPoint.verticesLoop = new List<RTVertexData>();

                            var centerVertexPosition =
                                branchPoint.point - ivyRootTransform.position;
                            centerVertexPosition = Quaternion.Inverse(ivyRootTransform.rotation) *
                                                   centerVertexPosition;
                            var radius = CalculateRadius(branchPoint.length,
                                infoPool.ivyContainer.branches[b].totalLenght);

                            branchPoint.radius = radius;

                            if (p != infoPool.ivyContainer.branches[b].branchPoints.Count - 1)
                            {
                                //In this array, the method puts firstVector at index 0 and ring rotation axis at index 1
                                var vectors = CalculateVectors(infoPool.ivyContainer.branches[b].branchPoints[p].point,
                                    p, b);

                                branchPoint.firstVector = vectors[0];
                                branchPoint.axis = vectors[1];


                                for (var v = 0; v < infoPool.ivyParameters.sides + 1; v++)
                                    if (infoPool.ivyParameters.generateBranches)
                                    {
                                        //BranchPoint branchPoint = infoPool.ivyContainer.branches[b].branchPoints[p];
                                        var tipInfluence = GetTipInfluence(branchPoint.length,
                                            infoPool.ivyContainer.branches[b].totalLenght);
                                        infoPool.ivyContainer.branches[b].branchPoints[p].radius = radius;

                                        var quat = Quaternion.AngleAxis(angle * v, vectors[1]);
                                        var direction = quat * vectors[0];
                                        //Exception for normal calculation if we have half geometry and 1 side
                                        if (infoPool.ivyParameters.halfgeom && infoPool.ivyParameters.sides == 1)
                                            normals[vertCount] = -infoPool.ivyContainer.branches[b].branchPoints[p]
                                                .grabVector;
                                        else
                                            normals[vertCount] = direction;

                                        var vertexForRuntime = direction * radius + centerVertexPosition;

                                        verts[vertCount] = direction * radius * tipInfluence +
                                                           infoPool.ivyContainer.branches[b].branchPoints[p].point;
                                        verts[vertCount] -= ivyRootTransform.position;
                                        verts[vertCount] =
                                            Quaternion.Inverse(ivyRootTransform.rotation) *
                                            verts[vertCount];
                                        
                                        uvs[vertCount] =
                                            new Vector2(
                                                branchPoint.length * infoPool.ivyParameters.uvScale.y +
                                                infoPool.ivyParameters.uvOffset.y - infoPool.ivyParameters.stepSize,
                                                1f / infoPool.ivyParameters.sides * v *
                                                infoPool.ivyParameters.uvScale.x + infoPool.ivyParameters.uvOffset.x);

                                        normals[vertCount] = Quaternion.Inverse(ivyRootTransform.rotation) * normals[vertCount];
                                        
                                        var vertexData = new RTVertexData(vertexForRuntime, normals[vertCount],
                                            uvs[vertCount], Vector2.zero, vColor[vertCount]);
                                        branchPoint.verticesLoop.Add(vertexData);


                                        //Update these counters to know where we were writing in the array for the next pass
                                        vertCount++;
                                        lastVertCount++;
                                    }
                            }
                            //If it's the last point, instead of calculating the ring, use the last point to write the last vertex of this branch
                            else
                            {
                                if (infoPool.ivyParameters.generateBranches)
                                {
                                    verts[vertCount] = infoPool.ivyContainer.branches[b].branchPoints[p].point;
                                    //Local space correction
                                    verts[vertCount] -= ivyRootTransform.position;
                                    verts[vertCount] =
                                        Quaternion.Inverse(ivyRootTransform.rotation) *
                                        verts[vertCount];

                                    //Exception for normals in the case of half geometry and only 1 side
                                    if (infoPool.ivyParameters.halfgeom && infoPool.ivyParameters.sides == 1)
                                        normals[vertCount] =
                                            -infoPool.ivyContainer.branches[b].branchPoints[p].grabVector;
                                    else
                                        normals[vertCount] = Vector3.Normalize(
                                            infoPool.ivyContainer.branches[b].branchPoints[p].point -
                                            infoPool.ivyContainer.branches[b].branchPoints[p - 1].point);
                                    uvs[vertCount] = new Vector2(
                                        infoPool.ivyContainer.branches[b].totalLenght *
                                        infoPool.ivyParameters.uvScale.y + infoPool.ivyParameters.uvOffset.y,
                                        0.5f * infoPool.ivyParameters.uvScale.x + infoPool.ivyParameters.uvOffset.x);

                                    normals[vertCount] =
                                        Quaternion.Inverse(ivyRootTransform.rotation) *
                                        normals[vertCount];

                                    var vertexForRuntime = centerVertexPosition;

                                    var vertexData = new RTVertexData(vertexForRuntime, normals[vertCount],
                                        uvs[vertCount], Vector2.zero, vColor[vertCount]);
                                    branchPoint.verticesLoop.Add(vertexData);

                                    //Update these counters to know where we were writing in the array for the next pass
                                    vertCount++;
                                    lastVertCount++;

                                    //And after placing the last vertex, triangulate
                                    TriangulateBranch(b, ref triBranchesCount, vertCount, lastVertCount);
                                }
                            }
                        }
                    }

                    //Write the index where we stopped into the dictionary to later transform UVs of each element according to its real dimension
                    var fromTo = new int[2] { firstVertex, vertCount - 1 };
                    branchesLeavesIndices.Add(branchesLeavesIndices.Count, fromTo);

                    if (infoPool.ivyParameters.generateLeaves)
                        //infoPool.ivyContainer.branches[b].ClearRuntimeVerticesLeaves();
                        BuildLeaves(b, ref vertCount);
                }

                //And pass vertices and tris to the mesh
                ivyMesh.vertices = verts;
                ivyMesh.normals = normals;
                ivyMesh.uv = uvs;
                ivyMesh.colors = vColor;
                ivyMesh.SetTriangles(trisBranches, 0);
                //For each material, put leaf triangles into the corresponding submesh
                for (var i = 0; i < leavesMaterials.Count; i++) ivyMesh.SetTriangles(trisLeaves[i], i + 1);
                ivyMesh.RecalculateTangents();
                ivyMesh.RecalculateBounds();
            }
        }


        //This calculates vectors for each ring calculation
        private Vector3[] CalculateVectors(Vector3 branchPoint, int p, int b)
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
                if (p == 0)
                    axis = infoPool.ivyContainer.branches[b].branchPoints[p + 1].point -
                           infoPool.ivyContainer.branches[b].branchPoints[p].point;
                else
                    axis = Vector3.Normalize(Vector3.Lerp(
                        infoPool.ivyContainer.branches[b].branchPoints[p].point -
                        infoPool.ivyContainer.branches[b].branchPoints[p - 1].point,
                        infoPool.ivyContainer.branches[b].branchPoints[p + 1].point -
                        infoPool.ivyContainer.branches[b].branchPoints[p].point, 0.5f));
                if (!infoPool.ivyParameters.halfgeom)
                    firstVector =
                        Vector3.Normalize(
                            Vector3.ProjectOnPlane(infoPool.ivyContainer.branches[b].branchPoints[p].grabVector, axis));
                else
                    firstVector = Quaternion.AngleAxis(90f, axis) * Vector3.Normalize(
                        Vector3.ProjectOnPlane(infoPool.ivyContainer.branches[b].branchPoints[p].grabVector, axis));
            }

            //Return the calculated vectors

            return new Vector3[2] { firstVector, axis };
        }

        //Radius calculation based on distance traveled by the branch at that point, it's not complex, skipping explanation
        private float CalculateRadius(float lenght, float totalLenght)
        {
            var value = (Mathf.Sin(lenght * infoPool.ivyParameters.radiusVarFreq +
                                   infoPool.ivyParameters.radiusVarOffset) + 1f) / 2f;
            var radius = Mathf.Lerp(infoPool.ivyParameters.minRadius, infoPool.ivyParameters.maxRadius, value);

            //I don't remember why I put this -0.1f here :S
            /*if (lenght - 0.1f >= totalLenght - infoPool.ivyParameters.tipInfluence) {
                radius *= Mathf.InverseLerp (totalLenght, totalLenght - infoPool.ivyParameters.tipInfluence, lenght - 0.1f);
            }*/
            return radius;
        }

        private float GetTipInfluence(float lenght, float totalLenght)
        {
            var res = 1.0f;

            if (lenght - 0.1f >= totalLenght - infoPool.ivyParameters.tipInfluence)
                res = Mathf.InverseLerp(totalLenght, totalLenght - infoPool.ivyParameters.tipInfluence, lenght - 0.1f);

            return res;
        }

        //Triangulation algorithm, using branch point count, global triangle counter, global vertex counter, and vertex count of the last branch.
        private void TriangulateBranch(int b, ref int triCount, int vertCount, int lastVertCount)
        {
            //Do a round for each branch point up to the penultimate one
            for (var round = 0; round < infoPool.ivyContainer.branches[b].branchPoints.Count - 2; round++)
                //And for each round do a pass on each side of the branch
            for (var i = 0; i < infoPool.ivyParameters.sides; i++)
            {
                //Assign indices to each slot in the tri array with the algorithm. To write in correct slots, add total vertices and subtract those from the last branch to start in the correct place
                trisBranches[triCount] = i + round * (infoPool.ivyParameters.sides + 1) + vertCount - lastVertCount;
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

            //Here come the cap triangles
            for (int t = 0, c = 0; t < infoPool.ivyParameters.sides * 3; t += 3, c++)
            {
                trisBranches[triCount] = vertCount - 1;
                trisBranches[triCount + 1] = vertCount - 3 - c;
                trisBranches[triCount + 2] = vertCount - 2 - c;
                triCount += 3;
            }
        }

#if UNITY_EDITOR
        public void GenerateLMUVs()
        {
            if (ivyMesh) Unwrapping.GenerateSecondaryUVSet(ivyMesh);
        }
#endif
    }
}