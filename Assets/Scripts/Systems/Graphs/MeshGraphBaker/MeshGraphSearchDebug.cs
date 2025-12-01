using System.IO;
using UnityEngine;

namespace Arielado.Graphs {
    [ExecuteAlways,
    RequireComponent(typeof(MeshFilter))]
    public class MeshGraphSearchDebug : MonoBehaviour {
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshTriangleGraph graph;
        [SerializeField] private Mesh mesh;
        [SerializeField, Range(0.01f, 0.2f)] private float debugRadius = 0.02f;
        [SerializeField] private int start, goal;
        [SerializeField, ReadOnly] private int currentPathIndex;
        
        [SerializeField] private bool displayGraph = true, 
                                      displayNodeValues = true,
                                      ignoreClosedList = false;

        public int Start => start;
        public int Goal => goal;
        public int CurrentPathIndex { get => currentPathIndex; set => currentPathIndex = value;}
        public float DebugRadius => debugRadius;
        private void Setup() {
            meshFilter = GetComponent<MeshFilter>();

            mesh = meshFilter.sharedMesh;

            string path = Paths.GetPersistentDir(Paths.TRIANGLE_GRAPHS) + mesh.name + ".json";
            if (File.Exists(path)) graph = JsonUtility.FromJson<MeshTriangleGraph>(File.ReadAllText(path));
            else Debug.Log($"File {path} doesn't exist!");

            start = Mathf.Clamp(start, 0, graph.Size-1);
            goal  = Mathf.Clamp(goal, 0, graph.Size-1);
        }

        private void Update() {
            if (mesh == null || graph.Size == 0) Setup();

            if (mesh == null || graph.Size == 0) return;
        }
    }
}
