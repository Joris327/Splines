using UnityEngine;

public class SplineFollower : MonoBehaviour
{
    [SerializeField] Spline spline;
    [SerializeField] float timeScale = 1;
    
    float timeVar = 0;
    int curveIndex = 0;
    
    void Update()
    {
        TravelAlongSpline();
    }
    
    void TravelAlongSpline()
    {
        if (!spline || spline.curves.Count < 1) return;
        
        transform.position = spline.curves[curveIndex].CalculatePointOnCurve(timeVar, spline.transform.position);
        
        timeVar += Time.deltaTime * timeScale;
        
        if (timeVar >= 1)
        {
            timeVar = 0;
            curveIndex = curveIndex < spline.curves.Count-1 ? curveIndex+1: curveIndex = 0;
        }
    }
}
