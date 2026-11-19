namespace Bit.Bmotion;

/// <summary>
/// Drag activation for a Bmotion element, motion.dev-style:
/// <code>
/// Drag="true"        // drag both axes (implicit bool conversion)
/// Drag="BmotionDrag.X"    // horizontal only
/// Drag="BmotionDrag.Y"    // vertical only
/// </code>
/// </summary>
public readonly struct BmotionDrag : IEquatable<BmotionDrag>
{
    private BmotionDrag(bool enabled, BmotionDragAxis axis)
    {
        Enabled = enabled;
        Axis = axis;
    }

    /// <summary>Whether dragging is enabled.</summary>
    public bool Enabled { get; }

    /// <summary>The allowed drag axis.</summary>
    public BmotionDragAxis Axis { get; }

    /// <summary>Dragging disabled (the default).</summary>
    public static BmotionDrag None => default;

    /// <summary>Drag on both axes.</summary>
    public static BmotionDrag Both => new(true, BmotionDragAxis.Both);

    /// <summary>Drag horizontally only.</summary>
    public static BmotionDrag X => new(true, BmotionDragAxis.X);

    /// <summary>Drag vertically only.</summary>
    public static BmotionDrag Y => new(true, BmotionDragAxis.Y);

    public static implicit operator BmotionDrag(bool enabled) => enabled ? Both : None;

    public bool Equals(BmotionDrag other) => Enabled == other.Enabled && Axis == other.Axis;
    public override bool Equals(object? obj) => obj is BmotionDrag d && Equals(d);
    public override int GetHashCode() => HashCode.Combine(Enabled, Axis);
    public static bool operator ==(BmotionDrag left, BmotionDrag right) => left.Equals(right);
    public static bool operator !=(BmotionDrag left, BmotionDrag right) => !left.Equals(right);
}
