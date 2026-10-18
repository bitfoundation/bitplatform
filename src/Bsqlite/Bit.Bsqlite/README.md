# bit Bsqlite

EF Core on SQLite-WASM for Blazor WebAssembly. The same entities and the same migrations on the
client as on the server.

This package is everything you need except a persistent backend: the EF Core integration, the
ADO.NET surface with live queries, the worker transport, the storage provider contract, a volatile
in-memory backend, and the browser assets (SQLite engine builds, worker host, JavaScript API) served
under `_content/Bit.Bsqlite`.

**Runtime:** .NET 8, 9 and 10, each with its EF Core. **License:** MIT (bundles [wa-sqlite](https://github.com/rhashimoto/wa-sqlite), also MIT).

## Install

```
dotnet add package Bit.Bsqlite
```

Add a persistent backend — without one you get the in-memory provider, which does not survive a
reload:

| Package | Notes |
|---|---|
| `Bit.Bsqlite.Storage.Opfs` | Sync engine (smallest download), multi-connection, worker-only |
| `Bit.Bsqlite.Storage.IndexedDb` | Widest browser reach, runs anywhere, JSPI/Asyncify |
| `Bit.Bsqlite.Storage.CacheStorage` | Imports a database left behind by [Bit.Besql](https://github.com/bitfoundation/bitplatform/tree/develop/src/Besql) |

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
`BsqliteSynchronousApiNotSupportedException` rather than deadlocking or lying. Use the `Async`
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

- [Documentation and source](https://github.com/bitfoundation/bitplatform/tree/develop/src/Bsqlite), and the demo website at [bsqlite.bitplatform.dev](https://bsqlite.bitplatform.dev)
- [Issues](https://github.com/bitfoundation/bitplatform/issues)
- Writing your own storage backend: implement `IBsqliteStorageProvider` and inherit the
  conformance kit from `Bit.Bsqlite.Testing`.
