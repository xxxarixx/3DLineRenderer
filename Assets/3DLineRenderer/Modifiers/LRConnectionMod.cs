using LineRenderer3D.Datas;
using System.Collections.Generic;
using UnityEngine;
using static LineRenderer3D.Datas.LRData;
using static Unity.Mathematics.math;

namespace LineRenderer3D.Mods
{
    /// <summary>
    /// Modifies the connection between segments by adding a curve between them.<br/>
    /// </summary>
    class LRConnectionModifier : MonoBehaviour, ILRModBase
    {
        int _pointsPerCurve = 5;

        [SerializeField]
        [Range(0.01f,0.5f)]
        [Tooltip("Adjusts the distance of the segment corners to create space for a smooth transition between segments.")]
        float _distance = 1f;

        [SerializeField]
        [Range(0.1f, 0.5f)]
        [Tooltip("Its blend range between connection curve and cylinder start/end.")]
        float blendRangeConnectionCylinder = 0.25f;

        [Header("Debug stuff")]

        [SerializeField] 
        bool _visualizeControlPoints;

        [SerializeField] 
        bool _visualizeConnectionPoints;

        [SerializeField] 
        bool _visualizeDirections;

        [SerializeField] 
        float _vertexGizmosSize = 0.1f;

        readonly List<Vector3> helpControlPoints = new();

        readonly List<Vector3> connectionPoints = new();

       

        public string Name => ToString();

        public bool IsEnabled => enabled;

        void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            if (_visualizeControlPoints)
                foreach (var point in helpControlPoints)
                    Gizmos.DrawSphere(point, _vertexGizmosSize);

            Gizmos.color = Color.yellow;
            if (_visualizeConnectionPoints)
                foreach (var point in connectionPoints)
                    Gizmos.DrawWireSphere(point, _vertexGizmosSize / 2);
        }
        
        public void ManipulateMesh(LRData data, ref List<SegmentInfo> segmentInfos, ref List<Vector3> vertices, ref List<Vector3> normals, ref List<Vector2> uvs, ref List<int> triangles)
        {
            if(segmentInfos.Count < 2)
                return;

            // Prepare lists to new LR
            helpControlPoints.Clear();
            connectionPoints.Clear();
            _pointsPerCurve = Mathf.Clamp(data.Config.NumberOfFaces, 2, data.Config.NumberOfFaces);

            for (int s = 0; s < segmentInfos.Count; s++)
            {
                // Change current segments to make distance for corner
                MakeDistanceForCorner(segmentID: s, data, ref vertices, ref segmentInfos, out bool hasCorner);

                if (!hasCorner || s == 0)
                    continue;

                CreateConnections(segmentIndex: s, data, segmentInfos, ref vertices, ref normals, ref uvs, ref triangles);
            }
        }

        [SerializeField]
        Vector3 lastRotOffset;

