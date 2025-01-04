//+:cnd:noEmit
//#if (framework == 'net9.0')
using Maui.AppStores;
using Maui.InAppReviews;
using System.Runtime.InteropServices;
//#endif
using Microsoft.Extensions.Logging;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]

namespace Boilerplate.Client.Maui;

public partial class App
{
    private readonly Page mainPage;
    //#if (framework == 'net9.0')
    private readonly IStorageService storageService;
    //#endif
    private readonly ILogger<App> logger;
    private readonly IExceptionHandler exceptionHandler;
    private readonly IBitDeviceCoordinator deviceCoordinator;
    private readonly IStringLocalizer<AppStrings> localizer;

    public App(MainPage mainPage,
        //#if (framework == 'net9.0')
        PubSubService pubSubService,
        IStorageService storageService,
        //#endif
        ILogger<App> logger,
        IExceptionHandler exceptionHandler,
        IBitDeviceCoordinator deviceCoordinator,
        IStringLocalizer<AppStrings> localizer)
    {
        this.logger = logger;
        this.localizer = localizer;
        //#if (framework == 'net9.0')
        this.storageService = storageService;
        //#endif
        this.exceptionHandler = exceptionHandler;
        this.deviceCoordinator = deviceCoordinator;
        this.mainPage = new NavigationPage(mainPage);

        //#if (framework == 'net9.0')
        pubSubService.Subscribe(ClientPubSubMessages.PROFILE_UPDATED, async _ =>
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
        //#endif

        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(mainPage) { };
    }

    protected override async void OnStart()
    {
        try
        {
            base.OnStart();

            await deviceCoordinator.ApplyTheme(AppInfo.Current.RequestedTheme is AppTheme.Dark);

            //-:cnd:noEmit
#if Android
            //+:cnd:noEmit
            //#if (framework == 'net9.0')
            const int minimumSupportedWebViewVersion = 94;
            // Download link for Android emulator (x86 or x86_64)
            // https://www.apkmirror.com/apk/google-inc/chrome/chrome-94-0-4606-50-release/
            // https://www.apkmirror.com/apk/google-inc/android-system-webview/android-system-webview-94-0-4606-85-release/
            //#elif (framework == 'net8.0')
            //#if (IsInsideProjectTemplate)
            /*
            //#endif
            const int minimumSupportedWebViewVersion = 84;
            // Download link for Android emulator (x86 or x86_64)
            // https://www.apkmirror.com/apk/google-inc/chrome/chrome-84-0-4147-89-release/
            // https://www.apkmirror.com/apk/google-inc/android-system-webview/android-system-webview-84-0-4147-111-release/
            //#if (IsInsideProjectTemplate)
            */
            //#endif
            //#endif

            if (Version.TryParse(Android.Webkit.WebView.CurrentWebViewPackage?.VersionName, out var webViewVersion) &&
                webViewVersion.Major < minimumSupportedWebViewVersion)
            {
                var webViewName = Android.Webkit.WebView.CurrentWebViewPackage.PackageName;
                logger.LogWarning("{webViewName} version {version} is not supported.", webViewName, webViewVersion);
                await App.Current!.Windows[0].Page!.DisplayAlert("Boilerplate", localizer[nameof(AppStrings.UpdateWebViewThroughGooglePlay)], localizer[nameof(AppStrings.Ok)]);
                await Launcher.OpenAsync($"https://play.google.com/store/apps/details?id={webViewName}");
            }
            //-:cnd:noEmit
#endif
            //+:cnd:noEmit

            //#if (framework == 'net9.0')
            await CheckForUpdates();
            //#endif
        }
        catch (Exception exp)
        {
            exceptionHandler.Handle(exp);
        }
    }

    //#if (framework == 'net9.0')
    private async Task CheckForUpdates()
    {
        await Task.Delay(TimeSpan.FromSeconds(3)); // No rush to check for updates.

        try
        {
            if (await AppStoreInfo.Current.IsUsingLatestVersionAsync() is false)
            {
                var newVersion = await AppStoreInfo.Current.GetLatestVersionAsync();
                var releaseNotes = (await AppStoreInfo.Current.GetInformationAsync()).ReleaseNotes;

                if (await storageService.GetItem($"{newVersion}_UpdateFromVersionIsRequested") is not "true")
                {
                    await storageService.SetItem($"{newVersion}_UpdateFromVersionIsRequested", "true");

                    // It's an opportune moment to request an update. (:
                    // https://github.com/oscoreio/Maui.AppStoreInfo
                    if (await App.Current!.Windows[0].Page!.DisplayAlert(localizer[nameof(AppStrings.NewVersionIsAvailable), newVersion], localizer[nameof(AppStrings.UpdateToNewVersion), releaseNotes], localizer[nameof(AppStrings.Yes)], localizer[nameof(AppStrings.No)]) is true)
                    {
                        await AppStoreInfo.Current.OpenApplicationInStoreAsync();
                    }
                }
            }
        }
        catch (InvalidOperationException) when ((AppPlatform.IsIOS || AppPlatform.IsMacOS) && AppEnvironment.IsDev()) { }
        catch (FileNotFoundException) { }
        catch (COMException) { }
    }
    //#endif
}
