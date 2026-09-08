using System.Globalization;
using System.Runtime.CompilerServices;

namespace Bit.BlazorUI;

/// <summary>
/// The AccordionList component is an advanced version of the BitAccordion that renders a list of expandable/collapsible
/// items (panels) from a single collection. It manages the expand/collapse state of all the items and supports
/// single-expand (default) and multiple-expand modes.
/// </summary>
public partial class BitAccordionList<TItem> : BitComponentBase where TItem : class
{
    // The keys the header of an item answers on top of the Tab key. They are suppressed on a listener of
    // the browser's own, since Blazor's preventDefault directive cannot be decided per key, and a header
    // that moves the focus while the page scrolls under it moves the reader twice.
    private static readonly string[] _navigationKeys = ["ArrowDown", "ArrowUp", "Home", "End"];

    private int _optionKeySeed;
    private bool _isToggling;
    private bool _oldMultiple;
    private bool _hasRendered;
    private bool _pendingBoundKeysPush;
    private bool _preventKeysRegistered;
    private bool _collectingOptionOrder;
    private string? _togglingKey;
    private List<TItem> _items = [];
    private List<TItem>? _oldItems;
    private string? _internalExpandedKey;
    private List<string> _internalExpandedKeys = [];
    private BitAccordionListClassStyles? _oldClasses;
    private BitAccordionListClassStyles? _oldStyles;
    private readonly List<TItem> _optionOrder = [];
    private readonly List<TItem> _pendingScrolls = [];
    private readonly HashSet<string> _expandedKeys = new(StringComparer.Ordinal);
    // The order the keys were expanded in, which is what MaxExpanded closes the oldest panel by. It is kept
    // beside the set rather than in it, since a HashSet has no order of its own.
    private readonly List<string> _expandOrder = [];
    private readonly Dictionary<TItem, string> _fallbackKeys = new(ReferenceComparer.Instance);
    private readonly Dictionary<TItem, _BitAccordionListItem<TItem>> _itemRefs = new(ReferenceComparer.Instance);
    internal BitAccordionClassStyles? _itemClasses;
    internal BitAccordionClassStyles? _itemStyles;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// The custom template to render beside the header of each item, outside of the toggle button and of the
    /// heading it sits in, so that it can hold its own interactive elements (a menu, a delete button, a switch).
    /// Used when an item does not provide its own actions.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? ActionsTemplate { get; set; }

    /// <summary>
    /// The color kind of the background of all the accordion items.
    /// </summary>
    [Parameter] public BitColorKind? Background { get; set; }

    /// <summary>
    /// The color kind of the border of all the accordion items.
    /// </summary>
    [Parameter] public BitColorKind? Border { get; set; }

    /// <summary>
    /// The content of the AccordionList, composed of BitAccordionListOption components.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the AccordionList.
    /// </summary>
    [Parameter] public BitAccordionListClassStyles? Classes { get; set; }

    /// <summary>
    /// Allows the expanded item to be collapsed again from its own header.
    /// <br />
    /// The default value is <strong>true</strong>.
    /// </summary>
    /// <remarks>
    /// Setting it to false keeps one item open at all times: the header of the last expanded item reports
    /// itself as <c>aria-disabled</c>, the way the WAI-ARIA authoring practices ask a header that cannot
    /// collapse its panel to, and no longer answers the pointer or the keyboard.
    /// <br />
    /// It is the header that is closed off, not the list itself: the <see cref="Collapse(string)"/>,
    /// <see cref="Toggle(string)"/> and <see cref="CollapseAll"/> methods still drive the AccordionList, and
    /// nothing is expanded on its behalf, so a list that starts with everything collapsed stays that way until
    /// something is opened.
    /// </remarks>
    [Parameter] public bool Collapsible { get; set; } = true;

    /// <summary>
    /// The custom template to render the body (content) of each item. Used when an item does not provide its own body.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? BodyTemplate { get; set; }

    /// <summary>
    /// The default expanded key in single-expand mode (used when <see cref="ExpandedKey"/> is not set).
    /// </summary>
    [Parameter] public string? DefaultExpandedKey { get; set; }

    /// <summary>
    /// The default expanded keys in multiple-expand mode (used when <see cref="ExpandedKeys"/> is not set).
    /// </summary>
    [Parameter] public IEnumerable<string>? DefaultExpandedKeys { get; set; }

    /// <summary>
    /// The expanded key in single-expand mode. (two-way bound)
    /// </summary>
    [Parameter, TwoWayBound] public string? ExpandedKey { get; set; }

    /// <summary>
    /// The expanded keys in multiple-expand mode. (two-way bound)
    /// </summary>
    [Parameter, TwoWayBound] public IEnumerable<string>? ExpandedKeys { get; set; }

    /// <summary>
    /// Gets or sets the icon to show in place of the expander icon of all items while they are expanded, using
    /// custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="ExpandedExpanderIconName"/> when both are set.
    /// Setting either of them also turns the rotation of the expander icon off, since a swapped icon already
    /// reports the state on its own. Can be overridden per item.
    /// </summary>
    [Parameter] public BitIconInfo? ExpandedExpanderIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon, from the built-in Fluent UI icons, to show in place of the expander
    /// icon of all items while they are expanded.
    /// Setting it also turns the rotation of the expander icon off. Can be overridden per item.
    /// </summary>
    [Parameter] public string? ExpandedExpanderIconName { get; set; }

    /// <summary>
    /// Gets or sets the icon to display as the expander of all items using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="ExpanderIconName"/> when both are set.
    /// Can be overridden per item.
    /// </summary>
    [Parameter] public BitIconInfo? ExpanderIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display as the expander of all items from the built-in Fluent UI icons.
    /// Can be overridden per item.
    /// </summary>
    [Parameter] public string? ExpanderIconName { get; set; }

    /// <summary>
    /// Gets or sets the side of the header the expander icon of all the items sits on.
    /// <br />
    /// The default value is <see cref="BitIconPosition.End"/>.
    /// </summary>
    [Parameter] public BitIconPosition? ExpanderIconPosition { get; set; }

    /// <summary>
    /// The custom template to render in place of the expander icon of each item, leaving the rest of the header
    /// as it is. Used when an item does not provide its own expander template.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? ExpanderTemplate { get; set; }

    /// <summary>
    /// Opens the panel of every item while the page is being printed, so that a collapsed section is not left
    /// out of the paper as a bare header.
    /// </summary>
    /// <remarks>
    /// Content that is not in the DOM at all cannot be printed by any of this: a <see cref="LazyContent"/>
    /// panel that has never been opened, and every collapsed panel of a list that uses
    /// <see cref="UnmountOnCollapse"/>, are still printed as a bare header.
    /// </remarks>
    [Parameter] public bool ExpandOnPrint { get; set; }

    /// <summary>
    /// The custom content to render in place of the items when the list has none.
    /// </summary>
    /// <remarks>
    /// A list built from <see cref="Options"/> or <see cref="ChildContent"/> only knows it is empty once its
    /// options have had their turn to register, so the empty content of one takes the render after the first
    /// rather than the first itself; a list built from <see cref="Items"/> shows it right away.
    /// </remarks>
    [Parameter] public RenderFragment? EmptyContent { get; set; }

    /// <summary>
    /// The space (gap) in pixels between the accordion items.
    /// </summary>
    [Parameter, ResetStyleBuilder] public int? Gap { get; set; }

    /// <summary>
    /// The custom template to render the header of each item. Replaces the default Title/Description header.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? HeaderTemplate { get; set; }

    /// <summary>
    /// Gets or sets the heading level (aria-level) reported for the header of every item, so that the list
    /// takes its right place in the heading outline of the page.
    /// <br />
    /// The default value is <strong>3</strong>, and the value is clamped to the 1..6 range.
    /// </summary>
    [Parameter] public int? HeadingLevel { get; set; }

    /// <summary>
    /// Removes the expander icon from the header of all the items. Can be overridden per item.
    /// </summary>
    [Parameter] public bool HideExpanderIcon { get; set; }

