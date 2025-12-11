using System;
using System.Collections.Generic;
using UnityEngine;

namespace TeamCrescendo.ProceduralIvy
{
    [Serializable]
    public class RTMeshData
    {
        public Vector3[] vertices;
        public Vector3[] normals;
        public Vector2[] uv;
        public Vector2[] uv2;
        public Color[] colors;

        public int[] triangleIndices;
        public int[][] triangles;
        private int vertCount;
        private int vertexIndex;

        public RTMeshData(int numVertices, int numSubmeshes, List<int> numTrianglesPerSubmesh)
        {
            var vertices = new Vector3[numVertices];
            var normals = new Vector3[numVertices];
            var uv = new Vector2[numVertices];
            var colors = new Color[numVertices];

            var triangles = new int[numSubmeshes][];
            for (var i = 0; i < triangles.Length; i++) 
                triangles[i] = new int[numTrianglesPerSubmesh[i]];

            SetValues(vertices, normals, uv, colors, triangles);
        }

        public RTMeshData(Mesh mesh)
        {
            var subMeshCount = mesh.subMeshCount;

            var triangles = new int[subMeshCount][];
            for (var i = 0; i < triangles.Length; i++) 
                triangles[i] = mesh.GetTriangles(i);

            SetValues(mesh.vertices, mesh.normals, mesh.uv, mesh.colors, triangles);
        }

        private void SetValues(Vector3[] vertices, Vector3[] normals, Vector2[] uv, Color[] colors, int[][] triangles)
        {
            this.vertices = vertices;
            this.normals = normals;
            this.uv = uv;
            this.colors = colors;
            this.triangles = triangles;

            triangleIndices = new int[triangles.Length];
            vertexIndex = 0;
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
                var newSize = triangles[sumbesh].Length * 2;
                Array.Resize(ref triangles[sumbesh], newSize);
            }

            if (triangles[sumbesh].Length > 0)
            {
                triangles[sumbesh][triangleIndices[sumbesh]] = value;
                triangleIndices[sumbesh]++;
            }
        }

        public void AddVertex(Vector3 vertexValue, Vector3 normalValue, Vector2 uvValue, Color color)
        {
            if (vertCount >= vertices.Length) Resize();

            vertices[vertexIndex] = vertexValue;
            normals[vertexIndex] = normalValue;
            uv[vertexIndex] = uvValue;
            colors[vertexIndex] = color;

            vertexIndex++;

            vertCount++;
        }

        private void Resize()
        {
            var newSize = vertices.Length * 2;
            Array.Resize(ref vertices, newSize);
            Array.Resize(ref normals, newSize);
            Array.Resize(ref uv, newSize);
            Array.Resize(ref colors, newSize);
        }

        public int VertexCount()
        {
            return vertCount;
        }

        public void Clear()
        {
            vertCount = 0;
            vertexIndex = 0;

            for (var i = 0; i < triangleIndices.Length; i++) triangleIndices[i] = 0;
        }
    }
}