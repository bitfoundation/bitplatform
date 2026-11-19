namespace Bit.Bonboarding;

/// <summary>One task of a <see cref="BonboardingChecklistDefinition"/>.</summary>
public sealed class BonboardingChecklistItem
{
    /// <summary>Stable identifier, under which a manual completion is stored.</summary>
    public required string Id { get; set; }

    /// <summary>What to do, e.g. "Invite a teammate".</summary>
    public required string Title { get; set; }

    /// <summary>A line under the title.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// A tour that shows how: pressing the item starts it, and the item is done once the tour has been completed.
    /// </summary>
    public BonboardingTourDefinition? Tour { get; set; }

    /// <summary>A registered tour, by id, used like <see cref="Tour"/>.</summary>
    public string? TourId { get; set; }

    /// <summary>Navigates here when the item is pressed and it has no tour, e.g. <c>"settings/team"</c>.</summary>
    public string? Route { get; set; }

    /// <summary>Runs when the item is pressed, instead of starting its tour or following its route.</summary>
    public Func<BonboardingChecklistItemContext, ValueTask>? OnClick { get; set; }

    /// <summary>
    /// The item is done while this returns true: <c>ctx =&gt; ValueTask.FromResult(user.HasAvatar)</c>. Asked when the
    /// checklist loads and whenever it refreshes, so keep it cheap.
    /// </summary>
    public Func<BonboardingChecklistItemContext, ValueTask<bool>>? IsComplete { get; set; }

    /// <summary>
    /// Ticks the item off, for good, when the application raises this signal through
    /// <see cref="IBonboardingService.SignalAsync"/> while the checklist is on screen.
    /// </summary>
    public string? CompleteOnSignal { get; set; }

    /// <summary>Lists the item only while this returns true, e.g. for one role.</summary>
    public Func<BonboardingChecklistItemContext, ValueTask<bool>>? When { get; set; }

    /// <summary>
    /// A nice-to-have rather than part of getting set up: listed, marked as optional, and ticked off like any other,
    /// but left out of the progress, so the checklist is complete once every required item is done. (A checklist of
    /// optional items only is complete once all of them are.) Default <see langword="false"/>.
    /// </summary>
    public bool Optional { get; set; }

    /// <summary>
    /// Ids of the items that have to be done first, e.g. "create a project" before "invite the team to it". Until they
    /// are, the item is listed as locked and pressing it does nothing; it still counts as done if it gets done another
    /// way (a signal, its condition). An item hidden by its <see cref="When"/> does not hold anything up.
    /// </summary>
    public IReadOnlyList<string>? DependsOn { get; set; }

    public override string ToString() => $"ChecklistItem({Id})";
}
