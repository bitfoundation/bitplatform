namespace Bit.BlazorUI;

/// <summary>
/// Waits out a quiet period that the next wait, or a <see cref="Cancel"/>, supersedes - so of a burst of
/// keystrokes only the last one goes on to apply. One per box: the quick-search box, and each column's filter box.
/// </summary>
internal sealed class BitDataGridDebouncer
{
    private CancellationTokenSource? _cts;

    /// <summary>Whether a wait is under way that has neither run out nor been superseded.</summary>
    public bool IsPending => _cts is not null;

    /// <summary>
    /// Supersedes any wait under way, then waits <paramref name="milliseconds"/>. True when the wait ran out with
    /// nothing superseding it, so the caller goes on to apply; false when a newer wait or a cancel took over.
    /// </summary>
    public async Task<bool> WaitAsync(int milliseconds)
    {
        Cancel();
        var cts = _cts = new CancellationTokenSource();
        var superseded = false;
        try
        {
            await Task.Delay(milliseconds, cts.Token);
        }
        catch (OperationCanceledException)
        {
            superseded = true;
        }
        finally
        {
            // Each wait disposes the source it created, whether it ran out or was superseded, so a long burst of
            // typing doesn't leave one per character for the GC. A wait that ran out drops its own source first, so
            // the apply that follows it can call Cancel without cancelling itself.
            if (ReferenceEquals(_cts, cts)) _cts = null;
            // The delay can finish just before a newer keystroke cancels it, with this continuation still queued
            // behind that keystroke's handler; the cancellation then arrives too late to throw, so it is read here.
            superseded |= cts.IsCancellationRequested;
            cts.Dispose();
        }
        return !superseded;
    }

    /// <summary>Supersedes the wait under way, if any.</summary>
    public void Cancel()
    {
        _cts?.Cancel();
        _cts = null;
    }
}
