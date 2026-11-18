namespace Bit.Bonboarding;

/// <summary>Where the user stands with a tour, as a <see cref="BonboardingTourList"/> shows it.</summary>
public enum BonboardingTourProgress
{
    /// <summary>Never started, or turned down: the list offers to start it.</summary>
    New,

    /// <summary>Started and left part way (or snoozed): the list offers to resume it.</summary>
    InProgress,

    /// <summary>Run to the end at some point: the list offers to replay it.</summary>
    Completed,
}
