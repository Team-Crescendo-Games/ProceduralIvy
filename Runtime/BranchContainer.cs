using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
#if UNITY_EDITOR
#endif

namespace TeamCrescendo.ProceduralIvy
{
    [Serializable]
    [PreferBinarySerialization]
    public class BranchContainer : ScriptableObject
    {
        public Vector3 growDirection;
        public Quaternion rotationOnFallIteration;
        public float totalLenght;
        public float fallIteration;
        public int branchNumber;
        public int branchSense;
        public float heightParameter;
        public float randomizeHeight;
        public float heightVar;
        public float currentHeight;
        public float deltaHeight;
        public float newHeight;
        public bool falling;

        public BranchPoint originPointOfThisBranch;
        public List<BranchPoint> branchPoints;
        public List<LeafPoint> leaves = new();
        [NonSerialized] public LeafPoint[][] leavesOrderedByInitSegment;

        public void Init(int numPoints, int numLeaves)
        {
            branchPoints = new List<BranchPoint>(numPoints);

            leavesOrderedByInitSegment = new LeafPoint[numPoints][];
            for (var i = 0; i < numPoints; i++) 
                leavesOrderedByInitSegment[i] = new LeafPoint[1];
        }
        
        public void Init(BranchContainer branchContainer, IvyParameters ivyParameters,
            IvyContainer rtIvyContainer, GameObject ivyGO, MeshData[] leavesMeshesByChosenLeaf)
        {
            totalLenght = branchContainer.totalLenght;
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

            branchPoints = new List<BranchPoint>(branchContainer.branchPoints.Count);
            for (var i = 0; i < branchContainer.branchPoints.Count; i++)
            {
                var rtBranchPoint = new BranchPoint(branchContainer.branchPoints[i], this, ivyParameters);

                rtBranchPoint.CalculateCenterLoop(ivyGO);
                rtBranchPoint.CalculateVerticesLoop(ivyParameters, rtIvyContainer, ivyGO);

                branchPoints.Add(rtBranchPoint);
            }

            var dictRTLeavesByInitSegment = branchContainer.PrepareRTLeavesDict();

            if (ivyParameters.generateLeaves)
            {
                leavesOrderedByInitSegment = new LeafPoint[branchPoints.Count][];
                for (var i = 0; i < branchPoints.Count; i++)
                {
                    var leavesToBake = dictRTLeavesByInitSegment[i];
                    var numLeaves = leavesToBake.Count;
                    leavesOrderedByInitSegment[i] = new LeafPoint[numLeaves];
                    
                    if (numLeaves == 0) continue;
                    
                    for (var j = 0; j < numLeaves; j++)
                    {
                        var rtLeafPoint = new LeafPoint(leavesToBake[j]);
                        var leafMeshData = leavesMeshesByChosenLeaf[rtLeafPoint.chosenLeave];

                        rtLeafPoint.CreateVertices(ivyParameters, leafMeshData, ivyGO.transform);
                        leavesOrderedByInitSegment[i][j] = rtLeafPoint;
                    }
                }
            }
        }

        public Dictionary<int, List<LeafPoint>> PrepareRTLeavesDict()
        {
            var dictRTLeavesByInitSegment = new Dictionary<int, List<LeafPoint>>();

            for (var i = 0; i < branchPoints.Count; i++)
            {
                var tmpLeaves = new List<LeafPoint>();
                GetLeavesInSegment(branchPoints[i], tmpLeaves);
                dictRTLeavesByInitSegment[i] = tmpLeaves;
            }
            
            return dictRTLeavesByInitSegment;
        }

        public void AddBranchPoint(Vector3 point, Vector3 grabVector, bool isNewBranch=false, int newBranchIndex=-1)
        {
            branchPoints ??= new List<BranchPoint>();
            var newBranchPoint = new BranchPoint(point, grabVector,
                branchPoints.Count, isNewBranch, newBranchIndex, totalLenght, this);
            branchPoints.Add(newBranchPoint);
        }
        
