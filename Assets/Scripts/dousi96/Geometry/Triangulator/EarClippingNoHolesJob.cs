using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;

namespace dousi96.Geometry.Triangulator
{
    /// <summary>
    /// Implementation of the Triangulation by Ear Clipping as explained in the following paper:
    /// http://www.geometrictools.com/Documentation/TriangulationByEarClipping.pdf
    /// </summary>
    public struct EarClippingNoHolesJob : IJob
    {
        /// <summary>
        /// Are the points winded in a counter-clockwise order
        /// </summary>
        [ReadOnly]
        public bool isCCW;
        [ReadOnly]
        public NativeArray<float2> InVerts;
        [WriteOnly]
        public NativeArray<int> OutTris;

        public void Execute()
        {
            NativeQueue<int> indexesQueue = new NativeQueue<int>(Allocator.Temp);

            for (int i = 0; i < InVerts.Length; ++i)
            {
                indexesQueue.Enqueue(i);
            }

            int trisIndex = 0;
            while (indexesQueue.Count > 2)
            {
                int iPrev = indexesQueue.Dequeue();
                int iCur = indexesQueue.Dequeue();
                int iNext = indexesQueue.Dequeue();

                //check if the current vertex is an interior one
                if (!Geometry2DUtils.IsVertexConvex(InVerts[iPrev], InVerts[iCur], InVerts[iNext], isCCW))
                {
                    indexesQueue.Enqueue(iPrev);
                    indexesQueue.Enqueue(iCur);
                    indexesQueue.Enqueue(iNext);
                    continue;
                }

                //check if any point inside the found triangle
                bool pointInsideTriangleExists = false;
                for (int j = 0; j < InVerts.Length; ++j)
                {
                    if (j == iPrev || j == iCur || j == iNext)
                    {
                        continue;
                    }

                    pointInsideTriangleExists |= Geometry2DUtils.IsInsideTriangle(InVerts[j], InVerts[iPrev], InVerts[iCur], InVerts[iNext]);
                }

                if (pointInsideTriangleExists)
                {
                    indexesQueue.Enqueue(iPrev);
                    indexesQueue.Enqueue(iCur);
                    indexesQueue.Enqueue(iNext);
                    continue;
                }

                OutTris[trisIndex] = iPrev;
                OutTris[trisIndex + 1] = iCur;
                OutTris[trisIndex + 2] = iNext;
                trisIndex += 3;

                indexesQueue.Enqueue(iPrev);
                indexesQueue.Enqueue(iNext);
            }

            indexesQueue.Dispose();
        }
    }
}
