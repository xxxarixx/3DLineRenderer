using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using static LineRenderer3D.Datas.LRData;
using static Unity.Mathematics.math;
using System;
using LineRenderer3D.Datas;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LineRenderer3D.Mods
{
    /// <summary>
    /// A modifier class for capping the ends of the line renderer with different shapes.
    /// </summary>
    class LRCap : MonoBehaviour, ILRModBase
    {
        [SerializeField]
        bool showGizmos;

        [SerializeField]
        float gizmosSize = 0.01f;
        
        [SerializeField]
        CapTypes capType;

        [Header(nameof(CapTypes.round))]
        List<int> ringEdgeIndexes;
        LRData data;


        [Header(nameof(CapTypes.spike))]
        [SerializeField]
        float capSize;

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

        public void ManipulateMesh(LRData data, ref List<SegmentInfo> segmentInfos,
            ref List<Vector3> vertices, ref List<Vector3> normals, ref List<Vector2> uvs, ref List<int> triangles)
        {
            if (segmentInfos.Count < 1 || segmentSplit == 0) return;

            switch (capType)
            {
                case CapTypes.spike:
                    SpikeCap(data, ref segmentInfos, ref vertices);
                    break;
                case CapTypes.round:
                    RoundCap(data, ref segmentInfos, ref vertices, ref triangles, ref uvs, ref normals);
                    break;
                default:
                    break;
            }
            
        }

        void RoundCap(LRData data, ref List<SegmentInfo> segmentInfos, ref List<Vector3> vertices, ref List<int> triangles, ref List<Vector2> uvs, ref List<Vector3> normals)
        {
            if (begginingCap)
                GenerateSphereCap(data, segmentInfos[0], ref vertices, ref triangles, ref uvs, ref normals, isStart: true);

            if (endCap)
                GenerateSphereCap(data, segmentInfos[^1], ref vertices, ref triangles, ref uvs, ref normals, isStart: false);
        }

        void GenerateSphereCap(LRData data, SegmentInfo segment, ref List<Vector3> vertices, ref List<int> triangles, ref List<Vector2> uvs, ref List<Vector3> normals, bool isStart)
        {
            if(isStart || begginingCap != endCap)
            {
                ringEdgeIndexes = new();
                this.data = data;
            }
            int numberOfFaces = data.NumberOfFaces;
            float radius = data.Radius;
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

        void SpikeCap(LRData data, ref List<SegmentInfo> segmentInfos, ref List<Vector3> vertices)
        {
            if (begginingCap)
            {
                Vector3 dir = normalize(segmentInfos[0].startSegmentCenter - segmentInfos[0].endSegmentCenter);
                Vector3 endPosition = segmentInfos[0].startSegmentCenter + dir * capSize;
                data.GenerateCylinder(segmentInfos[0].startSegmentCenter, endPosition, segmentInfos.Count, flipUV: false);
                var segmentInfo = data.GenerateSegmentInfo(segmentInfos[0].startSegmentCenter, endPosition, segmentInfos.Count);
                segmentInfos.Insert(0, segmentInfo);

                ProcessSegments(segmentInfos[0].uniqueId, isItBeggining:true, data, ref segmentInfos, ref vertices);
                ApplayCurveOnCap(0, ref segmentInfos, ref vertices, beginningCurve, (int)pow(2, segmentSplit) + 1, reverseCurve: false, isItEnd: false);
            }

            if (endCap)
            {
                ProcessSegments(segmentInfos[^1].uniqueId, isItBeggining:false, data, ref segmentInfos, ref vertices);
                int numberOfIndexesToAffect = (int)pow(2, segmentSplit) - 1;
                int startIndex = segmentInfos.Count - numberOfIndexesToAffect;
                ApplayCurveOnCap(startIndex, ref segmentInfos, ref vertices, endCurve, numberOfIndexesToAffect, reverseCurve: true, isItEnd: true);
            }
        }

        void ApplayCurveOnCap(int startingIndex,ref List<SegmentInfo> segmentInfos, ref List<Vector3> vertices, AnimationCurve curve, int numberOfIndexesToAffect, bool reverseCurve, bool isItEnd)
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

        void ProcessSegments(string startingUniqueId, bool isItBeggining, LRData data, ref List<SegmentInfo> segmentInfos, ref List<Vector3> vertices)
        {
            ids.Clear();
            int splitIndex = 0;

            ids.Add(startingUniqueId);

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
                    newIds.AddRange(SplitSegment(vertices: ref vertices,
                                                 idToSplit: id, 
                                                 data, 
                                                 segmentInfos: ref segmentInfos, 
                                                 isItBeggining));

                    ids.RemoveAt(0);
                }
            }
        }

        string[] SplitSegment(ref List<Vector3> vertices, string idToSplit, LRData data, ref List<SegmentInfo> segmentInfos, bool isItBeggining)
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
            data.GenerateCylinder(start: halfWayCenter,
                                  end: oldEndSegmentCenter,
                                  segmentInfos.Count,
                                  flipUV: false);
            var newSegment = data.GenerateSegmentInfo(start: halfWayCenter,
                                                    end: oldEndSegmentCenter,
                                                    cylinderIndex: segmentInfos.Count);
            segmentInfos.Insert(isItBeggining?segmentIndex : segmentIndex + 1, newSegment);
            newIds[0] = segment.uniqueId;
            newIds[1] = newSegment.uniqueId;
            return newIds;
        }

        void OnDrawGizmos()
        {
            if (data == null || !enabled || !showGizmos)
                return;
            switch (capType)
            {
                case CapTypes.spike:
                    break;
                case CapTypes.round:
                    for (int i = 0; i < ringEdgeIndexes.Count; i++)
                    {
                        Vector3 item = data.GetVertex(ringEdgeIndexes[i]);
                        Gizmos.color = Color.black;
#if UNITY_EDITOR
                        Handles.Label(item + new Vector3(gizmosSize, 0f,0f), $"{i}");
#endif
                        Gizmos.DrawSphere(item, gizmosSize);
                    }
                    break;
                default:
                    break;
            }
        }
    }
}