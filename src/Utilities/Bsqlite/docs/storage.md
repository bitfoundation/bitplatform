# Storage

A storage backend is a `IBsqliteStorageProvider`: a .NET half that declares capabilities and answers probes, and a JavaScript VFS the worker registers with the engine. The contract is public; the core consumes nothing else.

## First-party backends

| Name | Package | Engine build | Persistent | Notes |
|---|---|---|---|---|
| `opfs` | `Bit.Bsqlite.Storage.Opfs` | Synchronous | Yes | `OPFSCoopSyncVFS`. Fastest. Several connections and tabs, coordinated through Web Locks. No `ATTACH`. Worker-only, no COOP/COEP needed. |
| `indexeddb` | `Bit.Bsqlite.Storage.IndexedDb` | Async-capable | Yes | `IDBBatchAtomicVFS`. Works where OPFS does not (older Safari, some embedded views). Page size fixed at 4096. `cache_size` raised to 8 MiB so batch-atomic commits skip the journal. |
| `cache-storage` | `Bit.Bsqlite.Storage.CacheStorage` | Async-capable | Yes | 4096-byte pages in the Cache API. Imports a database left by [Bit.Besql](https://github.com/bitfoundation/bitplatform/tree/develop/src/Besql) on first open. |
| `in-memory` | `Bit.Bsqlite` (namespace `Bit.Bsqlite.Storage.InMemory`) | Synchronous | No | The engine's own memory VFS. Tests, demos, and a declared last resort. |

The engine build matters: a VFS that suspends to read storage (IndexedDB, Cache Storage) needs the JSPI build where the browser has it (Chrome 137+) and the Asyncify build otherwise. The synchronous build is the smallest and serves OPFS and in-memory. Exactly one is downloaded per session.

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

1. Implement `IBsqliteStorageProvider`: a stable `Name`, `Capabilities` (which engine build the VFS needs, whether data persists, and the rest), `VfsModule` pointing at an ES module, `Admin`, and `ProbeAsync` that reports unavailability rather than throwing.
2. Write the VFS against the base classes served from `_content/Bit.Bsqlite/engine/` (`VFS.js`, `FacadeVFS.js`, `WebLocksMixin.js`), imported relatively - `../Bit.Bsqlite/engine/VFS.js` from your package's own `_content` folder - so the application can be served under a sub-path. Export `register({ module, sqlite3, databaseName })` returning `{ vfsName, onOpenSql? }`.
3. Inherit `BsqliteStorageProviderConformanceTests` and `BsqliteStorageEngineConformanceTests` (namespace `Bit.Bsqlite.Storage.ConformanceTests`, package `Bit.Bsqlite.Testing`) in your MSTest project, with `[TestClass]` on each derived class. They check that the declared capabilities are coherent and that the admin surface behaves the way migration assumes.

Capability flags the core enforces for you: `SupportsMultiDatabaseTransactions` gates `ATTACH`, `CanChangePageSize` gates `PRAGMA page_size`, `IsPersistent` gates fallback. Declare the least capable answer when unsure.
