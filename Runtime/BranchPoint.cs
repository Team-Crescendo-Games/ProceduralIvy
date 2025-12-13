using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TeamCrescendo.ProceduralIvy
{
    [Serializable]
    public class BranchPoint
    {
        public Vector3 point;
        public Vector3 grabVector;
        public Vector2 pointSS;
        public float length;
        public Vector3 initialGrowDir;

        public BranchContainer branchContainer;
        public int index;

        public bool newBranch;
        public int newBranchNumber;

        public float radius;

        public List<RTVertexData> verticesLoop;
        public Vector3 firstVector;
        public Vector3 axis;

        public BranchPoint(Vector3 point, Vector3 grabVector, int index, bool newBranch, int newBranchNumber,
            float length, BranchContainer branchContainer)
        {
            SetValues(point, grabVector, Vector3.zero, branchContainer, index, false, newBranch, newBranchNumber,
                length);
        }

        public BranchPoint(Vector3 point, Vector3 grabVector, int index, float length, BranchContainer branchContainer)
        {
            SetValues(point, grabVector, Vector3.zero, branchContainer, index, false, false, -1, length);
        }

        public BranchPoint(Vector3 point, int index, float length, BranchContainer branchContainer)
        {
            SetValues(point, Vector3.zero, Vector3.zero, branchContainer, index, false, false, -1, length);
        }

        public void SetValues(Vector3 point, Vector3 grabVector, Vector2 pointSS,
            BranchContainer branchContainer, int index, bool blocked, bool newBranch,
            int newBranchNumber, float length)
        {
            this.point = point;
            this.grabVector = grabVector;
            this.pointSS = pointSS;
            this.branchContainer = branchContainer;
            this.index = index;
            this.newBranch = newBranch;
            this.newBranchNumber = newBranchNumber;

            radius = 1f;

            this.length = length;

            initialGrowDir = Vector3.zero;
            if (index >= 1) initialGrowDir = (point - branchContainer.branchPoints[index - 1].point).normalized;
        }

        public void InitializeRuntime(IvyParameters ivyParameters)
        {
            verticesLoop = new List<RTVertexData>(ivyParameters.sides + 1);
        }

#if UNITY_EDITOR
        public void CalculatePointSS()
        {
            pointSS = HandleUtility.WorldToGUIPoint(point);
        }
#endif

        public BranchPoint GetNextPoint() =>
            index < branchContainer.branchPoints.Count - 1 ? branchContainer.branchPoints[index + 1] : null;

        public BranchPoint GetPreviousPoint() =>
            index > 0 ? branchContainer.branchPoints[index - 1] : null;

        public void Move(Vector3 newPosition)
        {
            point = newPosition;
        }

        public void InitBranchInThisPoint(int branchNumber)
        {
            newBranch = true;
            newBranchNumber = branchNumber;
        }

        public void ReleasePoint()
        {
            newBranch = false;
            newBranchNumber = -1;
        }
    }
}