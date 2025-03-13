using System;
using System.Collections.Generic;
using UnityEngine;

namespace LineRenderer3D.Datas
{
    /// <summary>
    /// Before using this class, you need to call Setup method!
    /// </summary>
    [Serializable]
    public class LRData
    {
        public Transform LrTransform { get; private set; }

        [SerializeField]
        [Tooltip("needed to be even number, in order to uv be properly generated, it is handled automatically")]
        [Range(4, 20)]
        int _numberOfFaces = 8;

        /// <summary>
        /// needed to be even number, in order to uv be properly generated, it is handled automatically
        /// </summary>
        public int NumberOfFaces
        {
            get
            {
                int numberOfFaces = _numberOfFaces;
                if (numberOfFaces % 2 != 0)
                    numberOfFaces++;
                return numberOfFaces;
            }
        }

        [SerializeField]
        float _radius = 0.1f;

        /// <summary>
        /// The radius of the cylinder segments.
        /// </summary>
        public float Radius 
        { 
            get 
            { 
                return _radius;
            } 
        }

        public List<Vector3> Points = new();

        [Header("Debug")]

        [SerializeField]
        List<SegmentInfo> _segmentInfos;

        [SerializeField]
        List<Vector3> _vertices;

        [SerializeField]
        List<Vector3> _normals;

        [SerializeField]
        List<Vector2> _uvs;

        [SerializeField]
        List<int> _triangles;

        /// <summary>
        /// Contains valuable information about cylinder segment, like start and end center, and vertices index of start or end.
        /// </summary>
        [Serializable]
        public class SegmentInfo
        {
            public readonly string uniqueId;

            [Header("Start veriables")]
            public Vector3 startSegmentCenter;

            public readonly Vector3 initStartSegmentCenter;

            public List<int> startSegmentVericesIndex = new();

            [Header("End veriables")]
            public Vector3 endSegmentCenter;

            public readonly Vector3 initEndSegmentCenter;

            public List<int> endSegmentVericesIndex = new();

            public SegmentInfo(Vector3 startSegmentCenter, Vector3 endSegmentCenter)
            {
                initStartSegmentCenter = startSegmentCenter;
                initEndSegmentCenter = endSegmentCenter;

                this.startSegmentCenter = startSegmentCenter;
                this.endSegmentCenter = endSegmentCenter;

                uniqueId = Guid.NewGuid().ToString();
            }
        }

        public void Setup(Transform lrTransform)
        {
            LrTransform = lrTransform;
            _segmentInfos = new();
            _vertices = new();
            _normals = new();
            _uvs = new();
            _triangles = new();
        }

        #region Data Manipulation

        public SegmentInfo GenerateSegmentInfo(Vector3 start, Vector3 end, int cylinderIndex)
        {
            Vector3 direction = (end - start).normalized;
            if (direction == Vector3.zero) return null;

            Quaternion rotation = Quaternion.LookRotation(direction);
            Vector3 startCenter = start + rotation * Vector3.zero;
            Vector3 endCenter = end + rotation * Vector3.zero;

            List<int> startSegmentVericesIndex = new();
            List<int> endSegmentVericesIndex = new();

            int baseIndex = cylinderIndex * NumberOfFaces * 2;
            for (int i = 0; i < NumberOfFaces; i++)
            {
                int current = baseIndex + i * 2;
                startSegmentVericesIndex.Add(current);
                endSegmentVericesIndex.Add(current + 1);
            }

            SegmentInfo segmentInfo = new(LrTransform.TransformPoint(startCenter), LrTransform.TransformPoint(endCenter))
            {
                startSegmentVericesIndex = startSegmentVericesIndex,
                endSegmentVericesIndex = endSegmentVericesIndex,
            };
            return segmentInfo;
        }

        public bool IsCylinderIndexValid(int cylinderIndex) => cylinderIndex >= 0 && cylinderIndex < _segmentInfos.Count;


        /// <summary>
        /// Generates a cylinder between two points and adds it to the mesh data.
        /// </summary>
        /// <param name="cylinderIndex">The index of the cylinder, you must specify, in most cases it will be just segmentInfos.Count.</param>
        /// <param name="flipUV">Whether to flip the UV coordinates.</param>
        public void GenerateCylinder(Vector3 start, Vector3 end, int cylinderIndex, bool flipUV)
        {
            Vector3 direction = (end - start).normalized;

            if (direction == Vector3.zero) return;

            Quaternion rotation = Quaternion.LookRotation(direction);

            // Generate vertices for this segment
            for (int f = 0; f < NumberOfFaces; f++)
            {
                float theta = Mathf.PI * 2 * f / NumberOfFaces;
                Vector3 circleOffset = new(
                    Mathf.Cos(theta) * Radius,
                    Mathf.Sin(theta) * Radius,
                    0
                );

                // Calculate positions in local space
                Vector3 startVert = start + rotation * circleOffset;
                Vector3 endVert = end + rotation * circleOffset;

                _vertices.Add(startVert);
                _vertices.Add(endVert);

                // Normals point outward from cylinder center
                Vector3 normal = rotation * circleOffset.normalized;
                _normals.Add(normal);
                _normals.Add(normal);

                // UV mapping
                if (f > NumberOfFaces / 2)
                {
                    _uvs.Add(new Vector2(2f - (1f / NumberOfFaces * f) * 2f, flipUV ? 1 : 0));
                    _uvs.Add(new Vector2(2f - (1f / NumberOfFaces * f) * 2f, flipUV ? 0 : 1));
                }
                else
                {
                    _uvs.Add(new Vector2((1f / NumberOfFaces * f) * 2f, flipUV ? 1 : 0));
                    _uvs.Add(new Vector2((1f / NumberOfFaces * f) * 2f, flipUV ? 0 : 1));
                }
            }

            // Generate triangles for this segment
            int baseIndex = cylinderIndex * NumberOfFaces * 2;
            for (int i = 0; i < NumberOfFaces; i++)
            {
                int current = baseIndex + i * 2;
                int next = baseIndex + ((i + 1) % NumberOfFaces) * 2;
                // First triangle
                _triangles.Add(current);
                _triangles.Add(next);
                _triangles.Add(current + 1);

                // Second triangle
                _triangles.Add(next);
                _triangles.Add(next + 1);
                _triangles.Add(current + 1);
            }
        }

        public Vector3 GetVertex(int index) => _vertices[index];

        /// <summary>
        /// Adds a SegmentInfo object to the list of segment information.
        /// </summary>
        public void AddSegmentInfo(SegmentInfo segmentInfo) => _segmentInfos.Add(segmentInfo);

        /// <summary>
        /// Applies the mesh data to the given mesh.
        /// </summary>
        public void ApplayDataToMesh(ref Mesh mesh)
        {
            mesh.Clear();
            mesh.vertices = _vertices.ToArray();
            mesh.triangles = _triangles.ToArray();
            mesh.normals = _normals.ToArray();
            mesh.uv = _uvs.ToArray();
            mesh.RecalculateBounds();
        }

        public void GetMeshData(out List<SegmentInfo> segmentInfos, out List<Vector3> vertices, out List<Vector3> normals, out List<Vector2> uvs, out List<int> triangles)
        {
            segmentInfos = _segmentInfos;
            vertices = _vertices;
            normals = _normals;
            uvs = _uvs;
            triangles = _triangles;
        }
        
        #endregion
    }
}
