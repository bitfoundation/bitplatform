namespace Boilerplate.Client.Core.Components.Layout;

public partial class NavPanel
{
    private bool disposed;
    private bool isPanelOpen;
    private bool isPanelToggled;
    private bool isSignOutConfirmOpen;
    private List<BitNavItem> allNavItems = [];
    private Action unsubOpenNavPanel = default!;
    private BitSearchBox searchBoxRef = default!;
    private Action unsubUserDataChange = default!;
    private List<BitNavItem> flatNavItemList = [];
    private List<BitNavItem> filteredNavItems = [];


    [CascadingParameter] private BitDir? currentDir { get; set; }


    protected override async Task OnInitAsync()
    {
        CreateNavItems();
        flatNavItemList = Flatten(allNavItems).ToList().FindAll(link => !string.IsNullOrEmpty(link.Url));

        SearchNavItems(null);

        unsubOpenNavPanel = PubSubService.Subscribe(ClientPubSubMessages.OPEN_NAV_PANEL, async _ =>
        {
            isPanelOpen = true;
            StateHasChanged();
        });
    }


    private void ShowSignOutConfirm()
    {
        isSignOutConfirmOpen = true;
        ClosePanel();
    }

    private void HandleNavItemClick(BitNavItem item)
    {
        if (string.IsNullOrEmpty(item.Url)) return;

        filteredNavItems = allNavItems;

        ClosePanel();
    }

    private void ClosePanel()
    {
        isPanelOpen = false;
    }

    private void TogglePanel()
    {
        isPanelToggled = !isPanelToggled;
        if (isPanelToggled)
        {
            SearchNavItems(null);
        }
    }

    private async Task ToggleForSearch()
    {
        isPanelToggled = false;
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


    private decimal diffXPanel;
    private void HandleOnSwipeMove(BitSwipeTrapEventArgs args)
    {
        if (isPanelOpen is false) return;
        
        diffXPanel = args.DiffX;
        StateHasChanged();
    }
    private void HandleOnSwipeEnd(BitSwipeTrapEventArgs args)
    {
        if (isPanelOpen is false) return;

        diffXPanel = 0;
        StateHasChanged();
    }
    private void HandleOnSwipeTrigger(BitSwipeTrapTriggerArgs args)
    {
        if (isPanelOpen is false) return;

        if ((currentDir != BitDir.Rtl && args.Direction == BitSwipeDirection.Left) ||
            (currentDir == BitDir.Rtl && args.Direction == BitSwipeDirection.Right))
        {
            diffXPanel = 0;
            ClosePanel();
            StateHasChanged();
        }
    }
    private string GetPanelStyle()
    {
        if (isPanelOpen is false) return string.Empty;

        return ((currentDir != BitDir.Rtl && diffXPanel < 0) || (currentDir == BitDir.Rtl && diffXPanel > 0))
                ? $"transform: translateX({diffXPanel}px)"
                : string.Empty;
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
