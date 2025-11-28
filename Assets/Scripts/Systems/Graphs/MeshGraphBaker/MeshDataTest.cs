using UnityEngine;

[ExecuteAlways]
public class MeshDataTest : MonoBehaviour {
    public MeshFilter filter;
    public int vertexCount = 0;
    public int triangleCount = 0;
    public int triangleIndex = 0;

    public int readingIndex = 0;
    public int v0, v1, v2;
    void Update() {
        if (filter == null) return;

        Mesh m = filter.sharedMesh;

        triangleCount = m.triangles.Length / 3;
        vertexCount = m.vertices.Length;

        readingIndex = triangleIndex * 3;
        readingIndex = Mathf.Clamp(triangleIndex, 0, (triangleCount-1)*3);

        v0 = m.triangles[readingIndex];
        v1 = m.triangles[readingIndex+1];
        v2 = m.triangles[readingIndex+2];
    }
}
