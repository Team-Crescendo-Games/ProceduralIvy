using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TeamCrescendo.ProceduralIvy
{
    public class RTBakedMeshBuilder
    {
        public List<RTBranchContainer> activeBranches;

        private int backtrackingPoints;

        private Color blackColor;
        public RTMeshData buildingMeshData;
        private int endIdxLeaves;

        private int[] fromTo;

        private int initIdxLeaves;
        public GameObject ivyGO;

        private Mesh ivyMesh;
        public IvyParameters ivyParameters;
        private int lastLeafVertProcessed;


        private int lastPointCopied;
        private int[] lastTriangleIndexPerBranch;

        private int lastVertCount;

        private int lastVertexIndex;
        private int lastVerticesCountProcessed;

        //Booleano para saber si están inicializadas las estructuras para las hojas, así no intenta construir la geometría sin tener lo necesario
        public bool leavesDataInitialized;

        private RTMeshData[] leavesMeshesByChosenLeaf;

        private MeshFilter leavesMeshFilter;
        private MeshRenderer leavesMeshRenderer;

        private bool onOptimizedStretch;
        public List<List<int>> processedBranchesVerticesIndicesPerBranch;

        private Mesh processedMesh;
        public RTMeshData processedMeshData;

        public List<List<int>> processedVerticesIndicesPerBranch;
        public RTIvyContainer rtIvyContainer;
        private int[] submeshByChoseLeaf;

        private int submeshCount;

        private int vertCount;
        private int[] vertCountLeavesPerBranch;

        private int[] vertCountsPerBranch;
        private Vector2 zeroVector2;
        private Vector3 zeroVector3;

        public RTBakedMeshBuilder(RTIvyContainer ivyContainer, GameObject ivyGo)
        {
            rtIvyContainer = ivyContainer;
            ivyGO = ivyGo;
        }


        public void InitializeMeshBuilder(IvyParameters ivyParameters, RTIvyContainer ivyContainer,
            RTIvyContainer bakedIvyContainer, GameObject ivyGO, Mesh bakedMesh, MeshRenderer meshRenderer,
            MeshFilter meshFilter, int numBranches, Mesh processedMesh, float growSpeed, MeshRenderer mrProcessedMesh,
            int backtrackingPoints, int[] submeshByChoseLeaf, RTMeshData[] leavesMeshesByChosenLeaf,
            Material[] materials)
        {
            this.ivyParameters = ivyParameters;
            rtIvyContainer = ivyContainer;
            this.ivyGO = ivyGO;

            this.processedMesh = processedMesh;
            this.processedMesh.indexFormat = IndexFormat.UInt16;

            this.submeshByChoseLeaf = submeshByChoseLeaf;
            this.leavesMeshesByChosenLeaf = leavesMeshesByChosenLeaf;

            activeBranches = new List<RTBranchContainer>();

            fromTo = new int[2];

            this.backtrackingPoints = backtrackingPoints;

            submeshCount = meshRenderer.sharedMaterials.Length;

            vertCountsPerBranch = new int[numBranches];
            lastTriangleIndexPerBranch = new int[numBranches];
            vertCountLeavesPerBranch = new int[numBranches];
            processedVerticesIndicesPerBranch = new List<List<int>>(numBranches);
            processedBranchesVerticesIndicesPerBranch = new List<List<int>>(numBranches);

            for (var i = 0; i < numBranches; i++)
            {
                processedVerticesIndicesPerBranch.Add(new List<int>());
                processedBranchesVerticesIndicesPerBranch.Add(new List<int>());
            }

            vertCount = 0;

            ivyMesh = new Mesh();
            ivyMesh.subMeshCount = submeshCount;
            ivyMesh.name = Constants.IVY_MESH_NAME;

            meshFilter.mesh = ivyMesh;


            var filteredMaterials = new List<Material>();
            filteredMaterials.Add(materials[0]); //Bark material

            if (ivyParameters.generateLeaves)
                for (var i = 1; i < materials.Length; i++)
                    filteredMaterials.Add(materials[i]);

            var filteredMaterialArray = filteredMaterials.ToArray();


            ivyGO.GetComponent<MeshRenderer>().sharedMaterials = filteredMaterialArray;
            mrProcessedMesh.sharedMaterials = filteredMaterialArray;


            leavesDataInitialized = true;

            zeroVector3 = Vector3.zero;
            zeroVector2 = Vector2.zero;
            blackColor = Color.black;
        }

        public void InitializeMeshesDataBaked(Mesh bakedMesh, int numBranches)
        {
            CreateBuildingMeshData(bakedMesh, numBranches);
            CreateProcessedMeshData(bakedMesh);

            bakedMesh.Clear();
        }

        public void InitializeMeshesDataProcedural(Mesh bakedMesh, int numBranches, float lifetime, float velocity)
        {
            CreateBuildingMeshData(bakedMesh, numBranches);
            CreateProcessedMeshDataProcedural(bakedMesh, lifetime, velocity);

            bakedMesh.Clear();
        }

        public void CreateBuildingMeshData(Mesh bakedMesh, int numBranches)
        {
            var numVerticesPerLoop = ivyParameters.sides + 1;
            var numVertices = backtrackingPoints * numVerticesPerLoop + backtrackingPoints * 2 * 8;
            numVertices *= numBranches;

            var subMeshCount = bakedMesh.subMeshCount;
            var numTrianglesPerSubmesh = new List<int>();

            var branchTrianglesNumber = (backtrackingPoints - 2) * ivyParameters.sides * 6 + ivyParameters.sides * 3;
            branchTrianglesNumber *= numBranches;

            numTrianglesPerSubmesh.Add(branchTrianglesNumber);

            for (var i = 1; i < subMeshCount; i++)
            {
                var numTriangles = backtrackingPoints * 6 * numBranches;
                numTrianglesPerSubmesh.Add(numTriangles);
            }


            buildingMeshData = new RTMeshData(numVertices, subMeshCount, numTrianglesPerSubmesh);
        }

        public void CreateProcessedMeshDataProcedural(Mesh bakedMesh, float lifetime, float velocity)
        {
            var ratio = lifetime / velocity;

            var numPoints = Mathf.CeilToInt(ratio * 200);


            var numVertices = numPoints * (ivyParameters.sides + 1);
            var submeshCount = bakedMesh.subMeshCount;

            var numTrianglesPerSubmesh = new List<int>();
            for (var i = 0; i < submeshCount; i++)
            {
                var numTriangles = ivyParameters.sides * numPoints * 9;
                numTrianglesPerSubmesh.Add(numTriangles);
            }

            processedMeshData = new RTMeshData(numVertices, submeshCount, numTrianglesPerSubmesh);
        }

        public void CreateProcessedMeshData(Mesh bakedMesh)
        {
            var numVertices = bakedMesh.vertexCount;
            var submeshCount = bakedMesh.subMeshCount;

            var numTrianglesPerSubmesh = new List<int>();
            for (var i = 0; i < submeshCount; i++)
            {
                var numTriangles = bakedMesh.GetTriangles(i).Length;
                numTrianglesPerSubmesh.Add(numTriangles);
            }

            processedMeshData = new RTMeshData(numVertices, submeshCount, numTrianglesPerSubmesh);
        }

        private void ClearTipMesh()
        {
            buildingMeshData.Clear();
            for (var i = 0; i < vertCountsPerBranch.Length; i++)
            {
                vertCountsPerBranch[i] = 0;
                lastTriangleIndexPerBranch[i] = 0;
                vertCountLeavesPerBranch[i] = 0;
            }

            vertCount = 0;
        }

        public void CheckCopyMesh(int branchIndex, List<RTBranchContainer> bakedBranches)
        {
            var buildingBranch = rtIvyContainer.branches[branchIndex];
            var bakedBranch = bakedBranches[branchIndex];

            var initSegment = buildingBranch.branchPoints.Count - backtrackingPoints - 1;
            initSegment = Mathf.Clamp(initSegment, 0, int.MaxValue);

            var initSegmentIdx = initSegment;
            var endSegmentIndx = initSegment + 1;

            CopyToFixedMesh(branchIndex, initSegmentIdx, endSegmentIndx, buildingBranch, bakedBranch);
        }

        public void BuildGeometry02(List<RTBranchContainer> activeBakedBranches,
            List<RTBranchContainer> activeBuildingBranches)
        {
            if (leavesDataInitialized)
            {
                ClearTipMesh();

                //Recorremos cada rama y definimos el primer vértice que tenemos que escribir del array, recogido del vertcount actualizado en la iteración anterior
                for (var b = 0; b < rtIvyContainer.branches.Count; b++)
                {
                    var firstVertex = vertCount;
                    var currentBranch = activeBuildingBranches[b];

                    if (currentBranch.branchPoints.Count > 1)
                    {
                        lastVertCount = 0;

                        //Recorremos cada punto de la rama hasta el penúltimo
                        var initIndexPoint = currentBranch.branchPoints.Count - backtrackingPoints;
                        initIndexPoint = Mathf.Clamp(initIndexPoint, 0, int.MaxValue);

                        var endIndexPoint = currentBranch.branchPoints.Count;

                        for (var p = initIndexPoint; p < endIndexPoint; p++)
                        {
                            var currentBranchPoint = currentBranch.branchPoints[p];

                            var centerLoop = ivyGO.transform.InverseTransformPoint(currentBranchPoint.point);

                            var vertex = zeroVector3;
                            var normal = zeroVector3;
                            var uv = zeroVector2;

                            var tipInfluenceFactor = Mathf.InverseLerp(currentBranch.totalLength,
                                currentBranch.totalLength - ivyParameters.tipInfluence,
                                currentBranchPoint.length);

                            if (p < currentBranch.branchPoints.Count - 1)
                            {
                                for (var i = 0; i < currentBranchPoint.verticesLoop.Length; i++)
                                    if (ivyParameters.generateBranches)
                                    {
                                        vertex = Vector3.LerpUnclamped(currentBranchPoint.centerLoop,
                                            currentBranchPoint.verticesLoop[i].vertex, tipInfluenceFactor);
                                        buildingMeshData.AddVertex(vertex, currentBranchPoint.verticesLoop[i].normal,
                                            currentBranchPoint.verticesLoop[i].uv,
                                            currentBranchPoint.verticesLoop[i].color);


                                        vertCountsPerBranch[b]++;
                                        vertCount++;
                                        lastVertCount++;
                                    }
                            }
                            else
                            {
                                if (ivyParameters.generateBranches)
                                {
                                    vertex = centerLoop;

                                    normal = Vector3.Normalize(currentBranchPoint.point -
                                                               currentBranchPoint.GetPreviousPoint().point);
                                    normal = ivyGO.transform.InverseTransformVector(normal);

                                    uv = currentBranch.GetLastUV(ivyParameters);

                                    buildingMeshData.AddVertex(vertex, normal, uv, Color.black);

                                    vertCountsPerBranch[b]++;
                                    vertCount++;
                                    lastVertCount++;
                                }
                            }
                        }

                        if (ivyParameters.generateBranches) SetTriangles(currentBranch, vertCount, initIndexPoint, b);
                    }

                    fromTo[0] = firstVertex;
                    fromTo[1] = vertCount - 1;

                    if (ivyParameters.generateLeaves) BuildLeaves(b, activeBuildingBranches[b], activeBakedBranches[b]);
                }

                RefreshMesh();
            }
        }


        private float CalculateRadius(BranchPoint branchPoint, BranchContainer buildingBranchContainer)
        {
            var tipInfluenceFactor = Mathf.InverseLerp(branchPoint.branchContainer.totalLenght,
                branchPoint.branchContainer.totalLenght - ivyParameters.tipInfluence, branchPoint.length - 0.1f);

            branchPoint.currentRadius = branchPoint.radius * tipInfluenceFactor;
            var radius = branchPoint.currentRadius;

            return radius;
        }

        private void SetTriangles(RTBranchContainer branch, int vertCount, int initIndex, int branchIndex)
        {
            var initRound = 0;
            var endRound = Mathf.Min(branch.branchPoints.Count - 2, branch.branchPoints.Count - initIndex - 2);

            for (var round = initRound; round < endRound; round++)
            for (var i = 0; i < ivyParameters.sides; i++)
            {
                var offset = vertCount - lastVertCount;

                var v0 = i + round * (ivyParameters.sides + 1) + offset;
                var v1 = i + round * (ivyParameters.sides + 1) + 1 + offset;
                var v2 = i + round * (ivyParameters.sides + 1) + ivyParameters.sides + 1 + offset;
                var v3 = i + round * (ivyParameters.sides + 1) + 1 + offset;
                var v4 = i + round * (ivyParameters.sides + 1) + ivyParameters.sides + 2 + offset;
                var v5 = i + round * (ivyParameters.sides + 1) + ivyParameters.sides + 1 + offset;

                buildingMeshData.AddTriangle(0, v0);
                buildingMeshData.AddTriangle(0, v1);
                buildingMeshData.AddTriangle(0, v2);

                buildingMeshData.AddTriangle(0, v3);
                buildingMeshData.AddTriangle(0, v4);
                buildingMeshData.AddTriangle(0, v5);
            }

            for (int t = 0, c = 0; t < ivyParameters.sides * 3; t += 3, c++)
            {
                buildingMeshData.AddTriangle(0, vertCount - 1);
                buildingMeshData.AddTriangle(0, vertCount - 3 - c);
                buildingMeshData.AddTriangle(0, vertCount - 2 - c);
            }

            lastTriangleIndexPerBranch[branchIndex] = vertCount - 1;
        }


        private void BuildLeaves(int branchIndex, RTBranchContainer buildingBranchContainer,
            RTBranchContainer bakedBranchContainer)
        {
            RTMeshData chosenLeaveMeshData;


            var firstPointIdx = buildingBranchContainer.branchPoints.Count - backtrackingPoints;
            firstPointIdx = Mathf.Clamp(firstPointIdx, 0, int.MaxValue);


            for (var i = firstPointIdx; i < buildingBranchContainer.branchPoints.Count; i++)
            {
                var leaves = bakedBranchContainer.leavesOrderedByInitSegment[i];
                var rtBranchPoint = buildingBranchContainer.branchPoints[i];

                for (var j = 0; j < leaves.Length; j++)
                {
                    var currentLeaf = leaves[j];


                    if (currentLeaf == null) continue;


                    var tipInfluenceFactor = Mathf.InverseLerp(buildingBranchContainer.totalLength,
                        buildingBranchContainer.totalLength - ivyParameters.tipInfluence,
                        rtBranchPoint.length);

                    chosenLeaveMeshData = leavesMeshesByChosenLeaf[currentLeaf.chosenLeave];


                    //Metemos los triángulos correspondientes en el array correspondiente al material que estamos iterando
                    for (var t = 0; t < chosenLeaveMeshData.triangles[0].Length; t++)
                    {
                        var triangleValue = chosenLeaveMeshData.triangles[0][t] + vertCount;

                        var submesh = submeshByChoseLeaf[currentLeaf.chosenLeave];
                        buildingMeshData.AddTriangle(submesh, triangleValue);
                    }

                    for (var v = 0; v < currentLeaf.vertices.Length; v++)
                    {
                        var vertex = Vector3.LerpUnclamped(currentLeaf.leafCenter, currentLeaf.vertices[v].vertex,
                            tipInfluenceFactor);

                        buildingMeshData.AddVertex(vertex, currentLeaf.vertices[v].normal, currentLeaf.vertices[v].uv,
                            currentLeaf.vertices[v].color);

                        vertCountLeavesPerBranch[branchIndex]++;
                        vertCountsPerBranch[branchIndex]++;
                        vertCount++;
                    }
                }
            }
        }


        public void CopyToFixedMesh(int branchIndex, int initSegmentIdx,
            int endSegmentIdx, RTBranchContainer branchContainer, RTBranchContainer bakedBranchContainer)
        {
            var numVerticesPerLoop = ivyParameters.sides + 1;
            var numTrianglesPerLoop = ivyParameters.sides * 6;
            var numLoopsToProcess = 1;
            var onlyBranchVertices = vertCountsPerBranch[branchIndex] - vertCountLeavesPerBranch[branchIndex];


            var vertexOffset = 0;
            for (var i = 1; i <= branchIndex; i++) vertexOffset += vertCountsPerBranch[branchIndex];

            if (processedBranchesVerticesIndicesPerBranch[branchIndex].Count <= 0)
            {
                numLoopsToProcess = 2;
            }
            else
            {
                numLoopsToProcess = 1;
                vertexOffset += numVerticesPerLoop;
            }


            for (var i = numLoopsToProcess - 1; i >= 0; i--)
            {
                var index = branchContainer.branchPoints.Count - backtrackingPoints - i;

                var rtBranchPoint = branchContainer.branchPoints[index];

                for (var j = 0; j < rtBranchPoint.verticesLoop.Length; j++)
                {
                    var vertexData = rtBranchPoint.verticesLoop[j];
                    processedMeshData.AddVertex(vertexData.vertex, vertexData.normal, vertexData.uv, vertexData.color);

                    processedBranchesVerticesIndicesPerBranch[branchIndex].Add(processedMeshData.VertexCount() - 1);
                }
            }


            var triangleIndexOffset = 0;
            if (branchIndex > 0) triangleIndexOffset = lastTriangleIndexPerBranch[branchIndex];

            if (processedBranchesVerticesIndicesPerBranch[branchIndex].Count >= numVerticesPerLoop * 2)
            {
                var initIdx = processedBranchesVerticesIndicesPerBranch[branchIndex].Count - numVerticesPerLoop * 2;


                for (var i = 0; i < ivyParameters.sides; i++)
                {
                    var v0 = processedBranchesVerticesIndicesPerBranch[branchIndex][i + initIdx];

                    var v1 = processedBranchesVerticesIndicesPerBranch[branchIndex][i + 1 + initIdx];

                    var v2 = processedBranchesVerticesIndicesPerBranch[branchIndex][
                        i + ivyParameters.sides + 1 + initIdx];

                    var v3 = processedBranchesVerticesIndicesPerBranch[branchIndex][i + 1 + initIdx];

                    var v4 = processedBranchesVerticesIndicesPerBranch[branchIndex][
                        i + ivyParameters.sides + 2 + initIdx];

                    var v5 = processedBranchesVerticesIndicesPerBranch[branchIndex][
                        i + ivyParameters.sides + 1 + initIdx];


                    processedMeshData.AddTriangle(0, v0);
                    processedMeshData.AddTriangle(0, v1);
                    processedMeshData.AddTriangle(0, v2);

                    processedMeshData.AddTriangle(0, v3);
                    processedMeshData.AddTriangle(0, v4);
                    processedMeshData.AddTriangle(0, v5);
                }
            }


            if (ivyParameters.generateLeaves)
            {
                var lastVertexLeafProcessed = processedMeshData.VertexCount();
                var numLeavesProcessed = 0;

                for (var i = initSegmentIdx; i < endSegmentIdx; i++)
                {
                    var leaves = bakedBranchContainer.leavesOrderedByInitSegment[i];
                    for (var j = 0; j < leaves.Length; j++)
                    {
                        var currentLeaf = leaves[j];

                        if (currentLeaf == null) continue;

                        var chosenLeaveMeshData = leavesMeshesByChosenLeaf[currentLeaf.chosenLeave];

                        var submesh = submeshByChoseLeaf[currentLeaf.chosenLeave];
                        for (var t = 0; t < chosenLeaveMeshData.triangles[0].Length; t++)
                        {
                            var triangleValue = chosenLeaveMeshData.triangles[0][t] + lastVertexLeafProcessed;
                            processedMeshData.AddTriangle(submesh, triangleValue);
                        }

                        for (var v = 0; v < currentLeaf.vertices.Length; v++)
                        {
                            var vertexData = currentLeaf.vertices[v];
                            processedMeshData.AddVertex(vertexData.vertex,
                                vertexData.normal, vertexData.uv,
                                vertexData.color);

                            processedVerticesIndicesPerBranch[branchIndex].Add(processedMeshData.VertexCount() - 1);

                            lastVertexLeafProcessed++;
                        }

                        numLeavesProcessed++;
                    }
                }
            }
        }

        public void RefreshProcessedMesh()
        {
            processedMesh.MarkDynamic();

            processedMesh.subMeshCount = submeshCount;

            processedMesh.vertices = processedMeshData.vertices;
            processedMesh.normals = processedMeshData.normals;
            processedMesh.colors = processedMeshData.colors;
            processedMesh.uv = processedMeshData.uv;


            processedMesh.SetTriangles(processedMeshData.triangles[0], 0);

            if (ivyParameters.generateLeaves)
                for (var i = 1; i < submeshCount; i++)
                    processedMesh.SetTriangles(processedMeshData.triangles[i], i);


            processedMesh.RecalculateBounds();
        }

        private void RefreshMesh()
        {
            ivyMesh.Clear();

            ivyMesh.subMeshCount = submeshCount;

            ivyMesh.MarkDynamic();


            ivyMesh.vertices = buildingMeshData.vertices;
            ivyMesh.normals = buildingMeshData.normals;
            ivyMesh.colors = buildingMeshData.colors;
            ivyMesh.uv = buildingMeshData.uv;


            ivyMesh.SetTriangles(buildingMeshData.triangles[0], 0);
            if (ivyParameters.generateLeaves)
                for (var i = 1; i < submeshCount; i++)
                    ivyMesh.SetTriangles(buildingMeshData.triangles[i], i);

            ivyMesh.RecalculateBounds();
        }
    }
}