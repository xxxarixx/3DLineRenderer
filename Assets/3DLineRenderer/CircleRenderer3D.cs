using UnityEngine;
using static Unity.Mathematics.math;
namespace LineRenderer3D
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    [ExecuteAlways]
    class CircleRenderer3D : MonoBehaviour
    {
        Mesh _mesh;
        [SerializeField]
        bool visualizeVertices = false;

        [SerializeField]
        float radious;

        [SerializeField]
        int numberOfPoints;

        [SerializeField]
        Vector3[] vertices;

        [SerializeField]
        Vector3[] normals;

        [SerializeField]
        Vector2[] uv;

        [SerializeField]
        int[] triangles;

        void SetupMesh()
        {
            _mesh = new Mesh
            {
                name = "Procedural Mesh"
            };
            GetComponent<MeshFilter>().mesh = _mesh;
        }
        private void Awake()
        {
            SetupMesh();
            GenerateMesh();
        }

        [ContextMenu(nameof(GenerateMesh))]
        void GenerateMesh()
        {
            _mesh.Clear();
            numberOfPoints = clamp(numberOfPoints, 3, int.MaxValue);
            int verticesCount = numberOfPoints + 1;
            // Vertex count: sides + top cap + bottom cap
            vertices = new Vector3[verticesCount];
            normals = new Vector3[verticesCount];
            uv = new Vector2[verticesCount];
            triangles = new int[numberOfPoints * 3];

            Vector3 center = transform.position;
            vertices[0] = Vector3.zero;
            normals[0] = Vector3.back;
            uv[0] = Vector2.zero;
            for (int i = 1; i < verticesCount; i++)
            {
                vertices[i] = GetPointOnCircle(Vector3.zero, numberOfPoints, i - 1) * radious; // 5
                normals[i] = Vector3.back;
                uv[i] = GetPointOnCircle(Vector3.zero, numberOfPoints, i - 1);
            }
            for (int i = 1; i <= numberOfPoints; i++)
            {
                int ti = (i - 1) * 3;
                triangles[ti] = 0;
                if(i == numberOfPoints)
                    triangles[ti + 1] = 1;
                else
                    triangles[ti + 1] = i + 1;
                triangles[ti + 2] = i;
            }





            _mesh.vertices = vertices;
            _mesh.normals = normals;
            _mesh.triangles = triangles;
            _mesh.uv = uv;
            _mesh.RecalculateBounds();
        }
        Vector3 GetPointOnCircle(Vector3 center, int division, int index) => new(center.x + cos(index * (PI2 / division)), center.y + sin(index * (PI2 / division)), center.z);
        private void OnValidate()
        {
            if (_mesh == null)
                SetupMesh();
            GenerateMesh();
        }

        private void OnDrawGizmos()
        {
            if(visualizeVertices)
                for (int i = 0; i < _mesh.vertices.Length; i++)
                    Gizmos.DrawSphere(_mesh.vertices[i] + transform.position, 0.1f);
        }
    }
}
