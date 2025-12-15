using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace TeamCrescendo.ProceduralIvy
{
    [Serializable]
    public class MeshData
    {
        public List<Vector3> vertices;
        public List<Vector3> normals;
        public List<Vector2> uv;
        public List<Color32> colors32;

        // We use an array of Lists because the number of submeshes is usually 
        // fixed at initialization, but the number of triangles within them is dynamic.
        public List<int>[] triangles;

        public MeshData(int initialVertexCapacity, int numSubmeshes)
        {
            if (initialVertexCapacity < 0 || numSubmeshes <= 0)
            {
                throw new ArgumentException(
                    $"[MeshData] Invalid arguments: initialVertexCapacity={initialVertexCapacity}, numSubmeshes={numSubmeshes}");
            }

            vertices = new List<Vector3>(initialVertexCapacity);
            normals = new List<Vector3>(initialVertexCapacity);
            uv = new List<Vector2>(initialVertexCapacity);
            colors32 = new List<Color32>(initialVertexCapacity);

            triangles = new List<int>[numSubmeshes];
            for (var i = 0; i < numSubmeshes; i++)
            {
                // Initialize with a reasonable default capacity to avoid immediate resizing
                triangles[i] = new List<int>(initialVertexCapacity); 
            }
        }

        public MeshData(Mesh mesh)
        {
            Assert.IsNotNull(mesh);

            vertices = new List<Vector3>();
            normals = new List<Vector3>();
            uv = new List<Vector2>();
            colors32 = new List<Color32>();

            mesh.GetVertices(vertices);
            mesh.GetNormals(normals);
            mesh.GetUVs(0, uv);
            mesh.GetColors(colors32);

            triangles = new List<int>[mesh.subMeshCount];
            for (var i = 0; i < triangles.Length; i++)
            {
                triangles[i] = new List<int>();
                mesh.GetTriangles(triangles[i], i);
            }
        }

        public void AddTriangle(int submesh, int value)
        {
            if (submesh < 0 || submesh >= triangles.Length)
            {
                throw new IndexOutOfRangeException(
                    $"[MeshData] Attempted to access Submesh {submesh}, but only {triangles.Length} submeshes exist");
            }

            triangles[submesh].Add(value);
        }

        public void AddVertex(Vector3 vertexValue, Vector3 normalValue, Vector2 uvValue, Color32 color)
        {
            vertices.Add(vertexValue);
            normals.Add(normalValue);
            uv.Add(uvValue);
            colors32.Add(color);
        }

        public int VertexCount() => vertices.Count;

        public void Clear()
        {
            vertices.Clear();
            normals.Clear();
            uv.Clear();
            colors32.Clear();
            foreach (var triangleList in triangles)
                triangleList.Clear();
        }

        // Apply mesh data to a particular mesh at runtime
        public void Apply(Mesh targetMesh, int submeshCount, bool generateLeaves)
        {
            targetMesh.Clear();
            targetMesh.subMeshCount = submeshCount;
            targetMesh.MarkDynamic(); 

            targetMesh.SetVertices(vertices);
            targetMesh.SetNormals(normals);
            targetMesh.SetColors(colors32);
            targetMesh.SetUVs(0, uv);

            if (triangles.Length > 0)
                targetMesh.SetTriangles(triangles[0], 0);

            if (generateLeaves)
            {
                for (var i = 1; i < submeshCount; i++)
                {
                    if (i < triangles.Length)
                        targetMesh.SetTriangles(triangles[i], i);
                }
            }

            targetMesh.RecalculateBounds();
        }
    }
}