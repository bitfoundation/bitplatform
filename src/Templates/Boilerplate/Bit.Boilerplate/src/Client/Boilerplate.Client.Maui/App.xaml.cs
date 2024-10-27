[assembly: XamlCompilation(XamlCompilationOptions.Compile)]

namespace Boilerplate.Client.Maui;

public partial class App
{
    private readonly Page mainPage;
    private readonly IExceptionHandler exceptionHandler;
    private readonly IBitDeviceCoordinator deviceCoordinator;
    private readonly IStringLocalizer<AppStrings> localizer;

    public App(MainPage mainPage,
        IExceptionHandler exceptionHandler,
        IBitDeviceCoordinator deviceCoordinator,
        IStorageService storageService,
        IStringLocalizer<AppStrings> localizer)
    {
        this.exceptionHandler = exceptionHandler;
        this.deviceCoordinator = deviceCoordinator;
        this.mainPage = new NavigationPage(mainPage);
        this.localizer = localizer;

        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(mainPage) { };
    }

    protected async override void OnStart()
    {
        try
        {
            base.OnStart();

            await deviceCoordinator.ApplyTheme(AppInfo.Current.RequestedTheme is AppTheme.Dark);

#if Android
            if (Version.TryParse(Android.Webkit.WebView.CurrentWebViewPackage?.VersionName, out var webViewVersion) &&
        webViewVersion.Major < 83)
            {
                await App.Current!.Windows.First().Page!.DisplayAlert("Boilerplate", localizer[nameof(AppStrings.UpdateWebViewThroughGooglePlay)], localizer[nameof(AppStrings.Ok)]);
                await Launcher.OpenAsync($"https://play.google.com/store/apps/details?id={Android.Webkit.WebView.CurrentWebViewPackage.PackageName}");
                // Download for Android Emulator x86 + x86_64: https://www.apkmirror.com/apk/google-inc/android-system-webview/variant-%7B%22arches_slug%22%3A%5B%22x86%22%2C%22x86_64%22%5D%2C%22minapi_slug%22%3A%22minapi-26%22%7D/
                // Download for Android Device arm64-v8a + arm-v7a: https://www.apkmirror.com/apk/google-inc/android-system-webview/variant-%7B%22arches_slug%22%3A%5B%22arm64-v8a%22%2C%22armeabi-v7a%22%5D%2C%22minapi_slug%22%3A%22minapi-26%22%7D/
            }
#endif
        }
        catch (Exception exp)
        {
            exceptionHandler.Handle(exp);
        }
    }
}
