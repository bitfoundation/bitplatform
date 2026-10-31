# Getting started

## Install

Reference `Bit.Bsqlite` - the ADO.NET surface, the EF Core integration, the worker transport, every browser storage backend (OPFS, IndexedDB, Cache Storage and in-memory), and the file system backend a Blazor Hybrid app runs on, in one package.

```
dotnet add package Bit.Bsqlite
```

The engine (`.wasm` and the worker) and the backends' VFSes are served as static web assets under `_content/Bit.Bsqlite/`, each imported on demand: a session downloads one engine build and the VFS of the backend it selected, never the others. Nothing needs to be added to the host page.

A Blazor Hybrid app also brings the native SQLite engine it opens its files with, as an app on `Microsoft.EntityFrameworkCore.Sqlite.Core` does:

```
dotnet add package SQLitePCLRaw.bundle_e_sqlite3
```

- or keeps the SQLitePCLRaw bundle it already uses, such as `bundle_e_sqlcipher`. Added to a Razor class library shared with a WebAssembly client, the bundle stays out of the WebAssembly build.

Bit.Bsqlite targets .NET 10 and 11, each with the EF Core of the same major version.

## Register

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
        .AllowNonPersistentFallback())
    .AddDatabase("app.db"));

builder.Services.AddBitBsqliteDbContext<AppDbContext>(options =>
{
    options.Schema = BsqliteSchemaStrategy.Migrate;          // the default
    options.OnFirstUse = (db, ct) => DemoData.SeedIfEmptyAsync(db, ct);
});
```

Register them in the project that runs on WebAssembly. In a Blazor Hybrid app - MAUI, WPF, Windows Forms - the same registration works unchanged: .NET runs natively there, so Bsqlite adds the `file-system` backend, ranks it ahead of the browser ones, and keeps the database as a SQLite file in the app's own folder, opened in-process by the native SQLite the app brings (see [Install](#install) and [Storage](storage.md#file-system-blazor-hybrid)). A Razor class library shared by a WebAssembly client and a Hybrid app registers Bsqlite once for both.

In a Blazor Web App whose pages are prerendered, register them in the server project as well: the prerender pass instantiates the pages there, so what they inject has to resolve, and nothing touches the browser until a page opens the database - which it should do only once the page runs in the browser: `RendererInfo.IsInteractive` on Interactive WebAssembly, and `OperatingSystem.IsBrowser()` on Interactive Auto, where a page on the server circuit is interactive too.

A WebAssembly app can also leave Bsqlite and EF Core out of its startup download, and then registers nothing here: see [Lazy loading on WebAssembly](#lazy-loading-on-webassembly).

`AddBitBsqliteServices` registers, all scoped except the selection and the migrator (one scope is the whole application on WebAssembly, and a circuit or a request on a server):

| Service | What it is |
|---|---|
| `IBsqliteSessionFactory` | Resolves a backend, starts a worker (or, in Blazor Hybrid, opens the file), opens the database |
| `BsqliteDatabase` | The one shared session for the database named in `AddDatabase` - also a keyed service under that name, for an application that registers several |
| `BsqliteStorageProviderResolver`, `BsqliteStorageMigrator`, `IBsqliteStorageBindingStore` | Selection, migration, and the sticky binding (in `localStorage` by default; in a file next to the databases in Blazor Hybrid) |
| `IBsqliteTransportFactory` | The worker transport, or the native one for a backend that keeps files on the device |
| `BsqliteFileSystemStorageProvider` | Blazor Hybrid only: the `file-system` backend |

Builder options:

- `EnableForeignKeys(false)` - skip `PRAGMA foreign_keys = ON` on open. On by default, as with Microsoft.Data.Sqlite.
- `OnOpen("PRAGMA cache_size = -16000")` - statements to run after every open.
- `BeforeFirstOpen((sp, ct) => ...)` - work to finish before the first database opens, once per scope. An app that keeps
  `System.Private.Xml.wasm` out of its startup download with `<BlazorWebAssemblyLazyLoad>` loads it here; opening a
  database needs it:

  ```csharp
  .BeforeFirstOpen((sp, ct) => sp.GetRequiredService<LazyAssemblyLoader>()
      .LoadAssembliesAsync(["System.Private.Xml.wasm"]))
  ```

  An app that [lazy loads Bit.Bsqlite](#lazy-loading-on-webassembly) itself loads `System.Private.Xml.wasm` together
  with it instead.
- `AddExtensionModule("/my-functions.js")` - JavaScript that registers SQL functions; see [Extending](extending.md).
- `UseBindingStore<T>()` - replace where sticky bindings are remembered.
- `ConfigureFileSystemStorage(o => o.RootDirectory = ...)` - where a Blazor Hybrid app keeps its databases; by default a folder of the app's own under local application data.
- `UseWorker(hostModuleUrl, workerUrl)` - host the worker from a different path.

## Use EF Core

```csharp
@inject IBsqliteDbContextFactory<AppDbContext> Contexts

