using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SplineMesh))]
public class SplineMeshEditor : Editor
{
    public override void OnInspectorGUI()
    {
        SplineMesh splineMesh = target as SplineMesh;

        DrawDefaultInspector();

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Editor Settings", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();

        if (GUILayout.Button("Generate Mesh"))
        {
            splineMesh.GenerateMesh(splineMesh.GetComponent<Spline>());
            SceneView.RepaintAll();
        }
        if (GUILayout.Button("Update Collider"))
        {
            // MeshCollider meshCollider = splineMesh.GetComponent<MeshCollider>();
            // if (meshCollider) DestroyImmediate(meshCollider);
            // meshCollider = splineMesh.gameObject.AddComponent<MeshCollider>();
            // meshCollider.material = splineMesh.MeshPhysicsMaterial;
        }
    }
}
