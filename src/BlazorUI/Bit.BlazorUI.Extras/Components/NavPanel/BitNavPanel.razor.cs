namespace Bit.BlazorUI;

public partial class BitNavPanel<TItem> : BitComponentBase, IDisposable where TItem : class
{
    private bool _disposed;
    private bool _isPanelOpen;
    private bool _isPanelToggled;
    private BitNav<TItem> _bitNavRef = default!;
    private IList<TItem> _filteredNavItems = [];
    private BitSearchBox _searchBoxRef = default!;
    private IEnumerable<TItem> _flatNavItemList = [];


    /// <summary>
    /// The custom template to render as the header of the nav panel.
    /// </summary>
    [Parameter] public RenderFragment? HeaderTemplate { get; set; }

    /// <summary>
    /// The custom template to render as the header of the nav panel.
    /// </summary>
    [Parameter] public string? ImageUrl { get; set; }

    /// <summary>
    /// A collection of items to display in the nav panel.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetItems))]
    public IList<TItem> Items { get; set; } = [];



    protected override string RootElementClass => "bit-npn";

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
        _isPanelOpen = false;
    }

    private async Task ToggleNavPanel()
    {
        _isPanelToggled = !_isPanelToggled;

        if (_isPanelToggled)
        {
            SearchNavItems(null);
        }
    }

    private async Task ToggleForSearch()
    {
        _isPanelToggled = false;
        await Task.Delay(1);
        await _searchBoxRef.FocusAsync();
    }

    private void SearchNavItems(string? searchText)
    {
        _filteredNavItems = Items;
        if (searchText.HasNoValue()) return;

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

    private void OnSetItems()
    {
        _flatNavItemList = Items.Flatten(_bitNavRef.GetChildItems).Where(item => _bitNavRef.GetUrl(item).HasValue());
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
