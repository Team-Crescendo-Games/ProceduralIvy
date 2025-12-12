using System;
using System.Collections.Generic;
using UnityEngine;

namespace TeamCrescendo.ProceduralIvy
{
    [Serializable]
    public class InfoPool : ScriptableObject
    {
        public IvyContainer ivyContainer;
        public IvyParameters ivyParameters;
        
        public MeshFilter GetMeshFilter() => ivyContainer.ivyGO.GetComponent<MeshFilter>();
        public MeshRenderer GetMeshRenderer() => ivyContainer.ivyGO.GetComponent<MeshRenderer>();
        
#if UNITY_EDITOR
        public EditorMeshBuilder meshBuilder;
        public EditorIvyGrowth growth;
#endif
        
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
    }

    [Serializable]
    public class IvyParameterInt : IvyParameter
    {
        public IvyParameterInt(int value)
        {
            this.value = value;
        }

        public override void UpdateValue(float value)
        {
            this.value = (int)value;
        }

        public static implicit operator int(IvyParameterInt intParameter)
        {
            return (int)intParameter.value;
        }

        public static implicit operator IvyParameterInt(int intValue)
        {
            return new IvyParameterInt(intValue);
        }
    }

    [Serializable]
    public class IvyParameterFloat : IvyParameter
    {
        public IvyParameterFloat(float value)
        {
            this.value = value;
        }

        public override void UpdateValue(float value)
        {
            this.value = value;
        }

        public static implicit operator float(IvyParameterFloat floatParameter)
        {
            return floatParameter.value;
        }

        public static implicit operator IvyParameterFloat(float floatValue)
        {
            return new IvyParameterFloat(floatValue);
        }
    }

    public abstract class IvyParameter
    {
        public float value;
        public abstract void UpdateValue(float value);
    }

    [Serializable]
    public class IvyParametersGUI : ScriptableObject
    {
        //Growth parameters
        public IvyParameterFloat stepSize = 0.1f;
        public IvyParameterFloat branchProvability = 0.05f;
        public IvyParameterInt maxBranchs = 5;
        public LayerMask layerMask = -1;
        public IvyParameterFloat minDistanceToSurface = 0.01f;
        public IvyParameterFloat maxDistanceToSurface = 0.03f;
        public IvyParameterFloat DTSFrequency = 1f;
        public IvyParameterFloat DTSRandomness = 0.2f;
        public IvyParameterFloat directionFrequency = 1f;
        public IvyParameterFloat directionAmplitude = 20f;
        public IvyParameterFloat directionRandomness = 1f;
        public IvyParameterFloat gravityX = 0f;
        public IvyParameterFloat gravityY = -1f;
        public IvyParameterFloat gravityZ = 0f;
        public IvyParameterFloat grabProvabilityOnFall = 0.1f;
        public IvyParameterFloat stiffness = 0.03f;
        public IvyParameterFloat optAngleBias = 15f;
        public IvyParameterInt leaveEvery = 1;
        public IvyParameterInt randomLeaveEvery = 1;

        //Geometry parameters
        public bool buffer32Bits;
        public bool halfgeom;
        public IvyParameterInt sides = 3;
        public IvyParameterFloat minRadius = 0.025f;
        public IvyParameterFloat maxRadius = 0.05f;
        public IvyParameterFloat radiusVarFreq = 1f;
        public IvyParameterFloat radiusVarOffset = 0f;
        public IvyParameterFloat tipInfluence = 0.5f;
        public IvyParameterFloat uvScaleX = 1f;
        public IvyParameterFloat uvScaleY = 1f;
        public IvyParameterFloat uvOffsetX = 0f;
        public IvyParameterFloat uvOffsetY = 0f;

        public IvyParameterFloat minScale = 0.7f;
        public IvyParameterFloat maxScale = 1.2f;
        public bool globalOrientation;
        public IvyParameterFloat globalRotationX = 0f;
        public IvyParameterFloat globalRotationY = -1f;
        public IvyParameterFloat globalRotationZ = 0f;
        public IvyParameterFloat rotationX = 0f;
        public IvyParameterFloat rotationY = 0f;
        public IvyParameterFloat rotationZ = 0f;
        public IvyParameterFloat randomRotationX = 0f;
        public IvyParameterFloat randomRotationY = 0f;
        public IvyParameterFloat randomRotationZ = 0f;
        public IvyParameterFloat offsetX = 0f;
        public IvyParameterFloat offsetY = 0f;
        public IvyParameterFloat offsetZ = 0f;
        public float LMUVPadding = 0.002f;
        public Material branchesMaterial;

