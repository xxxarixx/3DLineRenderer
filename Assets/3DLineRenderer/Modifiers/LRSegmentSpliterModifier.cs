using LineRenderer3D.Modifiers;
using System.Collections.Generic;
using UnityEngine;

namespace LineRenderer3D
{
    class LRSegmentSpliterModifier : MonoBehaviour, IModifierBase
    {
        public string Name => ToString();

        public void ManipulateMesh(LRCylinder3D lr, List<LRCylinder3D.SegmentInfo> segmentInfos, ref List<Vector3> vertices, ref List<Vector3> normals, ref List<Vector2> uvs, ref List<int> triangles)
        {

        }
    }
}
