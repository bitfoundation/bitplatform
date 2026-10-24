# BlazeDb architecture decisions

Engineering record of the decisions taken while hardening the engine (October 2026). Each entry says
what was decided, why, what it costs, and what was rejected. The user guide is the repository's
`README.md`; this file is for people changing the engine. Status of every requirement is in
[requirements-matrix.md](requirements-matrix.md), the evidence in [assessment.md](assessment.md) and
[benchmarks.md](benchmarks.md).

## ADR-001 - Keep one writer and lock-free reads; no MVCC, no reader locks

**Context.** Reads (`Get`, index lookups, scans, every EF query) take no lock; writes are serialized by
`SyncRoot`. A read concurrent with a write on another thread is a data race. Blazor Server and
multi-threaded desktop hosts are where this bites.

**Decision.** Keep the model and document it as the contract (README "Threading model", "Transactions").
Writers on several threads are safe and serialized; readers concurrent with writers are not supported.

**Why.** The engine exists for one thread per tab. A point read costs about a nanosecond; any reader lock
(`ReaderWriterLockSlim`, a monitor) multiplies that, and an MVCC or copy-on-write row store adds an
allocation or indirection to every write and every read. Both would be paid by every WebAssembly user for a
hosting model the project lists as a non-goal (roadmap: "Multi-writer concurrency", "Server-side scale").

**Consequences.** A server host must serialize access itself (README says so). Tests cover concurrent
writers (`TransactionCompositionTests.Concurrent_Writers_Each_Get_An_Atomic_Unit`,
`ChangeNotificationTests.Concurrent_Writers_Lose_No_Notification`) and never concurrent readers.

**Rejected.** Opt-in thread-safe mode: two code paths through every read for a configuration nobody
should choose for a shared server store.

## ADR-002 - No async-flow guard on the ambient transaction

**Context.** While a transaction is open, every write on the database joins it - including a write from an
unrelated event handler that runs while the transaction's owner awaits something.

**Decision.** Keep ambient semantics. Add `RunInTransaction(Action)` / `RunInTransaction<T>(Func<T>)`, which
run synchronously under the write lock, and no asynchronous overload. Document "no `await` inside a
transaction".

**Why.** Scoping the transaction to an `AsyncLocal` flow was evaluated and rejected: an `AsyncLocal` set
inside an `async` method does not flow back to its caller, so `BeginTransaction` called from any async
helper - including EF Core's `BeginTransactionAsync` chain - would make the owner's own writes look foreign
and fail. A guard with false positives on correct code is worse than a documented rule.

**Consequences.** The hazard is documented, and the API that composes atomic units (`RunInTransaction`) makes
the interleaving impossible by construction.

## ADR-003 - Keep `SortedSet` for ordered indexes

**Context.** `SortedSet<T>.GetViewBetween` was historically O(k) in the size of the view, which would make
`Range(...).Take(20)` cost the whole range.

**Decision.** Measure before replacing. `IndexBenchmarks.Range_FirstPage` is 352 ns at 10,000 rows and
378 ns at 100,000 rows on .NET 10 - O(log n). The view counts lazily on current .NET. No B+tree.

**Consequences.** One less data structure to maintain and verify. If a future runtime regresses, the
benchmark shows it.

## ADR-004 - Index maintenance is atomic by unwinding

**Context.** `SetEntry` removed a row's old index entries, replaced the row and added the new entries. A
selector or comparer that throws part-way (a hand-written index definition, a key type whose comparison
throws) left some indexes updated and others not, with nothing above noticing.

**Decision.** `SetEntry`/`RemoveEntry` count the stores they updated and undo exactly those on failure, then
rethrow. The caller sees an ordinary refused write and the table is as it was.

**Rejected.** Copy-on-write index stores: an allocation per write to protect against misconfigured indexes.

**Evidence.** `CoreCorrectnessTests` (insert, update, delete and in-transaction failures against a
deliberately failing comparer), and `VerifyIntegrity` after each.

## ADR-005 - Change notifications: net change per commit, delivered after the lock

**Decision.**

- `BlazeDbTable<TKey,TRow>.Changed` (typed) and `BlazeDbDatabase.Changed` (all tables, boxed) are raised
  once per commit - an autocommit write, a committed transaction, a bulk write - with the net change per
  row: first old row to last new row; a row inserted and deleted inside one commit is omitted.
- Never raised before a commit, for a rollback, a savepoint rolled back to, or a commit the log refused.
- Collected under the lock only when the table or the database has a subscriber (one field read per write
  otherwise), queued, and delivered by the outermost public write once the lock is released. A write made
  inside a handler is queued and delivered after the current notification reached every subscriber, so
  notifications are in commit order.
- Every subscriber is invoked separately; their exceptions are aggregated into one
  `BlazeDbChangeHandlerException`, a type of its own so callers - the EF Core provider first - can tell "committed,
  but a handler failed" from a failed write. The provider completes the save (EF accepts its changes) and
  rethrows from `SavedChanges`; its transaction wrapper counts such a commit as committed.
- `BlazeDbLiveQuery<T>` (`table.Watch`) is built on the table event, with an optional relevance test that
  skips re-running for commits that changed no row it could see.

**Why.** UI frameworks need "what changed" after it is final, and nothing before. Delivering under the lock
would let a handler's write re-enter half-way through another notification, and deliver on the wrong
thread in a multi-threaded host anyway.

**Rejected.** Asynchronous dispatch (hidden tasks, unbounded queues, ordering problems on WebAssembly);
`IObservable` (would pull System.Reactive for the operators that make it worthwhile); per-operation events
(expose intermediate states).

**Evidence.** `ChangeNotificationTests`; `ModelBasedTests` rebuilds a shadow copy of the table purely from
notifications and requires it to equal the committed model after every one of 7,200 random steps.

