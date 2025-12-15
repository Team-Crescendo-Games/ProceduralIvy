using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace TeamCrescendo.ProceduralIvy
{
    public class RTBakedMeshBuilder
    {
        private MeshData buildingMeshData;
        private readonly GameObject ivyGameObject;
        private readonly IvyParameters ivyParameters;
        private readonly List<List<int>> processedBranchesVerticesIndicesPerBranch;
        public MeshData processedMeshData;
        private readonly List<List<int>> processedVerticesIndicesPerBranch;
        private readonly IvyContainer rtIvyContainer;

        private int backtrackingPoints;
        private int endIdxLeaves;
        private int initIdxLeaves;
        private Mesh ivyMesh;
        private int lastLeafVertProcessed;
        private int lastPointCopied;
        private int lastVertCount;
        private int lastVertexIndex;
        private int lastVerticesCountProcessed;
        private MeshData[] leavesMeshesByChosenLeaf;
        private bool onOptimizedStretch;
        private Mesh processedMesh;
        private int[] submeshByChosenLeaf;
        private int submeshCount;
        private int vertCount;
        private int[] vertCountLeavesPerBranch;
        private int[] vertCountsPerBranch;

        public RTBakedMeshBuilder(IvyParameters ivyParameters, IvyContainer ivyContainer,
            RuntimeIvy rtIvy, int numBranches, Mesh processedMesh, 
            MeshRenderer mrProcessedMesh, int backtrackingPoints, int[] submeshByChosenLeaf, 
            MeshData[] leavesMeshesByChosenLeaf, List<Material> materials)
        {
            this.ivyParameters = ivyParameters;
            rtIvyContainer = ivyContainer;
            ivyGameObject = rtIvy.gameObject;

            this.processedMesh = processedMesh;
            this.processedMesh.indexFormat = IndexFormat.UInt16;

            this.submeshByChosenLeaf = submeshByChosenLeaf;
            this.leavesMeshesByChosenLeaf = leavesMeshesByChosenLeaf;

            if (backtrackingPoints < 2)
                throw new ArgumentException($"[RTBakedMeshBuilder] Invalid backtracking points: {backtrackingPoints}");
            this.backtrackingPoints = backtrackingPoints;

            submeshCount = materials.Count;

            vertCountsPerBranch = new int[numBranches];
            vertCountLeavesPerBranch = new int[numBranches];
            processedVerticesIndicesPerBranch = new List<List<int>>(numBranches);
            processedBranchesVerticesIndicesPerBranch = new List<List<int>>(numBranches);

            for (var i = 0; i < numBranches; i++)
            {
                processedVerticesIndicesPerBranch.Add(new List<int>());
                processedBranchesVerticesIndicesPerBranch.Add(new List<int>());
            }

            vertCount = 0;

            ivyMesh = new Mesh
            {
                subMeshCount = submeshCount,
                name = Constants.IVY_MESH_NAME
            };

            var filteredMaterials = new List<Material> { materials[0] };

            if (ivyParameters.generateLeaves)
            {
                for (var i = 1; i < materials.Count; i++)
                    filteredMaterials.Add(materials[i]);
            }

            mrProcessedMesh.SetSharedMaterials(filteredMaterials);
        }

        public void InitializeMeshesDataBaked(Mesh bakedMesh, int numBranches)
        {
            CreateBuildingMeshData(bakedMesh, numBranches);
            CreateProcessedMeshData(bakedMesh);
        }

        public void InitializeMeshesDataProcedural(Mesh bakedMesh, int numBranches, float lifetime, float velocity)
        {
            CreateBuildingMeshData(bakedMesh, numBranches);
            CreateProcessedMeshDataProcedural(bakedMesh, lifetime, velocity);
            bakedMesh.Clear();
        }

        private void CreateBuildingMeshData(Mesh bakedMesh, int numBranches)
        {
            if (submeshCount == 0)
                throw new ArgumentException("[RTBakedMeshBuilder] Invalid submesh count: 0");
            
            var numVerticesPerLoop = ivyParameters.sides + 1;
            var numVertices = (backtrackingPoints * numVerticesPerLoop + backtrackingPoints * 2 * 8) * numBranches;

            var numTrianglesPerSubmesh = new List<int>();

            var branchTrianglesNumber = ((backtrackingPoints - 2) * ivyParameters.sides * 6 + ivyParameters.sides * 3) * numBranches;
            numTrianglesPerSubmesh.Add(branchTrianglesNumber);

            for (var i = 1; i < submeshCount; i++)
            {
                var numTriangles = backtrackingPoints * 6 * numBranches;
                numTrianglesPerSubmesh.Add(numTriangles);
            }

            buildingMeshData = new MeshData(numVertices, submeshCount, numTrianglesPerSubmesh);
        }

        private void CreateProcessedMeshDataProcedural(Mesh bakedMesh, float lifetime, float velocity)
        {
            if (submeshCount == 0)
                throw new ArgumentException("[RTBakedMeshBuilder] Invalid submesh count: 0");
            
            var ratio = lifetime / velocity;
            var numPoints = Mathf.CeilToInt(ratio * 200);
            var numVertices = numPoints * (ivyParameters.sides + 1);
            var numTrianglesPerSubmesh = new List<int>();

            for (var i = 0; i < submeshCount; i++)
            {
                var numTriangles = ivyParameters.sides * numPoints * 9;
                numTrianglesPerSubmesh.Add(numTriangles);
            }

            processedMeshData = new MeshData(numVertices, submeshCount, numTrianglesPerSubmesh);
        }

        private void CreateProcessedMeshData(Mesh bakedMesh)
        {
            var numVertices = bakedMesh.vertexCount;
            var bakedMeshSubMeshCount = bakedMesh.subMeshCount;
            var numTrianglesPerSubmesh = new List<int>(bakedMeshSubMeshCount);

            for (var i = 0; i < bakedMeshSubMeshCount; i++)
                numTrianglesPerSubmesh.Add(bakedMesh.GetTriangles(i).Length);

            processedMeshData = new MeshData(numVertices, bakedMeshSubMeshCount, numTrianglesPerSubmesh);
        }

        private void ClearTipMesh()
        {
            buildingMeshData.Clear();
            for (var i = 0; i < vertCountsPerBranch.Length; i++)
            {
                vertCountsPerBranch[i] = 0;
                vertCountLeavesPerBranch[i] = 0;
            }
            vertCount = 0;
        }

        public void CheckCopyMesh(int branchIndex, List<BranchContainer> bakedBranches)
        {
            var buildingBranch = rtIvyContainer.branches[branchIndex];
            var bakedBranch = bakedBranches[branchIndex];

            var initSegment = Mathf.Max(0, buildingBranch.branchPoints.Count - backtrackingPoints - 1);
            var endSegmentIndx = initSegment + 1;

            CopyToFixedMesh(branchIndex, initSegment, endSegmentIndx, buildingBranch, bakedBranch);
        }

        public void BuildGeometry(List<BranchContainer> activeBakedBranches, List<BranchContainer> activeBuildingBranches)
        {
            ClearTipMesh();

            for (var b = 0; b < rtIvyContainer.branches.Count; b++)
            {
                var currentBranch = activeBuildingBranches[b];

                if (currentBranch.branchPoints.Count > 1)
                {
                    lastVertCount = 0;

                    var initIndexPoint = Mathf.Max(0, currentBranch.branchPoints.Count - backtrackingPoints);
                    var endIndexPoint = currentBranch.branchPoints.Count;

                    for (var p = initIndexPoint; p < endIndexPoint; p++)
                    {
                        var currentBranchPoint = currentBranch.branchPoints[p];
                        var centerLoop = ivyGameObject.transform.InverseTransformPoint(currentBranchPoint.point);
                        
                        var tipInfluenceFactor = Mathf.InverseLerp(currentBranch.totalLength,
                            currentBranch.totalLength - ivyParameters.tipInfluence,
                            currentBranchPoint.length);

                        if (p < currentBranch.branchPoints.Count - 1)
                        {
                            for (var i = 0; i < currentBranchPoint.verticesLoop.Count; i++)
                            {
                                if (!ivyParameters.generateBranches) continue;

                                var vertex = Vector3.LerpUnclamped(currentBranchPoint.centerLoop,
                                    currentBranchPoint.verticesLoop[i].vertex, tipInfluenceFactor);

                                buildingMeshData.AddVertex(vertex, currentBranchPoint.verticesLoop[i].normal,
                                    currentBranchPoint.verticesLoop[i].uv,
                                    currentBranchPoint.verticesLoop[i].color32);

                                vertCountsPerBranch[b]++;
                                vertCount++;
                                lastVertCount++;
                            }
                        }
                        else
                        {
                            if (ivyParameters.generateBranches)
                            {
                                var vertex = centerLoop;
                                var normal = Vector3.Normalize(currentBranchPoint.point -
                                                           currentBranchPoint.GetPreviousPoint().point);
                                normal = ivyGameObject.transform.InverseTransformVector(normal);

                                var uv = currentBranch.GetLastUV(ivyParameters);

                                buildingMeshData.AddVertex(vertex, normal, uv, Color.black);

                                vertCountsPerBranch[b]++;
                                vertCount++;
                                lastVertCount++;
                            }
                        }
                    }

                    if (ivyParameters.generateBranches) SetTriangles(currentBranch, vertCount, initIndexPoint, b);
                }

                if (ivyParameters.generateLeaves)
                    BuildLeaves(b, activeBuildingBranches[b], activeBakedBranches[b]);
            }

            RefreshMesh();
        }

        private void SetTriangles(BranchContainer branch, int vertCount, int initIndex, int branchIndex)
        {
            var initRound = 0;
            var endRound = Mathf.Min(branch.branchPoints.Count - 2, branch.branchPoints.Count - initIndex - 2);

            for (var round = initRound; round < endRound; round++)
            {
                for (var i = 0; i < ivyParameters.sides; i++)
                {
                    var offset = vertCount - lastVertCount;
                    var baseIndex = i + round * (ivyParameters.sides + 1) + offset;

                    var v0 = baseIndex;
                    var v1 = baseIndex + 1;
                    var v2 = baseIndex + ivyParameters.sides + 1;
                    var v3 = baseIndex + 1;
                    var v4 = baseIndex + ivyParameters.sides + 2;
                    var v5 = baseIndex + ivyParameters.sides + 1;

                    buildingMeshData.AddTriangle(0, v0);
                    buildingMeshData.AddTriangle(0, v1);
                    buildingMeshData.AddTriangle(0, v2);

                    buildingMeshData.AddTriangle(0, v3);
                    buildingMeshData.AddTriangle(0, v4);
                    buildingMeshData.AddTriangle(0, v5);
                }
            }

            for (int t = 0, c = 0; t < ivyParameters.sides * 3; t += 3, c++)
            {
                buildingMeshData.AddTriangle(0, vertCount - 1);
                buildingMeshData.AddTriangle(0, vertCount - 3 - c);
                buildingMeshData.AddTriangle(0, vertCount - 2 - c);
            }
        }

        private void BuildLeaves(int branchIndex, BranchContainer buildingBranchContainer, BranchContainer bakedBranchContainer)
        {
            var firstPointIdx = Mathf.Max(0, buildingBranchContainer.branchPoints.Count - backtrackingPoints);

            for (var i = firstPointIdx; i < buildingBranchContainer.branchPoints.Count; i++)
            {
                var leaves = bakedBranchContainer.leavesOrderedByInitSegment[i];
                if (leaves == null)
                    continue;
                
                var rtBranchPoint = buildingBranchContainer.branchPoints[i];

                for (var j = 0; j < leaves.Length; j++)
                {
                    var currentLeaf = leaves[j];
                    if (currentLeaf == null) continue;

                    var tipInfluenceFactor = Mathf.InverseLerp(buildingBranchContainer.totalLength,
                        buildingBranchContainer.totalLength - ivyParameters.tipInfluence,
                        rtBranchPoint.length);

                    var chosenLeaveMeshData = leavesMeshesByChosenLeaf[currentLeaf.chosenLeave];

                    for (var t = 0; t < chosenLeaveMeshData.triangles[0].Length; t++)
                    {
                        var triangleValue = chosenLeaveMeshData.triangles[0][t] + vertCount;
                        var submesh = submeshByChosenLeaf[currentLeaf.chosenLeave];
                        buildingMeshData.AddTriangle(submesh, triangleValue);
                    }

                    for (var v = 0; v < currentLeaf.vertices.Count; v++)
                    {
                        var vertex = Vector3.LerpUnclamped(currentLeaf.leafCenter, currentLeaf.vertices[v].vertex,
                            tipInfluenceFactor);

                        buildingMeshData.AddVertex(vertex, currentLeaf.vertices[v].normal, currentLeaf.vertices[v].uv,
                            currentLeaf.vertices[v].color32);

                        vertCountLeavesPerBranch[branchIndex]++;
                        vertCountsPerBranch[branchIndex]++;
                        vertCount++;
                    }
                }
            }
        }

        private void CopyToFixedMesh(int branchIndex, int initSegmentIdx, int endSegmentIdx, BranchContainer branchContainer, BranchContainer bakedBranchContainer)
        {
            var numVerticesPerLoop = ivyParameters.sides + 1;
            var numLoopsToProcess = 1;

            if (processedBranchesVerticesIndicesPerBranch[branchIndex].Count <= 0)
            {
                numLoopsToProcess = 2;
            }

            for (var i = numLoopsToProcess - 1; i >= 0; i--)
            {
                var index = branchContainer.branchPoints.Count - backtrackingPoints - i;
                var rtBranchPoint = branchContainer.branchPoints[index];

                for (var j = 0; j < rtBranchPoint.verticesLoop.Count; j++)
                {
                    var vertexData = rtBranchPoint.verticesLoop[j];
                    processedMeshData.AddVertex(vertexData.vertex, vertexData.normal, vertexData.uv, vertexData.color32);
                    processedBranchesVerticesIndicesPerBranch[branchIndex].Add(processedMeshData.VertexCount() - 1);
                }
            }

            if (processedBranchesVerticesIndicesPerBranch[branchIndex].Count >= numVerticesPerLoop * 2)
            {
                var initIdx = processedBranchesVerticesIndicesPerBranch[branchIndex].Count - numVerticesPerLoop * 2;

                for (var i = 0; i < ivyParameters.sides; i++)
                {
                    var branchIndices = processedBranchesVerticesIndicesPerBranch[branchIndex];
                    var v0 = branchIndices[i + initIdx];
                    var v1 = branchIndices[i + 1 + initIdx];
                    var v2 = branchIndices[i + ivyParameters.sides + 1 + initIdx];
                    var v3 = branchIndices[i + 1 + initIdx];
                    var v4 = branchIndices[i + ivyParameters.sides + 2 + initIdx];
                    var v5 = branchIndices[i + ivyParameters.sides + 1 + initIdx];

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

                for (var i = initSegmentIdx; i < endSegmentIdx; i++)
                {
                    var leaves = bakedBranchContainer.leavesOrderedByInitSegment[i];
                    if (leaves == null) continue;
                    
                    for (var j = 0; j < leaves.Length; j++)
                    {
                        var currentLeaf = leaves[j];
                        if (currentLeaf == null) continue;

                        var chosenLeaveMeshData = leavesMeshesByChosenLeaf[currentLeaf.chosenLeave];
                        var submesh = submeshByChosenLeaf[currentLeaf.chosenLeave];

                        for (var t = 0; t < chosenLeaveMeshData.triangles[0].Length; t++)
                        {
                            var triangleValue = chosenLeaveMeshData.triangles[0][t] + lastVertexLeafProcessed;
                            processedMeshData.AddTriangle(submesh, triangleValue);
                        }

                        for (var v = 0; v < currentLeaf.vertices.Count; v++)
                        {
                            var vertexData = currentLeaf.vertices[v];
                            processedMeshData.AddVertex(vertexData.vertex, vertexData.normal, vertexData.uv, vertexData.color32);
                            processedVerticesIndicesPerBranch[branchIndex].Add(processedMeshData.VertexCount() - 1);
                            lastVertexLeafProcessed++;
                        }
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
            processedMesh.colors32 = processedMeshData.colors32;
            processedMesh.uv = processedMeshData.uv;

            processedMesh.SetTriangles(processedMeshData.triangles[0], 0);

            if (ivyParameters.generateLeaves)
            {
                for (var i = 1; i < submeshCount; i++)
                {
                    processedMesh.SetTriangles(processedMeshData.triangles[i], i);
                }
            }

            processedMesh.RecalculateBounds();
        }

        private void RefreshMesh()
        {
            ivyMesh.Clear();
            ivyMesh.subMeshCount = submeshCount;
            ivyMesh.MarkDynamic();

            ivyMesh.vertices = buildingMeshData.vertices;
            ivyMesh.normals = buildingMeshData.normals;
            ivyMesh.colors32 = buildingMeshData.colors32;
            ivyMesh.uv = buildingMeshData.uv;

            ivyMesh.SetTriangles(buildingMeshData.triangles[0], 0);

            if (ivyParameters.generateLeaves)
            {
                for (var i = 1; i < submeshCount; i++)
                    ivyMesh.SetTriangles(buildingMeshData.triangles[i], i);
            }

            ivyMesh.RecalculateBounds();
        }
    }
}