using UnityEngine;
using LineRenderer3D.Datas;
using LineRenderer3D.Mods;
using System.Collections.Generic;
using LinerRenderer3D.Datas;

namespace LineRenderer3D
{
    /// <summary>
    /// The main class that generates the mesh based on the current data and settings.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class LineRenderer3DExe : MonoBehaviour
    {
        public LRData Data;

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

        List<ILRModBase> _mods = new();

        void Awake()
        {
            UpdateMods();
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
            Debug.Log($"Dirty count {Data.Config.DirtyPoints.Count}");
        }

        void PartialMeshUpdate()
        {
            Debug.Log("Partial mesh update");
            CheckMeshAssigment();
            Data.UpdateDirtyPoints();
#if UNITY_EDITOR
            UpdateMods();
#endif
            if (_mods.Count > 0)
            {
                List<Vector3> vertices = new();
                List<Vector3> normals = new();
                List<Vector2> uvs = new();
                List<int> triangles = new();
                Data.GetMeshData(out var segmentInfos, out vertices, out normals, out uvs, out triangles);

                foreach (var mod in _mods)
                    if (mod.IsEnabled)
                    {
                        Debug.Log($"Mod activated: {mod.Name}");
                        mod.ManipulateMesh(Data, ref segmentInfos, ref vertices, ref normals, ref uvs, ref triangles);
                    }
                Data.ApplayDataToMesh(ref _mesh, vertices, normals, uvs, triangles);
            }
            else
            {
                Data.ApplayDataToMesh(ref _mesh);
            }
            _meshFilter.sharedMesh = _mesh;
        }

        void UpdateMods() => _mods = new List<ILRModBase>(GetComponents<ILRModBase>());

        /// <summary>
        /// Checks and assigns the mesh and mesh filter components.
        /// </summary>
        void CheckMeshAssigment()
        {
            if (_mesh == null)
                _mesh = new();

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
            Data ??= new();
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

            Debug.Log(config.PointsCount);
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

            // Applay mods to LR
            if (_mods.Count > 0)
            {
                List<Vector3> vertices = new();
                List<Vector3> normals = new();
                List<Vector2> uvs = new();
                List<int> triangles = new();
                Data.GetMeshData(out var segmentInfos, out vertices, out normals, out uvs, out triangles);

                foreach (var mod in _mods)
                    if (mod.IsEnabled)
                    {
                        Debug.Log($"Mod activated: {mod.Name}");
                        mod.ManipulateMesh(Data, ref segmentInfos, ref vertices, ref normals, ref uvs, ref triangles);
                    }
                Data.ApplayDataToMesh(ref _mesh, vertices, normals, uvs, triangles);
            }
            else
            {
                Data.ApplayDataToMesh(ref _mesh);
            }

            Data.ApplayDataToMesh(ref _mesh);
            _meshFilter.sharedMesh = _mesh;
        }
    }
}
