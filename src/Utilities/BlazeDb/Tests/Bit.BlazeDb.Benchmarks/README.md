# Bit.BlazeDb benchmarks

A [BenchmarkDotNet](https://benchmarkdotnet.org) console harness that measures the engine's read and write paths
and the EF Core provider against **native SQLite** (`Microsoft.Data.Sqlite`, in memory, prepared statements) on
the same 10,000-row table.

Native SQLite is a *favorable* proxy for the sqlite-wasm solutions a Blazor WebAssembly app would otherwise use:
in the browser those additionally pay JS interop, worker round-trips and WebAssembly execution overhead on every
query, so the gaps measured here are the smallest they get.

## What it measures

| Class | What it covers |
|-------|----------------|
| `ReadBenchmarks` | Point get by key, counting and listing by a hash index, and an ordered range scan, against the equivalent SQLite statements. Reads are dictionary and index probes over live objects, so they should not allocate. |
| `WriteBenchmarks` | An upsert in autocommit and inside a 1,000-operation transaction, both memory-only and with the full write-ahead log pipeline over in-memory storage - what the browser pays on the commit path, since the OPFS flush itself happens in the background. |
| `IndexBenchmarks` | Engine-only index costs at 10,000 and 100,000 rows: the first page of an ordered index in both directions, a page from a range, a range and a hash lookup walked end to end, and building the table. The first-page numbers staying flat as the table grows is what shows a range starts in O(log n). |
| `PersistenceBenchmarks` | What durability costs as the database grows: writing a checkpoint, and opening a database - recovery decodes the snapshot and rebuilds every index. |
| `EfCoreBenchmarks` | The same store through `UseBlazeDb()`: LINQ queries the translator turns into index lookups and ranges, and `SaveChanges`, next to the native engine calls they become - the difference is the translator's and the change tracker's overhead. |

## Running it

```bash
# From the repository's src folder. Release is required - a Debug build's numbers are meaningless.
dotnet run -c Release --project Utilities/BlazeDb/Tests/Bit.BlazeDb.Benchmarks

# A single set, selected with BenchmarkDotNet's glob filter:
dotnet run -c Release --project Utilities/BlazeDb/Tests/Bit.BlazeDb.Benchmarks -- --filter "*ReadBenchmarks*"

# A quick smoke run (one iteration, in-process, numbers not meaningful):
dotnet run -c Release --project Utilities/BlazeDb/Tests/Bit.BlazeDb.Benchmarks -- --filter "*" --job dry --inProcess
```

Run with no `--filter` to get an interactive picker. Results (Markdown, CSV and HTML reports) are written to
`BenchmarkDotNet.Artifacts` in the current directory.

## Indicative results

Captured on .NET 10.0.13, x64, an Intel Core i9-14900K desktop, `--job short`. Absolute numbers depend on
hardware; the ratios and the allocation figures are the meaningful part, so re-run locally for your own
hardware. The full set, with the index and persistence classes and the method behind them, is in
`docs/engineering/benchmarks.md`.

| Operation | BlazeDb | SQLite | Notes |
| --- | ---: | ---: | --- |
| Point get by key | 1.2 ns (0 alloc) | 743 ns | ~600x |
| Count by hash index | 2.1 ns (0 alloc) | 63,020 ns | O(1) vs table scan |
| Ordered range scan (~5,100 rows) | 45.4 us | 69.2 us | comparable; both index-driven |
| Upsert (autocommit) | 413 ns (with WAL) | 706 ns | memory-only: 311 ns |
| Upsert in 1,000-op transaction | 369 ns/op | 620 ns/op | with WAL journaling |
| First page of an ordered index | 274 ns at 10k rows, 283 ns at 100k | - | O(log n) start |
| Checkpoint / open, 100k rows | 4.1 ms / 21.9 ms | - | in-memory storage |

The demo site's `/bench` page measures the same operations in the browser's WebAssembly runtime, including the
per-row cost of storage.
