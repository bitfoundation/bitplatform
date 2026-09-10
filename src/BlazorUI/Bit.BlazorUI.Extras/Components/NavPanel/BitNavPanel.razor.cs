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
/// <br />
/// The drawer is a modal surface for as long as it covers the page: it reports itself as a dialog, holds the
/// page it covers from scrolling and the focus from leaving it, and hands the focus back to whatever had it
/// once it closes - each of which <see cref="NoScrollLock"/>, <see cref="NoFocusTrap"/> and
/// <see cref="NoRestoreFocus"/> give back, and none of which a panel with <see cref="NoOverlay"/> takes in
/// the first place.
/// </remarks>
public partial class BitNavPanel<TItem> : BitComponentBase where TItem : class
{
    // The accessible names the panel falls back to. They are only ever read where the API offers no name of
    // its own (ToggleAriaLabel, SearchBoxPlaceholder), so a localized app overrides them through those.
    private const string DefaultLogoAriaLabel = "Home";
    private const string DefaultSearchAriaLabel = "Search";
    private const string DefaultPanelAriaLabel = "Navigation";
    private const string DefaultCloseAriaLabel = "Close the navigation panel";
    private const string DefaultExpandAriaLabel = "Expand the navigation panel";
    private const string DefaultCollapseAriaLabel = "Collapse the navigation panel";

    // The rail that expands while the keyboard is inside it collapses again a moment after the focus has
    // left, since the focus leaving one item and landing on the next arrives as two separate events: a
    // collapse applied to the first of them would flicker the rail shut and open again on every arrow key.
    private const int FOCUS_OUT_DELAY_MS = 250;

    private bool _isHovered;
    private bool _isDrawer;
    private bool _isFocused;
    private int _focusOutToken;
    private bool _scrollLocked;
    private bool _focusTrapped;
    private decimal _diffXPanel;
    private string? _searchText;
    private bool _focusOnOpenPending;
    private bool _focusOriginCaptured;
    private bool _focusSearchBoxPending;
    private BitNav<TItem>? _bitNavRef;
    private BitSearchBox? _searchBoxRef;
    private IList<TItem> _filteredNavItems = [];



    [Inject] private IJSRuntime _js { get; set; } = default!;



    // The scroller of the application shell the panel was declared inside of, cascaded by BitAppShell under
    // this name. What the drawer holds while it covers the page is whatever actually scrolls behind it,
    // which in a shell is the region the shell scrolls rather than the document.
    [CascadingParameter(Name = "BitAppShell.Container")]
    private ElementReference? AppShellContainer { get; set; }



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
    /// The aria-label and the tooltip of the close button of the nav panel.
    /// </summary>
    [Parameter] public string? CloseAriaLabel { get; set; }

    /// <summary>
    /// The icon of the close button of the nav panel.
    /// Takes precedence over <see cref="CloseIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? CloseIcon { get; set; }

    /// <summary>
    /// The name of the icon of the close button of the nav panel.
    /// </summary>
    [Parameter] public string? CloseIconName { get; set; }

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
    /// Replacing the header also replaces the buttons it holds - the toggle button, and the close button of
    /// <see cref="ShowCloseButton"/> - so a custom header that wants either of them renders a control of its
    /// own that calls <see cref="Toggle"/> or <see cref="Close"/>.
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
    /// The accessible name of the logo in the header of the nav panel: the name of the link an
    /// <see cref="IconNavUrl"/> wraps it in, and the alternative text of the image otherwise.
    /// The panel falls back to <see cref="BitComponentBase.AriaLabel"/> and then to a built-in name, so set
    /// this whenever the name of the navigation landmark is not what the logo itself should be called.
    /// </summary>
    [Parameter] public string? IconAriaLabel { get; set; }

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
    /// Stops the open drawer of a small screen from holding the focus inside itself.
    /// The focus is only ever held while the panel actually covers the page, which is the state its overlay
    /// is rendered in, so a panel with <see cref="NoOverlay"/> never holds it in the first place.
    /// </summary>
    [Parameter] public bool NoFocusTrap { get; set; }

    /// <summary>
    /// Removes the overlay that is rendered behind the open nav panel in small screens.
    /// Without it the drawer no longer covers the page: it stops holding the focus and the page behind it
    /// keeps scrolling.
    /// </summary>
    [Parameter] public bool NoOverlay { get; set; }

    /// <summary>
    /// Disables the padded mode of the nav panel.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool NoPad { get; set; }

    /// <summary>
    /// Stops the closing drawer of a small screen from handing the focus back to the element that had it
    /// when the drawer opened. Only ever read by a panel that took the focus in the first place
    /// (see <see cref="AutoFocus"/>).
    /// </summary>
    [Parameter] public bool NoRestoreFocus { get; set; }

