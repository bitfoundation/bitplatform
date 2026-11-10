using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Bit.Bheadless;

/// <summary>Information about a completed reorder.</summary>
/// <typeparam name="TItem">The item type.</typeparam>
public sealed record BitBhReorderEventArgs<TItem>(TItem Item, int OldIndex, int NewIndex);

/// <summary>
/// Information about an item moved from one <see cref="BitBhSortable{TItem}"/> to another list of its
/// <see cref="BitBhSortable{TItem}.Group"/> - enough to move it in a model the lists are not bound to.
/// </summary>
/// <typeparam name="TItem">The item type.</typeparam>
/// <param name="Item">The item.</param>
/// <param name="Key">The item's key: <see cref="BitBhSortable{TItem}.ItemKey"/>, else the item itself.</param>
/// <param name="SourceList">The list it came from: its <see cref="BitBhSortable{TItem}.ListKey"/>, else its id.</param>
/// <param name="OldIndex">Its position in the source list.</param>
/// <param name="TargetList">The list it moved to: its <see cref="BitBhSortable{TItem}.ListKey"/>, else its id.</param>
/// <param name="NewIndex">Its position in the target list.</param>
public sealed record BitBhTransferEventArgs<TItem>(TItem Item, object? Key, object SourceList, int OldIndex, object TargetList, int NewIndex);

/// <summary>Customizable screen-reader announcements for <see cref="BitBhSortable{TItem}"/>.</summary>
public sealed class BitBhSortableAnnouncements
{
    /// <summary>Instructions referenced by every drag handle (<c>aria-describedby</c>).</summary>
    public string Instructions { get; set; } =
        "To pick up an item, press Space or Enter. While dragging, use the arrow keys to move it. Press Space or Enter again to drop it, or Escape to cancel.";

    /// <summary>(label, position, count) → message.</summary>
    public Func<string, int, int, string> PickedUp { get; set; } =
        (label, position, count) => $"Picked up {label}. Position {position} of {count}.";

    /// <summary>(label, position, count) → message.</summary>
    public Func<string, int, int, string> Moved { get; set; } =
        (label, position, count) => $"{label} moved to position {position} of {count}.";

    /// <summary>(label, position, count) → message.</summary>
    public Func<string, int, int, string> Dropped { get; set; } =
        (label, position, count) => $"Dropped {label} at position {position} of {count}.";

    /// <summary>(label, position) → message.</summary>
    public Func<string, int, string> Cancelled { get; set; } =
        (label, position) => $"Reordering cancelled. {label} returned to position {position}.";

    /// <summary>
    /// Instructions referenced by every drag handle of a list in a <see cref="BitBhSortable{TItem}.Group"/>, used
    /// instead of <see cref="Instructions"/>: they also say how to move an item to another list. Default:
    /// <see cref="BitBhStrings.SortableGroupInstructions"/>.
    /// </summary>
    public string? GroupInstructions { get; set; }

    /// <summary>
    /// (label, list, position, count) → message when a picked-up item moves to another list. Default:
    /// <see cref="BitBhStrings.SortableMovedToList"/>.
    /// </summary>
    public Func<string, string, int, int, string>? MovedToList { get; set; }

    /// <summary>
    /// (label, list, position, count) → message when an item is dropped in another list. Default:
    /// <see cref="BitBhStrings.SortableDroppedInList"/>.
    /// </summary>
    public Func<string, string, int, int, string>? DroppedInList { get; set; }

    /// <summary>
    /// (label, list, position) → message when a move that had taken an item to another list is cancelled. Default:
    /// <see cref="BitBhStrings.SortableReturnedToList"/>.
    /// </summary>
    public Func<string, string, int, string>? ReturnedToList { get; set; }
}

