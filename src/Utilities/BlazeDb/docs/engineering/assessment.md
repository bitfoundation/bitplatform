# BlazeDb assessment and improvement rounds

This is an engineering document, not a user guide: the guide is the repository's `README.md`, which the demo
site and its MCP server serve. It records what BlazeDb was when this pass started, how it compares with the
engines people would otherwise reach for, where it fell short, and what each round changed and proved. The
live status of every requirement is in [requirements-matrix.md](requirements-matrix.md); the decisions are in
[decisions.md](decisions.md); the measurements in [benchmarks.md](benchmarks.md).

## 1. Baseline (2026-10-24, at `5c88a12434`)

| Check | Result |
| --- | --- |
| `dotnet build Utilities/BlazeDb/Bit.BlazeDb.slnx` | succeeded; 1 warning (CS8602, `Tests.Mcp/McpSurfaceTests.cs:57`, pre-existing) |
| `Bit.BlazeDb.Tests` (engine + EF Core provider) | 873 passed (291 x net8.0, net9.0, net10.0) |
| `Bit.BlazeDb.Tests.Mcp` | 70 passed |
| `Bit.BlazeDb.Tests.E2E` (Chromium) | 13 passed (run after the demo changes; the 13 pre-existing tests) |

### What BlazeDb already was

A mature engine with a deliberately small surface: tables of live objects keyed in a `Dictionary`, hash and
ordered secondary indexes (`SortedSet`) with unique and compound variants and full null handling, one ambient
transaction with an undo log, a write-ahead log with group commit, CRC-checked records, atomic manifests,
snapshots, torn-tail repair and quota awareness; OPFS and IndexedDB backends with Web Locks election,
WebCrypto encryption and read-only replica tabs; a Roslyn generator with sixteen diagnostics (eighteen now); an EF Core
provider that translates LINQ onto index plans; a demo site and an MCP server. Persistence and recovery had
been hardened carefully - most baseline tests are about failure modes.

