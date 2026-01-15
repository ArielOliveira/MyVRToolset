using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace Arielado.FABRIK {
    [System.Serializable]
    public struct IKChain {
        public Vector3 origin;
        public Transform[] joints;
        public Vector3[] lastPositions;
        public Vector3[] positions;
        public Quaternion[] rotations;
        public float[] lengths;
        public float length;

        public static void Solve(ref IKChain chain, Transform target) => FABRIK.Solve(ref chain, target);
    }

    public static class FABRIK {
        public const int ITERATIONS  = 10;
        public const float TOLERANCE = 0.001f;
        
        public static bool IsReachable(ref IKChain chain, Transform target) => 
            Vector3.Distance(target.position, chain.origin) <= chain.length;
        public static bool IsWithinReach(ref IKChain chain, Transform target) =>
            Vector3.Distance(target.position, chain.joints[chain.joints.Length-1].position) <= TOLERANCE;

        public static Vector3 Constrain(ref IKChain chain, Vector3 position, Vector3 coneVector, Matrix4x4 rotor, int index) {
            float dot = Vector3.Dot(position, coneVector) / coneVector.magnitude;
            Vector3 projected = coneVector.normalized * dot;
            
            Vector3 top = rotor * Vector3.up;
            Vector3 ri = rotor * Vector3.right;

            Vector3 upVec    = (top - position).magnitude < (-top - position).magnitude ? top : -top;
            Vector3 rightVec = (ri - position).magnitude < (-ri - position).magnitude ? ri : -ri;

            Vector3 adjust = position - projected;
            if (dot < 0) projected = -projected;

            float xAspect = Vector3.Dot(adjust, rightVec) * Mathf.Sign(Vector3.Dot(ri, rightVec));
            float yAspect = Vector3.Dot(adjust, upVec) * Mathf.Sign(Vector3.Dot(top, upVec));

            float projMagnitude = projected.magnitude;

            float left  = -(projMagnitude * Mathf.Tan(89f * Mathf.Deg2Rad));
            float right =   projMagnitude * Mathf.Tan(89f * Mathf.Deg2Rad);
            float up    =   projMagnitude * Mathf.Tan(89f * Mathf.Deg2Rad);
            float down  = -(projMagnitude * Mathf.Tan(89f * Mathf.Deg2Rad));

            float xBound = xAspect >= 0 ? right : left;
            float yBound = yAspect >= 0 ? up : down;

            Vector3 result = position;

            float ellipse = ((xAspect*xAspect)/(xBound*xBound)) + ((yAspect*yAspect)/(yBound*yBound));
            bool inbounds = ellipse <= 1 && dot >= 0;

            if (!inbounds) {
                float a = Mathf.Atan2(yAspect, xAspect);
                float x = xBound * Mathf.Cos(a);
                float y = yBound * Mathf.Sin(a);

                result = (projected + rightVec * x + upVec * y).normalized * position.magnitude;
            }

            return result;
        }

        public static void Solve(ref IKChain chain, Transform target) {
            bool applyTransforms = false;

            if (IsReachable(ref chain, target)) {
                for (int i = 0; i < ITERATIONS && !IsWithinReach(ref chain, target); i++) {
                    // Set tip to target and propagate displacement to rest of chain
                    chain.positions[chain.positions.Length-1] = target.position;
                    Vector3 coneVec = (chain.positions[chain.positions.Length-2] - chain.positions[chain.positions.Length-1]).normalized;
                    for (int j = chain.positions.Length - 2; j > -1; --j) {
                        Vector3 goalPos = chain.positions[j + 1] + ((chain.positions[j] - chain.positions[j + 1]).normalized * chain.lengths[j]);
                        Matrix4x4 rotor = Matrix4x4.TRS(chain.positions[j+1], Quaternion.LookRotation(chain.positions[j+1] + coneVec), Vector3.one);
                        Vector3 t = Constrain(ref chain, goalPos - chain.positions[j+1], coneVec, rotor, j);
                        chain.positions[j] = chain.positions[j+1] + t;
                        coneVec = chain.positions[j] - chain.positions[j+1];
                    }

                    // Backward reaching phase
                    // Set root back at it's original position and propagate displacement to rest of chain
                    chain.positions[0] = chain.origin;
                    coneVec = (chain.positions[1] - chain.positions[0]).normalized;
                    for (int j = 1; j < chain.positions.Length; ++j) {
                        
                        Vector3 goalPos = chain.positions[j - 1] + ((chain.positions[j] - chain.positions[j - 1]).normalized * chain.lengths[j - 1]);
                        Matrix4x4 rotor = Matrix4x4.TRS(chain.positions[j-1], Quaternion.LookRotation(chain.positions[j-1] + coneVec), Vector3.one);
                        Vector3 t = Constrain(ref chain, goalPos - chain.positions[j-1], coneVec, rotor, j);
                        
                        chain.positions[j] = chain.positions[j-1] + t;
                        coneVec = chain.positions[j] - chain.positions[j-1];
                    }
                }                

                applyTransforms = true;
            } else if (!IsWithinReach(ref chain, target)) {
                for (int i = 0; i < chain.positions.Length-1; i++) {
                    float r = Vector3.Distance(target.position, chain.positions[i]);
                    float l = chain.lengths[i] / r;

                    chain.positions[i+1] = (1 - l) * chain.positions[i] + l * target.position;
                }

                applyTransforms = true;
            }

            if (applyTransforms) {
                for (int i = 0; i < chain.joints.Length-1; i++) {
                    Vector3 prevDir = chain.lastPositions[i+1] - chain.lastPositions[i];
                    Vector3 newDir  = chain.positions[i+1] - chain.positions[i];

                    chain.joints[i].rotation = Quaternion.FromToRotation(prevDir, newDir) * chain.rotations[i];
                }
            }
        }

        public static IKChain CreateChain(Transform root, Transform tip) {
                List<Vector3> positions = new List<Vector3>();
                List<Transform> joints = new List<Transform>();
                List<Quaternion> rotations = new List<Quaternion>();
                List<float> boneLengths = new List<float>();

                Transform probe = root;                

                float length = 0;

                while (probe != null && probe != tip) {
                    Transform child = probe.GetChild(0);

                    float boneLength = Vector3.Distance(probe.position, child.position);
                    length += boneLength;
                    
                    joints.Add(probe);
                    boneLengths.Add(boneLength);
                    positions.Add(probe.position);
                    rotations.Add(probe.rotation);

                    probe = child;
                }

                joints.Add(tip);
                rotations.Add(tip.rotation);
                positions.Add(tip.position);

                return new IKChain() {
                    origin        = root.position,
                    lastPositions = positions.ToArray(),
                    positions     = positions.ToArray(),
                    joints        = joints.ToArray(),
                    rotations     = rotations.ToArray(),
                    lengths       = boneLengths.ToArray(),
                    length        = length
                };
        }
    }
}
