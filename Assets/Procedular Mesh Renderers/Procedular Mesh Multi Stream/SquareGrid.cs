
using ProcedularMesh.MStream.Streams;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using static Unity.Mathematics.math;

namespace ProcedularMesh.MStream
{
    struct SquareGrid : IMeshGenerator
    {
        public int VertexCount => 4 * Resolution * Resolution;

        public int VertexIndexCount => 6 * Resolution * Resolution;

        public int JobLength => Resolution * Resolution;

        public Bounds Bounds => new(new Vector3(0.5f * Resolution, 0.5f * Resolution), new Vector3(1f * Resolution, 1f * Resolution));

        public int Resolution {get; set;}

        public void Execute<S>(int i, S streams) where S : struct, IMeshStreams
        {
            int vi = 4 * i, ti = 2 * i;
            int y = i / Resolution;
            int x = i - Resolution * y;
            var coordinates = float4(x, x + 0.9f, y, y + 0.9f);

            var vertex = new MeshVertexData();
            vertex.Normal.z = -1f;
            vertex.Tanget.xw = half2(half(1f), half(-1f));

            vertex.Position.xy = coordinates.xz;
            vertex.TexCoords = half(0);
            streams.SetVertex(vi + 0, vertex);

            vertex.Position.xy = coordinates.xw;
            vertex.TexCoords = half2(half(0), half(1f));
            streams.SetVertex(vi + 1, vertex);

            vertex.Position.xy = coordinates.yw;
            vertex.TexCoords = half(1f);
            streams.SetVertex(vi + 2, vertex);

            vertex.Position.xy = coordinates.yz;
            vertex.TexCoords = half2(half(1f), half(0));
            streams.SetVertex(vi + 3, vertex);

            streams.SetTriangle(ti + 0, vi + int3(0, 1, 3));
            streams.SetTriangle(ti + 1, vi + int3(1, 2, 3));
        }
    }

    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    internal struct MeshJob<Gen, Streams> : IJobFor
        where Gen : struct, IMeshGenerator
        where Streams : struct, IMeshStreams
    {
        Gen _generator;
        [WriteOnly]
        Streams _streams;
        public void Execute(int index) => _generator.Execute(index, _streams);

        internal static JobHandle ScheduleParallel(Mesh mesh, int resolution, Mesh.MeshData meshData, JobHandle dependency)
        {
            var job = new MeshJob<Gen, Streams>();
            job._generator.Resolution = resolution;
            mesh.bounds = job._generator.Bounds;
            job._streams.SetupData(meshData, job._generator.Bounds, job._generator.VertexCount, job._generator.VertexIndexCount);
            return job.ScheduleParallel(job._generator.JobLength, 1, dependency);
        }
    }

    
}
