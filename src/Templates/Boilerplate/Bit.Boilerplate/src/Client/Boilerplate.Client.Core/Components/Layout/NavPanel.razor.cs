namespace Boilerplate.Client.Core.Components.Layout;

public partial class NavPanel
{
    private bool disposed;
    private bool isMenuOpen;
    private bool isMenuToggled;
    private bool isSignOutModalOpen;
    private List<BitNavItem> allNavItems = [];
    private Action unsubOpenNavPanel = default!;
    private BitSearchBox searchBoxRef = default!;
    private Action unsubUserDataChange = default!;
    private List<BitNavItem> flatNavItemList = [];
    private List<BitNavItem> filteredNavItems = [];

    [AutoInject] private NavigationManager navManager = default!;



    protected override async Task OnInitAsync()
    {
        CreateNavItems();
        flatNavItemList = Flatten(allNavItems).ToList().FindAll(link => !string.IsNullOrEmpty(link.Url));

        SearchNavItems(null);

        unsubOpenNavPanel = PubSubService.Subscribe(PubSubMessages.OPEN_NAV_PANEL, async _ =>
        {
            isMenuOpen = true;
            StateHasChanged();
        });
    }


    private async Task DoSignOut()
    {
        isSignOutModalOpen = true;
        await CloseMenu();
    }

    private async Task HandleNavItemClick(BitNavItem item)
    {
        if (string.IsNullOrEmpty(item.Url)) return;

        filteredNavItems = allNavItems;

        await CloseMenu();
    }

    private async Task CloseMenu()
    {
        isMenuOpen = false;
    }

    private async Task ToggleNavPanel()
    {
        isMenuToggled = !isMenuToggled;
        if (isMenuToggled)
        {
            SearchNavItems(null);
        }
    }

    private async Task ToggleForSearch()
    {
        isMenuToggled = false;
        await Task.Delay(1);
        await searchBoxRef.FocusAsync();
    }

    private void SearchNavItems(string? searchText)
    {
        filteredNavItems = allNavItems;
        if (searchText is null) return;

        filteredNavItems = allNavItems;
        if (string.IsNullOrEmpty(searchText)) return;

        var mainItems = flatNavItemList
                            .FindAll(item => searchText.Split(' ')
                                                 .Where(t => string.IsNullOrEmpty(t) is false)
                                                 .Any(t => $"{item.Text} {item.Description}".Contains(t, StringComparison.InvariantCultureIgnoreCase)));

        var subItems = flatNavItemList
                            .FindAll(item => searchText.Split(' ')
                                                 .Where(t => string.IsNullOrEmpty(t) is false)
                                                 .Any(t => item.Data?.ToString()?.Contains(t, StringComparison.InvariantCultureIgnoreCase) ?? false));

        filteredNavItems = [.. mainItems, .. subItems];
    }

    private static IEnumerable<BitNavItem> Flatten(IEnumerable<BitNavItem> e) => e.SelectMany(c => Flatten(c.ChildItems)).Concat(e);


    protected override async ValueTask DisposeAsync(bool disposing)
    {
        await base.DisposeAsync(disposing);

        if (disposed || disposing is false) return;

        unsubOpenNavPanel?.Invoke();
        unsubUserDataChange?.Invoke();

        disposed = true;
    }
}
