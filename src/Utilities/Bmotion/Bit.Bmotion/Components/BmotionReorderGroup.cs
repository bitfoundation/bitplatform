using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace Bit.Bmotion;

/// <summary>
/// Drag-to-reorder list - motion.dev's <c>Reorder.Group</c>/<c>Reorder.Item</c> in a single
/// component. Renders <see cref="ChildContent"/> once per item; every item is draggable along
/// <see cref="Axis"/>, siblings spring out of the way while dragging, and the new order is
/// committed to <see cref="Items"/> on release:
/// <code>
/// &lt;BmotionReorderGroup @bind-Items="_tracks" ItemKey="t =&gt; ((Track)t).Id" Context="track"&gt;
///     &lt;div class="row"&gt;@track.Title&lt;/div&gt;
/// &lt;/BmotionReorderGroup&gt;
/// </code>
/// The reorder preview is transform-based: the list itself only re-renders once, on release,
/// so item state is preserved and the drag stays smooth. Works for vertical (default) and
/// horizontal lists, and wrapping grids (<see cref="Axis"/> = <see cref="BmDragAxis.Both"/>), where the dragged item
/// takes the cell nearest its centre.
/// <para>
/// Keyboard users reorder too (<see cref="Keyboard"/>): Space or Enter on a focused item picks it up, the arrow keys
/// along the list move it - the others making way as for a drag - Space or Enter drops it and Escape puts it back.
/// Each step is announced to screen readers (<see cref="Announce"/>). Items are made focusable if they aren't.
/// </para>
/// </summary>
public sealed class BmotionReorderGroup<TItem> : ComponentBase, IDisposable
{
    [Inject] private BmotionAnimationEngine Engine { get; set; } = null!;
    [Inject] private IBmotionInterop Interop { get; set; } = null!;

    /// <summary>The list being reordered. Supports <c>@bind-Items</c>.</summary>
    [Parameter, EditorRequired] public List<TItem> Items { get; set; } = null!;

    /// <summary>Raised with the reordered list when a drag commits. Enables <c>@bind-Items</c>.</summary>
    [Parameter] public EventCallback<List<TItem>> ItemsChanged { get; set; }

    /// <summary>
    /// Stable identity for an item across renders. Defaults to the item itself (value
    /// equality); supply one for re-created instances. Keys must be unique.
    /// </summary>
    [Parameter] public Func<TItem, object>? ItemKey { get; set; }

    /// <summary>
    /// How the items are laid out, and dragged: <see cref="BmDragAxis.Y"/> (a column, the default),
    /// <see cref="BmDragAxis.X"/> (a row), or <see cref="BmDragAxis.Both"/> - a wrapping grid, where an item is dragged
    /// freely and takes the cell nearest its centre, and the arrow keys move it a cell at a time (up and down, a row).
    /// </summary>
    [Parameter] public BmDragAxis Axis { get; set; } = BmDragAxis.Y;

    /// <summary>Template for each item. Its first root element becomes the draggable row.</summary>
    [Parameter] public RenderFragment<TItem>? ChildContent { get; set; }

    /// <summary>Gesture overlay while an item is dragged. Default: a slight scale-up.</summary>
    [Parameter] public BmTarget? WhileDrag { get; set; }

    /// <summary>
    /// CSS selector of a drag handle inside each item (e.g. <c>".grip"</c>): the drag only
    /// starts from the handle, leaving the rest of the row free for clicks and text selection.
    /// </summary>
    [Parameter] public string? HandleSelector { get; set; }

    /// <summary>Spring used for sibling displacement and the release settle. Default: a snappy spring.</summary>
    [Parameter] public BmTransition? Transition { get; set; }

    /// <summary>Fires after a reorder is committed to <see cref="Items"/>.</summary>
    [Parameter] public EventCallback OnReorder { get; set; }

    /// <summary>
    /// Whether dragging an item near the edge of the list's scroll container - or the page - scrolls it, so an item
    /// can be carried past what is in view (<see cref="Bmotion.DragAutoScroll"/>). Default: <c>true</c>.
    /// </summary>
    [Parameter] public bool AutoScroll { get; set; } = true;

