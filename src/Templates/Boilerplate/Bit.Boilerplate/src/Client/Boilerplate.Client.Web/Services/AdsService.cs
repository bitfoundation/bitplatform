namespace Boilerplate.Client.Web.Services;

public partial class AdsService : IAdsService
{
    [AutoInject] private IJSRuntime jsRuntime = default!;


    private TaskCompletionSource initTsc = new();
    private DotNetObjectReference<AdsService>? dotnetObj;


    public ValueTask Init(string adUnitPath)
    {
        dotnetObj = DotNetObjectReference.Create(this);

        return jsRuntime.InvokeVoidAsync("Ads.init", adUnitPath, dotnetObj);
    }

    public ValueTask Watch()
    {
        return jsRuntime.InvokeVoidAsync("Ads.watch");
    }


    [JSInvokable(nameof(AdNotSupported))]
    public async Task AdNotSupported() { }

    [JSInvokable(nameof(AdReady))]
    public async Task AdReady()
    {
        initTsc.SetResult();
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
}
