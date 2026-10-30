# Moving a bit Boilerplate project from Bit.Besql to Bit.Bsqlite

The bit Boilerplate template keeps its offline database - `AppOfflineDbContext`, which the offline Todo sample syncs
with the server through CommunityToolkit.Datasync - on Bit.Bsqlite. A project created before that, with the offline
database turned on (`--offlineDb`, the default), runs it on Bit.Besql, which is no longer developed. This guide makes
the same change in such a project, file by file, and shows how to keep what your users have stored on their devices.

It is [Moving from Bit.Besql](migrating-from-besql.md) applied to one project, so it names files rather than explaining
APIs. Read that guide for what each Bit.Besql API maps to and why.

In the paths and code below, `Boilerplate` stands for your project's name - `MyApp.Client.Core` where this guide says
`Boilerplate.Client.Core` - and `AC87AA5B-4B37-4E52-8468-2D5DF24AF256` for the GUID your project got when it was
created. Your current Bit.Besql registration shows it.

## What changes

| Where the app runs | With Bit.Besql | With Bit.Bsqlite |
|---|---|---|
| Blazor WebAssembly | SQLite on the main thread, the whole file copied into Cache Storage (`bit-Besql`) after writes | SQLite in a dedicated worker, `AppOffline.db` on OPFS, falling back to IndexedDB, then to memory |
| Blazor Hybrid - Android, iOS, Windows, macOS | `App_Data/AppOffline.db` under `{local application data}/AC87AA5B-...` | A SQLite file in the app's own folder, `{local application data}/{entry assembly name}/bit-bsqlite/AppOffline.db`, opened by the same native SQLite |
| Blazor Server, pre-rendering, `dotnet ef` | `App_Data/AppOffline.db` on the server - `/container_volume/App_Data/AppOffline.db` in Docker | Unchanged: the same file, through stock EF Core and a factory of the project's own |

Bsqlite does not serve the server: it keeps each user's database on that user's device, and never one file on the
server for every user. So where Bit.Besql was one registration, there are two - Bsqlite in the browser and in Blazor
Hybrid, plain EF Core SQLite on the server.

What stays as it is: `AppOfflineDbContext`'s model, its migrations, the compiled model you generate before publishing,
`SyncService`, the Todo pages and every Datasync call. They already use EF Core's async API, the only one Bsqlite serves.

## Your users' data

Decide this before you start:

- **Start fresh.** This is what the template does. The browser and the device open a new, empty database, and the Todo
  items come back from the server on the next pull. Changes a user made offline and has not pushed yet are lost.
  Nothing on the server changes.
