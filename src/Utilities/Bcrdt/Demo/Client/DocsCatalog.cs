using Bit.Bcrdt.Demo.Client.Pages;
using Microsoft.AspNetCore.Components;

namespace Bit.Bcrdt.Demo.Client;

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
    public const string Name = "Bcrdt";

    public const string PackageId = "Bit.Bcrdt";

    /// <summary>The library's folder in the repository, which the GitHub links point into.</summary>
    public const string ProjectPath = "src/Utilities/Bcrdt";

    /// <summary>What the library is, in the two or three sentences the footer has room for.</summary>
    public const string Tagline =
        "Bcrdt is real-time collaborative editing for .NET and Blazor, written natively in C#: a CRDT engine for " +
        "text, lists, maps and sets, with synchronization, presence, offline-first persistence and selective undo. " +
        "Every panel on this site is a separate replica, merging for real.";

    public static readonly string[] Badges = ["MIT", "net8.0", "net9.0", "net10.0"];

    /// <summary>Words the search palette suggests when a query finds nothing.</summary>
    public static readonly string[] SearchHints = ["offline", "undo", "conflict", "presence", "SignalR"];

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
                "Install the packages, register the services, map the relay and put a collaborative editor on a page.",
                "install setup nuget package AddBitBcrdtServices AddBitBcrdtHub MapHub BcrdtHub program.cs quick start five-minute data type",
                typeof(GettingStarted)),
        ]),
        new("Collaborating",
        [
            new("/text", "Collaborative text",
                "Three replicas editing one paragraph: take one offline, edit all three, and watch them converge without losing a word.",
                "text BcrdtText BcrdtTextArea editor textarea offline merge converge replicas presence caret",
                typeof(CollaborativeText)),
            new("/list", "Collaborative list",
                "A shared to-do list - a BcrdtArray of BcrdtMaps - where a rename and a concurrent tick of the same item both survive.",
                "list array BcrdtArray BcrdtMap nested todo move reorder delete items",
                typeof(CollaborativeList)),
            new("/form", "Collaborative form",
                "Map fields next to a nested text field, so last-writer-wins and a character-by-character merge show side by side.",
                "form fields map BcrdtMap last writer wins EnsureText nested text scalar",
                typeof(CollaborativeForm)),
        ]),
        new("Under the hood",
        [
            new("/conflicts", "Conflict lab",
                "Six conflicting edits made on purpose - same position, same element, same key - each with its outcome stated before it runs.",
                "conflict lab scenario concurrent insert delete same key converge verdict merge",
                typeof(ConflictLab)),
            new("/undo", "Collaborative undo",
                "Selective undo: each replica undoes its own edits and leaves everyone else's work alone.",
                "undo redo BcrdtUndoManager selective stack TrackedOrigins StopCapturing",
                typeof(UndoRedo)),
            new("/internals", "Internals",
                "The engine laid open: state vectors, block counts, encoded sizes and every update as it is produced.",
                "internals state vector blocks run-length encoding update size bytes delta log origin",
                typeof(Internals)),
        ]),
        new("Across tabs",
        [
            new("/live", "Live, across tabs",
                "One replica per browser tab, meeting through the demo server's SignalR hub or WebSocket endpoint.",
                "live tabs SignalR hub WebSocket relay BcrdtCollaborationScope BcrdtPresenceView server share link room",
                typeof(Live)),
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
    /// Finds the catalog entry for a base-relative path, or null when the page is not in the catalog. A page
    /// whose route takes a parameter (/live/{Room}) is listed by its bare address, so a path below an entry's
    /// address belongs to that entry when no entry names it exactly.
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
