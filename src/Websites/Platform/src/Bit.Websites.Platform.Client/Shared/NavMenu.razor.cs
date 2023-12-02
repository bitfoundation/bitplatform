namespace Bit.Websites.Platform.Client.Shared;

public partial class NavMenu : IDisposable
{
    private bool disposed;
    private bool isNavOpen = false;
    private string searchText = string.Empty;
    private List<BitNavItem> filteredNavItems = default!;
    private readonly List<BitNavItem> allNavItems = new()
    {
        new BitNavItem { Text = "Overview", Url = "/templates/overview", AdditionalUrls = new string[] { "/admin-panel/overview", "/todo-template/overview" } },
        new BitNavItem { Text = "Development prerequisites", Url = "/templates/development-prerequisites", AdditionalUrls = new string[] { "/admin-panel/development-prerequisites", "/todo-template/development-prerequisites" } },
        new BitNavItem { Text = "Create project", Url = "/templates/create-project", AdditionalUrls = new string[] { "/admin-panel/create-project", "/todo-template/create-project" } },
        new BitNavItem { Text = "Project structure", Url = "/templates/project-structure", AdditionalUrls = new string[] { "/admin-panel/project-structure", "/todo-template/project-structure" } },
        new BitNavItem { Text = "Database", Url = "/templates/database", AdditionalUrls = new string[] { "/admin-panel/database", "/todo-template/database" } },
        new BitNavItem { Text = "Run", Url = "/templates/run", AdditionalUrls = new string[] { "/admin-panel/run", "/todo-template/run" } },
        new BitNavItem { Text = "App models", Url = "/templates/app-models", AdditionalUrls = new string[] { "/admin-panel/hosting-models", "/todo-template/hosting-models" } },
        new BitNavItem { Text = "Deployment type", Url = "/templates/deployment-type", AdditionalUrls = new string[] { "/admin-panel/deployment-type", "/todo-template/deployment-type" } },
        new BitNavItem { Text = "Cache mechanism", Url = "/templates/cache-mechanism", AdditionalUrls = new string[] { "/admin-panel/cache-mechanism", "/todo-template/cache-mechanism" } },
        new BitNavItem { Text = "DevOps", Url = "/templates/devops", AdditionalUrls = new string[] { "/admin-panel/devops", "/todo-template/devops" } },
        new BitNavItem { Text = "Platform integration", Url = "/templates/platform-integration", AdditionalUrls = new string[] { "/admin-panel/platform-integration", "/todo-template/platform-integration" } },
        new BitNavItem { Text = "Settings", Url = "/templates/settings", AdditionalUrls = new string[] { "/admin-panel/settings", "/todo-template/settings" } },
        new BitNavItem { Text = "Exception handling", Url = "/templates/exception-handling", AdditionalUrls = new string[] { "/admin-panel/exception-handling", "/todo-template/exception-handling" } },
        new BitNavItem { Text = "Multilingualism", Url = "/templates/multilingualism", AdditionalUrls = new string[] { "/admin-panel/multilingualism", "/todo-template/multilingualism" } },
    };


    [AutoInject] private NavManuService navMenuService = default!;


    public string CurrentUrl { get; set; } = default!;

    protected override async Task OnInitAsync()
    {
        navMenuService.OnToggleMenu += ToggleMenu;

        HandleOnClear();

        base.OnInitialized();
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
        filteredNavItems = allNavItems;
    }

    private void HandleValueChanged(string text)
    {
        searchText = text;
        filteredNavItems = allNavItems;
        if (string.IsNullOrEmpty(text)) return;

        var flatNavLinkList = Flatten(allNavItems).ToList().FindAll(link => !string.IsNullOrEmpty(link.Url));
        filteredNavItems = flatNavLinkList.FindAll(link => link.Text.Contains(text, StringComparison.InvariantCultureIgnoreCase));
    }

    private async Task HandleOnItemClick(BitNavItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Url)) return;

        searchText = string.Empty;
        filteredNavItems = allNavItems;

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
