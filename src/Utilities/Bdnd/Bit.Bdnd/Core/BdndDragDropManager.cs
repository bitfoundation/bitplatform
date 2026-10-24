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
    private IBdndDragSourceHandler? _activeSource;
    private BdndDragInput _input;
    private string? _keyboardTargetId;
    private FocusRequest? _focusRequest;
    private Func<Task>? _pendingBegin;
    private Task? _beginSettled;
    private List<Task>? _pendingDrops;
    private int _dropCount; // drops of the current drag, in drop order (innermost target first)
    private int _dropResultPlace; // the place in that order of the drop _dropResult came from
    private readonly List<OffsetFollower> _offsetFollowers = [];

    /// <summary>Raised on structural changes (begin/end/enter/leave).</summary>
    public event Action? StateChanged;

    /// <summary>Raised frequently while dragging as the pointer offset changes.</summary>
    public event Action? OffsetChanged;

    /// <summary>
    /// Raised when a drag of this manager begins - once it is published, right before the source's <c>OnBegin</c>
    /// runs - whichever source and input started it.
    /// </summary>
    public event Action<BdndDragEventArgs>? DragStarted;

    /// <summary>
    /// Raised when a drag of this manager ends - dropped, cancelled, or ended because another began - with its
    /// outcome, after the drag state was cleared.
    /// </summary>
    public event Action<BdndDragEventArgs>? DragEnded;

    /// <summary>The shared monitor instance for this provider subtree.</summary>
    public IBdndMonitor Monitor => this;

    /// <summary>
    /// The texts screen readers are given about keyboard drags of this manager's sources: the instructions and the
    /// live-region announcements. A <see cref="BdndProvider"/> given <see cref="BdndProvider.Announcements"/> sets it.
    /// </summary>
    public BdndAnnouncements Announcements
    {
        get => _announcements;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (ReferenceEquals(value, _announcements)) return;
            _announcements = value;
            var instructionsId = InstructionsIdOf(value.Instructions);
            if (instructionsId == InstructionsId) return;
            InstructionsId = instructionsId;
            // the sources render the id into aria-describedby
            NotifyStateChanged();
        }
    }

    private BdndAnnouncements _announcements = BdndAnnouncements.Default;

    /// <summary>
    /// The id of the (hidden) element holding <see cref="BdndAnnouncements.Instructions"/>, which keyboard-draggable
    /// sources reference through <c>aria-describedby</c>. Derived from the text, so providers with the same
    /// instructions share one element.
    /// </summary>
    internal string InstructionsId { get; private set; } = InstructionsIdOf(BdndAnnouncements.Default.Instructions);

    // FNV-1a: stable across processes (string.GetHashCode is randomized), so a prerendered id matches the
    // interactive one
    private static string InstructionsIdOf(string text)
    {
        uint hash = 2166136261;
        foreach (var c in text) hash = (hash ^ c) * 16777619;
        return $"bit-bdnd-instructions-{hash:x8}";
    }

    /// <summary>Generates a handler id unique within this manager, e.g. <c>card-3</c>.</summary>
    public string GenerateHandlerId(string prefix) => $"{prefix}-{++_idSeed}";

    /// <summary>Identifies the current drag; changes with every <see cref="BeginDrag"/>.</summary>
    internal long DragId => _dragId;

    /// <summary>
    /// Whether the pointer moves of a drag of <paramref name="itemType"/> are followed - by an
    /// <see cref="OffsetChanged"/> handler, or a <see cref="BdndDragLayer{TItem}"/> that draws that type: only then
    /// does the dragged source report every pointer move (on Blazor Server each one is a message over the circuit).
    /// </summary>
    internal bool FollowsPointer(string? itemType) => OffsetChanged is not null || _offsetFollowers.Any(f => f.Follows(itemType));

    /// <summary>
    /// Follows the offsets of the drags of <paramref name="itemTypes"/> (null or empty: every drag), calling
    /// <paramref name="changed"/> when they change, until the result is disposed - a drag layer drawing some types.
    /// </summary>
    internal IDisposable FollowOffsets(IReadOnlyCollection<string>? itemTypes, Action changed)
    {
        var follower = new OffsetFollower(this, itemTypes, changed);
        _offsetFollowers.Add(follower);
        return follower;
    }

    private void RaiseOffsetChanged()
    {
        OffsetChanged?.Invoke();
        foreach (var follower in _offsetFollowers.ToArray())
        {
            if (follower.Follows(_itemType)) follower.Changed();
        }
    }

    private sealed class OffsetFollower(BdndDragDropManager manager, IReadOnlyCollection<string>? itemTypes, Action changed) : IDisposable
    {
        public Action Changed { get; } = changed;

        public bool Follows(string? itemType) => itemTypes is not { Count: > 0 } || (itemType is not null && itemTypes.Contains(itemType));

        public void Dispose() => manager._offsetFollowers.Remove(this);
    }

    // ---- registration ------------------------------------------------------------

    internal void RegisterTarget(BdndDropTargetEntry entry)
    {
        // Upsert: preserve a live IsOver across re-renders so an in-flight hover
        // is not lost when the component re-registers with new parameters.
        if (_targets.TryGetValue(entry.Id, out var existing))
        {
            existing.Accept = entry.Accept;
            existing.CanDrop = entry.CanDrop;
            existing.Label = entry.Label;
            existing.Handler = entry.Handler;
            existing.Edges = entry.Edges;
            existing.Changed = entry.Changed;
            if (existing.Edge is { } edge && Allows(entry.Edges, edge) is false) existing.Edge = null;
            // A target that no longer accepts what hovers it (disabled, or its types changed) is no longer over:
            // its state, its edge and its enclosing targets' shallow hover follow at once, not when the pointer leaves.
            var changed = false;
            if (existing.IsOver && (_itemType is null || existing.Accept.Contains(_itemType) is false))
            {
                existing.IsOver = false;
                existing.Edge = null;
                changed = true;
            }
            if (existing.NativeItemType is { } native && existing.Accept.Contains(native) is false)
            {
                existing.NativeItemType = null;
                existing.Edge = null;
                changed = true;
            }
            if (changed) NotifyStateChanged();
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
        Func<Task>? onBegun = null, BdndDragInput input = BdndDragInput.Native, IBdndDragSourceHandler? source = null)
    {
        // A drag still in progress here is one whose end never arrived (or has not yet): end it first, so its
        // source's OnEnd runs and every OnBegin has its OnEnd.
        if (_isDragging) _ = AbortDragAsync(); // guarded inside: a failing OnEnd or DragEnded handler cannot stop this drag
        _dragId++;
        _activeSource = source;
        _isDragging = true;
        _input = input;
        _keyboardTargetId = null;
        _focusRequest = null;
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
        foreach (var t in _targets.Values)
        {
            t.IsOver = false;
            t.Edge = null;
        }
        _pendingBegin = onBegun ?? (() => Task.CompletedTask);
        _beginSettled = null;
    }

    /// <summary>
    /// Publishes the drag started by <see cref="BeginDrag"/> (notifies subscribers and runs its
    /// begin callback) if that has not happened yet. Every other drag callback calls this first,
    /// so the source's begin callback always runs before hover, drop and end callbacks.
    /// </summary>
    internal Task FlushBeginAsync()
    {
        var onBegun = _pendingBegin;
        // A begin callback still running holds back every later callback of the drag, not only the first one: a
        // hover report arriving meanwhile must not overtake the one waiting for it. The later ones wait for it to
        // settle, and only the first gets its exception to report.
        if (onBegun is null) return _beginSettled ?? Task.CompletedTask;
        _pendingBegin = null;
        NotifyStateChanged();
        if (_isDragging && _itemType is not null)
        {
            Raise(DragStarted, new BdndDragEventArgs { Item = _item, ItemType = _itemType, Input = _input, Monitor = this });
        }
        Task begin;
        // a begin callback that throws before its first await fails the task instead: the callers guard the task,
        // and the end of the drag must not be skipped by an exception thrown from the call itself
        try
        {
            begin = onBegun();
        }
        catch (Exception exception)
        {
            begin = Task.FromException(exception);
        }
        _beginSettled = begin.IsCompleted ? null : SettleAsync(begin);
        return begin;

        static async Task SettleAsync(Task task)
        {
            try
            {
                await task;
            }
            catch (Exception)
            {
                // reported by the caller that got the task itself
            }
        }
    }

    internal void SetClientOffset(BdndXYCoord offset)
    {
        if (_isDragging is false || _clientOffset == offset) return;
        _clientOffset = offset;
        RaiseOffsetChanged();
    }

    /// <summary>
    /// Patches the source element's initial offset shortly after the drag begins (when the
    /// bounding rect has to be fetched asynchronously). Keeps custom drag layers aligned.
    /// </summary>
    internal void SetInitialSourceClientOffset(BdndXYCoord offset)
    {
        if (_isDragging is false || _initialSourceClientOffset == offset) return;
        _initialSourceClientOffset = offset;
        RaiseOffsetChanged();
    }

    /// <summary>Marks whether the pointer is over a target that accepts the dragged item.</summary>
    internal void SetTargetOver(string id, bool isOver)
    {
        if (_isDragging is false || _targets.TryGetValue(id, out var entry) is false) return;
        if (entry.IsOver == isOver)
        {
            // a leave always forgets the edge, also one recorded while the hover's report was still on its way
            if (isOver is false && entry.Edge is not null)
            {
                entry.Edge = null;
                entry.Changed?.Invoke();
            }
            return;
        }
        entry.IsOver = isOver;
        if (isOver is false) entry.Edge = null;
        NotifyStateChanged();
    }

    /// <summary>
    /// Records the edge of target <paramref name="id"/> the drag over it is closest to - only an edge the target lists.
    /// It is taken even when the report of the hover itself is still on its way (an async <c>OnBegin</c> delays
    /// that), and shown only while the target is hovered; the hover ending forgets it.
    /// </summary>
    internal void SetTargetEdge(string id, BdndEdge? edge)
    {
        if (_targets.TryGetValue(id, out var entry) is false || entry.Edge == edge) return;
        // only for a drag of this manager, or a native drag over the target - not a drag of another provider
        if (_isDragging is false && entry.NativeItemType is null) return;
        if (edge is { } e && Allows(entry.Edges, e) is false) return;
        entry.Edge = edge;
        // the edge is only this target's to show: the others need not even collect their state again
        if (entry.IsOver || entry.NativeItemType is not null) entry.Changed?.Invoke();
    }

    /// <summary>The edge of target <paramref name="id"/> the drag over it is closest to, or null.</summary>
    internal BdndEdge? GetTargetEdge(string id) => _targets.TryGetValue(id, out var entry) ? entry.Edge : null;

    internal static bool Allows(BdndEdges edges, BdndEdge edge) => edge switch
    {
        BdndEdge.Top => edges.HasFlag(BdndEdges.Top),
        BdndEdge.Bottom => edges.HasFlag(BdndEdges.Bottom),
        BdndEdge.Left => edges.HasFlag(BdndEdges.Left),
        BdndEdge.Right => edges.HasFlag(BdndEdges.Right),
        _ => false,
    };

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
        if (nativeItemType is null && _isDragging is false) entry.Edge = null;
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

    /// <summary>
    /// Ends the drag in progress at once, as a cancel: its source's <c>OnEnd</c> runs (with <c>DidDrop</c> false)
    /// and the drag state is cleared before this returns; the task is <c>OnEnd</c>'s.
    /// </summary>
    internal Task AbortDragAsync()
    {
        if (_isDragging is false) return Task.CompletedTask;
        var source = _activeSource;
        var dragId = _dragId;
        var onEnd = source?.AbortAsync(dragId) ?? Task.CompletedTask;
        // a source that could not end it (gone, or never told) still must not leave the drag stuck
        if (_isDragging && _dragId == dragId) EndDrag();
        return onEnd;
    }

    internal void EndDrag()
    {
        if (_isDragging is false) return;
        // a drag whose begin was never published (it ended right away) reports neither its start nor its end
        var ended = _pendingBegin is null && _itemType is not null
            ? new BdndDragEventArgs { Item = _item, ItemType = _itemType, Input = _input, DidDrop = _didDrop, DropResult = _dropResult, Monitor = this }
            : null;
        _isDragging = false;
        _activeSource = null;
        _input = BdndDragInput.Native;
        _keyboardTargetId = null;
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
        foreach (var t in _targets.Values)
        {
            t.IsOver = false;
            t.Edge = null;
        }
        NotifyStateChanged();
        if (ended is not null) Raise(DragEnded, ended);
    }

    // The handlers of DragStarted and DragEnded run while the manager updates its state: one that throws must not leave
    // the drag half begun or half ended. Each runs on its own; a failure goes to the renderer through a provider.
    private void Raise(Action<BdndDragEventArgs>? handlers, BdndDragEventArgs e)
    {
        if (handlers is null) return;
        foreach (var handler in handlers.GetInvocationList().Cast<Action<BdndDragEventArgs>>())
        {
            try
            {
                handler(e);
            }
            catch (Exception exception)
            {
                _ = ReportExceptionAsync(exception);
            }
        }
    }

    // ---- drags made without the native drag-and-drop API ---------------------------
    // The browser bubbles native dragover and drop events through the targets under the pointer. Keyboard drags
    // have no such events, so the manager walks the same chain itself: a target and its enclosing targets of this
    // manager, innermost first - the order nested components put their host elements in.

    /// <summary>The target the keyboard drag in progress is over, or null.</summary>
    internal string? KeyboardTargetId => _keyboardTargetId;

    /// <summary>Target <paramref name="id"/> and its enclosing targets of this manager, innermost first.</summary>
    internal List<string> ChainOf(string id)
    {
        var chain = new List<string>();
        for (string? current = id; current is not null && _targets.TryGetValue(current, out var entry); current = entry.ParentId)
        {
            chain.Add(current);
        }
        return chain;
    }

    /// <summary>
    /// The targets the item being dragged can be dropped on (its type and the target's predicate) and that can take
    /// a drop made without the native API.
    /// </summary>
    internal List<BdndDropTargetEntry> GetDroppableTargets()
    {
        var targets = new List<BdndDropTargetEntry>();
        foreach (var entry in _targets.Values)
        {
            if (entry.Handler is not null && CanDrop(entry.Id)) targets.Add(entry);
        }
        return targets;
    }

    internal BdndDropTargetEntry? GetTarget(string id) => _targets.TryGetValue(id, out var entry) ? entry : null;

    /// <summary>
    /// Puts a keyboard drag over target <paramref name="targetId"/> (null: over none), against its
    /// <paramref name="edge"/>: that target and its enclosing targets that accept the item's type report
    /// <c>IsOver</c>, every other target stops doing so, and only that target reports the edge.
    /// </summary>
    internal void SetKeyboardTarget(string? targetId, BdndEdge? edge = null)
    {
        if (_isDragging is false) return;
        _keyboardTargetId = targetId;
        var chain = targetId is null ? [] : ChainOf(targetId);
        var changed = false;
        foreach (var entry in _targets.Values)
        {
            var over = _itemType is not null && chain.Contains(entry.Id) && entry.Accept.Contains(_itemType);
            var entryEdge = over && entry.Id == targetId && edge is { } e && Allows(entry.Edges, e) ? edge : null;
            if (entry.IsOver == over && entry.Edge == entryEdge) continue;
            entry.IsOver = over;
            entry.Edge = entryEdge;
            changed = true;
        }
        if (changed) NotifyStateChanged();
    }

    /// <summary>Runs the <c>OnHover</c> of target <paramref name="targetId"/> and of its enclosing targets, innermost first.</summary>
    internal Task HoverChainAsync(string targetId, BdndXYCoord? pointer) =>
        RunChainAsync(targetId, static (handler, p) => handler.HoverAsync(p), pointer);

    /// <summary>
    /// Drops on target <paramref name="targetId"/>: the <c>OnDrop</c> of it and of its enclosing targets that can take
    /// the item runs, innermost first. As with native drop events, each starts once the one before it has run up
    /// to its first <c>await</c>, so an outer target sees an inner one's drop through <c>DidDrop()</c>.
    /// </summary>
    internal Task DropChainAsync(string targetId, BdndXYCoord? pointer) =>
        RunChainAsync(targetId, static (handler, p) => handler.DropAsync(p), pointer);

    private Task RunChainAsync(string targetId, Func<IBdndDropTargetHandler, BdndXYCoord?, Task> run, BdndXYCoord? pointer)
    {
        List<Task>? tasks = null;
        // looked up one at a time: a handler may re-render the page and remove a target further out
        foreach (var id in ChainOf(targetId))
        {
            if (_targets.TryGetValue(id, out var entry) && entry.Handler is { } handler)
                (tasks ??= []).Add(run(handler, pointer));
        }
        return tasks is null ? Task.CompletedTask : Task.WhenAll(tasks);
    }

    // ---- focus after a keyboard drag -----------------------------------------------
    // A keyboard drag keeps the focus on the dragged source. When a hover or a drop re-renders the item as another
    // draggable (moved to another list, say), the source's element is gone, and the draggable that now shows the
    // same item takes the focus over.

    private const long FocusRequestLifetimeMs = 1500;

    private sealed record FocusRequest(object? Item, string Type, string SourceId, long ExpiresAt);

    /// <summary>
    /// Asks the draggable that shows <paramref name="item"/> to take the focus over should the source's element lose
    /// it: for as long as the drag lasts, and for a moment after it <paramref name="ended"/>.
    /// </summary>
    internal void RequestFocus(object? item, string type, string sourceId, bool ended) =>
        _focusRequest = new(item, type, sourceId, ended ? Environment.TickCount64 + FocusRequestLifetimeMs : long.MaxValue);

    /// <summary>Whether a draggable showing <paramref name="item"/> of <paramref name="type"/> should take the focus over.</summary>
    internal bool WantsFocus(object? item, string type, string handlerId)
    {
        if (_focusRequest is not { } request) return false;
        if (Environment.TickCount64 > request.ExpiresAt)
        {
            _focusRequest = null;
            return false;
        }
        // an item that is null names nothing: every draggable of the type without an item would match
        return item is not null && request.SourceId != handlerId && request.Type == type && Equals(request.Item, item);
    }

    /// <summary>A draggable took the focus over; once the drag is over nothing else needs to.</summary>
    internal void FocusTaken()
    {
        if (_isDragging is false) _focusRequest = null;
    }

    // ---- exceptions ----------------------------------------------------------------
    // The callbacks the browser module runs from JS have no event dispatch to hand their exceptions to the renderer.
    // A component does that with DispatchExceptionAsync - but a source is often disposed by the time its OnEnd runs
    // (the drop re-rendered it away), and a disposed component cannot. The provider then reports it in its place.

    private readonly List<Func<Exception, Task>> _exceptionSinks = [];

    /// <summary>Lets a provider take the exceptions of callbacks whose component is gone (the last one added is used).</summary>
    internal void AddExceptionSink(Func<Exception, Task> sink) => _exceptionSinks.Add(sink);

    internal void RemoveExceptionSink(Func<Exception, Task> sink) => _exceptionSinks.Remove(sink);

    /// <summary>
    /// Hands an exception of an application callback to the renderer through a provider of this manager; with none
    /// left, writes it to the error output so it is not lost.
    /// </summary>
    internal async Task ReportExceptionAsync(Exception exception)
    {
        for (var i = _exceptionSinks.Count - 1; i >= 0; i--)
        {
            try
            {
                await _exceptionSinks[i](exception);
                return;
            }
            catch (Exception)
            {
                // that provider is gone too: try the next one
            }
        }
        Console.Error.WriteLine($"Bit.Bdnd: an exception was thrown by a drag-and-drop callback whose component is gone: {exception}");
    }

    private void NotifyStateChanged() => StateChanged?.Invoke();

    // ---- IBdndMonitor -------------------------------------------------------------

    public bool IsDragging() => _isDragging;

    public BdndDragInput? GetDragInput() => _isDragging ? _input : null;

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
