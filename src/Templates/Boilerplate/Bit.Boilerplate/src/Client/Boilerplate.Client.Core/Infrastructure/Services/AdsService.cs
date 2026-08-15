namespace Boilerplate.Client.Core.Infrastructure.Services;

public partial class AdsService : IAdsService, IAsyncDisposable
{
    [AutoInject] private IJSRuntime jsRuntime = default!;
    [AutoInject] private TimeProvider timeProvider = default!;
    [AutoInject] private ILogger<AdsService> logger = default!;

    /// <summary>
    /// Google Publisher Tag reports an unfilled slot through slotRenderEnded, but a slot that never renders at all
    /// reports nothing, so the wait needs its own deadline - otherwise the caller shows a loading indicator forever.
    /// </summary>
    private static readonly TimeSpan initTimeout = TimeSpan.FromSeconds(15);

    private TaskCompletionSource<AdInitResult>? initTsc;
    private TaskCompletionSource<AdWatchResult>? watchTsc;
    private DotNetObjectReference<AdsService>? dotnetObj;


    public async Task<AdInitResult> Init(string adUnitPath)
    {
        if (initTsc is not null) return await initTsc.Task; // An init that is already running or already succeeded.

        var tsc = initTsc = new();
        dotnetObj ??= DotNetObjectReference.Create(this);

        try
        {
            // Armed before the interop call, and the call is deliberately not awaited before the result: Ads.init
            // awaits the ad script's own load, so a CDN that never settles either way would leave this pending with
            // no deadline running at all - which is the "spins forever" this method exists to prevent.
            using var timeoutCts = new CancellationTokenSource(initTimeout, timeProvider);
            using var timeoutRegistration = timeoutCts.Token.Register(() => tsc.TrySetResult(AdInitResult.NotAvailable));

            var jsInit = jsRuntime.InvokeVoidAsync("Ads.init", adUnitPath, dotnetObj).AsTask();

            // An interop failure is a failed init rather than an unobserved task exception.
            _ = jsInit.ContinueWith(t =>
            {
                _ = t.Exception;
                tsc.TrySetResult(AdInitResult.ScriptFailed);
            }, TaskContinuationOptions.OnlyOnFaulted);

            var result = await tsc.Task;

            if (result is AdInitResult.Ready)
            {
                logger.LogInformation("Ad is ready");
            }
            else
            {
                // Only a ready ad is remembered. Latching a failure would make the next attempt return immediately
                // and successfully, so the caller would enable a Watch that no slot can ever complete.
                initTsc = null;
                logger.LogWarning("Ad is not ready. {AdInitResult}", result);
            }

            return result;
        }
        catch
        {
            initTsc = null;
            throw;
        }
    }

    public async Task<AdWatchResult> Watch()
    {
        watchTsc?.TrySetCanceled();
        var tsc = watchTsc = new();

        // The JS side answers false when there is no live rewarded slot, which would otherwise leave this awaiting
        // a completion that can never arrive.
        if (await jsRuntime.InvokeAsync<bool>("Ads.watch") is false)
        {
            tsc.TrySetResult(AdWatchResult.Failed);
        }

        var result = await tsc.Task;

        // Showing an ad destroys the rewarded slot, so the next Init has to define a new one.
        initTsc = null;

        if (result is AdWatchResult.Rewarded)
        {
            logger.LogInformation("Ad rewarded");
        }
        else
        {
            logger.LogWarning("Ad failed");
        }

        return result;
    }

    [JSInvokable(nameof(ScriptFailed))]
    public void ScriptFailed()
    {
        initTsc?.TrySetResult(AdInitResult.ScriptFailed);
    }

    [JSInvokable(nameof(AdNotSupported))]
    public void AdNotSupported()
    {
        initTsc?.TrySetResult(AdInitResult.NotSupported);
    }

    [JSInvokable(nameof(AdReady))]
    public void AdReady()
    {
        initTsc?.TrySetResult(AdInitResult.Ready);
    }

    [JSInvokable(nameof(AdClosed))]
    public void AdClosed(int? rewardAmount, string? rewardType)
    {
        if (rewardAmount.HasValue is false)
        {
            watchTsc?.TrySetResult(AdWatchResult.Failed);
        }
    }

    [JSInvokable(nameof(AdRewardGranted))]
    public void AdRewardGranted(int? rewardAmount, string? rewardType)
    {
        if (rewardAmount.HasValue)
        {
            watchTsc?.TrySetResult(AdWatchResult.Rewarded);
        }
    }

    [JSInvokable(nameof(AdSlotRendered))]
    public void AdSlotRendered(bool isEmpty)
    {
        // Raised for every Google Publisher Tag slot on the page, not only the rewarded one.
    }

    [JSInvokable(nameof(AdNotAvailable))]
    public void AdNotAvailable()
    {
        initTsc?.TrySetResult(AdInitResult.NotAvailable);
    }

    [JSInvokable(nameof(AdVisible))]
    public void AdVisible()
    {
    }

    public async ValueTask DisposeAsync()
    {
        // Anything still awaiting one of these would otherwise wait for callbacks that can no longer be delivered.
        initTsc?.TrySetResult(AdInitResult.NotAvailable);
        watchTsc?.TrySetResult(AdWatchResult.Failed);

        dotnetObj?.Dispose();

        await Task.CompletedTask;
    }
}
