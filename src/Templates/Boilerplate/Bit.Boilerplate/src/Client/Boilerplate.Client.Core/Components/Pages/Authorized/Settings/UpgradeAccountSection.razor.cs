namespace Boilerplate.Client.Core.Components.Pages.Authorized.Settings;

public partial class UpgradeAccountSection
{
    [AutoInject] private IAdsService adsService { get; set; } = default!;
    [AutoInject] private ClientCoreSettings clientCoreSettings { get; set; } = default!;


    private bool adIsReady;
    private bool adIsShown;


    protected override async Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        await adsService.Init(clientCoreSettings.AdUnitPath);

        adIsReady = true;

        StateHasChanged();
    }


    private async Task WatchAdd()
    {
        if (adIsReady is false) return;

        var result = await adsService.Watch();

        adIsShown = true;

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
}
