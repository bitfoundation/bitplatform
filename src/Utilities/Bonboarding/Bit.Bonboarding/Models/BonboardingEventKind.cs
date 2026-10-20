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
}
