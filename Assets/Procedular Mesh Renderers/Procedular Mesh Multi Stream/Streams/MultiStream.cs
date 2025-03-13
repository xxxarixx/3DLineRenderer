using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProcedularMesh.MStream.Streams
{
    struct MultiStream : IMeshStreams
    {
        [NativeDisableContainerSafetyRestriction]
        NativeArray<float3> _positions;
        [NativeDisableContainerSafetyRestriction]
        NativeArray<float3> _normals;
        [NativeDisableContainerSafetyRestriction]
        NativeArray<half4> _tangents;
        [NativeDisableContainerSafetyRestriction]
        NativeArray<half2> _texCoords;
        [NativeDisableContainerSafetyRestriction]
        NativeArray<int3> _trianglesIndexes;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IMeshStreams.SetVertex(int index, MeshVertexData meshVertexData)
        {
            _positions[index] = meshVertexData.Position;
            _normals[index] = meshVertexData.Normal;
            _tangents[index] = meshVertexData.Tanget;
            _texCoords[index] = meshVertexData.TexCoords;
        }

        void IMeshStreams.SetupData(Mesh.MeshData meshData, Bounds bounds, int vertexCount, int triangleIndexCount)
        {
            int vertexAttributeCount = 4;
            int subMeshCount = 1;
            NativeArray<VertexAttributeDescriptor> vertexAttributes = new(vertexAttributeCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            // Position, float high precision, XYZ = 3 dimentions, 1st memory spot
            vertexAttributes[0] = new(VertexAttribute.Position, VertexAttributeFormat.Float32, dimension: 3, stream: 0);
            // Normal, float high precision, XYZ = 3 dimentions, 2nd memory spot
            vertexAttributes[1] = new(VertexAttribute.Normal, VertexAttributeFormat.Float32, dimension: 3, stream: 1);
            // Tangent, float high precision, XYZW = 4 dimentions, 3rd memory spot
            vertexAttributes[2] = new(VertexAttribute.Tangent, VertexAttributeFormat.Float16, dimension: 4, stream: 2);
            // UV, float high precision, XY = 2 dimentions, 4th memory spot
            vertexAttributes[3] = new(VertexAttribute.TexCoord0, VertexAttributeFormat.Float16, dimension: 2, stream: 3);

            meshData.SetVertexBufferParams(vertexCount, vertexAttributes);
            vertexAttributes.Dispose();

            _positions = meshData.GetVertexData<float3>(stream: 0);
            _normals = meshData.GetVertexData<float3>(stream: 1);
            _tangents = meshData.GetVertexData<half4>(stream: 2);
            _texCoords = meshData.GetVertexData<half2>(stream: 3);

            meshData.SetIndexBufferParams(triangleIndexCount, IndexFormat.UInt32);
            //4 because int3 has 4 bit size
            _trianglesIndexes = meshData.GetIndexData<uint>().Reinterpret<int3>(4); 

            meshData.subMeshCount = subMeshCount;
            meshData.SetSubMesh(0, new SubMeshDescriptor(0, triangleIndexCount)
            {
                vertexCount = vertexCount,
                bounds = bounds
            }, MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices);

        }
        public void SetTriangle(int index, int3 triangle) => _trianglesIndexes[index] = triangle;
    }
}
