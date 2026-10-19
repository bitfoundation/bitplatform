namespace Bit.Bdnd;

/// <summary>
/// The central, scoped state container for a <see cref="BdndProvider"/> subtree.
/// This is the Blazor equivalent of React DnD's internal Redux store plus the
/// drag-drop manager. It owns the authoritative drag state, exposes it through the
/// <see cref="IBdndMonitor"/> interface, and notifies subscribers when things change.
/// </summary>
public sealed class BdndDragDropManager : IBdndMonitor
{
    private readonly Dictionary<string, BdndDropTargetEntry> _targets = [];
    private int _idSeed;

    // ---- authoritative drag state ------------------------------------------------
    private long _dragId;
    private bool _isDragging;
    private object? _item;
    private string? _itemType;
    private string? _sourceId;
    private object? _dropResult;
    private bool _didDrop;
    private BdndXYCoord? _initialClientOffset;
    private BdndXYCoord? _initialSourceClientOffset;
    private BdndXYCoord? _clientOffset;
    private Func<Task>? _pendingBegin;
    private List<Task>? _pendingDrops;
    private int _dropCount; // drops of the current drag, in drop order (innermost target first)
    private int _dropResultPlace; // the place in that order of the drop _dropResult came from

    /// <summary>Raised on structural changes (begin/end/enter/leave).</summary>
    public event Action? StateChanged;

    /// <summary>Raised frequently while dragging as the pointer offset changes.</summary>
    public event Action? OffsetChanged;

    /// <summary>The shared monitor instance for this provider subtree.</summary>
    public IBdndMonitor Monitor => this;

    /// <summary>Generates a handler id unique within this manager, e.g. <c>card-3</c>.</summary>
    public string GenerateHandlerId(string prefix) => $"{prefix}-{++_idSeed}";

    /// <summary>Identifies the current drag; changes with every <see cref="BeginDrag"/>.</summary>
    internal long DragId => _dragId;

    /// <summary>
    /// Whether anything follows <see cref="OffsetChanged"/> (a <see cref="BdndDragLayer{TItem}"/>, for one):
    /// only then does the dragged source report every pointer move.
    /// </summary>
    internal bool HasOffsetListeners => OffsetChanged is not null;

    // ---- registration ------------------------------------------------------------

    internal void RegisterTarget(BdndDropTargetEntry entry)
    {
        // Upsert: preserve a live IsOver across re-renders so an in-flight hover
        // is not lost when the component re-registers with new parameters.
        if (_targets.TryGetValue(entry.Id, out var existing))
        {
            existing.Accept = entry.Accept;
            existing.CanDrop = entry.CanDrop;
        }
        else
        {
            _targets[entry.Id] = entry;
        }
    }

    internal void UnregisterTarget(string id)
    {
        // a hovered target going away changes the shallow IsOver of its ancestors
        if (_targets.Remove(id, out var entry) && ((entry.IsOver && _isDragging) || entry.NativeItemType is not null))
            NotifyStateChanged();
    }

    // ---- lifecycle ---------------------------------------------------------------

    /// <summary>
    /// Starts a drag without notifying subscribers yet: the drag source publishes it with
    /// <see cref="FlushBeginAsync"/> once re-rendering can no longer affect the browser's
    /// drag image. <paramref name="onBegun"/> runs when the drag is published.
    /// </summary>
    internal void BeginDrag(string sourceId, object? item, string itemType,
        BdndXYCoord initialClientOffset, BdndXYCoord initialSourceClientOffset,
        Func<Task>? onBegun = null)
    {
        _dragId++;
        _isDragging = true;
        _sourceId = sourceId;
        _item = item;
        _itemType = itemType;
        _dropResult = null;
        _didDrop = false;
        _initialClientOffset = initialClientOffset;
        _initialSourceClientOffset = initialSourceClientOffset;
        _clientOffset = initialClientOffset;
        _pendingDrops = null;
        _dropCount = 0;
        _dropResultPlace = 0;
        foreach (var t in _targets.Values) t.IsOver = false;
        _pendingBegin = onBegun ?? (() => Task.CompletedTask);
    }

