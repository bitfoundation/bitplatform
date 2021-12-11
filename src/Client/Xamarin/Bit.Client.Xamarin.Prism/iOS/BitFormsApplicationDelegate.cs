#if iOS 
using Bit.View;
using Bit.ViewModel.Implementations;
using Foundation;
using System;
using System.Threading.Tasks;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

namespace Bit.iOS
{
    public class BitFormsApplicationDelegate : FormsApplicationDelegate
    {
        private bool _useDefaultConfiguration = false;

        /// <summary>
        /// Configures Xamarin Essentials | RgPluginsPopup | Bit.Client.Xamarin.Controls (DatePicker, Frame)
        /// </summary>
        protected virtual void UseDefaultConfiguration()
        {
            _useDefaultConfiguration = true;
#if Xamarin
            Xamarin.Essentials.VersionTracking.Track();
#else
            Microsoft.Maui.Essentials.VersionTracking.Track();
#endif
            Rg.Plugins.Popup.Popup.Init();
            BitCSharpClientControls.Init();
            SetBitPlatformServices();
        }

        protected virtual async void SetBitPlatformServices()
        {
            while (true)
            {
                await Task.Delay(25);
                if (Forms.IsInitialized)
                {
                    Device.PlatformServices = new BitPlatformServices
                    {
                        OriginalPlatformService = Device.PlatformServices
                    };
                    break;
                }
            }
        }

        public override void PerformActionForShortcutItem(UIApplication application, UIApplicationShortcutItem shortcutItem, UIOperationHandler completionHandler)
        {
            base.PerformActionForShortcutItem(application, shortcutItem, completionHandler);

            if (_useDefaultConfiguration)
            {
#if Xamarin
                Xamarin.Essentials.Platform.PerformActionForShortcutItem(application, shortcutItem, completionHandler);
#else
                Microsoft.Maui.Essentials.Platform.PerformActionForShortcutItem(application, shortcutItem, completionHandler);
#endif
            }
        }

        public override bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options)
        {
            if (_useDefaultConfiguration == true &&
#if Xamarin
                Xamarin.Essentials.Platform.OpenUrl(app, url, options)
#else
                Microsoft.Maui.Essentials.Platform.OpenUrl(app, url, options)
#endif
                )
            {
                return true;
            }
            else
            {
                return base.OpenUrl(app, url, options);
            }
        }
    }
}
#endif
