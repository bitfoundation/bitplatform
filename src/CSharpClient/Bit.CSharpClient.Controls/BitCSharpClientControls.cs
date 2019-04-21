using System;

namespace Bit.View
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class IgnoreMeInNavigationStackAttribute : Attribute
    {
    }
}

namespace Bit.View
{
    public static class BitCSharpClientControls
    {
        public static void Init()
        {
#if iOS
            Bit.View.iOS.BitFrameRenderer.Init();
#elif UWP
            Bit.View.UWP.BitFrameRenderer.Init();
#elif Android
            Bit.View.Android.BitFrameRenderer.Init();
#endif
        }
    }
}