using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Bit.Bortable;

/// <summary>
/// A Blazor-native drag-and-drop sortable list, inspired by SortableJS. Reorder items within a list or
/// drag them between lists that share a <see cref="Group"/>. The sorting engine runs in C# on the
/// browser's native drag events; the <c>bit-bortable.js</c> module only hit-tests, installs drag handles
/// and plays the FLIP animations. Needs <see cref="BitBortable.AddBitBortableServices"/>.
/// </summary>
/// <typeparam name="TItem">The type of the bound items.</typeparam>
public partial class Bortable<TItem> : IBortableZone, IHandleEvent, IAsyncDisposable
{
    private const string ModulePath = "./_content/Bit.Bortable/bit-bortable.js";

    private static readonly EqualityComparer<TItem> Comparer = EqualityComparer<TItem>.Default;

    // Items currently selected for multi-drag in this list.
    private readonly HashSet<TItem> _selected = new();

    private ElementReference _listElement;
    private IJSObjectReference? _module;
    private string? _appliedHandle;
    private bool _playFlipOnRender;
    private bool _disposed;

    [Inject] private BortableService Service { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    /// <summary>
    /// The list of items to display and reorder. Bind with <c>@bind-Items</c>. It must be
    /// a mutable list (e.g. <see cref="List{T}"/>); a read-only one (such as an array) can
    /// only be dragged from in <see cref="BortablePull.Clone"/> mode.
    /// </summary>
    [Parameter] public IList<TItem>? Items { get; set; }

    /// <summary>Raised when the bound list changes (supports <c>@bind-Items</c>).</summary>
    [Parameter] public EventCallback<IList<TItem>> ItemsChanged { get; set; }

    /// <summary>Template rendered for each item.</summary>
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>Optional extra content rendered after the items (e.g. an empty-state placeholder).</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Stable DOM id for the list. Auto-generated when not supplied.</summary>
    [Parameter] public string Id { get; set; } = $"bit-bortable-{Guid.NewGuid():N}";

    /// <summary>Lists that share the same group name can exchange items.</summary>
    [Parameter] public string? Group { get; set; }

    /// <summary>Controls how items may be dragged out of this list.</summary>
    [Parameter] public BortablePull Pull { get; set; } = BortablePull.Move;

    /// <summary>Whether items from other lists in the group may be dropped here.</summary>
    [Parameter] public bool Put { get; set; } = true;

    /// <summary>Allow reordering within this list. Set to <c>false</c> to only allow dragging out.</summary>
    [Parameter] public bool Sort { get; set; } = true;

    /// <summary>
    /// The fraction (0–1) of an item that acts as its swap zone, mirroring the SortableJS
    /// <c>swapThreshold</c> option. With the default <c>1</c> the whole item swaps; a smaller
    /// value (e.g. <c>0.65</c>) leaves a dead zone near each edge, so the pointer has to move
    /// further into an item before the dragged one swaps past it.
    /// </summary>
    [Parameter] public double SwapThreshold { get; set; } = 1;

    /// <summary>
    /// When <c>true</c> the swap zones sit at the item's edges instead of its centre, giving a
    /// "sort between items" feel. Mirrors the SortableJS <c>invertSwap</c> option.
    /// </summary>
    [Parameter] public bool InvertSwap { get; set; }

    /// <summary>
    /// The fraction (0–1) of an item used for the inverted swap zone. Defaults to
    /// <see cref="SwapThreshold"/> when left at <c>0</c>. Mirrors <c>invertedSwapThreshold</c>.
    /// </summary>
    [Parameter] public double InvertedSwapThreshold { get; set; }

    /// <summary>
    /// The axis to sort along. <see cref="BortableDirection.Auto"/> detects it from the
    /// layout; set it explicitly for grids. Mirrors the SortableJS <c>direction</c> option.
    /// </summary>
    [Parameter] public BortableDirection Direction { get; set; } = BortableDirection.Auto;

    /// <summary>Disables all drag behaviour when <c>true</c>.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>
    /// Duration in milliseconds of the slide animation when items move to new
    /// positions, mirroring the SortableJS <c>animation</c> option. Set to <c>0</c>
    /// to disable animations. Default is <c>150</c>.
    /// </summary>
    [Parameter] public int Animation { get; set; } = 150;

    /// <summary>
    /// CSS selector for a drag handle within each item. With a handle only the handle starts a drag, and
    /// the rest of the item keeps its normal pointer behaviour (text selection, inputs, links).
    /// </summary>
    [Parameter] public string? Handle { get; set; }

    /// <summary>Predicate returning <c>true</c> for items that must not be dragged.</summary>
    [Parameter] public Func<TItem, bool>? Filter { get; set; }

    /// <summary>Factory used to copy an item when <see cref="Pull"/> is <see cref="BortablePull.Clone"/>.</summary>
    [Parameter] public Func<TItem, TItem>? Clone { get; set; }

    /// <summary>Extra CSS classes for the list container.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Inline styles for the list container.</summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>Base CSS class applied to every item wrapper.</summary>
    [Parameter] public string? ItemClass { get; set; }

    /// <summary>
    /// Optional per-item class selector. Its result is appended to each item's wrapper
    /// class, letting an item keep its own styling regardless of which list holds it.
    /// </summary>
    [Parameter] public Func<TItem, string?>? ItemClassSelector { get; set; }

    /// <summary>Class applied to the item being dragged (and to the drop placeholder). Default <c>bit-bortable-ghost</c>.</summary>
    [Parameter] public string GhostClass { get; set; } = "bit-bortable-ghost";

    /// <summary>Class applied to the chosen item. Default <c>bit-bortable-chosen</c>.</summary>
    [Parameter] public string ChosenClass { get; set; } = "bit-bortable-chosen";

    /// <summary>
    /// Enables the Swap plugin behaviour: instead of sorting, the dragged item trades
    /// places with the item it is dropped on. While dragging, the item under the
    /// pointer is highlighted with <see cref="SwapClass"/>. Mirrors the SortableJS
    /// <c>swap</c> option.
    /// </summary>
    [Parameter] public bool Swap { get; set; }

    /// <summary>
    /// Class applied to the item highlighted as the swap target while dragging in
    /// swap mode. Mirrors the SortableJS <c>swapClass</c> option. Default <c>bit-bortable-swap-highlight</c>.
    /// </summary>
    [Parameter] public string SwapClass { get; set; } = "bit-bortable-swap-highlight";

    /// <summary>
    /// Enables the MultiDrag plugin behaviour: click items to select several, then
    /// drag any one of them to move the whole selection together. Mirrors the
    /// SortableJS <c>multiDrag</c> option.
    /// </summary>
    [Parameter] public bool MultiDrag { get; set; }

    /// <summary>
    /// Class applied to items that are part of the multi-drag selection. Mirrors the
    /// SortableJS <c>selectedClass</c> option. Default <c>bit-bortable-selected</c>.
    /// </summary>
    [Parameter] public string SelectedClass { get; set; } = "bit-bortable-selected";

    /// <summary>Raised on this list when a drag starts in it.</summary>
    [Parameter] public EventCallback<BortableEventArgs<TItem>> OnStart { get; set; }

    /// <summary>Raised on the source list when a drag ends, whatever its outcome.</summary>
    [Parameter] public EventCallback<BortableEventArgs<TItem>> OnEnd { get; set; }

    /// <summary>Raised on the destination list when an item is dropped in from another list.</summary>
    [Parameter] public EventCallback<BortableEventArgs<TItem>> OnAdd { get; set; }

    /// <summary>Raised on the source list when an item is moved out to another list.</summary>
    [Parameter] public EventCallback<BortableEventArgs<TItem>> OnRemove { get; set; }

    /// <summary>Raised when the order of this list changed within the list.</summary>
    [Parameter] public EventCallback<BortableEventArgs<TItem>> OnUpdate { get; set; }

    /// <summary>Raised on every list whose items changed (add, remove or update).</summary>
    [Parameter] public EventCallback<BortableEventArgs<TItem>> OnSort { get; set; }

    /// <summary>Raised while dragging, each time the dragged item moves to a new position in this list.</summary>
    [Parameter] public EventCallback<BortableEventArgs<TItem>> OnChange { get; set; }

    // ----- lifecycle ------------------------------------------------------

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        // Forget selected items that are no longer in the (possibly replaced) list.
        if (_selected.Count > 0)
        {
            var items = Items;
            if (!MultiDrag || items is null) _selected.Clear();
            else _selected.RemoveWhere(item => !items.Contains(item));
        }
    }

