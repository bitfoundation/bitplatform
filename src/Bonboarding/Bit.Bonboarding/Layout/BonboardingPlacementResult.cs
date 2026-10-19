using System.Globalization;

namespace Bit.Bonboarding;

/// <summary>The outcome of <see cref="BonboardingPlacementEngine.Place"/>.</summary>
public readonly record struct BonboardingPlacementResult
{
    /// <summary>Final popover rectangle in viewport coordinates.</summary>
    public BonboardingRect Popover { get; init; }

    /// <summary>Side of the target the popover ended up on.</summary>
    public BonboardingPlacementSide Side { get; init; }

    /// <summary>Alignment along the target's cross axis.</summary>
    public BonboardingPlacementAlign Align { get; init; }

    /// <summary>Whether an arrow should be drawn.</summary>
    public bool ShowArrow { get; init; }

    /// <summary>
    /// Distance from the popover's leading edge (left for top/bottom, top for left/right) to the
    /// arrow's centre, in pixels.
    /// </summary>
    public double ArrowOffset { get; init; }

    /// <summary>True when the requested placement was used unchanged.</summary>
    public bool UsedPreferred { get; init; }

    /// <summary>Pixels of the popover that had to be clipped to fit; <c>0</c> when it fits cleanly.</summary>
    public double Overflow { get; init; }

    /// <summary>The CSS class suffix for the chosen side, e.g. <c>"bottom"</c>.</summary>
    public string SideName => Side switch
    {
        BonboardingPlacementSide.Top => "top",
        BonboardingPlacementSide.Right => "right",
        BonboardingPlacementSide.Bottom => "bottom",
        BonboardingPlacementSide.Left => "left",
        _ => "center",
    };

    public override string ToString()
        => string.Create(CultureInfo.InvariantCulture, $"{SideName}/{Align} at {Popover}");
}