    /// <summary>
    /// The collection of items to render in the AccordionList.
    /// </summary>
    [Parameter] public IEnumerable<TItem> Items { get; set; } = [];

    /// <summary>
    /// Delays the first render of the content of each item until it is expanded for the first time. The content
    /// stays in the DOM afterwards, so the state it holds survives a collapse.
    /// </summary>
    [Parameter] public bool LazyContent { get; set; }

    /// <summary>
    /// Gets or sets the maximum height of the content of every item (any CSS length), beyond which the content
    /// scrolls inside the item instead of growing it.
    /// </summary>
    [Parameter] public string? MaxHeight { get; set; }

    /// <summary>
    /// Gets or sets the greatest number of items that can be expanded at the same time in multiple-expand
    /// mode. Expanding one more closes the panel that has been open the longest.
    /// </summary>
    /// <remarks>
    /// Nothing is turned away: a click always opens the panel it was aimed at, which is what tells this apart
    /// from a limit that leaves a header answering nothing. A value below 1 is no limit at all, and the cap
    /// applies to <see cref="ExpandAll"/> and to the default and the bound keys as well - the ones beyond it
    /// are the ones dropped. It means nothing outside of <see cref="Multiple"/>, where one panel is the limit
    /// already.
    /// </remarks>
    [Parameter] public int? MaxExpanded { get; set; }

    /// <summary>
    /// Enables the multiple-expand mode in which more than one item can be expanded at the same time.
    /// <see cref="MaxExpanded"/> caps how many of them may be.
    /// </summary>
    [Parameter, ResetClassBuilder] public bool Multiple { get; set; }

    /// <summary>
    /// Moves the focus between the headers of the items with the ArrowUp, ArrowDown, Home and End keys, which
    /// the WAI-ARIA authoring practices offer as an addition to the Tab key rather than in place of it.
    /// <br />
    /// The default value is <strong>true</strong>.
    /// </summary>
    /// <remarks>
    /// Only the headers answer these keys: the same keys pressed inside the panel of an item belong to whatever
    /// the panel holds and are left alone. The navigation wraps around at both ends of the list and skips the
    /// items that are disabled.
    /// </remarks>
    [Parameter] public bool Navigable { get; set; } = true;

    /// <summary>
    /// Removes the default border of all the accordion items and gives a background color to their body.
    /// </summary>
    [Parameter] public bool NoBorder { get; set; }

    /// <summary>
    /// Removes the <c>region</c> role from the panel of every item, leaving it a plain container.
    /// </summary>
    /// <remarks>
    /// The role names the panel as a landmark, which helps a screen reader user find their way back to the
    /// content of a panel that holds headings or another accordion. The WAI-ARIA authoring practices ask for it
    /// to be dropped where it would flood the page with landmarks instead - more than about six panels that can
    /// all be open at the same time - which is what this is for.
    /// </remarks>
    [Parameter] public bool NoContentRegion { get; set; }

    /// <summary>
    /// Keeps the expander icon of every item still instead of turning it over when the item is expanded.
    /// </summary>
    [Parameter] public bool NoExpanderRotation { get; set; }

    /// <summary>
    /// Stops the keyboard navigation of <see cref="Navigable"/> at the two ends of the list instead of
    /// wrapping it around from the last header to the first and back.
    /// </summary>
    /// <remarks>
    /// The two are the <c>linear</c> and the <c>circular</c> navigation other libraries offer: wrapping
    /// keeps the arrow keys moving forever, while stopping tells the reader where the list ends. The Home
    /// and End keys still reach both ends either way.
    /// </remarks>
    [Parameter] public bool NoNavigationLoop { get; set; }

    /// <summary>
    /// The callback that is called when an item is collapsed.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnCollapse { get; set; }

    /// <summary>
    /// The callback that is called when an item is expanded.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnExpand { get; set; }

    /// <summary>
    /// The callback that is called when the header of an item is clicked.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnItemClick { get; set; }

    /// <summary>
    /// The callback that is called when an item is toggled (expanded or collapsed).
    /// </summary>
    [Parameter] public EventCallback<TItem> OnToggle { get; set; }

    /// <summary>
    /// Callback invoked before an item expands or collapses, letting the change be cancelled.
    /// </summary>
    /// <remarks>
    /// Set <c>Cancel</c> on the provided <see cref="BitAccordionListToggleArgs{TItem}"/> to leave the item as it
    /// is, and read its <c>Item</c>, <c>Key</c>, <c>IsExpanding</c> and <c>Reason</c> to tell an expansion from a
    /// collapse and a click on a header from an <see cref="Expand(string)"/>, <see cref="Collapse(string)"/>,
    /// <see cref="Toggle(string)"/>, <see cref="ExpandAll"/> or <see cref="CollapseAll"/> call. Since the callback
    /// is awaited, it can also run asynchronous work first, and nothing else toggles the list while it is running.
    /// <br />
    /// The implicit collapse of the previously expanded item in single-expand mode is part of the expansion that
    /// caused it and is not offered here; it is still reported through <see cref="OnCollapse"/> and
    /// <see cref="OnToggle"/>.
    /// </remarks>
    [Parameter] public EventCallback<BitAccordionListToggleArgs<TItem>> OnToggling { get; set; }

    /// <summary>
    /// Alias of the ChildContent.
    /// </summary>
    [Parameter] public RenderFragment? Options { get; set; }

    /// <summary>
    /// Names and selectors of the custom input type properties.
    /// </summary>
    [Parameter] public BitAccordionListNameSelectors<TItem>? NameSelectors { get; set; }

    /// <summary>
    /// Leaves every item where it is: the headers keep their colors and their place in the tab order, but they
    /// no longer answer the pointer or the keyboard. Can be overridden per item.
    /// </summary>
    /// <remarks>
    /// This is the list whose panels have to stay as they are rather than the one that is turned off, so the
    /// headers report themselves as <c>aria-disabled</c> without being greyed out the way
    /// <see cref="BitComponentBase.IsEnabled"/> greys them. <see cref="OnItemClick"/> still reports the click,
    /// and the public methods still drive the list.
    /// </remarks>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>
    /// Brings the item that has just been expanded into view, so that a panel opened at the bottom of the
    /// window is not left off the screen it was opened on.
    /// </summary>
    /// <remarks>
    /// The item is moved as little as the browser can move it, so nothing happens to one that is already in
    /// view, and the scroll is instant rather than smooth for a reader who has asked for less motion. It
    /// covers the ways a single panel opens - a click on its header, <see cref="Expand(string)"/>,
    /// <see cref="Toggle(string)"/> and the bound keys - and runs when the panel is put on screen rather than
    /// when the transition that opens it ends. <see cref="ExpandAll"/> scrolls to nothing: there is no one
    /// panel it opened.
    /// </remarks>
    [Parameter] public bool ScrollIntoViewOnExpand { get; set; }

    /// <summary>
    /// Gets or sets the size of all the accordion items, which drives the padding of the headers and of the
    /// contents and the size of the titles.
    /// <br />
    /// The default value is <see cref="BitSize.Medium"/>.
    /// </summary>
    [Parameter] public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the AccordionList.
    /// </summary>
    [Parameter] public BitAccordionListClassStyles? Styles { get; set; }

    /// <summary>
    /// The custom template to render in place of the title of each item, leaving the rest of the header as it is.
    /// Used when an item does not provide its own title template.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? TitleTemplate { get; set; }

    /// <summary>
    /// Gets or sets the duration of the expand/collapse transition of every item in milliseconds, overriding the
    /// duration the theme provides. A reduced-motion preference still collapses it, unless the ForceAnimation
    /// parameter opts out of that.
    /// </summary>
    [Parameter] public int? TransitionDuration { get; set; }

    /// <summary>
    /// Removes the content of an item from the DOM while it is collapsed, so that nothing it holds keeps running
    /// behind a closed header.
    /// </summary>
    [Parameter] public bool UnmountOnCollapse { get; set; }



