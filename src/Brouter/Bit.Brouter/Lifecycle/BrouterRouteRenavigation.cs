namespace Bit.Brouter;

/// <summary>
/// Payload of <see cref="IBrouterRoute.OnRenavigatedAsync"/>: a navigation re-committed this route
/// while the same component instance stayed visible (route/query parameter changes on a singleton
/// route, or a repeated navigation to the same route).
/// </summary>
public sealed class BrouterRouteRenavigation
{
    /// <summary>The location the navigation committed (where the user is now).</summary>
    public BrouterLocation Location { get; }

    /// <summary>The location the navigation came from.</summary>
    public BrouterLocation PreviousLocation { get; }

    internal BrouterRouteRenavigation(BrouterLocation location, BrouterLocation previousLocation)
    {
        Location = location;
        PreviousLocation = previousLocation;
    }
}
