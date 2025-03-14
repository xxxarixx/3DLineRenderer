using UnityEngine;
using System.Collections.Generic;
using LineRenderer3D.Datas;
using static LineRenderer3D.Datas.LRData;

namespace LineRenderer3D
{
    public class LineRenderer3DExtenction : MonoBehaviour
    {
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
            segmentInfos.Insert(isItBeggining ? segmentIndex : segmentIndex + 1, newSegment);
            newIds[0] = segment.uniqueId;
            newIds[1] = newSegment.uniqueId;
            return newIds;
        }
    }
}
