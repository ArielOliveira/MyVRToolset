using UnityEngine;

namespace Arielado.Math {
    public static class Geometry {
        public static bool RayPlaneIntersection(Vector3 pNormal, Vector3 pPoint, Vector3 rayOrigin, Vector3 rayDir, out float t) {
            // assuming vectors are all normalized
            float denom = Vector3.Dot(pNormal, rayDir);

            if (denom > 1e-6) {
                Vector3 p0l0 = pPoint - rayOrigin;
                t = Vector3.Dot(p0l0, pNormal) / denom;

                return t >= 0f;
            }

            t = float.MaxValue;

            return false;
        }

        public static bool LinePlaneIntersection(Vector3 p0, Vector3 p1, Vector3 pPos, Vector3 pNormal, out Vector3 intersection, out float normalizedLinePoint) {
            Vector3 diff = p1 - p0;
            Vector3 rayDir = diff.normalized;

            float length = diff.magnitude;

            bool planeResult = RayPlaneIntersection(pNormal, pPos, p0, rayDir, out float dist);
            bool hasIntersected = planeResult && dist <= length;

            dist = Mathf.Clamp(dist, 0, length);
            normalizedLinePoint = Mathf.Abs(dist) / length;
            
            intersection = p0 + rayDir * dist;

            return hasIntersected;
        }

        public static bool LineTrianglePlaneIntersection(Vector3 lhs, Vector3 rhs, Vector3 pNormal, float trianglePlane, out Vector3 iPoint) {
            float d0 = Vector3.Dot(pNormal, lhs) + trianglePlane;
            float d1 = Vector3.Dot(pNormal, rhs) + trianglePlane;
            
            iPoint = Vector3.zero;

            bool lhsI = Mathf.Abs(d0) < float.Epsilon;
            bool rhsI = Mathf.Abs(d1) < float.Epsilon;

            if (lhsI && rhsI) {
                iPoint = (lhs + rhs) / 2;

                return true;
            }

            if (d0*d1 > float.Epsilon) return false; // same side of the plane

            float t = d0 / (d0 - d1); // 'time' of intersection point on the segment
            iPoint = (lhs + t * (rhs - lhs));

            return true;
        }    

         // https://forum.unity.com/threads/how-do-i-find-the-closest-point-on-a-line.340058/
        public static Vector3 LineNearestPoint(Vector3 start, Vector3 end, Vector3 pnt) {
            var line = end - start;
            var len = line.magnitude;
            line.Normalize();
   
            var v = pnt - start;
            var d = Vector3.Dot(v, line);
            d = Mathf.Clamp(d, 0f, len);
            
            return start + line * d;
        }

        public static Vector3 MidPoint(Vector3 lhs, Vector3 rhs) {
            Vector3 midPoint = Vector3.zero;

            midPoint.x = (lhs.x + rhs.x) * 0.5f;
            midPoint.y = (lhs.y + rhs.y) * 0.5f;
            midPoint.z = (lhs.z + rhs.z) * 0.5f;
        
            return midPoint;
        }

         // Assuming everything is at origin and identity rotation
        public static bool LineCircleIntersection(float r, Vector3 center, Vector3 l0, Vector3 l1, Vector3 right, Vector3 up, out Vector3 p0, out Vector3 p1) {
            p0 = Vector3.positiveInfinity;
            p1 = Vector3.positiveInfinity;

            Quaternion rot = Quaternion.LookRotation(right, up);
            Matrix4x4 rotationMat = Matrix4x4.Rotate(rot);

            center = rotationMat.inverse.MultiplyPoint(center);
            l0 = rotationMat.inverse.MultiplyPoint(l0);
            l1 = rotationMat.inverse.MultiplyPoint(l1);

            // Compute the direction vector of the line segment
            Vector3 dir = l1 - l0;

            // Compute the coefficients of the quadratic equation
            float a = dir.x * dir.x + dir.y * dir.y;
            float b = 2 * (dir.x * (l0.x - center.x) + dir.y * (l0.y - center.y));
            float c = center.x * center.x + center.y * center.y;
            c += l0.x * l0.x + l0.y * l0.y;
            c -= 2 * (center.x * l0.x + center.y * l0.y);
            c -= r * r;

            // Compute the discriminant
            float discriminant = b * b - 4 * a * c;

            // If the discriminant is negative, there are no intersections
            if (Mathf.Abs(a) < float.Epsilon || discriminant < 0) return false;

            // Compute the values of t at the points of intersection
            float t1 = (-b + Mathf.Sqrt(discriminant)) / (2 * a);
            float t2 = (-b - Mathf.Sqrt(discriminant)) / (2 * a);

            // Calculate the intersection points
            //if (t1 >= 0 && t1 <= 1)
            float intersectionX1 = l0.x + t1 * (l1.x - l0.x);
            float intersectionY1 = l0.y + t1 * (l1.y - l0.y);
            p0 = rotationMat.MultiplyPoint(new Vector3(intersectionX1, intersectionY1, l0.z));
            
            float intersectionX2 = l0.x + t2 * (l1.x - l0.x);
            float intersectionY2 = l0.y + t2 * (l1.y - l0.y);
            p1 = rotationMat.MultiplyPoint(new Vector3(intersectionX2, intersectionY2, l1.z));

            return true;
        }

