namespace Bit.Bsqlite.Demo.Client;

/// <summary>One page of the sample site.</summary>
/// <param name="Href">Relative route, without a leading slash.</param>
/// <param name="Title">Label in the sidebar and on the overview grid.</param>
/// <param name="Icon">Name understood by <c>Components/Icon.razor</c>.</param>
/// <param name="Summary">One line describing what the page proves.</param>
internal sealed record SamplePage(string Href, string Title, string Icon, string Summary);

/// <summary>A titled run of pages in the sidebar.</summary>
internal sealed record SampleSection(string Title, IReadOnlyList<SamplePage> Pages);

/// <summary>
/// The navigation model, declared once. The sidebar, the overview page's demo grid, and the
/// document title all read from here, so adding a page cannot leave one of them behind.
/// </summary>
internal static class SiteMap
{
    public static SamplePage Overview { get; } = new(
        "", "Overview", "home",
        "What the library is, how it is wired up, and a catalog that re-runs itself.");

    public static IReadOnlyList<SampleSection> Sections { get; } =
    [
        new("Start here", [Overview]),
        new("Model and data",
        [
            new("catalog", "Catalog", "boxes",
                "CRUD over a related graph: unique SKU, decimal price, TimeSpan lead time, nullable DateOnly."),
            new("customers", "Customers", "users",
                "Guid keys, a unique index the engine enforces, VIP flag, decimal credit limits."),
            new("orders", "Orders", "receipt",
                "Master-detail with Include and ThenInclude, string-backed enum, DateTimeOffset, restrict FKs."),
        ]),
        new("Engine and platform",
        [
            new("queries", "SQL and live queries", "terminal",
                "Generated SQL, ef_compare on decimals, REGEXP, and plain ADO.NET on the same connection."),
            new("storage", "Storage backends", "drive",
                "How a backend is chosen, what each one can do, quota probes, persistent-storage permission."),
            new("admin", "Database files", "files",
                "Databases as files: list, export to a .db, import one back, delete."),
            new("limits", "Limits", "warning",
                "What the web cannot do - sync APIs, WAL, ATTACH, page size - and exactly how it fails."),
        ]),
        new("For AI agents",
        [
            new("mcp-server", "MCP server", "bolt",
                "The library's API, guides and storage selection, served to coding agents at /mcp - and tried live here."),
        ]),
    ];

    /// <summary>Every page except the overview, in sidebar order.</summary>
    public static IEnumerable<SamplePage> Demos =>
        Sections.SelectMany(s => s.Pages).Where(p => p.Href.Length > 0);
}
