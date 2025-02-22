using System;
using System.Collections.Generic;
using UnityEngine;
using static Unity.Mathematics.math;

namespace LineRenderer3D
{
    [ExecuteAlways]
    class LRCylinder3D : MonoBehaviour
    {
        [SerializeField]
        int numberOfFaces = 8;

        [SerializeField]
        float radius = 0.1f;

        [SerializeField]
        List<Vector3> points = new();

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
        bool stopRegeneration = false;

        [SerializeField]
        float vertexGizmosSize = 0.1f;

        [Flags]
        enum DebugGizmos
        {
            None = 0,
            Vertices = 1,
            Normals = 0b10,
            SegmentsInfo = 0b100
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

        [SerializeField]
        float distance = 1f;

        [SerializeField]
        [Range(0.5f, 2f)]
        float distanceControlPointMultiplayer;

        [SerializeField]
        int pointsPerCurve = 5;


        struct SegmentInfo
        {
            internal Vector3 startSegmentCenter;
            internal List<int> startSegmentVericesIndex;
            internal Vector3 endSegmentCenter;
            internal List<int> endSegmentVericesIndex;
        }
        List<SegmentInfo> segmentInfos;
        void GenerateMesh()
        {
            if (stopRegeneration)
                return;
            if(mesh == null)
            {
                meshFilter = GetComponent<MeshFilter>();
                mesh = new Mesh();
                meshFilter.mesh = mesh;
            }
            if (points.Count < 2)
            {
                mesh.Clear();
                return;
            }

            vertices = new List<Vector3>();
            triangles = new List<int>();
            normals = new List<Vector3>();
            uv = new List<Vector2>();
            segmentInfos = new();

            //Setup segments info
            for (int s = 0; s < points.Count - 1; s++)
            {
                Vector3 start = transform.InverseTransformPoint(points[s]);
                Vector3 end = transform.InverseTransformPoint(points[s + 1]);
                Vector3 direction = (end - start).normalized;
                if (direction == Vector3.zero) continue;

                Quaternion rotation = Quaternion.LookRotation(direction);
                Vector3 directionToEnd = (end - start).normalized;
                Vector3 startCenter = start + rotation * Vector3.zero;
                Vector3 endCenter = end + rotation * Vector3.zero;
                startCenter = (s > 0) ? startCenter - (-directionToEnd * distance) : startCenter;
                endCenter = (s < points.Count - 2) ? endCenter - directionToEnd * distance : endCenter;
                List<int> startSegmentVericesIndex = new();
                List<int> endSegmentVericesIndex = new();

                int baseIndex = s * numberOfFaces * 2;
                int baseNextIndex = (s + 1) * numberOfFaces * 2;
                for (int i = 0; i < numberOfFaces; i++)
                {
                    int current = baseIndex + i * 2;
                    int next = baseIndex + ((i + 1) % numberOfFaces) * 2;

                    int nextSegmentCurrent = baseNextIndex + i * 2;
                    int nextSegmentNext = baseNextIndex + ((i + 1) % numberOfFaces) * 2;

                    if (s < points.Count - 1)
                    {
                        // First connection triangle (current segment end to next segment start)
                        startSegmentVericesIndex.Add(current + 1);
                        endSegmentVericesIndex.Add(next + 1);
                        startSegmentVericesIndex.Add(nextSegmentCurrent);

                        // Second connection triangle (next segment start to next segment end)
                        endSegmentVericesIndex.Add(next + 1);
                        endSegmentVericesIndex.Add(nextSegmentNext);
                        startSegmentVericesIndex.Add(nextSegmentCurrent);
                    }
                }

                SegmentInfo segmentInfo = new() 
                {
                    startSegmentCenter = transform.TransformPoint(startCenter),
                    endSegmentCenter = transform.TransformPoint(endCenter),
                    startSegmentVericesIndex = startSegmentVericesIndex,
                    endSegmentVericesIndex = endSegmentVericesIndex
                };
                segmentInfos.Add(segmentInfo);
            }

            //Generate cylinders
            for (int s = 0; s < points.Count - 1; s++)
            {
                Vector3 start = transform.InverseTransformPoint(points[s]);
                Vector3 end = transform.InverseTransformPoint(points[s + 1]);
                Vector3 direction = (end - start).normalized;

                if (direction == Vector3.zero) continue;

                Quaternion rotation = Quaternion.LookRotation(direction);

                // Generate vertices for this segment
                for (int i = 0; i < numberOfFaces; i++)
                {
                    float theta = Mathf.PI * 2 * i / numberOfFaces;
                    Vector3 circleOffset = new Vector3(
                        Mathf.Cos(theta) * radius,
                        Mathf.Sin(theta) * radius,
                        0
                    );

                    // Calculate positions in local space
                    Vector3 startVert = start + rotation * circleOffset;
                    Vector3 endVert = end + rotation * circleOffset;
                    Vector3 directionToEnd = (endVert - startVert).normalized;
                    startVert = (s > 0) ? startVert - (-directionToEnd * distance) : startVert;
                    endVert = (s < points.Count - 2) ? endVert - directionToEnd * distance : endVert;

                    vertices.Add(startVert);
                    vertices.Add(endVert);

                    // Normals point outward from cylinder center
                    Vector3 normal = rotation * circleOffset.normalized;
                    normals.Add(normal);
                    normals.Add(normal);

                    // UV mapping
                    uv.Add(new Vector2((float)i / numberOfFaces, 0));
                    uv.Add(new Vector2((float)i / numberOfFaces, 1));


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

            //Generate connection curve
            for (int s = 0; s < points.Count - 1; s++)
            {
                if (s > 0 && points.Count > 1)
                {
                    Vector3 A = points[s - 1];
                    Vector3 B = points[s];
                    Vector3 C = points[s + 1];

                    Vector3 dirToA = (A - B).normalized;
                    Vector3 dirToC = (C - B).normalized;

                    Vector3 inBetweenDir = normalize(-lerp(dirToA, dirToC, 0.5f));
                    Debug.DrawRay(B, dirToA, Color.black, 2f);
                    Debug.DrawRay(B, dirToC, Color.blue, 2f);
                    Debug.DrawRay(B, inBetweenDir, Color.green, 2f);
                    Vector3 helpControlPoint = B + inBetweenDir * (distance * distanceControlPointMultiplayer);
                    
                    var currentSegment = segmentInfos[s];
                    var previousSegment = segmentInfos[s - 1];

                    //get end vertices of segment
                    //get start vertices of segment
                    for (int i = 0; i < segmentInfos[s].startSegmentVericesIndex.Count; i++)
                    {
                        for (int p = 1; p < pointsPerCurve; p++)
                        {
                            vertices.Add(transform.InverseTransformPoint(QuadraticBezier(previousSegment.endSegmentCenter, helpControlPoint, currentSegment.startSegmentCenter, p / (float)pointsPerCurve)));
                            normals.Add(inBetweenDir);
                            uv.Add(new Vector2(0, 0));
                        }
                    }
                }
            }

            mesh.Clear();
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.normals = normals.ToArray();
            mesh.uv = uv.ToArray();
            mesh.RecalculateBounds();
            if(meshFilter != null)
                meshFilter.mesh = mesh;
        }

        Vector3 QuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            float u = 1 - t;
            return (u * u) * p0 + (2 * u * t) * p1 + (t * t) * p2;
        }

        void Update()
        {
            UpdateLine();
        }
        // Update mesh when values change in inspector
        void OnValidate()
        {
            if (meshFilter != null && points.Count > 1)
                GenerateMesh();

            pointsDebug.Capacity = points.Capacity;
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
            for (int p = 0; p < pointsDebug.Count; p++)
            {
                var debugGizmos = pointsDebug[p];
                if (debugGizmos == DebugGizmos.None)
                    continue;
                var segmentInfo = segmentInfos[p];
                bool visualizeVertices = (debugGizmos & DebugGizmos.Vertices) != 0;
                bool visualizeNormals = (debugGizmos & DebugGizmos.Normals) != 0;
                bool visualizeSegmentsInfo = (debugGizmos & DebugGizmos.SegmentsInfo) != 0;
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
                if(visualizeVertices)
                {
                    Gizmos.color = Color.red;
                    foreach (var vertice in vertices)
                        Gizmos.DrawSphere(transform.TransformPoint(vertice), vertexGizmosSize);
                }
                if(visualizeSegmentsInfo)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere((segmentInfo.startSegmentCenter), vertexGizmosSize);
                    for (int i = 0; i < segmentInfo.startSegmentVericesIndex.Count; i++)
                        Gizmos.DrawWireSphere(transform.TransformPoint(vertices[segmentInfo.startSegmentVericesIndex[i]]), vertexGizmosSize / 2);
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawWireSphere((segmentInfo.endSegmentCenter), vertexGizmosSize + vertexGizmosSize / 2);
                    for (int i = 0; i < segmentInfo.endSegmentVericesIndex.Count; i++)
                        Gizmos.DrawWireSphere(transform.TransformPoint(vertices[segmentInfo.endSegmentVericesIndex[i]]), vertexGizmosSize);
                }

                    
            }
        }
    }
}
