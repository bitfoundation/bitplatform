namespace Bit.BlazorUI;

/// <summary>
/// Timed work of a component - a confirmation that reverts, a message that goes away, a countdown that ticks - run on the
/// component's dispatcher, one run at a time: starting it again cancels the run before, and once disposed it starts
/// nothing more, so a run started by code that resumes after the component went away (an awaited callback of the app)
/// never outlives it.
/// </summary>
/// <param name="dispatch">How the work reaches the component: its <c>InvokeAsync</c>.</param>
internal sealed class BitAiTimer(Func<Action, Task> dispatch) : IDisposable
{
    private CancellationTokenSource? _cts;
    private bool _disposed;

    /// <summary>Runs <paramref name="action"/> once, after <paramref name="delay"/>, unless cancelled before.</summary>
    public void After(TimeSpan delay, Action action) => Start(async token =>
    {
        await Task.Delay(delay, token);

        await DispatchAsync(action, token);
    });

    /// <summary>Runs <paramref name="tick"/> every <paramref name="interval"/> until cancelled.</summary>
    public void Every(TimeSpan interval, Action tick) => Start(async token =>
    {
        using var timer = new PeriodicTimer(interval);

        while (await timer.WaitForNextTickAsync(token))
        {
            await DispatchAsync(tick, token);
        }
    });

    /// <summary>Cancels the run, if there is one; what it has not done yet is never done.</summary>
    public void Cancel()
    {
        if (_cts is { } cts)
        {
            _cts = null;
            cts.Cancel();
            cts.Dispose();
        }
    }

    public void Dispose()
    {
        _disposed = true;

        Cancel();
    }

    private void Start(Func<CancellationToken, Task> run)
    {
        Cancel();

        if (_disposed) return;

        var cts = new CancellationTokenSource();
        _cts = cts;

        _ = RunAsync(run, cts);
    }

    private async Task RunAsync(Func<CancellationToken, Task> run, CancellationTokenSource cts)
    {
        try
        {
            await run(cts.Token);
        }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { } // the renderer went first
        finally
        {
            // A run that ended on its own leaves nothing to cancel.
            if (ReferenceEquals(_cts, cts))
            {
                _cts = null;
                cts.Dispose();
            }
        }
    }

    // Checked again on the dispatcher, where a cancellation made after the wait ended but before this ran is seen.
    private Task DispatchAsync(Action action, CancellationToken token) => dispatch(() =>
    {
        if (token.IsCancellationRequested is false) action();
    });
}
