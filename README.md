# Ear Clipping Triangulation Job

Implementation of the Triangulation by Ear Clipping inside the Job System.
The basic algorithm is explained in [this paper](http://www.geometrictools.com/Documentation/TriangulationByEarClipping.pdf).

## How to use it

Initialize the ```PolygonJobData``` structure using one of the 2 constructors available.

```C#
PolygonJobData (Vector2[] contour, Allocator allocator)
PolygonJobData (Vector2[] contour, Vector2[][] holes, Allocator allocator)
```

The contour points array must be in **counter-clockwise order**.

The points of every hole inside the holes array must be in **clockwise order**.

Define the ```NativeArray``` that will contain the triangles indexes as the output of the job.

```C#
int totNumVerts = polygon.NumHoles * 2 + polygon.NumTotVertices;
int ntris = (totNumVerts - 2) * 3;
NativeArray<int> outTriangles = new NativeArray<int>(ntris, Allocator.TempJob);
```

Create and schedule the ```EarClippingTriangulatorJob```.

```C#
EarClippingTriangulatorJob triangulatorJob = new EarClippingTriangulatorJob()
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

Consider to take a look at the [TestEarClippingJob script](./Assets/Scripts/TestEarClippingJob.cs) to see the algorithm in action.

### Limitations

The implementation of this algorithm inside the Unity's job system is based on the [Jackson Dunstan](http://github.com/jacksondunstan) implementation of the Linked List data structure as a native collection.
[Here the link](http://github.com/jacksondunstan/NativeCollections) to the Jackson Dunstan repository containing his implementation of the Linked List data structure and many other data structures.
To use this implementation of the Triangulation by Ear Clipping you need to enable the "unsafe" code execution inside the Unity project settings.