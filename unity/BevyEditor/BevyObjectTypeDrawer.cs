using System.Collections.Generic;
using Bevy;
using UnityEditor;
using UnityEngine;

namespace BevyEditor
{
    [CustomPropertyDrawer(typeof(BevyObjectTypeAttribute))]
    public class BevyObjectTypeDrawer : PropertyDrawer
    {
        private static bool _initialized;
        private static List<Meta.MetaInfo> _items;
        private static Dictionary<uint, int> _indexSet;
        private static GUIContent[] _titles;

        private static void Init()
        {
            if (_initialized) return;
            _initialized = true;

            _items = new List<Meta.MetaInfo>();
            _indexSet = new Dictionary<uint, int>();

            var titles = new List<GUIContent>
            {
                new GUIContent("None")
            };

            foreach (var f in Meta.GetAll())
            {
                foreach (var i in f.metadata.o)
                {
                    _indexSet[i.i] = _items.Count;
                    _items.Add(i);
                    titles.Add(new GUIContent($"{i.n} ({i.i})"));
                }
            }

            _titles = titles.ToArray();
        }


        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Init();

            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.LabelField(position, label);
            if (_indexSet.TryGetValue(property.uintValue, out var index))
            {
                index++;
            }
            else
            {
                index = 0;
            }

            index = EditorGUI.Popup(position, label, index, _titles);

            property.uintValue = index > 0 ? _items[index - 1].i : (uint)0;

            EditorGUI.EndProperty();

            if (GUILayout.Button("Refresh"))
            {
                _initialized = false;
            }
        }
    }
}