using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;

namespace Bevy
{
    public class BevyClient : MonoBehaviour
    {
        private const uint ActionTypeSpawn = 1;
        private const uint ActionTypeDespawn = 2;
        private const uint ActionTypeChange = 3;
        private const uint ActionTypeInvoke = 4;

        private readonly Dictionary<ulong, BevyObject> _objectSet = new();

        [FormerlySerializedAs("Prefabs")] public List<PrefabItem> prefabs = new();

        [Serializable]
        public class PrefabItem
        {
            public uint type;
            public GameObject prefab;
        }

        private IntPtr _client;

        public string GetErrorMessage()
        {
            var p = bevy_axon_ffi_errmsg();
            return p == IntPtr.Zero ? null : Marshal.PtrToStringAnsi(p);
        }

        public void Connect(string addr)
        {
            if (_client != IntPtr.Zero)
            {
                bevy_axon_ffi_exit(_client);
                _client = IntPtr.Zero;
            }

            if (string.IsNullOrEmpty(addr)) return;
            var cAddr = Marshal.StringToHGlobalAnsi(addr);
            _client = bevy_axon_ffi_create(cAddr);
            Marshal.FreeHGlobal(cAddr);

            foreach (var v in _objectSet.Values)
            {
                Destroy(v);
            }

            _objectSet.Clear();
        }

        public void Disconnect()
        {
            if (_client == IntPtr.Zero) return;
            bevy_axon_ffi_exit(_client);
            _client = IntPtr.Zero;
        }

        private void OnDestroy()
        {
            if (_client == IntPtr.Zero) return;
            bevy_axon_ffi_exit(_client);
            _client = IntPtr.Zero;
        }

        ~BevyClient()
        {
            if (_client == IntPtr.Zero) return;
            bevy_axon_ffi_exit(_client);
            _client = IntPtr.Zero;
        }

        public bool IsConnected => _client != IntPtr.Zero && bevy_axon_ffi_is_connected(_client) != 0;


        private static int Find(ArraySegment<byte> data, int off, int c)
        {
            for (var i = off; i < data.Count; i++)
            {
                if (data[i] == c) return i;
            }

            return -1;
        }

        // ReSharper disable Unity.PerformanceAnalysis
        private void OnRawData(ArraySegment<byte> data)
        {
            var i = 0;
            var n = data.Count;
            while (i < n)
            {
                var end = Find(data, i, 10);
                if (end == -1) return;
                var vs = Encoding.UTF8.GetString(data[i..end]).Split(",").Select(ulong.Parse).ToArray();
                i = end + 1;
                end = Find(data, i, 10);
                if (end == -1) return;
                var value = data[i..end];
                i = end + 1;
                switch (vs[0])
                {
                    case ActionTypeSpawn when vs.Length < 3:
                    case ActionTypeSpawn when _objectSet.ContainsKey(vs[1]):
                        continue;
                    case ActionTypeSpawn:
                    {
                        var item = prefabs.FirstOrDefault(v => v.type == vs[2]);
                        if (item == null || !item.prefab) continue;
                        var v = Instantiate(item.prefab, transform);
                        var s = v.GetComponent<BevyObject>();
                        if (!s)
                        {
                            Destroy(v);
                            continue;
                        }

                        s.Id = vs[1];
                        var tr = v.transform;
                        tr.localPosition = Vector3.zero;
                        tr.localScale = Vector3.one;
                        tr.localRotation = Quaternion.identity;
                        _objectSet[s.Id] = s;
                        break;
                    }
                    case ActionTypeDespawn when vs.Length < 2:
                        continue;
                    case ActionTypeDespawn:
                    {
                        if (!_objectSet.Remove(vs[1], out var v)) continue;
                        Destroy(v);
                        break;
                    }
                    case ActionTypeChange when vs.Length < 3:
                        continue;
                    case ActionTypeChange:
                    {
                        if (!_objectSet.TryGetValue(vs[1], out var v)) continue;
                        v.SetValue((uint)vs[2], value);
                        break;
                    }
                }
            }
        }

        private void FixedUpdate()
        {
            if (_client == IntPtr.Zero) return;
            if (_invoke.Length > 0)
            {
                var handle = GCHandle.Alloc(_invoke.Buffer, GCHandleType.Pinned);
                try
                {
                    bevy_axon_ffi_invoke(_client, handle.AddrOfPinnedObject(), _invoke.Length);
                }
                finally
                {
                    handle.Free();
                }

                _invoke.Clear();
            }

            var raw = bevy_axon_ffi_update(_client, Time.deltaTime, out var len);
            if (raw == IntPtr.Zero || len <= 0) return;
            _raw.EnsureCapacity((int)len);
            Marshal.Copy(raw, _raw.Buffer, 0, (int)len);
            OnRawData(new ArraySegment<byte>(_raw.Buffer, 0, (int)len));
        }

        private readonly NativeBuffer _raw = new(20480);
        private readonly NativeBuffer _invoke = new(20480);

        public void Invoke(uint type, object data)
        {
            _invoke.Write(Encoding.UTF8.GetBytes($"{ActionTypeInvoke},${type}\n${JsonUtility.ToJson(data)}\n"));
        }

        public void Invoke(object data)
        {
            var attr = data?.GetType().GetCustomAttribute<BevyEventAttribute>();
            if (attr == null) return;
            Invoke(attr.EventType, data);
        }


#if (UNITY_WEBGL || UNITY_IPHONE) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport("bevy_axon", CallingConvention = CallingConvention.Cdecl)]
#endif
        private static extern IntPtr bevy_axon_ffi_create(IntPtr addr);

#if (UNITY_WEBGL || UNITY_IPHONE) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport("bevy_axon", CallingConvention = CallingConvention.Cdecl)]
#endif
        private static extern void bevy_axon_ffi_exit(IntPtr ptr);

#if (UNITY_WEBGL || UNITY_IPHONE) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport("bevy_axon", CallingConvention = CallingConvention.Cdecl)]
#endif
        private static extern int bevy_axon_ffi_is_connected(IntPtr ptr);

#if (UNITY_WEBGL || UNITY_IPHONE) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport("bevy_axon", CallingConvention = CallingConvention.Cdecl)]
#endif
        private static extern IntPtr bevy_axon_ffi_update(IntPtr ptr, float dt, out long len);

#if (UNITY_WEBGL || UNITY_IPHONE) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport("bevy_axon", CallingConvention = CallingConvention.Cdecl)]
#endif
        private static extern void bevy_axon_ffi_invoke(IntPtr ptr, IntPtr raw, long len);

#if (UNITY_WEBGL || UNITY_IPHONE) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport("bevy_axon", CallingConvention = CallingConvention.Cdecl)]
#endif
        private static extern IntPtr bevy_axon_ffi_errmsg();
    }
}