using UnityEngine;
using System.Collections.Generic;

public class Spline : MonoBehaviour
{
    [SerializeField] bool looping = false;
    public bool Looping { get { return looping; } }

    [SerializeField] bool mirrored = false;
    public bool Mirrored { get { return mirrored; } }

    public List<BezierCurve> curves = new();
}
