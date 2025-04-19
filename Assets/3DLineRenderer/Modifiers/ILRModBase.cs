using System.Collections.Generic;
using LineRenderer3D.Datas;

namespace LineRenderer3D.Mods
{
    /// <summary>
    /// The base interface for all line renderer modifiers.
    /// </summary>
    public interface ILRModBase
    {
        string KeyName { get; }

        bool IsEnabled { get; }

        LRData.ModInfo ManipulateMesh(LRData data, int startVerticeIndex, int startTriangleIndex, int segmentIndex, ref List<LRData.SegmentInfo> segmentInfos);

        List<int> RecalculateTriangles(LRData data, LRData.ModInfo currentMod, int startVerticeIndex, int startTriangleIndex, int segmentIndex, List<LRData.SegmentInfo> segmentInfos);
    }
}
