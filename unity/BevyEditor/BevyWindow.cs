using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace BevyEditor
{
    public class BevyWindow : EditorWindow
    {
        [MenuItem("Window/Bevy")]
        private static void Init()
        {
            var window = (BevyWindow)GetWindow(typeof(BevyWindow));
            window.titleContent = new GUIContent("Bevy");
            window.Show();
        }

        private Vector2 _scrollPos;

        private void OnGUI()
        {
            _scrollPos = GUILayout.BeginScrollView(_scrollPos);

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Refresh", GUILayout.Width(100)))
            {
                ReloadData();
            }

            GUILayout.EndHorizontal();

            foreach (var item in _fileItems)
            {
                GUILayout.BeginHorizontal();

                GUILayout.Label(item.name);

                if (GUILayout.Button("Gen", GUILayout.Width(80)))
                {
                    Gen(item);
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();

            GUILayout.EndScrollView();
        }

        private static string GenType(string type)
        {
            if (type.EndsWith("[]"))
            {
                return $"{GenType(type[..^2])}[]";
            }

            switch (type)
            {
                case "u8":
                case "i8":
                    return "byte";
                case "u16":
                    return "ushort";
                case "i16":
                    return "short";
                case "u32":
                    return "uint";
                case "i32":
                    return "int";
                case "u64":
                    return "ulong";
                case "i64":
                    return "long";
                case "f32":
                    return "float";
                case "f64":
                    return "double";
                case "bool":
                    return "bool";
                case "String":
                    return "string";
                default:
                    return type;
            }
        }

        private static void GenField(Meta.MetaField fd, StringBuilder sb, string prefix)
        {
            sb.Append($"{prefix}public {GenType(fd.t)} {fd.n};\n");
            if (fd.p is not { Length: > 0 }) return;
            sb.Append($"{prefix}[System.Serializable]\n");
            sb.Append($"{prefix}public struct {fd.t.Replace("[]", "")} {{\n");
            foreach (var i in fd.p)
            {
                GenField(i, sb, $"{prefix}\t");
            }

            sb.Append($"{prefix}}}\n");
        }

        private static void GenClass(Meta.MetaInfo info, StringBuilder sb, string prefix)
        {
            sb.Append($"{prefix}[System.Serializable]\n");
            sb.Append($"{prefix}public class {info.n} {{\n");
            sb.Append($"{prefix}\tpublic const uint TypeId = {info.i};\n");

            sb.Append(
                $"{prefix}\t[UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)]\n");
            sb.Append($"{prefix}\tprivate static void Initialize() {{\n");
            sb.Append($"{prefix}\t\tBevy.BevyClient.AddType({info.i},typeof({info.n}));\n");
            sb.Append($"{prefix}\t}}\n");

            foreach (var fd in info.p)
            {
                GenField(fd, sb, $"{prefix}\t");
            }

            sb.Append($"{prefix}}}\n");
        }

        private static void GenVariant(Meta.MetaInfo info, StringBuilder sb, string prefix)
        {
            sb.Append($"{prefix}[Bevy.BevyVariant({info.i})]\n");
            GenClass(info, sb, prefix);
        }

        private static void GenEvent(Meta.MetaInfo info, StringBuilder sb, string prefix)
        {
            sb.Append($"{prefix}[Bevy.BevyEvent({info.i})]\n");
            GenClass(info, sb, prefix);
        }

        private static void Gen(Meta.MetaFileItem item)
        {
            var sb = new StringBuilder();
            var ns = Path.GetDirectoryName(item.name)!;
            sb.Append($"namespace {ns} {{\n");
            foreach (var i in item.metadata.v)
            {
                GenVariant(i, sb, "\t");
            }

            foreach (var i in item.metadata.e)
            {
                GenEvent(i, sb, "\t");
            }

            sb.Append("}\n");

            File.WriteAllText(Path.Join(Path.GetDirectoryName(item.path)!, "Gen.cs"), sb.ToString());

            EditorUtility.DisplayDialog("", "Finished", "OK");
        }

        private Meta.MetaFileItem[] _fileItems;

        private void ReloadData()
        {
            _fileItems = Meta.GetAll();
        }

        private void OnEnable()
        {
            ReloadData();
        }
    }
}