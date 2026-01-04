using System.IO;
using UnityEngine;
using Arielado.Graphs;
using Arielado.Math.Primitives;
using UnityEditor;
using System.Collections.Generic;


namespace Arielado {
    public class FingerPointsGenerator : MonoBehaviour {
        [SerializeField] private Collider _collider;
        [SerializeField] private Mesh mesh;
        [SerializeField] private float debugRadius = 0.1f, fingerRadius = 0.2f;
        [SerializeField, ReadOnly] private int stepIndex = -1;
        [SerializeField] private bool useStepIndex, step = false;
        private MeshTriangleGraph graph;
        

        private void OnDrawGizmosSelected() {
            if (_collider == null || mesh == null) return;

            if (graph.triangles == null || graph.triangles.Length == 0) {
                string path = Paths.GetPersistentDir(Paths.TRIANGLE_GRAPHS + mesh.name + ".json");
                if (!File.Exists(path)) { Debug.Log(path + " File doesn't exists!"); return; }
                graph = JsonUtility.FromJson<MeshTriangleGraph>(File.ReadAllText(path));    
            }

            if (!useStepIndex) stepIndex = -1;

            Handles.color = Color.blue;
            Handles.DrawWireDisc(transform.position, transform.right, fingerRadius);

            Vector3 closestToFinger = _collider.ClosestPoint(transform.position);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(closestToFinger, debugRadius);

            int triIndex = useStepIndex && stepIndex >= 0 ? stepIndex : GetClosestTriangleToCircle(out Vector3 closestPointToFinger);
            Triangle tri = Triangle.Transform(graph.triangles[triIndex], _collider.transform);
            
            Vector3 triCenter = (tri.v0 + tri.v1 + tri.v2) / 3;

            Vector3 surfaceClockwiseDir = -Vector3.Cross(tri.normal, transform.right);

            Vector3[] pList = new Vector3[] { tri.v0, tri.v1, tri.v2 };

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(triCenter, triCenter + surfaceClockwiseDir * debugRadius);

            if (Math.Geometry.CircleTriangleIntersection(tri.v0, tri.v1, tri.v2, triCenter, tri.normal, transform.position, transform.up, transform.right, fingerRadius, out Vector3 i0, out Vector3 i1, out bool i0Intersects, out bool i1Intersects)) {
                Gizmos.color = Color.yellow;
                
                Gizmos.DrawLineStrip(pList, true);
                
                Gizmos.color = i0Intersects ? Color.yellow : Color.red;
                Gizmos.DrawWireSphere(i0, debugRadius * 0.5f);
                
                Gizmos.color = i1Intersects ? Color.yellow : Color.red;
                Gizmos.DrawWireSphere(i1, debugRadius * 0.5f);

                Vector3 intersectionPointCenter = (i0 + i1) * 0.5f;

                triIndex = StepForward(triIndex, intersectionPointCenter, surfaceClockwiseDir); 

                tri = graph.triangles[triIndex];

                tri.v0 = _collider.transform.TransformPoint(tri.v0);
                tri.v1 = _collider.transform.TransformPoint(tri.v1);
                tri.v2 = _collider.transform.TransformPoint(tri.v2);   

                pList = new Vector3[] { tri.v0, tri.v1, tri.v2 };

                Gizmos.color = Color.blue;
                Gizmos.DrawLineStrip(pList, true);

                if (step && useStepIndex) {
                    stepIndex = triIndex;
                    step = false;
                } 
            } else {
                Gizmos.color = Color.red;
                
                Gizmos.DrawLineStrip(pList, true);
                
                Gizmos.DrawWireSphere(i0, debugRadius);
                Gizmos.DrawWireSphere(i1, debugRadius);

                int neighbour = StepTowardsCircle(triIndex, triCenter);
                
                tri = Triangle.Transform(graph.triangles[neighbour], _collider.transform);
                triCenter = (tri.v0 + tri.v1 + tri.v2) / 3;

                pList = new Vector3[] { tri.v0, tri.v1, tri.v2 };

                if (Math.Geometry.CircleTriangleIntersection(tri.v0, tri.v1, tri.v2, triCenter, tri.normal, transform.position, transform.up, transform.right, fingerRadius, out i0, out i1, out i0Intersects, out i1Intersects)) {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLineStrip(pList, true);
                    
                    Gizmos.color = i0Intersects ? Color.yellow : Color.red;
                    Gizmos.DrawWireSphere(i0, debugRadius * 0.5f);

                    Gizmos.color = i1Intersects ? Color.yellow : Color.red;
                    Gizmos.DrawWireSphere(i1, debugRadius * 0.5f);

                    if (step && useStepIndex) {
                        stepIndex = neighbour;
                        step = false;
                    }
                } else {
                    Gizmos.color = Color.orangeRed;
                    Gizmos.DrawLineStrip(pList, true);
                    
                    Gizmos.color = i0Intersects ? Color.yellow : Color.red;
                    Gizmos.DrawWireSphere(i0, debugRadius * 0.5f);

                    Gizmos.color = i1Intersects ? Color.yellow : Color.red;
                    Gizmos.DrawWireSphere(i1, debugRadius * 0.5f);
                }
            }

            List<Vector3> points = GetFingerPoints();

            if (points == null) return;

            for (int i = 0; i < points.Count; i++) {
                Gizmos.color = Color.darkBlue;
                Gizmos.DrawLineStrip(points.ToArray(), false);
                Gizmos.DrawSphere(points[i], debugRadius * 0.5f);
            }
        }

