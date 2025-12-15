using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
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
        public Vector3 leafCenter;

        public Vector3 lpForward;
        public Vector3 lpUpward;
        public Vector3 GetLeafLeft() => Vector3.Cross(lpForward, lpUpward).normalized;
        
        public int chosenLeave;
        public int initSegmentIdx;
        public int endSegmentIdx;
        public float lpLength;
        public float displacementFromInitSegment;
        public float leafScale;
        
        public List<VertexData> vertices;

        public LeafPoint(LeafPoint other)
        {
            point = other.point;
            lpLength = other.lpLength;
            lpForward = other.lpForward;
            lpUpward = other.lpUpward;
            chosenLeave = other.chosenLeave;
            initSegmentIdx = other.initSegmentIdx;
            endSegmentIdx = other.endSegmentIdx;
            displacementFromInitSegment = other.displacementFromInitSegment;
            leafScale = other.leafScale;
            vertices = new List<VertexData>(other.vertices);
        }
        
        public LeafPoint(int maxNumVertices)
        {
            vertices = new List<VertexData>(maxNumVertices);
        }

        public LeafPoint(Vector3 point, float lpLength, Vector3 lpForward,
            Vector3 lpUpward, int chosenLeave, BranchPoint initSegment,
            BranchPoint endSegment)
        {
            SetValues(point, lpLength, lpForward, lpUpward, chosenLeave, initSegment, endSegment);
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

            var segmentDistance = (initSegment.point - endSegment.point).magnitude;
            var t = (point - initSegment.point).magnitude / segmentDistance;

            displacementFromInitSegment = Mathf.Clamp(t, 0.01f, 0.99f);
        }
        
        public void SetValues(Vector3 point, float lpLength, Vector3 lpForward, Vector3 lpUpward,
            int chosenLeave, BranchPoint initSegment, BranchPoint endSegment, float leafScale,
            IvyParameters ivyParameters)
        {
            this.point = point;
            this.lpForward = lpForward;
            this.lpUpward = lpUpward;
            this.chosenLeave = chosenLeave;
            initSegmentIdx = initSegment.index;
            this.leafScale = leafScale;
        }
        
        public Vector2 GetScreenspacePosition() => HandleUtility.WorldToGUIPoint(point);
        
        public void CreateVertices(IvyParameters ivyParameters, MeshData leafMeshData, Transform rootTransform)
        {
            int numVertices = leafMeshData.vertices.Count;
            vertices = new List<VertexData>(numVertices);

            Quaternion randomLocalRot = ProceduralIvyCommon.CalculateLeafOrientation(ivyParameters, 
                lpForward, lpUpward, null, out _, out _);
            
            Quaternion rootRotInv = Quaternion.Inverse(rootTransform.rotation);
            Quaternion finalRot = rootRotInv * randomLocalRot;

            Vector3 worldOffset = GetLeafLeft() * ivyParameters.leafOffset.x 
                                  + lpUpward * ivyParameters.leafOffset.y 
                                  + lpForward * ivyParameters.leafOffset.z;

            Vector3 relativePos = point + worldOffset - rootTransform.position;
            Vector3 finalPosOffset = rootRotInv * relativePos;

            for (var v = 0; v < numVertices; v++)
            {
                Vector3 vertex = finalRot * leafMeshData.vertices[v] * leafScale + finalPosOffset;
                Vector3 normal = finalRot * leafMeshData.normals[v];
                vertices.Add(new VertexData(vertex, normal, leafMeshData.uv[v], leafMeshData.colors32[v]));
            }
        }
    }
}