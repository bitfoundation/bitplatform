namespace Bit.Bmotion;

/// <summary>What a layout (FLIP) animation animates.</summary>
public enum BmLayoutMode
{
    /// <summary>
    /// Animate both position and size (translate + scale). Direct children are counter-scaled and the
    /// border-radius is corrected each frame, so text/children don't stretch and corners stay round.
    /// </summary>
    Full,
    /// <summary>Animate position only - the cheapest, always-distortion-free option.</summary>
    Position,
}

/// <summary>
/// Layout-animation activation for a Bmotion element, motion.dev-style:
/// <code>
/// Layout="true"                 // animate position + size, with child counter-scale + radius correction
/// Layout="BmLayout.Position"    // animate position only (cheapest, no scaling at all)
/// </code>
/// </summary>
public readonly struct BmLayout : IEquatable<BmLayout>
{
    private BmLayout(bool enabled, BmLayoutMode mode)
    {
        Enabled = enabled;
        Mode = mode;
    }

    /// <summary>Whether automatic layout (FLIP) animation is enabled.</summary>
    public bool Enabled { get; }

    /// <summary>What the layout animation animates.</summary>
    public BmLayoutMode Mode { get; }

    /// <summary>Layout animation disabled (the default).</summary>
    public static BmLayout None => default;

    /// <summary>Animate position and size.</summary>
    public static BmLayout Full => new(true, BmLayoutMode.Full);

    /// <summary>Animate position only.</summary>
    public static BmLayout Position => new(true, BmLayoutMode.Position);

    public static implicit operator BmLayout(bool enabled) => enabled ? Full : None;

    public bool Equals(BmLayout other) => Enabled == other.Enabled && Mode == other.Mode;
    public override bool Equals(object? obj) => obj is BmLayout l && Equals(l);
    public override int GetHashCode() => HashCode.Combine(Enabled, Mode);
    public static bool operator ==(BmLayout left, BmLayout right) => left.Equals(right);
    public static bool operator !=(BmLayout left, BmLayout right) => !left.Equals(right);
}