    /// <summary>
    /// Publishes the drag started by <see cref="BeginDrag"/> (notifies subscribers and runs its
    /// begin callback) if that has not happened yet. Every other drag callback calls this first,
    /// so the source's begin callback always runs before hover, drop and end callbacks.
    /// </summary>
    internal Task FlushBeginAsync()
    {
        var onBegun = _pendingBegin;
        if (onBegun is null) return Task.CompletedTask;
        _pendingBegin = null;
        NotifyStateChanged();
        return onBegun();
    }

    internal void SetClientOffset(BdndXYCoord offset)
    {
        if (_isDragging is false || _clientOffset == offset) return;
        _clientOffset = offset;
        OffsetChanged?.Invoke();
    }

    /// <summary>
    /// Patches the source element's initial offset shortly after the drag begins (when the
    /// bounding rect has to be fetched asynchronously). Keeps custom drag layers aligned.
    /// </summary>
    internal void SetInitialSourceClientOffset(BdndXYCoord offset)
    {
        if (_isDragging is false || _initialSourceClientOffset == offset) return;
        _initialSourceClientOffset = offset;
        OffsetChanged?.Invoke();
    }

    /// <summary>Marks whether the pointer is over a target that accepts the dragged item.</summary>
    internal void SetTargetOver(string id, bool isOver)
    {
        if (_isDragging is false || _targets.TryGetValue(id, out var entry) is false || entry.IsOver == isOver) return;
        entry.IsOver = isOver;
        NotifyStateChanged();
    }

    /// <summary>
    /// Records that a drop occurred during drag <paramref name="dragId"/>. Called by each drop
    /// target in the bubbling chain (innermost first). A non-null result from a later (outer)
    /// target overrides the result set by an inner target, matching React DnD semantics.
    /// Calls for a drag that already ended (e.g. from a slow async handler) are ignored.
    /// </summary>
    internal void RecordDrop(long dragId, object? result)
    {
        if (_isDragging is false || dragId != _dragId) return;
        _didDrop = true;
        SetDropResult(++_dropCount, result);
    }

    /// <summary>
    /// Records a drop of drag <paramref name="dragId"/> whose result is still being computed by an async
    /// <c>OnDrop</c> handler. The drop counts right away; the result takes the drop's place in the drop order
    /// once <paramref name="result"/> completes, so an outer target's result still overrides it, and
    /// <see cref="WhenDropsCompletedAsync"/> lets the drag source wait for it.
    /// </summary>
    internal void RecordPendingDrop(long dragId, Task<object?> result)
    {
        if (_isDragging is false || dragId != _dragId) return;
        _didDrop = true;
        var place = ++_dropCount;
        (_pendingDrops ??= []).Add(CompleteAsync());

        async Task CompleteAsync()
        {
            object? value;
            try
            {
                value = await result;
            }
            catch (Exception)
            {
                return; // surfaced by the drop event that ran the handler
            }
            if (_isDragging && dragId == _dragId) SetDropResult(place, value);
        }
    }

    /// <summary>Completes once the results of the current drag's pending drops (<see cref="RecordPendingDrop"/>) are in.</summary>
    internal async Task WhenDropsCompletedAsync()
    {
        while (_pendingDrops is { Count: > 0 } drops)
        {
            _pendingDrops = null;
            await Task.WhenAll(drops);
        }
    }

    // a later (outer) drop's non-null result wins, whatever order the results arrive in
    private void SetDropResult(int place, object? result)
    {
        if (result is null || place < _dropResultPlace) return;
        _dropResult = result;
        _dropResultPlace = place;
    }

    // ---- native drags --------------------------------------------------------------

    /// <summary>
    /// Records which native type (one of <see cref="BdndNativeTypes"/>), if any, the native drag hovering
    /// target <paramref name="id"/> matched. Native drags are not drags of this manager; this only lets
    /// targets work out whether a nested target is hovered (shallow <c>IsOver</c>).
    /// </summary>
    internal void SetNativeOver(string id, string? nativeItemType)
    {
        if (_targets.TryGetValue(id, out var entry) is false || entry.NativeItemType == nativeItemType) return;
        entry.NativeItemType = nativeItemType;
        NotifyStateChanged();
    }

