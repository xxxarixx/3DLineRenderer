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
        List<Vector3> ringEdgeVertexes;
        LRData data;

        [SerializeField]
        bool showGizmos;

        [SerializeField]
        float gizmosSize = 0.01f;
        
        [SerializeField] bool begginingCap = true;
        [SerializeField] bool endCap = true;
        public string Name => ToString();
        public bool IsEnabled => enabled;

        public void ManipulateMesh(LRData data, ref List<SegmentInfo> segmentInfos,
            ref List<Vector3> vertices, ref List<Vector3> normals, ref List<Vector2> uvs, ref List<int> triangles)
        {
            if (segmentInfos.Count < 1) 
                return;

            RoundCap(data, ref segmentInfos, ref vertices, ref triangles, ref uvs, ref normals);
            
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
                ringEdgeVertexes = new();
                this.data = data;
            }
            int numberOfFaces = data.Config.NumberOfFaces;
            float radius = data.Config.Radius;
            int baseVertexCount = vertices.Count;
            Vector3 center = isStart ? segment.startSegmentCenter : segment.endSegmentCenter;
            center = data.LrTransform.InverseTransformPoint(center);
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
                    uvs.Add(correctionRotation * new Vector2((float)s / _segments, isStart? 1f - (float)ring / _rings : (float)ring / _rings));

                    
                    // second half of vertices
                    if (ring != 0 && ring != _rings && s == _segments)
                    {
                        ringEdgeVertexes.Add(vertex);
                    }
                    // first half of vertices
                    if (s == 0)
                    {
                        ringEdgeVertexes.Add(vertex);
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

        void OnDrawGizmos()
        {
            if (data == null || !enabled || !showGizmos)
                return;
            for (int i = 0; i < ringEdgeVertexes.Count; i++)
            {
                Vector3 item = ringEdgeVertexes[i];
                item = data.LrTransform.TransformPoint(item);
                Gizmos.color = Color.black;
#if UNITY_EDITOR
                Handles.Label(item + new Vector3(gizmosSize, 0f, 0f), $"{i}");
#endif
                Gizmos.DrawSphere(item, gizmosSize);
            }
        }
    }
}