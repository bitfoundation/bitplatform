namespace Bit.BlazeDb.Demo.Client.Components;

public sealed record NavItem(string Path, string Title, string Icon, string Group, string Blurb);

public static class SiteMap
{
    public static readonly NavItem[] Items =
    [
        new("", "Overview", "overview", "Start",
            "What BlazeDb is, how the engine is layered, and how it performs."),

        new("todos", "Live data", "list", "Start",
            "A working app over the real engine: CRUD, index-backed filters, paging and persistence."),

        new("schema", "Schema & codegen", "schema", "Engine",
            "Attributes, the generated descriptor, supported property types and generator diagnostics."),

        new("indexes", "Indexes", "index", "Engine",
            "Hash equality indexes, ordered indexes, inclusive range bounds and index maintenance."),

        new("queries", "Query primitives", "query", "Engine",
            "Compose an index source, residual filters, ordering and paging without a query language."),

        new("efcore", "EF Core", "layers", "Engine",
            "UseBlazeDb, AddDbContext, LINQ over live rows, and SaveChanges as one transaction."),

        new("transactions", "Transactions", "txn", "Engine",
            "Ambient batches, atomic commit, rollback semantics and the single-writer rule."),

        new("durability", "Durability & recovery", "shield", "Persistence",
            "The write-ahead log, snapshots, checkpointing, crash recovery and torn writes."),

        new("storage", "Storage backends", "disk", "Persistence",
            "The four-method storage contract, OPFS in the browser, and a live trace of engine I/O."),

        new("serialization", "Wire format", "binary", "Persistence",
            "Inspect the exact bytes a row becomes, field by field, and prove schema evolution works."),

        new("errors", "Errors & limits", "alert", "Reference",
            "Every exception the engine raises, triggered live, plus what BlazeDb deliberately does not do."),

        new("bench", "Benchmarks", "gauge", "Reference",
            "Run the engine's read, write and encoding paths in your own browser and see what they cost."),

        new("roadmap", "Roadmap", "map", "Reference",
            "What ships, what never will, and how the repository is laid out."),

        new("mcp-server", "MCP server", "cpu", "Reference",
            "The Model Context Protocol server this site hosts, so an AI agent writes BlazeDb code against the real API."),
    ];

    public static readonly string[] Groups = ["Start", "Engine", "Persistence", "Reference"];

    public static IEnumerable<NavItem> InGroup(string group) => Items.Where(i => i.Group == group);

    public static NavItem? Find(string relativePath)
    {
        var path = relativePath.Split('?', '#')[0].Trim('/');
        return Items.FirstOrDefault(i => string.Equals(i.Path, path, StringComparison.OrdinalIgnoreCase));
    }
}
