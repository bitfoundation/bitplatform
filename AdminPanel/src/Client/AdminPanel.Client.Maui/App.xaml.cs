[assembly: XamlCompilation(XamlCompilationOptions.Compile)]

namespace AdminPanel.Client.Maui;

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

        }
        catch (Exception exp)
        {
            exceptionHandler.Handle(exp);
        }
    }
}
