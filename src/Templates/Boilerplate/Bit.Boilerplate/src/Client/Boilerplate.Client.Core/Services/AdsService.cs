namespace Boilerplate.Client.Core.Services;

public partial class AdsService : IAdsService
{
    [AutoInject] private IJSRuntime jsRuntime = default!;


    private TaskCompletionSource? initTsc;
    private TaskCompletionSource<AdWatchResult>? watchTsc;
    private DotNetObjectReference<AdsService>? dotnetObj;


    public async Task Init(string adUnitPath)
    {
        if (initTsc is not null) return;

        initTsc = new();
        dotnetObj = DotNetObjectReference.Create(this);

        await jsRuntime.InvokeVoidAsync("Ads.init", adUnitPath, dotnetObj);
        await initTsc.Task;
    }

    public async Task<AdWatchResult> Watch()
    {
        watchTsc?.TrySetCanceled();
        watchTsc = null;
        watchTsc = new();

        await jsRuntime.InvokeVoidAsync("Ads.watch");
        return await watchTsc.Task;
    }

    [JSInvokable(nameof(ScriptFailed))]
    public async Task ScriptFailed()
    {
        initTsc?.SetCanceled();
    }

    [JSInvokable(nameof(AdNotSupported))]
    public async Task AdNotSupported()
    {
        initTsc?.SetCanceled();
    }

    [JSInvokable(nameof(AdReady))]
    public async Task AdReady()
    {
        initTsc?.SetResult();
    }

    [JSInvokable(nameof(AdClosed))]
    public async Task AdClosed(int? rewardAmount, string? rewardType)
    {
        if (rewardAmount.HasValue is false)
        {
            watchTsc?.SetResult(AdWatchResult.Failed);
        }
    }

    [JSInvokable(nameof(AdRewardGranted))]
    public async Task AdRewardGranted(int? rewardAmount, string? rewardType)
    {
        if (rewardAmount.HasValue)
        {
            watchTsc?.SetResult(AdWatchResult.Rewarded);
        }
    }

    [JSInvokable(nameof(AdSlotRendered))]
    public async Task AdSlotRendered(bool isEmpty)
    {

    }

    [JSInvokable(nameof(AdNotAvailable))]
    public async Task AdNotAvailable()
    {
    }

    [JSInvokable(nameof(AdVisible))]
    public async Task AdVisible()
    {
    }
}
