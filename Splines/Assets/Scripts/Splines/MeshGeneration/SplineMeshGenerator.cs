
using UnityEngine;

public abstract class SplineMeshGenerator : MonoBehaviour
{
    [SerializeField] protected MeshFilter meshObjectPrefab;
    [SerializeField] PhysicsMaterial meshPhysicsMaterial;
    public PhysicsMaterial MeshPhysicsMaterial => meshPhysicsMaterial;
    
    public virtual void GenerateMesh(Spline spline)
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            DestroyImmediate(child);
        }
    }
}
