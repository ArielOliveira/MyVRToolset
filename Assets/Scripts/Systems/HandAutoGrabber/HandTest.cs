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

    public struct PointAngle {
        public Vector3 point;
        public float angle;
    }

    [Serializable]
    public struct FingerTarget {
        [Range(0, 1)]   public float length;
        [Range(180, 0)] public float angle;
    }

    [SerializeField] private Transform palm;
    [SerializeField] private FingerReferenceData[] fingers;

    [SerializeField] private FingerData[] fingerData;
    [SerializeField] private FingerTarget[] fingerTargets;
    [SerializeField, ReadOnly] private Vector3 palmSize;
    [SerializeField] private float debugRadius = 0.05f;
    [SerializeField] private int fingerTest = 0, triangleTest = 0;
    [SerializeField] private bool drawFingerData, useTriangleTest;
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
        Gizmos.DrawWireMesh(Resources.GetBuiltinResource<Mesh>("Cube.fbx"), palm.position + (palm.forward * (palmSize.z * 0.5f)), palm.rotation, palmSize);

        DrawIntersectionData();
        DrawFingerIntersectionData();
    }

    private void DrawFingerIntersectionData() {
        if (!inTestRange) return;

        MeshTriangleGraph gra = (MeshTriangleGraph)graph;
        TriangleNode triNode = gra.triangleNodes[lastPalmTriangle];

        fingerTest = Mathf.Clamp(fingerTest, 0, fingerData.Length-1);

        Vector3 pPos    = fingerData[fingerTest].bones[0].position;
        Vector3 pNormal = fingerData[fingerTest].bones[0].right;

        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.yellow;
        style.fontSize = 18;

        Handles.color = Color.yellow;
        Handles.DrawWireDisc(pPos, pNormal, fingerData[fingerTest].length);

        if (GetFingerPoint(fingerTest, useTriangleTest ? triangleTest : lastPalmTriangle, out Vector3 grabPoint, out List<Vector3> tracedPoints)) {
            Transform root = fingerData[fingerTest].bones[0];
            Quaternion offsetRot = root.rotation * Quaternion.Euler(90, 0, 0);
            
            Vector3 rootPos = root.position;
            Vector3 right = offsetRot * -Vector3.right;
            Vector3 up    = offsetRot * Vector3.up;

            for (int i = 0; i < tracedPoints.Count; i++) {
                Vector3 point = tracedPoints[i];

                float angle = Geometry.Split180(Geometry.PointToAngle(rootPos, point, right, up));

                Handles.Label(point, angle.ToString("0.00"), style);

                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(point, debugRadius * 0.5f);
            }

            Gizmos.color = Color.blue;
            Gizmos.DrawLineStrip(tracedPoints.ToArray(), false);

            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(grabPoint, debugRadius);
        }
    }

    private void DrawFingerData() {
        if (!drawFingerData) return;

        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.fontSize = 18;

        for (int i = 0; i < fingerData.Length; i++) {
            Transform root = fingerData[i].bones[0];
            Quaternion offsetRot = root.rotation * Quaternion.Euler(90, 0, 0);
            
            Vector3 rootPos = root.position;
            Vector3 right = offsetRot * -Vector3.right;
            Vector3 up    = offsetRot * Vector3.up;
            Vector3 forward = offsetRot * Vector3.forward;
            Vector3 upDiff = up * fingerData[i].length;
            Vector3 forwardDiff = forward * fingerData[i].length;
            
            Vector3 top = rootPos + upDiff;
            Vector3 bot = rootPos - upDiff;
            Vector3 front = rootPos + forwardDiff;

            float topAngle = Geometry.Split180(Geometry.PointToAngle(rootPos, top, right, up));
            float botAngle = Geometry.Split180(Geometry.PointToAngle(rootPos, bot, right, up));
            float forwardAngle = Geometry.Split180(Geometry.PointToAngle(rootPos, front, right, up));

            Vector3 testTarget = Geometry.AngleToPoint(fingerTargets[i].angle, fingerData[i].length * fingerTargets[i].length, right, up, rootPos);

            FingerData finger = fingerData[i];

            Handles.Label(top, topAngle.ToString("0.00"), style);
            Handles.Label(bot, botAngle.ToString("0.00"), style);
            Handles.Label(front, forwardAngle.ToString("0.00"), style);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(testTarget, debugRadius);
        }
    }

    private void DrawIntersectionData() {
        if (!inTestRange) return;

        Vector3 closestToPalm = graph.GetNodeClosestPointToWS(grabbable.transform, palm.position, lastPalmTriangle);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(closestToPalm, debugRadius);

        triangleTest = Mathf.Clamp(triangleTest, 0, graph.Size-1);

        if (useTriangleTest) {
            Triangle tri = Triangle.Transform(((MeshTriangleGraph)graph).triangles[triangleTest], grabbable.transform);

            Vector3[] vertices = new Vector3[] { tri.v0, tri.v1, tri.v2};

            Gizmos.color = Color.white;
            Gizmos.DrawLineStrip(vertices, true);
        }
    }

    private void UpdateIntersectionPoints() {
        if (!inTestRange) return;

        triangleTest = Mathf.Clamp(triangleTest, 0, graph.Size-1);

        AStarSearch.GetPath(graph, grabbable.transform, lastPalmTriangle, palm.position, out List<PathNode> path);
        PathNode result = path[path.Count-1];
        lastPalmTriangle = result.index;

        Vector3 surfaceNormal = graph.GetNodeNormalWS(grabbable.transform, lastPalmTriangle);
        Vector3 surfaceDir    = -Vector3.Cross(surfaceNormal, palm.forward);        
    }

    private bool GetFingerPoint(int finger, int startStep, out Vector3 point, out List<Vector3> points) {
        point = Vector3.positiveInfinity;
        points = null;

        if (finger < 0 || finger > fingerData.Length-1) return false;

        points = new List<Vector3>();

        float highestAngle = float.NegativeInfinity;

        Transform root = fingerData[finger].bones[0];
        Quaternion offsetRot = root.rotation * Quaternion.Euler(90, 0, 0);
            
        Vector3 rootPos = root.position;
        Vector3 right = offsetRot * -Vector3.right;
        Vector3 up    = offsetRot * Vector3.up;
        float radius = fingerData[finger].length;

        bool step = true;
        int currentStep = startStep;

        while(step) {
            step = false;

            Triangle tri = Triangle.Transform(((MeshTriangleGraph)graph).triangles[currentStep], grabbable.transform);
            Vector3 triCenter = (tri.v0 + tri.v1 + tri.v2) / 3f;
            
            if (Geometry.CircleTriangleIntersection(tri.v0, tri.v1, tri.v2, triCenter, tri.normal, rootPos, up, right, radius, out Vector3 i0, out Vector3 i1, out bool i0Intersects, out bool i1Intersects)) {
                if (i0Intersects && (points.Count > 0 && (points[points.Count-1] - i0).sqrMagnitude > 0.000025f || points.Count == 0)) points.Add(i0);
                if (i1Intersects && (points.Count > 0 && (points[points.Count-1] - i1).sqrMagnitude > 0.000025f || points.Count == 0)) points.Add(i1);

                float angle = 0;

                if (i0Intersects) {
                    angle = Geometry.Split180(Geometry.PointToAngle(rootPos, i0, right, up));
                    if (angle > highestAngle) { highestAngle = angle; point = i0; }
                }

                if (i1Intersects) {
                    angle = Geometry.Split180(Geometry.PointToAngle(rootPos, i1, right, up));
                    if (angle > highestAngle) { highestAngle = angle; point = i1; }
                }
                    
                Vector3 intersectionPointCenter = (i0 + i1) * 0.5f;
                Vector3 surfaceClockwiseDir = -Vector3.Cross(tri.normal, -right);

                if (graph.StepTowards(grabbable.transform, rootPos, -right, surfaceClockwiseDir, intersectionPointCenter, currentStep, out currentStep, out Vector3 intersection)) {
                    step = currentStep == startStep || Vector3.Distance(intersection, rootPos) >= radius ? false : true;
                }
            }
        }

        return true;
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

    private void SetFingerPosition(int finger, Vector3 target) {
        
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
        fingerTargets = new FingerTarget[fingers.Length];

        for (int i = 0; i < fingers.Length; i++) {
            FingerReferenceData frd = fingers[i];

            List<Transform> bones = new List<Transform>();
            List<Quaternion> boneRotations = new List<Quaternion>();
            List<float> boneLengths = new List<float>();

            Transform probe = frd.root;

            fingerTargets[i] = new FingerTarget() { length = 1, angle = 90};
            

            float length = 0;

            while (probe != null && probe != frd.tip) {
                Vector3 euler = probe.localRotation.eulerAngles;
                boneRotations.Add(Quaternion.Euler(0, euler.y, euler.z));

                Transform child = probe.GetChild(0);
                float boneLength = (probe.position - child.position).magnitude;
                length += boneLength;
                
                bones.Add(probe);
                boneLengths.Add(boneLength);

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
