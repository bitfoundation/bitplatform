using System.Text;
using System.Text.RegularExpressions;

namespace Bit.BlazeDb.Demo.Server.Services;

/// <summary>
/// Assembles the wiring for one hosting model: the checklist, the guide's own section on that
/// hosting model, and - where the demo is that hosting model - the demo's real files.
/// <para>
/// Where a BlazeDb database may live is decided by two facts that an agent gets wrong in ways no
/// compiler catches: the browser backends exist only on WebAssembly (a prerendered or server-rendered
/// component that opens one fails at runtime, not at build time), and the engine assumes one writer
/// with no concurrent readers (a server that shares an engine between requests corrupts it under load,
/// not in a demo). The guide's hosting section, quoted verbatim, says so in the library's own words.
/// </para>
/// </summary>
public static partial class BlazeDbSetupGuide
{
    public static readonly string[] HostingModels = ["wasm", "standalone-wasm", "auto", "server", "hybrid", "desktop"];

    /// <summary>The demo files that make up its wiring - the demo is a Blazor Web App using InteractiveWebAssembly.</summary>
    private static readonly string[] _demoFiles =
    [
        "Demo/Client/Bit.BlazeDb.Demo.Client.csproj",
        "Demo/Client/Program.cs",
        "Demo/Client/Extensions/IServiceCollectionExtensions.cs",
        "Demo/Server/Program.cs",
        "Demo/Server/Components/App.razor",
        "Demo/Client/Data/DemoDbService.cs",
    ];

    private sealed record Mode(string Title, string Summary, string[] GuideSections, string? Files, bool IncludeDemo, bool Browser);

    /// <summary>The wiring for one hosting model as Markdown, or null when the model is not one of <see cref="HostingModels"/>.</summary>
    public static string? Get(string? hostingModel) => Describe(hostingModel) is { } mode ? Compose(mode) : null;

    /// <summary>What sets one hosting model apart, in a paragraph - the line the search index ranks a setup guide by.</summary>
    public static string Summary(string hostingModel) => Describe(hostingModel) is { } mode ? $"{mode.Title}. {mode.Summary.ReplaceLineEndings(" ")}" : string.Empty;

    private static Mode? Describe(string? hostingModel)
    {
        var mode = (hostingModel ?? string.Empty).Trim().ToLowerInvariant().Replace(" ", "-", StringComparison.Ordinal);

        mode = mode switch
        {
            "webassembly" or "interactivewebassembly" or "blazor-wasm" or "web-app" or "blazor-web-app" => "wasm",
            "standalone" or "standalone-webassembly" or "standalone-wasm-app" => "standalone-wasm",
            "interactiveauto" => "auto",
            "interactiveserver" or "blazor-server" or "aspnetcore" or "asp.net-core" => "server",
            "maui" or "blazor-hybrid" or "wpf" or "winforms" or "maui-hybrid" => "hybrid",
            "console" or "tests" or "test" or "tool" => "desktop",
            _ => mode
        };

        return mode switch
        {
            "wasm" => new Mode(
                "Blazor Web App - InteractiveWebAssembly",
                """
                The database lives in the browser tab - in managed memory, persisted to OPFS - so it belongs in the .Client
                project, opened once by a singleton service and shared by every page. The server project serves the client
                and must not render the components that open it: OPFS, IndexedDB and Web Locks exist only in the browser.
                This demo turns prerendering off for the whole app, since every page opens the database; an app that wants
                prerendering opens the database in `OnAfterRenderAsync` (or behind `OperatingSystem.IsBrowser()`) instead.
                The files below are the demo's own.
                """,
                ["Blazor Web App", "Blazor WebAssembly"],
                null,
                IncludeDemo: true,
                Browser: true),

            "standalone-wasm" => new Mode(
                "standalone Blazor WebAssembly",
                """
                One project and no prerendering: the database is opened in the browser from the first render, once per tab,
                by a singleton service every page shares.
                """,
                ["Blazor WebAssembly"],
                StandaloneFiles,
                IncludeDemo: false,
                Browser: true),

            "auto" => new Mode(
                "Blazor Web App - InteractiveAuto",
                """
                The first visit renders on the server while the WebAssembly runtime downloads, later ones in the browser - but
                the database only ever lives in the browser. Put the code that uses it in the .Client project and open it only
                once the component runs in the browser (`OperatingSystem.IsBrowser()`, or `RendererInfo.Name == "WebAssembly"`
                on .NET 9 and later), rendering a placeholder during the server-rendered visit. Pages that need the
                database on every visit are simpler as `InteractiveWebAssembly` without prerendering, as in this demo.
                """,
                ["Blazor Web App", "Blazor WebAssembly"],
                null,
                IncludeDemo: true,
                Browser: true),

            "server" => new Mode(
                "Blazor Server and ASP.NET Core",
                """
                The engine runs in the server process and persists through `BlazeDbFileStorage`. It assumes one logical writer
                and no concurrent readers, and every circuit and request runs on its own thread: either serialize all access to
                a shared database (one lock around every read and write, as below), or give each user a database of their own
                in a directory of their own. A multi-user server store is a database server's job; BlazeDb is one user's local store.
                """,
                ["Blazor Server and ASP.NET Core", "Threading model", "Files on the desktop"],
                ServerFiles,
                IncludeDemo: false,
                Browser: false),

            "hybrid" => new Mode(
                "Blazor Hybrid (MAUI, WPF, Windows Forms)",
                """
                .NET runs in-process, so the browser backends do not apply even though the UI is a WebView: persist through
                `BlazeDbFileStorage` in the app's data directory, opened once for the app's lifetime. Components render on the
                UI thread, which serializes their access; marshal work from background threads back to it.
                """,
                ["Blazor Hybrid and desktop", "Files on the desktop"],
                HybridFiles,
                IncludeDemo: false,
                Browser: false),

            "desktop" => new Mode(
                "console apps, tools and tests",
                """
                The engine has no browser dependency: open it over `BlazeDbFileStorage` for a database on disk, or over
                `BlazeDbInMemoryStorage` (or no storage at all) for one that lives only as long as the process - which is
                what makes crash and corruption scenarios reproducible in a test.
                """,
                ["Files on the desktop", "Threading model"],
                DesktopFiles,
                IncludeDemo: false,
                Browser: false),

            _ => null
        };
    }

