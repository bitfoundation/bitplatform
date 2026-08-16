using System.Text;
using System.Text.Json;

namespace Bit.BlazorUI;

/// <summary>
/// Breadcrumbs should be used as a navigational aid in your app or site. They indicate the current page’s location within a hierarchy and help the user understand where they are in relation to the rest of that hierarchy.
/// </summary>
public partial class BitBreadcrumb<TItem> : BitComponentBase where TItem : class
{
    private bool _isCalloutOpen;
    private bool _optionsOrderDirty;
    private bool _focusFirstItemOnOpen;
    private bool _preventKeysRegistered;
    private bool _lastAutoCollapse;
    private bool _resizeObserverRegistered;
    private bool _measureRequested;
    private int _measurePasses;
    private uint _autoMaxDisplayedItems;
    private uint _internalOverflowIndex;
    private uint _internalMaxDisplayedItems;
    private uint _lastOverflowIndex;
    private uint _lastMaxDisplayedItems;
    // The width each collapsed item took while it was still in the trail, newest first, so an item is
    // only brought back when the room that freed up is at least the room it needs again.
    private readonly Stack<double> _collapsedWidths = new();
    private List<TItem> _items = [];
    private List<TItem> _internalItems = [];
    private readonly List<TItem> _displayItems = [];
    private readonly List<TItem> _overflowItems = [];
    private ElementReference _calloutRef;
    private ElementReference _overflowButtonRef;
    private DotNetObjectReference<BitBreadcrumb<TItem>> _dotnetObj = default!;

    private string _calloutId = default!;
    private string _overlayId = default!;
    private string _overflowAnchorId = default!;
    private string _scrollContainerId = default!;
    private string _optionsContainerId = default!;



    [Inject] private IJSRuntime _js { get; set; } = default!;

    [Inject] private NavigationManager _navigationManager { get; set; } = default!;



    /// <summary>
    /// Collapses the items that do not fit the width of the breadcrumb into the overflow menu, and brings
    /// them back as the room for them returns, so the trail always stays on a single line.
    /// <br />
    /// It measures the rendered items through JS interop and follows the size of the component with a
    /// resize observer, and it is off by default. MaxDisplayedItems, when it is set, still caps how many
    /// items the automatic collapsing may leave in the trail, and OverflowIndex still decides where the
    /// overflow button sits. It has no effect on a wrapping breadcrumb, which never overflows its line.
    /// </summary>
    [Parameter] public bool AutoCollapse { get; set; }

    /// <summary>
    /// Keeps the rendered order of the items in sync with the markup order of the options even when
    /// existing options are only reordered (not added or removed) - for example when the options are
    /// produced by a keyed loop whose source collection gets re-sorted. This is achieved by reading the
    /// DOM order of the options after each render, so it adds a JS interop call per render and is opt-in.
    /// It only affects the options API (ChildContent/Options); the items API already follows the order
    /// of the Items collection. Adding or removing options is always kept in order regardless of this flag.
    /// </summary>
    [Parameter] public bool AutoReorderOptions { get; set; }

    /// <summary>
    /// The content of the BitBreadcrumb, that are BitBreadcrumbOption components.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the breadcrumb.
    /// </summary>
    [Parameter] public BitBreadcrumbClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the breadcrumb items.
    /// <br />
    /// The default value is <strong>null</strong>, which keeps the default foreground color of the theme.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// Render a custom divider icon in place of the default chevron.
    /// </summary>
    [Parameter] public BitIconInfo? DividerIcon { get; set; }

    /// <summary>
    /// Name of the icon to render as the divider in place of the default chevron.
    /// </summary>
    [Parameter] public string? DividerIconName { get; set; }

    /// <summary>
    /// The custom template content to render divider icon.
    /// </summary>
    [Parameter] public RenderFragment? DividerIconTemplate { get; set; }

    /// <summary>
    /// A plain text divider (for example "/" or "›") to render in place of the default chevron icon.
    /// <br />
    /// It is ignored when the DividerIconTemplate is provided.
    /// </summary>
    [Parameter] public string? DividerText { get; set; }

    /// <summary>
    /// Collection of the items to render in the breadcrumb.
    /// </summary>
    [Parameter] public IList<TItem> Items { get; set; } = [];

    /// <summary>
    /// The custom template content to render each item.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>
    /// The maximum width of the text of each item as a CSS length (for example "8rem").
    /// The text of a longer item is truncated with an ellipsis, so setting the Title of the items is
    /// recommended to keep the full text reachable.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public string? MaxItemWidth { get; set; }

    /// <summary>
    /// The maximum number of items to display before coalescing.
    /// If not specified, all of the items will be rendered.
    /// </summary>
    [Parameter] public uint MaxDisplayedItems { get; set; }

