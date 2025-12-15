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

            for (int i = 0; i < stats.branchCount; i++)
            {
                var branch = ivyContainer.branches[i];
                if (branch == null) continue;

                // Points
                if (branch.branchPoints != null)
                {
                    int pCount = branch.branchPoints.Count;
                    stats.pointCount += pCount;
                }

                // Leaves
                if (branch.leaves != null)
                {
                    int lCount = branch.leaves.Count;
                    stats.leafCount += lCount;
                    
                    for (int k = 0; k < lCount; k++)
                    {
                        var lp = branch.leaves[k];
                        if (lp.vertices != null)
                        {
                            int vCount = lp.vertices.Count;
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