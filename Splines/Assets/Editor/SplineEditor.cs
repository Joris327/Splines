using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

[CustomEditor(typeof(Spline))]
public class SplineEditor : Editor
{
    const float handleSize = 0.04f;
	const float pickSize = 0.06f;
	
	int selectedIndex = -1;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        Spline spline = target as Spline;

        //EditorGUILayout.IntField("Lines per Curve", linesPerCurve);
        //EditorGUILayout.Toggle("Show Direction", showDirection);

        EditorGUILayout.Space();

        if (GUILayout.Button("Add Curve"))
        {
            spline.curves.Add(new(spline.curves.Count > 0 ? spline.curves[^1].points[^1] : new Vector3()));
            SceneView.RepaintAll();
        }
        if (GUILayout.Button("Remove Curve") && spline.curves.Count > 0)
        {
            spline.curves.RemoveAt(spline.curves.Count - 1);
            SceneView.RepaintAll();
        }
        
        EditorGUILayout.Space();
        
        SplineMesh splineMesh = spline.GetComponent<SplineMesh>();
        if (splineMesh)
        {
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
                meshCollider = splineMesh.AddComponent<MeshCollider>();
                meshCollider.material = splineMesh.physicsMaterial;
            }
            //EditorGUILayout.Toggle("Generate Mesh On Edit", GenerateMeshOnEdit);
        }
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

            Vector3 point0 = ShowPoint(0, i, spline, curve, handleTransform, handleRotation);
            Vector3 point1 = ShowPoint(1, i, spline, curve, handleTransform, handleRotation);
            Vector3 point2 = ShowPoint(2, i, spline, curve, handleTransform, handleRotation);
            Vector3 point3 = ShowPoint(3, i, spline, curve, handleTransform, handleRotation);

            Handles.color = Color.grey;
            Handles.DrawLine(point0, point1);
            Handles.DrawLine(point2, point3);
            Handles.color = Color.white;

            //Handles.DrawBezier(point0, point3, point1, point2, Color.white, Texture2D.whiteTexture, 1);

            Vector3 lineStart = curve.points[0] + spline.transform.position;
            for (int j = 0; j <= spline.linesPerCurve; j++)
            {
                //draw spline itself
                Handles.color = Color.white;
                Vector3 lineEnd = spline.curves[i].CalculatePointOnCurve(j / (1f * spline.linesPerCurve), spline.transform.position);
                Handles.DrawLine(spline.transform.TransformDirection(lineStart), spline.transform.TransformDirection(lineEnd));

                //draw direction
                if (spline.showDirection)
                {
                    Handles.color = Color.green;
                    Handles.DrawLine(lineEnd, lineEnd + curve.GetDirection(j / (float)spline.linesPerCurve, spline.transform));
                }
                
                lineStart = lineEnd;
            }
        }
    }
    
    Vector3 ShowPoint(int pointIndex, int curveIndex, Spline spline, BezierCurve curve, Transform handleTransform, Quaternion handleRotation)
    {
        Vector3 point = handleTransform.TransformPoint(curve.points[pointIndex]);
        Vector3 oldPoint = point;
        float size = HandleUtility.GetHandleSize(point);
        Handles.color = Color.white;

        if (Handles.Button(point, handleRotation, size * handleSize, size * pickSize, Handles.DotHandleCap))
        {
            selectedIndex = pointIndex + curveIndex * 4;
        }
        
        if (selectedIndex != pointIndex + curveIndex * 4) return point;
        
        if (Tools.current == Tool.Rotate)
        {
            if (pointIndex != 0 && pointIndex != 3) return point;
            EditorGUI.BeginChangeCheck();

            Vector3 direction = curve.GetDirection(pointIndex == 0 ? 0 : 1, spline.transform);
            Quaternion currentRotation = Quaternion.AngleAxis(pointIndex == 0 ? curve.angles[0] : curve.angles[1], direction);
            Quaternion newRotation = Handles.Disc(currentRotation, point, direction, 1, false, 0);

            if (EditorGUI.EndChangeCheck())
            {
                //undo
                Undo.RecordObject(spline, "Rotate point");
                EditorUtility.SetDirty(spline);

                //calculate the new angle for the point
                Quaternion check = Quaternion.AngleAxis(180, direction);
                //Debug.Log(Quaternion.Dot(check, newRotation));
                float newAngle = 0;

                if (Quaternion.Dot(check, newRotation) > 0)
                {
                    newAngle = Quaternion.Angle(Quaternion.identity, newRotation);
                }
                else
                {
                    newAngle = -Quaternion.Angle(Quaternion.identity, newRotation);
                }

                //apply the new angle
                curve.angles[pointIndex == 0 ? 0 : 1] = newAngle;

                //adapt connecting points
                if (pointIndex == 0)
                {
                    if (curveIndex == 0 && spline.Loop) spline.curves[^1].angles[^1] = newAngle;
                    else if (curveIndex > 0) spline.curves[curveIndex - 1].angles[^1] = newAngle;
                }
                else //point index == 3
                {
                    if (curveIndex == spline.curves.Count - 1 && spline.Loop) spline.curves[0].angles[0] = newAngle;
                    else if (curveIndex < spline.curves.Count - 1) spline.curves[curveIndex + 1].angles[0] = newAngle;
                }

                SplineMesh splineMesh = spline.GetComponent<SplineMesh>();
                if (splineMesh && splineMesh.GenerateMeshOnEdit)
                {
                    splineMesh.SetComponentReferences();
                    splineMesh.GenerateMesh();
                }
            }
        }
        else if (Tools.current == Tool.Move)
        {
            EditorGUI.BeginChangeCheck();
            point = Handles.DoPositionHandle(point, handleRotation);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(spline, "Move point");
                EditorUtility.SetDirty(spline);
                curve.points[pointIndex] = handleTransform.InverseTransformPoint(point);

                if (pointIndex == 0 && curveIndex > 0)
                {
                    spline.curves[curveIndex - 1].points[3] = handleTransform.InverseTransformPoint(point);
                }
                else if (pointIndex == 3 && curveIndex < spline.curves.Count - 1)
                {
                    spline.curves[curveIndex + 1].points[0] = handleTransform.InverseTransformPoint(point);
                }
                else if (spline.Loop)
                {
                    if (curveIndex == 0 && pointIndex == 0) spline.curves[^1].points[3] = handleTransform.InverseTransformPoint(point);
                    else if (curveIndex == spline.curves.Count - 1 && pointIndex == 3) spline.curves[0].points[0] = handleTransform.InverseTransformPoint(point);
                }

                if (spline.Mirrored)
                {
                    Vector3 pointDiff = point - oldPoint;
                    Vector3 oppositePos;

                    switch (pointIndex)
                    {
                        case 0:
                            curve.points[1] += pointDiff;

                            if (curveIndex > 0) spline.curves[curveIndex - 1].points[2] += pointDiff;
                            else if (spline.Loop && curveIndex == 0) spline.curves[^1].points[2] += pointDiff;
                            break;

                        case 1:
                            oppositePos = (curve.points[0] - curve.points[1]) * 2 + curve.points[1];

                            if (spline.Loop && curveIndex == 0) spline.curves[^1].points[2] = oppositePos;
                            else if (curveIndex > 0) spline.curves[curveIndex - 1].points[2] = oppositePos;
                            break;

                        case 2:
                            oppositePos = (curve.points[3] - curve.points[2]) * 2 + curve.points[2];

                            if (spline.Loop && curveIndex == spline.curves.Count - 1) spline.curves[0].points[1] = oppositePos;
                            else if (curveIndex < spline.curves.Count - 1) spline.curves[curveIndex + 1].points[1] = oppositePos;
                            break;

                        case 3:
                            curve.points[2] += pointDiff;

                            if (curveIndex < spline.curves.Count - 1) spline.curves[curveIndex + 1].points[1] += pointDiff;
                            else if (spline.Loop && curveIndex == spline.curves.Count - 1) spline.curves[0].points[1] += pointDiff;
                            break;
                    }
                }

                SplineMesh splineMesh = spline.GetComponent<SplineMesh>();
                if (splineMesh && splineMesh.GenerateMeshOnEdit)
                {
                    splineMesh.SetComponentReferences();
                    splineMesh.GenerateMesh();
                }
            }
        }
        
        return point;
    }
}
