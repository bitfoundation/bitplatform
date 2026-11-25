using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Bit.BlazorUI;

/// <summary>
/// BitSortable is a drag-and-drop sortable list, inspired by SortableJS: reorder the items of a list, or drag them
/// between lists that share a <see cref="Group"/> - with live reordering, slide animations, clone and move pull modes,
/// swap thresholds, drag handles, filters, nested lists, swap mode, multi-drag, and the keyboard.
/// </summary>
/// <remarks>
/// The sorting engine - which list takes an item, what a position means, every change to the bound lists and every
/// event - runs in C#, over the page's own <see cref="IList{T}"/>, changed in place. The script of the package does what
/// has to happen where the pointer is: it follows the browser's drag events, measures where the pointer is against the
/// items (the swap thresholds), calls .NET only when the drop position changes - so a drag stays smooth on Blazor Server
/// too - and plays the slide animations. Lists exchange items through a coordinator of their own per scope (per circuit
/// on Blazor Server), so nothing has to be registered for them.
/// </remarks>
/// <typeparam name="TItem">The type of the bound items.</typeparam>
public partial class BitSortable<TItem> : BitComponentBase, IBitSortableZone, IHandleEvent
{
    private static readonly EqualityComparer<TItem> Comparer = EqualityComparer<TItem>.Default;

    // How the list tells its items apart (see ItemKey): by the key ItemKey gives them, or as the items compare.
    private Identity _identity = Identity.Default;

    // Items currently selected for multi-drag in this list.
    private HashSet<TItem> _selected = new(Identity.Default);

    // The key each item is rendered with (data-bit-srt-key), by which the script names an item. Items are compared the
    // way @key compares them, so an item keeps its key for as long as it stays in the list.
    private Dictionary<object, string> _keyOf = new(new ItemComparer(Identity.Default));
    private readonly Dictionary<string, TItem> _itemOf = new(StringComparer.Ordinal);

    private DotNetObjectReference<BitSortable<TItem>>? _dotnetObj;
    // The options the script last received; null until the list is registered with it.
    private BitSortableJsOptions? _appliedOptions;
    // The key of the item that is the list's tab stop (see ActiveKey).
    private string? _activeKey;
    // The multi-drag selection as last handed to SelectedItemsChanged: the same collection coming back as SelectedItems
    // is not a new selection. And whether a drag changed the selection since.
    private IReadOnlyCollection<TItem>? _publishedSelection;
    private bool _selectionChanged;
    private Anchor? _anchor;
    // How many times the page gave the list its parameters: what an isolated item template shows may have changed.
    private int _parametersVersion;
    private bool _playFlipOnRender;
    // The list is registered with the script: the next render that changes an option sends it again.
    private bool _registering;



    [Inject] private IJSRuntime _js { get; set; } = default!;

    [Inject] private IServiceProvider _services { get; set; } = default!;



    /// <summary>
    /// Gets or sets the cascading parameters for the sortable component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple sortable components
    /// through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitSortableParams.ParamName)]
    public BitSortableParams? CascadingParameters { get; set; }



    /// <summary>
    /// The duration in milliseconds of the slide animation of the items moving to their new positions, mirroring the
    /// SortableJS <c>animation</c> option. It follows the reduced-motion preference the way every animation of the
    /// library does (see <see cref="BitComponentBase.ForceAnimation"/>). Set to <c>0</c> to turn the animations off.
    /// <br />
    /// The default value is <strong>150</strong>.
    /// </summary>
    [Parameter] public int Animation { get; set; } = 150;

    /// <summary>
    /// Decides whether the dragged item may be placed at a position in this list, as the item reaches it: return
    /// <c>false</c> to refuse. Asked with the proposed move - <see cref="BitSortableEventArgs{TItem}.NewIndex"/> is the
    /// index the item would have here, <see cref="BitSortableEventArgs{TItem}.FromId"/> the list it comes from - for a
    /// reorder within this list and for an item coming in from another list alike, before anything is shown. A
    /// refused position keeps the item where it was, and the pointer shows that it cannot be dropped there. In swap
    /// mode it is asked about the dragged item alone, with the index of the item it would trade places with; a swap
    /// across lists also asks the list the dragged item is in about the item that would come back to it. A multi-drag
    /// asks once more on the drop, with the index the item takes once the rest of the selection gathers next to it: a
    /// refusal there puts every selected item back and ends the drag as cancelled. One that throws refuses the position
    /// it was asked about, and the exception is passed on once that step of the drag is done: thrown to the caller of
    /// <see cref="MoveAsync"/> or <see cref="MoveToAsync"/>, and raised as this list's own during a drag the user makes,
    /// for the nearest <c>ErrorBoundary</c>. Keep it fast and free of side effects: it runs while the pointer moves.
    /// Mirrors the SortableJS <c>onMove</c> option.
    /// </summary>
    [Parameter] public Func<BitSortableEventArgs<TItem>, bool>? CanDrop { get; set; }

    /// <summary>
    /// Extra content rendered after the items, inside the list.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the sortable list.
    /// </summary>
    [Parameter, ResetClassBuilder] public BitSortableClassStyles? Classes { get; set; }

    /// <summary>
    /// The factory used to copy an item when <see cref="Pull"/> is <see cref="BitSortablePull.Clone"/>. Without one an
    /// item that is <see cref="ICloneable"/> is cloned, and any other one is shared, which is safe for values and strings.
    /// </summary>
    [Parameter] public Func<TItem, TItem>? Clone { get; set; }

    /// <summary>
    /// Asked once an item has been dropped into this list - from this list or another one - whether to keep the
    /// move, for a check that has to wait (a server call, a confirmation dialog). The drop shows at once; the lists
    /// stay as dropped while the task runs, and no other drag starts until it completes. Return <c>false</c> to put
    /// every list the drag changed back as it was: <c>ItemsChanged</c> is raised for them (the page may have rendered
    /// them as dropped meanwhile), <c>OnEnd</c> reports <see cref="BitSortableEventArgs{TItem}.Canceled"/>, and no
    /// <c>OnUpdate</c>, <c>OnAdd</c>, <c>OnRemove</c> or <c>OnSort</c> is raised. Return <c>true</c> to keep it: the
    /// events follow as usual. Do not change the lists while it runs: a refusal puts back what they held before the
    /// drag. One that throws refuses the drop, and the exception is passed on once the drag is reported, as one
    /// <see cref="CanDrop"/> throws is.
    /// </summary>
    [Parameter] public Func<BitSortableEventArgs<TItem>, Task<bool>>? ConfirmDrop { get; set; }

    /// <summary>
    /// How long, in milliseconds, a mouse or pen press on an item must be held before it can be dragged; a pointer
    /// that moves further than <see cref="DelayTolerance"/> first is a click or a selection instead. The SortableJS
    /// <c>delay</c> option.
    /// <br />
    /// The default value is <strong>0</strong>.
    /// </summary>
    [Parameter] public int Delay { get; set; }

    /// <summary>
    /// How far, in pixels, the pointer may move while <see cref="Delay"/> or <see cref="TouchDelay"/> runs before the
    /// press is taken for a scroll or a click instead of a drag. The SortableJS <c>touchStartThreshold</c> option.
    /// <br />
    /// The default value is <strong>5</strong>.
    /// </summary>
    [Parameter] public int DelayTolerance { get; set; } = 5;

    /// <summary>
    /// The axis to sort along. <see cref="BitSortableDirection.Auto"/> detects it from the layout; set it explicitly for
    /// grids. Mirrors the SortableJS <c>direction</c> option.
    /// <br />
    /// The default value is <strong><see cref="BitSortableDirection.Auto"/></strong>.
    /// </summary>
    [Parameter] public BitSortableDirection Direction { get; set; } = BitSortableDirection.Auto;

    /// <summary>
    /// The template of what the list shows while it has no items. It takes precedence over <see cref="EmptyText"/>.
    /// </summary>
    [Parameter] public RenderFragment? EmptyTemplate { get; set; }

    /// <summary>
    /// The text the list shows while it has no items.
    /// <br />
    /// The default value is <strong>null</strong>, which uses the NoItemsFound text of the strings in force
    /// (<see cref="BitStrings.NoItemsFound"/>).
    /// </summary>
    [Parameter] public string? EmptyText { get; set; }

    /// <summary>
    /// A predicate returning <c>true</c> for the items that must not be dragged. The other items can still be dragged
    /// past a filtered item, which moves it along by one, so it does not pin an item to its index.
    /// </summary>
    [Parameter] public Func<TItem, bool>? Filter { get; set; }

    /// <summary>
    /// When <c>true</c>, the mouse drags like a finger or a pen: with pointer events and a preview of the item that
    /// follows the pointer (<see cref="BitSortableClassStyles.Drag"/>), instead of the browser's own drag and its drag
    /// image - for a preview styled the app's way, or a web view whose native drag and drop does not work. A finger and
    /// a pen always drag this way. The SortableJS <c>forceFallback</c> option.
    /// </summary>
    [Parameter] public bool ForceFallback { get; set; }

    /// <summary>
    /// The name of the group of lists that can exchange items: lists with the same group name take each other's items.
    /// </summary>
    [Parameter] public string? Group { get; set; }

    /// <summary>
    /// The CSS selector of a drag handle within each item. With a handle only the handle starts a drag, and the rest of
    /// the item keeps its normal pointer behaviour (text selection, inputs, links).
    /// </summary>
    [Parameter, ResetClassBuilder] public string? Handle { get; set; }

    /// <summary>
    /// The fraction (0-1) of an item used for the inverted swap zone. Defaults to <see cref="SwapThreshold"/> when left
    /// at <c>0</c>. Mirrors the SortableJS <c>invertedSwapThreshold</c> option.
    /// </summary>
    [Parameter] public double InvertedSwapThreshold { get; set; }

    /// <summary>
    /// When <c>true</c> the swap zones sit at the item's edges instead of its centre, giving a "sort between items"
    /// feel. Mirrors the SortableJS <c>invertSwap</c> option.
    /// </summary>
    [Parameter] public bool InvertSwap { get; set; }

    /// <summary>
    /// Renders each item's template in a component of its own, rendered again only when the item, the template or the
    /// list's parameters change - not on every step of a drag, where only the item wrappers move. For long lists with
    /// costly templates. Leave it off when a template shows something that changes as other items move during a
    /// drag (an item's position, say): it is brought up to date only when the drag ends.
    /// </summary>
    [Parameter] public bool IsolateItems { get; set; }

    /// <summary>
    /// The CSS class of an item's wrapper, chosen per item, which follows the item from list to list.
    /// </summary>
    [Parameter] public Func<TItem, string?>? ItemClass { get; set; }