/// <summary>
/// A headless, accessible sortable list: reorder items by dragging with a pointer
/// (mouse, pen, touch) or entirely with the keyboard, with live screen-reader
/// announcements of every step.
///
/// <para><b>Pointer:</b> drag an item (or its <see cref="BitBhSortableHandle{TItem}"/>); siblings
/// shift out of the way in the browser and the final order is committed on drop.
/// Escape cancels.</para>
/// <para><b>Keyboard:</b> focus a handle, press Space / Enter to pick the item up,
/// arrow keys to move it, Space / Enter to drop, Escape to cancel.</para>
/// <para><b>Between lists:</b> lists with the same <see cref="Group"/> exchange items. Drag an item into another
/// list - an empty one too - or, once it is picked up with the keyboard, press the arrow keys across the list
/// (Left / Right in a vertical list, Up / Down in a horizontal one) to move it to the next or previous list of the
/// group, in document order. <see cref="AllowDragOut"/>, <see cref="AllowDropIn"/> and <see cref="CanAccept"/>
/// restrict the moves. While a pointer drags an item, the other lists that take it get <c>data-drop-target</c> and
/// the one under the pointer <c>data-drag-over</c>; a list without items has <c>data-empty</c> (give it a size, so
/// it can be dropped into).</para>
/// <para><b>Data:</b> <c>@bind-Items</c>, or pass a mutable <see cref="IList{T}"/> and it
/// is reordered in place; <see cref="OnReorder"/> reports each move within the list and
/// <see cref="OnTransfer"/> each item it receives from another list.</para>
///
/// <code>
/// &lt;BitBhSortable TItem="Task" @bind-Items="tasks" ItemKey="t =&gt; t.Id" ItemLabel="t =&gt; t.Title" Context="s"&gt;
///     &lt;BitBhSortableHandle TItem="Task" aria-label="@($"Reorder {s.Item.Title}")"&gt;⠿&lt;/BitBhSortableHandle&gt;
///     @s.Item.Title
/// &lt;/BitBhSortable&gt;
/// </code>
/// </summary>
/// <typeparam name="TItem">The item type.</typeparam>
public class BitBhSortable<TItem> : BitBhComponentBase, IAsyncDisposable
{
    private List<TItem>? _preview;
    private int _pointerDragIndex = -1;
    private bool _pointerIgnored;
    private int _handle;
    private bool _attachTried;
    private string? _sentOptions;
    private bool _syncing;
    private bool _disposed;
    private DotNetObjectReference<DragCallbacks>? _callbacksRef;
    private string? _focusAfterRender;
    private BitBhSortableGroup<TItem>? _group;

    /// <summary>The items. Use <c>@bind-Items</c>, or pass a mutable list to reorder it in place.</summary>
    [Parameter, EditorRequired]
    public IList<TItem> Items { get; set; } = [];

    /// <summary>Two-way binding callback for <see cref="Items"/> (receives a new list).</summary>
    [Parameter]
    public EventCallback<IList<TItem>> ItemsChanged { get; set; }

    /// <summary>Fires after an item moved.</summary>
    [Parameter]
    public EventCallback<BitBhReorderEventArgs<TItem>> OnReorder { get; set; }

    /// <summary>A stable key per item (used as <c>@key</c>). Defaults to the item itself.</summary>
    [Parameter]
    public Func<TItem, object>? ItemKey { get; set; }

    /// <summary>Text naming an item in announcements. Defaults to <c>ToString()</c>.</summary>
    [Parameter]
    public Func<TItem, string>? ItemLabel { get; set; }

    /// <summary>Renders an item's content; receives <see cref="BitBhSortableItemContext{TItem}"/>.</summary>
    [Parameter]
    public RenderFragment<BitBhSortableItemContext<TItem>>? ChildContent { get; set; }

    /// <summary>Vertical (default) or horizontal list (which arrow keys move).</summary>
    [Parameter]
    public BitBhOrientation Orientation { get; set; } = BitBhOrientation.Vertical;

    /// <summary>Disables reordering.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>The element rendered for each item. Default: <c>li</c> (with the default <c>ul</c> container).</summary>
    [Parameter]
    public string ItemTag { get; set; } = "li";

    /// <summary>Screen-reader messages (localize here).</summary>
    [Parameter]
    public BitBhSortableAnnouncements Announcements { get; set; } = new();

    /// <summary>
    /// The group of lists this one exchanges items with: every sortable with the same key (and item type) on the page.
    /// Without one, items stay in their list.
    /// </summary>
    [Parameter]
    public string? Group { get; set; }

    /// <summary>Whether items can be moved out of this list into the other lists of its <see cref="Group"/>. Default: true; false makes it accept-only.</summary>
    [Parameter]
    public bool AllowDragOut { get; set; } = true;

