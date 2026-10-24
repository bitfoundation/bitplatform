namespace Bit.Blex.Blazor;

/// <summary>
/// Spaces out the renders store changes ask a component for: the first after a quiet interval renders at once, and
/// the ones that follow within the interval become a single trailing render, which shows the latest state.
/// </summary>
/// <remarks>
/// A store that changes many times a second -- a live feed, a progress counter, a drag -- would otherwise re-render
/// its components that often, which on Blazor Server is a diff sent over the wire each time. Throttled, a component
/// renders at most once per interval and never misses the last change. Changes arrive on any thread (a timer, a
/// SignalR handler); the gate keeps the bookkeeping consistent.
/// </remarks>
internal sealed class BlexRenderThrottle(Action render) : IDisposable
{
    private readonly Lock _gate = new();
    private TimeProvider? _clock;
    private long _lastRender;
    private bool _rendered;
    private ITimer? _trailing;
    private bool _disposed;

    /// <summary>Renders now, or once the interval since the last render has passed; at once when the interval is zero.</summary>
    public void Request(TimeSpan interval, TimeProvider clock)
    {
        if (interval <= TimeSpan.Zero)
        {
            render();
            return;
        }

        lock (_gate)
        {
            // A trailing render is already due, and it will show this change too.
            if (_disposed || _trailing is not null)
                return;

            var elapsed = _rendered ? clock.GetElapsedTime(_lastRender) : TimeSpan.MaxValue;
            if (elapsed < interval)
            {
                // Created under the gate: the callback takes it too, so it cannot run before the field is set.
                _clock = clock;
                _trailing = clock.CreateTimer(static state => ((BlexRenderThrottle)state!).OnTrailing(), this, interval - elapsed, Timeout.InfiniteTimeSpan);
                return;
            }

            _rendered = true;
            _lastRender = clock.GetTimestamp();
        }

        render();
    }

    private void OnTrailing()
    {
        lock (_gate)
        {
            _trailing?.Dispose();
            _trailing = null;
            if (_disposed)
                return;

            _rendered = true;
            _lastRender = _clock!.GetTimestamp();
        }

        render();
    }

    /// <summary>Drops a trailing render still due; the component is going away.</summary>
    public void Dispose()
    {
        lock (_gate)
        {
            _disposed = true;
            _trailing?.Dispose();
            _trailing = null;
        }
    }
}