await using var db = await Contexts.CreateDbContextAsync();
db.Products.Add(new Product { Name = "Widget", Price = 19.99m });
await db.SaveChangesAsync();
```

The factory opens the database on first use, runs the schema step once per session, and hands out contexts bound to the shared connection. Disposing a context does not close the database. The factory also implements `IDbContextFactory<TContext>`; its synchronous `CreateDbContext()` works once an awaited `CreateDbContextAsync()` has opened the session and run the schema step on it.

The schema step and `OnFirstUse` run under a lock shared with every other tab, and so does EF's own `MigrateAsync`: each tab has its own worker over the same file, and two tabs opening after a deploy would otherwise both apply the new migration - or both seed an empty database. The lock is a Web Lock, so a tab that closes mid-migration releases it.

With more than one database registered, name the one a context belongs to: `AddBitBsqliteDbContext<CacheDbContext>(o => o.DatabaseName = "cache.db")`. Leaving it out is an error then, rather than a context bound to whichever was registered last.

Use the async EF API throughout: `ToListAsync`, `SaveChangesAsync`, `MigrateAsync`. Synchronous members throw `BsqliteSynchronousApiNotSupportedException` naming the async overload, because the engine runs in a worker the main thread cannot wait on. Blazor Hybrid keeps the same rule, so code written once runs in both - and its UI thread never waits on the disk either.

### Concurrent contexts

Every context of a session shares its one connection - one SQLite connection in the worker - and every statement is a round trip, so two contexts' operations interleave: a page saving while a sync service pulls, say. They take turns the way two SQLite connections over one file do:

- A transaction - `BeginTransactionAsync`, or the one EF wraps around a `SaveChangesAsync` that writes more than one row - keeps every other context's statements and transactions waiting until it commits or rolls back. They neither join it nor roll back with it.
- The wait lasts at most the command's `CommandTimeout` (`Database.SetCommandTimeout`), and `BsqliteConnection.DefaultTimeout` for a `BeginTransactionAsync` - 30 seconds each by default, as with Microsoft.Data.Sqlite's busy timeout - and then fails with `BsqliteConcurrencyException` ("database is locked").
- So code holding a transaction must not wait on another context: that context waits on the transaction, and the wait ends only with the timeout. Finish the transaction first, or use the context that holds it.

ADO.NET commands on the connection itself keep ADO.NET's rule: one that leaves `Transaction` unset runs inside the connection's open transaction.

### Without the factory

```csharp
var session = await sessionFactory.OpenAsync("app.db");
var options = new DbContextOptionsBuilder<AppDbContext>().UseBitBsqlite(session).Options;
await using var db = new AppDbContext(options);
```

`UseBitBsqlite` is `UseSqlite(connection)` plus replacements for the two EF services that would otherwise open the connection synchronously. SQL generation and type mapping are the stock provider's.

## Use ADO.NET

```csharp
@inject BsqliteDatabase Database

var session = await Database.OpenAsync();
await using var command = session.Connection.CreateCommand();
command.CommandText = "SELECT name FROM product WHERE price > @min";
command.Parameters.AddWithValue("@min", 10m);

