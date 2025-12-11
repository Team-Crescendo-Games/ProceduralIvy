using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Dynamite3D.RealIvy
{
	[System.Serializable]
	public class BranchPoint
	{
		public Vector3 originalPoint;
		public Vector3 point;
		public Vector3 grabVector;
		public Vector2 pointSS;
		public float length;
		public Vector3 initialGrowDir;

		public BranchContainer branchContainer;
		public int index;
		//public bool blocked;


		public bool newBranch;
		public int newBranchNumber; //Número identificativo de la rama que comienza en este punto


		public float radius;
		public float currentRadius;

		public Quaternion forwardRotation;


		/*RUNTIME*/
		public List<RTVertexData> verticesLoop;
		public Vector3 firstVector;
		public Vector3 axis;

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

			this.radius = 1f;
			this.currentRadius = 1f;

			this.length = length;

			this.initialGrowDir = Vector3.zero;
			if (index >= 1)
			{
				this.initialGrowDir = (point - branchContainer.branchPoints[index - 1].point).normalized;
			}
		}

		public BranchPoint()
		{}

		public void InitializeRuntime(IvyParameters ivyParameters)
		{
			this.verticesLoop = new List<RTVertexData>(ivyParameters.sides + 1);
		}

		public BranchPoint(Vector3 point, Vector3 grabVector, int index, bool newBranch, int newBranchNumber, float length, BranchContainer branchContainer)
		{
			SetValues(point, grabVector, Vector3.zero, branchContainer, index, false, newBranch, newBranchNumber, length);
		}

		public BranchPoint(Vector3 point, Vector3 grabVector, int index, float length, BranchContainer branchContainer)
		{
			SetValues(point, grabVector, Vector3.zero, branchContainer, index, false, false, -1, length);
		}

		public BranchPoint(Vector3 point, int index, float length, BranchContainer branchContainer)
		{
			SetValues(point, Vector3.zero, Vector3.zero, branchContainer, index, false, false, -1, length);
		}

		/*public void CalculateVerticesLoop(IvyParameters ivyParameters, RTIvyContainerOLD rtIvyContainer, GameObject ivyGO)
		{
			if (verticesLoop == null)
			{
				verticesLoop = new List<RTVertexData>(ivyParameters.sides + 1);
			}

			float value = (Mathf.Sin(this.length * ivyParameters.radiusVarFreq + ivyParameters.radiusVarOffset) + 1f) / 2f;
			float radius = Mathf.Lerp(ivyParameters.minRadius, ivyParameters.maxRadius, value);
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


			Vector3 axis = GetLoopAxis(index, newBranchNumber, rtIvyContainer, ivyGO);
			Vector3 firstVector = GetFirstVector(index, newBranchNumber, rtIvyContainer, ivyParameters, axis);


			for(int i = 0; i < ivyParameters.sides + 1; i++)
			{
				Quaternion quat = Quaternion.AngleAxis(angle * i, axis);
				Vector3 direction = quat * firstVector;

				if (ivyParameters.halfgeom && ivyParameters.sides == 1)
				{
					normal = -grabVector;
				}
				else
				{
					normal = direction;
				}

				normal = Quaternion.Inverse(ivyGO.transform.rotation) * normal;

				vertex = direction * radius + point;
				vertex -= ivyGO.transform.position;
				vertex = Quaternion.Inverse(ivyGO.transform.rotation) * vertex;

				uv = new Vector2(length * ivyParameters.uvScale.y + ivyParameters.uvOffset.y - ivyParameters.stepSize, 
					1f / ivyParameters.sides * i * ivyParameters.uvScale.x + ivyParameters.uvOffset.x);

				RTVertexData vertexData = new RTVertexData(vertex, normal, uv, Vector2.zero, Color.black);
				verticesLoop.Add(vertexData);
			}
		}*/

		/*public Vector3 GetFirstVector(int p, int b, RTIvyContainerOLD rtIvyContainer, IvyParameters ivyParameters, Vector3 axis)
		{
			Vector3 firstVector = Vector3.zero;

			if (b == 0 && p == 0)
			{
				if (!ivyParameters.halfgeom)
				{
					firstVector = rtIvyContainer.firstVertexVector;
				}
				else
				{
					firstVector = Quaternion.AngleAxis(90f, axis) * rtIvyContainer.firstVertexVector;
				}
			}
			else
			{
				if (!ivyParameters.halfgeom)
				{
					firstVector = Vector3.Normalize(Vector3.ProjectOnPlane(grabVector, axis));
				}
				else
				{
					firstVector = Quaternion.AngleAxis(90f, axis) * Vector3.Normalize(Vector3.ProjectOnPlane(grabVector, axis));
				}
			}

			return firstVector;
		}

		public Vector3 GetLoopAxis(int p, int b, RTIvyContainerOLD rtIvyContainer, GameObject ivyGo)
		{
			Vector3 axis = Vector3.zero;

			if (p == 0 && b == 0)
			{
				axis = ivyGo.transform.up;
			}
			else
			{
				if (p == 0 || branchContainer.branchPoints.Count < 2)
				{
					axis = GetNextPoint().point - point;
				}
				else
				{
					axis = Vector3.Normalize(Vector3.Lerp(point - GetPreviousPoint().point, 
						GetNextPoint().point - point, 0.5f));
				}
			}

			return axis;
		}*/

#if UNITY_EDITOR
		public void CalculatePointSS()
		{
			this.pointSS = HandleUtility.WorldToGUIPoint(point);
		}
#endif

		public void SetOriginalPoint()
		{
			this.originalPoint = point;
		}

		public BranchPoint GetNextPoint()
		{
			BranchPoint res = null;
			if (index < branchContainer.branchPoints.Count - 1)
			{
				res = branchContainer.branchPoints[index + 1];
			}

			return res;
		}

		public BranchPoint GetPreviousPoint()
		{
			BranchPoint res = null;
			if (index > 0)
			{
				res = branchContainer.branchPoints[index - 1];
			}

			return res;
		}

		public void Move(Vector3 newPosition)
		{
			this.point = newPosition;
		}

		public void InitBranchInThisPoint(int branchNumber)
		{
			this.newBranch = true;
			this.newBranchNumber = branchNumber;
		}

		public void ReleasePoint()
		{
			this.newBranch = false;
			this.newBranchNumber = -1;
		}
	}
}