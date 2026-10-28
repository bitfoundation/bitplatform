# BlazeDb requirements traceability matrix

Engineering tracking document; see [assessment.md](assessment.md) for the reasoning and
[decisions.md](decisions.md) for the decisions. Paths are relative to `src/Utilities/BlazeDb`; test classes are
in `Tests/Bit.BlazeDb.Tests` unless marked E2E (`Tests/Bit.BlazeDb.Tests.E2E`) or MCP
(`Tests/Bit.BlazeDb.Tests.Mcp`).

**Status values**: `verified` (implemented, and the named tests prove the acceptance criteria), `implemented`
(code exists, verification incomplete - the gap is named), `partial`, `missing`, `out of scope` (with the
reason), `blocked` (external constraint). **Pri**: P0 correctness/data loss, P1 core expectation, P2
valuable, P3 nice to have. **New** marks work done in this pass; everything else was present at the baseline
(`5c88a12434`) and re-verified.

## A. Core architecture

| Requirement | Status | Pri | Implementation | Evidence | Limitations |
| --- | --- | --- | --- | --- | --- |
| Separation of engine, tables, indexes, transactions, WAL, storage, query | verified | P1 | `BlazeDbDatabase`, `Tables/`, `Indexing/`, `Transactions/`, `Wal/`, `Storage/`, `Querying/` | whole suite | - |
| Explicit ownership: database does not own storage; transaction owned by database | verified | P1 | `BlazeDbDatabase.DisposeAsync`, `BlazeDbTransaction` | `DurabilityTests`, `CoreCorrectnessTests.Disposing_With_A_Transaction_Open_Rolls_It_Back` | - |
| Lifecycle: dispose idempotent and race-safe; dispose ends an open transaction (**new**) | verified | P0 | `DisposeAsync` (`Interlocked`), `BlazeDbTransaction.MarkCompleted` | `CoreCorrectnessTests.Disposing_*` | - |
| Thread safety: concurrent writers serialized | verified | P1 | `SyncRoot` around every write | `TransactionCompositionTests.Concurrent_Writers_Each_Get_An_Atomic_Unit`, `ChangeNotificationTests.Concurrent_Writers_Lose_No_Notification` | - |
| Concurrent readers with writers | out of scope | - | - | - | ADR-001: single writer, lock-free reads; documented in README "Threading model" |
| Deterministic, reproducible tests | verified | P1 | seeded randomized tests | `ModelBasedTests` (12 fixed seeds) | - |
| Minimal hot-path overhead | verified | P1 | - | `benchmarks.md` (point get 1.1 ns, no allocation) | - |

## B. Data modeling and type safety

| Requirement | Status | Pri | Implementation | Evidence | Limitations |
| --- | --- | --- | --- | --- | --- |
| Strongly typed tables from attributes, no runtime reflection | verified | P1 | `Bit.BlazeDb.Generators` | `SourceGeneratorTests`, MCP `McpInspectorTests` | - |
| Primary keys (scalar) | verified | P1 | `[BlazeDbKey]` | `TableCrudTests` | Composite primary keys: use a compound unique index (documented pattern) |
| Generated / auto-increment keys | missing | P3 | - | - | Keys are the application's (EF convention `ValueGenerated.Never`); `Guid.NewGuid()` / `CreateVersion7()` cover it |
| Scalars incl. `DateOnly`, `TimeOnly`, `Half`, `Int128`, `UInt128` (**new**) | verified | P1 | `BlazeDbTypeMap`, `BlazeDbBufferWriter/Reader` | `DataModelingTests` (round trips, NaN bits, corrupt values) | - |
| Nullable scalars, enums, `byte[]`, date/time | verified | P1 | generator | `SourceGeneratorTests`, `SerializationTests` | - |
| Collections: `List<T>`, `T[]`, list interfaces, `HashSet<T>`/`ISet<T>`/`IReadOnlySet<T>` (**new**) | verified | P1 | `CollectionCategory`, `SetCollection` | `DataModelingTests`; EF `ModelShapeTests.A_Set_Changed_In_Place_Is_Saved` | Elements must be non-nullable scalars |
| Records, positional records, constructor-bound get-only properties (**new**) | verified | P1 | `ChooseConstructor`, emitter | `DataModelingTests`, EF `ModelShapeTests`, MCP `McpInspectorTests` | - |
| Index-only computed properties (**new**) | verified | P2 | `BlazeDbPropModel.IndexOnly` | `DataModelingTests.Index_Only_*` | Must be recomputed from stored values; same live-row rule |
| Nested objects / embedded documents | out of scope (deferred) | P2 | - | - | ADR-010; alternatives documented in README "Modeling data" |
| Unique constraints (single, compound; null semantics) | verified | P0 | `ValidateUnique` | `ConstraintTests`, `ModelBasedTests` | - |
| Required fields / validation hooks | missing | P3 | - | - | Validate before writing; the serializer refuses a null non-nullable string at commit when persisting |
| Referential integrity / cascades | out of scope | - | - | - | Relationships are key properties; documented |
| Schema evolution (field numbers, tolerant reader, BLZ013) | verified | P0 | generator | `SchemaEvolutionTests` | - |
| Data migrations with a stored version (**new**) | verified | P1 | `UserVersion`, `AddMigration`, `BlazeDbMetaTable` | `BackupAndMigrationTests` | - |
| Compile-time diagnostics for every unsupported shape | verified | P1 | `BlazeDbDiagnostics` BLZ001-018 (BLZ017, BLZ018 **new**) | MCP `McpInspectorTests` (every catalog sample) | BLZ004 now means "no constructor rows can be built with" |
| `required` members taken by the constructor (**new**) | verified | P2 | emitter sets them in the initializer too | `ReviewRegressionTests.A_Required_Member_*` | - |

