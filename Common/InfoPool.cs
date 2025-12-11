using System.Collections.Generic;
using UnityEngine;

namespace Dynamite3D.RealIvy{

	[System.Serializable]
	public class InfoPool : ScriptableObject{
		public IvyContainer ivyContainer;
		public EditorMeshBuilder meshBuilder;
		public IvyParameters ivyParameters;
		public EditorIvyGrowth growth;
	}
	
    [System.Serializable]
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

    [System.Serializable]
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

    [System.Serializable]
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
        public bool buffer32Bits = false;
        public bool halfgeom = false;
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

		public List<GameObject> leavesPrefabs = new List<GameObject>();
        public List<float> leavesProb = new List<float>();

        //public Dictionary<int, Vector2> UV2IslesSizes;

        public bool generateBranches, generateLeaves, generateLightmapUVs;

		public void CopyFrom(IvyPreset ivyPreset)
		{
			CopyFrom(ivyPreset.ivyParameters);
		}

		public void CopyFrom(IvyParameters copyFrom)
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
            this.gravityX = copyFrom.gravity.x;
            this.gravityY = copyFrom.gravity.y;
            this.gravityZ = copyFrom.gravity.z;
            this.grabProvabilityOnFall = copyFrom.grabProvabilityOnFall;
            this.stiffness = copyFrom.stiffness;
            this.optAngleBias = copyFrom.optAngleBias;
            this.leaveEvery = copyFrom.leaveEvery;
            this.randomLeaveEvery = copyFrom.randomLeaveEvery;

            this.buffer32Bits = copyFrom.buffer32Bits;
            this.halfgeom = copyFrom.halfgeom;
            this.sides = copyFrom.sides;
            this.minRadius = copyFrom.minRadius;
            this.maxRadius = copyFrom.maxRadius;
            this.radiusVarFreq = copyFrom.radiusVarFreq;
            this.radiusVarOffset = copyFrom.radiusVarOffset;
            this.tipInfluence = copyFrom.tipInfluence;
            this.uvScaleX = copyFrom.uvScale.x;
            this.uvScaleY = copyFrom.uvScale.y;
            this.uvOffsetX = copyFrom.uvOffset.x;
            this.uvOffsetY = copyFrom.uvOffset.y;
            this.minScale = copyFrom.minScale;
            this.maxScale = copyFrom.maxScale;
            this.globalOrientation = copyFrom.globalOrientation;
            this.globalRotationX = copyFrom.globalRotation.x;
            this.globalRotationY = copyFrom.globalRotation.y;
            this.globalRotationZ = copyFrom.globalRotation.z;
            this.rotationX = copyFrom.rotation.x;
            this.rotationY = copyFrom.rotation.y;
            this.rotationZ = copyFrom.rotation.z;
            this.randomRotationX = copyFrom.randomRotation.x;
            this.randomRotationY = copyFrom.randomRotation.y;
            this.randomRotationZ = copyFrom.randomRotation.z;
            this.randomRotationX = copyFrom.randomRotation.x;
            this.randomRotationY = copyFrom.randomRotation.y;
            this.randomRotationZ = copyFrom.randomRotation.z;
            this.offsetX = copyFrom.offset.x;
            this.offsetY = copyFrom.offset.y;
            this.offsetZ = copyFrom.offset.z;
            this.LMUVPadding = copyFrom.LMUVPadding;

            this.generateBranches = copyFrom.generateBranches;
            this.generateLeaves = copyFrom.generateLeaves;
            this.generateLightmapUVs = copyFrom.generateLightmapUVs;

            this.branchesMaterial = copyFrom.branchesMaterial;

            this.leavesProb.Clear();
            for (int i = 0; i < copyFrom.leavesProb.Length; i++)
            {
                this.leavesProb.Add(copyFrom.leavesProb[i]);
            }

			this.leavesPrefabs.Clear();
			for(int i = 0; i < copyFrom.leavesPrefabs.Length; i++)
			{
				this.leavesPrefabs.Add(copyFrom.leavesPrefabs[i]);
			}
			//this.UV2IslesSizes = copyFrom.UV2IslesSizes;	
		}
    }
}