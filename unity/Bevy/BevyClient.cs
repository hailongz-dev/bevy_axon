using System;
using System.Collections.Generic;
using System.IO;
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
        private static readonly Dictionary<uint, Type> TypeSet = new();

        public static void AddType(uint t, Type type)
        {
            TypeSet.Add(t, type);
        }

        public static bool TryGetType(uint t, out Type type)
        {
            return TypeSet.TryGetValue(t, out type);
        }

        private const byte ActionTypeSpawn = 1;
        private const byte ActionTypeDespawn = 2;
        private const byte ActionTypeChange = 3;
        private const byte ActionTypeInvoke = 4;

        private readonly Dictionary<ulong, BevyObject> _objectSet = new();

        [FormerlySerializedAs("Prefabs")] public List<BevyObject> prefabs = new();

        private IntPtr _client;
        private readonly ulong _clientId = (ulong)DateTimeOffset.Now.ToUnixTimeMilliseconds();

        public ulong ClientId => _clientId;

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
            _client = bevy_axon_ffi_create(cAddr, _clientId);
            Marshal.FreeHGlobal(cAddr);

            foreach (var v in _objectSet.Values)
            {
                Destroy(v.gameObject);
            }

            _objectSet.Clear();

            Debug.Log($"Connecting to {addr} , ClientId: {_clientId}");
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
            var sb = new SbinReader(data);
            try
            {
                while (true)
                {
                    var act = sb.ReadU8();
                    var id = sb.ReadU64();
                    var t = sb.ReadU32();
                    var d = sb.ReadBytes();

                    switch (act)
                    {
                        case ActionTypeSpawn when _objectSet.ContainsKey(id):
                            continue;
                        case ActionTypeSpawn:
                        {
                            var item = prefabs.FirstOrDefault(v => v.typeId == t);
                            if (!item) continue;
                            var v = Instantiate(item.gameObject, transform);
                            var s = v.GetComponent<BevyObject>();
                            if (!s)
                            {
                                Destroy(v);
                                continue;
                            }

                            s.Id = id;
                            var tr = v.transform;
                            tr.localPosition = Vector3.zero;
                            tr.localScale = Vector3.one;
                            tr.localRotation = Quaternion.identity;
                            _objectSet[s.Id] = s;
                            Debug.Log($"spawn {id}");
                            break;
                        }
                        case ActionTypeDespawn:
                        {
                            if (!_objectSet.Remove(id, out var v)) continue;
                            Destroy(v.gameObject);
                            Debug.Log($"despawn {id}");
                            break;
                        }
                        case ActionTypeChange:
                        {
                            if (!_objectSet.TryGetValue(id, out var v)) continue;
                            if (!TypeSet.TryGetValue(t, out var tt)) continue;
                            try
                            {
                                v.SetValue(t, new SbinReader(d).ReadSerializable(tt));
                            }
                            catch (Exception e)
                            {
                                Debug.LogError(e);
                            }

                            break;
                        }
                        case ActionTypeInvoke:
                            break;
                    }
                }
            }
            catch (EndOfStreamException)
            {
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        private void FixedUpdate()
        {
            if (_client == IntPtr.Zero) return;
            if (_invoke.Length > 0)
            {
                var handle = GCHandle.Alloc(_invoke.GetBuffer(), GCHandleType.Pinned);
                try
                {
                    bevy_axon_ffi_invoke(_client, handle.AddrOfPinnedObject(), _invoke.Length);
                }
                finally
                {
                    handle.Free();
                }

                _invoke.SetLength(0);
                _invoke.Position = 0;
            }

            var raw = bevy_axon_ffi_update(_client, Time.deltaTime, out var len);
            if (raw == IntPtr.Zero || len <= 0) return;
            _raw.SetLength((int)len);
            Marshal.Copy(raw, _raw.GetBuffer(), 0, (int)len);
            OnRawData(new ArraySegment<byte>(_raw.GetBuffer(), 0, (int)len));
        }

        private readonly MemoryStream _raw = new(20480);
        private readonly MemoryStream _invoke = new(20480);
        private readonly MemoryStream _value = new(20480);

        public void Invoke(uint type, object data)
        {
            var sb = new SbinWriter(_invoke);
            sb.WriteU8(ActionTypeInvoke);
            sb.WriteU64(0);
            sb.WriteU32(type);

            _value.SetLength(0);
            _value.Position = 0;
            var s = new SbinWriter(_value);
            s.WriteSerializable(data);
            sb.WriteBytes(s.ToArray());
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
        private static extern IntPtr bevy_axon_ffi_create(IntPtr addr, ulong clientId);

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