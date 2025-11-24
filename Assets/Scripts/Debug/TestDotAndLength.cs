using UnityEngine;
using Arielado.Math;

[ExecuteAlways]
public class TestDotAndLength : MonoBehaviour {
    public Transform rayDir;

    public Vector3 v0 = new Vector3(-0.5f, 0, 0), 
                    v1 = new Vector3(0.5f, 0, 0),
                    v2 = new Vector3(0, 0.5f, 0);

    [Range(0.01f, 1f)] public float debugSphereSize = 0.2f;

    public float dotLength, magnitude;
    public float u, v, w, t;
    public bool intersected = false;

    private void OnDrawGizmos() {
        Gizmos.color = Color.white;
        Vector3 v0Offset = transform.TransformPoint(v0);
        Vector3 v1Offset = transform.TransformPoint(v1);
        Vector3 v2Offset = transform.TransformPoint(v2);

        Gizmos.DrawSphere(v0Offset, debugSphereSize);
        Gizmos.DrawSphere(v1Offset, debugSphereSize);
        Gizmos.DrawSphere(v2Offset, debugSphereSize);


        if (rayDir == null) return;

        Gizmos.color = Color.gray;
        Gizmos.DrawLine(rayDir.position, rayDir.position + rayDir.forward * float.MaxValue);

        intersected = Geometry.RayTriangleIntersection(rayDir.position, rayDir.forward,
                                         v0Offset, v1Offset, v2Offset, 
                                         out Vector3 intersection, out t, out u, out v, out w);

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(intersection, debugSphereSize);
    }

    private void Update() {
        magnitude = transform.position.magnitude;
        dotLength = Mathf.Sqrt(Vector3.Dot(transform.position, transform.position));
    }
}
