using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Bit.Bortable;

/// <summary>
/// A Blazor-native drag-and-drop sortable list, inspired by SortableJS. Reorder items within a list or
/// drag them between lists that share a <see cref="Group"/>. The sorting engine runs in C#; the
/// <c>bit-bortable.js</c> module routes the input where the pointer is - it hit-tests the lists and calls
/// .NET only when the drop position changes - installs drag handles and plays the FLIP animations.
/// Needs <see cref="BitBortable.AddBitBortableServices(Microsoft.Extensions.DependencyInjection.IServiceCollection)"/>.
/// </summary>
/// <typeparam name="TItem">The type of the bound items.</typeparam>
public partial class Bortable<TItem> : IBortableZone, IHandleEvent, IAsyncDisposable
{
    private static readonly EqualityComparer<TItem> Comparer = EqualityComparer<TItem>.Default;

    // Items currently selected for multi-drag in this list.
    private readonly HashSet<TItem> _selected = new();

    // The key each item is rendered with (data-bortable-key), by which the browser layer names an item. Items are
    // compared the way @key compares them, so an item keeps its key for as long as it stays in the list.
    private readonly Dictionary<object, string> _keyOf = new(ItemComparer.Instance);
    private readonly Dictionary<string, TItem> _itemOf = new(StringComparer.Ordinal);

    private ElementReference _listElement;
    private IJSObjectReference? _module;
    private DotNetObjectReference<BortableBridge>? _bridge;
    // The options the browser layer last received; null until the list is registered with it.
    private BortableJsOptions? _appliedOptions;
    // The key of the item that is the list's tab stop (see ActiveKey).
    private string? _activeKey;
    // The multi-drag selection as last handed to SelectedItemsChanged: the same collection coming back as SelectedItems
    // is not a new selection. And whether a drag changed the selection since.
    private IReadOnlyCollection<TItem>? _publishedSelection;
    private bool _selectionChanged;
    private Anchor? _anchor;
    // How many times the page gave the list its parameters: what an isolated item template shows may have changed.
    private int _parametersVersion;
    private bool _defaultsApplied;
    private bool _playFlipOnRender;
    private bool _disposed;

    [Inject] private BortableService Service { get; set; } = default!;

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

    /// <summary>
    /// Renders each item's template in a component of its own, rendered again only when the item, the template or the
    /// list's parameters change - not on every step of a drag, where only the item wrappers move. For long lists with
    /// costly templates. Leave it off when a template shows something that changes as other items move during a
    /// drag (an item's position, say): it is brought up to date only when the drag ends. Default <c>false</c>.
    /// </summary>
    [Parameter] public bool IsolateItems { get; set; }

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
    /// Decides whether the dragged item may be placed at a position in this list, as the item reaches it: return
    /// <c>false</c> to refuse. Asked with the proposed move - <see cref="BortableEventArgs{TItem}.NewIndex"/> is the
    /// index the item would have here, <see cref="BortableEventArgs{TItem}.FromId"/> the list it comes from - for a
    /// reorder within this list and for an item coming in from another list alike, before anything is shown. A
    /// refused position keeps the item where it was, and the pointer shows that it cannot be dropped there. Keep it
    /// fast and free of side effects: it runs while the pointer moves. Mirrors the SortableJS <c>onMove</c> option.
    /// </summary>
    [Parameter] public Func<BortableEventArgs<TItem>, bool>? CanDrop { get; set; }

    /// <summary>
    /// The most items this list holds: once it holds this many, it takes no item from another list (a multi-drag
    /// counts every item it carries). Reordering within the list, and swapping, are not affected. <c>null</c>
    /// (the default) sets no limit.
    /// </summary>
    [Parameter] public int? MaxItems { get; set; }

    /// <summary>
    /// Asked once an item has been dropped into this list - from this list or another one - whether to keep the
    /// move, for a check that has to wait (a server call, a confirmation dialog). The drop shows at once; the lists
    /// stay as dropped while the task runs, and no other drag starts until it completes. Return <c>false</c> to put
    /// every list the drag changed back as it was: <c>ItemsChanged</c> is raised for them (the page may have rendered
    /// them as dropped meanwhile), <c>OnEnd</c> reports <see cref="BortableEventArgs{TItem}.Canceled"/>, and no
    /// <c>OnUpdate</c>, <c>OnAdd</c>, <c>OnRemove</c> or <c>OnSort</c> is raised. Return <c>true</c> to keep it: the
    /// events follow as usual. Do not change the lists while it runs: a refusal puts back what they held before the
    /// drag.
    /// </summary>
    [Parameter] public Func<BortableEventArgs<TItem>, Task<bool>>? ConfirmDrop { get; set; }

    /// <summary>
    /// When <c>true</c>, a drag that starts in this list and is released outside every list that takes it puts
    /// everything back where it was: a live reorder is undone, and an item previewed in another list returns home.
    /// The SortableJS <c>revertOnSpill</c> option. With the default <c>false</c> the drag keeps what it did, as in
    /// SortableJS: an item previewed in another list lands there wherever it is released. A drag cancelled with
    /// Escape where Bortable can tell (a keyboard drag, or a touch or pen drag) is always put back.
    /// </summary>
    [Parameter] public bool RevertOnSpill { get; set; }

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

    /// <summary>
    /// Predicate returning <c>true</c> for items that must not be dragged. The other items can still be dragged
    /// past a filtered item, which moves it along by one, so it does not pin an item to its index.
    /// </summary>
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

    /// <summary>
    /// The items selected for a multi-drag (<see cref="MultiDrag"/>), to read or set from the page: bind it with
    /// <c>@bind-SelectedItems</c>. Items that are not in the list, or that cannot be dragged, are left out.
    /// </summary>
    [Parameter] public IReadOnlyCollection<TItem>? SelectedItems { get; set; }

    /// <summary>
    /// Raised with the selection, in list order, when a click, Shift+click or Shift+Space changes it, or a drag
    /// spends or clears it (supports <c>@bind-SelectedItems</c>).
    /// </summary>
    [Parameter] public EventCallback<IReadOnlyCollection<TItem>> SelectedItemsChanged { get; set; }

    /// <summary>
    /// How long, in milliseconds, a mouse or pen press on an item must be held before it can be dragged; a pointer
    /// that moves further than <see cref="DelayTolerance"/> first is a click or a selection instead. Default <c>0</c>.
    /// The SortableJS <c>delay</c> option.
    /// </summary>
    [Parameter] public int Delay { get; set; }

    /// <summary>
    /// How long, in milliseconds, a finger must rest on an item before it drags it - a long press - so that a swipe
    /// over the list still scrolls the page. A finger that moves further than <see cref="DelayTolerance"/> first
    /// scrolls. Default <c>200</c>; set <c>0</c> with a <see cref="Handle"/> whose CSS has <c>touch-action: none</c>
    /// for a handle that drags at once.
    /// </summary>
    [Parameter] public int TouchDelay { get; set; } = 200;

    /// <summary>
    /// How far, in pixels, the pointer may move while <see cref="Delay"/> or <see cref="TouchDelay"/> runs before the
    /// press is taken for a scroll or a click instead of a drag. Default <c>5</c>. The SortableJS
    /// <c>touchStartThreshold</c> option.
    /// </summary>
    [Parameter] public int DelayTolerance { get; set; } = 5;

    /// <summary>
    /// When <c>true</c>, the mouse drags like a finger or a pen: with pointer events and a preview of the item that
    /// follows the pointer (<see cref="DragClass"/>), instead of the browser's own drag and its drag image - for a
    /// preview styled the app's way, or a web view whose native drag and drop does not work. A finger and a pen
    /// always drag this way. The SortableJS <c>forceFallback</c> option.
    /// </summary>
    [Parameter] public bool ForceFallback { get; set; }

    /// <summary>
    /// Class of the preview that follows a finger, a pen, or the mouse with <see cref="ForceFallback"/>: a copy of the
    /// item, placed above the page. Default <c>bit-bortable-drag</c>. The SortableJS <c>dragClass</c> option.
    /// </summary>
    [Parameter] public string DragClass { get; set; } = "bit-bortable-drag";

    /// <summary>
    /// Scrolls whatever the pointer is near the edge of - the list, a scrolling panel around it, the page - while a
    /// finger, a pen or a <see cref="ForceFallback"/> mouse drags an item, the innermost one that can still scroll
    /// that way first. A native mouse drag is scrolled by the browser itself. Default <c>true</c>. The SortableJS
    /// <c>scroll</c> option.
    /// </summary>
    [Parameter] public bool Scroll { get; set; } = true;

