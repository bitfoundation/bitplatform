//+:cnd:noEmit
namespace Boilerplate.Client.Core.Components.Pages.Settings;

public partial class UpgradeAccountSection
{
    [AutoInject] private IAdsService adsService { get; set; } = default!;
    [AutoInject] private ClientCoreSettings clientCoreSettings { get; set; } = default!;
    [AutoInject] private ILogger<AdsService> logger { get; set; } = default!;


    private bool adIsReady;
    private bool adIsShown;
    private AdWatchResult? watchResult = null;
    //#if (signalR == true)
    private bool showTroubleButton;
    //#endif


    protected override async Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        //#if (signalR == true)
        _ = Task.Delay(5000).ContinueWith(_ =>
        {
            if (adIsReady) return;

            showTroubleButton = true;
            InvokeAsync(StateHasChanged);
        });
        //#endif

        await adsService.Init(clientCoreSettings.AdUnitPath);

        adIsReady = true;
        //#if (signalR == true)
        showTroubleButton = false;
        //#endif

        StateHasChanged();
    }


    private async Task WatchAd()
    {
        if (adIsReady is false || adIsShown) return;

        //#if (signalR == true)
        _ = Task.Delay(3000).ContinueWith(_ =>
        {
            if (watchResult is not null || adIsShown) return;

            showTroubleButton = true;
            InvokeAsync(StateHasChanged);
        });
        //#endif

        watchResult = await adsService.Watch();

        adIsShown = true;
        //#if (signalR == true)
        showTroubleButton = false;
        //#endif

        StateHasChanged();

        if (watchResult is AdWatchResult.Rewarded)
        {
            SnackBarService.Success(Localizer[nameof(AppStrings.UpgradeSuccessMessage)]);
        }
        else
        {
            SnackBarService.Error(Localizer[nameof(AppStrings.UpgradeFailMessage)]);
        }
    }

    //#if (signalR == true)
    private async Task HandleAdTrouble()
    {
        logger.LogWarning("User having trouble with ads");
        PubSubService.Publish(ClientAppMessages.AD_HAVE_TROUBLE);
    }
    //#endif
}
