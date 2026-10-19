# Limits

What the browser cannot do, and how the library tells you.

| Limit | Behaviour |
|---|---|
| Synchronous ADO.NET and EF members (`Open`, `ExecuteReader`, `SaveChanges`, `ToList`, `EnsureCreated`) | Throw `BsqliteSynchronousApiNotSupportedException` naming the async overload. The worker cannot be waited on from the main thread. |
| `PRAGMA journal_mode = WAL` | Rejected on every backend. No web VFS provides the shared memory WAL needs. Journal mode is `DELETE` or `TRUNCATE`. |
| `ATTACH` | Rejected when the backend declares `SupportsMultiDatabaseTransactions = false` (OPFS). Open each database on its own session. |
| `PRAGMA page_size = …` | Rejected when the backend declares `CanChangePageSize = false` (IndexedDB, Cache Storage). |
| Command timeout and `Cancel()` | Accepted, not enforced. A cancellation token stops the caller waiting; the statement runs to completion in the worker. |
| Declared column types from `GetDataTypeName` | The engine is built with `SQLITE_OMIT_DECLTYPE`; the worker reports the storage class of the first row (`INTEGER`, `TEXT`, …). EF Core is unaffected. |
| `EnsureDeletedAsync` | Drops every view and table and runs `VACUUM`; it cannot unlink the file while the session holds it. Use `BsqliteDatabase.WhileClosedAsync` with `provider.Admin.DeleteAsync` to wipe storage. |
| Firefox and Safari | Supported through the Asyncify build for async backends; OPFS synchronous access handles need a recent version. The browser suite runs on Chromium by default; set `BSQLITE_E2E_BROWSER=firefox` or `webkit` to run it on either, after installing that browser for Playwright. |
| EF Core 8 and 9 | The SQLite provider translates `SUM` over a `decimal` from EF Core 9 on and `ORDER BY` a `decimal` from EF Core 10 on. Older providers throw `NotSupportedException` for those queries, in the browser exactly as on a server. |

Guards run on both sides: the .NET command layer rejects the statement before it leaves, and the worker rejects it again so a JavaScript caller cannot bypass the capability declaration.
