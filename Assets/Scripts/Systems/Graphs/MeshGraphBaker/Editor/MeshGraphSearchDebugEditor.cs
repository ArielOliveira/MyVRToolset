using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Arielado.Graphs;
using Unity.VisualScripting;
using Arielado.Math.Primitives;

namespace Arielado.Graphs {

    [CustomEditor(typeof(MeshGraphSearchDebug))]
    public class MeshGraphSearchDebugEditor : Editor {
        SerializedProperty graph;
        SerializedProperty displayGraph, displayNodeValues, ignoreClosedList;
        GraphSolver solver;
        SortedList<double, PathNode> openList;
        HashSet<int> closedList = new HashSet<int>();
        List<int> path;


        private void OnEnable() {
            graph = serializedObject.FindProperty("graph");
            displayGraph = serializedObject.FindProperty("displayGraph");
            displayNodeValues = serializedObject.FindProperty("displayNodeValues");
            ignoreClosedList = serializedObject.FindProperty("ignoreClosedList");
        }

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();

            if (GUILayout.Button("Search Step")) 
                SearchStep();

            if (GUILayout.Button("Reset Search")) 
                ResetSearch();
        }

        private void SearchStep() {
            MeshTriangleGraph meshGraph = (MeshTriangleGraph)graph.GetUnderlyingValue();
            MeshGraphSearchDebug debugger = (MeshGraphSearchDebug)target;

            if (solver == null || openList == null || closedList == null || path == null) ResetSearch();

            PathNode current = openList.First().Value;
            debugger.CurrentPathIndex = current.index;
            openList.RemoveAt(0);

            if (!closedList.Contains(current.index)) {
                closedList.Add(current.index);
                path.Add(current.index);
            }

            if (!meshGraph.IsInBounds(current.index)) { Debug.Log("Out of Bounds!"); return; }

            if (current.index == debugger.Goal) { Debug.Log("Reached Goal!"); return; }

            int[] neighbours = meshGraph.GetNodeNeighbours(current.index);
            for (int i = 0; i < neighbours.Length; i++) {
                PathNode candidate = solver.GetNode(neighbours[i]);

                if (!closedList.Contains(candidate.index)) {
                    double gNew = solver.ComputeG(current.index, candidate.index, debugger.Start);
                    double hNew = solver.ComputeH(candidate.index, debugger.Goal, debugger.Start);
                    double fNew = gNew + hNew;

                    if (candidate.f > fNew) {
                        solver.SetNode(candidate.index, new PathNode() { index = candidate.index, parent = current.index, f = fNew, g = gNew, h = hNew } );
                        openList.Add(fNew, solver.GetNode(candidate.index));
                    }
                }
            }
        }

        private void ResetSearch() {
            MeshTriangleGraph meshGraph = (MeshTriangleGraph)graph.GetUnderlyingValue();
            MeshGraphSearchDebug debugger = (MeshGraphSearchDebug)target;

            solver = new GraphSolver(debugger.transform, meshGraph);
            openList = new SortedList<double, PathNode>();
            closedList = new HashSet<int>();
            path = new List<int>();

            solver.SetNode(debugger.Start, new PathNode() { index = debugger.Start, parent = -1, f = 0, g = 0, h = 0 });
            openList.Add(solver.GetNode(debugger.Start).f, solver.GetNode(debugger.Start));

            debugger.CurrentPathIndex = debugger.Start;
        }

        private void OnSceneGUI() {
            if (!displayGraph.boolValue) return;
            
            MeshTriangleGraph meshGraph = (MeshTriangleGraph)graph.GetUnderlyingValue();
            MeshGraphSearchDebug debugger = (MeshGraphSearchDebug)target;

            for (int i = 0; i < meshGraph.triangleNodes[debugger.Start].neighbours.Length; i++) {
                RenderTriangle(meshGraph.triangleNodes[debugger.Start].neighbours[i],
                            Color.white, 
                            Color.red, 
                            Color.brown,
                            Vector4.zero);
            }        

            RenderTriangle(debugger.Start, Color.yellow, Color.black, Color.magenta, Vector4.zero);
            RenderTriangle(debugger.Goal, Color.white, Color.white, Vector4.zero, Color.white);

            RenderNodeValues();
        }

