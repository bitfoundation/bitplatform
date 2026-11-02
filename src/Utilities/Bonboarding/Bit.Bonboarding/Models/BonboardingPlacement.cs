namespace Bit.Bonboarding;

/// <summary>
/// Requested position of the popover relative to its target. The sides Top, Bottom, Left and Right are physical;
/// the Start and End alignments of Top and Bottom are logical (they mirror in right-to-left). The InlineStart and
/// InlineEnd sides are logical too: the side text starts from, and the side it ends on.
/// </summary>
public enum BonboardingPlacement
{
    /// <summary>Pick the side with the most available space, then align to the target centre.</summary>
    Auto,
    Top, TopStart, TopEnd,
    Bottom, BottomStart, BottomEnd,
    Left, LeftStart, LeftEnd,
    Right, RightStart, RightEnd,
    /// <summary>Ignore the target and centre the popover in the viewport.</summary>
    Center,

    /// <summary>Beside the target where text starts: left in left-to-right, right in right-to-left.</summary>
    InlineStart,

    /// <summary><see cref="InlineStart"/>, top edges aligned.</summary>
    InlineStartTop,

    /// <summary><see cref="InlineStart"/>, bottom edges aligned.</summary>
    InlineStartBottom,

    /// <summary>Beside the target where text ends: right in left-to-right, left in right-to-left.</summary>
    InlineEnd,

    /// <summary><see cref="InlineEnd"/>, top edges aligned.</summary>
    InlineEndTop,

    /// <summary><see cref="InlineEnd"/>, bottom edges aligned.</summary>
    InlineEndBottom,
}
