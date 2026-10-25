namespace Bit.Bonboarding;

/// <summary>What a start does when another tour is already running. See <see cref="BonboardingStartOptions.WhenBusy"/>.</summary>
public enum BonboardingBusyBehavior
{
    /// <summary>
    /// Cancel the running tour (it keeps its progress and resumes next time) and start this one. Right for a start
    /// the user asked for, such as a "Take the tour" button.
    /// </summary>
    Replace,

    /// <summary>
    /// Leave the running tour alone and start this one when it ends, after every queued tour of a higher
    /// <see cref="BonboardingTourDefinition.Priority"/>. Its eligibility is checked again at that moment. Right for a
    /// start nobody asked for, such as a page's automatic tour, which must not interrupt one the user is taking.
    /// </summary>
    Queue,

    /// <summary>Do not start at all: <c>StartOnceAsync</c> returns <see langword="null"/>, <c>StartAsync</c> throws.</summary>
    Skip,
}
