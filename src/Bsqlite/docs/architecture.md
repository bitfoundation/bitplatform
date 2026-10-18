# Architecture and performance

```
Blazor main thread                       Dedicated worker
──────────────────────                   ────────────────────────────────
EF Core (stock SQLite provider)
  │
BsqliteConnection / Command / Batch
  │  BsqliteCommandRequest[]
BsqliteWorkerTransport
  │  byte[] (binary wire format)
bit-bsqlite-host.js  ── postMessage, transferred ──▶  bit-bsqlite-worker.js
  ◀── postMessage, transferred ──                        │ statement cache, update hook
                                                          wa-sqlite (SQLite 3.53 WASM)
                                                          │
                                                          VFS: OPFS / IndexedDB / Cache / memory
```

## Why a worker

OPFS synchronous access handles - the fastest storage the web has - exist only in workers. Putting the engine in a dedicated worker also keeps SQLite's work off the UI thread. The price is that every call is asynchronous, which is why the synchronous ADO.NET surface throws rather than blocks.

One worker owns one database. A second `BsqliteDatabase` or a second session starts a second worker; the VFSes coordinate through Web Locks.

## The binary wire format

Everything the transport sends or receives is one `byte[]`:

- A **batch** is magic, version, count, then per request the SQL, a result-kind byte, a row limit, and parameters as name, SQLite storage-class tag, and payload.
- A **reply** is magic, version, status, then either an error (message, result code, name) or one result per request: columns with their storage class, records affected, last insert rowid, and rows as tagged cells.

Integers are eight bytes, doubles eight bytes, strings and blobs length-prefixed. Blazor marshals byte arrays across JS interop without JSON or base64, the host transfers the buffer to the worker instead of cloning it, and the worker writes rows straight from `sqlite3_column_*` into the reply buffer with no intermediate object graph. A failure is a reply with an error status, so the SQLite result code survives the boundary and becomes a typed exception.

`BsqliteWireFormat` (C#) and `bit-bsqlite-wire.js` are the two halves; the format version is checked on every reply. The host module, the worker, and every module they import are loaded with the assembly's version in the query string, so a browser cannot pair new .NET with JavaScript it cached under an earlier release - and if something else serves a stale copy anyway, the version check says so.

JavaScript callers get the same information in a structured-clone-friendly tagged shape (`{ t: [...], v: [...] }` rows) through `host.execute`.

## The statement cache

The worker keeps up to 64 prepared statements keyed by SQL text, lent out and reset after each use. EF Core sends the same text over and over with different parameters; preparing is a parse and a plan, resetting is nearly free. Only single-statement texts are cached. A DDL statement clears the cache, since column metadata is read before stepping and must reflect the new schema.

## Change tracking

`sqlite3_update_hook` names every table a row changed in, through triggers and cascades; the statement text covers DDL and truncating deletes, which the hook does not see. `sqlite3_commit_hook` says when those rows became visible. The worker accumulates tables until the connection is back in autocommit and then notifies - this tab's .NET transport and, over a `BroadcastChannel`, every other tab - only if a commit happened. See [Live queries](live-queries.md#how-changes-are-detected). Affected-row counts honour `sqlite3_stmt_readonly`, so an `INSERT` followed by a `SELECT` reports the insert once - the rule Microsoft.Data.Sqlite applies.

## Open-time configuration

Every connection runs `PRAGMA foreign_keys = ON` unless told otherwise, because the wa-sqlite build defaults to off while Microsoft.Data.Sqlite defaults to on, and a cascade rule that silently does nothing is a data bug. Providers add their own statements (IndexedDB raises `cache_size`), and applications add theirs through `OnOpen`.

## Where the time goes

Per round trip, roughly: Blazor interop and JSON for the envelope, the worker hop, the SQL itself, and encoding the result. The binary format removes the JSON and base64 share entirely; the statement cache removes the parse; batching amortises the hop. What remains is storage: OPFS reads are synchronous memory copies, IndexedDB and Cache Storage pay an async transaction per page group. Choose OPFS first where it is available.

The manual benchmark page, `TestPages/benchmarks.html` in `Tests/Bit.Bsqlite.Tests.E2E`, measures cold open, a 1 000-row read on both encodings, a 100-row insert batch, and repeated parameterised statements per backend; the test project's README says how to open it.
