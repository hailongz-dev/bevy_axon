using Bevy;
using UnityEngine;
using UnityEngine.Serialization;

namespace BevyGraphics
{
    public class BevyColor : BevyValueBehaviour<Color>
    {
        [FormerlySerializedAs("SpriteRenderers")]
        public SpriteRenderer[] spriteRenderers;

        protected override void OnValueChanged(Color value)
        {
            if (spriteRenderers == null || spriteRenderers.Length == 0 || value == null) return;
            foreach (var spriteRenderer in spriteRenderers)
            {
                if (!spriteRenderer) continue;
                spriteRenderer.color = new UnityEngine.Color(value.r, value.g, value.b, value.a);
            }
        }
    }
}