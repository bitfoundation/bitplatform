# Storage

A storage backend is a `IBsqliteStorageProvider`: a .NET half that declares capabilities and answers probes, and a JavaScript VFS the worker registers with the engine - or, for a Blazor Hybrid app's backend, a folder of SQLite files the native engine opens in-process. The contract is public; the core consumes nothing else - the first-party backends implement it exactly as a backend of your own would.

## First-party backends

All five ship in the `Bit.Bsqlite` package, each in a namespace of its own. Four serve the browser; the fifth serves Blazor Hybrid apps, where .NET runs natively.

| Name | Provider (namespace) | Engine build | Persistent | Notes |
|---|---|---|---|---|
| `opfs` | `BsqliteOpfsStorageProvider` (`Bit.Bsqlite.Storage.Opfs`) | Synchronous | Yes | `OPFSCoopSyncVFS`. Fastest. Several connections and tabs, coordinated through Web Locks. No `ATTACH`. Worker-only, no COOP/COEP needed. |
| `indexeddb` | `BsqliteIndexedDbStorageProvider` (`Bit.Bsqlite.Storage.IndexedDb`) | Async-capable | Yes | `IDBBatchAtomicVFS`. Works where OPFS does not (older Safari, some embedded views). Page size fixed at 4096. `cache_size` raised to 8 MiB so batch-atomic commits skip the journal. |
| `cache-storage` | `BsqliteCacheStorageProvider` (`Bit.Bsqlite.Storage.CacheStorage`) | Async-capable | Yes | 4096-byte pages in the Cache API. Imports a database left by [Bit.Besql](migrating-from-besql.md) on first open. |
| `in-memory` | `BsqliteInMemoryStorageProvider` (`Bit.Bsqlite.Storage.InMemory`) | Synchronous | No | The engine's own memory VFS. Tests, demos, and a declared last resort. |
| `file-system` | `BsqliteFileSystemStorageProvider` (`Bit.Bsqlite.Storage.FileSystem`) | None - the native SQLite the app brings | Yes | Blazor Hybrid only, registered and selected for it automatically. Plain SQLite files in the app's own folder. |

The engine build matters: a VFS that suspends to read storage (IndexedDB, Cache Storage) needs the JSPI build where the browser has it (Chrome 137+) and the Asyncify build otherwise. The synchronous build is the smallest and serves OPFS and in-memory. Exactly one is downloaded per session.

Registering a backend costs the browser nothing until it is used. Each one's JavaScript - its VFS module with the wa-sqlite VFS it wraps, and its probe and admin module - is a static web asset under `_content/Bit.Bsqlite/`, imported on demand: the worker imports the VFS of the selected backend only, and a backend's admin module loads only when it is probed or administered.

What each one declares - the core enforces every capability generically, never by a backend's name:

| | `opfs` | `indexeddb` | `cache-storage` | `in-memory` | `file-system` |
|---|---|---|---|---|---|
| Persistent | Yes | Yes | Yes | No | Yes |
| Multiple connections | Yes | Yes | Yes | No | Yes |
| Concurrent reads | No | No | Yes | No | Yes |
| Relaxed durability | No | Yes | No | No | No |
| `ATTACH` (multi-database transactions) | No | Yes | Yes | Yes | Yes |
| `PRAGMA page_size` | Yes | No | No | Yes | Yes |
| Execution contexts | Dedicated worker | Any in the browser | Any in the browser | Any in the browser | Native process |

### OPFS

The one to pick when you can: the only persistent backend on the synchronous engine build, which is the smallest of the three and needs neither JSPI nor Asyncify. Databases live in the origin private file system as real files.

It registers `OPFSCoopSyncVFS`, chosen over `AccessHandlePoolVFS` because more than one connection may be open and the `.db` stays a real file - exporting is just a read. Access handles are worker-only, so the engine stays in a dedicated worker; probe and admin use the async file API and run on the window, so availability checks and import/export work from component code. No COOP/COEP headers are required.

### IndexedDB

The backend that reaches browsers OPFS does not, and the only persistent one that can run outside a dedicated worker. Use it as the fallback behind OPFS, or on its own when reach matters more than download size. It registers `IDBBatchAtomicVFS`.

- **Page size** is fixed. The limit is declared, not documented and hoped for: the core reads `Capabilities` and rejects a `PRAGMA page_size` the backend cannot honour rather than letting it be silently ignored.
- **Concurrent reads:** `IDBBatchAtomicVFS` can support them, but `bit-bsqlite-idb-vfs.js` registers it without a lock policy, so `WebLocksMixin` applies its `exclusive` default and a read takes the same lock a write does. Readers queue behind each other, and the capability says so. Several connections are still supported - they serialize rather than run in parallel.
- **Download size:** the engine build is picked at runtime from the selected provider plus a JSPI probe. JSPI costs about 0.9% over the synchronous build; Asyncify costs about 107%. Browsers with JSPI pay almost nothing for this backend.