## C. CRUD and bulk

| Requirement | Status | Pri | Implementation | Evidence | Limitations |
| --- | --- | --- | --- | --- | --- |
| Insert/Get/Update/Upsert/Delete/Contains | verified | P1 | `BlazeDbTable` | `TableCrudTests`, `ModelBasedTests` | - |
| Insert-if-absent, update-if-present (**new**) | verified | P1 | `TryInsert`, `TryUpdate(row)` | `BulkAndConditionalWriteTests` | - |
| Optimistic concurrency (**new**) | verified | P1 | `TryUpdate(row, comparisonValue)`, `TryDelete(key, comparisonValue)` | `BulkAndConditionalWriteTests`, `ModelBasedTests` | Compares by `EqualityComparer<T>.Default` |
| Atomic bulk insert/upsert/delete (**new**) | verified | P1 | `InsertMany`, `UpsertMany`, `DeleteMany`, `DeleteWhere`, `DeleteAll` | `BulkAndConditionalWriteTests`, `ModelBasedTests`, `ReviewFollowUpTests.Bulk_Writes_*` | Journaled as ordinary ops; input read in full first (ADR-013) |
| Null row / null key handling (**new**) | verified | P1 | `KeyOf` | `CoreCorrectnessTests.A_Null_*` | - |
| Live-object mutation safety | verified | P0 | `EnsureNotMutatedInPlace`, `UpdateInPlace`, `BlazeDbStaleRowException` | `InPlaceWriteTests` | Documented rule: replace, don't mutate |

## D. Query engine

| Requirement | Status | Pri | Implementation | Evidence | Limitations |
| --- | --- | --- | --- | --- | --- |
| Typed predicates, composition, paging | verified | P1 | `BlazeDbQuery` | `QueryTests`, `IndexQueryTests` | - |
| Multi-key ordering (**new** `ThenBy`) | verified | P2 | `BlazeDbQuery.ThenBy` | `IndexQueryTests.ThenBy_*` | - |
| Keyset pagination via exclusive bounds (**new**) | verified | P1 | `BlazeDbBound<T>.Exclusive` | `IndexQueryTests`, `ModelBasedTests` (random exclusivity) | - |
| Prefix search (**new**) | verified | P2 | `StartsWith`, `UseIndexPrefix` | `IndexQueryTests.StartsWith_*` | Ordinal only; refused under a custom comparer |
| Aggregates, grouping, distinct, joins | verified | P1 | LINQ to Objects over reads; EF provider | EF `QueryTests` | No engine-native operators (rows are objects; nothing to gain) |
| Per-value counts / facets (**new**) | verified | P2 | `CountBy(hashIndex)` | `IndexQueryTests.CountBy_*` | Hash indexes |
| Streaming with cancellation and cooperative yielding | verified | P1 | `ExecuteAsync` | `StreamingQueryTests` | - |
| Stream consistency under foreign writes (**new**) | verified | P0 | `ExecuteAsync` version check | `CoreCorrectnessTests.A_Stream_*`, `ReviewFollowUpTests.A_Stream_*` | Throws; ADR-011 |
| Query plan explanation (**new**) | verified | P2 | `BlazeDbQuery.Explain()` | `IndexQueryTests.Explain_*`, `ReadmeExampleTests` | Never prints values |
| LINQ provider with index selection | verified | P1 | EF Core provider | `IndexSelectionTests`, `IndexExactnessTests` | Strict `<`/`>` still use an inclusive range plus a residual filter |
| Query caching | verified | P2 | EF `BlazeDbDelegateCache` | EF tests | - |
| SQL | out of scope | - | - | - | Product principle |

