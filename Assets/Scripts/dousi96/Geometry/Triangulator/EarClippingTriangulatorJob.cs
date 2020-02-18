using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using JacksonDunstan.NativeCollections;

namespace dousi96.Geometry.Triangulator
{
    /// <summary>
    /// Implementation of the Triangulation by Ear Clipping as explained in the following paper:
    /// http://geometrictools.com/Documentation/TriangulationByEarClipping.pdf
    /// </summary>
    [BurstCompile]
    public struct EarClippingTriangulatorJob : IJob
    {
        [ReadOnly]
        public PolygonJobData Polygon;
        [WriteOnly]
        public NativeArray<int> OutTriangles;

        public void Execute()
        {
            int totNumVerts = Polygon.NumHoles * 2 + Polygon.NumTotVertices;
            NativeLinkedList<int> VertexIndexLinkedList = new NativeLinkedList<int>(totNumVerts, Allocator.Temp);
            //add contourn points to the vertices linked list and set the max ray length
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

            #region Removing Holes
            if (Polygon.NumHoles > 0)
            {
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
                float maxRayLength = math.distance(minx, maxx);
                for (int i = 0; i < holesData.Length; ++i)
                {
                    float2 M = Polygon.GetHolePoint(holesData[i].HoleIndex, holesData[i].IndexMaxX);
                    float distanceMI = float.MaxValue;
                    float2 I = new float2();
                    NativeLinkedList<int>.Enumerator vi = VertexIndexLinkedList.Head;

                    for (NativeLinkedList<int>.Enumerator contournEnum = VertexIndexLinkedList.Head;
                        contournEnum.IsValid;
                        contournEnum.MoveNext())
                    {
                        NativeLinkedList<int>.Enumerator contournNextEnum = (!contournEnum.Next.IsValid) ? VertexIndexLinkedList.Head : contournEnum.Next;

                        //intersect the ray
                        float2 intersection;
                        bool areSegmentsIntersecting = Geometry2DUtils.SegmentsIntersection(M, new float2(maxRayLength, M.y), Polygon.Vertices[contournEnum.Value], Polygon.Vertices[contournNextEnum.Value], out intersection);
                        if (!areSegmentsIntersecting)
                        {
                            continue;
                        }

                        float distance = math.distance(M, intersection);
                        if (distance < distanceMI)
                        {
                            vi = contournEnum;
                            I = intersection;
                            distanceMI = distance;
                        }
                    }

                    NativeLinkedList<int>.Enumerator selectedBridgePoint;
                    if (Geometry2DUtils.SamePoints(I, Polygon.Vertices[vi.Value]))
                    {
                        //I is a vertex of the outer polygon
                        selectedBridgePoint = vi;
                    }
                    else
                    {
                        NativeLinkedList<int>.Enumerator viplus1 = (!vi.Next.IsValid) ? VertexIndexLinkedList.Head : vi.Next;
                        //I is an interior point of the edge <V(i), V(i+1)>, select P as the maximum x-value endpoint of the edge
                        NativeLinkedList<int>.Enumerator P = (Polygon.Vertices[viplus1.Value].x > Polygon.Vertices[vi.Value].x) ? viplus1 : vi;
                        selectedBridgePoint = P;
                        //Search the reflex vertices of the outer polygon (not including P if it happens to be reflex)                    
                        float minAngle = float.MaxValue;
                        float minDist = float.MaxValue;
                        for (NativeLinkedList<int>.Enumerator contournEnum = VertexIndexLinkedList.Head;
                            contournEnum.IsValid;
                            contournEnum.MoveNext())
                        {
                            //not including P
                            if (contournEnum == P)
                            {
                                continue;
                            }

                            int currentIndex = contournEnum.Value;
                            int previousIndex = (!contournEnum.Prev.IsValid) ? VertexIndexLinkedList.Tail.Value : contournEnum.Prev.Value;
                            int nextIndex = (!contournEnum.Next.IsValid) ? VertexIndexLinkedList.Head.Value : contournEnum.Next.Value;

                            bool isReflex = Geometry2DUtils.IsVertexReflex(Polygon.Vertices[previousIndex], Polygon.Vertices[currentIndex], Polygon.Vertices[nextIndex], true);
                            if (!isReflex)
                            {
                                continue;
                            }

                            bool isReflexVertexInsideMIPTriangle = Geometry2DUtils.IsInsideTriangle(Polygon.Vertices[currentIndex], M, I, Polygon.Vertices[P.Value]);
                            if (isReflexVertexInsideMIPTriangle)
                            {
                                //search for the reflex vertex R that minimizes the angle between (1,0) and the line segment M-R
                                float2 atan2 = Polygon.Vertices[currentIndex] - M;
                                float angleRMI = math.atan2(atan2.y, atan2.x);
                                if (angleRMI < minAngle)
                                {
                                    selectedBridgePoint = contournEnum;
                                    minAngle = angleRMI;
                                }
                                else if (math.abs(angleRMI - minAngle) < float.Epsilon)
                                {
                                    //same angle
                                    float distanceRM = math.lengthsq(atan2);
                                    if (distanceRM < minDist)
                                    {
                                        selectedBridgePoint = contournEnum;
                                        minDist = distanceRM;
                                    }
                                }
                            }
                        }
                    }

                    //insert the bridge points and the holes points inside the linked list
                    int holeStartIndex = Polygon.StartPointsHoles[holesData[i].HoleIndex];
                    int holeLength = Polygon.NumPointsPerHole[holesData[i].HoleIndex];
                    int holeEndIndex = holeStartIndex + holeLength;
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
            #endregion

            #region Triangulation
            //Triangulation
            int trisIndex = 0;
            NativeLinkedList<int>.Enumerator cur = VertexIndexLinkedList.Head;
            while (VertexIndexLinkedList.Length > 2)
            {
                if (!cur.IsValid)
                {
                    cur = VertexIndexLinkedList.Head;
                }
                int iCur = cur.Value;
                int iPrev = (!cur.Prev.IsValid) ? VertexIndexLinkedList.Tail.Value : cur.Prev.Value;
                int iNext = (!cur.Next.IsValid) ? VertexIndexLinkedList.Head.Value : cur.Next.Value;

                //check if the current vertex is an interior one
                if (!Geometry2DUtils.IsVertexConvex(Polygon.Vertices[iPrev], Polygon.Vertices[iCur], Polygon.Vertices[iNext], true))
                {
                    cur.MoveNext();
                    continue;
                }

                //check if any point inside the found triangle
                bool pointInsideTriangleExists = false;
                for (int j = 0; j < Polygon.NumTotVertices; ++j)
                {
                    if (j == iPrev || j == iCur || j == iNext)
                    {
                        continue;
                    }
                    pointInsideTriangleExists |= Geometry2DUtils.IsInsideTriangle(Polygon.Vertices[j], Polygon.Vertices[iPrev], Polygon.Vertices[iCur], Polygon.Vertices[iNext]);
                }

                if (pointInsideTriangleExists)
                {
                    cur.MoveNext();
                    continue;
                }

                //create the tris
                OutTriangles[trisIndex] = iNext;
                OutTriangles[trisIndex + 1] = iCur;
                OutTriangles[trisIndex + 2] = iPrev;
                trisIndex += 3;

                VertexIndexLinkedList.Remove(cur);
            }
            VertexIndexLinkedList.Dispose();
            #endregion
        }
    }
}
