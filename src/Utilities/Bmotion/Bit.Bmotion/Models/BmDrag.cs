namespace Bit.Bmotion;

/// <summary>The axis a drag gesture is allowed to move along.</summary>
public enum BmDragAxis { Both, X, Y }

/// <summary>
/// Drag activation for a Bmotion element, motion.dev-style:
/// <code>
/// Drag="true"        // drag both axes (implicit bool conversion)
/// Drag="BmDrag.X"    // horizontal only
/// Drag="BmDrag.Y"    // vertical only
/// </code>
/// </summary>
public readonly struct BmDrag : IEquatable<BmDrag>
{
    private BmDrag(bool enabled, BmDragAxis axis)
    {
        Enabled = enabled;
        Axis = axis;
    }

    /// <summary>Whether dragging is enabled.</summary>
    public bool Enabled { get; }

    /// <summary>The allowed drag axis.</summary>
    public BmDragAxis Axis { get; }

    /// <summary>Dragging disabled (the default).</summary>
    public static BmDrag None => default;

    /// <summary>Drag on both axes.</summary>
    public static BmDrag Both => new(true, BmDragAxis.Both);

    /// <summary>Drag horizontally only.</summary>
    public static BmDrag X => new(true, BmDragAxis.X);

    /// <summary>Drag vertically only.</summary>
    public static BmDrag Y => new(true, BmDragAxis.Y);

    public static implicit operator BmDrag(bool enabled) => enabled ? Both : None;

    public bool Equals(BmDrag other) => Enabled == other.Enabled && Axis == other.Axis;
    public override bool Equals(object? obj) => obj is BmDrag d && Equals(d);
    public override int GetHashCode() => HashCode.Combine(Enabled, Axis);
    public static bool operator ==(BmDrag left, BmDrag right) => left.Equals(right);
    public static bool operator !=(BmDrag left, BmDrag right) => !left.Equals(right);
}
