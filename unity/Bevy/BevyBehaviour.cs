using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Bevy
{
    public abstract class BevyBehaviour : MonoBehaviour
    {
        private static readonly Dictionary<Type, Dictionary<uint, FieldInfo>> TypeVariantCache = new();

        private Dictionary<uint, FieldInfo> _variantSet;

        protected virtual void Awake()
        {
            InitVariantSet();
        }

        private void InitVariantSet()
        {
            var type = GetType();

            if (TypeVariantCache.TryGetValue(type, out _variantSet)) return;
            _variantSet = new Dictionary<uint, FieldInfo>();

            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public |
                                                 BindingFlags.NonPublic))
            {
                var attr = field.GetCustomAttribute<BevyVariantAttribute>();
                if (attr != null)
                {
                    _variantSet[attr.VariantType] = field;
                }
            }

            TypeVariantCache[type] = _variantSet;
        }

        public void SetValue(uint type, ArraySegment<byte> rawData)
        {
            if (_variantSet == null || !_variantSet.TryGetValue(type, out var field)) return;
            try
            {
                field.SetValue(this, JsonUtility.FromJson(Encoding.UTF8.GetString(rawData), field.FieldType));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public void Invoke(object data)
        {
            var s = GetComponentInParent<BevyClient>();
            if (!s) return;
            s.Invoke(data);
        }
    }
}