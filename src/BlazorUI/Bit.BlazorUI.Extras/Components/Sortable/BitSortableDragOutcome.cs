namespace Bit.BlazorUI;

/// <summary>
/// What a finished drag still has to tell the app. It is worked out while the drag handlers are serialized
/// (<see cref="BitSortableCoordinator.Gate"/>) and reported once they are not, so that the app's callbacks - a slow
/// save in <c>ItemsChanged</c>, say - never hold up the next drag.
/// </summary>
internal sealed class BitSortableDragOutcome
{
    /// <summary>Where the item ended up.</summary>
    public required BitSortableMoveInfo Info { get; init; }

    /// <summary>The lists the drag left different, whose <c>ItemsChanged</c> is raised.</summary>
    public required IReadOnlyList<IBitSortableZone> ChangedZones { get; init; }

    /// <summary>Whether the drag changed the destination list (always so when the item crossed lists).</summary>
    public bool ListChanged { get; init; }

    /// <summary>Whether the drag changed the source list.</summary>
    public bool SourceChanged { get; init; }

    /// <summary>The drag whose outcome this is, kept to put its lists back should the destination refuse it.</summary>
    public required BitSortableDragContext Context { get; init; }
}
