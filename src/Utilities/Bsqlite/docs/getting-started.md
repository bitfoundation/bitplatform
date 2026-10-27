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

Bit.Bsqlite targets .NET 8, 9 and 10, each with the EF Core of the same major version.

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

In a Blazor Web App whose pages are prerendered, register them in the server project as well: the prerender pass instantiates the pages there, so what they inject has to resolve, and nothing touches the browser until a page opens the database - which it should do only once the page runs in the browser: `RendererInfo.IsInteractive` on Interactive WebAssembly, and `OperatingSystem.IsBrowser()` on Interactive Auto, where a page on the server circuit is interactive too (and on .NET 8, which has no `RendererInfo`).

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

The schema step and `OnFirstUse` run under a lock shared with every other tab, and so does EF's own `MigrateAsync` on EF Core 9 and later: each tab has its own worker over the same file, and two tabs opening after a deploy would otherwise both apply the new migration - or both seed an empty database. The lock is a Web Lock, so a tab that closes mid-migration releases it.

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
