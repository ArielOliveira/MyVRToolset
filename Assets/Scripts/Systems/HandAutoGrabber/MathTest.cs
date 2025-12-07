using System.Collections.Generic;
using System.IO;
using Arielado;
using Arielado.Graphs;
using Arielado.Math;
using Arielado.Math.Primitives;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

[ExecuteAlways,
 RequireComponent(typeof(MeshFilter))]
public class MathTest : MonoBehaviour {
    [SerializeField] private Mesh mesh;
    [SerializeField] private Vector3 circleTestPos, circleTestRot;
    [SerializeField] private float circleRadius, debugRadius;

    [SerializeField] private int testTriangle;

    [SerializeField] private bool reloadGraph;
    MeshTriangleGraph graph;

    private void OnDrawGizmos() {
        if (mesh == null) return;

        testTriangle = Mathf.Clamp(testTriangle, 0, graph.Size-1);

        Triangle tri = graph.triangles[testTriangle];

        Vector3 v0 = transform.TransformPoint(tri.v0);
        Vector3 v1 = transform.TransformPoint(tri.v1);
        Vector3 v2 = transform.TransformPoint(tri.v2);
        Vector3 triCenter = (v0 + v1 + v2) / 3f;
        Vector3 triNormal = transform.TransformDirection(tri.normal);

        Handles.color = Color.white;
        Gizmos.DrawLineStrip(new Vector3[] { v0, v1, v2}, true);
        Gizmos.DrawLine(triCenter, triCenter + (triNormal * 0.15f));

        Quaternion q = Quaternion.Euler(circleTestRot);
        Vector3 circleUp = q * Vector3.up;
        Vector3 circleForward = q * Vector3.forward;
        Vector3 circleNormal = q * Vector3.right;
        
        //////// Step 1: Circle intersects triangle plane ///////////////////////////
        Vector3 circleTriPlane = Vector3.Cross(triNormal, circleNormal);
        Vector3 circleTriPlaneNormalized = circleTriPlane.normalized;
        Vector3 circleToTriUp = Vector3.Cross(circleNormal, circleTriPlane).normalized;
    
        float trianglePlane = -Vector3.Dot(triNormal, v0);
        float normalAngle = Geometry.CirclePointToAngle(Vector3.zero, -triNormal, circleNormal, circleUp);
        Vector3 anglePoint = Geometry.CirclePointFromAngle(normalAngle, circleRadius, circleNormal, circleUp, circleTestPos);

        Vector3 l0 = circleTestPos + (circleToTriUp * circleRadius);
        Vector3 l1 = anglePoint;

        bool triPlaneIntersection = Geometry.LinePlaneIntersection(l0, l1, triCenter, -triNormal, out Vector3 iPoint, out float normalizedLinePoint);

        Gizmos.color = Color.black;
        Gizmos.DrawSphere(anglePoint, debugRadius);

        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.fontSize = 14;
        Handles.Label(anglePoint, normalAngle.ToString("0.00"), style);

        Gizmos.color = Color.yellowGreen;
        Gizmos.DrawLine(l0, l1);

        Handles.color = Color.yellow;
        Handles.DrawLine(circleTestPos, circleTestPos + (circleTriPlane * 0.15f));
        
        // Step 2: Find Circle-Plane Intersecting Points
        if (triPlaneIntersection) {
            Gizmos.DrawSphere(iPoint, debugRadius);

            Vector3 segment0 = iPoint - (circleTriPlaneNormalized * circleRadius);
            Vector3 segment1 = iPoint + (circleTriPlaneNormalized * circleRadius);

            Handles.color = Color.red;
            Handles.DrawLine(iPoint, segment0);

            Handles.color = Color.red;
            Handles.DrawLine(iPoint, segment1);

            Geometry.LineCircleIntersection(circleRadius, circleTestPos, segment0, segment1, circleNormal, circleUp, out Vector3 ip0, out Vector3 ip1);

            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(ip0, debugRadius);
            Gizmos.DrawSphere(ip1, debugRadius);

            float angle0 = Geometry.CirclePointToAngle(circleTestPos, ip0, circleNormal, circleUp);
            float angle1 = Geometry.CirclePointToAngle(circleTestPos, ip1, circleNormal, circleUp);

            Handles.Label(ip0, angle0.ToString("0.00"), style);
            Handles.Label(ip1, angle1.ToString("0.00"), style);

            // Step 3: Find if any of the points are inside the triangle
            bool isIP0Valid = Geometry.IsPointInsideTriangle(v0, v1, v2, ip0);
            bool isIP1Valid = Geometry.IsPointInsideTriangle(v0, v1, v2, ip1);

            Gizmos.color = Color.green;
            if (isIP0Valid)
                Gizmos.DrawSphere(ip0, debugRadius);
            if (isIP1Valid)
                Gizmos.DrawSphere(ip1, debugRadius);

            // Step 4: If both points are inside the triangle, stop here and return true.

            // Step 5: If one or none of the points are inside the triangle, check if the triangle 
            // area is within the circle radius.
        }
        /////////////////////////////////////////////////////////////////////////////////////////

        Handles.color = Color.cyan;
        Handles.DrawWireDisc(circleTestPos, circleNormal, circleRadius);

        Handles.color = Color.green;
        Handles.DrawLine(circleTestPos, circleTestPos + (circleUp * 0.15f));

        Handles.color = Color.red;
        Handles.DrawLine(circleTestPos, circleTestPos + (circleNormal * 0.15f));
        
        Handles.color = Color.blue;
        Handles.DrawLine(circleTestPos, circleTestPos + (circleForward * 0.15f));



        /*if (Geometry.LineCircleIntersection(circleRadius, circleTestPos, v0, v1, circleNormal, circleUp, out Vector3 p0, out Vector3 p1)) {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(p0, debugRadius);

            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(p1, debugRadius);
        }*/
    }


    private void Update() {
        if (mesh == null || reloadGraph || graph.Size < 1) {
            MeshFilter filter = gameObject.GetComponent<MeshFilter>();

            mesh = filter?.sharedMesh;
            
            string path = Paths.GetPersistentDir(Paths.TRIANGLE_GRAPHS + mesh.name + ".json");
            if (!File.Exists(path)) { Debug.Log(path + " File doesn't exists!"); return; }
            graph = JsonUtility.FromJson<MeshTriangleGraph>(File.ReadAllText(path));

            reloadGraph = false;
        }
    }
}
