using System;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

namespace dousi96.Geometry
{
    [BurstCompile]
    public struct SinglePolygonData : IDisposable
    {
        [BurstCompile]
        private struct HoleData
        {
            public int Length;
            public int StartIndex;
        }

        [BurstCompile]
        public struct Vertex
        {
            public int Index;
            public float2 Point;
        }

        public bool IsCreated { get => vertices.IsCreated; }
        private NativeList<Vertex> vertices;        
        public float2 this[int i] { get => vertices[i].Point; }
        public int VerticesNum { get => vertices.Length; }
        public int ContourPointsNum { get; private set; }       
        private NativeList<HoleData> holes;
        public int HolesNum { get => holes.Length; }

        public SinglePolygonData(Allocator allocator, Vector2[] contour, Vector2[][] holes = null)
        {
            this.vertices = new NativeList<Vertex>(allocator);
            this.holes = new NativeList<HoleData>(allocator);

            ContourPointsNum = contour.Length;
            for (int i = 0, index = vertices.Length; i < contour.Length; ++i, ++index)
            {
                vertices.Add(new Vertex
                {
                    Index = index,
                    Point = contour[i]
                });
            }

            if (holes == null)
            {
                return;
            }

            foreach (Vector2[] hole in holes)
            {
                AddHole(hole);
            }
        }

        public void AddHole(NativeArray<float2> hole)
        {
            holes.Add(new HoleData
            {
                Length = hole.Length,
                StartIndex = vertices.Length
            });

            for (int i = 0, index = vertices.Length; i < hole.Length; ++i, ++index)
            {
                vertices.Add(new Vertex
                {
                    Index = index,
                    Point = hole[i]
                });
            }
        }

        public void AddHole(Vector2[] hole)
        {
            holes.Add(new HoleData
            {
                Length = hole.Length,
                StartIndex = vertices.Length
            });

            for (int i = 0, index = vertices.Length; i < hole.Length; ++i, ++index)
            {
                vertices.Add(new Vertex
                {
                    Index = index,
                    Point = hole[i]
                });
            }
        }

        public NativeArray<Vertex> GetContourPoints()
        {
            return vertices.AsArray().GetSubArray(0, ContourPointsNum);
        }

        public NativeArray<Vertex> GetPolygonHole(int holeIndex)
        {
            if (holeIndex < 0 || holeIndex >= holes.Length)
            {
                return vertices.AsArray().GetSubArray(0, 0);
            }
            return vertices.AsArray().GetSubArray(holes[holeIndex].StartIndex, holes[holeIndex].Length);
        }

        public void ClearPolygons()
        {
            vertices.Clear();
            holes.Clear();
        }

        public void Dispose()
        {
            vertices.Dispose();
            holes.Dispose();
        }

        public float Area()
        {
            float result = 0f;
            //contour
            for (int ci = 0; ci < ContourPointsNum; ++ci)
            {
                int nextIndex = (ci + 1) % ContourPointsNum;
                // Shoelace formula
                result += this[ci].x * this[nextIndex].y;
                result -= this[ci].y * this[nextIndex].x;
            }            
            //holes
            for (int hi = 0; hi < holes.Length; ++hi)
            {
                int holeStartIndex = holes[hi].StartIndex;
                int holeLength = holes[hi].Length;

                for (int hci = 0; hci < holeLength; ++hci)
                {
                    int currIndex = hci + holeStartIndex;
                    int nextIndex = (hci + 1) % holeLength + holeStartIndex;
                    // Shoelace formula
                    result -= this[currIndex].x * this[nextIndex].y;
                    result += this[currIndex].y * this[nextIndex].x;
                }
            }
            return result;
        }
    }
}