await using var reader = await command.ExecuteReaderAsync();
while (await reader.ReadAsync())
{
    Console.WriteLine(reader.GetString(0));
}
```

What the connection offers beyond the basics:

- **Batches.** `connection.CreateBatch()` runs several commands in one worker round trip; `ExecuteReaderAsync` on a batch walks every result set with `NextResultAsync`.
- **Transactions.** `BeginTransactionAsync()` issues `BEGIN IMMEDIATE` (as Microsoft.Data.Sqlite does); any isolation level other than `Serializable` begins deferred. Disposing without committing rolls back. Savepoints: `SaveAsync`, `ReleaseAsync`, `RollbackAsync(name)`.
- **Counters.** `command.LastInsertRowId` and `connection.LastInsertRowId` after any execution.
- **Export.** `connection.ExportAsync()` returns a consistent SQLite file image of the open database.
- **Typed errors.** `BsqliteConstraintException`, `BsqliteConcurrencyException`, `BsqliteQuotaExceededException`, `BsqliteCorruptDatabaseException`, all deriving from `BsqliteException` with `SqliteErrorCode`.

## The shared database

`BsqliteDatabase` owns the session for the lifetime of its scope - the whole application on WebAssembly:

```csharp
await Database.OpenAsync();                // idempotent
Database.Resolution?.Provider.Name;         // "opfs", "indexeddb", ...
await Database.CloseAsync();               // raises Closing first, then terminates the worker
await Database.WhileClosedAsync(ct => provider.Admin.ImportAsync("app.db", bytes, ct).AsTask());
```

Subscribe to `Closing` to drop contexts and live queries before the worker goes away, and to `Opened` to reload afterwards. The demo's `SessionLifecycle` component is one way to wire that into pages.

`WhileClosedAsync` keeps the database closed until its work is done: an `OpenAsync` from anywhere else - a page loading, an `Opened` handler still running - waits and gets the reopened session, so a binding the work changes is the one the database opens on. The work itself must not open the database; it gets an `InvalidOperationException` if it tries.

## Lazy loading on WebAssembly

EF Core and Bsqlite are most of what a WebAssembly app downloads for its database: about 2.1 MB of the 4.9 MB of assemblies a small app on the demo's model downloads, compressed with Brotli. An app whose database only some pages use can download them when one of those pages first opens. `Program.cs` then registers nothing of Bsqlite's - it runs before the assemblies are there - and the pages build the same services with `GetBitBsqliteServices`, the first time one of them asks:

```csharp
// AppDatabase.cs: internal, and called only from the pages the assemblies are loaded for.
using Bit.Bsqlite.EntityFrameworkCore;
using Bit.Bsqlite.Storage.IndexedDb;
using Bit.Bsqlite.Storage.Opfs;

internal static class AppDatabase
{
    // What Program.cs would have registered, unchanged - and what a Blazor Hybrid app that shares the pages registers.
    public static void Register(IServiceCollection services)
    {
        services.AddBitBsqliteServices(sqlite => sqlite
            .AddStorageProvider<BsqliteOpfsStorageProvider>()
            .AddStorageProvider<BsqliteIndexedDbStorageProvider>()
            .UseStorage(s => s
                .Prefer(BsqliteOpfsStorageProvider.ProviderName)
                .Fallback(BsqliteIndexedDbStorageProvider.ProviderName))
            .AddDatabase("app.db"));

        services.AddBitBsqliteDbContext<AppDbContext>();
    }

    public static IBsqliteDbContextFactory<AppDbContext> Contexts(IServiceProvider provider) => provider
        .GetBitBsqliteServices(Register)
        .GetRequiredService<IBsqliteDbContextFactory<AppDbContext>>();
}
```

```razor
@* Pages/Products.razor *@
@page "/products"
@inject IServiceProvider Services

