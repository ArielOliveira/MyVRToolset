using System;
using System.Collections.Generic;
using Arielado.Math.Primitives;
using UnityEngine;

namespace Arielado {
    [RequireComponent(typeof(MeshFilter))]
    public class MeshGraphBaker : MonoBehaviour {
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private bool useSharedEdges;

        private Dictionary<int, HashSet<int>> triangleNeighbours;
        public Dictionary<int, HashSet<int>> TriangleNeighbours => triangleNeighbours;

        public Mesh _Mesh => meshFilter?.sharedMesh;

        public void Setup() {
            meshFilter = GetComponent<MeshFilter>();

            if (meshFilter == null) 
                meshFilter = gameObject.AddComponent<MeshFilter>();
        }

        public void BuildGraph() {
            if (useSharedEdges)
                BuildGraphUsingSharedEdges();
            else 
                BuildGraphUsingSharedVertices();
        }

        private Dictionary<Vector3, int> VerticeDuplicateFilter() {
            Dictionary<Vector3, int> verticesPosToIndex = new Dictionary<Vector3, int>();

            int duplicates = 0;

            for (int i = 0; i < _Mesh.vertices.Length; i++) {
                Vector3 vertice = _Mesh.vertices[i];

                if (!verticesPosToIndex.TryAdd(vertice, i)) 
                    duplicates++;
            }

            Debug.Log($"Vertice filtering finished. {duplicates} duplicates found! Reduced to {verticesPosToIndex.Count}.");

            return verticesPosToIndex;
        } 

        private void BuildGraphUsingSharedVertices() {
            if (_Mesh == null) return;
            Dictionary<Vector3, int> verticeIDs = VerticeDuplicateFilter();
            Dictionary<int, HashSet<int>> verticesToTriangles = new Dictionary<int, HashSet<int>>();
            triangleNeighbours = new Dictionary<int, HashSet<int>>();

            int[] verticeGroup = new int[3];

            for (int j = 0; j < _Mesh.triangles.Length; j += 3) {
                int triangleIndex = j/3;

                int iv0 = _Mesh.triangles[j];
                int iv1 = _Mesh.triangles[j+1];
                int iv2 = _Mesh.triangles[j+2];

                Vector3 v0 = _Mesh.vertices[iv0];
                Vector3 v1 = _Mesh.vertices[iv1];
                Vector3 v2 = _Mesh.vertices[iv2];

                verticeGroup[0] = verticeIDs[v0];
                verticeGroup[1] = verticeIDs[v1];
                verticeGroup[2] = verticeIDs[v2];

                MapVertice(verticesToTriangles, verticeGroup[0], triangleIndex);
                MapVertice(verticesToTriangles, verticeGroup[1], triangleIndex);
                MapVertice(verticesToTriangles, verticeGroup[2], triangleIndex);

                ConnectTriangles(verticesToTriangles, verticeGroup, triangleIndex);
            }
        }

        public void BuildGraphUsingSharedEdges() {
            if (_Mesh == null) return;

            Dictionary<Vector3, int> verticeIDs = VerticeDuplicateFilter();
            Dictionary<Line, HashSet<int>> edgeToTriangles = new Dictionary<Line, HashSet<int>>();
            triangleNeighbours = new Dictionary<int, HashSet<int>>();

            Line[] edgeGroup = new Line[3];

            for (int i = 0; i < _Mesh.triangles.Length; i += 3) {
                int triangleIndex = i/3;

                int iv0 = _Mesh.triangles[i];
                int iv1 = _Mesh.triangles[i+1];
                int iv2 = _Mesh.triangles[i+2];

                Vector3 v0 = _Mesh.vertices[iv0];
                Vector3 v1 = _Mesh.vertices[iv1];
                Vector3 v2 = _Mesh.vertices[iv2];

                int v0ID = verticeIDs[v0];
                int v1ID = verticeIDs[v1];
                int v2ID = verticeIDs[v2];

                v0 = _Mesh.vertices[v0ID];
                v1 = _Mesh.vertices[v1ID];
                v2 = _Mesh.vertices[v2ID];

                Line edge0 = new Line(v0, v1);
                Line edge1 = new Line(v0, v2);
                Line edge2 = new Line(v1, v2);

                edgeGroup[0] = edge0;
                edgeGroup[1] = edge1;
                edgeGroup[2] = edge2;

                MapEdge(edgeToTriangles, edge0, triangleIndex);
                MapEdge(edgeToTriangles, edge1, triangleIndex);
                MapEdge(edgeToTriangles, edge2, triangleIndex);

                ConnectTriangles(edgeToTriangles, edgeGroup, triangleIndex);
            }
        }

        private void MapVertice(Dictionary<int, HashSet<int>> verticesToTriangles, int vertice, int triangle) {
            if (verticesToTriangles.TryGetValue(vertice, out HashSet<int> triangles)) {
                triangles.Add(triangle);                
            } else {
                verticesToTriangles.Add(vertice, new HashSet<int>() { triangle });
            }
        }

        private void ConnectTriangles(Dictionary<int, HashSet<int>> verticesToTriangles, int[] verticeGroup, int triangle) {
            for (int i = 0; i < verticeGroup.Length; i++) {
                HashSet<int> triangles = verticesToTriangles[verticeGroup[i]];

                foreach (int neighbour in triangles) {
                    if (neighbour == triangle) continue;

                    ConnectTriangle(triangle, neighbour);
                    ConnectTriangle(neighbour, triangle);
                }
            }
        }

        private void ConnectTriangle(int triangle0, int triangle1) {
            if (triangleNeighbours.TryGetValue(triangle0, out HashSet<int> neighbours)) {
                if (!neighbours.Contains(triangle1)) neighbours.Add(triangle1);
            } else {
                triangleNeighbours.Add(triangle0, new HashSet<int>() { triangle1 });
            }
        }

        private void MapEdge(Dictionary<Line, HashSet<int>> edgeToTriangles, Line edge, int triangle) {
            if (edgeToTriangles.TryGetValue(edge, out HashSet<int> triangles)) {
                triangles.Add(triangle);
            } else {
                edgeToTriangles.Add(edge, new HashSet<int>() { triangle });
            }
        }

        private void ConnectTriangles(Dictionary<Line, HashSet<int>> edgeToTriangles, Line[] edgeGroup, int triangle) {
            for (int i = 0; i < edgeGroup.Length; i++) {
                HashSet<int> triangles = edgeToTriangles[edgeGroup[i]];

                foreach (int neighbour in triangles) {
                    if (neighbour == triangle) continue;

                    ConnectTriangle(triangle, neighbour);
                    ConnectTriangle(neighbour, triangle);
                }
            }
        }
    }
}