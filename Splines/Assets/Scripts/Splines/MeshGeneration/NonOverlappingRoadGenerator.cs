using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Spline))]
public class NonOverlappingRoadGenerator : SplineMeshGenerator
{

    readonly List<Vector3> leftVertices = new();
    readonly List<Vector3> rightVertexes = new();
    readonly List<Vector2> leftUVs = new();
    readonly List<Vector2> rightUVs = new();

    readonly List<Vector3> vertices = new();
    readonly List<Vector2> newUVs = new();
    readonly List<int> triangles = new();
    
    /// <summary>
    /// Vertexes per metre
    /// </summary>
    [SerializeField, Min(1f)] float vertexDensity = 10f;
    [SerializeField] float roadWidth = 1;
    
    public override void GenerateMesh(Spline spline)
    {
        base.GenerateMesh(spline);
        
        for (int i = 0; i < spline.curves.Count; i++)
        {
            leftVertices.Clear();
            rightVertexes.Clear();
            leftUVs.Clear();
            rightUVs.Clear();
            
            vertices.Clear();
            newUVs.Clear();
            triangles.Clear();
            
            GenerateVerteces(spline.curves[i]);
            TriangulateVerteces(i);
        }
        
        leftVertices.Clear();
        rightVertexes.Clear();
        leftUVs.Clear();
        rightUVs.Clear();
        
        vertices.Clear();
        newUVs.Clear();
        triangles.Clear();
    }
    
    bool IsVertexPlacementValid(Vector3 vertex, Vector3 p1, Vector3 p2, float stepDistance)
    {
        if ((vertex - p1).magnitude < stepDistance) return false;
        if ((vertex - p2).magnitude < stepDistance) return false;
        return true;
    }
    
    void GenerateVerteces(BezierCurve curve)
    {
        float curveLength = curve.ArcLength;
        int stepAmount = (int)(curveLength * vertexDensity);
        float stepDistance = curveLength / stepAmount;
        
        AddVerteces(0, stepDistance, curve, Vector3.zero, Vector3.zero, true);
        
        //pre-calculate the vector positions of the final 2 vectors, so that while generating we can check no verteces get too close
        Vector3 endCentrePoint = curve.CalculatePointOnCurve(1);
        float endSmoothedT = -(Mathf.Cos(Mathf.PI * 1) - 1) / 2; //map the linear progression of the t variable to an S curve
        float endInterpolatedAngle = Mathf.LerpAngle(curve.angles[0], curve.angles[1], endSmoothedT); //will be used to angle the road surface to this value
        Vector3 endDirection = curve.GetDirectionAt(1);
        Vector3 endPointNormal = Vector3.Cross(endDirection, Vector3.up).normalized;
        endPointNormal = Quaternion.AngleAxis(endInterpolatedAngle, endDirection) * endPointNormal;
        
        Vector3 endLeftVertex = endCentrePoint - (endPointNormal * roadWidth);
        Vector3 endRightVertex = endCentrePoint + (endPointNormal * roadWidth);
        //---

        for (int j = 1; j < stepAmount; j++)
        {
            float t = j / (float)stepAmount;

            AddVerteces(t, stepDistance, curve, endLeftVertex, endRightVertex);
        }
        
        leftVertices.Add(endLeftVertex);
        rightVertexes.Add(endRightVertex);
        
        leftUVs.Add(new Vector2(0, 1));
        rightUVs.Add(new Vector2(1, 1));
    }
    
