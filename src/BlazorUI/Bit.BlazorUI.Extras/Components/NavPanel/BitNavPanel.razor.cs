namespace Bit.BlazorUI;

/// <summary>
/// BitNavPanel is a navigation component specialized to be rendered in a vertical panel.
/// </summary>
/// <remarks>
/// The panel wraps a <see cref="BitNav{TItem}"/> in the chrome a side navigation needs: a header with a logo
/// and the button that collapses the panel down to a rail of icons, a search box that filters the items as
/// they are typed, and a footer. On small screens it turns into an off-canvas drawer that opens over the page
/// with an overlay behind it, and closes on a click on that overlay, on the Escape key, on a swipe towards the
/// side it came from, and on the navigation of an item.
/// </remarks>
public partial class BitNavPanel<TItem> : BitComponentBase where TItem : class
{
    // The accessible names the panel falls back to. They are only ever read where the API offers no name of
    // its own (ToggleAriaLabel, SearchBoxPlaceholder), so a localized app overrides them through those.
    private const string DefaultLogoAriaLabel = "Home";
    private const string DefaultSearchAriaLabel = "Search";
    private const string DefaultExpandAriaLabel = "Expand the navigation panel";
    private const string DefaultCollapseAriaLabel = "Collapse the navigation panel";

    private bool _isHovered;
    private decimal _diffXPanel;
    private string? _searchText;
    private bool _focusOnOpenPending;
    private bool _focusSearchBoxPending;
    private BitNav<TItem>? _bitNavRef;
    private BitSearchBox? _searchBoxRef;
    private IList<TItem> _filteredNavItems = [];



    /// <summary>
    /// The accent color of the nav.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Accent { get; set; }

    /// <summary>
    /// Expands all items on first render.
    /// </summary>
    [Parameter] public bool AllExpanded { get; set; }

    /// <summary>
    /// Moves the focus into the nav panel as it opens - onto the search box, or onto the first item of a panel
    /// without one - which is what the drawer of a small screen is expected to do.
    /// </summary>
    [Parameter] public bool AutoFocus { get; set; }

    /// <summary>
    /// The icon for the chevron-down element of each nav item.
    /// Takes precedence over <see cref="ChevronDownIconName"/> when both are set.
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="ChevronDownIconName"/> instead.
    /// </summary>
    [Parameter] public BitIconInfo? ChevronDownIcon { get; set; }

