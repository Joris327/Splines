using System.Collections.Generic;
using UnityEngine;

public class SimpleRoadGenerator : SplineMeshGenerator
{
    /// <summary>
    /// Vertexes per metre
    /// </summary>
    [SerializeField, Min(1f)] float vertexDensity = 10f;
    [SerializeField] float roadWidth = 1;
    
    public override void GenerateMesh(Spline spline)
    {
        base.GenerateMesh(spline);
        
        GenerateSimpleMesh(spline);
    }
    
    void GenerateSimpleMesh(Spline spline)
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
        if (meshFilter.sharedMesh) meshFilter.sharedMesh.Clear();
        meshFilter.sharedMesh = new()
        {
            name = "Spline Mesh",
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