## E. Indexing

| Requirement | Status | Pri | Implementation | Evidence | Limitations |
| --- | --- | --- | --- | --- | --- |
| Hash and ordered secondary indexes, unique, compound, nulls | verified | P1 | `Indexing/` | `ConstraintTests`, `ModelBasedTests` | - |
| Index maintenance through insert/update/delete/rollback/bulk/replay | verified | P0 | `SetEntry`, `RemoveEntry`, ops | `ModelBasedTests` (7,200 steps x 3 TFMs), `TransactionTests` | - |
| Atomic index maintenance under a throwing selector/comparer (**new**) | verified | P0 | unwinding in `SetEntry`/`RemoveEntry` | `CoreCorrectnessTests` | - |
| Index consistency verification (**new**) | verified | P1 | `VerifyIntegrity`, `BlazeDbIntegrityIssue` | `CoreCorrectnessTests.VerifyIntegrity_*`, used by most new tests | - |
| Range scans O(log n) start | verified | P1 | `SortedSet` views | `IndexBenchmarks` 10k vs 100k (ADR-003) | - |
| Index statistics / automatic recommendations | missing | P3 | - | - | `CountBy(index)` gives per-value counts; no planner statistics (EF selector is rule-based) |

## F. Transactions and concurrency

| Requirement | Status | Pri | Implementation | Evidence | Limitations |
| --- | --- | --- | --- | --- | --- |
| Commit/rollback/dispose-without-commit, atomic in memory and log | verified | P0 | `BlazeDbTransaction` | `TransactionTests`, `CommitFailureTests`, `DurabilityTests` | - |
| Savepoints (**new** public) | verified | P1 | `CreateSavepoint`, `RollbackTo(BlazeDbSavepoint)` | `TransactionCompositionTests`, `ModelBasedTests` | - |
| Composable atomic units (**new**) | verified | P1 | `RunInTransaction` | `TransactionCompositionTests` | Synchronous only (ADR-002) |
| Nested `BeginTransaction` policy | verified | P1 | explicit rejection | `TransactionTests.Nested_Transactions_Are_Rejected` | - |
| Rollback robust to a failing undo (**new**) | verified | P0 | `RollbackTransaction` finally, per-op truncation; a database with storage refuses writes, checkpoints and exports after a failed undo, one without storage carries on and `VerifyIntegrity` reports it; the failing unit's own exception propagates | `ReviewRegressionTests.A_Rollback_That_Cannot_Undo_*`, `Disposing_With_A_Transaction_Whose_Undo_Fails_*`, `ReviewFollowUpTests.A_Failed_Unit_*`, `Without_Storage_A_Failed_Undo_*` | Reopen to recover |
| Atomic unit survives rolling back past its own start (**new**) | verified | P0 | clamped unit marks (`BeginUnit`/`RollbackUnit`) | `ReviewRegressionTests.A_Unit_That_Rolls_Back_*` | - |
| Commit/rollback racing dispose (**new**) | verified | P0 | `VerifyActive` under the lock | `CoreCorrectnessTests.Disposing_*` | - |
| Isolation | verified (documented weak) | P1 | single writer | README "Transactions" | Reads see the open transaction's writes; no snapshot isolation |
| Async-flow ownership of the ambient transaction | out of scope | - | - | - | ADR-002 |
| Commits too large for a log record refused at commit (**new**) | implemented | P0 | `AppendCommit` | code review | A >256 MB test is impractical in CI |

## G. Memory and performance

