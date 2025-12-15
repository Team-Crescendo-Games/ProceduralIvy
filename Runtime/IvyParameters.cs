using System;
using System.Linq;
using UnityEngine;

namespace TeamCrescendo.ProceduralIvy
{
    [Serializable]
    public class IvyParameters
    {
        [Header("Growth Settings")]
        public float stepSize = 0.1f;
        public int randomSeed;
        [Range(0f, 1f)] public float branchProbability = 0.05f;
        [Range(1, 100)] public int maxBranches = 5;
        public LayerMask layerMask = -1;
        public float minDistanceToSurface = 0.01f;
        public float maxDistanceToSurface = 0.03f;
        public float DTSFrequency = 1f;
        public float DTSRandomness = 0.2f;
        public float directionFrequency = 1f;
        public float directionAmplitude = 20f;
        public float directionRandomness = 1f;
        public Vector3 gravity;
        [Range(0,1)] public float grabProbabilityOnFall = 0.1f;
        public float stiffness = 0.03f;
        public float optimizationAngle = 15f;
        
        [Header("Geometry Settings")]
        public bool buffer32Bits;
        public bool halfgeom;
        public int sides = 3;
        public float minBranchRadius = 0.025f;
        public float maxBranchRadius = 0.05f;
        public float radiusVarFreq = 1f;
        public float radiusVarOffset;
        public float tipInfluence = 0.5f;
        
        [Header("UV")]
        public Vector2 uvScale = new(1f, 1f);
        public Vector2 uvOffset = new(0f, 0f);
        
        [Header("Leaf")]
        public int leaveEvery = 1;
        public int randomLeaveEvery = 1;
        public float minLeafScale = 0.7f;
        public float maxLeafScale = 1.2f;
        public bool globalLeafOrientation;
        public Vector3 globalLeafRotation = -Vector3.up;
        public Vector3 leafLocalRotation = Vector3.zero;
        public Vector3 leafRandomRotation = Vector3.zero;
        public Vector3 leafOffset = Vector3.zero;

        [Header("Prefab Settings")]
        public Material branchesMaterial;
        public GameObject[] leavesPrefabs = Array.Empty<GameObject>();
        public float[] leavesProb = Array.Empty<float>();
        
        [Header("Generation")]
        public bool generateBranches = true;
        public bool generateLeaves = true;
        public bool generateLightmapUVs;

        public IvyParameters(IvyParameters paramsCopy)
        {
            DeepCopy(paramsCopy);
        }
        
        public IvyParameters(IvyPreset preset)
        {
            DeepCopy(preset);
        }

        public void DeepCopy(IvyPreset ivyPreset)
        {
            if (ivyPreset == null) return;
            DeepCopy(ivyPreset.ivyParameters);
        }

        public void DeepCopy(IvyParameters copyFrom)
        {
            if (copyFrom == null) return;
             
            stepSize = copyFrom.stepSize;
            branchProbability = copyFrom.branchProbability;
            maxBranches = copyFrom.maxBranches;
            layerMask = copyFrom.layerMask;
            minDistanceToSurface = copyFrom.minDistanceToSurface;
            maxDistanceToSurface = copyFrom.maxDistanceToSurface;
            DTSFrequency = copyFrom.DTSFrequency;
            DTSRandomness = copyFrom.DTSRandomness;
            directionFrequency = copyFrom.directionFrequency;
            directionAmplitude = copyFrom.directionAmplitude;
            directionRandomness = copyFrom.directionRandomness;
            gravity = copyFrom.gravity;
            grabProbabilityOnFall = copyFrom.grabProbabilityOnFall;
            stiffness = copyFrom.stiffness;
            optimizationAngle = copyFrom.optimizationAngle;
            leaveEvery = copyFrom.leaveEvery;
            randomLeaveEvery = copyFrom.randomLeaveEvery;

            halfgeom = copyFrom.halfgeom;
            sides = copyFrom.sides;
            minBranchRadius = copyFrom.minBranchRadius;
            maxBranchRadius = copyFrom.maxBranchRadius;
            radiusVarFreq = copyFrom.radiusVarFreq;
            radiusVarOffset = copyFrom.radiusVarOffset;
            tipInfluence = copyFrom.tipInfluence;
            uvScale = copyFrom.uvScale;
            uvOffset = copyFrom.uvOffset;
            minLeafScale = copyFrom.minLeafScale;
            maxLeafScale = copyFrom.maxLeafScale;
            globalLeafOrientation = copyFrom.globalLeafOrientation;
            globalLeafRotation = copyFrom.globalLeafRotation;
            leafLocalRotation = copyFrom.leafLocalRotation;
            leafRandomRotation = copyFrom.leafRandomRotation;
            leafOffset = copyFrom.leafOffset;

            generateBranches = copyFrom.generateBranches;
            generateLeaves = copyFrom.generateLeaves;
            generateLightmapUVs = copyFrom.generateLightmapUVs;

            branchesMaterial = copyFrom.branchesMaterial;

            leavesPrefabs = new GameObject[copyFrom.leavesPrefabs.Length];
            for (var i = 0; i < copyFrom.leavesPrefabs.Length; i++) leavesPrefabs[i] = copyFrom.leavesPrefabs[i];

            leavesProb = new float[copyFrom.leavesProb.Length];
            for (var i = 0; i < copyFrom.leavesProb.Length; i++) leavesProb[i] = copyFrom.leavesProb[i];
        }

