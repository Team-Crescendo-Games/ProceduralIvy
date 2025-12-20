using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
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
        public Vector3 initialGrowDir;
        public Vector3 firstVector;
        public Vector3 axis;

        public int index;
        public int newBranchNumber;
        public float length;
        public float radius;
        public bool newBranch;

        public BranchContainer branchContainer;
        [NonSerialized] public List<VertexData> verticesLoop = new();

        public BranchPoint(Vector3 point, Vector3 grabVector, int index, bool newBranch, int newBranchNumber, float length, BranchContainer branchContainer)
        {
            this.point = point;
            this.grabVector = grabVector;
            this.branchContainer = branchContainer;
            this.index = index;
            this.newBranch = newBranch;
            this.newBranchNumber = newBranchNumber;

            radius = 1f;

            this.length = length;

            if (index >= 1 && branchContainer != null && branchContainer.branchPoints.Count > index - 1)
            {
                var prevPoint = branchContainer.branchPoints[index - 1].point;
                initialGrowDir = (point - prevPoint).normalized;
            }
            else
            {
                initialGrowDir = Vector3.zero;
            }
        }

        public BranchPoint(Vector3 point, Vector3 grabVector, int index, float length, BranchContainer branchContainer)
        : this(point, grabVector, index, false, -1, length, branchContainer) { }
        
        public BranchPoint(BranchPoint branchPoint, BranchContainer rtBranchContainer)
         {
             point = branchPoint.point;
             grabVector = branchPoint.grabVector;
             length = branchPoint.length;
             index = branchPoint.index;
             newBranch = branchPoint.newBranch;
             newBranchNumber = branchPoint.newBranchNumber;

             branchContainer = rtBranchContainer;

             radius = branchPoint.radius;
             firstVector = branchPoint.firstVector;
             axis = branchPoint.axis;
         }

#if UNITY_EDITOR
        public Vector2 GetScreenspacePosition() => HandleUtility.WorldToGUIPoint(point);
#endif
        
        public BranchPoint GetNextPoint() =>
            index < branchContainer.branchPoints.Count - 1 ? branchContainer.branchPoints[index + 1] : null;

        public BranchPoint GetPreviousPoint() => index > 0 ? branchContainer.branchPoints[index - 1] : null;

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

        public void CalculateVerticesLoop(IvyParameters ivyParameters, IvyContainer rtIvyContainer, GameObject ivyGO,
            Vector3 firstVector, Vector3 axis, float radius)
        {
            this.firstVector = firstVector;
            this.axis = axis;
            this.radius = radius;

            CalculateVerticesLoop(ivyParameters, rtIvyContainer, ivyGO);
        }

        public void CalculateVerticesLoop(IvyParameters ivyParameters, IvyContainer rtIvyContainer, GameObject ivyGO)
        {
            Assert.IsNotNull(verticesLoop);
            verticesLoop.Clear();
            
            float totalAngle = ivyParameters.halfgeom ? 180f : 360f;
            var angle = totalAngle / ivyParameters.sides;

            var rootRotationInv = Quaternion.Inverse(ivyGO.transform.rotation);

            for (var i = 0; i < ivyParameters.sides + 1; i++)
            {
                var quat = Quaternion.AngleAxis(angle * i, axis);
                var direction = quat * firstVector;

                Vector3 normal;
                if (ivyParameters.halfgeom && ivyParameters.sides == 1)
                    normal = -grabVector;
                else
                    normal = direction;

                normal = rootRotationInv * normal;

                var vertex = direction * radius + point;
                vertex -= ivyGO.transform.position;
                vertex = rootRotationInv * vertex;

                var uv = new Vector2(
                    length * ivyParameters.uvScale.y + ivyParameters.uvOffset.y - ivyParameters.stepSize,
                    1f / ivyParameters.sides * i * ivyParameters.uvScale.x + ivyParameters.uvOffset.x);

                verticesLoop.Add(new VertexData(vertex, normal, uv, Color.black));
            }
        }
    }
}