| Requirement | Status | Pri | Implementation | Evidence | Limitations |
| --- | --- | --- | --- | --- | --- |
| Allocation-free reads | verified | P1 | - | `ReadBenchmarks` (0 B point get, count) | - |
| Fewer copies on commit (**new**) | verified | P2 | in-place WAL record encoding | `benchmarks.md` A/B | Time neutral (within noise) |
| Snapshot without double buffering (**new**) | verified | P1 | presized buffer, no `ToArray` | `PersistenceBenchmarks` (100k: 13.4 MB to 5.6 MB, 6.1 to 4.1 ms) | - |
| Buffers past 1 GB (**new**) | implemented | P1 | `BlazeDbBufferWriter.Grow` | found by benchmark; code review | Not unit tested (needs > 1 GB) |
| Checkpoint blocks writers while the snapshot is encoded | implemented (limit) | P2 | - | `PersistenceBenchmarks` (100k rows: 4 ms desktop) | Documented; copy-on-write not justified |
| Memory estimates / diagnostics | missing | P3 | - | - | - |

## H. Persistence and recovery

| Requirement | Status | Pri | Implementation | Evidence | Limitations |
| --- | --- | --- | --- | --- | --- |
| Ephemeral mode | verified | P1 | `Storage = null` | suite | - |
| WAL + snapshots + atomic manifest + CRC + torn-tail repair | verified | P0 | `BlazeDbWalManager` | `DurabilityTests`, `ChecksumTests` | - |
| Backup / restore, single-file export/import (**new**) | verified | P1 | `ExportAsync`, `ImportAsync` | `BackupAndMigrationTests` (damaged files, overwrite refusal, stale log), `ReviewFollowUpTests` (export after a failed undo, import from a memory stream) | Import holds the whole file in memory, once |
| Format versioning, older snapshot formats | verified | P1 | `BlazeDbSnapshotFormat` | `DurabilityTests` | - |
| Quota awareness, eviction risk documented | verified | P1 | `IBlazeDbQuotaAwareStorage` | `QuotaTests`; README "Persistence guarantees" | Browser eviction cannot be prevented |
| Encryption at rest | verified | P2 | `BlazeDbEncryptedStorage` | `EncryptionTests` | No rollback protection, no key rotation (documented) |

## I. Runtimes

| Requirement | Status | Pri | Implementation | Evidence | Limitations |
| --- | --- | --- | --- | --- | --- |
| Blazor WebAssembly (OPFS, IndexedDB, Web Locks, tabs) | verified | P1 | `Browser/` | E2E `PersistenceTests`, `BrowserModuleTests`, `DemoPageTests`, `DemoScenarioTests`, `ChangeFeedTests` | Chromium, Firefox and WebKit in CI, against the published demo; no OPFS in WebKit's Playwright contexts |
| net10.0 / net11.0 | verified | P1 | multi-targeting | unit suite on both | - |
| Blazor Server / ASP.NET Core | implemented (documented) | P2 | file storage | README "Hosting models" | Application serializes access (ADR-001) |
| MAUI / desktop | implemented | P2 | file storage | `DurabilityTests` (file backend) | Not run on devices here |

## J. Integrity and mutability

| Requirement | Status | Pri | Implementation | Evidence | Limitations |
| --- | --- | --- | --- | --- | --- |
| Ownership of returned rows documented | verified | P0 | README "Rows are live objects" | `InPlaceWriteTests` | - |
| Detection of in-place mutation (**new** diagnostic) | verified | P1 | `VerifyIntegrity` | `CoreCorrectnessTests` | Detects; cannot prevent |
| Defensive handling of user callbacks (**new**) | verified | P0 | unwinding; per-subscriber invocation | `CoreCorrectnessTests`, `ChangeNotificationTests` | - |

## K. Events and reactive access

