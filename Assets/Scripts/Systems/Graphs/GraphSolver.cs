using System.IO;
using Arielado.Math.Primitives;
using Unity.Mathematics;
using UnityEngine;

namespace Arielado.Graphs {
    public interface IGraph {
        public int Size { get; }
        public bool IsInBounds(int node);
        public int GetNodeIndex(int node);
        Vector3 GetNodeClosestPointToWS(Transform reference, Vector3 target, int node);
        Vector3 GetNodeClosestPointToOS(Vector3 target, int node);
        Vector3 GetNodeCenterWS(Transform reference, int node);
        Vector3 GetNodeCenterOS(int node);
        int[] GetNodeNeighbours(int node);
        void CopyTo(ref IGraph graph);
    }

    public interface IGraphSolver {
        IGraph Graph { get; }
        PathNode GetNode(int node);
        void SetNode(int node, PathNode newValue);

        bool IsValid(int from, int to, int start, int goal);

        // Should return F 
        double ComputeStepCost(int from, int to, int start, int goal, out double g, out double h);
    }

    public interface IGraphSolver<T> : IGraphSolver where T : IGraph {}

    public struct PathNode {
        public int index;
        public int parent;
        public double f, g, h;
    }

    public class GraphSolver : IGraphSolver {
        private Transform reference;
        private IGraph graph;
        private PathNode[] pathNodes;

        public GraphSolver(Transform reference, IGraph graph) {
            this.reference = reference;
            graph.CopyTo(ref this.graph);

            pathNodes = new PathNode[graph.Size];

            for (int i = 0; i < pathNodes.Length; i++) {
                pathNodes[i] = new PathNode() {
                    index = graph.GetNodeIndex(i),
                    parent = -1,
                    f = float.PositiveInfinity,
                    g = float.PositiveInfinity,
                    h = float.PositiveInfinity
                };
            }
        }

        public IGraph Graph => graph;
        public PathNode GetNode(int node) {
            if (!graph.IsInBounds(node)) throw new System.Exception("Node index out of bounds");

            return pathNodes[node];
        }

        public void SetNode(int node, PathNode newValue) {
            if (!graph.IsInBounds(node)) throw new System.Exception("Node index out of bounds");

            pathNodes[node] = newValue;
        }

        public bool IsValid(int from, int to, int start, int goal) => true;

        public double ComputeStepCost(int from, int to, int start, int goal, 
                                      out double g, out double h) {
            Vector3 goalPos = graph.GetNodeCenterWS(reference, goal);
            Vector3 candidateClosestPointToGoal = graph.GetNodeClosestPointToWS(reference, goalPos, to);
            Vector3 currentClosestPointToGoal = graph.GetNodeClosestPointToWS(reference, candidateClosestPointToGoal, from);

            g = Vector3.Distance(candidateClosestPointToGoal, currentClosestPointToGoal);
            h = Vector3.Distance(candidateClosestPointToGoal, goalPos);

            return g + h;
        }
    }
}