        private void RenderTriangle(int index, Color verticeColor, Color edgeColor, Color centerColor, Color goalColor) {
            MeshTriangleGraph meshGraph = (MeshTriangleGraph)graph.GetUnderlyingValue();
            MeshGraphSearchDebug debugger = (MeshGraphSearchDebug)target;

            Triangle triangle = meshGraph.triangles[index];

            Vector3 v0 = debugger.transform.TransformPoint(triangle.v0);
            Vector3 v1 = debugger.transform.TransformPoint(triangle.v1);
            Vector3 v2 = debugger.transform.TransformPoint(triangle.v2);
            Vector3 center = (v0 + v1 + v2) / 3f;
            Vector3 normal = debugger.transform.TransformDirection(triangle.normal);

            Handles.color = verticeColor;
            Handles.DrawSolidDisc(v0, normal, debugger.DebugRadius);
            Handles.DrawSolidDisc(v1, normal, debugger.DebugRadius);
            Handles.DrawSolidDisc(v2, normal, debugger.DebugRadius);

            Handles.color = edgeColor;
            Handles.DrawLine(v0, v1);
            Handles.DrawLine(v0, v2);
            Handles.DrawLine(v1, v2);

            Handles.color = centerColor;
            Handles.DrawSolidDisc(center, normal, debugger.DebugRadius);

            Handles.color = goalColor;
            Handles.DrawWireDisc(center, normal, debugger.DebugRadius * 2);
            Handles.DrawWireDisc(center, Vector3.Cross(normal, Vector3.right), debugger.DebugRadius * 2);            
        }

        private void RenderNodeValues() {
            if (!displayNodeValues.boolValue) return;
            if (solver == null) return;

            MeshTriangleGraph meshGraph = (MeshTriangleGraph)graph.GetUnderlyingValue();
            MeshGraphSearchDebug debugger = (MeshGraphSearchDebug)target;

            RenderNodeValues(debugger.CurrentPathIndex, Color.blue);

            int[] neighbours = meshGraph.GetNodeNeighbours(debugger.CurrentPathIndex);
            for (int i = 0; i < neighbours.Length; i++) {
                RenderNodeValues(debugger.CurrentPathIndex, neighbours[i], Color.white, Color.red, Color.cyan);
            }
        }

        private void RenderNodeValues(int nodeIndex, Color fColor) {
            if (solver == null) return;

            MeshTriangleGraph meshGraph = (MeshTriangleGraph)graph.GetUnderlyingValue();
            MeshGraphSearchDebug debugger = (MeshGraphSearchDebug)target;

            Triangle triangle = meshGraph.triangles[nodeIndex];
            Vector3 v0 = debugger.transform.TransformPoint(triangle.v0);
            Vector3 v1 = debugger.transform.TransformPoint(triangle.v1);
            Vector3 v2 = debugger.transform.TransformPoint(triangle.v2);
            Vector3 center = (v0 + v1 + v2) / 3f;
            Vector3 normal = debugger.transform.TransformDirection(triangle.normal);

            Vector3 textPos = center + normal * 0.05f;

            GUIStyle style = new GUIStyle();
            style.normal.textColor = fColor;
            style.fontSize = 14;

            Handles.Label(textPos, nodeIndex.ToString() + ":" + solver.GetNode(nodeIndex).f.ToString("0.000"), style);
        } 

        private void RenderNodeValues(int nodeIndex, int candidate, Color fColor, Color fColorClosed, Color lineColor) {
             if (solver == null) return;

            MeshTriangleGraph meshGraph = (MeshTriangleGraph)graph.GetUnderlyingValue();
            MeshGraphSearchDebug debugger = (MeshGraphSearchDebug)target;

            Triangle candidateTri = meshGraph.triangles[candidate];
            Vector3 v0 = debugger.transform.TransformPoint(candidateTri.v0);
            Vector3 v1 = debugger.transform.TransformPoint(candidateTri.v1);
            Vector3 v2 = debugger.transform.TransformPoint(candidateTri.v2);
            Vector3 center = (v0 + v1 + v2) / 3f;

            Vector3 closestToCandidate = solver.Graph.GetNodeClosestPointToWS(debugger.transform, center, nodeIndex);

            Triangle currentTri = meshGraph.triangles[nodeIndex];
            v0 = debugger.transform.TransformPoint(currentTri.v0);
            v1 = debugger.transform.TransformPoint(currentTri.v1);
            v2 = debugger.transform.TransformPoint(currentTri.v2);
            Vector3 normal = debugger.transform.TransformDirection(currentTri.normal);
            center = (v0 + v1 + v2) / 3f;

            Handles.color = lineColor;
            Handles.DrawLine(center, closestToCandidate);

            GUIStyle style = new GUIStyle();
            style.normal.textColor = !closedList.Contains(candidate) ? fColor : fColorClosed;
            style.fontSize = 14;
            Handles.Label(closestToCandidate + normal * 0.05f, candidate.ToString() + ":" + solver.GetNode(candidate).f.ToString("0.000"), style);
        }
    }
}
