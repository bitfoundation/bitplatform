namespace Bit.Bonboarding;

/// <summary>How much of the page the user may interact with while a step is shown.</summary>
public enum BonboardingInteractionMode
{
    /// <summary>The overlay swallows every pointer event outside the popover.</summary>
    Blocked,
    /// <summary>The spotlight cut-out is click-through, so the user can operate the highlighted element.</summary>
    TargetOnly,
    /// <summary>The overlay never intercepts pointer events; the whole page stays usable.</summary>
    Free,
}
