namespace Boilerplate.Client.Core.Components;

public partial class AppComponentBase
{
    /// <summary>
    /// Opt-in flag (disabled by default). When enabled, the renders triggered during the app's
    /// startup window are coalesced: a burst of <see cref="ComponentBase.StateHasChanged"/> calls
    /// collapses into a single render once the burst settles, instead of re-rendering this component
    /// (and its whole subtree) once per call.
    /// <para>
    /// This targets Blazor WebAssembly startup cost only, so it is active exclusively in the browser.
    /// The window is measured from app start (<see cref="startupTimestamp"/>), components created after <see cref="CoalesceRendersDuration"/> has elapsed
    /// never coalesce and render normally, keeping steady-state interactivity immediately responsive.
    /// </para>
    /// </summary>
    protected virtual bool CoalesceRenders => false;

    /// <summary>How long after app start the coalescing stays active.</summary>
    protected virtual TimeSpan CoalesceRendersDuration => TimeSpan.FromSeconds(3);

    /// <summary>The quiet period a render burst must settle for before it is flushed (extended on each new change).</summary>
    protected virtual TimeSpan CoalesceRendersWindow => TimeSpan.FromMilliseconds(300);


    private static readonly long startupTimestamp = Stopwatch.GetTimestamp();
    private bool passCoalescedRender = true;
    private bool coalesceDisposed;
    private ITimer? coalesceTimer;


    protected override bool ShouldRender()
    {
        // Only Blazor WebAssembly (where startup rendering is the bottleneck); everywhere else render normally.
        if (CoalesceRenders is false || AppPlatform.IsBrowser is false)
            return base.ShouldRender();

        // Past the startup window (measured from app start, not this component) → behave normally from here on.
        if (Stopwatch.GetElapsedTime(startupTimestamp) >= CoalesceRendersDuration)
        {
            DisposeCoalesceTimer();
            return base.ShouldRender();
        }

        // A render we scheduled ourselves (the trailing edge), let it through.
        if (passCoalescedRender)
        {
            passCoalescedRender = false;
            return true;
        }

        // Inside a burst: suppress this render, but (re)arm a single trailing render so the final
        // startup state is guaranteed to paint. Created once (stopped), then each new change just
        // resets the due time via Change, no re-allocation per suppressed render (debounce).
        coalesceTimer ??= TimeProvider.CreateTimer(static state =>
        {
            var self = (AppComponentBase)state!;
            // The timer fires on a threadpool thread, hop back onto the renderer's sync context.
            _ = self.InvokeAsync(() =>
            {
                if (self.coalesceDisposed) return; // do not render after disposal
                self.passCoalescedRender = true;
                self.StateHasChanged();
            });
        }, this, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

        coalesceTimer.Change(CoalesceRendersWindow, Timeout.InfiniteTimeSpan);

        return false;
    }

    private void DisposeCoalesceTimer()
    {
        coalesceTimer?.Dispose();
        coalesceTimer = null;
    }

    /// <summary>
    /// Implementation of the partial declared in <c>AppComponentBase.cs</c>, invoked from
    /// <see cref="DisposeAsync()"/> so the coalescing timer is always released while its whole
    /// logic stays isolated in this file. Compiles away entirely when this partial is absent.
    /// </summary>
    partial void DisposeRenderCoalescing()
    {
        coalesceDisposed = true;
        DisposeCoalesceTimer();
    }
}
