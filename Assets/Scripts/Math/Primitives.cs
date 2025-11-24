using System;
using UnityEngine;

namespace Arielado.Math.Primitives {
    public struct Line : IEquatable<Line> {
        private readonly Vector3 p0, p1;
        private readonly Vector3 direction;
        private readonly float length;

        public Line(Vector3 p0, Vector3 p1) {
            this.p0 = p0;
            this.p1 = p1;

            Vector3 dir = p0 - p1;

            length = dir.magnitude;
            direction = dir.normalized;
        }

        public Vector3 P0 => p0;
        public Vector3 P1 => p1;

        public Vector3 Direction => direction;
        public float Length => length;

        public override int GetHashCode() => 
            ((p0 + p1) / 2f).GetHashCode(); 

        public override bool Equals(object other) {
            if (other is Line) 
                return Equals((Line)other);

            return false;
        }

        public bool Equals(Line other) =>
            (p0 == other.p0 && p1 == other.p1) || 
            (p0 == other.p1 && p1 == other.p0);    
            
    }
    public struct Triangle {
        private readonly Vector3 v0, v1, v2;
        private readonly Vector3 normal, normalScaled;
        public Triangle(Vector3 v0, Vector3 v1, Vector3 v2) {
            this.v0 = v0;
            this.v1 = v1;
            this.v2 = v2;

            normalScaled = Vector3.Cross(v1 - v0, v2 - v0);
            normal = normalScaled.normalized;
        }

        public Vector3 V0 => v0;
        public Vector3 V1 => v1;
        public Vector3 V2 => v2;
        public Vector3 Normal => normal;
        public Vector3 NormalScaled => normalScaled;

        public static bool Intersects(Vector3 origin, Vector3 dir, 
                                      Triangle tri,
                                      out TriangleIntersectionData intersection) {
            bool isIntersecting = Geometry.RayTriangleIntersection(origin, dir, tri.v0, tri.v1, tri.v2, out Vector3 point, out float t, out float u, out float v, out float w);
            intersection = new TriangleIntersectionData(point, u, v, w, t);
            return isIntersecting;
        }
    }

    public struct TriangleIntersectionData {
        private readonly Vector3 intersection;
        private readonly float u, v, w, t;

        public TriangleIntersectionData(Vector3 intersection, float u, float v, float w, float t) {
            this.intersection = intersection;
            this.u = u;
            this.v = v;
            this.w = w;
            this.t = t;
        }

        public Vector3 Intersection => intersection;
        public float U => u;
        public float V => v;
        public float W => w;
        public float T => t;
    }
}