    /// <summary>Whether items of the other lists of its <see cref="Group"/> can be moved into this list. Default: true; false makes it give-only.</summary>
    [Parameter]
    public bool AllowDropIn { get; set; } = true;

    /// <summary>
    /// Decides whether this list takes an item from another list of its <see cref="Group"/>: (item, the source list's
    /// <see cref="ListKey"/>) → true to accept it. Lists that refuse it are not drop targets and the keyboard skips them.
    /// </summary>
    [Parameter]
    public Func<TItem, object, bool>? CanAccept { get; set; }

    /// <summary>Identifies this list in <see cref="BitBhTransferEventArgs{TItem}"/> and <see cref="CanAccept"/>. Defaults to its id.</summary>
    [Parameter]
    public object? ListKey { get; set; }

    /// <summary>
    /// Names this list in the announcements of moves between lists ("… moved to Done"). Defaults to its
    /// <c>aria-label</c>, else <see cref="BitBhStrings.SortableList"/>.
    /// </summary>
    [Parameter]
    public string? ListLabel { get; set; }

    /// <summary>
    /// Fires on the list that receives an item from another list of its <see cref="Group"/>, once the item moved
    /// (when both lists are bound or mutable; otherwise neither is changed - move it in your model here).
    /// </summary>
    [Parameter]
    public EventCallback<BitBhTransferEventArgs<TItem>> OnTransfer { get; set; }

    protected override string DefaultTag => "ul";

    // The keyboard grab's preview order, else Items itself: read on every render and keydown, so not copied.
    private IList<TItem> Display => _preview ?? Items;

    // The group's lists exchange items and the keyboard grab; a list without a Group has a group of its own.
    private BitBhSortableGroup<TItem> SortGroup => _group ??= BitBhSortableGroup<TItem>.Own(this);

    internal string InstructionsId => $"{ComponentId}-instructions";

    private string Label(TItem item) => ItemLabel?.Invoke(item) ?? item?.ToString() ?? string.Empty;

    internal bool IsGrabbed(int index) => SortGroup.Grab is { } grab && grab.Current == this && grab.Index == index;

    internal bool IsDragging(int index) => IsGrabbed(index) || _pointerDragIndex == index;

    // An item of the group is picked up with the keyboard: every handle's keys drive it.
    internal bool IsGrabActive => SortGroup.Grab is not null;

    internal bool IsDisabled => Disabled;

    internal string HandleId(int index) => $"{ComponentId}-handle-{index}";

    internal string ContainerId => ComponentId;

    private object ListId => ListKey ?? ComponentId;

    private string ListName => ListLabel ?? GetAdditionalAttribute("aria-label") ?? Strings.SortableList(SortGroup.Members.IndexOf(this) + 1);

    private bool CanGive => !Disabled && AllowDragOut;

    // Whether a pointer drag can take an item out of this list: the item then follows the pointer freely.
    private bool Grouped => SortGroup.IsShared && CanGive;

    // Whether this list takes item from source, another list of its group.
    internal bool Accepts(BitBhSortable<TItem> source, TItem item)
        => source != this && _disposed is false && Disabled is false && AllowDropIn && (CanAccept?.Invoke(item, source.ListId) ?? true)
           // An item whose key is already here would give two items one @key.
           && HasKey(KeyOf(item)) is false;

    private object? KeyOf(TItem item) => ItemKey?.Invoke(item) ?? item;

    private bool HasKey(object? key)
    {
        foreach (var item in Items)
            if (Equals(KeyOf(item), key)) return true;
        return false;
    }

    internal void ClearPreview() => _preview = null;

    internal void Rerender()
    {
        if (_disposed is false) StateHasChanged();
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (_disposed || (_group is not null && _group.Name == Group)) return;
        LeaveGroup();
        _group = Group is null ? BitBhSortableGroup<TItem>.Own(this) : BitBhSortableGroup<TItem>.Join(Interop, Group, this);
    }

