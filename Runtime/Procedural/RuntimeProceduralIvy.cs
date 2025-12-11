using UnityEngine;

namespace Dynamite3D.RealIvy
{
	public class RuntimeProceduralIvy : RTIvy
	{
		private RuntimeIvyGrowth rtIvyGrowth;

		protected override void Init(IvyContainer ivyContainer, IvyParameters ivyParameters)
		{
			base.Init(ivyContainer, ivyParameters);

			rtIvyGrowth = new RuntimeIvyGrowth();
			rtIvyGrowth.Init(rtIvyContainer, ivyParameters, gameObject, leavesMeshesByChosenLeaf, 
				GetMaxNumPoints(), GetMaxNumLeaves(), GetMaxNumVerticesPerLeaf());

			for (int i = 0; i < 10; i++)
			{
				rtIvyGrowth.Step();
			}

			this.currentLifetime = this.growthParameters.lifetime;
		}

		protected override void NextPoints(int branchIndex)
		{
			base.NextPoints(branchIndex);
			rtIvyGrowth.Step();
		}

		public override bool IsGrowingFinished()
		{
			bool res = currentTimer > currentLifetime;
			return res;
		}

		protected override float GetNormalizedLifeTime()
		{
			float res = currentTimer / this.growthParameters.lifetime;
			res = Mathf.Clamp(res, 0.1f, 1f);
			return res;
		}

		public void SetIvyParameters(IvyPreset ivyPreset)
		{
			this.ivyParameters.CopyFrom(ivyPreset);
		}

		protected override void InitializeMeshesData(Mesh bakedMesh, int numBranches)
		{
			meshBuilder.InitializeMeshesDataProcedural(bakedMesh, numBranches, this.growthParameters.lifetime, growthParameters.growthSpeed);
		}

		protected override int GetMaxNumPoints()
		{
			float timePerPoint = ivyParameters.stepSize / growthParameters.growthSpeed;
			int res = Mathf.CeilToInt(growthParameters.lifetime / timePerPoint) * ivyParameters.maxBranchs * 2;

			res = 20;

			return res;
		}

		protected override int GetMaxNumLeaves()
		{
			int res = GetMaxNumPoints();

			return res;
		}

		public override void InitIvy(RuntimeGrowthParameters growthParameters, IvyContainer ivyContainer, IvyParameters ivyParameters)
		{
			this.growthParameters = growthParameters;
			Init(null, ivyParameters);
			InitMeshBuilder();
			AddFirstBranch();
		}

		private int GetMaxNumVerticesPerLeaf()
		{
			int res = 0;

			for (int i = 0; i < ivyParameters.leavesPrefabs.Length; i++)
			{
				if(res <= leavesMeshesByChosenLeaf[i].vertices.Length)
				{
					res = leavesMeshesByChosenLeaf[i].vertices.Length;
				}
			}

			return res;
		}
	}
}