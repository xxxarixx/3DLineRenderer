using System.Collections.Generic;
using UnityEngine;

namespace LineRenderer3D
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class CylinderLineRenderer : MonoBehaviour
    {
        public int numberOfFaces = 8;
        public float radius = 0.1f;
        public List<Vector3> points = new List<Vector3>();

        private Mesh mesh;
        private MeshFilter meshFilter;

        void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            mesh = new Mesh();
            meshFilter.mesh = mesh;

            // Assign default material if none exists
            if (GetComponent<MeshRenderer>().material == null)
                GetComponent<MeshRenderer>().material = new Material(Shader.Find("Standard"));

            UpdateLine();
        }

        public void UpdateLine()
        {
            GenerateMesh();
        }

        private void GenerateMesh()
        {
            if (points.Count < 2)
            {
                Debug.Log("cleared?");
                mesh.Clear();
                return;
            }

            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();

            for (int s = 0; s < points.Count - 1; s++)
            {
                Vector3 start = transform.InverseTransformPoint(points[s]);
                Vector3 end = transform.InverseTransformPoint(points[s + 1]);
                Vector3 direction = (end - start).normalized;

                Debug.Log($"direction {direction}");
                if (direction == Vector3.zero) continue;

                Quaternion rotation = Quaternion.LookRotation(direction);

                // Generate vertices for this segment
                for (int i = 0; i < numberOfFaces; i++)
                {
                    Debug.Log("face");
                    float theta = Mathf.PI * 2 * i / numberOfFaces;
                    Vector3 circleOffset = new Vector3(
                        Mathf.Cos(theta) * radius,
                        Mathf.Sin(theta) * radius,
                        0
                    );

                    // Calculate positions in local space
                    Vector3 startVert = start + rotation * circleOffset;
                    Vector3 endVert = end + rotation * circleOffset;

                    vertices.Add(startVert);
                    vertices.Add(endVert);

                    // Normals point outward from cylinder center
                    Vector3 normal = rotation * circleOffset.normalized;
                    normals.Add(normal);
                    normals.Add(normal);

                    // UV mapping
                    uvs.Add(new Vector2((float)i / numberOfFaces, 0));
                    uvs.Add(new Vector2((float)i / numberOfFaces, 1));
                }

                // Generate triangles for this segment
                int baseIndex = s * numberOfFaces * 2;
                for (int i = 0; i < numberOfFaces; i++)
                {
                    int current = baseIndex + i * 2;
                    int next = baseIndex + ((i + 1) % numberOfFaces) * 2;

                    // First triangle
                    triangles.Add(current);
                    triangles.Add(next);
                    triangles.Add(current + 1);

                    // Second triangle
                    triangles.Add(next);
                    triangles.Add(next + 1);
                    triangles.Add(current + 1);
                }
            }

            mesh.Clear();
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.normals = normals.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateBounds();
        }

        public void SetPoints(List<Vector3> newPoints)
        {
            points = new List<Vector3>(newPoints);
            GenerateMesh();
        }

        // Update mesh when values change in inspector
        private void OnValidate()
        {
            if (meshFilter != null && points.Count > 1)
                GenerateMesh();
        }
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            foreach (Vector3 point in points)
                Gizmos.DrawWireSphere(point,.1f);
        }
    }
}
