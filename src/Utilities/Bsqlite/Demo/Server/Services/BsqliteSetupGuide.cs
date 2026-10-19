using System.Text;
using Bit.Bsqlite.Demo.Server.Dtos;
using System.Text.RegularExpressions;

namespace Bit.Bsqlite.Demo.Server.Services;

/// <summary>
/// Assembles the wiring for one scenario: the checklist, the code, the library's own documents on the
/// subject quoted verbatim, and - where the demo does it - the demo's real files.
/// <para>
/// Bsqlite's setup is short, which is exactly why an agent gets it wrong: it registers the services
/// only in the client of a Blazor Web App and prerendering fails, opens the database in
/// OnInitializedAsync and prerendering fails differently, reaches for Interactive Server, calls
/// SaveChanges(), or ranks a new backend above the one a Bit.Besql app's data is in and starts every
/// returning user on an empty database. None of those is a compiler error. Each scenario here leads with
/// the rule that prevents its version of the mistake, and the documents it quotes say the rest in the
/// library's own words, so the caveats cannot drift from a paraphrase of them.
/// </para>
/// </summary>
public static partial class BsqliteSetupGuide
{
    public static readonly string[] Scenarios = ["standalone-wasm", "wasm", "auto", "server", "storage", "move-from-besql", "testing", "custom-provider"];

    /// <summary>The demo files that make up the wiring of a prerendered Blazor Web App on InteractiveWebAssembly - the demo's own model.</summary>
    private static readonly string[] _webAppFiles =
    [
        "Demo/Client/Extensions/IServiceCollectionExtensions.cs",
        "Demo/Server/Program.cs",
        "Demo/Client/Program.cs",
        "Demo/Server/Components/App.razor",
        "Demo/Client/Components/SessionLifecycle.razor",
        "Demo/Client/BrowserDatabase.cs",
        "Demo/Client/Routes.razor",
        "Demo/Client/_Imports.razor",
    ];

    private sealed record Scenario(string Title, string Summary, string Checklist, string? Code, string[] Files, (string Document, string Section)[] Quotes, string Next);

    /// <summary>The wiring for one scenario as Markdown, or null when the scenario is not one of <see cref="Scenarios"/>.</summary>
    public static string? Get(string? scenario) => Describe(scenario) is { } found ? Compose(found) : null;

    /// <summary>What sets one scenario apart, in a paragraph - the line the search index ranks a setup guide by.</summary>
    public static string Summary(string scenario) => Describe(scenario) is { } found ? $"{found.Title}. {found.Summary.ReplaceLineEndings(" ")}" : string.Empty;

    /// <summary>The scenario's title, for a listing.</summary>
    public static string Title(string scenario) => Describe(scenario)?.Title ?? scenario;

    /// <summary>
    /// What a scenario quotes - its document sections and its demo files. A guide leaves out a quote it
    /// cannot find rather than failing, so this is what lets a test notice a heading renamed underneath it.
    /// </summary>
    internal static ((string Document, string Section)[] Quotes, string[] Files) Sources(string scenario)
        => Describe(scenario) is { } found ? (found.Quotes, found.Files) : ([], []);

    /// <summary>The scenario a caller means: its own name, or one of the spellings people reach for first.</summary>
    public static string? Resolve(string? scenario)
    {
        var key = (scenario ?? string.Empty).Trim().ToLowerInvariant().Replace(' ', '-').Replace('_', '-');

        key = key switch
        {
            "standalone" or "standalone-webassembly" or "blazor-webassembly" or "webassembly-standalone" or "wasm-standalone" => "standalone-wasm",
            "webassembly" or "interactivewebassembly" or "blazor-web-app" or "web-app" or "prerendering" or "prerendered" => "wasm",
            "interactiveauto" or "interactive-auto" => "auto",
            "interactiveserver" or "interactive-server" or "blazor-server" => "server",
            "storage-providers" or "providers" or "backends" or "storage-backends" or "opfs" or "indexeddb" => "storage",
            "besql" or "bit.besql" or "from-besql" or "migrate-from-besql" or "cache-storage" => "move-from-besql",
            "tests" or "test" or "unit-testing" or "unit-tests" or "mstest" or "bit.bsqlite.testing" => "testing",
            "provider" or "custom-storage" or "storage-provider" or "custom-storage-provider" or "custom-backend" or "vfs" => "custom-provider",
            _ => key
        };

        return Scenarios.Contains(key, StringComparer.Ordinal) ? key : null;
    }

