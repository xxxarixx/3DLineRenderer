using LinerRenderer3D.Datas;
using UnityEngine;

namespace LineRenderer3D.Mods
{
    public class LRModBase : MonoBehaviour
    {
        protected LRBoot _boot;

        abstract void OnEnableMethod();

        abstract void OnDisableMethod();

        private void OnEnable()
        {
            if (_boot == null)
                _boot = GetComponent<LRBoot>();
            if (_boot.Data.ModsInfos.Find(x => x.Name == nameof(T)) == null)
                _boot.AddModMarkAllPointsDirty(KeyName);
            OnEnableMethod();
        }

        private void OnDisable()
        {
            OnDisableMethod();
        }
    }
}
