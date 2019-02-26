using System;

namespace Bit
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class IgnoreMeInNavigationStackAttribute : Attribute
    {
    }

    public static class BitCSharpClientControls
    {
        public static void Init()
        {
        }
    }
}