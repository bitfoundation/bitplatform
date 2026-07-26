namespace Bit.Brouter;

/// <summary>
/// Payload of <see cref="IBrouterRoute.OnActivatedAsync"/>: the navigation that made this
/// component's routed content the committed, visible route content.
/// </summary>
public sealed class BrouterRouteActivation
{
    /// <summary>
    /// <c>true</c> on the very first activation of this content instance (fresh mount);
    /// <c>false</c> when a kept-alive instance is being revealed again from its hidden retention.
    /// Branch on this to separate one-time setup from every-visit refresh without duplicating
    /// logic across <c>OnInitialized</c> and this callback.
    /// </summary>
    public bool IsFirstActivation { get; }

    /// <summary>The location whose navigation activated this content.</summary>
    public BrouterLocation Location { get; }

    internal BrouterRouteActivation(bool isFirstActivation, BrouterLocation location)
    {
        IsFirstActivation = isFirstActivation;
        Location = location;
    }
}
