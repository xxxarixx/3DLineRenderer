using System.Collections.Generic;
using UnityEngine;
using static Unity.Mathematics.math;

namespace LineRenderer3D.Modifiers
{
    class LRConnectionModifier : MonoBehaviour, IModifierBase
    {
        [SerializeField] bool visualizeControlPoints;
        [SerializeField] bool visualizeConnectionPoints;
        [SerializeField] bool visualizeDirections;
        [SerializeField][Range(-2f, 2f)] float distanceControlPointMultiplayer;
        [SerializeField] float vertexGizmosSize = 0.1f;
        [SerializeField] internal int pointsPerCurve = 5;

        readonly List<Vector3> helpControlPoints = new();
        readonly List<Vector3> connectionPoints = new();

        public string Name => ToString();

        public void ManipulateMesh(LRCylinder3D lr, List<LRCylinder3D.SegmentInfo> segmentInfos, ref List<Vector3> vertices, ref List<Vector3> normals, ref List<Vector2> uvs, ref List<int> triangles)
        {
            helpControlPoints.Clear();
            connectionPoints.Clear();
            pointsPerCurve = Mathf.Clamp(pointsPerCurve, 2, pointsPerCurve);

            int numberOfFaces = lr.numberOfFaces;
            float radius = lr.radius;

            for (int s = 1; s < lr.points.Count - 1; s++) // Start from 1 to avoid first segment
            {
                var currentSegment = segmentInfos[s];
                var previousSegment = segmentInfos[s - 1];

                Vector3 A = lr.points[s - 1];
                Vector3 B = lr.points[s];
                Vector3 C = lr.points[s + 1];

                Vector3 dirToA = (A - B).normalized;
                Vector3 dirToC = (C - B).normalized;
                Vector3 inBetweenDir = normalize(-lerp(dirToA, dirToC, 0.5f));

                Vector3 prevEndCenter = previousSegment.endSegmentCenter;
                Vector3 currStartCenter = currentSegment.startSegmentCenter;

                Vector3 helpControlPoint = Vector3.Lerp(prevEndCenter, currStartCenter, 0.5f) + inBetweenDir * (lr.distance * distanceControlPointMultiplayer);
                helpControlPoints.Add(helpControlPoint);

                int initialVerticesCount = vertices.Count;

                // Generate vertices for the connection curve
                for (int p = 1; p < pointsPerCurve; p++)
                {
                    float t = p / (float)(pointsPerCurve - 1);
                    Vector3 centralPoint = QuadraticBezier(prevEndCenter, helpControlPoint, currStartCenter, t);
                    Vector3 tangent = QuadraticBezierDerivative(prevEndCenter, helpControlPoint, currStartCenter, t).normalized;
                    Quaternion rotation = Quaternion.LookRotation(tangent);

                    for (int f = 0; f < numberOfFaces; f++)
                    {
                        float theta = Mathf.PI * 2 * f / numberOfFaces;
                        Vector3 circleOffset = new Vector3(
                            Mathf.Cos(theta) * radius,
                            Mathf.Sin(theta) * radius,
                            0
                        );

                        Vector3 vertexPos = centralPoint + rotation * circleOffset;
                        vertices.Add(transform.InverseTransformPoint(vertexPos));
                        connectionPoints.Add(vertexPos);

                        normals.Add((vertexPos - centralPoint).normalized);
                        // UV mapping
                        if (f > numberOfFaces / 2)
                            uvs.Add(new Vector2(1f - (float)f / numberOfFaces, 1 - t));
                        else
                            uvs.Add(new Vector2((float)f / numberOfFaces, 1 - t));
                        //uvs.Add(new Vector2((float)f / numberOfFaces, 1 - t));
                    }
                }

                // Generate triangles between segments
                for (int i = 0; i < numberOfFaces; i++)
                {
                    int nextI = (i + 1) % numberOfFaces;

                    for (int p = 0; p < pointsPerCurve - 1; p++)
                    {
                        int currentP = p;
                        int nextP = p + 1;

                        // Current ring indices
                        int currentA = GetRingIndex(previousSegment, currentSegment, initialVerticesCount, numberOfFaces, i, currentP);
                        int currentB = GetRingIndex(previousSegment, currentSegment, initialVerticesCount, numberOfFaces, nextI, currentP);

                        // Next ring indices
                        int nextA = GetRingIndex(previousSegment, currentSegment, initialVerticesCount, numberOfFaces, i, nextP);
                        int nextB = GetRingIndex(previousSegment, currentSegment, initialVerticesCount, numberOfFaces, nextI, nextP);

                        // Create two triangles per quad
                        triangles.Add(currentA);
                        triangles.Add(currentB);
                        triangles.Add(nextA);

                        triangles.Add(nextA);
                        triangles.Add(currentB);
                        triangles.Add(nextB);
                    }
                }
            }
        }

        int GetRingIndex(LRCylinder3D.SegmentInfo prevSeg, LRCylinder3D.SegmentInfo currSeg, int initialVertices, int faces, int faceIndex, int ring)
        {
            if (ring == 0) // Previous segment's end
                return prevSeg.endSegmentVericesIndex[faceIndex];

            if (ring == pointsPerCurve - 1) // Current segment's start
                return currSeg.startSegmentVericesIndex[faceIndex];

            // New connection vertices: initialVertices + (ring-1)*faces + faceIndex
            return initialVertices + (ring - 1) * faces + faceIndex;
        }

        Vector3 QuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            float u = 1 - t;
            return (u * u) * p0 + (2 * u * t) * p1 + (t * t) * p2;
        }

        Vector3 QuadraticBezierDerivative(Vector3 p0, Vector3 p1, Vector3 p2, float t) =>
            2 * (1 - t) * (p1 - p0) + 2 * t * (p2 - p1);

        void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            if (visualizeControlPoints)
                foreach (var point in helpControlPoints)
                    Gizmos.DrawSphere(point, vertexGizmosSize);

            Gizmos.color = Color.yellow;
            if (visualizeConnectionPoints)
                foreach (var point in connectionPoints)
                    Gizmos.DrawWireSphere(point, vertexGizmosSize / 2);
        }
    }
}