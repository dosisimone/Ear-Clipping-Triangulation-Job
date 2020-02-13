using Unity.Mathematics;

namespace dousi96.Geometry 
{
    public static class Geometry2DUtils
    {        
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

        public static float TriangleArea(float2 p0, float2 p1, float2 p2)
        {
            return math.mul(
                math.abs(
                    math.mul(p0.x, (p1.y - p2.y)) +
                    math.mul(p1.x, (p2.y - p0.y)) +
                    math.mul(p2.x, (p0.y - p1.y))
                ), 0.5f);
        }

        public static bool IsInsideTriangle(float2 p, float2 t0, float2 t1, float2 t2)
        {
            float A = TriangleArea(t0, t1, t2);
            float Apt1t2 = TriangleArea(p, t1, t2);
            float At0pt2 = TriangleArea(t0, p, t2);
            float At0t1p = TriangleArea(t0, t1, p);
            return math.abs(A - Apt1t2 - At0pt2 - At0t1p) <= float.Epsilon;
        }

        public static bool SegmentIntersection(float2 a0, float2 a1, float2 b0, float2 b1, out float2 intersection)
        {
            intersection = new float2();

            float2 Ia = new float2()
            {
                x = math.min(a0.x, a1.x),
                y = math.max(a0.x, a1.x)
            };

            float2 Ib = new float2()
            {
                x = math.min(b0.x, b1.x),
                y = math.max(b0.x, b1.x)
            };

            if (Ia.y < Ib.x)
            {
                return false;
            }

            float A1 = (a0.y - a1.y) / (a0.x - a1.x);
            float A2 = (b0.y - b1.y) / (b0.x - b1.x);

            //the segments are parallel
            if (math.abs(A1 - A2) <= float.Epsilon)
            {
                return false;
            }

            float B1 = a0.y - A1 * a0.x;
            float B2 = b0.y - A2 * b0.x;

            intersection.x = (B2 - B1) / (A1 - A2);
            intersection.y = A1 * intersection.x + B1;

            float2 I = new float2()
            {
                x = math.max(Ia.x, Ib.x),
                y = math.min(Ia.y, Ib.y)
            };

            bool intersectionPointNotValid = intersection.x < I.x || intersection.x > I.y;
            if (intersectionPointNotValid)
            {
                return false;
            }
            return true;
        }
    }
}
