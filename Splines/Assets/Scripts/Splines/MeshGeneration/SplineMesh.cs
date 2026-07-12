using UnityEngine;

[RequireComponent(typeof(Spline))]
public class SplineMesh : MonoBehaviour
{
    public bool GenerateMeshOnEdit = false;
    
    [SerializeField] SplineMeshGenerator splineMeshGenerator;
    
    public void GenerateMesh(Spline spline)
    {
        if (splineMeshGenerator) splineMeshGenerator.GenerateMesh(spline);
    }
}
