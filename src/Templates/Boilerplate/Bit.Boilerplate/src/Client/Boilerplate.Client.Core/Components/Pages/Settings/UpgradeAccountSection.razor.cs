using Microsoft.Extensions.Logging;

namespace Boilerplate.Client.Core.Components.Pages.Settings;

public partial class UpgradeAccountSection
{
    [AutoInject] private IAdsService adsService { get; set; } = default!;
    [AutoInject] private ClientCoreSettings clientCoreSettings { get; set; } = default!;
    [AutoInject] private ILogger<AdsService> logger { get; set; } = default!;


    private bool adIsReady;
    private bool adIsShown;
    private bool showTroubleButton;
    private AdWatchResult? watchResult = null;


    protected override async Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        _ = Task.Delay(5000).ContinueWith(_ =>
        {
            if (adIsReady) return;

            showTroubleButton = true;
            InvokeAsync(StateHasChanged);
        });

        await adsService.Init(clientCoreSettings.AdUnitPath);

        adIsReady = true;
        showTroubleButton = false;

        StateHasChanged();
    }


    private async Task WatchAd()
    {
        if (adIsReady is false || adIsShown) return;

        _ = Task.Delay(3000).ContinueWith(_ =>
        {
            if (watchResult is not null || adIsShown) return;

            showTroubleButton = true;
            InvokeAsync(StateHasChanged);
        });

        watchResult = await adsService.Watch();

        adIsShown = true;
        showTroubleButton = false;

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

    private async Task HandleAdTrouble()
    {
        logger.LogWarning("User having trouble with ads");
        PubSubService.Publish(ClientAppMessages.AD_HAVE_TROUBLE);
    }
}
