using UnityEngine;
using System.Collections.Generic;

public class Spline : MonoBehaviour
{
    [SerializeField] bool looping = false;
    public bool Looping { get => looping; set => looping = value; }

    [SerializeField] bool mirrored = false;
    public bool Mirrored { get => mirrored; set => mirrored = value; }

    [NonReorderable] public List<BezierCurve> curves = new();

    void Awake()
    {
        foreach (BezierCurve curve in curves) curve.CalculateDistanceLUT();
    }
    
    public void MovePointOnCurveTo(Vector3 localPos, int curveIndex, int pointIndex)
    {
        if (curveIndex < 0 || curveIndex > curves.Count - 1 || pointIndex < 0 || pointIndex > 3)
            return;
        
        Vector3 startingPos = curves[curveIndex][pointIndex];
        Vector3 movementDelta = localPos - startingPos;
        curves[curveIndex][pointIndex] = localPos;
        
        //move local tangent point if needed
        if      (pointIndex == 0) curves[curveIndex][pointIndex + 1] += movementDelta;
        else if (pointIndex == 3) curves[curveIndex][pointIndex - 1] += movementDelta;
        
        //determine if another point on another curve needs moving
        int siblingCurveIndex = -1;
        int siblingPointIndex = -1;

        switch (pointIndex)
        {
            case 0:
                siblingCurveIndex = curveIndex - 1;
                siblingPointIndex = 3;
                break;
            case 1:
                siblingCurveIndex = curveIndex - 1;
                siblingPointIndex = 2;
                break;
            case 2:
                siblingCurveIndex = curveIndex + 1;
                siblingPointIndex = 1;
                break;
            case 3:
                siblingCurveIndex = curveIndex + 1;
                siblingPointIndex = 0;
                break;
        }
        
        if (looping)
        {
            if (curveIndex == 0 && pointIndex < 2) siblingCurveIndex = curves.Count - 1; //setting sibling point index not needed because it's already done above
            else if (curveIndex == curves.Count -1 && pointIndex > 1) siblingCurveIndex = 0;
        }
        
        if (siblingCurveIndex < 0 || siblingCurveIndex >= curves.Count) return;
        
        //move sibling indexes and tangents if needed
        BezierCurve siblingCurve = curves[siblingCurveIndex];
        switch (siblingPointIndex)
        {
            case 0:
                siblingCurve.point0 = localPos;
                siblingCurve.point1 += movementDelta;
                break;
            case 1:
                if (mirrored)
                    siblingCurve.point1 = curves[siblingCurveIndex].point0 + (curves[curveIndex].point3 - curves[curveIndex].point2);
                break;
            case 2:
                if (mirrored)
                    siblingCurve.point2 = curves[siblingCurveIndex].point3 + (curves[curveIndex].point0 - curves[curveIndex].point1);
                break;
            case 3:
                siblingCurve.point3 = localPos;
                siblingCurve.point2 += movementDelta;
                break;
        }
    }
    
    public void RotatePointOnCurveTo(float newAngle, int curveIndex, int pointIndex)
    {
        if (curveIndex < 0 || curveIndex > curves.Count - 1)
            return;
        if (pointIndex != 0 && pointIndex != 3)
            return;

        //apply the new angle
        curves[curveIndex].angles[pointIndex == 0 ? 0 : 1] = newAngle;
        
        //adapt connecting points
        if (pointIndex == 0)
        {
            if      (curveIndex == 0 && Looping) curves[^1].angles[1] = newAngle;
            else if (curveIndex > 0) curves[curveIndex - 1].angles[1] = newAngle;
        }
        else //point index == 3
        {
            if      (curveIndex == curves.Count - 1 && Looping) curves[0].angles[0] = newAngle;
            else if (curveIndex < curves.Count - 1) curves[curveIndex + 1].angles[0] = newAngle;
        }
    }
    
    public void RotatePointOnCurveTo(Quaternion newRotation, int curveIndex, int pointIndex)
    {
        if (curveIndex < 0 || curveIndex > curves.Count - 1)
            return;
        if (pointIndex != 0 && pointIndex != 3)
            return;
        
        BezierCurve curve = curves[curveIndex];
        Vector3 direction = curve.GetDirectionAt(pointIndex == 0 ? 0 : 1);
        
        //calculate the new angle for the point
        Quaternion check = Quaternion.AngleAxis(180, direction);
        
        float newAngle;
        if (Quaternion.Dot(check, newRotation) > 0)
            newAngle = Quaternion.Angle(Quaternion.identity, newRotation);
        else
            newAngle = -Quaternion.Angle(Quaternion.identity, newRotation);

        RotatePointOnCurveTo(newAngle, curveIndex, pointIndex);
    }
    
    /// <summary>
    /// Calculates the angle between two vectors in degrees. In format -179 to 180.
    /// </summary>
    /// <returns>Positive for counterclockwise, negative for clockwise</returns>
    public static float SignedAngleBetween(Vector3 a, Vector3 b)
    {
        // angle in [0,180]
        float angle = Vector3.Angle(a,b);
        float sign = Mathf.Sign(Vector3.Dot(Vector3.up, Vector3.Cross(a,b)));

        // angle in [-179,180]
        float signed_angle = angle * sign;

        // angle in [0,360] (not used but included here for completeness)
        //float angle360 =  (signed_angle + 180) % 360;

        return signed_angle;
    }
}
