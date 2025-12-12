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
        public List<BranchPoint> branchPoints;
        public Vector3 growDirection;
        public List<LeafPoint> leaves;
        public float totalLenght;
        public float fallIteration;
        public bool falling;
        public Quaternion rotationOnFallIteration;
        public int branchSense;
        public float heightParameter;
        public float randomizeHeight;
        public float heightVar;
        public float currentHeight;
        public float deltaHeight;
        public float newHeight;

        public BranchPoint originPointOfThisBranch;
        public int branchNumber;

        public Dictionary<int, List<LeafPoint>> dictRTLeavesByInitSegment;

        public int GetNumLeaves()
        {
            return leaves.Count;
        }

        public void SetValues(Vector3 growDirection, float randomizeHeight,
            float currentHeight, float heightParameter, int branchSense, BranchPoint originPointOfThisBranch)
        {
            branchPoints = new List<BranchPoint>(1000);
            this.growDirection = growDirection;
            leaves = new List<LeafPoint>(1000);
            totalLenght = 0f;
            fallIteration = 0f;
            falling = false;
            rotationOnFallIteration = Quaternion.identity;
            this.branchSense = branchSense;
            this.heightParameter = heightParameter;
            this.randomizeHeight = randomizeHeight;
            heightVar = 0f;
            this.currentHeight = currentHeight;
            deltaHeight = 0f;
            newHeight = 0f;
            this.originPointOfThisBranch = originPointOfThisBranch;
            branchNumber = -1;
        }

        public void Init(int branchPointsSize, int numLeaves)
        {
            branchPoints = new List<BranchPoint>(branchPointsSize * 2);
            leaves = new List<LeafPoint>(numLeaves * 2);
        }

        public void Init()
        {
            Init(0, 0);
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

        public void AddBranchPoint(Vector3 point, Vector3 grabVector)
        {
            AddBranchPoint(point, grabVector, false, -1);
        }

        public void AddBranchPoint(Vector3 point, Vector3 grabVector, bool isNewBranch, int newBranchIndex)
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
                if (leaves[i].initSegmentIdx == initSegment.index)
                    res.Add(leaves[i]);
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
            var res = InsertLeaf(pointWS, lpLength, forward,
                -initSegment.grabVector, chosenLeave, leafIndex,
                initSegment, endSegment);

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
            if (leaves[^1].endSegmentIdx >= branchPoints.Count)
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

        public BranchPoint GetNearestPointSSFrom(Vector2 from)
        {
            BranchPoint res = null;
            var minDistance = float.MaxValue;

            for (var i = 0; i < branchPoints.Count; i++)
            {
                var newSqrDst = (branchPoints[i].pointSS - from).sqrMagnitude;
                if (newSqrDst <= minDistance)
                {
                    res = branchPoints[i];
                    minDistance = newSqrDst;
                }
            }

            return res;
        }

        public Vector3[] GetSegmentPoints(Vector3 worldPoint)
        {
            var res = new Vector3[2];

            var initSegment = Vector3.zero;
            var endSegment = Vector3.zero;

            var nearestPoint = GetNearestPointFrom(worldPoint);
            var nextPoint = nearestPoint.GetNextPoint();
            var previousPoint = nearestPoint.GetPreviousPoint();

            if (nextPoint != null && previousPoint != null)
            {
                var distanceToNextPoint = (worldPoint - nextPoint.point).magnitude;
                var distanceToPreviousPoint = (worldPoint - previousPoint.point).magnitude;

                if (distanceToNextPoint <= distanceToPreviousPoint)
                {
                    initSegment = nearestPoint.point;
                    endSegment = nextPoint.point;
                }
                else
                {
                    initSegment = previousPoint.point;
                    endSegment = nearestPoint.point;
                }
            }

            res[0] = initSegment;
            res[1] = endSegment;

            return res;
        }

        public BranchPoint GetLastBranchPoint()
        {
            return branchPoints[^1];
        }

        public void AddLeaf(LeafPoint leafPoint)
        {
            leaves.Add(leafPoint);
        }

        public LeafPoint AddLeaf(Vector3 leafPoint, float lpLength, Vector3 lpForward, Vector3 lpUpward,
            int chosenLeave, BranchPoint initSegment, BranchPoint endSegment)
        {
            var newLeaf = new LeafPoint(leafPoint, lpLength, lpForward, lpUpward, chosenLeave, initSegment, endSegment);
            leaves.Add(newLeaf);
            return newLeaf;
        }

        public LeafPoint InsertLeaf(Vector3 leafPoint, float lpLength, Vector3 lpForward, Vector3 lpUpward,
            int chosenLeave, int leafIndex, BranchPoint initSegment, BranchPoint endSegment)
        {
            var newLeaf = new LeafPoint(leafPoint, lpLength, lpForward, lpUpward, chosenLeave, initSegment, endSegment);

            var clampedLeafIndex = Mathf.Clamp(leafIndex, 0, int.MaxValue);

            leaves.Insert(clampedLeafIndex, newLeaf);
            return newLeaf;
        }

        public void RemoveLeaves(List<LeafPoint> leaves)
        {
            for (var i = 0; i < leaves.Count; i++)
                this.leaves.Remove(leaves[i]);
        }

        public void DrawLeavesVectors(List<BranchPoint> branchPointsToFilter)
        {
            for (var i = 0; i < leaves.Count; i++)
                leaves[i].DrawVectors();
        }

        public void GetInitIdxEndIdxLeaves(int initIdxBranchPoint, float stepSize, out int initIdxLeaves,
            out int endIdxLeaves)
        {
            var initIdxFound = false;
            var endIdxFound = false;

            initIdxLeaves = -1;
            endIdxLeaves = -1;

            for (var i = 0; i < leaves.Count; i++)
            {
                if (!initIdxFound && leaves[i].lpLength > initIdxBranchPoint * stepSize)
                {
                    initIdxFound = true;
                    initIdxLeaves = i;
                }

                if (!endIdxFound && leaves[i].lpLength >= totalLenght)
                {
                    endIdxFound = true;
                    endIdxLeaves = i;
                    break;
                }
            }
        }

        public void ReleasePoint(int indexPoint)
        {
            if (indexPoint < branchPoints.Count)
                branchPoints[indexPoint].ReleasePoint();
        }

        public void GetInitIdxEndIdxLeaves(int initIdxBranchPoint, int endIdxBranchPoint, float stepSize,
            out int initIdxLeaves, out int endIdxLeaves)
        {
            var initIdxFound = false;
            var endIdxFound = false;

            initIdxLeaves = -1;
            endIdxLeaves = -1;

            for (var i = 0; i < leaves.Count; i++)
            {
                if (!initIdxFound && leaves[i].lpLength >= initIdxBranchPoint * stepSize)
                {
                    initIdxFound = true;
                    initIdxLeaves = i;
                }

                if (!endIdxFound && leaves[i].lpLength >= endIdxBranchPoint * stepSize)
                {
                    endIdxFound = true;
                    endIdxLeaves = i - 1;
                    break;
                }
            }
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

                leaves[i].forwarRot = Quaternion.FromToRotation(oldForward, newForward);

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