using UnityEngine;

[System.Serializable]
public class BezierCurve
{
    public Vector3 this[int index]
    {
        get
        {
            return index switch
            {
                0 => point0,
                1 => point1,
                2 => point2,
                3 => point3,
                _ => throw new System.IndexOutOfRangeException()
            };
        }
        set
        {
            switch (index)
            {
                case 0: point0 = value; break;
                case 1: point1 = value; break;
                case 2: point2 = value; break;
                case 3: point3 = value; break;
                default: throw new System.IndexOutOfRangeException();
            }
        }
    }
    
    public Vector3 point0 = new();
    public Vector3 point1 = new(1, 0, 0);
    public Vector3 point2 = new(2, 0, 0);
    public Vector3 point3 = new(3, 0, 0);
    
    public CurveAngles angles = new();

    [SerializeField] int distanceSamplesAmount = 10;

    //public float ArcLength => distanceLUT[^1];
    public float ArcLength
    {
        get
        {
            //if (distanceLUT == null) CalculateDistanceLUT();
            CalculateDistanceLUT();
            return distanceLUT[^1];
        }
    }

    /// <summary>
    /// Lookup table for distance to t-value
    /// </summary>
    float[] distanceLUT;
    public float[] DistanceLUT => distanceLUT;

    static readonly Matrix4x4 characteristicMatrix = new(
        new(1, 0, 0, 0),
        new(-3, 3, 0, 0),
        new(3, -6, 3, 0),
        new(-1, 3, -3, 1)
    );

    public BezierCurve(Vector3 pPoint0, Vector3 pPoint1, Vector3 pPoint2, Vector3 pPoint3)
    {
        point0 = pPoint0;
        point1 = pPoint1;
        point2 = pPoint2;
        point3 = pPoint3;
    }

    public BezierCurve(Vector3 origin, CurveAngles startingAngles)
    {
        point0 = origin + new Vector3(0, 0, 0);
        point1 = origin + new Vector3(1, 0, 0);
        point2 = origin + new Vector3(2, 0, 0);
        point3 = origin + new Vector3(3, 0, 0);
        
        angles = startingAngles;
    }

    /// <summary>
    /// Also known as the first derivative.
    /// </summary>
    public Vector3 GetVelocity(float t)
    {
        t = Mathf.Clamp01(t);
        Vector4 powersOfT = new(0, 1, t * 2, 3 * Mathf.Pow(t, 2));
        return Derivative(powersOfT, Vector3.zero);
    }

    /// <summary>
    /// Normalized velocity.
    /// </summary>
    public Vector3 GetDirectionAt(float t)
    {
        return GetVelocity(t).normalized;
    }

    /// <summary>
    /// Also known as the second derivative.
    /// </summary>
    public Vector3 GetAcceleration(float t)
    {
        t = Mathf.Clamp01(t);
        Vector4 powersOfT = new(0, 0, 2, 6 * t);
        return Derivative(powersOfT, Vector3.zero);
    }

    /// <summary>
    /// Calculates the osculating circle, or the curvature at that point.
    /// </summary>
    /// <returns>The amount of curvature at a certain point of the spline, where higher values represent a tighter curve, and 0 no curvature at all.</returns>
    public float GetCurvature(float t)
    {
        Vector3 velocity = GetVelocity(t);

        float denominator = Mathf.Pow(velocity.magnitude, 3);
        if (denominator == 0) return 0;

        float numerator = Vector3.Cross(velocity, GetAcceleration(t)).magnitude;

        return numerator / denominator; //Mathf.Pow(numerator / denominator, -1);
    }
    
    /// <summary>
    /// Calculates the radius of the osculating curve.
    /// </summary>
    public float GetRadius(float t) => Mathf.Pow(GetCurvature(t), -1);

    public Vector3 CalculatePointOnCurve(float t)
    {
        t = Mathf.Clamp01(t);

        Vector4 powersOfT = new(1, t, Mathf.Pow(t, 2), Mathf.Pow(t, 3));

        return Derivative(powersOfT);
    }

    public Vector3 CalculatePointOnCurve(float t, Vector3 transformWorldPos) => CalculatePointOnCurve(t) + transformWorldPos;

    Vector3 Derivative(Vector4 powersOfT)
    {
        Vector4 polynomial = characteristicMatrix * powersOfT;

        Vector4 worldPoint0 = new(point0.x, point0.y, point0.z);
        Vector4 worldPoint1 = new(point1.x, point1.y, point1.z);
        Vector4 worldPoint2 = new(point2.x, point2.y, point2.z);
        Vector4 worldPoint3 = new(point3.x, point3.y, point3.z);

        Matrix4x4 pointMatrix = new(worldPoint0, worldPoint1, worldPoint2, worldPoint3);

        Vector4 result = pointMatrix * polynomial;

        return new Vector3(result.x, result.y, result.z);
    }

    Vector3 Derivative(Vector4 powersOfT, Vector3 transformWorldPos) => Derivative(powersOfT) + transformWorldPos;

    public void CalculateDistanceLUT()
    {
        Vector3[] distanceSamples = new Vector3[distanceSamplesAmount];
        distanceLUT = new float[distanceSamplesAmount];
        distanceSamples[0] = CalculatePointOnCurve(0, Vector3.zero);

        for (int i = 1; i < distanceSamplesAmount; i++)
        {
            float t = (float)i / (distanceSamplesAmount - 1);

            distanceSamples[i] = CalculatePointOnCurve(t, Vector3.zero);

            float pointDelta = (distanceSamples[i] - distanceSamples[i - 1]).magnitude;

            distanceLUT[i] = distanceLUT[i - 1] + pointDelta;
        }
    }

    public float GetTFromDistance(float distance)
    {
        if (distance <= 0) return 0;

        int sampleAmount = distanceLUT.Length - 1;
        for (int i = 0; i < sampleAmount; i++)
        {
            float lowerDistance = distanceLUT[i];
            float upperDistance = distanceLUT[i + 1];
            if (distance > lowerDistance && distance <= upperDistance)
            {
                float midPoint = (distance - lowerDistance) / (upperDistance - lowerDistance);

                return ((float)i / sampleAmount) + (midPoint * (1f / sampleAmount));
            }
        }

        return 1;
    }

    public float GetDistanceFromT(float t)
    {
        if (t <= 0) return 0;
        if (t >= 1) return ArcLength;
        
        int sampleAmount = distanceLUT.Length - 1;
        int index = (int)(t * sampleAmount);
        
        float minDistance = distanceLUT[index];
        float maxDistance = distanceLUT[index + 1];
        float midPoint = (t * sampleAmount) % 1;

        return Mathf.Lerp(minDistance, maxDistance, midPoint);
    }
}