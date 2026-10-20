using Bit.Bonboarding.Demo.Client.Pages;
using Microsoft.AspNetCore.Components;

namespace Bit.Bonboarding.Demo.Client;

/// <summary>
/// One documentation page: <paramref name="Url"/> is its address, <paramref name="Keywords"/> feeds
/// the header's search palette, and <paramref name="PageType"/> is the component the address renders -
/// which is what the "Edit this page on GitHub" link under every page is built from.
/// </summary>
public record DocsPageInfo(string Url, string Title, string Description, string Keywords, Type PageType);

public record DocsSection(string Title, DocsPageInfo[] Pages);

/// <summary>
/// A link in the header. <paramref name="MatchesDocs"/> marks the entry that stands for the docs as a
/// whole: it is the current section on every page no other entry claims.
/// </summary>
public record SiteNavItem(string Href, string Label, bool MatchesDocs = false);

/// <summary>
/// What the shared site chrome in Layout/ - the header, the docs shell, the palette and the footer -
/// says about this particular demo. Those components are the same file in every Utilities demo; this
/// class and <see cref="DocsCatalog"/> are what each demo fills in for itself.
/// </summary>
public static class SiteInfo
{
    /// <summary>The library's name, as the header's wordmark and the breadcrumb's root show it.</summary>
    public const string Name = "Bonboarding";

    public const string PackageId = "Bit.Bonboarding";

    /// <summary>The library's folder in the repository, which the GitHub links point into.</summary>
    public const string ProjectPath = "src/Utilities/Bonboarding";

    /// <summary>What the library is, in the two or three sentences the footer has room for.</summary>
    public const string Tagline =
        "Bonboarding is a Blazor-native onboarding and product tour library: spotlights, contextual hints, " +
        "branching and multi-page tours, with every decision made in C#. Every tour on this site is the " +
        "library's own, running on it.";

    public static readonly string[] Badges = ["MIT", "net8.0", "net9.0", "net10.0"];

    /// <summary>Words the search palette suggests when a query finds nothing.</summary>
    public static readonly string[] SearchHints = ["spotlight", "branching", "route", "theme", "events"];

    /// <summary>
    /// Whether Ctrl+K / Cmd+K opens the site's search palette. "/" always does. A demo whose own
    /// library answers to Ctrl+K turns this off so the two do not fight over the key.
    /// </summary>
    public static readonly bool PaletteCtrlK = true;

    public static readonly SiteNavItem[] Primary =
    [
        new("/", "Home"),
        new("/getting-started", "Docs", MatchesDocs: true),
        new("/mcp-server", "MCP server"),
    ];

    public const string RepositoryUrl = "https://github.com/bitfoundation/bitplatform";

    public static string ProjectUrl => $"{RepositoryUrl}/tree/develop/{ProjectPath}";

    public static string NuGetUrl => $"https://www.nuget.org/packages/{PackageId}";

    /// <summary>
    /// The page's source file on GitHub. Razor gives a component the namespace of its folder, so the
    /// path below Demo/Client is the type's namespace past this assembly's root, one folder a segment.
    /// </summary>
    public static string SourceUrlOf(Type page)
    {
        var root = typeof(SiteInfo).Namespace!;
        var relative = page.FullName!.StartsWith(root + ".", StringComparison.Ordinal)
            ? page.FullName[(root.Length + 1)..]
            : page.Name;

        return $"{RepositoryUrl}/blob/develop/{ProjectPath}/Demo/Client/{relative.Replace('.', '/')}.razor";
    }
}

