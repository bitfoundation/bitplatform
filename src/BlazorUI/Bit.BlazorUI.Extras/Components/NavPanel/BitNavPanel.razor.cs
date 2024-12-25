namespace Bit.BlazorUI;

public partial class BitNavPanel<TItem> : BitComponentBase, IDisposable where TItem : class
{
    private bool _disposed;
    private BitNav<TItem> _bitNavRef = default!;
    private IList<TItem> _filteredNavItems = [];
    private BitSearchBox _searchBoxRef = default!;
    private IEnumerable<TItem> _flatNavItemList = [];



    /// <summary>
    /// The custom template to render as the footer of the nav panel.
    /// </summary>
    [Parameter] public RenderFragment? FooterTemplate { get; set; }

    /// <summary>
    /// The custom template to render as the header of the nav panel.
    /// </summary>
    [Parameter] public RenderFragment? HeaderTemplate { get; set; }

    /// <summary>
    /// The custom template to render as the header of the nav panel.
    /// </summary>
    [Parameter] public string? ImageUrl { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [Parameter, TwoWayBound, ResetClassBuilder]
    public bool IsOpen { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [Parameter, TwoWayBound]
    public bool IsToggled { get; set; }

    /// <summary>
    /// A collection of items to display in the nav panel.
    /// </summary>
    [Parameter] public IList<TItem> Items { get; set; } = [];



    protected override string RootElementClass => "bit-npn";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => IsOpen ? string.Empty : "bit-npn-cls");
    }

    protected override async Task OnInitializedAsync()
    {
        SearchNavItems(null);
    }

    private async Task HandleNavItemClick(TItem item)
    {
        if (string.IsNullOrEmpty(_bitNavRef.GetUrl(item))) return;

        _filteredNavItems = Items;

        await CloseMenu();
    }

    private async Task CloseMenu()
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
