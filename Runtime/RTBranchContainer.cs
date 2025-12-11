using System;
using System.Collections.Generic;
using UnityEngine;

namespace TeamCrescendo.ProceduralIvy
{
    public class RTBranchContainer
    {
        public int branchNumber;
        public List<RTBranchPoint> branchPoints;

        public int branchSense;
        public float currentHeight;
        public float deltaHeight;

        public bool falling;
        public float fallIteration;
        public Vector3 growDirection;
        public float heightParameter;

        public float heightVar;

        public RTLeafPoint[][] leavesOrderedByInitSegment;
        public float newHeight;
        public float randomizeHeight;
        public Quaternion rotationOnFallIteration;

        public float totalLength;

        public RTBranchContainer(BranchContainer branchContainer, IvyParameters ivyParameters,
            RTIvyContainer rtIvyContainer,
            GameObject ivyGO, RTMeshData[] leavesMeshesByChosenLeaf)
        {
            totalLength = branchContainer.totalLenght;
            growDirection = branchContainer.growDirection;
            randomizeHeight = branchContainer.randomizeHeight;
            heightVar = branchContainer.heightVar;
            newHeight = branchContainer.newHeight;
            heightParameter = branchContainer.heightParameter;
            deltaHeight = branchContainer.deltaHeight;
            currentHeight = branchContainer.currentHeight;
            branchSense = branchContainer.branchSense;
            falling = branchContainer.falling;
            rotationOnFallIteration = branchContainer.rotationOnFallIteration;
            branchNumber = branchContainer.branchNumber;

            branchPoints = new List<RTBranchPoint>(branchContainer.branchPoints.Count);
            for (var i = 0; i < branchContainer.branchPoints.Count; i++)
            {
                var rtBranchPoint = new RTBranchPoint(branchContainer.branchPoints[i], this);

                rtBranchPoint.CalculateCenterLoop(ivyGO);
                rtBranchPoint.PreInit(ivyParameters);
                rtBranchPoint.CalculateVerticesLoop(ivyParameters, rtIvyContainer, ivyGO);

                branchPoints.Add(rtBranchPoint);
            }

            branchContainer.PrepareRTLeavesDict();

            if (ivyParameters.generateLeaves)
            {
                leavesOrderedByInitSegment = new RTLeafPoint[branchPoints.Count][];
                for (var i = 0; i < branchPoints.Count; i++)
                {
                    var leavesToBake = branchContainer.dictRTLeavesByInitSegment[i];
                    var numLeaves = 0;
                    if (leavesToBake != null) numLeaves = leavesToBake.Count;


                    leavesOrderedByInitSegment[i] = new RTLeafPoint[numLeaves];


                    for (var j = 0; j < numLeaves; j++)
                    {
                        var rtLeafPoint = new RTLeafPoint(leavesToBake[j], ivyParameters);
                        var leafMeshData = leavesMeshesByChosenLeaf[rtLeafPoint.chosenLeave];

                        rtLeafPoint.CreateVertices(ivyParameters, leafMeshData, ivyGO);
                        leavesOrderedByInitSegment[i][j] = rtLeafPoint;
                    }
                }
            }
        }

        public RTBranchContainer(int numPoints, int numLeaves)
        {
            Init(numPoints, numLeaves);
        }

        public Vector2 GetLastUV(IvyParameters ivyParameters)
        {
            var res = new Vector2(totalLength * ivyParameters.uvScale.y + ivyParameters.uvOffset.y,
                0.5f * ivyParameters.uvScale.x + ivyParameters.uvOffset.x);
            return res;
        }

        private void Init(int numPoints, int numLeaves)
        {
            branchPoints = new List<RTBranchPoint>(numPoints);

            leavesOrderedByInitSegment = new RTLeafPoint[numPoints][];
            for (var i = 0; i < numPoints; i++) leavesOrderedByInitSegment[i] = new RTLeafPoint[1];
        }

        public void AddBranchPoint(RTBranchPoint rtBranchPoint, float deltaLength)
        {
            totalLength += deltaLength;

            rtBranchPoint.length = totalLength;
            rtBranchPoint.index = branchPoints.Count;
            rtBranchPoint.branchContainer = this;

            branchPoints.Add(rtBranchPoint);
        }

        public RTBranchPoint GetLastBranchPoint() => branchPoints[^1];

        public void AddLeaf(RTLeafPoint leafAdded)
        {
            if (leafAdded.initSegmentIdx >= leavesOrderedByInitSegment.Length)
            {
                Array.Resize(ref leavesOrderedByInitSegment, leavesOrderedByInitSegment.Length * 2);

                for (var i = leafAdded.initSegmentIdx; i < leavesOrderedByInitSegment.Length; i++)
                    leavesOrderedByInitSegment[i] = new RTLeafPoint[1];
            }

            leavesOrderedByInitSegment[leafAdded.initSegmentIdx][0] = leafAdded;
        }
    }
}