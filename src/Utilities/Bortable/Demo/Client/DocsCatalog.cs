using Bit.Bortable.Demo.Client.Pages;
using Microsoft.AspNetCore.Components;

namespace Bit.Bortable.Demo.Client;

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
    public const string Name = "Bortable";

    public const string PackageId = "Bit.Bortable";

    /// <summary>The library's folder in the repository, which the GitHub links point into.</summary>
    public const string ProjectPath = "src/Utilities/Bortable";

    /// <summary>What the library is, in the two or three sentences the footer has room for.</summary>
    public const string Tagline =
        "Bortable is a Blazor-native drag-and-drop sortable list inspired by SortableJS: reorder items, drag " +
        "them between lists, clone, swap and multi-drag, with the sorting engine written in C#. This site is " +
        "itself a Blazor app built on it.";

    public static readonly string[] Badges = ["MIT", "net8.0", "net9.0", "net10.0"];

    /// <summary>Words the search palette suggests when a query finds nothing.</summary>
    public static readonly string[] SearchHints = ["handle", "clone", "swap", "nested", "events"];

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
                "Install the package, register the services, link the stylesheet and put your first list on a page.",
                "install setup nuget package AddBitBortableServices quick start program.cs stylesheet render mode",
                typeof(GettingStarted)),
        ]),
        new("Sorting",
        [
            new("/simple-list", "Simple list",
                "Drag the items to reorder them; the bound list updates live with @bind-Items.",
                "reorder sort bind-Items items itemtemplate basic list",
                typeof(SimpleList)),
            new("/shared", "Shared lists",
                "Lists with the same Group exchange items, with a ghost preview of where the item lands.",
                "group shared lists move between ItemClassSelector tint ghost",
                typeof(Shared)),
            new("/cloning", "Cloning",
                "Pull=\"BortablePull.Clone\" copies an item into another list and keeps the original in place.",
                "clone copy pull BortablePull source target",
                typeof(Cloning)),
            new("/disable-sort", "Disabling sorting",
                "Sort=\"false\" and Put=\"false\": a list whose items can only be dragged out of it.",
                "sort put disable read-only palette pick",
                typeof(DisableSort)),
            new("/handle", "Handle",
                "Handle=\".handle\" limits dragging to a grip, so the rest of the row stays clickable.",
                "handle grip drag handle selector",
                typeof(HandleDemo)),
            new("/filter", "Filter",
                "A Filter predicate locks items so they cannot be picked up.",
                "filter locked non-draggable predicate disabled item",
                typeof(FilterDemo)),
            new("/thresholds", "Swap thresholds",
                "SwapThreshold, InvertSwap and InvertedSwapThreshold, with the swap zones drawn as you tune them.",
                "threshold swapthreshold invertswap invertedswapthreshold direction zones dead zone",
                typeof(Thresholds)),
        ]),
        new("Layouts & events",
        [
            new("/grid", "Grid",
                "Point the list at a CSS grid container and it sorts a grid.",
                "grid layout css grid tiles",
                typeof(Grid)),
            new("/nested", "Nested sortables",
                "Every item holds a list of its own, and items move between branches and depths.",
                "nested tree recursive branches depth",
                typeof(Nested)),
            new("/events", "Events",
                "The SortableJS lifecycle callbacks - OnStart, OnEnd, OnAdd, OnRemove, OnUpdate - logged live.",
                "events OnStart OnEnd OnAdd OnRemove OnUpdate OnSort OnChange callbacks log",
                typeof(Events)),
        ]),
        new("Plugins",
        [
            new("/multi-drag", "Multi-drag",
                "MultiDrag=\"true\": select several items and drag them together as one block.",
                "multi drag multidrag select selection plugin",
                typeof(MultiDragDemo)),
            new("/swap", "Swap",
                "Swap=\"true\": drop an item onto another to trade their places.",
                "swap plugin trade places highlight",
                typeof(SwapDemo)),
        ]),
        new("Tooling",
        [
            new("/mcp-server", "MCP server",
                "The Model Context Protocol server this site hosts, so an AI agent reads the library instead of guessing.",
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
