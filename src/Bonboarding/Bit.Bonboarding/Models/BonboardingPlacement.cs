namespace Bit.Bonboarding;

/// <summary>Requested position of the popover relative to its target.</summary>
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
}