    /// <summary>
    /// The custom icon name of the chevron-down element of each nav item.
    /// </summary>
    [Parameter] public string? ChevronDownIconName { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the nav panel.
    /// </summary>
    [Parameter] public BitNavPanelClassStyles? Classes { get; set; }

    /// <summary>
    /// The default aria-label of the expand/collapse button of an expanded item of the nav.
    /// </summary>
    [Parameter] public string? CollapseAriaLabel { get; set; }

    /// <summary>
    /// The general color of the nav.
    /// </summary>
    [Parameter]
    public BitColor? Color { get; set; }

    /// <summary>
    /// The initially selected item of the nav in manual mode.
    /// </summary>
    [Parameter] public TItem? DefaultSelectedItem { get; set; }

    /// <summary>
    /// The custom template for when the search result is empty.
    /// </summary>
    [Parameter] public RenderFragment? EmptyListTemplate { get; set; }

    /// <summary>
    /// The custom message for when the search result is empty.
    /// </summary>
    [Parameter] public string? EmptyListMessage { get; set; }

    /// <summary>
    /// The default aria-label of the expand/collapse button of a collapsed item of the nav.
    /// </summary>
    [Parameter] public string? ExpandAriaLabel { get; set; }

    /// <summary>
    /// Expands the toggled (rail) nav panel back to its full width while the pointer is over it.
    /// The panel widens in place, so the content beside it moves along with it.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool ExpandOnHover { get; set; }

    /// <summary>
    /// Renders the nav panel with fit-content width.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FitWidth { get; set; }

    /// <summary>
    /// The custom template to render as the footer of the nav panel.
    /// </summary>
    [Parameter] public RenderFragment? Footer { get; set; }

    /// <summary>
    /// Renders the nav panel with full (100%) width.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FullWidth { get; set; }

    /// <summary>
    /// The custom template to render as the header of the nav panel.
    /// Replacing the header also replaces the toggle button it holds, so a custom header that wants to keep
    /// the toggle feature renders a control of its own that calls <see cref="Toggle"/>.
    /// </summary>
    [Parameter] public RenderFragment? Header { get; set; }

    /// <summary>
    /// Used to customize how nav content inside the group header is rendered.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? HeaderTemplate { get; set; }

    /// <summary>
    /// The render mode of the custom HeaderTemplate of the nav.
    /// </summary>
    [Parameter] public BitNavItemTemplateRenderMode HeaderTemplateRenderMode { get; set; }

    /// <summary>
    /// Removes the toggle button.
    /// </summary>
    [Parameter] public bool HideToggle { get; set; }

    /// <summary>
    /// Renders an anchor wrapping the icon to navigate to the specified url.
    /// </summary>
    [Parameter] public string? IconNavUrl { get; set; }

    /// <summary>
    /// The icon url to show in the header of the nav panel.
    /// </summary>
    [Parameter] public string? IconUrl { get; set; }

    /// <summary>
    /// The indentation padding in px for items without children (compensation space for chevron icon).
    /// </summary>
    [Parameter] public int IndentPadding { get; set; } = 27;

    /// <summary>
    /// The indentation padding in px for items in reversed mode.
    /// </summary>
    [Parameter] public int IndentReversedPadding { get; set; } = 4;

    /// <summary>
    /// The indentation value in px for each level of depth of child item.
    /// </summary>
    [Parameter] public int IndentValue { get; set; } = 16;

    /// <summary>
    /// Determines if the nav panel is open in small screens.
    /// </summary>
    [Parameter, TwoWayBound, ResetClassBuilder, CallOnSet(nameof(OnIsOpenSet))]
    public bool IsOpen { get; set; }

    /// <summary>
    /// Determines if the nav panel is in the toggled state.
    /// </summary>
    [Parameter, TwoWayBound]
    public bool IsToggled { get; set; }

    /// <summary>
    /// Used to customize how content inside each item is rendered.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>
    /// The render mode of the custom ItemTemplate.
    /// </summary>
    [Parameter] public BitNavItemTemplateRenderMode ItemTemplateRenderMode { get; set; }

    /// <summary>
    /// A collection of items to display in the nav panel.
    /// </summary>
    [Parameter, CallOnSet(nameof(OnItemsSet))]
    public IList<TItem> Items { get; set; } = [];

    /// <summary>
    /// Names and selectors of the custom input type properties.
    /// </summary>
    [Parameter] public BitNavNameSelectors<TItem>? NameSelectors { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the nav component of the nav panel.
    /// </summary>
    [Parameter] public BitNavClassStyles? NavClasses { get; set; }

    /// <summary>
    /// Determines the global URL matching behavior of the nav.
    /// </summary>
    [Parameter] public BitNavMatch? NavMatch { get; set; }

    /// <summary>
    /// Determines how the navigation will be handled.
    /// </summary>
    [Parameter] public BitNavMode NavMode { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the nav component of the nav panel.
    /// </summary>
    [Parameter] public BitNavClassStyles? NavStyles { get; set; }

    /// <summary>
    /// Keeps the nav panel open when an item with a URL is clicked, instead of closing it.
    /// </summary>
    [Parameter] public bool NoAutoClose { get; set; }

    /// <summary>
    /// Disables and hides all collapse/expand buttons of the nav component.
    /// </summary>
    [Parameter] public bool NoCollapse { get; set; }

    /// <summary>
    /// Removes the overlay that is rendered behind the open nav panel in small screens.
    /// </summary>
    [Parameter] public bool NoOverlay { get; set; }

    /// <summary>
    /// Disables the padded mode of the nav panel.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool NoPad { get; set; }

    /// <summary>
    /// Removes the search box from the nav panel.
    /// </summary>
    [Parameter] public bool NoSearchBox { get; set; }

    /// <summary>
    /// Disables the swipe gesture that closes the open nav panel in small screens.
    /// </summary>
    [Parameter] public bool NoSwipe { get; set; }

    /// <summary>
    /// Disables the toggle feature of the nav panel.
    /// </summary>
    [Parameter] public bool NoToggle { get; set; }

    /// <summary>
    /// Event fired up when an item is clicked.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnItemClick { get; set; }

    /// <summary>
    /// Callback invoked when a group header is clicked and Expanded or Collapse.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnItemToggle { get; set; }

    /// <summary>
    /// Callback invoked when the search text of the nav panel changes.
    /// </summary>
    [Parameter] public EventCallback<string?> OnSearch { get; set; }

    /// <summary>
    /// Callback invoked when an item is selected.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnSelectItem { get; set; }

    /// <summary>
    /// The way to render nav items.
    /// </summary>
    [Parameter] public BitNavRenderType RenderType { get; set; }

    /// <summary>
    /// Enables recalling the select events when the same item is selected.
    /// </summary>
    [Parameter] public bool Reselectable { get; set; }

    /// <summary>
    /// Reverses the location of the expander chevron.
    /// </summary>
    [Parameter] public bool ReversedChevron { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the search box of the nav panel.
    /// </summary>
    [Parameter] public BitSearchBoxClassStyles? SearchBoxClasses { get; set; }

    /// <summary>
    /// The placeholder of the input element of the search box of the nav panel.
    /// </summary>
    [Parameter] public string? SearchBoxPlaceholder { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the search box of the nav panel.
    /// </summary>
    [Parameter] public BitSearchBoxClassStyles? SearchBoxStyles { get; set; }

    /// <summary>
    /// Builds the text that the screen reader announces through the live region of the nav panel whenever the
    /// search filters the items, in place of the built-in English announcement. The argument is the number of
    /// items the search matched. Returning null or an empty string announces nothing.
    /// </summary>
    [Parameter] public Func<int, string?>? SearchAnnouncementProvider { get; set; }

    /// <summary>
    /// The debounce time in milliseconds of the search box of the nav panel.
    /// </summary>
    [Parameter] public int SearchDebounceTime { get; set; } = 500;

    /// <summary>
    /// The custom function to decide whether an item matches a search term, replacing the default matching
    /// over the text, the description and the data of an item.
    /// </summary>
    [Parameter] public Func<TItem, string, bool>? SearchFilter { get; set; }

    /// <summary>
    /// The search text of the nav panel that filters its items.
    /// </summary>
    [Parameter, TwoWayBound, CallOnSet(nameof(OnSearchTextSet))]
    public string? SearchText { get; set; }

    /// <summary>
    /// The selected item of the nav in manual mode.
    /// </summary>
    [Parameter, TwoWayBound]
    public TItem? SelectedItem { get; set; }

    /// <summary>
    /// Enables the single-expand mode in the BitNav.
    /// </summary>
    [Parameter] public bool SingleExpand { get; set; }

    /// <summary>
    /// The size of the nav items.
    /// </summary>
    [Parameter] public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the nav panel.
    /// </summary>
    [Parameter] public BitNavPanelClassStyles? Styles { get; set; }

    /// <summary>
    /// The aria-label of the toggle button of the nav panel.
    /// </summary>
    [Parameter] public string? ToggleAriaLabel { get; set; }

    /// <summary>
    /// The icon of the toggle button of the nav panel.
    /// Takes precedence over <see cref="ToggleIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? ToggleIcon { get; set; }

    /// <summary>
    /// The name of the icon of the toggle button of the nav panel.
    /// </summary>
    [Parameter] public string? ToggleIconName { get; set; }

    /// <summary>
    /// The width of the nav panel in px in its toggled (rail) state.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public int ToggledWidth { get; set; }

    /// <summary>
    /// The top CSS property value of the root element of the nav panel in px.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public int Top { get; set; }

    /// <summary>
    /// The width of the nav panel in px. It is ignored in the FitWidth and FullWidth modes.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public int Width { get; set; }



    /// <summary>
    /// Clears the search text of the nav panel, so the whole list of items is shown again.
    /// Nothing happens when there is no search in place.
    /// </summary>
    public async Task ClearSearch()
    {
        if (_searchText.HasNoValue() && SearchText.HasNoValue()) return;

        _searchText = null;

        await AssignSearchText(null);

        SearchNavItems(null);

        await OnSearch.InvokeAsync(null);

        StateHasChanged();
    }

    /// <summary>
    /// Closes the nav panel.
    /// </summary>
    public async Task Close()
    {
        await AssignIsOpen(false);
    }

    /// <summary>
    /// Collapses all items of the nav.
    /// </summary>
    public void CollapseAll() => _bitNavRef?.CollapseAll();

    /// <summary>
    /// Collapses an item of the nav, and does nothing when it is already collapsed.
    /// </summary>
    public Task CollapseItem(TItem item) => _bitNavRef?.CollapseItem(item) ?? Task.CompletedTask;

    /// <summary>
    /// Expands all items of the nav in non-SingleExpand mode.
    /// </summary>
    public void ExpandAll() => _bitNavRef?.ExpandAll();

    /// <summary>
    /// Expands an item of the nav, and does nothing when it is already expanded.
    /// </summary>
    public Task ExpandItem(TItem item) => _bitNavRef?.ExpandItem(item) ?? Task.CompletedTask;

    /// <summary>
    /// Moves the focus to an item of the nav, opening the branches it is nested in when it is not rendered yet.
    /// </summary>
    public ValueTask FocusItem(TItem item) => _bitNavRef?.FocusItem(item) ?? ValueTask.CompletedTask;

    /// <summary>
    /// Moves the focus to the search box of the nav panel, opening the panel out of its toggled state first
    /// when the search box is not on screen.
    /// </summary>
    public async Task FocusSearchBox()
    {
        if (NoSearchBox) return;

        if (IsToggled && NoToggle is false)
        {
            await ToggleForSearch();
            return;
        }

        if (_searchBoxRef is null) return;

        await _searchBoxRef.FocusAsync();
    }

    /// <summary>
    /// Opens the nav panel.
    /// </summary>
    public async Task Open()
    {
        await AssignIsOpen(true);
    }

    /// <summary>
    /// Selects an item of the nav programmatically, exactly like a click on that item would in the manual mode.
    /// </summary>
    public Task SelectItem(TItem? item) => _bitNavRef?.SelectItem(item) ?? Task.CompletedTask;

    /// <summary>
    /// Toggles the nav panel if possible.
    /// </summary>
    public async Task Toggle()
    {
        await ToggleNavPanel();
    }

    /// <summary>
    /// Toggles an item of the nav.
    /// </summary>
    public Task ToggleItem(TItem item) => _bitNavRef?.ToggleItem(item) ?? Task.CompletedTask;



    protected override string RootElementClass => "bit-npn";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => FitWidth ? "bit-npn-fiw" : string.Empty);
        ClassBuilder.Register(() => FullWidth ? "bit-npn-fuw" : string.Empty);

        ClassBuilder.Register(() => IsOpen ? string.Empty : "bit-npn-cls");
        ClassBuilder.Register(() => NoPad ? "bit-npn-npd" : string.Empty);
        ClassBuilder.Register(() => ExpandOnHover ? "bit-npn-eoh" : string.Empty);

        ClassBuilder.Register(() => Accent switch
        {
            BitColor.Primary => "bit-nav-apri",
            BitColor.Secondary => "bit-nav-asec",
            BitColor.Tertiary => "bit-nav-ater",
            BitColor.Info => "bit-nav-ainf",
            BitColor.Success => "bit-nav-asuc",
            BitColor.Warning => "bit-nav-awrn",
            BitColor.SevereWarning => "bit-nav-aswr",
            BitColor.Error => "bit-nav-aerr",
            BitColor.PrimaryBackground => "bit-nav-apbg",
            BitColor.SecondaryBackground => "bit-nav-asbg",
            BitColor.TertiaryBackground => "bit-nav-atbg",
            BitColor.PrimaryForeground => "bit-nav-apfg",
            BitColor.SecondaryForeground => "bit-nav-asfg",
            BitColor.TertiaryForeground => "bit-nav-atfg",
            BitColor.PrimaryBorder => "bit-nav-apbr",
            BitColor.SecondaryBorder => "bit-nav-asbr",
            BitColor.TertiaryBorder => "bit-nav-atbr",
            _ => string.Empty,
        });
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
        StyleBuilder.Register(() => Top > 0 ? $"top:{Top}px;height:calc(var(--bit-env-height-avl) - {Top}px)" : string.Empty);
        // The widths travel as custom properties instead of as a width declaration, so the stylesheet stays
        // the one deciding which of them applies to the current state (rail, padded, small screen, ...).
        StyleBuilder.Register(() => Width > 0 ? $"--bit-npn-w:{Width}px" : string.Empty);
        StyleBuilder.Register(() => ToggledWidth > 0 ? $"--bit-npn-tw:{ToggledWidth}px" : string.Empty);
    }

    protected override async Task OnInitializedAsync()
    {
        _searchText = SearchText;

        SearchNavItems(_searchText);

        // The nav reads its own DefaultSelectedItem only while nothing has assigned its SelectedItem, and the
        // panel always hands it one (it two-way binds the selection through), so the initial selection is
        // resolved here instead, where the two parameters of the panel are the ones being read.
        if (SelectedItem is null && DefaultSelectedItem is not null)
        {
            await AssignSelectedItem(DefaultSelectedItem);
        }

        await base.OnInitializedAsync();
    }

    protected override void OnParametersSet()
    {
        // The Items collection is re-read here rather than only when the parameter is assigned a new
        // instance, so a collection that is mutated in place (an item appended to the same list) reaches
        // the filtered list a search is showing as well.
        if (_searchText.HasValue())
        {
            SearchNavItems(_searchText);
        }

        base.OnParametersSet();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // The item accessors of the nav (which know how to read a custom item type) are only reachable once
        // the nav itself is rendered, so a search text that arrived as a parameter is applied here rather
        // than being dropped on the first pass.
        if (firstRender && _searchText.HasValue())
        {
            SearchNavItems(_searchText);
            StateHasChanged();
        }

        // The search box only exists once the panel has left its toggled state, so the focus is moved in the
        // render that brought it back rather than after a guessed delay.
        if (_focusSearchBoxPending && _searchBoxRef is not null)
        {
            _focusSearchBoxPending = false;

            await _searchBoxRef.FocusAsync();
        }

        // The panel that has just opened takes the focus with it, so the keyboard lands in the drawer that
        // covers the page rather than on the page behind it.
        if (_focusOnOpenPending)
        {
            _focusOnOpenPending = false;

            await FocusFirstElement();
        }

        await base.OnAfterRenderAsync(firstRender);
    }



    private async Task HandleNavItemClick(TItem item)
    {
        await OnItemClick.InvokeAsync(item);

        // An item without a URL navigates nowhere: it only expands its children, so the panel stays open and
        // the search that led to it stays in place.
        if (_bitNavRef?.GetUrl(item).HasNoValue() is true) return;

        await ClearSearch();

        if (NoAutoClose) return;

        await ClosePanel();
    }

    private Task HandleSelectedItemChanged(TItem? item) => AssignSelectedItem(item);

    private void OnIsOpenSet()
    {
        if (AutoFocus is false) return;
        if (IsOpen is false) return;

        _focusOnOpenPending = true;
    }

    // Where the focus goes when the panel opens: the search box is the first thing in it, and a panel without
    // one hands the focus to its first item instead. A panel with neither keeps the focus where it was.
    private async Task FocusFirstElement()
    {
        if (NoSearchBox is false && IsToggled is false && _searchBoxRef is not null)
        {
            await _searchBoxRef.FocusAsync();
            return;
        }

        if (_bitNavRef is null || _filteredNavItems.Count == 0) return;

        await _bitNavRef.FocusItem(_filteredNavItems[0]);
    }

    private async Task ClosePanel()
    {
        await AssignIsOpen(false);
    }

    private async Task HandleOverlayClick()
    {
        if (IsEnabled is false) return;

        await ClosePanel();
    }

    private async Task HandleOnKeyDown(KeyboardEventArgs e)
    {
        if (IsEnabled is false) return;
        if (e.Key is not "Escape") return;

        // The first Escape empties an active search - which is what the search box does on its own when the
        // focus is in it, and what the key is expected to do from anywhere else in the panel - and only the
        // next one closes the panel, exactly like a filtered list inside any other dismissible surface.
        if (_searchText.HasValue())
        {
            await ClearSearch();
            return;
        }

        if (IsOpen is false) return;

        await ClosePanel();
    }

    // The attributes of the root element: the ones the caller passed, plus the hover handlers of a panel that
    // expands on hover. They are only attached in that mode, since an attached handler re-renders the whole
    // panel every time the pointer enters or leaves it, whether it changes anything or not.
    private Dictionary<string, object> GetRootAttributes()
    {
        if (ExpandOnHover is false) return HtmlAttributes;

        return new(HtmlAttributes)
        {
            ["onmouseenter"] = EventCallback.Factory.Create<MouseEventArgs>(this, () => _isHovered = true),
            ["onmouseleave"] = EventCallback.Factory.Create<MouseEventArgs>(this, () => _isHovered = false),
        };
    }

    private async Task ToggleNavPanel()
    {
        if (NoToggle) return;

        if (await AssignIsToggled(!IsToggled) is false) return;

        // A rail has no room for a search box, so the panel that collapses into one drops the search it was
        // showing instead of keeping a filtered list the user can no longer see the reason for.
        if (IsToggled)
        {
            await ClearSearch();
        }
    }

    private async Task ToggleForSearch()
    {
        if (await AssignIsToggled(false) is false) return;

        _focusSearchBoxPending = true;
    }

    private async Task HandleSearchChange(string? searchText)
    {
        _searchText = searchText;

        await AssignSearchText(searchText);

        SearchNavItems(searchText);

        await OnSearch.InvokeAsync(searchText);
    }

    private void OnSearchTextSet()
    {
        _searchText = SearchText;

        SearchNavItems(_searchText);
    }

    // Reduces the items to the ones the search text points at: an item that matches keeps the whole branch
    // under it, so the items it groups stay reachable and no descendant of a match is listed a second time
    // on its own.
    private void SearchNavItems(string? searchText)
    {
        var items = Items ?? [];

        _filteredNavItems = items;

        if (searchText.HasNoValue() || _bitNavRef is null) return;

        var terms = searchText!.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (terms.Length == 0) return;

        List<TItem> result = [];

        Collect(items);

        _filteredNavItems = result;

        void Collect(IList<TItem> list)
        {
            foreach (var item in list)
            {
                if (IsSearchMatch(item, terms))
                {
                    result.Add(item);
                    continue;
                }

                Collect(_bitNavRef.GetChildItems(item));
            }
        }
    }

    // What the live region announces once a search has filtered the list: how many items it matched, or
    // nothing at all while there is no search in place.
    private string? GetSearchAnnouncement()
    {
        if (_searchText.HasNoValue()) return null;

        var count = _filteredNavItems.Count;

        if (SearchAnnouncementProvider is not null) return SearchAnnouncementProvider(count);

        return count switch
        {
            0 => "No item found.",
            1 => "1 item found.",
            _ => $"{count} items found."
        };
    }

    private bool IsSearchMatch(TItem item, string[] terms)
    {
        if (SearchFilter is not null)
        {
            return terms.Any(t => SearchFilter(item, t));
        }

        var haystack = $"{_bitNavRef!.GetText(item)} {_bitNavRef.GetDescription(item)} {_bitNavRef.GetData(item)}";

        return terms.Any(t => haystack.Contains(t, StringComparison.InvariantCultureIgnoreCase));
    }

    private void HandleOnSwipeMove(BitSwipeTrapEventArgs args)
    {
        if (NoSwipe) return;
        if (IsOpen is false) return;

        _diffXPanel = args.DiffX;
        StateHasChanged();
    }

    private void HandleOnSwipeEnd(BitSwipeTrapEventArgs args)
    {
        if (NoSwipe) return;
        if (IsOpen is false) return;

        _diffXPanel = 0;
        StateHasChanged();
    }

    private async Task HandleOnSwipeTrigger(BitSwipeTrapTriggerArgs args)
    {
        if (NoSwipe) return;
        if (IsOpen is false) return;

        if ((Dir != BitDir.Rtl && args.Direction == BitSwipeDirection.Left) ||
            (Dir == BitDir.Rtl && args.Direction == BitSwipeDirection.Right))
        {
            _diffXPanel = 0;
            await ClosePanel();
            StateHasChanged();
        }
    }

    private string? GetPanelStyle(bool isToggled)
    {
        if (IsOpen is false) return $"{StyleBuilder.Value};{(isToggled ? Styles?.Toggled : string.Empty)}".Trim(';');

        var translate = ((Dir != BitDir.Rtl && _diffXPanel < 0) || (Dir == BitDir.Rtl && _diffXPanel > 0))
                            ? FormattableString.Invariant($"transform: translateX({_diffXPanel}px)")
                            : string.Empty;
        return $"{translate};{StyleBuilder.Value};{(isToggled ? Styles?.Toggled : string.Empty)}".Trim(';');
    }

    private void OnItemsSet()
    {
        SearchNavItems(_searchText);
    }
}
