using LineRenderer3D.Modifiers;
using System.Collections.Generic;
using UnityEngine;

namespace LineRenderer3D
{
    class LRSegmentSpliterModifier : MonoBehaviour, IModifierBase
    {
        [SerializeField]
        float textureSize = 2f;
        [SerializeField]
        float gizmosSize = .1f;
        public string Name => ToString();

        public bool IsEnabled => enabled;

        List<Vector3> splitedCenter = new();
        List<Vector3> splitedCircle = new();

        [SerializeField]
        Vector3 offset;

        public void ManipulateMesh(LRCylinder3D lr, List<LRCylinder3D.SegmentInfo> segmentInfos, ref List<Vector3> vertices, ref List<Vector3> normals, ref List<Vector2> uvs, ref List<int> triangles)
        {
            splitedCenter = new();
            splitedCircle = new();
            int numberOfFaces = lr.numberOfFaces;
            int cylinderIndex = lr.points.Count - 1;
            for (int i = 0; i < segmentInfos.Count; i++)
            {
                LRCylinder3D.SegmentInfo segment = segmentInfos[i];
                float distance = Vector3.Distance(segment.startSegmentCenter, segment.endSegmentCenter);
                if(distance > textureSize)
                {
                    // Split into half
                    Debug.Log($"segment {i} should be splited cylinderIndex:{cylinderIndex}");
                    //LRCylinder3D.SegmentInfo segmentInfo = new();
                    Vector3 startHalfwayCenter = Vector3.Lerp(segment.startSegmentCenter, segment.endSegmentCenter, 0.5f);
                    Vector3 endCenter = segment.endSegmentCenter;
                    splitedCenter.Add(startHalfwayCenter);
                    for (int f = 0; f < numberOfFaces; f++)
                    {

                        // Change current segment vertices locations
                        Vector3 halfWayVertice = Vector3.Lerp(vertices[segment.startSegmentVericesIndex[f]], vertices[segment.endSegmentVericesIndex[f]], 0.5f);
                        vertices[segment.endSegmentVericesIndex[f]] = halfWayVertice;
                        segment.endSegmentCenter = startHalfwayCenter;
                        splitedCircle.Add(halfWayVertice);
                    }
                    lr.GenerateCylinder(start:transform.InverseTransformPoint(offset + startHalfwayCenter), 
                                        end:transform.InverseTransformPoint(offset + endCenter), 
                                        cylinderIndex:cylinderIndex,
                                        canMakeCorner:false);
                    var segmentInfo = lr.GenerateSegmentInfo(start: transform.InverseTransformPoint(offset + startHalfwayCenter), 
                                                             end: transform.InverseTransformPoint(offset + endCenter), 
                                                             cylinderIndex: cylinderIndex,
                                                             canMakeCorner: false);
                    segmentInfos.Add(segmentInfo);
                    cylinderIndex++;
                    i--;
                }
            }

        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            foreach (var item in splitedCenter)
                Gizmos.DrawWireSphere(item, gizmosSize);

            Gizmos.color = Color.magenta;
            foreach (var item in splitedCircle)
                Gizmos.DrawWireSphere(transform.InverseTransformPoint(item), gizmosSize / 4f);
        }
    }
}