    /// <summary>
    /// Expands all the items (only effective in multiple-expand mode). Disabled items are left as they are,
    /// since their headers could not close again what would be opened for them.
    /// </summary>
    public async Task ExpandAll()
    {
        if (Multiple is false) return;

        var changed = false;

        foreach (var item in _items.ToArray())
        {
            if (GetIsEnabled(item) is false) continue;

            // The cap is a cap on the whole list, so ExpandAll stops at it rather than opening every panel
            // and letting each one close the one before it.
            if (_MaxExpanded is int max && _expandedKeys.Count >= max) break;

            var key = GetItemKey(item);
            if (key.HasNoValue() || _expandedKeys.Contains(key!)) continue;

            changed |= await ApplyToggle(item, key!, true, BitAccordionToggleReason.Method);
        }

        if (changed is false) return;

        await UpdateBoundKeys();
        await RefreshAndRender();
    }

    /// <summary>
    /// Collapses all the expanded items, the disabled ones included.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="ExpandAll"/>, which cannot open what is turned off, this one closes every panel: a
    /// disabled item whose panel was opened by a default value would otherwise be left open with no way of
    /// closing it, since its own header answers nothing.
    /// </remarks>
    public async Task CollapseAll()
    {
        var changed = false;

        foreach (var item in _items.ToArray())
        {
            var key = GetItemKey(item);
            if (key.HasNoValue() || _expandedKeys.Contains(key!) is false) continue;

            changed |= await ApplyToggle(item, key!, false, BitAccordionToggleReason.Method);
        }

        // Keys that no longer map to an item of the list are dropped along with the rest, so a collapsed list
        // does not keep reporting them through the two-way bound ExpandedKey(s).
        var orphans = _expandedKeys.Where(k => FindItem(k) is null).ToArray();
        if (orphans.Length > 0)
        {
            foreach (var orphan in orphans) RemoveExpandedKey(orphan);
            changed = true;
        }

        if (changed is false) return;

        await UpdateBoundKeys();
        await RefreshAndRender();
    }

    /// <summary>
    /// Expands the item with the provided key. Does nothing if it is already expanded or if no item carries
    /// that key. In single-expand mode the currently expanded item is collapsed along the way.
    /// </summary>
    /// <remarks>
    /// A call of its own is not turned away by <see cref="BitComponentBase.IsEnabled"/>, by
    /// <see cref="ReadOnly"/> or by <see cref="Collapsible"/>: what those close off is the way in from the
    /// header, not the one the app itself uses.
    /// </remarks>
    public Task Expand(string key) => SetExpandedByKey(key, true);

    /// <summary>
    /// Collapses the item with the provided key. Does nothing if it is already collapsed or if no item carries
    /// that key.
    /// </summary>
    /// <remarks>
    /// Not turned away by <see cref="BitComponentBase.IsEnabled"/>, <see cref="ReadOnly"/> or
    /// <see cref="Collapsible"/>; see <see cref="Expand(string)"/>.
    /// </remarks>
    public Task Collapse(string key) => SetExpandedByKey(key, false);

    /// <summary>
    /// Expands the item with the provided key if it is collapsed and collapses it if it is expanded.
    /// </summary>
    /// <remarks>
    /// Not turned away by <see cref="BitComponentBase.IsEnabled"/>, <see cref="ReadOnly"/> or
    /// <see cref="Collapsible"/>; see <see cref="Expand(string)"/>.
    /// </remarks>
    public Task Toggle(string key)
    {
        return key.HasNoValue() ? Task.CompletedTask : SetExpandedByKey(key, _expandedKeys.Contains(key) is false);
    }

    /// <summary>
    /// Reports whether the item with the provided key is currently expanded.
    /// </summary>
    public bool IsExpanded(string? key) => key.HasValue() && _expandedKeys.Contains(key!);

    /// <summary>
    /// Returns the keys of the currently expanded items, in the order of the items of the list.
    /// </summary>
    public IReadOnlyList<string> GetExpandedKeys() => GetOrderedExpandedKeys();

    /// <summary>
    /// Gives the focus to the header of the item with the provided key.
    /// </summary>
    public async Task FocusItem(string key)
    {
        var item = FindItem(key);
        if (item is null) return;

        await InvokeAsync(() => FocusItemCore(item));
    }

    /// <summary>
    /// Gives the focus to the header of the first item of the list that can take it.
    /// </summary>
    public async Task FocusAsync()
    {
        var item = _items.FirstOrDefault(GetIsEnabled);
        if (item is null) return;

        await InvokeAsync(() => FocusItemCore(item));
    }



    internal void RegisterOption(BitAccordionListOption option)
    {
        if (option.Key.HasNoValue())
        {
            // Use a monotonic seed so keys remain unique even after removals, and guard
            // against colliding with any existing explicit keys.
            var key = _optionKeySeed++.ToString(CultureInfo.InvariantCulture);
            while (_items.Any(i => GetItemKey(i) == key))
            {
                key = _optionKeySeed++.ToString(CultureInfo.InvariantCulture);
            }
            option.Key = key;
        }

        var item = (option as TItem)!;

        _items.Add(item);

        if (ShouldExpandOnRegister(option.Key!, option.IsExpanded))
        {
            AddExpandedKey(option.Key!);
            _internalExpandedKeys = GetOrderedExpandedKeys();
            _internalExpandedKey = _internalExpandedKeys.FirstOrDefault();

            // The bound value is pushed as well, so a page that binds ExpandedKey(s) is told about the option
            // that opened itself on registration rather than being left with a stale value. It is deferred to
            // the end of the render, since the registration runs in the middle of one.
            _pendingBoundKeysPush = true;
        }

        StateHasChanged();
    }

    private bool ShouldExpandOnRegister(string key, bool optionIsExpanded)
    {
        // The controlled values take precedence over the default values which take
        // precedence over the option's own IsExpanded parameter.
        if (Multiple)
        {
            if (ExpandedKeysHasBeenSet)
            {
                return ExpandedKeys?.Contains(key) ?? false;
            }

            if (DefaultExpandedKeys is not null)
            {
                return DefaultExpandedKeys.Contains(key);
            }

            return optionIsExpanded;
        }

        if (ExpandedKeyHasBeenSet)
        {
            return ExpandedKey == key;
        }

        if (DefaultExpandedKey.HasValue())
        {
            return DefaultExpandedKey == key;
        }

        // In single-expand mode only the first expanded option wins.
        return optionIsExpanded && _expandedKeys.Count == 0;
    }

    internal async Task UnregisterOption(BitAccordionListOption option)
    {
        var item = (option as TItem)!;

        _items.Remove(item);
        _itemRefs.Remove(item);
        _fallbackKeys.Remove(item);

        var wasExpanded = false;
        if (option.Key.HasValue())
        {
            wasExpanded = RemoveExpandedKey(option.Key!);
        }

        // When a removed option was expanded, refresh the internal representations and the
        // two-way bound values so they don't keep referencing the removed key.
        if (wasExpanded)
        {
            await UpdateBoundKeys();
        }

        StateHasChanged();
    }

    // An option registers itself when it is initialized, which is the markup order only for the options that
    // were there on the first render: one added conditionally later registers behind every option that was
    // already there, wherever in the markup it sits. So the order is read back from the render itself - the
    // options render in markup order - and _items is put back into it once the render is over. It is what the
    // keyboard navigation walks and what the expanded keys are reported in, so it has to be the order the
    // reader sees rather than the order the options happened to arrive in.
    internal void BeginOptionsOrder()
    {
        // A pass that is already open is left as it is: registering an option asks the list to render again,
        // and that second render lands in the same batch as the first - so clearing here would throw away
        // the very order the options had just reported.
        if (_collectingOptionOrder) return;

        _optionOrder.Clear();
        _collectingOptionOrder = true;
    }

    internal void ReportOptionOrder(BitAccordionListOption option)
    {
        if (_collectingOptionOrder is false) return;

        var item = (option as TItem)!;

        if (_optionOrder.Any(i => ReferenceEquals(i, item))) return;

        _optionOrder.Add(item);
    }

