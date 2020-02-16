using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;

using JacksonDunstan.NativeCollections;

namespace dousi96.Geometry.Triangulator
{
    /// <summary>
    /// Implementation of the Triangulation by Ear Clipping as explained in the following paper:
    /// http://www.geometrictools.com/Documentation/TriangulationByEarClipping.pdf
    /// </summary>
    [BurstCompile]
    public struct EarClippingTriangulatorJob : IJob
    {
        /// <summary>
        /// Are the points winded in a counter-clockwise order
        /// </summary>
        [ReadOnly]
        public PolygonJobData Polygon;
        public NativeLinkedList<int> VertexIndexLinkedList;
        [WriteOnly]
        public NativeArray<int> OutTris;

        public void Execute()
        {       
            //actual triangulation
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
                OutTris[trisIndex]      = iNext;
                OutTris[trisIndex + 1]  = iCur;
                OutTris[trisIndex + 2]  = iPrev;
                trisIndex += 3;

                VertexIndexLinkedList.Remove(cur);
            }
        }
    }
}