/// <summary>
/// The single source of truth for the site's information architecture. The docs nav panel, the
/// header's search palette, the previous/next pager, the breadcrumb and the footer's link columns are
/// all driven from here, so adding a page means adding exactly one entry.
/// </summary>
public static class DocsCatalog
{
    public static readonly DocsSection[] Sections =
    [
        new("Introduction",
        [
            new("/getting-started", "Getting started",
                "Install the package, register the services, link the stylesheet, host one BonboardingRoot and write a first tour.",
                "install setup nuget package AddBitBonboardingServices quick start program.cs stylesheet BonboardingRoot render mode prerendering",
                typeof(GettingStarted)),
        ]),
        new("Guides",
        [
            new("/placement", "Placement & spotlight",
                "Collision-aware positioning computed in C#: shift before flip, spotlight shapes and padding, and what the overlay does with a click.",
                "placement position flip shift collision viewport spotlight shape padding offset interaction blocked targetonly free scroll",
                typeof(PlacementLab)),
            new("/dynamic", "Dynamic targets",
                "Elements that render late, re-render or never appear, steps that wait for the user to act, and targets picked at run time.",
                "late element missing target wait skip center fail mutation observer advanceon targetclick dynamic BonboardingStepTarget",
                typeof(Dynamic)),
            new("/flows", "Conditional & branching",
                "Steps gated on a condition, tours that fork on an answer, and validation before the tour moves on.",
                "condition when branch fork goto actions ResolveNext CanAdvance validation role feature flag back history",
                typeof(Flows)),
            new("/workspace", "Multi-page tour",
                "A tour that spans routes: it navigates for you, waits for the page to render, and holds its place if you wander off.",
                "route routes navigation multi-page cross page resume wander workspace",
                typeof(Workspace)),
            new("/workspace/settings", "Workspace settings",
                "The second page of the multi-page tour, where its settings steps run - and a place to start the same tour from the far end.",
                "settings second page route navigate backwards api keys profile",
                typeof(WorkspaceSettings)),
        ]),
        new("Customise",
        [
            new("/theming", "Theming & RTL",
                "The --bit-bonboarding-* custom properties, light and dark, progress styles, localised labels and right-to-left tours.",
                "theme theming dark light css variables custom properties skin PopoverClass progress dots bar labels localization rtl right-to-left direction",
                typeof(Theming)),
            new("/headless", "Headless UI",
                "Replace the card entirely and keep collision-aware placement, focus management and dialog semantics.",
                "headless custom template ChildContent Unstyled render your own popover card Header Body Footer",
                typeof(Headless)),
            new("/events", "Events & persistence",
                "The event stream to forward to analytics, and the store that remembers how far each user got.",
                "events EventRaised observer analytics IBonboardingObserver persistence store IBonboardingStore localStorage record reset",
                typeof(Events)),
        ]),
        new("Tooling",
        [
            new("/mcp-server", "MCP server",
                "The Model Context Protocol server this site hosts, so an AI agent reads the library instead of guessing at it.",
                "mcp model context protocol ai agent tools resources prompts claude copilot server",
                typeof(McpServer)),
        ]),
    ];

    public static readonly DocsPageInfo[] AllPages = [.. Sections.SelectMany(s => s.Pages)];

    /// <summary>The current address relative to the base, without its query string or fragment.</summary>
    public static string PathOf(NavigationManager nav)
    {
        var path = nav.ToBaseRelativePath(nav.Uri);
        var cut = path.IndexOfAny(['?', '#']);
        return (cut >= 0 ? path[..cut] : path).Trim('/');
    }

    /// <summary>Whether a link's address and a base-relative path name the same page, ignoring case as the router does.</summary>
    public static bool IsSamePath(string href, string path)
        => string.Equals(href.Trim('/'), path.Trim('/'), StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Finds the catalog entry for a base-relative path, or null when the page is not in the catalog. A
    /// page whose route takes a parameter is listed by its bare address, so a deeper path falls back to
    /// the entry it starts with.
    /// </summary>
    public static DocsPageInfo? FindByPath(string path)
        => AllPages.FirstOrDefault(p => IsSamePath(p.Url, path))
           ?? AllPages.FirstOrDefault(p => path.Trim('/').StartsWith(p.Url.Trim('/') + "/", StringComparison.OrdinalIgnoreCase));

    /// <summary>Previous/next pages in reading order for the bottom-of-page pager.</summary>
    public static (DocsPageInfo? Previous, DocsPageInfo? Next) NeighborsOf(DocsPageInfo page)
    {
        var index = Array.IndexOf(AllPages, page);
        if (index < 0) return (null, null);
        return (index > 0 ? AllPages[index - 1] : null,
                index < AllPages.Length - 1 ? AllPages[index + 1] : null);
    }

    /// <summary>Section a page belongs to (the breadcrumb's middle step and the palette's group label).</summary>
    public static string SectionOf(DocsPageInfo page)
        => Sections.First(s => s.Pages.Contains(page)).Title;
}
