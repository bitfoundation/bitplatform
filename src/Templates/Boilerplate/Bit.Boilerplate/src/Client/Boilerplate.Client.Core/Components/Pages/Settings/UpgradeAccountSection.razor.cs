//+:cnd:noEmit
namespace Boilerplate.Client.Core.Components.Pages.Settings;

public partial class UpgradeAccountSection
{
    [AutoInject] private IAdsService adsService { get; set; } = default!;
    [AutoInject] private ConsentService consentService { get; set; } = default!;
    [AutoInject] private ClientCoreSettings clientCoreSettings { get; set; } = default!;
    [AutoInject] private ILogger<AdsService> logger { get; set; } = default!;


    private bool adIsShown;
    private AdInitResult? adInitResult;
    private AdWatchResult? watchResult = null;
    //#if (signalR == true)
    private bool showTroubleButton;
    //#endif


    protected override async Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        //#if (signalR == true)
        // The check and the assignment run together on the renderer, so an init that completes while this callback
        // is in flight cannot end with the trouble button shown over a working ad.
        _ = Task.Delay(5000).ContinueWith(_ => InvokeAsync(() =>
        {
            if (adInitResult is not null) return;

            showTroubleButton = true;
            StateHasChanged();
        }));
        //#endif

        adInitResult = await adsService.Init(clientCoreSettings.AdUnitPath);

        //#if (signalR == true)
        // An unanswered consent question is not trouble with the ad; offering to troubleshoot sends the user
        // looking for a fault that is not there.
        showTroubleButton = adInitResult is not AdInitResult.Ready and not AdInitResult.ConsentRequired;
        //#endif

        StateHasChanged();
    }

    /// <summary>
    /// The same decision the banner asks for, stored the same way, so the settings toggle reflects it rather than
    /// this being a "just this once" the user cannot find again.
    /// </summary>
    private async Task GrantAdvertisingConsent()
    {
        await consentService.Set(ConsentCategory.Advertising, granted: true);

        adInitResult = await adsService.Init(clientCoreSettings.AdUnitPath);
    }


    private async Task WatchAd()
    {
        if (adInitResult is not AdInitResult.Ready || adIsShown) return;

        //#if (signalR == true)
        _ = Task.Delay(3000).ContinueWith(_ => InvokeAsync(() =>
        {
            if (watchResult is not null || adIsShown) return;

            showTroubleButton = true;
            StateHasChanged();
        }));
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
