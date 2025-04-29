using LineRenderer3D.Datas;
using LinerRenderer3D.Datas;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static LineRenderer3D.Datas.LRData;
using static Unity.Mathematics.math;

namespace LineRenderer3D.Mods
{
    /// <summary>
    /// Modifies the connection between segments by adding a curve between them.<br/>
    /// </summary>
    [ExecuteAlways]
    class LRConnectionModifier : MonoBehaviour, ILRModBase
    {
        int _pointsPerCurve;

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
        float _vertexGizmosSize = 0.1f;

        readonly List<Vector3> helpControlPoints = new();

        readonly List<Vector3> connectionPoints = new();

        public bool IsEnabled => enabled;

        public string KeyName => nameof(LRConnectionModifier);

        void OnDrawGizmos()
        {
            // Control Points
            Gizmos.color = Color.cyan;
            if (_visualizeControlPoints)
                foreach (var point in helpControlPoints)
                    Gizmos.DrawSphere(point, _vertexGizmosSize);

            // Connection Points
            Gizmos.color = Color.yellow;
            if (_visualizeConnectionPoints)
                foreach (var point in connectionPoints)
                    Gizmos.DrawWireSphere(point, _vertexGizmosSize / 2);
        }

        LRBoot _boot;

        void OnEnable()
        {
            if (_boot == null)
                _boot = GetComponent<LRBoot>();
            if (_boot.Data.ModsInfos.Find(x => x.Name == KeyName) == null)
                _boot.AddModMarkAllPointsDirty(KeyName);
        }

        void OnDisable()
        {
            if (_boot.Data.ModsInfos.Find(x => x.Name == KeyName) != null)
            {
                _boot.RemoveModMarkAllPointsDirty(KeyName);
                lastAdditionalData = null;
            }
        }

        public List<int> RecalculateTriangles(LRData data, ModInfo currentMod, int startVerticeIndex, int startTriangleIndex, int segmentIndex, List<SegmentInfo> segmentInfos)
        {
            /*var segment = segmentInfos[segmentIndex];
            bool isFirstSegment = segmentIndex == 0;
            bool hasCorner = false;
            if (!isFirstSegment)
            {
                var prevSegment = segmentInfos[segmentIndex - 1];
                hasCorner = ArePointsFormingCorner(
                    prevSegment.endSegmentCenter,
                    segment.startSegmentCenter,
                    segment.endSegmentCenter
                );
            }

            if (!hasCorner || segmentIndex == 0 || segmentIndex >= segmentInfos.Count && segmentInfos.Count >= data.Config.PointsCount)
                return new();

            return GetConnectionTraingles(segmentIndex, startVerticeIndex, data, segmentInfos);*/
            return currentMod.Triangles;
        }

        
        public AdditionalData lastAdditionalData;

        public ModInfo ManipulateMesh(LRData data, int startVerticeIndex, int startTriangleIndex, int segmentIndex, ref List<SegmentInfo> segmentInfos)
        {
            // It's not possible to have angle with less than 2 segments
            if (segmentInfos.Count < 2)
                return default;

            // Prepare lists to new LR
            helpControlPoints.Clear();
            connectionPoints.Clear();
            _pointsPerCurve = Mathf.Clamp(data.Config.NumberOfFaces, 2, data.Config.NumberOfFaces);

            ModInfo lrConnectionMod = data.ModsInfos.Find(x => x.Name == KeyName);
            lrConnectionMod ??= new();
            lrConnectionMod.ModsAdditionalData ??= new AdditionalData();
            ConnectionData connectionData = new();
            AdditionalData additionalData = (AdditionalData)lrConnectionMod.ModsAdditionalData;
            Dictionary<int, ConnectionData> connectionDatas = additionalData.connectionDatas;

            Debug.Log($"holding segmentID: {segmentIndex}");
            // Change current segments to make distance for corner
            MakeDistanceForCorner(segmentID: segmentIndex, data, ref segmentInfos, out bool hasCorner);

            if (!hasCorner || segmentIndex == 0 || segmentIndex >= segmentInfos.Count && segmentInfos.Count >= data.Config.PointsCount)
            {
                lastAdditionalData = additionalData;
                return default;
            }
            if (!connectionDatas.ContainsKey(segmentIndex))
                connectionDatas.Add(segmentIndex, connectionData);
            Debug.Log($"passed segmentID: {segmentIndex} connectionDataCount: {additionalData.connectionDatas.Count}");
            
            if (lrConnectionMod.ModsAdditionalData != null && segmentIndex > 1)
            {
                Debug.Log("replaced");  
                startVerticeIndex = connectionDatas[segmentIndex - 1].Triangles[^3] + 1;
            }
            

             CreateConnections(segmentIndex: segmentIndex, startVerticeIndex, ref connectionData, data, segmentInfos);


            additionalData.connectionDatas[segmentIndex] = connectionData;
            lastAdditionalData = additionalData;

            lrConnectionMod.Vertices.Clear();
            lrConnectionMod.Normals.Clear();
            lrConnectionMod.Uvs.Clear();
            lrConnectionMod.Triangles.Clear();
            foreach (var item in connectionDatas)
            {
                var conData = item.Value;
                lrConnectionMod.Vertices.AddRange(conData.Vertices);
                lrConnectionMod.Normals.AddRange(conData.Normals);
                lrConnectionMod.Uvs.AddRange(conData.Uvs);
                lrConnectionMod.Triangles.AddRange(conData.Triangles);
            }

            return lrConnectionMod;
        }

