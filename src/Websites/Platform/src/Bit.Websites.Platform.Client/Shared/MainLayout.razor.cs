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
        new BitNavItem { Text = "Overview", Url = "/templates", AdditionalUrls = [ "/templates/overview", "/boilerplate", "/boilerplate/overview" ] },
        //new BitNavItem { Text = "Samples", Url = "/templates/samples", AdditionalUrls = [ "/boilerplate/samples" ] },
        new BitNavItem { Text = "Getting started", Url = "/templates/getting-started", AdditionalUrls = [ "/templates/development-prerequisites", "/boilerplate/getting-started", "/boilerplate/development-prerequisites" ] },
        new BitNavItem { Text = "Project structure", Url = "/templates/project-structure", AdditionalUrls = [ "/boilerplate/project-structure" ] },
        new BitNavItem { Text = "Create project", Url = "/templates/create-project", AdditionalUrls = [ "/boilerplate/create-project" ] },
        new BitNavItem { Text = "Run project", Url = "/templates/run-project", AdditionalUrls = [ "/boilerplate/run-project" ] },
        new BitNavItem 
        { 
            Text = "Wiki", 
            Url = "/templates/wiki", 
            AdditionalUrls = ["/boilerplate/wiki"],
            IsExpanded = true,
            ChildItems =
            [
                new BitNavItem { Text = "Entity Framework Core", Url = "/templates/wiki#ef-core", AdditionalUrls = ["/boilerplate/wiki#ef-core"] },
                new BitNavItem { Text = "DTOs & Mapperly", Url = "/templates/wiki#dtos-mappers", AdditionalUrls = ["/boilerplate/wiki#dtos-mappers"] },
                new BitNavItem { Text = "API & OData", Url = "/templates/wiki#api-odata", AdditionalUrls = ["/boilerplate/wiki#api-odata"] },
                new BitNavItem { Text = "Background Jobs", Url = "/templates/wiki#background-jobs", AdditionalUrls = ["/boilerplate/wiki#background-jobs"] },
                new BitNavItem { Text = "Localization", Url = "/templates/wiki#localization", AdditionalUrls = ["/boilerplate/wiki#localization"] },
                new BitNavItem { Text = "Exception Handling", Url = "/templates/wiki#exception-handling", AdditionalUrls = ["/boilerplate/wiki#exception-handling"] },
                new BitNavItem { Text = "Identity & Auth", Url = "/templates/wiki#identity-auth", AdditionalUrls = ["/boilerplate/wiki#identity-auth"] },
                new BitNavItem { Text = "Components, Styling", Url = "/templates/wiki#components-styling", AdditionalUrls = ["/boilerplate/wiki#components-styling"] },
                new BitNavItem { Text = "Dependency Injection", Url = "/templates/wiki#dependency-injection", AdditionalUrls = ["/boilerplate/wiki#dependency-injection"] },
                new BitNavItem { Text = "Configuration", Url = "/templates/wiki#configuration", AdditionalUrls = ["/boilerplate/wiki#configuration"] },
                new BitNavItem { Text = "TypeScript", Url = "/templates/wiki#typescript", AdditionalUrls = ["/boilerplate/wiki#typescript"] },
                new BitNavItem { Text = "Blazor Modes & PWA", Url = "/templates/wiki#blazor-modes", AdditionalUrls = ["/boilerplate/wiki#blazor-modes"] },
                new BitNavItem { Text = "Force Update", Url = "/templates/wiki#force-update", AdditionalUrls = ["/boilerplate/wiki#force-update"] },
                new BitNavItem { Text = "Response Caching", Url = "/templates/wiki#response-caching", AdditionalUrls = ["/boilerplate/wiki#response-caching"] },
                new BitNavItem { Text = "Logging & OpenTelemetry", Url = "/templates/wiki#logging", AdditionalUrls = ["/boilerplate/wiki#logging"] },
                new BitNavItem { Text = "CI-CD Pipeline", Url = "/templates/wiki#cicd", AdditionalUrls = ["/boilerplate/wiki#cicd"] },
                new BitNavItem { Text = "Automated Testing", Url = "/templates/wiki#testing", AdditionalUrls = ["/boilerplate/wiki#testing"] },
                new BitNavItem { Text = "Prompt Templates", Url = "/templates/wiki#prompts", AdditionalUrls = ["/boilerplate/wiki#prompts"] },
                new BitNavItem { Text = "Misc Files", Url = "/templates/wiki#misc-files", AdditionalUrls = ["/boilerplate/wiki#misc-files"] },
                new BitNavItem { Text = ".NET Aspire", Url = "/templates/wiki#aspire", AdditionalUrls = ["/boilerplate/wiki#aspire"] },
                new BitNavItem { Text = "MAUI - Blazor Hybrid", Url = "/templates/wiki#maui-hybrid", AdditionalUrls = ["/boilerplate/wiki#maui-hybrid"] },
                new BitNavItem { Text = "Messaging", Url = "/templates/wiki#messaging", AdditionalUrls = ["/boilerplate/wiki#messaging"] },
                new BitNavItem { Text = "Diagnostic Modal", Url = "/templates/wiki#diagnostic", AdditionalUrls = ["/boilerplate/wiki#diagnostic"] },
                new BitNavItem { Text = "WebAuthn and Passwordless Auth", Url = "/templates/wiki#webauthn", AdditionalUrls = ["/boilerplate/wiki#webauthn"] },
                new BitNavItem { Text = "RAG - Vector Embeddings", Url = "/templates/wiki#vector-embeddings", AdditionalUrls = ["/boilerplate/wiki#vector-embeddings"] },
            ]
        },
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
        new BitNavItem { Text = "Cryptography", Url = "/butil/crypto" },
        new BitNavItem { Text = "Web Authentication", Url = "/butil/webAuthn" },
        new BitNavItem { Text = "Clipboard", Url = "/butil/clipboard" },
        new BitNavItem { Text = "Keyboard", Url = "/butil/keyboard" },
        new BitNavItem { Text = "Console", Url = "/butil/console" },
        new BitNavItem { Text = "Notification", Url = "/butil/notification" },
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
        new BitNavItem { Text = "UserAgent", Url = "/butil/userAgent" },
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
        isBswupDocRoute = currentUrl.Contains("/bswup");
        isBesqlDocRoute = currentUrl.Contains("/besql");
        isButilDocRoute = currentUrl.Contains("/butil");
        isLcncDocRoute = currentUrl.Contains("/lowcode-nocode");
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