@code {
    private List<Product> _products = [];

    protected override async Task OnInitializedAsync()
    {
        // A prerendered page runs this on the server first, where there is no database to open: wait for the browser.
        if (!RendererInfo.IsInteractive)
            return;

        await using var db = await AppDatabase.Contexts(Services).CreateDbContextAsync();
        _products = await db.Products.OrderBy(p => p.Name).ToListAsync();
    }
}
```

`GetBitBsqliteServices` runs the method it is given - its `configure`, `Register` above - once, the first time a page asks, into a container of Bsqlite's own, so every page that asks gets the same services and the same open database. Pass it the same method every time: it builds the services from the first call's, so a call with another method throws rather than get services that lack what that one registers. Keep every database, every context and every service of yours that takes Bsqlite's types in that one method. What it registers still reaches the app's services: a `BeforeFirstOpen` step, `options.Configure`, a context's constructor and a service of your own registered in `configure` get from the app's container whatever Bsqlite's lacks, and so does code that resolves from the provider it returns, or from a scope created through it. So a service of yours that takes Bsqlite's types - the demo's `BrowserDatabase`, say - is registered in `configure`, next to them, and resolved from that provider. The app's services come from a scope of the app's own, created with the container and kept as long as it is: its singletons are the ones the components get, while a service it registers as scoped is an instance of that scope's, so register as a singleton one whose instance the components have to share. Logging and options stay the app's: `configure` calls neither `AddLogging` nor `AddOptions`, nor what calls them - `AddMemoryCache`, `AddHttpClient` - since their second set would answer for the app's; `GetBitBsqliteServices` throws, naming them, if it does.

Only WebAssembly builds the services this way: everywhere else Bit.Bsqlite is loaded with the app, which registers them as usual. Where the app did call `AddBitBsqliteServices` - a Blazor Hybrid app that shares the pages, or a server that prerenders them - `GetBitBsqliteServices` returns the app's own container instead, once it has checked that what `configure` registers is there too: such an app registers with the same method, `AppDatabase.Register(builder.Services)`. Anywhere else it throws, which is why the page above asks only once `RendererInfo.IsInteractive` is true.

Mark the assemblies as lazy loaded in the project that runs in the browser, and set a runtime switch next to the marks so the router knows they are there to load:

```xml
<!-- AOT builds are left out: see below. -->
<ItemGroup Condition=" '$(RunAOTCompilation)' != 'true' ">
  <BlazorWebAssemblyLazyLoad Include="Bit.Bsqlite.wasm" />
  <BlazorWebAssemblyLazyLoad Include="Microsoft.EntityFrameworkCore.wasm" />
  <BlazorWebAssemblyLazyLoad Include="Microsoft.EntityFrameworkCore.Relational.wasm" />
  <BlazorWebAssemblyLazyLoad Include="Microsoft.EntityFrameworkCore.Sqlite.wasm" />
  <BlazorWebAssemblyLazyLoad Include="Microsoft.Data.Sqlite.wasm" />
  <BlazorWebAssemblyLazyLoad Include="SQLitePCLRaw.core.wasm" />
  <BlazorWebAssemblyLazyLoad Include="Microsoft.Extensions.Caching.Abstractions.wasm" />
  <BlazorWebAssemblyLazyLoad Include="Microsoft.Extensions.Caching.Memory.wasm" />
  <BlazorWebAssemblyLazyLoad Include="Microsoft.Extensions.DependencyModel.wasm" />
  <BlazorWebAssemblyLazyLoad Include="System.Data.Common.wasm" />
  <BlazorWebAssemblyLazyLoad Include="System.Private.Xml.wasm" />
  <BlazorWebAssemblyLazyLoad Include="System.Transactions.Local.wasm" />
  <!-- The library with your context, its configurations and its migrations: see below. -->
  <BlazorWebAssemblyLazyLoad Include="MyApp.Data.wasm" />
  <RuntimeHostConfigurationOption Include="MyApp.LazyLoadsBsqlite" Value="true" Trim="true" />
</ItemGroup>
```

Then load them before the first page that uses the database renders, from the router's `OnNavigateAsync`, naming every assembly the project file marks:

```razor
@* App.razor - in a Blazor Web App, the client project's Routes.razor: the router waits for OnNavigateAsync
   before it renders the page. typeof(Program) is the project the router's file is in, in either kind of app. *@
@using Microsoft.AspNetCore.Components.WebAssembly.Services
@inject LazyAssemblyLoader LazyAssemblyLoader