    /// <summary>
    /// Whether keyboard users can reorder: Space/Enter picks the focused item up and drops it, the arrow keys along
    /// <see cref="Axis"/> move it while it is held, Escape puts it back. Default: <c>true</c>.
    /// </summary>
    [Parameter] public bool Keyboard { get; set; } = true;

    /// <summary>
    /// What screen readers hear at each keyboard reorder step - picked up, moved, dropped, cancelled - e.g. to name
    /// the item or speak another language. Default: <see cref="BmReorderAnnouncement{TItem}.DefaultText"/>.
    /// </summary>
    [Parameter] public Func<BmReorderAnnouncement<TItem>, string>? Announce { get; set; }

    private sealed class Entry
    {
        public required object Key { get; init; }
        public required TItem Item { get; set; }
        public required string Id { get; init; }
        public Bmotion? Ref { get; set; }
    }

    private readonly List<Entry> _entries = new();

    // ── Active drag session ────────────────────────────────────────────────────
    private Entry? _dragged;
    private List<Entry> _visualOrder = new();
    private readonly Dictionary<object, double> _origStart = new(); // untransformed main-axis start per key
    private readonly Dictionary<object, double> _extent = new();    // main-axis size per key
    private readonly Dictionary<object, (double X, double Y)> _dispTarget = new(); // last displacement target per key
    // A grid's cells (Axis = Both) - where each item sat when the session started, which the items move between - and
    // each item's own untransformed box.
    private readonly List<(double Left, double Top, double Width, double Height)> _cells = new();
    private readonly Dictionary<object, (double Left, double Top, double Width, double Height)> _box = new();
    private double _listStart;
    private double _gap;
    private bool _pendingTransformClear;

    private bool Horizontal => Axis == BmDragAxis.X;
    private bool Grid => Axis == BmDragAxis.Both;

