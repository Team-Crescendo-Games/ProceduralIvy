using System.Collections.Generic;
using UnityEngine;

namespace Dynamite3D.RealIvy
{
	public abstract class RTIvy : MonoBehaviour
	{

		protected IvyParameters ivyParameters;
		protected RTIvyContainer rtIvyContainer;


		protected RTIvyContainer rtBuildingIvyContainer;


		public MeshFilter meshFilter;
		public MeshRenderer meshRenderer;
		public MeshRenderer mrProcessedMesh;
		public MeshFilter mfProcessedMesh;


		protected List<RTBranchContainer> activeBakedBranches;
		protected List<RTBranchContainer> activeBuildingBranches;

		protected int lastIdxActiveBranch;
		public List<float> srcTotalLengthPerBranch;
		public List<float> dstTotalLengthPerBranch;
		public List<float> growingFactorPerBranch;
		public List<float> lengthPerBranch;
		protected List<int> lastCopiedIndexPerBranch;

		protected List<Vector3> srcPoints;
		protected List<Vector3> dstPoints;
		protected List<LeafPoint> leavesToCopyMesh;

		public RTBakedMeshBuilder meshBuilder;
		private Mesh bakedMesh;
		private Mesh processedMesh;

		private bool refreshProcessedMesh;
		private int backtrackingPoints;
		protected float currentLifetime;
		protected float currentSpeed;
		protected float currentGrowthSpeed;
		protected float leafLengthCorrrectionFactor;
		protected float currentTimer;

		protected RuntimeGrowthParameters growthParameters;
		protected List<Material> leavesMaterials;

		protected RTMeshData[] leavesMeshesByChosenLeaf;
		protected int[] submeshByChoseLeaf;
		protected int maxBranches;



		public void AwakeInit()
		{
			this.bakedMesh = meshFilter.sharedMesh;
			meshFilter.sharedMesh = null;
		}



		/* public void InitIvy(RuntimeGrowthParameters growthParameters, IvyContainer ivyContainer, 
			IvyParameters ivyParameters)
		{
			this.growthParameters = growthParameters;
			Init(ivyContainer, ivyParameters);
			AddFirstBranch();
		} */



		protected virtual void Init(IvyContainer ivyContainer, IvyParameters ivyParameters)
		{
			this.rtIvyContainer = new RTIvyContainer();

			this.ivyParameters = new IvyParameters();
			this.ivyParameters.CopyFrom(ivyParameters);

			CreateLeavesDict();

			if (ivyContainer != null)
			{
				this.rtIvyContainer.Initialize(ivyContainer, ivyParameters, gameObject, 
					leavesMeshesByChosenLeaf, ivyContainer.firstVertexVector);
			}
			else
			{
				this.rtIvyContainer.Initialize();
			}



			SetUpMaxBranches(ivyContainer);

			
			activeBakedBranches = new List<RTBranchContainer>(maxBranches);
			activeBuildingBranches = new List<RTBranchContainer>(maxBranches);

			rtBuildingIvyContainer = new RTIvyContainer();

			Vector3 firstVertexVector = ivyContainer == null ? CalculateFirstVertexVector() : ivyContainer.firstVertexVector;
			rtBuildingIvyContainer.Initialize(firstVertexVector);
			lastIdxActiveBranch = -1;

			leafLengthCorrrectionFactor = 1;

			int submeshCount = ivyParameters.leavesPrefabs.Length + 1;
			this.processedMesh = new Mesh();
			processedMesh.subMeshCount = submeshCount;
			mfProcessedMesh.sharedMesh = processedMesh;

			this.refreshProcessedMesh = false;
			this.backtrackingPoints = GetBacktrackingPoints();

			if(this.bakedMesh == null)
			{
				this.bakedMesh = new Mesh();
				this.bakedMesh.subMeshCount = submeshCount;
			}

			/* meshBuilder = new RTBakedMeshBuilder(rtIvyContainer, gameObject);
			meshBuilder.InitializeMeshBuilder(ivyParameters, rtBuildingIvyContainer, rtIvyContainer,
				gameObject, bakedMesh, meshRenderer, meshFilter, maxBranches,
				processedMesh, growthParameters.growthSpeed, mrProcessedMesh, 
				backtrackingPoints, submeshByChoseLeaf, leavesMeshesDict, leavesMaterials.ToArray()); */



			lastCopiedIndexPerBranch = new List<int>(maxBranches);
			leavesToCopyMesh = new List<LeafPoint>(50);
			srcPoints = new List<Vector3>(maxBranches);
			dstPoints = new List<Vector3>(maxBranches);
			growingFactorPerBranch = new List<float>(maxBranches);
			srcTotalLengthPerBranch = new List<float>(maxBranches);
			dstTotalLengthPerBranch = new List<float>(maxBranches);
			lengthPerBranch = new List<float>(maxBranches);


			for (int i = 0; i < maxBranches; i++)
			{
				srcPoints.Add(Vector3.zero);
				dstPoints.Add(Vector3.zero);
				growingFactorPerBranch.Add(0f);
				srcTotalLengthPerBranch.Add(0f);
				dstTotalLengthPerBranch.Add(0f);
				lastCopiedIndexPerBranch.Add(-1);
				lengthPerBranch.Add(0f);

				int branchPointsSize = GetMaxNumPoints();
				int numLeaves = GetMaxNumLeaves();

				RTBranchContainer branchContainer = new RTBranchContainer(branchPointsSize, numLeaves);
				activeBuildingBranches.Add(branchContainer);
			}
		}

