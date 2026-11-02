namespace Bit.Bonboarding;

/// <summary>Everything the browser layer needs to keep one beacon on its target.</summary>
public sealed record BonboardingBeaconActivation
{
    /// <summary>Identifies the beacon for <see cref="IBonboardingInterop.DetachBeaconAsync"/>.</summary>
    public required string BeaconId { get; init; }

    /// <summary>The element the beacon sits on.</summary>
    public required BonboardingTargetDescriptor Target { get; init; }

    /// <summary>One of <c>top-end</c>, <c>top-start</c>, <c>top</c>, <c>end</c>, <c>center</c>, <c>start</c>, <c>bottom-end</c>, <c>bottom-start</c>, <c>bottom</c>.</summary>
    public required string Position { get; init; }

    /// <summary>Pixels the beacon is moved along the inline axis (towards the end in left-to-right).</summary>
    public double OffsetX { get; init; }

    /// <summary>Pixels the beacon is moved down.</summary>
    public double OffsetY { get; init; }
}
