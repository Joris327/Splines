using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Spline))]
//[CanEditMultipleObjects]
public class SplineEditor : Editor
{
    const float handleSize = 0.04f;
	const float pickSize = 0.06f;
	
	int selectedIndex = -1;
    
    static int linesPerCurve = 15;
    static bool showDirections = false;
    static bool curvesFoldoutStatus = true;

    public override void OnInspectorGUI()
    {
        Spline spline = target as Spline;
        
        SerializedProperty looping = serializedObject.FindProperty("looping");
        EditorGUILayout.PropertyField(looping);
        
        SerializedProperty mirrored = serializedObject.FindProperty("mirrored");
        EditorGUILayout.PropertyField(mirrored);
        
        curvesFoldoutStatus = EditorGUILayout.Foldout(curvesFoldoutStatus, "Curves");
        if (curvesFoldoutStatus)
        {
            SerializedProperty curves = serializedObject.FindProperty("curves");
            
            EditorGUI.indentLevel++;
            GUI.enabled = false;
            for (int i = 0; i < curves.arraySize; i++)
            {
                SerializedProperty curve = curves.GetArrayElementAtIndex(i);
                EditorGUILayout.PropertyField(curve);
            }
            GUI.enabled = true;
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("Editor Settings", EditorStyles.boldLabel);

        linesPerCurve = EditorGUILayout.IntField("Lines Per Curve", linesPerCurve);
        showDirections = EditorGUILayout.Toggle("Show Directions", showDirections);

        EditorGUILayout.Space();

        if (GUILayout.Button("Add Curve"))
        {
            CurveAngles angles = spline.curves.Count > 0 ? spline.curves[^1].angles : new();
            spline.curves.Add(new BezierCurve(spline.curves.Count > 0 ? spline.curves[^1][3] : new Vector3(), angles));
            SceneView.RepaintAll();
        }
        if (GUILayout.Button("Remove Curve") && spline.curves.Count > 0)
        {
            spline.curves.RemoveAt(spline.curves.Count - 1);
            SceneView.RepaintAll();
        }
        
        serializedObject.ApplyModifiedProperties();
    }
    
    void OnSceneGUI()
    {
        Spline spline = target as Spline;
        
        Transform handleTransform = spline.transform;
        Quaternion handleRotation = Tools.pivotRotation == PivotRotation.Local ? handleTransform.rotation : Quaternion.identity;

        if (spline.curves == null || spline.curves.Count == 0) return;
        for (int i = 0; i < spline.curves.Count; i++)
        {
            BezierCurve curve = spline.curves[i];

            Vector3 point0 = ShowPoint(0, i, spline, curve, handleRotation);
            Vector3 point1 = ShowPoint(1, i, spline, curve, handleRotation);
            Vector3 point2 = ShowPoint(2, i, spline, curve, handleRotation);
            Vector3 point3 = ShowPoint(3, i, spline, curve, handleRotation);

            Handles.color = Color.grey;
            Handles.DrawLine(point0, point1);
            Handles.DrawLine(point2, point3);
            Handles.color = Color.white;

            Vector3 lineStart = curve.point0 + spline.transform.position;
            for (int j = 0; j <= linesPerCurve; j++)
            {
                //draw spline itself
                Handles.color = Color.white;
                Vector3 lineEnd = spline.curves[i].CalculatePointOnCurve(j / (float)linesPerCurve, spline.transform.position);
                Handles.DrawLine(spline.transform.TransformDirection(lineStart), spline.transform.TransformDirection(lineEnd));

                //draw direction
                if (showDirections)
                {
                    Handles.color = Color.green;
                    Handles.DrawLine(lineEnd, lineEnd + curve.GetDirectionAt(j / (float)linesPerCurve));
                }
                
                lineStart = lineEnd;
            }
        }
    }
    
    Vector3 ShowPoint(int pointIndex, int curveIndex, Spline spline, BezierCurve curve, Quaternion handleRotation)
    {
        Transform splineTransform = spline.transform;
        
        Vector3 point = splineTransform.TransformPoint(curve[pointIndex]);
        Vector3 oldPoint = point;
        
        float size = HandleUtility.GetHandleSize(point);
        Handles.color = Color.white;
        
        if (Handles.Button(point, handleRotation, size * handleSize, size * pickSize, Handles.DotHandleCap))
        {
            selectedIndex = pointIndex + curveIndex * 4;
        }
        
        if (Tools.current == Tool.Rotate)
        {
            if (pointIndex != 0 && pointIndex != 3) return point;
            
            EditorGUI.BeginChangeCheck();

            Vector3 direction = curve.GetDirectionAt(pointIndex == 0 ? 0 : 1);
            Quaternion currentRotation = Quaternion.AngleAxis(pointIndex == 0 ? curve.angles[0] : curve.angles[1], direction);
            Quaternion newRotation = Handles.Disc(currentRotation, point, direction, 1, false, 0);

            if (EditorGUI.EndChangeCheck())
            {
                //undo
                //Undo.RecordObject(spline, "Rotate point");
                EditorUtility.SetDirty(spline);

                spline.RotatePointOnCurveTo(newRotation, curveIndex, pointIndex);

                SplineMesh splineMesh = spline.GetComponent<SplineMesh>();
                if (splineMesh && splineMesh.GenerateMeshOnEdit)
                {
                    splineMesh.GenerateMesh(spline);
                }
            }
        }
        
        if (selectedIndex != pointIndex + curveIndex * 4) return point;
        
        if (Tools.current == Tool.Move)
        {
            EditorGUI.BeginChangeCheck();
            
            point = Handles.DoPositionHandle(point, handleRotation);

            if (EditorGUI.EndChangeCheck())
            {
                //Undo.RecordObject(spline, "Move point");
                EditorUtility.SetDirty(spline);
                spline.MovePointOnCurveTo(splineTransform.InverseTransformPoint(point), curveIndex, pointIndex);

                SplineMesh splineMesh = spline.GetComponent<SplineMesh>();
                if (splineMesh && splineMesh.GenerateMeshOnEdit)
                {
                    splineMesh.GenerateMesh(spline);
                }

                curve.CalculateDistanceLUT();
            }
        }
        
        return point;
    }
}
