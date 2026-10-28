# bit Bsqlite

EF Core on SQLite-WASM for Blazor WebAssembly - and on the device's native SQLite for Blazor Hybrid,
with the same registration. The same entities and the same migrations on the client as on the server.

This package is everything you need: the EF Core integration, the ADO.NET surface with live queries,
the worker transport, the storage provider contract, the storage backends - OPFS, IndexedDB, Cache
Storage and in-memory - the browser assets (SQLite engine builds, worker host, VFSes) served
under `_content/Bit.Bsqlite`, and the file system backend and native SQLite a Blazor Hybrid app runs on.

**Runtime:** .NET 10 and 11, each with its EF Core. **License:** MIT (bundles [wa-sqlite](https://github.com/rhashimoto/wa-sqlite), also MIT).

## Install

```
dotnet add package Bit.Bsqlite
```

## Storage backends

Register the ones you want with `AddStorageProvider<T>()`. Registering one costs the browser nothing
until it is selected: the worker imports only the selected backend's VFS, and the one engine build it
needs.

| Provider (namespace) | Name | Engine build | Notes |
|---|---|---|---|
| `BsqliteOpfsStorageProvider` (`Bit.Bsqlite.Storage.Opfs`) | `opfs` | Synchronous — the smallest download | Persistent, real files in the origin private file system, several connections, worker-only, no COOP/COEP headers. Pick it when you can. |
| `BsqliteIndexedDbStorageProvider` (`Bit.Bsqlite.Storage.IndexedDb`) | `indexeddb` | JSPI where available, Asyncify otherwise | Persistent, the widest browser reach and the fallback behind OPFS. Fixed page size; readers serialize. |
| `BsqliteCacheStorageProvider` (`Bit.Bsqlite.Storage.CacheStorage`) | `cache-storage` | JSPI where available, Asyncify otherwise | Persistent. Imports a database left behind by Bit.Besql losslessly on first open - the [migration path off Besql](https://github.com/bitfoundation/bitplatform/blob/develop/src/Utilities/Bsqlite/docs/migrating-from-besql.md). |
| `BsqliteInMemoryStorageProvider` (`Bit.Bsqlite.Storage.InMemory`) | `in-memory` | Synchronous | Volatile: gone on reload. Tests, demos, and a declared last resort. |
| `BsqliteFileSystemStorageProvider` (`Bit.Bsqlite.Storage.FileSystem`) | `file-system` | None - the native SQLite the app brings (`SQLitePCLRaw.bundle_e_sqlite3`, or the bundle it already uses) | Blazor Hybrid only, registered and selected for it automatically: plain SQLite files in the app's own folder. |

The three persistent providers take an optional `IJSRuntime` and report themselves unavailable without
one, so selection also runs - and explains itself - in desktop tests. The capabilities each one declares, and what the
core does with them, are in the [storage guide](https://github.com/bitfoundation/bitplatform/blob/develop/src/Utilities/Bsqlite/docs/storage.md).

## Register

```csharp
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

builder.Services.AddBitBsqliteDbContext<AppDbContext>();
```

In a Blazor Hybrid app (MAUI, WPF, Windows Forms) the same registration works unchanged: Bsqlite
adds the `file-system` backend, ranks it ahead of the browser ones, and keeps the database as a SQLite
file in the app's own folder. `ConfigureFileSystemStorage(o => o.RootDirectory = ...)` moves it.

## Use

```csharp
@inject IBsqliteDbContextFactory<AppDbContext> Contexts

await using var db = await Contexts.CreateDbContextAsync();   // migrates once per session
var open = await db.Orders.Where(o => o.Open).ToListAsync();
```

Without dependency injection the same pieces compose by hand:

```csharp
var session = await factory.OpenAsync("app.db");   // a BsqliteSessionFactory
options.UseBitBsqlite(session.Connection);
```

Selection is sticky: existing data outranks preference, so a database already written on one backend
is reopened there rather than silently starting empty somewhere else.
`BsqliteStorageMigrationMode.KeepExisting` is the default; `AutomaticOnOpen` copies the image,
verifies the SQLite header, then flips the binding.

## Live queries

Table-level, and they fire across tabs. A committed write re-runs any live query that reads that
table. The worker learns what changed from SQLite's own update hook - cascades and triggers included -
and reports it once the transaction commits, so a rolled-back write is never shown and a
multi-statement `SaveChangesAsync` re-runs a query once, not per statement.

```csharp
await using var live = ctx.Orders.Where(o => o.Open).AsLiveQuery();
live.Changed += (_, rows) => InvokeAsync(StateHasChanged);
```

## Async only

The browser cannot block on storage, so the synchronous ADO.NET surface throws
`BsqliteSynchronousApiNotSupportedException` rather than deadlocking or lying. Blazor Hybrid keeps the
same rule, so one code path serves both. Use the `Async`
overloads throughout — `ToListAsync`, `SaveChangesAsync`, `MigrateAsync`.

## Hosting under a sub-path

Every asset is reached relative to the document, the way Blazor's own are, so an application published
under `<base href="/app/">` needs no configuration: the host resolves each provider's VFS module -
and each extension module - against the document base before the worker imports it.

## Caching the browser assets

The host module is imported with a `?v=<assembly version>` query and passes it on to the worker and
the modules it loads, so an upgrade cannot pair new .NET with JavaScript the browser cached from an
older version. The engine files under `_content/Bit.Bsqlite/engine` carry no query - they change only
when the pinned wa-sqlite version does - so if your server sends long `max-age` headers for static
content, exclude `_content/Bit.Bsqlite/` or serve it with `Cache-Control: no-cache` so the browser
revalidates.

## Links

- [Documentation and source](https://github.com/bitfoundation/bitplatform/tree/develop/src/Utilities/Bsqlite), and the demo website at [bsqlite.bitplatform.dev](https://bsqlite.bitplatform.dev)
- [Issues](https://github.com/bitfoundation/bitplatform/issues)
- Writing your own storage backend: implement `IBsqliteStorageProvider` - the same public contract
  the backends above are built on - and inherit the conformance kit from `Bit.Bsqlite.Testing`.
