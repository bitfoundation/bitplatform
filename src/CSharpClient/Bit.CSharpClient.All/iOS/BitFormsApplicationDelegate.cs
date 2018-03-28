#if iOS 
using Bit.ViewModel.Implementations;
using Foundation;
using System;
using UIKit;
using Xamarin.Forms.Platform.iOS;

namespace Bit.iOS
{
    public class BitFormsApplicationDelegate : FormsApplicationDelegate
    {
        public override bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options)
        {
            Uri uri = new Uri(url.AbsoluteString);

            DefaultSecurityService.Current.OnSsoLoginLogoutRedirectCompleted(uri);

            return true;
        }
    }
}
#endif