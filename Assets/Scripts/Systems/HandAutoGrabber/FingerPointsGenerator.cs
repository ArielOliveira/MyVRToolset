using System.IO;
using UnityEngine;
using Arielado.Graphs;
using Arielado.Math.Primitives;
using UnityEditor;
using System.Collections.Generic;
using Arielado.Math;


namespace Arielado {
    public class FingerPointsGenerator : MonoBehaviour {
        [SerializeField] private Collider _collider;
        [SerializeField] private Mesh mesh;
        [SerializeField] private float debugRadius = 0.1f, fingerRadius = 0.2f;
        [SerializeField, ReadOnly] private int stepIndex = -1;
        [SerializeField] private int testTriangle = 0;
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

                StepForward(triIndex, intersectionPointCenter, surfaceClockwiseDir, out triIndex); 

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
                
                Handles.Label(points[i] + transform.up * 0.1f, (i + 1).ToString() + "|" + points.Count.ToString());
            }
        }

        /*private void OnDrawGizmosSelected() {
            if (_collider == null || mesh == null) return;

            if (graph.triangles == null || graph.triangles.Length == 0) {
                string path = Paths.GetPersistentDir(Paths.TRIANGLE_GRAPHS + mesh.name + ".json");
                if (!File.Exists(path)) { Debug.Log(path + " File doesn't exists!"); return; }
                graph = JsonUtility.FromJson<MeshTriangleGraph>(File.ReadAllText(path));    
            }

            Handles.color = Color.blue;
            Handles.DrawWireDisc(transform.position, transform.right, fingerRadius);

            testTriangle = Mathf.Clamp(testTriangle, 0, graph.triangles.Length-1);

            Triangle tri = Triangle.Transform(graph.triangles[testTriangle], _collider.transform);
            Vector3 triCenter = (tri.v0 + tri.v1 + tri.v2) / 3f;

            Vector3[] pList = new Vector3[] { tri.v0, tri.v1, tri.v2 };

            Vector3 i0 = Vector3.negativeInfinity;
            Vector3 i1 = Vector3.negativeInfinity;
            bool i0Intersects = false;
            bool i1Intersects = false;

            //////// Step 1: Circle intersects triangle plane ///////////////////////////
            Vector3 circleTriPlane = Vector3.Cross(tri.normal, transform.right).normalized;
            Vector3 circleToTriUp = Vector3.Cross(transform.right, circleTriPlane).normalized;

            float normalAngle = Geometry.CirclePointToAngle(Vector3.zero, -tri.normal, transform.right, transform.up);
            Vector3 anglePoint = Geometry.CirclePointFromAngle(normalAngle, fingerRadius, transform.right, transform.up, transform.position);

            // Intersection line crosses circle vertically towards the triangle plane
            Vector3 l0 = transform.position + (circleToTriUp * fingerRadius);
            Vector3 l1 = anglePoint;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(l0, l1);

            bool triPlaneIntersection = Geometry.LinePlaneIntersection(l0, l1, triCenter, -tri.normal, out Vector3 planeIntersection, out float normalizedLinePoint);
            
            //////// Step 2: Find the two points on the circle that intersects the triangle plane
            //if (!triPlaneIntersection) return;

            // Intersection line crosses circle horizontally along the triangle plane
            Vector3 segment0 = planeIntersection - (circleTriPlane * fingerRadius);
            Vector3 segment1 = planeIntersection + (circleTriPlane * fingerRadius);

            Geometry.LineCircleIntersection(fingerRadius, transform.position, segment0, segment1, transform.right, transform.up, out i0, out i1);

            Gizmos.color = (i0Intersects || i1Intersects) ? Color.green : Color.red;
            Gizmos.DrawLineStrip(pList, true);
        }*/

        private int FindCircleIntersectingTriangle(out Triangle wsTri, out Vector3 triCenter) {
            int index = GetClosestTriangleToCircle(out Vector3 closestPointToFinger);

            wsTri = Triangle.Transform(graph.triangles[index], _collider.transform);
            triCenter = (wsTri.v0 + wsTri.v1 + wsTri.v2) / 3;

            if (!Math.Geometry.CircleTriangleIntersection(wsTri.v0, wsTri.v1, wsTri.v2, triCenter, wsTri.normal, transform.position, transform.up, transform.right, fingerRadius, out Vector3 i0, out Vector3 i1, out bool i0Intersects, out bool i1Intersects)) {
                index = StepTowardsCircle(index, triCenter);
            }

            return index;
        }

        private int GetClosestTriangleToCircle(out Vector3 closestPointToFinger) {
            closestPointToFinger = _collider.ClosestPoint(transform.position);

            bool canReach = AStarSearch.GetPath(graph, _collider.transform, 0, closestPointToFinger, out List<PathNode> waypoint);

            for (int i = 0; i < waypoint.Count; i++) {
                Triangle tri = Triangle.Transform(graph.triangles[waypoint[i].index], _collider.transform);
                Vector3 triCenter = (tri.v0 + tri.v1 + tri.v2) / 3; 

                Gizmos.color = Color.white;
                Gizmos.DrawWireSphere(triCenter, debugRadius * 0.5f);
            }

            return waypoint[waypoint.Count-1].index;
        }

        private List<Vector3> GetFingerPoints() {
            List<Vector3> points = new List<Vector3>();

            int index = FindCircleIntersectingTriangle(out Triangle tri, out Vector3 triCenter);

            HashSet<int> visited = new HashSet<int>();          

            bool canStep = true;

            while (canStep) {
                canStep = false;
                visited.Add(index);

                tri = Triangle.Transform(graph.triangles[index], _collider.transform);
                triCenter = (tri.v0 + tri.v1 + tri.v2) / 3f;
                    
                if (Math.Geometry.CircleTriangleIntersection(tri.v0, tri.v1, tri.v2, triCenter, tri.normal, transform.position, transform.up, transform.right, fingerRadius, out Vector3 i0, out Vector3 i1, out bool i0Intersects, out bool i1Intersects)) {   
                    if (i0Intersects && (points.Count > 0 && (points[points.Count-1] - i0).sqrMagnitude > 0.000025f || points.Count == 0)) points.Add(i0);
                    if (i1Intersects && (points.Count > 0 && (points[points.Count-1] - i1).sqrMagnitude > 0.000025f || points.Count == 0)) points.Add(i1);
                    
                    Vector3 intersectionPointCenter = (i0 + i1) * 0.5f;
                    Vector3 surfaceClockwiseDir = -Vector3.Cross(tri.normal, transform.right);

                    if (StepForward(index, intersectionPointCenter, surfaceClockwiseDir, out index)) {
                        canStep = visited.Contains(index) ? false : true;

                        Gizmos.color = canStep ? Color.black : Color.red;
                        Gizmos.DrawWireSphere(intersectionPointCenter, debugRadius);
                    } 
                } 

                Gizmos.color = i0Intersects ? Color.darkBlue : Color.darkOrange;
                Gizmos.DrawWireSphere(i0, debugRadius);

                Gizmos.color = i1Intersects ? Color.darkBlue : Color.darkOrange;
                Gizmos.DrawWireSphere(i1, debugRadius);
            }
            
            return points;
        }



        private int StepTowardsCircle(int nodeIndex, Vector3 triCenter) {
            TriangleNode node = graph.triangleNodes[nodeIndex];

            float score = -1f;
            TriangleEdge selectedEdge = graph.edges[TriangleNode.GetEdge(node, 0)];

            for (int i = 0; i < 3; i++) {
                TriangleEdge edge = graph.edges[TriangleNode.GetEdge(node, i)];

                Vector3 p0 = _collider.transform.TransformPoint(edge.line.p0);
                Vector3 p1 = _collider.transform.TransformPoint(edge.line.p1);
                Vector3 center = (p0 + p1) * 0.5f;
                Vector3 stepDir = (center - triCenter).normalized;

                float dot = Vector3.Dot(stepDir, transform.right);

                if (dot > score) {
                    score = dot;
                    selectedEdge = edge;
                }
            }

            return selectedEdge.triangle0 == nodeIndex ? selectedEdge.triangle1 : selectedEdge.triangle0;
        }

        private bool StepForward(int nodeIndex, Vector3 circleTriPoint, Vector3 surfaceForward, out int stepIndex) {
            TriangleNode node = graph.triangleNodes[nodeIndex];

            float score = -1f;
            int selectedEdge = TriangleNode.GetEdge(node, 0);
            stepIndex = nodeIndex;

            Vector3 selectedIntersection = circleTriPoint;

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
                        selectedIntersection = intersection;
                    }
                }
            }

            if (Vector3.Distance(selectedIntersection, transform.position) >= fingerRadius || score == -1f)  return false;

            TriangleEdge e = graph.edges[selectedEdge];
            stepIndex = (e.triangle0 == nodeIndex) ? e.triangle1 : e.triangle0;

            return true;
        }
    }
}