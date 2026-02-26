using System;
using UnityEngine;

namespace Bevy
{
    public class BevyObject : MonoBehaviour
    {
        public ulong Id { get; set; }

        public void SetValue(uint type, ArraySegment<byte> rawData)
        {
            foreach (var dst in GetComponentsInChildren<BevyBehaviour>(true))
            {
                dst.SetValue(type, rawData);
            }
        }
    }
}