namespace Bit.BlazorUI.Demo.Client.Maui;

public partial class MainPage
{
    private readonly IExceptionHandler exceptionHandler;
    private readonly IBitDeviceCoordinator deviceCoordinator;

    public MainPage(IExceptionHandler exceptionHandler, IBitDeviceCoordinator deviceCoordinator)
    {
        this.exceptionHandler = exceptionHandler;
        this.deviceCoordinator = deviceCoordinator;

        InitializeComponent();
    }

    protected async override void OnAppearing()
    {
        try
        {
            base.OnAppearing();

            await deviceCoordinator.ApplyTheme(AppInfo.Current.RequestedTheme is AppTheme.Dark);
        }
        catch (Exception exp)
        {
            exceptionHandler.Handle(exp);
        }
    }
}