        // https://www.scratchapixel.com/lessons/3d-basic-rendering/ray-tracing-rendering-a-triangle/barycentric-coordinates.html
        public static bool RayTriangleIntersection(Vector3 origin, Vector3 dir, 
                                                    Vector3 v0, Vector3 v1, Vector3 v2,
                                                    out Vector3 intersection,
                                                    out float t, out float u, out float v, out float w) {
            
            intersection = Vector3.negativeInfinity;
            t = float.NegativeInfinity;
            u = float.NegativeInfinity;
            v = float.NegativeInfinity;
            w = float.NegativeInfinity;

            // Step 1: Compute triangle's plane normal and intersection
            Vector3 v0v1 = v1 - v0;
            Vector3 v0v2 = v2 - v0;
            // no need to normalize
            Vector3 normal = Vector3.Cross(v0v1, v0v2);

            float normalDotRayDirection = Vector3.Dot(normal, dir);

            if (Mathf.Abs(normalDotRayDirection) < float.Epsilon) return false; // Ray is parallel to the triangle

            float denom = Vector3.Dot(normal, normal);
            float dist = -Vector3.Dot(normal, v0);

            t = -(Vector3.Dot(normal, origin) + dist) / normalDotRayDirection;

            if (t < 0) return false;

            intersection = origin + dir * t;

            // Step 2: Inside-outside test using barycentric coordinates
            Vector3 c;

            Vector3 v1p = intersection - v1;
            Vector3 v1v2 = v2 - v1;
            c = Vector3.Cross(v1v2, v1p);
            u = Vector3.Dot(normal, c);
            if (u < 0) return false; // intersection is on the left side

            Vector3 v2p = intersection - v2;
            Vector3 v2v0 = v0 - v2;
            c = Vector3.Cross(v2v0, v2p);
            v = Vector3.Dot(normal, c);
            if (v < 0) return false; // intersection is on the right side
            
            Vector3 v0p = intersection - v0;
            c = Vector3.Cross(v0v1, v0p);
            if (Vector3.Dot(normal, c) < 0) return false; // intersection is on the right side

            u /= denom;
            v /= denom;
            
            w = 1 - u - v;

            return true;
        }

        public static Vector3 GetBarycentricCoordinates(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 point) {
            Vector3 v0 = p2 - p1;
            Vector3 v1 = p3 - p1;
            Vector3 v2 = point - p1;

            float d00 = Vector3.Dot(v0, v0);
            float d01 = Vector3.Dot(v0, v1);
            float d11 = Vector3.Dot(v1, v1);
            float d20 = Vector3.Dot(v2, v0);
            float d21 = Vector3.Dot(v2, v1);
            float denom = d00 * d11 - d01 * d01;

            float v = (d11 * d20 - d01 * d21) / denom;
            float w = (d00 * d21 - d01 * d20) / denom;
            float u = 1f - v - w;

            return new Vector3(u, v, w);
        }

        public static bool IsPointInsideTriangle(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 p) {
            return SameSide(p, v0, v1, v2) && SameSide(p, v1, v0, v2) && SameSide(p, v2, v0, v1);
        }

        public static bool SameSide(Vector3 p0, Vector3 p1, Vector3 a, Vector3 b) {
            Vector3 cp0 = Vector3.Cross(b-a, p0-a);
            Vector3 cp1 = Vector3.Cross(b-a, p1-a);

            return (Vector3.Dot(cp0, cp1)) >= 0;
        }

        public static Vector3 ClosestPointTo(Vector3 target, Vector3[] points) {
            float distance = float.MaxValue;
            int selectedIndex = 0;

            for (int i = 0; i < points.Length; i++) {
                float sqrMagnitude = (target - points[i]).sqrMagnitude;

                if (sqrMagnitude < distance) {
                    selectedIndex = i;
                    distance = sqrMagnitude;
                }
            }

            return points[selectedIndex];
        }

        public static Vector3 ClosestPointOnTriangle(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 point) {
            // Step 1: Find the normal vector of the plane that contains the triangle
            Vector3 normal = Vector3.Cross(p2 - p1, p3 - p1).normalized;

            // Step 2: Find the equation of the plane that contains the triangle
            float d = -Vector3.Dot(normal, p1);
            Plane plane = new Plane(p1, p2, p3);
            

            // Step 3: Project the point onto the plane
            Vector3 projectedPoint = plane.ClosestPointOnPlane(point);

            // Step 4: Check if the projected point is inside the triangle
            Vector3 barycentric = GetBarycentricCoordinates(p1, p2, p3, projectedPoint);
            if (barycentric.x >= 0f && barycentric.y >= 0f && barycentric.z >= 0f) {
                // The projected point is inside the triangle  
                
                return projectedPoint;
            }

            // Step 5: Find the closest point on each edge
            Vector3 c1 = LineNearestPoint(p1, p2, point);
            Vector3 c2 = LineNearestPoint(p2, p3, point);
            Vector3 c3 = LineNearestPoint(p3, p1, point);

            // Step 6: Calculate the distance from the point to each of these closest points on the edges
            float mag1 = Vector3.Distance(point, c1);
            float mag2 = Vector3.Distance(point, c2);
            float mag3 = Vector3.Distance(point, c3);

            float min = Mathf.Min(mag1, mag2);
            min = Mathf.Min(min, mag3);

            if (min == mag1)
                return c1;
            else if (min == mag2)
                return c2;
            else
                return c3;
        }

