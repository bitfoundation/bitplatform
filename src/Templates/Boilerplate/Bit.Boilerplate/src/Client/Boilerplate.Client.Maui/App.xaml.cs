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

            //+:cnd:noEmit
            //#if (framework == 'net9.0')
            const int minimumSupportedWebViewVersion = 94;
            //#elif (framework == 'net8.0')
            //#if (IsInsideProjectTemplate)
            /*
            //#endif
            const int minimumSupportedWebViewVersion = 83;
            //#if (IsInsideProjectTemplate)
            */
            //#endif
            //#endif
            //-:cnd:noEmit

            if (Version.TryParse(Android.Webkit.WebView.CurrentWebViewPackage?.VersionName, out var webViewVersion) &&
        webViewVersion.Major < minimumSupportedWebViewVersion)
            {
                await App.Current!.Windows.First().Page!.DisplayAlert("Boilerplate", localizer[nameof(AppStrings.UpdateWebViewThroughGooglePlay)], localizer[nameof(AppStrings.Ok)]);
                await Launcher.OpenAsync($"https://play.google.com/store/apps/details?id={Android.Webkit.WebView.CurrentWebViewPackage.PackageName}");
            }
#endif
        }
        catch (Exception exp)
        {
            exceptionHandler.Handle(exp);
        }
    }
}
