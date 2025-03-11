using System.Collections.Generic;
using UnityEngine;
using static LineRenderer3D.LRCylinder3D;
using System.Linq;
using Unity.Mathematics;
using static Unity.Mathematics.math;

using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LineRenderer3D.Modifiers
{
    class LRCap : MonoBehaviour, IModifierBase
    {

        [SerializeField]
        CapTypes capType;

        [SerializeField]
        float gizmosSize = 0.01f;

        [Header(nameof(CapTypes.spike))]
        [SerializeField]
        AnimationCurve beginningCurve;

        [SerializeField]
        AnimationCurve endCurve;

        [SerializeField][Range(0, 5)] int segmentSplit = 3;
        [SerializeField] bool begginingCap = true;
        [SerializeField] bool endCap = true;
        public string Name => ToString();
        public bool IsEnabled => enabled;
        [SerializeField]
        List<string> ids = new();
        enum CapTypes
        {
            spike,
            round
        }
        public void ManipulateMesh(LRCylinder3D lr, ref List<SegmentInfo> segmentInfos,
            ref List<Vector3> vertices, ref List<Vector3> normals, ref List<Vector2> uvs, ref List<int> triangles)
        {
            if (segmentInfos.Count < 1 || segmentSplit == 0) return;

            switch (capType)
            {
                case CapTypes.spike:
                    SpikeCap(lr, ref segmentInfos, ref vertices);
                    break;
                case CapTypes.round:
                    RoundCap(lr, ref segmentInfos, ref vertices, ref triangles, ref uvs, ref normals);
                    break;
                default:
                    break;
            }
            
        }
        void RoundCap(LRCylinder3D lr, ref List<SegmentInfo> segmentInfos, ref List<Vector3> vertices, ref List<int> triangles, ref List<Vector2> uvs, ref List<Vector3> normals)
        {
            if (begginingCap)
                GenerateSphereCap(lr, segmentInfos[0], ref vertices, ref triangles, ref uvs, ref normals, isStart: true);

            if (endCap)
                GenerateSphereCap(lr, segmentInfos[^1], ref vertices, ref triangles, ref uvs, ref normals, isStart: false);
        }

        [SerializeField]
        List<Vector3> debugCapEdgeVertices = new();

        void GenerateSphereCap(LRCylinder3D lr, SegmentInfo segment, ref List<Vector3> vertices, ref List<int> triangles, ref List<Vector2> uvs, ref List<Vector3> normals, bool isStart)
        {
            if(isStart || begginingCap != endCap)
                debugCapEdgeVertices = new();
            List<int> ringEdgeIndexes = new();
            int numberOfFaces = lr.numberOfFaces;
            float radius = lr.radius;
            int baseVertexCount = vertices.Count;
            Vector3 center = isStart ? segment.startSegmentCenter : segment.endSegmentCenter;
            List<int> baseIndexes = isStart ? segment.startSegmentVericesIndex : segment.endSegmentVericesIndex;
            Vector3 direction = isStart ? (segment.startSegmentCenter - segment.endSegmentCenter).normalized :
                                         (segment.endSegmentCenter - segment.startSegmentCenter).normalized;

            Quaternion correctionRotation = Quaternion.Euler(0, 0, isStart ? 90f : -90f);
            Quaternion rotation = Quaternion.LookRotation(direction) * correctionRotation;

            

            
            int _rings = numberOfFaces / 2;
            int _segments = numberOfFaces / 2;

            float deltaTheta = (Mathf.PI) / _rings;
            float deltaPhi = (Mathf.PI) / _segments;

            for (int ring = 0; ring <= _rings; ring++)
            {
                float theta = ring * deltaTheta;
                float sinTheta = Mathf.Sin(theta);
                float cosTheta = Mathf.Cos(theta);

                for (int s = 0; s <= _segments; s++)
                {
                    float phi = s * deltaPhi;
                    float sinPhi = Mathf.Sin(phi);
                    float cosPhi = Mathf.Cos(phi);

                    float x = radius * sinTheta * cosPhi;
                    float y = radius * cosTheta;
                    float z = radius * sinTheta * sinPhi;

                    Vector3 vertex = center + rotation * new Vector3(x, y, z);
                    vertices.Add(vertex);
                    normals.Add(normalize(vertex - center));
                    uvs.Add(correctionRotation * new Vector2((float)s / _segments, (float)ring / _rings));

                    
                    // second half of vertices
                    if (ring != 0 && ring != _rings && s == _segments)
                    {
                        ringEdgeIndexes.Add(vertices.Count - 1);
                    }
                    // first half of vertices
                    if (s == 0)
                    {
                        ringEdgeIndexes.Add(vertices.Count - 1);
                    }
                }
            }

            foreach (var ringIndex in ringEdgeIndexes)
                debugCapEdgeVertices.Add(vertices[ringIndex]);

            // Triangles
            for (int ring = 0; ring < _rings; ring++)
            {
                for (int s = 0; s < _segments; s++)
                {
                    int current = baseVertexCount + ring * (_segments + 1) + s;
                    int next = current + _segments + 1;

                    triangles.Add(current);
                    triangles.Add(current + 1);
                    triangles.Add(next);

                    triangles.Add(next);
                    triangles.Add(current + 1);
                    triangles.Add(next + 1);
                }
            }
        }


        void GenerateHalfSphereCap(LRCylinder3D lr, SegmentInfo segment, ref List<Vector3> vertices, ref List<int> triangles, ref List<Vector2> uvs, ref List<Vector3> normals, bool isStart)
        {
            int numberOfFaces = lr.numberOfFaces;
            float radius = lr.radius;

            Vector3 center = isStart ? segment.startSegmentCenter : segment.endSegmentCenter;
            List<int> baseIndexes = isStart ? segment.startSegmentVericesIndex : segment.endSegmentVericesIndex;

            Vector3 direction = isStart ? (segment.startSegmentCenter - segment.endSegmentCenter).normalized :
                                          (segment.endSegmentCenter - segment.startSegmentCenter).normalized;
            Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, direction) * Quaternion.Euler(90, 0, 0);

            int baseVertexCount = vertices.Count;
            List<int> previousRing = new List<int>();
            List<int> currentRing = new List<int>();

            // Generate the first ring (base of the cap)
            for (int j = 0; j < numberOfFaces; j++)
            {
                previousRing.Add(baseIndexes[j]);
            }

            for (int i = 1; i <= numberOfFaces; i++)
            {
                float phi = (Mathf.PI / 2) * (i / (float)numberOfFaces);
                float y = Mathf.Cos(phi) * radius;
                float ringRadius = Mathf.Sin(phi) * radius;

                currentRing.Clear();
                for (int j = 0; j < numberOfFaces; j++)
                {
                    float theta = 2 * Mathf.PI * j / numberOfFaces;
                    Vector3 point = new Vector3(Mathf.Cos(theta) * ringRadius, y, Mathf.Sin(theta) * ringRadius);
                    Vector3 vertex = center + rotation * point;
                    vertices.Add(vertex);
                    normals.Add((vertex - center).normalized);
                    float v = isStart ? (1.0f - i / (float)numberOfFaces) : (i / (float)numberOfFaces);
                    uvs.Add(new Vector2((float)j / lr.numberOfFaces, v));
                    currentRing.Add(vertices.Count - 1);
                }

                // Connect previous ring to current ring
                for (int j = 0; j < lr.numberOfFaces; j++)
                {
                    int next = (j + 1) % lr.numberOfFaces;
                    triangles.Add(previousRing[j]);
                    triangles.Add(previousRing[next]);
                    triangles.Add(currentRing[j]);

                    triangles.Add(previousRing[next]);
                    triangles.Add(currentRing[next]);
                    triangles.Add(currentRing[j]);
                }
                previousRing = new List<int>(currentRing);
            }

            int topVertexIndex = vertices.Count;
            vertices.Add(center + rotation * new Vector3(0, lr.radius, 0));
            normals.Add(rotation * Vector3.up);
            uvs.Add(new Vector2(0.5f, isStart ? 0.0f : 1.0f));
        }


        void SpikeCap(LRCylinder3D lr, ref List<SegmentInfo> segmentInfos, ref List<Vector3> vertices)
        {
            if (begginingCap)
            {
                ProcessSegments(lr, ref segmentInfos, ref vertices, segmentInfos[0].uniqueId);
                SmoothCap(0, ref segmentInfos, ref vertices, beginningCurve, (int)math.pow(2, segmentSplit) + 1, reverseCurve: false, isItEnd: false);
            }

            if (endCap)
            {
                ProcessSegments(lr, ref segmentInfos, ref vertices, segmentInfos[^1].uniqueId);
                int numberOfIndexesToAffect = (int)math.pow(2, segmentSplit) - 1;
                int startIndex = segmentInfos.Count - numberOfIndexesToAffect;
                SmoothCap(startIndex, ref segmentInfos, ref vertices, endCurve, numberOfIndexesToAffect, reverseCurve: true, isItEnd: true);
            }
        }

        void SmoothCap(int startingIndex,ref List<SegmentInfo> segmentInfos, ref List<Vector3> vertices, AnimationCurve curve, int numberOfIndexesToAffect, bool reverseCurve, bool isItEnd)
        {
            for (int i = startingIndex; i < startingIndex + numberOfIndexesToAffect; i++)
            {
                var segmentInfo = segmentInfos[i];
                float t = (i - startingIndex) * (1f / (numberOfIndexesToAffect));
                if(reverseCurve)
                    t = 1 - t;
                foreach (var verticeIndex in segmentInfo.startSegmentVericesIndex)
                    vertices[verticeIndex] = Vector3.Lerp(segmentInfo.startSegmentCenter, vertices[verticeIndex], curve.Evaluate(t));
            }
            for (int i = startingIndex; i < startingIndex + numberOfIndexesToAffect - 1; i++)
            {
                var segmentInfo = segmentInfos[i];
                var nextSegmentInfo = segmentInfos[i + 1];
                for (int vi = 0; vi < segmentInfo.endSegmentVericesIndex.Count; vi++)
                {
                    int verticeIndex = segmentInfo.endSegmentVericesIndex[vi];
                    vertices[verticeIndex] = vertices[nextSegmentInfo.startSegmentVericesIndex[vi]];
                }
            }

            if(isItEnd)
            {
                var segmentInfo = segmentInfos[^1];
                for (int vi = 0; vi < segmentInfo.endSegmentVericesIndex.Count; vi++)
                    vertices[segmentInfo.endSegmentVericesIndex[vi]] = Vector3.Lerp(vertices[segmentInfo.endSegmentVericesIndex[vi]], segmentInfo.endSegmentCenter, curve.Evaluate(1));
            }
        }

        void ProcessSegments(LRCylinder3D lr, ref List<SegmentInfo> segmentInfos, ref List<Vector3> vertices, params string[] startingUniqueIds)
        {
            ids.Clear();
            int splitIndex = 0;

            foreach (var uniqueId in startingUniqueIds)
                ids.Add(uniqueId);

            List<string> newIds = new();
            while (splitIndex < segmentSplit)
            {
                if (ids.Count == 0)
                {
                    splitIndex++;
                    newIds = newIds.OrderByDescending(x => x).ToList();
                    ids.Clear();
                    ids.AddRange(newIds);
                    newIds.Clear();
                }
                else
                {
                    var id = ids[0];
                    newIds.AddRange(SplitSegment(id, lr, ref segmentInfos, ref vertices));
                    ids.RemoveAt(0);
                }
            }
        }

        string[] SplitSegment(string idToSplit, LRCylinder3D lr, ref List<SegmentInfo> segmentInfos, ref List<Vector3> vertices)
        {
            var newIds = new string[2];
            var segmentIndex = segmentInfos.FindIndex(x => x.uniqueId == idToSplit);
            var segment = segmentInfos[segmentIndex];
            Vector3 oldEndSegmentCenter = segment.endSegmentCenter;
            Vector3 halfWayCenter = Vector3.Lerp(segment.startSegmentCenter, segment.endSegmentCenter, 0.5f);
            segment.endSegmentCenter = halfWayCenter;
            for (int i = 0; i < segment.endSegmentVericesIndex.Count; i++)
            {
                Vector3 startVertice = vertices[segment.startSegmentVericesIndex[i]];
                Vector3 endVertice = vertices[segment.endSegmentVericesIndex[i]];
                Vector3 halfWay = Vector3.Lerp(startVertice, endVertice, 0.5f);
                vertices[segment.endSegmentVericesIndex[i]] = halfWay;
            }
            lr.GenerateCylinder(start: halfWayCenter,
            end: oldEndSegmentCenter,
                                segmentInfos.Count,
                                flipUV: false);
            var newSegment = lr.GenerateSegmentInfo(start: halfWayCenter,
                                                    end: oldEndSegmentCenter,
                                                    cylinderIndex: segmentInfos.Count);
            segmentInfos.Insert(segmentIndex + 1, newSegment);
            newIds[0] = segment.uniqueId;
            newIds[1] = newSegment.uniqueId;
            return newIds;
        }

        void OnDrawGizmos()
        {
            for (int i = 0; i < debugCapEdgeVertices.Count; i++)
            {
                Vector3 item = debugCapEdgeVertices[i];
                Gizmos.color = Color.black;
#if UNITY_EDITOR
                Handles.Label(item + new Vector3(gizmosSize, 0f,0f), $"{i}");
#endif
                Gizmos.DrawSphere(item, gizmosSize);
            }
        }
    }
}