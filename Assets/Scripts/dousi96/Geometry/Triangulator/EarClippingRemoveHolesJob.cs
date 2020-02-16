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
        public PolygonJobData Polygon;
        public NativeLinkedList<int> VertexIndexLinkedList;

        public void Execute()
        {
            //add contourn points and set the max ray length
            float minx = float.MaxValue;
            float maxx = float.MinValue;
            for (int i = 0; i < Polygon.NumContournPoints; ++i)
            {
                VertexIndexLinkedList.InsertAfter(VertexIndexLinkedList.Tail, i);
                if (Polygon.Vertices[i].x < minx)
                {
                    minx = Polygon.Vertices[i].x;
                }
                if (Polygon.Vertices[i].x > maxx)
                {
                    maxx = Polygon.Vertices[i].x;
                }
            }
            float maxRayLength = math.distance(minx, maxx);

            //create the array containing the holes data
            NativeArray<EarClippingHoleData> holesData = new NativeArray<EarClippingHoleData>(Polygon.NumHoles, Allocator.Temp);
            for (int i = 0; i < Polygon.NumHoles; ++i)
            {
                int indexMaxX = -1;
                float maxX = float.MinValue;                
                for (int j = 0; j < Polygon.NumPointsPerHole[i]; ++j)
                {
                    float2 holeVertex = Polygon.GetHolePoint(i, j);
                    if (maxX < holeVertex.x)
                    {
                        maxX = holeVertex.x;
                        indexMaxX = j;
                    }
                }
                holesData[i] = new EarClippingHoleData(Polygon, i, indexMaxX);
            }
            holesData.Sort();

            //start the hole removing algorithm
            for (int i = 0; i < holesData.Length; ++i)
            {
                int holeStartIndex = Polygon.StartPointsHoles[holesData[i].HoleIndex];
                int holeLength = Polygon.NumPointsPerHole[holesData[i].HoleIndex];
                int holeEndIndex = holeStartIndex + holeLength;
                float2 M = Polygon.GetHolePoint(holesData[i].HoleIndex, holesData[i].IndexMaxX);

                NativeLinkedList<int>.Enumerator selectedBridgePoint = new NativeLinkedList<int>.Enumerator();
                for (NativeLinkedList<int>.Enumerator contournEnum = VertexIndexLinkedList.Head; 
                    contournEnum.IsValid; 
                    contournEnum.MoveNext())
                {
                    NativeLinkedList<int>.Enumerator contournNextEnum = (!contournEnum.Next.IsValid) ? VertexIndexLinkedList.Head : contournEnum.Next;

                    //intersect the ray
                    float2 I;
                    bool areSegmentsIntersecting = Geometry2DUtils.SegmentIntersection(M, new float2(maxRayLength, M.y), Polygon.Vertices[contournEnum.Value], Polygon.Vertices[contournNextEnum.Value], out I);
                    if (!areSegmentsIntersecting)
                    {
                        continue;
                    }

                    //I is a vertex of the outer polygon
                    if (math.distance(I, Polygon.Vertices[contournEnum.Value]) <= float.Epsilon)
                    {
                        selectedBridgePoint = contournEnum;
                        break;
                    }

                    //I is an interior point of the edge <V(i), V(i+1)>, select P as the maximum x-value enpoint of the edge
                    NativeLinkedList<int>.Enumerator P = (Polygon.Vertices[contournEnum.Value].x > Polygon.Vertices[contournNextEnum.Value].x) ? contournEnum : contournNextEnum;

                    //Search the reflex vertices of the outer polygon (not including P if it happens to be reflex)                    
                    bool allOutsideMIP = true;
                    float minAngle = float.MaxValue;
                    float minDist = float.MaxValue;
                    NativeLinkedList<int>.Enumerator R = new NativeLinkedList<int>.Enumerator();
                    for (NativeLinkedList<int>.Enumerator reflexVertexEnum = VertexIndexLinkedList.Head; 
                        reflexVertexEnum.IsValid; 
                        reflexVertexEnum.MoveNext())
                    {
                        //not including P
                        if (reflexVertexEnum == P)
                        {
                            continue;
                        }

                        int iReflexCur = reflexVertexEnum.Value;
                        int iReflexPrev = (!reflexVertexEnum.Prev.IsValid) ? VertexIndexLinkedList.Tail.Value : reflexVertexEnum.Prev.Value;
                        int iReflexNext = (!reflexVertexEnum.Next.IsValid) ? VertexIndexLinkedList.Head.Value : reflexVertexEnum.Next.Value;

                        bool isReflex = Geometry2DUtils.IsVertexReflex(Polygon.Vertices[iReflexPrev], Polygon.Vertices[iReflexCur], Polygon.Vertices[iReflexNext], true);
                        if (!isReflex)
                        {
                            continue;
                        }

                        bool isReflexVertexInsideMIPTriangle = Geometry2DUtils.IsInsideTriangle(Polygon.Vertices[iReflexCur], M, I, Polygon.Vertices[P.Value]);
                        if (isReflexVertexInsideMIPTriangle)
                        {
                            allOutsideMIP = false;
                            float2 atan2 = Polygon.Vertices[iReflexCur] - M;                            
                            float angleRMI = math.atan2(atan2.y, atan2.x);

                            //same angle
                            if (math.abs(angleRMI - minAngle) <= float.Epsilon)
                            {
                                float distanceRM = math.lengthsq(atan2);
                                if (distanceRM < minDist)
                                {
                                    R = reflexVertexEnum;
                                    minDist = distanceRM;
                                }
                            }
                            else
                            {
                                if (angleRMI < minAngle)
                                {
                                    R = reflexVertexEnum;
                                    minAngle = angleRMI;
                                }
                            }
                        }
                    }

                    //If all of these vertices are strictly outside triangle <M, I, Pi>, then M and P are mutually
                    selectedBridgePoint = (allOutsideMIP) ? P : R;
                    break;
                }

                //insert the bridge points and the holes points inside the linked list
                int internalMaxXIndex = holeStartIndex + holesData[i].IndexMaxX;
                VertexIndexLinkedList.InsertAfter(selectedBridgePoint, selectedBridgePoint.Value);
                for (int j = internalMaxXIndex, count = 0;
                    count < holeLength;
                    ++count, j = (j == holeStartIndex) ? holeEndIndex - 1 : j - 1)
                {
                    VertexIndexLinkedList.InsertAfter(selectedBridgePoint, j);
                }
                VertexIndexLinkedList.InsertAfter(selectedBridgePoint, internalMaxXIndex);
            }

            holesData.Dispose();
        }
    }
}