    internal void RegisterItem(TItem item, _BitAccordionListItem<TItem> itemRef)
    {
        _itemRefs[item] = itemRef;
    }

    internal void UnregisterItem(TItem item)
    {
        _itemRefs.Remove(item);
    }



    protected override string RootElementClass => "bit-acl";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Multiple ? "bit-acl-mlt" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => Gap.HasValue ? $"gap:{Gap}px" : string.Empty);
    }

    protected override async Task OnParametersSetAsync()
    {
        BuildItemClassStyles();

        if (ChildContent is null && Options is null && Items is not null)
        {
            // The snapshot is a copy rather than the collection itself, so a page that keeps mutating the very
            // list it handed over - adding an item to it, removing one - is still noticed here. A lazy sequence
            // is walked once, so what is compared is also exactly what is kept.
            List<TItem> snapshot = [.. Items];

            if (_oldItems is null || snapshot.SequenceEqual(_oldItems) is false)
            {
                var isFirstPass = _oldItems is null;

                _oldItems = snapshot;
                _items = [.. snapshot];

                AssignItemKeys();

                // Only the very first pass falls back to the default values: a later change of the collection
                // must not throw away the state the reader has built up by opening and closing the panels.
                InitializeExpandedKeys(preserveCurrent: isFirstPass is false);
            }
        }

        // Leaving the multiple-expand mode with more than one panel open would otherwise keep them all open
        // until the next click, which is the one state a single-expand list is there to rule out.
        if (_oldMultiple && Multiple is false && _expandedKeys.Count > 1)
        {
            var kept = GetOrderedExpandedKeys().FirstOrDefault();

            ClearExpandedKeys();
            if (kept.HasValue()) AddExpandedKey(kept!);

            SyncItemsExpandedState();

            _internalExpandedKeys = GetOrderedExpandedKeys();
            _internalExpandedKey = _internalExpandedKeys.FirstOrDefault();

            // The push is deferred to the end of the render, since a parameter set is no place to call back
            // into the page that is setting them.
            _pendingBoundKeysPush = true;
        }

        _oldMultiple = Multiple;

        // React to external (controlled) changes of the bound keys.
        if (Multiple)
        {
            if (ExpandedKeysHasBeenSet && (ExpandedKeys ?? []).SequenceEqual(_internalExpandedKeys) is false)
            {
                SyncFromExpandedKeys(ExpandedKeys);

                // Not every set of keys survives the way in whole: the ones beyond MaxExpanded are dropped,
                // and so are the empty and the repeated ones. The page is told what the list actually holds
                // rather than being left with a value it does not show - and rather than being read again as
                // a change on every render that follows.
                if ((ExpandedKeys ?? []).SequenceEqual(_internalExpandedKeys) is false)
                {
                    _pendingBoundKeysPush = true;
                }
            }
        }
        else
        {
            if (ExpandedKeyHasBeenSet && _internalExpandedKey != ExpandedKey)
            {
                SyncFromExpandedKey(ExpandedKey);
            }
        }

        // A cap that arrives - or is lowered - while more panels are open than it allows closes the oldest of
        // them, the same way opening one more would have. Everything that adds a key already stops at the cap,
        // so this is only about the cap itself changing.
        if (_MaxExpanded is int max && _expandedKeys.Count > max)
        {
            foreach (var key in _expandOrder.Take(_expandedKeys.Count - max).ToArray())
            {
                RemoveExpandedKey(key);
            }

            SyncItemsExpandedState();

            _internalExpandedKeys = GetOrderedExpandedKeys();
            _internalExpandedKey = _internalExpandedKeys.FirstOrDefault();

            // The push is deferred to the end of the render, since a parameter set is no place to call back
            // into the page that is setting them.
            _pendingBoundKeysPush = true;
        }

        // Options render their items themselves and Blazor skips re-rendering them when only the
        // accordion list's own parameters (Styles, ExpandedKey(s), ...) change, so push a re-render to each one.
        RefreshOptions();

        await base.OnParametersSetAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // A list of options only knows it is empty once its options have had their turn to register, and the
        // first render is where that is learned - so the empty content it was given needs a render of its own
        // to appear in. Nothing else would ask for one: an empty list has no option to report anything.
        var showEmptyContent = _ShowEmptyContent;

        _hasRendered = true;

        if (showEmptyContent is false && _ShowEmptyContent)
        {
            StateHasChanged();
        }

        await ReorderOptions();

        if (_pendingBoundKeysPush)
        {
            _pendingBoundKeysPush = false;

            await PushBoundKeys();
        }

        await UpdatePreventedKeys();

        await ScrollPendingItemsIntoView();

        await base.OnAfterRenderAsync(firstRender);
    }

    // Puts _items back into the order the options were rendered in, which is their markup order.
    private async Task ReorderOptions()
    {
        if (_collectingOptionOrder)
        {
            _collectingOptionOrder = false;

            // A pass in which not every option had its turn says nothing about the order of the ones that
            // did, so it is thrown away rather than guessed at. Blazor hands a child its parameters again
            // only when one of them has actually changed, so a list whose options carry nothing but
            // constants - no content, no template, no handler - can render without any of them reporting.
            var complete = _optionOrder.Count == _items.Count;
            var order = complete ? _optionOrder.ToArray() : [];

            _optionOrder.Clear();

            if (complete is false || _items.SequenceEqual(order, ReferenceComparer.Instance)) return;

            _items = [.. order];

            // Nothing on screen depends on the order - the options render their own items in place - but the
            // keys the list reports do, so they are pushed again where the order changed them.
            if (GetOrderedExpandedKeys().SequenceEqual(_internalExpandedKeys) is false)
            {
                await UpdateBoundKeys();
            }

            StateHasChanged();
        }
    }

    // The navigation keys are suppressed on a listener of the browser's own, and only for a key pressed on
    // one of this list's own item headers: the same keys pressed inside a panel belong to whatever the panel
    // holds, and the ones pressed on the headers of a list nested in a panel belong to that list.
    private async Task UpdatePreventedKeys()
    {
        var wanted = Navigable && IsEnabled;

        if (wanted == _preventKeysRegistered) return;

        try
        {
            if (wanted)
            {
                await _js.BitExtrasSetPreventKeys(RootElement, _navigationKeys, ".bit-acd-hdr", ".bit-acl");
            }
            else
            {
                await _js.BitExtrasDisposePreventKeys(RootElement);
            }

            _preventKeysRegistered = wanted;
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private async Task ScrollPendingItemsIntoView()
    {
        if (_pendingScrolls.Count == 0) return;

        var items = _pendingScrolls.ToArray();
        _pendingScrolls.Clear();

        foreach (var item in items)
        {
            if (_itemRefs.TryGetValue(item, out var itemRef) is false) continue;

            var element = itemRef.GetElement();
            if (element is null) continue;

            try
            {
                await _js.BitExtrasScrollIntoView(element.Value);
            }
            catch (JSDisconnectedException) { } // we can ignore this exception here
        }
    }



    private void AssignItemKeys()
    {
        // Collect the explicit keys first so the generated keys never collide with them.
        var usedKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in _items)
        {
            var key = GetDeclaredKey(item);
            if (key.HasValue()) usedKeys.Add(key!);
        }

        var present = new HashSet<TItem>(_items, ReferenceComparer.Instance);

        foreach (var item in _fallbackKeys.Keys.ToArray())
        {
            // A key handed out earlier is kept where the item is still in the list and the key is still free,
            // so an item does not lose its expanded state only because the collection around it changed.
            if (present.Contains(item) && usedKeys.Add(_fallbackKeys[item])) continue;

            _fallbackKeys.Remove(item);
        }

        for (int i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            if (GetItemKey(item).HasValue()) continue;

            // Start from the loop index and increment until a non-colliding key is found so the
            // result stays deterministic across renders while remaining unique.
            var suffix = i;
            var candidate = suffix.ToString(CultureInfo.InvariantCulture);
            while (usedKeys.Contains(candidate))
            {
                candidate = (++suffix).ToString(CultureInfo.InvariantCulture);
            }

            SetItemKey(item, candidate);
            usedKeys.Add(candidate);
        }
    }

    private void InitializeExpandedKeys(bool preserveCurrent = false)
    {
        // Read before the set is cleared: the keys of the items that were expanded and are still in the list.
        var surviving = preserveCurrent ? GetSurvivingExpandedKeys() : null;
        var selfExpanded = GetSelfExpandedKeys();

        ClearExpandedKeys();

        // Controlled values take precedence over what was there before, which takes precedence over the
        // default values, which take precedence over the items' own IsExpanded.
        if (Multiple)
        {
            if (ExpandedKeysHasBeenSet && ExpandedKeys is not null)
            {
                AddExpandedKeys(ExpandedKeys);
            }
            else if (surviving is not null)
            {
                AddExpandedKeys(surviving);
                AddExpandedKeys(selfExpanded);
            }
            else if (DefaultExpandedKeys is not null)
            {
                AddExpandedKeys(DefaultExpandedKeys);
            }
            else
            {
                AddExpandedKeys(selfExpanded);
            }
        }
        else
        {
            string? key;

            if (ExpandedKeyHasBeenSet)
            {
                key = ExpandedKey;
            }
            else if (surviving is not null)
            {
                key = surviving.FirstOrDefault() ?? selfExpanded.FirstOrDefault();
            }
            else if (DefaultExpandedKey.HasValue())
            {
                key = DefaultExpandedKey;
            }
            else
            {
                key = selfExpanded.FirstOrDefault();
            }

            if (key.HasValue()) AddExpandedKey(key!);
        }

        SyncItemsExpandedState();

        _internalExpandedKeys = GetOrderedExpandedKeys();
        _internalExpandedKey = _internalExpandedKeys.FirstOrDefault();
    }

    private List<string> GetSurvivingExpandedKeys()
    {
        return [.. _items.Select(GetItemKey).Where(k => k.HasValue() && _expandedKeys.Contains(k!)).Select(k => k!)];
    }

    private List<string> GetSelfExpandedKeys()
    {
        return [.. _items.Where(GetIsExpanded).Select(GetItemKey).Where(k => k.HasValue()).Select(k => k!)];
    }

    private void AddExpandedKeys(IEnumerable<string> keys)
    {
        foreach (var key in keys)
        {
            if (key.HasNoValue()) continue;
            AddExpandedKey(key);
            if (Multiple is false) break;

            // The keys beyond the cap are the ones dropped, so a set of defaults or of bound keys longer
            // than MaxExpanded opens the first of them rather than none of them.
            if (_MaxExpanded is int max && _expandedKeys.Count >= max) break;
        }
    }

    // The three of them are what keeps _expandOrder - the order the panels were opened in, which is what
    // MaxExpanded closes the oldest one by - beside the set that answers whether a key is expanded at all.
    private bool AddExpandedKey(string key)
    {
        if (_expandedKeys.Add(key) is false) return false;

        _expandOrder.Add(key);

        return true;
    }

    private bool RemoveExpandedKey(string key)
    {
        if (_expandedKeys.Remove(key) is false) return false;

        _expandOrder.Remove(key);

        return true;
    }

    private void ClearExpandedKeys()
    {
        _expandedKeys.Clear();
        _expandOrder.Clear();
    }

    // A cap of its own only means something where more than one panel can be open at a time, and a value
    // below one is no cap at all rather than a list nothing can be opened in.
    private int? _MaxExpanded => (Multiple && MaxExpanded is > 0) ? MaxExpanded : null;

    // The keys that have to close for the one being opened to fit under the cap, oldest first.
    private string[] GetOverflowKeys(string key)
    {
        if (_MaxExpanded is not int max) return [];

        var overflow = _expandedKeys.Count + 1 - max;
        if (overflow <= 0) return [];

        return [.. _expandOrder.Where(k => k != key).Take(overflow)];
    }

    private void SyncItemsExpandedState()
    {
        foreach (var item in _items)
        {
            var key = GetItemKey(item);
            SetIsExpanded(item, key.HasValue() && _expandedKeys.Contains(key!));
        }
    }

    // Emits the expanded keys in a stable order (the order of _items) so the two-way bound
    // ExpandedKeys and the internal SequenceEqual comparisons stay deterministic across renders.
    private List<string> GetOrderedExpandedKeys()
    {
        var ordered = new List<string>(_expandedKeys.Count);
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var item in _items)
        {
            var key = GetItemKey(item);
            if (key.HasValue() && _expandedKeys.Contains(key!) && seen.Add(key!))
            {
                ordered.Add(key!);
            }
        }

        // Preserve any expanded keys that don't currently map to an item, in a deterministic
        // order so the bound ExpandedKeys and SequenceEqual comparisons stay stable across renders.
        foreach (var key in _expandedKeys.Where(k => seen.Contains(k) is false).OrderBy(k => k, StringComparer.Ordinal))
        {
            if (seen.Add(key)) ordered.Add(key);
        }

        return ordered;
    }

    private void SyncFromExpandedKey(string? key)
    {
        // Read before the set is replaced, so that a panel the page has just opened through the binding is
        // brought into view the way a click on its header would have brought it.
        var opened = OpenedKeysOf(key.HasValue() ? [key!] : []);

        ClearExpandedKeys();
        if (key.HasValue()) AddExpandedKey(key!);
        SyncItemsExpandedState();
        _internalExpandedKey = key;

        QueueScrollIntoView(opened);
    }

    private void SyncFromExpandedKeys(IEnumerable<string>? keys)
    {
        var opened = OpenedKeysOf(keys ?? []);

        ClearExpandedKeys();
        if (keys is not null) AddExpandedKeys(keys);
        SyncItemsExpandedState();
        _internalExpandedKeys = GetOrderedExpandedKeys();

        QueueScrollIntoView(opened);
    }

    // The keys of the incoming set that are not expanded yet - the panels the change is about to open.
    private List<string> OpenedKeysOf(IEnumerable<string> keys)
    {
        return ScrollIntoViewOnExpand is false ? [] : [.. keys.Where(k => k.HasValue() && _expandedKeys.Contains(k) is false)];
    }

    private void QueueScrollIntoView(IEnumerable<string> keys)
    {
        foreach (var key in keys)
        {
            var item = FindItem(key);
            if (item is not null) QueueScrollIntoView(item);
        }
    }

    // The scroll is left to the render that puts the panel on screen: the item it belongs to can be one
    // that is only rendered by the change being applied here.
    private void QueueScrollIntoView(TItem item)
    {
        if (ScrollIntoViewOnExpand is false) return;

        if (_pendingScrolls.Any(i => ReferenceEquals(i, item))) return;

        _pendingScrolls.Add(item);
    }

    internal async Task HandleOnItemClick(TItem item)
    {
        if (IsEnabled is false || GetIsEnabled(item) is false) return;

        await OnItemClick.InvokeAsync(item);

        await InvokeItemClick(item);

        // A read-only item still reports the click - the page can want to say why the panel is staying where
        // it is - it just does not act on it.
        if (GetItemIsReadOnly(item)) return;

        var key = GetItemKey(item);
        if (key.HasNoValue()) return;

        var expand = _expandedKeys.Contains(key!) is false;

        await ToggleItem(item, key!, expand, BitAccordionToggleReason.Click);
    }

    internal async Task HandleOnItemKeyDown(KeyboardEventArgs e, TItem item)
    {
        if (Navigable is false || IsEnabled is false) return;

        if (e.Key is not ("ArrowDown" or "ArrowUp" or "Home" or "End")) return;

        // A disabled header is out of the tab order, so the navigation walks past it rather than parking the
        // focus on something that cannot be reached by the Tab key either.
        var focusables = _items.Where(GetIsEnabled).ToList();
        if (focusables.Count == 0) return;

        var index = focusables.FindIndex(i => ReferenceEquals(i, item));
        if (index < 0) return;

        var next = e.Key switch
        {
            "ArrowDown" => index + 1,
            "ArrowUp" => index - 1,
            "Home" => 0,
            _ => focusables.Count - 1
        };

        // The navigation wraps around at both ends of the list, unless it was asked to stop there.
        if (next < 0) next = NoNavigationLoop ? 0 : focusables.Count - 1;
        else if (next >= focusables.Count) next = NoNavigationLoop ? focusables.Count - 1 : 0;

        await FocusItemCore(focusables[next]);
    }

    private async Task SetExpandedByKey(string key, bool expand)
    {
        if (key.HasNoValue()) return;

        var item = FindItem(key);
        if (item is null) return;

        if (_expandedKeys.Contains(key) == expand) return;

        await ToggleItem(item, key, expand, BitAccordionToggleReason.Method);
    }

    private async Task ToggleItem(TItem item, string key, bool expand, BitAccordionToggleReason reason)
    {
        // Read before the expansion is applied, since applying it adds the new key to the set. A cancelled
        // expansion therefore leaves the previously expanded item(s) exactly where they were.
        //
        // In single-expand mode that is every other panel; in multiple-expand mode under a MaxExpanded cap
        // it is the oldest of them, as many as the one being opened needs to fit.
        var others = expand
                   ? (Multiple ? GetOverflowKeys(key) : [.. _expandedKeys.Where(k => k != key)])
                   : [];

        if (await ApplyToggle(item, key, expand, reason) is false) return;

        if (expand) QueueScrollIntoView(item);

        // Collapse the item(s) that were expanded before.
        foreach (var otherKey in others)
        {
            if (RemoveExpandedKey(otherKey) is false) continue;

            var otherItem = FindItem(otherKey);
            if (otherItem is null) continue;

            SetIsExpanded(otherItem, false);
            await OnCollapse.InvokeAsync(otherItem);
            await OnToggle.InvokeAsync(otherItem);
        }

        await UpdateBoundKeys();

        // A toggle can affect other items too (single-expand mode collapses the previously expanded
        // item), and the click handler runs on the clicked item's renderer, so both the registered
        // options and the accordion list itself need an explicit re-render.
        await RefreshAndRender();
    }

    // Runs the cancellable OnToggling callback and, when it is not refused, moves the single item between the
    // expanded and the collapsed state. The bound keys and the re-render are left to the caller, so that a
    // batch of items - ExpandAll, CollapseAll - reports itself once rather than once per item.
    private async Task<bool> ApplyToggle(TItem item, string key, bool expand, BitAccordionToggleReason reason)
    {
        if (_expandedKeys.Contains(key) == expand) return false;

        if (OnToggling.HasDelegate)
        {
            // The callback is awaited, so a second click - or a Toggle call while a confirmation prompt is
            // still open - would otherwise start a change of its own alongside the first one.
            if (_isToggling) return false;

            _isToggling = true;
            _togglingKey = key;

            // Nothing toggles the list while the callback is running, so the header of the item it was
            // asked about says as much - aria-busy for a screen reader, a busy cursor for a pointer -
            // rather than going on looking like a toggle that answers at once.
            await RefreshAndRender();

            try
            {
                var args = new BitAccordionListToggleArgs<TItem>(item, key, expand, reason);

                await OnToggling.InvokeAsync(args);

                if (args.Cancel) return false;

                // The state can have moved on while the callback was awaited - the page can have driven the
                // bound keys itself, or disposed the list altogether.
                if (IsDisposed || _expandedKeys.Contains(key) == expand) return false;
            }
            finally
            {
                _isToggling = false;
                _togglingKey = null;

                await RefreshAndRender();
            }
        }

        if (expand)
        {
            AddExpandedKey(key);
            SetIsExpanded(item, true);
            await OnExpand.InvokeAsync(item);
        }
        else
        {
            RemoveExpandedKey(key);
            SetIsExpanded(item, false);
            await OnCollapse.InvokeAsync(item);
        }

        await OnToggle.InvokeAsync(item);

        return true;
    }

    private void RefreshOptions()
    {
        // In the Items API there are no registered options, so there is nothing to refresh.
        if ((Options ?? ChildContent) is null) return;

        foreach (var item in _items)
        {
            (item as BitAccordionListOption)?.InternalStateHasChanged();
        }
    }

    // The public methods are not called from an event handler, so nothing re-renders the component on their
    // behalf the way Blazor does after a click, and the call can come from off the render loop altogether.
    private Task RefreshAndRender()
    {
        if (IsDisposed) return Task.CompletedTask;

        return InvokeAsync(() =>
        {
            RefreshOptions();
            StateHasChanged();
        });
    }

    private async Task FocusItemCore(TItem item)
    {
        if (_itemRefs.TryGetValue(item, out var itemRef) is false) return;

        try
        {
            await itemRef.FocusAsync();
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private TItem? FindItem(string? key)
    {
        return key.HasNoValue() ? null : _items.FirstOrDefault(i => GetItemKey(i) == key);
    }

    private async Task UpdateBoundKeys()
    {
        _internalExpandedKeys = GetOrderedExpandedKeys();
        _internalExpandedKey = _internalExpandedKeys.FirstOrDefault();

        await PushBoundKeys();
    }

    private async Task PushBoundKeys()
    {
        if (Multiple)
        {
            await AssignExpandedKeys([.. _internalExpandedKeys]);
        }
        else
        {
            await AssignExpandedKey(_internalExpandedKey);
        }
    }

    private void BuildItemClassStyles()
    {
        // The two objects are handed to every BitAccordion of the list as parameters, so a new pair of them on
        // every render would re-render every item for nothing.
        if (_itemClasses is not null && ReferenceEquals(_oldClasses, Classes) && ReferenceEquals(_oldStyles, Styles)) return;

        _oldClasses = Classes;
        _oldStyles = Styles;

        _itemClasses = new BitAccordionClassStyles
        {
            Root = Classes?.Item,
            Expanded = Classes?.ItemExpanded,
            HeaderWrapper = Classes?.ItemHeaderWrapper,
            Heading = Classes?.ItemHeading,
            Header = Classes?.ItemHeader,
            Icon = Classes?.ItemIcon,
            HeaderContent = Classes?.ItemHeaderContent,
            Title = Classes?.ItemTitle,
            Description = Classes?.ItemDescription,
            ExpanderIconWrapper = Classes?.ItemExpanderIconWrapper,
            ExpanderIcon = Classes?.ItemExpanderIcon,
            ExpandedIcon = Classes?.ItemExpandedIcon,
            Actions = Classes?.ItemActions,
            ContentContainer = Classes?.ItemContentContainer,
            ContentWrapper = Classes?.ItemContentWrapper,
            Content = Classes?.ItemContent,
        };

        _itemStyles = new BitAccordionClassStyles
        {
            Root = Styles?.Item,
            Expanded = Styles?.ItemExpanded,
            HeaderWrapper = Styles?.ItemHeaderWrapper,
            Heading = Styles?.ItemHeading,
            Header = Styles?.ItemHeader,
            Icon = Styles?.ItemIcon,
            HeaderContent = Styles?.ItemHeaderContent,
            Title = Styles?.ItemTitle,
            Description = Styles?.ItemDescription,
            ExpanderIconWrapper = Styles?.ItemExpanderIconWrapper,
            ExpanderIcon = Styles?.ItemExpanderIcon,
            ExpandedIcon = Styles?.ItemExpandedIcon,
            Actions = Styles?.ItemActions,
            ContentContainer = Styles?.ItemContentContainer,
            ContentWrapper = Styles?.ItemContentWrapper,
            Content = Styles?.ItemContent,
        };
    }

    internal bool IsItemExpanded(TItem item)
    {
        var key = GetItemKey(item);
        return key.HasValue() && _expandedKeys.Contains(key!);
    }

    // The header of the item an awaited OnToggling was asked about, and only that one: the rest of the list
    // is not doing anything, it is only refusing to start something else while this one is being decided.
    internal bool IsItemBusy(TItem item)
    {
        if (_togglingKey is null) return false;

        return GetItemKey(item) == _togglingKey;
    }

    // A list built from options only knows it is empty once its options have had their turn to register, so
    // the empty content of one waits for the render after the first rather than flashing on the first.
    private bool _ShowEmptyContent => EmptyContent is not null
                                   && _items.Count == 0
                                   && ((Options ?? ChildContent) is null || _hasRendered);

    // A label on a plain container is dropped by a screen reader, so the list that carries one says what it
    // is. It is rendered before the splatted attributes, so a role the page sets itself still wins over it.
    private string? _Role => AriaLabel.HasValue() ? "group" : null;

    // The header of the one panel that has to stay open reports itself as aria-disabled, the way the WAI-ARIA
    // authoring practices ask a header whose panel cannot be collapsed to.
    internal bool GetItemIsReadOnly(TItem item)
    {
        if (GetReadOnly(item) ?? ReadOnly) return true;

        return Collapsible is false && _expandedKeys.Count <= 1 && IsItemExpanded(item);
    }

    internal bool GetItemHideExpanderIcon(TItem item)
    {
        return GetHideExpanderIcon(item) ?? HideExpanderIcon;
    }

    internal string? GetItemHeaderAriaLabel(TItem item)
    {
        if (item is BitAccordionListItem listItem)
        {
            return listItem.HeaderAriaLabel;
        }

        if (item is BitAccordionListOption listOption)
        {
            return listOption.HeaderAriaLabel;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.HeaderAriaLabel.Selector is not null)
        {
            return NameSelectors.HeaderAriaLabel.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.HeaderAriaLabel.Name);
    }

    internal RenderFragment<bool>? GetItemHeaderTemplate(TItem item)
    {
        var itemTemplate = GetHeaderTemplate(item);
        if (itemTemplate is not null) return _ => itemTemplate(item);

        if (HeaderTemplate is not null)
        {
            return _ => HeaderTemplate(item);
        }

        return null;
    }

    internal RenderFragment? GetItemTitleTemplate(TItem item)
    {
        var itemTemplate = GetTitleTemplate(item);
        if (itemTemplate is not null) return itemTemplate(item);

        return TitleTemplate is not null ? TitleTemplate(item) : null;
    }

    internal RenderFragment<bool>? GetItemExpanderTemplate(TItem item)
    {
        var itemTemplate = GetExpanderTemplate(item);
        if (itemTemplate is not null) return _ => itemTemplate(item);

        if (ExpanderTemplate is not null)
        {
            return _ => ExpanderTemplate(item);
        }

        return null;
    }

    internal RenderFragment? GetItemActions(TItem item)
    {
        var itemActions = GetActions(item);
        if (itemActions is not null) return itemActions(item);

        return ActionsTemplate is not null ? ActionsTemplate(item) : null;
    }

    internal RenderFragment? GetItemBody(TItem item)
    {
        // The option's plain inline content (ChildContent) is rendered as-is.
        if (item is BitAccordionListOption listOption && listOption.ChildContent is not null)
        {
            return listOption.ChildContent;
        }

        var body = GetBody(item);
        if (body is not null) return body(item);

        if (BodyTemplate is not null)
        {
            return BodyTemplate(item);
        }

        return null;
    }

    internal BitIconInfo? GetItemExpanderIcon(TItem item)
    {
        return GetExpanderIcon(item) ?? ExpanderIcon;
    }

    internal string? GetItemExpanderIconName(TItem item)
    {
        return GetExpanderIconName(item) ?? ExpanderIconName;
    }

    internal BitIconInfo? GetItemExpandedExpanderIcon(TItem item)
    {
        return GetExpandedExpanderIcon(item) ?? ExpandedExpanderIcon;
    }

    internal string? GetItemExpandedExpanderIconName(TItem item)
    {
        return GetExpandedExpanderIconName(item) ?? ExpandedExpanderIconName;
    }

    internal BitIconInfo? GetItemIcon(TItem item)
    {
        return GetIcon(item);
    }

    internal string? GetItemIconName(TItem item)
    {
        return GetIconName(item);
    }



    internal string? GetItemKey(TItem? item)
    {
        if (item is null) return null;

        var key = GetDeclaredKey(item);
        if (key.HasValue()) return key;

        // An item that carries no key of its own - a custom type whose key property is computed, read-only or
        // simply not there - is given a generated one that is kept beside it rather than written into it.
        return _fallbackKeys.TryGetValue(item, out var fallback) ? fallback : null;
    }

    private string? GetDeclaredKey(TItem item)
    {
        if (item is BitAccordionListItem listItem)
        {
            return listItem.Key;
        }

        if (item is BitAccordionListOption listOption)
        {
            return listOption.Key;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Key.Selector is not null)
        {
            return NameSelectors.Key.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Key.Name);
    }

    private void SetItemKey(TItem item, string value)
    {
        if (item is BitAccordionListItem listItem)
        {
            listItem.Key = value;
            return;
        }

        if (item is BitAccordionListOption listOption)
        {
            listOption.Key = value;
            return;
        }

        _fallbackKeys[item] = value;
    }

    internal string? GetClass(TItem? item)
    {
        if (item is null) return null;

        if (item is BitAccordionListItem listItem)
        {
            return listItem.Class;
        }

        if (item is BitAccordionListOption listOption)
        {
            return listOption.Class;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Class.Selector is not null)
        {
            return NameSelectors.Class.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Class.Name);
    }

    internal string? GetStyle(TItem? item)
    {
        if (item is null) return null;

        if (item is BitAccordionListItem listItem)
        {
            return listItem.Style;
        }

        if (item is BitAccordionListOption listOption)
        {
            return listOption.Style;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Style.Selector is not null)
        {
            return NameSelectors.Style.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Style.Name);
    }

    internal string? GetTitle(TItem? item)
    {
        if (item is null) return null;

        if (item is BitAccordionListItem listItem)
        {
            return listItem.Title;
        }

        if (item is BitAccordionListOption listOption)
        {
            return listOption.Title;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Title.Selector is not null)
        {
            return NameSelectors.Title.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Title.Name);
    }

    internal string? GetDescription(TItem? item)
    {
        if (item is null) return null;

        if (item is BitAccordionListItem listItem)
        {
            return listItem.Description;
        }

        if (item is BitAccordionListOption listOption)
        {
            return listOption.Description;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Description.Selector is not null)
        {
            return NameSelectors.Description.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Description.Name);
    }

    internal bool GetIsEnabled(TItem? item)
    {
        if (item is null) return false;

        if (item is BitAccordionListItem listItem)
        {
            return listItem.IsEnabled;
        }

        if (item is BitAccordionListOption listOption)
        {
            return listOption.IsEnabled;
        }

        if (NameSelectors is null) return true;

        if (NameSelectors.IsEnabled.Selector is not null)
        {
            return NameSelectors.IsEnabled.Selector!(item);
        }

        return item.GetValueFromProperty(NameSelectors.IsEnabled.Name, true);
    }

    private bool GetIsExpanded(TItem? item)
    {
        if (item is null) return false;

        if (item is BitAccordionListItem listItem)
        {
            return listItem.IsExpanded;
        }

        if (item is BitAccordionListOption listOption)
        {
            return listOption.IsExpanded;
        }

        if (NameSelectors is null) return false;

        if (NameSelectors.IsExpanded.Selector is not null)
        {
            return NameSelectors.IsExpanded.Selector!(item);
        }

        return item.GetValueFromProperty(NameSelectors.IsExpanded.Name, false);
    }

    private void SetIsExpanded(TItem item, bool value)
    {
        if (item is BitAccordionListItem listItem)
        {
            listItem.IsExpanded = value;
            return;
        }

        // Option components expose IsExpanded only as an initial parameter; their runtime
        // state is tracked internally via the expanded keys set.
        if (item is BitAccordionListOption)
        {
            return;
        }

        if (NameSelectors is null) return;

        item.SetValueToProperty(NameSelectors.IsExpanded.Name, value);
    }

    private bool? GetReadOnly(TItem? item)
    {
        if (item is null) return null;

        if (item is BitAccordionListItem listItem)
        {
            return listItem.ReadOnly;
        }

        if (item is BitAccordionListOption listOption)
        {
            return listOption.ReadOnly;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.ReadOnly.Selector is not null)
        {
            return NameSelectors.ReadOnly.Selector!(item);
        }

        return item.GetValueFromProperty<bool?>(NameSelectors.ReadOnly.Name);
    }

    private bool? GetHideExpanderIcon(TItem? item)
    {
        if (item is null) return null;

        if (item is BitAccordionListItem listItem)
        {
            return listItem.HideExpanderIcon;
        }

        if (item is BitAccordionListOption listOption)
        {
            return listOption.HideExpanderIcon;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.HideExpanderIcon.Selector is not null)
        {
            return NameSelectors.HideExpanderIcon.Selector!(item);
        }

        return item.GetValueFromProperty<bool?>(NameSelectors.HideExpanderIcon.Name);
    }

    private BitIconInfo? GetIcon(TItem? item)
    {
        if (item is null) return null;

        if (item is BitAccordionListItem listItem)
        {
            return listItem.Icon;
        }

        if (item is BitAccordionListOption listOption)
        {
            return listOption.Icon;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Icon.Selector is not null)
        {
            return NameSelectors.Icon.Selector!(item);
        }

        return item.GetValueFromProperty<BitIconInfo?>(NameSelectors.Icon.Name);
    }

    private string? GetIconName(TItem? item)
    {
        if (item is null) return null;

        if (item is BitAccordionListItem listItem)
        {
            return listItem.IconName;
        }

        if (item is BitAccordionListOption listOption)
        {
            return listOption.IconName;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.IconName.Selector is not null)
        {
            return NameSelectors.IconName.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.IconName.Name);
    }

    private BitIconInfo? GetExpanderIcon(TItem? item)
    {
        if (item is null) return null;

        if (item is BitAccordionListItem listItem)
        {
            return listItem.ExpanderIcon;
        }

        if (item is BitAccordionListOption listOption)
        {
            return listOption.ExpanderIcon;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.ExpanderIcon.Selector is not null)
        {
            return NameSelectors.ExpanderIcon.Selector!(item);
        }

        return item.GetValueFromProperty<BitIconInfo?>(NameSelectors.ExpanderIcon.Name);
    }

    private string? GetExpanderIconName(TItem? item)
    {
        if (item is null) return null;

        if (item is BitAccordionListItem listItem)
        {
            return listItem.ExpanderIconName;
        }

        if (item is BitAccordionListOption listOption)
        {
            return listOption.ExpanderIconName;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.ExpanderIconName.Selector is not null)
        {
            return NameSelectors.ExpanderIconName.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.ExpanderIconName.Name);
    }

    private BitIconInfo? GetExpandedExpanderIcon(TItem? item)
    {
        if (item is null) return null;

        if (item is BitAccordionListItem listItem)
        {
            return listItem.ExpandedExpanderIcon;
        }

        if (item is BitAccordionListOption listOption)
        {
            return listOption.ExpandedExpanderIcon;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.ExpandedExpanderIcon.Selector is not null)
        {
            return NameSelectors.ExpandedExpanderIcon.Selector!(item);
        }

        return item.GetValueFromProperty<BitIconInfo?>(NameSelectors.ExpandedExpanderIcon.Name);
    }

    private string? GetExpandedExpanderIconName(TItem? item)
    {
        if (item is null) return null;

        if (item is BitAccordionListItem listItem)
        {
            return listItem.ExpandedExpanderIconName;
        }

        if (item is BitAccordionListOption listOption)
        {
            return listOption.ExpandedExpanderIconName;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.ExpandedExpanderIconName.Selector is not null)
        {
            return NameSelectors.ExpandedExpanderIconName.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.ExpandedExpanderIconName.Name);
    }

    private RenderFragment<TItem>? GetActions(TItem? item)
    {
        if (item is null) return null;

        if (item is BitAccordionListItem listItem)
        {
            return listItem.Actions as RenderFragment<TItem>;
        }

        if (item is BitAccordionListOption listOption)
        {
            return listOption.Actions as RenderFragment<TItem>;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Actions.Selector is not null)
        {
            return NameSelectors.Actions.Selector!(item);
        }

        return item.GetValueFromProperty<RenderFragment<TItem>?>(NameSelectors.Actions.Name);
    }

    private RenderFragment<TItem>? GetBody(TItem? item)
    {
        if (item is null) return null;

        if (item is BitAccordionListItem listItem)
        {
            return listItem.Body as RenderFragment<TItem>;
        }

        if (item is BitAccordionListOption listOption)
        {
            return listOption.Body as RenderFragment<TItem>;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Body.Selector is not null)
        {
            return NameSelectors.Body.Selector!(item);
        }

        return item.GetValueFromProperty<RenderFragment<TItem>?>(NameSelectors.Body.Name);
    }

    private RenderFragment<TItem>? GetHeaderTemplate(TItem? item)
    {
        if (item is null) return null;

        if (item is BitAccordionListItem listItem)
        {
            return listItem.HeaderTemplate as RenderFragment<TItem>;
        }

        if (item is BitAccordionListOption listOption)
        {
            return listOption.HeaderTemplate as RenderFragment<TItem>;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.HeaderTemplate.Selector is not null)
        {
            return NameSelectors.HeaderTemplate.Selector!(item);
        }

        return item.GetValueFromProperty<RenderFragment<TItem>?>(NameSelectors.HeaderTemplate.Name);
    }

    private RenderFragment<TItem>? GetTitleTemplate(TItem? item)
    {
        if (item is null) return null;

        if (item is BitAccordionListItem listItem)
        {
            return listItem.TitleTemplate as RenderFragment<TItem>;
        }

        if (item is BitAccordionListOption listOption)
        {
            return listOption.TitleTemplate as RenderFragment<TItem>;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.TitleTemplate.Selector is not null)
        {
            return NameSelectors.TitleTemplate.Selector!(item);
        }

        return item.GetValueFromProperty<RenderFragment<TItem>?>(NameSelectors.TitleTemplate.Name);
    }

    private RenderFragment<TItem>? GetExpanderTemplate(TItem? item)
    {
        if (item is null) return null;

        if (item is BitAccordionListItem listItem)
        {
            return listItem.ExpanderTemplate as RenderFragment<TItem>;
        }

        if (item is BitAccordionListOption listOption)
        {
            return listOption.ExpanderTemplate as RenderFragment<TItem>;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.ExpanderTemplate.Selector is not null)
        {
            return NameSelectors.ExpanderTemplate.Selector!(item);
        }

        return item.GetValueFromProperty<RenderFragment<TItem>?>(NameSelectors.ExpanderTemplate.Name);
    }

    private async Task InvokeItemClick(TItem item)
    {
        if (item is BitAccordionListItem listItem)
        {
            listItem.OnClick?.Invoke(listItem);
            return;
        }

        if (item is BitAccordionListOption listOption)
        {
            await listOption.OnClick.InvokeAsync(listOption);
            return;
        }

        if (NameSelectors is null) return;

        if (NameSelectors.OnClick.Selector is not null)
        {
            NameSelectors.OnClick.Selector!(item)?.Invoke(item);
        }
        else
        {
            item.GetValueFromProperty<Action<TItem>?>(NameSelectors.OnClick.Name)?.Invoke(item);
        }
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (disposing && _preventKeysRegistered)
        {
            _preventKeysRegistered = false;

            try
            {
                await _js.BitExtrasDisposePreventKeys(RootElement);
            }
            catch (JSDisconnectedException) { } // we can ignore this exception here
            catch (ObjectDisposedException) { } // we can ignore this exception here
        }

        await base.DisposeAsync(disposing);
    }



    // The items are held by identity: two items of a type that compares by value are still two panels of the
    // list, each with its own key and its own expanded state.
    private sealed class ReferenceComparer : IEqualityComparer<TItem>
    {
        internal static readonly ReferenceComparer Instance = new();

        public bool Equals(TItem? x, TItem? y) => ReferenceEquals(x, y);

        public int GetHashCode(TItem obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
