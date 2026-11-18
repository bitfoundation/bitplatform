namespace Bit.Bonboarding;

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