### Cache Storage

Databases are stored as 4096-byte pages in the Cache Storage API, committed through a journal. Batch-atomic writes are not offered, because the Cache API cannot commit several entries as one operation; durability comes from the journal instead.

Its reason to exist is migration: **a database left behind by Bit.Besql - in its `bit-Besql` cache - is imported losslessly on first open**, so an application moving off Bit.Besql keeps its users' data. [Moving from Bit.Besql](migrating-from-besql.md) walks through it. If you are not migrating from Bit.Besql, prefer OPFS or IndexedDB.

Nothing in selection, the transport or the worker knows it exists: it is built on the public storage contract alone, which is the proof that the contract is usable from outside the library - a backend of your own gets exactly what this one gets.

### In-memory

SQLite's own memory VFS: no JavaScript to load and nothing persisted. Every connection is its own worker with its own heap, so a second connection to the "same" database would be a different, empty one, and the backend declares no multiple connections. Use it for tests and demos, and as a last resort only behind `AllowNonPersistentFallback()`. Its `Admin` sees only images handed to it directly, not what a connection wrote.

### File system (Blazor Hybrid)

Where .NET runs natively - a Blazor Hybrid app on Android, iOS, Mac Catalyst, Windows or macOS - Bsqlite does not use the web view at all. `AddBitBsqliteServices` registers `BsqliteFileSystemStorageProvider` and ranks it ahead of whatever the selection names, so the registration a WebAssembly client uses serves the Hybrid app unchanged: the browser backends are reported unavailable without being probed ("needs .NET running in the browser"), and the database is a SQLite file opened in-process by the native SQLite engine.

- **The engine** is the app's to bring, as with `Microsoft.EntityFrameworkCore.Sqlite.Core`: add `SQLitePCLRaw.bundle_e_sqlite3` to the Blazor Hybrid app's project - NuGet picks the build for its platform - or keep the SQLitePCLRaw bundle it already uses, such as `bundle_e_sqlcipher`. Bit.Bsqlite brings none, so a server or WebAssembly app does not carry native binaries it never loads, and an app on SQLCipher does not end up with two bundles. Without one the backend reports itself unavailable and says what to add.
- **Which app is Hybrid.** Android, iOS and Mac Catalyst always are. On the desktop it is an app whose `IJSRuntime` is the web view's - MAUI, WPF and Windows Forms register it with their `Add...BlazorWebView` - not one that merely ships Microsoft.AspNetCore.Components.WebView, as a server referencing a library built on it may.
- **Where.** `{local application data}/{entry assembly name}/bit-bsqlite` by default - inside the app's sandbox on mobile, per user and per app on the desktop. `ConfigureFileSystemStorage(o => o.RootDirectory = ...)` moves it - to a drive's root, too. A database name is a path under the root (`AddDatabase("data/app.db")`), without `..` or a drive, and is held to what every platform's file system keeps as written: none of `<>:"|?*` or control characters, and no segment ending in a dot or a space. On Windows a `:` would name an alternate data stream of another file.
- **What.** Plain SQLite files in rollback-journal mode: `Admin` imports them byte for byte, exports them byte for byte once anything SQLite left next to the file - a write-ahead log, or a journal from a crash - is folded in, lists the SQLite files under the root and nothing else (an empty file, a database opened but never written, holds nothing and is not listed), and deletes a database with its journal. A file that arrives in WAL mode - stock EF Core creates them that way, so Bit.Besql's on a device is one - is switched back the first time it opens on its own, checkpointing the log into the file.
- **Bindings** live in a file next to the databases rather than in the web view's `localStorage`, so a database opens before the web view is up. A Hybrid app that ran Bsqlite in its web view before kept its databases on a browser backend there: their bindings in the web view's `localStorage` are still read, and such a database keeps opening where it is - existing data outranks preference - with `file-system` ranked above it, ready to migrate to (`AutomaticOnOpen` does it on the next open).
- **Two windows** of an app are two sessions on one file, coordinated the way two tabs are: in-process locks for EF's migration and first-use steps, and live queries in each window re-run on the other's writes. `Admin`'s delete, import and export need the database closed in every window - each through `BsqliteDatabase.WhileClosedAsync` - and throw while a session in the app still has the file open, rather than replace it under that session.
- **Not on a server.** The backend is never offered outside Blazor Hybrid: a server rendering a page has no business keeping a user's database, which belongs in their browser.

The engine opens the file, runs each batch and takes each snapshot on the thread pool, as the worker keeps it off the browser's main thread, so the async-only rule holds here too and the UI thread never waits on the disk. JavaScript extension modules do not load in-process; they are skipped with a warning.