    /// <summary>How close, in pixels, the pointer must come to an edge to scroll (see <see cref="Scroll"/>). Default <c>40</c>.</summary>
    [Parameter] public int ScrollSensitivity { get; set; } = 40;

    /// <summary>The most pixels an edge scrolls per frame, reached at the very edge (see <see cref="Scroll"/>). Default <c>16</c>.</summary>
    [Parameter] public int ScrollSpeed { get; set; } = 16;

    /// <summary>
    /// Lets the items be picked up, moved and dropped from the keyboard: an item is focusable (the list is one tab
    /// stop, and the arrow keys move between its items), Space or Enter picks it up, the arrow keys move it - within
    /// the list, and into the next list of the group in that direction - Home and End move it to the ends, Space or
    /// Enter drops it and Escape puts it back. Every step is announced to screen readers. Default <c>true</c>.
    /// </summary>
    [Parameter] public bool Keyboard { get; set; } = true;

    /// <summary>The list's accessible name (<c>aria-label</c>), by which announcements also name it, e.g. "Done".</summary>
    [Parameter] public string? AriaLabel { get; set; }

    /// <summary>How announcements name an item, e.g. <c>card => card.Title</c>. Default: the item's <c>ToString()</c>.</summary>
    [Parameter] public Func<TItem, string>? ItemLabel { get; set; }

    /// <summary>
    /// The texts the list gives assistive technology - the keyboard instructions, how an item is described, and what
    /// is announced as items are picked up, moved, dropped or put back - to translate or reword them. Default
    /// <see cref="BortableMessages.Default"/>.
    /// </summary>
    [Parameter] public BortableMessages? Messages { get; set; }

    /// <summary>Any other attribute given to the list (<c>aria-describedby</c>, <c>data-*</c>, <c>title</c>...), rendered on its container.</summary>
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>Raised on this list when a drag starts in it.</summary>
    [Parameter] public EventCallback<BortableEventArgs<TItem>> OnStart { get; set; }

    /// <summary>
    /// Raised on the source list when a drag ends, whatever its outcome - also when it ends before it was
    /// released, because the source list left the page or the browser never reported its end.
    /// </summary>
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
    public override Task SetParametersAsync(ParameterView parameters)
    {
        // The app-wide defaults, for every parameter the page does not set itself: the parameters it does set are
        // assigned right after, over them. Only the first time: a parameter left out later keeps its value, as any
        // Blazor parameter does.
        if (!_defaultsApplied)
        {
            _defaultsApplied = true;
            Service.Options.ApplyTo(this);
        }
        return base.SetParametersAsync(parameters);
    }

    /// <inheritdoc />
    protected override void OnInitialized() => Service.Zones.Add(this);

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        _parametersVersion++;

        // A selection set by the page (@bind-SelectedItems) replaces the list's own.
        if (MultiDrag && SelectedItems is { } wanted && !ReferenceEquals(wanted, _publishedSelection))
        {
            _publishedSelection = wanted;
            _selected.Clear();
            foreach (var item in wanted)
                if (CanDrag(item)) _selected.Add(item);
        }

