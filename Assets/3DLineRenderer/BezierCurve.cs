using UnityEngine;

public class BezierCurve : MonoBehaviour
{
    public Transform point0; // Start point
    public Transform point1; // Control point
    public Transform point2; // End point
    public int resolution = 20; // Number of points to generate

    private void OnDrawGizmos()
    {
        if (point0 == null || point1 == null || point2 == null) return;

        Vector3 previousPoint = point0.position;
        for (int i = 1; i <= resolution; i++)
        {
            float t = i / (float)resolution;
            Vector3 newPoint = QuadraticBezier(point0.position, point1.position, point2.position, t);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(previousPoint, newPoint);
            previousPoint = newPoint;
        }
    }

    private Vector3 QuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        float u = 1 - t;
        return (u * u) * p0 + (2 * u * t) * p1 + (t * t) * p2;
    }
}