using System;

namespace Bevy
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct)]
    public class BevyVariantAttribute : Attribute
    {
        public readonly uint VariantType;

        public BevyVariantAttribute(uint variantType)
        {
            VariantType = variantType;
        }
    }
}