    void AddVerteces(float t, float stepDistance, BezierCurve curve, Vector3 endLeftVertex, Vector3 endRightVertex, bool skipVertexCheck = false)
    {
        t = Mathf.Clamp01(t);
        
        Vector3 centrePoint = curve.CalculatePointOnCurve(t);
        
        //map the linear progression of the t variable to an S curve
        float smoothedT = -(Mathf.Cos(Mathf.PI * t) - 1) / 2; 
        //will be used to angle the road surface to this value
        float interpolatedAngle = Mathf.LerpAngle(curve.angles[0], curve.angles[1], smoothedT); 
        
        Vector3 direction = curve.GetDirectionAt(t);
        Vector3 normal = Vector3.Cross(direction, Vector3.up).normalized;
        normal = Quaternion.AngleAxis(interpolatedAngle, direction) * normal;
        
        Vector3 newLeftVertex = centrePoint - (normal * roadWidth);
        Vector3 newRightVertex = centrePoint + (normal * roadWidth);
        
        if (skipVertexCheck)
        {
            leftVertices.Add(newLeftVertex);
            leftUVs.Add(new Vector2(0, t));
            
            rightVertexes.Add(newRightVertex);
            rightUVs.Add(new Vector2(1, t));
            return;
        }
        
        if (IsVertexPlacementValid(newLeftVertex, leftVertices[^1], endLeftVertex, stepDistance))
        {
            leftVertices.Add(newLeftVertex);
            leftUVs.Add(new Vector2(0, t));
        }
        if (IsVertexPlacementValid(newRightVertex, rightVertexes[^1], endRightVertex, stepDistance))
        {
            rightVertexes.Add(newRightVertex);
            rightUVs.Add(new Vector2(1, t));
        }
    }
    
    void TriangulateVerteces(int curveIndex)
    {
        float vertexRatio;

        //add triangles
        int currentOppositeIndex = 0;
        int oppositeListStartIndex;

        if (rightVertexes.Count > leftVertices.Count)
        {
            vertices.AddRange(rightVertexes);
            vertices.AddRange(leftVertices);
            oppositeListStartIndex = rightVertexes.Count;
            newUVs.AddRange(rightUVs);
            newUVs.AddRange(leftUVs);
            vertexRatio = (float)leftVertices.Count / rightVertexes.Count;
            
            for (int j = 1; j < oppositeListStartIndex; j++)
            {
                triangles.Add(j-1);
                triangles.Add(j);
                triangles.Add(currentOppositeIndex + oppositeListStartIndex);

                if (j-1 > currentOppositeIndex / vertexRatio)
                {
                    currentOppositeIndex++;
                    triangles.Add(currentOppositeIndex + oppositeListStartIndex);
                    triangles.Add(currentOppositeIndex + oppositeListStartIndex-1);
                    triangles.Add(j);
                }
            }
            
            triangles.Add(oppositeListStartIndex-1);
            triangles.Add(vertices.Count-1);
            triangles.Add(vertices.Count-2);
        }
        else
        {
            vertices.AddRange(leftVertices);
            vertices.AddRange(rightVertexes);
            oppositeListStartIndex = leftVertices.Count;
            newUVs.AddRange(leftUVs);
            newUVs.AddRange(rightUVs);
            vertexRatio = (float)rightVertexes.Count / leftVertices.Count;
            
            for (int j = 1; j < oppositeListStartIndex; j++)
            {
                triangles.Add(j);
                triangles.Add(j-1);
                triangles.Add(currentOppositeIndex + oppositeListStartIndex);

                if (j-1 > currentOppositeIndex / vertexRatio)
                {
                    currentOppositeIndex++;
                    triangles.Add(currentOppositeIndex + oppositeListStartIndex-1);
                    triangles.Add(currentOppositeIndex + oppositeListStartIndex);
                    triangles.Add(j);
                }
            }
            
            triangles.Add(oppositeListStartIndex-1);
            triangles.Add(vertices.Count-2);
            triangles.Add(vertices.Count-1);
        }
        
        MeshFilter meshFilter = Instantiate(meshObjectPrefab, transform);
        if (meshFilter.sharedMesh) meshFilter.sharedMesh.Clear();
        meshFilter.sharedMesh = new()
        {
            name = "Spline Mesh " + curveIndex,
            vertices = vertices.ToArray(),
            uv = newUVs.ToArray(),
            triangles = triangles.ToArray()
        };
        meshFilter.sharedMesh.RecalculateNormals();
    }
}
