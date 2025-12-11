using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TeamCrescendo.ProceduralIvy
{
    [Serializable]
    public class LeafPoint
    {
        public Vector3 point;
        public Vector2 pointSS;
        public float lpLength;

        public Vector3 left;
        public Vector3 lpForward;
        public Vector3 lpUpward;
        public int chosenLeave;

        public Quaternion forwarRot;

        public int initSegmentIdx;
        public int endSegmentIdx;
        public float displacementFromInitSegment;

        public Quaternion leafRotation;
        public float currentScale;
        public float dstScale;


        /* RUNTIME */
        public Vector3 leafCenter;
        public List<RTVertexData> verticesLeaves;
        public float leafScale;

        public LeafPoint()
        {
        }

        public LeafPoint(Vector3 point, float lpLength, Vector3 lpForward,
            Vector3 lpUpward, int chosenLeave, BranchPoint initSegment,
            BranchPoint endSegment)
        {
            SetValues(point, lpLength, lpForward, lpUpward, chosenLeave, initSegment, endSegment);
        }

        public void InitializeRuntime()
        {
            verticesLeaves = new List<RTVertexData>(4);
        }

        public void SetValues(Vector3 point, float lpLength, Vector3 lpForward, Vector3 lpUpward,
            int chosenLeave, BranchPoint initSegment, BranchPoint endSegment)
        {
            this.point = point;
            this.lpLength = lpLength;
            this.lpForward = lpForward;
            this.lpUpward = lpUpward;
            this.chosenLeave = chosenLeave;
            initSegmentIdx = initSegment.index;
            endSegmentIdx = endSegment.index;
            forwarRot = Quaternion.identity;

            var segmentDistance = (initSegment.point - endSegment.point).magnitude;
            var t = (point - initSegment.point).magnitude / segmentDistance;

            displacementFromInitSegment = Mathf.Clamp(t, 0.01f, 0.99f);
            left = Vector3.Cross(lpForward, lpUpward).normalized;

            //this.verticesLeaves = new List<RTVertexData>();
        }
#if UNITY_EDITOR
        public void CalculatePointSS()
        {
            pointSS = HandleUtility.WorldToGUIPoint(point);
        }
#endif

        public void DrawVectors()
        {
            Debug.DrawLine(point, point + lpForward * 0.25f, Color.red, 5f);
            Debug.DrawLine(point, point + lpUpward * 0.25f, Color.blue, 5f);
            Debug.DrawLine(point, point + left * 0.25f, Color.green, 5f);
        }

        public float GetLengthFactor(BranchContainer branchContainer, float correctionFactor)
        {
            var res = lpLength <= branchContainer.totalLenght * 1.15f * correctionFactor ? 1f : 0f;
            return res;
        }

        public void CreateVertices(IvyParameters ivyParameters, RTMeshData leafMeshData, GameObject ivyGO)
        {
            Vector3 left, forward;
            Quaternion quat;


            if (!ivyParameters.globalOrientation)
            {
                forward = lpForward;
                left = this.left;
            }
            else
            {
                forward = ivyParameters.globalRotation;
                left = Vector3.Normalize(Vector3.Cross(ivyParameters.globalRotation, lpUpward));
            }

            quat = Quaternion.LookRotation(lpUpward, forward);

            quat = Quaternion.AngleAxis(ivyParameters.rotation.x, left) *
                   Quaternion.AngleAxis(ivyParameters.rotation.y, lpUpward) *
                   Quaternion.AngleAxis(ivyParameters.rotation.z, forward) *
                   quat;


            quat = Quaternion.AngleAxis(Random.Range(-ivyParameters.randomRotation.x, ivyParameters.randomRotation.x),
                       left) *
                   Quaternion.AngleAxis(Random.Range(-ivyParameters.randomRotation.y, ivyParameters.randomRotation.y),
                       lpUpward) *
                   Quaternion.AngleAxis(Random.Range(-ivyParameters.randomRotation.z, ivyParameters.randomRotation.z),
                       forward) *
                   quat;

            quat = forwarRot * quat;


            var scale = Random.Range(ivyParameters.minScale, ivyParameters.maxScale);

            leafRotation = quat;

            leafCenter = point - ivyGO.transform.position;
            leafCenter = Quaternion.Inverse(ivyGO.transform.rotation) * leafCenter;

            verticesLeaves ??= new List<RTVertexData>(4);

            var ivyGOInverseRotation = Quaternion.Inverse(ivyGO.transform.rotation);

            for (var v = 0; v < leafMeshData.vertices.Length; v++)
            {
                var offset = left * ivyParameters.offset.x + lpUpward * ivyParameters.offset.y +
                             lpForward * ivyParameters.offset.z;

                var vertex = quat * leafMeshData.vertices[v] * scale + leafCenter + offset;

                var normal = quat * leafMeshData.normals[v];
                normal = ivyGOInverseRotation * normal;

                var vertexData = new RTVertexData(vertex, normal, leafMeshData.uv[v], Vector2.zero, leafMeshData.colors[v]);
                verticesLeaves.Add(vertexData);
            }
        }
    }
}