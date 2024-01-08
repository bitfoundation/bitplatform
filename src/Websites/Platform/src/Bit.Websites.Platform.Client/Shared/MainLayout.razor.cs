using Microsoft.AspNetCore.Components.Routing;

namespace Bit.Websites.Platform.Client.Shared;

public partial class MainLayout : IDisposable
{
    [AutoInject] private NavigationManager navigationManager = default!;

    private bool isDocsRoute;
    private bool isTemplateDocRoute;
    private bool isBswupDocRoute;


    private readonly List<BitNavItem> templatesNavItems = new()
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

    private readonly List<BitNavItem> bswupNavItems = new()
    {
        new BitNavItem { Text = "Overview", Url = "/bswup/overview" },
        new BitNavItem { Text = "Install", Url = "/bswup/install" },
        new BitNavItem { Text = "Scripts", Url = "/bswup/scripts" },
        new BitNavItem { Text = "Events", Url = "/bswup/events" },
        new BitNavItem { Text = "Service Worker", Url = "/bswup/service-worker" },
        new BitNavItem { Text = "Caching", Url = "/bswup/caching" },
    };


    protected override Task OnInitializedAsync()
    {
        SetCurrentUrl();

        navigationManager.LocationChanged += OnLocationChanged;

        return base.OnInitializedAsync();
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        SetCurrentUrl();

        StateHasChanged();
    }

    private void SetCurrentUrl()
    {
        var currentUrl = navigationManager.Uri.Replace(navigationManager.BaseUri, "/", StringComparison.InvariantCultureIgnoreCase);

        isTemplateDocRoute = currentUrl.Contains("templates") || currentUrl.Contains("admin-panel") || currentUrl.Contains("todo-template");
        isBswupDocRoute = currentUrl.Contains("bswup");

        isDocsRoute = isTemplateDocRoute || isBswupDocRoute;
    }

    public void Dispose()
    {
        navigationManager.LocationChanged -= OnLocationChanged;
    }
}