    /// <summary>
    /// What identifies an item, e.g. <c>card => card.Id</c>: the value its element is keyed by (<c>@key</c>), by which the
    /// list finds it, tells it from the other items and keeps it selected. Give it when the page may replace the items
    /// with new instances of the same data - a list loaded again from a server - so that each item keeps its element,
    /// its focus and its selection; or when items compared by reference stand for records with an id. The key must be
    /// unique within the list and must not change while the item is in it, and a <see cref="Clone"/> factory has to
    /// give a copy a key of its own: a copy whose key is already in the target list is refused as a duplicate.
    /// A lambda written inline is fine: the list tells a new ItemKey by its code, not by the delegate instance a render
    /// creates, so the same lambda given again - capturing other values or not - is taken to key the items the same way.
    /// Without it an item is identified by itself, compared by its <c>Equals</c>.
    /// </summary>
    [Parameter] public Func<TItem, object>? ItemKey { get; set; }

    /// <summary>
    /// How the announcements name an item, e.g. <c>card => card.Title</c>. Without it an item is named by its
    /// <c>ToString()</c>.
    /// </summary>
    [Parameter] public Func<TItem, string>? ItemLabel { get; set; }

    /// <summary>
    /// The list of items to display and reorder. Bind with <c>@bind-Items</c>. It must be a mutable list (e.g.
    /// <see cref="List{T}"/>), changed in place; a read-only one (such as an array) can only be dragged from in
    /// <see cref="BitSortablePull.Clone"/> mode.
    /// </summary>
    [Parameter] public IList<TItem>? Items { get; set; }

    /// <summary>
    /// The callback raised with the list each time a drag changed it (supports <c>@bind-Items</c>). The list is the
    /// one <see cref="Items"/> holds, changed in place.
    /// </summary>
    [Parameter] public EventCallback<IList<TItem>> ItemsChanged { get; set; }

    /// <summary>
    /// The inline style of an item's wrapper, chosen per item, which follows the item from list to list.
    /// </summary>
    [Parameter] public Func<TItem, string?>? ItemStyle { get; set; }

    /// <summary>
    /// The template rendered for each item.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>
    /// Lets the items be picked up, moved and dropped from the keyboard: an item is focusable (the list is one tab
    /// stop, and the arrow keys move between its items), Space or Enter picks it up, the arrow keys move it - within
    /// the list, and into the next list of the group in that direction - Home and End move it to the ends, Space or
    /// Enter drops it and Escape puts it back. Every step is announced to screen readers.
    /// <br />
    /// The default value is <strong>true</strong>.
    /// </summary>
    [Parameter] public bool Keyboard { get; set; } = true;

    /// <summary>
    /// The most items this list holds: once it holds this many, it takes no item from another list (a multi-drag
    /// counts every item it carries). Reordering within the list, and swapping, are not affected. <c>null</c>
    /// (the default) sets no limit.
    /// </summary>
    [Parameter] public int? MaxItems { get; set; }

    /// <summary>
    /// The fewest items this list keeps: an item is not moved out to another list when that would leave fewer (a
    /// multi-drag counts every item it carries away). Reordering within the list, cloning out of it and swapping are not
    /// affected, and neither is a change the page makes to the list itself. <c>null</c> (the default) sets no limit.
    /// </summary>
    [Parameter] public int? MinItems { get; set; }

    /// <summary>
    /// Enables the MultiDrag plugin behaviour: click items to select several, then drag any one of them to move the
    /// whole selection together. Mirrors the SortableJS <c>multiDrag</c> option.
    /// </summary>
    [Parameter] public bool MultiDrag { get; set; }

    /// <summary>
    /// The callback raised on the destination list when an item is dropped in from another list.
    /// </summary>
    [Parameter] public EventCallback<BitSortableEventArgs<TItem>> OnAdd { get; set; }

    /// <summary>
    /// The callback raised while dragging, each time the dragged item moves to a new position in this list.
    /// </summary>
    [Parameter] public EventCallback<BitSortableEventArgs<TItem>> OnChange { get; set; }

    /// <summary>
    /// The callback raised on the source list when a drag ends, whatever its outcome - also when it ends before it was
    /// released, because the source list left the page or the browser never reported its end.
    /// </summary>
    [Parameter] public EventCallback<BitSortableEventArgs<TItem>> OnEnd { get; set; }

    /// <summary>
    /// The callback raised on the source list when an item is moved out to another list.
    /// </summary>
    [Parameter] public EventCallback<BitSortableEventArgs<TItem>> OnRemove { get; set; }

    /// <summary>
    /// The callback raised on every list whose items changed (add, remove or update).
    /// </summary>
    [Parameter] public EventCallback<BitSortableEventArgs<TItem>> OnSort { get; set; }

    /// <summary>
    /// The callback raised on this list when a drag starts in it.
    /// </summary>
    [Parameter] public EventCallback<BitSortableEventArgs<TItem>> OnStart { get; set; }

    /// <summary>
    /// The callback raised when the order of this list changed within the list.
    /// </summary>
    [Parameter] public EventCallback<BitSortableEventArgs<TItem>> OnUpdate { get; set; }

    /// <summary>
    /// Controls how items may be dragged out of this list, mirroring the SortableJS <c>group.pull</c> option.
    /// <br />
    /// The default value is <strong><see cref="BitSortablePull.Move"/></strong>.
    /// </summary>
    [Parameter] public BitSortablePull Pull { get; set; } = BitSortablePull.Move;

    /// <summary>
    /// Whether items from other lists in the group may be dropped here.
    /// <br />
    /// The default value is <strong>true</strong>.
    /// </summary>
    [Parameter] public bool Put { get; set; } = true;

    /// <summary>
    /// Other groups whose items may be dropped into this list, besides its own <see cref="Group"/>: a trash or an archive
    /// list that takes the cards of several boards, say. It lets items in only - this list's own items still go only to
    /// the lists of its group, or to lists that name its group here - and <see cref="Put"/>, <see cref="MaxItems"/>,
    /// <see cref="CanDrop"/> and the item type apply as for any other drop. The SortableJS <c>group.put</c> option
    /// given a list of group names.
    /// </summary>
    [Parameter] public IReadOnlyCollection<string>? PutGroups { get; set; }

    /// <summary>
    /// When <c>true</c>, a drag that starts in this list and is released outside every list that takes it puts
    /// everything back where it was: a live reorder is undone, and an item previewed in another list returns home.
    /// The SortableJS <c>revertOnSpill</c> option. With the default <c>false</c> the drag keeps what it did, as in
    /// SortableJS: an item previewed in another list lands there wherever it is released. A drag cancelled with
    /// Escape where the list can tell (a keyboard drag, or a touch or pen drag) is always put back.
    /// </summary>
    [Parameter] public bool RevertOnSpill { get; set; }

    /// <summary>
    /// Scrolls whatever the pointer is near the edge of - the list, a scrolling panel around it, the page - while a
    /// finger, a pen or a <see cref="ForceFallback"/> mouse drags an item, the innermost one that can still scroll
    /// that way first. A native mouse drag is scrolled by the browser itself. The SortableJS <c>scroll</c> option.
    /// <br />
    /// The default value is <strong>true</strong>.
    /// </summary>
    [Parameter] public bool Scroll { get; set; } = true;

    /// <summary>
    /// How close, in pixels, the pointer must come to an edge to scroll (see <see cref="Scroll"/>).
    /// <br />
    /// The default value is <strong>40</strong>.
    /// </summary>
    [Parameter] public int ScrollSensitivity { get; set; } = 40;

    /// <summary>
    /// The most pixels an edge scrolls per frame, reached at the very edge (see <see cref="Scroll"/>).
    /// <br />
    /// The default value is <strong>16</strong>.
    /// </summary>
    [Parameter] public int ScrollSpeed { get; set; } = 16;

    /// <summary>
    /// The items selected for a multi-drag (<see cref="MultiDrag"/>), to read or set from the page: bind it with
    /// <c>@bind-SelectedItems</c>. Items that are not in the list, or that cannot be dragged, are left out.
    /// </summary>
    [Parameter] public IReadOnlyCollection<TItem>? SelectedItems { get; set; }

    /// <summary>
    /// The callback raised with the selection, in list order, when a click, Shift+click or Shift+Space changes it, or a
    /// drag spends or clears it (supports <c>@bind-SelectedItems</c>).
    /// </summary>
    [Parameter] public EventCallback<IReadOnlyCollection<TItem>> SelectedItemsChanged { get; set; }

    /// <summary>
    /// Allows reordering within this list. Set to <c>false</c> to only allow dragging out.
    /// <br />
    /// The default value is <strong>true</strong>.
    /// </summary>
    [Parameter] public bool Sort { get; set; } = true;

    /// <summary>
    /// The texts the list gives assistive technology - the keyboard instructions, how an item is described, and what
    /// is announced as items are picked up, moved, dropped or put back - to translate or reword them.
    /// <br />
    /// The default value is <strong>null</strong>, which uses the English texts of a new <see cref="BitSortableStrings"/>.
    /// </summary>
    [Parameter] public BitSortableStrings? Strings { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the sortable list.
    /// </summary>
    [Parameter, ResetStyleBuilder] public BitSortableClassStyles? Styles { get; set; }

    /// <summary>
    /// Enables the Swap plugin behaviour: instead of sorting, the dragged item trades places with the item it is
    /// dropped on. While dragging, the item under the pointer is highlighted (<see cref="BitSortableClassStyles.SwapTarget"/>).
    /// Mirrors the SortableJS <c>swap</c> option.
    /// </summary>
    [Parameter] public bool Swap { get; set; }

    /// <summary>
    /// The fraction (0-1) of an item that acts as its swap zone, mirroring the SortableJS <c>swapThreshold</c> option.
    /// With the default <c>1</c> the whole item swaps; a smaller value (e.g. <c>0.65</c>) leaves a dead zone near each
    /// edge, so the pointer has to move further into an item before the dragged one swaps past it.
    /// <br />
    /// The default value is <strong>1</strong>.
    /// </summary>
    [Parameter] public double SwapThreshold { get; set; } = 1;

    /// <summary>
    /// How long, in milliseconds, a finger must rest on an item before it drags it - a long press - so that a swipe
    /// over the list still scrolls the page. A finger that moves further than <see cref="DelayTolerance"/> first
    /// scrolls. Set <c>0</c> with a <see cref="Handle"/> whose CSS has <c>touch-action: none</c> for a handle that drags
    /// at once.
    /// <br />
    /// The default value is <strong>200</strong>.
    /// </summary>
    [Parameter] public int TouchDelay { get; set; } = 200;



    // ----- lifecycle ------------------------------------------------------

    protected override string RootElementClass => "bit-srt";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Handle.HasValue() ? "bit-srt-hnd" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override void OnInitialized()
    {
        Service.Zones.Add(this);

        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        _parametersVersion++;

        // A new ItemKey tells the items apart differently: the sets that hold them are built again with it. One that is
        // the same code as the last - a lambda written inline, which is a new delegate on every render of the page -
        // keys the items the same way, and only takes the old one's place.
        if (ItemKey is { } itemKey && _identity.Key is { } current && itemKey.Method == current.Method)
        {
            _identity.Key = itemKey;
        }
        else if (!ReferenceEquals(ItemKey, _identity.Key))
        {
            _identity = ItemKey is null ? Identity.Default : new Identity(ItemKey);
            _selected = new HashSet<TItem>(_selected, _identity);
            var keys = new Dictionary<object, string>(new ItemComparer(_identity));
            foreach (var (item, key) in _keyOf) keys[item] = key;
            _keyOf = keys;
        }

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
            else _selected.IntersectWith(new HashSet<TItem>(items, _identity));
        }

        base.OnParametersSet();
    }