    /// <summary>
    /// Lets the page behind the open drawer of a small screen keep scrolling.
    /// The page is only ever held while the panel actually covers it, which is the state its overlay is
    /// rendered in, so a panel with <see cref="NoOverlay"/> never holds it in the first place.
    /// </summary>
    [Parameter] public bool NoScrollLock { get; set; }

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
    /// The edge the off-canvas drawer of a small screen comes from, and the side it is docked to while it
    /// is open. The default is the starting edge of the text direction.
    /// It has no effect on a wide screen, where the panel is a column in the normal flow of the page.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitNavPanelPosition Position { get; set; }

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
    /// The icon of the button that the collapsed (rail) nav panel shows in place of its search box.
    /// Takes precedence over <see cref="SearchIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? SearchIcon { get; set; }

    /// <summary>
    /// The name of the icon of the button that the collapsed (rail) nav panel shows in place of its search
    /// box.
    /// </summary>
    [Parameter] public string? SearchIconName { get; set; }

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
    /// Renders a close button in the header of the nav panel, on the screens the panel is an off-canvas
    /// drawer on. It is the control the toggle button is not there: the toggle collapses a permanent panel
    /// into a rail, which a drawer that is either open or gone has no state for, and a drawer that is only
    /// dismissed by its overlay, the Escape key or a swipe offers a touch user nothing to aim at.
    /// </summary>
    [Parameter] public bool ShowCloseButton { get; set; }

    /// <summary>
    /// The size of the nav items.
    /// </summary>
    [Parameter] public BitSize? Size { get; set; }

    /// <summary>
    /// Pins the two ends of the nav panel - the header with its search box, and the footer - in place and
    /// scrolls only the items between them, instead of scrolling the whole panel as one.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool StickyEnds { get; set; }

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
    /// Whether an item of the nav is currently expanded.
    /// </summary>
    public bool IsItemExpanded(TItem item) => _bitNavRef?.IsItemExpanded(item) is true;

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
        ClassBuilder.Register(() => Position is BitNavPanelPosition.End ? "bit-npn-end" : string.Empty);
        ClassBuilder.Register(() => StickyEnds ? "bit-npn-ste" : string.Empty);

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
        // The automatic mode has no initial selection of its own to make: the current URL is what decides
        // there, and seeding a selection would light up an item the page is not on.
        if (NavMode is BitNavMode.Manual && SelectedItem is null && DefaultSelectedItem is not null)
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

