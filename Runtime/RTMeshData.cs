using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dynamite3D.RealIvy
{
	[System.Serializable]
	public class RTMeshData
	{
		int vertCount;
		int vertexIndex;
		public Vector3[] vertices;
		public Vector3[] normals;
		public Vector2[] uv;
		public Vector2[] uv2;
		public Color[] colors;

		public int[] triangleIndices;
		public int[][] triangles;


		public RTMeshData(int numVertices, int numSubmeshes, List<int> numTrianglesPerSubmesh)
		{
			Vector3[] vertices = new Vector3[numVertices];
			Vector3[] normals = new Vector3[numVertices];
			Vector2[] uv = new Vector2[numVertices];
			Color[] colors = new Color[numVertices];

			int[][] triangles = new int[numSubmeshes][];
			for (int i = 0; i < triangles.Length; i++)
			{
				triangles[i] = new int[numTrianglesPerSubmesh[i]];
			}

			SetValues(vertices, normals, uv, colors, triangles);
		}

		public RTMeshData(Mesh mesh)
		{
			int subMeshCount = mesh.subMeshCount;

			Vector3[] vertices = mesh.vertices;
			Vector3[] normals = mesh.normals;
			Vector2[] uv = mesh.uv;
			Vector2[] uv2Aux = mesh.uv2;
			Color[] colors = mesh.colors;


			int[][] triangles = new int[subMeshCount][];
			for (int i = 0; i < triangles.Length; i++)
			{
				triangles[i] = mesh.GetTriangles(i);
			}

			SetValues(vertices, normals, uv, colors, triangles);
		}

		private void SetValues(Vector3[] vertices, Vector3[] normals, Vector2[] uv, Color[] colors, int[][] triangles)
		{
			this.vertices = vertices;
			this.normals = normals;
			this.uv = uv;
			this.colors = colors;
			this.triangles = triangles;

			this.triangleIndices = new int[triangles.Length];
			this.vertexIndex = 0;
		}

		public void CopyDataFromIndex(int index, int lastTriCount, int numTris, RTMeshData copyFrom)
		{
			vertices[index] = copyFrom.vertices[index];
			normals[index] = copyFrom.normals[index];
			uv[index] = copyFrom.uv[index];
		}

		public void AddTriangle(int sumbesh, int value)
		{
			if (triangleIndices[sumbesh] >= triangles[sumbesh].Length)
			{
				int newSize = triangles[sumbesh].Length * 2;
				Array.Resize<int>(ref triangles[sumbesh], newSize);
			}

			if (triangles[sumbesh].Length > 0)
			{
				triangles[sumbesh][triangleIndices[sumbesh]] = value;
				triangleIndices[sumbesh]++;
			}
		}

		public void AddVertex(Vector3 vertexValue, Vector3 normalValue, Vector2 uvValue, Color color)
		{
			if (vertCount >= vertices.Length)
			{
				Resize();
			}


			vertices[vertexIndex] = vertexValue;
			normals[vertexIndex] = normalValue;
			uv[vertexIndex] = uvValue;
			colors[vertexIndex] = color;


			vertexIndex++;

			vertCount++;
		}

		private void Resize()
		{
			int newSize = vertices.Length * 2;
			Array.Resize<Vector3>(ref vertices, newSize);
			Array.Resize<Vector3>(ref normals, newSize);
			Array.Resize<Vector2>(ref uv, newSize);
			Array.Resize<Color>(ref colors, newSize);
		}

		public int VertexCount()
		{
			return vertCount;
		}

		public void Clear()
		{
			this.vertCount = 0;
			this.vertexIndex = 0;

			for (int i = 0; i < triangleIndices.Length; i++)
			{
				this.triangleIndices[i] = 0;
			}
		}
	}
}