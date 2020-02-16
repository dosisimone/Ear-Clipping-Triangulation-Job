using System.Collections;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;

using dousi96.Geometry.Triangulator;

using JacksonDunstan.NativeCollections;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class TestEarClippingJob : MonoBehaviour
{
    MeshFilter filter;

    PolygonJobData polygon;
    NativeArray<int> outTriangles;
    NativeLinkedList<int> ll;

    JobHandle handleTriangulatorJob;

    private void Start()
    {
        filter = GetComponent<MeshFilter>();

        TestEarClippingWithJob();
    }

    private void TestEarClippingWithJob()
    {
        Vector2[] contourn1 =
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
                new Vector2(-1f, -1f),
                new Vector2(-1f, +1f),
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
        };

        polygon = new PolygonJobData(contourn1, holes, Allocator.TempJob);

        int totNumVerts = polygon.NumHoles * 2 + polygon.NumTotVertices;
        int ntris = (totNumVerts - 2) * 3;
        outTriangles = new NativeArray<int>(ntris, Allocator.TempJob);
        ll = new NativeLinkedList<int>(totNumVerts, Allocator.TempJob);

        //creating the jobs 
        EarClippingRemoveHolesJob removeHolesJob = new EarClippingRemoveHolesJob()
        {
            Polygon = polygon,
            VertexIndexLinkedList = ll
        };

        EarClippingTriangulatorJob triangulatorJob = new EarClippingTriangulatorJob()
        {
            Polygon = polygon,
            VertexIndexLinkedList = ll,
            OutTris = outTriangles
        };

        //schedule the jobs
        JobHandle handleRemoveHolesJob = removeHolesJob.Schedule();
        handleTriangulatorJob = triangulatorJob.Schedule(handleRemoveHolesJob);
        handleTriangulatorJob.Complete();

        StartCoroutine(WaitForTriangulationEnd());
    }


    IEnumerator WaitForTriangulationEnd()
    {
        while (!handleTriangulatorJob.IsCompleted)
        {
            yield return null;
        }

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
        ll.Dispose();

        filter.sharedMesh = new Mesh();
        filter.sharedMesh.vertices = vertices;
        filter.sharedMesh.triangles = triangles;

        filter.sharedMesh.RecalculateNormals();
    }
}
