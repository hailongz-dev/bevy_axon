using System;

namespace Bevy
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class BevyEventAttribute : Attribute
    {
        public readonly uint EventType;

        public BevyEventAttribute(uint eventType)
        {
            EventType = eventType;
        }
    }
}