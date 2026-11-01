using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TeamCrescendo.ProceduralIvy
{
    public static class SurfaceCursor
    {
        private const int SamplesPerSide = 8;
        private static readonly Color CreaseColor = new Color(1f, 0.85f, 0.2f, 1f);

        public static void Draw(Vector3 point, Vector3 normal, float brushSize, Color color, LayerMask mask)
        {
            if (Event.current.type != EventType.Repaint) return;

            float handleSize = HandleUtility.GetHandleSize(point);
            float radius = handleSize * (brushSize / 80f) * 0.5f;

            var previousZTest = Handles.zTest;
            Handles.zTest = CompareFunction.LessEqual;
            DrawSurfacePoints(point, normal, radius, handleSize, color, mask);
            Handles.zTest = previousZTest;

            Handles.color = color;
            Handles.DrawWireDisc(point, normal, handleSize * 0.06f, 2f);
            Handles.DrawLine(point, point + normal * handleSize * 0.3f, 2f);
            Handles.DotHandleCap(0, point, Quaternion.identity, handleSize * 0.012f, EventType.Repaint);
        }

        private static void DrawSurfacePoints(Vector3 center, Vector3 normal, float radius, float handleSize, Color color, LayerMask mask)
        {
            var tangent = Vector3.Cross(normal, Mathf.Abs(normal.y) < 0.99f ? Vector3.up : Vector3.right).normalized;
            var bitangent = Vector3.Cross(normal, tangent);
            float spacing = radius / SamplesPerSide;
            float dotSize = handleSize * 0.006f;

            for (int y = -SamplesPerSide; y <= SamplesPerSide; y++)
            for (int x = -SamplesPerSide; x <= SamplesPerSide; x++)
            {
                float falloff = new Vector2(x, y).magnitude / SamplesPerSide;
                if (falloff > 1f) continue;

                var origin = center + (tangent * x + bitangent * y) * spacing + normal * radius;
                if (!Physics.Raycast(origin, -normal, out var hit, radius * 2f, mask)) continue;

                float crease = 1f - Mathf.Clamp01(Vector3.Dot(hit.normal, normal));
                var c = Color.Lerp(color, CreaseColor, crease * 2f);
                c.a = 1f - falloff * falloff;
                Handles.color = c;
                Handles.DotHandleCap(0, hit.point + hit.normal * (handleSize * 0.002f), Quaternion.identity, dotSize, EventType.Repaint);
            }
        }
    }
}
