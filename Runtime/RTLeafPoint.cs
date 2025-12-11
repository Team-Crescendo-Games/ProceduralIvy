using Dynamite3D.RealIvy;
using System.Collections.Generic;
using UnityEngine;

namespace Dynamite3D.RealIvy
{
    public class RTLeafPoint
    {
        public Vector3 point;
        public float lpLength;

        public Vector3 left;
        public Vector3 lpForward;
        public Vector3 lpUpward;

        public int initSegmentIdx;
        public int endSegmentIdx;
        public int chosenLeave;

        public RTVertexData[] vertices;
        public Vector3 leafCenter;
        public Quaternion leafRotation;
        public float leafScale;


        public void InitializeRuntime()
        { }

        public RTLeafPoint()
        { }

        public RTLeafPoint(LeafPoint leafPoint, IvyParameters ivyParameters)
        {
            this.point = leafPoint.point;
            this.lpLength = leafPoint.lpLength;
            this.left = leafPoint.left;
            this.lpForward = leafPoint.lpForward;
            this.lpUpward = leafPoint.lpUpward;

            this.initSegmentIdx = leafPoint.initSegmentIdx;
            this.endSegmentIdx = leafPoint.endSegmentIdx;
            this.chosenLeave = leafPoint.chosenLeave;

            this.vertices = leafPoint.verticesLeaves.ToArray();
            this.leafCenter = leafPoint.leafCenter;
            this.leafRotation = leafPoint.leafRotation;
            this.leafScale = leafPoint.leafScale;

            CalculateLeafRotation(ivyParameters);
        }

        public void PreInit(int numVertices)
        {
            this.vertices = new RTVertexData[numVertices];
        }

        public void PreInit(RTMeshData leafMeshData)
        {
            this.vertices = new RTVertexData[leafMeshData.vertices.Length];
        }

        public void SetValues(Vector3 point, float lpLength, Vector3 lpForward, Vector3 lpUpward,
        int chosenLeave, RTBranchPoint initSegment, RTBranchPoint endSegment, float leafScale, IvyParameters ivyParameters)
        {
            this.point = point;
            this.lpLength = lpLength;
            this.lpForward = lpForward;
            this.lpUpward = lpUpward;
            this.chosenLeave = chosenLeave;
            this.initSegmentIdx = initSegment.index;
            this.endSegmentIdx = endSegment.index;
            this.leafScale = leafScale;

            this.left = Vector3.Cross(lpForward, lpUpward).normalized;

            CalculateLeafRotation(ivyParameters);
        }

        private void CalculateLeafRotation(IvyParameters ivyParameters)
        {
            Vector3 left, forward;

            if (!ivyParameters.globalOrientation)
            {
                forward = this.lpForward;
                left = this.left;
            }
            else
            {
                forward = ivyParameters.globalRotation;
                left = Vector3.Normalize(Vector3.Cross(ivyParameters.globalRotation, this.lpUpward));
            }

            leafRotation = Quaternion.LookRotation(this.lpUpward, forward);

            leafRotation =
                Quaternion.AngleAxis(ivyParameters.rotation.x, left) * Quaternion.AngleAxis(ivyParameters.rotation.y, lpUpward) *
                Quaternion.AngleAxis(ivyParameters.rotation.z, forward) * leafRotation;

            leafRotation =
                Quaternion.AngleAxis(Random.Range(-ivyParameters.randomRotation.x, ivyParameters.randomRotation.x), left) *
                Quaternion.AngleAxis(Random.Range(-ivyParameters.randomRotation.y, ivyParameters.randomRotation.y), lpUpward) *
                Quaternion.AngleAxis(Random.Range(-ivyParameters.randomRotation.z, ivyParameters.randomRotation.z), forward) *
                leafRotation;

        }

        public void CreateVertices(IvyParameters ivyParameters, RTMeshData leafMeshData, GameObject ivyGO)
        {
            Vector3 vertex = Vector3.zero;
            Vector3 normal = Vector3.zero;
            Vector2 uv = Vector2.zero;
            Color vertexColor = Color.black;
            Quaternion ivyGOInverseRotation = Quaternion.Inverse(ivyGO.transform.rotation);
            leafCenter = ivyGO.transform.InverseTransformPoint(point);


            for (int v = 0; v < leafMeshData.vertices.Length; v++)
            {
                Vector3 offset = left * ivyParameters.offset.x +
                    this.lpUpward * ivyParameters.offset.y +
                    this.lpForward * ivyParameters.offset.z;

                vertex = leafRotation * leafMeshData.vertices[v] * leafScale + point + offset;
                vertex = vertex - ivyGO.transform.position;
                vertex = ivyGOInverseRotation * vertex;

                normal = leafRotation * leafMeshData.normals[v];
                normal = ivyGOInverseRotation * normal;

                uv = leafMeshData.uv[v];
                vertexColor = leafMeshData.colors[v];

                this.vertices[v] = new RTVertexData(vertex, normal, uv, Vector2.zero, vertexColor);
            }
        }
    }
}