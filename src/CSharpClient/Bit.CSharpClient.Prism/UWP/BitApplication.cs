#if UWP
using System.Collections.Generic;
using System.Reflection;
using Windows.ApplicationModel.Activation;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace Bit.UWP
{
    public class BitApplication : Windows.UI.Xaml.Application
    {
        /// <summary>
        /// Configures VersionTracking | RgPluginsPopup
        /// </summary>
        protected virtual void UseDefaultConfiguration()
        {
            VersionTracking.Track();
            Rg.Plugins.Popup.Popup.Init();
        }
    }
}
#endif