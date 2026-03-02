using Bevy;
using UnityEngine;

namespace BevyGraphics
{
    public class BevyRotation : BevyValueBehaviour<Rotation>
    {
        protected override void OnValueChanged(Rotation value)
        {
            transform.localRotation = Quaternion.Euler(value.x, value.y, value.z);
        }
    }
}