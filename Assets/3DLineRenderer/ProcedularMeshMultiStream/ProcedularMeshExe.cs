using ProcedularMesh.Streams;
using UnityEngine;

namespace ProcedularMesh
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    [ExecuteAlways]
    class ProcedularMeshExe : MonoBehaviour
    {
        private Mesh mesh;
        private MeshFilter meshFilter;

        [SerializeField]
        int resolution;

        void Awake()
        {
            SetupMesh();
            GenerateMesh();
        }
        void SetupMesh()
        {
            mesh = new Mesh
            {
                name = "Procedural Mesh"
            };
            GetComponent<MeshFilter>().mesh = mesh;
        }

        [ContextMenu(nameof(GenerateMesh))]
        void GenerateMesh() 
        { 
            Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
            Mesh.MeshData meshData = meshDataArray[0];

            MeshJob<SquareGrid, MultiStream>.ScheduleParallel(mesh, resolution, meshData, default).Complete();

            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);
        }

        private void OnValidate()
        {
            if (mesh == null)
                SetupMesh();
            GenerateMesh();
        }
    }
}