- **Keep it.** A few more lines in the registration, in [Keeping your users' data](#keeping-your-users-data). The
  database Bit.Besql left in the browser moves to OPFS the first time the new version opens it, and the file it left on
  the device is opened where it is.

Either way, follow the steps below; the second choice changes step 5 only.

## Step by step

### 1. Swap the package

In `src/Directory.Packages.props`, replace Bit.Besql's version with Bit.Bsqlite's, at the version of the project's
other `Bit.*` packages:

```xml
<!-- Before -->
<PackageVersion Include="Bit.Besql" Version="..." />

<!-- After -->
<PackageVersion Include="Bit.Bsqlite" Version="..." />
```

In `src/Client/Boilerplate.Client.Core/Boilerplate.Client.Core.csproj`, do the same with the reference:

```xml
<PackageReference Include="Bit.Bsqlite" />
```

Keep `Microsoft.EntityFrameworkCore.Sqlite`. The server's registration in step 5 uses it, and it brings the native
SQLite the Android, iOS, Windows and macOS apps open their file with.

### 2. Clean the host pages and the WebAssembly project

Remove Bit.Besql's script from `src/Client/Boilerplate.Client.Web/wwwroot/index.html`:

```html
<script src="_content/Bit.Besql/bit-besql.js"></script>
```

and from `src/Server/Boilerplate.Server.Web/Components/App.razor`:

```razor
<Script Src="_content/Bit.Besql/bit-besql.js"></Script>
```

Bsqlite needs nothing in the host page: it loads what it needs when the database first opens.

In `src/Client/Boilerplate.Client.Web/Boilerplate.Client.Web.csproj`, remove:

```xml
<WasmBuildNative>true</WasmBuildNative>
```

Bit.Besql needed it to link SQLite into the WebAssembly runtime. Bsqlite runs its own SQLite build in a worker instead,
and, with the line gone, keeps the native SQLite that `Microsoft.EntityFrameworkCore.Sqlite` brings for the device apps
out of the WebAssembly build. Left in, that native SQLite is linked into `dotnet.native.wasm` for nothing.

Keep this line in the same file:

```xml
<BlazorWebAssemblyLazyLoad Include="System.Private.Xml.wasm" />
```

Bit.Besql's factory loaded that assembly before handing out the first context. The registration in step 5 loads it
before the database first opens.

### 3. Move the initialization into `AppOfflineDbContext`

Bit.Besql ran your `dbContextInitializer` before the first context: the check that a compiled model was generated,
then the migrations. Both of step 5's registrations need that code, so it moves into
`src/Client/Boilerplate.Client.Core/Infrastructure/Data/AppOfflineDbContext.cs`:

```csharp
using Microsoft.EntityFrameworkCore.Metadata;

public partial class AppOfflineDbContext(DbContextOptions<AppOfflineDbContext> options) : OfflineDbContext(options)
{
    /// <summary>
    /// Brings the database up to date with the model. Runs once, before the first context is handed out.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (AppEnvironment.IsDevelopment() is false && Model.GetType() == typeof(RuntimeModel))
            throw new InvalidOperationException("AppOfflineDbContext has not been optimized. ..."); // Keep the message your check has now.

        await Database.MigrateAsync(cancellationToken);
    }

    // ... the rest of the class, unchanged ...
}
```

The `Task.Run` around `MigrateAsync` is not needed any more: Bsqlite runs SQLite off the UI thread, in a worker in the
browser and on the thread pool on a device.

### 4. Add `AppOfflineDbContextFactory`

On the server, the database is opened by stock EF Core, which does not run code before the first context. Add a factory
that does, as `src/Client/Boilerplate.Client.Core/Infrastructure/Data/AppOfflineDbContextFactory.cs`:

```csharp
namespace Boilerplate.Client.Core.Infrastructure.Data;

/// <summary>
/// Creates <see cref="AppOfflineDbContext"/> where the offline database is a plain SQLite file on the server - Blazor Server,
/// pre-rendering and the design-time tools - bringing the database up to date before handing out the first context.
/// In the browser and in Blazor Hybrid, Bsqlite's factory does the same (See IClientCoreServiceCollectionExtensions).
/// </summary>
public partial class AppOfflineDbContextFactory(DbContextOptions<AppOfflineDbContext> options) : IDbContextFactory<AppOfflineDbContext>
{
    private readonly SemaphoreSlim initializationLock = new(1, 1);
    private bool isInitialized;

    public AppOfflineDbContext CreateDbContext() => new(options);

    public async Task<AppOfflineDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        if (isInitialized is false)
        {
            await initializationLock.WaitAsync(cancellationToken);
            try
            {
                if (isInitialized is false)
                {
                    await using var dbContext = new AppOfflineDbContext(options);
                    await dbContext.InitializeAsync(cancellationToken);
                    isInitialized = true;
                }
            }
            finally
            {
                initializationLock.Release();
            }
        }

        return new(options);
    }
}
```

### 5. Register

In `src/Client/Boilerplate.Client.Core/Infrastructure/Extensions/IClientCoreServiceCollectionExtensions.cs`, add these
at the top of the file:

```csharp
using Bit.Bsqlite.Storage.Opfs;
using Bit.Bsqlite.Storage.InMemory;
using Bit.Bsqlite.Storage.IndexedDb;
using Bit.Bsqlite.EntityFrameworkCore;
```

Then replace the whole `services.AddBesqlDbContextFactory<AppOfflineDbContext>(...)` call - from that line to its
`lifetime: ServiceLifetime.Scoped);` - with:

```csharp
if (AppPlatform.IsBlazorHybridOrBrowser)
{
    // The offline database lives on the user's device: in the browser's storage, and in Blazor Hybrid in a SQLite file
    // of the app's own.
    services.AddBitBsqliteServices(sqlite => sqlite
        .AddStorageProvider<BsqliteOpfsStorageProvider>()
        .AddStorageProvider<BsqliteIndexedDbStorageProvider>()
        .AddStorageProvider<BsqliteInMemoryStorageProvider>()
        .UseStorage(storage => storage
            .Prefer(BsqliteOpfsStorageProvider.ProviderName)
            .Fallback(BsqliteIndexedDbStorageProvider.ProviderName)
            .Fallback(BsqliteInMemoryStorageProvider.ProviderName)
            .AllowNonPersistentFallback())
        .BeforeFirstOpen(async (sp, _) =>
        {
            // Opening a database needs System.Private.Xml.wasm, which Boilerplate.Client.Web.csproj lazy loads.
            if (AppPlatform.IsBrowser)
            {
                await sp.GetRequiredService<LazyAssemblyLoader>().LoadAssembliesAsync(["System.Private.Xml.wasm"]);
            }
        })
        .AddDatabase("AppOffline.db"));

    services.AddBitBsqliteDbContext<AppOfflineDbContext>(options =>
    {
        options.Schema = BsqliteSchemaStrategy.None; // InitializeAsync migrates, once it has checked the compiled model.
        options.Configure = (_, optionsBuilder) => optionsBuilder
            .EnableSensitiveDataLogging(AppEnvironment.IsDevelopment())
            .EnableDetailedErrors(AppEnvironment.IsDevelopment());
        options.OnFirstUse = (dbContext, cancellationToken) => dbContext.InitializeAsync(cancellationToken);
    });
}
else
{
    // Blazor Server, pre-rendering and the design-time tools (dotnet ef) run on the server, where Bsqlite does not:
    // it keeps a user's database on their device. Here the offline database is a plain SQLite file on the server instead.
    services.AddDbContextFactory<AppOfflineDbContext, AppOfflineDbContextFactory>(optionsBuilder =>
    {
        var isRunningInsideDocker = Directory.Exists("/container_volume"); // It's supposed to be a mounted volume named /container_volume
        var dirPath = Path.Combine(isRunningInsideDocker ? "/container_volume" : Directory.GetCurrentDirectory(), "App_Data");

        Directory.CreateDirectory(dirPath);

        optionsBuilder.UseSqlite($"Data Source={Path.Combine(dirPath, "AppOffline.db")}");

        optionsBuilder.EnableSensitiveDataLogging(AppEnvironment.IsDevelopment())
                .EnableDetailedErrors(AppEnvironment.IsDevelopment());
    }, ServiceLifetime.Scoped);
}
```

Leave `services.AddScoped<SyncService>();` after it, as it is.

What each part does:

- **`AppPlatform.IsBlazorHybridOrBrowser`** picks where the database lives. The server's branch also serves `dotnet ef`,
  whose `migrations add` and `dbcontext optimize` build the context from `Boilerplate.Server.Web`'s services. So the
  commands in `Infrastructure/Data/README.md` work as they did.
- **`Schema = None` and `OnFirstUse`.** Bsqlite migrates on its own by default. Here `InitializeAsync` does it, after its
  check. It runs once per session, which is once for the whole app on WebAssembly.
- **`BeforeFirstOpen`** lazy loads `System.Private.Xml.wasm` through the `LazyAssemblyLoader` that the same file already
  registers.
- **The in-memory backend, last in line,** serves a browser that offers no persistent storage, rather than the open
  failing. That database is gone on reload, and the next pull fills it again.

Both branches register `IDbContextFactory<AppOfflineDbContext>`, so `SyncService`, the Todo pages and the diagnostic
modal keep injecting it unchanged.

To keep your users' data, make the changes in [Keeping your users' data](#keeping-your-users-data) to the first branch.

### 6. Update the diagnostic modal

`src/Client/Boilerplate.Client.Core/Components/Layout/Diagnostic/AppDiagnosticModal.razor.Utils.cs` recreates the
offline database when the user clears the app's storage. Remove the `ConfigureSqliteJournalMode()` call: the method
was Bit.Besql's, and Bsqlite never runs in WAL mode.

```csharp
// Before
await dbContext.Database.EnsureDeletedAsync(CurrentCancellationToken); // Blazor Hybrid stores the db outside webview's storage.
await dbContext.Database.ConfigureSqliteJournalMode();
await dbContext.Database.MigrateAsync(CurrentCancellationToken);

// After
// The offline db lives outside the storages cleared above: on OPFS or in IndexedDB in the browser, and in a file in Blazor Hybrid.
await dbContext.Database.EnsureDeletedAsync(CurrentCancellationToken);
await dbContext.Database.MigrateAsync(CurrentCancellationToken);
```

### 7. The rest

- **Acknowledgements.** Bsqlite ships wa-sqlite, the SQLite build it runs in the browser. Add it next to the Community
  Toolkit Datasync entry in `src/Client/Boilerplate.Client.Core/Components/Common/Acknowledgements.razor.cs`:

  ```csharp
  new("wa-sqlite", "https://github.com/rhashimoto/wa-sqlite", "https://github.com/rhashimoto/wa-sqlite", "MIT"),
  ```

- **Tests.** If the project has `src/Tests`, `Features/Todo/OfflineTodoTests.cs` goes offline as soon as the page is
  interactive. The first open of the database now downloads the SQLite engine, which the test server's Development
  build leaves to the network. So wait for the list at the end of `GoToOfflineTodoPage`, before going offline:

  ```csharp
  // Nor is it the offline database being open: its first open downloads the SQLite engine and its storage, which
  // the test server's development build leaves to the network rather than to the service worker's cache. The list
  // replaces the loading indicator once the page has opened the database and pulled from the server.
  await Expect(Page.Locator(".todo-list")).ToBeVisibleAsync();
  ```

- **The project's own docs.** `src/Client/Boilerplate.Client.Core/Infrastructure/Data/README.md` shows how to download
  the database from the `bit-Besql` cache, which Bsqlite does not use. To get the database out of the browser now,
  inject `BsqliteDatabase` and call `ExportAsync()`, which returns a consistent SQLite file. `.docs/01- Entity Framework
  Core.md`, `.docs/12- Blazor Modes, PreRendering & PWA.md` and a comment in
  `src/Client/Boilerplate.Client.Web/wwwroot/service-worker.published.js` mention Bit.Besql too.

## Keeping your users' data

Bit.Besql left the data in two places:

- **In the browser**, in the `bit-Besql` cache under `/data/cache/App_Data/AppOffline.db`. Bit.Besql named the entry
  after the connection string's `Data Source`, which on WebAssembly was `/App_Data/AppOffline.db`.
- **On a device**, in `{local application data}/AC87AA5B-4B37-4E52-8468-2D5DF24AF256/App_Data/AppOffline.db`.

The server's file needs nothing: the server's branch in step 5 opens the same file Bit.Besql did.

Add two more `using` directives at the top of `IClientCoreServiceCollectionExtensions.cs`, next to the ones from step 5:

```csharp
using Bit.Bsqlite;
using Bit.Bsqlite.Storage.CacheStorage;
```

Then make four changes to the `AddBitBsqliteServices` call in step 5's first branch. The lines step 5 already has stay
as they are:

```csharp
services.AddBitBsqliteServices(sqlite => sqlite
    // ... the three AddStorageProvider calls of step 5 ...
    .AddStorageProvider<BsqliteCacheStorageProvider>() // 1. Reads the database Bit.Besql left in the browser.
    .UseStorage(storage => storage
        // ... Prefer, Fallback and AllowNonPersistentFallback, as in step 5 ...
        .WithMigrationMode(BsqliteStorageMigrationMode.AutomaticOnOpen)) // 2. Moves it to OPFS on its first open.
    .ConfigureFileSystemStorage(fileSystem => fileSystem.RootDirectory = Path.Combine( // 3. Bit.Besql's folder on a device.
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AC87AA5B-4B37-4E52-8468-2D5DF24AF256"))
    // ... BeforeFirstOpen, as in step 5 ...
    .AddDatabase("App_Data/AppOffline.db")); // 4. Bit.Besql's name for it, instead of "AppOffline.db".
```

1. **The `cache-storage` backend** reads the entry Bit.Besql left. The first time the new version opens the database it
   has no binding yet, and a database with no binding is looked for on every registered backend before a new one is
   created. So a returning user's database is found in `bit-Besql`, while a new user, with nothing anywhere, starts on
   OPFS. `cache-storage` does not need a place in the preference for this; being registered is enough.
2. **`AutomaticOnOpen`** moves the found database to OPFS, or to IndexedDB in a browser without OPFS. Without it, the
   database would stay on `cache-storage` for good, since existing data outranks preference. On that first open
   Bsqlite copies the database, checks the copy, moves the binding, and only then deletes the `bit-Besql` entry. If the
   copy fails, the database opens where it is, and the next open tries again. Later on, the same setting also moves a
   database created on IndexedDB once the browser offers OPFS, which is what the preference asks for anyway.
3. **`ConfigureFileSystemStorage`** points the `file-system` backend, which Bsqlite uses on a device, at Bit.Besql's
   folder. The name in change 4 then finds Bit.Besql's file there, and Bsqlite opens it where it is. Bit.Besql's file is
   in WAL mode, since stock EF Core created it. Bsqlite takes it back to a rollback journal the first time it opens it,
   folding the log into the file. In the browser this setting changes nothing.
4. **`App_Data/AppOffline.db`** is the name Bit.Besql used, in the browser and on a device alike. New users get the same
   name, and nothing else in the project depends on it.

Once a user has opened the new version, their browser no longer holds the data in `bit-Besql`. If you roll the
deployment back to a Bit.Besql version, that user starts on an empty database. The next pull fills it from the server,
and only changes they had not pushed are lost.

## Check the result

`BsqliteDatabase` reports where the database opened. Resolve it only where `AppPlatform.IsBlazorHybridOrBrowser` is
true, since the server's branch does not register it, and read `Resolution` once the database is open:

- **`Resolution.Provider.Name`** is `opfs` or `indexeddb` in the browser, and `file-system` on a device.
- **`Resolution.Attempts`** lists every candidate and what happened to it.

To see your users' data come across before you ship:

1. Run the version on Bit.Besql in a browser, and add a few Todo items.
2. Deploy the version on Bit.Bsqlite to the same address, and load it in that browser.
3. The items are listed, `Resolution.Provider.Name` is `opfs`, and the browser's developer tools no longer show
   `App_Data/AppOffline.db` under Cache Storage > `bit-Besql`. Bsqlite deletes that entry only once it has checked the
   copy on OPFS.

## What behaves differently

- **Synchronous EF Core calls throw** `BsqliteSynchronousApiNotSupportedException` in the browser and on a device. The
  template's code is async throughout. Code of your own that calls `ToList()`, `SaveChanges()` or `Database.Migrate()`
  on `AppOfflineDbContext` needs the async overloads. On the server they still work, but code shared with the client
  must not rely on that.
- **Several tabs can use the database at once.** With Bit.Besql, the last tab to write overwrote the others' changes.
- **The contexts of one session take turns.** The page's context and `SyncService`'s share the session's connection;
  while one has a transaction open, the other waits for it. See [Concurrent contexts](getting-started.md#concurrent-contexts).
- **The first open in a browser downloads the SQLite engine** and its storage module, so it takes longer than the opens
  after it.
- **Blazor Server and pre-rendering are unchanged.** They open the server's file, as they did with Bit.Besql.