    /// <summary>
    /// Names and selectors of the custom input type properties.
    /// </summary>
    [Parameter] public BitBreadcrumbNameSelectors<TItem>? NameSelectors { get; set; }

    /// <summary>
    /// Callback for when a breadcrumb item is clicked.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnItemClick { get; set; }

    /// <summary>
    /// Alias of the ChildContent.
    /// </summary>
    [Parameter] public RenderFragment? Options { get; set; }

    /// <summary>
    /// Aria label for the overflow button.
    /// <br />
    /// The default value is <strong>"More items"</strong>.
    /// </summary>
    [Parameter] public string? OverflowAriaLabel { get; set; }

    /// <summary>
    /// Optional index where overflow items will be collapsed.
    /// </summary>
    [Parameter] public uint OverflowIndex { get; set; }

    /// <summary>
    /// Render a custom overflow icon in place of the default icon.
    /// </summary>
    [Parameter] public BitIconInfo? OverflowIcon { get; set; }

    /// <summary>
    /// Name of the icon to render in the overflow button in place of the default icon.
    /// </summary>
    [Parameter] public string? OverflowIconName { get; set; }

    /// <summary>
    /// The custom template content to render each overflow icon.
    /// </summary>
    [Parameter] public RenderFragment? OverflowIconTemplate { get; set; }

    /// <summary>
    /// The custom template content to render each item in overflow list.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? OverflowTemplate { get; set; }

    /// <summary>
    /// Reverses the positions of the icon and the item text of the item content.
    /// </summary>
    [Parameter] public bool ReversedIcon { get; set; }

    /// <summary>
    /// The size of the items of the breadcrumb.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// Renders the trail as a schema.org BreadcrumbList in a JSON-LD script next to it, which is what
    /// search engines read to show the hierarchy of the page in their results.
    /// <br />
    /// The whole hierarchy is written, including the items the overflow menu holds, and the Href of each
    /// item is resolved against the base address of the app. It is off by default.
    /// </summary>
    [Parameter] public bool StructuredData { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the breadcrumb.
    /// </summary>
    [Parameter] public BitBreadcrumbClassStyles? Styles { get; set; }

    /// <summary>
    /// Lets a long breadcrumb trail wrap into multiple lines instead of overflowing its container in a single line.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Wrap { get; set; }



    [JSInvokable("CloseCallout")]
    public void CloseCalloutBeforeAnotherCalloutIsOpened()
    {
        _isCalloutOpen = false;
        StateHasChanged();
    }

    /// <summary>
    /// Called by the resize observer of the automatic collapsing, which starts a new round of
    /// measurements since the room the trail has just changed.
    /// <br />
    /// <strong>This method is intended for internal use and should not be called directly.</strong>
    /// </summary>
    [JSInvokable("OnResize")]
    public void _OnResize(ContentRect _)
    {
        if (IsDisposed || AutoCollapse is false) return;

        _measurePasses = 0;
        _measureRequested = true;

        StateHasChanged();
    }



    internal void RegisterOptions(BitBreadcrumbOption option)
    {
        _items.Add((option as TItem)!);
        _optionsOrderDirty = true;
        RefreshItems();
        StateHasChanged();
    }

    // The breadcrumb renders the visible items itself from the registered options (they only render a
    // hidden marker), reading each option's data during its own render. So when an option's parameters
    // change, the breadcrumb must re-render to pick up the new values.
    internal void NotifyOptionParametersChanged()
    {
        if (IsDisposed) return;

        StateHasChanged();
    }

    internal void UnregisterOptions(BitBreadcrumbOption option)
    {
        if (IsDisposed) return;

        _items.Remove((option as TItem)!);
        _optionsOrderDirty = true;
        RefreshItems();
        StateHasChanged();
    }

    // Reorders the registered options based on the DOM order of their rendered markers, since an
    // option that gets conditionally rendered after the first render registers itself at the end of
    // the items list, no matter where in the markup it is located.
    internal void ReorderOptions(string[] orderedOptionIds)
    {
        if (orderedOptionIds.Length == 0) return;

        List<TItem> ordered = new(_items.Count);

        foreach (var optionId in orderedOptionIds)
        {
            var item = _items.FirstOrDefault(i => (i as BitBreadcrumbOption)?._OptionId == optionId);
            if (item is null || ordered.Contains(item)) continue;

            ordered.Add(item);
        }

        if (ordered.Count == 0) return;

        ordered.AddRange(_items.Except(ordered));

        if (ordered.SequenceEqual(_items)) return;

        _items = ordered;
        RefreshItems();
        StateHasChanged();
    }



    protected override string RootElementClass => "bit-brc";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(GetColorClass);

        ClassBuilder.Register(GetSizeClass);

        ClassBuilder.Register(() => Wrap ? "bit-brc-wrp" : null);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(GetMaxItemWidthStyle);
    }

