using System;
using System.Collections.Generic;
using UnityEngine;

namespace TeamCrescendo.ProceduralIvy
{
    [Serializable]
    public class RTIvyContainer
    {
        public int lastBranchNumberAssigned;
        public Vector3 firstVertexVector;

        public List<RTBranchContainer> branches;

        public void Initialize(Vector3 firstVertexVector)
        {
            lastBranchNumberAssigned = 0;
            this.firstVertexVector = firstVertexVector;

            branches = new List<RTBranchContainer>();
        }

        public void Initialize(IvyContainer ivyContainer, IvyParameters ivyParameters, GameObject ivyGO,
            RTMeshData[] leavesMeshesByChosenLeaf, Vector3 firstVertexVector)
        {
            lastBranchNumberAssigned = 0;
            branches = new List<RTBranchContainer>(ivyContainer.branches.Count);

            for (var i = 0; i < ivyContainer.branches.Count; i++)
            {
                var rtBranch = new RTBranchContainer(ivyContainer.branches[i], ivyParameters, this, ivyGO,
                    leavesMeshesByChosenLeaf);
                branches.Add(rtBranch);
            }

            this.firstVertexVector = firstVertexVector;
        }

        public void Initialize()
        {
            branches = new List<RTBranchContainer>();
        }

        public void AddBranch(RTBranchContainer rtBranch)
        {
            rtBranch.branchNumber = lastBranchNumberAssigned;
            branches.Add(rtBranch);
            lastBranchNumberAssigned++;
        }

        public RTBranchContainer GetBranchContainerByBranchNumber(int branchNumber)
        {
            foreach (var branch in branches)
            {
                if (branch.branchNumber == branchNumber)
                    return branch;
            }

            return null;
        }
    }
}