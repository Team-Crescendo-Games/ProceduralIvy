using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

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
            if (numVertices <= 0 || numSubmeshes <= 0 || numTrianglesPerSubmesh.Count != numSubmeshes)
                throw new ArgumentException(
                    $"[RTMeshData] Invalid arguments: numVertices={numVertices}, numSubmeshes={numSubmeshes}, numTrianglesPerSubmesh.Count={numTrianglesPerSubmesh.Count}");

            vertices = new Vector3[numVertices];
            normals = new Vector3[numVertices];
            uv = new Vector2[numVertices];
            colors = new Color[numVertices];

            triangles = new int[numSubmeshes][];
            for (var i = 0; i < triangles.Length; i++) 
                triangles[i] = new int[numTrianglesPerSubmesh[i]];

            triangleIndices = new int[triangles.Length];
            vertexIndex = 0;
        }

        public RTMeshData(Mesh mesh)
        {
            Assert.IsNotNull(mesh);
            
            vertices = mesh.vertices;
            normals = mesh.normals;
            uv = mesh.uv;
            colors = mesh.colors;
            
            triangles = new int[mesh.subMeshCount][];
            for (var i = 0; i < triangles.Length; i++) 
                triangles[i] = mesh.GetTriangles(i);
            
            triangleIndices = new int[triangles.Length];
            vertexIndex = 0;
        }

        public void AddTriangle(int submesh, int value)
        {
            if (submesh < 0 || submesh >= triangles.Length) 
            {
                throw new IndexOutOfRangeException(
                    $"[RTMeshData] Attempted to access Submesh {submesh}, but only {triangles.Length} submeshes exist)");
            }
    
            if (triangleIndices[submesh] >= triangles[submesh].Length)
            {
                // If current size is 0, jump to 4 (or any small power of 2). Otherwise, double it.
                var currentLen = triangles[submesh].Length;
                var newSize = currentLen == 0 ? 4 : currentLen * 2;
        
                Array.Resize(ref triangles[submesh], newSize);
            }

            triangles[submesh][triangleIndices[submesh]] = value;
            triangleIndices[submesh]++;
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

        public int VertexCount() => vertCount;

        public void Clear()
        {
            vertCount = 0;
            vertexIndex = 0;

            for (var i = 0; i < triangleIndices.Length; i++) 
                triangleIndices[i] = 0;
        }
    }
}