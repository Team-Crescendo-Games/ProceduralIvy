using UnityEngine;

namespace TeamCrescendo.ProceduralIvy
{
    public class RTLeafPoint
    {
        public int chosenLeave;

        public int initSegmentIdx;
        public Vector3 leafCenter;
        public Quaternion leafRotation;
        public float leafScale;

        public Vector3 left;
        public Vector3 lpForward;
        public Vector3 lpUpward;
        public Vector3 point;

        public RTVertexData[] vertices;

        public RTLeafPoint()
        {
        }

        public RTLeafPoint(LeafPoint leafPoint, IvyParameters ivyParameters)
        {
            point = leafPoint.point;
            left = leafPoint.left;
            lpForward = leafPoint.lpForward;
            lpUpward = leafPoint.lpUpward;

            initSegmentIdx = leafPoint.initSegmentIdx;
            chosenLeave = leafPoint.chosenLeave;

            vertices = leafPoint.verticesLeaves.ToArray();
            leafCenter = leafPoint.leafCenter;
            leafRotation = leafPoint.leafRotation;
            leafScale = leafPoint.leafScale;

            CalculateLeafRotation(ivyParameters);
        }

        public void PreInit(int numVertices)
        {
            vertices = new RTVertexData[numVertices];
        }

        public void SetValues(Vector3 point, float lpLength, Vector3 lpForward, Vector3 lpUpward,
            int chosenLeave, RTBranchPoint initSegment, RTBranchPoint endSegment, float leafScale,
            IvyParameters ivyParameters)
        {
            this.point = point;
            this.lpForward = lpForward;
            this.lpUpward = lpUpward;
            this.chosenLeave = chosenLeave;
            initSegmentIdx = initSegment.index;
            this.leafScale = leafScale;

            left = Vector3.Cross(lpForward, lpUpward).normalized;

            CalculateLeafRotation(ivyParameters);
        }

        private void CalculateLeafRotation(IvyParameters ivyParameters)
        {
            Vector3 left, forward;

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

            leafRotation = Quaternion.LookRotation(lpUpward, forward);

            leafRotation =
                Quaternion.AngleAxis(ivyParameters.rotation.x, left) *
                Quaternion.AngleAxis(ivyParameters.rotation.y, lpUpward) *
                Quaternion.AngleAxis(ivyParameters.rotation.z, forward) * leafRotation;

            leafRotation =
                Quaternion.AngleAxis(Random.Range(-ivyParameters.randomRotation.x, ivyParameters.randomRotation.x),
                    left) *
                Quaternion.AngleAxis(Random.Range(-ivyParameters.randomRotation.y, ivyParameters.randomRotation.y),
                    lpUpward) *
                Quaternion.AngleAxis(Random.Range(-ivyParameters.randomRotation.z, ivyParameters.randomRotation.z),
                    forward) *
                leafRotation;
        }

        public void CreateVertices(IvyParameters ivyParameters, RTMeshData leafMeshData, GameObject ivyGO)
        {
            var ivyGOInverseRotation = Quaternion.Inverse(ivyGO.transform.rotation);
            leafCenter = ivyGO.transform.InverseTransformPoint(point);

            for (var v = 0; v < leafMeshData.vertices.Length; v++)
            {
                var offset = left * ivyParameters.offset.x +
                             lpUpward * ivyParameters.offset.y +
                             lpForward * ivyParameters.offset.z;

                var vertex = leafRotation * leafMeshData.vertices[v] * leafScale + point + offset;
                vertex = vertex - ivyGO.transform.position;
                vertex = ivyGOInverseRotation * vertex;

                var normal = leafRotation * leafMeshData.normals[v];
                normal = ivyGOInverseRotation * normal;

                var uv = leafMeshData.uv[v];
                var vertexColor = leafMeshData.colors[v];

                vertices[v] = new RTVertexData(vertex, normal, uv, Vector2.zero, vertexColor);
            }
        }
    }
}