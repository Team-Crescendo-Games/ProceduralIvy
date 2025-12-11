#if UNITY_EDITOR
using UnityEditor;
#endif

using System.Collections.Generic;
using UnityEngine;

namespace Dynamite3D.RealIvy
{
	[System.Serializable]
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

		public void InitializeRuntime()
		{
			this.verticesLeaves = new List<RTVertexData>(4);
		}

		public LeafPoint()
		{}

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
			this.initSegmentIdx = initSegment.index;
			this.endSegmentIdx = endSegment.index;
			this.forwarRot = Quaternion.identity;

			float segmentDistance = (initSegment.point - endSegment.point).magnitude;
			float t = (point - initSegment.point).magnitude / segmentDistance;

			this.displacementFromInitSegment = Mathf.Clamp(t, 0.01f, 0.99f);
			this.left = Vector3.Cross(lpForward, lpUpward).normalized;

			//this.verticesLeaves = new List<RTVertexData>();
		}
#if UNITY_EDITOR
		public void CalculatePointSS()
		{
			this.pointSS = HandleUtility.WorldToGUIPoint(point);
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
			float res = lpLength <= (branchContainer.totalLenght * 1.15f * correctionFactor) ? 1f : 0f;
			return res;
		}

		public void CreateVertices(IvyParameters ivyParameters, RTMeshData leafMeshData, GameObject ivyGO)
		{
			Vector3 left, forward;
			Quaternion quat;


			//Aquí cálculos de orientación en función de las opciones de rotación
			if (!ivyParameters.globalOrientation)
			{
				forward = lpForward;
				left = this.left;
				//left = Vector3.Cross(currentLeaf.lpForward, currentLeaf.lpUpward);
			}
			else
			{
				forward = ivyParameters.globalRotation;
				left = Vector3.Normalize(Vector3.Cross(ivyParameters.globalRotation, this.lpUpward));
			}
			//Y aplicamos la rotación

			quat = Quaternion.LookRotation(this.lpUpward, forward);

			quat = Quaternion.AngleAxis(ivyParameters.rotation.x, left) * 
				Quaternion.AngleAxis(ivyParameters.rotation.y, this.lpUpward) * 
				Quaternion.AngleAxis(ivyParameters.rotation.z, forward) * 
				quat;


			quat = Quaternion.AngleAxis(Random.Range(-ivyParameters.randomRotation.x, ivyParameters.randomRotation.x), left) * 
				Quaternion.AngleAxis(Random.Range(-ivyParameters.randomRotation.y, ivyParameters.randomRotation.y), this.lpUpward) * 
				Quaternion.AngleAxis(Random.Range(-ivyParameters.randomRotation.z, ivyParameters.randomRotation.z), forward) * 
				quat;

			quat = this.forwarRot * quat;



			//Aquí la escala, que es facilita, incluyendo el tip influence
			float scale = Random.Range(ivyParameters.minScale, ivyParameters.maxScale);
			//scale *= Mathf.InverseLerp(infoPool.ivyContainer.branches[b].totalLenght, infoPool.ivyContainer.branches[b].totalLenght - infoPool.ivyParameters.tipInfluence, currentLeaf.lpLength);



			/*******************/
			this.leafRotation = quat;
			//currentLeaf.dstScale = scale;
			/*******************/

			this.leafCenter = this.point - ivyGO.transform.position;
			this.leafCenter = Quaternion.Inverse(ivyGO.transform.rotation) * this.leafCenter;


			if(this.verticesLeaves == null)
			{
				this.verticesLeaves = new List<RTVertexData>(4);
			}
			//this.verticesLeaves.Clear();

			Vector3 vertex = Vector3.zero;
			Vector3 normal = Vector3.zero;
			Vector2 uv = Vector2.zero;
			Color vertexColor = Color.black;
			Quaternion ivyGOInverseRotation = Quaternion.Inverse(ivyGO.transform.rotation);


			for (int v = 0; v < leafMeshData.vertices.Length; v++)
			{
				Vector3 offset = left * ivyParameters.offset.x + this.lpUpward * ivyParameters.offset.y + this.lpForward * ivyParameters.offset.z;

				vertex = quat * leafMeshData.vertices[v] * scale + leafCenter + offset;

				normal = quat * leafMeshData.normals[v];
				normal = ivyGOInverseRotation * normal;

				uv = leafMeshData.uv[v];
				vertexColor = leafMeshData.colors[v];

				RTVertexData vertexData = new RTVertexData(vertex, normal, uv, Vector2.zero, vertexColor);
				this.verticesLeaves.Add(vertexData);
			}
		}
	}
}