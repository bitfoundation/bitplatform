namespace Bit.Bonboarding;

/// <summary>
/// Where on its target a <see cref="BonboardingBeacon"/> sits. Start and End are logical: in a right-to-left
/// layout, <see cref="TopEnd"/> is the top-left corner.
/// </summary>
public enum BonboardingBeaconPosition
{
    TopEnd,
    TopStart,
    Top,
    End,
    Center,
    Start,
    BottomEnd,
    BottomStart,
    Bottom,
}
