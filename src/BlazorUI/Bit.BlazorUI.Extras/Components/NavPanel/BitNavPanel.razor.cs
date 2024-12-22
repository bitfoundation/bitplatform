using Microsoft.Extensions.Options;

namespace Bit.BlazorUI;

public partial class BitNavPanel<TItem> : BitComponentBase, IDisposable where TItem : class
{
    private bool _disposed;
    private bool _isPanelOpen;
    private bool _isMenuToggled;
    private List<BitNavItem> allNavItems = [];
    private BitSearchBox _searchBoxRef = default!;
    private List<BitNavItem> _flatNavItemList = [];
    private List<BitNavItem> _filteredNavItems = [];



    /// <summary>
    /// A collection of item to display in the navigation bar.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetItems))]
    public IList<TItem> Items { get; set; } = [];



    protected override string RootElementClass => "bit-nav";

    protected override async Task OnInitializedAsync()
    {
        //CreateNavItems();
        _flatNavItemList = Flatten(allNavItems).ToList().FindAll(link => !string.IsNullOrEmpty(link.Url));

        SearchNavItems(null);

        //unsubOpenNavPanel = PubSubService.Subscribe(ClientPubSubMessages.OPEN_NAV_PANEL, async _ =>
        //{
        //    isMenuOpen = true;
        //    StateHasChanged();
        //});
    }

    private async Task HandleNavItemClick(BitNavItem item)
    {
        if (string.IsNullOrEmpty(item.Url)) return;

        _filteredNavItems = allNavItems;

        await CloseMenu();
    }

    private async Task CloseMenu()
    {
        _isPanelOpen = false;
    }

    private async Task ToggleNavPanel()
    {
        _isMenuToggled = !_isMenuToggled;
        if (_isMenuToggled)
        {
            SearchNavItems(null);
        }
    }

    private async Task ToggleForSearch()
    {
        _isMenuToggled = false;
        await Task.Delay(1);
        await _searchBoxRef.FocusAsync();
    }

    private void SearchNavItems(string? searchText)
    {
        _filteredNavItems = allNavItems;
        if (searchText is null) return;

        _filteredNavItems = allNavItems;
        if (string.IsNullOrEmpty(searchText)) return;

        var mainItems = _flatNavItemList
                            .FindAll(item => searchText.Split(' ')
                                                 .Where(t => string.IsNullOrEmpty(t) is false)
                                                 .Any(t => $"{item.Text} {item.Description}".Contains(t, StringComparison.InvariantCultureIgnoreCase)));

        var subItems = _flatNavItemList
                            .FindAll(item => searchText.Split(' ')
                                                 .Where(t => string.IsNullOrEmpty(t) is false)
                                                 .Any(t => item.Data?.ToString()?.Contains(t, StringComparison.InvariantCultureIgnoreCase) ?? false));

        _filteredNavItems = [.. mainItems, .. subItems];
    }

    private static IEnumerable<BitNavItem> Flatten(IEnumerable<BitNavItem> e) => e.SelectMany(c => Flatten(c.ChildItems)).Concat(e);

    private void OnSetItems()
    {
        if (ChildContent is not null || Options is not null || Items == _oldItems) return;

        _items = Items?.ToList() ?? [];
        _oldItems = Items;
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
