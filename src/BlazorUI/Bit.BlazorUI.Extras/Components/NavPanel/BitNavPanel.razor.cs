namespace Bit.BlazorUI;

public partial class BitNavPanel<TItem> : BitComponentBase, IDisposable where TItem : class
{
    private bool _disposed;
    private decimal diffXPanel;
    private BitNav<TItem> _bitNavRef = default!;
    private IList<TItem> _filteredNavItems = [];
    private BitSearchBox _searchBoxRef = default!;
    private ElementReference _containerRef = default;
    private IEnumerable<TItem> _flatNavItemList = [];


    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Custom CSS classes for different parts of the nav panel.
    /// </summary>
    [Parameter] public BitNavPanelClassStyles? Classes { get; set; }

    /// <summary>
    /// The custom template for when the search result is empty.
    /// </summary>
    [Parameter] public RenderFragment? EmptyListTemplate { get; set; }

    /// <summary>
    /// The custom message for when the search result is empty.
    /// </summary>
    [Parameter] public string? EmptyListMessage { get; set; }

    /// <summary>
    /// The custom template to render as the footer of the nav panel.
    /// </summary>
    [Parameter] public RenderFragment? Footer { get; set; }

    /// <summary>
    /// The custom template to render as the header of the nav panel.
    /// </summary>
    [Parameter] public RenderFragment? Header { get; set; }

    /// <summary>
    /// The icon url to show in the header of the nav panel.
    /// </summary>
    [Parameter] public string? IconUrl { get; set; }

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
    /// A collection of items to display in the nav panel.
    /// </summary>
    [Parameter] public IList<TItem> Items { get; set; } = [];

    /// <summary>
    /// Custom CSS classes for different parts of the nav component of the nav panel.
    /// </summary>
    [Parameter] public BitNavClassStyles? NavClasses { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the nav component of the nav panel.
    /// </summary>
    [Parameter] public BitNavClassStyles? NavStyles { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the search box of the nav panel.
    /// </summary>
    [Parameter] public BitSearchBoxClassStyles? SearchBoxClasses { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the search box of the nav panel.
    /// </summary>
    [Parameter] public BitSearchBoxClassStyles? SearchBoxStyles { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the nav panel.
    /// </summary>
    [Parameter] public BitNavPanelClassStyles? Styles { get; set; }

    /// <summary>
    /// Enables the toggle feature of the nav panel.
    /// </summary>
    [Parameter] public bool Togglable { get; set; }

    /// <summary>
    /// The top CSS property value of the root element of the nav panel in px.
    /// </summary>
    [Parameter] public int Top { get; set; }



    protected override string RootElementClass => "bit-npn";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);
        ClassBuilder.Register(() => IsOpen ? string.Empty : "bit-npn-cls");
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
        if (string.IsNullOrEmpty(_bitNavRef.GetUrl(item))) return;

        _filteredNavItems = Items;

        await ClosePanel();
    }

    private async Task ClosePanel()
    {
        if (await AssignIsOpen(false)) return;
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
        if (await AssignIsToggled(false)) return;

        await Task.Delay(1);
        await _searchBoxRef.FocusAsync();
    }

    private void SearchNavItems(string? searchText)
    {
        _filteredNavItems = Items;
        if (searchText.HasNoValue()) return;

        _flatNavItemList = Items.Flatten(_bitNavRef.GetChildItems).Where(item => _bitNavRef.GetUrl(item).HasValue());

        var mainItems = _flatNavItemList.Where(item => searchText!.Split(' ')
                                                                  .Where(t => string.IsNullOrEmpty(t) is false)
                                                                  .Any(t => $"{_bitNavRef.GetText(item)} {_bitNavRef.GetDescription(item)}"
                                                                             .Contains(t, StringComparison.InvariantCultureIgnoreCase)));

        var subItems = _flatNavItemList.Where(item => searchText!.Split(' ')
                                                                 .Where(t => string.IsNullOrEmpty(t) is false)
                                                                 .Any(t => _bitNavRef.GetData(item)?.ToString()?
                                                                                     .Contains(t, StringComparison.InvariantCultureIgnoreCase) ?? false));

        _filteredNavItems = [.. mainItems, .. subItems];
    }

    private decimal _oldDiffY = 0;
    private void HandleOnSwipeMove(BitSwipeTrapEventArgs args)
    {
        if (IsOpen is false) return;

        if (Math.Abs(args.DiffX) > Math.Abs(args.DiffY))
        {
            diffXPanel = args.DiffX;
            StateHasChanged();
        }
        else
        {
            var diff = args.DiffY - _oldDiffY;
            _js.BitExtrasScrollBy(RootElement, 0, diff > 0 ? -10 : 10);
            _oldDiffY = args.DiffY;
        }

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

    private string GetPanelStyle()
    {
        if (IsOpen is false) return string.Empty;

        var translate = ((Dir != BitDir.Rtl && diffXPanel < 0) || (Dir == BitDir.Rtl && diffXPanel > 0))
                            ? $"transform: translateX({diffXPanel}px)"
                            : string.Empty;
        return $"{translate};{StyleBuilder.Value}".Trim(';');
    }


    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing is false || _disposed) return;

        _disposed = true;
    }
}
