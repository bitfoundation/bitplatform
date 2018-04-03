using Bit.ViewModel.Contracts;
using Prism;
using Prism.AppModel;
using Prism.Services;
using System;

namespace Bit.ViewModel.Implementations
{
    public class DefaultBrowserService : IBrowserService
    {
#if Android
        private readonly Android.Content.Context _context;
#endif 
        private readonly IDeviceService _deviceService;

        public DefaultBrowserService(IDeviceService deviceService
#if Android
            , Android.Content.Context context
#endif
            )
        {
#if Android
            _context = context;
#endif
            _deviceService = deviceService;
        }

        public virtual void OpenUrl(Uri url)
        {
            if (url == null)
                throw new ArgumentNullException(nameof(url));

#if Android

            var mgr = new Android.Support.CustomTabs.CustomTabsActivityManager((Android.App.Activity)_context);

            mgr.CustomTabsServiceConnected += delegate
            {
                mgr.LaunchUrl(url.ToString());
            };

            mgr.BindService();

            return;
#elif iOS
            Foundation.NSUrl destination = Foundation.NSUrl.FromString(url.AbsoluteUri);

            using (SafariServices.SFSafariViewController sfViewController = new SafariServices.SFSafariViewController(destination))
            {
                UIKit.UIWindow window = UIKit.UIApplication.SharedApplication.KeyWindow;

                UIKit.UIViewController controller = window.RootViewController;

                controller.PresentViewController(sfViewController, true, null);

                return;
            }
#else
            if (_deviceService.RuntimePlatform == RuntimePlatform.Android || _deviceService.RuntimePlatform == RuntimePlatform.iOS)
            {
                throw new InvalidOperationException($"Register {nameof(DefaultBrowserService)} using platform specific {nameof(IPlatformInitializer)}");
            }

            _deviceService.OpenUri(url);
#endif
        }
    }
}
