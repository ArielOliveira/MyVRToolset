
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Arielado;
using Arielado.Graphs;
using Arielado.Math;
using Arielado.Math.Primitives;
using UnityEditor;
using UnityEngine;

[ExecuteAlways,
 RequireComponent(typeof(MeshFilter))]
public class MathTest : MonoBehaviour {
    [SerializeField] private Mesh mesh;
    [SerializeField] private Vector3 circleTestPos, circleTestRot;
    [SerializeField] private float circleRadius, debugRadius, angleTestRot;
    [SerializeField] private int testTriangle;
    [SerializeField] private List<int> validTriangles;

    [SerializeField] private bool reloadGraph, goToSelected, useClosestToCenter;
    [ReadOnly, SerializeField] private int neighbourCount;
    private MeshTriangleGraph graph;
    private Triangle current;


    private void OnDrawGizmos() {
        if (mesh == null) return;
        Triangle tri;
        Vector3 v0, v1, v2, i0, i1;
        Vector3 circleNormal, circleForward, circleUp, selectedTriClosestPoint;
        Quaternion circleRot = Quaternion.Euler(circleTestRot);
        circleNormal = circleRot * -Vector3.right;
        bool intersects, i0Intersects, i1Intersects;        

        tri = graph.triangles[testTriangle];

        v0 = transform.TransformPoint(tri.v0);
        v1 = transform.TransformPoint(tri.v1);
        v2 = transform.TransformPoint(tri.v2);
        Vector3 triCenter = (v0 + v1 + v2) / 3f;
        selectedTriClosestPoint = triCenter;
        Vector3 triNormal = transform.TransformDirection(tri.normal);

        if (useClosestToCenter) {
            float distance = float.PositiveInfinity;

            for (int i = 0; i < graph.triangles.Length; i++) {
                Vector3 closestPoint = Triangle.ClosestPointTo(graph.triangles[i], circleTestPos, transform);
                float testDistance = Vector3.Distance(closestPoint, circleTestPos);

                if (testDistance < distance) {
                    distance = testDistance;
                    testTriangle = i;
                    selectedTriClosestPoint = closestPoint;
                }
            }
        }

        Vector3 circleTriPlane = Vector3.Cross(triNormal, circleNormal).normalized;
        Vector3 circleTriUp = Vector3.Cross(circleNormal, circleTriPlane).normalized;

        intersects = ComputeTriangle(testTriangle, ref graph, out i0, out i1, out i0Intersects, out i1Intersects);

        Vector3 closest = Geometry.ClosestPointTo(circleTestPos, i0, i1);
        Vector3 farthest = Geometry.FarthestPointTo(circleTestPos, i0, i1);

        circleUp = circleTriUp;
        circleForward = -circleTriPlane;
        //circleForward = (circleTestPos - farthest).normalized;
        //circleUp = Vector3.Cross(circleForward, circleNormal);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(circleTestPos + (circleUp * circleRadius), circleTestPos - (circleUp * circleRadius));

        Vector3 point = Quaternion.Euler(angleTestRot, 0, 0) * circleForward;

        Gizmos.color = Color.saddleBrown;
        Gizmos.DrawSphere(circleTestPos + point, debugRadius);

        float upAngle = Split180(Geometry.CirclePointToAngle(circleTestPos, circleTestPos + circleUp, circleNormal, circleUp));
        float downAngle = Split180(Geometry.CirclePointToAngle(circleTestPos, circleTestPos - circleUp, circleNormal, circleUp));
        float forwardAngle = Split180(Geometry.CirclePointToAngle(circleTestPos, circleTestPos + circleForward, circleNormal, circleUp));
        float backAngle = Split180(Geometry.CirclePointToAngle(circleTestPos, circleTestPos - circleForward, circleNormal, circleUp));
        float testAngle = Split180(Geometry.CirclePointToAngle(circleTestPos, circleTestPos + point, circleNormal, circleUp));
        float closestPointAngle = Split180(Geometry.CirclePointToAngle(circleTestPos, closest, circleNormal,  circleUp));

        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.fontSize = 18;

        style.normal.textColor = Color.yellow;
        Handles.Label(circleTestPos + circleUp, upAngle.ToString("0"), style);
        Handles.Label(circleTestPos - circleUp, downAngle.ToString("0"), style);
        Handles.Label(circleTestPos + circleForward, forwardAngle.ToString("0"), style);
        Handles.Label(circleTestPos - circleForward, backAngle.ToString("0"), style);
        Handles.Label(circleTestPos + point, testAngle.ToString("0"), style);

        style.normal.textColor = Color.white;

        Color i0Color, i1Color;
        
        testTriangle = Mathf.Clamp(testTriangle, 0, graph.Size-1);

        int[] neighbours = graph.GetNodeNeighbours(testTriangle);
        neighbourCount = neighbours.Length;
        float highestAngle = float.NegativeInfinity;
        float selectedAngle = closestPointAngle;
        float highestDot = float.NegativeInfinity;

        int selectedTri = -1;
        Vector3 selectedPoint = selectedTriClosestPoint;
        Gizmos.color = Color.black;
        Gizmos.DrawWireSphere(selectedPoint, debugRadius);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(circleTestPos, circleTestPos + (circleForward * 0.1f));

        Gizmos.color = Color.red;
        Gizmos.DrawLine(circleTestPos, circleTestPos + (circleNormal * 0.1f));

        for (int i = 0; i < neighbours.Length; i++) {
            tri = graph.triangles[neighbours[i]];

            v0 = transform.TransformPoint(tri.v0);
            v1 = transform.TransformPoint(tri.v1);
            v2 = transform.TransformPoint(tri.v2);

            intersects = ComputeTriangle(neighbours[i], ref graph, out i0, out i1, out i0Intersects, out i1Intersects);

            i0Color = i0Intersects ? Color.yellowGreen : Color.orange;
            i1Color = i1Intersects ? Color.yellowGreen : Color.red;

            float angle0 = Split180(Geometry.CirclePointToAngle(circleTestPos, i0, circleNormal, circleUp));
            float angle1 = Split180(Geometry.CirclePointToAngle(circleTestPos, i1, circleNormal, circleUp));

            float dot0   = Vector3.Dot(circleForward, (i0 - selectedPoint).normalized);
            float dot1   = Vector3.Dot(circleForward, (i1 - selectedPoint).normalized);

            if (intersects) {    
                Debug.Log(neighbours[i] + "|" + dot0.ToString("0.00") + "|" + dot1.ToString("0.00"));

                if (i0Intersects && dot0 > highestDot) {
                    selectedTri = neighbours[i];
                    highestAngle = angle0;
                    selectedPoint = i0;    
                    selectedAngle = angle0;
                    highestDot = dot0;
                }

                if (i1Intersects && dot1 > highestDot) {
                    selectedTri = neighbours[i];
                    highestAngle = angle1;
                    selectedPoint = i1;  
                    selectedAngle = angle1;  
                    highestDot = dot1;
                }                
            }

            Handles.Label(i0, dot0.ToString("0.00"), style);
            Handles.Label(i1, dot1.ToString("0.00"), style);

            Gizmos.color = i0Color;
            Gizmos.DrawSphere(i0, debugRadius);

            Gizmos.color = i1Color;
            Gizmos.DrawSphere(i1, debugRadius);

            Gizmos.color = intersects ? Color.yellowGreen : Color.red;
            Gizmos.DrawLineStrip(new Vector3[] { v0, v1, v2 }, true);
        }

        //style.normal.textColor = Color.red;
        //Handles.Label(closest, closestPointAngle.ToString("0.00"), style);
        //style.normal.textColor = Color.magenta;
        //Handles.Label(selectedPoint, selectedAngle.ToString("0.00"), style);

        tri = graph.triangles[testTriangle];

        v0 = transform.TransformPoint(tri.v0);
        v1 = transform.TransformPoint(tri.v1);
        v2 = transform.TransformPoint(tri.v2);
        circleNormal = Quaternion.Euler(circleTestRot) * Vector3.right;

        intersects = ComputeTriangle(testTriangle, ref graph, out i0, out i1, out i0Intersects, out i1Intersects);

        Color circle = intersects ? Color.blue : Color.red;

        i0Color = i0Intersects ? Color.green : Color.red;
        i1Color = i1Intersects ? Color.green : Color.red;

        Handles.color = circle;
        Handles.DrawWireDisc(circleTestPos, circleNormal, circleRadius);

        //Gizmos.color = i0Color;
        //Gizmos.DrawSphere(i0, debugRadius);

        //Gizmos.color = i1Color;
        //Gizmos.DrawSphere(i1, debugRadius);

        Gizmos.color = intersects ? Color.green : Color.red;
        Gizmos.DrawLineStrip(new Vector3[] { v0, v1, v2 }, true);

        if (selectedTri >= 0) {
            tri = graph.triangles[selectedTri];

            v0 = transform.TransformPoint(tri.v0);
            v1 = transform.TransformPoint(tri.v1);
            v2 = transform.TransformPoint(tri.v2);
            
            Gizmos.color = Color.magenta;
            Gizmos.DrawLineStrip(new Vector3[] { v0, v1, v2 }, true);

            Gizmos.color = i0Color;
            Gizmos.DrawWireSphere(selectedPoint, debugRadius + debugRadius);

            if (goToSelected) {
                testTriangle = selectedTri;
                goToSelected = false;
            }
        }
    }

    private bool ComputeTriangle(int triangle, ref MeshTriangleGraph graph, out Vector3 i0, out Vector3 i1, out bool i0Intersects, out bool i1Intersects) {
        triangle = Mathf.Clamp(triangle, 0, graph.Size-1);

        Triangle tri = graph.triangles[triangle];

        Vector3 v0 = transform.TransformPoint(tri.v0);
        Vector3 v1 = transform.TransformPoint(tri.v1);
        Vector3 v2 = transform.TransformPoint(tri.v2);
        Vector3 triCenter = (v0 + v1 + v2) / 3f;
        Vector3 triNormal = transform.TransformDirection(tri.normal);

        Quaternion q = Quaternion.Euler(circleTestRot);
        Vector3 circleUp = q * Vector3.up;
        Vector3 circleNormal = q * Vector3.right;

        return Geometry.CircleTriangleIntersection(v0, v1, v2, triCenter, triNormal, circleTestPos, circleUp, circleNormal, circleRadius, 
                                            out i0, out i1, out i0Intersects, out i1Intersects);
    }

    private float Split180(float angle) =>
        angle > 180f ? (angle - 180f) * -1 : angle;

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
#endif