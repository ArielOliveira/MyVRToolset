using UnityEngine;

namespace Arielado.Math {
    public static class Interpolation {
        public static float SmoothLerp(float a, float b, float decay, float dt) =>
            b + (a - b) * Mathf.Exp(-decay*dt);
        public static Vector3 SmoothLerp(Vector3 a, Vector3 b, float decay, float dt) =>
            b + (a - b) * Mathf.Exp(-decay*dt);
        public static Quaternion SmoothLerp(Quaternion a, Quaternion b, float decay, float dt) =>
            Quaternion.Lerp(a, b, 1 - Mathf.Exp(-decay*dt));

        public static float SmoothStep(float edge0, float edge1, float input) {
            if (input < edge0)
                return 0;

            if (input >= edge1)
                return 1;

            input = (input - edge0) / (edge1 - edge0);

            return input * input * (3 - 2 * input);
        }
    }
}
