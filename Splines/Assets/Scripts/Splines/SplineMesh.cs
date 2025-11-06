using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Spline))]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class SplineMesh : MonoBehaviour
{
    Spline spline;
    MeshFilter meshFilter;
    MeshRenderer meshRenderer;

    public PhysicsMaterial physicsMaterial;
    /// <summary>
    /// Vertexes per metre
    /// </summary>
    [SerializeField, Min(1f)] float vertexDensity = 10f;
    [SerializeField] float roadWidth = 1;
    
    public bool GenerateMeshOnEdit = false;

    void Awake()
    {
        SetComponentReferences();
    }

    public void SetComponentReferences()
    {
        if (!TryGetComponent(out spline)) Debug.LogError(name + ": could not find Spline component.");
        if (!TryGetComponent(out meshFilter)) Debug.LogError(name + ": could not find MeshFilter component.");
        if (!TryGetComponent(out meshRenderer)) Debug.LogError(name + ": could find MeshRenderer component.");
    }

    public void GenerateMesh()
    {
        GenerateNonOverlappingMesh();
    }
    
    void GenerateSimpleMesh()
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

        meshFilter.sharedMesh.Clear();
        meshFilter.sharedMesh = new()
        {
            name = "Spline Mesh",
            vertices = vertices.ToArray(),
            uv = newUVs.ToArray(),
            triangles = triangles.ToArray()
        };

        if (!meshRenderer.sharedMaterial) meshRenderer.sharedMaterial = new(Shader.Find("Universal Render Pipeline/Lit"));
        meshFilter.sharedMesh.RecalculateNormals();
    }

    float Distance(Vector3 a, Vector3 b)
    {
        return (a - b).magnitude;
    }
    
    bool FarEnoughFromEveryone(Vector3 subject, List<Vector3> vectors, Vector3 extra, float distance)
    {
        if ((subject - extra).magnitude <= distance) return false;

        foreach (Vector3 v in vectors)
        {
            if ((subject - v).magnitude <= distance) return false;
        }

        return true;
    }
    
    public void GenerateNonOverlappingMesh()
    {
        List<Vector3> leftVertices = new();
        List<Vector3> rightVertexes = new();
        List<Vector2> leftUVs = new();
        List<Vector2> rightUVs = new();
        
        List<Vector3> vertices = new();
        List<Vector2> newUVs = new();
        List<int> triangles = new();

        for (int i = 0; i < 1/*spline.curves.Count*/; i++)
        {
            BezierCurve curve = spline.curves[i];

            Vector3 normal = Vector3.Cross(curve.GetDirection(0), Vector3.up).normalized;
            Vector3 centrePoint = curve.CalculatePointOnCurve(0);

            leftVertices.Add(centrePoint - (normal * roadWidth));
            rightVertexes.Add(centrePoint + (normal * roadWidth));

            leftUVs.Add(new Vector2(0, 0));
            rightUVs.Add(new Vector2(1, 0));
            
            normal = Vector3.Cross(curve.GetDirection(1), Vector3.up).normalized;
            centrePoint = curve.CalculatePointOnCurve(1);

            Vector3 lastLeftVertex = centrePoint - (normal * roadWidth);
            Vector3 lastRightVertex = centrePoint + (normal * roadWidth);

            float curveLength = curve.ArcLength;
            int stepAmount = (int)(curveLength * vertexDensity);
            float stepDistance = curveLength / stepAmount;

            for (int j = 1; j < stepAmount; j++)
            {
                float t = j / (float)stepAmount;

                normal = Vector3.Cross(curve.GetDirection(t), Vector3.up).normalized;
                centrePoint = curve.CalculatePointOnCurve(t);

                Vector3 newLeftVertex = centrePoint - (normal * roadWidth);
                Vector3 newRightVertex = centrePoint + (normal * roadWidth);

                if (Distance(newLeftVertex, leftVertices[^1]) >= stepDistance //not too close to previous vertex
                 && Distance(newLeftVertex, leftVertices[0]) > stepDistance //not too close to first vertex (prevent overlap)
                 && Distance(newLeftVertex, lastLeftVertex) > stepDistance) //not too close to last vertex (prevent overlap)
                //if (FarEnoughFromEveryone(newLeftVertex, leftVertices, lastLeftVertex, stepDistance))
                {
                    leftVertices.Add(newLeftVertex);
                    leftUVs.Add(new Vector2(0, t));
                }
                if (Distance(newRightVertex, rightVertexes[^1]) >= stepDistance
                 && Distance(newRightVertex, rightVertexes[0]) > stepDistance
                 && Distance(newRightVertex, lastRightVertex) > stepDistance)
                //if (FarEnoughFromEveryone(newRightVertex, rightVertexes, lastRightVertex, stepDistance))
                {
                    rightVertexes.Add(newRightVertex);
                    rightUVs.Add(new Vector2(1, t));
                }
            }
            
            leftVertices.Add(lastLeftVertex);
            rightVertexes.Add(lastRightVertex);
            
            leftUVs.Add(new Vector2(0, 1));
            rightUVs.Add(new Vector2(1, 1));
        }
        
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
                
                //Debug.Log(j + " - " + currentOppositeIndex + " - " + triangles.Count);
            }
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
                
                //Debug.Log(j + " - " + currentOppositeIndex + " - " + triangles.Count);
            }
        }

        //Debug.Log("------------");
        //Debug.Log(vertexRatio);

        
        
        //Debug.Log(leftVertices.Count + " - " + rightVertexes.Count);
        //Debug.Log(vertices.Count + " - " + newUVs.Count);
        //Debug.Log(triangles.Count);
        
        meshFilter.sharedMesh.Clear();
        meshFilter.sharedMesh = new()
        {
            name = "Spline Mesh",
            vertices = vertices.ToArray(),
            uv = newUVs.ToArray(),
            triangles = triangles.ToArray()
        };

        if (!meshRenderer.sharedMaterial) meshRenderer.sharedMaterial = new(Shader.Find("Universal Render Pipeline/Lit"));
        meshFilter.sharedMesh.RecalculateNormals();
    }
    
    void AddVertexes(Vector3 centrePoint, BezierCurve curve, float progress, List<Vector2> uv, List<Vector3> vertices)
    {
        progress = Mathf.Clamp01(progress);
        progress = -(Mathf.Cos(Mathf.PI * progress) - 1) / 2; //smooth out the curve
        
        Vector3 direction = curve.GetDirection(progress);
        
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
