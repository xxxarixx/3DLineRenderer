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
        [SerializeField] int pointsPerCurve = 5;
        [SerializeField] float distance = 1f;

        readonly List<Vector3> helpControlPoints = new();
        readonly List<Vector3> connectionPoints = new();

        public string Name => ToString();

        public bool IsEnabled => enabled;

        public void ManipulateMesh(LRCylinder3D lr, ref List<LRCylinder3D.SegmentInfo> segmentInfos, ref List<Vector3> vertices, ref List<Vector3> normals, ref List<Vector2> uvs, ref List<int> triangles)
        {
            if(segmentInfos.Count < 2)
                return;

            helpControlPoints.Clear();
            connectionPoints.Clear();
            pointsPerCurve = Mathf.Clamp(pointsPerCurve, 2, pointsPerCurve);

            int numberOfFaces = lr.numberOfFaces;
            float radius = lr.radius;

            for (int s = 0; s < segmentInfos.Count; s++)
            {
                // Change current segments to make distance for corner
                MakeDistanceForCorner(lr, s, ref segmentInfos, out bool hasCorner);

                if (!hasCorner || s == 0)
                    continue;

                CreateConnections(s, numberOfFaces, radius, lr, segmentInfos, ref vertices, ref normals, ref uvs, ref triangles);
            }
        }

        void CreateConnections(int segmentIndex, int numberOfFaces, float radius, LRCylinder3D lr, List<LRCylinder3D.SegmentInfo> segmentInfos, ref List<Vector3> vertices, ref List<Vector3> normals, ref List<Vector2> uvs, ref List<int> triangles)
        {
            var currentSegment = segmentInfos[segmentIndex];
            var previousSegment = segmentInfos[segmentIndex - 1];

            Vector3 A = Vector3.zero;
            Vector3 B = Vector3.zero;
            Vector3 C = Vector3.zero;
            if (lr.points.Count > segmentInfos.Count)
            {
                A = lr.points[segmentIndex - 1];
                B = lr.points[segmentIndex];
                C = lr.points[segmentIndex + 1];
            }
            else
            {
                A = segmentInfos[segmentIndex - 1].startSegmentCenter;
                B = segmentInfos[segmentIndex].initStartSegmentCenter;
                C = segmentInfos[segmentIndex + 1].endSegmentCenter;
                if (segmentIndex == segmentInfos.Count - 1)
                    return;
            }

            Vector3 dirToA = (A - B).normalized;
            Vector3 dirToC = (C - B).normalized;
            Vector3 inBetweenDir = normalize(-lerp(dirToA, dirToC, 0.5f));

            Vector3 prevEndCenter = previousSegment.endSegmentCenter;
            Vector3 currStartCenter = currentSegment.startSegmentCenter;

            Vector3 helpControlPoint = Vector3.Lerp(prevEndCenter, currStartCenter, 0.5f) + inBetweenDir * (distance * distanceControlPointMultiplayer);
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
                        uvs.Add(new Vector2(2f - ((float)f / numberOfFaces) * 2f, 1 - t));
                    else
                        uvs.Add(new Vector2(((float)f / numberOfFaces) * 2f, 1 - t));
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

        void MakeDistanceForCorner(LRCylinder3D lr, int segmentID, ref List<LRCylinder3D.SegmentInfo> segmentInfos, out bool hasCorner)
        {
            hasCorner = false;
            if (!lr.IsCylinderIndexValid(segmentID)) return;

            var segment = segmentInfos[segmentID];
            bool isFirstSegment = segmentID == 0;
            bool isLastSegment = segmentID == segmentInfos.Count - 1;

            // Check for corners at BOTH ends of the segment
            bool startHasCorner = false;
            bool endHasCorner = false;

            // Check previous segment connection (start of current segment)
            if (!isFirstSegment)
            {
                var prevSegment = segmentInfos[segmentID - 1];
                startHasCorner = ArePointsFormingCorner(
                    prevSegment.endSegmentCenter,
                    segment.endSegmentCenter,
                    segment.startSegmentCenter
                );
            }

            // Check next segment connection (end of current segment)
            if (!isLastSegment)
            {
                var nextSegment = segmentInfos[segmentID + 1];
                endHasCorner = ArePointsFormingCorner(
                    segment.startSegmentCenter,
                    segment.endSegmentCenter,
                    nextSegment.endSegmentCenter
                );
            }

            // Apply translations only where needed
            if (startHasCorner)
            {
                Vector3 startDirection = (segment.endSegmentCenter - segment.startSegmentCenter).normalized;
                Vector3 startTranslation = startDirection * distance;

                // Move start vertices towards segment center
                segment.startSegmentCenter += startTranslation;
                lr.ChangeSegmentVerticesLocation(segment.startSegmentVericesIndex, startTranslation);
                hasCorner = true;
            }

            if (endHasCorner)
            {
                Vector3 endDirection = (segment.startSegmentCenter - segment.endSegmentCenter).normalized;
                Vector3 endTranslation = endDirection * distance;

                // Move end vertices towards segment center
                segment.endSegmentCenter += endTranslation;
                lr.ChangeSegmentVerticesLocation(segment.endSegmentVericesIndex, endTranslation);
                //hasCorner = true;
            }
        }

        bool ArePointsFormingCorner(Vector3 a, Vector3 b, Vector3 c, float tolerance = 0.0001f)
        {
            Vector3 ab = b - a;
            Vector3 bc = c - b;

            // Need minimum segment length to avoid false positives
            if (ab.magnitude < 0.1f || bc.magnitude < 0.1f) return false;

            Vector3 crossProduct = Vector3.Cross(ab.normalized, bc.normalized);
            return crossProduct.sqrMagnitude > tolerance * tolerance;
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