using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Bevy;
using UnityEngine;
using UnityEngine.Serialization;

namespace BevyGraphics
{
    public class BevyPage : BevyValueBehaviour<Page>
    {
        [FormerlySerializedAs("DataBindItems")]
        public DataBindItem[] dataBindItems;


        [Serializable]
        public struct DataBindItem
        {
            public string dataKey;
            public string methodName;
            public GameObject[] targets;
        }

        private static PageEvent.PageValue ParseEventParam(string s)
        {
            var vs = s.Split("=");
            return new PageEvent.PageValue()
                { k = vs[0], v = vs.Length > 1 ? HttpUtility.UrlDecode(vs[1]) : null };
        }

        public void OnAction(string url)
        {
            if (string.IsNullOrEmpty(url)) return;
            var p = GetComponentInParent<BevyObject>();
            if (!p) return;
            if (p.Id == 0) return;
            var cli = GetComponentInParent<BevyClient>();
            if (!cli) return;

            var i = url.IndexOf("?", StringComparison.Ordinal);
            var vs = i >= 0
                ? url[(i + 1)..].Split("&").Where(v => !string.IsNullOrEmpty(v)).Select(ParseEventParam).ToArray()
                : Array.Empty<PageEvent.PageValue>();
            var eventName = i >= 0 ? url[..i] : url;

            cli.Invoke(new PageEvent()
            {
                client_id = cli.ClientId,
                id = p.Id,
                n = eventName,
                p = vs,
            });
        }
        
        protected override void OnValueChanged(Page v)
        {
            if (v == null) return;

            foreach (var s in GetComponentsInChildren<IBevyPageSetData>(true))
            {
                s.SetPageData(v);
            }

            if (dataBindItems == null || dataBindItems.Length == 0) return;

            var m = new Dictionary<string, string>();

            foreach (var i in v.p)
            {
                m[i.k] = i.v;
            }

            foreach (var s in dataBindItems)
            {
                if (s.targets == null || s.targets.Length == 0) continue;
                if (string.IsNullOrEmpty(s.dataKey)) continue;
                if (!m.TryGetValue(s.dataKey, out var vv)) continue;
                foreach (var target in s.targets)
                {
                    if (!target) continue;
                    var ss = target.GetComponent<IBevyPageDataBind>();
                    if (ss != null)
                    {
                        ss.SetValue(vv, s.methodName);
                        continue;
                    }

                    if (string.IsNullOrEmpty(s.methodName)) continue;
                    foreach (var c in target.GetComponents<Component>())
                    {
                        try
                        {
                            var methodInfo = c.GetType().GetMethod(s.methodName, new[] { typeof(string) });
                            if (methodInfo == null || methodInfo.ReturnType != typeof(void)) continue;
                            methodInfo.Invoke(c, new object[] { vv });
                            break;
                        }
                        catch (Exception)
                        {
                            // ignore
                        }
                    }
                }
            }
        }
    }
}