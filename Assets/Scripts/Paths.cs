using UnityEngine;

namespace Arielado {
    public static class Paths {
        public const string DATA = "Data/";
        public const string MESH_GRAPHS = DATA + "Mesh Graphs/";
        public const string TRIANGLE_GRAPHS = MESH_GRAPHS + "Triangles/";
        public const string TEXTURES = "Textures/";
        public const string LUT_TEXTURES = TEXTURES + "LUTs/";

        public static string GetPersistentDir(string path) {
            #if UNITY_EDITOR
                return Application.dataPath + "/" + path;
            #else
                return Application.persistentDataPath + "/" + path;
            #endif
        }
    }
}
