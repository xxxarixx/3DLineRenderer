using LineRenderer3D.Datas;
using LineRenderer3D.Mods;
using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class LRDebugMod : MonoBehaviour, ILRModBase
{
    public string Name => name;

    public bool IsEnabled => enabled;

    [SerializeField]
    List<DebugGizmos> pointsDebug = new();

    [SerializeField]
    bool visualizeAllVertices;

    [SerializeField]
    bool visualizePointsPositions;

    [SerializeField]
    float vertexGizmosSize = 0.1f;

    [Flags]
    enum DebugGizmos
    {
        None = 0,
        Vertices = 1,
        Normals = 0b10,
        SegmentsInfo = 0b100,
        UV = 0b1000,
    }

    public void ManipulateMesh(LRData data, ref List<LRData.SegmentInfo> segmentInfos, ref List<Vector3> vertices, ref List<Vector3> normals, ref List<Vector2> uvs, ref List<int> triangles)
    {
        _data = data;
        _segmentInfos = segmentInfos;
        _vertices = vertices;
        _normals = normals;
        _uvs = uvs;
        _triangles = triangles;
    }

    List<LRData.SegmentInfo> _segmentInfos;
    List<Vector3> _vertices;
    List<Vector3> _normals;
    List<Vector2> _uvs;
    List<int> _triangles;
    LRData _data;

    void OnDrawGizmos()
    {
        if (!IsEnabled)
            return;

        if (visualizePointsPositions)
        {
            Gizmos.color = Color.green;
            foreach (Vector3 point in _data.Points)
                Gizmos.DrawWireSphere(point, .1f);
        }

        DebugGizmoses();
    }

    void DebugGizmoses()
    {
        Gizmos.color = Color.red;
        if (visualizeAllVertices)
            foreach (var vertice in _vertices)
                Gizmos.DrawSphere(transform.TransformPoint(vertice), vertexGizmosSize);

        for (int p = 0; p < pointsDebug.Count; p++)
        {
            var debugGizmos = pointsDebug[p];
            if (debugGizmos == DebugGizmos.None)
                continue;
            if (!_data.IsCylinderIndexValid(p))
                continue;
            var segmentInfo = _segmentInfos[p];
            bool visualizeVertices = (debugGizmos & DebugGizmos.Vertices) != 0;
            bool visualizeNormals = (debugGizmos & DebugGizmos.Normals) != 0;
            bool visualizeSegmentsInfo = (debugGizmos & DebugGizmos.SegmentsInfo) != 0;
            bool visualizeUV = (debugGizmos & DebugGizmos.UV) != 0;

            if (visualizeNormals)
            {
                Gizmos.color = Color.blue;
                for (int i = 0; i < _normals.Count; i++)
                {
                    Vector3 normal = _normals[i];
                    Vector3 verice = transform.TransformPoint(_vertices[i]);
                    Gizmos.DrawRay(verice, normal);
                }
            }
            Gizmos.color = Color.red;
            if (visualizeVertices)
            {
                for (int i = 0; i < segmentInfo.startSegmentVericesIndex.Count; i++)
                {
                    var index = segmentInfo.startSegmentVericesIndex[i];
                    Vector3 pos = transform.TransformPoint(_vertices[index]);
                    Gizmos.DrawSphere(pos, vertexGizmosSize);
                }
                for (int i = 0; i < segmentInfo.endSegmentVericesIndex.Count; i++)
                {
                    var index = segmentInfo.endSegmentVericesIndex[i];
                    Vector3 pos = transform.TransformPoint(_vertices[index]);
                    Gizmos.DrawSphere(pos, vertexGizmosSize);
                }
            }

            if (visualizeSegmentsInfo)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere((segmentInfo.startSegmentCenter), vertexGizmosSize);
                for (int i = 0; i < segmentInfo.startSegmentVericesIndex.Count; i++)
                    if (segmentInfo.startSegmentVericesIndex[i] < _vertices.Count)
                    {
                        Gizmos.DrawWireSphere(transform.TransformPoint(_vertices[segmentInfo.startSegmentVericesIndex[i]]), vertexGizmosSize / 2);
#if UNITY_EDITOR
                            Handles.Label(
                                position:transform.TransformPoint(_vertices[segmentInfo.startSegmentVericesIndex[i]]) + new Vector3(vertexGizmosSize / 2, 0,0), 
                                text:$"{segmentInfo.startSegmentVericesIndex[i]}");
#endif
                    }

                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere((segmentInfo.endSegmentCenter), vertexGizmosSize + vertexGizmosSize / 2);
                for (int i = 0; i < segmentInfo.endSegmentVericesIndex.Count; i++)
                    if (segmentInfo.endSegmentVericesIndex[i] < _vertices.Count)
                    {
                        Gizmos.DrawWireSphere(transform.TransformPoint(_vertices[segmentInfo.endSegmentVericesIndex[i]]), vertexGizmosSize);
#if UNITY_EDITOR
                            Handles.Label(
                                position: transform.TransformPoint(_vertices[segmentInfo.endSegmentVericesIndex[i]]) + new Vector3(vertexGizmosSize, vertexGizmosSize, 0),
                                text: $"{segmentInfo.endSegmentVericesIndex[i]}");
#endif
                    }

                Gizmos.color = Color.blue;
                Gizmos.DrawCube(transform.TransformPoint(segmentInfo.initStartSegmentCenter), Vector3.one * vertexGizmosSize);
                Gizmos.color = Color.white;
                Gizmos.DrawCube(transform.TransformPoint(segmentInfo.initEndSegmentCenter), Vector3.one * vertexGizmosSize);
            }
#if UNITY_EDITOR
                if (_uvs != null && visualizeUV)
                {
                    for (int i = 0; i < segmentInfo.startSegmentVericesIndex.Count; i++)
                    {
                        var index = segmentInfo.startSegmentVericesIndex[i];
                        Vector3 pos = transform.TransformPoint(_vertices[index]);
                        Handles.Label(pos, $"U: {_uvs[index].x:F2}\nV: {_uvs[index].y:F2}");
                    }
                    for (int i = 0; i < segmentInfo.endSegmentVericesIndex.Count; i++)
                    {
                        var index = segmentInfo.endSegmentVericesIndex[i];
                        Vector3 pos = transform.TransformPoint(_vertices[index]);
                        Handles.Label(pos, $"U: {_uvs[index].x:F2}\nV: {_uvs[index].y:F2}");
                    }
                }
#endif


        }

    }
}
