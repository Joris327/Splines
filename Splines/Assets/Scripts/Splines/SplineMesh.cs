using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Spline))]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class SplineMesh : MonoBehaviour
{
    Spline spline;
    MeshFilter meshFilter;
    MeshRenderer meshRenderer;
    
    List<Vector3> vertices;

    public PhysicsMaterial physicsMaterial;
    [SerializeField, Min(2)] float vertexResolution = 10;
    [SerializeField] float roadWidth = 1;
    
    enum Axis { x, y, z }
    
    public bool GenerateMeshOnEdit = false;

    void Awake()
    {
        SetComponentReferences();
    }

    public void SetComponentReferences()
    {
        if (!TryGetComponent(out spline)) Debug.LogError(name + ": could not find Spline component.");
        if (!TryGetComponent(out meshFilter)) Debug.LogError(name + ": could not find MeshFilter component.");
        if (!TryGetComponent(out meshRenderer)) Debug.LogError(name + " could find MeshRenderer component.");
    }
    
    public void GenerateMesh()
    {
        vertices = new();
        List<Vector2> uv = new();

        if (spline.curves == null || spline.curves.Count == 0) Debug.Log("No curves found.");
        for (int i = 0; i < spline.curves.Count; i++)
        {
            BezierCurve curve = spline.curves[i];

            for (int j = 0; j < vertexResolution; j++)
            {
                float progress = j / vertexResolution;

                Vector3 centrePoint = curve.CalculatePointOnCurve(progress, transform.position) - transform.position;

                //Vector3 axis;

                // switch (BiggestAxis(direction))
                // {
                //     case Axis.y: axis = new(-direction.y, direction.x, direction.z); break;
                //     default: axis = new(-direction.z, direction.y, direction.x); break;
                // }

                AddVertexes(centrePoint, curve, progress, uv);
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

        if (spline.Loop)
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

            AddVertexes(centrePoint, curve, 1, uv);

            triangles.Add(vertices.Count - 4);
            triangles.Add(vertices.Count - 2);
            triangles.Add(vertices.Count - 3);
            triangles.Add(vertices.Count - 3);
            triangles.Add(vertices.Count - 2);
            triangles.Add(vertices.Count - 1);
        }

        meshFilter.sharedMesh = new()
        {
            name = "Spline Mesh",
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray()
        };

        meshFilter.sharedMesh.SetUVs(0, uv);

        if (!meshRenderer.sharedMaterial) meshRenderer.sharedMaterial = new(Shader.Find("Universal Render Pipeline/Lit"));
        meshFilter.sharedMesh.RecalculateNormals();
    }
    
    void AddVertexes(Vector3 centrePoint, BezierCurve curve, float progress, List<Vector2> uv)
    {
        progress = Mathf.Clamp01(progress);
        Vector3 direction = curve.GetDirection(progress, transform);
        //Vector3 direction = curve.CalculatePointOnCurve(progress + 1 / vertexResolution, transform.position) - centrePoint;
        //direction.Normalize();

        float interpolatedAngle = Mathf.LerpAngle(curve.angles[0], curve.angles[1], progress);
        //float interpolatedAngle = ((curve.angles[0] + curve.angles[1]) / 2) * progress;
        if (progress == 0.5f) Debug.Log(interpolatedAngle);
        Vector3 angleDir = direction; //forward
        
        //angleDir = Quaternion.Euler(0, -90, 0) * angleDir; //90 degrees along global y axis
        angleDir = Quaternion.AngleAxis(90, new(-direction.z, direction.y, direction.x)) * angleDir; //rotate 90 degrees left along local y axis
        
        angleDir = Quaternion.AngleAxis(interpolatedAngle, direction) * angleDir; //X degrees along local z axis
        //angleDir = interpolatedAngle * angleDir;
        
        Vector3 cross = Vector3.Cross(direction, angleDir.normalized).normalized;
        //cross = Vector3.Cross(cross, direction).normalized;
        
        Vector3 vertex1 = centrePoint + (cross * roadWidth);
        Vector3 vertex2 = centrePoint - (cross * roadWidth);
        
        vertices.Add(vertex1);
        uv.Add(new(0, progress));
        vertices.Add(vertex2);
        uv.Add(new(1, progress));
    }
    
    Axis BiggestAxis (Vector3 input)
    {
        if (input.x > input.y && input.x > input.z) return Axis.x;
        if (input.y > input.x && input.y > input.z) return Axis.y;
        if (input.z > input.x && input.z > input.y) return Axis.z;
        return Axis.z;
    }
    
    private void OnDrawGizmos ()
    {
        if (vertices == null)
        {
			return;
		}
        
		Gizmos.color = Color.black;
		for (int i = 0; i < vertices.Count; i++)
        {
			Gizmos.DrawSphere(vertices[i] + transform.position, 0.1f);
		}
	}
}
