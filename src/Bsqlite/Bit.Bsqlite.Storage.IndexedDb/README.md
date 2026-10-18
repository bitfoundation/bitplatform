# Bit.Bsqlite.Storage.IndexedDb

IndexedDB storage for [bit Bsqlite](https://www.nuget.org/packages/Bit.Bsqlite), backed by
wa-sqlite's `IDBBatchAtomicVFS`.

This is the backend that reaches browsers OPFS does not, and the only one that can run outside a
dedicated worker. Use it as the fallback behind OPFS, or on its own when reach matters more than
download size.

## Install

```
dotnet add package Bit.Bsqlite.Storage.IndexedDb
```

```csharp
new BsqliteIndexedDbStorageProvider(jsRuntime)   // ProviderName == "indexeddb"
```

## Capabilities

| | |
|---|---|
| Engine build | Async-capable — JSPI where available, Asyncify otherwise |
| Persistent | Yes |
| Multiple connections | Yes |
| Concurrent reads | No — see below |
| Relaxed durability | Yes |
| Page size changeable | No |
| Execution contexts | Dedicated worker, window, shared worker, service worker |

The page-size limit is declared, not documented-and-hoped: the core reads `Capabilities` and rejects
a `PRAGMA page_size` that the backend cannot honour, rather than letting it be silently ignored.

**On concurrent reads:** `IDBBatchAtomicVFS` can support them, but `bit-bsqlite-idb-vfs.js` registers it without
a lock policy, so `WebLocksMixin` applies its `exclusive` default and a read takes the same lock a
write does. Readers therefore queue behind each other, and the capability says so. Multiple
*connections* are still supported — they serialize rather than run in parallel.

**On download size:** the engine build is picked at runtime from the selected provider plus a JSPI
probe. JSPI costs about 0.9% over the synchronous build; Asyncify costs about 107%. Browsers with
JSPI pay almost nothing for this backend.

MIT. Part of [bit Bsqlite](https://github.com/bitfoundation/bitplatform/tree/develop/src/Bsqlite).