		private void SetUpMaxBranches(IvyContainer ivyContainer)
		{
			maxBranches = ivyParameters.maxBranchs;
			if (ivyContainer != null)
			{
				maxBranches = Mathf.Max(ivyParameters.maxBranchs, ivyContainer.branches.Count);
			}
		}

		protected void InitMeshBuilder()
		{
			//CreateLeavesDict();
			meshBuilder = new RTBakedMeshBuilder(rtIvyContainer, gameObject);

			meshBuilder.InitializeMeshBuilder(ivyParameters, rtBuildingIvyContainer, rtIvyContainer,
				gameObject, bakedMesh, meshRenderer, meshFilter, maxBranches,
				processedMesh, growthParameters.growthSpeed, mrProcessedMesh,
				backtrackingPoints, submeshByChoseLeaf, leavesMeshesByChosenLeaf, leavesMaterials.ToArray());


			InitializeMeshesData(bakedMesh, maxBranches);
		}

		protected virtual void AddFirstBranch()
		{
			AddNextBranch(0);
		}

		private int GetBacktrackingPoints()
		{
			int res = Mathf.CeilToInt(ivyParameters.tipInfluence / ivyParameters.stepSize);
			return res;
		}

		public virtual void UpdateIvy(float deltaTime)
		{
			UpdateGrowthSpeed();


			for (int i = 0; i < activeBakedBranches.Count; i++)
			{
				Growing(i, deltaTime);
			}

			currentTimer += deltaTime;

			RefreshGeometry();

			if (refreshProcessedMesh)
			{
				meshBuilder.RefreshProcessedMesh();
				refreshProcessedMesh = false;
			}
		}

		protected virtual void Growing(int branchIndex, float deltaTime)
		{
			RTBranchContainer currentBranch = activeBuildingBranches[branchIndex];

			CalculateFactors(srcPoints[branchIndex], dstPoints[branchIndex]);
			meshBuilder.SetLeafLengthCorrectionFactor(leafLengthCorrrectionFactor);
			growingFactorPerBranch[branchIndex] += currentSpeed * deltaTime;
			growingFactorPerBranch[branchIndex] = Mathf.Clamp(growingFactorPerBranch[branchIndex], 0f, 1f);

			currentBranch.totalLength = Mathf.Lerp(srcTotalLengthPerBranch[branchIndex], dstTotalLengthPerBranch[branchIndex], growingFactorPerBranch[branchIndex]);


			RTBranchPoint lastPoint = currentBranch.GetLastBranchPoint();
			lastPoint.length = currentBranch.totalLength;

			lastPoint.point = Vector3.Lerp(srcPoints[branchIndex], dstPoints[branchIndex], growingFactorPerBranch[branchIndex]);


			if (growingFactorPerBranch[branchIndex] >= 1)
			{
				RefreshGeometry();
				NextPoints(branchIndex);
			}
		}

		protected virtual void NextPoints(int branchIndex)
		{
			if (rtBuildingIvyContainer.branches[branchIndex].branchPoints.Count > 0)
			{
				RTBranchPoint lastBuildingBranchPoint = rtBuildingIvyContainer.branches[branchIndex].GetLastBranchPoint();
				if (lastBuildingBranchPoint.index < activeBakedBranches[branchIndex].branchPoints.Count - 1)
				{
					int indexBranchPoint = lastBuildingBranchPoint.index;
					indexBranchPoint++;

					RTBranchPoint branchPoint = activeBakedBranches[branchIndex].branchPoints[indexBranchPoint];
					RTBranchContainer branch = rtBuildingIvyContainer.branches[branchIndex];

					branch.AddBranchPoint(branchPoint, ivyParameters.stepSize);
					
					if (branchPoint.newBranch)
					{
						RTBranchContainer candidateBranch = rtIvyContainer.GetBranchContainerByBranchNumber(branchPoint.newBranchNumber);
						if (candidateBranch.branchPoints.Count >= 2)
						{
							AddNextBranch(branchPoint.newBranchNumber);
						}
					}

					UpdateGrowingPoints(branchIndex);


					if (rtBuildingIvyContainer.branches[branchIndex].branchPoints.Count > backtrackingPoints)
					{
						if (!IsVertexLimitReached())
						{
							meshBuilder.CheckCopyMesh(branchIndex, activeBakedBranches);
							refreshProcessedMesh = true;
						}
						else
						{
							Debug.LogWarning("Limit vertices reached! --> " + Constants.VERTEX_LIMIT_16 + " vertices", meshBuilder.ivyGO);
						}
					}
				}
			}
		}

