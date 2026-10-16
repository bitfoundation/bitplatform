namespace Bit.Bonboarding;

/// <summary>What changed about a session.</summary>
[Flags]
public enum BonboardingTourSessionChange
{
    None = 0,
    /// <summary>The status or the current step changed; a full re-render is needed.</summary>
    Step = 1,
    /// <summary>Only the measured geometry changed; the popover just needs repositioning.</summary>
    Geometry = 2,
    /// <summary>The session ended.</summary>
    Ended = 4,
}
