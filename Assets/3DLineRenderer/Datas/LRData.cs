using LinerRenderer3D.Datas;
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
        public LRConfig Config;

        List<SegmentInfo> _segmentInfos;

        /// <summary>
        /// Contains valuable information about cylinder segment, like start and end center, and vertices index of start or end.
        /// </summary>
        [Serializable]
        public class SegmentInfo
        {
            public readonly string uniqueId;

            public Quaternion rotation;

            [Header("Start veriables")]
            public Vector3 startSegmentCenter;

            public readonly Vector3 initStartSegmentCenter;

            public List<int> startSegmentVericesIndex = new();

            [Header("End veriables")]
            public Vector3 endSegmentCenter;

            public readonly Vector3 initEndSegmentCenter;

            public List<int> endSegmentVericesIndex = new();

            public List<Vector3> vertices = new();

            public List<Vector3> normals = new();
            
            public List<Vector2> uvs = new();

            public List<int> triangles = new();

            public SegmentInfo(Vector3 startSegmentCenter, Vector3 endSegmentCenter)
            {
                initStartSegmentCenter = startSegmentCenter;
                initEndSegmentCenter = endSegmentCenter;

                this.startSegmentCenter = startSegmentCenter;
                this.endSegmentCenter = endSegmentCenter;

                uniqueId = Guid.NewGuid().ToString();
            }

            public void ShiftVertices(int shiftPower, int numberOfFaces)
            {
                int shiftIndex = shiftPower * numberOfFaces * 2;
                for (int i = 0; i < startSegmentVericesIndex.Count; i++)
                {
                    startSegmentVericesIndex[i] += shiftIndex;
                    endSegmentVericesIndex[i] += shiftIndex;
                }
            }
        }

        public void UpdateDirtyPoints()
        {
            foreach ((int index, LRConfig.DirtyFlag dirtyFlag) in Config.DirtyPoints)
            {
                Debug.Log($"dirty point ({index}), flag:{dirtyFlag}");

                Vector3 start;
                Vector3 end;
                switch (dirtyFlag)
                {
                    case LRConfig.DirtyFlag.ChangedPosition:
                        if (!IsCylinderIndexValid(index) || index > Config.PointsCount - 2)
                            continue;
                        GetStartEndCylinder(index, out start, out end);
                        GenerateCylinder(start, end, index, false, isItUpdate: true);
                        UpdateSegment(index);
                        break;
                    case LRConfig.DirtyFlag.Removed:
                        Debug.Log($"Removed at {index}");
                        _segmentInfos.RemoveAt(index);
                        UpdateNextSegmentsTriangles(index);
                        break;
                    case LRConfig.DirtyFlag.Added:
                        GetStartEndCylinder(index, out start, out end);
                        _segmentInfos.Insert(index, GenerateSegmentInfo(start, end, index));
                        GenerateCylinder(start, end, index, false);
                        UpdateNextSegmentsTriangles(index + 1);
                        break;
                    default:
                        break;
                }
            }
        }

        public void Setup(Transform lrTransform)
        {
            LrTransform = lrTransform;
            _segmentInfos = new();
        }

        #region Data Manipulation

        public SegmentInfo GetSegmentInfo(int index) => _segmentInfos[index];

        public void UpdateNextSegmentsTriangles(int index) 
        {
            for (int i = index; i < _segmentInfos.Count; i++)
            {
                RegenerateCylinderTriangles(i);
                Debug.Log($"Regenerated triangles for segment info {i}");
            }
        }

        void UpdateSegment(int pointIndex)
        {
            GetStartEndCylinder(pointIndex, out Vector3 start, out Vector3 end);

            SegmentInfo segmentInfo = _segmentInfos[pointIndex];
            segmentInfo.startSegmentCenter = LrTransform.TransformPoint(start);
            segmentInfo.endSegmentCenter = LrTransform.TransformPoint(end);

            Vector3 direction = (end - start).normalized;
            Vector3 worldUp = Vector3.up;

            // Handle the case where direction is parallel to worldUp
            if (Mathf.Abs(Vector3.Dot(direction, worldUp)) > 0.9999f)
            {
                // Use an alternative reference axis (e.g., forward) to compute right
                Vector3 alternativeReference = Vector3.forward;
                Vector3 right = Vector3.Cross(direction, alternativeReference).normalized;
                worldUp = Vector3.Cross(right, direction).normalized;
            }
            else
            {
                // Compute right and up vectors using standard method
                Vector3 right = Vector3.Cross(worldUp, direction).normalized;
                worldUp = Vector3.Cross(direction, right).normalized;
            }

            Quaternion rotation = Quaternion.LookRotation(direction, worldUp);
            segmentInfo.rotation = rotation;
            _segmentInfos[pointIndex] = segmentInfo;
        }

        public void AddSegmentInfo(SegmentInfo segment) => _segmentInfos.Add(segment);

        public SegmentInfo GenerateSegmentInfo(Vector3 start, Vector3 end, int cylinderIndex)
        {
            int numberOfFaces = Config.NumberOfFaces;
            Vector3 direction = (end - start).normalized;
            if (direction == Vector3.zero) return null;
            Vector3 worldUp = Vector3.up;

            // Handle the case where direction is parallel to worldUp
            if (Mathf.Abs(Vector3.Dot(direction, worldUp)) > 0.9999f)
            {
                // Use an alternative reference axis (e.g., forward) to compute right
                Vector3 alternativeReference = Vector3.forward;
                Vector3 right = Vector3.Cross(direction, alternativeReference).normalized;
                worldUp = Vector3.Cross(right, direction).normalized;
            }
            else
            {
                // Compute right and up vectors using standard method
                Vector3 right = Vector3.Cross(worldUp, direction).normalized;
                worldUp = Vector3.Cross(direction, right).normalized;
            }

            Quaternion rotation = Quaternion.LookRotation(direction, worldUp);
            Vector3 startCenter = start + rotation * Vector3.zero;
            Vector3 endCenter = end + rotation * Vector3.zero;

            List<int> startSegmentVericesIndex = new();
            List<int> endSegmentVericesIndex = new();

            int baseIndex = cylinderIndex * numberOfFaces * 2;
            for (int i = 0; i < numberOfFaces; i++)
            {
                int current = baseIndex + i * 2;
                startSegmentVericesIndex.Add(current);
                endSegmentVericesIndex.Add(current + 1);
            }

            SegmentInfo segmentInfo = new(LrTransform.TransformPoint(startCenter), LrTransform.TransformPoint(endCenter))
            {
                startSegmentVericesIndex = startSegmentVericesIndex,
                endSegmentVericesIndex = endSegmentVericesIndex,
                rotation = rotation
            };
            return segmentInfo;
        }

        public bool IsCylinderIndexValid(int cylinderIndex) => cylinderIndex >= 0 && cylinderIndex < _segmentInfos.Count;

        /// <summary>
        /// Generates a cylinder between two points and adds it to the mesh data.
        /// </summary>
        /// <param name="cylinderIndex">The index of the cylinder, you must specify, in most cases it will be just segmentInfos.Count.</param>
        /// <param name="flipUV">Whether to flip the UV coordinates.</param>
        public void GenerateCylinder(Vector3 start, Vector3 end, int cylinderIndex, bool flipUV, bool isItUpdate = false)
        {
            int numberOfFaces = Config.NumberOfFaces;
            float radius = Config.Radius;
            Vector3 direction = (end - start).normalized;
            SegmentInfo segment = _segmentInfos[cylinderIndex];

            Vector3 worldUp = Vector3.up;
            
            // Handle the case where direction is parallel to worldUp
            if (Mathf.Abs(Vector3.Dot(direction, worldUp)) > 0.9999f)
            {
                // Use an alternative reference axis (e.g., forward) to compute right
                Vector3 alternativeReference = Vector3.forward;
                Vector3 right = Vector3.Cross(direction, alternativeReference).normalized;
                worldUp = Vector3.Cross(right, direction).normalized;
            }
            else
            {
                // Compute right and up vectors using standard method
                Vector3 right = Vector3.Cross(worldUp, direction).normalized;
                worldUp = Vector3.Cross(direction, right).normalized;
            }

            Quaternion rotation = Quaternion.LookRotation(direction, worldUp);
            if(isItUpdate)
            {
                segment.vertices.Clear();
                segment.normals.Clear();
                segment.uvs.Clear();
                segment.triangles.Clear();
            }

            // Generate vertices for this segment
            for (int f = 0; f < numberOfFaces; f++)
            {
                float theta = Mathf.PI * 2 * f / numberOfFaces;
                Vector3 circleOffset = new(
                    Mathf.Cos(theta) * radius,
                    Mathf.Sin(theta) * radius,
                    0
                );

                // Calculate positions in local space
                Vector3 startVert = start + rotation * circleOffset;
                Vector3 endVert = end + rotation * circleOffset;

                segment.vertices.Add(startVert);
                segment.vertices.Add(endVert);

                // Normals point outward from cylinder center
                Vector3 normal = rotation * circleOffset.normalized;
                segment.normals.Add(normal);
                segment.normals.Add(normal);

                // UV mapping
                if (f > numberOfFaces / 2)
                {
                    segment.uvs.Add(new Vector2(2f - (1f / numberOfFaces * f) * 2f, flipUV ? 1 : 0));
                    segment.uvs.Add(new Vector2(2f - (1f / numberOfFaces * f) * 2f, flipUV ? 0 : 1));
                }
                else
                {
                    segment.uvs.Add(new Vector2((1f / numberOfFaces * f) * 2f, flipUV ? 1 : 0));
                    segment.uvs.Add(new Vector2((1f / numberOfFaces * f) * 2f, flipUV ? 0 : 1));
                }
            }


            // Generate triangles for this segment
            int baseIndex = cylinderIndex * numberOfFaces * 2;
            for (int i = 0; i < numberOfFaces; i++)
            {
                int current = baseIndex + i * 2;
                int next = baseIndex + ((i + 1) % numberOfFaces) * 2;
                // First triangle
                segment.triangles.Add(current);
                segment.triangles.Add(next);
                segment.triangles.Add(current + 1);

                // Second triangle
                segment.triangles.Add(next);
                segment.triangles.Add(next + 1);
                segment.triangles.Add(current + 1);
            }

            _segmentInfos[cylinderIndex] = segment;
        }

        public void RegenerateCylinderTriangles(int cylinderIndex)
        {
            int numberOfFaces = Config.NumberOfFaces;
            SegmentInfo segment = _segmentInfos[cylinderIndex];
            segment.triangles.Clear();

            // Generate triangles for this segment
            int baseIndex = cylinderIndex * numberOfFaces * 2;
            for (int i = 0; i < numberOfFaces; i++)
            {
                int current = baseIndex + i * 2;
                int next = baseIndex + ((i + 1) % numberOfFaces) * 2;
                // First triangle
                segment.triangles.Add(current);
                segment.triangles.Add(next);
                segment.triangles.Add(current + 1);

                // Second triangle
                segment.triangles.Add(next);
                segment.triangles.Add(next + 1);
                segment.triangles.Add(current + 1);
            }

            _segmentInfos[cylinderIndex] = segment;
        }

        public void GetStartEndCylinder(int cylinderIndex, out Vector3 start, out Vector3 end)
        {
            start = LrTransform.InverseTransformPoint(Config.GetPoint(cylinderIndex));
            end = LrTransform.InverseTransformPoint(Config.GetPoint(cylinderIndex + 1));
        }

       // public Vector3 GetVertex(int index) => _vertices[index];

        /// <summary>
        /// Applies the mesh data to the given mesh.
        /// </summary>
        public void ApplayDataToMesh(ref Mesh mesh)
        {
            mesh.Clear();
            List<Vector3> vertices = new();
            List<Vector3> normals = new();
            List<Vector2> uvs = new();
            List<int> triangles = new();
            foreach (var segment in _segmentInfos)
            {
                vertices.AddRange(segment.vertices);
                normals.AddRange(segment.normals);
                uvs.AddRange(segment.uvs);
                triangles.AddRange(segment.triangles);
            }
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.normals = normals.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateBounds();
            Config.ClearDirtyFlags();
        }

        public void GetMeshData(out List<SegmentInfo> segmentInfos, out List<Vector3> vertices, out List<Vector3> normals, out List<Vector2> uvs, out List<int> triangles)
        {
            segmentInfos = _segmentInfos;
            vertices = new();
            normals = new();
            uvs = new();
            triangles = new();
            foreach (var segment in _segmentInfos)
            {
                vertices.AddRange(segment.vertices);
                normals.AddRange(segment.normals);
                uvs.AddRange(segment.uvs);
                triangles.AddRange(segment.triangles);
            }
        }

        #endregion
    }
}