    // Drag events fire continuously, so this component decides itself when to
    // re-render instead of re-rendering after every handled DOM event.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);

    // ----- rendering helpers ---------------------------------------------

    private string ContainerCssClass => Css("bit-bortable", Class,
        Disabled ? "bit-bortable-disabled" : null,
        string.IsNullOrEmpty(Handle) ? null : "bit-bortable-has-handle");

    private string ItemCssClass(TItem item)
    {
        var ctx = Service.Context;
        string? dragState = null;
        if (ctx is not null && ReferenceEquals(ctx.CurrentZone, this) && ctx.Item is TItem dragged && Comparer.Equals(dragged, item))
        {
            if (ctx.PlaceholderZone is null || ReferenceEquals(ctx.PlaceholderZone, this))
                dragState = Css(GhostClass, ChosenClass);
            else if (ctx.PullMode != BortablePull.Clone)
                // Previewed as the placeholder in another list: hide the original here, as
                // SortableJS does. For a clone, the original stays put and visible.
                dragState = "bit-bortable-hidden";
        }

        bool swapTarget = ctx is not null && ReferenceEquals(ctx.SwapTargetZone, this)
            && ctx.SwapTargetItem is TItem target && Comparer.Equals(target, item);

        return Css("bit-bortable-item", ItemClass, ItemClassSelector?.Invoke(item), dragState,
            swapTarget ? SwapClass : null,
            MultiDrag && _selected.Contains(item) ? SelectedClass : null,
            IsFiltered(item) ? "bit-bortable-filtered" : null);
    }

