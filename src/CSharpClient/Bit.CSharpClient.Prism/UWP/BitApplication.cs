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
        private bool _useDefaultConfiguration = false;

        /// <summary>
        /// Configures VersionTracking | RgPluginsPopup | Xamarin Forms
        /// </summary>
        protected virtual void UseDefaultConfiguration(IActivatedEventArgs launchActivatedEventArgs, IEnumerable<Assembly> rendererAssemblies = null)
        {
            VersionTracking.Track();
            Rg.Plugins.Popup.Popup.Init();
            Forms.Init(launchActivatedEventArgs, rendererAssemblies);
            _useDefaultConfiguration = true;
        }
    }
}
#endif