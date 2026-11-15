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

    /// <summary>
    /// What to do when another tour is already running (only when <see cref="StopOthers"/> is true). Default
    /// <see cref="BonboardingBusyBehavior.Replace"/>, which suits a start the user asked for; use
    /// <see cref="BonboardingBusyBehavior.Queue"/> for a start nobody asked for. A declarative tour's
    /// <c>AutoStart</c> queues. Another session of the same tour is always replaced.
    /// </summary>
    public BonboardingBusyBehavior WhenBusy { get; set; } = BonboardingBusyBehavior.Replace;

    /// <summary>
    /// Says that nobody asked for this start: the application decided to show the tour (on first login, on reaching a
    /// page). An automatic start passes over a tour the user snoozed, and asks <see cref="BonboardingOptions.CanAutoStart"/>
    /// first, so a user who turned tips off is not shown any; a start the user asks for (a help menu, a replay) is not
    /// held back by either. <see cref="IBonboardingService.StartOnceAsync"/> and a declarative <c>AutoStart</c> are
    /// always automatic. A start that is held back throws a <see cref="BonboardingException"/>, as one
    /// <see cref="BonboardingTourDefinition.CanStart"/> declines does. Default <see langword="false"/>.
    /// </summary>
    public bool Automatic { get; set; }

    /// <summary>
    /// Runs the tour without touching the user's progress, for authors and testers checking a tour: stored progress is
    /// neither read nor written (it starts at the top, or at <see cref="StartAtStepId"/>), and it starts whatever
    /// <see cref="BonboardingTourDefinition.CanStart"/> says and however the user left the tour. Its events carry
    /// <see cref="BonboardingEvent.IsPreview"/>, so analytics can leave them out. Default <see langword="false"/>.
    /// </summary>
    public bool Preview { get; set; }
}