    protected override Task OnInitializedAsync()
    {
        _calloutId = $"BitBreadcrumb-{UniqueId}-callout";
        _overlayId = $"BitBreadcrumb-{UniqueId}-overlay";
        _overflowAnchorId = $"BitBreadcrumb-{UniqueId}-overflow-anchor";
        _scrollContainerId = $"BitBreadcrumb-{UniqueId}-scroll-container";
        _optionsContainerId = $"BitBreadcrumb-{UniqueId}-options-container";

        return base.OnInitializedAsync();
    }

    protected override void OnParametersSet()
    {
        if (ChildContent is null && Options is null)
        {
            _items = Items is not null ? [.. Items] : [];
        }
        else if (AutoReorderOptions)
        {
            // Opt-in: re-read the DOM order of the options after this render so a pure reorder of
            // existing options (which registers/unregisters nothing) is picked up. ReorderOptions
            // only mutates when the order actually changed, so a stable order costs one DOM read.
            _optionsOrderDirty = true;
        }

        // The comparison is order sensitive so that re-sorting the Items collection re-splits the
        // display and the overflow items, and it runs against the raw parameter values so that a
        // clamped MaxDisplayedItems/OverflowIndex does not look like a change on every render.
        var isDirty = _internalItems.SequenceEqual(_items) is false ||
                      _lastOverflowIndex != OverflowIndex ||
                      _lastMaxDisplayedItems != MaxDisplayedItems ||
                      _lastAutoCollapse != AutoCollapse;

        _lastOverflowIndex = OverflowIndex;
        _lastMaxDisplayedItems = MaxDisplayedItems;
        _lastAutoCollapse = AutoCollapse;

        if (isDirty)
        {
            RefreshItems();
        }

        base.OnParametersSet();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            _dotnetObj = DotNetObjectReference.Create(this);
        }

        base.OnAfterRender(firstRender);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        // An open menu whose button is gone would leave the callout and its click-catching overlay behind,
        // which happens as soon as everything fits the trail again (the items changed, the room grew, the
        // cap was raised, ...) or the whole component gets disabled while the menu is open.
        if (_isCalloutOpen && (_overflowItems.Count == 0 || IsEnabled is false))
        {
            await CloseCallout();

            StateHasChanged();
        }

        // The navigation keys of the overflow menu scroll the page by default, which has to be stopped
        // on the DOM side since a Blazor keydown handler cannot preventDefault per key. The overflow
        // button only exists while there are overflow items, so the registration follows its lifetime.
        if (_overflowItems.Count == 0)
        {
            _preventKeysRegistered = false;
        }
        else if (_preventKeysRegistered is false)
        {
            _preventKeysRegistered = true;

            try
            {
                await _js.BitUtilsRegisterPreventKeys(_calloutRef, ["ArrowUp", "ArrowDown", "Home", "End", "Tab"]);
                await _js.BitUtilsRegisterPreventKeys(_overflowButtonRef, ["ArrowUp", "ArrowDown"]);
            }
            catch (JSDisconnectedException) { } // the circuit is gone, nothing to register
            catch (JSException) { } // a JS-side failure here only costs the page-scroll prevention
        }

        if (AutoCollapse)
        {
            if (_resizeObserverRegistered is false)
            {
                _resizeObserverRegistered = true;
                _measureRequested = true;

                try
                {
                    await _js.BitObserversRegisterResize(_Id, RootElement, _dotnetObj);
                }
                catch (JSDisconnectedException) { } // the circuit is gone, nothing to observe
                catch (JSException) { } // without the observer the trail is measured on renders only
            }

            if (_measureRequested)
            {
                _measureRequested = false;

                await MeasureAndCollapse();
            }
        }

