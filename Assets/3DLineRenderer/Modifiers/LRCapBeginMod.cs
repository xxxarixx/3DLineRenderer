using System.Collections.Generic;
using UnityEngine;
using static LineRenderer3D.Datas.LRData;
using static Unity.Mathematics.math;
using LineRenderer3D.Datas;
using LinerRenderer3D.Datas;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LineRenderer3D.Mods
{
    /// <summary>
    /// A modifier class for capping the ends of the line renderer with different shapes.
    /// </summary>
    [ExecuteAlways]
    class LRCapBeginMod : MonoBehaviour, ILRModBase
    {
        readonly List<Vector3> ringEdgeVertexes;
        LRData Data => _boot.Data;
        LRConfig Config => _boot.Data.Config;

        [SerializeField]
        bool showGizmos;

        [SerializeField]
        float gizmosSize = 0.01f;
       
        public string KeyName => nameof(LRCapBeginMod);
        public bool IsEnabled => enabled;

        public ModInfo ManipulateMesh(LRData data, int startVerticeIndex, int startTriangleIndex, int segmentIndex, ref List<SegmentInfo> segmentInfos)
        {
            // TODO: There is bug when one cap is generated second one cannot, it's due to possibility to only have one ModInfo per mod and values aren't added but cleared and then added.
            if (segmentInfos.Count < 1)
                return default;
            ModInfo generatedModInfo = default;

            if (segmentIndex == 0)
            {
                generatedModInfo = LineRenderer3DExtenction.GenerateSphereCap(data, startVerticeIndex, segmentInfos[segmentIndex], isStart: true);
            }
            return generatedModInfo;
        }

        public List<int> RecalculateTriangles(LRData data, ModInfo currentMod, int startVerticeIndex, int startTriangleIndex, int segmentIndex, List<SegmentInfo> segmentInfos)
        {
            Debug.Log($"RecalculateTriangles start start vertice: {startVerticeIndex}");
            return LineRenderer3DExtenction.GenerateSphereCapTringles(data, startVerticeIndex);
        }

        LRBoot _boot;

        void OnEnable()
        {
            if (_boot == null)
                _boot = GetComponent<LRBoot>();
            if(_boot.Data.ModsInfos.Find(x => x.Name == KeyName) == null)
                _boot.AddMod(KeyName, 0);
        }

        void OnDisable()
        {
            if (_boot.Data.ModsInfos.Find(x => x.Name == KeyName) != null)
                _boot.RemoveMod(KeyName, 0);
        }

        void OnDrawGizmos()
        {
            if (!enabled || Data == null || !showGizmos)
                return;
            for (int i = 0; i < ringEdgeVertexes.Count; i++)
            {
                Vector3 item = ringEdgeVertexes[i];
                item = Data.LrTransform.TransformPoint(item);
                Gizmos.color = Color.black;
#if UNITY_EDITOR
                Handles.Label(item + new Vector3(gizmosSize, 0f, 0f), $"{i}");
#endif
                Gizmos.DrawSphere(item, gizmosSize);
            }
        }
    }
}