        private int GetClosestTriangleToCircle(out Vector3 closestPointToFinger) {
            closestPointToFinger = _collider.ClosestPoint(transform.position);

            bool canReach = AStarSearch.GetPath(graph, _collider.transform, 0, closestPointToFinger, out List<PathNode> waypoint);

            return waypoint[waypoint.Count-1].index;
        }

        private List<Vector3> GetFingerPoints() {
            List<Vector3> points = new List<Vector3>();

            int index = GetClosestTriangleToCircle(out Vector3 closestPointToFinger);

            Triangle tri = Triangle.Transform(graph.triangles[index], _collider.transform);
            Vector3 triCenter = (tri.v0 + tri.v1 + tri.v2) / 3;

            if (!Math.Geometry.CircleTriangleIntersection(tri.v0, tri.v1, tri.v2, triCenter, tri.normal, transform.position, transform.up, transform.right, fingerRadius, out Vector3 i0, out Vector3 i1, out bool i0Intersects, out bool i1Intersects)) {
                index = StepTowardsCircle(index, triCenter);
                
                tri = Triangle.Transform(graph.triangles[index], _collider.transform);
                triCenter = (tri.v0 + tri.v1 + tri.v2) / 3;

                if (!Math.Geometry.CircleTriangleIntersection(tri.v0, tri.v1, tri.v1, triCenter, tri.normal, transform.position, transform.up, transform.right, fingerRadius, out i0, out i1, out i0Intersects, out i1Intersects)) return null;
            }

            HashSet<int> visited = new HashSet<int>();
            HashSet<Vector3> addedPoints = new HashSet<Vector3>();

            visited.Add(index);
            
            bool canStep = true;

            Gizmos.color = i0Intersects ? Color.darkBlue : Color.darkOrange;
            Gizmos.DrawWireSphere(i0, debugRadius);

            Gizmos.color = i1Intersects ? Color.darkBlue : Color.darkOrange;
            Gizmos.DrawWireSphere(i1, debugRadius);
            
            if (i0Intersects) {
                points.Add(i0);
                addedPoints.Add(i0);
            }

            if (i1Intersects) {
                points.Add(i1);
                addedPoints.Add(i1);
            }

            while (canStep) {
                canStep = false;

                Vector3 intersectionPointCenter = (i0 + i1) * 0.5f;
                Vector3 surfaceClockwiseDir = -Vector3.Cross(tri.normal, transform.right);

                int stepTriangle = StepForward(index, intersectionPointCenter, surfaceClockwiseDir);

                if (visited.Contains(stepTriangle)) break;

                tri = Triangle.Transform(graph.triangles[stepTriangle], _collider.transform);
                
                if (Math.Geometry.CircleTriangleIntersection(tri.v0, tri.v1, tri.v2, triCenter, tri.normal, transform.position, transform.up, transform.right, fingerRadius, out i0, out i1, out i0Intersects, out i1Intersects)) {
                    canStep = true;
                    index = stepTriangle;

                    Gizmos.color = i0Intersects ? Color.darkBlue : Color.darkOrange;
                    Gizmos.DrawWireSphere(i0, debugRadius);

                    Gizmos.color = i1Intersects ? Color.darkBlue : Color.darkOrange;
                    Gizmos.DrawWireSphere(i1, debugRadius);

                    if (i0Intersects && !addedPoints.Contains(i0)) {
                        points.Add(i0);
                        addedPoints.Add(i0);
                    } 
                    
                    if (i1Intersects && !addedPoints.Contains(i1)) {
                        points.Add(i1);
                        addedPoints.Add(i1);
                    }

                    visited.Add(stepTriangle);
                } 
            }

            return points;
        }



