namespace Bit.BlazorUI;

/// <summary>
/// BitNavPanel is a navigation component specialized to be rendered in a vertical panel.
/// </summary>
public partial class BitNavPanel<TItem> : BitComponentBase where TItem : class
{
    private decimal diffXPanel;
    private BitNav<TItem> _bitNavRef = default!;
    private IList<TItem> _filteredNavItems = [];
    private BitSearchBox _searchBoxRef = default!;
    private IEnumerable<TItem> _flatNavItemList = [];



    /// <summary>
    /// The accent color of the nav.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Accent { get; set; }

    /// <summary>
    /// The custom icon name of the chevron-down element of each nav item.
    /// </summary>
    [Parameter] public string? ChevronDownIcon { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the nav panel.
    /// </summary>
    [Parameter] public BitNavPanelClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the nav.
    /// </summary>
    [Parameter]
    public BitColor? Color { get; set; }

    /// <summary>
    /// The custom template for when the search result is empty.
    /// </summary>
    [Parameter] public RenderFragment? EmptyListTemplate { get; set; }

    /// <summary>
    /// The custom message for when the search result is empty.
    /// </summary>
    [Parameter] public string? EmptyListMessage { get; set; }

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
    /// If true, the search box is hidden.
    /// </summary>
    [Parameter] public bool HideSearchBox { get; set; }

    /// <summary>
    /// Renders an anchor wrapping the icon to navigate to the specified url.
    /// </summary>
    [Parameter] public string? IconNavUrl { get; set; }

    /// <summary>
    /// The icon url to show in the header of the nav panel.
    /// </summary>
    [Parameter] public string? IconUrl { get; set; }

    /// <summary>
    /// The indentation value in px for each level of depth of child item.
    /// </summary>
    [Parameter] public int IndentValue { get; set; } = 16;

    /// <summary>
    /// The indentation padding in px for items without children (compensation space for chevron icon).
    /// </summary>
    [Parameter] public int IndentPadding { get; set; } = 27;

    /// <summary>
    /// The indentation padding in px for items in reversed mode.
    /// </summary>
    [Parameter] public int IndentReversedPadding { get; set; } = 4;

    /// <summary>
    /// Determines if the nav panel is open in small screens.
    /// </summary>
    [Parameter, TwoWayBound, ResetClassBuilder]
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
    /// Custom CSS classes for different parts of the nav component of the nav panel.
    /// </summary>
    [Parameter] public BitNavClassStyles? NavClasses { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the nav component of the nav panel.
    /// </summary>
    [Parameter] public BitNavClassStyles? NavStyles { get; set; }

    /// <summary>
    /// Disables the padded mode of the nav panel.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool NoPad { get; set; }

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
    /// Enables the single-expand mode in the BitNav.
    /// </summary>
    [Parameter] public bool SingleExpand { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the nav panel.
    /// </summary>
    [Parameter] public BitNavPanelClassStyles? Styles { get; set; }

    /// <summary>
    /// The top CSS property value of the root element of the nav panel in px.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public int Top { get; set; }



    protected override string RootElementClass => "bit-npn";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => FitWidth ? "bit-npn-fiw" : string.Empty);
        ClassBuilder.Register(() => FullWidth ? "bit-npn-fuw" : string.Empty);

        ClassBuilder.Register(() => IsOpen ? string.Empty : "bit-npn-cls");
        ClassBuilder.Register(() => NoPad ? "bit-npn-npd" : string.Empty);

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
    }

    protected override async Task OnInitializedAsync()
    {
        SearchNavItems(null);
    }

    private async Task HandleNavItemClick(TItem item)
    {
        await OnItemClick.InvokeAsync(item);

        if (_bitNavRef.GetUrl(item).HasNoValue()) return;

        _filteredNavItems = Items;

        await _searchBoxRef.Clear();

        await ClosePanel();
    }

    private async Task ClosePanel()
    {
        await AssignIsOpen(false);
    }

    private async Task ToggleNavPanel()
    {
        if (await AssignIsToggled(!IsToggled)) return;

        if (IsToggled)
        {
            SearchNavItems(null);
        }
    }

    private async Task ToggleForSearch()
    {
        if (await AssignIsToggled(false) is false) return;

        await Task.Delay(1);
        await _searchBoxRef.FocusAsync();
    }

    private void SearchNavItems(string? searchText)
    {
        _filteredNavItems = Items;
        if (searchText.HasNoValue() || _bitNavRef is null) return;

        _flatNavItemList = Items.Flatten(_bitNavRef.GetChildItems).Where(item => _bitNavRef.GetUrl(item).HasValue());

        var mainItems = _flatNavItemList.Where(item => searchText!.Split(' ')
                                                                  .Where(t => t.HasValue())
                                                                  .Any(t => $"{_bitNavRef.GetText(item)} {_bitNavRef.GetDescription(item)}"
                                                                             .Contains(t, StringComparison.InvariantCultureIgnoreCase)));

        var subItems = _flatNavItemList.Where(item => searchText!.Split(' ')
                                                                 .Where(t => t.HasValue())
                                                                 .Any(t => _bitNavRef.GetData(item)?.ToString()?
                                                                                     .Contains(t, StringComparison.InvariantCultureIgnoreCase) ?? false));

        _filteredNavItems = [.. mainItems, .. subItems];
    }

    private void HandleOnSwipeMove(BitSwipeTrapEventArgs args)
    {
        if (IsOpen is false) return;

        diffXPanel = args.DiffX;
        StateHasChanged();

    }
    private void HandleOnSwipeEnd(BitSwipeTrapEventArgs args)
    {
        if (IsOpen is false) return;

        diffXPanel = 0;
        StateHasChanged();
    }
    private async Task HandleOnSwipeTrigger(BitSwipeTrapTriggerArgs args)
    {
        if (IsOpen is false) return;

        if ((Dir != BitDir.Rtl && args.Direction == BitSwipeDirection.Left) ||
            (Dir == BitDir.Rtl && args.Direction == BitSwipeDirection.Right))
        {
            diffXPanel = 0;
            await ClosePanel();
            StateHasChanged();
        }
    }

    private string? GetPanelStyle()
    {
        if (IsOpen is false) return StyleBuilder.Value;

        var translate = ((Dir != BitDir.Rtl && diffXPanel < 0) || (Dir == BitDir.Rtl && diffXPanel > 0))
                            ? $"transform: translateX({diffXPanel}px)"
                            : string.Empty;
        return $"{translate};{StyleBuilder.Value}".Trim(';');
    }

    private async Task OnItemsSet()
    {
        SearchNavItems(_searchBoxRef?.Value);
    }
}
