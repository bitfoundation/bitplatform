using ModelContextProtocol.Server;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Bit.BlazeDb.Demo.Server.Controllers;

/// <summary>
/// Ready-made workflows for the three things people actually ask an embedded database for: add it to
/// an app, build a data feature with it, and work out why the data does not behave.
/// <para>
/// Each prompt spends its words on the order to call the tools in and on the mistakes that are
/// expensive to make - the ones that produce a database that silently forgets everything on reload,
/// a table whose stored rows decode into the wrong properties, or an engine corrupted by concurrent
/// requests, rather than a compiler error - because the failure mode of an agent with a handful of
/// tools is not ignorance, it is calling them in a sequence that skips the check which would have
/// caught the bug.
/// </para>
/// </summary>
[McpServerPromptType]
public static class McpPrompts
{
    [McpServerPrompt(Name = "add-blazedb-to-app", Title = "Add Bit.BlazeDb to an app")]
    [Description("Walks through adding Bit.BlazeDb to an existing app, in the right order for its hosting model.")]
    public static string AddBlazeDbToApp(
        // The values go into the prompt's own argument schema, which is what a client offers as
        // completions - so picking the model is a choice from a list rather than a spelling test.
        [AllowedValues("unknown", "wasm", "standalone-wasm", "auto", "server", "hybrid", "desktop")]
        [Description("The app's hosting model: wasm, standalone-wasm, auto, server, hybrid or desktop. Pass 'unknown' to have it determined from the project first.")] string hostingModel = "unknown")
    {
        return $"""
            Add Bit.BlazeDb to this app. Its hosting model is: {hostingModel}.

            Work in this order:

            1. If the hosting model is 'unknown', determine it from the project first: look for
               AddInteractiveServerComponents / AddInteractiveWebAssemblyComponents in Program.cs, for a separate .Client
               project, for a standalone WebAssemblyHostBuilder, and for a MAUI/WPF/Windows Forms BlazorWebView host, then
               continue with what you find.
            2. Call `GetBlazeDbSetupGuide` with that hosting model and follow it. In a Blazor Web App the database lives in
               the browser, so the packages and the code that opens it belong in the .Client project.
            3. Add `Bit.BlazeDb.Generators` next to `Bit.BlazeDb` - without it a [BlazeDbTable] type compiles but has no
               `Table` and no `Indexes`. Add `Bit.BlazeDb.Browser` only where the database runs in the browser.
            4. Declare the tables, then run each one through `InspectBlazeDbTable` and fix every BLZ diagnostic it reports.
               Pin field numbers with [BlazeDbField(n)] before the app stores real data.
            5. Open the database once and share it (a singleton service), and never open a browser backend during
               prerendering or a server render.
            6. Build the app and fix what the compiler says.

            Show me the diff for each file you change, and say which project the database code lives in and why.
            """;
    }

    [McpServerPrompt(Name = "implement-blazedb-feature", Title = "Implement a data feature")]
    [Description("Implements a data feature with Bit.BlazeDb - a persisted list, an offline cache, an index-backed filter or range, a transaction, an EF Core context, encryption, multi-tab replicas - using its real API rather than a guessed one.")]
    public static string ImplementBlazeDbFeature(
        [Description("What the data feature should do, in your own words - e.g. 'a todo list that survives a page reload' or 'a range query over dates, newest first'.")] string feature)
    {
        return $"""
            Implement this with Bit.BlazeDb: {feature}

            Work in this order:

            1. Call `SearchBlazeDb` with the request above. It searches the guide, every public member, the generator's
               diagnostics, the setup guides and the demo's sources at once, and every hit tells you the exact follow-up
               call to make.
            2. Read the best hit in full with the call it hands you, before writing any code.
            3. Call `GetBlazeDbApi` for every BlazeDb type you are about to use, and match the member names, types and
               defaults exactly. Every public type is prefixed `BlazeDb`; there is no SQL and no query string anywhere -
               do not borrow names from SQLite, IndexedDB wrappers or another embedded database.
            4. Write the [BlazeDbTable] types first and run them through `InspectBlazeDbTable`: use the `Table` and
               `Indexes.X` members it reports, and put an index on every property you filter, order or range by -
               `[BlazeDbIndex]` for equality, `[BlazeDbOrderedIndex]` for ranges and ordering, a compound index for a
               filter on several properties at once.
            5. Prefer the library's own mechanism over hand-rolling one: `Lookup`/`Range`/`OrderBy` or `BlazeDbQuery` over
               scanning and filtering yourself, a transaction (`BeginTransaction` ... `Commit`) over a sequence of writes
               that can stop half-way, `BlazeDbEncryptedStorage` over encrypting values yourself, `BlazeDbTabSync` over
               polling, and `ExecuteAsync(batchSize)` over a long synchronous scan in the browser.
            6. Rows are live objects: `Update` takes a NEW instance; to write a row changed in place, call
               `UpdateInPlace(row, previousValues)` - or use EF Core, whose `SaveChanges` does that for you.
            7. Write the code, then say which tool call each non-obvious choice came from.

            If the demo already contains a working example of this, fetch it with `GetBlazeDbSourceFile` and follow its
            shape rather than inventing a new one.
            """;
    }

    [McpServerPrompt(Name = "debug-blazedb-database", Title = "Debug a database problem")]
    [Description("Diagnoses a Bit.BlazeDb problem - data lost on reload, a locked database in a second tab, a missing 'Table' member, an index that misses a row, a stale-row or concurrency exception, rows decoding wrongly after a schema change, a frozen UI.")]
    public static string DebugBlazeDbDatabase(
        [Description("What goes wrong, with the table, the page or the exception involved if you know them.")] string symptom)
    {
        return $"""
            Diagnose this Bit.BlazeDb problem: {symptom}

            Work in this order:

            1. If the problem is a build error, run the table type through `InspectBlazeDbTable` and read every BLZ
               diagnostic with `GetBlazeDbDiagnostics` - a rejected table gets no `Table` member at all, so "does not contain
               a definition for 'Table'" is the symptom of a BLZ error, or of a missing `Bit.BlazeDb.Generators` package.
            2. Check the setup against `GetBlazeDbSetupGuide` for this hosting model: the browser backends only in the
               browser and never while prerendering; one database per tab, opened once; storage disposed after the
               database; no concurrent reads and writes on a server.
            3. Call `SearchBlazeDb` with the symptom and read the guide section it points to - "Rows are live objects" covers
               refused updates and stale rows, "Multiple tabs" covers the writer lock and replicas, "Storage quota" covers
               refused flushes, "Schema evolution" covers rows decoding into the wrong properties, "EF Core" covers
               concurrency exceptions and what `SaveChanges` does.
            4. Confirm the API you are relying on with `GetBlazeDbApi` before concluding it is a bug - check the actual
               default value of the option involved (the flush interval, for one, decides when data reaches storage).

            Tell me the cause and the fix, and cite the tool call that established it.
            """;
    }
}