		private void CalculateFactors(Vector3 srcPoint, Vector3 dstPoint)
		{
			float factor = (Vector3.Distance(srcPoint, dstPoint) / ivyParameters.stepSize);
			factor = (1.0f / factor);
			currentSpeed = factor * currentGrowthSpeed;

			leafLengthCorrrectionFactor = Mathf.Lerp(0.92f, 1f, factor);
		}

		protected virtual void AddNextBranch(int branchNumber)
		{
			lastIdxActiveBranch++;

			RTBranchContainer newBuildingBranch = activeBuildingBranches[lastIdxActiveBranch];
			RTBranchContainer bakedBranch = rtIvyContainer.GetBranchContainerByBranchNumber(branchNumber);

			newBuildingBranch.AddBranchPoint(bakedBranch.branchPoints[0], ivyParameters.stepSize);
			newBuildingBranch.AddBranchPoint(bakedBranch.branchPoints[1], ivyParameters.stepSize);


			newBuildingBranch.leavesOrderedByInitSegment = bakedBranch.leavesOrderedByInitSegment;

			rtBuildingIvyContainer.AddBranch(newBuildingBranch);
			activeBakedBranches.Add(bakedBranch);
			activeBuildingBranches.Add(newBuildingBranch);
			meshBuilder.activeBranches.Add(newBuildingBranch);

			UpdateGrowingPoints(rtBuildingIvyContainer.branches.Count - 1);

			RTBranchPoint lastBranchPoint = newBuildingBranch.GetLastBranchPoint();
			if (lastBranchPoint.newBranch)
			{
				AddNextBranch(lastBranchPoint.newBranchNumber);
			}
		}

		private void UpdateGrowingPoints(int branchIndex)
		{
			if (rtBuildingIvyContainer.branches[branchIndex].branchPoints.Count > 0)
			{
				RTBranchPoint fromPoint = rtBuildingIvyContainer.branches[branchIndex].GetLastBranchPoint();
				if (fromPoint.index < activeBakedBranches[branchIndex].branchPoints.Count - 1)
				{
					RTBranchPoint nextPoint = activeBakedBranches[branchIndex].branchPoints[fromPoint.index + 1];
					growingFactorPerBranch[branchIndex] = 0f;

					srcPoints[branchIndex] = fromPoint.point;
					dstPoints[branchIndex] = nextPoint.point;


					srcTotalLengthPerBranch[branchIndex] = fromPoint.length;
					dstTotalLengthPerBranch[branchIndex] = fromPoint.length + ivyParameters.stepSize;
				}
			}
		}

		private void RefreshGeometry()
		{
			meshBuilder.BuildGeometry02(activeBakedBranches, activeBuildingBranches);
		}

		private void UpdateGrowthSpeed()
		{
			currentGrowthSpeed = growthParameters.growthSpeed;

			if (growthParameters.speedOverLifetimeEnabled)
			{
				float t = GetNormalizedLifeTime();
				currentGrowthSpeed = growthParameters.growthSpeed * growthParameters.speedOverLifetimeCurve.Evaluate(t);
			}
		}

		public bool IsVertexLimitReached()
		{
			int numVertices = meshBuilder.processedMeshData.VertexCount() + ivyParameters.sides + 1;
			bool res = numVertices >= Constants.VERTEX_LIMIT_16;
			return res;
		}

		private Vector3 CalculateFirstVertexVector()
		{
			Vector3 res = Quaternion.AngleAxis(Random.value * 360f, transform.up) * transform.forward;
			return res;
	
		}

		private void CreateLeavesDict()
		{
			List<List<int>> typesByMat = new List<List<int>>();
			this.leavesMaterials = new List<Material>();


			this.leavesMeshesByChosenLeaf = new RTMeshData[ivyParameters.leavesPrefabs.Length];

			leavesMaterials.Add(ivyParameters.branchesMaterial);

			this.submeshByChoseLeaf = new int[ivyParameters.leavesPrefabs.Length];
			int submeshCount = 0;
			for (int i = 0; i < ivyParameters.leavesPrefabs.Length; i++)
			{
				MeshRenderer leafMeshRenderer = ivyParameters.leavesPrefabs[i].GetComponent<MeshRenderer>();
				MeshFilter leafMeshFilter = ivyParameters.leavesPrefabs[i].GetComponent<MeshFilter>();

				if (!leavesMaterials.Contains(leafMeshRenderer.sharedMaterial))
				{
					leavesMaterials.Add(leafMeshRenderer.sharedMaterial);

					
					submeshCount++;
				}
				
				this.submeshByChoseLeaf[i] = submeshCount;
				RTMeshData leafMeshData = new RTMeshData(leafMeshFilter.sharedMesh);
				leavesMeshesByChosenLeaf[i] = leafMeshData;
			}

			Material[] materials = leavesMaterials.ToArray();
			mrProcessedMesh.sharedMaterials = materials;
		}


		protected abstract void InitializeMeshesData(Mesh bakedMesh, int numBranches);
		protected abstract float GetNormalizedLifeTime();
		protected abstract int GetMaxNumPoints();
		protected abstract int GetMaxNumLeaves();
		public abstract bool IsGrowingFinished();
		public abstract void InitIvy(RuntimeGrowthParameters growthParameters, IvyContainer ivyContainer, IvyParameters ivyParameters);
	}
}