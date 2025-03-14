using UnityEngine;
using LineRenderer3D.Datas;
using LineRenderer3D.Mods;

namespace LineRenderer3D
{
    /// <summary>
    /// The main class that generates the mesh based on the current data and settings.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    class LineRenderer3DExe : MonoBehaviour
    {
        [SerializeField]
        LRData _data;

        [Header("Debug")]
        [SerializeField]
        bool _regenerateBasedOnCurrentValues;

        [SerializeField]
        bool _stopRegeneration;

        Mesh _mesh;

        MeshFilter _meshFilter;

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
            if(_mesh == null)
                _mesh = new();

            if(_meshFilter == null)
                _meshFilter = GetComponent<MeshFilter>();

            if(_meshFilter.sharedMesh == null)
                _meshFilter.sharedMesh = _mesh;
        }

        /// <summary>
        /// Generates the mesh based on the current data and settings.
        /// </summary>
        void GenerateMesh()
        {
            _data ??= new();
            CheckMeshAssigment();

            if (_regenerateBasedOnCurrentValues)
            {
                _data.ApplayDataToMesh(ref _mesh);
                return;
            }

            if (_stopRegeneration)
                return;

            if (_data.Points.Count < 2)
            {
                _mesh.Clear();
                return;
            }

            _data.Setup(lrTransform: transform);

            // Setup segments info
            for (int s = 0; s < _data.Points.Count - 1; s++)
            {
                var segment = _data.GenerateSegmentInfo(start: transform.InverseTransformPoint(_data.Points[s]),
                                                       end: transform.InverseTransformPoint(_data.Points[s + 1]),
                                                       cylinderIndex: s);
                if (segment == null)
                    continue;
                _data.AddSegmentInfo(segment);
            }

            // Generate cylinders
            for (int s = 0; s < _data.Points.Count - 1; s++)
            {
                /*if (s > 0 && Vector3.Distance(_data.Points[s - 1], _data.Points[s]) < 0.0001f)
                    continue;*/
                _data.GenerateCylinder(start: transform.InverseTransformPoint(_data.Points[s]),
                                      end: transform.InverseTransformPoint(_data.Points[s + 1]),
                                      cylinderIndex: s,
                                      flipUV: false);
            }

            // Applay mods to LR
            foreach (var mod in GetComponents<ILRModBase>())
                if (mod.IsEnabled)
                {
                    _data.GetMeshData(out var segmentInfos, out var vertices, out var normals, out var uv, out var triangles);
                    mod.ManipulateMesh(_data, ref segmentInfos, ref vertices, ref normals, ref uv, ref triangles);
                }

            _data.ApplayDataToMesh(ref _mesh);
            _meshFilter.sharedMesh = _mesh;
        }
    }      
}
