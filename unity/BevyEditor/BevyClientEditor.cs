using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bevy;
using UnityEditor;
using UnityEngine;

namespace BevyEditor
{
    [CustomEditor(typeof(BevyClient))]
    public class BevyClientEditor : UnityEditor.Editor
    {
        private string _addr = "127.0.0.1:7777";


        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (target is not BevyClient client) return;


            if (!Application.isPlaying) return;

            if (client.IsConnected)
            {
                if (GUILayout.Button("Disconnect"))
                {
                    client.Disconnect();
                }
            }
            else
            {
                GUILayout.BeginHorizontal();
                _addr = GUILayout.TextField(_addr);

                GUI.enabled = !string.IsNullOrEmpty(_addr);

                if (GUILayout.Button("Connect", GUILayout.Width(100)))
                {
                    client.Connect(_addr);
                    client.StartCoroutine(OnConnecting(client));
                }

                GUILayout.EndHorizontal();
            }
        }

        private IEnumerator OnConnecting(BevyClient client)
        {
            var t = 0f;
            while (!client.IsConnected)
            {
                yield return null;
                t += Time.deltaTime;
                if (!(t >= 1.2f)) continue;
                var s = client.GetErrorMessage();
                if (!string.IsNullOrEmpty(s))
                {
                    EditorUtility.DisplayDialog("Error", s, "OK");
                }

                yield break;
            }

            Repaint();
        }
    }
}