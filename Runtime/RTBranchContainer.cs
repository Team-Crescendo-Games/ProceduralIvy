using System.Collections.Generic;
using UnityEngine;

namespace Dynamite3D.RealIvy
{
	public class RTBranchContainer
	{
		public List<RTBranchPoint> branchPoints;
		//public List<RTLeafPoint> leaves;
		//public List<List<RTLeafPoint>> leavesOrderedByInitSegment;

		public RTLeafPoint[][] leavesOrderedByInitSegment;

		public float totalLength;
		public Vector3 growDirection;
		public float randomizeHeight;

		public float heightVar;
		public float newHeight;
		public float heightParameter;
		public float deltaHeight;
		public float currentHeight;

		public int branchSense;

		public bool falling;
		public Quaternion rotationOnFallIteration;
		public float fallIteration;

		public int branchNumber;


		public RTBranchContainer(BranchContainer branchContainer, IvyParameters ivyParameters, RTIvyContainer rtIvyContainer,
			GameObject ivyGO, RTMeshData[] leavesMeshesByChosenLeaf)
		{
			this.totalLength = branchContainer.totalLenght;
			this.growDirection = branchContainer.growDirection;
			this.randomizeHeight = branchContainer.randomizeHeight;
			this.heightVar = branchContainer.heightVar;
			this.newHeight = branchContainer.newHeight;
			this.heightParameter = branchContainer.heightParameter;
			this.deltaHeight = branchContainer.deltaHeight;
			this.currentHeight = branchContainer.currentHeight;
			this.branchSense = branchContainer.branchSense;
			this.falling = branchContainer.falling;
			this.rotationOnFallIteration = branchContainer.rotationOnFallIteration;
			this.branchNumber = branchContainer.branchNumber;



			this.branchPoints = new List<RTBranchPoint>(branchContainer.branchPoints.Count);
			for (int i = 0; i < branchContainer.branchPoints.Count; i++)
			{
				RTBranchPoint rtBranchPoint = new RTBranchPoint(branchContainer.branchPoints[i], this);

				rtBranchPoint.CalculateCenterLoop(ivyGO);
				rtBranchPoint.PreInit(ivyParameters);
				rtBranchPoint.CalculateVerticesLoop(ivyParameters, rtIvyContainer, ivyGO);

				this.branchPoints.Add(rtBranchPoint);
			}


			branchContainer.PrepareRTLeavesDict();


			if (ivyParameters.generateLeaves)
			{
				this.leavesOrderedByInitSegment = new RTLeafPoint[branchPoints.Count][];
				for (int i = 0; i < branchPoints.Count; i++)
				{
					List<LeafPoint> leavesToBake = branchContainer.dictRTLeavesByInitSegment[i];
					int numLeaves = 0;
					if (leavesToBake != null)
					{
						numLeaves = leavesToBake.Count;
					}


					this.leavesOrderedByInitSegment[i] = new RTLeafPoint[numLeaves];


					for (int j = 0; j < numLeaves; j++)
					{
						RTLeafPoint rtLeafPoint = new RTLeafPoint(leavesToBake[j], ivyParameters);
						RTMeshData leafMeshData = leavesMeshesByChosenLeaf[rtLeafPoint.chosenLeave];

						rtLeafPoint.CreateVertices(ivyParameters, leafMeshData, ivyGO);
						this.leavesOrderedByInitSegment[i][j] = rtLeafPoint;
					}
				}
			}
		}

		public Vector2 GetLastUV(IvyParameters ivyParameters)
		{
			Vector2 res = new Vector2(totalLength * ivyParameters.uvScale.y + ivyParameters.uvOffset.y,
				0.5f * ivyParameters.uvScale.x + ivyParameters.uvOffset.x);
			return res;
		}

		public RTBranchContainer(int numPoints, int numLeaves)
		{
			Init(numPoints, numLeaves);
		}

		private void Init(int numPoints, int numLeaves)
		{
			this.branchPoints = new List<RTBranchPoint>(numPoints);

			this.leavesOrderedByInitSegment = new RTLeafPoint[numPoints][];
			for (int i = 0; i < numPoints; i++)
			{
				this.leavesOrderedByInitSegment[i] = new RTLeafPoint[1];
			}
		}

		//public void PrepareRTLeavesDict()
		//{
		//	dictRTLeavesByInitSegment = new Dictionary<int, List<RTLeafPoint>>();

		//	for (int i = 0; i < branchPoints.Count; i++)
		//	{
		//		List<RTLeafPoint> leaves = new List<RTLeafPoint>();
		//		GetLeavesInSegment(branchPoints[i], leaves);
		//		dictRTLeavesByInitSegment[i] = leaves;
		//	}
		//}

		//public void UpdateLeavesDictEntry(int initSegmentIndex, RTLeafPoint leafAdded)
		//{
		//	if(leavesOrderedByInitSegment.Count > initSegmentIndex)
		//	{
		//		leavesOrderedByInitSegment[initSegmentIndex] = leafAdded;
		//	}
		//	else
		//	{
		//		leavesOrderedByInitSegment[initSegmentIndex].Add(leafAdded);
		//	}
		//}

		public void AddBranchPoint(RTBranchPoint rtBranchPoint, float deltaLength)
		{
			this.totalLength += deltaLength;

			rtBranchPoint.length = this.totalLength;
			rtBranchPoint.index = branchPoints.Count;
			rtBranchPoint.branchContainer = this;

			branchPoints.Add(rtBranchPoint);
		}

		public RTBranchPoint GetLastBranchPoint()
		{
			RTBranchPoint res = branchPoints[branchPoints.Count - 1];
			return res;
		}

		public void AddLeaf(RTLeafPoint leafAdded)
		{
			if(leafAdded.initSegmentIdx >= leavesOrderedByInitSegment.Length)
			{
				System.Array.Resize(ref leavesOrderedByInitSegment, leavesOrderedByInitSegment.Length * 2);

				for(int i = leafAdded.initSegmentIdx; i < leavesOrderedByInitSegment.Length; i++)
				{
					leavesOrderedByInitSegment[i] = new RTLeafPoint[1];
				}
			}

			leavesOrderedByInitSegment[leafAdded.initSegmentIdx][0] = leafAdded;
		}
	}
}