//-:cnd:noEmit
using Maui.AppStores;
using Maui.InAppReviews;

namespace Boilerplate.Client.Maui;

public partial class MainPage
{
    private readonly IExceptionHandler exceptionHandler;
    private readonly IBitDeviceCoordinator deviceCoordinator;
    private readonly IStorageService storageService;

    public MainPage(IExceptionHandler exceptionHandler,
        IBitDeviceCoordinator deviceCoordinator,
        IPubSubService pubSubService,
        IStorageService storageService)
    {
        this.exceptionHandler = exceptionHandler;
        this.deviceCoordinator = deviceCoordinator;
        this.storageService = storageService;

        InitializeComponent();

        pubSubService.Subscribe(PubSubMessages.USER_DATA_UPDATED, async _ =>
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
    }

    protected override async void OnAppearing()
    {
        try
        {
            base.OnAppearing();

            await deviceCoordinator.ApplyTheme(AppInfo.Current.RequestedTheme is AppTheme.Dark);

            await CheckForUpdates();
        }
        catch (Exception exp)
        {
            exceptionHandler.Handle(exp);
        }
    }

    private async Task CheckForUpdates()
    {
        if (OperatingSystem.IsAndroid()) // We're using in app updates for android thanks to Oscore.Maui.Android.InAppUpdates
            return;

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
                    if (await DisplayAlert(AppStrings.NewVersionIsAvailable, AppStrings.UpdateToNewVersion, AppStrings.Yes, AppStrings.No) is true)
                    {
                        await AppStoreInfo.Current.OpenApplicationInStoreAsync();
                    }
                }
            }
        }
        catch (InvalidOperationException) when ((OperatingSystem.IsIOS() || AppRenderMode.IsRunningOnMacOS) && AppEnvironment.IsDev()) { }
        catch (FileNotFoundException) { }
    }
}
