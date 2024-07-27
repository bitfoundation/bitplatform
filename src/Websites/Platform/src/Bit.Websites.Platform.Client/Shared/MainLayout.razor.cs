using Microsoft.AspNetCore.Components.Routing;

namespace Bit.Websites.Platform.Client.Shared;

public partial class MainLayout : IDisposable
{
    [AutoInject] private NavigationManager navigationManager = default!;

    private bool isDocsRoute;
    private bool isLcncDocRoute;
    private bool isTemplateDocRoute;
    private bool isBswupDocRoute;
    private bool isBesqlDocRoute;
    private bool isButilDocRoute;

    private List<BitNavItem> navItems = [];



    private readonly List<BitNavItem> lcncNavItems =
    [
        new BitNavItem { Text = "Overview", Url = "/lowcode-nocode/overview" },
        new BitNavItem { Text = "Benefits", Url = "/lowcode-nocode/benefits" },
        new BitNavItem { Text = "Specs", Url = "/lowcode-nocode/specs" },
        new BitNavItem { Text = "Customizations", Url = "/lowcode-nocode/customizations" },
        new BitNavItem { Text = "Comparison", Url = "/lowcode-nocode/comparison" },
        new BitNavItem { Text = "Stats", Url = "/lowcode-nocode/stats" },
    ];

    private readonly List<BitNavItem> templatesNavItems =
    [
        new BitNavItem { Text = "Overview", Url = "/templates/overview", AdditionalUrls = new string[] { "/admin-panel/overview", "/todo-template/overview" } },
        new BitNavItem { Text = "Samples", Url = "/templates/samples" },
        new BitNavItem { Text = "Getting started", Url = "/templates/getting-started", AdditionalUrls = new string[] { "/admin-panel/getting-started", "/todo-template/getting-started" } },
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
    ];

    private readonly List<BitNavItem> bswupNavItems =
    [
        new BitNavItem { Text = "Overview", Url = "/bswup/overview" },
        new BitNavItem { Text = "Install", Url = "/bswup/install" },
        new BitNavItem { Text = "Scripts", Url = "/bswup/scripts" },
        new BitNavItem { Text = "Events", Url = "/bswup/events" },
        new BitNavItem { Text = "Service Worker", Url = "/bswup/service-worker" },
        new BitNavItem { Text = "BswupProgress", Url = "/bswup/progress" },
    ];

    private readonly List<BitNavItem> besqlNavItems =
    [
        new BitNavItem { Text = "Overview", Url = "/besql/overview" },
        new BitNavItem { Text = "Install", Url = "/besql/install" },
        new BitNavItem { Text = "Usage", Url = "/besql/usage" },
    ];

    private readonly List<BitNavItem> butilNavItems =
    [
        new BitNavItem { Text = "Overview", Url = "/butil/overview" },
        new BitNavItem { Text = "Install", Url = "/butil/install" },
        new BitNavItem { Text = "Setup", Url = "/butil/setup" },
        new BitNavItem { Text = "Crypto", Url = "/butil/crypto" },
        new BitNavItem { Text = "Clipboard", Url = "/butil/clipboard" },
        new BitNavItem { Text = "Keyboard", Url = "/butil/keyboard" },
        new BitNavItem { Text = "Console", Url = "/butil/console" },
        new BitNavItem { Text = "Storage", Url = "/butil/storage" },
        new BitNavItem { Text = "Cookie", Url = "/butil/cookie" },
        new BitNavItem { Text = "History", Url = "/butil/history" },
        new BitNavItem { Text = "Element", Url = "/butil/element" },
        new BitNavItem { Text = "Window", Url = "/butil/window" },
        new BitNavItem { Text = "Document", Url = "/butil/document" },
        new BitNavItem { Text = "Navigator", Url = "/butil/navigator" },
        new BitNavItem { Text = "Location", Url = "/butil/location" },
        new BitNavItem { Text = "Screen", Url = "/butil/screen" },
        new BitNavItem { Text = "VisualViewport", Url = "/butil/visualViewport" },
        new BitNavItem { Text = "ScreenOrientation", Url = "/butil/screenOrientation" },
    ];


    protected override Task OnInitializedAsync()
    {
        SetNavItems();

        navigationManager.LocationChanged += OnLocationChanged;

        return base.OnInitializedAsync();
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        SetNavItems();

        StateHasChanged();
    }

    private void SetNavItems()
    {
        var currentUrl = navigationManager.Uri.Replace(navigationManager.BaseUri, "/", StringComparison.InvariantCultureIgnoreCase);

        isTemplateDocRoute = currentUrl.Contains("templates") || currentUrl.Contains("admin-panel") || currentUrl.Contains("todo-template");
        isBswupDocRoute = currentUrl.Contains("bswup");
        isBesqlDocRoute = currentUrl.Contains("besql");
        isButilDocRoute = currentUrl.Contains("butil");
        isLcncDocRoute = currentUrl.Contains("lowcode-nocode");
        isDocsRoute = isTemplateDocRoute || isBswupDocRoute || isBesqlDocRoute || isButilDocRoute /*|| isLcncDocRoute*/;

        navItems = isTemplateDocRoute ? templatesNavItems
                 : isBswupDocRoute ? bswupNavItems
                 : isBesqlDocRoute ? besqlNavItems
                 : isButilDocRoute ? butilNavItems
                 //: isLcncDocRoute ? lcncNavItems
                 : [];
    }

    public void Dispose()
    {
        navigationManager.LocationChanged -= OnLocationChanged;
    }
}
