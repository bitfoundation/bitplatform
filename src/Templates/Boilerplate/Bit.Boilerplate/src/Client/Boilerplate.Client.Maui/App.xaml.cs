//+:cnd:noEmit
[assembly: XamlCompilation(XamlCompilationOptions.Compile)]

namespace Boilerplate.Client.Maui;

public partial class App
{
    private readonly Page mainPage;
    private readonly IExceptionHandler exceptionHandler;
    private readonly IBitDeviceCoordinator deviceCoordinator;

    public App(MainPage mainPage,
        IExceptionHandler exceptionHandler,
        IBitDeviceCoordinator deviceCoordinator,
        IStorageService storageService)
    {
        this.exceptionHandler = exceptionHandler;
        this.deviceCoordinator = deviceCoordinator;
        this.mainPage = new NavigationPage(mainPage);

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
