using UnityEngine;
using Unity.Mathematics;
using System.Collections.Generic;

public class Spline : MonoBehaviour
{
    [SerializeField] bool loop = false;
    public bool Loop { get { return loop; } }

    [SerializeField] bool mirrored = false;
    public bool Mirrored { get { return mirrored; } }

     public List<BezierCurve> curves = new();
    
    public int linesPerCurve = 20;
    public bool showDirection = true;
}
