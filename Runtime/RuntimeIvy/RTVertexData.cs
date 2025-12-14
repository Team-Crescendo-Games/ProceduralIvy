using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace TeamCrescendo.ProceduralIvy
{
    [Serializable]
    public struct RTVertexData
    {
        public Vector3 vertex; // float32
        public Vector3 normal; // float32
        public Vector2 uv; // float32
        public Color32 color32; // unorm8

        public RTVertexData(Vector3 vertex, Vector3 normal, Vector2 uv, Color32 color32)
        {
            this.vertex = vertex;
            this.normal = normal;
            this.uv = uv;
            this.color32 = color32;
        }
    }
}