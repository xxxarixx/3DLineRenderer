using ProcedularMesh.Streams;
using UnityEngine;

namespace ProcedularMesh
{
    interface IMeshGenerator
    {
        void Execute<S>(int i, S streams) where S : struct, IMeshStreams;

        int VertexCount { get; }

        int Resolution { get; set; }

        int VertexIndexCount { get; }

        Bounds Bounds { get; }

        int JobLength { get; }
    }
}
