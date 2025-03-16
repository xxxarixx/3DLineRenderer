using UnityEngine;
using LineRenderer3D.Datas;
using LineRenderer3D.Mods;
using System.IO;
using System.Collections.Generic;

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

        void Awake()
        {
            GenerateMesh();
        }

        void Update()
        {
            GenerateMesh();
        }

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

        /// <summary>
        /// Generates the mesh based on the current data and settings.
        /// </summary>
        void GenerateMesh()
        {
            Data ??= new();
            if (Data.Config == null)
            {
                Debug.LogError($"There is no config, please set configuration files!");
                return;
            }
            CheckMeshAssigment();

            if (_regenerateBasedOnCurrentValues)
            {
                Data.ApplayDataToMesh(ref _mesh);
                return;
            }

            if (_stopRegeneration)
                return;

            if (Data.Config.Points.Count < 2)
            {
                _mesh.Clear();
                return;
            }

            Data.Setup(lrTransform: transform);

            // Setup segments info
            for (int s = 0; s < Data.Config.Points.Count - 1; s++)
            {
                var segment = Data.GenerateSegmentInfo(start: transform.InverseTransformPoint(Data.Config.Points[s]),
                                                       end: transform.InverseTransformPoint(Data.Config.Points[s + 1]),
                                                       cylinderIndex: s);
                if (segment == null)
                    continue;
                Data.AddSegmentInfo(segment);
            }

            // Generate cylinders
            for (int s = 0; s < Data.Config.Points.Count - 1; s++)
            {
                /*if (s > 0 && Vector3.Distance(_data.Points[s - 1], _data.Points[s]) < 0.0001f)
                    continue;*/
                Data.GenerateCylinder(start: transform.InverseTransformPoint(Data.Config.Points[s]),
                                      end: transform.InverseTransformPoint(Data.Config.Points[s + 1]),
                                      cylinderIndex: s,
                                      flipUV: false);
            }

            // Applay mods to LR
            foreach (var mod in GetComponents<ILRModBase>())
                if (mod.IsEnabled)
                {
                    Data.GetMeshData(out var segmentInfos, out var vertices, out var normals, out var uv, out var triangles);
                    mod.ManipulateMesh(Data, ref segmentInfos, ref vertices, ref normals, ref uv, ref triangles);
                }

            Data.ApplayDataToMesh(ref _mesh);
            _meshFilter.sharedMesh = _mesh;
        }
    }
}
