namespace Bit.Blex.Demo.Client.Stores;

/// <summary>
/// An async <c>[BlexActionAttribute]</c> that changes state many times before it finishes. Each change after an
/// <c>await</c> notifies at once, so the UI follows the progress, yet the whole run is recorded as one
/// <c>progress/Run</c> action - one feed entry, one undo step. The actions page shows that; the subscriptions page
/// uses the same burst of changes to show <c>RenderInterval</c> throttling the renders it causes.
/// </summary>
[BlexStoreAttribute(Name = "progress")]
public partial class ProgressStore
{
    [BlexStateAttribute] private int _percent;
    [BlexStateAttribute] private bool _isRunning;
    [BlexStateAttribute] private int _runs;

    /// <summary>Moves <c>Percent</c> from 0 to 100 in <paramref name="steps"/> steps, <paramref name="delayMilliseconds"/> apart.</summary>
    [BlexActionAttribute]
    private async Task OnRun(int steps, int delayMilliseconds)
    {
        IsRunning = true;
        Percent = 0;

        for (var step = 1; step <= steps; step++)
        {
            await Task.Delay(delayMilliseconds);
            Percent = step * 100 / steps;   // renders now, recorded with the rest when the run ends
        }

        IsRunning = false;
        Runs++;
    }
}
