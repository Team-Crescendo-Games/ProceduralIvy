using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

namespace Dynamite3D.RealIvy
{
    [System.Serializable]
    public class IvyPreset : ScriptableObject
    {
        public string presetName;
        ////Growth parameters
        //public float stepSize = 0.1f;
        //public int randomSeed;
        //public float branchProvability = 0.05f;
        //public int maxBranchs = 5;
        //public LayerMask layerMask = -1;
        //public float minDistanceToSurface = 0.01f;
        //public float maxDistanceToSurface = 0.03f;
        //public float DTSFrequency = 1f;
        //public float DTSRandomness = 0.2f;
        //public float directionFrequency = 1f;
        //public float directionAmplitude = 20f;
        //public float directionRandomness = 1f;
        //public Vector3 gravity;
        //public float grabProvabilityOnFall = 0.1f;
        //public float stiffness = 0.03f;
        //public float optAngleBias = 15f;
        //public int leaveEvery = 1;
        //public int randomLeaveEvery = 1;

        ////Geometry parameters
        //public bool halfgeom = false;
        //public int sides = 3;
        //public float minRadius = 0.025f;
        //public float maxRadius = 0.05f;
        //public float radiusVarFreq = 1f;
        //public float radiusVarOffset = 0f;
        //public float tipInfluence = 0.5f;
        //public Vector2 uvScale = new Vector2(1f, 1f);
        //public Vector2 uvOffset = new Vector2(0f, 0f);

        //public float minScale = 0.7f;
        //public float maxScale = 1.2f;
        //public bool globalOrientation;
        //public Vector3 globalRotation = -Vector3.up;
        //public Vector3 rotation = Vector3.zero;
        //public Vector3 randomRotation = Vector3.zero;
        //public Vector3 offset = Vector3.zero;
        //public float LMUVPadding = 0.002f;
        //public Material branchesMaterial;

        //public GameObject[] leavesPrefabs = new GameObject[0];
        //public float[] leavesProb = new float[0];

        //public Dictionary<int, Vector2> UV2IslesSizes;

        //public bool generateBranches, generateLeaves, generateLightmapUVs;

		public IvyParameters ivyParameters;

		public void CopyFrom(IvyParametersGUI copyFrom)
		{
			ivyParameters.CopyFrom(copyFrom);
		}



