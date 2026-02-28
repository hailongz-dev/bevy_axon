using Bevy;
using UnityEngine;

namespace BevyGraphics
{
    public class BevyPosition : BevyValueBehaviour<Position>
    {
        protected override void OnValueChanged(Position value)
        {
            transform.localPosition = new Vector3(value.x, value.y, value.z);
        }
    }
}