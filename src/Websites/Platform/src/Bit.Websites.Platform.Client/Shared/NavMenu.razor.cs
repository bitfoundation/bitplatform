namespace Bit.Websites.Platform.Client.Shared;

public partial class NavMenu : IDisposable
{
    private bool disposed;
    private bool isNavOpen = false;
    private string searchText = string.Empty;
    private List<BitNavItem> filteredNavItems = default!;
    

    [AutoInject] private NavManuService navMenuService = default!;


    [Parameter] public List<BitNavItem> NavItems { get; set; } = [];



    protected override async Task OnInitAsync()
    {
        navMenuService.OnToggleMenu += ToggleMenu;

        HandleOnClear();
    }

    protected override void OnParametersSet()
    {
        filteredNavItems = NavItems;
    }



    private async Task ToggleMenu()
    {
        try
        {
            isNavOpen = !isNavOpen;

            await JSRuntime.ToggleBodyOverflow(isNavOpen);
        }
        catch (Exception ex)
        {
            ExceptionHandler.Handle(ex);
        }
        finally
        {
            StateHasChanged();
        }
    }

    private void HandleOnClear()
    {
        filteredNavItems = NavItems;
    }

    private void HandleValueChanged(string text)
    {
        searchText = text;
        filteredNavItems = NavItems;
        if (string.IsNullOrEmpty(text)) return;

        var flatNavLinkList = Flatten(NavItems).ToList().FindAll(link => !string.IsNullOrEmpty(link.Url));
        filteredNavItems = flatNavLinkList.FindAll(link => link.Text.Contains(text, StringComparison.InvariantCultureIgnoreCase));
    }

    private async Task HandleOnItemClick(BitNavItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Url)) return;

        searchText = string.Empty;
        filteredNavItems = NavItems;

        await ToggleMenu();
    }

    private string GetNavMenuClass()
    {
        if (string.IsNullOrEmpty(searchText))
        {
            return "side-nav";
        }
        else
        {
            return "side-nav searched-side-nav";
        }
    }

    private static IEnumerable<BitNavItem> Flatten(IEnumerable<BitNavItem> e) => e.SelectMany(c => Flatten(c.ChildItems)).Concat(e);

    public void Dispose()
    {
        if (disposed) return;

        navMenuService.OnToggleMenu -= ToggleMenu;

        disposed = true;
    }
}
