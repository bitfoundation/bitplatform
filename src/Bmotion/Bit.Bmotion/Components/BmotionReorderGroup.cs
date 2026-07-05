using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

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
/// horizontal lists; wrapping grids are not supported.
/// </summary>
public sealed class BmotionReorderGroup<TItem> : ComponentBase
{
    [Inject] private BmotionAnimationEngine Engine { get; set; } = null!;
    [Inject] private BmotionInterop Interop { get; set; } = null!;

    /// <summary>The list being reordered. Supports <c>@bind-Items</c>.</summary>
    [Parameter, EditorRequired] public List<TItem> Items { get; set; } = null!;

    /// <summary>Raised with the reordered list when a drag commits. Enables <c>@bind-Items</c>.</summary>
    [Parameter] public EventCallback<List<TItem>> ItemsChanged { get; set; }

    /// <summary>
    /// Stable identity for an item across renders. Defaults to the item itself (value
    /// equality); supply one for re-created instances. Keys must be unique.
    /// </summary>
    [Parameter] public Func<TItem, object>? ItemKey { get; set; }

    /// <summary>The axis items are laid out (and dragged) along. Default: vertical.</summary>
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
    private readonly Dictionary<object, double> _dispTarget = new();// last displacement target per key
    private double _listStart;
    private double _gap;
    private bool _pendingTransformClear;

    private bool Horizontal => Axis == BmDragAxis.X;

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
            builder.AddComponentParameter(2, "Drag", Horizontal ? BmDrag.X : BmDrag.Y);
            builder.AddComponentParameter(3, "DragMomentum", false);
            builder.AddComponentParameter(4, "WhileDrag", WhileDrag ?? (BmTarget?)Bm.To(scale: 1.03));
            builder.AddComponentParameter(5, "OnDragStart",
                EventCallback.Factory.Create(this, () => HandleDragStartAsync(captured)));
            builder.AddComponentParameter(6, "OnDrag",
                EventCallback.Factory.Create(this, () => HandleDragMove(captured)));
            builder.AddComponentParameter(7, "OnDragEnd",
                EventCallback.Factory.Create(this, () => HandleDragEndAsync(captured)));
            builder.AddComponentParameter(8, "DragHandle", HandleSelector);
            builder.AddComponentParameter(9, "ChildContent",
                (RenderFragment)(b => b.AddContent(0, ChildContent, captured.Item)));
            builder.AddComponentReferenceCapture(10, o => captured.Ref = (Bmotion)o);
            builder.CloseComponent();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // After a commit re-render the DOM order matches the transform-displaced positions
        // exactly, so zeroing every transform is pixel-neutral - it just hands layout back to
        // the document flow.
        if (!_pendingTransformClear) return;
        _pendingTransformClear = false;
        foreach (var entry in _entries)
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
    }

    // ── Drag session ──────────────────────────────────────────────────────────

    private async Task HandleDragStartAsync(Entry entry)
    {
        _dragged = entry;
        _visualOrder = _entries.ToList();
        _origStart.Clear();
        _extent.Clear();
        _dispTarget.Clear();

        // Measure every row's untransformed position (its rect minus any in-flight transform
        // from a previous, still-settling reorder).
        foreach (var e in _entries)
        {
            var rect = await Interop.GetBoundingRectAsync(e.Id);
            if (rect is null || e.Ref is null) { _dragged = null; return; }
            var xy = e.Ref.GetCurrentXY();
            _origStart[e.Key] = (Horizontal ? rect.Left - xy.X : rect.Top - xy.Y);
            _extent[e.Key] = Horizontal ? rect.Width : rect.Height;
        }

        _listStart = _entries.Min(e => _origStart[e.Key]);
        // Derive the inter-item gap from the first adjacent pair (uniform-gap flex/grid lists).
        _gap = 0;
        var sorted = _entries.OrderBy(e => _origStart[e.Key]).ToList();
        if (sorted.Count > 1)
            _gap = Math.Max(0,
                _origStart[sorted[1].Key] - (_origStart[sorted[0].Key] + _extent[sorted[0].Key]));

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
        if (_dragged != entry || entry.Ref is null) return;

        var xy = entry.Ref.GetCurrentXY();
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
        if (_dragged != entry || entry.Ref is null) return;

        // Settle the dragged row into its final slot, then commit the new order.
        int index = _visualOrder.IndexOf(entry);
        double target = SlotStart(index) - _origStart[entry.Key];
        var values = new Dictionary<string, object?> { [Horizontal ? "x" : "y"] = target };
        await Engine.AnimateToAwaitAsync(entry.Id, values, DisplacementConfig());

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
            double target = SlotStart(i) - _origStart[e.Key];
            if (_dispTarget.TryGetValue(e.Key, out double prev) && prev == target) continue;
            _dispTarget[e.Key] = target;
            var values = new Dictionary<string, object?> { [Horizontal ? "x" : "y"] = target };
            _ = Engine.AnimateToAsync(e.Id, values, DisplacementConfig());
        }
    }

    private BmotionTransitionConfig DisplacementConfig()
        => (Transition ?? new BmSpring { Stiffness = 550, Damping = 40 }).ToConfig();
}
