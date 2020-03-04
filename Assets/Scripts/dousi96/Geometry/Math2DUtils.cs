using UnityEngine;
using Unity.Burst;
using Unity.Mathematics;

namespace dousi96.Geometry
{
    public static class Math2DUtils
    {
        [BurstCompile]
        public static bool IsVertexConvex(float2 prevP0, float2 P0, float2 nextP0, bool isCCW)
        {
            float2x2 m = new float2x2
            {
                c0 = P0 - prevP0,
                c1 = nextP0 - P0
            };
            float det = math.determinant(m);
            if (isCCW)
            {
                return det > 0;
            }
            return det < 0;
        }

        [BurstCompile]
        public static bool IsVertexReflex(float2 prevP0, float2 P0, float2 nextP0, bool isCCW)
        {
            float2x2 m = new float2x2
            {
                c0 = P0 - prevP0,
                c1 = nextP0 - P0
            };
            float det = math.determinant(m);
            if (isCCW)
            {
                return det < 0;
            }
            return det > 0;
        }

        [BurstCompile]
        public static bool IsInsideTriangle(float2 p, float2 a, float2 b, float2 c)
        {
            float doubleArea = (-b.y * c.x + a.y * (-b.x + c.x) + a.x * (b.y - c.y) + b.x * c.y);
            float s = 1 / (doubleArea) * (a.y * c.x - a.x * c.y + (c.y - a.y) * p.x + (a.x - c.x) * p.y);
            float t = 1 / (doubleArea) * (a.x * b.y - a.y * b.x + (a.y - b.y) * p.x + (b.x - a.x) * p.y);
            return s >= 0 && t >= 0 && (s + t) <= 1;
        }

        [BurstCompile]
        public static bool SegmentsIntersection(float2 a0, float2 a1, float2 b0, float2 b1, out float2 intersection)
        {
            intersection = new float2();

            float2 r = a1 - a0;
            float2 s = b1 - b0;

            float RxS = Cross2D(r, s);
            float QminusPxR = Cross2D(b0 - a0, r);
            float QminusPxS = Cross2D(b0 - a0, s);
            float t = QminusPxS / RxS;
            float u = QminusPxR / RxS;

            bool isRxS0 = math.abs(RxS) < float.Epsilon;
            bool isQminusPxR0 = math.abs(QminusPxR) < float.Epsilon;
            if (isRxS0 && isQminusPxR0)
            {
                //collinear
            }
            else if (isRxS0 && !isQminusPxR0)
            {
                //parallel lines
                return false;
            }
            else if (!isRxS0 && t >= 0 && t <= 1 && u >= 0 && u <= 1)
            {
                intersection = a0 + t * r;
                return true;
            }
            //no intersection
            return false;
        }

        [BurstCompile]
        public static float Cross2D(float2 a, float2 b)
        {
            return math.mul(a.x, b.y) - math.mul(a.y, b.x);
        }

        public static float Cross2D(Vector2 a, Vector2 b)
        {
            return a.x * b.y - a.y * b.x;
        }

        [BurstCompile]
        public static bool SamePoints(float2 a, float2 b)
        {
            return math.distance(a, b) <= float.Epsilon;
        }

        [BurstCompile]
        public static float LineSide(float2 p, float2 a, float2 b)
        {
            return math.mul((b.x - a.x), (p.y - a.y)) - math.mul((b.y - a.y), (p.x - a.x));
        }
    }
}
