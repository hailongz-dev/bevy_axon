using Bevy;
using UnityEngine;
using UnityEngine.Serialization;

namespace BevyGraphics
{
    public class BevySize : BevyValueBehaviour<Size>
    {
        [FormerlySerializedAs("SpriteRenderers")]
        public SpriteRenderer[] spriteRenderers;

        protected override void OnValueChanged(Size value)
        {
            if (spriteRenderers == null || spriteRenderers.Length == 0 || value == null) return;
            foreach (var spriteRenderer in spriteRenderers)
            {
                if (!spriteRenderer) continue;
                spriteRenderer.size = new Vector2(value.w, value.h);
            }
        }
    }
}