    // A list that leaves its group mid-grab ends the grab when the item is in it or came from it: it would be stranded.
    private void LeaveGroup()
    {
        if (_group is not { } group) return;
        _group = null;
        if (group.Grab is { } grab && (grab.Origin == this || grab.Current == this))
        {
            group.EndGrab();
            if (grab.Origin != this) grab.Origin.Rerender();
            if (grab.Current != this) grab.Current.Rerender();
        }
        group.Leave(this);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await SyncAsync();

        if (_focusAfterRender is { } id)
        {
            _focusAfterRender = null;
            await Interop.FocusAsync(id);
        }
    }

    // Renders can arrive while the attach is in flight. One loop applies them in order, and a sortable disposed
    // meanwhile detaches what the in-flight call attached: the runtime would otherwise keep its listener and
    // the container for good.
    private async Task SyncAsync()
    {
        if (_syncing) return;
        _syncing = true;
        try
        {
            while (_disposed is false)
            {
                var options = $"{Orientation.ToAttribute()} {Grouped}";
                if (_handle == 0)
                {
                    // One attempt, as before: a 0 handle (no runtime, no element) isn't retried on every render.
                    if (_attachTried) break;
                    _attachTried = true;
                    _callbacksRef ??= DotNetObjectReference.Create(new DragCallbacks(this));
                    _handle = await Interop.SortableAttachAsync(_callbacksRef, ComponentId, RuntimeOptions());
                    _sentOptions = options;
                    continue;
                }
                if (options == _sentOptions) break;
                _sentOptions = options;
                await Interop.SortableUpdateAsync(_handle, RuntimeOptions());
            }
        }
        finally
        {
            _syncing = false;
        }
        if (_disposed) await DetachAsync();
    }

    private Dictionary<string, object?> RuntimeOptions() => new()
    {
        ["orientation"] = Orientation.ToAttribute(),
        ["grouped"] = Grouped,
    };

    private async Task DetachAsync()
    {
        if (_handle <= 0) return;
        var handle = _handle;
        _handle = 0;
        await Interop.SortableDetachAsync(handle);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);

