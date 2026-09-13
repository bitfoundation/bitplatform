namespace Bit.Brouter;

/// <summary>
/// Payload of <see cref="IBrouterRoute.OnDeactivatedAsync"/>: the navigation that made this
/// component's routed content stop being the committed, visible route content.
/// </summary>
public sealed class BrouterRouteDeactivation
{
    /// <summary>
    /// Whether the content is being retained hidden (<see cref="BrouterRouteDeactivationReason.Hidden"/>,
    /// keep-alive) or torn down (<see cref="BrouterRouteDeactivationReason.Disposing"/>).
    /// </summary>
    public BrouterRouteDeactivationReason Reason { get; }

    /// <summary>The destination location of the navigation that deactivated this content.</summary>
    public BrouterLocation Location { get; }

    internal BrouterRouteDeactivation(BrouterRouteDeactivationReason reason, BrouterLocation location)
    {
        Reason = reason;
        Location = location;
    }
}
