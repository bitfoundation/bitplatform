//-:cnd:noEmit
using Maui.AppStores;
using Maui.InAppReviews;

namespace Boilerplate.Client.Maui;

public partial class MainPage
{
    private readonly IExceptionHandler exceptionHandler;
    private readonly IBitDeviceCoordinator deviceCoordinator;
    private readonly IStorageService storageService;

    public MainPage(IExceptionHandler exceptionHandler,
        IBitDeviceCoordinator deviceCoordinator,
        IPubSubService pubSubService,
        IStorageService storageService)
    {
        this.exceptionHandler = exceptionHandler;
        this.deviceCoordinator = deviceCoordinator;
        this.storageService = storageService;

        InitializeComponent();

        SetupBlazorWebView();
        SetupStatusBar();

        pubSubService.Subscribe(PubSubMessages.PROFILE_UPDATED, async _ =>
        {
            // It's an opportune moment to request a store review. (:

            await Dispatcher.DispatchAsync(async () =>
            {
                if ((await storageService.GetItem("StoreReviewRequested")) is not "true")
                {
                    await storageService.SetItem("StoreReviewRequested", "true");
                    ReviewStatus status = await InAppReview.Current.RequestAsync();
                }
            });
        });
    }

    private void SetupBlazorWebView()
    {
        BlazorWebViewHandler.BlazorWebViewMapper.AppendToMapping("CustomBlazorWebViewMapper", (handler, view) =>
        {
#if WINDOWS
            if (AppInfo.Current.RequestedTheme == AppTheme.Dark)
            {
                handler.PlatformView.DefaultBackgroundColor = Microsoft.UI.Colors.Black;
            }
#elif IOS || MACCATALYST
            handler.PlatformView.Configuration.AllowsInlineMediaPlayback = true;

            handler.PlatformView.ScrollView.Bounces = false;

            handler.PlatformView.BackgroundColor = UIKit.UIColor.Clear;
            handler.PlatformView.Opaque = false;
            if (BuildConfiguration.IsDebug())
            {
                if ((DeviceInfo.Current.Platform == DevicePlatform.MacCatalyst && DeviceInfo.Current.Version >= new Version(13, 3))
                    || (DeviceInfo.Current.Platform == DevicePlatform.iOS && DeviceInfo.Current.Version >= new Version(16, 4)))
                {
                    handler.PlatformView.SetValueForKey(Foundation.NSObject.FromObject(true), new Foundation.NSString("inspectable"));
                }
            }
#elif ANDROID
            handler.PlatformView.SetBackgroundColor(Android.Graphics.Color.Transparent);

            handler.PlatformView.OverScrollMode = Android.Views.OverScrollMode.Never;

            handler.PlatformView.HapticFeedbackEnabled = false;

            Android.Webkit.WebSettings settings = handler.PlatformView.Settings;

            settings.AllowFileAccessFromFileURLs =
                settings.AllowUniversalAccessFromFileURLs =
                settings.AllowContentAccess =
                settings.AllowFileAccess =
                settings.DatabaseEnabled =
                settings.JavaScriptCanOpenWindowsAutomatically =
                settings.DomStorageEnabled = true;

            if (BuildConfiguration.IsDebug())
            {
                settings.MixedContentMode = Android.Webkit.MixedContentHandling.AlwaysAllow;
            }

            settings.BlockNetworkLoads =
                settings.BlockNetworkImage = false;
#endif
        });

        Loaded += async delegate
        {
            try
            {
#if WINDOWS
                if (BuildConfiguration.IsRelease())
                {
                    var webView2 = (Microsoft.UI.Xaml.Controls.WebView2)blazorWebView.Handler!.PlatformView!;
                    await webView2.EnsureCoreWebView2Async();
                    var settings = webView2.CoreWebView2.Settings;
                    settings.IsZoomControlEnabled = false;
                    settings.AreBrowserAcceleratorKeysEnabled = false;
                }
#endif
            }
            catch (Exception exp)
            {
                exceptionHandler.Handle(exp);
            }
        };
    }

    private void SetupStatusBar()
    {
        Microsoft.Maui.Handlers.WindowHandler.Mapper.AppendToMapping(nameof(IWindow), async (handler, view) =>
        {
            try
            {
                await deviceCoordinator.ApplyTheme(AppInfo.Current.RequestedTheme is AppTheme.Dark);
            }
            catch (Exception exp)
            {
                exceptionHandler.Handle(exp);
            }
        });
    }

    protected override async void OnAppearing()
    {
        try
        {
            base.OnAppearing();

            await CheckForUpdates();
        }
        catch (Exception exp)
        {
            exceptionHandler.Handle(exp);
        }
    }

    private async Task CheckForUpdates()
    {
        if (OperatingSystem.IsAndroid()) // We're using in app updates for android thanks to Oscore.Maui.Android.InAppUpdates
            return;

        await Task.Delay(TimeSpan.FromSeconds(3)); // No rush to check for updates.

        try
        {
            if (await AppStoreInfo.Current.IsUsingLatestVersionAsync() is false)
            {
                if (await storageService.GetItem($"{AppInfo.Version}_UpdateFromVersionIsRequested") is not "true")
                {
                    await storageService.SetItem($"{AppInfo.Version}_UpdateFromVersionIsRequested", "true");

                    // It's an opportune moment to request an update. (:
                    // https://github.com/oscoreio/Maui.AppStoreInfo
                    if (await DisplayAlert(AppStrings.NewVersionIsAvailable, AppStrings.UpdateToNewVersion, AppStrings.Yes, AppStrings.No) is true)
                    {
                        await AppStoreInfo.Current.OpenApplicationInStoreAsync();
                    }
                }
            }
        }
        catch (FileNotFoundException) { }
    }
}
