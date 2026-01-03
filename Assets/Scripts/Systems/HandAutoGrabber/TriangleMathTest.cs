using System.IO;
using Arielado.Graphs;
using Arielado.Math.Primitives;
using UnityEngine;

namespace Arielado {
    public class TriangleMathTest : MonoBehaviour {
        [SerializeField] private Mesh mesh;
        [SerializeField] private Transform t, goal;
        [SerializeField] private int testTriangle;
        [SerializeField] private float debugRadius;
        [SerializeField] private bool step;
        [SerializeField, ReadOnly] private int selectedTriangle;
        private MeshTriangleGraph graph;

        private void OnDrawGizmos() {
            if (mesh == null || t == null || goal == null) return;

            if (graph.triangles == null || graph.triangles.Length == 0) {
                string path = Paths.GetPersistentDir(Paths.TRIANGLE_GRAPHS + mesh.name + ".json");
                if (!File.Exists(path)) { Debug.Log(path + " File doesn't exists!"); return; }
                graph = JsonUtility.FromJson<MeshTriangleGraph>(File.ReadAllText(path));    
            }

            testTriangle = Mathf.Clamp(testTriangle, 0, graph.triangles.Length-1);

            Triangle tri = Triangle.Transform(graph.triangles[testTriangle], t);

            Vector3 closestPoint = Triangle.ClosestPointTo(tri, goal.position);

            float distance = Vector3.Distance(closestPoint, goal.position);

            int[] neighbours = graph.triangleNodes[testTriangle].neighbours;

            for (int i = 0; i < neighbours.Length; i++) {
                Triangle candidate = Triangle.Transform(graph.triangles[neighbours[i]], t);

                closestPoint = Triangle.ClosestPointTo(candidate, goal.position);

                Gizmos.color = Color.blue;
                Gizmos.DrawLineStrip(new Vector3[] { candidate.v0, candidate.v1, candidate.v2 }, true);
            
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(closestPoint, debugRadius);  

                float dist = Vector3.Distance(closestPoint, goal.position);

                if (dist < distance) {
                    distance = dist;
                    selectedTriangle = neighbours[i];
                }
            }

            Gizmos.color = Color.white;
            Gizmos.DrawLineStrip(new Vector3[] { tri.v0, tri.v1, tri.v2 }, true);
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(closestPoint, debugRadius);

            tri = Triangle.Transform(graph.triangles[selectedTriangle], t);

            Gizmos.color = Color.black;
            Gizmos.DrawLineStrip(new Vector3[] { tri.v0, tri.v1, tri.v2 }, true);
            
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(closestPoint, debugRadius);

            if (step) {
                testTriangle = selectedTriangle;
                step = false;
            }
        }
    }
}