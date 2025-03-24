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
        int _dirtyNumberOfFaces = 0;

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
        public enum DirtyFlag
        {
            ChangedPosition,
            Removed,
            Added
        }
        public float SegmentMinLength => 2 * Radius;

        List<Vector3> _points = new();

        [System.NonSerialized]
        public HashSet<(int, DirtyFlag)> DirtyPoints = new();

        [Header("Editor")]
        /// <summary>
        /// Threshold for displaying the buttons and move pivolts near the points
        /// </summary>  

        public float DisplayThreshold = 2f;


        private void OnValidate()
        {
            if(_dirtyNumberOfFaces != _numberOfFaces)
            {
                MarkAlPointsDirty();
                _dirtyNumberOfFaces = _numberOfFaces;
            }
            while(_points.Count < 2)
            {
                AddPoint(Vector3.zero);
            }
        }

        public void AddPoint(Vector3 location)
        {
            _points.Add(location);
            MarkPointDirty(_points.Count - 1, DirtyFlag.Added);
        }

        public void InsertPoint(int index, Vector3 location)
        {
            _points.Insert(index, location);
            MarkPointDirty(index, DirtyFlag.Added);
        }

        public void RemovePoint(int index)
        {
            if (_points.Count < 3)
                return;
            
            _points.RemoveAt(index);
            MarkPointDirty(index, DirtyFlag.Removed);
        }

        public void UpdatePointPosition(int pointIndex, Vector3 newLocation)
        {
            _points[pointIndex] = newLocation;
            MarkPointDirty(pointIndex, DirtyFlag.ChangedPosition);
        }

        public int PointsCount => _points.Count;

        public Vector3 GetPoint(int index) => _points[index];

        void MarkPointDirty(int index, DirtyFlag dirtyFlag)
        {
            

            if(dirtyFlag == DirtyFlag.ChangedPosition)
                DirtyPoints.Add((index, dirtyFlag));

            if (index > 0)
            {
                DirtyPoints.Add((index - 1, dirtyFlag));

                if(dirtyFlag == DirtyFlag.Added || dirtyFlag == DirtyFlag.Removed)
                    DirtyPoints.Add((index - 1, DirtyFlag.ChangedPosition));
            }

        }

        void MarkAlPointsDirty()
        {
            for (int i = 0; i < _points.Count; i++)
                DirtyPoints.Add((i, DirtyFlag.ChangedPosition));
        }

        public void ClearDirtyFlags() => DirtyPoints.Clear();
    }
}
