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
        public List<LeafPoint> leaves;
        public Dictionary<int, List<LeafPoint>> dictRTLeavesByInitSegment;

        public BranchContainer()
        {
            Init(0,0);
        }

        public void Init(int branchPointsSize, int numLeaves)
        {
            branchPoints = new List<BranchPoint>(branchPointsSize * 2);
            leaves = new List<LeafPoint>(numLeaves * 2);
        }

        public void PrepareRTLeavesDict()
        {
            dictRTLeavesByInitSegment = new Dictionary<int, List<LeafPoint>>();

            for (var i = 0; i < branchPoints.Count; i++)
            {
                var leaves = new List<LeafPoint>();
                GetLeavesInSegment(branchPoints[i], leaves);
                dictRTLeavesByInitSegment[i] = leaves;
            }
        }

        public void UpdateLeavesDictEntry(int initSegmentIdx, LeafPoint leaf)
        {
            if (dictRTLeavesByInitSegment.ContainsKey(initSegmentIdx))
            {
                dictRTLeavesByInitSegment[initSegmentIdx].Add(leaf);
            }
            else
            {
                var newEntryLeaves = new List<LeafPoint>();
                newEntryLeaves.Add(leaf);
                dictRTLeavesByInitSegment[initSegmentIdx] = newEntryLeaves;
            }
        }

        public void AddBranchPoint(BranchPoint branchPoint)
        {
            branchPoint.index = branchPoints.Count;
            branchPoint.newBranch = false;
            branchPoint.newBranchNumber = -1;
            branchPoint.branchContainer = this;
            branchPoint.length = totalLenght;

            branchPoints.Add(branchPoint);
        }

        public void AddBranchPoint(Vector3 point, Vector3 grabVector, bool isNewBranch=false, int newBranchIndex=-1)
        {
            var newBranchPoint = new BranchPoint(point, grabVector,
                branchPoints.Count, isNewBranch, newBranchIndex, totalLenght, this);

            branchPoints.Add(newBranchPoint);
        }

        public BranchPoint InsertBranchPoint(Vector3 point, Vector3 grabVector, int index)
        {
            var newPointLength = Mathf.Lerp(branchPoints[index - 1].length, branchPoints[index].length, 0.5f);

            var newBranchPoint = new BranchPoint(point, grabVector, index, newPointLength, this);
            branchPoints.Insert(index, newBranchPoint);

            for (var i = index + 1; i < branchPoints.Count; i++) branchPoints[i].index += 1;

            return newBranchPoint;
        }

        public void GetLeavesInSegmentRT(int initSegmentIdx, int endSegmentIdx, List<LeafPoint> res)
        {
            for (var i = initSegmentIdx; i <= endSegmentIdx; i++)
                if (dictRTLeavesByInitSegment.ContainsKey(i))
                    res.AddRange(dictRTLeavesByInitSegment[i]);
        }

        public void GetLeavesInSegment(BranchPoint initSegment, List<LeafPoint> res)
        {
            for (var i = 0; i < leaves.Count; i++)
            {
                if (leaves[i].initSegmentIdx == initSegment.index)
                    res.Add(leaves[i]);
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

        public void RepositionLeavesAfterAdd02(BranchPoint newPoint)
        {
            var previousPoint = newPoint.GetPreviousPoint();
            var nextPoint = newPoint.GetNextPoint();

            var leaves = new List<LeafPoint>();
            GetLeavesInSegment(previousPoint, leaves);

            var dirSegment01 = (newPoint.point - previousPoint.point).normalized;
            var dirSegment02 = (nextPoint.point - newPoint.point).normalized;
            for (var i = 0; i < leaves.Count; i++)
            {
                var oldLeafVector01 = leaves[i].point - branchPoints[leaves[i].initSegmentIdx].point;
                var oldLeafVector02 = leaves[i].point - branchPoints[leaves[i].endSegmentIdx].point;

                var projectionOnSegment01 =
                    previousPoint.point + dirSegment01 * Vector3.Dot(oldLeafVector01, dirSegment01);
                var projectionOnSegment02 = nextPoint.point + dirSegment02 * Vector3.Dot(oldLeafVector02, dirSegment02);
                var newLeafPositionToNewPoint = newPoint.point - projectionOnSegment01;

                if (Vector3.Dot(newLeafPositionToNewPoint, dirSegment01) >= 0)
                    leaves[i].SetValues(projectionOnSegment01, leaves[i].lpLength, dirSegment01, leaves[i].lpUpward,
                        leaves[i].chosenLeave, previousPoint, newPoint);
                else
                    leaves[i].SetValues(projectionOnSegment02, leaves[i].lpLength, dirSegment02, leaves[i].lpUpward,
                        leaves[i].chosenLeave, newPoint, nextPoint);
            }
        }

        public void RepositionLeavesAfterRemove02(BranchPoint removedPoint)
        {
            var previousPoint = removedPoint.GetPreviousPoint();
            var nextPoint = removedPoint.GetNextPoint();

            var leaves = GetLeavesInSegment(previousPoint);
            leaves.AddRange(GetLeavesInSegment(removedPoint));

            for (var i = 0; i < leaves.Count; i++)
            {
                var pointToLeaf = leaves[i].point - previousPoint.point;
                var newSegmentDir = (nextPoint.point - previousPoint.point).normalized;
                var dotProduct = Vector3.Dot(pointToLeaf, newSegmentDir);

                var newLeafPosition = previousPoint.point + newSegmentDir * dotProduct;

                leaves[i].SetValues(newLeafPosition, leaves[i].lpLength, previousPoint.initialGrowDir,
                    -previousPoint.grabVector, leaves[i].chosenLeave, previousPoint, nextPoint);
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

        public BranchPoint GetNearestPointFrom(Vector3 from)
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

        public void AddLeaf(LeafPoint leafPoint) => leaves.Add(leafPoint);

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

#if UNITY_EDITOR
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

                var newForward = direction.normalized;
                var oldForward = leaves[i].lpForward;

                leaves[i].forwardRot = Quaternion.FromToRotation(oldForward, newForward);

                var newLeafPosition = Vector3.LerpUnclamped(previousPoint.point, nextPoint.point,
                    leaves[i].displacementFromInitSegment);

                if (updatePosition)
                    leaves[i].point = newLeafPosition;
            }
        }

        public void RepositionLeaves(bool updatePositionLeaves) => RepositionLeaves(leaves, updatePositionLeaves);
#endif
    }
}