        public void AddBranchPoint(BranchPoint rtBranchPoint, float deltaLength)
        {
            totalLenght += deltaLength;

            rtBranchPoint.length = totalLenght;
            rtBranchPoint.index = branchPoints.Count;
            rtBranchPoint.branchContainer = this;

            branchPoints.Add(rtBranchPoint);
        }

        public BranchPoint InsertBranchPoint(Vector3 point, Vector3 grabVector, int index)
        {
            var newPointLength = Mathf.Lerp(branchPoints[index - 1].length, branchPoints[index].length, 0.5f);

            var newBranchPoint = new BranchPoint(point, grabVector, index, newPointLength, this);
            branchPoints.Insert(index, newBranchPoint);

            for (var i = index + 1; i < branchPoints.Count; i++) 
                branchPoints[i].index += 1;

            return newBranchPoint;
        }

        public void GetLeavesInSegment(BranchPoint initSegment, List<LeafPoint> res)
        {
            foreach (var leaf in leaves)
            {
                if (leaf.initSegmentIdx == initSegment.index) 
                    res.Add(leaf);
            }
        }

        public List<LeafPoint> GetLeavesInSegment(BranchPoint initSegment)
        {
            var res = new List<LeafPoint>();
            GetLeavesInSegment(initSegment, res);
            return res;
        }

        public LeafPoint AddRandomLeaf(Vector3 pointWS, BranchPoint initSegment, BranchPoint endSegment, int leafIndex,
            InfoPool infoPool)
        {
            var chosenLeave = Random.Range(0, infoPool.ivyParameters.leavesPrefabs.Length);

            var forward = initSegment.initialGrowDir;

            var lpLength = initSegment.length + Vector3.Distance(pointWS, initSegment.point);
            var res = AddLeaf(pointWS, lpLength, forward,
                -initSegment.grabVector, chosenLeave,
                initSegment, endSegment, leafIndex);

            return res;
        }

        public void RepositionLeavesAfterRemove02(BranchPoint removedPoint)
        {
            var previousPoint = removedPoint.GetPreviousPoint();
            var nextPoint = removedPoint.GetNextPoint();

            var leavesInSegment = GetLeavesInSegment(previousPoint);
            leavesInSegment.AddRange(GetLeavesInSegment(removedPoint));

            foreach (var leaf in leavesInSegment)
            {
                var pointToLeaf = leaf.point - previousPoint.point;
                var newSegmentDir = (nextPoint.point - previousPoint.point).normalized;
                var dotProduct = Vector3.Dot(pointToLeaf, newSegmentDir);

                var newLeafPosition = previousPoint.point + newSegmentDir * dotProduct;

                leaf.SetValues(newLeafPosition, leaf.lpLength, previousPoint.initialGrowDir,
                    -previousPoint.grabVector, leaf.chosenLeave, previousPoint, nextPoint);
            }
        }

        public void RemoveBranchPoint(int indexToRemove)
        {
            RepositionLeavesAfterRemove02(branchPoints[indexToRemove]);

            for (var i = indexToRemove + 1; i < branchPoints.Count; i++)
            {
                var modifiedLeaves = new List<LeafPoint>();
                GetLeavesInSegment(branchPoints[i], modifiedLeaves);

                for (var j = 0; j < modifiedLeaves.Count; j++)
                {
                    modifiedLeaves[j].initSegmentIdx -= 1;
                    modifiedLeaves[j].endSegmentIdx -= 1;
                }

                branchPoints[i].index -= 1;
            }

            branchPoints.RemoveAt(indexToRemove);
        }