    // The preview slot uses the exact same ghost styling as a within-list drag,
    // and keeps the dragged item's own per-item class so its style is preserved.
    private string PlaceholderCssClass()
    {
        var extra = ItemClassSelector is not null && Service.Context?.Item is TItem preview ? ItemClassSelector(preview) : null;
        return Css("bit-bortable-item", ItemClass, extra, GhostClass, ChosenClass);
    }

    private static string Css(params ReadOnlySpan<string?> parts)
    {
        var sb = new StringBuilder();
        foreach (var part in parts)
        {
            if (string.IsNullOrWhiteSpace(part)) continue;
            if (sb.Length > 0) sb.Append(' ');
            sb.Append(part.Trim());
        }
        return sb.ToString();
    }

    private bool IsPlaceholderAt(int index)
    {
        var ctx = Service.Context;
        return ctx is not null && ReferenceEquals(ctx.PlaceholderZone, this) && ctx.PlaceholderIndex == index;
    }

    private bool IsFiltered(TItem item) => Filter is not null && Filter(item);

    private bool CanDrag(TItem item) => !Disabled && !IsFiltered(item);

    // A read-only list (e.g. an array) can be displayed and cloned from, but not changed.
    private bool IsMutable => Items is { IsReadOnly: false };

    private bool AllowSort => Sort && !Disabled && IsMutable;

    private BortablePull EffectivePull => Pull == BortablePull.Move && !IsMutable ? BortablePull.None : Pull;

    private string DirectionValue => Direction switch
    {
        BortableDirection.Horizontal => "horizontal",
        BortableDirection.Vertical => "vertical",
        _ => "auto"
    };

    // ----- multi-drag selection ------------------------------------------

    private EventCallback ItemClickCallback(TItem item)
        => MultiDrag ? EventCallback.Factory.Create(this, () => ToggleSelection(item)) : default;

    /// <summary>Toggles an item's multi-drag selection when clicked (MultiDrag mode).</summary>
    private void ToggleSelection(TItem item)
    {
        if (!MultiDrag || !CanDrag(item)) return;
        if (!_selected.Add(item)) _selected.Remove(item);
        StateHasChanged();
    }

    // ----- drag event handlers -------------------------------------------

    private async Task OnItemDragStart(TItem item)
    {
        if (!CanDrag(item) || Items is null) return;

        BortableMoveInfo info;
        await Service.Gate.WaitAsync();
        try
        {
            // A drag whose end was never observed (e.g. its element was removed mid-drag): discard it.
            if (Service.Context is { } stale) await AbandonAsync(stale);

            int oldIndex = Items.IndexOf(item);
            if (oldIndex < 0) return;

            // Multi-drag: dragging a selected item carries the whole selection; dragging an
            // unselected item drops the selection and drags only that item (like SortableJS).
            List<object>? block = null;
            if (MultiDrag)
            {
                if (_selected.Contains(item))
                {
                    block = new List<object>();
                    foreach (var it in Items)
                        if (_selected.Contains(it)) block.Add(it!);
                }
                else
                {
                    _selected.Clear();
                }
            }

            var ctx = new BortableDragContext
            {
                Item = item!,
                OriginalItem = item!,
                Source = this,
                CurrentZone = this,
                OldIndex = oldIndex,
                PullMode = EffectivePull,
                MultiItems = block is { Count: > 1 } ? block : null
            };
            Service.Context = ctx;
            info = MoveInfo(ctx, this, oldIndex);
            StateHasChanged();
        }
        finally
        {
            Service.Gate.Release();
        }

        await RaiseEventAsync(BortableEventType.Start, info);
    }

    /// <summary>
    /// Positions the drag preview as the pointer moves. Within the home list it
    /// reorders the items live; over another list it shows a drop placeholder.
    /// Driven by <c>dragover</c> (which bubbles up from the items) so the pointer
    /// coordinates are available for swap-threshold hit testing.
    /// </summary>
    private async Task OnContainerDragOver(DragEventArgs e)
    {
        // dragover fires continuously; skip it while a previous one is still being handled.
        if (Service.Context is null || _disposed || !await Service.Gate.WaitAsync(0)) return;

        BortableMoveInfo? change = null;
        IBortableZone? changed = null;
        try
        {
            var ctx = Service.Context;
            if (ctx is null || ctx.Finished) return;

            if (Swap)
            {
                await UpdateSwapTargetAsync(ctx, e);
            }
            else if (ReferenceEquals(ctx.CurrentZone, this))
            {
                change = await ReorderWithinAsync(ctx, e);
                changed = this;
            }
            else
            {
                change = await PreviewFromOtherListAsync(ctx, e);
                changed = this;
            }
        }
        finally
        {
            Service.Gate.Release();
        }

        if (change is not null && changed is not null)
            await changed.RaiseEventAsync(BortableEventType.Change, change);
    }

