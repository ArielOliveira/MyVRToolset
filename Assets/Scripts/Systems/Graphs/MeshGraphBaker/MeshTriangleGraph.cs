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
    public struct MeshTriangleGraph : IGraph {
        public Triangle[] triangles;
        public TriangleNode[] triangleNodes;

        public MeshTriangleGraph(Triangle[] triangles, TriangleNode[] triangleNodes) {
            this.triangles = triangles;
            this.triangleNodes = triangleNodes;
        } 

        public Vector3 GetNodeClosestPointToWS(Transform reference, Vector3 target, int node) {
            if (!IsInBounds(node)) return Vector3.negativeInfinity;

            Triangle osTriangle = triangles[node];
            Triangle wsTriangle = new Triangle(reference.TransformPoint(osTriangle.v0), 
                                               reference.TransformPoint(osTriangle.v1), 
                                               reference.TransformPoint(osTriangle.v2));

            return Triangle.ClosestPointTo(wsTriangle, target);                                
        }

        public Vector3 GetNodeClosestPointToOS(Vector3 target, int node) {
            if (!IsInBounds(node)) return Vector3.negativeInfinity;

            return Triangle.ClosestPointTo(triangles[node], target);
        }

        public Vector3 GetNodeCenterWS(Transform reference, int node) {
            if (reference == null) return Vector3.negativeInfinity;
            if (!IsInBounds(node)) return Vector3.negativeInfinity;

            Triangle t = triangles[node];

            return reference.TransformPoint((t.v0 + t.v1 + t.v2) / 3f);
        }

        public Vector3 GetNodeCenterOS(int node) {
            if (!IsInBounds(node)) return Vector3.negativeInfinity;

            Triangle t = triangles[node];

            return (t.v0 + t.v1 + t.v2) / 3f;
        }

        public int Size => triangles?.Length ?? 0;
        public bool IsInBounds(int node) =>
            node >= 0 && node < triangles.Length;

        public int GetNodeIndex(int node) {
            if (!IsInBounds(node)) return -1;

            return triangleNodes[node].index;
        }

        public int[] GetNodeNeighbours(int node) {
            if (!IsInBounds(node)) return null;

            return triangleNodes[node].neighbours;
        }

        public void CopyTo(ref IGraph graph) {
            graph = new MeshTriangleGraph(this.triangles, this.triangleNodes);
        }
    }
}
