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
#if iOS
            Bit.CSharpClient.Controls.iOS.BitFrameRenderer.Init();
#elif UWP
            CSharpClient.Controls.UWP.BitFrameRenderer.Init();
#elif Android
            Bit.CSharpClient.Controls.Android.BitFrameRenderer.Init();
#endif
        }
    }
}