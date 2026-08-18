using Bit.Brouter.Demo.Client.Pages;

namespace Bit.Brouter.Demo.Client;

/// <summary>
/// One documentation page: <paramref name="Slug"/> is the path under /docs (empty for the
/// /docs index itself), <paramref name="Keywords"/> feeds the header search box, and
/// <paramref name="PageType"/> is the component AppRouter renders for the slug - which is what
/// lets the MCP server render a page's documentation on demand.
/// </summary>
public record DocsPageInfo(string Slug, string Title, string Description, string Keywords, Type PageType)
{
    public string Url => Slug.Length == 0 ? "/docs" : $"/docs/{Slug}";
}

public record DocsSection(string Title, DocsPageInfo[] Pages);

/// <summary>
/// The single source of truth for the documentation's information architecture. The docs
/// sidebar, the header search box, the previous/next pager and the landing page's feature
/// links are all driven from here, so adding a page means adding exactly one entry.
/// </summary>
public static class DocsCatalog
{
    public static readonly DocsSection[] Sections =
    [
        new("Introduction",
        [
            new("", "Overview",
                "What Brouter is, the problems it solves, and a map of every feature area.",
                "overview introduction about features map",
                typeof(DocsOverviewPage)),
            new("getting-started", "Getting started",
                "Install the packages, register the services, and declare your first routes.",
                "install setup nuget package AddBitBrouterServices quick start program.cs",
                typeof(GettingStartedPage)),
            new("navigation-pipeline", "How a navigation works",
                "The ordered pipeline behind every navigation: what runs before the URL moves, what runs after, and what each step can do about it.",
                "pipeline order phases decide commit preventive supersession cancel redirect fail closed revalidation preload concepts",
                typeof(PipelinePage)),
        ]),
        new("Routing",
        [
            new("route-templates", "Route templates",
                "The full template grammar: literals, typed parameters, optionals, defaults, complex segments, catch-alls and specificity.",
                "template syntax segment literal optional default catch-all wildcard complex specificity precedence",
                typeof(TemplatesPage)),
            new("constraints", "Constraints",
                "18 built-in type and validation constraints, chaining rules, and custom constraints - tested interactively.",
                "constraint int guid datetime alpha regex min max range length custom register",
                typeof(ConstraintsPage)),
            new("route-parameters", "Route parameters",
                "How URL values reach components: [Parameter] binding, the cascaded parameter bag, and query-string binding.",
                "parameter binding SupplyParameterFromQuery BrouterParameter BrouterQuery query string typed cascade",
                typeof(ParametersPage)),
            new("nested-routes", "Nested routes & outlets",
                "Route trees where parents render persistent layout and children fill outlets - named views and pathless groups included.",
                "nested outlet BrouterOutlet BrouterView named views pathless group index route layout",
                typeof(OutletsPage)),
            new("page-discovery", "@page discovery",
                "Scan assemblies for @page / [Route] components so routes stay colocated with their pages.",
                "attribute route discovery AppAssembly AdditionalAssemblies @page [Route] razor class library lazy assembly",
                typeof(PageDiscoveryPage)),
        ]),
        new("Navigation",
        [
            new("navigation", "Navigation & history",
                "Programmatic navigation with awaited outcomes, history entry state, query updates, named routes and BrouterLink.",
                "navigate NavigateAsync outcome back forward history state NavigateWithQuery named routes relative BrouterLink",
                typeof(NavigationPage)),
            new("guards", "Guards & navigation locks",
                "Enter guards, preventive leave guards, component-level locks with custom dialogs, redirects and global hooks.",
                "guard leave guard lock OnDeactivating OnRenavigating cancel redirect unsaved changes hooks OnNavigating",
                typeof(GuardsPage)),
            new("scroll-and-focus", "Scroll & focus",
                "Scroll-to-top, fragment scrolling, scroll restoration on Back/Forward, and accessible focus management.",
                "scroll restore fragment anchor hash focus accessibility FocusOnNavigateSelector a11y",
                typeof(ScrollFocusPage)),
        ]),
        new("Data",
        [
            new("data-loading", "Data loading",
                "Route loaders with stale-while-revalidate caching, revalidation, preloading, deferred data and error boundaries.",
                "loader cache StaleTime revalidate preload Intent Viewport deferred BrouterAwait ErrorContent retry",
                typeof(LoadersPage)),
        ]),
        new("User experience",
        [
            new("view-transitions", "View transitions",
                "Direction-aware page animations and shared-element morphs via the browser View Transitions API.",
                "view transition animation morph shared element startViewTransition reduced motion",
                typeof(TransitionsPage)),
            new("lifecycle", "Lifecycle & keep-alive",
                "Activation, renavigation and deactivation callbacks, plus keep-alive retention with per-parameter instances.",
                "lifecycle OnActivated OnDeactivated OnRenavigated keep-alive KeepAlive KeepAliveMax retention LRU",
                typeof(LifecycleOverviewPage)),
        ]),
        new("Tooling & adoption",
        [
            new("typed-routes", "Typed routes (generator)",
                "The Bit.Brouter.Generators source generator: compile-time-safe URL builders from your route declarations.",
                "generator source generator BrouterRoutes typed url builder compile-time Names",
                typeof(GeneratorPage)),
            new("mcp", "MCP server",
                "The Model Context Protocol server this site hosts: the page calls its tools, resources and prompts live and shows every JSON-RPC message they exchange.",
                "mcp model context protocol ai agent tools resources prompts json-rpc streamable http claude copilot server",
                typeof(McpPage)),
            new("migration", "Migrating from the built-in Router",
                "Drop-in migration: the Found template, zero-template authorization, layouts, and the parameter mapping table.",
                "migration built-in router Found RouteView AuthorizeRouteView authorization layout NotFound Navigating",
                typeof(MigrationPage)),
            new("performance", "Performance & scalability",
                "What routes cost, how matching scales, prerender state bridging, and guidance for very large apps.",
                "performance benchmark memory startup scalability prerender PersistLoaderState SSR",
                typeof(PerformancePage)),
        ]),
        new("Reference",
        [
            new("api", "API reference",
                "Every component parameter, service member, option and value type, with defaults.",
                "api reference parameters options BrouterOptions IBrouter Broute BrouterLink enums types defaults lookup",
                typeof(ApiPage)),
            new("recipes", "Recipes",
                "Task-oriented solutions: protected areas, unsaved changes, revalidation, query state, breadcrumbs, keep-alive lists.",
                "recipes cookbook patterns how-to examples auth login unsaved changes breadcrumbs paging filters localization",
                typeof(RecipesPage)),
            new("faq", "FAQ & troubleshooting",
                "Adoption questions, and the symptoms you might hit afterwards - each with the reason behind it.",
                "faq troubleshooting problem error not matching 404 ambiguous null parameter cache stale animation help",
                typeof(FaqPage)),
        ]),
    ];

