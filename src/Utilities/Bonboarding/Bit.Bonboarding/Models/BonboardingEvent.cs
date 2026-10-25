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

    /// <summary>How long the previous step was on screen, for <see cref="BonboardingEventKind.StepLeft"/>.</summary>
    public TimeSpan? Duration { get; init; }

    /// <summary>Populated for <see cref="BonboardingEventKind.ActionInvoked"/>.</summary>
    public BonboardingStepAction? Action { get; init; }

    /// <summary>Populated for terminal events.</summary>
    public BonboardingTourEndReason? Reason { get; init; }

    /// <summary>Populated for <see cref="BonboardingEventKind.TourFailed"/>.</summary>
    public Exception? Error { get; init; }

    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    public override string ToString() => $"{Kind}({Session.Tour.Id}{(Step is null ? "" : $"/{Step.Id}")})";
}
