using LineRenderer3D.Datas;
using LineRenderer3D.Mods;
using System.Collections.Generic;
using UnityEngine;
using static LineRenderer3D.Datas.LRData;

namespace LineRenderer3D
{
    class LRSegmentSpliterModifier : MonoBehaviour, ILRModBase
    {
        [SerializeField]
        [Range(0.5f, 1.3f)]
        float textureSize = 2f;

        [SerializeField]
        float gizmosSize = .1f;

        [SerializeField]
        bool showGizmos;
        public string Name => ToString();

        public bool IsEnabled => enabled;

        List<Vector3> splitedCenter = new();
        List<Vector3> splitedCircle = new();

        

        public void ManipulateMesh(LRData data, ref List<SegmentInfo> segmentInfos, ref List<Vector3> vertices, ref List<Vector3> normals, ref List<Vector2> uvs, ref List<int> triangles)
        {
            textureSize = Mathf.Clamp(textureSize, 0.1f, 10f);
            splitedCenter = new();
            splitedCircle = new();

            int numberOfFaces = data.NumberOfFaces;
            int cylinderIndex = data.Points.Count - 1;

            bool flipUV = true;

            for (int i = 0; i < segmentInfos.Count; i++)
            {
                SegmentInfo segment = segmentInfos[i];
                float distance = Vector3.Distance(segment.startSegmentCenter, segment.endSegmentCenter);
                if(distance > textureSize)
                {
                    // Split into half
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
                    var segmentInfo = data.GenerateSegmentInfo(start: transform.InverseTransformPoint(startHalfwayCenter), 
                                                             end: transform.InverseTransformPoint(endCenter), 
                                                             cylinderIndex: cylinderIndex);
                    segmentInfos.Insert(i + 1,segmentInfo);
                    data.GenerateCylinder(start:transform.InverseTransformPoint(startHalfwayCenter), 
                                          end:transform.InverseTransformPoint(endCenter), 
                                          cylinderIndex: cylinderIndex,
                                          flipUV: flipUV = !flipUV);
                    cylinderIndex++;
                    i--;
                }
            }

        }

        void OnDrawGizmos()
        {
            if (!IsEnabled || !showGizmos)
                return;


            Gizmos.color = Color.yellow;
            foreach (var item in splitedCenter)
                Gizmos.DrawWireSphere(item, gizmosSize);

            Gizmos.color = Color.magenta;
            foreach (var item in splitedCircle)
                Gizmos.DrawWireSphere(transform.InverseTransformPoint(item), gizmosSize / 4f);
        }
    }
}
