using UnityEngine;

namespace TeamCrescendo.ProceduralIvy
{
    public class RTBranchPoint
    {
        public Vector3 axis;

        public RTBranchContainer branchContainer;
        public Vector3 centerLoop;
        public Vector3 firstVector;
        public Vector3 grabVector;
        public int index;
        public Vector3 lastVectorNormal;
        public float length;
        public bool newBranch;
        public int newBranchNumber;
        public Vector3 point;

        public float radius;


        public RTVertexData[] verticesLoop;


        public RTBranchPoint()
        {
        }

        public RTBranchPoint(BranchPoint branchPoint, RTBranchContainer rtBranchContainer)
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

        public void PreInit(IvyParameters ivyParameters)
        {
            verticesLoop = new RTVertexData[ivyParameters.sides + 1];
        }


        public void SetValues(Vector3 point, Vector3 grabVector)
        {
            SetValues(point, grabVector, false, -1);
        }

        public void SetValues(Vector3 point, Vector3 grabVector, bool newBranch, int newBranchNumber)
        {
            this.point = point;
            this.grabVector = grabVector;
            this.newBranch = newBranch;
            this.newBranchNumber = newBranchNumber;
        }

        public void InitBranchInThisPoint(int branchNumber)
        {
            newBranch = true;
            newBranchNumber = branchNumber;
        }

        public void CalculateVerticesLoop(IvyParameters ivyParameters, RTIvyContainer rtIvyContainer, GameObject ivyGO,
            Vector3 firstVector, Vector3 axis, float radius)
        {
            this.firstVector = firstVector;
            this.axis = axis;
            this.radius = radius;

            CalculateVerticesLoop(ivyParameters, rtIvyContainer, ivyGO);
        }


        public void CalculateVerticesLoop(IvyParameters ivyParameters, RTIvyContainer rtIvyContainer, GameObject ivyGO)
        {
            var angle = 0f;
            if (!ivyParameters.halfgeom)
                angle = Mathf.Rad2Deg * 2 * Mathf.PI / ivyParameters.sides;
            else
                angle = Mathf.Rad2Deg * 2 * Mathf.PI / ivyParameters.sides / 2;


            var vertex = Vector3.zero;
            var normal = Vector3.zero;
            var uv = Vector2.zero;
            var quat = Quaternion.identity;
            var direction = Vector3.zero;


            var inverseIvyGORotation = Quaternion.Inverse(ivyGO.transform.rotation);


            for (var i = 0; i < ivyParameters.sides + 1; i++)
            {
                quat = Quaternion.AngleAxis(angle * i, axis);
                direction = quat * firstVector;

                if (ivyParameters.halfgeom && ivyParameters.sides == 1)
                    normal = -grabVector;
                else
                    normal = direction;

                normal = inverseIvyGORotation * normal;


                vertex = direction * radius + point;
                vertex -= ivyGO.transform.position;
                vertex = inverseIvyGORotation * vertex;

                uv = new Vector2(length * ivyParameters.uvScale.y + ivyParameters.uvOffset.y - ivyParameters.stepSize,
                    1f / ivyParameters.sides * i * ivyParameters.uvScale.x + ivyParameters.uvOffset.x);


                verticesLoop[i] = new RTVertexData(vertex, normal, uv, Vector2.zero, Color.black);
            }
        }

        public void CalculateCenterLoop(GameObject ivyGO)
        {
            centerLoop = Quaternion.Inverse(ivyGO.transform.rotation) * (point - ivyGO.transform.position);


            lastVectorNormal = ivyGO.transform.InverseTransformVector(grabVector);
        }

        public RTBranchPoint GetNextPoint()
        {
            var res = branchContainer.branchPoints[index + 1];
            return res;
        }

        public RTBranchPoint GetPreviousPoint()
        {
            var res = branchContainer.branchPoints[index - 1];
            return res;
        }
    }
}