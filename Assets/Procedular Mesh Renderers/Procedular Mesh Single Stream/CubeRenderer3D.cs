using UnityEngine;

namespace ProcedularMesh.SStream
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    [ExecuteAlways]
    class CubeRenderer3D : MonoBehaviour
    {
        Mesh _mesh;
        [SerializeField]
        bool visualizeNormals = false;

        void SetupMesh()
        {
            _mesh = new Mesh
            {
                name = "Procedural Mesh"
            };
            GetComponent<MeshFilter>().mesh = _mesh;
        }
        private void Awake()
        {
            SetupMesh();
            GenerateMesh();
        }
        
        Vector3[] GetVertices()
        {
            Vector3 a = new(0, 0, 0);
            Vector3 b = new(0, 1, 0);
            Vector3 c = new(1, 1, 0);
            Vector3 d = new(1, 0, 0);
            Vector3 e = new(0, 1, 1);
            Vector3 f = new(1, 1, 1);
            Vector3 g = new(1, 0, 1);
            Vector3 h = new(0, 0, 1);

            return new Vector3[]{
                a,b,c,d, //front
                d,c,f,g, //right
                a,b,e,h, //left
                a,h,g,d, //bottom
                h,e,f,g,  //back
                b,e,f,c  //up
            };
        }
        Vector3[] GetNormals()
        {
            var b = Vector3.back;
            var f = Vector3.forward;
            var l = Vector3.left;
            var r = Vector3.right;
            var t = Vector3.up;
            var u = Vector3.down;

            return new Vector3[]{
                b,b,b,b, //front
                r,r,r,r, //right
                l,l,l,l, //left
                u,u,u,u, //bottom 
                f,f,f,f,  //back
                t,t,t,t  //up
            };
        }

        Vector2[] GetUvs()
        {
            return new Vector2[]
            {
                 new(0,0), new(0,1), new(1,1), new(1,0), //front
                 new(0,0), new(0,1), new(1,1), new(1,0), //right
                 new(1,0), new(1,1), new(0,1), new(0,0), //left
                 new(1,0), new(1,1), new(0,1), new(0,0), //bottom
                 new(1,0), new(1,1), new(0,1), new(0,0), //back
                 new(0,0), new(0,1), new(1,1), new(1,0) //up
            };
        }
        /*
               E--------F
              /        /|
             /        / |
            B--------C  |
            |        |  |
            |   H    |  G
            |  /     | /
            | /      |/
            A--------D
        */
        int[] GetTriangles()
        {

            return new int[]
            {
                0,1,2,0,2,3, //front
                4,5,6,4,6,7, //right
                8,10,9,8,11,10, //left
                12,14,13,12,15,14, //bottom
                16,18,17,16,19,18,  //back
                20,21,22,20,22,23, //up
            };
        }
        [ContextMenu(nameof(GenerateMesh))]
        void GenerateMesh()
        {
            _mesh.Clear();



            _mesh.vertices = GetVertices();

            _mesh.uv = GetUvs();

            _mesh.normals = GetNormals();


            _mesh.triangles = GetTriangles();


            _mesh.RecalculateBounds();
        }

        private void OnValidate()
        {
            if (_mesh == null)
                SetupMesh();
            GenerateMesh();
        }
        private void OnDrawGizmos()
        {
            if (visualizeNormals)
                for (int i = 0; i < _mesh.vertices.Length; i++)
                    Gizmos.DrawRay(transform.position + _mesh.vertices[i], _mesh.normals[i]);
        }
    }
}
