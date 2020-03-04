# Ear Clipping Triangulation Job

Implementation of the Triangulation by Ear Clipping inside Unity's Job System.
The basic algorithm is explained in [this paper](http://www.geometrictools.com/Documentation/TriangulationByEarClipping.pdf).

## How to use it

Define and init the ```PolygonJobData``` structure using the constructor available.

```C#
PolygonJobData (Allocator allocator, Vector2[] contourn, Vector2[][] holes = null)
```

The contourn points array must be in **counter clockwise order**.

The points of every hole inside the holes array must be in **clockwise order**.

Define the ```NativeArray``` that will contain the triangles indexes as output of the job.

```C#
int totNumVerts = polygon.HolesNum * 2 + polygon.VerticesNum;
int ntris = (totNumVerts - 2) * 3;
NativeArray<int> outTriangles = new NativeArray<int>(ntris, Allocator.TempJob);
```

Create and schedule the ```EarClippingTriangulatorJob```.

```C#
ECTriangulatorJob triangulatorJob = new ECTriangulatorJob()
{
    Polygon = polygon,
    OutTriangles = outTriangles
};
JobHandle handle = triangulatorJob.Schedule();
```

When the job is completed you can get the triangles.

```C#
int[] triangles = new int[outTriangles.Length];
for (int i = 0; i < outTriangles.Length; ++i)
{
    triangles[i] = outTriangles[i];
}  
```

Dispose the polygon data structure and the triangles native collection used previously.

```C#
polygon.Dispose();
outTriangles.Dispose();
```

Consider to take a look to the [TestEarClippingJob script](./Assets/Scripts/TestEarClippingJob.cs) to see the algorithm in action.

### Notes

The algorithm is implemented using the ```NativeLinkedList<T>``` created by [Jackson Dunstan](http://github.com/jacksondunstan) in [his repository about Native Collections](http://github.com/jacksondunstan/NativeCollections).
To use this code you will need to enable the "unsafe" code execution inside the project settings panel.