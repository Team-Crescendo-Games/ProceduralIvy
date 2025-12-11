using UnityEngine;

namespace Dynamite3D.RealIvy
{
    public static class RealIvyMathUtils
    {

        public struct Segment
        {
            public Vector2 a;
            public Vector2 b;
        }

        public static float DistanceBetweenPointAndSegmentSS(Vector2 point, Vector2 a, Vector2 b)
        {
            float res = 0f;

            float u = (point.x - a.x) * (b.x - a.x) + (point.y - a.y) * (b.y - a.y);
            u = u / (((b.x - a.x) * (b.x - a.x)) + ((b.y - a.y) * (b.y - a.y)));

            if (u < 0)
            {
                res = (point - a).sqrMagnitude;
            }
            else if (u >= 0 && u <= 1)
            {
                Vector2 pointInSegment = new Vector2(a.x + u * (b.x - a.x), a.y + u * (b.y - a.y));
                res = (point - pointInSegment).sqrMagnitude;
            }
            else
            {
                res = (point - b).sqrMagnitude;
            }

            return res;
        }
    }
}