## ADR-006 - Engine metadata in a reserved table, omitted while empty

**Context.** `UserVersion` (and migrations driven by it) must be stored with the data, be transactional,
travel with exports, and not break databases opened by an earlier engine version.

**Decision.** An internal table `__blazedb_meta`, registered in every database, written through the ordinary
table path (journaled, transactional, checkpointed). Names starting `__blazedb` are reserved. Snapshots
leave the table out while it is empty, so a database that never uses it is byte-for-byte what an earlier
engine would write; one that does is opened by an earlier engine as an unregistered table, which that
engine already carries along untouched.

**Rejected.** A new manifest format version: earlier engines refuse unknown manifest versions outright.

## ADR-007 - Migrations run at open, one transaction per step

`AddMigration(version, action)`: each migration above `UserVersion` runs once, lowest first, inside a
transaction that also sets `UserVersion`. A failing migration fails `OpenAsync` (the database is disposed,
earlier steps stay committed) and is retried on the next open. Replicas do not migrate. Rejected: one
transaction for every pending step (IndexedDB style) - a failure on step 5 would also discard steps 1-4 that
succeeded, and re-running them is not necessarily idempotent.

## ADR-008 - Export format is the snapshot format

`ExportAsync` writes exactly what a checkpoint writes - magic, format version, rows per table with their
keys, CRC-32 over everything - so there is one format to version and recover, and an export carries
unregistered tables along. `ImportAsync` verifies the header and checksum before writing anything,
refuses a backend that already holds a database (no accidental overwrite), deletes a stale first-generation
log, and writes the manifest last. The CRC detects damage; it is not a security boundary, and the file is
not encrypted (README says so).

## ADR-009 - Constructor binding and index-only properties in the generator

**Decision.** A type without a parameterless constructor is read through the constructor with the most
parameters that each name a property of the same type (records' copy constructors excluded); get-only
properties it binds are stored. A get-only property that is indexed (or named by a compound index) and not
bound is index-only: computed, indexed, never stored, no field number. The EF Core provider mirrors the
same choice: a convention maps the constructor-bound get-only properties, and original values for a
constructor-built type are reconstructed without running a constructor.

**Why.** Positional records were the most common shape the generator refused (BLZ004), and index-only
properties are the cheap answer to "index a nested or derived value" without nested-object storage.

Only a *computed* property can be index-only: a get-only auto-property holds a value of its own (set by an
initializer or a constructor the reader does not run) that a reload could not restore, so indexing one the
reader's constructor does not take is a compile error (BLZ017). Likewise a `required` member is set in the
reader's object initializer even when the constructor took it (the compiler insists), and one BlazeDb does
not store is reported (BLZ018) rather than left to fail in generated code.

## ADR-010 - Nested objects are not stored

**Decision.** Deferred, documented with the alternatives (a child table keyed by the parent, or an
application-encoded string/`byte[]`).

**Why.** Storing nested objects needs, beyond serializer generation: deep change detection in the EF
provider (value comparers that snapshot and compare object graphs), a policy for polymorphic values (a
derived instance stored as its base type silently loses data), cycle detection, and index semantics for
nested members. Shipping the serializer alone would make EF saves of in-place nested edits silently miss.

## ADR-011 - A stream detects foreign writes rather than snapshotting

`BlazeDbQuery.ExecuteAsync` yields between batches. If the table's `Version` moved while it was yielded,
it throws `InvalidOperationException` naming the table. Writes made by the loop's own body (between the
version reads) remain allowed, as for a synchronous `foreach`. Rejected: materializing the source first
(defeats streaming) and silently continuing (Dictionary enumeration tolerates removals, so results would
mix states without an error).

## ADR-012 - WAL records are encoded in place; oversized commits are refused at commit

The record header is written as a placeholder into the pending buffer, the payload encoded behind it, and
length and CRC patched in - one copy of each row instead of three. A failure cuts the buffer back. A payload
above the 256 MB record limit is now refused at commit (and the commit undone); before, it was journaled and
then silently treated as a torn record on replay, losing it and every later commit.

## ADR-013 - Bulk writes are journaled as ordinary operations

`InsertMany`, `UpsertMany`, `DeleteMany`, `DeleteWhere` and `DeleteAll` run through `RunInTransaction`, so a
batch is atomic and one log record, but each row is an ordinary set or delete inside it. No new WAL op codes:
a log written by this version replays on earlier engines.

## ADR-014 - Buffers grow with 64-bit arithmetic and refuse past `Array.MaxLength`

`BlazeDbBufferWriter` overflowed `int` once a buffer passed about 1 GB, surfacing as "Destination is too
short" from a copy into a buffer that never grew (found by a long benchmark run over in-memory storage).
Growth is computed in 64 bits and a request past `Array.MaxLength` throws a `BlazeDbException` that says
what to split.

## ADR-015 - Atomic units use clamped marks, not savepoints

`RunInTransaction` inside an open transaction, and an EF Core save joining one, open a *unit* rather than a
public savepoint. A public savepoint is forgotten when a rollback goes below it (SQL semantics); a unit's mark
is lowered to the point the rollback reached instead, because every write recorded above that point was made
inside the unit. A unit that fails after rolling the transaction back past its own start therefore still takes
back everything it wrote, and never reports an "out of range" error in place of its own exception.

## ADR-016 - A failed undo stops writes until the database is reopened

Undo can only fail when an index selector or comparer throws. When it does, memory may hold a write the log
never recorded, and the next checkpoint would make it durable. The database records the failure and refuses
further writes and checkpoints with an exception that says to reopen; committed records already buffered are
still flushed, and disposal still closes the log before rethrowing the undo's exception.