    private static Scenario? Describe(string? scenario) => Resolve(scenario) switch
    {
        "standalone-wasm" => new Scenario(
            "standalone Blazor WebAssembly",
            """
            One project and no prerendering: the app runs on WebAssembly from its first render, so a page can open the
            database whenever it likes - OnInitializedAsync included - and everything Bsqlite does is available.
            """,
            HostingChecklist,
            StandaloneCode,
            [],
            [("getting-started", "Register"), ("getting-started", "Use EF Core")],
            NextHosting),

        "wasm" => new Scenario(
            "Blazor Web App - InteractiveWebAssembly with prerendering",
            """
            How this demo is built. Every page is prerendered on the server and then runs on WebAssembly, where the
            database lives. So the services are registered in BOTH containers - the prerender pass instantiates every
            page on the server, and whatever a page injects has to resolve there - which is harmless, because nothing
            touches the browser until a page opens the database. And a page opens it only once
            `RendererInfo.IsInteractive` is true: during prerendering OnInitializedAsync runs on the server, where there
            is no browser to open a database in. The demo shares one registration method between the two projects and
            gates every page through one component; both are below, as they ship.
            """,
            HostingChecklist + "\n" + WebAppChecklist,
            WebAppCode,
            _webAppFiles,
            [("readme", "Installation"), ("readme", "Where it runs")],
            NextHosting),

        "auto" => new Scenario(
            "Blazor Web App - InteractiveAuto",
            """
            A page on InteractiveAuto runs on the server over a circuit until the WebAssembly runtime has downloaded,
            and on WebAssembly after that. Bsqlite works once the page runs on WebAssembly - on the circuit the engine
            would be in the user's tab while EF Core ran on the server, which is not supported. Two things follow.
            `RendererInfo.IsInteractive` is true on the circuit as well, so it is not the gate here:
            `OperatingSystem.IsBrowser()` is. And a component that started on the circuit stays there until the next
            full load, so the reliable setup is per-page render modes: InteractiveWebAssembly for the pages that use the
            database, Auto for the rest. Everything else is the InteractiveWebAssembly setup, whose files are the demo's.
            """,
            HostingChecklist + "\n" + WebAppChecklist + "\n" + AutoChecklist,
            AutoCode,
            _webAppFiles,
            [("readme", "Where it runs")],
            NextHosting),

        "server" => new Scenario(
            "Blazor Web App - InteractiveServer (not supported)",
            """
            Not supported, by design: the engine runs in the user's browser tab, in a worker, and EF Core on the server
            would need a native SQLite of its own rather than a database in someone's browser. A server-rendered app can
            still use Bsqlite for the pages that need an on-device database: render those with InteractiveWebAssembly
            (they live in a .Client project) and keep Interactive Server for the rest. If the data belongs on the server
            instead, that is Microsoft.EntityFrameworkCore.Sqlite or any other server database - not Bsqlite.
            """,
            ServerChecklist,
            ServerCode,
            [],
            [("readme", "Where it runs")],
            "For the pages that do run on WebAssembly, follow `GetBsqliteSetupGuide(scenario: \"wasm\")`."),

        "storage" => new Scenario(
            "Choosing and adding storage backends",
            """
            Every backend is a package plus one registration line, and the preference order is yours to set. Two rules
            decide more than the order does: existing data outranks preference - a database stays on the backend that
            holds it - and nothing is substituted silently, so a non-persistent fallback needs an explicit opt-in. The
            table below is read off the provider types themselves.
            """,
            StorageChecklist,
            StorageTable() + "\n" + StorageCode,
            ["Demo/Client/Extensions/IServiceCollectionExtensions.cs", "Demo/Client/BrowserDatabase.cs"],
            [("storage", "Selection"), ("storage", "Migration"), ("storage", "Admin")],
            "`PlanBsqliteStorageSelection` runs this selection on a browser you describe - which backends are available, " +
            "where the data already is, which migration mode - and reports every candidate's fate. " +
            "`GetBsqliteStorageProviders(provider: \"...\")` has one backend in full."),

        "move-from-besql" => new Scenario(
            "Moving off Bit.Besql",
            """
            Bit.Besql keeps a whole SQLite file in Cache Storage - the `bit-Besql` cache, at `/data/cache/{file name}`.
            The `cache-storage` backend reads that file: the first time a database is opened on it and Bsqlite has no
            copy of its own, the Besql file of the same name is imported losslessly into Bsqlite's page layout, so the
            app's users keep their data. Two things decide whether that happens: the database name must be Besql's
            Data Source exactly, and `cache-storage` must be the first choice - a database with no binding yet is
            created on the first available candidate, so any other backend ranked above it starts every returning user
            on an empty database and the Besql file is never read.
            """,
            BesqlChecklist,
            BesqlCode,
            [],
            [("readme", "Moving off Bit.Besql"), ("Bit.Bsqlite.Storage.CacheStorage", "Capabilities")],
            "`GetBsqliteStorageProviders(provider: \"cache-storage\")` has the backend in full; for the page lifecycle " +
            "under prerendering, `GetBsqliteSetupGuide(scenario: \"wasm\")`."),

        "testing" => new Scenario(
            "Unit testing with Bit.Bsqlite.Testing",
            """
            One package, two jobs. `BsqliteInProcessTransport` stands in for the browser's worker with Microsoft.Data.Sqlite,
            so EF Core mappings, migrations, generated SQL and live queries run on desktop .NET in a test - it installs
            the same EF function set the worker does and reports writes the way the worker does. And the MSTest
            conformance kit is what every storage provider must pass.
            """,
            TestingChecklist,
            TestingCode,
            [],
            [("Bit.Bsqlite.Testing", "1. Run the stack without a browser"), ("Bit.Bsqlite.Testing", "2. The storage conformance kit")],
            "`GetBsqliteApi(typeName: \"BsqliteInProcessTransport\")` has the transport's constructor and what it records; " +
            "`GetBsqliteSetupGuide(scenario: \"custom-provider\")` is the conformance kit's other half."),

        "custom-provider" => new Scenario(
            "Writing a custom storage provider",
            """
            A backend is two halves: a .NET `IBsqliteStorageProvider` that declares capabilities and answers probes, and a
            JavaScript VFS the worker registers with the engine. The contract is public and the core consumes nothing
            else, so a backend needs no change to Bsqlite - Cache Storage was built that way. What it does need is
            honesty: the core trusts the declared capabilities without checking them, which is what the conformance kit
            is for.
            """,
            ProviderChecklist,
            ProviderContract() + "\n" + ProviderCode,
            [],
            [("storage", "Writing a provider"), ("Bit.Bsqlite.Testing", "2. The storage conformance kit")],
            "`GetBsqliteApi(typeName: \"BsqliteInMemoryStorageProvider\")` is the contract's reference implementation - the " +
            "smallest provider that passes the kit - and `GetBsqliteStorageProviders()` shows what the first-party " +
            "backends declare."),

        _ => null
    };

