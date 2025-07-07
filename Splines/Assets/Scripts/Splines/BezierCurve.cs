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
    
    public Vector3 CalculatePointOnCurve(float timeStamp, Vector3 position)
    {
        timeStamp = Mathf.Clamp01(timeStamp);
        
        //method 1 ---------------------------------------------------------------------------------------------------------------
        
        // Vector3 tempPoint1 = Vector3.Lerp(points[0], points[1], timeStamp);
        // Vector3 tempPoint2 = Vector3.Lerp(points[2], points[3], timeStamp);
        // Vector3 tempPoint3 = Vector3.Lerp(points[3], points[4], timeStamp);
        
        // Vector3 tempPoint4 = Vector3.Lerp(tempPoint1, tempPoint2, timeStamp);
        // Vector3 tempPoint5 = Vector3.Lerp(tempPoint2, tempPoint3, timeStamp);
        
        // return Vector3.Lerp(tempPoint4, tempPoint5, timeStamp);
        
        //method 2 ---------------------------------------------------------------------------------------------------------------
        
        // return 
        //     points[0] * (Mathf.Pow(-timeStamp, 3) + (3 * Mathf.Pow(timeStamp, 2)) - (3 * timeStamp) + 1) +
        //     points[1] * (3 * Mathf.Pow(timeStamp, 3) - (6 * Mathf.Pow(timeStamp, 2)) + (3 * timeStamp)) +
        //     points[2] * (-3 * Mathf.Pow(timeStamp, 3) + (3 * Mathf.Pow(timeStamp, 2))) +
        //     points[3] * Mathf.Pow(timeStamp, 3);
        
        //method 3 ---------------------------------------------------------------------------------------------------------------
        
        Vector4 powersOfT = new(1, timeStamp, Mathf.Pow(timeStamp, 2), Mathf.Pow(timeStamp, 3));
        
        Vector3 worldPoint0 = points[0] + position;
        Vector3 worldPoint1 = points[1] + position;
        Vector3 worldPoint2 = points[2] + position;
        Vector3 worldPoint3 = points[3] + position;

        float[,] pointMatrix = {
            { worldPoint0.x, worldPoint0.y, worldPoint0.z },
            { worldPoint1.x, worldPoint1.y, worldPoint1.z },
            { worldPoint2.x, worldPoint2.y, worldPoint2.z },
            { worldPoint3.x, worldPoint3.y, worldPoint3.z },
        };

        // float4x3 pointMatrix = new(
        //     worldPoint0.x, worldPoint0.y, worldPoint0.z,
        //     worldPoint1.x, worldPoint1.y, worldPoint1.z,
        //     worldPoint2.x, worldPoint2.y, worldPoint2.z,
        //     worldPoint3.x, worldPoint3.y, worldPoint3.z
        // );

        float[,] positionMatrix = {
            {
                Vector4.Dot(new(characteristicMatrix[0,0], characteristicMatrix[1,0], characteristicMatrix[2,0], characteristicMatrix[3,0]), new(pointMatrix[0,0], pointMatrix[1,0], pointMatrix[2,0], pointMatrix[3,0])),
                Vector4.Dot(new(characteristicMatrix[0,0], characteristicMatrix[1,0], characteristicMatrix[2,0], characteristicMatrix[3,0]), new(pointMatrix[0,1], pointMatrix[1,1], pointMatrix[2,1], pointMatrix[3,1])),
                Vector4.Dot(new(characteristicMatrix[0,0], characteristicMatrix[1,0], characteristicMatrix[2,0], characteristicMatrix[3,0]), new(pointMatrix[0,2], pointMatrix[1,2], pointMatrix[2,2], pointMatrix[3,2]))
            },
            {
                Vector4.Dot(new(characteristicMatrix[0,1], characteristicMatrix[1,1], characteristicMatrix[2,1], characteristicMatrix[3,1]), new(pointMatrix[0,0], pointMatrix[1,0], pointMatrix[2,0], pointMatrix[3,0])),
                Vector4.Dot(new(characteristicMatrix[0,1], characteristicMatrix[1,1], characteristicMatrix[2,1], characteristicMatrix[3,1]), new(pointMatrix[0,1], pointMatrix[1,1], pointMatrix[2,1], pointMatrix[3,1])),
                Vector4.Dot(new(characteristicMatrix[0,1], characteristicMatrix[1,1], characteristicMatrix[2,1], characteristicMatrix[3,1]), new(pointMatrix[0,2], pointMatrix[1,2], pointMatrix[2,2], pointMatrix[3,2]))
            },
            {
                Vector4.Dot(new(characteristicMatrix[0,2], characteristicMatrix[1,2], characteristicMatrix[2,2], characteristicMatrix[3,2]), new(pointMatrix[0,0], pointMatrix[1,0], pointMatrix[2,0], pointMatrix[3,0])),
                Vector4.Dot(new(characteristicMatrix[0,2], characteristicMatrix[1,2], characteristicMatrix[2,2], characteristicMatrix[3,2]), new(pointMatrix[0,1], pointMatrix[1,1], pointMatrix[2,1], pointMatrix[3,1])),
                Vector4.Dot(new(characteristicMatrix[0,2], characteristicMatrix[1,2], characteristicMatrix[2,2], characteristicMatrix[3,2]), new(pointMatrix[0,2], pointMatrix[1,2], pointMatrix[2,2], pointMatrix[3,2]))
            },
            {
                Vector4.Dot(new(characteristicMatrix[0,3], characteristicMatrix[1,3], characteristicMatrix[2,3], characteristicMatrix[3,3]), new(pointMatrix[0,0], pointMatrix[1,0], pointMatrix[2,0], pointMatrix[3,0])),
                Vector4.Dot(new(characteristicMatrix[0,3], characteristicMatrix[1,3], characteristicMatrix[2,3], characteristicMatrix[3,3]), new(pointMatrix[0,1], pointMatrix[1,1], pointMatrix[2,1], pointMatrix[3,1])),
                Vector4.Dot(new(characteristicMatrix[0,3], characteristicMatrix[1,3], characteristicMatrix[2,3], characteristicMatrix[3,3]), new(pointMatrix[0,2], pointMatrix[1,2], pointMatrix[2,2], pointMatrix[3,2]))
            }
        };

        //float4x3 positionMatrix = math.mul(characteristicMatrix, pointMatrix);

        Vector3 returnValue = new(
            Vector4.Dot(powersOfT, new(positionMatrix[0,0], positionMatrix[1,0], positionMatrix[2,0], positionMatrix[3,0])),
            Vector4.Dot(powersOfT, new(positionMatrix[0,1], positionMatrix[1,1], positionMatrix[2,1], positionMatrix[3,1])),
            Vector4.Dot(powersOfT, new(positionMatrix[0,2], positionMatrix[1,2], positionMatrix[2,2], positionMatrix[3,2]))
        );

        //Vector3 returnValue = math.mul(powersOfT, positionMatrix);
        
        return returnValue;
    }
}
