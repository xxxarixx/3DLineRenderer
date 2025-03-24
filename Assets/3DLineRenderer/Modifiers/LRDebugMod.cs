using LineRenderer3D.Datas;
using LineRenderer3D.Mods;
using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// A modifier class for debugging line renderer data by visualizing various aspects of the mesh.
/// </summary>
public class LRDebugMod : MonoBehaviour, ILRModBase
{
    public string Name => name;

    public bool IsEnabled => enabled;

    [SerializeField]
    List<DebugGizmos> _pointsDebug = new();

    [SerializeField]
    bool _visualizeAllVertices;

    [SerializeField]
    bool _visualizePointsPositions;

    [SerializeField]
    float _vertexGizmosSize = 0.01f;

    [Flags]
    enum DebugGizmos
    {
        None = 0,
        Vertices = 1,
        Normals = 0b10,
        SegmentsInfo = 0b100,
        UV = 0b1000,
        Directions = 0b10000
    }

    public void ManipulateMesh(LRData data, int segmentIndex, ref List<LRData.SegmentInfo> segmentInfos)
    {
        _data = data;
        _segmentInfos = segmentInfos;
        _vertices = new();
        _normals = new();
        _uvs = new();
        _triangles = new();
        foreach (var segment in _segmentInfos)
        {
            _vertices.AddRange(segment.vertices);
            _normals.AddRange(segment.normals);
            _uvs.AddRange(segment.uvs);
            _triangles.AddRange(segment.triangles);
        }
    }

    // Copied veriables from modifier to visualize them in gizmos.
    [SerializeField]
    List<LRData.SegmentInfo> _segmentInfos;

    [SerializeField]
    List<Vector3> _vertices;

    [SerializeField]
    List<Vector3> _normals;

    [SerializeField]
    List<Vector2> _uvs;

    [SerializeField]
    List<int> _triangles;
    LRData _data;

    void OnDrawGizmos()
    {
        if (!IsEnabled)
            return;

        if (_visualizePointsPositions)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < _data.Config.PointsCount; i++)
                Gizmos.DrawWireSphere(_data.Config.GetPoint(i), .1f);     
        }

        DebugGizmoses();
    }

    /// <summary>
    /// Visualizes various debug gizmos based on the selected options.
    /// </summary>
    void DebugGizmoses()
    {
        Gizmos.color = Color.red;
        if (_visualizeAllVertices)
            foreach (var vertice in _vertices)
                Gizmos.DrawSphere(transform.TransformPoint(vertice), _vertexGizmosSize);

        for (int p = 0; p < _pointsDebug.Count; p++)
        {
            var debugGizmos = _pointsDebug[p];
            if (debugGizmos == DebugGizmos.None)
                continue;
            if (!_data.IsCylinderIndexValid(p))
                continue;
            // Options of visualization
            var segmentInfo = _segmentInfos[p];
            bool visualizeVertices = (debugGizmos & DebugGizmos.Vertices) != 0;
            bool visualizeNormals = (debugGizmos & DebugGizmos.Normals) != 0;
            bool visualizeSegmentsInfo = (debugGizmos & DebugGizmos.SegmentsInfo) != 0;
            bool visualizeUV = (debugGizmos & DebugGizmos.UV) != 0;
            bool visualizeDirection = (debugGizmos & DebugGizmos.Directions) != 0;

            // Visualize normals
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

            // Visualize vertices
            if (visualizeVertices)
            {
                Gizmos.color = Color.red;
                for (int i = 0; i < segmentInfo.startSegmentVericesIndex.Count; i++)
                {
                    var index = segmentInfo.startSegmentVericesIndex[i];
                    Vector3 pos = transform.TransformPoint(_vertices[index]);
                    Gizmos.DrawSphere(pos, _vertexGizmosSize);
                }
                for (int i = 0; i < segmentInfo.endSegmentVericesIndex.Count; i++)
                {
                    var index = segmentInfo.endSegmentVericesIndex[i];
                    Vector3 pos = transform.TransformPoint(_vertices[index]);
                    Gizmos.DrawSphere(pos, _vertexGizmosSize);
                }
            }

            // Visualize segments info
            if (visualizeSegmentsInfo)
            {
                // Visualize start segment vertices and center
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere((segmentInfo.startSegmentCenter), _vertexGizmosSize);
                for (int i = 0; i < segmentInfo.startSegmentVericesIndex.Count; i++)
                    if (segmentInfo.startSegmentVericesIndex[i] < _vertices.Count)
                    {
                        Gizmos.DrawWireSphere(transform.TransformPoint(_vertices[segmentInfo.startSegmentVericesIndex[i]]), _vertexGizmosSize / 2);
#if UNITY_EDITOR
                            Handles.Label(
                                position:transform.TransformPoint(_vertices[segmentInfo.startSegmentVericesIndex[i]]) + new Vector3(_vertexGizmosSize / 2, 0,0), 
                                text:$"{segmentInfo.startSegmentVericesIndex[i]}");
#endif
                    }

                // Visualize end segment vertices and center
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere((segmentInfo.endSegmentCenter), _vertexGizmosSize + _vertexGizmosSize / 2);
                for (int i = 0; i < segmentInfo.endSegmentVericesIndex.Count; i++)
                    if (segmentInfo.endSegmentVericesIndex[i] < _vertices.Count)
                    {
                        Gizmos.DrawWireSphere(transform.TransformPoint(_vertices[segmentInfo.endSegmentVericesIndex[i]]), _vertexGizmosSize);
#if UNITY_EDITOR
                            Handles.Label(
                                position: transform.TransformPoint(_vertices[segmentInfo.endSegmentVericesIndex[i]]) + new Vector3(_vertexGizmosSize, _vertexGizmosSize, 0),
                                text: $"{segmentInfo.endSegmentVericesIndex[i]}");
#endif
                    }

                // Visualize initial start and end segment center
                Gizmos.color = Color.blue;
                Gizmos.DrawCube((segmentInfo.initStartSegmentCenter), Vector3.one * _vertexGizmosSize);
                Gizmos.color = Color.white;
                Gizmos.DrawCube((segmentInfo.initEndSegmentCenter), Vector3.one * _vertexGizmosSize);
            }

            if (visualizeDirection && _segmentInfos != null)
            {
                foreach (var segment in _segmentInfos)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawRay(segment.startSegmentCenter, segment.initStartSegmentCenter - segment.startSegmentCenter);
                    Gizmos.color = Color.blue;
                    Gizmos.DrawRay(segment.endSegmentCenter, segment.initEndSegmentCenter - segment.endSegmentCenter);
                }
            }

            // Visualize Uvs
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
