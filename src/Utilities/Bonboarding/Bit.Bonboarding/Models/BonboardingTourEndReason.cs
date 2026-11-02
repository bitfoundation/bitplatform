namespace Bit.Bonboarding;

/// <summary>Why a tour stopped.</summary>
public enum BonboardingTourEndReason
{
    // New members are only ever appended, so a value persisted or sent to analytics keeps its meaning.
    Completed,
    Skipped,
    Dismissed,
    Cancelled,
    Failed,

    /// <summary>The user asked to be reminded later; see <see cref="IBonboardingTourSession.SnoozeAsync"/>.</summary>
    Snoozed,
}
