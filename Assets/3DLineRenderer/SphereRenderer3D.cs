using UnityEngine;
using static Unity.Mathematics.math;

namespace LineRenderer3D
{
    class SphereRenderer3D : MonoBehaviour
    {
        Mesh _mesh;
        [SerializeField]
        bool visualizeVertices = false;

        [SerializeField]
        bool visualizeNormals = false;

        [SerializeField]
        Vector3[] vertices;

        [SerializeField]
        Vector3[] normals;

        [SerializeField]
        Vector2[] uv;

        [SerializeField]
        int[] triangles;

        public float radius = 1f;
        public int pointsCount = 3;
        int _segments => pointsCount;
        int _rings => pointsCount;

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
            // Vertices
            vertices = new Vector3[(_segments + 1) * (_rings + 1)];
            uv = new Vector2[vertices.Length];
            triangles = new int[_segments * _rings * 6];
            normals = new Vector3[vertices.Length];

            float deltaTheta = Mathf.PI / _rings;
            float deltaPhi = 2 * Mathf.PI / _segments;

            int vertexIndex = 0;
            for (int ring = 0; ring <= _rings; ring++)
            {
                float theta = ring * deltaTheta;
                float sinTheta = Mathf.Sin(theta);
                float cosTheta = Mathf.Cos(theta);

                for (int segment = 0; segment <= _segments; segment++)
                {
                    float phi = segment * deltaPhi;
                    float sinPhi = Mathf.Sin(phi);
                    float cosPhi = Mathf.Cos(phi);

                    float x = radius * sinTheta * cosPhi;
                    float y = radius * cosTheta;
                    float z = radius * sinTheta * sinPhi;

                    vertices[vertexIndex] = new Vector3(x, y, z);
                    normals[vertexIndex] = normalize(new Vector3(x, y, z) - Vector3.zero);
                    uv[vertexIndex] = new Vector2((float)segment / _segments, (float)ring / _rings);
                    vertexIndex++;
                }
            }

            // Triangles
            int triangleIndex = 0;
            for (int ring = 0; ring < _rings; ring++)
            {
                for (int segment = 0; segment < _segments; segment++)
                {
                    int current = ring * (_segments + 1) + segment;
                    int next = current + _segments + 1;

                    triangles[triangleIndex++] = current;
                    triangles[triangleIndex++] = current + 1;
                    triangles[triangleIndex++] = next;

                    triangles[triangleIndex++] = next;
                    triangles[triangleIndex++] = current + 1;
                    triangles[triangleIndex++] = next + 1;
                }
            }

            _mesh.vertices = vertices;
            _mesh.uv = uv;
            _mesh.triangles = triangles;
            _mesh.RecalculateNormals();
        }



        private void OnValidate()
        {
            if (_mesh == null)
                SetupMesh();
            GenerateMesh();
        }

        private void OnDrawGizmos()
        {
            if (visualizeVertices)
            {
                if (vertices != null)
                {
                    Gizmos.color = Color.red;
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        Gizmos.DrawSphere(transform.TransformPoint(vertices[i]), 0.05f);
                    }
                }
            }
            if (visualizeNormals)
            {
                if (normals != null)
                {
                    Gizmos.color = Color.blue;
                    for (int i = 0; i < normals.Length; i++)
                    {
                        Gizmos.DrawRay(transform.TransformPoint(vertices[i]), normals[i]);
                    }
                }
            }
        }
    }
}
