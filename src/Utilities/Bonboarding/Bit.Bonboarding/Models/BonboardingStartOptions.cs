namespace Bit.Bonboarding;

/// <summary>Fine control over how a tour starts.</summary>
public sealed class BonboardingStartOptions
{
    /// <summary>Begin at this step instead of the first one.</summary>
    public string? StartAtStepId { get; set; }

    /// <summary>Begin at this index instead of the first step. Ignored when <see cref="StartAtStepId"/> is set.</summary>
    public int? StartAtIndex { get; set; }

    /// <summary>
    /// Continue from persisted progress when there is any. Default <see langword="true"/>.
    /// Ignored when a start position is given explicitly.
    /// </summary>
    public bool Resume { get; set; } = true;

    /// <summary>
    /// Start even when the tour is recorded as completed or dismissed, and even when
    /// <see cref="BonboardingTourDefinition.CanStart"/> says no. Default <see langword="false"/>.
    /// </summary>
    public bool Force { get; set; }

    /// <summary>Seed values for <see cref="IBonboardingTourSession.State"/>.</summary>
    public IReadOnlyDictionary<string, object?>? State { get; set; }

    /// <summary>
    /// End any other running tour first. Default <see langword="true"/>; set false to manage
    /// several simultaneous sessions yourself.
    /// </summary>
    public bool StopOthers { get; set; } = true;
}
