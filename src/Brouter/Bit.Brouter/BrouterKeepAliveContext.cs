namespace Bit.Brouter;

/// <summary>
/// Cascaded to a <see cref="Broute.KeepAlive"/> route's content so the rendered component can tell
/// whether it is the currently visible route or is being kept mounted (hidden) after the user
/// navigated away. Consume it with <c>[CascadingParameter]</c> and branch in <c>OnParametersSet</c>
/// to pause background work (timers, polling, live subscriptions) while inactive and resume or
/// refresh on reactivation - the keep-alive equivalent of Vue's <c>onActivated</c>/<c>onDeactivated</c>
/// and Angular's route-reuse hooks.
/// </summary>
/// <remarks>
/// A fresh instance is cascaded each time the active state flips (the cascade is not fixed), so a
/// component that reads <see cref="IsActive"/> in <c>OnParametersSet</c> is re-invoked on every
/// activate/deactivate transition. While a route is kept but hidden its component stays fully alive
/// and keeps running unless it chooses to pause when <see cref="IsActive"/> is <c>false</c>.
/// Permanent teardown - the route being removed, or <see cref="IBrouter.ClearKeepAlive"/> dropping
/// the retained instance - surfaces through normal component disposal (<c>IDisposable.Dispose</c>).
/// </remarks>
public sealed class BrouterKeepAliveContext
{
    /// <summary>
    /// <c>true</c> when this is the currently matched, visible route; <c>false</c> while the
    /// component is kept mounted but hidden after navigating away.
    /// </summary>
    public bool IsActive { get; }

    internal BrouterKeepAliveContext(bool isActive) => IsActive = isActive;
}
