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
| `EfCoreBenchmarks` | The same store through `UseBlazeDb()`: LINQ queries the translator turns into index lookups and ranges, and `SaveChanges`, next to the native engine calls they become - the difference is the translator's and the change tracker's overhead. |

## Running it

```bash
# From the repository's src folder. Release is required - a Debug build's numbers are meaningless.
dotnet run -c Release --project BlazeDb/Tests/Bit.BlazeDb.Benchmarks

# A single set, selected with BenchmarkDotNet's glob filter:
dotnet run -c Release --project BlazeDb/Tests/Bit.BlazeDb.Benchmarks -- --filter "*ReadBenchmarks*"

# A quick smoke run (one iteration, in-process, numbers not meaningful):
dotnet run -c Release --project BlazeDb/Tests/Bit.BlazeDb.Benchmarks -- --filter "*" --job dry --inProcess
```

Run with no `--filter` to get an interactive picker. Results (Markdown, CSV and HTML reports) are written to
`BenchmarkDotNet.Artifacts` in the current directory.

## Indicative results

Captured on .NET 10, x64, a desktop machine. Absolute numbers depend on hardware; the ratios and the allocation
figures are the meaningful part, so re-run locally for your own hardware.

| Operation | BlazeDb | SQLite | Notes |
| --- | ---: | ---: | --- |
| Point get by key | 2.6 ns (0 alloc) | 876 ns | ~340x |
| Count by hash index | 2.7 ns (0 alloc) | 67,649 ns | O(1) vs table scan |
| Ordered range scan (~5,100 rows) | 55.6 us | 69.3 us | comparable; both index-driven |
| Upsert (autocommit) | 462 ns (with WAL) | 752 ns | memory-only: 328 ns |
| Upsert in 1,000-op transaction | 487 ns/op | 682 ns/op | with WAL journaling |

The demo site's `/bench` page measures the same operations in the browser's WebAssembly runtime, including the
per-row cost of storage.
