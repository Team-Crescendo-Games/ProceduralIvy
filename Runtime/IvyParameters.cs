using System;
using System.Collections.Generic;
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
        public float branchProvability = 0.05f;
        public int maxBranchs = 5;
        public LayerMask layerMask = -1;
        public float minDistanceToSurface = 0.01f;
        public float maxDistanceToSurface = 0.03f;
        public float DTSFrequency = 1f;
        public float DTSRandomness = 0.2f;
        public float directionFrequency = 1f;
        public float directionAmplitude = 20f;
        public float directionRandomness = 1f;
        public Vector3 gravity;
        public float grabProvabilityOnFall = 0.1f;
        public float stiffness = 0.03f;
        public float optAngleBias = 15f;
        public int leaveEvery = 1;
        public int randomLeaveEvery = 1;

        [Header("Geometry Settings")]
        public bool buffer32Bits;
        public bool halfgeom;
        public int sides = 3;
        public float minRadius = 0.025f;
        public float maxRadius = 0.05f;
        public float radiusVarFreq = 1f;
        public float radiusVarOffset;
        public float tipInfluence = 0.5f;
        public Vector2 uvScale = new(1f, 1f);
        public Vector2 uvOffset = new(0f, 0f);

        public float minScale = 0.7f;
        public float maxScale = 1.2f;
        public bool globalOrientation;
        public Vector3 globalRotation = -Vector3.up;
        public Vector3 rotation = Vector3.zero;
        public Vector3 randomRotation = Vector3.zero;
        public Vector3 offset = Vector3.zero;
        public float LMUVPadding = 0.002f;
        public Material branchesMaterial;

        [Header("Prefab Settings")]
        public GameObject[] leavesPrefabs = Array.Empty<GameObject>();
        public float[] leavesProb = Array.Empty<float>();
        
        [Header("Generation")]
        public bool generateBranches;
        public bool generateLeaves;
        public bool generateLightmapUVs;

        public IvyParameters(IvyParametersGUI paramsGuiCopy)
        {
            DeepCopy(paramsGuiCopy);
        }

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
            DeepCopy(ivyPreset.ivyParameters);
        }

        public void DeepCopy(IvyParametersGUI copyFrom)
        {
            stepSize = copyFrom.stepSize;
            branchProvability = copyFrom.branchProvability;
            maxBranchs = copyFrom.maxBranchs;
            layerMask = copyFrom.layerMask;
            minDistanceToSurface = copyFrom.minDistanceToSurface;
            maxDistanceToSurface = copyFrom.maxDistanceToSurface;
            DTSFrequency = copyFrom.DTSFrequency;
            DTSRandomness = copyFrom.DTSRandomness;
            directionFrequency = copyFrom.directionFrequency;
            directionAmplitude = copyFrom.directionAmplitude;
            directionRandomness = copyFrom.directionRandomness;
            gravity = new Vector3(copyFrom.gravityX, copyFrom.gravityY, copyFrom.gravityZ);
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
            uvScale = new Vector2(copyFrom.uvScaleX, copyFrom.uvScaleY);
            uvOffset = new Vector2(copyFrom.uvOffsetX, copyFrom.uvOffsetY);
            minScale = copyFrom.minScale;
            maxScale = copyFrom.maxScale;
            globalOrientation = copyFrom.globalOrientation;
            globalRotation = new Vector3(copyFrom.globalRotationX, copyFrom.globalRotationY, copyFrom.globalRotationZ);
            rotation = new Vector3(copyFrom.rotationX, copyFrom.rotationY, copyFrom.rotationZ);
            randomRotation = new Vector3(copyFrom.randomRotationX, copyFrom.randomRotationY, copyFrom.randomRotationZ);
            offset = new Vector3(copyFrom.offsetX, copyFrom.offsetY, copyFrom.offsetZ);
            LMUVPadding = copyFrom.LMUVPadding;

            generateBranches = copyFrom.generateBranches;
            generateLeaves = copyFrom.generateLeaves;
            generateLightmapUVs = copyFrom.generateLightmapUVs;

            branchesMaterial = copyFrom.branchesMaterial;


            leavesPrefabs = new GameObject[copyFrom.leavesPrefabs.Count];
            for (var i = 0; i < copyFrom.leavesPrefabs.Count; i++) leavesPrefabs[i] = copyFrom.leavesPrefabs[i];

            leavesProb = new float[copyFrom.leavesProb.Count];
            for (var i = 0; i < copyFrom.leavesProb.Count; i++) leavesProb[i] = copyFrom.leavesProb[i];
        }

        public void DeepCopy(IvyParameters copyFrom)
        {
            stepSize = copyFrom.stepSize;
            branchProvability = copyFrom.branchProvability;
            maxBranchs = copyFrom.maxBranchs;
            layerMask = copyFrom.layerMask;
            minDistanceToSurface = copyFrom.minDistanceToSurface;
            maxDistanceToSurface = copyFrom.maxDistanceToSurface;
            DTSFrequency = copyFrom.DTSFrequency;
            DTSRandomness = copyFrom.DTSRandomness;
            directionFrequency = copyFrom.directionFrequency;
            directionAmplitude = copyFrom.directionAmplitude;
            directionRandomness = copyFrom.directionRandomness;
            gravity = copyFrom.gravity;
            grabProvabilityOnFall = copyFrom.grabProvabilityOnFall;
            stiffness = copyFrom.stiffness;
            optAngleBias = copyFrom.optAngleBias;
            leaveEvery = copyFrom.leaveEvery;
            randomLeaveEvery = copyFrom.randomLeaveEvery;

            halfgeom = copyFrom.halfgeom;
            sides = copyFrom.sides;
            minRadius = copyFrom.minRadius;
            maxRadius = copyFrom.maxRadius;
            radiusVarFreq = copyFrom.radiusVarFreq;
            radiusVarOffset = copyFrom.radiusVarOffset;
            tipInfluence = copyFrom.tipInfluence;
            uvScale = copyFrom.uvScale;
            uvOffset = copyFrom.uvOffset;
            minScale = copyFrom.minScale;
            maxScale = copyFrom.maxScale;
            globalOrientation = copyFrom.globalOrientation;
            globalRotation = copyFrom.globalRotation;
            rotation = copyFrom.rotation;
            randomRotation = copyFrom.randomRotation;
            offset = copyFrom.offset;
            LMUVPadding = copyFrom.LMUVPadding;

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
                Mathf.Approximately(branchProvability, compareTo.branchProvability) &&
                Mathf.Approximately(minDistanceToSurface, compareTo.minDistanceToSurface) &&
                Mathf.Approximately(maxDistanceToSurface, compareTo.maxDistanceToSurface) &&
                Mathf.Approximately(DTSFrequency, compareTo.DTSFrequency) &&
                Mathf.Approximately(DTSRandomness, compareTo.DTSRandomness) &&
                Mathf.Approximately(directionFrequency, compareTo.directionFrequency) &&
                Mathf.Approximately(directionAmplitude, compareTo.directionAmplitude) &&
                Mathf.Approximately(directionRandomness, compareTo.directionRandomness) &&
                Mathf.Approximately(grabProvabilityOnFall, compareTo.grabProvabilityOnFall) &&
                Mathf.Approximately(stiffness, compareTo.stiffness) &&
                Mathf.Approximately(optAngleBias, compareTo.optAngleBias) &&
                Mathf.Approximately(minRadius, compareTo.minRadius) &&
                Mathf.Approximately(maxRadius, compareTo.maxRadius) &&
                Mathf.Approximately(radiusVarFreq, compareTo.radiusVarFreq) &&
                Mathf.Approximately(radiusVarOffset, compareTo.radiusVarOffset) &&
                Mathf.Approximately(tipInfluence, compareTo.tipInfluence) &&
                Mathf.Approximately(minScale, compareTo.minScale) &&
                Mathf.Approximately(maxScale, compareTo.maxScale) &&
                Mathf.Approximately(LMUVPadding, compareTo.LMUVPadding);

            if (!floatsEqual) return false;

            bool vectorsEqual =
                gravity == compareTo.gravity &&
                uvScale == compareTo.uvScale &&
                uvOffset == compareTo.uvOffset &&
                globalRotation == compareTo.globalRotation &&
                rotation == compareTo.rotation &&
                randomRotation == compareTo.randomRotation &&
                offset == compareTo.offset;

            if (!vectorsEqual) return false;

            bool othersEqual =
                randomSeed == compareTo.randomSeed &&
                maxBranchs == compareTo.maxBranchs &&
                layerMask == compareTo.layerMask &&
                leaveEvery == compareTo.leaveEvery &&
                randomLeaveEvery == compareTo.randomLeaveEvery &&
                buffer32Bits == compareTo.buffer32Bits &&
                halfgeom == compareTo.halfgeom &&
                sides == compareTo.sides &&
                globalOrientation == compareTo.globalOrientation &&
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