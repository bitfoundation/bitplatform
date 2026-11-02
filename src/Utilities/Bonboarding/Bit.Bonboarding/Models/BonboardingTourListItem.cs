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

/// <summary>One tour in a <see cref="BonboardingTourList"/>, for its <see cref="BonboardingTourList.ItemTemplate"/>.</summary>
public sealed class BonboardingTourListItem
{
    internal BonboardingTourListItem(BonboardingTourDefinition tour, BonboardingTourProgress progress, bool isRunning)
    {
        Tour = tour;
        Progress = progress;
        IsRunning = isRunning;
    }

    public BonboardingTourDefinition Tour { get; }

    public BonboardingTourProgress Progress { get; }

    /// <summary>True while the tour is on screen (or paused).</summary>
    public bool IsRunning { get; }

    /// <summary>Starts, resumes or replays the tour, as <see cref="Progress"/> suggests.</summary>
    public Func<Task> StartAsync { get; internal init; } = () => Task.CompletedTask;
}
