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
    public static readonly string[] SearchHints = ["handle", "clone", "swap", "keyboard", "events"];

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
                "Drag the items to reorder them; the bound list updates live with @bind-Items, and Animation sets the slide.",
                "reorder sort bind-Items items itemtemplate basic list animation flip key",
                typeof(SimpleList)),
            new("/shared", "Shared lists",
                "Lists with the same Group exchange items, with a ghost preview of where the item lands; an empty list shows ChildContent.",
                "group shared lists move between ItemClassSelector tint ghost empty state childcontent placeholder",
                typeof(Shared)),
            new("/cloning", "Cloning",
                "Pull=\"BortablePull.Clone\" copies an item into another list and keeps the original; a read-only array makes a palette.",
                "clone copy pull BortablePull source target palette read-only array form builder iscloned",
                typeof(Cloning)),
            new("/disable-sort", "Disabling sorting",
                "Sort, Put, Pull=\"None\" and Disabled: what a list lets in, lets out and lets move.",
                "sort put pull none disabled disable read-only palette pick lock",
                typeof(DisableSort)),
            new("/handle", "Handle",
                "Handle=\".handle\" limits dragging to a grip, so the rest of the row - its text, inputs and buttons - stays usable.",
                "handle grip drag handle selector input controls",
                typeof(HandleDemo)),
            new("/filter", "Filter",
                "A Filter predicate locks items so they cannot be picked up - from your data, so a lock can change.",
                "filter locked non-draggable predicate disabled item lock",
                typeof(FilterDemo)),
            new("/thresholds", "Swap thresholds",
                "SwapThreshold, InvertSwap and InvertedSwapThreshold, with the swap zones drawn as you tune them.",
                "threshold swapthreshold invertswap invertedswapthreshold direction zones dead zone",
                typeof(Thresholds)),
        ]),
        new("Input & rules",
        [
            new("/accessibility", "Keyboard and screen readers",
                "Pick up, move and drop from the keyboard, every step announced - in words you can translate; buttons that move items without dragging.",
                "keyboard accessibility a11y screen reader aria announcements live region focus wcag moveasync movetoasync buttons messages localization translate german",
                typeof(Accessibility)),
            new("/rules", "Rules",
                "A board with a workflow: MaxItems, CanDrop, ConfirmDrop with a server that may refuse, RevertOnSpill.",
                "rules validation candrop maxitems capacity confirmdrop revertonspill kanban workflow refuse rollback canceled",
                typeof(Rules)),
            new("/touch", "Touch and pen",
                "A finger or a pen drags with pointer events and a preview; a long press picks an item up, a swipe scrolls.",
                "touch pen mobile pointer long press touchdelay delay delaytolerance forcefallback fallback dragclass preview",
                typeof(TouchDemo)),
            new("/large-list", "Large list",
                "Three hundred rows in a scrolling panel: auto-scroll, and IsolateItems for long lists.",
                "large long list performance scroll autoscroll scrollsensitivity scrollspeed isolateitems virtualization panel diagnostics",
                typeof(LargeList)),
        ]),
        new("Layouts & events",
        [
            new("/grid", "Grid and layouts",
                "A CSS grid, a horizontal row and a right-to-left one: the list is measured as it is laid out.",
                "grid layout css grid tiles horizontal row chips rtl right-to-left direction additionalattributes dir",
                typeof(Grid)),
            new("/nested", "Nested sortables",
                "Every item holds a list of its own, and items move between branches and depths.",
                "nested tree recursive branches depth",
                typeof(Nested)),
            new("/styling", "Styling",
                "The stylesheet's custom properties and drag-state classes, and classes of your own for the ghost, the chosen item and the preview.",
                "styling style theme css custom properties accent ghostclass chosenclass dragclass swapclass selectedclass classes outline dark",
                typeof(Styling)),
            new("/events", "Events",
                "The SortableJS lifecycle callbacks - OnStart, OnChange, OnUpdate, OnRemove, OnAdd, OnSort, OnEnd - logged live.",
                "events OnStart OnEnd OnAdd OnRemove OnUpdate OnSort OnChange callbacks log input isclone canceled arguments",
                typeof(Events)),
        ]),
        new("Plugins",
        [
            new("/multi-drag", "Multi-drag",
                "MultiDrag=\"true\": select several items - by click, Shift+click or Shift+Space - and drag them together as one block.",
                "multi drag multidrag select selection plugin selecteditems bind-SelectedItems shift range",
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
