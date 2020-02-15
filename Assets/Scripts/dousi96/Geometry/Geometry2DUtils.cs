using Unity.Mathematics;
using Unity.Burst;

namespace dousi96.Geometry 
{
    public static class Geometry2DUtils
    {
        [BurstCompile]
        public static bool IsVertexConvex(float2 prevP0, float2 P0, float2 nextP0, bool isCCW)
        {
            float2x2 m = new float2x2
            {
                c0 = prevP0 - P0,
                c1 = nextP0 - P0
            };
            float det = math.determinant(m);
            if (isCCW)
            {
                return det <= 0;
            }
            else
            {
                return det >= 0;
            }
        }

        [BurstCompile]
        public static float TriangleArea(float2 p0, float2 p1, float2 p2)
        {
            return math.mul(
                math.abs(
                    math.mul(p0.x, (p1.y - p2.y)) +
                    math.mul(p1.x, (p2.y - p0.y)) +
                    math.mul(p2.x, (p0.y - p1.y))
                ), 0.5f);
        }

        [BurstCompile]
        public static bool IsInsideTriangle(float2 p, float2 t0, float2 t1, float2 t2)
        {
            float A = TriangleArea(t0, t1, t2);
            float Apt1t2 = TriangleArea(p, t1, t2);
            float At0pt2 = TriangleArea(t0, p, t2);
            float At0t1p = TriangleArea(t0, t1, p);
            return math.abs(A - Apt1t2 - At0pt2 - At0t1p) <= float.Epsilon;
        }

        [BurstCompile]
        public static bool SegmentIntersection(float2 a0, float2 a1, float2 b0, float2 b1, out float2 intersection)
        {
            intersection = new float2();

            float2 r = a1 - a0;
            float2 s = b1 - b0;

            float RxS = Cross2D(r, s);
            float QminusPxR = Cross2D(b0 - a0, r);
            float QminusPxS = Cross2D(b0 - a0, s);
            float t = QminusPxS / RxS;
            float u = QminusPxR / RxS;            

            bool isRxS0 = math.abs(RxS) <= float.Epsilon;
            bool isQminusPxR0 = math.abs(QminusPxR) <= float.Epsilon;
            if (isRxS0 && isQminusPxR0)
            {
                //collinear

            }
            else if (isRxS0 && !isQminusPxR0)
            {
                //parallel lines
                return false;
            }
            else if (!isRxS0 && t >= 0 && t <=1 && u >= 0 && u <= 1)
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
    }
}
