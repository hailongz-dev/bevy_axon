using Bevy;
using UnityEngine;

namespace BevyGraphics
{
    public class BevyScale : BevyValueBehaviour<Scale>
    {
        protected override void OnValueChanged(Scale value)
        {
            transform.localScale = new Vector3(value.x, value.y, value.z);
        }
    }
}