        // Forget selected items that are no longer in the (possibly replaced) list.
        if (_selected.Count > 0)
        {
            var items = Items;
            if (!MultiDrag || items is null) _selected.Clear();
            else _selected.RemoveWhere(item => !items.Contains(item));
        }
    }

    // The component decides itself when to re-render instead of re-rendering after every handled DOM event.
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
                // A keyboard drag moves the item itself, with no drag image: it is chosen, not a ghost of itself.
                dragState = ctx.Input == BortableInput.Keyboard ? ChosenClass : Css(GhostClass, ChosenClass);
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

    private bool KeyboardEnabled => Keyboard && !Disabled;

    private BortableMessages EffectiveMessages => Messages ?? BortableMessages.Default;

    /// <summary>The id of the hidden element holding the keyboard instructions every item refers to.</summary>
    private string InstructionsId => $"{Id}-instructions";

    /// <summary>
    /// The key of the item that is the list's tab stop: the one last focused, or the first. The other items take
    /// the focus from the arrow keys or a click, so a list is a single stop in the page's tab order.
    /// </summary>
    private string? ActiveKey()
    {
        var items = Items;
        if (!KeyboardEnabled || items is null || items.Count == 0) return null;
        if (_activeKey is not null && _itemOf.TryGetValue(_activeKey, out var active) && items.Contains(active)) return _activeKey;
        return KeyOf(items[0]);
    }

    private string LabelOf(TItem item) => (ItemLabel is not null ? ItemLabel(item) : item?.ToString()) ?? string.Empty;

    /// <summary>An item's template in a component of its own (see <see cref="IsolateItems"/>).</summary>
    private RenderFragment IsolatedItem(TItem item) => builder =>
    {
        builder.OpenComponent<BortableItemView<TItem>>(0);
        builder.AddComponentParameter(1, nameof(BortableItemView<TItem>.Item), item);
        builder.AddComponentParameter(2, nameof(BortableItemView<TItem>.Template), ItemTemplate);
        builder.AddComponentParameter(3, nameof(BortableItemView<TItem>.Version), _parametersVersion);
        builder.CloseComponent();
    };

    /// <summary>The key <paramref name="item"/> is rendered with, which the browser layer names it by.</summary>
    private string? KeyOf(TItem item)
    {
        if (item is null) return null;
        if (_keyOf.TryGetValue(item, out var key))
        {
            // The page may have replaced the item with an equal one (an entity compared by its id, say): a drag of
            // this key must move the instance the list holds now, never the one it first rendered.
            _itemOf[key] = item;
            return key;
        }
        key = Service.NextKey();
        _keyOf[item] = key;
        _itemOf[key] = item;
        return key;
    }

    /// <summary>Lets go of the keys of items that have left the list, once there are enough of them to matter.</summary>
    private void PruneKeys()
    {
        var items = Items;
        if (items is null || _keyOf.Count <= items.Count * 2 + 32) return;

        var keep = new Dictionary<object, string>(ItemComparer.Instance);
        foreach (var item in items)
            if (item is not null && _keyOf.TryGetValue(item, out var key)) keep[item] = key;

        _keyOf.Clear();
        _itemOf.Clear();
        foreach (var (item, key) in keep)
        {
            _keyOf[item] = key;
            _itemOf[key] = (TItem)item;
        }
    }

    // ----- multi-drag selection ------------------------------------------

    private EventCallback<MouseEventArgs> ItemClickCallback(TItem item)
        => MultiDrag ? EventCallback.Factory.Create<MouseEventArgs>(this, e => SelectAsync(item, e.ShiftKey)) : default;

    /// <summary>
    /// A click on an item in MultiDrag mode: toggles its selection, or with <paramref name="range"/> (Shift) selects
    /// every item from the one last clicked to this one.
    /// </summary>
    private async Task<bool> SelectAsync(TItem item, bool range)
    {
        var items = Items;
        if (!MultiDrag || items is null || !CanDrag(item) || items.IndexOf(item) is var at && at < 0) return false;

        int from = range && _anchor is { } anchor ? items.IndexOf(anchor.Item) : -1;
        if (from >= 0)
        {
            for (int i = Math.Min(from, at); i <= Math.Max(from, at); i++)
                if (CanDrag(items[i])) _selected.Add(items[i]);
        }
        else if (!_selected.Add(item))
        {
            _selected.Remove(item);
        }

        _anchor = new(item);
        StateHasChanged();
        await PublishSelectionAsync();
        return _selected.Contains(item);
    }

    /// <summary>Hands the selection to <see cref="SelectedItemsChanged"/>, in list order.</summary>
    private Task PublishSelectionAsync()
    {
        _selectionChanged = false;
        if (!SelectedItemsChanged.HasDelegate) return Task.CompletedTask;
        IReadOnlyCollection<TItem> snapshot = Items is { } items ? items.Where(_selected.Contains).ToList() : [];
        _publishedSelection = snapshot;
        return SelectedItemsChanged.InvokeAsync(snapshot);
    }

    /// <summary>The item a Shift-click selects a range from (boxed, as an item may be a default value type).</summary>
    private sealed record Anchor(TItem Item);

    // ----- the drag, as the browser layer reports it ---------------------

    /// <summary>
    /// Starts a drag of the item the browser layer names by <paramref name="key"/>. Answers the lists that take the
    /// drag, so the browser layer routes the pointer to them alone; <c>null</c> when the item cannot be dragged.
    /// </summary>
    private async Task<BortableStartResult?> StartAsync(string key, string input)
    {
        if (_disposed || Items is null || Service.Confirming > 0 || !_itemOf.TryGetValue(key, out var item) || !CanDrag(item)) return null;

        BortableMoveInfo? info = null;
        BortableDragOutcome? abandoned = null;
        string[]? lists = null;
        string? announce = null;
        bool clone = false;
        BortableDragContext? started = null;
        await Service.Gate.WaitAsync();
        try
        {
            // A drag whose end was never observed (e.g. its element was removed mid-drag): end it where it is.
            if (Service.Context is { } stale) abandoned = Finish(stale, commit: false);

            info = BeginDrag(item, InputOf(input), carrySelection: true);
            if (info is not null && Service.Context is { } ctx)
            {
                lists = [.. Service.Zones.Where(zone => zone.TakesDrag(ctx)).Select(zone => zone.Id)];
                announce = EffectiveMessages.PickedUp(Announcement(ctx, this, item!));
                clone = ctx.PullMode == BortablePull.Clone;
                started = ctx;
            }
        }
        finally
        {
            Service.Gate.Release();
        }

        // The abandoned drag is reported first, as it ended before this one started.
        if (abandoned is not null) await ReportAsync(abandoned);
        // Picking up an unselected item drops the selection.
        if (_selectionChanged) await PublishSelectionAsync();
        if (info is not null)
        {
            try
            {
                await RaiseEventAsync(BortableEventType.Start, info);
            }
            catch
            {
                // The browser is never told the drag started, so it would never end it: end it here, as cancelled.
                if (started is not null) await AbortAsync(started);
                throw;
            }
        }
        return lists is null ? null : new BortableStartResult { Lists = lists, Announce = announce, Clone = clone, Drag = started?.Id ?? 0 };
    }

    /// <summary>Ends <paramref name="ctx"/> as cancelled, if it is still the drag in progress, and reports it.</summary>
    private async Task AbortAsync(BortableDragContext ctx)
    {
        BortableDragOutcome? outcome = null;
        await Service.Gate.WaitAsync();
        try
        {
            if (ReferenceEquals(Service.Context, ctx) && !ctx.Finished) outcome = Cancel(ctx);
        }
        finally
        {
            Service.Gate.Release();
        }

        if (outcome is not null) await ReportAsync(outcome);
    }

    /// <summary>What an announcement says about <paramref name="item"/>, as it sits in <paramref name="zone"/> now.</summary>
    private static BortableAnnouncement Announcement(BortableDragContext ctx, IBortableZone zone, object item, string? target = null) => new()
    {
        Item = zone.LabelOf(item),
        Position = zone.IndexOf(item) + 1,
        Count = zone.Count,
        List = zone.Label,
        OldPosition = ctx.OldIndex + 1,
        FromList = ctx.Source.Label,
        Returned = ReferenceEquals(zone, ctx.Source) && zone.IndexOf(item) == ctx.OldIndex,
        Target = target
    };

    /// <summary>What to announce once a drag has ended as <paramref name="outcome"/> says.</summary>
    private static string EndAnnouncement(BortableDragOutcome outcome)
    {
        var info = outcome.Info;
        var messages = outcome.Context.Source.Messages;
        var about = Announcement(outcome.Context, info.To, info.Item);
        return info.Canceled ? messages.Canceled(about) : messages.Dropped(about);
    }

    // ----- the keyboard ---------------------------------------------------

    private static readonly BortableKeyResult NotDragging = new() { Dragging = false };

    /// <summary>
    /// A key pressed on the item rendered with <paramref name="key"/>, or during a keyboard drag, as the browser layer
    /// reports it: <c>pick</c> picks the item up, <c>to</c> moves it to <paramref name="index"/> of the list
    /// <paramref name="listId"/> (or of the list it is in) - the browser layer works out which item or list lies in the
    /// direction of the arrow key - and <c>drop</c> and <c>cancel</c> end the drag. A keyboard drag moves the item
    /// itself, into another list too: there is no native drag to keep alive. Answers what to announce and where the
    /// item is now, so the focus follows it.
    /// </summary>
    private async Task<BortableKeyResult> KeyAsync(string key, string action, string? listId, int index, int drag)
    {
        switch (action)
        {
            case "pick": return await PickAsync(key);
            case "select": return await KeySelectAsync(key);
            case "to": return await KeyMoveAsync(listId, index, drag);
            case "drop": return await KeyEndAsync(drop: true, drag);
            case "cancel": return await KeyEndAsync(drop: false, drag);
            default: return NotDragging;
        }
    }

    private async Task<BortableKeyResult> PickAsync(string key)
    {
        if (_disposed || !KeyboardEnabled || Items is null || !_itemOf.TryGetValue(key, out var item) || Items.IndexOf(item) < 0)
            return NotDragging;

        _activeKey = key;
        if (!CanDrag(item))
            return new BortableKeyResult { Announce = EffectiveMessages.Locked(new BortableAnnouncement { Item = LabelOf(item), Position = Items.IndexOf(item) + 1, Count = Items.Count, List = AriaLabel }) };

        var started = await StartAsync(key, "keyboard");
        if (started is null) return NotDragging;

        return new BortableKeyResult { Dragging = true, Lists = started.Lists, Announce = started.Announce, List = Id, Key = key, Drag = started.Drag };
    }

    /// <summary>Shift+Space on an item of a MultiDrag list: toggles its selection, and says so.</summary>
    private async Task<BortableKeyResult> KeySelectAsync(string key)
    {
        if (_disposed || !KeyboardEnabled || !MultiDrag || Items is null || Service.Context is not null || !_itemOf.TryGetValue(key, out var item))
            return NotDragging;

        _activeKey = key;
        var about = new BortableAnnouncement { Item = LabelOf(item), Position = Items.IndexOf(item) + 1, Count = Items.Count, List = AriaLabel };
        if (!CanDrag(item)) return new BortableKeyResult { Announce = EffectiveMessages.Locked(about) };

        bool selected = await SelectAsync(item, range: false);
        about = new BortableAnnouncement { Item = about.Item, Position = about.Position, Count = about.Count, List = about.List, Selected = _selected.Count };
        return new BortableKeyResult { Announce = selected ? EffectiveMessages.Selected(about) : EffectiveMessages.Deselected(about), List = Id, Key = key };
    }

    private async Task<BortableKeyResult> KeyMoveAsync(string? listId, int index, int drag)
    {
        BortableMoveInfo? change = null;
        BortableKeyResult result;
        await Service.Gate.WaitAsync();
        try
        {
            var ctx = Service.ContextOf(drag);
            if (ctx is null || ctx.Input != BortableInput.Keyboard) return NotDragging;

            var current = ctx.CurrentZone;
            var target = listId is null ? current : Service.Zones.FirstOrDefault(zone => zone.Id == listId) ?? current;
            var messages = ctx.Source.Messages;
            string? announce;

            if (target.IsSwap)
            {
                int outcome = target.ApplyOver(ctx, index, out change);
                announce = outcome == Refused
                    ? messages.Refused(Announcement(ctx, current, ctx.Item))
                    : messages.SwapTarget(Announcement(ctx, current, ctx.Item,
                        ctx.SwapTargetItem is { } partner ? ctx.SwapTargetZone!.LabelOf(partner) : null));
            }
            else
            {
                bool withinHome = ReferenceEquals(target, current) && ReferenceEquals(current, ctx.Source);
                int outcome = withinHome
                    ? current.ApplyOver(ctx, index > current.IndexOf(ctx.Item) ? index + 1 : index, out change)
                    : MoveByKey(ctx, target, index, out change);
                // A list that cannot be sorted keeps its order: said, as any other position refused.
                if (withinHome && outcome == Unchanged && !current.AllowSort) outcome = Refused;
                announce = outcome switch
                {
                    Changed => messages.Moved(Announcement(ctx, ctx.CurrentZone, ctx.Item)),
                    Refused => messages.Refused(Announcement(ctx, current, ctx.Item)),
                    _ => null
                };
            }

            var zone = ctx.CurrentZone;
            zone.Focus(zone.KeyOfItem(ctx.Item));
            result = new BortableKeyResult
            {
                Dragging = true,
                Announce = announce,
                List = zone.Id,
                Key = zone.KeyOfItem(ctx.Item),
                Target = ctx.SwapTargetItem is { } swapWith ? ctx.SwapTargetZone!.KeyOfItem(swapWith) : null
            };
        }
        finally
        {
            Service.Gate.Release();
        }

        if (change is not null) await change.To.RaiseEventAsync(BortableEventType.Change, change);
        return result;
    }

    /// <summary>
    /// Moves the dragged item to <paramref name="index"/> of <paramref name="target"/> in a keyboard drag: within a
    /// list the item has been carried into, or into another list - out of the list it is in, or, for a clone, as a
    /// copy, which is taken out again when it moves on. Back home it simply returns. Asked of the target's rules
    /// like any other position.
    /// </summary>
    private static int MoveByKey(BortableDragContext ctx, IBortableZone target, int index, out BortableMoveInfo? change)
    {
        change = null;
        var current = ctx.CurrentZone;
        var source = ctx.Source;
        bool home = ReferenceEquals(target, source);
        bool clone = ctx.PullMode == BortablePull.Clone;

        if (!home && !ReferenceEquals(target, current) && !target.TakesDrag(ctx)) return Refused;

        // The item that will sit in the target: the one carried, the original back home, or a copy on its way out.
        var item = home ? ctx.OriginalItem : clone && ReferenceEquals(current, source) ? null : ctx.Item;

        // Where it ends up. Within the list it is in, the browser layer names an item's index; into another list, a
        // gap among its items. Back home with a clone, that gap counts the original, which is still there and moves
        // too: past it, the original lands one place earlier. Back home to a list that cannot be sorted (Sort="false",
        // a read-only palette), the item returns to where it was picked up, as it does under a pointer, and a clone's
        // original stays where it is.
        bool revert = home && !target.AllowSort;
        int there = home && clone ? target.IndexOf(ctx.OriginalItem) : -1;
        if (revert) index = there >= 0 ? there : Math.Min(ctx.OldIndex, target.Count);
        else if (ReferenceEquals(target, current)) index = Math.Clamp(index, 0, target.Count - 1);
        else if (there >= 0) index = Math.Clamp(index > there ? index - 1 : index, 0, target.Count - 1);
        else index = Math.Clamp(index, 0, target.Count);
        if (ReferenceEquals(target, current) && target.IndexOf(ctx.Item) == index) return Unchanged;
        if (!revert && !target.AllowsDropAt(ctx, index)) return Refused;

        item ??= source.CloneItem(ctx.OriginalItem);
        if (!ReferenceEquals(item, ctx.Item) && !ReferenceEquals(target, current) && target.IndexOf(item) >= 0 && !home) return Refused; // a copy equal to an item there

        // Out of where it is (a clone's original never leaves its list).
        if (!(clone && ReferenceEquals(current, source)))
        {
            int at = current.IndexOf(ctx.Item);
            if (at >= 0)
            {
                ctx.Touch(current);
                current.RemoveAt(at);
            }
        }

        // Into the target: a clone's original is still at home, so coming back moves it there instead - unless it is
        // there already, which leaves the home list (possibly a read-only one) untouched.
        there = target.IndexOf(item);
        if (there != index)
        {
            ctx.Touch(target);
            if (there >= 0) target.RemoveAt(there);
            target.Insert(Math.Min(index, target.Count), item);
        }

        // A swap highlighted in a swap list on the way no longer applies.
        ClearSwapTarget(ctx);
        ctx.Item = item;
        ctx.CurrentZone = target;
        current.Rerender();
        target.Rerender();
        change = MoveInfo(ctx, target, target.IndexOf(item));
        return Changed;
    }

    private async Task<BortableKeyResult> KeyEndAsync(bool drop, int drag)
    {
        BortableDragOutcome outcome;
        await Service.Gate.WaitAsync();
        try
        {
            var ctx = Service.ContextOf(drag);
            if (ctx is null || ctx.Input != BortableInput.Keyboard) return NotDragging;
            outcome = drop ? Finish(ctx, commit: true) : Cancel(ctx);
        }
        finally
        {
            Service.Gate.Release();
        }

        outcome = await ConfirmAndReportAsync(outcome);
        var zone = outcome.Info.To;
        var key = zone.KeyOfItem(outcome.Info.Item);
        zone.Focus(key);
        return new BortableKeyResult { Announce = EndAnnouncement(outcome), List = zone.Id, Key = key };
    }

    private static BortableInput InputOf(string input) => input switch
    {
        "touch" => BortableInput.Touch,
        "pen" => BortableInput.Pen,
        "keyboard" => BortableInput.Keyboard,
        _ => BortableInput.Mouse
    };

    /// <summary>
    /// Records a drag of <paramref name="item"/> on the coordinator; <c>null</c> when the item is no longer in the
    /// list. With <paramref name="carrySelection"/> a selected item carries the multi-drag selection with it.
    /// </summary>
    private BortableMoveInfo? BeginDrag(TItem item, BortableInput input, bool carrySelection)
    {
        int oldIndex = Items?.IndexOf(item) ?? -1;
        if (oldIndex < 0) return null;

        // Multi-drag: dragging a selected item carries the whole selection; dragging an
        // unselected item drops the selection and drags only that item (like SortableJS).
        List<object>? block = null;
        if (MultiDrag && carrySelection)
        {
            if (_selected.Contains(item))
            {
                block = new List<object>();
                foreach (var it in Items!)
                    if (_selected.Contains(it)) block.Add(it!);
            }
            else if (_selected.Count > 0)
            {
                _selected.Clear();
                _selectionChanged = true;
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
            Id = Service.NextDragId(),
            Input = input,
            MultiItems = block is { Count: > 1 } ? block : null
        };
        Service.Context = ctx;
        StateHasChanged();
        return MoveInfo(ctx, this, oldIndex);
    }

    /// <summary>
    /// Whether this list takes the drag: the list the item is in always does (it sorts, or takes the item back),
    /// any other one when it can receive the item, or in swap mode trade an item of its own for it. The browser
    /// layer leaves a list that does not to the list around it, as SortableJS does, so a column of a board can be
    /// dragged over the card lists of the other columns.
    /// </summary>
    private bool HandlesDrag(BortableDragContext ctx)
    {
        if (ReferenceEquals(ctx.CurrentZone, this)) return true;

        // Swapping moves the dragged item here and the target item to the source list, so both directions must be allowed.
        return CanAccept(ctx) && (!Swap || (EffectivePull != BortablePull.None && ctx.Source.CanReceiveFrom(this)));
    }

    // What ApplyOver answers: a list changed, nothing changed, or this list refused the position.
    private const int Changed = 1, Unchanged = 0, Refused = -1;

    /// <summary>
    /// Positions the drag preview at drop position <paramref name="index"/>, which the browser layer measured where
    /// the pointer is. Within the home list it reorders the items live; over another list it shows a drop
    /// placeholder; in swap mode it highlights the item under the pointer (<c>-1</c>: none). Answers 1 when a list
    /// changed (the browser layer plays the slide animation), 0 when nothing did, -1 when this list refused it.
    /// </summary>
    private async Task<int> OverAsync(int index, int drag)
    {
        BortableMoveInfo? change;
        int result;
        await Service.Gate.WaitAsync();
        try
        {
            var ctx = Service.ContextOf(drag);
            if (ctx is null || _disposed || !HandlesDrag(ctx)) return Refused;
            result = ApplyOver(ctx, index, out change);
        }
        finally
        {
            Service.Gate.Release();
        }

        if (change is not null) await RaiseEventAsync(BortableEventType.Change, change);
        return result;
    }

    /// <summary>Positions the drag at drop position <paramref name="index"/> of this list, inside the gate (see <see cref="OverAsync"/>).</summary>
    private int ApplyOver(BortableDragContext ctx, int index, out BortableMoveInfo? change)
    {
        change = null;
        if (Swap) return UpdateSwapTarget(ctx, index) ? Changed : Unchanged;
        return ReferenceEquals(ctx.CurrentZone, this)
            ? ReorderWithin(ctx, index, out change)
            : PreviewFromOtherList(ctx, index, out change);
    }

    /// <summary>Reorders the home list as the dragged item moves over it.</summary>
    private int ReorderWithin(BortableDragContext ctx, int target, out BortableMoveInfo? change)
    {
        change = null;

        if (!AllowSort || Items is null || ctx.Item is not TItem dragged)
        {
            // Back home from a cross-list preview into a list that cannot be sorted: the
            // item returns to its original place (SortableJS "revert").
            bool reverted = ClearSwapTarget(ctx) | ClearPlaceholder(ctx);
            return reverted ? Changed : Unchanged;
        }

        // While the item is previewed in another list its original here is hidden; the
        // drop index is still expressed in this list's indices (hidden item included).
        if (target < 0) return ClearSwapTarget(ctx) ? Changed : Unchanged; // dead zone: keep the current order (and any preview)

        int from = Items.IndexOf(dragged);
        if (from < 0) return Unchanged;
        int insertAt = Math.Min(target > from ? target - 1 : target, Items.Count - 1);
        bool previewedElsewhere = ctx.PlaceholderZone is not null;
        if (insertAt == from && !previewedElsewhere) return ClearSwapTarget(ctx) ? Changed : Unchanged;

        // The app's own rule for this position, asked before anything shows.
        if (!AllowsDrop(ctx, insertAt)) return Refused;

        // A swap target highlighted by a swap-mode list of the group no longer applies.
        ClearSwapTarget(ctx);

        // Back home from a cross-list preview: take the item back, at the pointer's position.
        if (previewedElsewhere) ClearPlaceholder(ctx);

        if (insertAt != from)
        {
            ctx.Touch(this);
            Items.RemoveAt(from);
            Items.Insert(insertAt, dragged);
            change = MoveInfo(ctx, this, insertAt);
        }
        StateHasChanged();
        return Changed;
    }

    /// <summary>
    /// Previews where an item dragged in from another list would land. The actual
    /// item is not moved until the drag ends, so the native drag stays alive.
    /// </summary>
    private int PreviewFromOtherList(BortableDragContext ctx, int index, out BortableMoveInfo? change)
    {
        change = null;
        if (index < 0) return Unchanged; // dead zone: keep the current preview (if any)

        index = Math.Min(index, Count);
        if (ReferenceEquals(ctx.PlaceholderZone, this) && ctx.PlaceholderIndex == index) return Unchanged;
        if (!AllowsDrop(ctx, index)) return Refused;

        ShowPlaceholder(ctx, index);
        change = MoveInfo(ctx, this, index);
        return Changed;
    }

    /// <summary>Whether <see cref="CanDrop"/> lets the dragged item take index <paramref name="newIndex"/> of this list.</summary>
    private bool AllowsDrop(BortableDragContext ctx, int newIndex)
        => CanDrop is null || CanDrop(ArgsOf(MoveInfo(ctx, this, newIndex)));

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
            return HasRoomFor(block.Count);
        }

        return ctx.Item is TItem dragged && !Items.Contains(dragged) && HasRoomFor(1);
    }

    /// <summary>Whether <see cref="MaxItems"/> leaves room for <paramref name="incoming"/> more items. A swap brings one and takes one.</summary>
    private bool HasRoomFor(int incoming) => Swap || MaxItems is not { } max || Count + incoming <= max;

    private bool CanReceiveFrom(IBortableZone source)
    {
        if (Disabled || !Put || !IsMutable) return false;
        if (ReferenceEquals(source, this)) return true;
        if (Group is null || source.GroupName is null) return false;
        return string.Equals(Group, source.GroupName, StringComparison.Ordinal);
    }

    /// <summary>Shows (or moves) the drop placeholder in this zone at <paramref name="index"/>.</summary>
    private void ShowPlaceholder(BortableDragContext ctx, int index)
    {
        var previous = ctx.PlaceholderZone;
        var source = ctx.CurrentZone; // hides its original while the item is previewed elsewhere

        ClearSwapTarget(ctx);
        ctx.PlaceholderZone = this;
        ctx.PlaceholderIndex = index;

        if (previous is not null && !ReferenceEquals(previous, this)) previous.Rerender();
        if (previous is null) source.Rerender();
        StateHasChanged();
    }

    /// <summary>Removes the placeholder from whichever zone is currently showing it; <c>true</c> when there was one.</summary>
    private static bool ClearPlaceholder(BortableDragContext ctx)
    {
        var zone = ctx.PlaceholderZone;
        if (zone is null) return false;

        var source = ctx.CurrentZone; // its hidden original becomes visible again
        ctx.PlaceholderZone = null;

        zone.Rerender();
        if (!ReferenceEquals(source, zone)) source.Rerender();
        return true;
    }

    // ----- swap mode (Swap plugin) ---------------------------------------

    /// <summary>
    /// Tracks the item under the pointer (the item at <paramref name="index"/>, or none) as the swap target while
    /// dragging in swap mode. Nothing is moved until drop; only the highlight (<see cref="SwapClass"/>) follows the
    /// pointer. Works within this list and across lists in the group.
    /// </summary>
    private bool UpdateSwapTarget(BortableDragContext ctx, int index)
    {
        if (Items is null) return false;

        // A placeholder shown by a sorting list of the group no longer applies.
        bool changed = ClearPlaceholder(ctx);

        bool home = ReferenceEquals(ctx.CurrentZone, this);
        object? target = null;
        if ((!home || AllowSort) && index >= 0 && index < Items.Count)
        {
            var candidate = Items[index];
            bool valid = candidate is not null && (home
                ? !Equals(candidate, ctx.Item)                                          // not the dragged item itself
                : ctx.Source.Accepts(candidate) && ctx.Source.IndexOf(candidate) < 0);  // fits the source list
            if (valid) target = candidate;
        }

        return SetSwapTarget(ctx, target is null ? null : this, target) | changed;
    }

    private static bool SetSwapTarget(BortableDragContext ctx, IBortableZone? zone, object? item)
    {
        if (ReferenceEquals(ctx.SwapTargetZone, zone) && Equals(ctx.SwapTargetItem, item)) return false;

        var previous = ctx.SwapTargetZone;
        ctx.SwapTargetZone = zone;
        ctx.SwapTargetItem = item;

        if (previous is not null && !ReferenceEquals(previous, zone)) previous.Rerender();
        zone?.Rerender();
        return true;
    }

    private static bool ClearSwapTarget(BortableDragContext ctx)
    {
        var zone = ctx.SwapTargetZone;
        if (zone is null) return false;
        ctx.SwapTargetZone = null;
        ctx.SwapTargetItem = null;
        zone.Rerender();
        return true;
    }

    /// <summary>Trades the dragged item's slot with the highlighted swap target.</summary>
    private static void CommitSwap(BortableDragContext ctx)
    {
        var fromZone = ctx.CurrentZone;
        var toZone = ctx.SwapTargetZone!;
        var dragged = ctx.Item;
        var target = ctx.SwapTargetItem!;

        int fromIndex = fromZone.IndexOf(dragged);
        int toIndex = toZone.IndexOf(target);
        if (fromIndex < 0 || toIndex < 0) return;

        ctx.Touch(fromZone);
        ctx.Touch(toZone);
        fromZone.Replace(fromIndex, target);
        toZone.Replace(toIndex, dragged);
        ctx.CurrentZone = toZone;
    }

    // ----- drop / end ----------------------------------------------------

    /// <summary>
    /// Ends the drag as the browser layer reports it:
    /// <list type="bullet">
    /// <item><c>drop</c> - released over a list that takes it: the pending swap or placeholder is committed and a
    /// multi-drag selection gathered;</item>
    /// <item><c>release</c> - released anywhere else (for a native drag, also Escape, which the browser does not tell
    /// apart): the same, as in SortableJS, unless the source list has <see cref="RevertOnSpill"/>, which puts
    /// everything back;</item>
    /// <item><c>cancel</c> - cancelled (Escape in a keyboard, touch or pen drag, a pointer the system took over):
    /// everything is put back;</item>
    /// <item><c>abandon</c> - the browser lost track of the drag: it ends where it is, its pending preview dropped.</item>
    /// </list>
    /// Then the destination's <see cref="ConfirmDrop"/> is asked, and the lifecycle events are raised. Answers what to
    /// announce.
    /// </summary>
    private async Task<string?> EndAsync(string how, int drag)
    {
        if (Service.Context is null) return null;

        BortableDragOutcome? outcome = null;
        await Service.Gate.WaitAsync();
        try
        {
            if (Service.ContextOf(drag) is { } ctx)
            {
                bool putBack = how == "cancel" || (how == "release" && ctx.Source.RevertOnSpill);
                outcome = putBack ? Cancel(ctx) : Finish(ctx, commit: how != "abandon");
            }
        }
        finally
        {
            Service.Gate.Release();
        }

        if (outcome is null) return null;
        outcome = await ConfirmAndReportAsync(outcome);
        return how == "abandon" ? null : EndAnnouncement(outcome);
    }

    /// <summary>
    /// Ends <paramref name="ctx"/>, inside the gate. With <paramref name="commit"/> the pending swap or placeholder
    /// is committed and a multi-drag selection gathered; without it (a drag that ends before it was released, as
    /// its list is gone) whatever is pending is dropped and the lists keep what the drag already did to them.
    /// Returns what <see cref="ReportAsync"/> raises once the gate is released.
    /// </summary>
    private BortableDragOutcome Finish(BortableDragContext ctx, bool commit)
    {
        ctx.Finished = true;

        // Zones showing drag state now; committing may detach them from the context.
        var placeholderZone = ctx.PlaceholderZone;
        var swapZone = ctx.SwapTargetZone;
        IReadOnlyList<object>? moved = null;
        try
        {
            if (commit) moved = Commit(ctx);
        }
        finally
        {
            EndDrag(ctx, placeholderZone, swapZone);
        }

        return Outcome(ctx, moved, canceled: false);
    }

    /// <summary>
    /// Ends <paramref name="ctx"/> without keeping anything, inside the gate: the pending swap, placeholder and
    /// multi-drag gathering are dropped, and the item the drag moved goes back to where it was.
    /// </summary>
    private BortableDragOutcome Cancel(BortableDragContext ctx)
    {
        ctx.Finished = true;

        var placeholderZone = ctx.PlaceholderZone;
        var swapZone = ctx.SwapTargetZone;
        try
        {
            PutBack(ctx);
        }
        finally
        {
            EndDrag(ctx, placeholderZone, swapZone);
        }

        return Outcome(ctx, moved: null, canceled: true);
    }

    /// <summary>
    /// Undoes the moves the drag made, by the item rather than by the list as it was: an edit the app made to its
    /// own list in the meantime stays. Only the dragged item moves before the drop (within its list, or, in a
    /// keyboard drag, into another list); everything else is still pending and is simply never committed.
    /// </summary>
    private static void PutBack(BortableDragContext ctx)
    {
        var source = ctx.Source;
        var current = ctx.CurrentZone;
        var item = ctx.Item;

        if (!ReferenceEquals(current, source))
        {
            int at = current.IndexOf(item);
            if (at >= 0)
            {
                ctx.Touch(current);
                current.RemoveAt(at);
            }
            ctx.Item = ctx.OriginalItem;
            ctx.CurrentZone = source;
            item = ctx.OriginalItem;
        }

        int index = source.IndexOf(item);
        if (index == ctx.OldIndex) return;

        ctx.Touch(source);
        if (index >= 0) source.RemoveAt(index);
        source.Insert(Math.Min(ctx.OldIndex, source.Count), item);
    }

    /// <summary>What a finished drag reports: where the item ended up, and which lists it left different.</summary>
    private static BortableDragOutcome Outcome(BortableDragContext ctx, IReadOnlyList<object>? moved, bool canceled)
    {
        var to = ctx.CurrentZone;
        return new BortableDragOutcome
        {
            Info = new BortableMoveInfo
            {
                Item = ctx.Item,
                Items = moved,
                OldIndex = ctx.OldIndex,
                NewIndex = to.IndexOf(ctx.Item),
                From = ctx.Source,
                To = to,
                Input = ctx.Input,
                IsClone = IsCloneInto(ctx, to),
                Canceled = canceled
            },
            // Only the lists the drag left different: an item dragged away and back again changed nothing.
            ChangedZones = ctx.TouchedZones.Where(ctx.Changed).ToList(),
            // Whether the drag itself changed the list, not whether the list differs from what it was at
            // dragstart: an edit the app made to its own list in the meantime is not the drag's update.
            ListChanged = !ReferenceEquals(to, ctx.Source) || ctx.Changed(to),
            SourceChanged = ctx.Changed(ctx.Source),
            Context = ctx
        };
    }

    private static bool IsCloneInto(BortableDragContext ctx, IBortableZone to)
        => ctx.PullMode == BortablePull.Clone && !ReferenceEquals(to, ctx.Source);

    // ----- moves made by code --------------------------------------------

    /// <summary>
    /// Moves <paramref name="item"/> to <paramref name="newIndex"/> within this list, as a drag would: the list must
    /// allow it (<see cref="Sort"/>, <see cref="Disabled"/>, <see cref="CanDrop"/>), the destination's
    /// <see cref="ConfirmDrop"/> is asked, the items slide into place, and the drag's events follow, with
    /// <see cref="BortableEventArgs{TItem}.Input"/> set to <see cref="BortableInput.Api"/>. In swap mode the item
    /// trades places with the one at <paramref name="newIndex"/>. <see cref="Filter"/> locks an item against the
    /// user's drags only, and a multi-drag selection is not carried along.
    /// <para>
    /// This is the non-drag way to rearrange a list that an accessible app offers next to dragging: "move up",
    /// "move to top" buttons or an item menu.
    /// </para>
    /// </summary>
    /// <returns><c>true</c> when the item moved; <c>false</c> when it is not in this list, the index is out of range or
    /// the item is already there, a drag is in progress, or a rule refused the move.</returns>
    public Task<bool> MoveAsync(TItem item, int newIndex) => MoveCoreAsync(item, this, newIndex);

    /// <summary>
    /// Moves <paramref name="item"/> from this list into the list whose <see cref="Id"/> is <paramref name="listId"/>,
    /// at <paramref name="newIndex"/>, as a drag would: the lists must exchange items (<see cref="Group"/>,
    /// <see cref="Pull"/>, <see cref="Put"/>, <see cref="Disabled"/>, <see cref="MaxItems"/>, the item type), the destination's
    /// <see cref="CanDrop"/> and <see cref="ConfirmDrop"/> are asked, a <see cref="BortablePull.Clone"/> source puts
    /// a copy there, and the drag's events follow with <see cref="BortableEventArgs{TItem}.Input"/> set to
    /// <see cref="BortableInput.Api"/>. Into a swap-mode list the item trades places with the one at
    /// <paramref name="newIndex"/>.
    /// </summary>
    /// <returns><c>true</c> when the item moved; <c>false</c> when there is no such list, the item is not in this
    /// list, the index is out of range, a drag is in progress, or a rule refused the move.</returns>
    public Task<bool> MoveToAsync(TItem item, string listId, int newIndex)
    {
        ArgumentNullException.ThrowIfNull(listId);
        var target = Service.Zones.FirstOrDefault(zone => string.Equals(zone.Id, listId, StringComparison.Ordinal));
        return target is null ? Task.FromResult(false) : MoveCoreAsync(item, target, newIndex);
    }

    /// <summary>A whole drag in one call: picked up, put at <paramref name="newIndex"/> of <paramref name="target"/>, dropped.</summary>
    private async Task<bool> MoveCoreAsync(TItem item, IBortableZone target, int newIndex)
    {
        // A disabled list takes part in no drag, nor in a move made by code (its items cannot be dragged out).
        if (_disposed || Disabled || Items is null || Service.Context is not null || Service.Confirming > 0) return false;

        bool within = ReferenceEquals(target, this);
        int from = Items.IndexOf(item);
        if (from < 0 || newIndex < 0) return false;
        if (within ? newIndex >= Items.Count || newIndex == from : newIndex > (target.IsSwap ? target.Count - 1 : target.Count)) return false;

        // The lists the move may change, snapshotted for the slide.
        await CaptureFlipAsync();
        if (!within) await target.CaptureFlipAsync();

        BortableMoveInfo? start;
        BortableMoveInfo? change = null;
        BortableDragOutcome outcome;
        await Service.Gate.WaitAsync();
        try
        {
            if (Service.Context is not null || Service.Confirming > 0) return false;

            start = BeginDrag(item, BortableInput.Api, carrySelection: false);
            if (start is null) return false;
            var ctx = Service.Context!;

            int result = Refused;
            if (target.TakesDrag(ctx))
            {
                // The position as the browser layer measures it: within a sorting list, a gap counted with the item
                // still in place; in swap mode, the item to trade with.
                int position = !target.IsSwap && within && newIndex > from ? newIndex + 1 : newIndex;
                RequestFlipPlay();
                if (!within) target.RequestFlipPlay();
                result = target.ApplyOver(ctx, position, out change);
            }

            outcome = result == Changed ? Finish(ctx, commit: true) : Cancel(ctx);
        }
        finally
        {
            Service.Gate.Release();
        }

        // The move is already made: a handler of OnStart or OnChange that throws does not keep it from being confirmed
        // and reported - OnEnd still follows OnStart - and its exception is passed on after.
        Exception? failure = null;
        try
        {
            await RaiseEventAsync(BortableEventType.Start, start);
            if (change is not null) await change.To.RaiseEventAsync(BortableEventType.Change, change);
        }
        catch (Exception exception)
        {
            failure = exception;
        }
        outcome = await ConfirmAndReportAsync(outcome);
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw(failure);

        // A screen reader hears what a button or a menu just did, as it would for a keyboard drag.
        var info = outcome.Info;
        if (!info.Canceled) await InvokeJsVoidAsync("announce", EffectiveMessages.Moved(Announcement(outcome.Context, info.To, info.Item)));
        return !info.Canceled;
    }

    /// <summary>
    /// Asks the destination's <see cref="ConfirmDrop"/> whether to keep a drag that changed the lists, puts them back
    /// when it says no, then reports the outcome, which it returns. No other drag starts while it is asked.
    /// </summary>
    private async Task<BortableDragOutcome> ConfirmAndReportAsync(BortableDragOutcome outcome)
    {
        if (!outcome.Info.Canceled && outcome.ChangedZones.Count > 0)
        {
            // Raised until a refusal has put the lists back, so no drag starts on lists about to be restored.
            Service.Confirming++;
            try
            {
                bool keep;
                try
                {
                    keep = await outcome.Info.To.ConfirmAsync(outcome.Info);
                }
                catch
                {
                    // Put back and reported like a refusal - OnEnd still follows OnStart - before the exception goes on.
                    await ReportAsync(await RefuseAsync(outcome));
                    throw;
                }

                if (!keep) outcome = await RefuseAsync(outcome);
            }
            finally
            {
                Service.Confirming--;
            }
        }

        await ReportAsync(outcome);
        return outcome;
    }

    /// <summary>Puts every list a refused drag changed back to what it held before the drag, sliding the items back.</summary>
    private async Task<BortableDragOutcome> RefuseAsync(BortableDragOutcome outcome)
    {
        var ctx = outcome.Context;
        foreach (var zone in outcome.ChangedZones)
            await zone.CaptureFlipAsync();

        await Service.Gate.WaitAsync();
        try
        {
            foreach (var zone in outcome.ChangedZones)
            {
                ctx.Restore(zone);
                zone.RequestFlipPlay();
                zone.Rerender();
            }
        }
        finally
        {
            Service.Gate.Release();
        }

        var source = ctx.Source;
        return new BortableDragOutcome
        {
            Info = new BortableMoveInfo
            {
                Item = ctx.OriginalItem,
                OldIndex = ctx.OldIndex,
                NewIndex = source.IndexOf(ctx.OriginalItem),
                From = source,
                To = source,
                Input = ctx.Input,
                Canceled = true
            },
            // The bound lists held the drop while ConfirmDrop ran, and a page may have rendered them so: ItemsChanged
            // tells it they are back.
            ChangedZones = outcome.ChangedZones,
            Context = ctx
        };
    }

    /// <summary>Commits the pending swap or placeholder and gathers a multi-drag selection; returns the items a multi-drag moved.</summary>
    private static IReadOnlyList<object>? Commit(BortableDragContext ctx)
    {
        if (ctx.SwapTargetZone is not null && ctx.SwapTargetItem is not null)
        {
            CommitSwap(ctx);
            return null;
        }

        if (ctx.PlaceholderZone is { } target && !ReferenceEquals(target, ctx.CurrentZone))
            target.CommitPlaceholder(ctx);
        return ctx.MultiItems is not null ? CollectMulti(ctx) : null;
    }

    /// <summary>
    /// Notifies <c>ItemsChanged</c> of every list a finished drag changed, then raises its lifecycle events.
    /// Called outside the gate: the handlers see the final state, and a slow one cannot hold up the next drag.
    /// </summary>
    private static async Task ReportAsync(BortableDragOutcome outcome)
    {
        foreach (var zone in outcome.ChangedZones)
            await zone.NotifyItemsChangedAsync();

        // A multi-drag spends its selection; items that left a list leave its selection.
        var ctx = outcome.Context;
        foreach (var zone in new HashSet<IBortableZone>([ctx.Source, ctx.CurrentZone, .. ctx.TouchedZones], ReferenceEqualityComparer.Instance))
            await zone.PublishSelectionIfChangedAsync();

        var info = outcome.Info;
        var from = info.From;
        if (info.Canceled)
        {
            // Nothing was kept: the drag only ends.
        }
        else if (ReferenceEquals(from, info.To))
        {
            if (outcome.ListChanged)
            {
                await from.RaiseEventAsync(BortableEventType.Update, info);
                await from.RaiseEventAsync(BortableEventType.Sort, info);
            }
        }
        else
        {
            if (outcome.SourceChanged) await from.RaiseEventAsync(BortableEventType.Remove, info);
            await info.To.RaiseEventAsync(BortableEventType.Add, info);
            if (outcome.SourceChanged) await from.RaiseEventAsync(BortableEventType.Sort, info);
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
    private static IReadOnlyList<object>? CollectMulti(BortableDragContext ctx)
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
        return block;
    }

    private static bool BlockContains(List<object> block, object? item)
    {
        foreach (var b in block)
            if (Equals(b, item)) return true;
        return false;
    }

    /// <summary>Moves (or clones) the dragged item from its source list into this list at the placeholder index.</summary>
    private void CommitPlaceholder(BortableDragContext ctx)
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

        if (!clone)
        {
            ctx.Touch(source);
            source.RemoveAt(sourceIndex);
        }

        ctx.Touch(this);
        Items.Insert(Math.Clamp(index, 0, Items.Count), item);
        ctx.Item = moving;
        ctx.CurrentZone = this;
    }

    /// <summary>Clears the drag state and brings every zone the drag touched up to date.</summary>
    private void EndDrag(BortableDragContext ctx, params IBortableZone?[] alsoTouched)
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
            zone.EndDrag();
            zone.Rerender();
        }
    }

    private static BortableMoveInfo MoveInfo(BortableDragContext ctx, IBortableZone to, int newIndex) => new()
    {
        Item = ctx.Item,
        // The whole selection travels within its own list or with a move. A clone into another list copies the
        // dragged item alone, which is also the only one checked against that list's item type (see CanAccept).
        Items = ReferenceEquals(to, ctx.Source) || ctx.PullMode == BortablePull.Move ? ctx.MultiItems : null,
        OldIndex = ctx.OldIndex,
        NewIndex = newIndex,
        From = ctx.Source,
        To = to,
        Input = ctx.Input,
        IsClone = IsCloneInto(ctx, to)
    };

    // ----- JS interop ----------------------------------------------------

    private async Task CaptureFlipAsync()
    {
        if (Animation > 0) await InvokeJsVoidAsync("capture", _listElement);
    }

    private void RequestFlipPlay()
    {
        if (Animation > 0) _playFlipOnRender = true;
    }

    private BortableJsOptions JsOptions() => new()
    {
        Id = Id,
        Handle = string.IsNullOrEmpty(Handle) ? null : Handle,
        SwapThreshold = SwapThreshold,
        InvertSwap = InvertSwap,
        InvertedSwapThreshold = InvertedSwapThreshold,
        Direction = DirectionValue,
        Animation = Animation,
        Swap = Swap,
        MultiDrag = MultiDrag,
        Keyboard = KeyboardEnabled,
        Delay = Math.Max(0, Delay),
        TouchDelay = Math.Max(0, TouchDelay),
        DelayTolerance = Math.Max(0, DelayTolerance),
        ForceFallback = ForceFallback,
        DragClass = DragClass ?? string.Empty,
        Scroll = Scroll,
        ScrollSensitivity = Math.Max(0, ScrollSensitivity),
        ScrollSpeed = Math.Max(0, ScrollSpeed)
    };

    /// <summary>
    /// Registers the list with the browser layer after its first interactive render, and sends its options again
    /// whenever one of them changes. A registration the browser never received is tried again on the next render.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BortableJsOptions))]
    private async Task SyncBrowserLayerAsync()
    {
        var options = JsOptions();
        if (options == _appliedOptions) return;

        bool registered = _appliedOptions is not null;
        _appliedOptions = options;
        if (registered)
        {
            await InvokeJsVoidAsync("configure", _listElement, options);
            return;
        }

        _bridge ??= DotNetObjectReference.Create(new BortableBridge(this));
        if (!await InvokeJsVoidAsync("register", _listElement, _bridge, options) && ReferenceEquals(_appliedOptions, options))
            _appliedOptions = null;
    }

    /// <summary>Calls the module, ignoring a browser that is gone; <c>false</c> when the call did not reach it.</summary>
    private async ValueTask<bool> InvokeJsVoidAsync(string identifier, params object?[] args)
    {
        if (_disposed || _module is null) return false;
        try
        {
            await _module.InvokeVoidAsync(identifier, args);
            return true;
        }
        catch (JSDisconnectedException) { return false; }
        catch (ObjectDisposedException) { return false; }
        catch (TaskCanceledException) { return false; }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_disposed) return;

        if (firstRender)
        {
            // Every list of the scope shares the one import.
            _module = await Service.GetModuleAsync();
            if (_disposed) _module = null;
        }

        if (_module is null) return;

        await SyncBrowserLayerAsync();

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
    bool IBortableZone.RevertOnSpill => RevertOnSpill;
    bool IBortableZone.IsSwap => Swap;
    bool IBortableZone.CanReceiveFrom(IBortableZone source) => CanReceiveFrom(source);
    bool IBortableZone.Accepts(object item) => item is TItem;
    bool IBortableZone.TakesDrag(BortableDragContext ctx) => !_disposed && HandlesDrag(ctx);

    private int Count => Items?.Count ?? 0;
    int IBortableZone.Count => Count;

    int IBortableZone.IndexOf(object item) => Items is not null && item is TItem t ? Items.IndexOf(t) : -1;

    object? IBortableZone.ItemAt(int index) => Items![index];

    void IBortableZone.Insert(int index, object item) => Items!.Insert(index, (TItem)item);

    void IBortableZone.RemoveAt(int index) => Items!.RemoveAt(index);

    void IBortableZone.Replace(int index, object item) => Items![index] = (TItem)item;

    void IBortableZone.SetItems(object?[] items)
    {
        var list = Items!;
        list.Clear();
        foreach (var item in items) list.Add((TItem)item!);
    }

    int IBortableZone.ApplyOver(BortableDragContext ctx, int index, out BortableMoveInfo? change)
    {
        change = null;
        return _disposed || !HandlesDrag(ctx) ? Refused : ApplyOver(ctx, index, out change);
    }

    void IBortableZone.ClearSelection()
    {
        if (_selected.Count == 0) return;
        _selected.Clear();
        _selectionChanged = true;
        StateHasChanged();
    }

    Task IBortableZone.PublishSelectionIfChangedAsync() => _selectionChanged ? PublishSelectionAsync() : Task.CompletedTask;

    object IBortableZone.CloneItem(object item)
    {
        if (Clone is not null) return Clone((TItem)item)!;
        if (item is ICloneable cloneable) return cloneable.Clone();
        return item; // value types / strings: safe to share
    }

    void IBortableZone.CommitPlaceholder(BortableDragContext ctx) => CommitPlaceholder(ctx);

    Task IBortableZone.CaptureFlipAsync() => CaptureFlipAsync();

    void IBortableZone.RequestFlipPlay() => RequestFlipPlay();

    void IBortableZone.Rerender() => StateHasChanged();

    async Task IBortableZone.NotifyItemsChangedAsync()
    {
        StateHasChanged();
        if (ItemsChanged.HasDelegate && Items is not null)
            await ItemsChanged.InvokeAsync(Items);
    }

    void IBortableZone.EndDrag()
    {
        var items = Items;
        if (_selected.Count > 0 && items is not null && _selected.RemoveWhere(item => !items.Contains(item)) > 0)
            _selectionChanged = true;
    }

    Task IBortableZone.RaiseEventAsync(BortableEventType type, BortableMoveInfo info)
        => RaiseEventAsync(type, info);

    Task<BortableStartResult?> IBortableZone.StartAsync(string key, string input) => StartAsync(key, input);

    Task<int> IBortableZone.OverAsync(int index, int drag) => OverAsync(index, drag);

    Task<string?> IBortableZone.EndAsync(string how, int drag) => EndAsync(how, drag);

    Task<BortableKeyResult> IBortableZone.KeyAsync(string key, string action, string? list, int index, int drag) => KeyAsync(key, action, list, index, drag);

    string? IBortableZone.Label => AriaLabel;

    BortableMessages IBortableZone.Messages => EffectiveMessages;

    string IBortableZone.LabelOf(object item) => item is TItem t ? LabelOf(t) : item.ToString() ?? string.Empty;

    string? IBortableZone.KeyOfItem(object item) => item is TItem t ? KeyOf(t) : null;

    void IBortableZone.Focus(string? key)
    {
        if (key is null || key == _activeKey) return;
        _activeKey = key;
        StateHasChanged();
    }

    bool IBortableZone.AllowsDropAt(BortableDragContext ctx, int newIndex) => AllowsDrop(ctx, newIndex);

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

        return callback.InvokeAsync(ArgsOf(info));
    }

    private static BortableEventArgs<TItem> ArgsOf(BortableMoveInfo info)
    {
        var item = (TItem)info.Item;
        return new BortableEventArgs<TItem>
        {
            Item = item,
            Items = info.Items is { } items ? items.Cast<TItem>().ToList() : [item],
            OldIndex = info.OldIndex,
            NewIndex = info.NewIndex,
            FromId = info.From.Id,
            ToId = info.To.Id,
            FromGroup = info.From.GroupName,
            ToGroup = info.To.GroupName,
            Input = info.Input,
            IsClone = info.IsClone,
            Canceled = info.Canceled
        };
    }

    async Task<bool> IBortableZone.ConfirmAsync(BortableMoveInfo info)
        => ConfirmDrop is null || info.Item is not TItem || await ConfirmDrop(ArgsOf(info));

    // ----- disposal ------------------------------------------------------

    /// <summary>Ends any drag this list is the source of, leaves any other drag this list was part of, and unregisters it from the browser layer.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        Service.Zones.Remove(this);
        var module = _module;
        _module = null; // shared by every list of the scope; BortableService releases it

        await LeaveDragAsync();

        if (module is not null && _appliedOptions is not null)
        {
            try { await module.InvokeVoidAsync("unregister", _listElement); }
            catch (JSDisconnectedException) { }
            catch (ObjectDisposedException) { }
            catch (TaskCanceledException) { }
        }
        _bridge?.Dispose();
    }

    private async Task LeaveDragAsync()
    {
        if (Service.Context is not { } ctx) return;

        if (ReferenceEquals(ctx.Source, this) || ReferenceEquals(ctx.CurrentZone, this))
        {
            // The list the item is dragged from is gone: the drag ends where it is, its pending preview dropped.
            // What it already did stays and is reported like the end of any other drag - a live reorder changed
            // the bound list, which the app may save on ItemsChanged or OnUpdate, and OnEnd still follows OnStart.
            BortableDragOutcome? outcome = null;
            await Service.Gate.WaitAsync();
            try
            {
                if (ReferenceEquals(Service.Context, ctx) && !ctx.Finished) outcome = Finish(ctx, commit: false);
            }
            finally
            {
                Service.Gate.Release();
            }

            if (outcome is not null) await ReportAsync(outcome);
            return;
        }

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

    /// <summary>Compares boxed items the way <c>@key</c> compares the items themselves.</summary>
    private sealed class ItemComparer : IEqualityComparer<object>
    {
        public static readonly ItemComparer Instance = new();

        bool IEqualityComparer<object>.Equals(object? x, object? y)
            => x is TItem a && y is TItem b ? Comparer.Equals(a, b) : Equals(x, y);

        int IEqualityComparer<object>.GetHashCode(object obj)
            => obj is TItem item ? Comparer.GetHashCode(item!) : obj.GetHashCode();
    }
}