        /// <summary>
        /// Creates connections between segments by generating vertices, normals, UVs, and triangles.
        /// </summary>
        /// <param name="segmentIndex">The index of the current segment.</param>
        void CreateConnections(int segmentIndex, LRData data, List<SegmentInfo> segmentInfos, ref List<Vector3> vertices, ref List<Vector3> normals, ref List<Vector2> uvs, ref List<int> triangles)
        {
            if(segmentIndex >= segmentInfos.Count && segmentInfos.Count >= data.Config.Points.Count)
                return;

            SegmentInfo currentSegment = segmentInfos[segmentIndex];
            SegmentInfo previousSegment = segmentInfos[segmentIndex - 1];

            int numberOfFaces = data.Config.NumberOfFaces;
            float radius = data.Config.Radius;

            // A => Previous Point, B => Current Point, C => Next Point
            Vector3 A = Vector3.zero;
            Vector3 B = Vector3.zero;
            Vector3 C = Vector3.zero;


            if (data.Config.Points.Count > segmentInfos.Count)
            {
                A = data.Config.Points[segmentIndex - 1];
                B = data.Config.Points[segmentIndex];
                C = data.Config.Points[segmentIndex + 1];
            }
            else
            {
                A = segmentInfos[segmentIndex - 1].endSegmentCenter;
                B = segmentInfos[segmentIndex].initStartSegmentCenter;
                C = segmentInfos[segmentIndex].startSegmentCenter;
            }

            // Auto adjust control point based on the angle between segments
            float distanceControlPointMultiplayer = GetDistanceMultiplayerFromAngle(LineRenderer3DExtenction.GetAngleBetweenVectors(A, B, C));

            Vector3 dirToA = (A - B).normalized;
            Vector3 dirToC = (C - B).normalized;
            Vector3 inBetweenDir = normalize(-lerp(dirToA, dirToC, 0.5f));

            Vector3 prevEndCenter = previousSegment.endSegmentCenter;
            Vector3 currStartCenter = currentSegment.startSegmentCenter;

            Vector3 helpControlPoint = Vector3.Lerp(prevEndCenter, currStartCenter, 0.5f) + inBetweenDir * (_distance * distanceControlPointMultiplayer);
            helpControlPoints.Add(helpControlPoint);

            int initialVerticesCount = vertices.Count;

            // Generate vertices for the connection curve
            for (int p = 0; p < _pointsPerCurve; p++)
            {
                float t = p / (float)(_pointsPerCurve - 1);
                Vector3 centralPoint = QuadraticBezier(prevEndCenter, helpControlPoint, currStartCenter, t);
                Vector3 tangent = QuadraticBezierDerivative(prevEndCenter, helpControlPoint, currStartCenter, t).normalized;
                // Smooth rotation by using previous up direction
                Quaternion rotation = Quaternion.LookRotation(tangent);
                // TODO: there is still a little bug/issue with rotation when angle is very sharp.
                if (t < 0.5f)
                {
                    float blendFactor = t / blendRangeConnectionCylinder;
                    rotation = Quaternion.Lerp(previousSegment.rotation, rotation, blendFactor);
                }
                else
                {
                    float blendFactor = (t - (1 - blendRangeConnectionCylinder)) / blendRangeConnectionCylinder;
                    rotation = Quaternion.Lerp(rotation, currentSegment.rotation, blendFactor);
                }

                for (int f = 0; f < numberOfFaces; f++)
                {
                    // TODO: try to lerp points so that last will connect with segment start
                    float theta = Mathf.PI * 2 * f / numberOfFaces;
                    Vector3 circleOffset = new(
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
                }
                
            }

            // Generate triangles between segments
            for (int i = 0; i < numberOfFaces; i++)
            {
                int nextI = (i + 1) % numberOfFaces;
                

                for (int p = 0; p < _pointsPerCurve; p++)
                {
                    int currentP = p;
                    int nextP = p + 1;

                    // Current ring indices
                    int currentA = GetRingIndex(previousSegment, currentSegment, initialVerticesCount, numberOfFaces, i, currentP);
                    int currentB = GetRingIndex(previousSegment, currentSegment, initialVerticesCount, numberOfFaces, nextI, currentP);

                    // Next ring indices
                    int nextA = GetRingIndex(previousSegment, currentSegment, initialVerticesCount, numberOfFaces, i, nextP);
                    int nextB = GetRingIndex(previousSegment, currentSegment, initialVerticesCount, numberOfFaces, nextI, nextP);
                    if (p != _pointsPerCurve - 1)
                    {
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

        float GetDistanceMultiplayerFromAngle(float angle)
        {
            // Clamp the angle to the specified range
            float clampedAngle = Mathf.Clamp(angle, 0f, 180f);

            // Normalize the angle to a 0-1 range
            float t = Mathf.InverseLerp(0f, 180f, clampedAngle);

            // Invert the normalized value (1 - t) and map to the value range
            return Mathf.Lerp(1f, 0f, t);
        }

        /// <summary>
        /// Adjusts the vertices of the segment to create space for a corner.
        /// </summary>
        void MakeDistanceForCorner(int segmentID, LRData data, ref List<Vector3> vertices, ref List<SegmentInfo> segmentInfos, out bool hasCorner)
        {
            hasCorner = false;
            if (!data.IsCylinderIndexValid(segmentID)) return;

            var segment = segmentInfos[segmentID];
            bool isFirstSegment = segmentID == 0;
            bool isLastSegment = segmentID == segmentInfos.Count - 1;

            bool startHasCorner = false;
            bool endHasCorner = false;

            // Check previous segment (start of current segment)
            if (!isFirstSegment)
            {
                var prevSegment = segmentInfos[segmentID - 1];
                startHasCorner = ArePointsFormingCorner(
                    prevSegment.endSegmentCenter,
                    segment.endSegmentCenter,
                    segment.startSegmentCenter
                );
            }

            // Check next segment (end of current segment)
            if (!isLastSegment)
            {
                var nextSegment = segmentInfos[segmentID + 1];
                endHasCorner = ArePointsFormingCorner(
                    segment.startSegmentCenter,
                    segment.endSegmentCenter,
                    nextSegment.endSegmentCenter
                );
            }

            // Calculate potential new length after adjustments
            float originalLength = Vector3.Distance(segment.startSegmentCenter, segment.endSegmentCenter);
            float remainingLength = originalLength;

            Vector3 startTranslation = Vector3.zero;
            Vector3 endTranslation = Vector3.zero;

            if (startHasCorner)
            {
                Vector3 startDirection = (segment.endSegmentCenter - segment.startSegmentCenter).normalized;
                startTranslation = startDirection * _distance;
                remainingLength -= _distance;
            }

            if (endHasCorner)
            {
                Vector3 endDirection = (segment.startSegmentCenter - segment.endSegmentCenter).normalized;
                endTranslation = endDirection * _distance;
                remainingLength -= _distance;
            }

            // Ensure the remaining length is at least twice the radius to prevent overlap
            float minLength = data.Config.SegmentMinLength;
            float totalAdjustment = originalLength - minLength;
            if (remainingLength < minLength)
            {
                // Adjust distances proportionally
                if (totalAdjustment > 0)
                {
                    // Not enough to adjust
                    if (startHasCorner && endHasCorner)
                    {
                        float adjustRatio = totalAdjustment / (2 * _distance);
                        startTranslation *= adjustRatio;
                        endTranslation *= adjustRatio;
                    }
                    else if (startHasCorner)
                    {
                        startTranslation = (segment.endSegmentCenter - segment.startSegmentCenter).normalized * totalAdjustment;
                    }
                    else if (endHasCorner)
                    {
                        endTranslation = (segment.startSegmentCenter - segment.endSegmentCenter).normalized * totalAdjustment;
                    }
                }

            }

            // Apply translations
            if (startHasCorner)
            {
                if(totalAdjustment > 0)
                {
                    segment.startSegmentCenter += startTranslation;
                    foreach (int index in segment.startSegmentVericesIndex)
                        vertices[index] += startTranslation;
                }
                hasCorner = true;
            }

            if (endHasCorner)
            {
                if(totalAdjustment > 0)
                {
                    segment.endSegmentCenter += endTranslation;
                    foreach (int index in segment.endSegmentVericesIndex)
                        vertices[index] += endTranslation;
                }
            }
        }

        /// <summary>
        /// Determines if three points form a corner based on the cross product of their vectors.
        /// (BUG) HERE IS THE BUG WITH DISAPPEARING CONNECTIONS
        /// </summary>
        bool ArePointsFormingCorner(Vector3 a, Vector3 b, Vector3 c, float tolerance = 0.0001f)
        {
            Vector3 ab = b - a;
            Vector3 bc = c - b;

            // Need minimum segment length to avoid false positives
            //if (ab.magnitude < 0.1f || bc.magnitude < 0.1f) return false;

            Vector3 crossProduct = Vector3.Cross(ab.normalized, bc.normalized);
            return crossProduct.sqrMagnitude > tolerance * tolerance;
        }

        /// <summary>
        /// Gets the index of a vertex in a ring of vertices, used for generating triangles.
        /// </summary>
        int GetRingIndex(SegmentInfo prevSeg, SegmentInfo currSeg, int initialVertices, int faces, int faceIndex, int ring)
        {
           /* if (ring == 0) // Previous segment's end
                return prevSeg.endSegmentVericesIndex[faceIndex];

            if (ring == _pointsPerCurve - 1) // Current segment's start
                return currSeg.startSegmentVericesIndex[faceIndex];*/

            // New connection vertices: initialVertices + (ring-1)*faces + faceIndex
            return initialVertices + (ring) * faces + faceIndex;
        }

        /// <param name="t">Interpolation parameter (0 to 1).</param>
        /// <returns>The point on the Bezier curve.</returns>
        Vector3 QuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            t = Mathf.Clamp01(t);
            float u = 1 - t;
            return (u * u) * p0 + (2 * u * t) * p1 + (t * t) * p2;
        }

        /// <param name="t">Interpolation parameter (0 to 1).</param>
        /// <returns>The derivative of the Bezier curve.</returns>
        Vector3 QuadraticBezierDerivative(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            t = Mathf.Clamp01(t);
            return 2 * (1 - t) * (p1 - p0) + 2 * t * (p2 - p1);
        }
    }
}