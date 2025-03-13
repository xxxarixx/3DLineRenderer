using UnityEngine;
using LineRenderer3D.Datas;
using LineRenderer3D.Mods;

namespace LineRenderer3D
{
    [ExecuteAlways]
    class LineRenderer3DExe : MonoBehaviour
    {
        [SerializeField]
        LRData data;

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

        void CheckMeshAssigment()
        {
            if(_mesh == null)
                _mesh = new();

            if(_meshFilter == null)
                _meshFilter = GetComponent<MeshFilter>();

            if(_meshFilter.sharedMesh == null)
                _meshFilter.sharedMesh = _mesh;
        }

        void GenerateMesh()
        {
            data ??= new();

            if (_regenerateBasedOnCurrentValues)
            {
                data.ApplayDataToMesh(ref _mesh);
                if (_meshFilter != null)
                    _meshFilter.mesh = _mesh;
                return;
            }

            if (_stopRegeneration)
                return;

            CheckMeshAssigment();

            if (data.Points.Count < 2)
            {
                _mesh.Clear();
                return;
            }

            

            data.Setup(lrTransform: transform);

            // Setup segments info
            for (int s = 0; s < data.Points.Count - 1; s++)
            {
                var segment = data.GenerateSegmentInfo(start: transform.InverseTransformPoint(data.Points[s]),
                                                       end: transform.InverseTransformPoint(data.Points[s + 1]),
                                                       cylinderIndex: s);
                if (segment == null)
                    continue;
                data.AddSegmentInfo(segment);
            }

            // Generate cylinders
            for (int s = 0; s < data.Points.Count - 1; s++)
                data.GenerateCylinder(start: transform.InverseTransformPoint(data.Points[s]),
                                      end: transform.InverseTransformPoint(data.Points[s + 1]),
                                      cylinderIndex: s,
                                      flipUV: false);

            // Applay mods to LR
            foreach (var mod in GetComponents<ILRModBase>())
                if (mod.IsEnabled)
                {
                    data.GetMeshData(out var segmentInfos, out var vertices, out var normals, out var uv, out var triangles);
                    mod.ManipulateMesh(data, ref segmentInfos, ref vertices, ref normals, ref uv, ref triangles);
                }

            data.ApplayDataToMesh(ref _mesh);
            _meshFilter.mesh = _mesh;
        }
    }      
}
