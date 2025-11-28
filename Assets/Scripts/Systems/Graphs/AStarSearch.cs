using System.Linq;
using System.Collections.Generic;
using UnityEngine;


namespace Arielado.Graphs {
    public static class AStarSearch {
        public static bool GetPath(IGraph graph, Transform reference, int start, int goal,  out List<Vector3> path) { 
            if (!graph.IsInBounds(start) || !graph.IsInBounds(goal)) {
                Debug.Log("Out of Bounds!");

                path = null;
                return false;
            }

            GraphSolver solver = new GraphSolver(reference, graph);
            solver.SetNode(start, new PathNode() { index = start, parent = -1, f = 0, g = 0, h = 0 });

            SortedList<double, PathNode> openList = new SortedList<double, PathNode>();
            HashSet<int> closedList = new HashSet<int>();

            openList.Add(solver.GetNode(start).f, solver.GetNode(start));

            path = new List<Vector3>(); 

            while (openList.Count > 0) {
                PathNode current = openList.First().Value;
                openList.RemoveAt(0);

                if (!closedList.Contains(current.index)) {
                    closedList.Add(current.index);
                    path.Add(graph.GetNodeCenterWS(reference, current.index));
                }

                if (!graph.IsInBounds(current.index)) { Debug.Log("Out of Bounds!"); return false; };

                if (current.index == goal) { Debug.Log("Reached Goal!"); return true; }

                int[] neighbours = graph.GetNodeNeighbours(current.index);
                for (int i = 0; i < neighbours.Length; i++) {
                    PathNode candidate = solver.GetNode(neighbours[i]);

                    if (!closedList.Contains(candidate.index)) {
                        double gNew = solver.ComputeG(current.index, candidate.index, start);
                        double hNew = solver.ComputeH(candidate.index, goal, start);
                        double fNew = gNew + hNew;

                        if (candidate.f == float.PositiveInfinity || candidate.f > fNew) {
                            solver.SetNode(candidate.index, new PathNode() { index = candidate.index, parent = current.index, f = fNew, g = gNew, h = hNew } );
                            openList.Add(fNew, solver.GetNode(candidate.index));
                        }
                    }
                }
            }

            Debug.Log("Reached Goal!");

            return true; 
        }
    }
}
