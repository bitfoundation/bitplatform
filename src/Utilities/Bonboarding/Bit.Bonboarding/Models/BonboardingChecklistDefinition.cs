namespace Bit.Bonboarding;

/// <summary>
/// An onboarding checklist: the handful of things a new user should do, each ticked off as it gets done - by finishing
/// a tour, by a condition becoming true, or by the application saying so. Render it with <see cref="BonboardingChecklist"/>.
/// </summary>
public sealed class BonboardingChecklistDefinition
{
    /// <summary>The prefix of the key a checklist's progress is stored under, next to the tours' records.</summary>
    public const string StorageKeyPrefix = "checklist:";

    /// <summary>Stable identifier; with <see cref="StorageKeyPrefix"/>, the persistence key.</summary>
    public required string Id { get; set; }

    /// <summary>Heading of the checklist.</summary>
    public string? Title { get; set; }

    /// <summary>A line under the heading.</summary>
    public string? Description { get; set; }

    /// <summary>The tasks, in the order they are listed.</summary>
    public List<BonboardingChecklistItem> Items { get; init; } = [];

    /// <summary>Finds an item by id.</summary>
    public BonboardingChecklistItem? FindItem(string itemId) => Items.FirstOrDefault(item => item.Id == itemId);

    /// <summary>
    /// Checks the definition for mistakes: blank or duplicate item ids, a <see cref="BonboardingChecklistItem.DependsOn"/>
    /// naming an item that does not exist, and items that wait for each other in a circle (so none of them can ever be
    /// started). <see cref="BonboardingChecklist"/> logs what it finds; call it from a unit test to catch it first.
    /// <see cref="BonboardingValidationIssue.StepId"/> is the item's id.
    /// </summary>
    public IReadOnlyList<BonboardingValidationIssue> Validate() => BonboardingChecklistValidator.Validate(this);

    public override string ToString() => $"Checklist({Id}, {Items.Count} items)";
}

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

/// <summary>What a checklist item's callbacks are given.</summary>
public sealed class BonboardingChecklistItemContext(
    BonboardingChecklistDefinition checklist,
    BonboardingChecklistItem item,
    IServiceProvider services,
    CancellationToken cancellationToken = default)
{
    public BonboardingChecklistDefinition Checklist { get; } = checklist;

    public BonboardingChecklistItem Item { get; } = item;

    /// <summary>The application's service provider.</summary>
    public IServiceProvider Services { get; } = services;

    public CancellationToken CancellationToken { get; } = cancellationToken;
}

/// <summary>An item of a checklist as it currently stands, for <see cref="BonboardingChecklist.ItemTemplate"/>.</summary>
public sealed class BonboardingChecklistItemState
{
    internal BonboardingChecklistItemState(BonboardingChecklistItem item, bool isDone, bool isActionable, IReadOnlyList<BonboardingChecklistItem>? blockedBy = null)
    {
        Item = item;
        IsDone = isDone;
        BlockedBy = blockedBy ?? [];
        IsActionable = isActionable && !IsLocked;
    }

    public BonboardingChecklistItem Item { get; }

    /// <summary>True once the item is done, however that came about.</summary>
    public bool IsDone { get; }

    /// <summary>True when pressing the item does something: a tour, a route or a handler, and it is not <see cref="IsLocked"/>.</summary>
    public bool IsActionable { get; }

    /// <summary>True while an item it <see cref="BonboardingChecklistItem.DependsOn"/> is not done yet, and it is not done itself.</summary>
    public bool IsLocked => !IsDone && BlockedBy.Count > 0;

    /// <summary>The listed items it <see cref="BonboardingChecklistItem.DependsOn"/> that are not done yet, in the order they are listed.</summary>
    public IReadOnlyList<BonboardingChecklistItem> BlockedBy { get; }

    /// <summary>True for an <see cref="BonboardingChecklistItem.Optional"/> item, which the progress leaves out.</summary>
    public bool IsOptional => Item.Optional;

    /// <summary>Runs the item's action. Set by the checklist that rendered the state.</summary>
    public Func<Task> ActivateAsync { get; internal init; } = () => Task.CompletedTask;
}
