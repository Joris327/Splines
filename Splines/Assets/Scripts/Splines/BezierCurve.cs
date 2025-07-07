using Unity.Mathematics;
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
    
    static readonly Matrix4x4 characteristicMatrix = new(
        new( 1,  0,  0,  0),
        new(-3,  3,  0,  0),
        new( 3, -6,  3,  0),
        new(-1,  3, -3,  1)
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
    
    public Vector3 GetFirstDerivative(float t)
    {
		t = Mathf.Clamp01(t);
		float oneMinusT = 1f - t;
		return
			3f * oneMinusT * oneMinusT * (points[1] - points[0]) +
			6f * oneMinusT * t * (points[2] - points[1]) +
			3f * t * t * (points[3] - points[2]);
	}
    
    public Vector3 GetVelocity (float t, Transform splineTransform)
    {
		return splineTransform.TransformPoint(GetFirstDerivative(t)) - splineTransform.position;
	}
    
    public Vector3 GetDirection(float t, Transform splineTransform)
    {
        return GetVelocity(t, splineTransform).normalized;
    }
    
    public Vector3 CalculatePointOnCurve(float timeStamp, Vector3 transformWorldPos)
    {
        timeStamp = Mathf.Clamp01(timeStamp);
        
        float4 powersOfT = new(1, timeStamp, math.pow(timeStamp, 2), math.pow(timeStamp, 3));

        float4 polynomial = math.mul(characteristicMatrix, powersOfT);
        
        float4 worldPoint0 = new(points[0], 0);
        float4 worldPoint1 = new(points[1], 0);
        float4 worldPoint2 = new(points[2], 0);
        float4 worldPoint3 = new(points[3], 0);

        float4x4 pointMatrix = new(worldPoint0, worldPoint1, worldPoint2, worldPoint3);
        
        float4 result = math.mul(pointMatrix, polynomial);

        return new Vector3(result.x, result.y, result.z) + transformWorldPos;
    }
}
