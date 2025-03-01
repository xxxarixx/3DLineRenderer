using System;
using System.Collections.Generic;
using UnityEngine;
using LineRenderer3D.Modifiers;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LineRenderer3D
{
    [ExecuteAlways]
    class LRCylinder3D : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("needed to be even number, in order to uv be properly generated")]
        internal int numberOfFaces = 8;

        [SerializeField]
        internal float radius = 0.1f;

        [SerializeField]
        internal List<Vector3> points = new();

        [SerializeField]
        List<DebugGizmos> pointsDebug = new();

        [SerializeField]
        List<Vector3> vertices;

        [SerializeField]
        List<Vector3> normals;

        [SerializeField]
        List<Vector2> uv;

        [SerializeField]
        List<int> triangles;

        [SerializeField]
        bool stopRegeneration;

        [SerializeField]
        bool regenerateBasedOnCurrentValues;

        [SerializeField]
        bool visualizeAllVertices;

        [SerializeField]
        float vertexGizmosSize = 0.1f;

        [Flags]
        enum DebugGizmos
        {
            None = 0,
            Vertices = 1,
            Normals = 0b10,
            SegmentsInfo = 0b100,
            UV = 0b1000,
        }

        Mesh mesh;
        MeshFilter meshFilter;

        void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            mesh = new Mesh();
            meshFilter.mesh = mesh;

            UpdateLine();
        }

        void UpdateLine()
        {
            GenerateMesh();
        }


        [Serializable]
        internal class SegmentInfo
        {
            [SerializeField]
            internal Vector3 startSegmentCenter;
            internal readonly Vector3 initStartSegmentCenter;
            internal List<int> startSegmentVericesIndex = new();
            [SerializeField]
            internal Vector3 endSegmentCenter;
            internal readonly Vector3 initEndSegmentCenter;
            internal List<int> endSegmentVericesIndex = new();

            internal SegmentInfo(Vector3 startSegmentCenter, Vector3 endSegmentCenter)
            {
                initStartSegmentCenter = startSegmentCenter;
                initEndSegmentCenter = endSegmentCenter;
                this.startSegmentCenter = startSegmentCenter;
                this.endSegmentCenter = endSegmentCenter;
            }

        }

        [SerializeField]
        List<SegmentInfo> segmentInfos;
        void GenerateMesh()
        {
            if(regenerateBasedOnCurrentValues)
            {
                mesh.Clear();
                mesh.vertices = vertices.ToArray();
                mesh.triangles = triangles.ToArray();
                mesh.normals = normals.ToArray();
                mesh.uv = uv.ToArray();
                mesh.RecalculateBounds();
                if (meshFilter != null)
                    meshFilter.mesh = mesh;
                return;
            }
            if (stopRegeneration)
                return;
            if(mesh == null)
            {
                mesh = new Mesh();
                meshFilter.mesh = mesh;
            }
            if (points.Count < 2)
            {
                mesh.Clear();
                return;
            }

            if (numberOfFaces % 2 != 0)
                numberOfFaces++;

            vertices = new List<Vector3>();
            triangles = new List<int>();
            normals = new List<Vector3>();
            uv = new List<Vector2>();
            segmentInfos = new();

            //Setup segments info
            for (int s = 0; s < points.Count - 1; s++)
            {
                var segment = GenerateSegmentInfo(start: transform.InverseTransformPoint(points[s]),
                                                  end: transform.InverseTransformPoint(points[s + 1]),
                                                  cylinderIndex: s,
                                                  cylinderMaxCount: points.Count);
                if (segment == null)
                    continue;
                segmentInfos.Add(segment);
            }

            //Generate cylinders
            for (int s = 0; s < points.Count - 1; s++)
                GenerateCylinder(start: transform.InverseTransformPoint(points[s]), 
                                 end: transform.InverseTransformPoint(points[s + 1]), 
                                 cylinderIndex: s,
                                 flipUV: false);

            foreach (var mod in GetComponents<IModifierBase>())
                if(mod.IsEnabled)
                    mod.ManipulateMesh(this, ref segmentInfos, ref vertices, ref normals, ref uv, ref triangles);
                

            mesh.Clear();
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.normals = normals.ToArray();
            mesh.uv = uv.ToArray();
            mesh.RecalculateBounds();
            if(meshFilter != null)
                meshFilter.mesh = mesh;
        }
        
        internal void GenerateCylinder(Vector3 start, Vector3 end, int cylinderIndex, bool flipUV)
        {
            Vector3 direction = (end - start).normalized;

            if (direction == Vector3.zero) return;

            Quaternion rotation = Quaternion.LookRotation(direction);

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
                Vector3 directionToEnd = (endVert - startVert).normalized;

                //startVert = cylinderIndex > 0 && canMakeCorner ? startVert - (-directionToEnd * distance) : startVert;
                //endVert = cylinderIndex < segmentInfos.Count - 1 && canMakeCorner ? endVert - directionToEnd * distance : endVert;


                vertices.Add(startVert);
                vertices.Add(endVert);

                // Normals point outward from cylinder center
                Vector3 normal = rotation * circleOffset.normalized;
                normals.Add(normal);
                normals.Add(normal);

                // UV mapping
                if(f > numberOfFaces / 2)
                {
                    uv.Add(new Vector2(2f - (1f / numberOfFaces * f) * 2f, flipUV ? 1 : 0));
                    uv.Add(new Vector2(2f - (1f / numberOfFaces * f) * 2f, flipUV ? 0 : 1));
                }
                else
                {
                    uv.Add(new Vector2((1f / numberOfFaces * f) * 2f, flipUV ? 1 : 0));
                    uv.Add(new Vector2((1f / numberOfFaces * f) * 2f, flipUV ? 0 : 1));
                }
            }

            // Generate triangles for this segment
            int baseIndex = cylinderIndex * numberOfFaces * 2;
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

        internal void ChangeSegmentVerticesLocation(List<int> verticeIndexes, Vector3 translation, int segmentID, bool isStart, bool shouldUpdateUV)
        {
            foreach (int index in verticeIndexes)
                vertices[index] += translation;
            if(shouldUpdateUV)
                UpdateUVForVertices(verticeIndexes, segmentID, isStart);
        }

        internal void UpdateUVForVertices(List<int> verticeIndexes, int segmentID, bool isStart)
        {
            for (int i = 0; i < verticeIndexes.Count; i++)
            {
                int vertexIndex = verticeIndexes[i];
                int faceIndex = i % numberOfFaces;

                // Calc new UV based on segment and position
                float u = CalculateU(faceIndex, numberOfFaces);
                float v = isStart ? 0f : 1.5f;

                uv[vertexIndex] = new Vector2(u, v);
            }
        }

        float CalculateU(int faceIndex, int numberOfFaces)
        {
            if (faceIndex > numberOfFaces / 2)
                return 2f - ((float)faceIndex / numberOfFaces) * 2f;
            else
                return ((float)faceIndex / numberOfFaces) * 2f;
        }

        internal SegmentInfo GenerateSegmentInfo(Vector3 start, Vector3 end, int cylinderIndex, int cylinderMaxCount)
        {
            Vector3 direction = (end - start).normalized;
            if (direction == Vector3.zero) return null;

            Quaternion rotation = Quaternion.LookRotation(direction);
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

            SegmentInfo segmentInfo = new(transform.TransformPoint(startCenter), transform.TransformPoint(endCenter))
            {
                startSegmentVericesIndex = startSegmentVericesIndex,
                endSegmentVericesIndex = endSegmentVericesIndex,
            };
            return segmentInfo;
        }

        internal bool IsCylinderIndexValid(int cylinderIndex) => cylinderIndex >= 0 && cylinderIndex < segmentInfos.Count;

        void Update()
        {
            UpdateLine();
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            foreach (Vector3 point in points)
                Gizmos.DrawWireSphere(point, .1f);

            DebugGizmoses();
        }
        void DebugGizmoses()
        {
            Gizmos.color = Color.red;
            if (visualizeAllVertices)
                foreach (var vertice in vertices)
                    Gizmos.DrawSphere(transform.TransformPoint(vertice), vertexGizmosSize);

            for (int p = 0; p < pointsDebug.Count; p++)
            {
                var debugGizmos = pointsDebug[p];
                if (debugGizmos == DebugGizmos.None)
                    continue;
                if (!IsCylinderIndexValid(p))
                    continue;
                var segmentInfo = segmentInfos[p];
                bool visualizeVertices = (debugGizmos & DebugGizmos.Vertices) != 0;
                bool visualizeNormals = (debugGizmos & DebugGizmos.Normals) != 0;
                bool visualizeSegmentsInfo = (debugGizmos & DebugGizmos.SegmentsInfo) != 0;
                bool visualizeUV = (debugGizmos & DebugGizmos.UV) != 0;

                if (visualizeNormals)
                {
                    Gizmos.color = Color.blue;
                    for (int i = 0; i < normals.Count; i++)
                    {
                        Vector3 normal = normals[i];
                        Vector3 verice = transform.TransformPoint(vertices[i]);
                        Gizmos.DrawRay(verice, normal);
                    }
                }
                Gizmos.color = Color.red;
                if(visualizeVertices)
                {
                    for (int i = 0; i < segmentInfo.startSegmentVericesIndex.Count; i++)
                    {
                        var index = segmentInfo.startSegmentVericesIndex[i];
                        Vector3 pos = transform.TransformPoint(vertices[index]);
                        Gizmos.DrawSphere(pos, vertexGizmosSize);
                    }
                    for (int i = 0; i < segmentInfo.endSegmentVericesIndex.Count; i++)
                    {
                        var index = segmentInfo.endSegmentVericesIndex[i];
                        Vector3 pos = transform.TransformPoint(vertices[index]);
                        Gizmos.DrawSphere(pos, vertexGizmosSize);
                    } 
                }
                
                if (visualizeSegmentsInfo)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere((segmentInfo.startSegmentCenter), vertexGizmosSize);
                    for (int i = 0; i < segmentInfo.startSegmentVericesIndex.Count; i++)
                        Gizmos.DrawWireSphere(transform.TransformPoint(vertices[segmentInfo.startSegmentVericesIndex[i]]), vertexGizmosSize / 2);

                    Gizmos.color = Color.magenta;
                    Gizmos.DrawWireSphere((segmentInfo.endSegmentCenter), vertexGizmosSize + vertexGizmosSize / 2);
                    for (int i = 0; i < segmentInfo.endSegmentVericesIndex.Count; i++)
                        Gizmos.DrawWireSphere(transform.TransformPoint(vertices[segmentInfo.endSegmentVericesIndex[i]]), vertexGizmosSize);

                    Gizmos.color = Color.blue;
                    Gizmos.DrawCube(transform.TransformPoint(segmentInfo.initStartSegmentCenter), Vector3.one * vertexGizmosSize);
                    Gizmos.color = Color.white;
                    Gizmos.DrawCube(transform.TransformPoint(segmentInfo.initEndSegmentCenter), Vector3.one * vertexGizmosSize);
                }
#if UNITY_EDITOR
                if (uv != null && visualizeUV)
                {
                    for (int i = 0; i < segmentInfo.startSegmentVericesIndex.Count; i++)
                    {
                        var index = segmentInfo.startSegmentVericesIndex[i];
                        Vector3 pos = transform.TransformPoint(vertices[index]);
                        Handles.Label(pos, $"U: {uv[index].x:F2}\nV: {uv[index].y:F2}");
                    }
                    for (int i = 0; i < segmentInfo.endSegmentVericesIndex.Count; i++)
                    {
                        var index = segmentInfo.endSegmentVericesIndex[i];
                        Vector3 pos = transform.TransformPoint(vertices[index]);
                        Handles.Label(pos, $"U: {uv[index].x:F2}\nV: {uv[index].y:F2}");
                    }
                }
#endif


            }

        }
    }
}
