using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

using JacksonDunstan.NativeCollections;

namespace dousi96.Geometry.Triangulator
{
    [BurstCompile]
    public struct EarClippingRemoveHolesJob : IJob
    {
        [ReadOnly]
        public bool isCCW;
        [ReadOnly]
        public NativeArray<float2> Vertices;
        [ReadOnly]
        public int NumContournPoints;
        [ReadOnly]
        public NativeArray<int> StartPointsHoles;
        [ReadOnly]
        public NativeArray<int> NumPointsPerHole;
        private int numHoles { get => StartPointsHoles.Length; }
        public NativeLinkedList<int> VertexIndexLinkedList;

        public void Execute()
        {
            //add contourn points
            float minx = float.MaxValue;
            float maxx = float.MinValue;
            for (int i = 0; i < NumContournPoints; ++i)
            {
                VertexIndexLinkedList.InsertAfter(VertexIndexLinkedList.Tail, i);

                if (Vertices[i].x < minx)
                {
                    minx = Vertices[i].x;
                }
                if (Vertices[i].x > maxx)
                {
                    maxx = Vertices[i].x;
                }
            }
            float maxRayLength = math.distance(minx, maxx);

            NativeArray<HoleData> holesData = new NativeArray<HoleData>(numHoles, Allocator.Temp);
            for (int i = 0; i < numHoles; ++i)
            {
                HoleData holeData = new HoleData();
                holeData.startIndex = StartPointsHoles[i];
                holeData.Length = NumPointsPerHole[i];
                holeData.maxX = float.MinValue;
                holeData.indexMaxX = -1;
                for (int j = holeData.startIndex; j < holeData.startIndex + holeData.Length; ++j)
                {
                    if (holeData.maxX < Vertices[j].x)
                    {
                        holeData.maxX = Vertices[j].x;
                        holeData.indexMaxX = j;
                    }
                }
                holesData[i] = holeData;
            }
            holesData.Sort();

            for (int i = 0; i < holesData.Length; ++i)
            {
                int holeStartIndex = holesData[i].startIndex;
                int holeLength = holesData[i].Length;
                int holeEndIndex = holesData[i].startIndex + holeLength;
                int indexM = holesData[i].indexMaxX;

                //intersect 
                float2 endPointM = new float2(maxRayLength, Vertices[indexM].y);
                int indexI = -1;
                for (NativeLinkedList<int>.Enumerator enumLL = VertexIndexLinkedList.Head; enumLL.IsValid; enumLL.MoveNext())
                {
                    int iCur = enumLL.Value;
                    int iNext = (!enumLL.Next.IsValid) ? VertexIndexLinkedList.Head.Value : enumLL.Next.Value;

                    //intersect the ray
                    float2 I;
                    if (!Geometry2DUtils.SegmentIntersection(Vertices[indexM], endPointM, Vertices[iCur], Vertices[iNext], out I))
                    {
                        continue;
                    }

                    //I is a vertex of the outer polygon
                    if (math.distance(I, Vertices[iCur]) <= float.Epsilon)
                    {
                        indexI = iCur;
                        break;
                    }

                    //I is an interior point of the edge <V(i), V(i+1)>, select P as the maximum x-value enpoint of the edge
                    int indexP = (Vertices[iCur].x > Vertices[iNext].x) ? iCur : iNext;

                    //Search the reflex vertices of the outer polygon (not including P if it happens to be reflex)                    
                    bool allOutsideMIP = true;
                    float minAngle = float.MaxValue;
                    float minDist = float.MaxValue;
                    int indexR = -1;
                    for (var reflexVertexEnum = VertexIndexLinkedList.Head; reflexVertexEnum.IsValid; reflexVertexEnum.MoveNext())
                    {
                        int iReflexCur = reflexVertexEnum.Value;

                        //not including P
                        if (iReflexCur == indexP)
                        {
                            continue;
                        }

                        int iReflexPrev = (!reflexVertexEnum.Prev.IsValid) ? VertexIndexLinkedList.Tail.Value : reflexVertexEnum.Prev.Value;
                        int iReflexNext = (!reflexVertexEnum.Next.IsValid) ? VertexIndexLinkedList.Head.Value : reflexVertexEnum.Next.Value;

                        bool isReflex = !Geometry2DUtils.IsVertexConvex(Vertices[iReflexPrev], Vertices[iReflexCur], Vertices[iReflexNext], isCCW);
                        if (!isReflex)
                        {
                            continue;
                        }

                        if (Geometry2DUtils.IsInsideTriangle(Vertices[iReflexCur], Vertices[indexM], I, Vertices[indexP]))
                        {
                            allOutsideMIP = false;

                            float2 atan2 = Vertices[iReflexCur] - Vertices[indexM];
                            float distanceRM = math.lengthsq(atan2);
                            float angleRMI = math.atan2(atan2.x, atan2.y);

                            if (angleRMI < minAngle)
                            {
                                indexR = iReflexCur;
                            }
                            else if (math.abs(angleRMI - minAngle) <= float.Epsilon)
                            {
                                if (distanceRM < minDist)
                                {
                                    indexR = iReflexCur;
                                }
                            }
                        }
                    }

                    //If all of these vertices are strictly outside triangle <M, I, Pi>, then M and P are mutually
                    indexI = (allOutsideMIP) ? indexP : indexR;
                    break;
                }

                for (NativeLinkedList<int>.Enumerator enumLL = VertexIndexLinkedList.Head; enumLL.IsValid; enumLL.MoveNext())
                {
                    if (enumLL.Value != indexI)
                    {
                        continue;
                    }

                    VertexIndexLinkedList.InsertAfter(enumLL, indexI);
                    for (int j = indexM, count = 0;
                        count < holeLength;
                        ++count, j = (j == holeStartIndex) ? holeEndIndex - 1 : j - 1)
                    {
                        VertexIndexLinkedList.InsertAfter(enumLL, j);
                    }
                    VertexIndexLinkedList.InsertAfter(enumLL, indexM);
                    break;
                }
            }

            holesData.Dispose();
        }
    }
}