    /// <summary>Reorders the home list as the dragged item moves over it.</summary>
    private async Task<BortableMoveInfo?> ReorderWithinAsync(BortableDragContext ctx, DragEventArgs e)
    {
        // A swap target highlighted by a swap-mode list of the group no longer applies.
        ClearSwapTarget(ctx);

        if (!AllowSort || Items is null || ctx.Item is not TItem dragged)
        {
            // Back home from a cross-list preview into a list that cannot be sorted: the
            // item returns to its original place (SortableJS "revert").
            await ClearPlaceholderAsync(ctx);
            return null;
        }

        // While the item is previewed in another list its original here is hidden; the
        // drop index is still expressed in this list's indices (hidden item included).
        int target = await GetDropIndexAsync(e.ClientX, e.ClientY);
        if (target < 0) return null; // dead zone: keep the current order (and any preview)

        int from = Items.IndexOf(dragged);
        if (from < 0) return null;
        int insertAt = Math.Min(target > from ? target - 1 : target, Items.Count - 1);
        bool previewedElsewhere = ctx.PlaceholderZone is not null;
        if (insertAt == from && !previewedElsewhere) return null;

        // Back home from a cross-list preview: take the item back, at the pointer's position.
        if (previewedElsewhere) await ClearPlaceholderAsync(ctx);
        await CaptureFlipAsync();
        if (Stale(ctx)) return null;

        if (insertAt != from)
        {
            ctx.Touch(this);
            Items.RemoveAt(from);
            Items.Insert(insertAt, dragged);
        }
        RequestFlipPlay();
        StateHasChanged();

        return insertAt != from ? MoveInfo(ctx, this, insertAt) : null;
    }

    /// <summary>
    /// Previews where an item dragged in from another list would land. The actual
    /// item is not moved until the drag ends, so the native drag stays alive.
    /// </summary>
    private async Task<BortableMoveInfo?> PreviewFromOtherListAsync(BortableDragContext ctx, DragEventArgs e)
    {
        // Refuse to receive an item into its own descendant list (or into the preview
        // copy of it), which would detach the dragged subtree from the render tree.
        if (!CanAccept(ctx) || await WouldNestAsync()) return null;

        int index = await GetDropIndexAsync(e.ClientX, e.ClientY);
        if (index < 0) return null; // dead zone: keep the current preview (if any)

        index = Math.Min(index, Count);
        if (ReferenceEquals(ctx.PlaceholderZone, this) && ctx.PlaceholderIndex == index) return null;

        if (!await ShowPlaceholderAsync(ctx, index)) return null;
        return MoveInfo(ctx, this, index);
    }

    /// <summary>
    /// Whether the drag moved on while this handler was awaiting the browser: the list was disposed in the
    /// meantime (a list that is gone must not become the drop target or the swap target again, or the item
    /// would be committed into a list nobody can see), or the drag was finished or abandoned.
    /// </summary>
    private bool Stale(BortableDragContext ctx) => _disposed || ctx.Finished;

    /// <summary>
    /// Whether the item currently being dragged (and, for a multi-drag, the rest of the
    /// selection) may be dropped into this list from another one.
    /// </summary>
    private bool CanAccept(BortableDragContext ctx)
    {
        if (Items is null || ctx.PullMode == BortablePull.None || !CanReceiveFrom(ctx.Source)) return false;

        // Every incoming item must be of this list's type and not already in it: Blazor
        // requires unique @key values, so a value-equal duplicate cannot be added.
        if (ctx.MultiItems is { } block && ctx.PullMode == BortablePull.Move)
        {
            foreach (var item in block)
                if (item is not TItem t || Items.Contains(t)) return false;
            return true;
        }

        return ctx.Item is TItem dragged && !Items.Contains(dragged);
    }

    private bool CanReceiveFrom(IBortableZone source)
    {
        if (Disabled || !Put || !IsMutable) return false;
        if (ReferenceEquals(source, this)) return true;
        if (Group is null || source.GroupName is null) return false;
        return string.Equals(Group, source.GroupName, StringComparison.Ordinal);
    }

    /// <summary>
    /// Shows (or moves) the drop placeholder in this zone at <paramref name="index"/>; <c>false</c> when the
    /// drag went stale while the positions were being captured.
    /// </summary>
    private async Task<bool> ShowPlaceholderAsync(BortableDragContext ctx, int index)
    {
        var previous = ctx.PlaceholderZone;
        var source = ctx.CurrentZone; // hides its original while the item is previewed elsewhere

        // Snapshot every list whose layout is about to change (FLIP).
        await CaptureFlipAsync();
        if (previous is not null && !ReferenceEquals(previous, this)) await previous.CaptureFlipAsync();
        if (previous is null) await source.CaptureFlipAsync();
        if (Stale(ctx)) return false;

        ClearSwapTarget(ctx);
        ctx.PlaceholderZone = this;
        ctx.PlaceholderIndex = index;

        if (previous is not null && !ReferenceEquals(previous, this))
        {
            previous.RequestFlipPlay();
            previous.Rerender();
        }
        if (previous is null)
        {
            source.RequestFlipPlay();
            source.Rerender();
        }
        RequestFlipPlay();
        StateHasChanged();
        return true;
    }