        public List<GameObject> leavesPrefabs = new();
        public List<float> leavesProb = new();

        public bool generateBranches, generateLeaves, generateLightmapUVs;

        public void CopyFrom(IvyPreset ivyPreset)
        {
            CopyFrom(ivyPreset.ivyParameters);
        }

        public void CopyFrom(IvyParameters copyFrom)
        {
            stepSize = copyFrom.stepSize;
            branchProvability = copyFrom.branchProbability;
            maxBranchs = copyFrom.maxBranches;
            layerMask = copyFrom.layerMask;
            minDistanceToSurface = copyFrom.minDistanceToSurface;
            maxDistanceToSurface = copyFrom.maxDistanceToSurface;
            DTSFrequency = copyFrom.DTSFrequency;
            DTSRandomness = copyFrom.DTSRandomness;
            directionFrequency = copyFrom.directionFrequency;
            directionAmplitude = copyFrom.directionAmplitude;
            directionRandomness = copyFrom.directionRandomness;
            gravityX = copyFrom.gravity.x;
            gravityY = copyFrom.gravity.y;
            gravityZ = copyFrom.gravity.z;
            grabProvabilityOnFall = copyFrom.grabProvabilityOnFall;
            stiffness = copyFrom.stiffness;
            optAngleBias = copyFrom.optAngleBias;
            leaveEvery = copyFrom.leaveEvery;
            randomLeaveEvery = copyFrom.randomLeaveEvery;

            buffer32Bits = copyFrom.buffer32Bits;
            halfgeom = copyFrom.halfgeom;
            sides = copyFrom.sides;
            minRadius = copyFrom.minRadius;
            maxRadius = copyFrom.maxRadius;
            radiusVarFreq = copyFrom.radiusVarFreq;
            radiusVarOffset = copyFrom.radiusVarOffset;
            tipInfluence = copyFrom.tipInfluence;
            uvScaleX = copyFrom.uvScale.x;
            uvScaleY = copyFrom.uvScale.y;
            uvOffsetX = copyFrom.uvOffset.x;
            uvOffsetY = copyFrom.uvOffset.y;
            minScale = copyFrom.minScale;
            maxScale = copyFrom.maxScale;
            globalOrientation = copyFrom.globalOrientation;
            globalRotationX = copyFrom.globalRotation.x;
            globalRotationY = copyFrom.globalRotation.y;
            globalRotationZ = copyFrom.globalRotation.z;
            rotationX = copyFrom.rotation.x;
            rotationY = copyFrom.rotation.y;
            rotationZ = copyFrom.rotation.z;
            randomRotationX = copyFrom.randomRotation.x;
            randomRotationY = copyFrom.randomRotation.y;
            randomRotationZ = copyFrom.randomRotation.z;
            randomRotationX = copyFrom.randomRotation.x;
            randomRotationY = copyFrom.randomRotation.y;
            randomRotationZ = copyFrom.randomRotation.z;
            offsetX = copyFrom.offset.x;
            offsetY = copyFrom.offset.y;
            offsetZ = copyFrom.offset.z;
            LMUVPadding = copyFrom.LMUVPadding;

            generateBranches = copyFrom.generateBranches;
            generateLeaves = copyFrom.generateLeaves;
            generateLightmapUVs = copyFrom.generateLightmapUVs;

            branchesMaterial = copyFrom.branchesMaterial;

            leavesProb.Clear();
            for (var i = 0; i < copyFrom.leavesProb.Length; i++) 
                leavesProb.Add(copyFrom.leavesProb[i]);

            leavesPrefabs.Clear();
            for (var i = 0; i < copyFrom.leavesPrefabs.Length; i++) 
                leavesPrefabs.Add(copyFrom.leavesPrefabs[i]);
        }
    }
}