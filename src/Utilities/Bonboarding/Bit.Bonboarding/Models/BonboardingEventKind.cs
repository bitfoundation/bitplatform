namespace Bit.Bonboarding;

/// <summary>Every observable moment in a tour's life, for analytics pipelines.</summary>
public enum BonboardingEventKind
{
    TourStarted,
    TourResumed,
    StepShown,
    StepLeft,
    StepSkipped,
    TargetMissing,
    TargetResolved,
    ActionInvoked,
    TourPaused,
    TourCompleted,
    TourSkipped,
    TourDismissed,
    TourCancelled,
    TourFailed,

    /// <summary>
    /// <see cref="BonboardingStepDefinition.CanAdvance"/> held the user on the step. Frequent occurrences on one step
    /// point at an instruction that is hard to follow.
    /// </summary>
    AdvanceBlocked,

    /// <summary>The user asked to be reminded later; <see cref="BonboardingEvent.Reason"/> is <see cref="BonboardingTourEndReason.Snoozed"/>.</summary>
    TourSnoozed,

    /// <summary>
    /// A start found another tour running and was queued behind it (<see cref="BonboardingBusyBehavior.Queue"/>). The
    /// session's <c>TourStarted</c> follows when it really starts, or <c>TourCancelled</c> if it never does.
    /// </summary>
    TourQueued,
}
