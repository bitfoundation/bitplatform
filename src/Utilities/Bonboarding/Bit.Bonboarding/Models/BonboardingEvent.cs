namespace Bit.Bonboarding;

/// <summary>A single analytics-grade record of something that happened during a tour.</summary>
public sealed class BonboardingEvent
{
    public required BonboardingEventKind Kind { get; init; }
    public required IBonboardingTourSession Session { get; init; }
    public BonboardingTourDefinition Tour => Session.Tour;
    public BonboardingStepDefinition? Step { get; init; }

    /// <summary>Zero-based index of <see cref="Step"/>, or <c>-1</c>.</summary>
    public int StepIndex { get; init; } = -1;

    /// <summary>
    /// How long the step was on screen, for <see cref="BonboardingEventKind.StepLeft"/>; how long the tour ran, from its
    /// start to its end, for the event that ends it (<see cref="BonboardingEventKind.TourCompleted"/> and the other
    /// terminal kinds). <see langword="null"/> for a tour that ended before it started (a queued start withdrawn).
    /// </summary>
    public TimeSpan? Duration { get; init; }

    /// <summary>How the user reached the step, for <see cref="BonboardingEventKind.StepShown"/>.</summary>
    public BonboardingStepChangeReason? ChangeReason { get; init; }

    /// <summary>
    /// Why the target could not be pointed at, for <see cref="BonboardingEventKind.TargetMissing"/>:
    /// <see cref="BonboardingTargetIssue.NotFound"/>, <see cref="BonboardingTargetIssue.Hidden"/> or
    /// <see cref="BonboardingTargetIssue.InvalidSelector"/>.
    /// </summary>
    public BonboardingTargetIssue TargetIssue { get; init; }

    /// <summary>
    /// True for the events of a tour started as a preview (<see cref="BonboardingStartOptions.Preview"/>), which an
    /// analytics sink normally leaves out: they are an author checking the tour, not a user taking it.
    /// </summary>
    public bool IsPreview { get; init; }

    /// <summary>Populated for <see cref="BonboardingEventKind.ActionInvoked"/>.</summary>
    public BonboardingStepAction? Action { get; init; }

    /// <summary>Populated for terminal events.</summary>
    public BonboardingTourEndReason? Reason { get; init; }

    /// <summary>Populated for <see cref="BonboardingEventKind.TourFailed"/>.</summary>
    public Exception? Error { get; init; }

    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    public override string ToString() => $"{Kind}({Session.Tour.Id}{(Step is null ? "" : $"/{Step.Id}")})";
}
