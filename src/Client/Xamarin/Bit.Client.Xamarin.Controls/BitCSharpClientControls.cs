using Bit.Client.Xamarin.Controls.Implementations;
using Rg.Plugins.Popup.Services;
using System;
using Xamarin.Forms;

#if !UWP
[assembly: XmlnsDefinition("https://bitplatform.dev", "Bit.View", AssemblyName = "Bit.Client.Xamarin.Controls")]
[assembly: XmlnsDefinition("https://bitplatform.dev", "Bit.View.Controls", AssemblyName = "Bit.Client.Xamarin.Controls")]
#endif

[assembly: Xamarin.Forms.Internals.Preserve]

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
            UseBitPopupNavigation();
        }

        /// <summary>
        /// To be called in platform specific project.
        /// </summary>
        public static void Init()
        {
#if iOS
            Bit.iOS.BitFrameRenderer.Init();
#elif UWP
            Bit.UWP.BitFrameRenderer.Init();
#elif Android
            Bit.Android.BitFrameRenderer.Init();
#endif
        }

        public static void UseBitPopupNavigation()
        {
#if Android || iOS || UWP
            if (!(PopupNavigation.Instance is BitPopupNavigation))
            {
                PopupNavigation.SetInstance(new BitPopupNavigation
                {
                    OriginalImplementation = PopupNavigation.Instance
                });
            }
#endif
        }
    }
}
