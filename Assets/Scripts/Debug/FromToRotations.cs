using System;
using UnityEditor;
using UnityEngine;

[ExecuteAlways]
public class FromToRotations : MonoBehaviour {
    [Range(0, 179f)] public float leftConstraint, rightConstraint, upConstraint, downConstraint;
    public Transform p0, p1, target, toRotate;
    public Vector3 previousDir;
    public float length = 0.2f;

    private void OnDrawGizmos() {
        if (!p0 || !p1 || !toRotate || !target) return;

        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.fontSize = 18;

        Vector3 coneVector = (p1.position - p0.position);

        Vector3 goalPos = target.position - p1.position;

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(goalPos, 0.05f);
        
        Gizmos.color = Color.white;
        Gizmos.DrawLine(p0.position, p1.position);

        Matrix4x4 rotor = Matrix4x4.TRS(p0.position, Quaternion.LookRotation(coneVector.normalized), Vector3.one);
        float dot = Vector3.Dot(coneVector, goalPos) / coneVector.magnitude;
        Vector3 projected = coneVector.normalized * dot;

        Handles.color = Color.ghostWhite;
        Vector3 pPos = goalPos - projected;

        Vector3 top = rotor * Vector3.up;
        Vector3 ri  = rotor * Vector3.right;

        Vector3 pUp = pPos + top;
        Vector3 pDown = pPos - top;
        Vector3 pRight = pPos + ri;
        Vector3 pLeft = pPos - ri;

        Vector3 upVec    = (top - goalPos).sqrMagnitude < (-top - goalPos).sqrMagnitude ? top : -top;
        Vector3 rightVec = (ri - goalPos).sqrMagnitude < (-ri - goalPos).sqrMagnitude ? ri : -ri;

        if (dot < 0) projected = -projected;

        float projMagnitude = projected.magnitude;

        float xAspect = Vector3.Dot(pPos, rightVec) * Mathf.Sign(Vector3.Dot(ri, rightVec));
        float yAspect = Vector3.Dot(pPos, upVec) * Mathf.Sign(Vector3.Dot(top, upVec));

        float topDist = (pUp - goalPos).magnitude;
        float botDist = (pDown - goalPos).magnitude;
        float riDist  = (pRight - goalPos).magnitude;
        float leDist =  (pLeft - goalPos).magnitude;

        float left  = -(projMagnitude * Mathf.Tan(leftConstraint * Mathf.Deg2Rad));
        float right =   projMagnitude * Mathf.Tan(rightConstraint * Mathf.Deg2Rad);
        float up    =   projMagnitude * Mathf.Tan(upConstraint * Mathf.Deg2Rad);
        float down  = -(projMagnitude * Mathf.Tan(downConstraint * Mathf.Deg2Rad));

        float xBound = xAspect >= 0 ? right : left;
        float yBound = yAspect >= 0 ? up : down;

        float ellipse = ((xAspect*xAspect)/(xBound*xBound)) + ((yAspect*yAspect)/(yBound*yBound));
        bool inBounds = ellipse <= 1 && dot >= 0;

        Vector3 result = goalPos;

        if (!inBounds) {
            float a = Mathf.Atan2(yAspect, xAspect);
            float x = xBound * Mathf.Cos(a);
            float y = yBound * Mathf.Sin(a);

            result = (projected + rightVec * x + upVec * y).normalized * goalPos.magnitude;
        }

        Vector3 upConstr   = p0.position + (top * up);
        Vector3 downConstr = p0.position + (top * down);
        Vector3 lConstr    = p0.position + (ri  * left);
        Vector3 rConstr    = p0.position + (ri  * right);

        Vector3 selectedUp = p0.position + upVec;
        Vector3 selectedRight = p0.position + rightVec;

        Vector3[] verts = new Vector3[] {
            p0.position + ((top - ri)),
            p0.position + ((top + ri)),
            p0.position + ((-top + ri)),
            p0.position + ((-top - ri))
        };

        Handles.DrawSolidRectangleWithOutline(verts, new Color(Color.aquamarine.r, Color.aquamarine.g, Color.aquamarine.b, 0.1f), inBounds ? Color.darkBlue : Color.darkRed);

        style.normal.textColor = Color.white;
        Handles.Label(p0.position + top * 1.6f, "dot: " + dot.ToString("0.00"), style);
        Handles.Label(p0.position + top * 1.4f, "ellipse: " + ellipse.ToString("0.00"), style);

        style.normal.textColor = Color.green;
        //Handles.Label(pUp, topDist.ToString("0.00"), style);
        Handles.Label(upConstr, up.ToString("0.00"), style);
        Handles.Label(selectedUp + top * 0.2f, "yAspect: " + yAspect.ToString("0.0000"), style);
        Handles.Label(selectedUp + top * 0.05f, "yBound: " + yBound.ToString("0.00"), style);
        

        style.normal.textColor = Color.darkGreen;
        //Handles.Label(pDown, botDist.ToString("0.00"), style);
        Handles.Label(downConstr, down.ToString("0.00"), style);

        style.normal.textColor = Color.red;
        Handles.Label(pRight, riDist.ToString("0.00"), style);
        Handles.Label(rConstr, right.ToString("0.00"), style);
        Handles.Label(selectedRight + top * 0.2f, "xAspect: " + xAspect.ToString("0.0000"), style);
        Handles.Label(selectedRight + top * 0.05f, "xBound: " + xBound.ToString("0.00"), style);

        style.normal.textColor = Color.darkRed;
        Handles.Label(pLeft, leDist.ToString("0.00"), style);
        Handles.Label(lConstr, left.ToString("0.00"), style);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(p0.position, p0.position + top);
        Gizmos.DrawSphere(selectedUp, 0.05f);
        Gizmos.DrawWireSphere(upConstr, 0.05f);

        //Gizmos.color = Color.green 
        //Gizmos.DrawWireSphere(selectedUp, 0.1f);

        Gizmos.color = Color.darkGreen;
        Gizmos.DrawLine(p0.position, p0.position - top);
        Gizmos.DrawWireSphere(downConstr, 0.05f);

        Gizmos.color = Color.darkRed;
        Gizmos.DrawLine(p0.position, p0.position + ri);
        Gizmos.DrawSphere(selectedRight, 0.05f);
        Gizmos.DrawWireSphere(rConstr, 0.05f);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(p0.position, p0.position - ri);
        Gizmos.DrawWireSphere(lConstr, 0.05f);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(p0.position + result, 0.1f);
    }


