using System;
using Arielado.Math.Primitives;
using UnityEngine;

namespace Arielado.Graphs {
    [System.Serializable]
    public struct TriangleEdge : IEquatable<TriangleEdge> {
        public int index;
        public Line line;
        public int triangle0;
        public int triangle1;

        public override int GetHashCode() => line.GetHashCode();
        public override bool Equals(object other) {
            if (other is Line) 
                return Equals((Line)other);

            return false;
        }

        public bool Equals(TriangleEdge other) => line.Equals(other.line);
    }


    [System.Serializable]
    public struct TriangleNode {
        public int index;
        public int[] neighbours;
        public int edge0, edge1, edge2;

        public TriangleNode(int index, int[] neighbours, int edge0, int edge1, int edge2) {
            this.index = index;
            this.neighbours = neighbours;
            this.edge0 = edge0;
            this.edge1 = edge1;
            this.edge2 = edge2;
        }

        public static int GetEdge(TriangleNode node, int index) {
            index = Mathf.Clamp(index, 0, 2);

            switch(index) {
                case 0: return node.edge0;
                case 1: return node.edge1;
                case 2: return node.edge2;
                default: return node.edge0;
            }
        }
    }

    [Serializable]
    public struct MeshTriangleGraph : IGraph {
        public Triangle[] triangles;
        public TriangleNode[] triangleNodes;
        public TriangleEdge[] edges;

        public MeshTriangleGraph(Triangle[] triangles, TriangleNode[] triangleNodes, TriangleEdge[] edges) {
            this.triangles = triangles;
            this.triangleNodes = triangleNodes;
            this.edges = edges;
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

        public Vector3 GetNodeNormalWS(Transform reference, int node) {
            if (!IsInBounds(node)) return Vector3.negativeInfinity;

            return reference.TransformDirection(triangles[node].normal);
        }

        public Vector3 GetNodeNormalOS(int node) {
            if (!IsInBounds(node)) return Vector3.negativeInfinity;

            return triangles[node].normal;
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

        public bool StepTowards(Transform reference, Vector3 pPos, Vector3 pNormal, Vector3 surfaceDirection, int node, out int stepIndex) {
            stepIndex = 0;

            return false;
        }

        public bool StepTowards(Transform reference, Vector3 pPos, Vector3 pNormal, Vector3 surfaceDirection, Vector3 referencePoint, int node, out int stepIndex) {
            TriangleNode triNode = triangleNodes[node];
            stepIndex = -1;

            float score = -1f;
            
            for (int i = 0; i < 3; i++) {
                TriangleEdge edge = edges[TriangleNode.GetEdge(triNode, i)];

                Vector3 p0 = reference.TransformPoint(edge.line.p0);
                Vector3 p1 = reference.TransformPoint(edge.line.p1);
                Vector3 rayDir = reference.TransformDirection(-edge.line.direction);
                // We're ignoring the line side
                Vector3 normal = pNormal * Mathf.Sign(Vector3.Dot(rayDir, pNormal));
                if (Math.Geometry.LinePlaneIntersection(p0, p1, pPos, normal, out Vector3 intersection, out float t)) {
                    Vector3 dir = (intersection - referencePoint).normalized;

                    float dot = Vector3.Dot(dir, surfaceDirection);

                    if (dot > score) {
                        score = dot;
                        stepIndex = edge.triangle0 == node ? edge.triangle1 : edge.triangle0;
                    }
                }
            }

            return score == -1f ? false : true;
        }
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
            graph = new MeshTriangleGraph(this.triangles, this.triangleNodes, this.edges);
        }
    }
}
