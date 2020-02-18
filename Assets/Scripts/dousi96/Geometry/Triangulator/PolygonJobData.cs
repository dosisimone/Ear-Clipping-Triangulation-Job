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
        public readonly int NumContournPoints;
        [ReadOnly]
        public readonly NativeArray<int> StartPointsHoles;
        [ReadOnly]
        public readonly NativeArray<int> NumPointsPerHole;
        public int NumHoles { get => StartPointsHoles.Length; }

        public PolygonJobData(Vector2[] contourn, Allocator allocator)
        {
            Vertices = new NativeArray<float2>(contourn.Length, allocator);
            NumContournPoints = contourn.Length;
            StartPointsHoles = new NativeArray<int>(0, allocator);
            NumPointsPerHole = new NativeArray<int>(0, allocator);

            for (int i = 0; i < contourn.Length; ++i)
            {
                Vertices[i] = contourn[i];
            }
        }

        public PolygonJobData (Vector2[] contourn, Vector2[][]holes, Allocator allocator)
        {
            int vertsCount = contourn.Length;            
            for (int i = 0; i < holes.Length; ++i)
            {
                vertsCount += holes[i].Length;
            }

            Vertices = new NativeArray<float2>(vertsCount, allocator);
            NumContournPoints = contourn.Length;
            StartPointsHoles = new NativeArray<int>(holes.Length, allocator);
            NumPointsPerHole = new NativeArray<int>(holes.Length, allocator);

            for (int i = 0; i < contourn.Length; ++i)
            {
                Vertices[i] = contourn[i];
            }

            int startIndex = contourn.Length;
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
