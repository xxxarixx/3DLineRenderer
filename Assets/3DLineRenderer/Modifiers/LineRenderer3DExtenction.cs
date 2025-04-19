using UnityEngine;
using System.Collections.Generic;
using LineRenderer3D.Datas;
using static LineRenderer3D.Datas.LRData;
using static Unity.Mathematics.math;

namespace LineRenderer3D.Mods
{
    internal static class LineRenderer3DExtenction
    {
        internal static string[] SplitSegment(ref List<Vector3> vertices, string idToSplit, LRData data, ref List<SegmentInfo> segmentInfos, bool isItBeggining)
        {
            var newIds = new string[2];
            var segmentIndex = segmentInfos.FindIndex(x => x.uniqueId == idToSplit);
            var segment = segmentInfos[segmentIndex];
            Vector3 oldEndSegmentCenter = segment.endSegmentCenter;
            Vector3 halfWayCenter = Vector3.Lerp(segment.startSegmentCenter, segment.endSegmentCenter, 0.5f);
            segment.endSegmentCenter = halfWayCenter;
            for (int i = 0; i < segment.endSegmentVericesIndex.Count; i++)
            {
                Vector3 startVertice = vertices[segment.startSegmentVericesIndex[i]];
                Vector3 endVertice = vertices[segment.endSegmentVericesIndex[i]];
                Vector3 halfWay = Vector3.Lerp(startVertice, endVertice, 0.5f);
                vertices[segment.endSegmentVericesIndex[i]] = halfWay;
            }
            data.GenerateCylinder(start: halfWayCenter,
                                  end: oldEndSegmentCenter,
                                  segmentInfos.Count,
                                  flipUV: false);
            var newSegment = data.GenerateSegmentInfo(start: halfWayCenter,
                                                    end: oldEndSegmentCenter,
                                                    cylinderIndex: segmentInfos.Count);
            segmentInfos.Insert(isItBeggining ? segmentIndex : segmentIndex + 1, newSegment);
            newIds[0] = segment.uniqueId;
            newIds[1] = newSegment.uniqueId;
            return newIds;
        }

        internal static float GetAngleBetweenVectors(Vector3 a, Vector3 b, Vector3 c)
        {
            // Vectors from B to A and B to C
            Vector3 AB = (a - b).normalized;
            Vector3 BC = (c - b).normalized;

            // Calculate the angle between the vectors
            float angle = Vector3.Angle(AB, BC);

            return angle;
        }

        internal static ModInfo GenerateSphereCap(LRData data, int startVerticeIndex, SegmentInfo segment, bool isStart)
        {
            ModInfo currentMod = new();

            int numberOfFaces = data.Config.NumberOfFaces;
            float radius = data.Config.Radius;
            Vector3 center = isStart ? segment.startSegmentCenter : segment.endSegmentCenter;
            center = data.LrTransform.InverseTransformPoint(center);
            Vector3 direction = isStart ? (segment.startSegmentCenter - segment.endSegmentCenter).normalized :
                                         (segment.endSegmentCenter - segment.startSegmentCenter).normalized;

            Quaternion correctionRotation = Quaternion.Euler(0, 0, isStart ? 90f : -90f);
            Quaternion rotation = Quaternion.LookRotation(direction) * correctionRotation;

            int _rings = numberOfFaces / 2;
            int _segments = numberOfFaces / 2;

            float deltaTheta = PI / _rings;
            float deltaPhi = PI / _segments;

            for (int ring = 0; ring <= _rings; ring++)
            {
                float theta = ring * deltaTheta;
                float sinTheta = sin(theta);
                float cosTheta = cos(theta);

                for (int s = 0; s <= _segments; s++)
                {
                    float phi = s * deltaPhi;
                    float sinPhi = sin(phi);
                    float cosPhi = cos(phi);

                    float x = radius * sinTheta * cosPhi;
                    float y = radius * cosTheta;
                    float z = radius * sinTheta * sinPhi;

                    Vector3 vertex = center + rotation * new Vector3(x, y, z);
                    currentMod.Vertices.Add(vertex);
                    currentMod.Normals.Add(normalize(vertex - center));
                    currentMod.Uvs.Add(correctionRotation * new Vector2((float)s / _segments, isStart ? 1f - (float)ring / _rings : (float)ring / _rings));
/*
                    // second half of vertices
                    if (ring != 0 && ring != _rings && s == _segments)
                    {
                        ringEdgeVertexes.Add(vertex);
                    }
                    // first half of vertices
                    if (s == 0)
                    {
                        ringEdgeVertexes.Add(vertex);
                    }*/
                }
            }
            Debug.Log($"is start: {isStart}, verticeCount: {startVerticeIndex}");
            // Triangles
            for (int ring = 0; ring < _rings; ring++)
            {
                for (int s = 0; s < _segments; s++)
                {
                    int current = startVerticeIndex + ring * (_segments + 1) + s;
                    int next = current + _segments + 1;

                    currentMod.Triangles.Add(current);
                    currentMod.Triangles.Add(current + 1);
                    currentMod.Triangles.Add(next);

                    currentMod.Triangles.Add(next);
                    currentMod.Triangles.Add(current + 1);
                    currentMod.Triangles.Add(next + 1);
                }
            }

            return currentMod;
        }

        internal static List<int> GenerateSphereCapTringles(LRData data, int startVerticeIndex)
        {
            List<int> trinagles = new List<int>();
            int numberOfFaces = data.Config.NumberOfFaces;

            int _rings = numberOfFaces / 2;
            int _segments = numberOfFaces / 2;
            
            // Triangles
            for (int ring = 0; ring < _rings; ring++)
            {
                for (int s = 0; s < _segments; s++)
                {
                    int current = startVerticeIndex + ring * (_segments + 1) + s;
                    int next = current + _segments + 1;

                    trinagles.Add(current);
                    trinagles.Add(current + 1);
                    trinagles.Add(next);

                    trinagles.Add(next);
                    trinagles.Add(current + 1);
                    trinagles.Add(next + 1);
                }
            }

            return trinagles;
        }
    }
}
