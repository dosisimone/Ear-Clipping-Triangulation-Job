using System;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;

namespace dousi96.Geometry.Triangulator
{
    [BurstCompile]
    public struct PolygonJobData : IDisposable
    {
        [ReadOnly]
        public readonly NativeArray<float2> Vertices;
        public int NumTotVertices { get => Vertices.Length; }
        [ReadOnly]
        public readonly int NumContourPoints;
        [ReadOnly]
        public readonly NativeArray<int> StartPointsHoles;
        [ReadOnly]
        public readonly NativeArray<int> NumPointsPerHole;
        public int NumHoles { get => StartPointsHoles.Length; }

        public PolygonJobData(Vector2[] contour, Allocator allocator)
        {
            Vertices = new NativeArray<float2>(contour.Length, allocator);
            NumContourPoints = contour.Length;
            StartPointsHoles = new NativeArray<int>(0, allocator);
            NumPointsPerHole = new NativeArray<int>(0, allocator);

            for (int i = 0; i < contour.Length; ++i)
            {
                Vertices[i] = contour[i];
            }
        }

        public PolygonJobData (Vector2[] contour, Vector2[][] holes, Allocator allocator)
        {
            int vertsCount = contour.Length;            
            for (int i = 0; i < holes.Length; ++i)
            {
                vertsCount += holes[i].Length;
            }

            Vertices = new NativeArray<float2>(vertsCount, allocator);
            NumContourPoints = contour.Length;
            StartPointsHoles = new NativeArray<int>(holes.Length, allocator);
            NumPointsPerHole = new NativeArray<int>(holes.Length, allocator);

            for (int i = 0; i < contour.Length; ++i)
            {
                Vertices[i] = contour[i];
            }

            int startIndex = contour.Length;
            for (int i = 0; i < holes.Length; ++i)
            {
                StartPointsHoles[i] = startIndex;
                for (int j = 0; j < holes[i].Length; ++j)
                {
                    Vertices[startIndex + j] = holes[i][j];
                }
                NumPointsPerHole[i] = holes[i].Length;
                startIndex += holes[i].Length;
            }
        }

        public void Dispose()
        {
            Vertices.Dispose();
            StartPointsHoles.Dispose();
            NumPointsPerHole.Dispose();
        }

        public float2 GetHolePoint (int holeIndex, int pointIndex)
        {
            return Vertices[StartPointsHoles[holeIndex] + pointIndex];
        }
    }
}
