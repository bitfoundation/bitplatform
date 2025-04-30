namespace Boilerplate.Client.Core.Components.Pages.Authorized.Settings;

public partial class UpgradeAccountSection
{
    [AutoInject] private IAdsService adsService { get; set; } = default!;
    [AutoInject] private ClientCoreSettings clientCoreSettings { get; set; } = default!;


    private bool adIsReady;


    protected override async Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        await adsService.Init(clientCoreSettings.AdUnitPath);

        adIsReady = true;
    }


    private async Task WatchAdd()
    {
        if (adIsReady is false) return;

        await adsService.Watch();
    }
}