         public static float CirclePointToAngle(Vector3 origin, Vector3 target, Vector3 right, Vector3 up) {
            Vector3 centerToPoint = origin - target;

            Quaternion rot = Quaternion.LookRotation(right, up);
            Matrix4x4 rotationMat = Matrix4x4.Rotate(rot);

            centerToPoint = rotationMat.inverse.MultiplyPoint(centerToPoint).normalized;

            float theta = (Mathf.Atan2(centerToPoint.y, centerToPoint.x) * Mathf.Rad2Deg) + 180f;

            if (theta < 0) theta += 360f;

            return theta % 360f;
        }

        public static Vector3 CirclePointFromAngle(float angle, float radius, Vector3 right, Vector3 up) {
            Quaternion rot = Quaternion.LookRotation(right, up);
            Matrix4x4 rotationMat = Matrix4x4.Rotate(rot);

            Vector3 P = Vector3.zero;

            angle *= Mathf.Deg2Rad;

            P.x = radius * Mathf.Cos(angle);
            P.y = radius * Mathf.Sin(angle);

            P = rotationMat.MultiplyPoint(P);

            return P;
        }

        public static Vector3 CirclePointFromAngle(float angle, float radius, Vector3 right, Vector3 up, Vector3 origin) =>
             origin + CirclePointFromAngle(angle, radius, right, up);

        public static bool CircleTriangleIntersection(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 triCenter, Vector3 triNormal, Vector3 cPos, Vector3 cUp, Vector3 cRight, float radius,
                                                      out Vector3 i0, out Vector3 i1, out bool i0Intersects, out bool i1Intersects) {
            
            i0 = Vector3.negativeInfinity;
            i1 = Vector3.negativeInfinity;
            i0Intersects = false;
            i1Intersects = false;

            //////// Step 1: Circle intersects triangle plane ///////////////////////////
            Vector3 circleTriPlane = Vector3.Cross(triNormal, cRight);
            Vector3 circleToTriUp = Vector3.Cross(cRight, circleTriPlane);

            float normalAngle = CirclePointToAngle(Vector3.zero, -triNormal, cRight, cUp);
            Vector3 anglePoint = CirclePointFromAngle(normalAngle, radius, cRight, cUp, cPos);

            // Intersection line crosses circle vertically towards the triangle plane
            Vector3 l0 = cPos + (circleToTriUp * radius);
            Vector3 l1 = anglePoint;

            bool triPlaneIntersection = LinePlaneIntersection(l0, l1, triCenter, -triNormal, out Vector3 planeIntersection, out float normalizedLinePoint);
            
            //////// Step 2: Find the two points on the circle that intersects the triangle plane
            if (!triPlaneIntersection) return false;

            // Intersection line crosses circle horizontally along the triangle plane
            Vector3 segment0 = planeIntersection - (circleTriPlane * radius);
            Vector3 segment1 = planeIntersection + (circleTriPlane * radius);

            LineCircleIntersection(radius, cPos, segment0, segment1, cRight, cUp, out i0, out i1);

            // Step 3: Find if any of the two points are inside the triangle
            i0Intersects = IsPointInsideTriangle(v0, v1, v2, i0);
            i1Intersects = IsPointInsideTriangle(v0, v1, v2, i1);

            if (i0Intersects && i1Intersects) return true;

            // Step 4: Else, find the closest point at the edge of the triangle
            // and test if is within the circle radius
            float trianglePlane = -Vector3.Dot(cRight, i0);

            bool ltp0 = LineTrianglePlaneIntersection(v0, v1, cRight, trianglePlane, out Vector3 lp0);
            bool ltp1 = LineTrianglePlaneIntersection(v0, v2, cRight, trianglePlane, out Vector3 lp1);
            bool ltp2 = LineTrianglePlaneIntersection(v1, v2, cRight, trianglePlane, out Vector3 lp2);

            Vector3[] edgePoints = new Vector3[] { lp0, lp1, lp2 };

            if (!i0Intersects) {
                Vector3 closest = ClosestPointTo(i0, edgePoints);
                i0Intersects = Vector3.Distance(cPos, closest) <= radius;

                i0 = closest;
            }

            if (!i1Intersects) {
                Vector3 closest = ClosestPointTo(i1, edgePoints);
                i1Intersects = Vector3.Distance(cPos, closest) <= radius;

                i1 = closest;
            }

            return i0Intersects || i1Intersects;
        }
    }
}
