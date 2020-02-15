using UnityEngine;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;

using dousi96.Geometry.Triangulator;
using JacksonDunstan.NativeCollections;
using System.Collections;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class TestEarClippingJob : MonoBehaviour
{
    MeshFilter filter;

    NativeArray<float2> verts;
    NativeArray<int> outTriangles;
    NativeArray<int> startPointsHoles;
    NativeArray<int> numPointsPerHole;
    NativeLinkedList<int> ll;

    JobHandle handleTriangulatorJob;

    private void Start()
    {
        filter = GetComponent<MeshFilter>();
    }

    void Update()
    {       
        TestEarClippingWithJob();
    }

    private void TestEarClippingWithJob()
    {
        int nHoles = 2;
        int ncontourPoints = 5;

        startPointsHoles = new NativeArray<int>(nHoles, Allocator.TempJob);
        startPointsHoles[0] = 5;
        startPointsHoles[1] = 8;
        numPointsPerHole = new NativeArray<int>(nHoles, Allocator.TempJob);
        numPointsPerHole[0] = 3;
        numPointsPerHole[1] = 4;
        
        verts = new NativeArray<float2>(12, Allocator.TempJob);
        //contourn
        verts[0] = new float2(0f, 0f);
        verts[1] = new float2(2f, 0f);
        verts[2] = new float2(2f, 2f);
        verts[3] = new float2(0f, 2f);
        verts[4] = new float2(-1f, 3f);
        //hole 1
        verts[5] = new float2(0.25f, 0.25f);
        verts[6] = new float2(0.25f, 0.5f);
        verts[7] = new float2(0.5f, 0.25f);
        //hole 2
        verts[8] = new float2(1f, 1f);
        verts[9] = new float2(1f, 1.5f);
        verts[10] = new float2(1.5f, 1.5f);
        verts[11] = new float2(1.5f, 1f);

        int totNumVerts = nHoles * 2 + verts.Length;
        int ntris = (totNumVerts - 2) * 3;
        outTriangles = new NativeArray<int>(ntris, Allocator.TempJob);
        ll = new NativeLinkedList<int>(totNumVerts, Allocator.TempJob);

        //creating the jobs
        EarClippingRemoveHolesJob removeHolesJob = new EarClippingRemoveHolesJob()
        {
            isCCW = true,
            Vertices = verts,
            NumContournPoints = ncontourPoints,
            StartPointsHoles = startPointsHoles,
            NumPointsPerHole = numPointsPerHole,
            VertexIndexLinkedList = ll
        };

        EarClippingTriangulatorJob triangulatorJob = new EarClippingTriangulatorJob()
        {
            isCCW = true,
            Vertices = verts,
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
        Vector3[] vertices = new Vector3[verts.Length];
        for (int i = 0; i < verts.Length; ++i)
        {
            vertices[i] = new Vector3(verts[i].x, verts[i].y, 0f);
        }

        int[] triangles = new int[outTriangles.Length];
        for (int i = 0; i < outTriangles.Length; ++i)
        {
            triangles[i] = outTriangles[i];
        }

        verts.Dispose();
        startPointsHoles.Dispose();
        numPointsPerHole.Dispose();
        outTriangles.Dispose();
        ll.Dispose();

        filter.sharedMesh = new Mesh();
        filter.sharedMesh.vertices = vertices;
        filter.sharedMesh.triangles = triangles;

        filter.sharedMesh.RecalculateNormals();
    }
}
