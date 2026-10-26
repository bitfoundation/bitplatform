using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Bit.BlazorHeadlessUI;

/// <summary>Information about a completed reorder.</summary>
/// <typeparam name="TItem">The item type.</typeparam>
public sealed record BitBhReorderEventArgs<TItem>(TItem Item, int OldIndex, int NewIndex);

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
/// <para><b>Data:</b> <c>@bind-Items</c>, or pass a mutable <see cref="IList{T}"/> and it
/// is reordered in place; <see cref="OnReorder"/> reports each move.</para>
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
    private int _grabbedFrom = -1;
    private int _grabbedIndex = -1;
    private int _pointerDragIndex = -1;
    private bool _pointerIgnored;
    private int _handle;
    private bool _attachTried;
    private string? _sentOptions;
    private bool _syncing;
    private bool _disposed;
    private DotNetObjectReference<DragCallbacks>? _callbacksRef;
    private string? _focusAfterRender;

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

    protected override string DefaultTag => "ul";

    private IReadOnlyList<TItem> Display => _preview ?? (IReadOnlyList<TItem>)Items.ToList();

    internal string InstructionsId => $"{ComponentId}-instructions";

    private string Label(TItem item) => ItemLabel?.Invoke(item) ?? item?.ToString() ?? string.Empty;

    internal bool IsGrabbed(int index) => _grabbedIndex == index;

    internal bool IsDragging(int index) => _grabbedIndex == index || _pointerDragIndex == index;

    internal bool IsDisabled => Disabled;

    internal string HandleId(int index) => $"{ComponentId}-handle-{index}";

    internal string ContainerId => ComponentId;

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
                var options = Orientation.ToAttribute();
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

    private Dictionary<string, object?> RuntimeOptions() => new() { ["orientation"] = Orientation.ToAttribute() };

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
        builder.AddContent(63, Announcements.Instructions);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataValue(attrs, "orientation", Orientation.ToAttribute());
        SetDataFlag(attrs, "disabled", Disabled);
        SetDataFlag(attrs, "dragging", _grabbedIndex >= 0 || _pointerDragIndex >= 0);
        return attrs;
    }

    // ── Keyboard reordering ──────────────────────────────────────────────────

    internal async Task HandleKeyDownAsync(int index, KeyboardEventArgs e)
    {
        // A held Space / Enter repeats: the item would be dropped and picked up again and again.
        if (Disabled || (e.Repeat && e.Key is " " or "Enter")) return;
        var count = Display.Count;
        var next = Orientation == BitBhOrientation.Vertical ? "ArrowDown" : "ArrowRight";
        var prev = Orientation == BitBhOrientation.Vertical ? "ArrowUp" : "ArrowLeft";

        // While an item is grabbed every handle's keys drive the grab, not only the grabbed item's: handles are
        // positional, so right after a move the focused handle is the one at the item's old position until the
        // focus follows it - on Blazor Server a round trip later - and a key typed in between must not be lost.
        if (_grabbedIndex < 0)
        {
            if (e.Key is " " or "Enter")
            {
                _preview = Items.ToList();
                _grabbedFrom = _grabbedIndex = index;
                await AnnounceAsync(Announcements.PickedUp(Label(_preview[index]), index + 1, count));
                StateHasChanged();
            }
            return;
        }

        if (e.Key == next || e.Key == prev)
        {
            var target = Math.Clamp(_grabbedIndex + (e.Key == next ? 1 : -1), 0, count - 1);
            if (target == _grabbedIndex) return;
            var item = _preview![_grabbedIndex];
            _preview.RemoveAt(_grabbedIndex);
            _preview.Insert(target, item);
            _grabbedIndex = target;
            _focusAfterRender = HandleId(target);
            await AnnounceAsync(Announcements.Moved(Label(item), target + 1, count));
            StateHasChanged();
        }
        else if (e.Key is " " or "Enter")
        {
            var item = _preview![_grabbedIndex];
            var from = _grabbedFrom;
            var to = _grabbedIndex;
            ResetGrab();
            await CommitAsync(from, to);
            await AnnounceAsync(Announcements.Dropped(Label(item), to + 1, count));
        }
        else if (e.Key is "Escape" or "Tab")
        {
            await CancelGrabAsync();
        }
    }

    internal async Task CancelGrabAsync()
    {
        if (_grabbedIndex < 0) return;
        var from = _grabbedFrom;
        var item = Items[from];
        ResetGrab();
        _focusAfterRender = HandleId(from);
        await AnnounceAsync(Announcements.Cancelled(Label(item), from + 1));
        StateHasChanged();
    }

    private void ResetGrab()
    {
        _preview = null;
        _grabbedFrom = _grabbedIndex = -1;
    }

    // ── Pointer reordering (reported by the JS runtime) ──────────────────────

    /// <summary>
    /// The runtime's drag callbacks. A separate object, not the sortable: a reference to the component would
    /// root it for the runtime and make its JS-invokable methods part of its public API.
    /// </summary>
    private sealed class DragCallbacks(BitBhSortable<TItem> sortable)
    {
        /// <summary>A pointer drag started. The runtime calls it by this name.</summary>
        [JSInvokable]
        public Task OnPointerDragStart(int from) => sortable.HandlePointerDragStartAsync(from);

        /// <summary>A pointer drag ended. The runtime calls it by this name.</summary>
        [JSInvokable]
        public Task OnPointerDragEnd(int from, int to, bool cancelled) => sortable.HandlePointerDragEndAsync(from, to, cancelled);
    }

    internal async Task HandlePointerDragStartAsync(int from)
    {
        // The runtime counts positions in the page, which shows the keyboard grab's preview order, not Items:
        // a pointer drag during a keyboard grab would move another item than the one dragged. It is ignored.
        _pointerIgnored = _grabbedIndex >= 0;
        if (_disposed || _pointerIgnored || Disabled || from < 0 || from >= Items.Count) return;
        _pointerDragIndex = from;
        await AnnounceAsync(Announcements.PickedUp(Label(Items[from]), from + 1, Items.Count));
        StateHasChanged();
    }

    internal async Task HandlePointerDragEndAsync(int from, int to, bool cancelled)
    {
        _pointerDragIndex = -1;
        var ignored = _pointerIgnored || _grabbedIndex >= 0;
        _pointerIgnored = false;
        if (_disposed || ignored || Disabled || from < 0 || from >= Items.Count)
        {
            StateHasChanged();
            return;
        }
        var item = Items[from];
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

    private ValueTask AnnounceAsync(string message)
        => (GetService<IBitBhAnnouncer>() ?? new BitBhAnnouncer(Interop)).AnnounceAsync(message, BitBhPoliteness.Assertive);

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        // An attach in flight is undone by SyncAsync once it returns.
        if (_syncing is false) await DetachAsync();
        _callbacksRef?.Dispose();
        GC.SuppressFinalize(this);
    }
}