    protected override void OnParametersSet()
    {
        var items = Items ?? throw new InvalidOperationException("BmotionReorderGroup requires Items.");
        Func<TItem, object> keyOf = ItemKey ?? (static item => item!);

        var incoming = new Dictionary<object, TItem>(items.Count);
        var order = new List<object>(items.Count);
        foreach (var item in items)
        {
            var key = keyOf(item) ?? throw new InvalidOperationException(
                "BmotionReorderGroup ItemKey returned null; keys must be non-null and unique.");
            if (!incoming.TryAdd(key, item))
                throw new InvalidOperationException(
                    $"BmotionReorderGroup received duplicate item key '{key}'. Keys must be unique.");
            order.Add(key);
        }

        // An external Items mutation (keys added/removed/reordered) while a drag preview is in
        // flight invalidates the measured session; drop it and follow the new list.
        if (_dragged != null && !order.SequenceEqual(_entries.Select(e => e.Key)))
            _dragged = null;

        var byKey = _entries.ToDictionary(e => e.Key, e => e);
        _entries.Clear();
        foreach (var key in order)
        {
            if (byKey.TryGetValue(key, out var existing))
            {
                existing.Item = incoming[key];
                _entries.Add(existing);
            }
            else
            {
                _entries.Add(new Entry { Key = key, Item = incoming[key], Id = $"bm-reorder-{Guid.NewGuid():N}" });
            }
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (ChildContent is null) return;
        foreach (var entry in _entries)
        {
            var captured = entry;
            builder.OpenComponent<Bmotion>(0);
            builder.SetKey(captured.Key);
            builder.AddComponentParameter(1, "Id", captured.Id);
            builder.AddComponentParameter(2, "Drag", Grid ? BmDrag.Both : Horizontal ? BmDrag.X : BmDrag.Y);
            builder.AddComponentParameter(3, "DragMomentum", false);
            builder.AddComponentParameter(4, "WhileDrag", WhileDrag ?? (BmTarget?)Bm.To(scale: 1.03));
            builder.AddComponentParameter(5, "OnDragStart",
                EventCallback.Factory.Create(this, () => HandleDragStartAsync(captured)));
            builder.AddComponentParameter(6, "OnDrag",
                EventCallback.Factory.Create(this, () => HandleDragMove(captured)));
            builder.AddComponentParameter(7, "OnDragEnd",
                EventCallback.Factory.Create(this, () => HandleDragEndAsync(captured)));
            builder.AddComponentParameter(8, "DragHandle", HandleSelector);
            builder.AddComponentParameter(9, "DragAutoScroll", AutoScroll);
            builder.AddComponentParameter(10, "ChildContent",
                (RenderFragment)(b => b.AddContent(0, ChildContent, captured.Item)));
            builder.AddComponentReferenceCapture(11, o => captured.Ref = (Bmotion)o);
            builder.CloseComponent();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await SyncKeysAsync();

        // After a commit re-render the DOM order matches the transform-displaced positions
        // exactly, so zeroing every transform is pixel-neutral - it just hands layout back to
        // the document flow.
        if (!_pendingTransformClear) return;
        _pendingTransformClear = false;
        foreach (var entry in _entries.ToArray()) // a snapshot: a parameter update can rebuild the list while this awaits
        {
            Engine.SetInstant(entry.Id, new Dictionary<string, object?> { ["x"] = 0.0, ["y"] = 0.0 });
            try
            {
                await Interop.ApplyStylesAsync(entry.Id, new Dictionary<string, string>
                {
                    ["transform"] = "translate(0px, 0px)",
                    ["zIndex"] = "",
                    ["position"] = "",
                });
            }
            catch { /* element may be gone mid-teardown */ }
        }

        // Re-rendering in the new order can move the focused item's element, which loses focus: a keyboard user
        // keeps hold of the item they just moved.
        if (_refocus is { } id)
        {
            _refocus = null;
            try { await Interop.FocusAsync(id); } catch { /* gone */ }
        }
    }

    // ── Drag session ──────────────────────────────────────────────────────────

    // The drag start's measurement pass, and whether it has completed. Moves and the release can
    // arrive while it is still measuring (always on Blazor Server, where every measurement is a round
    // trip): a move before then has no positions to work with and is skipped - the next one catches
    // up - and the release waits for it.
    private Task _measuring = Task.CompletedTask;
    private bool _measured;
    // Bumped by every measurement pass, so an older pass still awaiting a round trip can tell that
    // a newer drag session has replaced it and must not touch that session's state.
    private int _dragSession;

    private Task HandleDragStartAsync(Entry entry) => _measuring = StartDragSessionAsync(entry);

    private async Task StartDragSessionAsync(Entry entry)
    {
        // A pointer takes over from an item held from the keyboard: it goes back first - after the keyboard step in
        // flight, which may still be measuring the held item or committing a drop.
        await Step(() => _held is { } held ? CancelHeldAsync(held) : Task.CompletedTask);
        await MeasureDragSessionAsync(entry);
    }

    private async Task MeasureDragSessionAsync(Entry entry)
    {
        int session = ++_dragSession;
        _measured = false;
        _dragged = entry;
        _visualOrder = _entries.ToList();
        _origStart.Clear();
        _extent.Clear();
        _dispTarget.Clear();
        _cells.Clear();
        _box.Clear();

        // Measure every row's untransformed position (its rect minus any in-flight transform
        // from a previous, still-settling reorder).
        foreach (var e in _entries.ToArray()) // a snapshot: a parameter update can rebuild the list while this awaits
        {
            var rect = await Interop.GetBoundingRectAsync(e.Id);
            if (session != _dragSession) return;
            if (rect is null || e.Ref is null) { _dragged = null; return; }
            var xy = e.Ref.GetCurrentXY();
            _origStart[e.Key] = (Horizontal ? rect.Left - xy.X : rect.Top - xy.Y);
            _extent[e.Key] = Horizontal ? rect.Width : rect.Height;
            _box[e.Key] = (rect.Left - xy.X, rect.Top - xy.Y, rect.Width, rect.Height);
        }
        foreach (var e in _visualOrder) _cells.Add(_box[e.Key]);

        _listStart = _entries.Min(e => _origStart[e.Key]);
        // Derive the inter-item gap from the first adjacent pair (uniform-gap flex/grid lists).
        _gap = 0;
        var sorted = _entries.OrderBy(e => _origStart[e.Key]).ToList();
        if (sorted.Count > 1)
            _gap = Math.Max(0,
                _origStart[sorted[1].Key] - (_origStart[sorted[0].Key] + _extent[sorted[0].Key]));
        _measured = true;

        // Lift the dragged row above its siblings for the duration of the session.
        try
        {
            await Interop.ApplyStylesAsync(entry.Id, new Dictionary<string, string>
                { ["position"] = "relative", ["zIndex"] = "5" });
        }
        catch { /* cosmetic only */ }
    }

    private void HandleDragMove(Entry entry)
    {
        if (_dragged != entry || !_measured || entry.Ref is null) return;

        var xy = entry.Ref.GetCurrentXY();
        if (Grid)
        {
            // The cell nearest the dragged item's centre is its place; the others close up around it.
            var box = _box[entry.Key];
            int nearest = NearestCell(box.Left + (box.Width / 2) + xy.X, box.Top + (box.Height / 2) + xy.Y);
            int at = _visualOrder.IndexOf(entry);
            if (nearest == at) return;
            _visualOrder.RemoveAt(at);
            _visualOrder.Insert(nearest, entry);
            AnimateDisplacements();
            return;
        }
        double offset = Horizontal ? xy.X : xy.Y;
        double center = _origStart[entry.Key] + _extent[entry.Key] / 2 + offset;

        // Bubble the dragged row through the visual order until its center sits in its slot.
        bool changed = false;
        while (true)
        {
            int i = _visualOrder.IndexOf(entry);
            if (i > 0 && center < SlotCenter(i - 1))
            {
                (_visualOrder[i - 1], _visualOrder[i]) = (_visualOrder[i], _visualOrder[i - 1]);
                changed = true;
                continue;
            }
            if (i < _visualOrder.Count - 1 && center > SlotCenter(i + 1))
            {
                (_visualOrder[i + 1], _visualOrder[i]) = (_visualOrder[i], _visualOrder[i + 1]);
                changed = true;
                continue;
            }
            break;
        }

        if (changed) AnimateDisplacements();
    }

    private async Task HandleDragEndAsync(Entry entry)
    {
        await _measuring;
        if (_dragged != entry || !_measured || entry.Ref is null) return;
        // Catch the order up with where the row was released: on Blazor Server the moves that would
        // have reordered it may all have arrived before the measurement did.
        HandleDragMove(entry);
        await CommitAsync(entry);
    }

    // Settles the moved row into its final slot, then commits the new order - the end of a drag, and of a keyboard
    // move once the item is dropped.
    private async Task CommitAsync(Entry entry)
    {
        await Engine.AnimateToAwaitAsync(entry.Id, DisplacementValues(entry, _visualOrder.IndexOf(entry)), DisplacementConfig());

        if (_dragged != entry) return; // a new drag or external change superseded this session
        _dragged = null;

        var newOrder = _visualOrder.ToList();
        bool orderChanged = !newOrder.SequenceEqual(_entries);
        _entries.Clear();
        _entries.AddRange(newOrder);
        _pendingTransformClear = true;

        if (orderChanged)
        {
            var newItems = newOrder.Select(e => e.Item).ToList();
            if (ItemsChanged.HasDelegate)
            {
                await ItemsChanged.InvokeAsync(newItems);
            }
            else
            {
                Items.Clear();
                Items.AddRange(newItems);
            }
            await OnReorder.InvokeAsync();
        }
        StateHasChanged();
    }

    // ── Keyboard ──────────────────────────────────────────────────────────────

    private DotNetObjectReference<BmotionReorderGroup<TItem>>? _keysRef;
    // The items listening for the keyboard, and the axis and handle each listens with.
    private readonly Dictionary<string, (string Axis, string? Handle)> _keysAttached = new(StringComparer.Ordinal);
    // The item a keyboard user is holding, and the order it was picked up from (for Escape).
    private Entry? _held;
    private List<Entry>? _orderBeforeHold;
    // An item to give focus back to once the order it was moved into has rendered.
    private string? _refocus;
    // Keyboard steps run one after another: each awaits round trips (measuring, on Blazor Server), and a held arrow
    // key repeats faster than that.
    private Task _keySteps = Task.CompletedTask;

    // Brings every item's keyboard listener in line with Keyboard, Axis and HandleSelector.
    private async Task SyncKeysAsync()
    {
        if (!Keyboard)
        {
            // Nothing listens any more, so an item held from the keyboard could never be dropped: it goes back.
            if (_held is { } held) await Step(() => _held == held ? CancelHeldAsync(held) : Task.CompletedTask);
            foreach (var id in _keysAttached.Keys.ToArray())
            {
                _keysAttached.Remove(id);
                try { await Interop.DetachReorderKeysAsync(id); } catch { /* gone */ }
            }
            return;
        }

        _keysRef ??= DotNetObjectReference.Create(this);
        foreach (var entry in _entries.ToArray()) // a snapshot: a parameter update can rebuild the list while this awaits
        {
            if (!Keyboard) return; // turned off while this awaited: that render detaches them
            // Read each time round, as the parameters can change while this awaits. Attaching again replaces the
            // item's listener with one on the new settings.
            var settings = (Axis: Grid ? "both" : Horizontal ? "x" : "y", Handle: HandleSelector);
            if (_keysAttached.TryGetValue(entry.Id, out var attached) && attached == settings) continue;
            _keysAttached[entry.Id] = settings;
            try { await Interop.AttachReorderKeysAsync(entry.Id, _keysRef, settings.Axis, settings.Handle); }
            catch { _keysAttached.Remove(entry.Id); /* not attached yet: the next render retries */ }
        }
    }

    private Task Step(Func<Task> step)
    {
        var previous = _keySteps;
        return _keySteps = RunAsync();

        async Task RunAsync()
        {
            try { await previous; } catch { /* a failed step doesn't block the next */ }
            await step();
        }
    }

    /// <summary>Space or Enter on an item that isn't held: picks it up. Returns whether it was. Not for direct use.</summary>
    [JSInvokable]
    public async Task<bool> OnReorderPickUp(string elementId)
    {
        bool held = false;
        await Step(async () => held = await PickUpAsync(elementId));
        return held;
    }

    /// <summary>An arrow key while an item is held: moves it one place. Not for direct use.</summary>
    [JSInvokable]
    public Task OnReorderMove(string elementId, int delta) => Step(() => MoveHeldAsync(elementId, Math.Sign(delta)));

    /// <summary>Up or down while an item of a grid is held: moves it a row. Not for direct use.</summary>
    [JSInvokable]
    public Task OnReorderMoveRow(string elementId, int rows) => Step(() => MoveHeldAsync(elementId, Math.Sign(rows) * Columns()));

    /// <summary>Space, Enter or focus leaving a held item: drops it where it is. Not for direct use.</summary>
    [JSInvokable]
    public Task OnReorderDrop(string elementId) => Step(() => DropHeldAsync(elementId));

    /// <summary>Escape on a held item: puts it back where it was picked up. Not for direct use.</summary>
    [JSInvokable]
    public Task OnReorderCancel(string elementId)
        => Step(() => _held is { } held && held.Id == elementId ? CancelHeldAsync(held) : Task.CompletedTask);

    private async Task<bool> PickUpAsync(string elementId)
    {
        // A pointer drag - under way, or starting once the keyboard steps before it are done - or another held item
        // owns the session.
        if (_dragged != null || !_measuring.IsCompleted || _entries.Find(e => e.Id == elementId) is not { } entry) return false;
        _orderBeforeHold = _entries.ToList();
        _held = entry;
        await (_measuring = MeasureDragSessionAsync(entry));
        if (_dragged != entry || !_measured)
        {
            _held = null;
            return false;
        }
        if (entry.Ref is { } row) await row.ShowDragOverlayAsync(true);
        await AnnounceAsync(BmReorderEvent.PickedUp, entry);
        return true;
    }

    private async Task MoveHeldAsync(string elementId, int delta)
    {
        if (_held is not { } entry || entry.Id != elementId || _dragged != entry || !_measured) return;
        int from = _visualOrder.IndexOf(entry);
        int to = Math.Clamp(from + delta, 0, _visualOrder.Count - 1);
        if (to == from) return;
        _visualOrder.RemoveAt(from);
        _visualOrder.Insert(to, entry);
        // The others make way exactly as for a drag; the held item moves into its new slot itself.
        AnimateDisplacements();
        _ = Engine.AnimateToAsync(entry.Id, DisplacementValues(entry, to), DisplacementConfig());
        await AnnounceAsync(BmReorderEvent.Moved, entry);
    }

    private async Task DropHeldAsync(string elementId)
    {
        if (_held is not { } entry || entry.Id != elementId) return;
        _held = null;
        _orderBeforeHold = null;
        if (entry.Ref is { } row) await row.ShowDragOverlayAsync(false);
        if (_dragged != entry || !_measured) return;
        int position = _visualOrder.IndexOf(entry) + 1;
        _refocus = entry.Id;
        await CommitAsync(entry);
        await AnnounceAsync(BmReorderEvent.Dropped, entry, position);
    }

    private async Task CancelHeldAsync(Entry entry)
    {
        _held = null;
        if (entry.Ref is { } row) await row.ShowDragOverlayAsync(false);
        if (_dragged == entry && _orderBeforeHold is { } before)
        {
            // Everything goes back to where it was; nothing is committed.
            _visualOrder = before;
            AnimateDisplacements();
            _ = Engine.AnimateToAsync(entry.Id, DisplacementValues(entry, _visualOrder.IndexOf(entry)), DisplacementConfig());
            _dragged = null;
            try { await Interop.ApplyStylesAsync(entry.Id, new Dictionary<string, string> { ["position"] = "", ["zIndex"] = "" }); }
            catch { /* cosmetic only */ }
        }
        _orderBeforeHold = null;
        await AnnounceAsync(BmReorderEvent.Cancelled, entry);
    }

    private async Task AnnounceAsync(BmReorderEvent kind, Entry entry, int? position = null)
    {
        var order = _dragged == entry ? _visualOrder : _entries;
        var announcement = new BmReorderAnnouncement<TItem>(kind, entry.Item, position ?? order.IndexOf(entry) + 1, _entries.Count);
        var text = Announce?.Invoke(announcement) ?? announcement.DefaultText;
        if (string.IsNullOrEmpty(text)) return;
        try { await Interop.AnnounceAsync(text); } catch { /* the circuit is going */ }
    }

    public void Dispose() => _keysRef?.Dispose();

    // ── Slot math (current visual order) ──────────────────────────────────────

    private double SlotStart(int index)
    {
        double pos = _listStart;
        for (int i = 0; i < index; i++)
            pos += _extent[_visualOrder[i].Key] + _gap;
        return pos;
    }

    private double SlotCenter(int index) => SlotStart(index) + _extent[_visualOrder[index].Key] / 2;

    private void AnimateDisplacements()
    {
        for (int i = 0; i < _visualOrder.Count; i++)
        {
            var e = _visualOrder[i];
            if (e == _dragged) continue;
            var target = Displacement(e, i);
            if (_dispTarget.TryGetValue(e.Key, out var prev) && prev == target) continue;
            _dispTarget[e.Key] = target;
            _ = Engine.AnimateToAsync(e.Id, DisplacementValues(e, i), DisplacementConfig());
        }
    }

    // How far the item at visual index `index` is moved from its own place: along the list, or to a grid's cell.
    private (double X, double Y) Displacement(Entry e, int index)
    {
        if (Grid)
        {
            var (cell, box) = (_cells[index], _box[e.Key]);
            return (cell.Left - box.Left, cell.Top - box.Top);
        }
        double along = SlotStart(index) - _origStart[e.Key];
        return Horizontal ? (along, 0) : (0, along);
    }

    private Dictionary<string, object?> DisplacementValues(Entry e, int index)
    {
        var (x, y) = Displacement(e, index);
        if (Grid) return new() { ["x"] = x, ["y"] = y };
        return Horizontal ? new() { ["x"] = x } : new() { ["y"] = y };
    }

    // The grid cell whose centre is nearest (x, y).
    private int NearestCell(double x, double y)
    {
        int nearest = 0;
        double best = double.MaxValue;
        for (int i = 0; i < _cells.Count; i++)
        {
            var c = _cells[i];
            double d = Math.Pow(c.Left + (c.Width / 2) - x, 2) + Math.Pow(c.Top + (c.Height / 2) - y, 2);
            if (d < best) (best, nearest) = (d, i);
        }
        return nearest;
    }

    // How many cells a grid row holds: those level with the first.
    private int Columns()
    {
        if (!Grid || _cells.Count == 0) return 1;
        return Math.Max(1, _cells.Count(c => Math.Abs(c.Top - _cells[0].Top) < 1));
    }

    private BmotionTransitionConfig DisplacementConfig()
        => (Transition ?? new BmSpring { Stiffness = 550, Damping = 40 }).ToConfig();
}
