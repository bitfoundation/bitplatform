//-:cnd:noEmit
using Maui.AppStores;
//#if (notification == true)
using Plugin.LocalNotification;
//#endif
using System.Runtime.InteropServices;

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

            //#if (notification == true)
            if (await LocalNotificationCenter.Current.AreNotificationsEnabled() == false)
            {
                await LocalNotificationCenter.Current.RequestNotificationPermission();
            }
            //#endif

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
                if (await storageService.GetItem($"{AppInfo.Version}_UpdateFromVersionIsRequested") is not "true")
                {
                    await storageService.SetItem($"{AppInfo.Version}_UpdateFromVersionIsRequested", "true");

                    // It's an opportune moment to request an update. (:
                    // https://github.com/oscoreio/Maui.AppStoreInfo
                    if (await App.Current!.MainPage!.DisplayAlert(AppStrings.NewVersionIsAvailable, AppStrings.UpdateToNewVersion, AppStrings.Yes, AppStrings.No) is true)
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
