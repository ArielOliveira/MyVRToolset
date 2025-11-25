using UnityEngine;

namespace Arielado {
    public static class Paths {
        public const string MESH_GRAPHS = "Mesh Graphs/";
        public const string TRIANGLE_GRAPHS = MESH_GRAPHS + "Triangles/";

        public static string GetPersistentDir(string path) {
            #if UNITY_EDITOR
                return Application.dataPath + "/" + path;
            #else
                return Application.persistentDataPath + "/" + path;
            #endif
        }
    }
}