    private static string Compose(Mode mode)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"# Setting BlazeDb up - {mode.Title}").AppendLine();
        builder.AppendLine(mode.Summary).AppendLine();
        builder.AppendLine(mode.Browser ? BrowserChecklist : ServerChecklist).AppendLine();

        if (mode.Files is not null) builder.AppendLine(mode.Files).AppendLine();

        if (mode.IncludeDemo)
        {
            builder.AppendLine("## The files, from this demo").AppendLine();
            builder.AppendLine("Everything below compiles as-is; rename the projects and namespaces to your own. Inside this repository the ")
                   .AppendLine("client references the generator as an analyzer project reference - from NuGet it is the `Bit.BlazeDb.Generators` package.")
                   .AppendLine();

            foreach (var path in _demoFiles)
            {
                var content = BlazeDbSourceCatalog.GetSourceFile(path);
                if (content is null) continue;

                AppendFile(builder, path, content);
            }
        }

        // The guide's own words on the hosting model, so the caveats are the library's rather than a
        // paraphrase of them that can drift.
        builder.AppendLine("## From the guide").AppendLine();

        foreach (var heading in mode.GuideSections)
        {
            var section = BlazeDbSourceCatalog.GetGuideSection(heading);
            if (section is null) continue;

            builder.AppendLine(Demote(section)).AppendLine();
        }

        builder.AppendLine(Next);

        return builder.ToString();
    }

    private const string BrowserChecklist = """
        ## Checklist

        1. `dotnet add package Bit.BlazeDb`, `Bit.BlazeDb.Generators` and `Bit.BlazeDb.Browser` - plus
           `Bit.BlazeDb.EntityFrameworkCore` for `UseBlazeDb()`. In a Blazor Web App, add them to the .Client project.
           Without `Bit.BlazeDb.Generators` a `[BlazeDbTable]` type compiles but gets no `Table` and no `Indexes`.
        2. Declare each table as a top-level `partial` type marked `[BlazeDbTable("name")]` with one `[BlazeDbKey]`
           property, and check it with `InspectBlazeDbTable`.
        3. Open the database ONCE per tab, in a singleton service, and share it - not per component or per page. The
           storage (`BlazeDbOpfsStorage.CreateAsync`) takes the tab's writer lock; dispose it after the database.
        4. Never open a browser backend outside the browser: not during prerendering, not in an InteractiveServer or
           InteractiveAuto server render. `Bit.BlazeDb.Browser` is `[SupportedOSPlatform("browser")]`.
        5. Handle the second tab: `CreateAsync` throws `BlazeDbDatabaseLockedException` when another tab is the writer -
           open a read-only replica there (`CreateReadOnlyAsync` + `ReadOnly = true`), or fall back to in-memory storage.
        6. Where OPFS cannot be written (Firefox private windows, Safari before 26), use `BlazeDbIndexedDbStorage`;
           `BlazeDbIndexedDbStorage.IsOpfsAvailableAsync()` says which.
        7. Writes reach storage on the flush interval; `await db.FlushAsync()` where you need a durability point, and
           watch `db.LastBackgroundError` to tell the user when durability is lagging.
        """;

    private const string ServerChecklist = """
        ## Checklist

        1. `dotnet add package Bit.BlazeDb` and `Bit.BlazeDb.Generators` - plus `Bit.BlazeDb.EntityFrameworkCore` for
           `UseBlazeDb()`. `Bit.BlazeDb.Browser` does not apply here: its backends exist only on WebAssembly.
        2. Declare each table as a top-level `partial` type marked `[BlazeDbTable("name")]` with one `[BlazeDbKey]`
           property, and check it with `InspectBlazeDbTable`.
        3. Open each database once and keep it for the process's lifetime. `BlazeDbFileStorage` takes an exclusive lock on
           its directory for as long as it lives: dispose it after the database, and open a second process's view with
           `BlazeDbFileStorage.OpenReadOnly(directory)`.
        4. Reads take no lock. Never let a read overlap a write on another thread: serialize access yourself, or keep
           each database on one thread.
        5. Writes reach the files on the flush interval; `await db.FlushAsync()` where you need a durability point -
           and before the process exits, which `DisposeAsync` does.
        """;

    private const string StandaloneFiles = """
        ## The files

        ### `MyApp.csproj`

        ```xml
        <Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">
          <PropertyGroup>
            <TargetFramework>net10.0</TargetFramework>
            <Nullable>enable</Nullable>
            <ImplicitUsings>enable</ImplicitUsings>
          </PropertyGroup>
          <ItemGroup>
            <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" Version="10.0.*" />
            <PackageReference Include="Bit.BlazeDb" Version="*" />
            <PackageReference Include="Bit.BlazeDb.Generators" Version="*" PrivateAssets="all" />
            <PackageReference Include="Bit.BlazeDb.Browser" Version="*" />
          </ItemGroup>
        </Project>
        ```

        ### `Data/TodoItem.cs`

        ```csharp
        using Bit.BlazeDb;

        [BlazeDbTable("todos")]
        public partial class TodoItem
        {
            [BlazeDbKey, BlazeDbField(1)] public Guid Id { get; set; }
            [BlazeDbField(2)] public string Title { get; set; } = "";
            [BlazeDbIndex, BlazeDbField(3)] public bool Done { get; set; }
            [BlazeDbOrderedIndex, BlazeDbField(4)] public DateTime CreatedAt { get; set; }
        }
        ```

        ### `Data/AppDatabase.cs`

        ```csharp
        using Bit.BlazeDb;
        using Bit.BlazeDb.Browser;
        using Bit.BlazeDb.Storage;

        /// <summary>The tab's one database, opened on first use and shared by every page.</summary>
        public sealed class AppDatabase : IAsyncDisposable
        {
            private Task<BlazeDbDatabase>? _opening;
            private IDisposable? _storage;

            public Task<BlazeDbDatabase> GetAsync() => _opening ??= OpenAsync();

            private async Task<BlazeDbDatabase> OpenAsync()
            {
                IBlazeDbStorage storage;
                try
                {
                    var opfs = await BlazeDbOpfsStorage.CreateAsync("my-app");   // takes this tab's writer lock
                    _storage = opfs;
                    storage = opfs;
                }
                catch (BlazeDbDatabaseLockedException)
                {
                    storage = new BlazeDbInMemoryStorage();                      // another tab is the writer
                }

                return await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions { Storage = storage }
                    .AddTable(TodoItem.Table));
            }

            public async ValueTask DisposeAsync()
            {
                if (_opening is not null) await (await _opening).DisposeAsync();
                _storage?.Dispose();
            }
        }
        ```

        ### `Program.cs`

        ```csharp
        using Microsoft.AspNetCore.Components.Web;
        using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

        var builder = WebAssemblyHostBuilder.CreateDefault(args);

        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        builder.Services.AddSingleton<AppDatabase>();

        await builder.Build().RunAsync();
        ```

        ### `Pages/Todos.razor`

        ```razor
        @page "/todos"
        @inject AppDatabase Database

        @if (_open is null)
        {
            <p>Opening...</p>
        }
        else
        {
            <button @onclick="Add">Add</button>
            <ul>
                @foreach (var todo in _open)
                {
                    <li>@todo.Title</li>
                }
            </ul>
        }

        @code {
            private BlazeDbTable<Guid, TodoItem>? _todos;
            private IReadOnlyList<TodoItem>? _open;

            protected override async Task OnInitializedAsync()
            {
                _todos = (await Database.GetAsync()).GetTable(TodoItem.Table);
                Refresh();
            }

            private void Add()
            {
                _todos!.Insert(new TodoItem { Id = Guid.NewGuid(), Title = $"Todo {_todos.Count + 1}", CreatedAt = DateTime.UtcNow });
                Refresh();
            }

            private void Refresh() => _open = [.. _todos!.Lookup(TodoItem.Indexes.Done, false)];
        }
        ```

        There is no script tag to add to `wwwroot/index.html`: the browser backends load their ES modules from
        `_content/Bit.BlazeDb.Browser/` themselves, resolved against the document's `<base href>`.
        """;

    private const string ServerFiles = """
        ## The files

        ### `Data/AppDatabase.cs`

        ```csharp
        using Bit.BlazeDb;
        using Bit.BlazeDb.Storage;

        /// <summary>
        /// One database for the process, and one lock around every use of it: the engine's reads take no lock
        /// of their own, and requests and circuits run on many threads.
        /// </summary>
        public sealed class AppDatabase(IWebHostEnvironment environment) : IAsyncDisposable
        {
            private readonly SemaphoreSlim _gate = new(1, 1);
            private BlazeDbFileStorage? _storage;
            private BlazeDbDatabase? _db;

            public async Task<T> UseAsync<T>(Func<BlazeDbDatabase, T> work)
            {
                await _gate.WaitAsync();
                try
                {
                    _db ??= await OpenAsync();
                    return work(_db);
                }
                finally
                {
                    _gate.Release();
                }
            }

            private async Task<BlazeDbDatabase> OpenAsync()
            {
                _storage = new BlazeDbFileStorage(Path.Combine(environment.ContentRootPath, "App_Data", "db"));
                return await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions { Storage = _storage }.AddTable(TodoItem.Table));
            }

            public async ValueTask DisposeAsync()
            {
                if (_db is not null) await _db.DisposeAsync();   // flushes what is still pending
                _storage?.Dispose();                              // releases the directory lock
            }
        }
        ```

        ### `Program.cs`

        ```csharp
        builder.Services.AddSingleton<AppDatabase>();
        ```

        ### Using it

        ```csharp
        var open = await Database.UseAsync(db => db.GetTable(TodoItem.Table).Lookup(TodoItem.Indexes.Done, false).ToList());
        ```

        Copy what a page renders out of the lock (`ToList()` above): the rows are the live objects the table holds,
        and reading them after the lock is released races the next writer like any other read.
        """;

    private const string HybridFiles = """
        ## The files

        ### `MauiProgram.cs` (or the WPF / Windows Forms host)

        ```csharp
        builder.Services.AddMauiBlazorWebView();

        builder.Services.AddSingleton(_ => new BlazeDbFileStorage(Path.Combine(FileSystem.AppDataDirectory, "db")));
        builder.Services.AddSingleton<AppDatabase>();
        ```

        ### `Data/AppDatabase.cs`

        ```csharp
        using Bit.BlazeDb;
        using Bit.BlazeDb.Storage;

        public sealed class AppDatabase(BlazeDbFileStorage storage) : IAsyncDisposable
        {
            private Task<BlazeDbDatabase>? _opening;

            public Task<BlazeDbDatabase> GetAsync() => _opening ??=
                BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions { Storage = storage }.AddTable(TodoItem.Table)).AsTask();

            public async ValueTask DisposeAsync()
            {
                if (_opening is not null) await (await _opening).DisposeAsync();
            }
        }
        ```

        The container disposes the storage after the database service that depends on it, which releases the
        directory lock last. Components use the database from the UI thread they render on.
        """;

    private const string DesktopFiles = """
        ## The files

        ### `Program.cs`

        ```csharp
        using Bit.BlazeDb;
        using Bit.BlazeDb.Storage;

        using var storage = new BlazeDbFileStorage("data");   // exclusive lock on the directory while it lives
        await using var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions { Storage = storage }
            .AddTable(TodoItem.Table));

        var todos = db.GetTable(TodoItem.Table);
        todos.Upsert(new TodoItem { Id = Guid.NewGuid(), Title = "from the console", CreatedAt = DateTime.UtcNow });

        await db.FlushAsync();
        ```

        `await using` disposes the database before the `using` disposes the storage - declarations are disposed in
        reverse order - so the last flush happens while the directory is still locked.

        ### In a test

        ```csharp
        await using var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions
        {
            Storage = new BlazeDbInMemoryStorage(),   // or none: a database that lives only in memory
        }.AddTable(TodoItem.Table));
        ```
        """;

    private const string Next = """
        The database itself is plain objects - call `GetBlazeDbGuideSection(heading: "Quick start")` for tables, lookups,
        ranges and transactions, `GetBlazeDbGuideSection(heading: "EF Core")` for `UseBlazeDb()`, `InspectBlazeDbTable`
        to check a table type, or `GetBlazeDbSourceFile()` for the index of the demo's working pages.
        """;

    /// <summary>
    /// Pushes a quoted guide section two heading levels down, so it nests under "From the guide"
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
