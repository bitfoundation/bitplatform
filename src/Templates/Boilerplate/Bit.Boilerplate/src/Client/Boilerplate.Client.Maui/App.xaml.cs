using Maui.AppStores;
using Maui.InAppReviews;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]

namespace Boilerplate.Client.Maui;

public partial class App
{
    private readonly Page mainPage;
    private readonly IStorageService storageService;

    private readonly ILogger<App> logger;
    private readonly IExceptionHandler exceptionHandler;
    private readonly IBitDeviceCoordinator deviceCoordinator;
    private readonly IStringLocalizer<AppStrings> localizer;

    public App(MainPage mainPage,
        PubSubService pubSubService,
        IStorageService storageService,
        ILogger<App> logger,
        IExceptionHandler exceptionHandler,
        IBitDeviceCoordinator deviceCoordinator,
        IStringLocalizer<AppStrings> localizer)
    {
        this.logger = logger;
        this.localizer = localizer;
        this.storageService = storageService;
        this.exceptionHandler = exceptionHandler;
        this.deviceCoordinator = deviceCoordinator;
        this.mainPage = new NavigationPage(mainPage);

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
            const int minimumSupportedWebViewVersion = 85;
            // Download link for Android emulator (x86 or x86_64)
            // https://www.apkmirror.com/apk/google-inc/chrome/chrome-85-0-4183-127-release/
            // https://www.apkmirror.com/apk/google-inc/android-system-webview/android-system-webview-85-0-4183-127-release/

            if (Version.TryParse(Android.Webkit.WebView.CurrentWebViewPackage?.VersionName, out var webViewVersion) &&
                webViewVersion.Major < minimumSupportedWebViewVersion)
            {
                var webViewName = Android.Webkit.WebView.CurrentWebViewPackage.PackageName;
                logger.LogWarning("{webViewName} version {version} is not supported.", webViewName, webViewVersion);
                await App.Current!.Windows[0].Page!.DisplayAlert("Boilerplate", localizer[nameof(AppStrings.UpdateWebViewThroughGooglePlay)], localizer[nameof(AppStrings.Ok)]);
                await Launcher.OpenAsync($"https://play.google.com/store/apps/details?id={webViewName}");
            }
#endif
            //+:cnd:noEmit
            await CheckForUpdates();
        }
        catch (Exception exp)
        {
            exceptionHandler.Handle(exp);
        }
    }

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
}
