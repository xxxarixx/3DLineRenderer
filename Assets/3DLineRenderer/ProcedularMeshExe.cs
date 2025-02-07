using UnityEngine;

namespace ProcedularMesh
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    class ProcedularMeshExe : MonoBehaviour
    {
        private Mesh mesh;
        private MeshFilter meshFilter;

        void Awake()
        {
            mesh = new Mesh
            {
                name = "Procedural Mesh"
            };
            GenerateMesh();
            GetComponent<MeshFilter>().mesh = mesh;
        }

        [ContextMenu(nameof(GenerateMesh))]
        void GenerateMesh() 
        { 
            Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
            Mesh.MeshData meshData = meshDataArray[0];
        }
    }
}