    // The component decides itself when to re-render instead of re-rendering after every handled DOM event.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);



    // ----- rendering helpers ---------------------------------------------

    // The coordinator of the scope, shared by every list rendered in it.
    private BitSortableCoordinator? _service;
    private BitSortableCoordinator Service => _service ??= BitSortableCoordinator.Of(_services);

    private string ItemCssClass(TItem item, bool selected)
    {
        var ctx = Service.Context;
        string? dragState = null;
        if (ctx is not null && ReferenceEquals(ctx.CurrentZone, this) && ctx.Item is TItem dragged && _identity.Equals(dragged, item))
        {
            if (ctx.PlaceholderZone is null || ReferenceEquals(ctx.PlaceholderZone, this))
                // A keyboard drag moves the item itself, with no drag image: it is chosen, not a ghost of itself.
                dragState = ctx.Input == BitSortableInput.Keyboard ? ChosenClasses : Css(GhostClasses, ChosenClasses);
            else if (ctx.PullMode != BitSortablePull.Clone)
                // Previewed as the placeholder in another list: hide the original here, as SortableJS does. For a
                // clone, the original stays put and visible.
                dragState = "bit-srt-hid";
        }

        return Css("bit-srt-itm", Classes?.Item, ItemClass?.Invoke(item), dragState,
            IsSwapTarget(ctx, item) ? Css("bit-srt-swp", Classes?.SwapTarget) : null,
            selected ? Css("bit-srt-sel", Classes?.Selected) : null,
            IsFiltered(item) ? Css("bit-srt-flt", Classes?.Filtered) : null);
    }

    private string? ItemCssStyle(TItem item, bool selected)
    {
        var ctx = Service.Context;
        string? dragState = null;
        if (ctx is not null && ReferenceEquals(ctx.CurrentZone, this) && ctx.Item is TItem dragged && _identity.Equals(dragged, item)
            && (ctx.PlaceholderZone is null || ReferenceEquals(ctx.PlaceholderZone, this)))
        {
            dragState = ctx.Input == BitSortableInput.Keyboard ? Styles?.Chosen : JoinStyles(Styles?.Ghost, Styles?.Chosen);
        }

        var style = JoinStyles(Styles?.Item, ItemStyle?.Invoke(item));
        style = JoinStyles(style, dragState);
        if (IsSwapTarget(ctx, item)) style = JoinStyles(style, Styles?.SwapTarget);
        if (selected) style = JoinStyles(style, Styles?.Selected);
        if (IsFiltered(item)) style = JoinStyles(style, Styles?.Filtered);
        return style.HasValue() ? style : null;
    }

    private bool IsSwapTarget(BitSortableDragContext? ctx, TItem item)
        => ctx is not null && ReferenceEquals(ctx.SwapTargetZone, this) && ctx.SwapTargetItem is TItem target && _identity.Equals(target, item);

    private string GhostClasses => Css("bit-srt-gst", Classes?.Ghost);

    private string ChosenClasses => Css("bit-srt-chs", Classes?.Chosen);

    // The preview slot uses the exact same ghost styling as a within-list drag, and keeps the dragged item's own
    // per-item class and style so its look is preserved.
    private string PlaceholderCssClass()
    {
        var extra = ItemClass is not null && Service.Context?.Item is TItem preview ? ItemClass(preview) : null;
        return Css("bit-srt-itm", Classes?.Item, extra, GhostClasses, ChosenClasses);
    }