    /// <summary>The native type the native drag hovering target <paramref name="id"/> matched, or null.</summary>
    internal string? GetNativeItemType(string id) =>
        _targets.TryGetValue(id, out var entry) ? entry.NativeItemType : null;

    /// <summary>
    /// Whether a native drag that target <paramref name="id"/> accepts hovers it - with
    /// <paramref name="shallow"/>, and no accepting target nested in it.
    /// </summary>
    internal bool IsNativeOver(string id, bool shallow = false)
    {
        if (_targets.TryGetValue(id, out var entry) is false || entry.NativeItemType is null) return false;
        return shallow is false || HasHoveredDescendant(entry, static t => t.NativeItemType is not null) is false;
    }

    internal void EndDrag()
    {
        if (_isDragging is false) return;
        _isDragging = false;
        _pendingBegin = null;
        _pendingDrops = null;
        _dropCount = 0;
        _dropResultPlace = 0;
        _item = null;
        _itemType = null;
        _sourceId = null;
        _dropResult = null;
        _didDrop = false;
        _initialClientOffset = null;
        _initialSourceClientOffset = null;
        _clientOffset = null;
        foreach (var t in _targets.Values) t.IsOver = false;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => StateChanged?.Invoke();

    // ---- IBdndMonitor -------------------------------------------------------------

    public bool IsDragging() => _isDragging;

    public bool IsDragging(string? sourceId)
    {
        if (_isDragging is false) return false;
        if (sourceId is null) return true;
        return _sourceId == sourceId;
    }

    public string? GetItemType() => _itemType;

    public object? GetItem() => _item;

    public T? GetItem<T>() => _item is T t ? t : default;

    public object? GetDropResult() => _dropResult;

    public T? GetDropResult<T>() => _dropResult is T t ? t : default;

    public bool DidDrop() => _didDrop;

    public bool CanDrop(string handlerId)
    {
        if (_isDragging is false) return false;
        if (_targets.TryGetValue(handlerId, out var entry) is false) return false;
        if (_itemType is null || entry.Accept.Contains(_itemType) is false) return false;
        return entry.CanDrop?.Invoke(_item, this) ?? true;
    }

    public bool IsOver(string handlerId, bool shallow = false)
    {
        if (_isDragging is false || _targets.TryGetValue(handlerId, out var entry) is false || entry.IsOver is false) return false;
        // shallow: no hovered target is nested inside this one
        return shallow is false || HasHoveredDescendant(entry, static t => t.IsOver) is false;
    }

    private bool HasHoveredDescendant(BdndDropTargetEntry entry, Func<BdndDropTargetEntry, bool> isHovered)
    {
        foreach (var other in _targets.Values)
        {
            if (isHovered(other) && ReferenceEquals(other, entry) is false && IsNestedIn(other, entry.Id)) return true;
        }
        return false;
    }

    private bool IsNestedIn(BdndDropTargetEntry entry, string ancestorId)
    {
        for (var parentId = entry.ParentId; parentId is not null;
             parentId = _targets.TryGetValue(parentId, out var parent) ? parent.ParentId : null)
        {
            if (parentId == ancestorId) return true;
        }
        return false;
    }

    public BdndXYCoord? GetInitialClientOffset() => _initialClientOffset;

    public BdndXYCoord? GetInitialSourceClientOffset() => _initialSourceClientOffset;

    public BdndXYCoord? GetClientOffset() => _clientOffset;

    public BdndXYCoord? GetDifferenceFromInitialOffset()
    {
        if (_clientOffset is null || _initialClientOffset is null) return null;
        return _clientOffset.Value - _initialClientOffset.Value;
    }

    public BdndXYCoord? GetSourceClientOffset()
    {
        var diff = GetDifferenceFromInitialOffset();
        if (diff is null || _initialSourceClientOffset is null) return null;
        return _initialSourceClientOffset.Value + diff.Value;
    }
}
