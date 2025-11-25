using Arielado.Math.Primitives;
using UnityEngine;

namespace Arielado.Graphs {
    [System.Serializable]
    public struct TriangleNode {
        public int index;
        public int[] neighbours;

        public TriangleNode(int index, int[] neighbours) {
            this.index = index;
            this.neighbours = neighbours;
        }
    }

    [System.Serializable]
    public struct MeshTriangleGraph {
        public Triangle[] triangles;
        public TriangleNode[] triangleNodes;

        public MeshTriangleGraph(Triangle[] triangles, TriangleNode[] triangleNodes) {
            this.triangles = triangles;
            this.triangleNodes = triangleNodes;
        } 
    }
}
