using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SplineMesh))]
public class SplineMeshEditor : Editor
{
    bool showDebugSpheres = false;

    public override void OnInspectorGUI()
    {
        SplineMesh splineMesh = target as SplineMesh;

        DrawDefaultInspector();

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Editor Settings", EditorStyles.boldLabel);

        showDebugSpheres = EditorGUILayout.Toggle("Show Debug Spheres", showDebugSpheres);

        if (GUILayout.Button("Generate Mesh"))
        {
            splineMesh.SetComponentReferences();
            splineMesh.GenerateMesh();
            SceneView.RepaintAll();
        }
        if (GUILayout.Button("Update Collider"))
        {
            MeshCollider meshCollider = splineMesh.GetComponent<MeshCollider>();
            if (meshCollider) DestroyImmediate(meshCollider);
            meshCollider = splineMesh.gameObject.AddComponent<MeshCollider>();
            meshCollider.material = splineMesh.physicsMaterial;
        }
    }

    void OnSceneGUI()
    {
        SplineMesh splineMesh = target as SplineMesh;

        if (showDebugSpheres && splineMesh.vertices != null)
        {
            foreach (Vector3 vertex in splineMesh.vertices)
            {
                Handles.DrawSolidDisc(vertex, Vector3.up, 0.1f);
            }
        }
    }
}