    /// <summary>Removes the placeholder from whichever zone is currently showing it.</summary>
    private static async Task ClearPlaceholderAsync(BortableDragContext ctx)
    {
        var zone = ctx.PlaceholderZone;
        if (zone is null) return;

        var source = ctx.CurrentZone; // its hidden original becomes visible again
        await zone.CaptureFlipAsync();
        if (!ReferenceEquals(source, zone)) await source.CaptureFlipAsync();

        ctx.PlaceholderZone = null;

        zone.RequestFlipPlay();
        zone.Rerender();
        if (!ReferenceEquals(source, zone))
        {
            source.RequestFlipPlay();
            source.Rerender();
        }
    }

    // ----- swap mode (Swap plugin) ---------------------------------------

    /// <summary>
    /// Tracks the item under the pointer as the swap target while dragging in swap
    /// mode. Nothing is moved until drop; only the highlight (<see cref="SwapClass"/>)
    /// follows the pointer. Works within this list and across lists in the group.
    /// </summary>
    private async Task UpdateSwapTargetAsync(BortableDragContext ctx, DragEventArgs e)
    {
        // A placeholder shown by a sorting list of the group no longer applies.
        await ClearPlaceholderAsync(ctx);

        if (Items is null) return;

        bool home = ReferenceEquals(ctx.CurrentZone, this);
        // Swapping moves the dragged item here and the target item to the source list,
        // so both directions must be allowed. Never let an item land inside its own subtree.
        bool allowed = home
            ? AllowSort
            : CanAccept(ctx) && EffectivePull != BortablePull.None && ctx.Source.CanReceiveFrom(this) && !await WouldNestAsync();

        object? target = null;
        if (allowed)
        {
            int index = await GetItemIndexAtAsync(e.ClientX, e.ClientY);
            if (index >= 0 && index < Items.Count)
            {
                var candidate = Items[index];
                bool valid = candidate is not null && (home
                    ? !Equals(candidate, ctx.Item)                                          // not the dragged item itself
                    : ctx.Source.Accepts(candidate) && ctx.Source.IndexOf(candidate) < 0);  // fits the source list
                if (valid) target = candidate;
            }
        }

        if (Stale(ctx)) return;
        SetSwapTarget(ctx, target is null ? null : this, target);
    }

    private void SetSwapTarget(BortableDragContext ctx, IBortableZone? zone, object? item)
    {
        if (ReferenceEquals(ctx.SwapTargetZone, zone) && Equals(ctx.SwapTargetItem, item)) return;

        var previous = ctx.SwapTargetZone;
        ctx.SwapTargetZone = zone;
        ctx.SwapTargetItem = item;

        if (previous is not null && !ReferenceEquals(previous, zone)) previous.Rerender();
        zone?.Rerender();
    }

    private static void ClearSwapTarget(BortableDragContext ctx)
    {
        var zone = ctx.SwapTargetZone;
        if (zone is null) return;
        ctx.SwapTargetZone = null;
        ctx.SwapTargetItem = null;
        zone.Rerender();
    }

    /// <summary>Trades the dragged item's slot with the highlighted swap target.</summary>
    private static async Task CommitSwapAsync(BortableDragContext ctx)
    {
        var fromZone = ctx.CurrentZone;
        var toZone = ctx.SwapTargetZone!;
        var dragged = ctx.Item;
        var target = ctx.SwapTargetItem!;

        int fromIndex = fromZone.IndexOf(dragged);
        int toIndex = toZone.IndexOf(target);
        if (fromIndex < 0 || toIndex < 0) return;

        await fromZone.CaptureFlipAsync();
        if (!ReferenceEquals(toZone, fromZone)) await toZone.CaptureFlipAsync();

        ctx.Touch(fromZone);
        ctx.Touch(toZone);
        fromZone.Replace(fromIndex, target);
        toZone.Replace(toIndex, dragged);
        ctx.CurrentZone = toZone;

        fromZone.RequestFlipPlay();
        toZone.RequestFlipPlay();
    }

    // ----- drop / end ----------------------------------------------------

    private Task OnDrop(DragEventArgs _) => CompleteDragAsync();

    // Fires on the source item whether or not the drop landed on a list (released
    // elsewhere, or cancelled with Escape). Whichever of drop/dragend comes first
    // completes the drag; the other one finds nothing left to do.
    private Task OnItemDragEnd(DragEventArgs _) => CompleteDragAsync();

