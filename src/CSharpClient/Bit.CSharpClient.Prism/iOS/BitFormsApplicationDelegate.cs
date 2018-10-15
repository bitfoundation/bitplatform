#if iOS 
using Foundation;
using System;
using System.Threading.Tasks;
using UIKit;
using Xamarin.Forms.Platform.iOS;

namespace Bit.iOS
{
    public class BitFormsApplicationDelegate : FormsApplicationDelegate
    {
        public override bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options)
        {
            Uri uri = new Uri(url.AbsoluteString);

            OnSsoLoginLogoutRedirectCompleted?.Invoke(uri);

            return true;
        }

        public static Func<Uri, Task> OnSsoLoginLogoutRedirectCompleted;
    }
}
#endif
