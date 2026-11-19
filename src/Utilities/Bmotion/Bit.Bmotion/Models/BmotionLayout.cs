namespace Bit.Bmotion;

/// <summary>
/// Layout-animation activation for a Bmotion element, motion.dev-style:
/// <code>
/// Layout="true"                 // animate position + size, with child counter-scale + radius correction
/// Layout="BmotionLayout.Position"    // animate position only (cheapest, no scaling at all)
/// Layout="BmotionLayout.Size"        // animate size only (the element snaps to its new position)
/// </code>
/// </summary>
public readonly struct BmotionLayout : IEquatable<BmotionLayout>
{
    private BmotionLayout(bool enabled, BmotionLayoutMode mode)
    {
        Enabled = enabled;
        Mode = mode;
    }

    /// <summary>Whether automatic layout (FLIP) animation is enabled.</summary>
    public bool Enabled { get; }

    /// <summary>What the layout animation animates.</summary>
    public BmotionLayoutMode Mode { get; }

    /// <summary>Layout animation disabled (the default).</summary>
    public static BmotionLayout None => default;

    /// <summary>Animate position and size.</summary>
    public static BmotionLayout Full => new(true, BmotionLayoutMode.Full);

    /// <summary>Animate position only.</summary>
    public static BmotionLayout Position => new(true, BmotionLayoutMode.Position);

    /// <summary>Animate size only.</summary>
    public static BmotionLayout Size => new(true, BmotionLayoutMode.Size);

    public static implicit operator BmotionLayout(bool enabled) => enabled ? Full : None;

    public bool Equals(BmotionLayout other) => Enabled == other.Enabled && Mode == other.Mode;
    public override bool Equals(object? obj) => obj is BmotionLayout l && Equals(l);
    public override int GetHashCode() => HashCode.Combine(Enabled, Mode);
    public static bool operator ==(BmotionLayout left, BmotionLayout right) => left.Equals(right);
    public static bool operator !=(BmotionLayout left, BmotionLayout right) => !left.Equals(right);
}