| Requirement | Status | Pri | Implementation | Evidence | Limitations |
| --- | --- | --- | --- | --- | --- |
| Table and database change notifications (**new**) | verified | P1 | `Changed` events, `BlazeDbChangeCollector` | `ChangeNotificationTests`, `ModelBasedTests` shadow, E2E `ChangeFeedTests`, `ReviewFollowUpTests` (per-thread delivery, whole commits only) | Synchronous on the committing thread; each thread delivers its own commits (ADR-005) |
| Handler failure reported without misreporting the commit (**new**) | verified | P0 | `BlazeDbChangeHandlerException`; EF save completes, then rethrows from `SavedChanges` | `ReviewRegressionTests.A_Throwing_Handler_*` | - |
| Engine metadata kept out of notifications (**new**) | verified | P1 | `IsEngineTable` | `ReviewRegressionTests.The_Engine_Table_*` | - |
| No intermediate or rolled-back states (**new**) | verified | P0 | net change per commit | as above | - |
| Live queries with relevance filtering (**new**) | verified | P1 | `Watch`, `BlazeDbLiveQuery<T>` | `ChangeNotificationTests`, E2E `ChangeFeedTests`, `ReviewFollowUpTests.A_Live_Query_*` | No debouncing; re-runs per relevant commit |
| Subscription disposal | verified | P1 | `BlazeDbLiveQuery.Dispose` | `ChangeNotificationTests` | Events: unsubscribe with `-=` |
| Replica reload notification (**new**) | verified | P2 | `IsReset` | `ChangeNotificationTests.A_Replica_*` | - |

## L. Advanced capabilities

| Capability | Classification | Notes |
| --- | --- | --- |
| Full-text search, fuzzy ranking | out of scope | Roadmap non-goal; index-only properties and `StartsWith` cover keyword prefixes |
| Geospatial, vector search | out of scope | Application-level over live objects |
| Time series | covered by ordered indexes | Range scans + `CountBy` |
| TTL / expiry | covered | `DeleteMany(Range(...))` recipe in README |
| Change history / audit | application responsibility | `BlazeDbDatabase.Changed` is an in-process event, raised after commit and not persisted; durable recording is the application's (write each change to an audit table or outbox) |
| Soft delete, multi-tenancy | application patterns | Indexed flag / tenant key + compound index |
| Graph traversal | out of scope | - |

## M. Sync readiness

| Requirement | Status | Pri | Implementation | Evidence | Limitations |
| --- | --- | --- | --- | --- | --- |
| Change feed for an outbox (**new**) | verified | P2 | `BlazeDbDatabase.Changed` | `ChangeNotificationTests` | In-process only; not persisted |
| Stable identifiers | verified | P1 | application keys | - | - |
| Conflict detection primitive (**new**) | verified | P2 | `TryUpdate(row, comparisonValue)` | `BulkAndConditionalWriteTests` | - |
| Version vectors, tombstones, CRDTs, resumable sync | out of scope | - | - | - | Roadmap non-goal ("Sync and conflict resolution") |

## N-P. Security, reliability, diagnostics

| Requirement | Status | Pri | Implementation | Evidence | Limitations |
| --- | --- | --- | --- | --- | --- |
| Corrupt/untrusted persisted input fails loudly, bounded allocation | verified | P0 | `ReadRowCount`, length checks, CRC | `DurabilityTests`, `SerializationTests`, `BackupAndMigrationTests` | CRC is not authentication |
| Diagnostics without data exposure | verified | P1 | `Explain` omits values | `IndexQueryTests.Explain_*` | Exception messages name keys (as before) |
| Actionable exceptions | verified | P1 | `BlazeDbException` family | `Errors` demo page, tests | - |
| Background failure surfaced | verified | P1 | `LastBackgroundError` | `QuotaTests` | - |
| Timing/slow-query listeners | missing | P3 | - | - | Hot-path cost not justified |

## Q-T. Developer experience, testing, benchmarks, docs

| Requirement | Status | Pri | Evidence |
| --- | --- | --- | --- |
| XML docs on new public API | verified | P1 | every new member documented; MCP API catalog reflects them |
| README sections for every new feature, examples compiled | verified | P1 | `ReadmeExampleTests` runs the README's samples on its own model |
| Demo page for change notifications (**new**) | verified | P2 | `Demo/Client/Pages/Changes.razor`, E2E `ChangeFeedTests` |
| Randomized differential tests against a reference model (**new**) | verified | P0 | `ModelBasedTests` (mutation-checked: disabling unique validation fails all seeds) |
| Concurrency stress | verified | P1 | `TransactionCompositionTests`, `ChangeNotificationTests` |
| Benchmark suite with index and persistence scaling (**new** classes) | verified | P1 | `IndexBenchmarks`, `PersistenceBenchmarks`; `benchmarks.md` |
