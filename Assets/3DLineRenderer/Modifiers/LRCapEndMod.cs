using System.Collections.Generic;
using UnityEngine;
using static LineRenderer3D.Datas.LRData;
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
    class LRCapEndMod : MonoBehaviour, ILRModBase
    {
        List<Vector3> ringEdgeVertexes;
        LRData Data => _boot.Data;
        LRConfig Config => _boot.Data.Config;

        [SerializeField]
        bool showGizmos;

        [SerializeField]
        float gizmosSize = 0.01f;
        public string KeyName => nameof(LRCapEndMod);
        public bool IsEnabled => enabled;

        public ModInfo ManipulateMesh(LRData data, int startVerticeIndex, int startTriangleIndex, int segmentIndex, ref List<SegmentInfo> segmentInfos)
        {
            if (segmentInfos.Count < 1) 
                return default;

            ModInfo generatedModInfo = default;
            if (segmentIndex == segmentInfos.Count - 1)
            {
                generatedModInfo = LineRenderer3DExtenction.GenerateSphereCap(data, startVerticeIndex, segmentInfos[segmentIndex], isStart: false);
            }
            return generatedModInfo;
        }

        public List<int> RecalculateTriangles(LRData data, ModInfo currentMod, int startVerticeIndex, int startTriangleIndex, int segmentIndex, List<SegmentInfo> segmentInfos)
        {
            Debug.Log($"RecalculateTriangles end start vertice: {startVerticeIndex}");
            return LineRenderer3DExtenction.GenerateSphereCapTringles(data, startVerticeIndex);
        }

        LRBoot _boot;

        void OnEnable()
        {
            if (_boot == null)
                _boot = GetComponent<LRBoot>();
            _boot.EnableMod(KeyName, shouldMarkPointsDirty: true);
        }

        void OnDisable()
        {
            _boot.DisableMod(KeyName, shouldMarkPointsDirty: true);
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