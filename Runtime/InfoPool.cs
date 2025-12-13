using System;
using System.Collections.Generic;
using UnityEngine;

namespace TeamCrescendo.ProceduralIvy
{
    [Serializable]
    [PreferBinarySerialization]
    public class InfoPool : ScriptableObject
    {
        public IvyContainer ivyContainer;
        public IvyParameters ivyParameters;
#if UNITY_EDITOR
        public EditorMeshBuilder meshBuilder;
        public EditorIvyGrowth growth;
        
        public struct IvyMemoryStats
        {
            public int branchCount;
            public int pointCount;
            public int leafCount;
            public int vertexCount;
            public float memoryKB;
        }
        
        public IvyMemoryStats GetMemoryStats()
        {
            var stats = new IvyMemoryStats();
            
            if (ivyContainer == null || ivyContainer.branches == null) return stats;

            const int SIZE_RT_VERTEX = 68;
            const int SIZE_BRANCH = 128; 
            const int SIZE_POINT = 160;
            const int SIZE_LEAF = 220;

            long totalBytes = 0;

            stats.branchCount = ivyContainer.branches.Count;
            totalBytes += stats.branchCount * SIZE_BRANCH;

            // Loop is optimized to avoid GC allocs
            for (int i = 0; i < stats.branchCount; i++)
            {
                var branch = ivyContainer.branches[i];
                if (branch == null) continue;

                // Points
                if (branch.branchPoints != null)
                {
                    int pCount = branch.branchPoints.Count;
                    stats.pointCount += pCount;
                    totalBytes += pCount * SIZE_POINT;

                    for (int j = 0; j < pCount; j++)
                    {
                        var bp = branch.branchPoints[j];
                        if (bp.verticesLoop != null)
                        {
                            int vCount = bp.verticesLoop.Count;
                            stats.vertexCount += vCount;
                            totalBytes += vCount * SIZE_RT_VERTEX;
                        }
                    }
                }

                // Leaves
                if (branch.leaves != null)
                {
                    int lCount = branch.leaves.Count;
                    stats.leafCount += lCount;
                    totalBytes += lCount * SIZE_LEAF;
                    
                    for (int k = 0; k < lCount; k++)
                    {
                        var lp = branch.leaves[k];
                        if (lp.verticesLeaves != null)
                        {
                            int vCount = lp.verticesLeaves.Count;
                            stats.vertexCount += vCount;
                            totalBytes += vCount * SIZE_RT_VERTEX;
                        }
                    }
                }
            }

            stats.memoryKB = totalBytes / 1024f;
            return stats;
        }
#endif
    }
}