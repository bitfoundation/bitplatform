using System.ComponentModel;
using ModelContextProtocol.Server;
using System.ComponentModel.DataAnnotations;

namespace Bit.Bsqlite.Demo.Server.Controllers;

/// <summary>
/// Ready-made workflows for the three things people actually ask a browser database for: add it to an
/// app, move an existing Bit.Besql app onto it without losing anyone's data, and work out why a
/// database does not open, opens empty, or will not take a statement.
/// <para>
/// Each prompt spends its words on the order to call the tools in and on the mistakes that are
/// expensive to make - the ones that produce a page that fails only when prerendered, or a returning
/// user who silently starts on an empty database, rather than a compiler error - because the failure
/// mode of an agent with a handful of tools is not ignorance, it is calling them in a sequence that
/// skips the check which would have caught the bug.
/// </para>
/// </summary>
[McpServerPromptType]
public static class McpPrompts
{
    [McpServerPrompt(Name = "add-bsqlite-to-app", Title = "Add Bit.Bsqlite to a Blazor app")]
    [Description("Walks through adding Bit.Bsqlite - EF Core on SQLite in the browser - to an existing Blazor app, in the right order for its hosting model.")]
    public static string AddBsqliteToApp(
        // The values go into the prompt's own argument schema, which is what a client offers as
        // completions - so picking the hosting model is a choice from a list rather than a spelling test.
        [AllowedValues("unknown", "standalone-wasm", "wasm", "auto", "server")]
        [Description("How the app is hosted: standalone-wasm, wasm (Blazor Web App on InteractiveWebAssembly), auto or server. Pass 'unknown' to have it determined from the project first.")] string hosting = "unknown")
    {
        return $"""
            Add Bit.Bsqlite to this Blazor app. Its hosting model is: {hosting}.

            Work in this order:

            1. If the hosting model is 'unknown', determine it from the project first: look for
               WebAssemblyHostBuilder (standalone-wasm), AddInteractiveWebAssemblyComponents and a .Client project (wasm),
               both of those plus AddInteractiveServerComponents (auto), or only AddInteractiveServerComponents (server).
            2. Call `GetBsqliteSetupGuide` with that scenario and follow it. For 'server' it explains why the pages that use
               the database have to run on WebAssembly - do not try to make Interactive Server work.
            3. Call `GetBsqliteStorageProviders` and pick the backends: OPFS first and IndexedDB as its fallback is the
               default answer; in-memory only with `AllowNonPersistentFallback()`. Then call `PlanBsqliteStorageSelection`
               with that preference order - once as is and once with 'opfs' unavailable - and check where the database lands.
            4. In a Blazor Web App, register the services in BOTH the client and the server project (one shared extension
               method), and open the database only once `RendererInfo.IsInteractive` is true. Missing either is the most
               common mistake: it fails during prerendering, not at compile time.
            5. Call `GetBsqliteApi` for every Bsqlite type you use and copy its namespace: `AddBitBsqliteServices` and
               `AddBitBsqliteDbContext` are in `Microsoft.Extensions.DependencyInjection`, `IBsqliteDbContextFactory` in
               `Bit.Bsqlite.EntityFrameworkCore`, the providers in `Bit.Bsqlite.Storage.*`.
            6. Use the async EF Core API throughout; replace any `SaveChanges()`, `ToList()` or `First()` you touch.
            7. Build the app and fix what the compiler says.

            Show me the diff for each file you change, and say which project the registration and the pages live in and why.
            """;
    }