        /*public void CopyFrom(IvyParametersGUI copyFrom)
        {
            this.stepSize = copyFrom.stepSize;
            this.branchProvability = copyFrom.branchProvability;
            this.maxBranchs = copyFrom.maxBranchs;
            this.layerMask = copyFrom.layerMask;
            this.minDistanceToSurface = copyFrom.minDistanceToSurface;
            this.maxDistanceToSurface = copyFrom.maxDistanceToSurface;
            this.DTSFrequency = copyFrom.DTSFrequency;
            this.DTSRandomness = copyFrom.DTSRandomness;
            this.directionFrequency = copyFrom.directionFrequency;
            this.directionAmplitude = copyFrom.directionAmplitude;
            this.directionRandomness = copyFrom.directionRandomness;
            this.gravity = new Vector3(copyFrom.gravityX, copyFrom.gravityY, copyFrom.gravityZ);
            this.grabProvabilityOnFall = copyFrom.grabProvabilityOnFall;
            this.stiffness = copyFrom.stiffness;
            this.optAngleBias = copyFrom.optAngleBias;
            this.leaveEvery = copyFrom.leaveEvery;
            this.randomLeaveEvery = copyFrom.randomLeaveEvery;

            this.halfgeom = copyFrom.halfgeom;
            this.sides = copyFrom.sides;
            this.minRadius = copyFrom.minRadius;
            this.maxRadius = copyFrom.maxRadius;
            this.radiusVarFreq = copyFrom.radiusVarFreq;
            this.radiusVarOffset = copyFrom.radiusVarOffset;
            this.tipInfluence = copyFrom.tipInfluence;
            this.uvScale = new Vector2(copyFrom.uvScaleX, copyFrom.uvScaleY);
            this.uvOffset = new Vector2(copyFrom.uvOffsetX, copyFrom.uvOffsetY);
            this.minScale = copyFrom.minScale;
            this.maxScale = copyFrom.maxScale;
            this.globalOrientation = copyFrom.globalOrientation;
            this.globalRotation = new Vector3(copyFrom.globalRotationX, copyFrom.globalRotationY, copyFrom.globalRotationZ);
            this.rotation = new Vector3(copyFrom.rotationX, copyFrom.rotationY, copyFrom.rotationZ);
            this.randomRotation = new Vector3(copyFrom.randomRotationX, copyFrom.randomRotationY, copyFrom.randomRotationZ);
            this.offset = new Vector3(copyFrom.offsetX, copyFrom.offsetY, copyFrom.offsetZ);
            this.LMUVPadding = copyFrom.LMUVPadding;

            this.generateBranches = copyFrom.generateBranches;
            this.generateLeaves = copyFrom.generateLeaves;
            this.generateLightmapUVs = copyFrom.generateLightmapUVs;

            this.branchesMaterial = copyFrom.branchesMaterial;


			this.leavesPrefabs = new GameObject[copyFrom.leavesPrefabs.Count];
			for (int i = 0; i < copyFrom.leavesPrefabs.Count; i++)
			{
				this.leavesPrefabs[i] = copyFrom.leavesPrefabs[i];
			}
			
			this.leavesProb = new float[copyFrom.leavesProb.Count];
			for(int i = 0; i < copyFrom.leavesProb.Count; i++)
			{
				this.leavesProb[i] = copyFrom.leavesProb[i];
			}
        }

		public void CopyFrom(IvyPreset copyFrom)
		{
			this.stepSize = copyFrom.stepSize;
			this.branchProvability = copyFrom.branchProvability;
			this.maxBranchs = copyFrom.maxBranchs;
			this.layerMask = copyFrom.layerMask;
			this.minDistanceToSurface = copyFrom.minDistanceToSurface;
			this.maxDistanceToSurface = copyFrom.maxDistanceToSurface;
			this.DTSFrequency = copyFrom.DTSFrequency;
			this.DTSRandomness = copyFrom.DTSRandomness;
			this.directionFrequency = copyFrom.directionFrequency;
			this.directionAmplitude = copyFrom.directionAmplitude;
			this.directionRandomness = copyFrom.directionRandomness;
			this.gravity = copyFrom.gravity;
			this.grabProvabilityOnFall = copyFrom.grabProvabilityOnFall;
			this.stiffness = copyFrom.stiffness;
			this.optAngleBias = copyFrom.optAngleBias;
			this.leaveEvery = copyFrom.leaveEvery;
			this.randomLeaveEvery = copyFrom.randomLeaveEvery;

			this.halfgeom = copyFrom.halfgeom;
			this.sides = copyFrom.sides;
			this.minRadius = copyFrom.minRadius;
			this.maxRadius = copyFrom.maxRadius;
			this.radiusVarFreq = copyFrom.radiusVarFreq;
			this.radiusVarOffset = copyFrom.radiusVarOffset;
			this.tipInfluence = copyFrom.tipInfluence;
			this.uvScale = copyFrom.uvScale;
			this.uvOffset = copyFrom.uvOffset;
			this.minScale = copyFrom.minScale;
			this.maxScale = copyFrom.maxScale;
			this.globalOrientation = copyFrom.globalOrientation;
			this.globalRotation = copyFrom.globalRotation;
			this.rotation = copyFrom.rotation;
			this.randomRotation = copyFrom.randomRotation;
			this.offset = copyFrom.offset;
			this.LMUVPadding = copyFrom.LMUVPadding;

			this.generateBranches = copyFrom.generateBranches;
			this.generateLeaves = copyFrom.generateLeaves;
			this.generateLightmapUVs = copyFrom.generateLightmapUVs;

			this.branchesMaterial = copyFrom.branchesMaterial;

			this.leavesPrefabs = new GameObject[copyFrom.leavesPrefabs.Length];
			for (int i = 0; i < copyFrom.leavesPrefabs.Length; i++)
			{
				this.leavesPrefabs[i] = copyFrom.leavesPrefabs[i];
			}

			this.leavesProb = new float[copyFrom.leavesProb.Length];
			for (int i = 0; i < copyFrom.leavesProb.Length; i++)
			{
				this.leavesProb[i] = copyFrom.leavesProb[i];
			}
		}

        public bool IsEqualTo(IvyPreset compareTo)
        {
            bool isEqual;

            isEqual = 
                stepSize == compareTo.stepSize &&
                branchProvability == compareTo.branchProvability &&
                maxBranchs == compareTo.maxBranchs &&
                layerMask == compareTo.layerMask &&
                minDistanceToSurface == compareTo.minDistanceToSurface &&
                maxDistanceToSurface == compareTo.maxDistanceToSurface &&
                DTSFrequency == compareTo.DTSFrequency &&
                DTSRandomness == compareTo.DTSRandomness &&
                directionFrequency == compareTo.directionFrequency &&
                directionAmplitude == compareTo.directionAmplitude &&
                directionRandomness == compareTo.directionRandomness &&
                gravity == compareTo.gravity &&
                grabProvabilityOnFall == compareTo.grabProvabilityOnFall &&
                stiffness == compareTo.stiffness &&
                optAngleBias == compareTo.optAngleBias &&
                leaveEvery == compareTo.leaveEvery &&
                randomLeaveEvery == compareTo.randomLeaveEvery &&
                halfgeom == compareTo.halfgeom &&
                sides == compareTo.sides &&
                minRadius == compareTo.minRadius &&
                maxRadius == compareTo.maxRadius &&
                radiusVarFreq == compareTo.radiusVarFreq &&
                radiusVarOffset == compareTo.radiusVarOffset &&
                tipInfluence == compareTo.tipInfluence &&
                uvScale == compareTo.uvScale &&
                uvOffset == compareTo.uvOffset &&
                minScale == compareTo.minScale &&
                maxScale == compareTo.maxScale &&
                globalOrientation == compareTo.globalOrientation &&
                globalRotation == compareTo.globalRotation &&
                rotation == compareTo.rotation &&
                randomRotation == compareTo.randomRotation &&
                offset == compareTo.offset &&
                LMUVPadding == compareTo.LMUVPadding &&
                branchesMaterial == compareTo.branchesMaterial &&
                leavesPrefabs.SequenceEqual(compareTo.leavesPrefabs) &&
                leavesProb.SequenceEqual(compareTo.leavesProb) &&
                generateBranches == compareTo.generateBranches &&
                generateLeaves == compareTo.generateLeaves &&
                generateLightmapUVs == compareTo.generateLightmapUVs;
            return isEqual;
        }*/

        /*public IvyPreset()
        {
            gravity = new Vector3(0f, -1f, 0f);
            //tempGravity = new Vector3 (0f, -1f, 0f);
        }*/
#if UNITY_EDITOR
        [ContextMenu("Show GUID")]
		public void ShowGUID()
		{
			string res = UnityEditor.AssetDatabase.AssetPathToGUID(UnityEditor.AssetDatabase.GetAssetPath(this));
			Debug.Log("GUID: " + res);
		}
#endif

        /*[ContextMenu("Copy To Inner Ivy Parameters")]
		public void CopyToInnerIvyParameters()
		{
			ivyParameters.CopyFrom(this);
			UnityEditor.EditorUtility.SetDirty(this);
		}*/
    }
}