        builder.OpenComponent<CascadingValue<BitBhSortable<TItem>>>(35);
        builder.AddComponentParameter(36, "Value", this);
        builder.AddComponentParameter(37, "IsFixed", true);
        builder.AddComponentParameter(38, "ChildContent", (RenderFragment)(inner =>
        {
            var items = Display;
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                inner.OpenComponent<BitBhSortableItem<TItem>>(40);
                inner.SetKey(ItemKey?.Invoke(item) ?? item);
                inner.AddComponentParameter(41, nameof(BitBhSortableItem<TItem>.Sortable), this);
                inner.AddComponentParameter(42, nameof(BitBhSortableItem<TItem>.Item), item);
                inner.AddComponentParameter(43, nameof(BitBhSortableItem<TItem>.Index), i);
                inner.AddComponentParameter(44, nameof(BitBhSortableItem<TItem>.Tag), ItemTag);
                inner.AddComponentParameter(45, nameof(BitBhSortableItem<TItem>.ChildContent), ChildContent);
                inner.CloseComponent();
            }
        }));
        builder.CloseComponent();
        builder.CloseElement();

        // Instructions referenced by every handle.
        builder.OpenElement(60, "div");
        builder.AddAttribute(61, "id", InstructionsId);
        builder.AddAttribute(62, "hidden", true);
        builder.AddContent(63, SortGroup.IsShared
            ? Announcements.GroupInstructions ?? Strings.SortableGroupInstructions(Orientation)
            : Announcements.Instructions);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataValue(attrs, "orientation", Orientation.ToAttribute());
        SetDataFlag(attrs, "disabled", Disabled);
        SetDataFlag(attrs, "dragging", (SortGroup.Grab is { } grab && grab.Current == this) || _pointerDragIndex >= 0);
        SetDataFlag(attrs, "empty", Display.Count == 0);
        return attrs;
    }

    // ── Keyboard reordering ──────────────────────────────────────────────────

    internal async Task HandleKeyDownAsync(int index, KeyboardEventArgs e)
    {
        if (Disabled)
        {
            // While an item of the group is picked up every handle keeps Tab from moving the focus (its keys end the
            // grab): on a handle of a disabled list it would hold the focus there for good. It ends the grab here too.
            if (e.Key == "Tab" && SortGroup.Grab is not null) await CancelGrabAsync();
            return;
        }
        // A held Space / Enter repeats: the item would be dropped and picked up again and again.
        if (e.Repeat && e.Key is " " or "Enter") return;

        // While an item is grabbed every handle's keys drive the grab, not only the grabbed item's - nor only those of
        // the list it is in: handles are positional, so right after a move the focused handle is the one at the item's
        // old position (or in the list it left) until the focus follows it - on Blazor Server a round trip later - and
        // a key typed in between must not be lost.
        if (SortGroup.Grab is { } grab)
        {
            await grab.Current.HandleGrabKeyAsync(grab, e);
            return;
        }

        if (e.Key is " " or "Enter" && index >= 0 && index < Items.Count)
        {
            _preview = Items.ToList();
            SortGroup.Grab = new BitBhSortableGrab<TItem>(this, index, _preview[index]);
            await AnnounceAsync(Announcements.PickedUp(Label(_preview[index]), index + 1, _preview.Count));
            // Every list of the group: their handles' keys now drive the grab.
            foreach (var list in SortGroup.Members.ToArray()) list.Rerender();
        }
    }

    // A key of the grab, on the list that shows the grabbed item.
    private async Task HandleGrabKeyAsync(BitBhSortableGrab<TItem> grab, KeyboardEventArgs e)
    {
        var preview = _preview ??= Items.ToList();
        var count = preview.Count;
        var next = Orientation == BitBhOrientation.Vertical ? "ArrowDown" : "ArrowRight";
        var prev = Orientation == BitBhOrientation.Vertical ? "ArrowUp" : "ArrowLeft";

        if (e.Key == next || e.Key == prev)
        {
            var target = Math.Clamp(grab.Index + (e.Key == next ? 1 : -1), 0, count - 1);
            if (target == grab.Index) return;
            var item = preview[grab.Index];
            preview.RemoveAt(grab.Index);
            preview.Insert(target, item);
            grab.Index = target;
            _focusAfterRender = HandleId(target);
            await AnnounceAsync(Announcements.Moved(Label(item), target + 1, count));
            StateHasChanged();
        }
        else if (SortGroup.IsShared && e.Key is "ArrowUp" or "ArrowDown" or "ArrowLeft" or "ArrowRight")
        {
            // The arrow keys across the list move the item to the next or previous list of the group.
            await MoveToListAsync(grab, forward: e.Key is "ArrowDown" or "ArrowRight");
        }
        else if (e.Key is " " or "Enter")
        {
            var origin = grab.Origin;
            var to = grab.Index;
            // What changed while the item was picked up: the list it came from (the item is no longer where it was), or
            // the rules (the list it came from keeps it, this one refuses it now). The move is cancelled, not made.
            var stillThere = grab.OriginIndex < origin.Items.Count && Equals(origin.KeyOf(origin.Items[grab.OriginIndex]), origin.KeyOf(grab.Item));
            if (stillThere is false || (origin != this && (origin.CanGive is false || Accepts(origin, grab.Item) is false)))
            {
                await CancelGrabAsync();
                return;
            }
            SortGroup.EndGrab();
            if (origin == this)
            {
                await CommitAsync(grab.OriginIndex, to);
                await AnnounceAsync(Announcements.Dropped(Label(grab.Item), to + 1, count));
            }
            else
            {
                await TransferAsync(origin, grab.OriginIndex, this, to);
                await AnnounceAsync(DroppedInListMessage(grab.Item, to + 1, count));
            }
        }
        else if (e.Key is "Escape" or "Tab")
        {
            await CancelGrabAsync();
        }
    }

    // Moves the grabbed item to the next (or previous) list of the group that takes it.
    private async Task MoveToListAsync(BitBhSortableGrab<TItem> grab, bool forward)
    {
        var group = SortGroup;
        var origin = grab.Origin;
        // The lists the item can be in: its own, and - unless it may not leave it - those that take it.
        var lists = group.Members.Where(m => m == origin || (origin.CanGive && m.Accepts(origin, grab.Item))).ToList();
        if (lists.Count < 2) return;

        // The page knows the lists' order (and the reading direction); without a browser, the order they joined in.
        var order = await Interop.SortableOrderAsync(ContainerId, [.. lists.Select(l => l.ContainerId)], horizontal: Orientation == BitBhOrientation.Vertical);

        // Keys typed meanwhile may have dropped, cancelled or moved the item: go on from where it is now.
        if (group.Grab != grab) return;
        var current = grab.Current;
        List<BitBhSortable<TItem>> ordered = order is null ? lists : [.. order.Select(id => lists.Find(l => l.ContainerId == id)).OfType<BitBhSortable<TItem>>()];
        if (ordered.Contains(current) is false) ordered = lists;
        var at = ordered.IndexOf(current) + (forward ? 1 : -1);
        if (at < 0 || at >= ordered.Count || ordered[at]._disposed) return;
        // The list may have left the group (another Group) while its order was asked for.
        if (group.Members.Contains(ordered[at]) is false) return;
        await current.MoveGrabToAsync(grab, ordered[at]);
    }

    // Takes the grabbed item out of this list's preview and puts it in target's, at the same position where it can.
    private async Task MoveGrabToAsync(BitBhSortableGrab<TItem> grab, BitBhSortable<TItem> target)
    {
        var item = grab.Item;
        // The list it came from keeps showing its items without it; any other shows its own again.
        if (this == grab.Origin) (_preview ??= Items.ToList()).RemoveAt(grab.Index);
        else _preview = null;

        var preview = target == grab.Origin && target._preview is { } own ? own : target.Items.ToList();
        var index = Math.Clamp(grab.Index, 0, preview.Count);
        preview.Insert(index, item);
        target._preview = preview;
        grab.Current = target;
        grab.Index = index;
        target._focusAfterRender = target.HandleId(index);
        await target.AnnounceAsync((target.Announcements.MovedToList ?? Strings.SortableMovedToList)(target.Label(item), target.ListName, index + 1, preview.Count));
        StateHasChanged();
        target.StateHasChanged();
    }

    internal async Task CancelGrabAsync()
    {
        if (SortGroup.Grab is not { } grab) return;
        var origin = grab.Origin;
        var current = grab.Current;
        var from = grab.OriginIndex;
        SortGroup.EndGrab();
        origin._focusAfterRender = origin.HandleId(from);
        await origin.AnnounceAsync(current == origin
            ? origin.Announcements.Cancelled(origin.Label(grab.Item), from + 1)
            : (origin.Announcements.ReturnedToList ?? Strings.SortableReturnedToList)(origin.Label(grab.Item), origin.ListName, from + 1));
        origin.StateHasChanged();
        if (current != origin) current.Rerender();
    }

    private string DroppedInListMessage(TItem item, int position, int count)
        => (Announcements.DroppedInList ?? Strings.SortableDroppedInList)(Label(item), ListName, position, count);

    // ── Pointer reordering (reported by the JS runtime) ──────────────────────

    /// <summary>
    /// The runtime's drag callbacks. A separate object, not the sortable: a reference to the component would
    /// root it for the runtime and make its JS-invokable methods part of its public API.
    /// </summary>
    private sealed class DragCallbacks(BitBhSortable<TItem> sortable)
    {
        /// <summary>
        /// A pointer drag started. Returns the ids of the other lists the item can be dropped into. The runtime
        /// calls it by this name.
        /// </summary>
        [JSInvokable]
        public async Task<string[]?> OnPointerDragStart(int from)
        {
            string[]? targets = null;
            await sortable.RunFromRuntimeAsync(async () => targets = await sortable.HandlePointerDragStartAsync(from));
            return targets;
        }

        /// <summary>A pointer drag ended, in this list or in <paramref name="target"/>. The runtime calls it by this name.</summary>
        [JSInvokable]
        public Task OnPointerDragEnd(int from, int to, bool cancelled, string? target)
            => sortable.RunFromRuntimeAsync(() => sortable.HandlePointerDragEndAsync(from, to, cancelled, target));
    }

    internal async Task<string[]?> HandlePointerDragStartAsync(int from)
    {
        // The runtime counts positions in the page, which shows the keyboard grab's preview order, not Items:
        // a pointer drag during a keyboard grab would move another item than the one dragged. It is ignored.
        _pointerIgnored = SortGroup.Grab is not null;
        if (_disposed || _pointerIgnored || Disabled || from < 0 || from >= Items.Count) return null;
        _pointerDragIndex = from;
        var item = Items[from];
        string[]? targets = Grouped ? [.. SortGroup.Members.Where(m => m.Accepts(this, item)).Select(m => m.ContainerId)] : null;
        await AnnounceAsync(Announcements.PickedUp(Label(item), from + 1, Items.Count));
        StateHasChanged();
        return targets;
    }

    internal async Task HandlePointerDragEndAsync(int from, int to, bool cancelled, string? target = null)
    {
        _pointerDragIndex = -1;
        var ignored = _pointerIgnored || SortGroup.Grab is not null;
        _pointerIgnored = false;
        if (_disposed || ignored || Disabled || from < 0 || from >= Items.Count)
        {
            StateHasChanged();
            return;
        }
        var item = Items[from];

        // Dropped in another list of the group - if it still takes the item.
        if (cancelled is false && target is not null && target != ContainerId)
        {
            if (SortGroup.Find(target) is { } list && CanGive && list.Accepts(this, item))
            {
                to = Math.Clamp(to, 0, list.Items.Count);
                var count = list.Items.Count + 1;
                await TransferAsync(this, from, list, to);
                await list.AnnounceAsync(list.DroppedInListMessage(item, to + 1, count));
                return;
            }
            cancelled = true;
        }

        to = Math.Clamp(to, 0, Items.Count - 1);
        if (cancelled || from == to)
        {
            await AnnounceAsync(Announcements.Cancelled(Label(item), from + 1));
            StateHasChanged();
            return;
        }
        await CommitAsync(from, to);
        await AnnounceAsync(Announcements.Dropped(Label(item), to + 1, Items.Count));
    }

    private async Task CommitAsync(int from, int to)
    {
        if (from == to)
        {
            StateHasChanged();
            return;
        }
        var item = Items[from];
        if (ItemsChanged.HasDelegate)
        {
            var list = Items.ToList();
            list.RemoveAt(from);
            list.Insert(to, item);
            await ItemsChanged.InvokeAsync(list);
        }
        else if (!Items.IsReadOnly)
        {
            Items.RemoveAt(from);
            Items.Insert(to, item);
        }
        await OnReorder.InvokeAsync(new BitBhReorderEventArgs<TItem>(item, from, to));
        _focusAfterRender ??= HandleId(to);
        StateHasChanged();
    }

    // Moves the item at from in source to to in target: through their bindings, else in place in mutable lists. The target
    // takes it first: if that fails (a throwing handler), the source still has it. Both lists or neither: changed in one
    // only, the item would be in both or in none - when either can't be changed here, OnTransfer is where it is moved.
    private static async Task TransferAsync(BitBhSortable<TItem> source, int from, BitBhSortable<TItem> target, int to)
    {
        var item = source.Items[from];
        // Where it lands: the event and the focus name that position.
        to = Math.Clamp(to, 0, target.Items.Count);
        if (source.CanChangeItems && target.CanChangeItems)
        {
            await target.ChangeItemsAsync(list => list.Insert(to, item));
            await source.ChangeItemsAsync(list => list.RemoveAt(from));
        }
        await target.OnTransfer.InvokeAsync(new BitBhTransferEventArgs<TItem>(
            item, target.ItemKey?.Invoke(item) ?? item, source.ListId, from, target.ListId, to));
        target._focusAfterRender ??= target.HandleId(to);
        source.Rerender();
        target.Rerender();
    }

    // Whether the list's items can be changed here: through its binding, else in place.
    private bool CanChangeItems => ItemsChanged.HasDelegate || !Items.IsReadOnly;

    private async Task ChangeItemsAsync(Action<IList<TItem>> change)
    {
        if (ItemsChanged.HasDelegate)
        {
            var list = Items.ToList();
            change(list);
            await ItemsChanged.InvokeAsync(list);
        }
        else if (!Items.IsReadOnly)
        {
            change(Items);
        }
    }

    private ValueTask AnnounceAsync(string message)
        => (GetService<IBitBhAnnouncer>() ?? new BitBhAnnouncer(Interop)).AnnounceAsync(message, BitBhPoliteness.Assertive);

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        LeaveGroup();
        // An attach in flight is undone by SyncAsync once it returns.
        if (_syncing is false) await DetachAsync();
        _callbacksRef?.Dispose();
        GC.SuppressFinalize(this);
    }
}
