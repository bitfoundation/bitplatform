# Getting started

## Install

Reference `Bit.Bsqlite` - the ADO.NET surface, the EF Core integration, the worker transport, and the in-memory backend in one package - plus the storage packages you want. Every storage package is optional; register at least one.

```
dotnet add package Bit.Bsqlite
dotnet add package Bit.Bsqlite.Storage.Opfs
dotnet add package Bit.Bsqlite.Storage.IndexedDb
```

The engine (`.wasm` and the worker) is served as static web assets under `_content/Bit.Bsqlite/`. Nothing needs to be added to the host page.

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

Register them in the project that runs on WebAssembly. In a Blazor Web App whose pages are prerendered, register them in the server project as well: the prerender pass instantiates the pages there, so what they inject has to resolve, and nothing touches the browser until a page opens the database - which it should do only once the page runs in the browser: `RendererInfo.IsInteractive` on Interactive WebAssembly, and `OperatingSystem.IsBrowser()` on Interactive Auto, where a page on the server circuit is interactive too (and on .NET 8, which has no `RendererInfo`).

`AddBitBsqliteServices` registers, all scoped except the selection and the migrator (one scope is the whole application on WebAssembly, and a circuit or a request on a server):

| Service | What it is |
|---|---|
| `IBsqliteSessionFactory` | Resolves a backend, starts a worker, opens the database |
| `BsqliteDatabase` | The one shared session for the database named in `AddDatabase` |
| `BsqliteStorageProviderResolver`, `BsqliteStorageMigrator`, `IBsqliteStorageBindingStore` | Selection, migration, and the sticky binding (in `localStorage` by default) |
| `IBsqliteTransportFactory` | The worker transport |

Builder options:

- `EnableForeignKeys(false)` - skip `PRAGMA foreign_keys = ON` on open. On by default, as with Microsoft.Data.Sqlite.
- `OnOpen("PRAGMA cache_size = -16000")` - statements to run after every open.
- `AddExtensionModule("/my-functions.js")` - JavaScript that registers SQL functions; see [Extending](extending.md).
- `UseBindingStore<T>()` - replace where sticky bindings are remembered.
- `UseWorker(hostModuleUrl, workerUrl)` - host the worker from a different path.

## Use EF Core

```csharp
@inject IBsqliteDbContextFactory<AppDbContext> Contexts

await using var db = await Contexts.CreateDbContextAsync();
db.Products.Add(new Product { Name = "Widget", Price = 19.99m });
await db.SaveChangesAsync();
```

The factory opens the database on first use, runs the schema step once per session, and hands out contexts bound to the shared connection. Disposing a context does not close the database. The factory also implements `IDbContextFactory<TContext>`; its synchronous `CreateDbContext()` works once the database has been opened.

Use the async EF API throughout: `ToListAsync`, `SaveChangesAsync`, `MigrateAsync`. Synchronous members throw `BsqliteSynchronousApiNotSupportedException` naming the async overload, because the engine runs in a worker the main thread cannot wait on.

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
await Database.WhileClosedAsync(ct => provider.Admin.ImportAsync("app.db", bytes, ct));
```

Subscribe to `Closing` to drop contexts and live queries before the worker goes away, and to `Opened` to reload afterwards. The demo's `SessionLifecycle` component is one way to wire that into pages.
