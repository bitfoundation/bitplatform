namespace Bit.Bonboarding;

/// <summary>Lifecycle state of a tour session.</summary>
public enum BonboardingTourStatus
{
    /// <summary>Created but not started.</summary>
    Idle,
    /// <summary>A step is displayed.</summary>
    Running,
    /// <summary>Waiting for a target element, a route change, or a guard.</summary>
    Waiting,
    /// <summary>Explicitly paused by the application.</summary>
    Paused,
    /// <summary>Reached the end and was finished by the user or programmatically.</summary>
    Completed,
    /// <summary>The user skipped the remainder of the tour.</summary>
    Skipped,
    /// <summary>The user closed the tour.</summary>
    Dismissed,
    /// <summary>
    /// Stopped by the application rather than by the user, for instance because another tour
    /// started. Progress is kept, so the tour resumes where it left off next time.
    /// </summary>
    Cancelled,
    /// <summary>Stopped by an unrecoverable error.</summary>
    Failed,
}