    /// <summary>
    /// Ends the drag: commits the pending swap or placeholder (like SortableJS, an
    /// item previewed in another list stays there even when released outside a
    /// list), gathers a multi-drag selection, then raises the lifecycle events.
    /// </summary>
    private async Task CompleteDragAsync()
    {
        if (Service.Context is null) return;

        BortableMoveInfo? info = null;
        bool sameList = false, listChanged = false, sourceChanged = false;

        await Service.Gate.WaitAsync();
        try
        {
            var ctx = Service.Context;
            if (ctx is null || ctx.Finished) return;
            ctx.Finished = true;

            // Zones showing drag state now; committing may detach them from the context.
            var placeholderZone = ctx.PlaceholderZone;
            var swapZone = ctx.SwapTargetZone;
            try
            {
                IReadOnlyList<object>? moved = null;
                if (ctx.SwapTargetZone is not null && ctx.SwapTargetItem is not null)
                {
                    await CommitSwapAsync(ctx);
                }
                else
                {
                    if (ctx.PlaceholderZone is { } target && !ReferenceEquals(target, ctx.CurrentZone))
                        await target.CommitPlaceholderAsync(ctx);
                    if (ctx.MultiItems is not null)
                        moved = await CollectMultiAsync(ctx);
                }

                var to = ctx.CurrentZone;
                sameList = ReferenceEquals(to, ctx.Source);
                // Whether the drag itself changed the list, not whether the list differs from what it was at
                // dragstart: an edit the app made to its own list in the meantime is not the drag's update.
                listChanged = !sameList || ctx.Changed(to);
                sourceChanged = ctx.Changed(ctx.Source);
                info = new BortableMoveInfo
                {
                    Item = ctx.Item,
                    Items = moved,
                    OldIndex = ctx.OldIndex,
                    NewIndex = to.IndexOf(ctx.Item),
                    From = ctx.Source,
                    To = to
                };
            }
            finally
            {
                await EndAsync(ctx, placeholderZone, swapZone);
            }
        }
        finally
        {
            Service.Gate.Release();
        }

        // Raised once the drag is over (and outside the gate), so handlers see the final state.
        if (info is null) return;
        var from = info.From;
        if (sameList)
        {
            if (listChanged)
            {
                await from.RaiseEventAsync(BortableEventType.Update, info);
                await from.RaiseEventAsync(BortableEventType.Sort, info);
            }
        }
        else
        {
            if (sourceChanged) await from.RaiseEventAsync(BortableEventType.Remove, info);
            await info.To.RaiseEventAsync(BortableEventType.Add, info);
            if (sourceChanged) await from.RaiseEventAsync(BortableEventType.Sort, info);
            await info.To.RaiseEventAsync(BortableEventType.Sort, info);
        }
        await from.RaiseEventAsync(BortableEventType.End, info);
    }

    /// <summary>
    /// Gathers the rest of the multi-drag selection next to the primary item, which has
    /// already been positioned by the normal reorder / placeholder flow. The selected
    /// block is removed from wherever it sits and re-inserted contiguously, in its
    /// original order, at the primary's position in the destination list.
    /// </summary>
    private static async Task<IReadOnlyList<object>?> CollectMultiAsync(BortableDragContext ctx)
    {
        var block = ctx.MultiItems;
        ctx.MultiItems = null;
        if (block is null || block.Count <= 1) return null;

        var source = ctx.Source;
        var target = ctx.CurrentZone;
        bool sameList = ReferenceEquals(target, source);

        // Within a list the block is gathered only if the list may be sorted; across lists
        // only when moving (multi-drag in clone mode clones just the dragged item).
        if (sameList ? !source.AllowSort : ctx.PullMode != BortablePull.Move) return null;

        int primaryPos = target.IndexOf(ctx.Item);
        if (primaryPos < 0) return null;

        // Anchor: number of non-block items before the primary in the destination list.
        int anchor = 0;
        for (int i = 0; i < primaryPos; i++)
            if (!BlockContains(block, target.ItemAt(i))) anchor++;

        await source.CaptureFlipAsync();
        if (!sameList) await target.CaptureFlipAsync();

        ctx.Touch(source);
        ctx.Touch(target);

        // The primary sits in the destination list; the rest of the block in the source.
        foreach (var item in block)
        {
            var zone = !sameList && Equals(item, ctx.Item) ? target : source;
            int index = zone.IndexOf(item);
            if (index >= 0) zone.RemoveAt(index);
        }

        anchor = Math.Min(anchor, target.Count);
        for (int i = 0; i < block.Count; i++)
            target.Insert(anchor + i, block[i]);

        source.ClearSelection();
        source.RequestFlipPlay();
        target.RequestFlipPlay();
        return block;
    }

    private static bool BlockContains(List<object> block, object? item)
    {
        foreach (var b in block)
            if (Equals(b, item)) return true;
        return false;
    }

    /// <summary>Moves (or clones) the dragged item from its source list into this list at the placeholder index.</summary>
    private async Task CommitPlaceholderAsync(BortableDragContext ctx)
    {
        int index = ctx.PlaceholderIndex;
        ctx.PlaceholderZone = null;
        if (Items is null || !IsMutable) return;

        var source = ctx.CurrentZone;
        bool clone = ctx.PullMode == BortablePull.Clone;
        var moving = clone ? ctx.Source.CloneItem(ctx.OriginalItem) : ctx.Item;

        // Never insert an item of the wrong type or one that is already here (a
        // duplicate @key, e.g. a Clone factory returning an equal value): cancel instead.
        if (moving is not TItem item || Items.Contains(item)) return;

        int sourceIndex = -1;
        if (!clone)
        {
            sourceIndex = source.IndexOf(moving);
            if (sourceIndex < 0) return;
        }

        await source.CaptureFlipAsync();
        await CaptureFlipAsync();

        if (!clone)
        {
            ctx.Touch(source);
            source.RemoveAt(sourceIndex);
            source.RequestFlipPlay();
        }

        ctx.Touch(this);
        Items.Insert(Math.Clamp(index, 0, Items.Count), item);
        ctx.Item = moving;
        ctx.CurrentZone = this;
        RequestFlipPlay();
    }

