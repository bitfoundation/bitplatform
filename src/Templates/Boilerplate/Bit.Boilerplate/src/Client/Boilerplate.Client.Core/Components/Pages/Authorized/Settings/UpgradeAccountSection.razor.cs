namespace Boilerplate.Client.Core.Components.Pages.Authorized.Settings;

public partial class UpgradeAccountSection
{
    [AutoInject] private IAdsService adsService { get; set; } = default!;
    [AutoInject] private ClientCoreSettings clientCoreSettings { get; set; } = default!;


    private bool adIsReady;
    private bool adIsShown;
    private bool showTroubleButton;


    protected override async Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        _ = Task.Delay(5000).ContinueWith(_ =>
        {
            showTroubleButton = true;
            StateHasChanged();
        });

        await adsService.Init(clientCoreSettings.AdUnitPath);

        adIsReady = true;
        showTroubleButton = false;

        StateHasChanged();
    }


    private async Task WatchAd()
    {
        if (adIsReady is false || adIsShown) return;

        AdWatchResult? result = null;

        _ = Task.Delay(3000).ContinueWith(_ =>
        {
            if (result is not null || adIsShown) return;

            showTroubleButton = true;
            StateHasChanged();
        });

        result = await adsService.Watch();

        adIsShown = true;
        showTroubleButton = false;

        StateHasChanged();

        if (result is AdWatchResult.Rewarded)
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
        PubSubService.Publish(ClientPubSubMessages.AD_HAVE_TROUBLE);
    }
}