    private string? PlaceholderCssStyle()
    {
        var extra = ItemStyle is not null && Service.Context?.Item is TItem preview ? ItemStyle(preview) : null;
        var style = JoinStyles(JoinStyles(Styles?.Item, extra), JoinStyles(Styles?.Ghost, Styles?.Chosen));
        return style.HasValue() ? style : null;
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

    // Shown while the list has no items, unless the drop preview of a drag is in it.
    private bool ShowsEmpty => (Items is null || Items.Count == 0) && IsPlaceholderAt(0) is false;

    private bool IsFiltered(TItem item) => Filter is not null && Filter(item);

    private bool CanDrag(TItem item) => !Disabled && !IsFiltered(item);

    // A read-only list (e.g. an array) can be displayed and cloned from, but not changed.
    private bool IsMutable => Items is { IsReadOnly: false };

    private bool AllowSort => Sort && !Disabled && IsMutable;

    private BitSortablePull EffectivePull => Pull == BitSortablePull.Move && !IsMutable ? BitSortablePull.None : Pull;

    private string DirectionValue => Direction switch
    {
        BitSortableDirection.Horizontal => "horizontal",
        BitSortableDirection.Vertical => "vertical",
        _ => "auto"
    };

    private bool KeyboardEnabled => Keyboard && !Disabled;

    private BitSortableStrings EffectiveStrings => Strings ?? BitSortableStrings.Default;

    // The list's accessible name, which the announcements name it by: the AriaLabel, or an aria-label written on it.
    private string? Label => ResolveAriaLabel();

    /// <summary>The id of the hidden element holding the keyboard instructions every item refers to.</summary>
    private string InstructionsId => $"{_Id}-ins";

    /// <summary>
    /// The key of the item that is the list's tab stop: the one last focused, or the first. The other items take
    /// the focus from the arrow keys or a click, so a list is a single stop in the page's tab order.
    /// </summary>
    private string? ActiveKey()
    {
        var items = Items;
        if (!KeyboardEnabled || items is null || items.Count == 0) return null;
        if (_activeKey is not null && _itemOf.TryGetValue(_activeKey, out var active) && IndexOfItem(active) >= 0) return _activeKey;
        return KeyOf(items[0]);
    }

    private string LabelOf(TItem item) => (ItemLabel is not null ? ItemLabel(item) : item?.ToString()) ?? string.Empty;

    /// <summary>An item's template in a component of its own (see <see cref="IsolateItems"/>).</summary>
    private RenderFragment IsolatedItem(TItem item) => builder =>
    {
        builder.OpenComponent<_BitSortableItem<TItem>>(0);
        builder.AddComponentParameter(1, nameof(_BitSortableItem<TItem>.Item), item);
        builder.AddComponentParameter(2, nameof(_BitSortableItem<TItem>.Template), ItemTemplate);
        builder.AddComponentParameter(3, nameof(_BitSortableItem<TItem>.Version), _parametersVersion);
        builder.CloseComponent();
    };

    /// <summary>The key <paramref name="item"/> is rendered with, which the script names it by.</summary>
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

        var keep = new Dictionary<object, string>(new ItemComparer(_identity));
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
        if (!MultiDrag || items is null || !CanDrag(item) || IndexOfItem(item) is var at && at < 0) return false;

        int from = range && _anchor is { } anchor ? IndexOfItem(anchor.Item) : -1;
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



    // ----- the script's calls --------------------------------------------

    // The answers of OnDragStart and OnKey are internal types, which a public method cannot return as themselves: they
    // are returned as object, which the interop serializes as the type they are, kept by the DynamicDependency.

    /// <summary>
    /// A drag of the item rendered with <paramref name="key"/> starts, made with <paramref name="input"/>. Answers the
    /// lists that take the drag (a <c>BitSortableStartResult</c>), so the script routes the pointer to them alone;
    /// <c>null</c> when the item cannot be dragged.
    /// </summary>
    [JSInvokable("OnDragStart")]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitSortableStartResult))]
    public async Task<object?> _OnDragStart(string key, string input)
    {
        if (IsDisposing) return null;

        return await StartAsync(key, input);
    }

    /// <summary>
    /// The pointer reached drop position <paramref name="index"/> of this list. Answers 1 when a list changed (the
    /// script plays the slide), 0 when nothing did, -1 when the list refused the position (it shows the pointer as
    /// unable to drop there). Every call about a drag names it by the id the start answered (<paramref name="drag"/>):
    /// a late call about a drag that has since ended is ignored.
    /// </summary>
    [JSInvokable("OnDragOver")]
    public async Task<int> _OnDragOver(int index, int drag)
    {
        if (IsDisposing) return Refused;

        return await OverAsync(index, drag);
    }

    /// <summary>
    /// The drag ended; <paramref name="how"/> is <c>drop</c>, <c>release</c>, <c>cancel</c> or <c>abandon</c>. Answers
    /// what to announce.
    /// </summary>
    [JSInvokable("OnDragEnd")]
    public async Task<string?> _OnDragEnd(string how, int drag)
    {
        if (IsDisposing) return null;

        return await EndAsync(how, drag);
    }

    /// <summary>
    /// A keyboard action on the item rendered with <paramref name="key"/>: <c>pick</c>, <c>select</c>, <c>to</c> (the
    /// item moves to <paramref name="index"/> of the list <paramref name="list"/>, this one when <c>null</c>),
    /// <c>drop</c> or <c>cancel</c>; <paramref name="drag"/> names the keyboard drag the step belongs to.
    /// </summary>
    [JSInvokable("OnKey")]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitSortableKeyResult))]
    public async Task<object> _OnKey(string key, string action, string? list, int index, int drag)
    {
        if (IsDisposing) return NotDragging;

        return await KeyAsync(key, action, list, index, drag);
    }

    /// <summary>The item rendered with <paramref name="key"/> got the focus: it becomes the list's tab stop.</summary>
    [JSInvokable("OnItemFocus")]
    public void _OnItemFocus(string key)
    {
        if (IsDisposing) return;

        ((IBitSortableZone)this).Focus(key);
    }


    // ----- the drag, as the browser layer reports it ---------------------

    /// <summary>
    /// Starts a drag of the item the browser layer names by <paramref name="key"/>. Answers the lists that take the
    /// drag, so the browser layer routes the pointer to them alone; <c>null</c> when the item cannot be dragged.
    /// </summary>
    private async Task<BitSortableStartResult?> StartAsync(string key, string input)
    {
        if (IsDisposing || Items is null || Service.Confirming > 0 || !_itemOf.TryGetValue(key, out var item) || !CanDrag(item)) return null;

        BitSortableMoveInfo? info = null;
        BitSortableDragOutcome? abandoned = null;
        string[]? lists = null;
        string? announce = null;
        bool clone = false;
        BitSortableDragContext? started = null;
        await Service.Gate.WaitAsync();
        try
        {
            // A drag whose end was never observed (e.g. its element was removed mid-drag): end it where it is.
            if (Service.Context is { } stale) abandoned = Finish(stale, commit: false);
        }
        finally
        {
            Service.Gate.Release();
        }

        // The abandoned drag is reported first, as it ended before this one started.
        if (abandoned is not null)
        {
            await ConfirmAndReportAsync(abandoned);
            await PassOnAsync(abandoned.Context);
            if (IsDisposing || Items is null || Service.Confirming > 0 || !CanDrag(item)) return null;
        }

        await Service.Gate.WaitAsync();
        try
        {
            // Asked again inside the gate: a drop that ended while this call waited for the gate may be confirmed now
            // (ConfirmAndReportAsync), and a drag started on its lists would be undone along with it.
            info = Service.Context is null && Service.Confirming == 0 ? BeginDrag(item, InputOf(input), carrySelection: true) : null;
            if (info is not null && Service.Context is { } ctx)
            {
                lists = ListsTaking(ctx);
                announce = EffectiveStrings.PickedUp(Announcement(ctx, this, item!));
                clone = ctx.PullMode == BitSortablePull.Clone;
                started = ctx;
            }
        }
        finally
        {
            Service.Gate.Release();
        }

        // Picking up an unselected item drops the selection. A SelectedItemsChanged handler that throws does not refuse
        // the drag, as it does not undo Shift+Space (KeySelectAsync): the selection changed all the same, and the drag
        // goes on - OnStart is raised - while what the handler threw is passed on once the browser has its answer.
        Exception? selectionFailure = null;
        if (_selectionChanged)
        {
            try
            {
                await PublishSelectionAsync();
            }
            catch (Exception exception)
            {
                selectionFailure = exception;
            }
        }

        if (info is null || started is null)
        {
            if (selectionFailure is not null) await PassOnAsync(selectionFailure);
            return null;
        }

        if (selectionFailure is not null) started.Fail(selectionFailure);
        bool startFailed = false;
        try
        {
            await RaiseEventAsync(BitSortableEventType.Start, info);
        }
        catch (Exception exception)
        {
            started.Fail(exception);
            startFailed = true;
        }

        // The browser is never told of a drag whose OnStart threw, so it would never end it: it ends here, as cancelled -
        // OnEnd still follows OnStart. What the app's code threw is passed on as the list's own (see PassOnAsync), as
        // nothing would report it once it went back to the browser layer. Should that take this list away (an
        // ErrorBoundary showing its error content), its disposal has ended the drag: the browser is not told of it.
        if (startFailed) await AbortAsync(started);
        await PassOnAsync(started);
        if (startFailed || IsDisposing) return null;

        return new BitSortableStartResult { Lists = lists!, Announce = announce, Clone = clone, Drag = started.Id };
    }

    /// <summary>
    /// Ends <paramref name="ctx"/> as cancelled, if it is still the drag in progress, and reports it; what the handlers
    /// throw is kept on <paramref name="ctx"/> (see <see cref="ReportAsync"/>).
    /// </summary>
    private async Task AbortAsync(BitSortableDragContext ctx)
    {
        BitSortableDragOutcome? outcome = null;
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
    private static BitSortableAnnouncement Announcement(BitSortableDragContext ctx, IBitSortableZone zone, object item, string? target = null) => new()
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
    private static string EndAnnouncement(BitSortableDragOutcome outcome)
    {
        var info = outcome.Info;
        var messages = outcome.Context.Source.Strings;
        var about = Announcement(outcome.Context, info.To, info.Item);
        return info.Canceled ? messages.Canceled(about) : messages.Dropped(about);
    }

    // ----- the keyboard ---------------------------------------------------

    private static readonly BitSortableKeyResult NotDragging = new() { Dragging = false };

    /// <summary>
    /// A key pressed on the item rendered with <paramref name="key"/>, or during a keyboard drag, as the browser layer
    /// reports it: <c>pick</c> picks the item up, <c>to</c> moves it to <paramref name="index"/> of the list
    /// <paramref name="listId"/> (or of the list it is in) - the browser layer works out which item or list lies in the
    /// direction of the arrow key - and <c>drop</c> and <c>cancel</c> end the drag. A keyboard drag moves the item
    /// itself, into another list too: there is no native drag to keep alive. Answers what to announce and where the
    /// item is now, so the focus follows it.
    /// </summary>
    private async Task<BitSortableKeyResult> KeyAsync(string key, string action, string? listId, int index, int drag)
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

    private async Task<BitSortableKeyResult> PickAsync(string key)
    {
        if (IsDisposing || !KeyboardEnabled || Items is null || !_itemOf.TryGetValue(key, out var item) || IndexOfItem(item) < 0)
            return NotDragging;

        _activeKey = key;
        if (!CanDrag(item))
            return new BitSortableKeyResult { Announce = EffectiveStrings.Locked(new BitSortableAnnouncement { Item = LabelOf(item), Position = IndexOfItem(item) + 1, Count = Items.Count, List = Label }) };

        var started = await StartAsync(key, "keyboard");
        if (started is null) return NotDragging;

        return new BitSortableKeyResult { Dragging = true, Lists = started.Lists, Announce = started.Announce, List = _Id, Key = key, Drag = started.Drag };
    }

    /// <summary>Shift+Space on an item of a MultiDrag list: toggles its selection, and says so.</summary>
    private async Task<BitSortableKeyResult> KeySelectAsync(string key)
    {
        if (IsDisposing || !KeyboardEnabled || !MultiDrag || Items is null || Service.Context is not null || !_itemOf.TryGetValue(key, out var item))
            return NotDragging;

        _activeKey = key;
        var about = new BitSortableAnnouncement { Item = LabelOf(item), Position = IndexOfItem(item) + 1, Count = Items.Count, List = Label };
        if (!CanDrag(item)) return new BitSortableKeyResult { Announce = EffectiveStrings.Locked(about) };

        bool selected;
        try
        {
            selected = await SelectAsync(item, range: false);
        }
        catch (Exception exception)
        {
            // A SelectedItemsChanged handler threw: the selection changed all the same, and is said; the exception is
            // passed on as the list's own, as nothing would report it once it went back to the browser layer.
            selected = _selected.Contains(item);
            await PassOnAsync(exception);
        }

        about = new BitSortableAnnouncement { Item = about.Item, Position = about.Position, Count = about.Count, List = about.List, Selected = _selected.Count };
        return new BitSortableKeyResult { Announce = selected ? EffectiveStrings.Selected(about) : EffectiveStrings.Deselected(about), List = _Id, Key = key };
    }

    private async Task<BitSortableKeyResult> KeyMoveAsync(string? listId, int index, int drag)
    {
        BitSortableDragContext? ctx;
        BitSortableMoveInfo? change = null;
        BitSortableKeyResult result;
        await Service.Gate.WaitAsync();
        try
        {
            ctx = Service.ContextOf(drag);
            if (ctx is null || ctx.Input != BitSortableInput.Keyboard) return NotDragging;

            var current = ctx.CurrentZone;
            var target = listId is null ? current : Service.Zones.FirstOrDefault(zone => zone.Id == listId) ?? current;
            var messages = ctx.Source.Strings;
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
            result = new BitSortableKeyResult
            {
                Dragging = true,
                Announce = announce,
                List = zone.Id,
                Key = zone.KeyOfItem(ctx.Item),
                Target = ctx.SwapTargetItem is { } swapWith ? ctx.SwapTargetZone!.KeyOfItem(swapWith) : null,
                // Carried into another list, the item may be taken by lists that did not take it where it was, and
                // the other way round (a swap list trades with the list the item is in).
                Lists = ListsTaking(ctx)
            };
        }
        finally
        {
            Service.Gate.Release();
        }

        if (change is not null) await RaiseKeepingAsync(change.To, BitSortableEventType.Change, change, ctx);
        await PassOnAsync(ctx);
        return result;
    }

    /// <summary>
    /// Moves the dragged item to <paramref name="index"/> of <paramref name="target"/> in a keyboard drag: within a
    /// list the item has been carried into, or into another list - out of the list it is in, or, for a clone, as a
    /// copy, which is taken out again when it moves on. Back home it simply returns. Asked of the target's rules
    /// like any other position.
    /// </summary>
    private static int MoveByKey(BitSortableDragContext ctx, IBitSortableZone target, int index, out BitSortableMoveInfo? change)
    {
        change = null;
        var current = ctx.CurrentZone;
        var source = ctx.Source;
        bool home = ReferenceEquals(target, source);
        bool clone = ctx.PullMode == BitSortablePull.Clone;

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

    private async Task<BitSortableKeyResult> KeyEndAsync(bool drop, int drag)
    {
        BitSortableDragOutcome outcome;
        await Service.Gate.WaitAsync();
        try
        {
            var ctx = Service.ContextOf(drag);
            if (ctx is null || ctx.Input != BitSortableInput.Keyboard) return NotDragging;
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
        var result = new BitSortableKeyResult { Announce = EndAnnouncement(outcome), List = zone.Id, Key = key };
        await PassOnAsync(outcome.Context);
        return result;
    }

    private static BitSortableInput InputOf(string input) => input switch
    {
        "touch" => BitSortableInput.Touch,
        "pen" => BitSortableInput.Pen,
        "keyboard" => BitSortableInput.Keyboard,
        _ => BitSortableInput.Mouse
    };

    /// <summary>
    /// Records a drag of <paramref name="item"/> on the coordinator; <c>null</c> when the item is no longer in the
    /// list. With <paramref name="carrySelection"/> a selected item carries the multi-drag selection with it.
    /// </summary>
    private BitSortableMoveInfo? BeginDrag(TItem item, BitSortableInput input, bool carrySelection)
    {
        int oldIndex = IndexOfItem(item);
        if (oldIndex < 0) return null;

        // The list's own instance of the item: with an ItemKey, MoveAsync and MoveToAsync may be handed a copy the page
        // loaded before its items were loaded again, and the drag moves and reports the one the list holds now.
        item = Items![oldIndex];

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

        var ctx = new BitSortableDragContext
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
    private bool HandlesDrag(BitSortableDragContext ctx)
    {
        if (ReferenceEquals(ctx.CurrentZone, this)) return true;

        // Swapping moves the dragged item here and the target item to the list the dragged item is in (the source, unless
        // a keyboard drag carried it into another list), so both directions must be allowed.
        return CanAccept(ctx) && (!Swap || (EffectivePull != BitSortablePull.None && ctx.CurrentZone.CanReceiveFrom(this)));
    }

    /// <summary>
    /// The ids of the lists the browser layer routes <paramref name="ctx"/> to: those that take it, and the list it
    /// started in, which takes the item back from wherever a keyboard drag carried it.
    /// </summary>
    private string[] ListsTaking(BitSortableDragContext ctx)
        => [.. Service.Zones.Where(zone => ReferenceEquals(zone, ctx.Source) || zone.TakesDrag(ctx)).Select(zone => zone.Id)];

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
        BitSortableDragContext? ctx;
        BitSortableMoveInfo? change;
        int result;
        await Service.Gate.WaitAsync();
        try
        {
            ctx = Service.ContextOf(drag);
            if (ctx is null || IsDisposing || !HandlesDrag(ctx)) return Refused;
            result = ApplyOver(ctx, index, out change);
        }
        finally
        {
            Service.Gate.Release();
        }

        if (change is not null) await RaiseKeepingAsync(this, BitSortableEventType.Change, change, ctx);
        await PassOnAsync(ctx);
        return result;
    }

    /// <summary>Positions the drag at drop position <paramref name="index"/> of this list, inside the gate (see <see cref="OverAsync"/>).</summary>
    private int ApplyOver(BitSortableDragContext ctx, int index, out BitSortableMoveInfo? change)
    {
        change = null;
        if (Swap) return UpdateSwapTarget(ctx, index);
        return ReferenceEquals(ctx.CurrentZone, this)
            ? ReorderWithin(ctx, index, out change)
            : PreviewFromOtherList(ctx, index, out change);
    }

    /// <summary>Reorders the home list as the dragged item moves over it.</summary>
    private int ReorderWithin(BitSortableDragContext ctx, int target, out BitSortableMoveInfo? change)
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

        int from = IndexOfItem(dragged);
        if (from < 0) return Unchanged;
        // The list's own instance of the item: with an ItemKey the page may have loaded its items again since the drag
        // began, and the item it moves is the one the list holds now, not the copy the drag started with.
        var moving = Items[from];
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
            Items.Insert(insertAt, moving);
            ctx.Item = moving!;
            change = MoveInfo(ctx, this, insertAt);
        }
        StateHasChanged();
        return Changed;
    }

    /// <summary>
    /// Previews where an item dragged in from another list would land. The actual
    /// item is not moved until the drag ends, so the native drag stays alive.
    /// </summary>
    private int PreviewFromOtherList(BitSortableDragContext ctx, int index, out BitSortableMoveInfo? change)
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
    private bool AllowsDrop(BitSortableDragContext ctx, int newIndex) => CanDropAllows(ctx, MoveInfo(ctx, this, newIndex));

    /// <summary>Whether <paramref name="info"/> moves an item of this list's type, to a position <see cref="CanDrop"/> allows.</summary>
    private bool AllowsMove(BitSortableDragContext ctx, BitSortableMoveInfo info) => info.Item is TItem && CanDropAllows(ctx, info);

    /// <summary>
    /// Asks <see cref="CanDrop"/> about <paramref name="info"/>, a move of the drag <paramref name="ctx"/>, inside the
    /// gate. One that throws refuses the move: the drag is left whole - no move half made, no drag left in progress
    /// with nobody to end it - and the exception is kept on <paramref name="ctx"/>, to be passed on once the gate is
    /// released (<see cref="PassOnAsync(BitSortableDragContext)"/>, <see cref="MoveCoreAsync"/>).
    /// </summary>
    private bool CanDropAllows(BitSortableDragContext ctx, BitSortableMoveInfo info)
    {
        if (CanDrop is null) return true;
        var args = ArgsOf(info);
        try
        {
            return CanDrop(args);
        }
        catch (Exception exception)
        {
            ctx.Fail(exception);
            return false;
        }
    }

    /// <summary>
    /// Whether the item currently being dragged (and, for a multi-drag, the rest of the
    /// selection) may be dropped into this list from another one.
    /// </summary>
    private bool CanAccept(BitSortableDragContext ctx)
    {
        if (Items is null || ctx.PullMode == BitSortablePull.None || !CanReceiveFrom(ctx.Source)) return false;

        // A swap leaves both lists as long as they were; any other drop takes the items out of the source for good.
        if (!Swap && !ctx.Source.LetsGo(ctx)) return false;

        // Every incoming item must be of this list's type, not already in it and not twice among them: Blazor
        // requires unique @key values, so a value-equal duplicate cannot be added.
        if (ctx.MultiItems is { } block && ctx.PullMode == BitSortablePull.Move)
            return CountIn(block, out bool unfit) == 0 && !unfit && HasRoomFor(block.Count);

        // A clone a Clone factory makes may have a key of its own, so the original's being here says nothing: the copy
        // is checked once it is made (CommitPlaceholder). One carried on by a keyboard drag is the copy itself.
        bool copyToCome = ctx.PullMode == BitSortablePull.Clone && ReferenceEquals(ctx.CurrentZone, ctx.Source) && ctx.Source.ClonesByFactory;
        return ctx.Item is TItem dragged && (copyToCome || IndexOfItem(dragged) < 0) && HasRoomFor(1);
    }

    /// <summary>Whether <see cref="MaxItems"/> leaves room for <paramref name="incoming"/> more items. A swap brings one and takes one.</summary>
    private bool HasRoomFor(int incoming) => Swap || MaxItems is not { } max || Count + incoming <= max;

    private bool CanReceiveFrom(IBitSortableZone source)
    {
        if (Disabled || !Put || !IsMutable) return false;
        if (ReferenceEquals(source, this)) return true;
        if (source.GroupName is not { } group) return false;
        if (string.Equals(Group, group, StringComparison.Ordinal)) return true;
        return PutGroups is { Count: > 0 } groups && groups.Contains(group, StringComparer.Ordinal);
    }

    /// <summary>
    /// Whether <see cref="MinItems"/> lets the drag <paramref name="ctx"/>, which started in this list, take its items
    /// out of it for good: a clone leaves them, and items a keyboard drag already carried out count as gone.
    /// </summary>
    private bool LetsGo(BitSortableDragContext ctx)
    {
        if (MinItems is not { } min || ctx.PullMode != BitSortablePull.Move) return true;

        int leaving = ctx.MultiItems is { } block
            ? CountIn(block, out _)
            : ctx.Item is TItem t && IndexOfItem(t) >= 0 ? 1 : 0;
        return Count - leaving >= min;
    }

    /// <summary>Shows (or moves) the drop placeholder in this zone at <paramref name="index"/>.</summary>
    private void ShowPlaceholder(BitSortableDragContext ctx, int index)
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
    private static bool ClearPlaceholder(BitSortableDragContext ctx)
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
    /// dragging in swap mode. Nothing is moved until drop; only the highlight (<see cref="BitSortableClassStyles.SwapTarget"/>) follows the
    /// pointer. Works within this list and across lists in the group.
    /// </summary>
    private int UpdateSwapTarget(BitSortableDragContext ctx, int index)
    {
        if (Items is null) return Unchanged;

        // A placeholder shown by a sorting list of the group no longer applies.
        bool changed = ClearPlaceholder(ctx);

        bool home = ReferenceEquals(ctx.CurrentZone, this);
        object? target = null;
        if ((!home || AllowSort) && index >= 0 && index < Items.Count)
        {
            var candidate = Items[index];
            // Across lists the candidate goes where the dragged item is: the source, or the list a keyboard drag
            // carried it into. A value-equal duplicate there would break Blazor's unique @key.
            var back = ctx.CurrentZone;
            bool valid = candidate is not null && (home
                ? !_identity.Same(candidate, ctx.Item)                          // not the dragged item itself
                : back.Accepts(candidate) && back.IndexOf(candidate) < 0);      // fits the list it goes to
            if (valid && !AllowsSwap(ctx, index, candidate!, home))
            {
                SetSwapTarget(ctx, null, null);
                return Refused;
            }
            if (valid) target = candidate;
        }

        return (SetSwapTarget(ctx, target is null ? null : this, target) | changed) ? Changed : Unchanged;
    }

    /// <summary>
    /// Whether <see cref="CanDrop"/> lets the dragged item take the place of <paramref name="partner"/>, at
    /// <paramref name="index"/> of this list, and - across lists - whether the list the dragged item is in lets
    /// <paramref name="partner"/> take its place. A swap trades the items themselves, so neither is proposed as a clone.
    /// </summary>
    private bool AllowsSwap(BitSortableDragContext ctx, int index, object partner, bool home)
    {
        var swapped = new BitSortableMoveInfo
        {
            Item = ctx.Item,
            OldIndex = ctx.OldIndex,
            NewIndex = index,
            From = ctx.Source,
            To = this,
            Input = ctx.Input,
            SwappedWith = partner
        };
        if (!AllowsMove(ctx, swapped)) return false;
        if (home) return true;

        var back = ctx.CurrentZone;
        return back.AllowsMove(ctx, new BitSortableMoveInfo
        {
            Item = partner,
            OldIndex = index,
            NewIndex = back.IndexOf(ctx.Item),
            From = this,
            To = back,
            Input = ctx.Input,
            SwappedWith = ctx.Item
        });
    }

    private static bool SetSwapTarget(BitSortableDragContext ctx, IBitSortableZone? zone, object? item)
    {
        // The same item again keeps the highlight - and the instance picked, which the list renders with.
        if (ReferenceEquals(ctx.SwapTargetZone, zone) && (zone is null || zone.Same(ctx.SwapTargetItem, item)))
        {
            ctx.SwapTargetItem = item;
            return false;
        }

        var previous = ctx.SwapTargetZone;
        ctx.SwapTargetZone = zone;
        ctx.SwapTargetItem = item;

        if (previous is not null && !ReferenceEquals(previous, zone)) previous.Rerender();
        zone?.Rerender();
        return true;
    }

    private static bool ClearSwapTarget(BitSortableDragContext ctx)
    {
        var zone = ctx.SwapTargetZone;
        if (zone is null) return false;
        ctx.SwapTargetZone = null;
        ctx.SwapTargetItem = null;
        zone.Rerender();
        return true;
    }

    /// <summary>
    /// Trades the dragged item's slot with the highlighted swap target. The list the item leaves is the one it is in:
    /// the source, unless a keyboard drag carried it into another list, which then changes too (see
    /// <see cref="ReportAsync"/>).
    /// </summary>
    private static void CommitSwap(BitSortableDragContext ctx)
    {
        var fromZone = ctx.CurrentZone;
        var toZone = ctx.SwapTargetZone!;

        int fromIndex = fromZone.IndexOf(ctx.Item);
        int toIndex = toZone.IndexOf(ctx.SwapTargetItem!);
        if (fromIndex < 0 || toIndex < 0) return;
        // A reloaded copy of the dragged item is the dragged item, never its partner.
        if (ReferenceEquals(fromZone, toZone) && fromIndex == toIndex) return;

        // The lists' own instances: with an ItemKey the page may have loaded its items again since they were picked.
        var dragged = fromZone.ItemAt(fromIndex)!;
        var target = toZone.ItemAt(toIndex)!;
        ctx.Item = dragged;

        ctx.Touch(fromZone);
        ctx.Touch(toZone);
        fromZone.Replace(fromIndex, target);
        toZone.Replace(toIndex, dragged);
        ctx.CurrentZone = toZone;
        ctx.SwappedWith = target;
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

        BitSortableDragOutcome? outcome = null;
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
        var announce = how == "abandon" ? null : EndAnnouncement(outcome);
        await PassOnAsync(outcome.Context);
        return announce;
    }

    /// <summary>
    /// Ends <paramref name="ctx"/>, inside the gate. With <paramref name="commit"/> the pending swap or placeholder
    /// is committed and a multi-drag selection gathered; without it (a drag that ends before it was released, as
    /// its list is gone) whatever is pending is dropped and the lists keep what the drag already did to them.
    /// A multi-drag whose gathering <see cref="CanDrop"/> refuses is put back, and ends as cancelled.
    /// Returns what <see cref="ReportAsync"/> raises once the gate is released.
    /// </summary>
    private BitSortableDragOutcome Finish(BitSortableDragContext ctx, bool commit)
    {
        ctx.Finished = true;

        // Zones showing drag state now; committing may detach them from the context.
        var placeholderZone = ctx.PlaceholderZone;
        var swapZone = ctx.SwapTargetZone;
        IReadOnlyList<object>? moved = null;
        bool refused = false;
        try
        {
            if (commit) moved = Commit(ctx, out refused);
        }
        finally
        {
            EndDrag(ctx, placeholderZone, swapZone);
        }

        return Outcome(ctx, moved, canceled: refused);
    }

    /// <summary>
    /// Ends <paramref name="ctx"/> without keeping anything, inside the gate: the pending swap, placeholder and
    /// multi-drag gathering are dropped, and the item the drag moved goes back to where it was.
    /// </summary>
    private BitSortableDragOutcome Cancel(BitSortableDragContext ctx)
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
    private static void PutBack(BitSortableDragContext ctx)
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
    private static BitSortableDragOutcome Outcome(BitSortableDragContext ctx, IReadOnlyList<object>? moved, bool canceled)
    {
        var to = ctx.CurrentZone;
        return new BitSortableDragOutcome
        {
            Info = new BitSortableMoveInfo
            {
                Item = ctx.Item,
                Items = moved,
                OldIndex = ctx.OldIndex,
                NewIndex = to.IndexOf(ctx.Item),
                From = ctx.Source,
                To = to,
                Input = ctx.Input,
                IsClone = IsCloneInto(ctx, to),
                Canceled = canceled,
                SwappedWith = canceled ? null : ctx.SwappedWith
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

    private static bool IsCloneInto(BitSortableDragContext ctx, IBitSortableZone to)
        => ctx.PullMode == BitSortablePull.Clone && !ReferenceEquals(to, ctx.Source);

    // ----- moves made by code --------------------------------------------

    /// <summary>
    /// Moves <paramref name="item"/> to <paramref name="newIndex"/> within this list, as a drag would: the list must
    /// allow it (<see cref="Sort"/>, <see cref="BitComponentBase.Disabled"/>, <see cref="CanDrop"/>), the destination's
    /// <see cref="ConfirmDrop"/> is asked, the items slide into place, and the drag's events follow, with
    /// <see cref="BitSortableEventArgs{TItem}.Input"/> set to <see cref="BitSortableInput.Api"/>. In swap mode the item
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
    /// Moves <paramref name="item"/> from this list into the list whose id (its <see cref="BitComponentBase.Id"/>, or its <see cref="BitComponentBase.UniqueId"/>) is <paramref name="listId"/>,
    /// at <paramref name="newIndex"/>, as a drag would: the lists must exchange items (<see cref="Group"/> or the
    /// destination's <see cref="PutGroups"/>, <see cref="Pull"/>, <see cref="Put"/>, <see cref="BitComponentBase.Disabled"/>, the
    /// destination's <see cref="MaxItems"/>, this list's <see cref="MinItems"/>, the item type), the destination's
    /// <see cref="CanDrop"/> and <see cref="ConfirmDrop"/> are asked, a <see cref="BitSortablePull.Clone"/> source puts
    /// a copy there, and the drag's events follow with <see cref="BitSortableEventArgs{TItem}.Input"/> set to
    /// <see cref="BitSortableInput.Api"/>. Into a swap-mode list the item trades places with the one at
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
    private async Task<bool> MoveCoreAsync(TItem item, IBitSortableZone target, int newIndex)
    {
        // A disabled list takes part in no drag, nor in a move made by code (its items cannot be dragged out).
        if (IsDisposing || Disabled || Items is null || Service.Context is not null || Service.Confirming > 0) return false;

        bool within = ReferenceEquals(target, this);
        int from = IndexOfItem(item);
        if (from < 0 || newIndex < 0) return false;
        if (within ? newIndex >= Items.Count || newIndex == from : newIndex > (target.IsSwap ? target.Count - 1 : target.Count)) return false;

        // The lists the move may change, snapshotted for the slide.
        await CaptureFlipAsync();
        if (!within) await target.CaptureFlipAsync();

        BitSortableMoveInfo? start;
        BitSortableMoveInfo? change = null;
        BitSortableDragOutcome outcome;
        await Service.Gate.WaitAsync();
        try
        {
            if (Service.Context is not null || Service.Confirming > 0) return false;

            start = BeginDrag(item, BitSortableInput.Api, carrySelection: false);
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
        // and reported - OnEnd still follows OnStart - and its exception is passed on after, as one CanDrop threw is.
        await RaiseKeepingAsync(this, BitSortableEventType.Start, start, outcome.Context);
        if (change is not null) await RaiseKeepingAsync(change.To, BitSortableEventType.Change, change, outcome.Context);
        outcome = await ConfirmAndReportAsync(outcome);

        // A screen reader hears what a button or a menu just did, as it would for a keyboard drag.
        var info = outcome.Info;
        if (!info.Canceled) await AnnounceAsync(EffectiveStrings.Moved(Announcement(outcome.Context, info.To, info.Item)));

        // The app's own code asked for the move: what its CanDrop, ConfirmDrop or handlers threw goes back to it.
        if (outcome.Context.TakeFailure() is { } failure) ExceptionDispatchInfo.Throw(failure);
        return !info.Canceled;
    }

    /// <summary>
    /// Asks the destination's <see cref="ConfirmDrop"/> whether to keep a drag that changed the lists, puts them back
    /// when it says no, then reports the outcome, which it returns. No other drag starts while it is asked. One that
    /// throws refuses the drag - OnEnd still follows OnStart. Without <paramref name="ask"/> it is not asked, and a
    /// destination that has one is taken to refuse: a drag it could not confirm is not kept. What the app's code throws
    /// meanwhile - the ConfirmDrop, a handler of the events that report the drag - is kept on the drag, after what was
    /// kept before it, for the caller to pass on once it is done (<see cref="BitSortableDragContext.TakeFailure"/>).
    /// </summary>
    private async Task<BitSortableDragOutcome> ConfirmAndReportAsync(BitSortableDragOutcome outcome, bool ask = true)
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
                    keep = ask ? await outcome.Info.To.ConfirmAsync(outcome.Info) : !outcome.Info.To.ConfirmsDrop;
                }
                catch (Exception exception)
                {
                    outcome.Context.Fail(exception);
                    keep = false;
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

    /// <summary>
    /// Raises <paramref name="type"/> on <paramref name="zone"/>, keeping what its handler throws on
    /// <paramref name="ctx"/> (<see cref="BitSortableDragContext.Fail"/>) for the caller to pass on: a handler that throws
    /// keeps no other from hearing of the drag.
    /// </summary>
    private static async Task RaiseKeepingAsync(IBitSortableZone zone, BitSortableEventType type, BitSortableMoveInfo info, BitSortableDragContext ctx)
    {
        try
        {
            await zone.RaiseEventAsync(type, info);
        }
        catch (Exception exception)
        {
            ctx.Fail(exception);
        }
    }

    /// <summary>
    /// Passes on what the app's code threw while <paramref name="ctx"/> was decided or reported (see
    /// <see cref="BitSortableDragContext.Fail"/>), once the browser layer's call has done its work - put back, reported,
    /// answered. That call does not come from the app's code, which could catch the exception, and the browser layer
    /// has no use for it: it is raised as this list's own, as one thrown by an event handler would be, for the nearest
    /// <c>ErrorBoundary</c> to handle.
    /// </summary>
    private Task PassOnAsync(BitSortableDragContext ctx)
        => ctx.TakeFailure() is { } failure ? PassOnAsync(failure) : Task.CompletedTask;

    /// <summary>
    /// Raises <paramref name="failure"/>, which the app's code threw during a call the browser layer made, as this list's
    /// own (see <see cref="PassOnAsync(BitSortableDragContext)"/>). A list the handler that threw took off the page - it
    /// hid the list, or navigated away - is one the renderer no longer knows, and would refuse it: another list of the
    /// scope still on the page raises it instead, or, with none left, it is logged, as nothing else would report it.
    /// </summary>
    private Task PassOnAsync(Exception failure)
    {
        if (!IsDisposing) return DispatchExceptionAsync(failure);
        if (Service.Zones.FirstOrDefault() is { } other) return other.PassOnAsync(failure);

        (_services.GetService(typeof(ILoggerFactory)) as ILoggerFactory)?.CreateLogger<BitSortable<TItem>>()
            .LogError(failure, "A handler of a drag threw after its list was removed from the page.");
        return Task.CompletedTask;
    }

    /// <summary>Puts every list a refused drag changed back to what it held before the drag, sliding the items back.</summary>
    private async Task<BitSortableDragOutcome> RefuseAsync(BitSortableDragOutcome outcome)
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
        return new BitSortableDragOutcome
        {
            Info = new BitSortableMoveInfo
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

    /// <summary>
    /// Commits the pending swap or placeholder and gathers a multi-drag selection; returns the items a multi-drag moved.
    /// <paramref name="refused"/> tells that the drop or the gathering was refused, and the drag put back.
    /// </summary>
    private static IReadOnlyList<object>? Commit(BitSortableDragContext ctx, out bool refused)
    {
        refused = false;
        if (ctx.SwapTargetZone is not null && ctx.SwapTargetItem is not null)
        {
            CommitSwap(ctx);
            return null;
        }

        if (ctx.PlaceholderZone is { } target && !ReferenceEquals(target, ctx.CurrentZone) && !target.CommitPlaceholder(ctx))
        {
            // Refused at the drop: nothing left the source, and no selection is gathered.
            PutBack(ctx);
            refused = true;
            return null;
        }
        return ctx.MultiItems is not null ? CollectMulti(ctx, out refused) : null;
    }

    /// <summary>
    /// Notifies <c>ItemsChanged</c> of every list a finished drag changed, then raises its lifecycle events.
    /// Called outside the gate: the handlers see the final state, and a slow one cannot hold up the next drag. A handler
    /// that throws does not keep the others from hearing of the drag - OnEnd still follows OnStart: what each threw is
    /// kept on the drag (<see cref="BitSortableDragContext.Fail"/>), for the caller to pass on.
    /// </summary>
    private static async Task ReportAsync(BitSortableDragOutcome outcome)
    {
        var ctx = outcome.Context;
        foreach (var zone in outcome.ChangedZones)
        {
            try
            {
                await zone.NotifyItemsChangedAsync();
            }
            catch (Exception exception)
            {
                ctx.Fail(exception);
            }
        }

        // A multi-drag spends its selection; items that left a list leave its selection.
        foreach (var zone in new HashSet<IBitSortableZone>([ctx.Source, ctx.CurrentZone, .. ctx.TouchedZones], ReferenceEqualityComparer.Instance))
        {
            try
            {
                await zone.PublishSelectionIfChangedAsync();
            }
            catch (Exception exception)
            {
                ctx.Fail(exception);
            }
        }

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
                await RaiseKeepingAsync(from, BitSortableEventType.Update, info, ctx);
                await RaiseKeepingAsync(from, BitSortableEventType.Sort, info, ctx);
            }
        }
        else
        {
            if (outcome.SourceChanged) await RaiseKeepingAsync(from, BitSortableEventType.Remove, info, ctx);
            await RaiseKeepingAsync(info.To, BitSortableEventType.Add, info, ctx);
            if (outcome.SourceChanged) await RaiseKeepingAsync(from, BitSortableEventType.Sort, info, ctx);
            await RaiseKeepingAsync(info.To, BitSortableEventType.Sort, info, ctx);
        }

        // A list that is neither - one a keyboard drag carried the item into, which a swap then traded it away from - was
        // changed by the drag too (CommitSwap).
        if (!info.Canceled)
        {
            foreach (var zone in outcome.ChangedZones)
                if (!ReferenceEquals(zone, from) && !ReferenceEquals(zone, info.To))
                    await RaiseKeepingAsync(zone, BitSortableEventType.Sort, info, ctx);
        }
        await RaiseKeepingAsync(from, BitSortableEventType.End, info, ctx);
    }

    /// <summary>
    /// Gathers the rest of the multi-drag selection next to the primary item, which has
    /// already been positioned by the normal reorder / placeholder flow. The selected
    /// block is removed from wherever it sits and re-inserted contiguously, in its
    /// original order, at the primary's position in the destination list. When the destination's
    /// <c>CanDrop</c> refuses the index the primary takes then, nothing is gathered, the primary is
    /// put back, and <paramref name="refused"/> is set.
    /// </summary>
    private static IReadOnlyList<object>? CollectMulti(BitSortableDragContext ctx, out bool refused)
    {
        refused = false;
        var block = ctx.MultiItems;
        if (block is null || block.Count <= 1) return null;

        var source = ctx.Source;
        var target = ctx.CurrentZone;
        bool sameList = ReferenceEquals(target, source);

        // Within a list the block is gathered only if the list may be sorted; across lists
        // only when moving (multi-drag in clone mode clones just the dragged item).
        if (sameList ? !source.AllowSort : ctx.PullMode != BitSortablePull.Move) return null;

        int primaryPos = target.IndexOf(ctx.Item);
        if (primaryPos < 0) return null;

        // Items are told apart by the lists' ItemKey: the page may have loaded them again since the drag began, so the
        // block may hold older copies of the items the lists now hold.
        int primary = IndexIn(source, block, ctx.Item);

        // Anchor: number of non-block items before the primary in the destination list.
        int anchor = 0;
        for (int i = 0; i < primaryPos; i++)
            if (IndexIn(target, block, target.ItemAt(i)) < 0) anchor++;

        // A CanDrop that throws refuses too (CanDropAllows): the drag is still put back and reported, and the exception
        // passed on after.
        // Into another list the rest of the selection leaves the source for good: MinItems is asked again, as the page may
        // have taken items out of it since the primary was carried there (a keyboard drag).
        if (!target.AllowsDropAt(ctx, anchor + Math.Max(primary, 0)) || !sameList && !source.LetsGo(ctx))
        {
            PutBack(ctx);
            refused = true;
            return null;
        }

        ctx.MultiItems = null;
        ctx.Touch(source);
        ctx.Touch(target);

        // The primary sits in the destination list; the rest of the block in the source. What moves is each list's own
        // instance of the item, and an item the page took out of the list meanwhile is not brought back.
        var moved = new List<object>(block.Count);
        for (int b = 0; b < block.Count; b++)
        {
            var zone = !sameList && b == primary ? target : source;
            int index = zone.IndexOf(block[b]);
            if (index < 0) continue;
            moved.Add(zone.ItemAt(index)!);
            zone.RemoveAt(index);
        }

        anchor = Math.Min(anchor, target.Count);
        for (int i = 0; i < moved.Count; i++)
            target.Insert(anchor + i, moved[i]);

        source.ClearSelection();
        return moved;
    }

    /// <summary>Where <paramref name="item"/> is in <paramref name="block"/>, the items being the same to <paramref name="zone"/>; <c>-1</c> when it is not there.</summary>
    private static int IndexIn(IBitSortableZone zone, List<object> block, object? item)
    {
        for (int i = 0; i < block.Count; i++)
            if (zone.Same(block[i], item)) return i;
        return -1;
    }

    /// <summary>
    /// Moves (or clones) the dragged item from its source list into this list at the placeholder index; <c>false</c>
    /// when the drop is refused (see <see cref="IBitSortableZone.CommitPlaceholder"/>).
    /// </summary>
    private bool CommitPlaceholder(BitSortableDragContext ctx)
    {
        int index = ctx.PlaceholderIndex;
        ctx.PlaceholderZone = null;
        if (Items is null || !IsMutable) return true;

        var source = ctx.CurrentZone;
        bool clone = ctx.PullMode == BitSortablePull.Clone;

        // What moves is the source list's own instance of the item: with an ItemKey the page may have loaded its items
        // again since the drag began, and the copy the drag started with is out of date. A clone copies that instance.
        int sourceIndex = clone ? ctx.Source.IndexOf(ctx.OriginalItem) : source.IndexOf(ctx.Item);
        if (sourceIndex < 0 && !clone) return true;
        var moving = clone
            ? ctx.Source.CloneItem(sourceIndex >= 0 ? ctx.Source.ItemAt(sourceIndex)! : ctx.OriginalItem)
            : source.ItemAt(sourceIndex);

        // Never insert an item of the wrong type or one that is already here (a
        // duplicate @key, e.g. a Clone factory returning an equal value): refuse instead.
        if (moving is not TItem item || IndexOfItem(item) >= 0) return false;

        // The page may have taken items out of the source since the preview was allowed: MinItems is asked again
        // before anything leaves it - for a multi-drag, of the whole selection, whose primary is still there.
        if (!ctx.Source.LetsGo(ctx)) return false;

        if (!clone)
        {
            ctx.Touch(source);
            source.RemoveAt(sourceIndex);
        }

        ctx.Touch(this);
        Items.Insert(Math.Clamp(index, 0, Items.Count), item);
        ctx.Item = moving;
        ctx.CurrentZone = this;
        return true;
    }

    /// <summary>Clears the drag state and brings every zone the drag touched up to date.</summary>
    private void EndDrag(BitSortableDragContext ctx, params IBitSortableZone?[] alsoTouched)
    {
        var zones = new HashSet<IBitSortableZone>(ReferenceEqualityComparer.Instance) { ctx.Source, ctx.CurrentZone };
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

    private static BitSortableMoveInfo MoveInfo(BitSortableDragContext ctx, IBitSortableZone to, int newIndex) => new()
    {
        Item = ctx.Item,
        // The whole selection travels within its own list or with a move. A clone into another list copies the
        // dragged item alone, which is also the only one checked against that list's item type (see CanAccept).
        Items = ReferenceEquals(to, ctx.Source) || ctx.PullMode == BitSortablePull.Move ? ctx.MultiItems : null,
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
        if (Animation > 0) await InvokeJsVoidAsync(() => _js.BitSortableCapture(RootElement));
    }

    private void RequestFlipPlay()
    {
        if (Animation > 0) _playFlipOnRender = true;
    }

    private BitSortableJsOptions JsOptions() => new()
    {
        Id = _Id,
        Handle = Handle.HasValue() ? Handle : null,
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
        DragClass = Css("bit-srt-drg", Classes?.Drag),
        DragStyle = Styles?.Drag.HasValue() is true ? Styles.Drag : null,
        Scroll = Scroll,
        ScrollSensitivity = Math.Max(0, ScrollSensitivity),
        ScrollSpeed = Math.Max(0, ScrollSpeed)
    };

    /// <summary>
    /// Registers the list with the script after its first interactive render, and sends its options again whenever one
    /// of them changes. A registration the browser never received is tried again on the next render.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitSortableJsOptions))]
    private async Task SyncBrowserLayerAsync()
    {
        var options = JsOptions();
        if (options == _appliedOptions) return;

        bool registered = _appliedOptions is not null;
        _appliedOptions = options;
        if (registered)
        {
            await InvokeJsVoidAsync(() => _js.BitSortableConfigure(RootElement, options));
            return;
        }

        // Renders that come while the registration is out neither wait for it nor register again: the one that
        // started it sends the options as they are by then.
        if (_registering) return;
        _registering = true;
        try
        {
            _dotnetObj ??= DotNetObjectReference.Create(this);
            if (!await InvokeJsVoidAsync(() => _js.BitSortableRegister(RootElement, _dotnetObj, options)) && ReferenceEquals(_appliedOptions, options))
                _appliedOptions = null;
        }
        catch
        {
            // A registration that failed - the script's import (a 404 during a deploy, a lost connection), or the call
            // itself - is thrown to the nearest ErrorBoundary as ever, and tried again on the next render rather than
            // leaving the list without its script for good.
            if (ReferenceEquals(_appliedOptions, options)) _appliedOptions = null;
            throw;
        }
        finally
        {
            _registering = false;
        }
    }

    /// <summary>Calls the script, ignoring a browser that is gone; <c>false</c> when the call did not reach it.</summary>
    private async ValueTask<bool> InvokeJsVoidAsync(Func<ValueTask> call)
    {
        if (IsDisposing) return false;
        try
        {
            await call();
            return true;
        }
        catch (JSDisconnectedException) { return false; }
        catch (ObjectDisposedException) { return false; }
        catch (TaskCanceledException) { return false; }
    }

    /// <summary>Reads <paramref name="text"/> out to screen readers through the script's live region.</summary>
    private ValueTask<bool> AnnounceAsync(string text) => InvokeJsVoidAsync(() => _js.BitSortableAnnounce(text));

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (IsDisposing) return;

        // The script adds the page-level listeners every list shares as it loads, so it is loaded before the list
        // registers with it (a failed load never fails the render: the registration below tries it again).
        if (firstRender) await _js.EnsureScriptAsync("Bit.BlazorUI.Extras", "BitSortable");

        if (IsDisposing) return;

        await SyncBrowserLayerAsync();

        if (_playFlipOnRender)
        {
            _playFlipOnRender = false;
            await InvokeJsVoidAsync(() => _js.BitSortablePlay(RootElement, Animation));
        }
    }



    // ----- IBitSortableZone ----------------------------------------------

    string IBitSortableZone.Id => _Id;
    string? IBitSortableZone.GroupName => Group;
    bool IBitSortableZone.AllowSort => AllowSort;
    bool IBitSortableZone.RevertOnSpill => RevertOnSpill;
    bool IBitSortableZone.IsSwap => Swap;
    bool IBitSortableZone.ConfirmsDrop => ConfirmDrop is not null;
    bool IBitSortableZone.CanReceiveFrom(IBitSortableZone source) => CanReceiveFrom(source);
    bool IBitSortableZone.Accepts(object item) => item is TItem;
    bool IBitSortableZone.TakesDrag(BitSortableDragContext ctx) => !IsDisposing && HandlesDrag(ctx);
    bool IBitSortableZone.LetsGo(BitSortableDragContext ctx) => LetsGo(ctx);

    private int Count => Items?.Count ?? 0;
    int IBitSortableZone.Count => Count;

    bool IBitSortableZone.Same(object? x, object? y) => _identity.Same(x, y);

    int IBitSortableZone.IndexOf(object item) => item is TItem t ? IndexOfItem(t) : -1;

    object? IBitSortableZone.ItemAt(int index) => Items![index];

    void IBitSortableZone.Insert(int index, object item) => Items!.Insert(index, (TItem)item);

    void IBitSortableZone.RemoveAt(int index) => Items!.RemoveAt(index);

    void IBitSortableZone.Replace(int index, object item) => Items![index] = (TItem)item;

    void IBitSortableZone.SetItems(object?[] items)
    {
        // The page may have taken the list away, or replaced it with one that cannot change, since: nothing to put back.
        if (Items is not { IsReadOnly: false } list) return;
        list.Clear();
        foreach (var item in items) list.Add((TItem)item!);
    }

    int IBitSortableZone.ApplyOver(BitSortableDragContext ctx, int index, out BitSortableMoveInfo? change)
    {
        change = null;
        return IsDisposing || !HandlesDrag(ctx) ? Refused : ApplyOver(ctx, index, out change);
    }

    void IBitSortableZone.ClearSelection()
    {
        if (_selected.Count == 0) return;
        _selected.Clear();
        _selectionChanged = true;
        StateHasChanged();
    }

    Task IBitSortableZone.PublishSelectionIfChangedAsync() => _selectionChanged ? PublishSelectionAsync() : Task.CompletedTask;

    object IBitSortableZone.CloneItem(object item)
    {
        if (Clone is not null) return Clone((TItem)item)!;
        if (item is ICloneable cloneable) return cloneable.Clone();
        return item; // value types / strings: safe to share
    }

    bool IBitSortableZone.ClonesByFactory => Clone is not null;

    bool IBitSortableZone.CommitPlaceholder(BitSortableDragContext ctx) => CommitPlaceholder(ctx);

    Task IBitSortableZone.CaptureFlipAsync() => CaptureFlipAsync();

    void IBitSortableZone.RequestFlipPlay() => RequestFlipPlay();

    void IBitSortableZone.Rerender() => StateHasChanged();

    async Task IBitSortableZone.NotifyItemsChangedAsync()
    {
        StateHasChanged();
        if (ItemsChanged.HasDelegate && Items is not null)
            await ItemsChanged.InvokeAsync(Items);
    }

    void IBitSortableZone.EndDrag()
    {
        var items = Items;
        if (_selected.Count > 0 && items is not null)
        {
            int before = _selected.Count;
            _selected.IntersectWith(new HashSet<TItem>(items, _identity));
            if (_selected.Count != before) _selectionChanged = true;
        }
    }

    Task IBitSortableZone.RaiseEventAsync(BitSortableEventType type, BitSortableMoveInfo info)
        => RaiseEventAsync(type, info);

    Task IBitSortableZone.PassOnAsync(Exception exception) => PassOnAsync(exception);

    Task<BitSortableStartResult?> IBitSortableZone.StartAsync(string key, string input) => StartAsync(key, input);

    Task<int> IBitSortableZone.OverAsync(int index, int drag) => OverAsync(index, drag);

    Task<string?> IBitSortableZone.EndAsync(string how, int drag) => EndAsync(how, drag);

    Task<BitSortableKeyResult> IBitSortableZone.KeyAsync(string key, string action, string? list, int index, int drag) => KeyAsync(key, action, list, index, drag);

    string? IBitSortableZone.Label => Label;

    BitSortableStrings IBitSortableZone.Strings => EffectiveStrings;

    string IBitSortableZone.LabelOf(object item) => item is TItem t ? LabelOf(t) : item.ToString() ?? string.Empty;

    string? IBitSortableZone.KeyOfItem(object item) => item is TItem t ? KeyOf(t) : null;

    void IBitSortableZone.Focus(string? key)
    {
        if (key is null || key == _activeKey) return;
        _activeKey = key;
        StateHasChanged();
    }

    bool IBitSortableZone.AllowsDropAt(BitSortableDragContext ctx, int newIndex) => AllowsDrop(ctx, newIndex);

    bool IBitSortableZone.AllowsMove(BitSortableDragContext ctx, BitSortableMoveInfo info) => AllowsMove(ctx, info);

    private Task RaiseEventAsync(BitSortableEventType type, BitSortableMoveInfo info)
    {
        var callback = type switch
        {
            BitSortableEventType.Start => OnStart,
            BitSortableEventType.End => OnEnd,
            BitSortableEventType.Add => OnAdd,
            BitSortableEventType.Remove => OnRemove,
            BitSortableEventType.Update => OnUpdate,
            BitSortableEventType.Sort => OnSort,
            BitSortableEventType.Change => OnChange,
            _ => default
        };
        if (!callback.HasDelegate) return Task.CompletedTask;

        return callback.InvokeAsync(ArgsOf(info));
    }

    private static BitSortableEventArgs<TItem> ArgsOf(BitSortableMoveInfo info)
    {
        var item = (TItem)info.Item;
        return new BitSortableEventArgs<TItem>
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
            Canceled = info.Canceled,
            // A swap partner is always of the type of both lists it is traded between: a list picks as a partner only an
            // item the list the dragged item is in accepts (UpdateSwapTarget), and takes the dragged item only when it
            // is of its own type (CanAccept). So a swap never reports a partner the handler cannot read.
            IsSwap = info.SwappedWith is TItem,
            SwappedWith = info.SwappedWith is TItem partner ? partner : default
        };
    }

    async Task<bool> IBitSortableZone.ConfirmAsync(BitSortableMoveInfo info)
        => ConfirmDrop is null || info.Item is not TItem || await ConfirmDrop(ArgsOf(info));



    // ----- disposal ------------------------------------------------------

    /// <summary>
    /// Ends any drag this list is the source of, leaves any other drag this list was part of, and unregisters it from
    /// the script.
    /// </summary>
    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        try
        {
            // A list that never resolved its coordinator never joined it, a drag or the script: there is nothing to leave.
            if (_service is null) return;

            _service.Zones.Remove(this);

            // Raises the app's handlers, which may throw: the list leaves the script all the same.
            await LeaveDragAsync();
        }
        finally
        {
            var registered = _appliedOptions is not null;
            await _dotnetObj.DisposeAfterAsync(async () =>
            {
                if (registered) await _js.BitSortableUnregister(RootElement);
            });

            await base.DisposeAsync(disposing);
        }
    }

    private async Task LeaveDragAsync()
    {
        if (Service.Context is not { } ctx) return;

        if (ReferenceEquals(ctx.Source, this) || ReferenceEquals(ctx.CurrentZone, this))
        {
            // The list the item is dragged from is gone: the drag ends where it is, its pending preview dropped, and
            // is reported like the end of any other drag - OnEnd still follows OnStart. What it already did stays - a
            // live reorder changed the bound list, which the app may save on ItemsChanged or OnUpdate - unless the
            // list it is in has a ConfirmDrop: that is not asked here, as the page may be going away, and a dialog it
            // opens with it, which would leave this disposal waiting for ever. A drag it could not confirm is not
            // kept: what it did is put back, as a refusal would.
            BitSortableDragOutcome? outcome = null;
            await Service.Gate.WaitAsync();
            try
            {
                if (ReferenceEquals(Service.Context, ctx) && !ctx.Finished) outcome = Finish(ctx, commit: false);
            }
            finally
            {
                Service.Gate.Release();
            }

            if (outcome is null) return;
            await ConfirmAndReportAsync(outcome, ask: false);

            // This list is gone, so it cannot raise what the handlers threw as its own: its disposal throws it.
            if (ctx.TakeFailure() is { } failure) ExceptionDispatchInfo.Throw(failure);
            return;
        }

        if (ReferenceEquals(ctx.PlaceholderZone, this))
        {
            // The list previewing the drop is gone: the original, hidden while it was previewed here, has to show again
            // in the list it is still dragged from - sliding back in, as it does when the pointer leaves a list.
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

    /// <summary>
    /// Where <paramref name="item"/> is in the list, found by its identity (<see cref="ItemKey"/>); <c>-1</c> when it is
    /// not there.
    /// </summary>
    private int IndexOfItem(TItem item)
    {
        var items = Items;
        if (items is null) return -1;
        if (_identity.Key is not { } key) return items.IndexOf(item);

        // The key of the item looked for, taken once rather than for every item it is compared with.
        if (item is null)
        {
            for (int i = 0; i < items.Count; i++)
                if (items[i] is null) return i;
            return -1;
        }
        var wanted = key(item);
        for (int i = 0; i < items.Count; i++)
            if (items[i] is { } candidate && Equals(key(candidate), wanted)) return i;
        return -1;
    }

    /// <summary>
    /// How many of <paramref name="block"/> - a multi-drag's items - are in this list: the list's items are hashed once,
    /// rather than the list searched for each item of the block, as this runs for every list the pointer moves over.
    /// <paramref name="unfit"/> tells that the block could not come in whole: an item is not of this list's type, or two
    /// are the same to this list's <see cref="ItemKey"/> (the source may tell its items apart by another one).
    /// </summary>
    private int CountIn(List<object> block, out bool unfit)
    {
        unfit = false;
        var items = Items;
        if (items is null) return 0;
        var here = new HashSet<TItem>(items, _identity);
        var incoming = new HashSet<TItem>(_identity);
        int count = 0;
        foreach (var item in block)
        {
            if (item is not TItem t || !incoming.Add(t)) unfit = true;
            else if (here.Contains(t)) count++;
        }
        return count;
    }

    /// <summary>The value an item's element is keyed by (<c>@key</c>): its <see cref="ItemKey"/>, or the item itself.</summary>
    private object? ElementKey(TItem item) => _identity.Key is { } key && item is not null ? key(item) : item;

    /// <summary>
    /// Tells items apart the way their elements are keyed: by the key <see cref="ItemKey"/> gives them, or - without
    /// one - as the items themselves compare.
    /// </summary>
    private sealed class Identity(Func<TItem, object>? key) : IEqualityComparer<TItem>
    {
        public static readonly Identity Default = new(null);

        // Replaced in place (never on Default) by an ItemKey that is the same code as the one before: a lambda the
        // page writes inline is a new delegate on every render, and keys the items as the last one did.
        public Func<TItem, object>? Key
        {
            get => key;
            set => key = value;
        }

        /// <summary>Whether <paramref name="x"/> and <paramref name="y"/> are the same item; values that are not items compare by <c>Equals</c>.</summary>
        public bool Same(object? x, object? y) => x is TItem a && y is TItem b ? Equals(a, b) : object.Equals(x, y);

        public bool Equals(TItem? x, TItem? y)
        {
            if (key is null) return Comparer.Equals(x!, y!);
            if (x is null || y is null) return x is null && y is null;
            return object.Equals(key(x), key(y));
        }

        public int GetHashCode(TItem obj)
        {
            if (obj is null) return 0;
            return key is null ? Comparer.GetHashCode(obj) : key(obj)?.GetHashCode() ?? 0;
        }
    }

    /// <summary>Compares boxed items the way <c>@key</c> compares the items themselves.</summary>
    private sealed class ItemComparer(Identity identity) : IEqualityComparer<object>
    {
        bool IEqualityComparer<object>.Equals(object? x, object? y) => identity.Same(x, y);

        int IEqualityComparer<object>.GetHashCode(object obj)
            => obj is TItem item ? identity.GetHashCode(item) : obj.GetHashCode();
    }
}
