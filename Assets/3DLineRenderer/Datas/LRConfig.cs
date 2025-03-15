using System.Collections.Generic;
using UnityEngine;

namespace LinerRenderer3D.Datas
{
    [CreateAssetMenu(menuName = nameof(LRConfig), fileName = nameof(LRConfig))]
    public class LRConfig : ScriptableObject
    {
        [SerializeField]
        [Tooltip("needed to be even number, in order to uv be properly generated, it is handled automatically")]
        [Range(4, 20)]
        int _numberOfFaces = 8;

        /// <summary>
        /// needed to be even number, in order to uv be properly generated, it is handled automatically
        /// </summary>
        public int NumberOfFaces
        {
            get
            {
                int numberOfFaces = _numberOfFaces;
                if (numberOfFaces % 2 != 0)
                    numberOfFaces++;
                return numberOfFaces;
            }
        }

        [SerializeField]
        float _radius = 0.1f;

        /// <summary>
        /// The radius of the cylinder segments.
        /// </summary>
        public float Radius
        {
            get
            {
                return _radius;
            }
        }

        public List<Vector3> Points = new();
    }
}
