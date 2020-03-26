using System;

public class PolygonSerializer
{
    [Serializable]
    public struct FlatPolygon
    {
        public myfloat2[] PolygonData;
        public int[] holeIndices;
    }
    [Serializable]
    public struct myfloat2
    {
        public float x;
        public float y;
        public myfloat2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }
    }
}
