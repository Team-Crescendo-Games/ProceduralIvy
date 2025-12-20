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
        public List<BranchContainer> branches = new();
        public Vector3 firstVertexVector;
        
        public static IvyContainer Create(Vector3 firstVertexVector)
        {
            IvyContainer container = CreateInstance<IvyContainer>();
            container.firstVertexVector = firstVertexVector;
            return container;
        }
        
        public static IvyContainer Create(IvyContainer ivyContainer, IvyParameters ivyParameters, GameObject ivyGO,
             MeshData[] leavesMeshesByChosenLeaf, Vector3 firstVertexVector)
         {
             IvyContainer container = CreateInstance<IvyContainer>();
             
             container.branches = new List<BranchContainer>(ivyContainer.branches.Count);

             for (var i = 0; i < ivyContainer.branches.Count; i++)
             {
                 var rtBranch = CreateInstance<BranchContainer>();
                 rtBranch.InitRuntime(ivyContainer.branches[i], ivyParameters, container, ivyGO, leavesMeshesByChosenLeaf);
                 container.branches.Add(rtBranch);
             }
             
             container.firstVertexVector = firstVertexVector;
             return container;
         }

        public void Clear()
        {
            foreach (var branch in branches)
                DeleteBranch(branch);

            branches.Clear();
        }
        
        private void DeleteBranch(BranchContainer branch)
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
            DeleteBranch(branchToDelete);
            RefreshBranchIndexing();
        }

        public BranchContainer GetBranchContainerByBranchNumber(int branchNumber) => branches.Find(b => b.branchNumber == branchNumber);

#if UNITY_EDITOR
        public BranchPoint[] GetNearestSegmentSSBelowDistance(Vector2 pointSS, float distanceThreshold)
        {
            BranchPoint initSegment = null;
            BranchPoint endSegment = null;

            var minSqrDistance = distanceThreshold * distanceThreshold;

            foreach (var branch in branches)
            {
                var points = branch.branchPoints;
                for (var j = 1; j < points.Count; j++)
                {
                    var a = points[j - 1];
                    var b = points[j];

                    var dSqr = SqrDistanceToSegment(pointSS, a.GetScreenspacePosition(), b.GetScreenspacePosition());

                    if (dSqr < minSqrDistance)
                    {
                        minSqrDistance = dSqr;
                        initSegment = a;
                        endSegment = b;
                    }
                }
            }

            if (initSegment == null)
                return null;

            return new [] { initSegment, endSegment };
        }
        
        public static float SqrDistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            var ap = point - a;
            var lengthSqr = ab.sqrMagnitude;

            if (lengthSqr == 0)
                return ap.sqrMagnitude;

            // Project point onto the line segment, clamped between 0 and 1
            var t = Mathf.Clamp01(Vector2.Dot(ap, ab) / lengthSqr);
            var projection = a + t * ab;

            return (point - projection).sqrMagnitude;
        }

        public BranchPoint[] GetNearestSegmentSS(Vector2 pointSS) => GetNearestSegmentSSBelowDistance(pointSS, float.MaxValue);

        public void AddBranchEditor(BranchContainer newBranchContainer)
        {
            newBranchContainer.name = "BranchContainer";
            AssetDatabase.AddObjectToAsset(newBranchContainer, this);
            branches.Add(newBranchContainer);
            RefreshBranchIndexing();
        }
#endif
        
        // runtime variant of AddBranch that doesn't add to asset database
        public void AddBranchRuntime(BranchContainer newBranchContainer)
        {
            branches.Add(newBranchContainer);
            RefreshBranchIndexing();
        }
    }
}