//-:cnd:noEmit
using Maui.AppStores;
using System.Runtime.InteropServices;
using Plugin.LocalNotification;
using Plugin.LocalNotification.EventArgs;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]

namespace Boilerplate.Client.Maui;

public partial class App
{
    private readonly Page mainPage;
    private readonly IExceptionHandler exceptionHandler;
    private readonly IBitDeviceCoordinator deviceCoordinator;
    private readonly IStorageService storageService;

    public App(MainPage mainPage,
        IExceptionHandler exceptionHandler,
        IBitDeviceCoordinator deviceCoordinator,
        IStorageService storageService)
    {
        this.exceptionHandler = exceptionHandler;
        this.deviceCoordinator = deviceCoordinator;
        this.storageService = storageService;
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

            if (await LocalNotificationCenter.Current.AreNotificationsEnabled() == false)
            {
                await LocalNotificationCenter.Current.RequestNotificationPermission();
            }

            await Permissions.RequestAsync<Permissions.PostNotifications>();

            LocalNotificationCenter.Current.NotificationActionTapped += OnNotificationActionTapped;

            await CheckForUpdates();
        }
        catch (Exception exp)
        {
            exceptionHandler.Handle(exp);
        }
    }

    private async void OnNotificationActionTapped(NotificationActionEventArgs e)
    {
        try
        {
            if (e.IsTapped)
            {
                switch (e.Request.ReturningData)
                {
                    case nameof(AppStrings.NewVersionIsAvailable):
                        await AppStoreInfo.Current.OpenApplicationInStoreAsync();
                        break;
                }
            }
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
                if (await storageService.GetItem($"{AppInfo.Version}_UpdateFromVersionIsRequested") is not "true")
                {
                    await storageService.SetItem($"{AppInfo.Version}_UpdateFromVersionIsRequested", "true");

                    await LocalNotificationCenter.Current.Show(new()
                    {
                        Title = AppStrings.NewVersionIsAvailable,
                        Description = AppStrings.UpdateToNewVersion,
                        ReturningData = nameof(AppStrings.NewVersionIsAvailable) // See OnNotificationActionTapped
                    });
                }
            }
        }
        catch (InvalidOperationException) when ((AppPlatform.IsIOS || AppPlatform.IsMacOS) && AppEnvironment.IsDev()) { }
        catch (FileNotFoundException) { }
        catch (COMException) { }
    }
}
