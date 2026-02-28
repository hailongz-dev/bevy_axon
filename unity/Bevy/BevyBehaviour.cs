using System;
using System.Reflection;
using UnityEngine;

namespace Bevy
{
    public class BevyBehaviour : MonoBehaviour, IBevyBehaviour
    {
        public virtual void SetValue(uint t, object data)
        {
            if (data == null) return;
            var type = data.GetType();
            foreach (var field in GetType()
                         .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                                    BindingFlags.SetField))
            {
                if (field.FieldType != type) continue;
                field.SetValue(this, data);
            }

            foreach (var field in GetType()
                         .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                                        BindingFlags.SetProperty))
            {
                if (field.PropertyType != type) continue;
                field.SetValue(this, data);
            }
        }

        public virtual void Invoke(uint t, object data)
        {
            if (data == null) return;
            try
            {
                var m = GetType().GetMethod("Invoke", new[] { data.GetType() });
                if (m == null) return;
                m.Invoke(this, new[] { data });
            }
            catch (Exception)
            {
                // ignore
            }
        }
    }
}