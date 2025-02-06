using NUnit.Framework.Constraints;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using static Unity.Mathematics.math;

namespace LineRenderer3D
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    [ExecuteAlways]
    class LineRenderer3D : MonoBehaviour
    {
        [SerializeField]
        List<Vector3> points = new();
        private void Start()
        {
            GenerateLineRenderer();
        }

        [ContextMenu("Generate Mesh")]
        void GenerateLineRenderer()
        {
            Debug.Log("Mesh being generated");
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            meshFilter.mesh = GenerateAdvancedMultiStreamMesh();
        }
        Mesh GenerateSimpleMesh()
        {
            Mesh mesh = new();
            mesh.name = "3D Line renderer";
            // Local vetices location on imaginary map 
            mesh.vertices = new Vector3[]
            {
                Vector3.zero,Vector3.up,Vector3.right,Vector3.one
            };
            // Texture coordinates 0-1f in Vector2 space
            mesh.uv = new Vector2[]
            {
                Vector2.zero, Vector2.up, Vector2.right, Vector3.one
            };
            // These are indexes of vertices, they must be cloakwise to face front
            mesh.triangles = new int[]
            {
                0,1,2, //zero, up, right
                1,3,2  //up, one , right
            };
            // Tells how mesh should should interact with light range (-1f,1f)  
            mesh.normals = new Vector3[] {
                Vector3.back, Vector3.back, Vector3.back, Vector3.back
            };
            // Light corrector, this is for conversion of texture space into wolrd space
            mesh.tangents = new Vector4[] {
                new (1f, 0f, 0f, -1f),
                new (1f, 0f, 0f, -1f),
                new (1f, 0f, 0f, -1f),
                new (1f, 0f, 0f, -1f),
            };

            

            return mesh;
        }
        Mesh GenerateAdvancedMultiStreamMesh()
        {
            int vertexAtributeCount = 4;
            int vertexCount = 4;
            int triangleIndexCount = 6;
            Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
            // Get first data
            Mesh.MeshData meshData = meshDataArray[0];
            // Setup stage

            //Skip memory initialization by using NativeArrayOptions.UninitializedMemory, avoiding Unity's default zero-fill.
            NativeArray<VertexAttributeDescriptor> vertexAttributes = new(vertexAtributeCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            // Position, float high precision, XYZ = 3 dimentions, 1st memory spot
            vertexAttributes[0] = new(VertexAttribute.Position, VertexAttributeFormat.Float32 ,dimension: 3, stream: 0);
            // Normal, float high precision, XYZ = 3 dimentions, 2nd memory spot
            vertexAttributes[1] = new(VertexAttribute.Normal, VertexAttributeFormat.Float32 ,dimension: 3, stream: 1);
            // Tangent, float high precision, XYZW = 4 dimentions, 3rd memory spot
            vertexAttributes[2] = new(VertexAttribute.Tangent, VertexAttributeFormat.Float16, dimension: 4, stream: 2);
            // UV, float high precision, XY = 2 dimentions, 4th memory spot
            vertexAttributes[3] = new(VertexAttribute.TexCoord0, VertexAttributeFormat.Float16, dimension: 2, stream: 3);

            meshData.SetVertexBufferParams(vertexCount, vertexAttributes);
            vertexAttributes.Dispose();

            // Allocation Stage
            NativeArray<float3> positions = meshData.GetVertexData<float3>(stream: 0);
            positions[0] = 0f; // Its shorten for new float3(0f,0f,0f)
            positions[1] = up();
            positions[2] = new float3(1f,1f,0f);
            positions[3] = right();

            NativeArray<float3> normals = meshData.GetVertexData<float3>(stream: 1);
            normals[0] = normals[1] = normals[2] = normals[3] = back();

            NativeArray<half4> tangets = meshData.GetVertexData<half4>(stream: 2);
            tangets[0] = tangets[1] = tangets[2] = tangets[3] = new half4(half(1), half(0), half(0), half(-1));

            NativeArray<half2> texCoords = meshData.GetVertexData<half2>(stream: 3);
            texCoords[0] = half(0); // Its shorten for new float2(0f,0f)
            texCoords[1] = new half2(half(0), half(1f));
            texCoords[2] = half(1f); // Its shorten for new float2(1f,1f)
            texCoords[3] = new half2(half(1f), half(0));

            meshData.SetIndexBufferParams(triangleIndexCount, IndexFormat.UInt16);
            NativeArray<ushort> trianglesIndeces = meshData.GetIndexData<ushort>();
            trianglesIndeces[0] = 0; //zero (left-bottom)
            trianglesIndeces[1] = 1; //up
            trianglesIndeces[2] = 3; //right
            trianglesIndeces[3] = 1; //up
            trianglesIndeces[4] = 2; //one (right-up)
            trianglesIndeces[5] = 3; //right

            meshData.subMeshCount = 1;
            var bounds = new Bounds(new Vector3(0.5f, 0.5f), new Vector3(1f, 1f));
            meshData.SetSubMesh(0, new SubMeshDescriptor(0, triangleIndexCount)
            {
                bounds = bounds,
                vertexCount = vertexCount
            }, MeshUpdateFlags.DontRecalculateBounds);
            Mesh mesh = new()
            {
                bounds = bounds,
                name = "3D Line renderer multi stream"
            };

            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);
            return mesh;
        }
        private void OnValidate()
        {
            GenerateLineRenderer();
        }

        private void OnDrawGizmos()
        {
            foreach (var point in points)
                Gizmos.DrawSphere(point, 0.1f);
        }
    }
}
