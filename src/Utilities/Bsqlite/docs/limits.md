# Limits

What the browser cannot do, what Bsqlite keeps the same on a device, and how the library tells you.

| Limit | Behaviour |
|---|---|
| Synchronous ADO.NET and EF members (`Open`, `ExecuteReader`, `SaveChanges`, `ToList`, `EnsureCreated`) | Throw `BsqliteSynchronousApiNotSupportedException` naming the async overload. The worker cannot be waited on from the main thread. |
| `PRAGMA journal_mode = WAL` | Rejected on every backend. No web VFS provides the shared memory WAL needs. Journal mode is `DELETE` or `TRUNCATE`. |
| `ATTACH` | Rejected when the backend declares `SupportsMultiDatabaseTransactions = false` (OPFS). Open each database on its own session. |
| `PRAGMA page_size = …` | Rejected when the backend declares `CanChangePageSize = false` (IndexedDB, Cache Storage). |
| Command timeout and `Cancel()` | Not enforced on a running statement: a cancellation token stops the caller waiting, and the statement runs to completion in the worker. The timeout does bound the wait for another context's transaction, after which the command fails with `BsqliteConcurrencyException`. |
| Contexts of one session | Share the session's connection and take turns: a transaction keeps every other context waiting until it ends - see [Concurrent contexts](getting-started.md#concurrent-contexts). Code holding a transaction that waits on another context gets "database is locked" once the timeout runs out. |
| Declared column types from `GetDataTypeName` | The engine is built with `SQLITE_OMIT_DECLTYPE`; the worker reports the storage class of the first row (`INTEGER`, `TEXT`, …). EF Core is unaffected. |
| `EnsureDeletedAsync` | Drops every view and table and runs `VACUUM`; it cannot unlink the file while the session holds it. Use `BsqliteDatabase.WhileClosedAsync` with `provider.Admin.DeleteAsync` to wipe storage. |
| Firefox and Safari | Supported through the Asyncify build for async backends; OPFS synchronous access handles need a recent version. The browser suite runs on Chromium by default; set `BSQLITE_E2E_BROWSER=firefox` or `webkit` to run it on either, after installing that browser for Playwright. |
| Blazor Hybrid | The same rules hold on a device - async members only, no WAL - so one code path serves both hosts. JavaScript extension modules (`AddExtensionModule`) do not load in-process; they are skipped with a logged warning, and SQL that calls their functions fails with `no such function`. |
| Interactive Server | Not supported. A user's database lives in their browser, or on their device in Blazor Hybrid; the `file-system` backend is never offered to a server, which would share one file between every user. See [Blazor Server](migrating-from-besql.md#blazor-server). |
| EF Core 8 and 9 | The SQLite provider translates `SUM` over a `decimal` from EF Core 9 on and `ORDER BY` a `decimal` from EF Core 10 on. Older providers throw `NotSupportedException` for those queries, in the browser exactly as on a server. |

Guards run on both sides: the .NET command layer rejects the statement before it leaves, and the worker rejects it again so a JavaScript caller cannot bypass the capability declaration.
