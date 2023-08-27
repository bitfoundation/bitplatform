using Bit.Websites.Platform.Web.Services;

namespace Bit.Websites.Platform.Web.Shared;

public partial class NavMenu : IDisposable
{
    private bool _disposed;
    private bool _isNavOpen = false;
    private string _searchText = string.Empty;
    private List<BitNavItem> _filteredNavItems = default!;
    private readonly List<BitNavItem> _allNavItems = new()
    {
        new BitNavItem { Text = "Overview", Url = "/templates/overview"},
        new BitNavItem { Text = "Development prerequisites", Url = "/templates/development-prerequisites"},
        new BitNavItem { Text = "Create project", Url = "/templates/create-project"},
        new BitNavItem { Text = "Project structure", Url = "/templates/project-structure"},
        new BitNavItem { Text = "Database", Url = "/templates/database"},
        new BitNavItem { Text = "Run", Url = "/templates/run"},
        new BitNavItem { Text = "Hosting models", Url = "/templates/hosting-models"},
        new BitNavItem { Text = "Deployment type", Url = "/templates/deployment-type"},
        new BitNavItem { Text = "Settings", Url = "/templates/settings"},
        new BitNavItem { Text = "Exception handling", Url = "/templates/exception-handling"},
        new BitNavItem { Text = "Cache mechanism", Url = "/templates/cache-mechanism"},
        new BitNavItem { Text = "Multilingualism", Url = "/templates/multilingualism"},
        new BitNavItem { Text = "DevOps", Url = "/templates/devops"},
        new BitNavItem { Text = "Platform integration", Url = "/templates/platform-integration"},
        new BitNavItem { Text = "Contribute", Url = "/templates/contribute"}
    };


    [AutoInject] private NavManuService _navMenuService = default!;


    public string CurrentUrl { get; set; } = default!;

    protected override void OnInitialized()
    {
        _navMenuService.OnToggleMenu += ToggleMenu;

        HandleOnClear();

        base.OnInitialized();
    }


    private async Task ToggleMenu()
    {
        try
        {
            _isNavOpen = !_isNavOpen;

            await JSRuntime.InvokeVoidAsync("toggleBodyOverflow", _isNavOpen);
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
        _filteredNavItems = _allNavItems;
    }

    private void HandleValueChanged(string text)
    {
        _searchText = text;
        _filteredNavItems = _allNavItems;
        if (string.IsNullOrEmpty(text)) return;

        var flatNavLinkList = Flatten(_allNavItems).ToList().FindAll(link => !string.IsNullOrEmpty(link.Url));
        _filteredNavItems = flatNavLinkList.FindAll(link => link.Text.Contains(text, StringComparison.InvariantCultureIgnoreCase));
    }

    private async Task HandleOnItemClick(BitNavItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Url)) return;

        _searchText = string.Empty;
        _filteredNavItems = _allNavItems;

        await ToggleMenu();
    }

    private string GetNavMenuClass()
    {
        if (string.IsNullOrEmpty(_searchText))
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
        if (_disposed) return;

        _navMenuService.OnToggleMenu -= ToggleMenu;

        _disposed = true;
    }
}
