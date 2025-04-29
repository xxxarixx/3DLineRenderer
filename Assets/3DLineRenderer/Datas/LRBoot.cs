using LineRenderer3D.Datas;
using UnityEngine;

namespace LinerRenderer3D.Datas
{
    public class LRBoot : MonoBehaviour
    {
        public LRData Data = new();

        public void AddMod(string keyName, int pointIndex)
        {
            Data.ModsInfos.Add(new() { Name = keyName });
            Data.Config.MarkPointDirty(pointIndex, LRConfig.DirtyFlag.ChangedPosition);
        }

        public void AddModMarkAllPointsDirty(string keyName)
        {
            Data.ModsInfos.Add(new() { Name = keyName });
            Data.Config.MarkAlPointsDirty();
        }

        public void AddModWithoutDirty(string keyName)
        {
            Data.ModsInfos.Add(new() { Name = keyName });
        }

        public void RemoveMod(string keyName, int pointIndex)
        {
            var index = Data.ModsInfos.FindIndex(x => x.Name == keyName);
            Data.ModsInfos.RemoveAt(index);
            for (int i = index; i < Data.ModsInfos.Count; i++)
                Data.ModsInfos[i].DirtyJustTriangles = true;
            Data.Config.MarkPointDirty(pointIndex, LRConfig.DirtyFlag.ChangedPosition);
        }

        public void RemoveModMarkAllPointsDirty(string keyName)
        {
            var index = Data.ModsInfos.FindIndex(x => x.Name == keyName);
            Data.ModsInfos.RemoveAt(index);
            for (int i = index; i < Data.ModsInfos.Count; i++)
                Data.ModsInfos[i].DirtyJustTriangles = true;
            Data.Config.MarkAlPointsDirty();
        }

        public void RemoveModWithoutDirty(string keyName)
        {
            Data.ModsInfos.RemoveAt(Data.ModsInfos.FindIndex(x => x.Name == keyName));
        }
    }
}
