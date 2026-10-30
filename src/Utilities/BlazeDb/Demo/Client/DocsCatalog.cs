using Bit.BlazeDb.Demo.Client.Pages;
using Microsoft.AspNetCore.Components;

namespace Bit.BlazeDb.Demo.Client;

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
    public const string Name = "BlazeDb";

    public const string PackageId = "Bit.BlazeDb";

    /// <summary>The library's folder in the repository, which the GitHub links point into.</summary>
    public const string ProjectPath = "src/Utilities/BlazeDb";

    /// <summary>What the library is, in the two or three sentences the footer has room for.</summary>
    public const string Tagline =
        "BlazeDb is a memory-first, SQL-free embedded database for .NET and Blazor WebAssembly: live rows in " +
        "managed memory, source-generated schemas, atomic transactions, an EF Core provider and a write-ahead " +
        "log persisted to OPFS. Every page of this site drives the real engine in your browser.";

    public static readonly string[] Badges = ["MIT", "net10.0", "net11.0"];

    /// <summary>Words the search palette suggests when a query finds nothing.</summary>
    public static readonly string[] SearchHints = ["index", "transaction", "opfs", "efcore", "wal"];

    /// <summary>
    /// Whether Ctrl+K / Cmd+K opens the site's search palette. "/" always does. A demo whose own
    /// library answers to Ctrl+K turns this off so the two do not fight over the key.
    /// </summary>
    public static readonly bool PaletteCtrlK = true;

    public static readonly SiteNavItem[] Primary =
    [
        new("/", "Home"),
        new("/getting-started", "Docs", MatchesDocs: true),
        new("/todos", "Live app"),
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
        new("Start",
        [
            new("/getting-started", "Getting started",
                "Install the package, declare a table, open a database and query it - the whole surface for typical use.",
                "install setup nuget package quick start OpenAsync AddTable BlazeDbTable namespaces requirements hosting",
                typeof(GettingStarted)),
            new("/hosting", "Hosting models",
                "Blazor WebAssembly, a Blazor Web App, Blazor Server, MAUI and the desktop, lazy loading the engine on WebAssembly - and the threading model that decides which fits.",
                "hosting models blazor webassembly web app prerender interactiveauto server asp.net core maui hybrid wpf desktop console threading thread lock BlazeDbFileStorage OpenReadOnly lazy load lazyassemblyloader blazorwebassemblylazyload aot",
                typeof(Hosting)),
            new("/todos", "Live data",
                "A working app over the real engine: CRUD, index-backed filters, paging and persisted preferences.",
                "todos crud app insert update upsert delete get tryget contains filter paging facets CountBy settings optimistic concurrency live demo",
                typeof(Todos)),
        ]),
        new("Engine",
        [
            new("/schema", "Schema & codegen",
                "Attributes, the generated descriptor inspected at runtime, supported property types and generator diagnostics.",
                "schema attributes BlazeDbTable BlazeDbKey BlazeDbIndex BlazeDbField BlazeDbIgnore source generator descriptor diagnostics BLZ types",
                typeof(Schema)),
            new("/writes", "Writing rows",
                "Insert, Update, Upsert and Delete, the Try forms, optimistic concurrency, atomic bulk writes and rows changed in place.",
                "write insert tryinsert update tryupdate upsert delete trydelete comparisonValue optimistic concurrency InsertMany UpsertMany DeleteMany DeleteWhere DeleteAll bulk atomic UpdateInPlace DeleteInPlace stale row VerifyIntegrity unique null key",
                typeof(Writes)),
            new("/indexes", "Indexes",
                "Hash and ordered indexes, inclusive and exclusive bounds, facets, compound and unique indexes, prefix scans and keyset paging.",
                "index hash ordered Lookup CountBy Range OrderBy StartsWith prefix keyset paging BlazeDbBound Exclusive unbounded null keys unique compound facets",
                typeof(Indexes)),
            new("/queries", "Query primitives",
                "Compose a key or index source, residual filters, ordering and paging, explain the plan, and stream large results.",
                "query BlazeDbQuery UseKey UseKeys UseIndex UseIndexPrefix Where OrderBy ThenBy Skip Take Execute Count IsIndexOrdered Explain plan ExecuteAsync ToListAsync streaming batch composer",
                typeof(Queries)),
            new("/efcore", "EF Core",
                "UseBlazeDb, AddDbContext, LINQ over live rows, SaveChanges as one transaction, and where translation stops.",
                "efcore entity framework DbContext UseBlazeDb AddDbContext EnsureCreatedAsync EnsureDeleted GetBlazeDb LINQ SaveChanges change tracking Include AsNoTracking DbUpdateConcurrencyException CompileQuery transaction",
                typeof(EfCore)),
            new("/transactions", "Transactions",
                "Ambient batches, atomic commit, rollback through an undo log, savepoints and the single-writer rule.",
                "transaction BeginTransaction Commit Rollback RunInTransaction savepoint CreateSavepoint RollbackTo dispose undo atomic batch nesting ambient crash",
                typeof(Transactions)),
            new("/changes", "Changes & live queries",
                "Changed events with the net change of every commit, and live queries that re-run only when a commit concerns them.",
                "changes changed event notification subscribe live query Watch reactive observe change feed BlazeDbLiveQuery outbox audit StateHasChanged",
                typeof(Changes)),
        ]),
        new("Persistence",
        [
            new("/durability", "Durability & recovery",
                "The write-ahead log, snapshots, checkpoints, crash recovery, torn tails and corrupted checksums.",
                "durability wal write-ahead log snapshot checkpoint FlushAsync CheckpointAsync FlushInterval crash recovery torn corrupt manifest",
                typeof(Durability)),
            new("/backups", "Backups & migrations",
                "ExportAsync and ImportAsync, UserVersion and migrations that bring a database forward, and tables the model does not register.",
                "backup export import ExportAsync ImportAsync restore DeleteAsync UserVersion SetUserVersion migration AddMigration schema version UnregisteredTables download",
                typeof(Backups)),
            new("/storage", "Storage backends",
                "The four-method storage contract, the backends that ship, a live trace of engine I/O, OPFS and Web Locks, and storage quota.",
                "storage IBlazeDbStorage opfs BlazeDbOpfsStorage BlazeDbFileStorage BlazeDbInMemoryStorage BlazeDbIndexedDbStorage indexeddb web locks quota QuotaReserveBytes OnQuotaPressure RequestPersistenceAsync LastBackgroundError trace decorator",
                typeof(StorageBackends)),
            new("/tabs", "Multiple tabs",
                "One writer tab, read-only replicas that reload on every checkpoint, and the IndexedDB fallback where OPFS cannot be written.",
                "tabs replica read-only ReadOnly CreateReadOnlyAsync ReloadAsync BlazeDbTabSync BroadcastChannel OnCheckpoint IsReset web locks writer election IndexedDB IsOpfsAvailableAsync",
                typeof(Tabs)),
            new("/encryption", "Encryption at rest",
                "BlazeDbEncryptedStorage over any backend, WebCrypto in the browser, wrong keys, tampering, torn frames and key rotation.",
                "encryption encrypted at rest BlazeDbEncryptedStorage DeriveKey PBKDF2 AES-GCM BlazeDbWebCryptoCipher BlazeDbAesGcmCipher WebCrypto passphrase salt key rotation tamper",
                typeof(Encryption)),
            new("/serialization", "Wire format",
                "The exact bytes a row becomes, field by field, with a hex dump and schema evolution in both directions.",
                "serialization wire format bytes varint tag field number hex BlazeDbBufferWriter BlazeDbBufferReader schema evolution",
                typeof(WireFormat)),
        ]),
        new("Reference",
        [
            new("/errors", "Errors & limits",
                "Every exception the engine raises, triggered live, and the cases that are deliberately not errors.",
                "errors exceptions BlazeDbException duplicate key unique constraint stale row corrupt locked quota failure",
                typeof(Errors)),
            new("/bench", "Benchmarks",
                "The engine's read, write and encoding paths run in your own browser, with what they cost on disk.",
                "benchmarks performance speed throughput latency ns per op reads writes snapshot size",
                typeof(Benchmarks)),
            new("/roadmap", "Roadmap",
                "What ships, what never will, and how the repository is laid out.",
                "roadmap shipped non-goals features repository projects build test",
                typeof(Roadmap)),
            new("/mcp-server", "MCP server",
                "The Model Context Protocol server this site hosts, so an AI agent writes BlazeDb code against the real API.",
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
