# BlazeDb benchmark report

Measurements taken during the October 2026 hardening pass, before the first engine change (baseline) and after
the last (final). The suite is `Tests/Bit.BlazeDb.Benchmarks`; see its README for how to run it. Absolute
numbers depend on hardware - compare ratios and allocations, and re-run locally.

## Methodology

- BenchmarkDotNet 0.15.8, `--job short` (3 iterations) unless stated, `MemoryDiagnoser` on every class.
- Windows 11 24H2, Intel Core i9-14900K, .NET SDK 10.0.402, runtime .NET 10.0.13 x64 RyuJIT, Release build,
  server GC (as the project configures).
- SQLite rows are `Microsoft.Data.Sqlite` 10.0.13, in memory, prepared statements - a favorable proxy for
  sqlite-wasm, which also pays JS interop and worker round-trips in the browser.
- The short job is noisy for anything with background work (the WAL flusher runs every 100 ms): where a
  decision depended on a write-path difference, an A/B with `--job medium` (15+ iterations) was run with only
  the file under test swapped, recorded below.
- Nothing here is a WebAssembly measurement; the demo's `/bench` page measures in the browser.

Commands:

```bash
# from src
dotnet run -c Release --project Utilities/BlazeDb/Tests/Bit.BlazeDb.Benchmarks -- --filter "*IndexBenchmarks*" "*PersistenceBenchmarks*" "*ReadBenchmarks*" "*WriteBenchmarks*" --job short
```

## Reads (ReadBenchmarks, 10,000 rows)

| Operation | Baseline | Final | SQLite (final run) |
| --- | ---: | ---: | ---: |
| Point get by key | 1.13 ns, 0 B | 1.21 ns, 0 B | 743 ns, 592 B |
| Count by hash index | 2.03 ns, 0 B | 2.09 ns, 0 B | 63,020 ns |
| Ordered range count (~5,100 rows) | 45.4 us | 45.4 us | 69.2 us |

No change expected and none seen: the read path was not modified.

## Indexes as the table grows (IndexBenchmarks, new)

| Operation | 10,000 rows | 100,000 rows |
| --- | ---: | ---: |
| `OrderBy(index).Take(20)` | 274 ns | 283 ns |
| `OrderBy(index, descending).Take(20)` | 318 ns | 325 ns |
| `Range(index, 10, 90).Take(20)` | 328 ns | 383 ns |
| `Range` walked end to end (~50% of rows) | 45.6 us | 655 us |
| `Lookup(hash)` walked end to end (50% of rows) | 9.7 us | 99.2 us |
| Build the table (insert every row, 2 secondary indexes) | 2.1 ms, 4.2 MB | 23.8 ms, 39.8 MB |

The first page costs the same at 10x the rows: a range starts in O(log n). This measurement is why the
ordered index keeps `SortedSet` (decisions.md, ADR-003). Final and baseline agree within noise (baseline:
277/309 ns, 352/378 ns first page; 22.8 ms build at 100,000).

## Persistence (PersistenceBenchmarks, new)

| Operation | Rows | Baseline | Final |
| --- | ---: | ---: | ---: |
| Checkpoint (snapshot + manifest) | 10,000 | 666 us, 929 KB | 502 us, 512 KB |
| Checkpoint | 100,000 | 6.12 ms, 13.4 MB | 4.06 ms, 5.6 MB |
| Open (recover + rebuild indexes) | 10,000 | 2.12 ms, 3.29 MB | 2.09 ms, 3.29 MB |
| Open | 100,000 | 21.7 ms, 32.0 MB | 21.9 ms, 32.0 MB |

The checkpoint improvement comes from sizing the snapshot buffer from the previous snapshot and handing
its bytes to storage without copying them into an exact-size array (round 6). The
checkpoint runs under the write lock, so this is also the pause a writer sees. Open is dominated by
decoding rows and rebuilding indexes and was not changed.

## Writes (WriteBenchmarks)

| Operation | Baseline (short) | Final (short) | SQLite (final) |
| --- | ---: | ---: | ---: |
| Upsert, memory only | 330 ns, 160 B | 311 ns, 160 B | - |
| Upsert, with WAL | 499 ns, 237 B | 413 ns, 291 B | 706 ns |
| Upsert inside a 1,000-op transaction, with WAL | 444 ns/op | 369 ns/op | 620 ns/op |

The WAL rows are not directly comparable: the benchmark used to run with checkpoints disabled, which grew the
in-memory log without bound and crashed a long run (that crash exposed the buffer overflow fixed in
ADR-014). It now uses the default checkpoint threshold, as an application would.

The fair comparison of the WAL encoding change is the medium-job A/B, same benchmark configuration, only
`BlazeDbWalManager.cs` swapped:

| Operation | Before (in-place encoding off) | After |
| --- | ---: | ---: |
| Upsert, memory only | 534 ns | 522 ns |
| Upsert, with WAL | 791 ns | 769 ns |
| Upsert in a 1,000-op transaction | 639 ns/op | 638 ns/op |

Within noise: removing two copies of each row's bytes does not show up next to the rest of a write. It is
kept for the correctness fix it carries (oversized commits refused at commit) and the lower peak memory.
(The medium job ran slower overall than the short job on the same machine - thermal and scheduling
conditions differ between runs, which is why only same-run comparisons are drawn.)

## Change notifications

With nobody subscribed a commit pays one field read per operation and one per public write - not visible in
the write numbers above (memory-only upsert 330 ns before notifications existed, 311 ns after). With
subscribers the cost is the collection of the changes and the handlers themselves; no benchmark measures
that, because it is dominated by what the handler does.

## Targets for regressions

Derived from these baselines rather than set in advance - a change that moves one of these by more than the
run-to-run noise should explain why:

- point get and hash count: allocation-free, low single-digit nanoseconds;
- the first page of an ordered index flat in table size;
- autocommit upsert with WAL within about 1.5x of memory-only;
- checkpoint allocation close to one snapshot's size.
