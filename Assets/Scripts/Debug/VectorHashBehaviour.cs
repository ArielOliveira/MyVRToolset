using UnityEngine;
using Arielado.Math.Primitives;
using UnityEditor;
using UnityEditorInternal;

[ExecuteAlways]
public class VectorHashBehaviour : MonoBehaviour {
    public int hashValue, invertedLineHashValue;
    public float t = 1f;

    public Vector3 v0 = Vector3.zero;
    public Vector3 v1 = Vector3.zero;

    // Update is called once per frame
    void Update() {
        Line l0 = new Line(v0, v1);
        Line l1 = new Line(v1, v0);
 
        hashValue = l0.GetHashCode();
        invertedLineHashValue = l1.GetHashCode();
    }
}
