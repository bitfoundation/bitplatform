namespace Bit.Bonboarding;

/// <summary>What a session in <see cref="BonboardingTourStatus.Waiting"/> is waiting for.</summary>
public enum BonboardingWaitReason
{
    /// <summary>The session is not waiting.</summary>
    None,

    /// <summary>
    /// The step's element has not appeared yet (or disappeared). The host shows a waiting card, since the user
    /// is on the right page and something is about to happen.
    /// </summary>
    Target,

    /// <summary>
    /// The user is not on the step's <see cref="BonboardingStepDefinition.Route"/>: the engine is navigating
    /// there, the user navigated away, or the route is a constraint the user has to satisfy. The host renders
    /// nothing, so the page stays fully usable until the route matches.
    /// </summary>
    Route,
}
