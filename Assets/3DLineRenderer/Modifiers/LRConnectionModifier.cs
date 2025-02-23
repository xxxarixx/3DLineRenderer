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
        [SerializeField][Range(0, 3)] int iVis;
        [SerializeField][Range(-2f, 2f)] float distanceControlPointMultiplayer;
        [SerializeField] float vertexGizmosSize = 0.1f;
        [SerializeField] internal int pointsPerCurve = 5;

        readonly List<Vector3> helpControlPoints = new();
        readonly List<Vector3> connectionPoints = new();

        public void ManipulateMesh(LRCylinder3D lr, List<LRCylinder3D.SegmentInfo> segmentInfos, ref List<Vector3> vertices, ref List<Vector3> normals, ref List<Vector2> uvs, ref List<int> triangles)
        {
            helpControlPoints.Clear();
            connectionPoints.Clear();
            pointsPerCurve = Mathf.Clamp(pointsPerCurve, 2, pointsPerCurve);

            int numberOfFaces = lr.numberOfFaces;
            var points = lr.points;

            for (int s = 0; s < points.Count - 1; s++)
            {
                if (s > 0 && points.Count > 1)
                {
                    Vector3 A = points[s - 1];
                    Vector3 B = points[s];
                    Vector3 C = points[s + 1];

                    Vector3 dirToA = (A - B).normalized;
                    Vector3 dirToC = (C - B).normalized;
                    Vector3 inBetweenDir = normalize(-lerp(dirToA, dirToC, 0.5f));

                    var currentSegment = segmentInfos[s];
                    var previousSegment = segmentInfos[s - 1];
                    int initialVerticesCount = vertices.Count;

                    for (int i = 0; i < numberOfFaces; i++)
                    {
                        var startSegmentVertex = transform.TransformPoint(vertices[currentSegment.startSegmentVericesIndex[i]]);
                        var endSegmentVertex = transform.TransformPoint(vertices[previousSegment.endSegmentVericesIndex[i]]);
                        Vector3 helpControlPoint = Vector3.Lerp(startSegmentVertex, endSegmentVertex, 0.5f) + inBetweenDir * (lr.distance * distanceControlPointMultiplayer);
                        helpControlPoints.Add(helpControlPoint);

                        int startIndex = vertices.Count;
                        for (int p = 1; p < pointsPerCurve; p++)
                        {
                            float t = p / (float)(pointsPerCurve - 1);
                            Vector3 point = QuadraticBezier(endSegmentVertex, helpControlPoint, startSegmentVertex, t);
                            vertices.Add(transform.InverseTransformPoint(point));
                            connectionPoints.Add(transform.TransformPoint(vertices[^1]));

                            // UV Mapping
                            float uvU = (float)i / numberOfFaces;
                            float uvV = 1 - t; // Transition from previous segment (V=1) to current (V=0)
                            uvs.Add(new Vector2(uvU, uvV));

                            // Normal Calculation
                            Vector3 prevNormal = normals[previousSegment.endSegmentVericesIndex[i]];
                            Vector3 currNormal = normals[currentSegment.startSegmentVericesIndex[i]];
                            Vector3 lerpedNormal = Vector3.Lerp(prevNormal, currNormal, t).normalized;
                            normals.Add(lerpedNormal);
                        }
                    }

                    // Triangle generation remains the same as previous answer
                    for (int i = 0; i < numberOfFaces; i++)
                    {
                        int faceI = i;
                        int faceJ = (i + 1) % numberOfFaces;

                        int startIndexI = initialVerticesCount + faceI * (pointsPerCurve - 1);
                        int startIndexJ = initialVerticesCount + faceJ * (pointsPerCurve - 1);

                        for (int step = 0; step < pointsPerCurve; step++)
                        {
                            int currentI, nextI;
                            if (step == 0)
                            {
                                currentI = previousSegment.endSegmentVericesIndex[faceI];
                                nextI = startIndexI;
                            }
                            else if (step == pointsPerCurve - 1)
                            {
                                currentI = startIndexI + (pointsPerCurve - 2);
                                nextI = currentSegment.startSegmentVericesIndex[faceI];
                            }
                            else
                            {
                                currentI = startIndexI + (step - 1);
                                nextI = startIndexI + step;
                            }

                            int currentJ, nextJ;
                            if (step == 0)
                            {
                                currentJ = previousSegment.endSegmentVericesIndex[faceJ];
                                nextJ = startIndexJ;
                            }
                            else if (step == pointsPerCurve - 1)
                            {
                                currentJ = startIndexJ + (pointsPerCurve - 2);
                                nextJ = currentSegment.startSegmentVericesIndex[faceJ];
                            }
                            else
                            {
                                currentJ = startIndexJ + (step - 1);
                                nextJ = startIndexJ + step;
                            }

                            triangles.Add(currentI);
                            triangles.Add(currentJ);
                            triangles.Add(nextI);

                            triangles.Add(nextI);
                            triangles.Add(currentJ);
                            triangles.Add(nextJ);
                        }
                    }
                }
            }
        }

        Vector3 QuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            float u = 1 - t;
            return (u * u) * p0 + (2 * u * t) * p1 + (t * t) * p2;
        }

        void OnDrawGizmos() { /* Gizmo drawing remains same */ }
    }
}