## Selection

```csharp
.UseStorage(s => s
    .Prefer("opfs")
    .Fallback("indexeddb")
    .Fallback("in-memory")
    .AllowNonPersistentFallback()
    .WithMigrationMode(BsqliteStorageMigrationMode.KeepExisting))
```

Two rules shape everything:

1. **Existing data outranks preference.** A database created on IndexedDB stays on IndexedDB when OPFS later becomes available. If the backend that holds the data cannot be reached, the open fails rather than producing an empty database elsewhere. The binding is recorded in `IBsqliteStorageBindingStore` (in `localStorage` by default) only after the open succeeds. A database with no binding - after an application clears `localStorage` on sign-out, say - is looked for on every available persistent backend before it is treated as new; found, it opens there, is bound again (`Resolution.WasFoundWithoutBinding`), and a warning is logged.
2. **Nothing is substituted silently.** A fallback is logged. Falling back to a non-persistent backend needs `AllowNonPersistentFallback()`, and is never bound: the data is gone by the next load, so the binding would protect nothing, and would keep every later visit off a persistent backend that has come back. Total failure throws `BsqliteStorageUnavailableException` listing every candidate and why it was rejected.

`session.Resolution` records the decision: the chosen provider, the probe, every attempt, whether it was the first choice, and whether a better backend is available for an existing database.

## Migration

`BsqliteStorageMigrationMode.AutomaticOnOpen` moves a database to a better-ranked backend before opening it. `Manual` and `KeepExisting` leave it and report through `Resolution.BetterProviderAvailable`.

A migration is: probe the target, check quota headroom, export, verify the SQLite header, import, read the copy back and compare it byte for byte, flip the binding, delete the source. A failure before the flip leaves the source and the binding untouched. If deleting the source fails - another tab still holding an OPFS file open, say - the flip is undone when the source is still whole, and kept when it is not; the error is rethrown either way, and the binding says where the database now lives. `AutomaticOnOpen` resolves again after any failure, so the session always opens where the binding points. The demo's storage switcher drives the same `BsqliteStorageMigrator` by hand.

## Admin

`provider.Admin` works on databases as files, with no engine running: `ExistsAsync`, `ListAsync`, `DeleteAsync`, `ExportAsync`, `ImportAsync`. Close the session first when the database may be open:

```csharp
await Database.WhileClosedAsync(async ct =>
{
    await provider.Admin.ImportAsync("app.db", bytes, ct);
    await bindings.SetProviderNameAsync("app.db", provider.Name, ct);
});
```

For a backup of a database that stays open, use `connection.ExportAsync()` instead: it snapshots through `sqlite3_serialize` under the engine's own lock.

`BsqlitePersistence.RequestAsync(js)` asks the browser to exempt the origin from storage eviction. It is origin-wide, not per backend.

## Writing a provider

A backend for the browser:

1. Implement `IBsqliteStorageProvider`: a stable `Name`, `Capabilities` (which engine build the VFS needs, whether data persists, and the rest), `VfsModule` pointing at an ES module, `Admin`, and `ProbeAsync` that reports unavailability rather than throwing.
2. Write the VFS against the base classes served from `_content/Bit.Bsqlite/engine/` (`VFS.js`, `FacadeVFS.js`, `WebLocksMixin.js`), imported relatively - `../Bit.Bsqlite/engine/VFS.js` from your package's own `_content` folder - so the application can be served under a sub-path. Export `register({ module, sqlite3, databaseName })` returning `{ vfsName, onOpenSql? }`. Ship it as a static web asset of your package: like the first-party VFSes, it is imported only once your backend is selected.
3. Inherit `BsqliteStorageProviderConformanceTests` and `BsqliteStorageEngineConformanceTests` (namespace `Bit.Bsqlite.Storage.ConformanceTests`, package `Bit.Bsqlite.Testing`) in your MSTest project, with `[TestClass]` on each derived class. They check that the declared capabilities are coherent and that the admin surface behaves the way migration assumes.

A backend for Blazor Hybrid declares `SupportedContexts = BsqliteExecutionContexts.NativeProcess` and implements `IBsqliteFileStorageProvider.GetDatabasePath`; it needs no VFS module, and the session factory opens its databases with `BsqliteNativeTransport`. Register it with `AddStorageProvider<T>()` and selection ranks it as it does the first-party one. The engine kit runs against it on desktop .NET with a `BsqliteNativeTransport` opened on the provider.

Capability flags the core enforces for you: `SupportsMultiDatabaseTransactions` gates `ATTACH`, `CanChangePageSize` gates `PRAGMA page_size`, `IsPersistent` gates fallback. Declare the least capable answer when unsure.
