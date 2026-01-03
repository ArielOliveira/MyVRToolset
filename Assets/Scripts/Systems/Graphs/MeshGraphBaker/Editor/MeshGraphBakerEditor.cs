using System.IO;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Arielado.Graphs {
    [CustomEditor(typeof(MeshGraphBaker))]
    public class MeshGraphBakerEditor : Editor {
        [SerializeField] private bool displayReference, displayReferencePerTriangle, displayGraph, displayPath;
        [SerializeField] private int triangleIndex, referenceTriangleIndex;
        [SerializeField] private int start, goal;
        [SerializeField] private float debugGraphicRadius = 0.05f; 

        [SerializeField] private List<Vector3> path = new List<Vector3>();

        [SerializeField] private int triangleNeighbours = 0;
        private void OnEnable() {
            ((MeshGraphBaker)target).Setup();
        }

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();

            MeshGraphBaker t = (MeshGraphBaker)target;

            displayReference = EditorGUILayout.Toggle("Display Reference", displayReference);
            displayReferencePerTriangle = EditorGUILayout.Toggle("Display Reference Per Triangle", displayReferencePerTriangle);
            displayGraph = EditorGUILayout.Toggle("Display Graph", displayGraph);
            displayPath = EditorGUILayout.Toggle("Display Path", displayPath);
            debugGraphicRadius = EditorGUILayout.Slider("Debug Graphic Radius", debugGraphicRadius, 0.01f, 2f);
            
            triangleIndex = EditorGUILayout.IntField("Triangle Index", triangleIndex);
            triangleIndex = Mathf.Clamp(triangleIndex, 0, t.TriangleNeighbours != null ? t.TriangleNeighbours.Count-1 : 0);

            referenceTriangleIndex = EditorGUILayout.IntField("Reference Triangle Index", referenceTriangleIndex);
            referenceTriangleIndex = Mathf.Clamp(referenceTriangleIndex, 0, t._Mesh != null ? t._Mesh.triangles.Length-1 : 0);

            start = EditorGUILayout.IntField("Search Start Index", start);
            start = Mathf.Clamp(start, 0, t._Mesh != null ? (t._Mesh.triangles.Length/3)-1 : 0);

            goal = EditorGUILayout.IntField("Search Goal Index", goal);
            goal = Mathf.Clamp(goal, 0, t._Mesh != null ? (t._Mesh.triangles.Length/3)-1 : 0);

            if (GUILayout.Button("Build Graph"))
                t.BuildGraph();

            if (GUILayout.Button("Search Path")) {
                if (t._Mesh == null) return;

                
                string filePath = Paths.GetPersistentDir(Paths.TRIANGLE_GRAPHS + t._Mesh.name + ".json");
                if (!File.Exists(filePath)) { Debug.Log(filePath + " File doesn't exists!"); return; }

                MeshTriangleGraph graph = JsonUtility.FromJson<MeshTriangleGraph>(File.ReadAllText(filePath));

                Debug.Log(graph.triangles.Length);

                AStarSearch.GetPath(graph, t.transform, start, goal, out path);
            }

            if (GUILayout.Button("Search Path With Position Goal")) {
                if (t._Mesh == null) return;

                string filePath = Paths.GetPersistentDir(Paths.TRIANGLE_GRAPHS + t._Mesh.name + ".json");
                if (!File.Exists(filePath)) { Debug.Log(filePath + " File doesn't exists!"); return; }

                MeshTriangleGraph graph = JsonUtility.FromJson<MeshTriangleGraph>(File.ReadAllText(filePath));

                Debug.Log(graph.triangles.Length);

                AStarSearch.GetPath(graph, t.transform, start, t._TestSearchGoal.position, out path);
            }

            GUI.enabled = false;
            EditorGUILayout.IntField("Triangle Neighbour Count: ", triangleNeighbours);
            GUI.enabled = true;
        }

        private void OnSceneGUI() {
            if (displayReference) DisplayReference();
            if (displayGraph) DisplayGraphData();
            if (displayPath) DisplayPath();

            Repaint();
        }

        private void DisplayReference() {
            Mesh mesh = ((MeshGraphBaker)target)._Mesh;

            if (mesh == null) return;
          
            for (int i = 0; i < mesh.triangles.Length; i += 3) {
                RenderTriangle(mesh, i);
            }
        }

        private void DisplayPath() {
            if (path == null || path.Count == 0) return;

            if (path.Count > 1) {
                for (int i = 0; i < path.Count-1; i++) {
                    Handles.color = Color.blue;
                    Handles.DrawLine(path[i], path[i+1]);

                    Handles.color = Color.yellow;    
                    Handles.DrawWireDisc(path[i], Vector3.up, debugGraphicRadius);
                    Handles.DrawWireDisc(path[i], Vector3.right, debugGraphicRadius);
                    Handles.DrawWireDisc(path[i+1], Vector3.up, debugGraphicRadius);
                    Handles.DrawWireDisc(path[i+1], Vector3.right, debugGraphicRadius);
                }
            } else {
                Handles.DrawSolidDisc(path[0], Vector3.up, debugGraphicRadius);
            }
        }

        private void DisplayGraphData() {
            MeshGraphBaker t = (MeshGraphBaker)target;

            if (t.TriangleNeighbours == null) return;

            Mesh mesh = ((MeshGraphBaker)target)._Mesh;

            HashSet<int> triangles = t.TriangleNeighbours[triangleIndex];

            int triangleMeshIndex = triangleIndex * 3;

            triangleNeighbours = triangles.Count;

            int refI0 = mesh.triangles[triangleMeshIndex];
            int refI1 = mesh.triangles[triangleMeshIndex+1];
            int refI2 = mesh.triangles[triangleMeshIndex+2];

            Vector3 refV0 = t.transform.TransformPoint(mesh.vertices[refI0]);
            Vector3 refV1 = t.transform.TransformPoint(mesh.vertices[refI1]);
            Vector3 refV2 = t.transform.TransformPoint(mesh.vertices[refI2]);

            Handles.color = Color.black;
            Handles.DrawLine(refV0, refV1);
            Handles.DrawLine(refV0, refV2);
            Handles.DrawLine(refV1, refV2);

            Vector3 refCenter = (refV0 + refV1 + refV2) / 3f;

            Vector3 refNormal = Vector3.Cross(refV1 - refV0, refV2 - refV0).normalized;

            Handles.color = Color.yellow;
            Handles.DrawWireDisc(refCenter, refNormal, debugGraphicRadius);

            foreach (int triangle in triangles) {
                int triangleIndex = triangle * 3;

                int i0 = mesh.triangles[triangleIndex];
                int i1 = mesh.triangles[triangleIndex+1];
                int i2 = mesh.triangles[triangleIndex+2];

                Vector3 v0 = t.transform.TransformPoint(mesh.vertices[i0]);
                Vector3 v1 = t.transform.TransformPoint(mesh.vertices[i1]);
                Vector3 v2 = t.transform.TransformPoint(mesh.vertices[i2]);

                Vector3 center = (v0 + v1 + v2) / 3f;

                Vector3 normal = Vector3.Cross(v1 - v0, v2 - v0).normalized;

                Handles.color = Color.green;
                Handles.DrawWireDisc(v0, normal, debugGraphicRadius);
                Handles.DrawWireDisc(v1, normal, debugGraphicRadius);
                Handles.DrawWireDisc(v2, normal, debugGraphicRadius);

                Handles.color = Color.black;
                Handles.DrawLine(v0, v1);
                Handles.DrawLine(v0, v2);
                Handles.DrawLine(v1, v2);


                Handles.color = Color.black;
                Handles.DrawLine(refCenter, center);
            }
        }

        private void RenderTriangle(Mesh mesh, int triangle) {
            MeshGraphBaker t = (MeshGraphBaker)target;

            int _vt0 = triangle;
            int _vt1 = triangle + 1;
            int _vt2 = triangle + 2;

            Vector3 v0 = t.transform.TransformPoint(mesh.vertices[mesh.triangles[_vt0]]);
            Vector3 v1 = t.transform.TransformPoint(mesh.vertices[mesh.triangles[_vt1]]);
            Vector3 v2 = t.transform.TransformPoint(mesh.vertices[mesh.triangles[_vt2]]);

            Handles.color = Color.black;
            Handles.DrawLine(v0, v1);
            Handles.DrawLine(v0, v2);
            Handles.DrawLine(v1, v2);

            Vector3 normal = Vector3.Cross(v1 - v0, v2 - v0).normalized;

            Handles.color = displayReferencePerTriangle && triangle == referenceTriangleIndex ? Color.magenta : Color.blue;
            Handles.DrawWireDisc(v0, normal, debugGraphicRadius);

            if (triangle == referenceTriangleIndex) {
                Handles.color = Color.magenta;
                
                Handles.DrawWireDisc(v1, normal, debugGraphicRadius);
                Handles.DrawWireDisc(v2, normal, debugGraphicRadius);

                Handles.DrawLine(v0, v1);
                Handles.DrawLine(v0, v2);
                Handles.DrawLine(v1, v2);
            }
        }

        /*private void RenderVertice(Mesh mesh, int vertice) {
            MeshGraphBaker t = (MeshGraphBaker)target;

            Vector3 v0 = t.transform.TransformPoint(mesh.vertices[vertice]);

            Handles.DrawWireDisc(v0, Vector3.up, debugGraphicRadius);
            Handles.DrawWireDisc(v0, Vector3.right, debugGraphicRadius);
        }*/
    }
}