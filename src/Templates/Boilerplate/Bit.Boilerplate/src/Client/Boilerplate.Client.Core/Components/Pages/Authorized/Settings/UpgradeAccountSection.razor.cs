namespace Boilerplate.Client.Core.Components.Pages.Authorized.Settings;

public partial class UpgradeAccountSection
{
    [JSInvokable(nameof(AdNotSupported))]
    public async Task AdNotSupported() { }

    [JSInvokable(nameof(AdReady))]
    public async Task AdReady()
    {
        adIsReady = true;
    }

    [JSInvokable(nameof(AdClosed))]
    public async Task AdClosed(int? rewardAmount, string? rewardType) { }

    [JSInvokable(nameof(AdRewardGranted))]
    public async Task AdRewardGranted(int? rewardAmount, string? rewardType) { }

    [JSInvokable(nameof(AdSlotRendered))]
    public async Task AdSlotRendered(bool isEmpty) { }

    [JSInvokable(nameof(AdNotAvailable))]
    public async Task AdNotAvailable() { }

    [JSInvokable(nameof(AdVisible))]
    public async Task AdVisible() { }


    [AutoInject] private IAdsService adsService { get; set; } = default!;
    [AutoInject] private ClientCoreSettings clientCoreSettings { get; set; } = default!;


    private bool adIsReady;
    private DotNetObjectReference<UpgradeAccountSection>? dotnetObj;


    protected override async Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        dotnetObj = DotNetObjectReference.Create(this);

        await adsService.Init(clientCoreSettings.AdUnitPath, dotnetObj);
    }


    private async Task WatchAdd()
    {
        if (adIsReady is false) return;

        await adsService.Watch();
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        dotnetObj?.Dispose();

        await base.DisposeAsync(disposing);
    }
}
