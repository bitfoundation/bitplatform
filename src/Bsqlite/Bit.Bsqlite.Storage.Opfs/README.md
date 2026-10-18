# Bit.Bsqlite.Storage.Opfs

OPFS storage for [bit Bsqlite](https://www.nuget.org/packages/Bit.Bsqlite), backed by wa-sqlite's
`OPFSCoopSyncVFS`. Databases live in the origin private file system as real files.

Pick this one when you can: it is the only backend that runs on the **synchronous** engine build,
which is the smallest of the three and needs neither JSPI nor Asyncify.

## Install

```
dotnet add package Bit.Bsqlite.Storage.Opfs
```

```csharp
new BsqliteOpfsStorageProvider(jsRuntime)   // ProviderName == "opfs"
```

## Capabilities

| | |
|---|---|
| Engine build | Synchronous — the smallest download |
| Persistent | Yes |
| Multiple connections | Yes |
| Concurrent reads | No |
| Relaxed durability | No |
| Page size changeable | Yes |
| Execution contexts | Dedicated worker only |

Access handles are worker-only, so the engine stays in a dedicated worker. Probe and admin use the
async file API and can run on the window, so availability checks and import/export work from
component code. No COOP/COEP headers required.

Chosen over `AccessHandlePoolVFS` because more than one connection may be open and the `.db` stays a
real file — exporting is just a read.

MIT. Part of [bit Bsqlite](https://github.com/bitfoundation/bitplatform/tree/develop/src/Bsqlite).
