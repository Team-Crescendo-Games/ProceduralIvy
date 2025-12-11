using System.Collections.Generic;
using UnityEngine;


namespace Dynamite3D.RealIvy
{
	[System.Serializable]
	public class RTIvyContainer
	{
		public int lastBranchNumberAssigned;

		//public GameObject ivyGO;
		public List<RTBranchContainer> branches;
		public Vector3 firstVertexVector;

		public void Initialize(Vector3 firstVertexVector)
		{
			lastBranchNumberAssigned = 0;
			this.firstVertexVector = firstVertexVector;

			this.branches = new List<RTBranchContainer>();
		}

		public void Initialize(IvyContainer ivyContainer, IvyParameters ivyParameters, GameObject ivyGO, RTMeshData[] leavesMeshesByChosenLeaf, Vector3 firstVertexVector)
		{
			lastBranchNumberAssigned = 0;
			this.branches = new List<RTBranchContainer>(ivyContainer.branches.Count);

			for (int i = 0; i < ivyContainer.branches.Count; i++)
			{
				RTBranchContainer rtBranch = new RTBranchContainer(ivyContainer.branches[i], ivyParameters, this, ivyGO, leavesMeshesByChosenLeaf);
				this.branches.Add(rtBranch);
			}

			this.firstVertexVector = firstVertexVector;
		}

		public void Initialize()
		{
			this.branches = new List<RTBranchContainer>();
		}

		public void AddBranch(RTBranchContainer rtBranch)
		{
			rtBranch.branchNumber = lastBranchNumberAssigned;
			branches.Add(rtBranch);

			lastBranchNumberAssigned++;
		}

		public RTBranchContainer GetBranchContainerByBranchNumber(int newBranchNumber)
		{
			RTBranchContainer res = null;

			for (int i = 0; i < branches.Count; i++)
			{
				if (branches[i].branchNumber == newBranchNumber)
				{
					res = branches[i];
					break;
				}
			}

			return res;
		}
	}
}