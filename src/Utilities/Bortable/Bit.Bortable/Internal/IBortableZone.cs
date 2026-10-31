namespace Bit.Bortable;

/// <summary>
/// The non-generic surface of a <see cref="Bortable{TItem}"/> list, used by the drag coordinator, sibling zones and
/// the browser bridge to manipulate items and raise events without knowing the concrete item type.
/// </summary>
internal interface IBortableZone
{
    /// <summary>Stable DOM id of the list.</summary>
    string Id { get; }

    /// <summary>The group name shared by lists that can exchange items.</summary>
    string? GroupName { get; }

    /// <summary>Whether sorting within this list is permitted.</summary>
    bool AllowSort { get; }

    /// <summary>Whether a drag that starts in this list is put back when it is released outside every list.</summary>
    bool RevertOnSpill { get; }

    /// <summary>Whether this list is in swap mode: a drop position names the item to trade places with.</summary>
    bool IsSwap { get; }

    /// <summary>The list's accessible name (<c>AriaLabel</c>), which announcements name it by; <c>null</c> when it has none.</summary>
    string? Label { get; }

    /// <summary>The texts this list announces with.</summary>
    BortableMessages Messages { get; }

    /// <summary>How announcements name <paramref name="item"/> (<c>ItemLabel</c>).</summary>
    string LabelOf(object item);

    /// <summary>The key <paramref name="item"/> is rendered with in this list.</summary>
    string? KeyOfItem(object item);

    /// <summary>Makes the item rendered with <paramref name="key"/> the list's tab stop.</summary>
    void Focus(string? key);

    /// <summary>Whether this list's <c>CanDrop</c> lets the dragged item take index <paramref name="newIndex"/> here.</summary>
    bool AllowsDropAt(BortableDragContext ctx, int newIndex);

    bool AllowsMove(BortableMoveInfo info);

    /// <summary>Whether this list can receive items dragged from <paramref name="source"/>.</summary>
    bool CanReceiveFrom(IBortableZone source);

    /// <summary>Whether <paramref name="item"/> is of this list's item type.</summary>
    bool Accepts(object item);

    /// <summary>
    /// Whether this list takes the drag <paramref name="ctx"/>: the list the item is in always does, any other one
    /// when it can receive the item (or, in swap mode, trade an item of its own for it).
    /// </summary>
    bool TakesDrag(BortableDragContext ctx);

    int Count { get; }
    int IndexOf(object item);
    object? ItemAt(int index);
    void Insert(int index, object item);
    void RemoveAt(int index);

    /// <summary>Replaces the item at <paramref name="index"/> (used by swap mode).</summary>
    void Replace(int index, object item);

    /// <summary>Replaces every item of the list (used to put a refused drag back).</summary>
    void SetItems(object?[] items);

    /// <summary>Clears the multi-drag selection of this list.</summary>
    void ClearSelection();

    /// <summary>Hands the selection to <c>SelectedItemsChanged</c> when a drag changed it.</summary>
    Task PublishSelectionIfChangedAsync();

    /// <summary>Creates a copy of <paramref name="item"/> for clone-mode drags.</summary>
    object CloneItem(object item);

    /// <summary>
    /// Positions the drag at drop position <paramref name="index"/> of this list, inside the gate. Returns 1 when a
    /// list changed, 0 when nothing did, -1 when this list refused the position; <paramref name="change"/> is the
    /// <c>OnChange</c> to raise once the gate is released.
    /// </summary>
    int ApplyOver(BortableDragContext ctx, int index, out BortableMoveInfo? change);

    /// <summary>Moves (or clones) the dragged item into this list at the placeholder position.</summary>
    void CommitPlaceholder(BortableDragContext ctx);

    /// <summary>Snapshots item positions before a change .NET makes on its own (for the FLIP animation).</summary>
    Task CaptureFlipAsync();

    /// <summary>Requests that this zone play a FLIP animation on its next render.</summary>
    void RequestFlipPlay();

    /// <summary>Re-renders the list (visual state only; the bound data is unchanged).</summary>
    void Rerender();

    /// <summary>Notifies <c>ItemsChanged</c> (two-way binding) that the bound list was modified.</summary>
    Task NotifyItemsChangedAsync();

    /// <summary>Cleans up after a drag that involved this zone.</summary>
    void EndDrag();

    /// <summary>Raises the given lifecycle event on this zone.</summary>
    Task RaiseEventAsync(BortableEventType type, BortableMoveInfo info);

    /// <summary>Asks this list's <c>ConfirmDrop</c>, if it has one, whether to keep the move <paramref name="info"/>.</summary>
    Task<bool> ConfirmAsync(BortableMoveInfo info);

    // ----- called by the browser layer, through BortableBridge -----

    /// <summary>Starts a drag of the item rendered with <paramref name="key"/>; <c>null</c> when it cannot be dragged.</summary>
    Task<BortableStartResult?> StartAsync(string key, string input);

    /// <summary>
    /// The pointer reached drop position <paramref name="index"/> of this list: 1 changed, 0 unchanged, -1 refused.
    /// <paramref name="drag"/> names the drag (0: whichever is in progress); a call about another one is refused.
    /// </summary>
    Task<int> OverAsync(int index, int drag = 0);

    /// <summary>The drag <paramref name="drag"/> ended (<c>drop</c>, <c>release</c>, <c>cancel</c> or <c>abandon</c>); answers what to announce.</summary>
    Task<string?> EndAsync(string how, int drag = 0);

    /// <summary>A keyboard action on the item rendered with <paramref name="key"/> (see <see cref="BortableBridge.Key"/>).</summary>
    Task<BortableKeyResult> KeyAsync(string key, string action, string? list, int index, int drag = 0);
}