        private int StepTowardsCircle(int nodeIndex, Vector3 triCenter) {
            Vector3 targetDir = (transform.position - triCenter).normalized;

            TriangleNode node = graph.triangleNodes[nodeIndex];

            float score = -1f;
            TriangleEdge selectedEdge = graph.edges[TriangleNode.GetEdge(node, 0)];

            for (int i = 0; i < 3; i++) {
                TriangleEdge edge = graph.edges[TriangleNode.GetEdge(node, i)];

                Vector3 p0 = _collider.transform.TransformPoint(edge.line.p0);
                Vector3 p1 = _collider.transform.TransformPoint(edge.line.p1);
                Vector3 center = (p0 + p1) * 0.5f;
                Vector3 stepDir = (center - triCenter).normalized;

                float dot = Vector3.Dot(stepDir, targetDir);

                if (dot > score) {
                    score = dot;
                    selectedEdge = edge;
                }
            }

            return selectedEdge.triangle0 == nodeIndex ? selectedEdge.triangle1 : selectedEdge.triangle0;
        }

        private int StepForward(int nodeIndex, Vector3 circleTriPoint, Vector3 surfaceForward) {
            TriangleNode node = graph.triangleNodes[nodeIndex];

            float score = -1f;
            int selectedEdge = TriangleNode.GetEdge(node, 0);

            for (int i = 0; i < 3; i++) {
                TriangleEdge edge = graph.edges[TriangleNode.GetEdge(node, i)];

                Vector3 p0 = _collider.transform.TransformPoint(edge.line.p0);
                Vector3 p1 = _collider.transform.TransformPoint(edge.line.p1);
                Vector3 rayDir = _collider.transform.TransformDirection(-edge.line.direction);
                // We're ignoring the line side
                Vector3 pNormal = transform.right * Mathf.Sign(Vector3.Dot(rayDir, transform.right));

                if (Math.Geometry.LinePlaneIntersection(p0, p1, transform.position, pNormal, out Vector3 intersection, out float t)) {
                    Vector3 dir = (intersection - circleTriPoint).normalized;

                    float dot = Vector3.Dot(dir, surfaceForward);

                    if (dot > score) {
                        score = dot;
                        selectedEdge = edge.index;
                    }
                }
            }

            TriangleEdge e = graph.edges[selectedEdge];

            return e.triangle0 == nodeIndex ? e.triangle1 : e.triangle0;
        }
    }
}