using System;

namespace dousi96.Geometry.Triangulator
{
    public struct HoleData : IComparable<HoleData>
    {
        public int startIndex;
        public int Length;
        public int indexMaxX;
        public float maxX;

        public int CompareTo(HoleData other)
        {
            if (this.maxX > other.maxX)
            {
                return -1;
            }
            else if (this.maxX < other.maxX)
            {
                return 1;
            }
            return 0;
        }
    }
}
