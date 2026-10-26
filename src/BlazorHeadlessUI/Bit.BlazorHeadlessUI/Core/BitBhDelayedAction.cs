namespace Bit.BlazorHeadlessUI;

/// <summary>
/// A cancellable, restartable delayed action (hover intent, close grace periods).
/// The callback runs through the owner's dispatcher (<c>InvokeAsync</c>).
/// </summary>
internal sealed class BitBhDelayedAction : IDisposable
{
    private readonly Func<Func<Task>, Task> _dispatch;
    private CancellationTokenSource? _cts;

    public BitBhDelayedAction(Func<Func<Task>, Task> dispatch) => _dispatch = dispatch;

    /// <summary>Whether a run is scheduled.</summary>
    public bool IsPending => _cts is { IsCancellationRequested: false };

    /// <summary>Schedules <paramref name="action"/> after <paramref name="delayMs"/>, cancelling any pending run.</summary>
    public void Schedule(int delayMs, Func<Task> action)
    {
        Cancel();
        if (delayMs <= 0)
        {
            _ = _dispatch(action);
            return;
        }
        var cts = _cts = new CancellationTokenSource();
        _ = RunAsync(delayMs, cts, action);
    }

    private async Task RunAsync(int delayMs, CancellationTokenSource cts, Func<Task> action)
    {
        // Wait off the dispatcher: a cancelled delay (every restart) must not queue work on the renderer.
        await Task.Delay(delayMs, cts.Token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        if (cts.IsCancellationRequested) return;
        await _dispatch(() =>
        {
            // Re-checked on the dispatcher: Cancel() may have run while this was queued.
            if (cts.IsCancellationRequested) return Task.CompletedTask;
            if (ReferenceEquals(_cts, cts)) _cts = null;
            return action();
        });
    }

    /// <summary>Cancels the pending run, if any.</summary>
    public void Cancel()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    public void Dispose() => Cancel();
}
