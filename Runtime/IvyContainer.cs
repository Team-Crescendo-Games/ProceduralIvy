using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TeamCrescendo.ProceduralIvy
{
    [Serializable]
    [PreferBinarySerialization]
    public class IvyContainer : ScriptableObject
    {
        public List<BranchContainer> branches;
        public Vector3 firstVertexVector;

        private IvyContainer()
        {
            branches = new List<BranchContainer>();
        }

        public void Clear()
        {
            foreach (var branch in branches)
                DeleteBranchAsset(branch);

            branches.Clear();
        }
        
        private void DeleteBranchAsset(BranchContainer branch)
        {
#if UNITY_EDITOR
            Undo.DestroyObjectImmediate(branch);
#else
            Destroy(branch); 
#endif
        }
        
        private void RefreshBranchIndexing()
        {
            for (var i = 0; i < branches.Count; i++)
                branches[i].branchNumber = i;
        }

        public void RemoveBranch(BranchContainer branchToDelete)
        {
            if (branchToDelete.originPointOfThisBranch != null)
                branchToDelete.originPointOfThisBranch.branchContainer.ReleasePoint(branchToDelete
                    .originPointOfThisBranch.index);
            
            branches.Remove(branchToDelete);
            DeleteBranchAsset(branchToDelete);
            RefreshBranchIndexing();
        }

        public BranchContainer GetBranchContainerByBranchNumber(int branchNumber) => branches.Find(b => b.branchNumber == branchNumber);

        public BranchPoint[] GetNearestSegmentSSBelowDistance(Vector2 pointSS, float distanceThreshold)
        {
            BranchPoint[] res = null;
            BranchPoint initSegment = null;
            BranchPoint endSegment = null;

            var minDistance = distanceThreshold;

            for (var i = 0; i < branches.Count; i++)
            {
                for (var j = 1; j < branches[i].branchPoints.Count; j++)
                {
                    var a = branches[i].branchPoints[j - 1];
                    var b = branches[i].branchPoints[j];

                    var d = DistanceBetweenPointAndSegmentSS(pointSS, a.GetScreenspacePosition(), b.GetScreenspacePosition());

                    if (d <= minDistance)
                    {
                        minDistance = d;
                        initSegment = a;
                        endSegment = b;
                    }
                }
            }

            if (initSegment != null && endSegment != null)
            {
                res = new BranchPoint[2];
                res[0] = initSegment;
                res[1] = endSegment;
            }

            return res;
        }
        
        public static float DistanceBetweenPointAndSegmentSS(Vector2 point, Vector2 a, Vector2 b)
        {
            var res = 0f;

            var u = (point.x - a.x) * (b.x - a.x) + (point.y - a.y) * (b.y - a.y);
            u = u / ((b.x - a.x) * (b.x - a.x) + (b.y - a.y) * (b.y - a.y));

            if (u < 0)
            {
                res = (point - a).sqrMagnitude;
            }
            else if (u >= 0 && u <= 1)
            {
                var pointInSegment = new Vector2(a.x + u * (b.x - a.x), a.y + u * (b.y - a.y));
                res = (point - pointInSegment).sqrMagnitude;
            }
            else
            {
                res = (point - b).sqrMagnitude;
            }

            return res;
        }

        public BranchPoint[] GetNearestSegmentSS(Vector2 pointSS)
        {
            return GetNearestSegmentSSBelowDistance(pointSS, float.MaxValue);
        }

        public void AddBranch(BranchContainer newBranchContainer)
        {
            newBranchContainer.name = "BranchContainer";
            AssetDatabase.AddObjectToAsset(newBranchContainer, this);
            branches.Add(newBranchContainer);
            RefreshBranchIndexing();
        }
    }
}