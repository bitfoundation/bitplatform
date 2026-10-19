namespace Bit.Bortable;

/// <summary>
/// The non-generic surface of a <see cref="Bortable{TItem}"/> list, used by the drag coordinator and
/// sibling zones to manipulate items and raise events without knowing the concrete item type.
/// </summary>
internal interface IBortableZone
{
    /// <summary>Stable DOM id of the list.</summary>
    string Id { get; }

    /// <summary>The group name shared by lists that can exchange items.</summary>
    string? GroupName { get; }

    /// <summary>Whether sorting within this list is permitted.</summary>
    bool AllowSort { get; }

    /// <summary>Whether this list can receive items dragged from <paramref name="source"/>.</summary>
    bool CanReceiveFrom(IBortableZone source);

    /// <summary>Whether <paramref name="item"/> is of this list's item type.</summary>
    bool Accepts(object item);

    int Count { get; }
    int IndexOf(object item);
    object? ItemAt(int index);
    void Insert(int index, object item);
    void RemoveAt(int index);

    /// <summary>Replaces the item at <paramref name="index"/> (used by swap mode).</summary>
    void Replace(int index, object item);

    /// <summary>Clears the multi-drag selection of this list.</summary>
    void ClearSelection();

    /// <summary>Creates a copy of <paramref name="item"/> for clone-mode drags.</summary>
    object CloneItem(object item);

    /// <summary>Moves (or clones) the dragged item into this list at the placeholder position.</summary>
    Task CommitPlaceholderAsync(BortableDragContext ctx);

    /// <summary>Snapshots item positions before a reorder (for the FLIP animation).</summary>
    Task CaptureFlipAsync();

    /// <summary>Requests that this zone play a FLIP animation on its next render.</summary>
    void RequestFlipPlay();

    /// <summary>Re-renders the list (visual state only; the bound data is unchanged).</summary>
    void Rerender();

    /// <summary>
    /// Re-renders the list when the drag that just started or ended changes whether it takes the drag's
    /// <c>dragover</c> events, or lets them bubble on to the list around it.
    /// </summary>
    void SyncDragState();

    /// <summary>Notifies <c>ItemsChanged</c> (two-way binding) that the bound list was modified.</summary>
    Task NotifyItemsChangedAsync();

    /// <summary>Cleans up after a drag that involved this zone.</summary>
    Task EndDragAsync();

    /// <summary>Raises the given lifecycle event on this zone.</summary>
    Task RaiseEventAsync(BortableEventType type, BortableMoveInfo info);
}