<Router AppAssembly="typeof(Program).Assembly" OnNavigateAsync="OnNavigateAsync">
    ...
</Router>

@code {
    // Set only where the assemblies are marked: loading an assembly that is not marked throws.
    private static readonly bool LazyLoadsBsqlite = AppContext.TryGetSwitch("MyApp.LazyLoadsBsqlite", out var on) && on;

    private static readonly string[] DatabaseAssemblies =
    [
        "Bit.Bsqlite.wasm", "Microsoft.EntityFrameworkCore.wasm", "Microsoft.EntityFrameworkCore.Relational.wasm",
        "Microsoft.EntityFrameworkCore.Sqlite.wasm", "Microsoft.Data.Sqlite.wasm", "SQLitePCLRaw.core.wasm",
        "Microsoft.Extensions.Caching.Abstractions.wasm", "Microsoft.Extensions.Caching.Memory.wasm",
        "Microsoft.Extensions.DependencyModel.wasm", "System.Data.Common.wasm", "System.Private.Xml.wasm",
        "System.Transactions.Local.wasm", "MyApp.Data.wasm",
    ];

    private async Task OnNavigateAsync(NavigationContext context)
    {
        // The pages that use the database: fetch the assemblies before they render. A second call loads nothing again.
        if (LazyLoadsBsqlite && UsesTheDatabase(context.Path))
            await LazyAssemblyLoader.LoadAssembliesAsync(DatabaseAssemblies);
    }

    // /products and the routes beneath it, such as /Products/42 (routes match regardless of case), but not
    // /products-archive. Add every other route whose page uses the database. The path keeps any query and fragment.
    private static bool UsesTheDatabase(string path)
    {
        var page = path.Split('?', '#')[0].Split('/')[0];
        return page.Equals("products", StringComparison.OrdinalIgnoreCase);
    }
}
```

The rest of the app has to keep to these rules:

- Only code that runs after the assemblies have loaded may use Bsqlite's or EF Core's types: the pages they are loaded for, a layout only those pages use, and `AppDatabase` - not `Program.cs`, `App.razor`, `Routes.razor` or a layout every page shares. So `Program.cs` calls none of `AddBitBsqliteServices`, `AddBitBsqliteDbContext`, `AddDbContext` and `AddDbContextFactory`, and registers no service of yours that takes their types - that goes in `configure`. The method that calls `LoadAssembliesAsync` cannot use them either: WebAssembly resolves what a method refers to when the method first runs, which is before the load, and does not retry what it failed to resolve. A page can hold them in fields - a context factory, an open context, a live query - since the router loads the assemblies before it creates the page.
- No public type in an assembly the router reads - the app's own assembly first of all, the client project's in a Blazor Web App - may derive from or implement one of EF Core's or Bsqlite's: no public `DbContext`, `Migration` or `IEntityTypeConfiguration<T>` of yours, and no public `IBsqliteStorageProvider` or `IBsqliteStorageBindingStore`. The router reads the public types of those assemblies when it first builds its route table, which is after the first navigation's `OnNavigateAsync`, and on WebAssembly such a type throws a `ReflectionTypeLoadException` there - `Could not load file or assembly 'Microsoft.EntityFrameworkCore'`. So it goes unnoticed when the app is opened at a page that uses the database, and fails the app on its first page when it is opened anywhere else: try the app from a page that does not. `dotnet ef` scaffolds migrations as public classes, so keep the context, its configurations and its migrations in a library of their own, as the demo's Data project does, and lazy load it with the rest (`MyApp.Data.wasm` above) - or make every one of them `internal`. The entities can stay where they are: they derive from nothing of EF Core's.
- `Microsoft.EntityFrameworkCore.Abstractions.wasm` stays out of the list. It holds the attributes an entity can carry, such as `[Index]` and `[Precision]`, which code that runs before the load may read - serializing an entity does - and it is 6 KB.
- The list is what only EF Core and Bsqlite use. Leave out an assembly your app also uses before the database pages open - `System.Data.Common.wasm` for a `DataTable`, `System.Private.Xml.wasm` for XML, `Microsoft.Extensions.Caching.Memory.wasm` for an `IMemoryCache` of its own: otherwise a page that does not use the database fails with `Could not load file or assembly` naming it. With `System.Private.Xml.wasm` loaded together with the others, a `BeforeFirstOpen` step that loads it has nothing left to do.
- In a Blazor Web App only the browser lazy loads. Give `Routes` the `InteractiveWebAssembly` render mode in `App.razor`, as the demo does, so the router - and its `OnNavigateAsync` - runs in the browser. The server project loads Bsqlite with the rest of the app, and registers `LazyAssemblyLoader`, which `Routes.razor` injects while it is prerendered there: `builder.Services.AddScoped<LazyAssemblyLoader>()`. The pages are prerendered there too, where `GetBitBsqliteServices` builds nothing: a page asks for the database only once `RendererInfo.IsInteractive` is true, as `Products.razor` above does, and so only in the browser. A server project that registers the services itself - with `AppDatabase.Register(builder.Services)` - may let its pages ask while prerendering too, and they get its own.
- With per-page render modes - the setup for Interactive Auto - the router renders statically on the server and never runs `OnNavigateAsync` in the browser, so a page that uses the database fails as it becomes interactive, with `Could not load file or assembly` or `Could not load type of field`. Such a page loads the assemblies itself. It names none of Bsqlite's or EF Core's types, loads them in `OnInitializedAsync`, and only then renders a component of yours, without a route, that uses the database:

  ```razor
  @* Pages/Products.razor, in the client project. ProductList is a component of yours that uses AppDatabase. *@
  @page "/products"
  @rendermode InteractiveWebAssembly
  @using Microsoft.AspNetCore.Components.WebAssembly.Services
  @inject LazyAssemblyLoader LazyAssemblyLoader

  @if (_loaded)
  {
      <ProductList />
  }

  @code {
      // The switch set next to the marks: unset in an AOT build, where the list renders at once.
      private static readonly bool LazyLoadsBsqlite = AppContext.TryGetSwitch("MyApp.LazyLoadsBsqlite", out var on) && on;

      private bool _loaded;

      protected override async Task OnInitializedAsync()
      {
          // Prerendered on the server, the page renders without the list: the database is in the browser.
          if (!RendererInfo.IsInteractive)
              return;

          if (LazyLoadsBsqlite)
              await LazyAssemblyLoader.LoadAssembliesAsync(["Bit.Bsqlite.wasm", /* every assembly the project marks */]);

          _loaded = true;
      }
  }
  ```
- A Blazor Hybrid app downloads nothing, so there is nothing to lazy load: it registers the services as usual, with the same method - `AppDatabase.Register(builder.Services)` in `MauiProgram.cs` - and `GetBitBsqliteServices` hands the pages it shares with the WebAssembly client its container. It builds none of its own there: a web view closes long before the app does, and would leave its databases open. If it shares the router's file, it registers `LazyAssemblyLoader` (`builder.Services.AddScoped<LazyAssemblyLoader>()`), because the router injects it there too, though the switch leaves it unused.
- The condition leaves AOT builds out. AOT compiles every assembly into `dotnet.native.wasm`, so there is nothing left to download later, and lazy loading an AOT-compiled assembly can fail at startup. The switch is set only where the marks are, so an AOT build skips `LoadAssembliesAsync` as well.
- `Could not load file or assembly 'Bit.Bsqlite'` - or `'Microsoft.EntityFrameworkCore'`, or `Could not load type of field` - when a database page opens means something used their types before the assemblies loaded: load them in `OnNavigateAsync` for every route whose page uses the database - from the page itself, for a page with its own render mode - and keep their types out of `Program.cs`, `App.razor`, the layouts every page shares and the method that loads them. If it comes in a `ReflectionTypeLoadException` from the router on the first page - and only when the app is opened away from the database pages - a public type in an assembly the router reads derives from or implements one of theirs. The same error naming another assembly of the list, on a page that does not use the database, is one your app uses itself: take it out of the list. `... must be marked with 'BlazorWebAssemblyLazyLoad' item group` names an assembly the call loads that the project file does not mark.
