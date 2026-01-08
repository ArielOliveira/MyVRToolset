#if UNITY_EDITOR

using System;
using System.IO;
using System.Collections.Generic;
using Arielado.Graphs;
using Arielado.Math;
using UnityEditor;
using UnityEngine;
using Arielado.Math.Primitives;

[ExecuteAlways,
 RequireComponent(typeof(SphereCollider))]
public class HandTest : MonoBehaviour {
    [Serializable]
    public struct FingerData {
        [ReadOnly] public Transform[] bones;
        [ReadOnly] public Quaternion[] boneRotations;
        [ReadOnly] public float[] boneLengths;
        [ReadOnly] public float length;
    }

    [Serializable]
    public struct FingerReferenceData {
        public Transform root, tip;
    }

    [SerializeField] private Transform palm;
    [SerializeField] private FingerReferenceData[] fingers;

    [SerializeField] private FingerData[] fingerData;
    [SerializeField, ReadOnly] private Vector3 palmSize;
    [SerializeField] private float debugRadius = 0.05f;
    [SerializeField] private bool drawFingerData;
    [SerializeField, ReadOnly] private bool inTestRange;

    private SkinnedMeshRenderer _renderer;
    private SphereCollider sphereCollider;

    private Collider grabbable;
    private IGraph graph;
    private int lastPalmTriangle, stepNode;
    private int[] lastFingerRootTriangles;

    private void Awake() {
        SetupHandData();

        lastFingerRootTriangles = new int[fingerData.Length];
    }

    private void Update() {
        UpdateIntersectionPoints();
    }

    private void OnDrawGizmosSelected() {
        DrawFingerData();

        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(palm.position + (palm.forward * palmSize.z * 0.5f) + (palm.up * palmSize.y * 0.5f), palmSize);

        DrawIntersectionData();
    }

    private void DrawFingerData() {
        if (!drawFingerData) return;

        for (int i = 0; i < fingerData.Length; i++) {
            FingerData finger = fingerData[i];

            Handles.DrawWireDisc(finger.bones[0].position, finger.bones[0].right, finger.length);

            for (int j = 0; j < fingerData[i].bones.Length; j++) {
                Gizmos.DrawSphere(fingerData[i].bones[j].position, debugRadius * 0.5f);
            }
        }
    }

    private void DrawIntersectionData() {
        if (!inTestRange) return;

        Vector3 closestToPalm = graph.GetNodeClosestPointToWS(grabbable.transform, palm.position, lastPalmTriangle);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(closestToPalm, debugRadius);
    }

    private void UpdateIntersectionPoints() {
        if (!inTestRange) return;

        AStarSearch.GetPath(graph, grabbable.transform, lastPalmTriangle, palm.position, out List<PathNode> path);
        PathNode result = path[path.Count-1];
        lastPalmTriangle = result.index;

        Vector3 surfaceNormal = graph.GetNodeNormalWS(grabbable.transform, lastPalmTriangle);
        Vector3 surfaceDir    = -Vector3.Cross(surfaceNormal, palm.forward);

        graph.StepTowards(grabbable.transform, palm.position, palm.forward, surfaceDir, lastPalmTriangle, out stepNode);
    }

    private void OnTriggerEnter(Collider other) {
        inTestRange = true;

        grabbable = other;
        lastPalmTriangle = 0;

        for (int i = 0; i < lastFingerRootTriangles.Length; i++) lastFingerRootTriangles[i] = 0;

        string path = Arielado.Paths.GetPersistentDir(Arielado.Paths.TRIANGLE_GRAPHS + other.GetComponent<MeshFilter>().sharedMesh.name + ".json");
        if (!File.Exists(path)) { Debug.Log(path + " File doesn't exists!"); inTestRange = false; return; }
        graph = JsonUtility.FromJson<MeshTriangleGraph>(File.ReadAllText(path));    
    }

    private void OnTriggerExit(Collider other) {
        inTestRange = false;
    }

    public void SetupHandData() {
        if (sphereCollider == null) sphereCollider = GetComponent<SphereCollider>();
        if (sphereCollider == null) return;
        
        if (_renderer == null) _renderer = transform.GetComponentInChildren<SkinnedMeshRenderer>();
        if (_renderer == null) return;

        sphereCollider.center    = transform.InverseTransformPoint(_renderer.bounds.center);
        sphereCollider.radius    = _renderer.bounds.size.magnitude * 0.5f;
        sphereCollider.isTrigger = true;

        SetupFingerData();

        palmSize = Vector3.zero;

        float maxRight = 0, maxLeft = 0, maxForward = 0;

        for (int i = 0; i < fingers.Length; i++) {
            Vector3 dir = (fingers[i].root.position - palm.position).normalized;

            float dot = Vector3.Dot(dir, palm.right);

            Geometry.PointToPlaneProjection(palm.right * Mathf.Sign(dot), fingers[i].root.position, palm.position, out float t);
        
            float forwardDistance = Vector3.Distance(fingers[i].root.position, palm.position);

            t = Mathf.Abs(t);

            if (dot > 0 && t > maxRight) {
                maxRight = t;
            } else if (dot < 0 && t > maxLeft) {
                maxLeft = t;
            }

            if (forwardDistance > maxForward) {
                maxForward = forwardDistance;
            }
        }

        palmSize.x = maxLeft + maxRight;
        palmSize.y = debugRadius;
        palmSize.z = maxForward;
    }

    public void SetupFingerData() {
        if (fingers == null || fingers.Length == 0) {
            Debug.LogWarning("No finger transform is set");

            return;
        }

        fingerData = new FingerData[fingers.Length];

        for (int i = 0; i < fingers.Length; i++) {
            FingerReferenceData frd = fingers[i];

            List<Transform> bones = new List<Transform>();
            List<Quaternion> boneRotations = new List<Quaternion>();
            List<float> boneLengths = new List<float>();

            Transform probe = frd.root;
            bones.Add(probe);

            float length = 0;

            while (probe != null && probe != frd.tip) {
                Vector3 euler = probe.localRotation.eulerAngles;
                boneRotations.Add(Quaternion.Euler(0, euler.y, euler.z));

                Transform child = probe.GetChild(0);

                float boneLength = (probe.position - child.position).magnitude;
                length += boneLength;

                boneLengths.Add(boneLength);

                bones.Add(child);
                probe = child;
            }

            fingerData[i] = new FingerData() {
                bones         = bones.ToArray(),
                boneRotations = boneRotations.ToArray(),
                boneLengths   = boneLengths.ToArray(),
                length        = length
            };
        }
    }
}


[CustomEditor(typeof(HandTest))]
public class HandTestEditor : Editor {
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        HandTest handTest = (HandTest)target;

        if (GUILayout.Button("Setup Finger Data"))
            handTest.SetupFingerData();

        if (GUILayout.Button("Setup Hand Data"))
            handTest.SetupHandData();
    }
}
#endif
