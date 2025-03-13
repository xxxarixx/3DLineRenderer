using System.Collections.Generic;
using UnityEngine;
using LineRenderer3D.Datas;

namespace LineRenderer3D.Mods
{
    /// <summary>
    /// The base interface for all line renderer modifiers.
    /// </summary>
    public interface ILRModBase
    {
        string Name { get; }

        bool IsEnabled { get; }

        void ManipulateMesh(LRData data, ref List<LRData.SegmentInfo> segmentInfos, ref List<Vector3> vertices, ref List<Vector3> normals, ref List<Vector2> uvs, ref List<int> triangles);
    }
}
