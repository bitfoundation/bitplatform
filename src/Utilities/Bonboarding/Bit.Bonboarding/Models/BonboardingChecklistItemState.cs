namespace Bit.Bonboarding;

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