    private static string Compose(Scenario scenario)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"# Setting Bsqlite up - {scenario.Title}").AppendLine();
        builder.AppendLine(scenario.Summary).AppendLine();
        builder.AppendLine(scenario.Checklist).AppendLine();

        if (scenario.Code is not null) builder.AppendLine(scenario.Code).AppendLine();

        if (scenario.Files.Length > 0)
        {
            builder.AppendLine("## The files, from this demo").AppendLine();
            builder.AppendLine("Everything below compiles as it ships; rename the projects and namespaces to your own. The host's ")
                   .AppendLine("Program.cs also runs the demo's model on the stock provider for its /api/products endpoint and hosts this ")
                   .AppendLine("MCP server - neither is anything Bsqlite needs.")
                   .AppendLine();

            foreach (var path in scenario.Files)
            {
                var content = BsqliteSourceCatalog.GetSourceFile(path);
                if (content is null) continue;

                AppendFile(builder, path, content);
            }
        }

        // The library's own words on the subject, so the caveats are the library's rather than a
        // paraphrase of them that can drift.
        builder.AppendLine("## From the documentation").AppendLine();

        foreach (var (document, section) in scenario.Quotes)
        {
            var text = BsqliteSourceCatalog.GetDocumentSection(document, section);
            if (text is null) continue;

            builder.AppendLine($"_{document}:_").AppendLine();
            builder.AppendLine(Demote(text)).AppendLine();
        }

        builder.AppendLine(scenario.Next);

        return builder.ToString();
    }

    private const string HostingChecklist = """
        ## Checklist

        1. `dotnet add package Bit.Bsqlite` plus at least one persistent backend - `Bit.Bsqlite.Storage.Opfs` first and
           `Bit.Bsqlite.Storage.IndexedDb` as its fallback. In a Blazor Web App, add them to the .Client project.
        2. Register with `AddBitBsqliteServices` (namespace `Microsoft.Extensions.DependencyInjection`): each backend with
           `AddStorageProvider<T>()`, the preference order with `UseStorage(s => s.Prefer(...).Fallback(...))` - required,
           selection never guesses - and the database with `AddDatabase("app.db")`.
        3. Register the context with `AddBitBsqliteDbContext<AppDbContext>()`. Migrations run once per session by default
           (`BsqliteSchemaStrategy.Migrate`); seed through `OnFirstUse`. Do not also call `AddDbContext` or `UseSqlite`:
           the factory binds every context to the one shared browser database.
        4. Inject `IBsqliteDbContextFactory<AppDbContext>` (namespace `Bit.Bsqlite.EntityFrameworkCore`; the plain
           `IDbContextFactory<AppDbContext>` is registered too) and create contexts with `await CreateDbContextAsync()`.
        5. Async only: `ToListAsync`, `SaveChangesAsync`, `MigrateAsync`. The synchronous members throw
           `BsqliteSynchronousApiNotSupportedException` naming the async overload - the engine runs in a worker the main
           thread cannot wait on.
        6. Nothing goes into the host page: the engine, the worker and the VFS modules are static web assets under
           `_content/Bit.Bsqlite*/`, imported on demand.
        7. Keep the model in a project that references only EF Core Relational, as the demo's Data project does, and
           scaffold migrations with `dotnet ef` against the stock SQLite provider: the same migrations apply in the browser.
        """;

    private const string WebAppChecklist = """
        ### In a Blazor Web App

        8. Register the services in the server project too - the prerender pass resolves everything a page injects. Put
           the registration in one extension method in the .Client project and call it from both Program.cs files, as the
           demo's `AddDemoServices` is.
        9. Open the database only once the page is interactive: `if (RendererInfo.IsInteractive is false) return;` before
           the first `CreateDbContextAsync()` (on .NET 8, which has no `RendererInfo`, `OperatingSystem.IsBrowser()`). The
           demo's `SessionLifecycle` component does it for every page.
        10. Render the pages that use the database with InteractiveWebAssembly - the demo sets it once, on `<Routes>`.
        11. Drop live queries and contexts when `BsqliteDatabase.Closing` fires and reload on `Opened`: that is how a page
            survives the database being moved or replaced underneath it.
        """;

    private const string AutoChecklist = """
        ### Under InteractiveAuto

        12. Put no render mode on `<Routes>`: give each page its own, InteractiveWebAssembly for every page that uses the
            database. A page inside an interactive parent cannot switch to another render mode.
        13. Where a page that touches the database has to stay on Auto, gate it on `OperatingSystem.IsBrowser()`, not
            `RendererInfo.IsInteractive` - the circuit is interactive too - and render a placeholder until it is true.
        """;

    private const string ServerChecklist = """
        ## Checklist

        1. Do not open a Bsqlite database from a server-rendered component. The services resolve on a circuit - every one
           is scoped per circuit, so nothing is shared between users - but opening a database there is not a supported path.
        2. Add a .Client project and `AddInteractiveWebAssemblyComponents()` / `AddInteractiveWebAssemblyRenderMode()` next
           to the Interactive Server ones, and move the pages that use the browser database into it with
           `@rendermode InteractiveWebAssembly`.
        3. Register Bsqlite in both projects, as for any prerendered Web App, and open the database only once the page is
           interactive on WebAssembly.
        """;

    private const string StorageChecklist = """
        ## Checklist

        1. OPFS first (`Bit.Bsqlite.Storage.Opfs`): the only persistent backend on the synchronous engine build - the
           smallest download - and the fastest storage the web has.
        2. IndexedDB as its fallback (`Bit.Bsqlite.Storage.IndexedDb`): the widest reach, where OPFS is missing.
        3. In-memory (`Bit.Bsqlite.Storage.InMemory`, in the core package) only as a declared last resort - with
           `AllowNonPersistentFallback()` - or for demos and tests. Data written to it is gone on reload.
        4. Cache Storage (`Bit.Bsqlite.Storage.CacheStorage`) when moving off Bit.Besql - see that scenario - and otherwise
           not: it exists for the import.
        5. Leave the migration mode at `KeepExisting` unless moving a user's database without asking is acceptable.
           `AutomaticOnOpen` copies, verifies and only then flips the binding; `Manual` reports
           `Resolution.BetterProviderAvailable` for the app to act on.
        6. Show `session.Resolution` (or `BsqliteDatabase.Resolution`) somewhere a support person can read it: it records
           every candidate, its probe and why it was passed over.
        """;

    private const string BesqlChecklist = """
        ## Checklist

        1. `dotnet remove package Bit.Besql`, then `dotnet add package Bit.Bsqlite` and
           `dotnet add package Bit.Bsqlite.Storage.CacheStorage`.
        2. Remove `<script src="_content/Bit.Besql/bit-besql.js"></script>` from the host page; Bsqlite needs nothing there.
        3. Replace `AddBesqlDbContextFactory<TContext>(...)` with `AddBitBsqliteServices` and
           `AddBitBsqliteDbContext<TContext>`, naming the database exactly as Besql's `Data Source` - the import looks for
           `/data/cache/{that name}`.
        4. Make `cache-storage` the first (or only) choice, as above. A database that has been opened is bound there for
           good; moving it to OPFS later is a deliberate migration (see the last step), never a reordering.
        5. Move what Besql's `dbContextInitializer` did: `MigrateAsync` is the default schema step, anything else goes in
           `OnFirstUse`. A compiled model passed to `UseModel` moves to `options.Configure`. Drop the calls to Besql's
           `ConfigureSqliteJournalMode()` - Bsqlite never runs in WAL mode.
        6. Code that injects `IDbContextFactory<TContext>` keeps working, but every synchronous call has to become async:
           Besql ran SQLite on the main thread, Bsqlite runs it in a worker, and `SaveChanges()`, `ToList()` and the like
           throw `BsqliteSynchronousApiNotSupportedException`. `CreateDbContext()` works only after the first
           `CreateDbContextAsync()`.
        7. Where Besql said "use the factory in OnAfterRender", open the database once `RendererInfo.IsInteractive` is
           true (see the `wasm` scenario). If the app also ran on Blazor Server or Hybrid, where Besql fell back to a file
           on disk, keep `Microsoft.EntityFrameworkCore.Sqlite` for those hosts: Bsqlite is for pages running on WebAssembly.
        8. To move the data to OPFS once it is in Bsqlite, migrate it explicitly - inject `BsqliteStorageMigrator`, the
           providers and `IBsqliteStorageBindingStore`, and call `MigrateAsync` inside `BsqliteDatabase.WhileClosedAsync`,
           as the demo's `BrowserDatabase.SwitchStorageAsync` does. Ranking OPFS above `cache-storage` instead would start
           users who have not opened the new version yet on an empty OPFS database.
        """;

    private const string TestingChecklist = """
        ## Checklist

        1. A desktop test project (net8.0, net9.0 or net10.0) on MSTest, and `dotnet add package Bit.Bsqlite.Testing` - it
           brings Microsoft.Data.Sqlite with its native engine, which belongs in tests and never in the browser app.
        2. `new BsqliteInProcessTransport()` is a private in-memory database; wrap it in
           `new BsqliteConnection(transport, "test.db")`, `await connection.OpenAsync()`, and hand the connection to
           `UseBitBsqlite` - the same call the browser makes. One transport is one database: `await using` it.
        3. Assert the SQL EF generated with `transport.ExecutedCommands`.
        4. The async rule holds here too - synchronous members throw - so a test catches a stray `SaveChanges()` before a
           user does. Live queries behave as in the browser: writes are reported from the update hook once they commit.
        5. To run the whole DI composition, register an `IBsqliteTransportFactory` that returns the in-process transport,
           the in-memory backend, and `UseBindingStore<BsqliteInMemoryStorageBindingStore>()` (the default store needs a
           browser's localStorage).
        6. A storage backend's test project inherits `BsqliteStorageProviderConformanceTests` (and, with a running engine,
           `BsqliteStorageEngineConformanceTests`) from namespace `Bit.Bsqlite.Storage.ConformanceTests`, with
           `[TestClass]` on each derived class; a backend that cannot run in that environment reports inconclusive.
        """;

    private const string ProviderChecklist = """
        ## Checklist

        1. A stable, qualified `Name` ("mycompany-encrypted", not "encrypted"): it is recorded as the sticky binding of
           every database the backend creates, so renaming it after release orphans every user's data.
        2. `Capabilities` that are constant and honest: `RequiredBuild` (`AsyncCapable` for any VFS that awaits storage)
           and `IsPersistent` are required; everything else defaults to the least capable answer - declare a capability
           only when the VFS really provides it. The core gates ATTACH, `PRAGMA page_size` and fallback on these.
        3. `ProbeAsync` reports unavailability with `BsqliteProbeResult.Unavailable(reason)` rather than throwing, and
           needs a browser: take an optional `IJSRuntime` and report unavailable without one, as the first-party
           providers do. The core probes at most once per session.
        4. `Admin` works on databases as files with no engine running - exists, list, delete, export, import - which is
           what migration and backups are built on.
        5. `VfsModule` points at an ES module in your package's static assets that exports `register`. Import the base
           classes relatively from `../Bit.Bsqlite/engine/`, so an app served under a sub-path still finds them.
        6. Register it with `AddStorageProvider<T>()` and inherit the conformance kit in its test project.
        """;

    private const string StandaloneCode = """
        ## The code

        ### `Program.cs`

        ```csharp
        using Bit.Bsqlite.Storage.IndexedDb;
        using Bit.Bsqlite.Storage.Opfs;
        using Microsoft.AspNetCore.Components.Web;
        using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

        var builder = WebAssemblyHostBuilder.CreateDefault(args);

        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        builder.Services.AddBitBsqliteServices(sqlite => sqlite
            .AddStorageProvider<BsqliteOpfsStorageProvider>()
            .AddStorageProvider<BsqliteIndexedDbStorageProvider>()
            .UseStorage(s => s
                .Prefer(BsqliteOpfsStorageProvider.ProviderName)
                .Fallback(BsqliteIndexedDbStorageProvider.ProviderName))
            .AddDatabase("app.db"));

        builder.Services.AddBitBsqliteDbContext<AppDbContext>();

        await builder.Build().RunAsync();
        ```

        ### `Pages/Products.razor`

        ```razor
        @page "/products"
        @using Bit.Bsqlite.EntityFrameworkCore
        @using Microsoft.EntityFrameworkCore
        @inject IBsqliteDbContextFactory<AppDbContext> Contexts

        <ul>
            @foreach (var product in _products)
            {
                <li>@product.Name - @product.Price</li>
            }
        </ul>

        <button @onclick="AddAsync">Add</button>

        @code {
            private List<Product> _products = [];

            protected override async Task OnInitializedAsync()
            {
                // The first context opens the database and applies the migrations; later ones reuse the session.
                await using var db = await Contexts.CreateDbContextAsync();
                _products = await db.Products.OrderBy(p => p.Name).ToListAsync();
            }

            private async Task AddAsync()
            {
                await using var db = await Contexts.CreateDbContextAsync();
                db.Products.Add(new Product { Name = $"Widget {_products.Count + 1}", Price = 19.99m });
                await db.SaveChangesAsync();
                _products = await db.Products.OrderBy(p => p.Name).ToListAsync();
            }
        }
        ```

        There is no script tag or stylesheet to add to `wwwroot/index.html`.
        """;

    private const string WebAppCode = """
        ## A page, without the demo's helpers

        The demo routes every page through its own `SessionLifecycle` component and `BrowserDatabase` wrapper (both
        below). An app needs neither - this is the whole rule:

        ```razor
        @page "/orders"
        @using Bit.Bsqlite.EntityFrameworkCore
        @using Microsoft.EntityFrameworkCore
        @inject IBsqliteDbContextFactory<AppDbContext> Contexts

        @code {
            private List<SalesOrder>? _orders;

            protected override async Task OnInitializedAsync()
            {
                // Prerendering runs this on the server first, where there is no browser to open a database in.
                // It runs again once the page is interactive on WebAssembly.
                if (RendererInfo.IsInteractive is false) return;

                await using var db = await Contexts.CreateDbContextAsync();
                _orders = await db.Orders.Where(o => o.Status == OrderStatus.Draft).ToListAsync();
            }
        }
        ```
        """;

    private const string AutoCode = """
        ## The differences from InteractiveWebAssembly

        ### Server `Program.cs`

        ```csharp
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents()
            .AddInteractiveWebAssemblyComponents();

        builder.Services.AddAppServices();   // the client's registration, Bsqlite's included

        // ...

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode()
            .AddInteractiveWebAssemblyRenderMode()
            .AddAdditionalAssemblies(typeof(MyApp.Client._Imports).Assembly);
        ```

        ### `App.razor`

        ```razor
        @* No render mode here: each page chooses its own. *@
        <Routes />
        ```

        ### A page that uses the database

        ```razor
        @page "/orders"
        @rendermode InteractiveWebAssembly
        @inject IBsqliteDbContextFactory<AppDbContext> Contexts
        ```

        ### A component that has to stay on Auto

        ```razor
        @code {
            protected override async Task OnInitializedAsync()
            {
                // False while prerendering AND on the server circuit Auto starts on: true only on WebAssembly.
                if (OperatingSystem.IsBrowser() is false) return;

                await using var db = await Contexts.CreateDbContextAsync();
                // ...
            }
        }
        ```
        """;

    private const string ServerCode = """
        ## The code

        ### Server `Program.cs`

        ```csharp
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents()
            .AddInteractiveWebAssemblyComponents();

        builder.Services.AddAppServices();   // shared with the .Client project, Bsqlite's registration included

        // ...

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode()
            .AddInteractiveWebAssemblyRenderMode()
            .AddAdditionalAssemblies(typeof(MyApp.Client._Imports).Assembly);
        ```

        ### `MyApp.Client/Pages/Notes.razor`

        ```razor
        @page "/notes"
        @rendermode InteractiveWebAssembly
        @inject IBsqliteDbContextFactory<NotesDbContext> Contexts
        ```
        """;

    private const string StorageCode = """
        ### Every first-party backend, in the recommended order

        ```csharp
        using Bit.Bsqlite.Storage.IndexedDb;
        using Bit.Bsqlite.Storage.InMemory;
        using Bit.Bsqlite.Storage.Opfs;

        builder.Services.AddBitBsqliteServices(sqlite => sqlite
            .AddStorageProvider<BsqliteOpfsStorageProvider>()
            .AddStorageProvider<BsqliteIndexedDbStorageProvider>()
            .AddStorageProvider<BsqliteInMemoryStorageProvider>()
            .UseStorage(s => s
                .Prefer(BsqliteOpfsStorageProvider.ProviderName)
                .Fallback(BsqliteIndexedDbStorageProvider.ProviderName)
                .Fallback(BsqliteInMemoryStorageProvider.ProviderName)
                .AllowNonPersistentFallback())          // the in-memory fallback is refused without it
            .AddDatabase("app.db"));
        ```

        ### Why this session landed where it did

        ```csharp
        // In a component that injects BsqliteDatabase (namespace Bit.Bsqlite.Interop) as Database:
        var session = await Database.OpenAsync();
        var resolution = session.Resolution;

        // resolution.Provider.Name, resolution.IsFirstChoice, resolution.WasDecidedByExistingData,
        // resolution.BetterProviderAvailable, and every candidate in resolution.Attempts with its status and reason.
        ```
        """;

    private const string BesqlCode = """
        ## The code

        ### Before - Bit.Besql

        ```csharp
        services.AddBesqlDbContextFactory<OfflineDbContext>((sp, optionsBuilder) =>
        {
            optionsBuilder
                .UseModel(OfflineDbContextModel.Instance)
                .UseSqlite("Data Source=Offline-Client.db");
        }, dbContextInitializer: async (sp, dbContext) => await dbContext.Database.MigrateAsync());
        ```

        ### After - Bit.Bsqlite

        ```csharp
        using Bit.Bsqlite.Storage.CacheStorage;

        services.AddBitBsqliteServices(sqlite => sqlite
            .AddStorageProvider<BsqliteCacheStorageProvider>()
            .UseStorage(s => s.Prefer(BsqliteCacheStorageProvider.ProviderName))
            .AddDatabase("Offline-Client.db"));             // Besql's Data Source, exactly

        services.AddBitBsqliteDbContext<OfflineDbContext>(options =>
        {
            // Migrate is the default schema step; Configure runs after UseBitBsqlite on every context's options.
            options.Configure = (sp, builder) => builder.UseModel(OfflineDbContextModel.Instance);
        });
        ```

        ### In a component

        ```razor
        @* IDbContextFactory<T> is still registered, next to IBsqliteDbContextFactory<T>. *@
        @inject IDbContextFactory<OfflineDbContext> Contexts

        @code {
            private async Task SaveAsync(Todo todo)
            {
                await using var db = await Contexts.CreateDbContextAsync();
                db.Todos.Add(todo);
                await db.SaveChangesAsync();   // SaveChanges() throws: the engine is in a worker
            }
        }
        ```
        """;

    private const string TestingCode = """
        ## The code

        ### A test of the model, the migrations and the SQL

        ```csharp
        using Bit.Bsqlite.Data;
        using Bit.Bsqlite.Testing;
        using Microsoft.EntityFrameworkCore;

        [TestClass]
        public sealed class ProductTests
        {
            public TestContext TestContext { get; set; } = null!;

            [TestMethod]
            public async Task A_saved_product_reads_back()
            {
                var ct = TestContext.CancellationToken;

                await using var transport = new BsqliteInProcessTransport();
                await using var connection = new BsqliteConnection(transport, "test.db");
                await connection.OpenAsync(ct);

                await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                    .UseBitBsqlite(connection)
                    .Options);

                await db.Database.MigrateAsync(ct);

                db.Products.Add(new Product { Name = "Widget", Price = 19.99m });
                await db.SaveChangesAsync(ct);
                db.ChangeTracker.Clear();

                Assert.AreEqual(19.99m, (await db.Products.SingleAsync(ct)).Price);
                Assert.IsTrue(transport.ExecutedCommands.Any(sql => sql.Contains("INSERT INTO", StringComparison.Ordinal)));
            }
        }
        ```

        ### The whole composition, through dependency injection

        ```csharp
        using Bit.Bsqlite.Data;
        using Bit.Bsqlite.Interop;
        using Bit.Bsqlite.Storage;
        using Bit.Bsqlite.Storage.InMemory;
        using Bit.Bsqlite.Testing;

        var services = new ServiceCollection();

        services.AddBitBsqliteServices(sqlite => sqlite
            .AddStorageProvider<BsqliteInMemoryStorageProvider>()
            .UseStorage(s => s.Prefer(BsqliteInMemoryStorageProvider.ProviderName))
            .UseBindingStore<BsqliteInMemoryStorageBindingStore>()
            .AddDatabase("app.db"));

        // Desktop has no worker; the in-process engine stands in for it.
        services.AddSingleton<IBsqliteTransportFactory>(new InProcessTransports());

        services.AddBitBsqliteDbContext<AppDbContext>();

        sealed class InProcessTransports : IBsqliteTransportFactory
        {
            public IBsqliteTransport Create(IBsqliteStorageProvider provider) => new BsqliteInProcessTransport();
        }
        ```

        ### A storage backend's conformance tests

        ```csharp
        using Bit.Bsqlite.Storage;
        using Bit.Bsqlite.Storage.ConformanceTests;

        [TestClass]
        public sealed class MyBackendConformanceTests : BsqliteStorageProviderConformanceTests
        {
            protected override IBsqliteStorageProvider CreateProvider() => new MyStorageProvider();
        }
        ```
        """;

    private const string ProviderCode = """
        ## The code

        ### The .NET half

        ```csharp
        using Bit.Bsqlite.Storage;
        using Microsoft.JSInterop;

        public sealed class MyStorageProvider(IJSRuntime? js = null) : IBsqliteStorageProvider
        {
            // Recorded as the sticky binding of every database this backend creates: never rename it.
            public const string ProviderName = "mycompany-encrypted";

            public string Name => ProviderName;

            public BsqliteStorageCapabilities Capabilities { get; } = new()
            {
                RequiredBuild = BsqliteEngineBuild.AsyncCapable,   // the VFS awaits its storage
                IsPersistent = true,
                SupportsMultipleConnections = true,
                // Everything left out is the least capable answer - declare only what the VFS really does.
            };

            public BsqliteJsModule? VfsModule { get; } = new("./_content/MyCompany.Storage/my-vfs.js");

            public IBsqliteStorageAdmin Admin { get; } = new MyStorageAdmin(js);

            public async ValueTask<BsqliteProbeResult> ProbeAsync(CancellationToken cancellationToken = default)
            {
                if (js is null)
                {
                    return BsqliteProbeResult.Unavailable("This backend can only be probed in a browser.");
                }

                var module = await js.InvokeAsync<IJSObjectReference>("import", cancellationToken, "./_content/MyCompany.Storage/my-admin.js");

                try
                {
                    return await module.InvokeAsync<bool>("probe", cancellationToken)
                        ? BsqliteProbeResult.Available()
                        : BsqliteProbeResult.Unavailable("The storage API this backend needs is missing in this browser.");
                }
                finally
                {
                    await module.DisposeAsync();
                }
            }
        }
        ```

        `MyStorageAdmin` implements `IBsqliteStorageAdmin`, whose members are listed above.

        ### The JavaScript half - `wwwroot/my-vfs.js`

        ```javascript
        // The base classes are wa-sqlite's, served by Bit.Bsqlite next to this package's own _content folder.
        import { FacadeVFS } from '../Bit.Bsqlite/engine/FacadeVFS.js';
        import * as VFS from '../Bit.Bsqlite/engine/VFS.js';
        import { WebLocksMixin } from '../Bit.Bsqlite/engine/WebLocksMixin.js';

        const VFS_NAME = 'mycompany-encrypted';

        class MyVFS extends WebLocksMixin(FacadeVFS) {
            // jOpen, jRead, jWrite, jTruncate, jSync, jFileSize, jClose, jDelete, jAccess ...
            // returning VFS.SQLITE_OK or an error code, as the Cache Storage VFS does.
        }

        // Called by the worker with the Emscripten module and the wa-sqlite API it just loaded.
        export async function register({ module, sqlite3, databaseName }) {
            const vfs = new MyVFS(VFS_NAME, module);
            await vfs.isReady();
            sqlite3.vfs_register(vfs, /* makeDefault */ false);
            return { vfsName: VFS_NAME };   // optionally onOpenSql: statements to run after every open
        }
        ```
        """;

    /// <summary>The first-party backends as a compact table, read off the provider types.</summary>
    private static string StorageTable()
    {
        var builder = new StringBuilder();

        builder.AppendLine("## The backends").AppendLine()
               .AppendLine("| Name | Package | Engine build | Persistent | Concurrent reads | ATTACH | PRAGMA page_size |")
               .AppendLine("| --- | --- | --- | --- | --- | --- | --- |");

        foreach (var provider in BsqliteStorageCatalog.Providers)
        {
            string Value(string name) => provider.Capabilities.First(capability => capability.Name == name).Value;

            builder.AppendLine($"| `{provider.Name}` | `{provider.Package}` | {Value("RequiredBuild")} | {Value("IsPersistent")} | " +
                               $"{Value("SupportsConcurrentReads")} | {Allowed(provider, "ATTACH")} | {Allowed(provider, "page_size")} |");
        }

        return builder.ToString();
    }

    private static string Allowed(BsqliteStorageProviderDto provider, string statement)
        => provider.Guarded.First(verdict => verdict.Name.Contains(statement, StringComparison.OrdinalIgnoreCase)).Accepted ? "allowed" : "refused";

    /// <summary>
    /// The members a provider implements, read off the two interfaces of the contract - so the list a
    /// backend author works from cannot fall behind a member added to it.
    /// </summary>
    private static string ProviderContract()
    {
        var builder = new StringBuilder();

        builder.AppendLine("## The contract").AppendLine();

        foreach (var name in new[] { "IBsqliteStorageProvider", "IBsqliteStorageAdmin" })
        {
            var details = BsqliteApiCatalog.GetTypeDetails(name);
            if (details is null) continue;

            builder.AppendLine($"`{details.FullName}`:").AppendLine();

            foreach (var member in details.Members)
            {
                builder.Append($"- `{member.Type} {member.Name}{member.Signature}`");
                if (BsqliteApiCatalog.FirstSentence(member.Summary) is { } summary) builder.Append($" - {summary}");
                builder.AppendLine();
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    private const string NextHosting = """
        The rest is ordinary EF Core and ADO.NET - `GetBsqliteDocument(name: "getting-started")` covers contexts, raw
        ADO.NET and the shared database, `GetBsqliteDocument(name: "live-queries")` queries that re-run themselves,
        `GetBsqliteSetupGuide(scenario: "storage")` choosing backends, and `GetBsqliteSourceFile()` the demo's working pages.
        """;

    /// <summary>
    /// Pushes a quoted section one heading level down, so it nests under "From the documentation"
    /// instead of competing with the headings of this document.
    /// </summary>
    private static string Demote(string section)
    {
        var lines = section.Split('\n');
        var fenced = false;

        for (int i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].TrimStart();

            if (trimmed.StartsWith("```", StringComparison.Ordinal) || trimmed.StartsWith("~~~", StringComparison.Ordinal))
            {
                fenced = fenced is false;
                continue;
            }

            if (fenced is false && lines[i].StartsWith('#')) lines[i] = $"#{lines[i]}";
        }

        return string.Join('\n', lines);
    }

    private static void AppendFile(StringBuilder builder, string path, string content)
    {
        content = content.TrimStart('﻿').TrimEnd();
        var fence = Fence(content);

        builder.AppendLine($"### `{path}`").AppendLine()
               .AppendLine($"{fence}{Language(path)}")
               .AppendLine(content)
               .AppendLine(fence)
               .AppendLine();
    }

    /// <summary>
    /// The fence for a file's code block. It has to outrun the longest run of backticks in the file
    /// itself - a file that shows Markdown would otherwise end the block in the middle of its own
    /// content, and the rest of the file would render as prose.
    /// </summary>
    private static string Fence(string content)
    {
        var longest = BacktickRunRegex().Matches(content).Select(match => match.Length).DefaultIfEmpty(0).Max();

        return new string('`', Math.Max(3, longest + 1));
    }

    private static string Language(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".razor" => "razor",
        ".cs" => "csharp",
        ".csproj" => "xml",
        ".css" => "css",
        ".js" => "javascript",
        _ => string.Empty
    };

    [GeneratedRegex("`+")]
    private static partial Regex BacktickRunRegex();
}