        /// <summary>
        /// Creates connections between segments by generating vertices, normals, UVs, and triangles.
        /// </summary>
        /// <param name="segmentIndex">The index of the current segment.</param>
        void CreateConnections(int segmentIndex, int startVerticeIndex, ref ConnectionData connectionData, LRData data, List<SegmentInfo> segmentInfos)
        {
            SegmentInfo currentSegment = segmentInfos[segmentIndex];
            SegmentInfo previousSegment = segmentInfos[segmentIndex - 1];

            int numberOfFaces = data.Config.NumberOfFaces;
            float radius = data.Config.Radius;

            // A => Previous Point, B => Current Point, C => Next Point
            Vector3 A, B, C;

            if (data.Config.PointsCount > segmentInfos.Count)
            {
                A = data.Config.GetPoint(segmentIndex - 1);
                B = data.Config.GetPoint(segmentIndex);
                C = data.Config.GetPoint(segmentIndex + 1);
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
                    float theta = PI2 * f / numberOfFaces;
                    Vector3 circleOffset = new(
                        Mathf.Cos(theta) * radius,
                        Mathf.Sin(theta) * radius,
                        0
                    );

                    // Vertexes
                    Vector3 vertexPos = centralPoint + rotation * circleOffset;
                    connectionData.Vertices.Add(transform.InverseTransformPoint(vertexPos));
                    connectionPoints.Add(vertexPos);
                    // Normals
                    connectionData.Normals.Add((vertexPos - centralPoint).normalized);

                    // UV mapping
                    if (f > numberOfFaces / 2)
                        connectionData.Uvs.Add(new Vector2(2f - ((float)f / numberOfFaces) * 2f, 1 - t));
                    else
                        connectionData.Uvs.Add(new Vector2(((float)f / numberOfFaces) * 2f, 1 - t));
                }
                
            }

            // Generate triangles between segments
            connectionData.Triangles.AddRange(GetConnectionTraingles(segmentIndex, startVerticeIndex, data, segmentInfos));
        }

        [System.Serializable]
        internal class AdditionalData 
        {
            [SerializeField]
            internal Dictionary<int, ConnectionData> connectionDatas = new();
        }

        [System.Serializable]
        internal class ConnectionData
        {
            [SerializeField]
            internal List<Vector3> Vertices = new();
            [SerializeField]
            internal List<Vector3> Normals = new();
            [SerializeField]
            internal List<Vector2> Uvs = new();
            [SerializeField]
            internal List<int> Triangles = new();
        }


        List<int> GetConnectionTraingles(int segmentIndex, int startVerticeIndex, LRData data, List<SegmentInfo> segmentInfos)
        {
            List<int> triangles = new();
            int numberOfFaces = data.Config.NumberOfFaces;
            // Generate triangles between segments
            for (int i = 0; i < numberOfFaces; i++)
            {
                int nextI = (i + 1) % numberOfFaces;


                for (int p = 0; p < _pointsPerCurve; p++)
                {
                    int currentP = p;
                    int nextP = p + 1;

                    // Current ring indices
                    int currentA = GetRingIndex(startVerticeIndex, numberOfFaces, i, currentP);
                    int currentB = GetRingIndex(startVerticeIndex, numberOfFaces, nextI, currentP);

                    // Next ring indices
                    int nextA = GetRingIndex(startVerticeIndex, numberOfFaces, i, nextP);
                    int nextB = GetRingIndex(startVerticeIndex, numberOfFaces, nextI, nextP);
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
            return triangles;
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
        void MakeDistanceForCorner(int segmentID, LRData data, ref List<SegmentInfo> segmentInfos, out bool hasCorner)
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
                    segment.startSegmentCenter,
                    segment.endSegmentCenter
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
                        data.TranslateVertex(index, startTranslation);
                }
                Debug.Log($"segment: {segmentID} has corner on start");
                hasCorner = true;
            }

            if (endHasCorner)
            {
                if(totalAdjustment > 0)
                {
                    segment.endSegmentCenter += endTranslation;
                    foreach (int index in segment.endSegmentVericesIndex)
                        data.TranslateVertex(index, endTranslation);
                }
            }
        }

        /// <summary>
        /// Determines if three points form a corner based on the cross product of their vectors.
        /// </summary>
        bool ArePointsFormingCorner(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 ab = b - a;
            Vector3 bc = c - b;

            Vector3 crossProduct = Vector3.Cross(ab.normalized, bc.normalized);
            return crossProduct.sqrMagnitude >= 0;
        }

        /// <summary>
        /// Gets the index of a vertex in a ring of vertices, used for generating triangles.
        /// </summary>
        int GetRingIndex(int initialVertices, int faces, int faceIndex, int ring)
        {
            // New connection vertices: initialVertices + (ring)*faces + faceIndex
            return initialVertices + (ring) * faces + faceIndex;
        }

        /// <param name="t">Interpolation parameter (0 to 1).</param>
        /// <returns>The point on the Bezier curve.</returns>
        Vector3 QuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            t = clamp(t, 0, 1);
            float u = 1 - t;
            return pow(u,2)* p0 + (2 * u * t) * p1 + (t * t) * p2;
        }

        /// <param name="t">Interpolation parameter (0 to 1).</param>
        /// <returns>The derivative of the Bezier curve.</returns>
        Vector3 QuadraticBezierDerivative(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            t = clamp(t, 0, 1);
            return 2 * (1 - t) * (p1 - p0) + 2 * t * (p2 - p1);
        }

        
    }
}