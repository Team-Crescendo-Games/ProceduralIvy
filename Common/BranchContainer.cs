using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Dynamite3D.RealIvy
{
    [System.Serializable]
    //Contenedor de rama, para toda la información necesaria para componer cada rama
    public class BranchContainer : ScriptableObject
    {
        public List<BranchPoint> branchPoints;
        public Vector3 growDirection;
        public List<LeafPoint> leaves;
        //public List<float> lenghts;
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


        //RUNRIME
        public Dictionary<int, List<LeafPoint>> dictRTLeavesByInitSegment;


        public int GetNumLeaves()
        {
            return leaves.Count;
        }

        public void SetValues(Vector3 growDirection, float randomizeHeight,
            float currentHeight, float heightParameter, int branchSense, BranchPoint originPointOfThisBranch)
        {
            this.branchPoints = new List<BranchPoint>(1000);
            this.growDirection = growDirection;
            this.leaves = new List<LeafPoint>(1000);
            this.totalLenght = 0f;
            this.fallIteration = 0f;
            this.falling = false;
            this.rotationOnFallIteration = Quaternion.identity;
            this.branchSense = branchSense;
            this.heightParameter = heightParameter;
            this.randomizeHeight = randomizeHeight;
            this.heightVar = 0f;
            this.currentHeight = currentHeight;
            this.deltaHeight = 0f;
            this.newHeight = 0f;
            this.originPointOfThisBranch = originPointOfThisBranch;
            this.branchNumber = -1;
        }

        /*BORRAR - Borrar este metodo!!!*/
        public void AddBranchPoint(BranchPoint branchPoint, float length, float stepSize)
        {
            branchPoint.branchContainer = this;
            branchPoints.Add(branchPoint);

            //totalLenght += stepSize;
        }


        /*RT IVY GROWTH*/
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

            for (int i = 0; i < branchPoints.Count; i++)
            {
                List<LeafPoint> leaves = new List<LeafPoint>();
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
                List<LeafPoint> newEntryLeaves = new List<LeafPoint>();
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
            BranchPoint newBranchPoint = new BranchPoint(point, grabVector,
                branchPoints.Count, isNewBranch, newBranchIndex, totalLenght, this);

            branchPoints.Add(newBranchPoint);
        }

        public BranchPoint InsertBranchPoint(Vector3 point, Vector3 grabVector, int index)
        {
            //float newPointLength = branchPoints[index].length + Vector3.Distance(branchPoints[index].point, point);
            float newPointLength = Mathf.Lerp(branchPoints[index - 1].length, branchPoints[index].length, 0.5f);

            BranchPoint newBranchPoint = new BranchPoint(point, grabVector, index, newPointLength, this);
            branchPoints.Insert(index, newBranchPoint);


            //Debug.Log("LENGTHS: " + branchPoints[index].length.ToString("F10") + " - " + newPointLength.ToString("F10") + " - " + branchPoints[index + 1].length.ToString("F10"));

            for (int i = index + 1; i < branchPoints.Count; i++)
            {
                branchPoints[i].index += 1;
            }

            return newBranchPoint;
        }

        public void GetLeavesInSegmentRT(int initSegmentIdx, int endSegmentIdx, List<LeafPoint> res)
        {
            for (int i = initSegmentIdx; i <= endSegmentIdx; i++)
            {
                if (dictRTLeavesByInitSegment.ContainsKey(i))
                {
                    res.AddRange(dictRTLeavesByInitSegment[i]);
                }
            }
        }

        /*public List<LeafPoint> FilterLeavesByLengthRT(List<LeafPoint> leaves, float maxLength)
        {
            List<LeafPoint> res = new List<LeafPoint>();
            for (int i = 0; i < leaves.Count; i++)
            {
                if(leaves[i].lpLength <= maxLength * 1.15f)
                {
                    res.Add(leaves[i]);
                }
            }

            return res;
        }*/



        public void GetLeavesInSegment(BranchPoint initSegment, List<LeafPoint> res)
        {
            for (int i = 0; i < leaves.Count; i++)
            {
                if (leaves[i].initSegmentIdx == initSegment.index)
                {
                    res.Add(leaves[i]);
                }
            }
        }

        public List<LeafPoint> GetLeavesInSegment(BranchPoint initSegment)
        {
            List<LeafPoint> res = new List<LeafPoint>();
            GetLeavesInSegment(initSegment, res);
            return res;
        }

        public LeafPoint AddRandomLeaf(Vector3 pointWS, BranchPoint initSegment, BranchPoint endSegment, int leafIndex, InfoPool infoPool)
        {
            int chosenLeave = UnityEngine.Random.Range(0, infoPool.ivyParameters.leavesPrefabs.Length);

            Vector3 forward = initSegment.initialGrowDir;

            float lpLength = initSegment.length + Vector3.Distance(pointWS, initSegment.point);
            LeafPoint res = InsertLeaf(pointWS, lpLength, forward,
                -initSegment.grabVector, chosenLeave, leafIndex,
                initSegment, endSegment);

            return res;
        }

#if UNITY_EDITOR
        public void RepositionLeaves02(List<LeafPoint> leaves, bool updatePosition)
        {
            for (int i = 0; i < leaves.Count; i++)
            {
                BranchPoint previousPoint = null;
                BranchPoint nextPoint = null;


                if (leaves[i].initSegmentIdx > branchPoints.Count - 1 || leaves[i].endSegmentIdx > branchPoints.Count - 1)
                {
                    previousPoint = branchPoints[leaves[i].initSegmentIdx];
                    nextPoint = branchPoints[leaves[i].endSegmentIdx - 1];
                }
                else
                {
                    previousPoint = branchPoints[leaves[i].initSegmentIdx];
                    nextPoint = branchPoints[leaves[i].endSegmentIdx];
                }


                Vector3 newForward = (nextPoint.point - previousPoint.point).normalized;


                Vector3 oldForward = leaves[i].lpForward;

                leaves[i].forwarRot = Quaternion.FromToRotation(oldForward, newForward);


                Vector3 newLeafPosition = Vector3.LerpUnclamped(previousPoint.point, nextPoint.point, leaves[i].displacementFromInitSegment);
                float angle = Vector3.Angle(oldForward, newForward);


                //Vector3 axis = SceneView.currentDrawingSceneView.camera.transform.forward;
                Vector3 cameraForward = SceneView.currentDrawingSceneView.camera.transform.forward;

                /*Vector3 axis = -leaves[i].left.normalized;*/
                Vector3 axis = cameraForward;

                Vector3 crossNewForwardOldForward = Vector3.Cross(newForward, oldForward);
                float sign = Mathf.Sign(Vector3.Dot(crossNewForwardOldForward, cameraForward));

                //float sign = Mathf.Sign(Vector3.Dot(newForward, oldForward));
                //Vector3 newUpward = Vector3.Cross(-newForward, -leaves[i].left.normalized).normalized;
                //Vector3 newUpward = Quaternion.AngleAxis(angle * -sign, axis) * leaves[i].lpUpward;
                //newUpward = newUpward.normalized;*/

                if (updatePosition)
                {
                    leaves[i].point = newLeafPosition;
                }
            }
        }

        public void RepositionLeaves02()
        {
            RepositionLeaves02(this.leaves, true);
        }

        public void RepositionLeaves02(bool updatePositionLeaves)
        {
            RepositionLeaves02(this.leaves, updatePositionLeaves);
        }
#endif

        //private Vector3 GetNewLeafUpward()
        //{
        //	Vector3 oldForward = leaves[i].lpForward;
        //	Vector3 newLeafPosition = Vector3.LerpUnclamped(previousPoint.point, nextPoint.point, leaves[i].displacementFromInitSegment);

        //	float angle = Vector3.Angle(oldForward, newForward);
        //	Vector3 axis = cameraForward;
        //	Vector3 crossNewForwardOldForward = Vector3.Cross(newForward, oldForward);
        //	float sign = Mathf.Sign(Vector3.Dot(crossNewForwardOldForward, cameraForward));
        //	Vector3 newUpward = Quaternion.AngleAxis(angle * -sign, axis) * leaves[i].lpUpward;

        //	return newUpward;
        //}

        public void RepositionLeavesAfterAdd02(BranchPoint newPoint)
        {
            BranchPoint previousPoint = newPoint.GetPreviousPoint();
            BranchPoint nextPoint = newPoint.GetNextPoint();

            List<LeafPoint> leaves = new List<LeafPoint>();
            GetLeavesInSegment(previousPoint, leaves);
            //leaves.AddRange(GetLeavesInSegment(removedPoint));

            Vector3 dirSegment01 = (newPoint.point - previousPoint.point).normalized;
            Vector3 dirSegment02 = (nextPoint.point - newPoint.point).normalized;
            for (int i = 0; i < leaves.Count; i++)
            {
                Vector3 oldLeafVector01 = leaves[i].point - branchPoints[leaves[i].initSegmentIdx].point;
                Vector3 oldLeafVector02 = leaves[i].point - branchPoints[leaves[i].endSegmentIdx].point;

                Vector3 projectionOnSegment01 = previousPoint.point + dirSegment01 * Vector3.Dot(oldLeafVector01, dirSegment01);
                Vector3 projectionOnSegment02 = nextPoint.point + dirSegment02 * Vector3.Dot(oldLeafVector02, dirSegment02);
                Vector3 newLeafPositionToNewPoint = newPoint.point - projectionOnSegment01;

                if (Vector3.Dot(newLeafPositionToNewPoint, dirSegment01) >= 0)
                {
                    leaves[i].SetValues(projectionOnSegment01, leaves[i].lpLength, dirSegment01, leaves[i].lpUpward, leaves[i].chosenLeave, previousPoint, newPoint);
                }
                else
                {
                    leaves[i].SetValues(projectionOnSegment02, leaves[i].lpLength, dirSegment02, leaves[i].lpUpward, leaves[i].chosenLeave, newPoint, nextPoint);
                }
            }
        }



        public void RepositionLeavesAfterRemove02(BranchPoint removedPoint)
        {
            BranchPoint previousPoint = removedPoint.GetPreviousPoint();
            BranchPoint nextPoint = removedPoint.GetNextPoint();

            List<LeafPoint> leaves = GetLeavesInSegment(previousPoint);
            leaves.AddRange(GetLeavesInSegment(removedPoint));

            for (int i = 0; i < leaves.Count; i++)
            {
                Vector3 pointToLeaf = leaves[i].point - previousPoint.point;
                Vector3 newSegmentDir = (nextPoint.point - previousPoint.point).normalized;
                float dotProduct = Vector3.Dot(pointToLeaf, newSegmentDir);

                Vector3 newLeafPosition = previousPoint.point + newSegmentDir * dotProduct;

                //leaves[i].point = newLeafPosition;
                leaves[i].SetValues(newLeafPosition, leaves[i].lpLength, previousPoint.initialGrowDir,
                    -previousPoint.grabVector, leaves[i].chosenLeave, previousPoint, nextPoint);

                //leaves[i].lpLength = 0f;
            }
        }

        public void RemoveBranchPoint(int indexToRemove)
        {
            //List<LeafPoint> orphanLeaves = branchPoints[indexToRemove].leaves;
            //RepositionLeaves02(branchPoints[indexToRemove]);
            //RepositionLeaves02(branchPoints[indexToRemove].GetPreviousPoint());
            //RepositionLeaves02(branchPoints[indexToRemove].GetNextPoint());


            RepositionLeavesAfterRemove02(branchPoints[indexToRemove]);


            for (int i = indexToRemove + 1; i < branchPoints.Count; i++)
            {

                List<LeafPoint> modifiedLeaves = new List<LeafPoint>();
                GetLeavesInSegment(branchPoints[i], modifiedLeaves);

                for (int j = 0; j < modifiedLeaves.Count; j++)
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
            List<LeafPoint> removedLeaves = new List<LeafPoint>();
            for (int i = index; i < index + count; i++)
            {
                GetLeavesInSegment(branchPoints[i], removedLeaves);
            }



            for (int i = 0; i < removedLeaves.Count; i++)
            {
                leaves.Remove(removedLeaves[i]);
            }

            for (int i = index + count; i < branchPoints.Count; i++)
            {
                branchPoints[i].index -= 1;
            }


            totalLenght = branchPoints[index - 1].length;
            branchPoints.RemoveRange(index, count);

            //Borramos la ultima hoja por seguridad en caso de que se haya quedado sin segmento
            if (leaves[leaves.Count - 1].endSegmentIdx >= branchPoints.Count)
            {
                leaves.RemoveAt(leaves.Count - 1);
            }
        }

        public BranchPoint GetNearestPointFrom(Vector3 from)
        {
            BranchPoint res = null;
            float minDistance = float.MaxValue;

            for (int i = 0; i < branchPoints.Count; i++)
            {
                float newSqrDst = (branchPoints[i].point - from).sqrMagnitude;
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
            float minDistance = float.MaxValue;

            for (int i = 0; i < branchPoints.Count; i++)
            {
                float newSqrDst = (branchPoints[i].point - from).sqrMagnitude;
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
            float minDistance = float.MaxValue;

            for (int i = 0; i < branchPoints.Count; i++)
            {
                float newSqrDst = (branchPoints[i].pointSS - from).sqrMagnitude;
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

            Vector3[] res = new Vector3[2];

            Vector3 initSegment = Vector3.zero;
            Vector3 endSegment = Vector3.zero;

            BranchPoint nearestPoint = GetNearestPointFrom(worldPoint);
            BranchPoint nextPoint = nearestPoint.GetNextPoint();
            BranchPoint previousPoint = nearestPoint.GetPreviousPoint();

            if (nextPoint != null && previousPoint != null)
            {
                float distanceToNextPoint = (worldPoint - nextPoint.point).magnitude;
                float distanceToPreviousPoint = (worldPoint - previousPoint.point).magnitude;

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
            BranchPoint res = branchPoints[branchPoints.Count - 1];
            return res;
        }

        public void AddLeaf(LeafPoint leafPoint)
        {
            leaves.Add(leafPoint);
        }

        public LeafPoint AddLeaf(Vector3 leafPoint, float lpLength, Vector3 lpForward, Vector3 lpUpward, int chosenLeave, BranchPoint initSegment, BranchPoint endSegment)
        {
            LeafPoint newLeaf = new LeafPoint(leafPoint, lpLength, lpForward, lpUpward, chosenLeave, initSegment, endSegment);
            leaves.Add(newLeaf);
            return newLeaf;
        }

        public LeafPoint InsertLeaf(Vector3 leafPoint, float lpLength, Vector3 lpForward, Vector3 lpUpward,
            int chosenLeave, int leafIndex, BranchPoint initSegment, BranchPoint endSegment)
        {
            LeafPoint newLeaf = new LeafPoint(leafPoint, lpLength, lpForward, lpUpward, chosenLeave, initSegment, endSegment);

            int clampedLeafIndex = Mathf.Clamp(leafIndex, 0, int.MaxValue);

            leaves.Insert(clampedLeafIndex, newLeaf);
            return newLeaf;
        }

        public void RemoveLeaves(List<LeafPoint> leaves)
        {
            for (int i = 0; i < leaves.Count; i++)
            {
                this.leaves.Remove(leaves[i]);
            }
        }

        public void DrawLeavesVectors(List<BranchPoint> branchPointsToFilter)
        {
            for (int i = 0; i < leaves.Count; i++)
            {
                //if (branchPointsToFilter.Contains(leaves[i].initSegment))
                //{
                leaves[i].DrawVectors();
                //}
            }
        }

        public void GetInitIdxEndIdxLeaves(int initIdxBranchPoint, float stepSize, out int initIdxLeaves, out int endIdxLeaves)
        {
            bool initIdxFound = false;
            bool endIdxFound = false;

            initIdxLeaves = -1;
            endIdxLeaves = -1;

            for (int i = 0; i < leaves.Count; i++)
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
            {
                branchPoints[indexPoint].ReleasePoint();
            }
        }

        public void GetInitIdxEndIdxLeaves(int initIdxBranchPoint, int endIdxBranchPoint, float stepSize, out int initIdxLeaves, out int endIdxLeaves)
        {
            bool initIdxFound = false;
            bool endIdxFound = false;

            initIdxLeaves = -1;
            endIdxLeaves = -1;

            for (int i = 0; i < leaves.Count; i++)
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
    }
}