        public bool IsEqualTo(IvyParameters compareTo)
        {
            bool floatsEqual =
                Mathf.Approximately(stepSize, compareTo.stepSize) &&
                Mathf.Approximately(branchProbability, compareTo.branchProbability) &&
                Mathf.Approximately(minDistanceToSurface, compareTo.minDistanceToSurface) &&
                Mathf.Approximately(maxDistanceToSurface, compareTo.maxDistanceToSurface) &&
                Mathf.Approximately(DTSFrequency, compareTo.DTSFrequency) &&
                Mathf.Approximately(DTSRandomness, compareTo.DTSRandomness) &&
                Mathf.Approximately(directionFrequency, compareTo.directionFrequency) &&
                Mathf.Approximately(directionAmplitude, compareTo.directionAmplitude) &&
                Mathf.Approximately(directionRandomness, compareTo.directionRandomness) &&
                Mathf.Approximately(grabProbabilityOnFall, compareTo.grabProbabilityOnFall) &&
                Mathf.Approximately(stiffness, compareTo.stiffness) &&
                Mathf.Approximately(optimizationAngle, compareTo.optimizationAngle) &&
                Mathf.Approximately(minBranchRadius, compareTo.minBranchRadius) &&
                Mathf.Approximately(maxBranchRadius, compareTo.maxBranchRadius) &&
                Mathf.Approximately(radiusVarFreq, compareTo.radiusVarFreq) &&
                Mathf.Approximately(radiusVarOffset, compareTo.radiusVarOffset) &&
                Mathf.Approximately(tipInfluence, compareTo.tipInfluence) &&
                Mathf.Approximately(minLeafScale, compareTo.minLeafScale) &&
                Mathf.Approximately(maxLeafScale, compareTo.maxLeafScale);

            if (!floatsEqual) return false;

            bool vectorsEqual =
                gravity == compareTo.gravity &&
                uvScale == compareTo.uvScale &&
                uvOffset == compareTo.uvOffset &&
                globalLeafRotation == compareTo.globalLeafRotation &&
                leafLocalRotation == compareTo.leafLocalRotation &&
                leafRandomRotation == compareTo.leafRandomRotation &&
                leafOffset == compareTo.leafOffset;

            if (!vectorsEqual) return false;

            bool othersEqual =
                randomSeed == compareTo.randomSeed &&
                maxBranches == compareTo.maxBranches &&
                layerMask == compareTo.layerMask &&
                leaveEvery == compareTo.leaveEvery &&
                randomLeaveEvery == compareTo.randomLeaveEvery &&
                buffer32Bits == compareTo.buffer32Bits &&
                halfgeom == compareTo.halfgeom &&
                sides == compareTo.sides &&
                globalLeafOrientation == compareTo.globalLeafOrientation &&
                generateBranches == compareTo.generateBranches &&
                generateLeaves == compareTo.generateLeaves &&
                generateLightmapUVs == compareTo.generateLightmapUVs &&
                branchesMaterial == compareTo.branchesMaterial; // Checks Object Reference

            if (!othersEqual) return false;

            bool arraysEqual =
                leavesPrefabs.SequenceEqual(compareTo.leavesPrefabs) &&
                leavesProb.SequenceEqual(compareTo.leavesProb);

            return arraysEqual;
        }
    }
}