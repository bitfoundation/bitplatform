# Moving from Bit.Besql to Bit.Bsqlite

Bit.Bsqlite replaces [Bit.Besql](https://github.com/bitfoundation/bitplatform/tree/v-10.7.0-pre-04/src/Besql), which is
no longer developed. This guide takes an application from one to the other without losing its users' data: what maps to
what, the steps in order, and what behaves differently afterwards.

## What changes underneath

Bit.Besql ran the native SQLite engine on Blazor's main thread, against a file in the WebAssembly in-memory file system,
and copied the whole file into Cache Storage after writes. Outside the browser - Blazor Server and Blazor Hybrid - it fell
back to a plain SQLite file and did nothing else.

Bit.Bsqlite runs the engine where it fits the host:

| Host | Bit.Besql | Bit.Bsqlite |
|---|---|---|
| Blazor WebAssembly, and a Blazor Web App's pages once they run on WebAssembly | Native SQLite on the main thread; the whole file copied to Cache Storage after each change | wa-sqlite in a dedicated worker; each committed transaction written to OPFS, IndexedDB or Cache Storage page by page |
| Blazor Hybrid (MAUI, WPF, Windows Forms) | A plain SQLite file at the path in the connection string | A plain SQLite file in the app's own folder, opened in-process by the native SQLite the app brings - selected automatically, with the same registration as the WebAssembly client |
| Blazor Server (Interactive Server) | A SQLite file on the server, shared by every user | Not supported - see [Blazor Server](#blazor-server) |

What you gain: several tabs can use the database at once (Bit.Besql's last tab to write overwrote the others' changes), a
write is stored when its transaction commits rather than by copying the whole file afterwards, nothing needs `WasmBuildNative` or the
`wasm-tools` workload, and live queries re-run when the tables they read change.

## At a glance

| Bit.Besql | Bit.Bsqlite |
|---|---|
| `dotnet add package Bit.Besql` | `dotnet add package Bit.Bsqlite` |
| `<script src="_content/Bit.Besql/bit-besql.js"></script>` in the host page | Nothing in the host page |
| `<WasmBuildNative>true</WasmBuildNative>` (and the `wasm-tools` workload) | Not needed |
| `services.AddBesqlDbContextFactory<TContext>(optionsAction, dbContextInitializer, lifetime)` | `services.AddBitBsqliteServices(...)` and `services.AddBitBsqliteDbContext<TContext>(...)` |
| `optionsBuilder.UseSqlite("Data Source=app.db")` | `AddDatabase("app.db")` |
| The rest of `optionsAction` - `UseModel`, logging, warnings | `options.Configure = (sp, builder) => ...` |
| `dbContextInitializer: (sp, db) => db.Database.MigrateAsync()` | The default: `options.Schema = BsqliteSchemaStrategy.Migrate` |
| Anything else the initializer did | `options.OnFirstUse = (db, ct) => ...` |
| The factory lazy loading `System.Private.Xml.wasm` | `sqlite.BeforeFirstOpen((sp, ct) => sp.GetRequiredService<LazyAssemblyLoader>().LoadAssembliesAsync(["System.Private.Xml.wasm"]))` |
| `lifetime: ServiceLifetime.Singleton` / `Scoped` | Always scoped - one scope is the whole app on WebAssembly, and one per web view in Blazor Hybrid |
| `IDbContextFactory<TContext>` | Unchanged; `IBsqliteDbContextFactory<TContext>` is the same instance |
| `IBitBesqlStorage.Persist` / `Load` | Gone: every committed transaction is stored |
| `PauseAutomaticPersistent()` / `ResumeAutomaticPersistent()` | A transaction - one commit stores the lot |
| `Database.ConfigureSqliteJournalMode()` | Gone: Bsqlite never runs in WAL mode |
| Copying the database file with `System.IO.File` (backups) | `BsqliteDatabase.ExportAsync()` |
| Writing a downloaded database over the file, then `Persist` | `Database.WhileClosedAsync(ct => provider.Admin.ImportAsync(name, bytes, ct).AsTask())` |
| `SaveChanges()`, `ToList()`, `Database.Migrate()` | `SaveChangesAsync()`, `ToListAsync()`, `Database.MigrateAsync()` - synchronous members throw |
| `new SqliteConnection(connectionString)` on the same database | `session.Connection`, a `BsqliteConnection` |

## Step by step

### 1. Swap the package

```bash
dotnet remove package Bit.Besql
dotnet add package Bit.Bsqlite
```

The `cache-storage` backend that reads what Bit.Besql left behind ships in `Bit.Bsqlite`. `Microsoft.EntityFrameworkCore.Sqlite`
is no longer needed for the browser; keep it only for a host Bsqlite does not serve. A Blazor Hybrid app brings the native
SQLite it opens its files with - `Microsoft.EntityFrameworkCore.Sqlite` already did, through `SQLitePCLRaw.bundle_e_sqlite3`,
so keep it there or reference that bundle on its own.

### 2. Clean the host page and the project file

Remove `<script src="_content/Bit.Besql/bit-besql.js"></script>` from `index.html` or `App.razor`: Bsqlite imports what it
needs on demand.

In the WebAssembly project, remove what was there only for Bit.Besql:

- `<WasmBuildNative>true</WasmBuildNative>` - unless something else needs native relinking (AOT sets it on its own). With
  it left on, a native SQLite bundle the project brings for Blazor Hybrid is linked into `dotnet.native.wasm` for nothing.
- Anything that was switched off because native relinking was on - IL stripping after AOT, a minifier step.

Keep `<BlazorWebAssemblyLazyLoad Include="System.Private.Xml.wasm" />` if the project has it. Bit.Besql's factory loaded
that assembly before its first context. Bsqlite needs it as well, for every connection and not only for EF Core, but does
not load it on its own: the registration in step 3 loads it with `BeforeFirstOpen`, before the first database opens. If
you drop the lazy load instead, the assembly downloads with the app, about 360 KB compressed with Brotli.

### 3. Register

Before:

```csharp
services.AddBesqlDbContextFactory<OfflineDbContext>((sp, options) =>
{
    options
        .UseModel(OfflineDbContextModel.Instance)
        .UseSqlite("Data Source=Offline-Client.db");
}, dbContextInitializer: async (sp, db) => await db.Database.MigrateAsync());
```

After:

```csharp
using Bit.Bsqlite.Storage.CacheStorage;
using Bit.Bsqlite.Storage.IndexedDb;
using Bit.Bsqlite.Storage.Opfs;
using Microsoft.AspNetCore.Components.WebAssembly.Services;

services.AddBitBsqliteServices(sqlite => sqlite
    .AddStorageProvider<BsqliteCacheStorageProvider>()   // where Bit.Besql left the data
    .AddStorageProvider<BsqliteOpfsStorageProvider>()
    .AddStorageProvider<BsqliteIndexedDbStorageProvider>()
    .UseStorage(s => s.Prefer(BsqliteCacheStorageProvider.ProviderName))
    .BeforeFirstOpen((sp, ct) => sp.GetRequiredService<LazyAssemblyLoader>()
        .LoadAssembliesAsync(["System.Private.Xml.wasm"])) // only with the lazy load kept, see step 2
    .AddDatabase("Offline-Client.db"));                  // Bit.Besql's Data Source, see step 4

services.AddBitBsqliteDbContext<OfflineDbContext>(options =>
{
    options.Configure = (sp, builder) => builder.UseModel(OfflineDbContextModel.Instance);
    // options.Schema = BsqliteSchemaStrategy.Migrate is the default: MigrateAsync once per session.
});
```

Register it where Bit.Besql was registered. In a Blazor Web App whose pages are prerendered, the server project registers
it too, so the pages resolve what they inject; the database opens only once a page runs on WebAssembly (step 6).

The same registration serves a Blazor Hybrid app unchanged: there the browser backends are passed over and the database
is a file on the device. See [Blazor Hybrid](#blazor-hybrid) for keeping the file Bit.Besql used.

`OnFirstUse` runs after the schema step. Work that has to happen before migrating - checking that a compiled model was
generated, say - goes in `OnFirstUse` with `options.Schema = BsqliteSchemaStrategy.None`, followed by its own
`await db.Database.MigrateAsync(ct)`.

### 4. Keep your users' data

**In the browser.** Bit.Besql kept each database in the `bit-Besql` cache under `/data/cache/{name}`, where the name is the
connection string's `Data Source` without its leading slash. The first time Bsqlite opens a database of that name on the
`cache-storage` backend, it imports that file - losslessly, into its own page layout. Two things decide whether that
happens:

- **The name must match.** `Data Source=Offline-Client.db` is `AddDatabase("Offline-Client.db")`. A path built from
  `Directory.GetCurrentDirectory()` - which is `/` on WebAssembly - such as `/App_Data/AppOffline.db`, is
  `AddDatabase("App_Data/AppOffline.db")`. When in doubt, open the browser's developer tools, Application > Cache Storage
  > `bit-Besql`, and read the key.
- **`cache-storage` must be the first choice** (or the only one) for a database that does not exist in Bsqlite yet. With
  OPFS ranked first, a user who has not opened the new version yet starts on an empty OPFS database, and the Bit.Besql
  file is never read.

A database that has been opened is bound to its backend for good - existing data outranks preference - so reordering the
preference later does not move it. To move it to OPFS once it is in Bsqlite, migrate it explicitly, inside
`BsqliteDatabase.WhileClosedAsync`, the way the demo's storage switcher does:

```csharp
await Database.WhileClosedAsync(ct => migrator.MigrateAsync("Offline-Client.db", cacheStorage, opfs, bindings, ct));
```

`BsqliteStorageMigrator` copies the image, checks it, flips the binding, and only then deletes the source - see
[Migration](storage.md#migration). An application that never shipped Bit.Besql to users has no data to keep: it can
register OPFS and IndexedDB alone.

**On a device.** See [Blazor Hybrid](#blazor-hybrid).

### 5. Update the code that used the database

- **Async throughout.** Bit.Besql ran SQLite on the calling thread, so `SaveChanges()` and `ToList()` worked. Bsqlite's
  engine is in a worker - or, in Blazor Hybrid, behind the same transport - and synchronous members throw
  `BsqliteSynchronousApiNotSupportedException` naming the async overload. `CreateDbContext()` works once an awaited
  `CreateDbContextAsync()` has opened the database.
- **`ConfigureSqliteJournalMode()`** - delete the calls, including the one after `EnsureDeletedAsync()` when recreating
  the database. Bsqlite rejects `PRAGMA journal_mode = WAL` on every backend.
- **Pause and resume.** Put the writes in one transaction; they are stored when it commits:

  ```csharp
  await using var db = await Contexts.CreateDbContextAsync();
  await using var transaction = await db.Database.BeginTransactionAsync();
  // ... many SaveChangesAsync / ExecuteDeleteAsync calls ...
  await transaction.CommitAsync();
  ```

- **Backups.** `await Database.ExportAsync()` returns a consistent SQLite file image of the open database, through
  `sqlite3_serialize`. `provider.Admin.ExportAsync(name)` reads a closed one.
- **A pre-populated database.** Download the file and import it while the database is closed:

  ```csharp
  var bytes = await http.GetByteArrayAsync("Pre-Populated-Offline-Client.db");
  await Database.WhileClosedAsync(ct => cacheStorage.Admin.ImportAsync("Offline-Client.db", bytes, ct).AsTask());
  ```

- **Recreating the database.** `EnsureDeletedAsync()` drops every table and view and runs `VACUUM`; it cannot unlink the
  file while the session holds it. Follow it with `MigrateAsync()` as before. To remove the file itself, delete it with
  `provider.Admin.DeleteAsync` inside `WhileClosedAsync`.
- **Your own `SqliteConnection`.** Use `session.Connection` (from `BsqliteDatabase.OpenAsync()`), or the context's
  `Database.GetDbConnection()`: a `BsqliteConnection` with the full async ADO.NET surface.

### 6. Open the database once the page runs where its data is

Bit.Besql said to use the factory in `OnAfterRender`, because it needed `IJSRuntime`. Bsqlite needs the page to run on
WebAssembly: on Interactive WebAssembly open the database once `RendererInfo.IsInteractive` is true, on Interactive Auto
once `OperatingSystem.IsBrowser()` is - a page on the server circuit is interactive too - and on .NET 8, which has no
`RendererInfo`, use `OperatingSystem.IsBrowser()` either way. Blazor Hybrid pages open it whenever they like.

### 7. Check the result

`BsqliteDatabase.Resolution` says which backend holds the database and why: `WasFoundWithoutBinding` is true the first time
an imported Bit.Besql database opens, and `Attempts` lists every candidate and what happened to it. Show it somewhere a
support person can read it.

## Blazor Hybrid

Nothing needs registering for the device: in a Blazor Hybrid app `AddBitBsqliteServices` adds the `file-system` backend
(`BsqliteFileSystemStorageProvider`) and ranks it ahead of the browser backends, which need .NET running in the browser.
The database is a plain SQLite file, opened in-process by the native SQLite the app brings for Android, iOS, Mac
Catalyst, Windows and macOS - `SQLitePCLRaw.bundle_e_sqlite3`, which `Microsoft.EntityFrameworkCore.Sqlite` brought for
Bit.Besql. By default it lives under `{local application data}/{entry assembly name}/bit-bsqlite`, and the database name is a
path under that folder - one every platform's file system keeps as written: no `<>:"|?*` or control characters, and no
segment ending in a dot or a space.

Bit.Besql used whatever path the connection string named. To keep that file, point the backend's root at the folder and
name the rest:

```csharp
// Bit.Besql: UseSqlite($"Data Source={Path.Combine(appData, "App_Data", "AppOffline.db")}")
services.AddBitBsqliteServices(sqlite => sqlite
    // ... the browser backends, as in step 3 ...
    .ConfigureFileSystemStorage(o => o.RootDirectory = appData)
    .AddDatabase("App_Data/AppOffline.db"));
```

The browser keeps resolving `App_Data/AppOffline.db` against Bit.Besql's cache key, so one name serves both.
`ConfigureFileSystemStorage` changes nothing in the browser.

Bit.Besql's file on a device was created by stock EF Core, in WAL mode. Bsqlite takes it back to a rollback journal the
first time it opens it - checkpointing the log into the file - so the whole database is in the one file. `Admin.ExportAsync`
folds a log still next to the file into it first - one written by a Bit.Besql app killed before it closed, read before
the database first opened in Bsqlite - and fails rather than leave committed rows out while another process holds the file.

On a device the same rules hold as in the browser - async members only, no WAL - so code written once runs on both. Two
windows of a Hybrid app share the file the way two tabs do: writes in one re-run live queries in the other, and `Admin`'s
delete, import and export need the database closed in both - they throw while either still has it open. JavaScript extension modules (`AddExtensionModule`)
run only in the browser worker; on a device they are skipped with a warning.

## Blazor Server

Bit.Besql on Interactive Server opened a SQLite file on the server, one for every user of the site. Bsqlite does not serve
that host: its databases belong to the user, in their browser or on their device, and the `file-system` backend is never
offered to a server, so a page on the circuit cannot quietly open a file the whole site would share. Choose one:

- Make the pages that use the database run on WebAssembly - put them in the `.Client` project with
  `@rendermode InteractiveWebAssembly` - and keep Interactive Server for the rest of the site.
- If the data belongs on the server, it is a server database: register `Microsoft.EntityFrameworkCore.Sqlite` (or any other
  provider) for the server host with `AddDbContextFactory`, beside Bsqlite's registration for the browser.

## Projects created from the bit Boilerplate template

A project created with `--offlineDb` registers its `AppOfflineDbContext` with Bit.Besql in
`Client.Core/Infrastructure/Extensions/IClientCoreServiceCollectionExtensions.cs`, and runs it in the browser, on devices,
and on the server when the app runs in Blazor Server mode. Moving it over:

1. In `Directory.Packages.props` and `Client.Core.csproj`, replace `Bit.Besql` with `Bit.Bsqlite`. Keep
   `Microsoft.EntityFrameworkCore.Sqlite`: the Blazor Server registration (step 5 of this list) uses it, and it brings the
   native SQLite the MAUI and Windows apps open their files with.
2. In `Client.Web/wwwroot/index.html`, remove the `bit-besql.js` script.
3. In `Client.Web.csproj`, remove `WasmBuildNative`, and let `WasmStripILAfterAOT` and `Bit.Minifier` apply as they do
   without an offline database. Keep the `System.Private.Xml.wasm` lazy load that `offlineDb` turned on: the registration
   in step 4 loads it with `BeforeFirstOpen`, through the `LazyAssemblyLoader` that `Client.Core` already registers.
4. Replace `AddBesqlDbContextFactory<AppOfflineDbContext>` with Bsqlite's registration. The template's database is
   `App_Data/AppOffline.db` in the browser's `bit-Besql` cache, and
   `{local application data}/AC87AA5B-4B37-4E52-8468-2D5DF24AF256/App_Data/AppOffline.db` on devices:

   ```csharp
   services.AddBitBsqliteServices(sqlite => sqlite
       .AddStorageProvider<BsqliteCacheStorageProvider>()
       .AddStorageProvider<BsqliteOpfsStorageProvider>()
       .AddStorageProvider<BsqliteIndexedDbStorageProvider>()
       .UseStorage(s => s.Prefer(BsqliteCacheStorageProvider.ProviderName))
       .ConfigureFileSystemStorage(o => o.RootDirectory = Path.Combine(
           Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
           "AC87AA5B-4B37-4E52-8468-2D5DF24AF256"))
       .BeforeFirstOpen((sp, ct) => sp.GetRequiredService<LazyAssemblyLoader>()
           .LoadAssembliesAsync(["System.Private.Xml.wasm"]))
       .AddDatabase("App_Data/AppOffline.db"));

   services.AddBitBsqliteDbContext<AppOfflineDbContext>(options =>
   {
       options.Schema = BsqliteSchemaStrategy.None;
       options.Configure = (sp, builder) => builder
           .EnableSensitiveDataLogging(AppEnvironment.IsDevelopment())
           .EnableDetailedErrors(AppEnvironment.IsDevelopment());
       options.OnFirstUse = async (db, ct) =>
       {
           // The template's "has not been optimized" check goes here, before migrating.
           await db.Database.MigrateAsync(ct);
       };
   });
   ```

5. Blazor Server mode - including the Docker image's `/container_volume` - is the [Blazor Server](#blazor-server) case:
   keep a plain `AddDbContextFactory<AppOfflineDbContext>(o => o.UseSqlite(...))` for that host, or run the offline pages
   on WebAssembly.
6. In `AppDiagnosticModal.razor.Utils.cs`, remove the `ConfigureSqliteJournalMode()` call that follows
   `EnsureDeletedAsync()`.

`SyncService` and the Todo page already use the async EF API, and CommunityToolkit.Datasync's offline context reads and
writes through async EF calls only, so neither needs changing.