        if (_optionsOrderDirty)
        {
            _optionsOrderDirty = false;

            try
            {
                var orderedOptionIds = await _js.BitUtilsGetChildrenAttributes(_optionsContainerId, BitBreadcrumbOption._OPTION_ID_ATTRIBUTE);
                if (IsDisposed) return;
                if (orderedOptionIds is not null)
                {
                    ReorderOptions(orderedOptionIds);
                }
            }
            catch (JSDisconnectedException) { } // the circuit is gone (e.g. the user navigated away), nothing to reorder
            catch (JSException) { } // a JS-side failure while reading the marker order is not fatal, keep the current order
        }
    }



    private async Task HandleOnItemClick(TItem item)
    {
        if (IsEnabled is false) return;
        if (GetIsEnabled(item) is false) return;

        if (OnItemClick.HasDelegate)
        {
            await OnItemClick.InvokeAsync(item);
        }

        if (item is BitBreadcrumbItem breadcrumbItem)
        {
            breadcrumbItem.OnClick?.Invoke(breadcrumbItem);
        }
        else if (item is BitBreadcrumbOption bitBreadcrumbOption)
        {
            await bitBreadcrumbOption.OnClick.InvokeAsync(bitBreadcrumbOption);
        }
        else if (NameSelectors?.OnClick is not null)
        {
            NameSelectors.OnClick(item);
        }
    }

    // Clicking an overflow item both runs its click handler and dismisses the menu, the same way
    // clicking a link in it navigates away from the open menu. The focus goes back onto the button the
    // menu belongs to, since the element it was on is gone with the menu.
    private async Task HandleOnOverflowItemClick(TItem item)
    {
        await HandleOnItemClick(item);

        if (_isCalloutOpen is false) return;

        await CloseCallout();

        await FocusOverflowButton();
    }

    // An item is rendered as a button when it has no Href but something to run on click, either the
    // component level OnItemClick or a click handler of its own.
    private bool HasClickHandler(TItem item)
    {
        if (OnItemClick.HasDelegate) return true;

        if (item is BitBreadcrumbItem breadcrumbItem) return breadcrumbItem.OnClick is not null;

        if (item is BitBreadcrumbOption bitBreadcrumbOption) return bitBreadcrumbOption.OnClick.HasDelegate;

        return NameSelectors?.OnClick is not null;
    }

    private void RefreshItems()
    {
        _internalItems = [.. _items];

        if (AutoCollapse)
        {
            // The automatic collapsing works its way down from the whole trail, so anything that changes
            // the items (or the settings around them) starts the measurements over from a clean slate.
            _autoMaxDisplayedItems = (uint)_internalItems.Count;
            _collapsedWidths.Clear();
            _measurePasses = 0;
            _measureRequested = true;
        }

        ApplyDisplaySettings();
    }

    private void ApplyDisplaySettings()
    {
        var max = MaxDisplayedItems == 0 ? (uint)_internalItems.Count : MaxDisplayedItems;

        if (AutoCollapse)
        {
            max = Math.Min(max, _autoMaxDisplayedItems);
        }

        _internalMaxDisplayedItems = max;
        _internalOverflowIndex = OverflowIndex >= _internalMaxDisplayedItems ? 0 : OverflowIndex;

        SetItemsToShow();
    }

    // Compares what the trail needs against the room it has and moves one item into (or out of) the
    // overflow menu, then asks for another measurement so it settles one item at a time.
    private async Task MeasureAndCollapse()
    {
        if (IsDisposed || _internalItems.Count < 2) return;

        // Each pass changes the trail by a single item, so the passes of a settling round are bounded
        // by the number of items; the guard is there so a layout that never settles cannot loop on.
        if (_measurePasses > _internalItems.Count + 2) return;

        BitOverflowMetrics? metrics;

        try
        {
            metrics = await _js.BitUtilsGetOverflowMetrics(_Id, ".bit-brc-icn > li");
        }
        catch (JSDisconnectedException) { return; } // the circuit is gone, nothing to measure
        catch (JSException) { return; } // a JS-side failure only costs the automatic collapsing

        if (IsDisposed || metrics is null || metrics.Available <= 0) return;

        var slack = metrics.Available - metrics.Content;

        if (slack < 0)
        {
            // The trail does not fit: the last item of it that may leave goes into the overflow menu.
            if (_autoMaxDisplayedItems <= 1)
            {
                _measurePasses = 0;
                return;
            }

            _collapsedWidths.Push(GetNextCollapsibleWidth(metrics));
            _autoMaxDisplayedItems--;
        }
        else
        {
            // The trail fits: the item that left last comes back once the room it took is free again.
            if (_autoMaxDisplayedItems >= _internalItems.Count ||
                _collapsedWidths.Count == 0 ||
                slack < _collapsedWidths.Peek())
            {
                _measurePasses = 0;
                return;
            }

            _collapsedWidths.Pop();
            _autoMaxDisplayedItems++;
        }

        _measurePasses++;
        _measureRequested = true;

        ApplyDisplaySettings();
        StateHasChanged();
    }

    // The room the next collapse frees: the item that is about to leave the trail plus the divider
    // that goes with it. The list items alternate between an item and a divider, and the item that
    // leaves next is the one at the overflow position (right after the overflow button once it is there).
    private double GetNextCollapsibleWidth(BitOverflowMetrics metrics)
    {
        var position = (int)_internalOverflowIndex + (_overflowItems.Count > 0 ? 1 : 0);

        var itemIndex = position * 2;
        if (itemIndex >= metrics.Widths.Length) return 0;

        var dividerIndex = itemIndex + 1;

        return metrics.Widths[itemIndex] + (dividerIndex < metrics.Widths.Length ? metrics.Widths[dividerIndex] : 0);
    }

    private void SetItemsToShow()
    {
        _displayItems.Clear();
        _overflowItems.Clear();

        if (_internalMaxDisplayedItems >= _internalItems.Count)
        {
            _displayItems.AddRange(_internalItems);
            return;
        }

        var overflowItemsCount = _internalItems.Count - _internalMaxDisplayedItems;

        foreach ((TItem item, int index) in _internalItems.Select((item, index) => (item, index)))
        {
            if (_internalOverflowIndex <= index && index < (overflowItemsCount + _internalOverflowIndex))
            {
                if (index == _internalOverflowIndex)
                {
                    _displayItems.Add(item);
                }

                _overflowItems.Add(item);
            }
            else
            {
                _displayItems.Add(item);
            }
        }
    }

    private string? GetColorClass() => Color switch
    {
        BitColor.Primary => "bit-brc-pri",
        BitColor.Secondary => "bit-brc-sec",
        BitColor.Tertiary => "bit-brc-ter",
        BitColor.Info => "bit-brc-inf",
        BitColor.Success => "bit-brc-suc",
        BitColor.Warning => "bit-brc-wrn",
        BitColor.SevereWarning => "bit-brc-swr",
        BitColor.Error => "bit-brc-err",
        BitColor.PrimaryBackground => "bit-brc-pbg",
        BitColor.SecondaryBackground => "bit-brc-sbg",
        BitColor.TertiaryBackground => "bit-brc-tbg",
        BitColor.PrimaryForeground => "bit-brc-pfg",
        BitColor.SecondaryForeground => "bit-brc-sfg",
        BitColor.TertiaryForeground => "bit-brc-tfg",
        BitColor.PrimaryBorder => "bit-brc-pbr",
        BitColor.SecondaryBorder => "bit-brc-sbr",
        BitColor.TertiaryBorder => "bit-brc-tbr",
        _ => null
    };

    private string? GetSizeClass() => Size switch
    {
        BitSize.Small => "bit-brc-sm",
        BitSize.Medium => "bit-brc-md",
        BitSize.Large => "bit-brc-lg",
        _ => null
    };

    private string? GetMaxItemWidthStyle()
    {
        return MaxItemWidth.HasValue() ? $"--bit-brc-itm-max-width:{MaxItemWidth}" : null;
    }

    // The callout is moved to the body by the JS positioning, so it is not a descendant of the root
    // element and cannot inherit anything from it: the class and style hooks that drive the look of
    // the items have to be repeated on it.
    private string GetCalloutClasses()
    {
        return string.Join(' ', new[] { "bit-brc-cal", GetColorClass(), GetSizeClass(), Classes?.Callout }
                                    .Where(c => c.HasValue()));
    }

    private string GetCalloutStyles()
    {
        return string.Join(';', new[] { GetMaxItemWidthStyle(), Styles?.Callout }
                                    .Where(s => s.HasValue()).Select(s => s!.Trim(';')));
    }

    private string? GetKey(TItem item)
    {
        if (item is BitBreadcrumbItem breadcrumbItem)
        {
            return breadcrumbItem.Key;
        }

        if (item is BitBreadcrumbOption bitBreadcrumbOption)
        {
            return bitBreadcrumbOption.Key;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Key.Selector is not null)
        {
            return NameSelectors.Key.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Key.Name);
    }

    private string GetClasses(TItem item, bool isOverflowItem)
    {
        var classes = new List<string>();

        if (GetItemClass(item).HasValue())
        {
            classes.Add(GetItemClass(item)!);
        }

        if (GetIsSelected(item))
        {
            classes.Add("bit-brc-sel");
        }

        if (isOverflowItem)
        {
            if (Classes?.OverflowItem.HasValue() ?? false)
            {
                classes.Add(Classes.OverflowItem!);
            }

            if (GetIsSelected(item) && (Classes?.OverflowSelectedItem.HasValue() ?? false))
            {
                classes.Add(Classes.OverflowSelectedItem!);
            }
        }
        else
        {
            if (Classes?.Item.HasValue() ?? false)
            {
                classes.Add(Classes.Item!);
            }

            if (GetIsSelected(item) && (Classes?.SelectedItem.HasValue() ?? false))
            {
                classes.Add(Classes.SelectedItem!);
            }
        }

        if (GetIsEnabled(item) is false)
        {
            classes.Add("bit-brc-dis");
        }

        if (GetReversedIcon(item))
        {
            classes.Add("bit-brc-rvi");
        }

        return string.Join(" ", classes);
    }

    private string GetStyles(TItem item, bool isOverflowItem)
    {
        var styles = new List<string>();

        if (GetItemStyle(item).HasValue())
        {
            styles.Add(GetItemStyle(item)!.Trim(';'));
        }

        if (isOverflowItem)
        {
            if (Styles?.OverflowItem.HasValue() ?? false)
            {
                styles.Add(Styles.OverflowItem!.Trim(';'));
            }

            if (GetIsSelected(item) && (Styles?.OverflowSelectedItem.HasValue() ?? false))
            {
                styles.Add(Styles.OverflowSelectedItem!.Trim(';'));
            }
        }
        else
        {
            if (Styles?.Item.HasValue() ?? false)
            {
                styles.Add(Styles.Item!.Trim(';'));
            }

            if (GetIsSelected(item) && (Styles?.SelectedItem.HasValue() ?? false))
            {
                styles.Add(Styles.SelectedItem!.Trim(';'));
            }
        }

        return string.Join(';', styles);
    }

    // A disabled item is not a place to navigate to, so it keeps its address but renders none.
    private string? GetItemHref(TItem item)
    {
        return GetIsEnabled(item) ? GetRawItemHref(item) : null;
    }

    // Opening a link in another browsing context hands the opener over to it unless it is turned down,
    // which the rel of the link does, the same way the other components of the library do it.
    private string? GetItemRel(TItem item)
    {
        return GetItemTarget(item) is "_blank" ? "noopener noreferrer" : null;
    }

    private string? GetRawItemHref(TItem item)
    {
        if (item is BitBreadcrumbItem breadcrumbItem)
        {
            return breadcrumbItem.Href;
        }

        if (item is BitBreadcrumbOption bitBreadcrumbOption)
        {
            return bitBreadcrumbOption.Href;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Href.Selector is not null)
        {
            return NameSelectors.Href.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Href.Name);
    }

    private string? GetItemClass(TItem item)
    {
        if (item is BitBreadcrumbItem breadcrumbItem)
        {
            return breadcrumbItem.Class;
        }

        if (item is BitBreadcrumbOption bitBreadcrumbOption)
        {
            return bitBreadcrumbOption.Class;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Class.Selector is not null)
        {
            return NameSelectors.Class.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Class.Name);
    }

    private string? GetItemStyle(TItem item)
    {
        if (item is BitBreadcrumbItem breadcrumbItem)
        {
            return breadcrumbItem.Style;
        }

        if (item is BitBreadcrumbOption bitBreadcrumbOption)
        {
            return bitBreadcrumbOption.Style;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Style.Selector is not null)
        {
            return NameSelectors.Style.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Style.Name);
    }

    private string? GetItemText(TItem item)
    {
        if (item is BitBreadcrumbItem breadcrumbItem)
        {
            return breadcrumbItem.Text;
        }

        if (item is BitBreadcrumbOption bitBreadcrumbOption)
        {
            return bitBreadcrumbOption.Text;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Text.Selector is not null)
        {
            return NameSelectors.Text.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Text.Name);
    }

    private string? GetItemTitle(TItem item)
    {
        if (item is BitBreadcrumbItem breadcrumbItem)
        {
            return breadcrumbItem.Title;
        }

        if (item is BitBreadcrumbOption bitBreadcrumbOption)
        {
            return bitBreadcrumbOption.Title;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Title.Selector is not null)
        {
            return NameSelectors.Title.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Title.Name);
    }

    private string? GetItemTarget(TItem item)
    {
        if (item is BitBreadcrumbItem breadcrumbItem)
        {
            return breadcrumbItem.Target;
        }

        if (item is BitBreadcrumbOption bitBreadcrumbOption)
        {
            return bitBreadcrumbOption.Target;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Target.Selector is not null)
        {
            return NameSelectors.Target.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Target.Name);
    }

    private string? GetItemAriaLabel(TItem item)
    {
        if (item is BitBreadcrumbItem breadcrumbItem)
        {
            return breadcrumbItem.AriaLabel;
        }

        if (item is BitBreadcrumbOption bitBreadcrumbOption)
        {
            return bitBreadcrumbOption.AriaLabel;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.AriaLabel.Selector is not null)
        {
            return NameSelectors.AriaLabel.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.AriaLabel.Name);
    }

    private BitIconInfo? GetIcon(TItem item)
    {
        if (item is BitBreadcrumbItem breadcrumbItem)
        {
            return BitIconInfo.From(breadcrumbItem.Icon, breadcrumbItem.IconName);
        }

        if (item is BitBreadcrumbOption bitBreadcrumbOption)
        {
            return BitIconInfo.From(bitBreadcrumbOption.Icon, bitBreadcrumbOption.IconName);
        }

        if (NameSelectors is null) return null;

        BitIconInfo? icon = null;
        if (NameSelectors.Icon.Selector is not null)
        {
            icon = NameSelectors.Icon.Selector!(item);
        }
        else
        {
            icon = item.GetValueFromProperty<BitIconInfo?>(NameSelectors.Icon.Name);
        }

        string? iconName = null;
        if (NameSelectors.IconName.Selector is not null)
        {
            iconName = NameSelectors.IconName.Selector!(item);
        }
        else
        {
            iconName = item.GetValueFromProperty<string?>(NameSelectors.IconName.Name);
        }

        return BitIconInfo.From(icon, iconName);
    }

    private bool GetReversedIcon(TItem item)
    {
        if (item is BitBreadcrumbItem breadcrumbItem)
        {
            return breadcrumbItem.ReversedIcon ?? ReversedIcon;
        }

        if (item is BitBreadcrumbOption bitBreadcrumbOption)
        {
            return bitBreadcrumbOption.ReversedIcon ?? ReversedIcon;
        }

        if (NameSelectors is null) return ReversedIcon;

        if (NameSelectors.ReversedIcon.Selector is not null)
        {
            return NameSelectors.ReversedIcon.Selector!(item) ?? ReversedIcon;
        }

        return item.GetValueFromProperty(NameSelectors.ReversedIcon.Name, ReversedIcon);
    }

    private bool GetIsSelected(TItem item)
    {
        if (item is BitBreadcrumbItem breadcrumbItem)
        {
            return breadcrumbItem.IsSelected;
        }

        if (item is BitBreadcrumbOption bitBreadcrumbOption)
        {
            return bitBreadcrumbOption.IsSelected;
        }

        if (NameSelectors is null) return false;

        if (NameSelectors.IsSelected.Selector is not null)
        {
            return NameSelectors.IsSelected.Selector!(item);
        }

        return item.GetValueFromProperty(NameSelectors.IsSelected.Name, false);
    }

    private bool GetIsEnabled(TItem item)
    {
        if (IsEnabled is false) return false;

        if (item is BitBreadcrumbItem breadcrumbItem)
        {
            return breadcrumbItem.IsEnabled;
        }

        if (item is BitBreadcrumbOption bitBreadcrumbOption)
        {
            return bitBreadcrumbOption.IsEnabled;
        }

        if (NameSelectors is null) return true;

        if (NameSelectors.IsEnabled.Selector is not null)
        {
            return NameSelectors.IsEnabled.Selector!(item);
        }

        return item.GetValueFromProperty(NameSelectors.IsEnabled.Name, true);
    }

    private RenderFragment<TItem>? GetOverflowTemplate(TItem? item)
    {
        if (item is null) return null;

        if (item is BitBreadcrumbItem breadcrumbItem)
        {
            return breadcrumbItem.OverflowTemplate as RenderFragment<TItem>;
        }

        if (item is BitBreadcrumbOption bitBreadcrumbOption)
        {
            return bitBreadcrumbOption.OverflowTemplate as RenderFragment<TItem>;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.OverflowTemplate.Selector is not null)
        {
            return NameSelectors.OverflowTemplate.Selector!(item);
        }

        return item.GetValueFromProperty<RenderFragment<TItem>?>(NameSelectors.OverflowTemplate.Name);
    }

    private RenderFragment<TItem>? GetTemplate(TItem? item)
    {
        if (item is null) return null;

        if (item is BitBreadcrumbItem breadcrumbItem)
        {
            return breadcrumbItem.Template as RenderFragment<TItem>;
        }

        if (item is BitBreadcrumbOption bitBreadcrumbOption)
        {
            return bitBreadcrumbOption.Template as RenderFragment<TItem>;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Template.Selector is not null)
        {
            return NameSelectors.Template.Selector!(item);
        }

        return item.GetValueFromProperty<RenderFragment<TItem>?>(NameSelectors.Template.Name);
    }

    private async Task OpenCallout()
    {
        if (_isCalloutOpen || IsEnabled is false) return;

        _isCalloutOpen = true;
        await ToggleCallout();

        if (_focusFirstItemOnOpen)
        {
            _focusFirstItemOnOpen = false;
            await FocusOverflowItem("first");
        }
    }

    private async Task CloseCallout()
    {
        if (_isCalloutOpen is false) return;

        _isCalloutOpen = false;
        await ToggleCallout();
    }

    // Closing has to work even on a breadcrumb that got disabled while its menu was open, so only a
    // disposed component (whose JS side is gone) turns the toggle away; opening is guarded on its own.
    private async Task ToggleCallout()
    {
        if (IsDisposed) return;

        try
        {
            await _js.BitCalloutToggleCallout(
                dotnetObj: _dotnetObj,
                componentId: _overflowAnchorId,
                component: null,
                calloutId: _calloutId,
                callout: null,
                overlayId: _overlayId,
                isCalloutOpen: _isCalloutOpen,
                responsiveMode: BitResponsiveMode.None,
                dropDirection: BitDropDirection.TopAndBottom,
                isRtl: Dir is BitDir.Rtl,
                scrollContainerId: _scrollContainerId,
                scrollOffset: 0,
                headerId: "",
                footerId: "",
                setCalloutWidth: false,
                fixedCalloutWidth: false,
                maxWindowWidth: 0);
        }
        catch (JSDisconnectedException) { } // the circuit is gone, there is no callout left to move
    }

    private async Task HandleOnOverflowButtonKeyDown(KeyboardEventArgs e)
    {
        if (IsEnabled is false) return;

        if (e.Key is "Escape")
        {
            if (_isCalloutOpen is false) return;

            await CloseCallout();
            return;
        }

        if (e.Key is "ArrowDown" or "ArrowUp")
        {
            await OpenCallout();
            await FocusOverflowItem(e.Key is "ArrowDown" ? "first" : "last");
        }
        else if (e.Key is "Enter" or " ")
        {
            // The native click that follows this keydown opens the menu, so mark it to move the
            // focus onto the first overflow item as the APG disclosure pattern requires.
            _focusFirstItemOnOpen = true;
        }
    }

    private async Task HandleOnCalloutKeyDown(KeyboardEventArgs e)
    {
        if (IsEnabled is false || _isCalloutOpen is false) return;

        switch (e.Key)
        {
            case "ArrowDown":
                await FocusOverflowItem("next");
                break;
            case "ArrowUp":
                await FocusOverflowItem("prev");
                break;
            case "Home":
                await FocusOverflowItem("first");
                break;
            case "End":
                await FocusOverflowItem("last");
                break;
            case "Escape":
            case "Tab":
                await CloseCallout();
                await FocusOverflowButton();
                break;
            default:
                // The typeahead of the menu pattern: a printable character jumps to the next item whose
                // text starts with it. A space is left alone since it activates the focused item.
                if (e.Key.Length == 1 && char.IsControl(e.Key[0]) is false && char.IsWhiteSpace(e.Key[0]) is false)
                {
                    await FocusOverflowItem("char", e.Key);
                }
                break;
        }
    }

    // The items that are not actionable are part of the rotation too, they are steps of the trail the
    // arrow keys have to reach; the script leaves the disabled ones out on its own.
    private async Task FocusOverflowItem(string mode, string? character = null)
    {
        try
        {
            await _js.BitUtilsFocusItem(_calloutId, ".bit-brc-ofi, .bit-brc-ofn", mode, character);
        }
        catch (JSDisconnectedException) { } // the circuit is gone, nothing to focus
        catch (JSException) { } // the callout may already be gone with the items it held
    }

    private async Task FocusOverflowButton()
    {
        try
        {
            await _overflowButtonRef.FocusAsync();
        }
        catch (JSDisconnectedException) { } // the circuit is gone, nothing to focus
        catch (JSException) { } // the button may already be gone with the items it collapsed
    }

    private string GetItemKey(TItem item, string defaultKey)
    {
        return GetKey(item) ?? $"{UniqueId}-{defaultKey}";
    }

    // The whole hierarchy as a schema.org BreadcrumbList, the items the overflow menu holds included,
    // since what the search engines are told about the page must not depend on how much of the trail
    // happens to fit the screen. The JSON is written rather than serialized so the output stays free of
    // reflection (the library is trimmable) and so every value goes through the escaping of the writer.
    private string? GetStructuredData()
    {
        var items = _internalItems.Where(i => GetStructuredDataName(i).HasValue()).ToArray();

        if (items.Length == 0) return null;

        using var buffer = new MemoryStream();

        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString("@context", "https://schema.org");
            writer.WriteString("@type", "BreadcrumbList");
            writer.WriteStartArray("itemListElement");

            for (var i = 0; i < items.Length; i++)
            {
                writer.WriteStartObject();
                writer.WriteString("@type", "ListItem");
                writer.WriteNumber("position", i + 1);
                writer.WriteString("name", GetStructuredDataName(items[i]));

                var url = GetStructuredDataUrl(items[i]);
                if (url.HasValue())
                {
                    writer.WriteString("item", url);
                }

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    private string? GetStructuredDataName(TItem item)
    {
        var text = GetItemText(item);

        return text.HasValue() ? text : GetItemAriaLabel(item);
    }

    // The address of a step of the hierarchy is an absolute one for a search engine, which the base
    // address of the app is what turns the relative addresses of the items into.
    private string? GetStructuredDataUrl(TItem item)
    {
        var href = GetRawItemHref(item);

        if (href.HasValue() is false) return null;

        try
        {
            return _navigationManager.ToAbsoluteUri(href).ToString();
        }
        catch (Exception) { return href; } // an address that is none to build upon is written as it is
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        await base.DisposeAsync(disposing);

        if (_dotnetObj is not null)
        {
            try
            {
                if (_resizeObserverRegistered)
                {
                    await _js.BitObserversUnregisterResize(_Id, RootElement, _dotnetObj);
                }

                await _js.BitCalloutClearCallout(_calloutId);
            }
            catch (JSDisconnectedException) { } // we can ignore this exception here
            catch (JSException) { } // the element of the observer may already be gone with its parent

            _dotnetObj.Dispose();
        }
    }
}
