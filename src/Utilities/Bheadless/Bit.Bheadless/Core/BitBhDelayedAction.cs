namespace Bit.Bheadless;

/// <summary>
/// A cancellable, restartable delayed action (hover intent, close grace periods).
/// The callback runs through the owner's dispatcher (<c>InvokeAsync</c>). The delays run on the
/// <see cref="TimeProvider"/> registered in the services, else on the system clock.
/// </summary>
internal sealed class BitBhDelayedAction : IDisposable
{
    private readonly Func<Func<Task>, Task> _dispatch;
    private readonly TimeProvider _clock;
    private CancellationTokenSource? _cts;

    public BitBhDelayedAction(Func<Func<Task>, Task> dispatch, TimeProvider? clock = null)
    {
        _dispatch = dispatch;
        _clock = clock ?? TimeProvider.System;
    }

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
        await Task.Delay(TimeSpan.FromMilliseconds(delayMs), _clock, cts.Token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        if (cts.IsCancellationRequested) return;
        await _dispatch(() =>
        {
            // Re-checked on the dispatcher: Cancel() may have run while this was queued.
            if (cts.IsCancellationRequested) return Task.CompletedTask;
            if (ReferenceEquals(_cts, cts))
            {
                _cts = null;
                cts.Dispose();
            }
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
