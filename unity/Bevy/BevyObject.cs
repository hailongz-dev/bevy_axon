using UnityEngine;
using UnityEngine.Serialization;

namespace Bevy
{
    public class BevyObject : MonoBehaviour
    {
        [FormerlySerializedAs("TypeId")] [BevyObjectType]
        public uint typeId;

        public ulong Id { get; set; }

        public void SetValue(uint type, object data)
        {
            foreach (var dst in GetComponentsInChildren<IBevyBehaviour>(true))
            {
                dst.SetValue(type, data);
            }
        }

        public void Invoke(uint type, object data)
        {
            if (data == null) return;
            foreach (var dst in GetComponentsInChildren<IBevyBehaviour>(true))
            {
                dst.Invoke(type, data);
            }
        }
    }
}