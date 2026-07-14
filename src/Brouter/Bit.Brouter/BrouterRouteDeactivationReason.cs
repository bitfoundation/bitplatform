namespace Bit.Brouter;

/// <summary>
/// Why a route's content was deactivated (see <see cref="IBrouterRoute.OnDeactivatedAsync"/>).
/// </summary>
public enum BrouterRouteDeactivationReason
{
    /// <summary>
    /// The content is kept mounted but hidden (<see cref="Broute.KeepAlive"/>): the component
    /// instance and its state survive, and a later visit reactivates the same instance. Pause
    /// background work here and resume it in <see cref="IBrouterRoute.OnActivatedAsync"/>.
    /// </summary>
    Hidden,

    /// <summary>
    /// The content is about to be unmounted and its components disposed (a non-keep-alive route the
    /// user navigated away from, or content replaced by a pending-navigation render). The
    /// synchronous part of the deactivation callback runs before disposal; <c>Dispose</c> remains
    /// the definitive teardown signal.
    /// </summary>
    Disposing,
}
