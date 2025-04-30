using LineRenderer3D.Datas;
using UnityEngine;

namespace LinerRenderer3D.Datas
{
    public class LRBoot : MonoBehaviour
    {
        public LRData Data = new();

        void AddModMarkAllPointsDirty(string keyName)
        {
            Data.ModsInfos.Add(new() { Name = keyName });
            Data.Config.MarkAlPointsDirty();
        }

        void AddModWithoutDirty(string keyName)
        {
            Data.ModsInfos.Add(new() { Name = keyName });
        }

        void RemoveModMarkAllPointsDirty(string keyName)
        {
            var index = Data.ModsInfos.FindIndex(x => x.Name == keyName);
            Data.ModsInfos.RemoveAt(index);
            for (int i = index; i < Data.ModsInfos.Count; i++)
                Data.ModsInfos[i].DirtyJustTriangles = true;
            Data.Config.MarkAlPointsDirty();
        }

        void RemoveModWithoutDirty(string keyName)
        {
            Data.ModsInfos.RemoveAt(Data.ModsInfos.FindIndex(x => x.Name == keyName));
        }

        /// <summary>
        /// Adds mod and marks all points dirty, you should mark points dirty if you are maniupulating the mesh structure.
        /// </summary>
        public void EnableMod(string keyName, bool shouldMarkPointsDirty)
        {
            if (Data.ModsInfos.Find(x => x.Name == keyName) == null)
                if(shouldMarkPointsDirty)
                    AddModMarkAllPointsDirty(keyName);
                else
                    AddModWithoutDirty(keyName);
        }

        /// <summary>
        /// Removes mod and marks all points dirty, you should mark points dirty if you are maniupulating the mesh structure.
        /// </summary>
        public void DisableMod(string keyName, bool shouldMarkPointsDirty)
        {
            if (Data.ModsInfos.Find(x => x.Name == keyName) != null)
                if (shouldMarkPointsDirty)
                    RemoveModMarkAllPointsDirty(keyName);
                else
                    RemoveModWithoutDirty(keyName);
        }
    }
}
