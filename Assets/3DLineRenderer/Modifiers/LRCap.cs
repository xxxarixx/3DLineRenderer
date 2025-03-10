using System.Collections.Generic;
using UnityEngine;
using static LineRenderer3D.LRCylinder3D;
using System.Linq;
using Unity.Mathematics;

namespace LineRenderer3D.Modifiers
{
    class LRCap : MonoBehaviour, IModifierBase
    {

        [SerializeField]
        CapTypes capType;

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
                    RoundCap(lr, ref segmentInfos, ref vertices);
                    break;
                default:
                    break;
            }
            
        }
        void RoundCap(LRCylinder3D lr, ref List<SegmentInfo> segmentInfos, ref List<Vector3> vertices)
        {
            // here do this sphere on the end of last segment and start of first segment

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
    }
}