    public static readonly DocsPageInfo[] AllPages = Sections.SelectMany(s => s.Pages).ToArray();

    /// <summary>Finds the catalog entry for a slug ("" for the overview), or null when there is none.</summary>
    public static DocsPageInfo? FindBySlug(string? slug)
    {
        var trimmed = (slug ?? string.Empty).Trim('/');

        return AllPages.FirstOrDefault(p => string.Equals(p.Slug, trimmed, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Finds the catalog entry matching a /docs path, or null for non-docs URLs.</summary>
    public static DocsPageInfo? FindByPath(string path)
    {
        var trimmed = path.TrimEnd('/');
        return AllPages.FirstOrDefault(p => string.Equals(p.Url, trimmed, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Previous/next pages in reading order for the bottom-of-page pager.</summary>
    public static (DocsPageInfo? Previous, DocsPageInfo? Next) NeighborsOf(DocsPageInfo page)
    {
        var index = Array.IndexOf(AllPages, page);
        if (index < 0) return (null, null);
        return (index > 0 ? AllPages[index - 1] : null,
                index < AllPages.Length - 1 ? AllPages[index + 1] : null);
    }

    /// <summary>Section a page belongs to (shown as the category label in search results).</summary>
    public static string SectionOf(DocsPageInfo page)
        => Sections.First(s => s.Pages.Contains(page)).Title;
}
