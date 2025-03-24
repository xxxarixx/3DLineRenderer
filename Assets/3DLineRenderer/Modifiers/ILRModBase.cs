using System.Collections.Generic;
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

        void ManipulateMesh(LRData data, int segmentIndex, ref List<LRData.SegmentInfo> segmentInfos);
    }
}
