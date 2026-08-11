using Microsoft.AspNetCore.Components.Routing;

namespace Bit.Websites.Platform.Client.Shared;

public partial class MainLayout : IDisposable
{
    [AutoInject] private NavigationManager navigationManager = default!;

    private bool isDocsRoute;
    private bool isLcncDocRoute;
    private bool isTemplateDocRoute;
    private bool isBesqlDocRoute;

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
        new BitNavItem { Text = "Overview", Url = "/templates", AdditionalUrls = [ "/templates/overview", "/boilerplate", "/boilerplate/overview" ] },
        new BitNavItem { Text = "Getting started", Url = "/templates/getting-started", AdditionalUrls = [ "/templates/development-prerequisites", "/boilerplate/getting-started", "/boilerplate/development-prerequisites" ] },
        new BitNavItem { Text = "Project structure", Url = "/templates/project-structure", AdditionalUrls = [ "/boilerplate/project-structure" ] },
        new BitNavItem { Text = "Create project", Url = "/templates/create-project", AdditionalUrls = [ "/boilerplate/create-project" ] },
        new BitNavItem { Text = "Run project", Url = "/templates/run-project", AdditionalUrls = [ "/boilerplate/run-project" ] },
        new BitNavItem 
        { 
            Text = "Wiki", 
            Url = "/templates/wiki", 
            AdditionalUrls = [
                "/templates/wiki#ef-core", "/boilerplate/wiki#ef-core",
                "/templates/wiki#dtos-mappers", "/boilerplate/wiki#dtos-mappers",
                "/templates/wiki#api-odata", "/boilerplate/wiki#api-odata",
                "/templates/wiki#background-jobs", "/boilerplate/wiki#background-jobs",
                "/templates/wiki#localization", "/boilerplate/wiki#localization",
                "/templates/wiki#exception-handling", "/boilerplate/wiki#exception-handling",
                "/templates/wiki#identity-auth", "/boilerplate/wiki#identity-auth",
                "/templates/wiki#components-styling", "/boilerplate/wiki#components-styling",
                "/templates/wiki#dependency-injection", "/boilerplate/wiki#dependency-injection",
                "/templates/wiki#configuration", "/boilerplate/wiki#configuration",
                "/templates/wiki#typescript", "/boilerplate/wiki#typescript",
                "/templates/wiki#blazor-modes", "/boilerplate/wiki#blazor-modes",
                "/templates/wiki#force-update", "/boilerplate/wiki#force-update",
                "/templates/wiki#response-caching", "/boilerplate/wiki#response-caching",
                "/templates/wiki#logging", "/boilerplate/wiki#logging",
                "/templates/wiki#cicd", "/boilerplate/wiki#cicd",
                "/templates/wiki#testing", "/boilerplate/wiki#testing",
                "/templates/wiki#prompts", "/boilerplate/wiki#prompts",
                "/templates/wiki#misc-files", "/boilerplate/wiki#misc-files",
                "/templates/wiki#aspire", "/boilerplate/wiki#aspire",
                "/templates/wiki#maui-hybrid", "/boilerplate/wiki#maui-hybrid",
                "/templates/wiki#messaging", "/boilerplate/wiki#messaging",
                "/templates/wiki#diagnostic", "/boilerplate/wiki#diagnostic",
                "/templates/wiki#webauthn", "/boilerplate/wiki#webauthn",
                "/templates/wiki#vector-embeddings", "/boilerplate/wiki#vector-embeddings",
            ],
        },
    ];

    private readonly List<BitNavItem> besqlNavItems =
    [
        new BitNavItem { Text = "Overview", Url = "/besql/overview" },
        new BitNavItem { Text = "Install", Url = "/besql/install" },
        new BitNavItem { Text = "Usage", Url = "/besql/usage" },
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

        isTemplateDocRoute = currentUrl.Contains("/templates") || currentUrl.Contains("/boilerplate");
        isBesqlDocRoute = currentUrl.Contains("/besql");
        isLcncDocRoute = currentUrl.Contains("/lowcode-nocode");
        isDocsRoute = isTemplateDocRoute || isBesqlDocRoute /*|| isLcncDocRoute*/;

        navItems = isTemplateDocRoute ? templatesNavItems
                 : isBesqlDocRoute ? besqlNavItems
                 //: isLcncDocRoute ? lcncNavItems
                 : [];
    }

    public void Dispose()
    {
        navigationManager.LocationChanged -= OnLocationChanged;
    }
}
