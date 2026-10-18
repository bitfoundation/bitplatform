# Bit.Bsqlite.Storage.CacheStorage

Cache Storage backend for [bit Bsqlite](https://www.nuget.org/packages/Bit.Bsqlite). Databases are
stored as 4096-byte pages in the Cache Storage API, committed through a journal.

Its reason to exist is migration: **a database left behind by [Bit.Besql](https://github.com/bitfoundation/bitplatform/tree/develop/src/Besql) - in its
`bit-Besql` cache - is imported losslessly on first open**, so an app moving off Bit.Besql keeps its
users' data.

## Install

```
dotnet add package Bit.Bsqlite.Storage.CacheStorage
```

```csharp
new BsqliteCacheStorageProvider(jsRuntime)   // ProviderName == "cache-storage"
```

## Capabilities

| | |
|---|---|
| Engine build | Async-capable — JSPI where available, Asyncify otherwise |
| Persistent | Yes |
| Multiple connections | Yes |
| Concurrent reads | Yes |
| Relaxed durability | No |
| Page size changeable | No |
| Execution contexts | Dedicated worker, window, shared worker, service worker |

Batch-atomic writes are not offered, because the Cache API cannot commit several entries as one
operation; durability comes from the journal instead. This backend was built entirely against the
public storage contract with no changes to the core — it is the proof that the contract is usable
from outside.

If you are not migrating from Bit.Besql, prefer `Bit.Bsqlite.Storage.Opfs` or
`Bit.Bsqlite.Storage.IndexedDb`.

MIT. Part of [bit Bsqlite](https://github.com/bitfoundation/bitplatform/tree/develop/src/Bsqlite).