    /// <summary>Clears the drag state and brings every zone the drag touched up to date.</summary>
    private async Task EndAsync(BortableDragContext ctx, params IBortableZone?[] alsoTouched)
    {
        var zones = new HashSet<IBortableZone>(ReferenceEqualityComparer.Instance) { ctx.Source, ctx.CurrentZone };
        if (ctx.PlaceholderZone is not null) zones.Add(ctx.PlaceholderZone);
        if (ctx.SwapTargetZone is not null) zones.Add(ctx.SwapTargetZone);
        foreach (var zone in alsoTouched)
            if (zone is not null) zones.Add(zone);
        zones.UnionWith(ctx.TouchedZones);

        ctx.PlaceholderZone = null;
        ctx.SwapTargetZone = null;
        ctx.SwapTargetItem = null;
        ctx.MultiItems = null;
        if (ReferenceEquals(Service.Context, ctx)) Service.Context = null;

        foreach (var zone in zones)
        {
            await zone.EndDragAsync();
            zone.Rerender();
        }
        // Only the lists the drag left different: an item dragged away and back again changed nothing.
        foreach (var zone in ctx.TouchedZones)
            if (ctx.Changed(zone)) await zone.NotifyItemsChangedAsync();
    }

    /// <summary>Discards a drag that never completed, restoring the zones it touched.</summary>
    private async Task AbandonAsync(BortableDragContext ctx)
    {
        ctx.Finished = true;
        await EndAsync(ctx);
    }

    private static BortableMoveInfo MoveInfo(BortableDragContext ctx, IBortableZone to, int newIndex) => new()
    {
        Item = ctx.Item,
        Items = ctx.MultiItems,
        OldIndex = ctx.OldIndex,
        NewIndex = newIndex,
        From = ctx.Source,
        To = to
    };

    // ----- JS interop ----------------------------------------------------

    private async Task<int> GetDropIndexAsync(double clientX, double clientY)
        => await InvokeJsAsync("dropIndex", -1, _listElement, clientX, clientY,
            SwapThreshold, InvertSwap, InvertedSwapThreshold, DirectionValue);

    private async Task<int> GetItemIndexAtAsync(double clientX, double clientY)
        => await InvokeJsAsync("itemIndexAt", -1, _listElement, clientX, clientY);

    /// <summary>
    /// True when this list must not receive the dragged item: it lives inside the DOM
    /// subtree of the item being dragged (receiving it here would nest the item inside
    /// itself), or inside the drop placeholder (a preview copy of the item).
    /// </summary>
    private async Task<bool> WouldNestAsync() => await InvokeJsAsync("wouldNest", false, _listElement);

    private async Task CaptureFlipAsync()
    {
        if (Animation > 0) await InvokeJsVoidAsync("capture", _listElement);
    }

    private void RequestFlipPlay()
    {
        if (Animation > 0) _playFlipOnRender = true;
    }

