using System;
using UnityEngine;
using UnityEngine.Profiling;

namespace TeamCrescendo.ProceduralIvy
{
    [Serializable]
    [PreferBinarySerialization]
    public class InfoPool : ScriptableObject
    {
        public IvyContainer ivyContainer;
        public IvyParameters ivyParameters;
        public Mesh mesh;
        
#if UNITY_EDITOR
        public struct IvyMemoryStats
        {
            public int branchCount;
            public int pointCount;
            public int leafCount;
            public int vertexCount;
            public long memoryBytes;
        }
        
        public IvyMemoryStats GetMemoryStats()
        {
            var stats = new IvyMemoryStats();
            if (ivyContainer == null || ivyContainer.branches == null) return stats;
            
            stats.branchCount = ivyContainer.branches.Count;

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

                    for (int j = 0; j < pCount; j++)
                    {
                        var bp = branch.branchPoints[j];
                        if (bp.verticesLoop != null)
                        {
                            int vCount = bp.verticesLoop.Count;
                            stats.vertexCount += vCount;
                        }
                    }
                }

                // Leaves
                if (branch.leaves != null)
                {
                    int lCount = branch.leaves.Count;
                    stats.leafCount += lCount;
                    
                    for (int k = 0; k < lCount; k++)
                    {
                        var lp = branch.leaves[k];
                        if (lp.verticesLeaves != null)
                        {
                            int vCount = lp.verticesLeaves.Count;
                            stats.vertexCount += vCount;
                        }
                    }
                }
            }

            stats.memoryBytes = Profiler.GetRuntimeMemorySizeLong(ivyContainer) + Profiler.GetRuntimeMemorySizeLong(mesh);
            return stats;
        }
#endif
    }
}