# bit Bsqlite

SQLite in the browser for Blazor, with EF Core on top. The same entities and the same migrations run on the client as
on the server, and the database lives in the user's browser tab - in OPFS, IndexedDB, Cache Storage or memory - or, in a
Blazor Hybrid app, in a file on the device, with the same registration and the same code.

```csharp
// Program.cs
builder.Services.AddBitBsqliteServices(sqlite => sqlite
    .AddStorageProvider<BsqliteOpfsStorageProvider>()
    .AddStorageProvider<BsqliteIndexedDbStorageProvider>()
    .UseStorage(s => s.Prefer("opfs").Fallback("indexeddb"))
    .AddDatabase("app.db"));

builder.Services.AddBitBsqliteDbContext<AppDbContext>();

// A component
@inject IBsqliteDbContextFactory<AppDbContext> Contexts

await using var db = await Contexts.CreateDbContextAsync();
var open = await db.Orders.Where(o => o.Status == OrderStatus.Open).ToListAsync();
```

That query runs in a dedicated worker in the browser: SQLite 3.53 (via [wa-sqlite](https://github.com/rhashimoto/wa-sqlite)
1.1.2), the stock EF Core SQLite provider, and a storage backend chosen and remembered per database.

> Targets **.NET 10 and 11** with the matching EF Core · Runs on **Blazor WebAssembly**, in a **Blazor Web App** on
> **Interactive WebAssembly** or **Interactive Auto** once the page runs on WebAssembly, and in **Blazor Hybrid** apps
> (MAUI, WPF, Windows Forms) on the device's native SQLite. Prerendered pages render on the server and open their database
> once interactive.

Bsqlite's documentation website lives in [`Demo`](https://github.com/bitfoundation/bitplatform/tree/develop/src/Utilities/Bsqlite/Demo/) - run the
`Demo/Server` project, or visit [bsqlite.bitplatform.dev](https://bsqlite.bitplatform.dev).

---

## Table of Contents

- [Why Bsqlite](#why-bsqlite)
- [Packages](#packages)
- [Installation](#installation)
- [Composition without dependency injection](#composition-without-dependency-injection)
- [Live queries](#live-queries)
- [Where it runs](#where-it-runs)
- [Lazy loading on WebAssembly](#lazy-loading-on-webassembly)
- [What this release does not claim](#what-this-release-does-not-claim)
- [Moving off Bit.Besql](#moving-off-bitbesql)
- [MCP server (for AI agents)](#mcp-server-for-ai-agents)
- [Tests](#tests)
- [Documentation](#documentation)
- [License](#license)

---

## Why Bsqlite

- **EF Core, unmodified.** `UseBitBsqlite` is the stock SQLite provider over a Bsqlite connection. Migrations scaffolded
  against the server apply in the browser. Decimal arithmetic, `REGEXP`, and the `EF_DECIMAL` collation are provided in
  the worker so query results match the server's.
- **Fast interop.** SQL batches and result sets cross the worker and Blazor boundaries as one binary buffer each,
  transferred rather than copied. No JSON, no base64, 64-bit integers intact. Prepared statements are cached in the
  worker, so EF's repeated statement texts are parsed once.
- **Honest storage.** Selection never substitutes a backend silently: existing data outranks preference, a
  non-persistent fallback needs an opt-in, and a failed open names every candidate and why it was rejected.
  Cross-backend migration verifies the copy before flipping the binding.
- **Live queries.** A write re-runs any live query that reads the table, including writes from another tab. Change
  tracking comes from the engine's update hook, so triggers and cascades count too.
- **Complete ADO.NET.** Commands, batches (`DbBatch`), transactions with savepoints and rollback-on-dispose,
  multi-result readers, typed exceptions by SQLite result code, and in-place export of an open database.

## Packages

| Package | Role |
| --- | --- |
| `Bit.Bsqlite` | `AddBitBsqliteServices`, `AddBitBsqliteDbContext`, `UseBitBsqlite(connection)`, ADO.NET and live queries, the worker transport, storage selection and migration, the storage provider contract, the storage backends, the browser assets (engine, worker, host, VFSes) under `_content/Bit.Bsqlite`, and the native transport and SQLite engine Blazor Hybrid apps run on |
| `Bit.Bsqlite.Testing` | An in-process transport for browser-free unit tests, plus the MSTest conformance kit every storage provider must pass |

The storage backends, each in its own namespace and each registered with `AddStorageProvider<T>()`:

| Backend | Provider (namespace) | Role |
| --- | --- | --- |
| `opfs` | `BsqliteOpfsStorageProvider` (`Bit.Bsqlite.Storage.Opfs`) | Persistent, synchronous engine build (the smallest download), several connections |
| `indexeddb` | `BsqliteIndexedDbStorageProvider` (`Bit.Bsqlite.Storage.IndexedDb`) | Persistent, JSPI or Asyncify engine build, widest reach |
| `cache-storage` | `BsqliteCacheStorageProvider` (`Bit.Bsqlite.Storage.CacheStorage`) | Persistent, and the migration path off `Bit.Besql` |
| `in-memory` | `BsqliteInMemoryStorageProvider` (`Bit.Bsqlite.Storage.InMemory`) | Volatile: tests, demos, and a declared last resort |
| `file-system` | `BsqliteFileSystemStorageProvider` (`Bit.Bsqlite.Storage.FileSystem`) | Blazor Hybrid: plain SQLite files in the app's own folder, registered and selected automatically |

## Installation

```bash
dotnet add package Bit.Bsqlite
```

That is the whole install - every backend is in it. Browser-free unit tests add `Bit.Bsqlite.Testing` to the test project.

Register the services in the project that runs on WebAssembly - and, in a Blazor Web App whose pages are prerendered,
in the server project too, so the prerender pass can resolve what the pages inject:

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

builder.Services.AddBitBsqliteDbContext<AppDbContext>();
```

The engine (`.wasm`), the worker and the VFSes are static web assets under `_content/Bit.Bsqlite/`, each imported on
demand: registering a backend costs nothing until it is selected, and a session downloads one engine build and the VFS of
the backend it selected. Nothing needs to be added to the host page.

The same registration serves a Blazor Hybrid app: .NET runs natively there, so Bsqlite registers the `file-system`
backend, ranks it ahead of the browser ones, and opens the database as a file in the app's own folder with the native
SQLite the app brings - `SQLitePCLRaw.bundle_e_sqlite3`, or the SQLitePCLRaw bundle it already uses.
`ConfigureFileSystemStorage(o => o.RootDirectory = ...)` moves the folder; nothing else is needed. The native engine
stays out of the WebAssembly build.

## Composition without dependency injection

`AddBitBsqliteServices` registers everything above. Without dependency injection, the same pieces compose by hand:

```csharp
var session = await factory.OpenAsync("app.db");   // a BsqliteSessionFactory
options.UseBitBsqlite(session.Connection);
```

Selection is sticky: existing data outranks preference. `BsqliteStorageMigrationMode.AutomaticOnOpen` copies, checks the
SQLite header, then flips the binding. `KeepExisting` is the default.

## Live queries

Table-level. A committed write re-runs any live query that reads that table, including across tabs. The worker learns
what changed from SQLite's own update hook - cascades and triggers included - and reports it once the transaction
commits, so a rolled-back write is never shown and a multi-statement `SaveChangesAsync` re-runs a query once, not per
statement.

```csharp
await using var live = ctx.Orders.Where(o => o.Open).AsLiveQuery();
live.Changed += (_, rows) => InvokeAsync(StateHasChanged);
```

## Where it runs

| Host | Support |
| --- | --- |
| Blazor WebAssembly | Everything |
| Blazor Web App, Interactive WebAssembly | Everything |
| Blazor Web App, Interactive Auto | Everything once the page runs on WebAssembly |
| Prerendering | Register the services on the server as well; they resolve there, and the database opens once the page runs in the browser (`RendererInfo.IsInteractive` on Interactive WebAssembly, `OperatingSystem.IsBrowser()` on Interactive Auto) |
| Blazor Hybrid (MAUI, WPF, Windows Forms) | Everything, on a SQLite file on the device - the same registration, async rules and live queries as in the browser; two windows share the file the way two tabs do |
| Interactive Server | Not supported: a user's database lives in their browser, and Bsqlite will not keep one file on the server for every user |

Every service that reaches the browser, or holds a database session, is registered as scoped: one for the whole
application on WebAssembly, one per web view in Blazor Hybrid, and one per circuit or request on a server, so nothing is
ever shared between users.

## Lazy loading on WebAssembly

EF Core and Bsqlite are most of what a WebAssembly app downloads for its database - about 2.1 MB, compressed with
Brotli, for a small app on the demo's model. An app whose database only some pages use can mark them
`BlazorWebAssemblyLazyLoad` and load them from the router's `OnNavigateAsync` before those pages render. `Program.cs`
then registers nothing of Bsqlite's, and the pages build the same services the first time one of them asks:

```csharp
// Called only from the pages the assemblies are loaded for, once they run in the browser.
internal static class AppDatabase
{
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

`GetBitBsqliteServices` makes that registration once, on WebAssembly, in a container of Bsqlite's own in front of the
app's, and takes the same method on every call. Where the app called `AddBitBsqliteServices` after all - a Blazor
Hybrid app sharing the pages, which registers with `AppDatabase.Register(builder.Services)` - it returns the app's own
container, once it has checked that what the method registers is there. Which assemblies to mark, the router's code, and the rules the rest of the app keeps - no public `DbContext` or migration in an assembly
the router reads, nothing of EF Core's in `Program.cs`, the database asked for only once a page is interactive, no AOT - are in the *Getting started* guide's
[Lazy loading on WebAssembly](https://github.com/bitfoundation/bitplatform/blob/develop/src/Utilities/Bsqlite/docs/getting-started.md#lazy-loading-on-webassembly).

## What this release does not claim

- A synchronous tier (`SaveChanges()`, `ToList()`). Synchronous members throw
  `BsqliteSynchronousApiNotSupportedException` naming the async overload.
- WAL journal mode. No web VFS can provide the shared memory it needs.
- Server synchronization. The demo's two sides share a model, not data.
- A synchronous worker bridge. Browsers forbid `Atomics.wait` on the main thread, a spin-wait would freeze the UI, and
  multithreaded WebAssembly is not Blazor's default.
- `in-memory` has no admin view of live data. It registers no VFS, so a database opened on it lives in the engine's own
  heap inside the worker; `Admin` describes only images passed to it directly. Export, list, and migration away from
  this backend do not see what a connection wrote, and a migration onto it does not reach the database a connection
  opens - the source is deleted and the next open starts empty. Persistent backends do not have this gap.
- `indexeddb` serializes readers. `IDBBatchAtomicVFS` is registered with `WebLocksMixin`'s default `exclusive` lock
  policy, so a read holds the same lock a write does. `SupportsConcurrentReads` reports that honestly.

## Moving off Bit.Besql

Bsqlite replaces Bit.Besql. The `cache-storage` backend (`BsqliteCacheStorageProvider`) reads the file Bit.Besql left in
its `bit-Besql` cache: a database is imported losslessly into Bsqlite's own page layout the first time it is opened, so an
application that switches keeps its users' data - and in a Blazor Hybrid app, the `file-system` backend opens the SQLite
file Bit.Besql used on the device in place. The *Moving from Bit.Besql* guide, listed under [Documentation](#documentation),
maps every Bit.Besql API to its replacement and walks through the move step by step.

## MCP server (for AI agents)

The demo website hosts a [Model Context Protocol](https://modelcontextprotocol.io) server at
`https://bsqlite.bitplatform.dev/mcp`. Point your coding agent at it to let it search the Bsqlite API, read the guides
and the demo's source, and plan a storage setup. See the MCP server page of the demo website for the full tool list.

## Tests

The wa-sqlite engine and the upstream VFS files are vendored at restore, verified against the checksums in
`Bit.Bsqlite/engine/wa-sqlite.lock.props`, and git-ignored, so restore before building.

```bash
cd src/Utilities/Bsqlite/Tests/Bit.Bsqlite.Tests && dotnet test        # ADO.NET, EF Core and storage selection, every target framework
cd src/Utilities/Bsqlite/Tests/Bit.Bsqlite.Tests.E2E && dotnet test    # the worker, the engine and every VFS in a real browser, plus the demo
cd src/Utilities/Bsqlite/Tests/Bit.Bsqlite.Tests.Mcp && dotnet test    # the MCP server, hosted in-memory
cd src/Utilities/Bsqlite/Tests/bit-bsqlite-js && npm ci && npm test   # the JavaScript that needs no browser
```

See [Tests/Bit.Bsqlite.Tests.E2E/README.md](https://github.com/bitfoundation/bitplatform/blob/develop/src/Utilities/Bsqlite/Tests/Bit.Bsqlite.Tests.E2E/README.md)
for the browser matrix, the soak suite and the benchmark page.

## Documentation

- [Getting started](https://github.com/bitfoundation/bitplatform/blob/develop/src/Utilities/Bsqlite/docs/getting-started.md) - registration, contexts, raw ADO.NET, the shared database, lazy loading on WebAssembly
- [Storage](https://github.com/bitfoundation/bitplatform/blob/develop/src/Utilities/Bsqlite/docs/storage.md) - backends, selection rules, migration, admin, writing a provider
- [Live queries](https://github.com/bitfoundation/bitplatform/blob/develop/src/Utilities/Bsqlite/docs/live-queries.md) - EF and ADO.NET live queries, cross-tab notifications
- [Architecture and performance](https://github.com/bitfoundation/bitplatform/blob/develop/src/Utilities/Bsqlite/docs/architecture.md) - the worker, the binary wire format, the statement cache, change tracking
- [Limits](https://github.com/bitfoundation/bitplatform/blob/develop/src/Utilities/Bsqlite/docs/limits.md) - what the browser cannot do and how the library tells you
- [Extending](https://github.com/bitfoundation/bitplatform/blob/develop/src/Utilities/Bsqlite/docs/extending.md) - SQL functions from JavaScript, custom transports
- [Moving from Bit.Besql](https://github.com/bitfoundation/bitplatform/blob/develop/src/Utilities/Bsqlite/docs/migrating-from-besql.md) - what replaces each Bit.Besql API, and keeping users' data
- [Moving a bit Boilerplate project from Bit.Besql](https://github.com/bitfoundation/bitplatform/blob/develop/src/Utilities/Bsqlite/docs/migrating-boilerplate-from-besql.md) - the offline database of a project made from the template, file by file

## License

MIT, as is the bundled wa-sqlite, whose license ships next to the engine under `_content/Bit.Bsqlite/engine`. See
[LICENSE](https://github.com/bitfoundation/bitplatform/blob/develop/LICENSE).
