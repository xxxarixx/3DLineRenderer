using UnityEngine;
using static Unity.Mathematics.math;

namespace ProcedularMesh.SStream
{
    class CylinderRenderer3D : MonoBehaviour
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

        public float height = 2f;       // Height of the cylinder
        public float radius = 0.5f;    // Radius of the cylinder
        public int pointsCount;        // Number of points around the cylinder
        int _segments => pointsCount;      // Number of segments around the cylinder
        int _heightSegments => pointsCount; // Number of segments along the height

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
            int capVertexCount = 2;
            int vertexCount = (_segments + 1) * (_heightSegments + 1) + capVertexCount;
            vertices = new Vector3[vertexCount];
            uv = new Vector2[vertexCount];
            normals = new Vector3[vertexCount];

            float deltaAngle = 2 * Mathf.PI / _segments;
            float deltaHeight = height / _heightSegments;

            int vertexIndex = 0;
            for (int y = 0; y <= _heightSegments; y++)
            {
                float currentHeight = -height / 2 + y * deltaHeight;

                for (int s = 0; s <= _segments; s++)
                {
                    float angle = s * deltaAngle;
                    float x = radius * Mathf.Cos(angle);
                    float z = radius * Mathf.Sin(angle);

                    vertices[vertexIndex] = new Vector3(x, currentHeight, z);
                    uv[vertexIndex] = new Vector2((float)s / _segments, (float)y / _heightSegments);
                    normals[vertexIndex] = normalize(new Vector3(x, currentHeight, z) - new Vector3(0,currentHeight,0));
                    vertexIndex++;
                }
            }

            // Triangles
            int capTraingleCount = _segments * 3 + _segments * 3;
            int sideTriangleCount = _segments * _heightSegments * 6;
            int triangleCount = sideTriangleCount + capTraingleCount;
            triangles = new int[triangleCount];

            int triangleIndex = 0;
            for (int y = 0; y < _heightSegments; y++)
            {
                for (int s = 0; s < _segments; s++)
                {
                    int current = y * (_segments + 1) + s;
                    int next = current + _segments + 1;

                    triangles[triangleIndex++] = current;
                    triangles[triangleIndex++] = next;
                    triangles[triangleIndex++] = current + 1;

                    triangles[triangleIndex++] = next;
                    triangles[triangleIndex++] = next + 1;
                    triangles[triangleIndex++] = current + 1;
                }
            }

            //bottom cap triangles
            vertices[^2] = new(0, -height / 2, 0);
            for (int i = 1; i <= _segments; i++)
            {
                int ti = sideTriangleCount + (i - 1) * 3;
                triangles[ti] = vertices.Length - 2;
                if (i == _segments)
                    triangles[ti + 2] = 1;
                else
                    triangles[ti + 2] = i + 1;
                triangles[ti + 1] = i;
            }


            //top cap triangles
            vertices[^1] = new(0, height / 2, 0);
            int topCapTriangleIndexStart = sideTriangleCount + _segments * 3;
            int startVericesIndex = vertices.Length - capVertexCount - _segments;
            for (int i = 1; i <= _segments; i++)
            {
                int ti = topCapTriangleIndexStart + (i - 1) * 3;
                int vi = startVericesIndex - 1 + i;
                triangles[ti] = vertices.Length - 1;
                if (i == _segments)
                    triangles[ti + 1] = startVericesIndex;
                else
                    triangles[ti + 1] = vi + 1;
                triangles[ti + 2] = vi;
            }

            // Assign to mesh
            _mesh.vertices = vertices;
            _mesh.uv = uv;
            _mesh.triangles = triangles;
            _mesh.normals = normals;
            _mesh.RecalculateBounds(); // Calculate normals for lighting
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
