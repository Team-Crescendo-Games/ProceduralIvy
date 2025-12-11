using UnityEngine;

namespace TeamCrescendo.ProceduralIvy
{
    public static class RealIvyMathUtils
    {
        public static float DistanceBetweenPointAndSegmentSS(Vector2 point, Vector2 a, Vector2 b)
        {
            var res = 0f;

            var u = (point.x - a.x) * (b.x - a.x) + (point.y - a.y) * (b.y - a.y);
            u = u / ((b.x - a.x) * (b.x - a.x) + (b.y - a.y) * (b.y - a.y));

            if (u < 0)
            {
                res = (point - a).sqrMagnitude;
            }
            else if (u >= 0 && u <= 1)
            {
                var pointInSegment = new Vector2(a.x + u * (b.x - a.x), a.y + u * (b.y - a.y));
                res = (point - pointInSegment).sqrMagnitude;
            }
            else
            {
                res = (point - b).sqrMagnitude;
            }

            return res;
        }

        public struct Segment
        {
            public Vector2 a;
            public Vector2 b;
        }
    }
}