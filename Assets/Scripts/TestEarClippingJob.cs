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

        NativeArray<float2> verts = new NativeArray<float2>(8, Allocator.TempJob);
        verts[7] = new float2(0f, 0f);
        verts[6] = new float2(1f, 0f);
        verts[5] = new float2(1f, 1f);
        verts[4] = new float2(2f, 1f);
        verts[3] = new float2(2f, 3f);
        verts[2] = new float2(0f, 2f);
        verts[1] = new float2(0.5f, 1.25f);
        verts[0] = new float2(-0.5f, 1.5f);

        int ntris = (verts.Length - 2) * 3;
        NativeArray<int> tris = new NativeArray<int>(ntris, Allocator.TempJob);
        NativeQueue<int> indexesQueue = new NativeQueue<int>(Allocator.TempJob);

        //creating the job
        EarClippingNoHolesJob triangulatorJob = new EarClippingNoHolesJob()
        {
            isCCW = false,
            InPoints = verts,
            OutTris = tris
        };
        JobHandle handle = triangulatorJob.Schedule();       
        handle.Complete();

        Vector3[] vertices = new Vector3[verts.Length];
        for (int i = 0; i < verts.Length; ++i)
        {
            vertices[i] = new Vector3(verts[i].x, verts[i].y, 0f);
        }

        //get the job results
        int[] triangles = new int[tris.Length];
        for (int i = 0; i < tris.Length; ++i)
        {
            triangles[i] = tris[i];
        }

        verts.Dispose();
        indexesQueue.Dispose();
        tris.Dispose();

        filter.sharedMesh = new Mesh();
        filter.sharedMesh.vertices = vertices;
        filter.sharedMesh.triangles = triangles;

        filter.sharedMesh.RecalculateNormals();
    }
}