    [McpServerPrompt(Name = "move-from-besql", Title = "Move a Bit.Besql app to Bit.Bsqlite")]
    [Description("Moves an app from Bit.Besql to Bit.Bsqlite so that every existing user's database is imported rather than lost.")]
    public static string MoveFromBesql(
        [Description("The Data Source the app passes to UseSqlite under Bit.Besql, e.g. 'Offline-Client.db'. Pass 'unknown' to have it found in the project first.")] string dataSource = "unknown")
    {
        return $"""
            Move this app from Bit.Besql to Bit.Bsqlite without losing any user's data. Besql's Data Source is: {dataSource}.

            Work in this order:

            1. If the Data Source is 'unknown', find it first: the `UseSqlite("Data Source=...")` inside
               `AddBesqlDbContextFactory`. That exact file name is the key: Bsqlite's cache-storage backend imports the
               database Besql left under `/data/cache/<that name>`, and only under it.
            2. Call `GetBsqliteSetupGuide` with scenario 'move-from-besql' and follow it.
            3. Register `BsqliteCacheStorageProvider` and make 'cache-storage' the FIRST candidate, and pass the Data Source
               to `AddDatabase`. Then prove it with `PlanBsqliteStorageSelection`: with no existing binding (a user opening
               the new version for the first time) the database must open on 'cache-storage'. Any other backend ranked
               first gives every returning user an empty database there, silently.
            4. Remove Bit.Besql's script tag and its `ConfigureSqliteJournalMode()` calls, move `dbContextInitializer` work
               to `OnFirstUse` (migrations already run by default) and a compiled model to `options.Configure`.
            5. Replace every synchronous EF call with its async overload: they worked under Besql and throw
               `BsqliteSynchronousApiNotSupportedException` under Bsqlite. Search the code for `SaveChanges()`,
               `.ToList()`, `.First(`, `.Single(`, `.Count()` and `CreateDbContext()`.
            6. If the app should end up on OPFS, do not reorder the candidates: migrate deliberately with
               `BsqliteStorageMigrator` once the database is bound to 'cache-storage' - fetch
               `GetBsqliteSourceFile(path: "Demo/Client/BrowserDatabase.cs")` for the demo's working version.
            7. Build the app and fix what the compiler says.

            Show me the diff for each file you change, and the PlanBsqliteStorageSelection result that shows a first open
            lands on cache-storage.
            """;
    }

    [McpServerPrompt(Name = "debug-bsqlite-storage", Title = "Debug a database that does not open or behave")]
    [Description("Diagnoses a Bit.Bsqlite problem - a database that does not open, opens empty, lands on the wrong backend, loses its data on reload, refuses a statement, or a live query that does not refresh.")]
    public static string DebugBsqliteStorage(
        [Description("What goes wrong, with the exception message, the browser and the backend involved if you know them.")] string symptom)
    {
        return $"""
            Diagnose this Bit.Bsqlite problem: {symptom}

            Work in this order:

            1. Check the wiring against `GetBsqliteSetupGuide` for the app's hosting model first: services registered in
               every container that renders the page (the server too, when prerendering), the database opened only once
               `RendererInfo.IsInteractive` is true, the page running on WebAssembly, async EF calls only.
            2. For anything about which backend a database is on - empty after a reload, empty after a browser update,
               `BsqliteStorageUnavailableException` - reproduce it with `PlanBsqliteStorageSelection`: the app's preference
               order, the backends this browser lacks, and the existing binding if there is one. It runs the library's own
               selection and reports every candidate's status. Remember that existing data outranks preference, and that a
               non-persistent fallback needs `AllowNonPersistentFallback()`.
            3. For a statement that is refused, pass it to `CheckBsqliteSql`: it runs the library's guards per backend and
               returns the exact message. WAL is refused everywhere; ATTACH and PRAGMA page_size depend on the backend's
               declared capabilities (`GetBsqliteStorageProviders`).
            4. For a live query that does not refresh, `CheckBsqliteSql` also shows the tables a query over that SQL
               watches; then read `GetBsqliteDocument(name: "live-queries")` - nothing is reported while a transaction is
               open, and a disposed query stops listening.
            5. Call `SearchBsqlite` with the exception's type or message, and confirm the API you rely on with
               `GetBsqliteApi` - the actual default of the option involved - before concluding it is a bug.

            Tell me the cause and the fix, and cite the tool call that established it.
            """;
    }
}
