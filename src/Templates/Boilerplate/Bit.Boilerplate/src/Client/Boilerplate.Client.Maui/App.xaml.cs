using Plugin.Maui.AppRating;
using Microsoft.Extensions.Logging;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]

namespace Boilerplate.Client.Maui;

public partial class App
{
    private readonly Page mainPage;

    private readonly ILogger<App> logger;
    private readonly IExceptionHandler exceptionHandler;
    private readonly IBitDeviceCoordinator deviceCoordinator;
    private readonly IStringLocalizer<AppStrings> localizer;
    private readonly Action pubSubHandlerReferenceToKeepAlive;

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
        this.exceptionHandler = exceptionHandler;
        this.deviceCoordinator = deviceCoordinator;
        this.mainPage = new NavigationPage(mainPage);

        pubSubHandlerReferenceToKeepAlive = pubSubService.Subscribe(ClientPubSubMessages.PROFILE_UPDATED, async _ =>
        {
            // It's an opportune moment to request a store review. (:
            await Dispatcher.DispatchAsync(async () =>
            {
                try
                {
                    if ((await storageService.GetItem("StoreReviewRequested")) is "true")
                    {
                        logger.LogInformation("Store review request already sent");
                        return;
                    }

                    await AppRating.Default!.PerformInAppRateAsync(isTestOrDebugMode: AppEnvironment.IsDevelopment());
                    await storageService.SetItem("StoreReviewRequested", "true");

                    logger.LogInformation("Store review request sent");
                }
                catch (Exception exp)
                {
                    logger.LogError(message: "Store review request failed", exception: exp);
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
        }
        catch (Exception exp)
        {
            exceptionHandler.Handle(exp);
        }
    }
}
