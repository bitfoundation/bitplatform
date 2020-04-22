#if iOS 
using Bit.View;
using Bit.ViewModel.Implementations;
using Foundation;
using System;
using System.Threading.Tasks;
using UIKit;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

namespace Bit.iOS
{
    public class BitFormsApplicationDelegate : FormsApplicationDelegate
    {
        private bool _useDefaultConfiguration = false;

        /// <summary>
        /// Configures VersionTracking | RgPluginsPopup | BitCSharpClientControls (DatePicker, Checkbox, RadioButton, Frame)
        /// </summary>
        protected virtual void UseDefaultConfiguration()
        {
            _useDefaultConfiguration = true;
            VersionTracking.Track();
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

        public override bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options)
        {
            if (_useDefaultConfiguration == true && Xamarin.Essentials.Platform.OpenUrl(app, url, options))
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
