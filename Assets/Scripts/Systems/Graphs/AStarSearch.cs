using System.Linq;
using System.Collections.Generic;
using UnityEngine;


namespace Arielado.Graphs {
    public static class AStarSearch {
        #region Node Based Search
        public static bool GetPath(IGraph graph, Transform reference, int start, int goal, out List<PathNode> path) { 
            if (!graph.IsInBounds(start) || !graph.IsInBounds(goal)) {
                Debug.Log("Out of Bounds!");

                path = null;
                return false;
            }

            GraphSolver solver = new GraphSolver(reference, graph);
            solver.SetNode(start, new PathNode() { index = start, parent = -1, f = 0, g = 0, h = 0 });

            SortedList<double, int> openList = new SortedList<double, int>();
            HashSet<int> closedList = new HashSet<int>();

            openList.Add(solver.GetNode(start).f, start);

            path = new List<PathNode>(); 

            while (openList.Count > 0) {
                PathNode current = solver.GetNode(openList.First().Value);
                openList.RemoveAt(0);

                if (!closedList.Contains(current.index)) {
                    closedList.Add(current.index);
                    //path.Add(graph.GetNodeClosestPointToWS(reference, graph.GetNodeCenterWS(reference, current.parent), current.index));
                    path.Add(current);
                }

                if (!graph.IsInBounds(current.index)) { Debug.Log("Out of Bounds!"); return false; };

                if (solver.IsDestiny(current.index, goal)) return true;

                int[] neighbours = graph.GetNodeNeighbours(current.index);
                for (int i = 0; i < neighbours.Length; i++) {
                    PathNode candidate = solver.GetNode(neighbours[i]);

                    if (!closedList.Contains(candidate.index)) {
                        double fNew = solver.ComputeStepCost(current.index, candidate.index, start, goal, out double gNew, out double hNew);

                        if (candidate.f > fNew) {
                            solver.SetNode(candidate.index, new PathNode() { index = candidate.index, parent = current.index, f = fNew, g = gNew, h = hNew } );

                            if (openList.ContainsKey(fNew))
                                openList.Add(fNew + (Random.Range(-1f, 1f) * 0.00001f), candidate.index);
                            else 
                                openList.Add(fNew, candidate.index);
                        }
                    }
                }
            }

            return true; 
        }
        #endregion

        #region Position Based Search (Bounds Constrained)
        public static bool GetPath(IGraph graph, Transform reference, int start, Vector3 goal, out List<PathNode> path) { 
            if (!graph.IsInBounds(start)) {
                Debug.Log("Out of Bounds!");

                path = null;
                return false;
            }

            GraphSolver solver = new GraphSolver(reference, graph);
            double startValue = solver.ComputeH(start, goal);
            solver.SetNode(start, new PathNode() { index = start, parent = -1, f = startValue + 1, g = 1, h = startValue });

            SortedList<double, int> openList = new SortedList<double, int>();
            HashSet<int> closedList = new HashSet<int>();

            openList.Add(solver.GetNode(start).f, start);

            path = new List<PathNode>(); 

            while (openList.Count > 0) {
                PathNode current = solver.GetNode(openList.Values[0]);
                openList.RemoveAt(0);

                if (!closedList.Contains(current.index)) {
                    closedList.Add(current.index);
                    //path.Add(graph.GetNodeClosestPointToWS(reference, graph.GetNodeCenterWS(reference, current.parent), current.index));
                    path.Add(current);
                }

                if (!graph.IsInBounds(current.index)) { Debug.Log("Out of Bounds!"); return false; };

                if (solver.IsDestiny(current.index, goal)) {
                    if (path.Count > 1) path.RemoveAt(path.Count-1);

                    return true;
                }

                int[] neighbours = graph.GetNodeNeighbours(current.index);
                for (int i = 0; i < neighbours.Length; i++) {
                    PathNode candidate = solver.GetNode(neighbours[i]);

                    if (!closedList.Contains(candidate.index)) {
                        double fNew = solver.ComputeStepCost(current.index, candidate.index, start, goal, out double gNew, out double hNew);

                        if (candidate.f > fNew) {
                            solver.SetNode(candidate.index, new PathNode() { index = candidate.index, parent = current.index, f = fNew, g = gNew, h = hNew } );

                            if (openList.ContainsKey(fNew))
                                openList.Add(fNew + (Random.Range(0f, 100f) * 0.0000001f), candidate.index);
                            else 
                                openList.Add(fNew, candidate.index);
                        }
                    }
                }
            }

            return true; 
        }
        #endregion
    }
}