        public void RemoveRange(int index, int count)
        {
            var removedLeaves = new List<LeafPoint>();
            for (var i = index; i < index + count; i++)
                GetLeavesInSegment(branchPoints[i], removedLeaves);

            for (var i = 0; i < removedLeaves.Count; i++)
                leaves.Remove(removedLeaves[i]);

            for (var i = index + count; i < branchPoints.Count; i++)
                branchPoints[i].index -= 1;

            totalLenght = branchPoints[index - 1].length;
            branchPoints.RemoveRange(index, count);

            // We delete the last leaf as a safety precaution in case it ran out of segments.
            if (leaves.Count > 0 && leaves[^1].endSegmentIdx >= branchPoints.Count)
                leaves.RemoveAt(leaves.Count - 1);
        }

        public BranchPoint GetNearestPointWSFrom(Vector3 from)
        {
            BranchPoint res = null;
            var minDistance = float.MaxValue;

            for (var i = 0; i < branchPoints.Count; i++)
            {
                var newSqrDst = (branchPoints[i].point - from).sqrMagnitude;
                if (newSqrDst <= minDistance)
                {
                    res = branchPoints[i];
                    minDistance = newSqrDst;
                }
            }

            return res;
        }

        public BranchPoint GetLastBranchPoint() => branchPoints[^1];

        // Appends a leaf at the given index or at the end if index is invalid.
        public LeafPoint AddLeaf(Vector3 position, float length, Vector3 forward, Vector3 upward,
            int prefabIndex, BranchPoint startSegment, BranchPoint endSegment, int atIndex = -1)
        {
            var newLeaf = new LeafPoint(position, length, forward, upward, prefabIndex, startSegment, endSegment);

            // If index is valid, insert there. Otherwise, append to the end.
            if (atIndex >= 0 && atIndex <= leaves.Count)
                leaves.Insert(atIndex, newLeaf);
            else
                leaves.Add(newLeaf);

            return newLeaf;
        }
        
        public void AddLeaf(LeafPoint leafAdded)
        {
            if (leafAdded.initSegmentIdx >= leavesOrderedByInitSegment.Length)
            {
                Array.Resize(ref leavesOrderedByInitSegment, leavesOrderedByInitSegment.Length * 2);

                for (var i = leafAdded.initSegmentIdx; i < leavesOrderedByInitSegment.Length; i++)
                    leavesOrderedByInitSegment[i] = new LeafPoint[1];
            }

            leavesOrderedByInitSegment[leafAdded.initSegmentIdx][0] = leafAdded;
        }
        
        public Vector2 GetLastUV(IvyParameters ivyParameters)
        {
            var res = new Vector2(totalLenght * ivyParameters.uvScale.y + ivyParameters.uvOffset.y,
                0.5f * ivyParameters.uvScale.x + ivyParameters.uvOffset.x);
            return res;
        }

        public void RemoveLeaves(List<LeafPoint> leavesToRemove)
        {
            var set = new HashSet<LeafPoint>(leavesToRemove);
            leaves.RemoveAll(x => set.Contains(x));
        }

        public void ReleasePoint(int indexPoint)
        {
            if (indexPoint < branchPoints.Count)
                branchPoints[indexPoint].ReleasePoint();
        }

        public void RepositionLeaves(List<LeafPoint> leaves, bool updatePosition)
        {
            if (branchPoints == null || branchPoints.Count < 2) return;

            int maxIndex = branchPoints.Count - 1;

            for (var i = 0; i < leaves.Count; i++)
            {
                int idxA = Mathf.Clamp(leaves[i].initSegmentIdx, 0, maxIndex - 1);
        
                int idxB = idxA + 1; 

                BranchPoint previousPoint = branchPoints[idxA];
                BranchPoint nextPoint = branchPoints[idxB];

                var direction = nextPoint.point - previousPoint.point;
                if (direction == Vector3.zero) continue;

                var newLeafPosition = Vector3.LerpUnclamped(previousPoint.point, nextPoint.point,
                    leaves[i].displacementFromInitSegment);

                if (updatePosition)
                    leaves[i].point = newLeafPosition;
            }
        }

        public void RepositionLeaves(bool updatePositionLeaves) => RepositionLeaves(leaves, updatePositionLeaves);
    }
}