namespace Bit.Bmage.Demo.Client.Shared;

/// <summary>
/// Coalesces rapid updates into one action after a quiet period.
/// </summary>
/// <remarks>
/// Dragging a slider fires an event per pixel of travel. Re-running an image pipeline that often would queue work
/// faster than it completes, so each change restarts a short timer and only the last one does any work. The pending
/// action is cancelled when a newer one arrives, which also cancels the pipeline run already in flight.
/// </remarks>
public sealed class Debouncer : IDisposable
{
    private readonly TimeSpan _delay;
    private CancellationTokenSource? _cts;

    public Debouncer(int milliseconds = 180) => _delay = TimeSpan.FromMilliseconds(milliseconds);

    /// <summary>
    /// Schedules <paramref name="action"/> to run after the quiet period, cancelling any previously scheduled run.
    /// The action receives a token that is cancelled if another update arrives while it is running.
    /// </summary>
    public void Schedule(Func<CancellationToken, Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var previous = Interlocked.Exchange(ref _cts, null);
        previous?.Cancel();
        previous?.Dispose();

        var cts = new CancellationTokenSource();
        _cts = cts;
        _ = RunAsync(action, cts);
    }

    private async Task RunAsync(Func<CancellationToken, Task> action, CancellationTokenSource cts)
    {
        try
        {
            await Task.Delay(_delay, cts.Token).ConfigureAwait(true);
            await action(cts.Token).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer update.
        }
        finally
        {
            if (Interlocked.CompareExchange(ref _cts, null, cts) == cts) cts.Dispose();
        }
    }

    public void Dispose()
    {
        var cts = Interlocked.Exchange(ref _cts, null);
        try { cts?.Cancel(); } catch (ObjectDisposedException) { }
        cts?.Dispose();
    }
}
