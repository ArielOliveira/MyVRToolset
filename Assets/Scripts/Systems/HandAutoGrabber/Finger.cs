using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Arielado.Math;
using Arielado;

public class Finger : MonoBehaviour {
    [SerializeField] private Transform root;
    [SerializeField] private Transform tip;
    [SerializeField] private FingerPointsGenerator generator;
    [SerializeField] private List<Matrix4x4> anchors;
    [SerializeField] private List<Vector3> anchorEulers;
    [SerializeField] private List<Transform> bones;
    [SerializeField] private List<float> boneLengths;
    [SerializeField] private List<Color> boneColors;
    [SerializeField] private float fingerLength;
    [SerializeField] private bool useGeneratedPoints, autoRegeneratePoints;
    
    [SerializeField] private List<float> targetAngles, solvedAngles, generatedTargetAngles;

    [SerializeField] private List<Vector3> generatedTargetPoints;

    [Range(0.001f, 0.1f)] public float debugSphereRadius;

    #if UNITY_EDITOR
    private void OnDrawGizmos() {
        if (root == null || tip == null || bones == null || boneLengths == null) return;

        Handles.color = Color.cyan * 0.25f;

        Vector3 rootRight =  anchors[0].MultiplyVector(Vector3.right);
        Vector3 rootUp    = -anchors[0].MultiplyVector(Vector3.up);

        if (autoRegeneratePoints) SetupPoints();

        float circleLength = 0;
        List<float> refAngles = (useGeneratedPoints && generatedTargetAngles.Count == bones.Count) ? generatedTargetAngles : targetAngles;

        for (int i = 0; i < bones.Count; i++) {
            circleLength += boneLengths[i];

            float lowest = refAngles[i];

            if (generatedTargetAngles.Count == bones.Count) {
                Gizmos.color = Color.red;
                Vector3 grabPoint = Geometry.CirclePointFromAngle(generatedTargetAngles[i], circleLength, rootRight, rootUp, root.position);
                Gizmos.DrawWireSphere(grabPoint, debugSphereRadius * 2f);
            }

            for (int j = i+1; j < bones.Count; j++) {
                if (refAngles[j] < lowest)
                    lowest = refAngles[j];
            }

            Vector3 right =  anchors[i].MultiplyVector(Vector3.right);
            Vector3 up    = -anchors[i].MultiplyVector(Vector3.up);

            if (i > 0) {
                right = bones[i-1].TransformDirection(right);
                up    = bones[i-1].TransformDirection(up);
            }

            Vector3 target = Geometry.CirclePointFromAngle(refAngles[i], circleLength, rootRight, rootUp, root.position);
            Vector3 solvedTarget = Geometry.CirclePointFromAngle(lowest, circleLength, rootRight, rootUp, root.position);
            float alignedAngle = Geometry.CirclePointToAngle(bones[i].position, solvedTarget, right, up);
            Vector3 alignedPoint = Geometry.CirclePointFromAngle(alignedAngle, boneLengths[i], right, up, bones[i].position);

            solvedAngles[i] = alignedAngle;

            Handles.color = boneColors[i];
            //Handles.DrawWireDisc(root.position, root.right, circleLength);
            Handles.DrawWireDisc(bones[i].position, right, boneLengths[i]);

            Gizmos.color = boneColors[i];
            Gizmos.DrawSphere(alignedPoint, debugSphereRadius);

            Gizmos.color = Color.black;
            Gizmos.DrawWireSphere(target, debugSphereRadius);

            Matrix4x4 local = anchors[i] * anchors[i].inverse;
            Vector3 euler = local.rotation.eulerAngles;

            Quaternion rot = Quaternion.Euler(alignedAngle, anchorEulers[i].y, anchorEulers[i].z);

            if (enabled)
                bones[i].localRotation = rot;
        }

        Gizmos.color = Color.white;
    }
    #endif

    public void SetupPoints() {
        List<Vector3> points = generator.GetFingerPoints();

        Vector3 rootRight =  anchors[0].MultiplyVector(Vector3.right);
        Vector3 rootUp    = -anchors[0].MultiplyVector(Vector3.up);

        generatedTargetAngles.Clear();
        Vector3 originPoint = root.position;

        int j = 0;

        for (int i = 0; (i < points.Count) && (j < bones.Count); i++) {
            float dist = Vector3.Distance(points[i], originPoint);

            if (dist >= boneLengths[j]) {
                Debug.Log("Adding target to bone: " + j);

                generatedTargetAngles.Add(Geometry.CirclePointToAngle(root.position, points[i], rootRight, rootUp));
                
                originPoint = points[i];
                j++;
            }
        }
    }

    public void SetupBones() {
        if (bones == null) bones = new List<Transform>();
        if (boneLengths == null) boneLengths = new List<float>();
        if (targetAngles == null) targetAngles = new List<float>();
        if (boneColors == null) boneColors = new List<Color>();
        if (solvedAngles == null) solvedAngles = new List<float>();
        if (anchors == null) anchors = new List<Matrix4x4>();
        if (anchorEulers == null) anchorEulers = new List<Vector3>();

        if (root == null || tip == null) return;

        fingerLength = 0;

        Transform probe = root;
        bones.Clear();
        boneLengths.Clear();
        targetAngles.Clear();
        boneColors.Clear();
        solvedAngles.Clear();
        anchors.Clear();
        anchorEulers.Clear();

        while (probe != null && probe != tip) {
            Vector3 euler = probe.localRotation.eulerAngles;
            probe.localRotation = Quaternion.Euler(0, euler.y, euler.z);
            anchors.Add(probe.localToWorldMatrix);
            anchorEulers.Add(euler);

            bones.Add(probe);

            Transform child = probe.GetChild(0);            

            float length = (probe.position - child.position).magnitude;
            fingerLength += length;

            boneLengths.Add(length);
            targetAngles.Add(0);
            solvedAngles.Add(0);
            boneColors.Add(Color.white);

            probe = child;
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(Finger))]
public class FingerEditor : Editor {
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        Finger finger = (Finger)target;

        if (GUILayout.Button("Setup Fingers")) {
            finger.SetupBones();
        }

        if (GUILayout.Button("Setup Grab Points")) {
            finger.SetupPoints();
        }
    }
}
#endif
