using UnityEngine;

public class SplineFollower : MonoBehaviour
{
    [SerializeField] Spline spline;
    [SerializeField] float speed = 1;

    float distance = 0;
    int curveIndex = 0;

    void Update()
    {
        TravelAlongSpline();
    }

    void TravelAlongSpline()
    {
        if (!spline || spline.curves.Count < 1) return;

        float t = spline.curves[curveIndex].GetTFromDistance(distance);
        transform.position = spline.curves[curveIndex].CalculatePointOnCurve(t, spline.transform.position);

        distance += Time.deltaTime * speed;

        if (t >= 1)
        {
            distance = 0;
            curveIndex = curveIndex < spline.curves.Count - 1 ? curveIndex + 1 : curveIndex = 0;
        }
    }
}
