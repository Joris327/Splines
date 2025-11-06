using UnityEngine;
using System.Collections.Generic;

public class Spline : MonoBehaviour
{
    [SerializeField] bool looping = false;
    public bool Looping { get { return looping; } }

    [SerializeField] bool mirrored = false;
    public bool Mirrored { get { return mirrored; } }

    public List<BezierCurve> curves = new();

    void Awake()
    {
        foreach (BezierCurve curve in curves) curve.CalculateDistanceLUT();

        // for (int i = 0; i < 10; i++) Debug.Log(curves[0].GetCurvature(i / 10f));

        // Debug.Log("---");

        // for (int i = 0; i < 10; i++) Debug.Log(curves[0].GetRadius(i / 10f));

        // Debug.Log("---");

        // Vector3 prevVel = curves[0].GetVelocity(0);
        // for (int i = 1; i < 10; i++)
        // {
        //     Vector3 vel = curves[0].GetVelocity(i / 10f);
        //     //Vector3 normal = Vector3.Cross(vel, Vector3.up);
        //     //Debug.Log(vel + " - " + Vector3.Cross(vel, Vector3.up));
        //     Debug.Log(SignedAngleBetween(vel, prevVel));
        //     prevVel = vel;
        // }
    }
    
    /// <summary>
    /// Positive for counterclockwise, negative for clockwise
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
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
