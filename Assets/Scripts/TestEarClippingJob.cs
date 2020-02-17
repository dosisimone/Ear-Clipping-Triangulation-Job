using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using dousi96.Geometry.Triangulator;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class TestEarClippingJob : MonoBehaviour
{
    MeshFilter filter;

    PolygonJobData polygon;
    NativeArray<int> outTriangles;

    JobHandle handleTriangulatorJob;

    private void Start()
    {
        filter = GetComponent<MeshFilter>();

        TestEarClippingWithJob();
    }

    private void TestEarClippingWithJob()
    {
        Vector2[] contourn =
        {
            new Vector2(-2f, -2f),
            new Vector2(+2f, -2f),
            new Vector2(+3f, +0f),
            new Vector2(+2f, +2f),
            new Vector2(-2f, +2f),
            new Vector2(-3f, +0f),
        };

        Vector2[][] holes = new Vector2[][]
        {
            new Vector2[]
            {
                new Vector2(-1f, -0.75f),
                new Vector2(-1f, +0.75f),
                new Vector2(+0f, +1f),
                new Vector2(-0.5f, 0f),
                new Vector2(+0f, -1f),
            },
            new Vector2[]
            {
                new Vector2(+1f, +1f),
                new Vector2(+2f, +0f),
                new Vector2(+1f, -1f),
            },
            new Vector2[]
            {
                new Vector2(-1.25f, +0.5f),
                new Vector2(-1.5f, +0f),
                new Vector2(-1.25f, -0.5f),
                new Vector2(-2.5f, +0f),                
            },
        };

        PolygonJobData polygon = new PolygonJobData(contourn, holes, Allocator.TempJob);

        int totNumVerts = polygon.NumHoles * 2 + polygon.NumTotVertices;
        int ntris = (totNumVerts - 2) * 3;
        NativeArray<int> outTriangles = new NativeArray<int>(ntris, Allocator.TempJob);

        EarClippingTriangulatorJob triangulatorJob = new EarClippingTriangulatorJob()
        {
            Polygon = polygon,
            OutTriangles = outTriangles
        };

        //schedule the jobs
        JobHandle handleTriangulatorJob = triangulatorJob.Schedule();
        handleTriangulatorJob.Complete();

        //get the job results
        Vector3[] vertices = new Vector3[polygon.NumTotVertices];
        for (int i = 0; i < polygon.NumTotVertices; ++i)
        {
            vertices[i] = new Vector3(polygon.Vertices[i].x, polygon.Vertices[i].y, 0f);
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