    private void Update() {
        if (!p0 || !p1 || !toRotate || !target) return;
    }

    public static Vector3 Constrain(Vector3 position, Vector3 coneVector, Quaternion rotation) {
            float dot = Vector3.Dot(position, coneVector) / coneVector.magnitude;
            Vector3 projected = coneVector.normalized * dot;
            
            Vector3 top = rotation * Vector3.up;
            Vector3 bot = rotation * Vector3.down;
            Vector3 l   = rotation * Vector3.left;
            Vector3 r   = rotation * Vector3.right;

            Vector3 upVec    = (top - position).magnitude < (bot - position).magnitude ? top : bot;
            Vector3 rightVec = (l - position).magnitude < (r - position).magnitude ? l : r;

            Vector3 adjust = position - projected;
            if (dot < 0) projected = -projected;

            float xAspect = Vector3.Dot(adjust, rightVec);
            float yAspect = Vector3.Dot(adjust, upVec);

            float projMagnitude = projected.magnitude;

            float left  = -(projMagnitude * Mathf.Tan(89));
            float right =   projMagnitude * Mathf.Tan(89);
            float up    =   projMagnitude * Mathf.Tan(89);
            float down  = -(projMagnitude * Mathf.Tan(89));

            float xBound = xAspect >= 0 ? right : left;
            float yBound = yAspect >= 0 ? up : down;

            Vector3 result = position;

            float ellipse = xAspect*xAspect/xBound*xBound + yAspect*yAspect/yBound*yBound;
            bool inbounds = ellipse <= 1 && dot >= 0;

            if (!inbounds) {
                float a = Mathf.Atan2(yAspect, xAspect);
                float x = xBound * Mathf.Cos(a);
                float y = yBound * Mathf.Sin(a);

                result = (projected + rightVec * x + upVec * y).normalized * position.magnitude;
            }

            return result;
        }
}
