using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BevyEditor
{
    public static class Meta
    {
        [Serializable]
        public class MetaFileItem
        {
            public string path;
            public string name;
            public Metadata metadata;
        }

        [Serializable]
        public struct Metadata
        {
            public MetaInfo[] o;
            public MetaInfo[] v;
            public MetaInfo[] e;
        }

        [Serializable]
        public struct MetaInfo
        {
            public uint i;
            public string n;
            public MetaField[] p;
        }

        [Serializable]
        public struct MetaField
        {
            public string n;
            public string t;
            public MetaField[] p;
        }

        public static MetaFileItem[] GetAll()
        {
            return (from id in AssetDatabase.FindAssets("t:TextAsset")
                select AssetDatabase.GUIDToAssetPath(id)
                into p
                let baseName = Path.GetFileName(p)
                where baseName == "axon.json"
                let text = AssetDatabase.LoadAssetAtPath<TextAsset>(p)
                select new MetaFileItem()
                {
                    path = p, name = Path.Join(Path.GetFileName(Path.GetDirectoryName(p)!), baseName),
                    metadata = JsonUtility.FromJson<Metadata>(text.text),
                }).ToArray();
        }
        
    }
}