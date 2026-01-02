using System;
using UnityEngine;

namespace Arielado.Math.Primitives {
    public struct Line : IEquatable<Line> {
        public Vector3 p0, p1;
        public Vector3 direction;
        public float length;

        public Line(Vector3 p0, Vector3 p1) {
            this.p0 = p0;
            this.p1 = p1;

            Vector3 dir = p0 - p1;

            length = dir.magnitude;
            direction = dir.normalized;
        }
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

    [Serializable]
    public struct Triangle {
        public Vector3 v0, v1, v2;
        public Vector3 normal, normalScaled;
        public Triangle(Vector3 v0, Vector3 v1, Vector3 v2) {
            this.v0 = v0;
            this.v1 = v1;
            this.v2 = v2;

            normalScaled = Vector3.Cross(v1 - v0, v2 - v0);
            normal = normalScaled.normalized;
        }

        public static bool Intersects(Vector3 origin, Vector3 dir, 
                                      Triangle tri,
                                      out TriangleIntersectionData intersection) {
            bool isIntersecting = Geometry.RayTriangleIntersection(origin, dir, tri.v0, tri.v1, tri.v2, out Vector3 point, out float t, out float u, out float v, out float w);
            intersection = new TriangleIntersectionData(point, u, v, w, t);
            return isIntersecting;
        }

        public static Vector3 ClosestPointTo(Triangle tri, Vector3 target) =>
            Geometry.ClosestPointOnTriangle(tri.v0, tri.v1, tri.v2, target);

        public static Vector3 ClosestPointTo(Triangle tri, Vector3 target, Transform transform) =>
            Geometry.ClosestPointOnTriangle(transform.TransformPoint(tri.v0),
                                            transform.TransformPoint(tri.v1),
                                            transform.TransformPoint(tri.v2), 
                                            target);
    }

    public struct TriangleIntersectionData {
        public Vector3 intersection;
        public float u, v, w, t;

        public TriangleIntersectionData(Vector3 intersection, float u, float v, float w, float t) {
            this.intersection = intersection;
            this.u = u;
            this.v = v;
            this.w = w;
            this.t = t;
        }
    }
}
