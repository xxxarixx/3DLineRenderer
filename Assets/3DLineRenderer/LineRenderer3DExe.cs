using UnityEngine;
using LineRenderer3D.Datas;
using LineRenderer3D.Mods;
using LinerRenderer3D.Datas;
using System.Linq;

namespace LineRenderer3D
{
    /// <summary>
    /// The main class that generates the mesh based on the current data and settings.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class LineRenderer3DExe : MonoBehaviour
    {
        public LRBoot boot;
        LRData Data => boot.Data;

        [Header("Debug")]
        [SerializeField]
        bool _regenerateBasedOnCurrentValues;

        [SerializeField]
        bool _stopRegeneration;

        Mesh _mesh;

        MeshFilter _meshFilter;

        [SerializeField]
        TextAsset pointsToLoad;

        [SerializeField]
        string savePath;


        void Awake()
        {
            GenerateMesh();
        }

        void Update()
        {
            if (Data.Config != null && Data.Config.DirtyPoints != null && Data.Config.DirtyPoints.Count > 0)
            {
                if(Data.Config.DirtyPoints.Count == Data.Config.PointsCount)
                    GenerateMesh();
                else
                    PartialMeshUpdate();
                Data.Config.ClearDirtyFlags();
            }
        }

        void PartialMeshUpdate()
        {
            Debug.Log("====Partial mesh update====");
            CheckMeshAssigment();
            Data.UpdateDirtyPoints();
            // Applay mods to LR
            int currentVertexCount = Data.GetSegmentVerticesCount();
            int currentTriangleCount = Data.GetSegmentTrianglesCount();
            foreach ((int, LRConfig.DirtyFlag) dirtyPoints in Data.Config.DirtyPoints)
                IntegrateMods(dirtyPoints, currentVertexCount, currentTriangleCount);


            Data.ApplayDataToMesh(ref _mesh);
            _meshFilter.sharedMesh = _mesh;
        }

        void IntegrateMods((int, LRConfig.DirtyFlag) dirtyPoint, int currentVertexCount, int currentTriangleCount)
        {
            int pointIndex = dirtyPoint.Item1;
            ILRModBase[] modsBase = GetComponents<ILRModBase>();
            for (int m = 0; m < Data.ModsInfos.Count; m++)
            {
                ILRModBase modBase = modsBase.First(x => x.KeyName == Data.ModsInfos[m].Name);
                string key = modBase.KeyName;
                if (!Data.ModsInfos[m].DirtyJustTriangles)
                {
                    LRData.ModInfo modInfo = modBase.ManipulateMesh(Data, currentVertexCount, currentTriangleCount, pointIndex, ref Data.SegmentInfos);
                    Debug.Log($"{key}, {pointIndex}, startVertices: {currentVertexCount}");

                    if (modInfo == null)
                    {
                        currentVertexCount += Data.ModsInfos[m].Vertices.Count;
                        continue;
                    }

                    currentVertexCount += modInfo.Vertices.Count;
                    modInfo.Name = key;
                    Data.ModsInfos[m] = modInfo;
                }
                else
                {
                    // Update triangles all above
                    for (int i = m; i < Data.ModsInfos.Count; i++)
                    {
                        var modInfo = Data.ModsInfos[i];
                        Data.ModsInfos[i].Triangles = modsBase.First(x => x.KeyName == modInfo.Name).RecalculateTriangles(Data, modInfo, currentVertexCount, currentTriangleCount, pointIndex, Data.SegmentInfos);
                    }
                    Data.ModsInfos[m].DirtyJustTriangles = false;
                }
            }
        }

        [ContextMenu(nameof(DebugMods))]
        void DebugMods() => Data.DebugMods();


        /// <summary>
        /// Checks and assigns the mesh and mesh filter components.
        /// </summary>
        void CheckMeshAssigment()
        {
            if (_mesh == null)
                _mesh = new()
                {
                    name = $"3DLineRenderer"
                };

            if (_meshFilter == null)
                _meshFilter = GetComponent<MeshFilter>();

            if (_meshFilter.sharedMesh == null)
                _meshFilter.sharedMesh = _mesh;
        }

        [ContextMenu(nameof(ClearPoints))]
        void ClearPoints()
        {
            Data.Config.ClearPoints();
            GenerateMesh();
        }

        [ContextMenu(nameof(GenerateMesh))]
        /// <summary>
        /// Generates the mesh based on the current data and settings.
        /// </summary>
        void GenerateMesh()
        {
            Debug.Log("full mesh update");
            if (Data.Config == null)
            {
                Debug.LogError($"There is no config, please set configuration files!");
                return;
            }
            LRConfig config = Data.Config;
            CheckMeshAssigment();

            if (_regenerateBasedOnCurrentValues)
            {
                Data.ApplayDataToMesh(ref _mesh);
                return;
            }

            if (_stopRegeneration)
                return;
            if (config.PointsCount < 2)
            {
                _mesh.Clear();
                return;
            }
            Data.Setup(lrTransform: transform);
            // Setup segments info
            for (int s = 0; s < config.PointsCount - 1; s++)
            {
                Data.GetStartEndCylinder(s, out Vector3 start, out Vector3 end);
                var segment = Data.GenerateSegmentInfo(start: start,
                                                       end: end,
                                                       cylinderIndex: s);

                Data.AddSegmentInfo(segment);
            }

            // Generate cylinders
            for (int s = 0; s < config.PointsCount - 1; s++)
            {
                Data.GetStartEndCylinder(s, out Vector3 start, out Vector3 end);
                Data.GenerateCylinder(start: start,
                                      end: end,
                                      cylinderIndex: s,
                                      flipUV: false);
            }

            int currentVertexCount = Data.GetSegmentVerticesCount();
            int currentTriangleCount = Data.GetSegmentTrianglesCount();

            // Applay mods to LR
            foreach (var dirtyPoints in Data.Config.DirtyPoints)
                IntegrateMods(dirtyPoints, currentVertexCount, currentTriangleCount);


            Data.ApplayDataToMesh(ref _mesh);
            _meshFilter.sharedMesh = _mesh;
        }
        [SerializeField]
        bool showVertices;
        private void OnDrawGizmos()
        {
            if(showVertices)
            {
                Gizmos.color = Color.red;
                foreach (var item in Data.GetLastVerticeList())
                {
                    Vector3 pos = transform.TransformPoint(item);
                    Gizmos.DrawSphere(pos, 0.01f);
                }
            }
        }
    }
}
