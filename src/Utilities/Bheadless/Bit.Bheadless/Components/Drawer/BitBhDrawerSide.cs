namespace Bit.Bheadless;

/// <summary>The edge of the screen a <see cref="BitBhDrawer"/> is attached to (and slides in from).</summary>
public enum BitBhDrawerSide
{
    /// <summary>The bottom edge (a bottom sheet).</summary>
    Bottom,

    /// <summary>The top edge.</summary>
    Top,

    /// <summary>The left edge.</summary>
    Left,

    /// <summary>The right edge.</summary>
    Right,

    /// <summary>The edge text starts at: left, or right in a right-to-left direction.</summary>
    Start,

    /// <summary>The edge text ends at: right, or left in a right-to-left direction.</summary>
    End,
}
