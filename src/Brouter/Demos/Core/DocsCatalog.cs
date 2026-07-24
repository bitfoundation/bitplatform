namespace Bit.Brouter.Demos.Core;

/// <summary>
/// One documentation page: <paramref name="Slug"/> is the path under /docs (empty for the
/// /docs index itself), <paramref name="Keywords"/> feeds the header search box.
/// </summary>
public record DocsPageInfo(string Slug, string Title, string Description, string Keywords)
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
                "overview introduction about features map"),
            new("getting-started", "Getting started",
                "Install the packages, register the services, and declare your first routes.",
                "install setup nuget package AddBitBrouterServices quick start program.cs"),
        ]),
        new("Routing",
        [
            new("route-templates", "Route templates",
                "The full template grammar: literals, typed parameters, optionals, defaults, complex segments, catch-alls and specificity.",
                "template syntax segment literal optional default catch-all wildcard complex specificity precedence"),
            new("constraints", "Constraints",
                "18 built-in type and validation constraints, chaining rules, and custom constraints - tested interactively.",
                "constraint int guid datetime alpha regex min max range length custom register"),
            new("route-parameters", "Route parameters",
                "How URL values reach components: [Parameter] binding, the cascaded parameter bag, and query-string binding.",
                "parameter binding SupplyParameterFromQuery BrouterParameter BrouterQuery query string typed cascade"),
            new("nested-routes", "Nested routes & outlets",
                "Route trees where parents render persistent layout and children fill outlets - named views and pathless groups included.",
                "nested outlet BrouterOutlet BrouterView named views pathless group index route layout"),
            new("page-discovery", "@page discovery",
                "Scan assemblies for @page / [Route] components so routes stay colocated with their pages.",
                "attribute route discovery AppAssembly AdditionalAssemblies @page [Route] razor class library lazy assembly"),
        ]),
        new("Navigation",
        [
            new("navigation", "Navigation & history",
                "Programmatic navigation with awaited outcomes, history entry state, query updates, named routes and BrouterLink.",
                "navigate NavigateAsync outcome back forward history state NavigateWithQuery named routes relative BrouterLink"),
            new("guards", "Guards & navigation locks",
                "Enter guards, preventive leave guards, component-level locks with custom dialogs, redirects and global hooks.",
                "guard leave guard lock OnDeactivating OnRenavigating cancel redirect unsaved changes hooks OnNavigating"),
            new("scroll-and-focus", "Scroll & focus",
                "Scroll-to-top, fragment scrolling, scroll restoration on Back/Forward, and accessible focus management.",
                "scroll restore fragment anchor hash focus accessibility FocusOnNavigateSelector a11y"),
        ]),
        new("Data",
        [
            new("data-loading", "Data loading",
                "Route loaders with stale-while-revalidate caching, revalidation, preloading, deferred data and error boundaries.",
                "loader cache StaleTime revalidate preload Intent Viewport deferred BrouterAwait ErrorContent retry"),
        ]),
        new("User experience",
        [
            new("view-transitions", "View transitions",
                "Direction-aware page animations and shared-element morphs via the browser View Transitions API.",
                "view transition animation morph shared element startViewTransition reduced motion"),
            new("lifecycle", "Lifecycle & keep-alive",
                "Activation, renavigation and deactivation callbacks, plus keep-alive retention with per-parameter instances.",
                "lifecycle OnActivated OnDeactivated OnRenavigated keep-alive KeepAlive KeepAliveMax retention LRU"),
        ]),
        new("Tooling & adoption",
        [
            new("typed-routes", "Typed routes (generator)",
                "The Bit.Brouter.Generators source generator: compile-time-safe URL builders from your route declarations.",
                "generator source generator BrouterRoutes typed url builder compile-time Names"),
            new("migration", "Migrating from the built-in Router",
                "Drop-in migration: the Found template, zero-template authorization, layouts, and the parameter mapping table.",
                "migration built-in router Found RouteView AuthorizeRouteView authorization layout NotFound Navigating"),
            new("performance", "Performance & scalability",
                "What routes cost, how matching scales, prerender state bridging, and guidance for very large apps.",
                "performance benchmark memory startup scalability prerender PersistLoaderState SSR"),
        ]),
    ];

    public static readonly DocsPageInfo[] AllPages = Sections.SelectMany(s => s.Pages).ToArray();

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
