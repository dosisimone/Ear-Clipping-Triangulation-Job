using System;
using Unity.Burst;

namespace dousi96.Geometry.Triangulator
{
    [BurstCompile]
    internal struct EarClippingHoleData : IComparable<EarClippingHoleData>
    {
        public readonly int HoleIndex;
        public readonly int IndexMaxX;
        private readonly float maxX;

        public EarClippingHoleData(PolygonJobData polygon, int holeIndex, int indexMaxX)
        {
            HoleIndex = holeIndex;
            IndexMaxX = indexMaxX;
            maxX = polygon.GetHolePoint(holeIndex, indexMaxX).x;
        }

        public int CompareTo(EarClippingHoleData other)
        {
            if (maxX > other.maxX)
            {
                return -1;
            }
            else if (maxX < other.maxX)
            {
                return 1;
            }
            return 0;
        }
    }
}
