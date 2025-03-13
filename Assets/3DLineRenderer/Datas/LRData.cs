using System;
using System.Collections.Generic;
using UnityEngine;

namespace LineRenderer3D.Datas
{
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

        [Serializable]
        public class SegmentInfo
        {
            [SerializeField]
            public readonly string uniqueId;

            [SerializeField]
            public Vector3 startSegmentCenter;
            public readonly Vector3 initStartSegmentCenter;
            public List<int> startSegmentVericesIndex = new();
            [SerializeField]
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

        public void ChangeSegmentVerticesLocation(List<int> verticeIndexes, Vector3 translation, int segmentID, bool isStart, bool shouldUpdateUV)
        {
            foreach (int index in verticeIndexes)
                _vertices[index] += translation;

            if (shouldUpdateUV)
                UpdateUVForVertices(verticeIndexes, segmentID, isStart);
        }

        public void UpdateUVForVertices(List<int> verticeIndexes, int segmentID, bool isStart)
        {
            for (int i = 0; i < verticeIndexes.Count; i++)
            {
                int vertexIndex = verticeIndexes[i];
                int faceIndex = i % NumberOfFaces;

                // Calc new UV based on segment and position
                float u = CalculateU(faceIndex, NumberOfFaces);
                float v = isStart ? 0f : 1.5f;

                _uvs[vertexIndex] = new Vector2(u, v);
            }
        }

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

        float CalculateU(int faceIndex, int numberOfFaces)
        {
            if (faceIndex > numberOfFaces / 2)
                return 2f - ((float)faceIndex / numberOfFaces) * 2f;
            else
                return ((float)faceIndex / numberOfFaces) * 2f;
        }

        public bool IsCylinderIndexValid(int cylinderIndex) => cylinderIndex >= 0 && cylinderIndex < _segmentInfos.Count;

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

        public Vector3 GetVertice(int index) => _vertices[index];

        public void AddSegmentInfo(SegmentInfo segmentInfo) => _segmentInfos.Add(segmentInfo);    

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
    }
}