Its explicit non-goals (the demo's roadmap page) were kept as constraints for this pass: SQL, multi-writer
concurrency, server-side scale, sync and conflict resolution, lazy loading from storage, full-text search.

## 2. Competitive research

Based on the public documentation of each project as of 2026; capabilities, not benchmarks. "Blazor WASM"
means runs inside a Blazor WebAssembly app without a separate JavaScript database.

| Capability | BlazeDb (after this pass) | SQLite (incl. wasm builds) | LiteDB | Realm .NET | IndexedDB (+ Dexie) | DuckDB-wasm / PGlite |
| --- | --- | --- | --- | --- | --- | --- |
| Data model | typed objects, generated schema | relational | BSON documents | live objects | JS object stores | relational (analytical / Postgres) |
| Query interface | typed primitives + EF Core LINQ | SQL | LINQ + expression SQL | LINQ subset | key ranges, cursors | SQL |
| Runs in Blazor WASM | yes, managed code, no interop on reads | via JS interop / worker | not designed for it (file I/O) | no | via JS interop | via JS interop |
| Secondary, unique, compound indexes | yes | yes | yes (expression indexes) | yes | yes (+ multiEntry) | yes |
| Prefix / range / keyset paging | yes | yes | yes | yes | yes (key ranges) | yes |
| Full-text search | no (non-goal) | FTS5 | no | full-text index | no (Dexie add-ons) | extensions |
| Transactions, savepoints | yes, savepoints | yes, savepoints | yes | write transactions | auto-committing scoped transactions | yes |
| Optimistic concurrency primitive | `TryUpdate(row, comparisonValue)` | via SQL | no | no | no | via SQL |
| Change notifications | per table and database, net per commit | update hooks / session extension (C API) | no | fine-grained collection notifications | none built in (Dexie `liveQuery`) | PGlite live queries |
| Live queries | `Watch` with relevance filter | no | no | live results | Dexie `liveQuery`, RxDB | PGlite extension |
| Schema version + migrations | `UserVersion`, `AddMigration` | `user_version`, app-run SQL | `UserVersion` | `SchemaVersion` + callback | `versionchange` upgrade | app-run SQL |
| Backup / export | single checksummed file | backup API, file copy | file copy | `WriteCopy` | none built in (Dexie export add-on) | export functions |
| Encryption | AES-GCM per frame (WebCrypto in browser) | SEE / SQLCipher (separate) | password (AES) | AES-256 | no | no |
| Multi-tab | Web Locks writer + replicas | app-managed | n/a | n/a | shared, versionchange events | app-managed |
| Sync | change feed only (non-goal) | no | no | Device Sync (deprecated) | RxDB replication | ElectricSQL (PGlite) |
| AOT / trimming | source-generated, no reflection | native | reflection-based mapper | weaver | n/a | n/a |

What differentiates BlazeDb: reads that are method calls on live objects (no interop, no deserialization),
source-generated schemas that are AOT-safe, and durability that stays off the read path. What users of the
others expect and BlazeDb lacked at the baseline - and so drove the priorities below - were change
notifications and live queries (Realm, Dexie, PGlite), a stored schema version with migrations (every one of
them), backups (SQLite, Realm), savepoints and atomic bulk writes (SQLite), optimistic concurrency, keyset
paging and prefix search, and positional records as tables (every .NET object store that binds
constructors). Full-text search, SQL and sync stay out of scope by the project's own decision.

## 3. Gap analysis at the baseline

Correctness (P0), found by reading the code and confirmed by tests written before the fix:

1. A selector or comparer that throws part-way through index maintenance left the table and its indexes
   disagreeing (`SetEntry` updated some stores and not others).
2. A rollback whose undo threw left the transaction registered, so every later write joined a transaction
   nobody could commit; a partial `RollbackTo` left the op list describing writes memory no longer held.
3. Disposing the database with a transaction open left it applied in memory and committable into a WAL
   that would never flush; commit/rollback checked "is this the active transaction" outside the lock, racing
   dispose.
4. A streaming query resumed after yielding could silently mix states if another handler wrote meanwhile
   (dictionary enumeration tolerates removals).
5. A commit larger than the 256 MB record limit was journaled, then treated as torn on replay - losing it
   and every commit after it.
6. `BlazeDbBufferWriter` overflowed `int` past about 1 GB and failed with "Destination is too short".

Capability gaps (P1/P2): no change notifications or live queries; no public savepoints or composable atomic
units; no Try*/optimistic-concurrency/bulk writes; inclusive-only range bounds (no keyset paging); no prefix
scans or per-value counts; no query explanation; no integrity check; no backup/restore; no stored schema
version or migrations; positional records and get-only properties refused (BLZ004); no `DateOnly`,
`TimeOnly`, `Half`, `Int128`, set or interface collections; no index on computed values.

Hypotheses rejected by measurement: `SortedSet` views being O(k) (ADR-003).

## 4. Rounds

| Round | Theme | Changes | Verification |
| --- | --- | --- | --- |
| 1 | Baseline | Reconnaissance, baseline build/tests, `IndexBenchmarks` and `PersistenceBenchmarks`, research | baseline numbers recorded; B+tree hypothesis rejected |
| 2 | Core correctness | Atomic index maintenance (ADR-004), null row/key errors, `Version`, `VerifyIntegrity`, dispose semantics, stream consistency (ADR-011) | `CoreCorrectnessTests` |
| 3 | Transactions | Public savepoints, `RunInTransaction`, active-transaction checks under the lock, robust rollback, Try*/optimistic/bulk writes | `TransactionCompositionTests`, `BulkAndConditionalWriteTests`, `ModelBasedTests` (12 seeds x 600 steps; mutation-checked) |
| 4 | Query and indexing | Exclusive bounds, `StartsWith`, `CountBy(index)`, `ThenBy`, `Explain` | `IndexQueryTests`; model test randomizes exclusivity |
| 5 | Data modeling | New scalars and collections, constructor binding, index-only properties; EF mapping and original values; MCP inspector | `DataModelingTests`, EF `ModelShapeTests`, MCP inspector tests |
| 6 | Performance | In-place WAL encoding, presized snapshots, buffer growth fix, write benchmark fix | A/B and before/after benchmarks (benchmarks.md) |
| 7 | Persistence | Export/import, `UserVersion`, migrations (ADR-006 - 008) | `BackupAndMigrationTests` |
| 8 | Reactive | `Changed` events, live queries (ADR-005) | `ChangeNotificationTests`; model test shadow from notifications |
| 9 | Developer experience | README sections for every feature, README examples run as tests, demo `/changes` page, stale demo text corrected, roadmap updated | `ReadmeExampleTests`, E2E `DemoPageTests` and `ChangeFeedTests`, MCP suite |
| 10 | Hardening | Independent review of the engine diff (fresh reviewer); its six findings fixed: committed EF saves reported as failed when a handler threw (`BlazeDbChangeHandlerException`), an atomic unit keeping writes after rolling back past its start (clamped unit marks), the metadata table leaking into `Changed`, `required` constructor-bound members not compiling, indexed get-only auto-properties silently unstored (BLZ017, BLZ018), and a failed undo leaving unjournaled writes for a checkpoint (database refuses writes until reopened); full verification, final benchmarks, documentation audit | `ReviewRegressionTests`; section 5 |

## 5. Final verification

Run after the last change, from `src` (benchmarks in [benchmarks.md](benchmarks.md)):

| Check | Result |
| --- | --- |
| `dotnet build Utilities/BlazeDb/Bit.BlazeDb.slnx` (Debug) | succeeded; only the pre-existing CS8602 warning |
| `dotnet build` of `Bit.BlazeDb` and `Bit.BlazeDb.EntityFrameworkCore` in Release (trim and AOT analyzers on) | succeeded, no warnings |
| `dotnet test --project Utilities/BlazeDb/Tests/Bit.BlazeDb.Tests` | 1,236 passed (412 x net8.0, net9.0, net10.0), up from 873 |
| `dotnet test --project Utilities/BlazeDb/Tests/Bit.BlazeDb.Tests.Mcp` | 70 passed |
| `dotnet test --project Utilities/BlazeDb/Tests/Bit.BlazeDb.Tests.E2E` (Chromium) | 14 passed, up from 13 |

## 6. Remaining risks and follow-up

- **Concurrent readers** are unsupported by design (ADR-001); a Blazor Server host must serialize access.
- **Nested objects** are not stored (ADR-010). The next candidate if users ask: generated serializers for
  `[BlazeDbEmbedded]` types plus a deep value comparer in the EF provider.
- **Checkpoint pause**: a checkpoint encodes the snapshot under the write lock - about 4 ms for 100,000
  small rows on a desktop, several times that in WebAssembly. Incremental or copy-on-write snapshots would
  remove it at the cost of write-path overhead.
- **Notifications are synchronous** on the committing thread; a slow handler slows the writer. Documented.
- **Untested at scale**: the > 256 MB commit refusal and the > 1 GB buffer refusal are verified by review
  only; allocating that much in CI is not practical.
- **EF provider**: strict `<`/`>` comparisons still read an inclusive range and filter the boundary rows;
  correct, slightly more work than an exclusive bound.
- **Browser coverage**: E2E runs in Chromium only.
