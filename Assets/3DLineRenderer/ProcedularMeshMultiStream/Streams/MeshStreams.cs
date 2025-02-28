using Unity.Mathematics;
using UnityEngine;

namespace ProcedularMesh.Streams
{
    interface IMeshStreams 
    {
        void SetupData(Mesh.MeshData vertex, Bounds bounds, int vertexCount, int triangleIndexCount);

        void SetVertex(int index, MeshVertexData vertex);

        void SetTriangle(int index, int3 triangle);
    }
}
