using System;
using Xamarin.Forms;

#if !UWP
[assembly: XmlnsDefinition("https://bitframework.com", "Bit.View", AssemblyName = "Bit.CSharpClient.Controls")]
[assembly: XmlnsDefinition("https://bitframework.com", "Bit.View.Controls", AssemblyName = "Bit.CSharpClient.Controls")]
#endif

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
        /// <summary>
        /// To be called in shared/net-standard project.
        /// https://docs.microsoft.com/bg-bg/xamarin/xamarin-forms/xaml/custom-namespace-schemas#consuming-a-custom-namespace-schema
        /// </summary>
        public static void XamlInit()
        {

        }

        /// <summary>
        /// To be called in platform specific project.
        /// </summary>
#if Android9
            [Obsolete("Bit is going to drop support for Android 9 Sdk. It's recommended to use Android 10 Sdk.")]
#endif
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