    // The annotation is the one IJSObjectReference.InvokeAsync puts on its result type; only int and bool flow through here.
    private async ValueTask<T> InvokeJsAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] T>(
        string identifier, T fallback, params object?[] args)
    {
        if (_disposed || _module is null) return fallback;
        try { return await _module.InvokeAsync<T>(identifier, args); }
        catch (JSDisconnectedException) { return fallback; }
        catch (ObjectDisposedException) { return fallback; }
        catch (TaskCanceledException) { return fallback; }
    }

    private async ValueTask InvokeJsVoidAsync(string identifier, params object?[] args)
    {
        if (_disposed || _module is null) return;
        try { await _module.InvokeVoidAsync(identifier, args); }
        catch (JSDisconnectedException) { }
        catch (ObjectDisposedException) { }
        catch (TaskCanceledException) { }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_disposed) return;

        if (firstRender)
        {
            try
            {
                _module = await JS.InvokeAsync<IJSObjectReference>("import", ModulePath);
            }
            catch (JSDisconnectedException) { return; }
            catch (TaskCanceledException) { return; }

            if (_disposed)
            {
                await DisposeModuleAsync();
                return;
            }
        }

        if (_module is null) return;

        var handle = string.IsNullOrEmpty(Handle) ? null : Handle;
        if (!string.Equals(handle, _appliedHandle, StringComparison.Ordinal))
        {
            _appliedHandle = handle;
            await InvokeJsVoidAsync("setHandle", _listElement, handle);
        }

        if (_playFlipOnRender)
        {
            _playFlipOnRender = false;
            await InvokeJsVoidAsync("play", _listElement, Animation);
        }
    }

    // ----- IBortableZone --------------------------------------------

    string IBortableZone.Id => Id;
    string? IBortableZone.GroupName => Group;
    bool IBortableZone.AllowSort => AllowSort;
    bool IBortableZone.CanReceiveFrom(IBortableZone source) => CanReceiveFrom(source);
    bool IBortableZone.Accepts(object item) => item is TItem;

    private int Count => Items?.Count ?? 0;
    int IBortableZone.Count => Count;

    int IBortableZone.IndexOf(object item) => Items is not null && item is TItem t ? Items.IndexOf(t) : -1;

    object? IBortableZone.ItemAt(int index) => Items![index];

    void IBortableZone.Insert(int index, object item) => Items!.Insert(index, (TItem)item);

    void IBortableZone.RemoveAt(int index) => Items!.RemoveAt(index);

    void IBortableZone.Replace(int index, object item) => Items![index] = (TItem)item;

    void IBortableZone.ClearSelection()
    {
        if (_selected.Count == 0) return;
        _selected.Clear();
        StateHasChanged();
    }

    object IBortableZone.CloneItem(object item)
    {
        if (Clone is not null) return Clone((TItem)item)!;
        if (item is ICloneable cloneable) return cloneable.Clone();
        return item; // value types / strings: safe to share
    }

    Task IBortableZone.CommitPlaceholderAsync(BortableDragContext ctx) => CommitPlaceholderAsync(ctx);

    Task IBortableZone.CaptureFlipAsync() => CaptureFlipAsync();

    void IBortableZone.RequestFlipPlay() => RequestFlipPlay();

    void IBortableZone.Rerender() => StateHasChanged();

    async Task IBortableZone.NotifyItemsChangedAsync()
    {
        StateHasChanged();
        if (ItemsChanged.HasDelegate && Items is not null)
            await ItemsChanged.InvokeAsync(Items);
    }

    async Task IBortableZone.EndDragAsync()
    {
        var items = Items;
        if (_selected.Count > 0 && items is not null)
            _selected.RemoveWhere(item => !items.Contains(item));

        // Drop a FLIP snapshot that no render is going to play.
        if (!_playFlipOnRender) await InvokeJsVoidAsync("clear", _listElement);
    }

    Task IBortableZone.RaiseEventAsync(BortableEventType type, BortableMoveInfo info)
        => RaiseEventAsync(type, info);

    private Task RaiseEventAsync(BortableEventType type, BortableMoveInfo info)
    {
        var callback = type switch
        {
            BortableEventType.Start => OnStart,
            BortableEventType.End => OnEnd,
            BortableEventType.Add => OnAdd,
            BortableEventType.Remove => OnRemove,
            BortableEventType.Update => OnUpdate,
            BortableEventType.Sort => OnSort,
            BortableEventType.Change => OnChange,
            _ => default
        };
        if (!callback.HasDelegate) return Task.CompletedTask;

        var item = (TItem)info.Item;
        var args = new BortableEventArgs<TItem>
        {
            Item = item,
            Items = info.Items is { } items ? items.Cast<TItem>().ToList() : [item],
            OldIndex = info.OldIndex,
            NewIndex = info.NewIndex,
            FromId = info.From.Id,
            ToId = info.To.Id,
            FromGroup = info.From.GroupName,
            ToGroup = info.To.GroupName
        };
        return callback.InvokeAsync(args);
    }

    // ----- disposal ------------------------------------------------------

    /// <summary>Ends any drag this list is part of and releases the JS module.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        if (Service.Context is { } ctx)
        {
            if (ReferenceEquals(ctx.Source, this) || ReferenceEquals(ctx.CurrentZone, this))
            {
                // The list the item is dragged from is gone: abandon the drag.
                Service.Context = null;
                ctx.Finished = true;
                ctx.PlaceholderZone?.Rerender();
                ctx.SwapTargetZone?.Rerender();
            }
            else
            {
                if (ReferenceEquals(ctx.PlaceholderZone, this))
                {
                    // The list previewing the drop is gone: the original, hidden while it was previewed here,
                    // has to show again in the list it is still dragged from - sliding back in, as it does when
                    // the pointer leaves a list.
                    ctx.PlaceholderZone = null;
                    await ctx.CurrentZone.CaptureFlipAsync();
                    ctx.CurrentZone.RequestFlipPlay();
                    ctx.CurrentZone.Rerender();
                }
                if (ReferenceEquals(ctx.SwapTargetZone, this))
                {
                    ctx.SwapTargetZone = null;
                    ctx.SwapTargetItem = null;
                }
            }
        }

        await DisposeModuleAsync();
    }

    private async ValueTask DisposeModuleAsync()
    {
        var module = _module;
        _module = null;
        if (module is null) return;
        try { await module.DisposeAsync(); }
        catch (JSDisconnectedException) { }
        catch (ObjectDisposedException) { }
        catch (TaskCanceledException) { }
    }
}
