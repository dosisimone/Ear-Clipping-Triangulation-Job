using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using dousi96.Geometry;
using dousi96.Geometry.Triangulator;
using System.IO;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class TestEarClippingJob : MonoBehaviour
{
    MeshFilter filter;

    private void Start()
    {
        filter = GetComponent<MeshFilter>();

        TestEarClippingWithJob();
    }

    private void TestEarClippingWithJob()
    {
        //Vector2[] contour =
        //{
        //    new Vector2(-2f, -2f),
        //    new Vector2(+2f, -2f),
        //    new Vector2(+2f, +2f),
        //    new Vector2(-2f, +2f),
        //};

        //Vector2[][] holes = new Vector2[][]
        //{
        //    new Vector2[]
        //    {
        //        new Vector2(+1f, +1.5f),
        //        new Vector2(+1f, +1.75f),
        //        new Vector2(+1.5f, +1.5f),
        //    },
        //    new Vector2[]
        //    {
        //        new Vector2(-1.5f, +1.1f),
        //        new Vector2(-1.5f, +1.4f),
        //        new Vector2(-1.25f, +1.1f),
        //    },

        //    new Vector2[]
        //    {
        //        new Vector2(-0.5f, +0f),
        //        new Vector2(-0.5f, +0.5f),
        //        new Vector2(+0f, +0f),
        //    }
        //};



        string json = File.ReadAllText("./Assets/1731.dat");
        var flatpolygon = JsonUtility.FromJson<PolygonSerializer.FlatPolygon>(json);

        int numberOfHoles = flatpolygon.holeIndices.Length;
        //int numberOfHoles = 200;

        Vector2[] contour = new Vector2[flatpolygon.holeIndices[0] - 1];
        Vector2[][] holes = new Vector2[numberOfHoles][];

        for (int i = 0; i < flatpolygon.holeIndices[0] - 1; i++)
        {
            contour[i].x = flatpolygon.PolygonData[i].x / 10000;
            contour[i].y = flatpolygon.PolygonData[i].y / 10000;
        }
        List<Vector2> temp = new List<Vector2>();
        temp.AddRange(contour);
        temp.Reverse();
        contour = temp.ToArray();

        for (int i = 0; i < numberOfHoles; i++)
        {
            int start = flatpolygon.holeIndices[i];
            int stop = i < flatpolygon.holeIndices.Length-1 ? flatpolygon.holeIndices[i+1] : flatpolygon.PolygonData.Length;
            holes[i] = new Vector2[stop - start];
            for (int j = start; j < stop; j++)
            {
                holes[i][j- start].x = flatpolygon.PolygonData[j].x / 10000;
                holes[i][j- start].y = flatpolygon.PolygonData[j].y / 10000;
            }
            temp = new List<Vector2>();
            temp.AddRange(holes[i]);
            temp.Reverse();
            holes[i] = temp.ToArray();
        }


        SinglePolygonData polygon = new SinglePolygonData(Allocator.TempJob, contour, holes);

        int totNumVerts = polygon.HolesNum * 2 + polygon.VerticesNum;
        int ntris = (totNumVerts - 2) * 3;
        NativeArray<int> outTriangles = new NativeArray<int>(ntris, Allocator.TempJob);

        ECTriangulatorJob triangulatorJob = new ECTriangulatorJob()
        {
            Polygon = polygon,
            OutTriangles = outTriangles
        };

        //schedule the jobs
        JobHandle handleTriangulatorJob = triangulatorJob.Schedule();
        handleTriangulatorJob.Complete();

        //get the job results
        Vector3[] vertices = new Vector3[polygon.VerticesNum];
        for (int i = 0; i < polygon.VerticesNum; ++i)
        {
            vertices[i] = new Vector3(polygon[i].x, polygon[i].y, 0f);
        }

        int[] triangles = new int[outTriangles.Length];
        for (int i = 0; i < outTriangles.Length; ++i)
        {
            triangles[i] = outTriangles[i];
        }

        polygon.Dispose();
        outTriangles.Dispose();

        filter.sharedMesh = new Mesh();
        filter.sharedMesh.vertices = vertices;
        filter.sharedMesh.triangles = triangles;

        filter.sharedMesh.RecalculateNormals();
    }
}
