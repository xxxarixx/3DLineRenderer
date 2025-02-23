using System.Collections.Generic;
using UnityEngine;
using static Unity.Mathematics.math;

namespace LineRenderer3D.Modifiers
{
    class LRConnectionModifier : MonoBehaviour, IModifierBase
    {
        [SerializeField]
        bool visualizeControlPoints;

        [SerializeField]
        bool visualizeConnectionPoints;

        readonly List<Vector3> helpControlPoints = new();

        readonly List<Vector3> connectionPoints = new();

        [SerializeField]
        [Range(0, 3)]
        int iVis;

        [SerializeField]
        [Range(-2f, 2f)]
        float distanceControlPointMultiplayer;

        [SerializeField]
        float vertexGizmosSize = 0.1f;

        public void ManipulateMesh(LRCylinder3D lr, List<LRCylinder3D.SegmentInfo> segmentInfos, ref List<Vector3> vertices, ref List<Vector3> normals, ref List<Vector2> uvs, ref List<int> triangles)
        {
            helpControlPoints.Clear();
            connectionPoints.Clear();

            int numberOfFaces = lr.numberOfFaces;
            int pointsPerCurve = lr.pointsPerCurve;
            var points = lr.points;

            //Generate connection curve
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
                    Debug.DrawRay(B, dirToA, Color.black, 2f);
                    Debug.DrawRay(B, dirToC, Color.blue, 2f);
                    Debug.DrawRay(B, inBetweenDir, Color.green, 2f);

                    var currentSegment = segmentInfos[s];
                    var previousSegment = segmentInfos[s - 1];

                    //get end vertices of segment
                    //get start vertices of segment
                    int firstSegmentStartVerices = vertices.Count;
                    for (int i = 0; i < currentSegment.startSegmentVericesIndex.Count; i++)
                    {
                        var startSegmentVertex = transform.TransformPoint(vertices[currentSegment.startSegmentVericesIndex[i]]);
                        var endSegmentVertex = transform.TransformPoint(vertices[previousSegment.endSegmentVericesIndex[i]]);
                        Vector3 helpControlPoint = Vector3.Lerp(startSegmentVertex, endSegmentVertex, 0.5f) + inBetweenDir * (lr.distance * distanceControlPointMultiplayer);
                        helpControlPoints.Add(helpControlPoint);

                        int startIndex = vertices.Count;
                        for (int p = 1; p < lr.pointsPerCurve; p++)
                        {
                            vertices.Add(transform.InverseTransformPoint(QuadraticBezier(endSegmentVertex, helpControlPoint, startSegmentVertex, p / (float)pointsPerCurve)));
                            connectionPoints.Add(transform.TransformPoint(vertices[^1]));
                            normals.Add(inBetweenDir);
                            uvs.Add(new Vector2(0, 0));
                        }
                        // todo: make sure to loop around
                        // todo: make this for any number of connection points
                        // Construct triangles for this segment
                        if (s == 1) // Ensure there are enough vertices to form triangles
                        {
                            int maxVertexIndexInThisCurve = firstSegmentStartVerices + pointsPerCurve * numberOfFaces - 2;
                            for (int p = 0; p < 1; p++)
                            {
                                var next = (i + 1) % numberOfFaces;
                                //Debug.Log($"index:{i} maxIndex:{numberOfFaces} value: {currentSegment.startSegmentVericesIndex[i]}  maxValue:{vertices.Count}");
                                triangles.Add(currentSegment.startSegmentVericesIndex[i]);
                                triangles.Add((startIndex + pointsPerCurve - 2));
                                triangles.Add(currentSegment.startSegmentVericesIndex[next]);

                                triangles.Add(currentSegment.startSegmentVericesIndex[next]);
                                triangles.Add((startIndex + pointsPerCurve - 2));

                                int nextVertexIndex = startIndex + (pointsPerCurve - 2) + pointsPerCurve - 1;
                                Debug.Log($"next:{nextVertexIndex} max:{maxVertexIndexInThisCurve}");
                                if (nextVertexIndex >= maxVertexIndexInThisCurve)
                                    triangles.Add(firstSegmentStartVerices + pointsPerCurve - 2);
                                else
                                    triangles.Add(nextVertexIndex);
                            }
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

        void OnDrawGizmos()
        {
            if (visualizeControlPoints)
            {
                Gizmos.color = Color.green;
                foreach (var item in helpControlPoints)
                    Gizmos.DrawCube(item, Vector3.one * vertexGizmosSize);
            }
            if (visualizeConnectionPoints)
            {
                Gizmos.color = Color.blue;
                foreach (var item in connectionPoints)
                    Gizmos.DrawSphere(item, vertexGizmosSize);
            }
        }
    }
}
