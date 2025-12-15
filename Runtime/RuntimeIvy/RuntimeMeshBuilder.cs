using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace TeamCrescendo.ProceduralIvy
{
    public class RuntimeMeshBuilder
    {
        public readonly MeshData processedMeshData;
        
        private readonly IvyParameters ivyParameters;
        private readonly List<List<int>> processedBranchesVerticesIndicesPerBranch;
        private readonly List<List<int>> processedVerticesIndicesPerBranch;
        private readonly IvyContainer rtIvyContainer;

        private readonly int backtrackingPoints;
        private readonly MeshData[] leavesMeshesByChosenLeaf;
        private readonly Mesh processedMesh;
        private readonly int[] submeshByChosenLeaf;
        private readonly int submeshCount;
        
        public RuntimeMeshBuilder(IvyParameters ivyParameters, IvyContainer ivyContainer,
            int numBranches, Mesh processedMesh, Mesh originalMesh, MeshRenderer mrProcessedMesh, 
            int backtrackingPoints, int[] submeshByChosenLeaf, 
            MeshData[] leavesMeshesByChosenLeaf, List<Material> materials)
        {
            this.ivyParameters = ivyParameters;
            rtIvyContainer = ivyContainer;

            this.processedMesh = processedMesh;
            this.processedMesh.indexFormat = IndexFormat.UInt16;

            this.submeshByChosenLeaf = submeshByChosenLeaf;
            this.leavesMeshesByChosenLeaf = leavesMeshesByChosenLeaf;

            if (backtrackingPoints < 2)
                throw new ArgumentException($"[RTBakedMeshBuilder] Invalid backtracking points: {backtrackingPoints}");
            this.backtrackingPoints = backtrackingPoints;

            submeshCount = materials.Count;
            if (submeshCount == 0)
                throw new ArgumentException("[RTBakedMeshBuilder] Invalid submesh count: 0");

            processedVerticesIndicesPerBranch = new List<List<int>>(numBranches);
            processedBranchesVerticesIndicesPerBranch = new List<List<int>>(numBranches);

            for (var i = 0; i < numBranches; i++)
            {
                processedVerticesIndicesPerBranch.Add(new List<int>());
                processedBranchesVerticesIndicesPerBranch.Add(new List<int>());
            }

            var filteredMaterials = new List<Material> { materials[0] };

            if (ivyParameters.generateLeaves)
            {
                for (var i = 1; i < materials.Count; i++)
                    filteredMaterials.Add(materials[i]);
            }

            mrProcessedMesh.SetSharedMaterials(filteredMaterials);
            
            processedMeshData = new MeshData(originalMesh.vertexCount, originalMesh.subMeshCount);
        }

        public void CheckCopyMesh(int branchIndex, List<BranchContainer> bakedBranches)
        {
            var buildingBranch = rtIvyContainer.branches[branchIndex];
            var bakedBranch = bakedBranches[branchIndex];

            var initSegment = Mathf.Max(0, buildingBranch.branchPoints.Count - backtrackingPoints - 1);
            var endSegmentIndx = initSegment + 1;

            CopyToFixedMesh(branchIndex, initSegment, endSegmentIndx, buildingBranch, bakedBranch);
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
                
                if (rtBranchPoint == null) continue; // TODO: investigate why this could be null

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
                    if (leaves == null) continue; // TODO: investigate why this could be null
                    
                    for (var j = 0; j < leaves.Count; j++)
                    {
                        var currentLeaf = leaves[j];
                        if (currentLeaf == null) continue; // TODO: investigate why this could be null

                        var chosenLeaveMeshData = leavesMeshesByChosenLeaf[currentLeaf.chosenLeave];
                        var submesh = submeshByChosenLeaf[currentLeaf.chosenLeave];

                        for (var t = 0; t < chosenLeaveMeshData.triangles[0].Count; t++)
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
        
        public void RefreshProcessedMesh() => processedMeshData.Apply(processedMesh, submeshCount, ivyParameters.generateLeaves);
    }
}