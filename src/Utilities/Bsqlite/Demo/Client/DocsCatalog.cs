using Bit.Bsqlite.Demo.Client.Pages;
using Microsoft.AspNetCore.Components;

namespace Bit.Bsqlite.Demo.Client;

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
    public const string Name = "Bsqlite";

    public const string PackageId = "Bit.Bsqlite";

    /// <summary>The library's folder in the repository, which the GitHub links point into.</summary>
    public const string ProjectPath = "src/Utilities/Bsqlite";

    /// <summary>What the library is, in the two or three sentences the footer has room for.</summary>
    public const string Tagline =
        "Bsqlite is SQLite in the browser for Blazor, with EF Core on top: the same entities and migrations as " +
        "on the server, in a database that lives in the user's tab - on OPFS, IndexedDB, Cache Storage or memory. " +
        "This site is itself a Blazor WebAssembly app built on it.";

    public static readonly string[] Badges = ["MIT", "net8.0", "net9.0", "net10.0"];

    /// <summary>Words the search palette suggests when a query finds nothing.</summary>
    public static readonly string[] SearchHints = ["live query", "opfs", "migrate", "decimal", "export"];

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
                "Install the package, register a storage preference and a database, and open an EF Core context in the browser.",
                "install setup nuget package AddBitBsqliteServices AddBitBsqliteDbContext UseBitBsqlite program.cs prerendering render mode quick start",
                typeof(GettingStarted)),
        ]),
        new("Model and data",
        [
            new("/catalog", "Catalog",
                "CRUD over a related graph: a unique SKU, a decimal price, a TimeSpan lead time and a nullable DateOnly.",
                "crud products categories insert update delete SaveChangesAsync foreign key cascade decimal timespan dateonly",
                typeof(Catalog)),
            new("/customers", "Customers",
                "Guid keys, a unique index the engine enforces, a VIP flag and decimal credit limits.",
                "guid unique index constraint DbUpdateException bool dateonly decimal precision types",
                typeof(Customers)),
            new("/orders", "Orders",
                "Master-detail with Include and ThenInclude, a string-backed enum, DateTimeOffset and restrict foreign keys.",
                "include theninclude master detail graph enum DateTimeOffset restrict relationships order lines",
                typeof(Orders)),
        ]),
        new("Engine and platform",
        [
            new("/queries", "SQL and live queries",
                "The generated SQL, ef_compare on decimals, REGEXP, live queries and plain ADO.NET on the same connection.",
                "live query AsLiveQuery ToQueryString ef_compare EF_DECIMAL regexp ado.net command reader transaction sqlite_version",
                typeof(Queries)),
            // Qualified: in this namespace a bare "Storage" is the library's Bit.Bsqlite.Storage namespace.
            new("/storage", "Storage backends",
                "How a backend is chosen and remembered, what each one can do, quota probes and the persistent-storage permission.",
                "opfs indexeddb cache storage in-memory selection prefer fallback resolution capabilities probe quota persist binding",
                typeof(Pages.Storage)),
            new("/admin", "Database files",
                "Databases as files: list what each backend holds, export a real .db, import one back, delete.",
                "admin export import download delete list file IBsqliteStorageAdmin seed .db",
                typeof(Admin)),
            new("/limits", "Limits",
                "What the web cannot do - synchronous APIs, WAL, ATTACH, page size - and exactly how each one fails.",
                "limits synchronous SaveChanges BsqliteSynchronousApiNotSupportedException wal journal_mode attach page_size guards",
                typeof(Limits)),
        ]),
        new("Tooling",
        [
            new("/mcp-server", "MCP server",
                "The Model Context Protocol server this site hosts, with its storage planner, SQL check and search tried live.",
                "mcp model context protocol ai agent tools resources prompts claude copilot server planner sql check",
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
