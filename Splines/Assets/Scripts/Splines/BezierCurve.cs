using UnityEngine;

[System.Serializable]
public class BezierCurve
{
    public Vector3[] points = {
        new(0, 0, 0), new(1, 0, 0), new(2, 0, 0), new(3, 0, 0)
    };

    public float[] angles = {
        new(), new()
    };

    [SerializeField] int distanceSamplesAmount = 10;
    
    public float ArcLength => distanceLUT[^1];

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
        points[0] = pPoint0;
        points[1] = pPoint1;
        points[2] = pPoint2;
        points[3] = pPoint3;
    }

    public BezierCurve(Vector3 anchor)
    {
        points = new Vector3[] {
            anchor, anchor + new Vector3(1, 0, 0), anchor + new Vector3(2, 0, 0), anchor + new Vector3(3, 0, 0)
        };

        angles = new float[] {
            0, 0
        };
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
    public Vector3 GetDirection(float t)
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
    public float GetCurvature(float t)
    {
        Vector3 velocity = GetVelocity(t);

        float denominator = Mathf.Pow(velocity.magnitude, 3);
        if (denominator == 0) return 0;

        float numerator = Vector3.Cross(velocity, GetAcceleration(t)).magnitude;

        return Mathf.Pow(numerator / denominator, -1);
    }

    public Vector3 CalculatePointOnCurve(float t, Vector3 transformWorldPos)
    {
        t = Mathf.Clamp01(t);

        Vector4 powersOfT = new(1, t, Mathf.Pow(t, 2), Mathf.Pow(t, 3));

        return Derivative(powersOfT, transformWorldPos);
    }

    Vector3 Derivative(Vector4 powersOfT, Vector3 transformWorldPos)
    {
        Vector4 polynomial = characteristicMatrix * powersOfT;

        Vector4 worldPoint0 = new(points[0].x, points[0].y, points[0].z);
        Vector4 worldPoint1 = new(points[1].x, points[1].y, points[1].z);
        Vector4 worldPoint2 = new(points[2].x, points[2].y, points[2].z);
        Vector4 worldPoint3 = new(points[3].x, points[3].y, points[3].z);

        Matrix4x4 pointMatrix = new(worldPoint0, worldPoint1, worldPoint2, worldPoint3);

        Vector4 result = pointMatrix * polynomial;

        return new Vector3(result.x, result.y, result.z) + transformWorldPos;
    }

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

        float sampleAmount = distanceLUT.Length;
        for (int i = 0; i < sampleAmount - 1; i++)
        {
            float lowerDistance = distanceLUT[i];
            float upperDistance = distanceLUT[i + 1];
            if (distance > lowerDistance && distance <= upperDistance)
            {
                float t = (distance - lowerDistance) / (upperDistance - lowerDistance);
                
                return (i / (sampleAmount-1)) + (t * (1 / sampleAmount));
            }
        }
        
        return 1;
    }
}
