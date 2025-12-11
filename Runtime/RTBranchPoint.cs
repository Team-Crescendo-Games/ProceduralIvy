using UnityEngine;

namespace Dynamite3D.RealIvy
{
	public class RTBranchPoint
	{
		public Vector3 point;
		public Vector3 grabVector;
		public float length;
		public int index;
		public bool newBranch;
		public int newBranchNumber;

		public float radius;
		public Vector3 firstVector;
		public Vector3 axis;
		public Vector3 centerLoop;

		public RTBranchContainer branchContainer;


		public RTVertexData[] verticesLoop;
		public Vector3 lastVectorNormal;




		public RTBranchPoint() { }

		public RTBranchPoint(BranchPoint branchPoint, RTBranchContainer rtBranchContainer)
		{
			this.point = branchPoint.point;
			this.grabVector = branchPoint.grabVector;
			this.length = branchPoint.length;
			this.index = branchPoint.index;
			this.newBranch = branchPoint.newBranch;
			this.newBranchNumber = branchPoint.newBranchNumber;

			this.branchContainer = rtBranchContainer;

			this.radius = branchPoint.radius;
			this.firstVector = branchPoint.firstVector;
			this.axis = branchPoint.axis;
		}

		public void PreInit(IvyParameters ivyParameters)
		{
			this.verticesLoop = new RTVertexData[ivyParameters.sides + 1];
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
			this.newBranch = true;
			this.newBranchNumber = branchNumber;
		}

		public void CalculateVerticesLoop(IvyParameters ivyParameters, RTIvyContainer rtIvyContainer, GameObject ivyGO, Vector3 firstVector, Vector3 axis, float radius)
		{
			this.firstVector = firstVector;
			this.axis = axis;
			this.radius = radius;

			CalculateVerticesLoop(ivyParameters, rtIvyContainer, ivyGO);
		}



		public void CalculateVerticesLoop(IvyParameters ivyParameters, RTIvyContainer rtIvyContainer, GameObject ivyGO)
		{
			float angle = 0f;
			if (!ivyParameters.halfgeom)
			{
				angle = Mathf.Rad2Deg * 2 * Mathf.PI / ivyParameters.sides;
			}
			else
			{
				angle = Mathf.Rad2Deg * 2 * Mathf.PI / ivyParameters.sides / 2;
			}



			Vector3 vertex = Vector3.zero;
			Vector3 normal = Vector3.zero;
			Vector2 uv = Vector2.zero;
			Quaternion quat = Quaternion.identity;
			Vector3 direction = Vector3.zero;


			Quaternion inverseIvyGORotation = Quaternion.Inverse(ivyGO.transform.rotation);



			for (int i = 0; i < ivyParameters.sides + 1; i++)
			{
				quat = Quaternion.AngleAxis(angle * i, axis);
				direction = quat * firstVector;

				if (ivyParameters.halfgeom && ivyParameters.sides == 1)
				{
					normal = -grabVector;
				}
				else
				{
					normal = direction;
				}

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
			this.centerLoop = Quaternion.Inverse(ivyGO.transform.rotation) * (point - ivyGO.transform.position);



			this.lastVectorNormal = ivyGO.transform.InverseTransformVector(grabVector);
		}

		public RTBranchPoint GetNextPoint()
		{
			RTBranchPoint res = branchContainer.branchPoints[this.index + 1];
			return res;
		}

		public RTBranchPoint GetPreviousPoint()
		{
			RTBranchPoint res = branchContainer.branchPoints[this.index - 1];
			return res;
		}
	}
}