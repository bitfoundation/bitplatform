#if iOS 
using Bit.View;
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
        }

        public override bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options)
        {
            if (_useDefaultConfiguration == true)
            {
                Uri uri = new Uri(url.AbsoluteString);
                OnSsoLoginLogoutRedirectCompleted?.Invoke(uri);

                return true;
            }
            else
            {
                return base.OpenUrl(app, url, options);
            }
        }

        public static Func<Uri, Task> OnSsoLoginLogoutRedirectCompleted;
    }
}
#endif
