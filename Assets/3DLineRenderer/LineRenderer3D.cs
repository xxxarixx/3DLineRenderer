using System.Collections.Generic;
using UnityEngine;

namespace LineRenderer3D
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    class LineRenderer3D : MonoBehaviour
    {
        [SerializeField]
        List<Vector3> points = new();
        private void Start()
        {
            GenerateLineRenderer();
        }

        [ContextMenu("Generate Mesh")]
        void GenerateLineRenderer()
        {
            Debug.Log("Mesh being generated");
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            meshFilter.mesh = GenerateMesh();
        }
        Mesh GenerateMesh()
        {
            Mesh mesh = new();
            mesh.name = "3D Line renderer";

            mesh.vertices = new Vector3[]
            {
                Vector3.zero,Vector3.right,Vector3.up
            };

            mesh.triangles = new int[]
            {
                0,2,1
            };

            mesh.normals = new Vector3[] {
                Vector3.back, Vector3.back, Vector3.back
            };

            mesh.uv = new Vector2[]
            {
                Vector2.zero, Vector2.right, Vector2.up
            };

            mesh.tangents = new Vector4[] {
                new (1f, 0f, 0f, -1f),
                new (1f, 0f, 0f, -1f),
                new (1f, 0f, 0f, -1f)
            };

            return mesh;
        }
        private void OnValidate()
        {
            GenerateLineRenderer();
        }

        private void OnDrawGizmos()
        {
            foreach (var point in points)
                Gizmos.DrawSphere(point, 0.1f);
        }
    }
}
