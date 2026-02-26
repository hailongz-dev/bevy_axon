using System;

namespace Bevy
{
    [AttributeUsage(AttributeTargets.Field)]
    public class BevyVariantAttribute : Attribute
    {
        public readonly uint VariantType;

        public BevyVariantAttribute(uint variantType)
        {
            VariantType = variantType;
        }
    }
}