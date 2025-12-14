using System;
using UnityEngine;

namespace TeamCrescendo.ProceduralIvy
{
    [Serializable]
    public struct RTVertexData
    {
        public Vector3 vertex;
        public Vector3 normal;
        public Vector2 uv;
        public Color color;

        public RTVertexData(Vector3 vertex, Vector3 normal, Vector2 uv, Color color)
        {
            this.vertex = vertex;
            this.normal = normal;
            this.uv = uv;
            this.color = color;
        }
    }
}