        // Everything the drawer of a small screen holds while it covers the page - the page it stops from
        // scrolling, the focus it keeps inside itself - is taken and handed back here rather than at the
        // moment a single parameter changes: whether the panel covers the page at all is decided by three
        // of them together (the screen, the open state and the overlay), and each step of it does nothing
        // unless it is the one that has actually changed.
        // It runs before the two focus moves below: the element the focus is handed back to on the way out
        // is the one that had it on the way in, which is no longer true once the panel has taken it.
        await UpdateModalState();

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
            // The keyboard reaches the rail the way the pointer does: an item that takes the focus opens the
            // panel it sits in, otherwise the text of the items would be readable by pointer only.
            ["onfocusin"] = EventCallback.Factory.Create<FocusEventArgs>(this, () => _isFocused = true),
            ["onfocusout"] = EventCallback.Factory.Create<FocusEventArgs>(this, HandleOnFocusOut),
        };
    }

    // The focus leaving an item and landing on the next one arrives as two events, in that order, so the
    // collapse waits to see whether anything inside the panel has taken the focus in the meantime.
    private async Task HandleOnFocusOut()
    {
        var token = ++_focusOutToken;

        await Task.Delay(FOCUS_OUT_DELAY_MS);

        if (IsDisposed || token != _focusOutToken) return;

        try
        {
            // The delay above leaves the renderer's thread behind, so the state change is handed back to it
            // rather than applied from whichever thread the timer completed on. A component torn down while
            // the delay was running has no renderer to hand it back to, and nothing left to render either.
            await InvokeAsync(() =>
            {
                _isFocused = false;

                StateHasChanged();
            });
        }
        catch (ObjectDisposedException) { } // we can ignore this exception here
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

                Collect(_bitNavRef!.GetChildItems(item));
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
        // Every word of the search term has to be somewhere in the item, which is what narrows the list as
        // more of them are typed. Matching any one of them would widen it with every keystroke instead.
        if (SearchFilter is not null)
        {
            return terms.All(t => SearchFilter(item, t));
        }

        var haystack = $"{_bitNavRef!.GetText(item)} {_bitNavRef.GetDescription(item)} {_bitNavRef.GetData(item)}";

        return terms.All(t => haystack.Contains(t, StringComparison.InvariantCultureIgnoreCase));
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

        // The swipe that closes the drawer goes towards the edge the drawer came from.
        if (args.Direction == (_IsDockedAtEnd ? BitSwipeDirection.Right : BitSwipeDirection.Left))
        {
            _diffXPanel = 0;
            await ClosePanel();
            StateHasChanged();
        }
    }

    private string? GetPanelStyle(bool isToggled)
    {
        if (IsOpen is false) return $"{StyleBuilder.Value};{(isToggled ? Styles?.Toggled : string.Empty)}".Trim(';');

        var translate = (_IsDockedAtEnd ? _diffXPanel > 0 : _diffXPanel < 0)
                            ? FormattableString.Invariant($"transform: translateX({_diffXPanel}px)")
                            : string.Empty;
        return $"{translate};{StyleBuilder.Value};{(isToggled ? Styles?.Toggled : string.Empty)}".Trim(';');
    }

    private void OnItemsSet()
    {
        SearchNavItems(_searchText);
    }

    // Which edge the drawer is docked to, and so which way the swipe that closes it goes. The position is
    // expressed in the text direction, so the End of a right-to-left layout is the left of the screen.
    private bool _IsDockedAtEnd => (Position is BitNavPanelPosition.End) != (Dir is BitDir.Rtl);

    // The panel is a modal drawer while it covers the page: only on a small screen, only while it is open,
    // and only with the overlay that is what makes it cover anything at all.
    private bool _IsModalDrawer => _isDrawer && IsOpen && NoOverlay is false && IsEnabled;

    // Reports the screen the panel renders on: below the breakpoint it is an off-canvas drawer, above it a
    // column of the page. A render is only asked for when the answer actually changes.
    private async Task HandleScreenChange(bool isDrawer)
    {
        if (_isDrawer == isDrawer) return;

        _isDrawer = isDrawer;

        await UpdateModalState();

        StateHasChanged();
    }

    private async Task UpdateModalState()
    {
        if (_IsModalDrawer)
        {
            await CaptureFocusOrigin();
            await LockScroll();
            await SetupFocusTrap();
        }
        else
        {
            await DisposeFocusTrap();
            await UnlockScroll();
            await RestoreFocusOrigin();
        }
    }

    private async Task SetupFocusTrap()
    {
        if (NoFocusTrap || _focusTrapped || IsDisposed || IsRendered is false) return;

        _focusTrapped = true;

        try
        {
            await _js.BitUtilsSetupFocusTrap(_Id);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private async Task DisposeFocusTrap()
    {
        // Only what was taken is handed back, and the hold is given up whether or not the call goes
        // through, so the panel can never end up holding a focus it has already let go of.
        if (_focusTrapped is false) return;

        _focusTrapped = false;

        try
        {
            await _js.BitUtilsDisposeFocusTrap(_Id);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private async Task CaptureFocusOrigin()
    {
        // Nothing is handed back by a panel that never takes the focus, or by one that was told not to hand
        // anything back, so nothing is recorded for either of them.
        if (AutoFocus is false || NoRestoreFocus || _focusOriginCaptured || IsDisposed || IsRendered is false) return;

        _focusOriginCaptured = true;

        try
        {
            await _js.BitUtilsCaptureFocusOrigin(_Id);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private async Task RestoreFocusOrigin()
    {
        if (_focusOriginCaptured is false) return;

        _focusOriginCaptured = false;

        if (NoRestoreFocus || IsDisposed) return;

        try
        {
            await _js.BitUtilsRestoreFocusOrigin(_Id);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private async Task LockScroll()
    {
        if (NoScrollLock || _scrollLocked || IsDisposed || IsRendered is false) return;

        _scrollLocked = true;

        try
        {
            if (AppShellContainer.HasValue)
            {
                await _js.BitUtilsLockScroll(_Id, AppShellContainer.Value);
            }
            else
            {
                await _js.BitUtilsLockScroll(_Id);
            }
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private async Task UnlockScroll()
    {
        if (_scrollLocked is false) return;

        _scrollLocked = false;

        try
        {
            await _js.BitUtilsUnlockScroll(_Id);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        // The page and the focus are handed back before the component goes: a drawer disposed while it is
        // open would otherwise leave the page held by a key nothing will ever release again.
        await DisposeFocusTrap();
        await UnlockScroll();

        try
        {
            if (_focusOriginCaptured)
            {
                _focusOriginCaptured = false;

                await _js.BitUtilsDisposeFocusOrigin(_Id);
            }
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here

        await base.DisposeAsync(disposing);
    }
}
