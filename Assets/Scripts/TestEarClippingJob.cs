using UnityEngine;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;

using dousi96.Geometry.Triangulator;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class TestEarClippingJob : MonoBehaviour
{
    MeshFilter filter;

    void Start()
    {
        filter = GetComponent<MeshFilter>();

        NativeArray<float2> verts = new NativeArray<float2>(4, Allocator.TempJob);
        verts[0] = new float2(0f, 0f);
        verts[1] = new float2(1f, 0f);
        verts[2] = new float2(1f, 1f);
        verts[3] = new float2(0f, 1f);

        int ntris = (verts.Length - 2) * 3;
        NativeArray<int> outTriangles = new NativeArray<int>(ntris, Allocator.TempJob);

        //creating the job
        EarClippingNoHolesJob triangulatorJob = new EarClippingNoHolesJob()
        {
            isCCW = true,
            InVerts = verts,
            OutTris = outTriangles
        };
        JobHandle handle = triangulatorJob.Schedule();       
        handle.Complete();

        Vector3[] vertices = new Vector3[verts.Length];
        for (int i = 0; i < verts.Length; ++i)
        {
            vertices[i] = new Vector3(verts[i].x, verts[i].y, 0f);
        }

        //get the job results
        int[] triangles = new int[outTriangles.Length];
        for (int i = 0; i < outTriangles.Length; ++i)
        {
            triangles[i] = outTriangles[i];
        }

        verts.Dispose();
        outTriangles.Dispose();

        filter.sharedMesh = new Mesh();
        filter.sharedMesh.vertices = vertices;
        filter.sharedMesh.triangles = triangles;

        filter.sharedMesh.RecalculateNormals();
    }
}
