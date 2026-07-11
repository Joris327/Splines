using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Spline))]
public class SplineMesh : MonoBehaviour
{
    Spline spline;
    // MeshFilter meshFilter;
    // MeshRenderer meshRenderer;
    
    [SerializeField] MeshFilter meshObjectPrefab;
    public PhysicsMaterial physicsMaterial;
    /// <summary>
    /// Vertexes per metre
    /// </summary>
    [SerializeField, Min(1f)] float vertexDensity = 10f;
    [SerializeField] float roadWidth = 1;
    
    public bool GenerateMeshOnEdit = false;
    
    List<Vector3> leftVertices = new();
    List<Vector3> rightVertexes = new();
    List<Vector2> leftUVs = new();
    List<Vector2> rightUVs = new();
    
    List<Vector3> vertices = new();
    List<Vector2> newUVs = new();
    List<int> triangles = new();

    void Awake()
    {
        SetComponentReferences();
    }

    public void SetComponentReferences()
    {
        if (!TryGetComponent(out spline)) Debug.LogError(name + ": could not find Spline component.");
        // if (!TryGetComponent(out meshFilter)) Debug.LogError(name + ": could not find MeshFilter component.");
        // if (!TryGetComponent(out meshRenderer)) Debug.LogError(name + ": could find MeshRenderer component.");
    }

    public void GenerateMesh()
    {
        GenerateNonOverlappingMesh();
    }
    
    void GenerateSimpleMesh(int curveIndex)
    {
        List<Vector3> vertices = new();
        List<Vector2> newUVs = new();

        if (spline.curves == null || spline.curves.Count == 0) Debug.Log("No curves found.");

        for (int i = 0; i < spline.curves.Count; i++)
        {
            BezierCurve curve = spline.curves[i];

            for (int j = 0; j < vertexDensity; j++)
            {
                float progress = j / vertexDensity;

                Vector3 centrePoint = curve.CalculatePointOnCurve(progress, transform.position) - transform.position;

                //Vector3 axis;

                // switch (BiggestAxis(direction))
                // {
                //     case Axis.y: axis = new(-direction.y, direction.x, direction.z); break;
                //     default: axis = new(-direction.z, direction.y, direction.x); break;
                // }

                AddVertexes(centrePoint, curve, progress, newUVs, vertices);
            }
        }

        List<int> triangles = new();
        for (int i = 0; i < vertices.Count - 2; i += 2)
        {
            triangles.Add(i);
            triangles.Add(i + 2);
            triangles.Add(i + 1);
            triangles.Add(i + 1);
            triangles.Add(i + 2);
            triangles.Add(i + 3);
        }

        if (spline.Looping)
        {
            triangles.Add(vertices.Count - 2);
            triangles.Add(0);
            triangles.Add(vertices.Count - 1);
            triangles.Add(vertices.Count - 1);
            triangles.Add(0);
            triangles.Add(1);
        }
        else
        {
            BezierCurve curve = spline.curves[^1];
            Vector3 centrePoint = curve.CalculatePointOnCurve(1, transform.position) - transform.position;

            AddVertexes(centrePoint, curve, 1, newUVs, vertices);

            triangles.Add(vertices.Count - 4);
            triangles.Add(vertices.Count - 2);
            triangles.Add(vertices.Count - 3);
            triangles.Add(vertices.Count - 3);
            triangles.Add(vertices.Count - 2);
            triangles.Add(vertices.Count - 1);
        }

        MeshFilter meshFilter = Instantiate(meshObjectPrefab, transform);
        meshFilter.sharedMesh.Clear();
        meshFilter.sharedMesh = new()
        {
            name = "Spline Mesh " + curveIndex,
            vertices = vertices.ToArray(),
            uv = newUVs.ToArray(),
            triangles = triangles.ToArray()
        };
        meshFilter.sharedMesh.RecalculateNormals();
    }

    bool IsVertexPlacementValid(Vector3 vertex, Vector3 p1, Vector3 p2, float stepDistance)
    {
        if ((vertex - p1).magnitude < stepDistance) return false;
        if ((vertex - p2).magnitude < stepDistance) return false;
        return true;
    }
    
    public void GenerateNonOverlappingMesh()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            DestroyImmediate(child);
        }
        
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

            AddVerteces(t, stepDistance, curve, endLeftVertex, endRightVertex, true);
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
        
        float smoothedT = -(Mathf.Cos(Mathf.PI * t) - 1) / 2; //map the linear progression of the t variable to an S curve
        float interpolatedAngle = Mathf.LerpAngle(curve.angles[0], curve.angles[1], smoothedT); //will be used to angle the road surface to this value
        
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
    
    void AddVertexes(Vector3 centrePoint, BezierCurve curve, float progress, List<Vector2> uv, List<Vector3> vertices)
    {
        progress = Mathf.Clamp01(progress);
        progress = -(Mathf.Cos(Mathf.PI * progress) - 1) / 2; //smooth out the curve
        
        Vector3 direction = curve.GetDirectionAt(progress);
        
        float interpolatedAngle = Mathf.LerpAngle(curve.angles[0], curve.angles[1], progress); //will be used to angle the road surface to this value
        
        Vector3 cross = Vector3.Cross(direction, Vector3.up).normalized;
        cross = Quaternion.AngleAxis(interpolatedAngle, direction) * cross;
        
        Vector3 vertex1 = centrePoint + (cross * roadWidth);
        Vector3 vertex2 = centrePoint - (cross * roadWidth);
        
        vertices.Add(vertex1);
        uv.Add(new(0, progress));
        
        vertices.Add(vertex2);
        uv.Add(new(1, progress));
    }
}
