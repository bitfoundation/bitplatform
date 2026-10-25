# Bit.Bcrdt benchmarks

The performance budgets in the [Performance](../../README.md#performance) section of the bit Bcrdt README
are stated as a promise: *budgets are measured by benchmarks, not taken on hope.* This project is where
they are measured. Every benchmark class names the budget it defends and models a scenario a real
application actually produces, so a regression here is a regression a user would feel.

Nothing here fails on its own. The benchmarks report numbers; they do not compare them against the
budgets, and nothing in the repository runs them automatically. A regression is caught by running
the suite and reading the results against the table below. The algorithmic properties the budgets
rest on - small incremental updates, block merging, a cached delete set - are asserted separately by
`PerformanceBudgetTests` in [`Bit.Bcrdt.Tests`](../Bit.Bcrdt.Tests/), which does run on every
`dotnet test`.

## Running

The suite is a [BenchmarkDotNet](https://benchmarkdotnet.org) switcher, so nothing runs until you
ask for it by name. Release configuration is mandatory - BenchmarkDotNet refuses to measure an
unoptimized assembly, and it is right to.

```bash
# One class.
dotnet run -c Release --project src/Utilities/Bcrdt/Tests/Bit.Bcrdt.Benchmarks -- --filter "*Text*"

# Several classes.
dotnet run -c Release --project src/Utilities/Bcrdt/Tests/Bit.Bcrdt.Benchmarks -- --filter "*Merge*" "*Serialization*"

# Everything. Takes a while: the 100 000-character and 50-replica cases are not cheap, and
# `ConcurrencyBenchmarks.ConvergeAllToAll` at 50 replicas is by some margin the longest single case.
dotnet run -c Release --project src/Utilities/Bcrdt/Tests/Bit.Bcrdt.Benchmarks -- --filter "*"

# Prove the harness works without paying for statistics: one warmup, one measurement, no analysis.
dotnet run -c Release --project src/Utilities/Bcrdt/Tests/Bit.Bcrdt.Benchmarks -- --filter "*DataType*" --job Dry

# What is available.
dotnet run -c Release --project src/Utilities/Bcrdt/Tests/Bit.Bcrdt.Benchmarks -- --list flat
```

Results land in `BenchmarkDotNet.Artifacts/results/` as Markdown, CSV and HTML alongside the console
output.

## What each class covers

| Class | Scenario | Budgets defended |
|---|---|---|
| `TextEditingBenchmarks` | Typing, clicking into the middle of a page, rendering, select-and-delete | Sequential insert, steady-state keystroke |
| `MergeBenchmarks` | Two replicas that edited without seeing each other, then reconciled | Divergent merge, idempotency fast path, update coalescing |
| `SerializationBenchmarks` | Encoding for the wire and for storage, decoding on arrival, state-vector round trips | Encode time and size, apply time, state-vector round trip |
| `ConcurrencyBenchmarks` | A room filling up: all-to-all convergence and presence fan-out | Awareness fan-out |
| `DataTypeBenchmarks` | Collaborative lists, dictionaries and sets - application state that is not prose | The technique list under the budget table: key index, search markers, transaction batching |
| `MemoryBenchmarks` | The same content written two ways, to price run-length encoding | Run-length blocks, allocation-free keystrokes |

## The budgets

Reproduced verbatim from the [Performance](../../README.md#performance) section of the bit Bcrdt README. Targets are
for a 2020-class desktop CPU.

| Scenario | Budget | Measured by |
|---|---|---|
| Insert 100 000 characters sequentially | < 150 ms | `TextEditingBenchmarks.SequentialAppend`, `Size=100000` |
| Apply a 100 000-char update from a peer | < 60 ms | `SerializationBenchmarks.ApplyUpdateIntoEmptyDocument`, `DocumentSize=100000` |
| Encode 100 000-char document | < 25 ms, < 1.2× raw UTF-8 size | `SerializationBenchmarks.EncodeWholeDocument` for the time; the ratio is printed by its `[GlobalCleanup]` |
| Merge two 50 000-op divergent replicas | < 200 ms | `MergeBenchmarks.MergeDivergentReplicas`, `EditsPerReplica=50000` |
| State vector encode/decode (100 clients) | < 10 µs | `SerializationBenchmarks.EncodeStateVector` / `DecodeStateVector`, `clientCount=100` |
| Steady-state keystroke (local, 1 MB doc) | < 50 µs, 0 gen-2 allocations | `TextEditingBenchmarks.InsertAtScatteredPositions`, `Size=100000`; `InsertAtBeginning` is the same keystroke with no position lookup, as a baseline |
| Awareness update fan-out (100 clients) | < 1 ms | `ConcurrencyBenchmarks.AwarenessFanOut`, `ReplicaCount=50` (extrapolate, or raise the parameter) |

Three of these need a note. The steady-state keystroke budget is stated for a 1 MB document; the
largest fixture here is 100 000 characters, or roughly 200 KB in UTF-16, so treat the reported number
as a lower bound and watch how it moves between `Size=10000` and `Size=100000` rather than reading
the absolute value against 50 µs. The awareness budget is stated for 100 clients; the parameter set
tops out at 50 because the convergence benchmark in the same class performs `n²` exchanges over a
document that is itself `O(n)` in size, and 100 replicas would dominate the suite's runtime. Fan-out
is linear, so doubling the 50-client figure is a fair estimate. And the merge budget is stated in
operations while `MergeBenchmarks` is parameterized in characters, one character per operation, so
`EditsPerReplica=50000` is the 50 000-op case - but note that the two replicas diverge in different
shapes on purpose, and it is the replica that types at the front of the document, whose blocks cannot
merge, whose history is the one being integrated.

## Reading the results

**`Mean` is per operation, not per invocation.** Several benchmarks perform a fixed batch of edits
and declare it through `OperationsPerInvoke`, because a single keystroke is too short to time
reliably. BenchmarkDotNet divides by that count for you, so a row reading `12 µs` for
`Insert at index zero of a large document` means 12 µs per keystroke, not per batch of one hundred.

**Read the trend across parameters, not just the absolute numbers.** Most of the architecture's
performance claims are claims about *scaling* rather than about constants. Position lookup is
supposed to be amortized constant, so `InsertAtScatteredPositions` should cost roughly the same at
`Size=100000` as at `Size=1000`; if it grows tenfold, the search-marker cache has stopped working
and no amount of absolute headroom makes that acceptable. `InsertAtBeginning` is the yardstick:
index zero is resolved from the head of the list without any lookup, so it is flat by construction,
and the gap between the two rows is what finding a position costs. Likewise a map `Get` should be
flat in key count, and the idempotent fast path should be flat in history length.

**`Allocated` is churn, not footprint.** The `[MemoryDiagnoser]` column reports bytes allocated per
operation, which is the number that matters for the "no gen-2 allocations per keystroke" half of the
steady-state budget. It says nothing about how much memory a document holds on to.
`MemoryBenchmarks` prints block counts and post-collection retained bytes to its log for that, and
`SerializationBenchmarks` prints encoded payload sizes and the ratio against raw UTF-8 the same way,
since neither is a timing BenchmarkDotNet has a column for.

**Absolute numbers are machine-specific; ratios are not.** The budget table assumes a 2020-class
desktop CPU. On a laptop under thermal pressure, or in a container with a CPU quota, expect every row
to be slower by a roughly constant factor - which is why the relationships between rows are the more
durable signal. Two comparisons are worth checking on any machine: `MergeManyIncrementalUpdates`
against `MergeOneCoalescedUpdate` prices the per-update overhead that persistence compaction removes
(and that a coalescing relay would), and `Array: iterate every element` against
`Array: indexed read` shows the difference between one pass over the block list and resolving each
position independently.

**A `Dry` job proves nothing about performance.** `--job Dry` runs each benchmark once with no
warmup, which is exactly what you want to check that a fixture builds and the harness is wired up,
and exactly what you must not quote: the numbers include cold JIT and are routinely several times
worse than the real thing.

## How the fixtures are built, and why it matters

Every class builds its inputs in `[GlobalSetup]` and, where a benchmark permanently changes what it
touches, restores them in `[IterationSetup]`. This is not ceremony. A benchmark that builds its own
document measures construction rather than the operation; a destructive benchmark that is not reset
measures the second invocation doing nothing, or doing measurably more work than the first because
the blocks it split are still split.

Because a reset is only correct if it happens as often as the mutation it undoes, the classes are
annotated `[InvocationCount(1, unrollFactor: 1)]` - one measured invocation per iteration, one
`[IterationSetup]` per measured invocation. The attribute applies to every benchmark in a class,
read-only ones included, which is why every benchmark whose single operation takes microseconds or
less is batched through `OperationsPerInvoke` rather than left to BenchmarkDotNet's usual auto-tuned
invocation count: with one invocation per iteration, a 200 ns operation would be lost in the timer's
noise. That covers the keystroke, probe and state-vector loops, and the read-only
`ReadWholeDocument`, `EncodeWholeDocument`, `ApplyUpdateAlreadyHeld` and `AwarenessFanOut`. The
exceptions are the two destructive single operations, `DeleteLargeRange` and
`ApplyUpdateIntoEmptyDocument`, which need a fresh fixture per operation; at their smallest
parameter values they run for only a few microseconds, so read those rows with that in mind.

Every document a benchmark mutates or merges into has an `UpdateSink` attached. A document only
encodes a transaction's update when something subscribes to `Updated`, and in an application
something always does - the sync provider and the persistence binding - so a fixture without a
subscriber would skip the encoding every real keystroke and every real merge pays for. The sink is
attached after the fixture is loaded, so only the measured work pays for it.

Every randomized workload - scattered insert positions, probe indices - is drawn from a fixed seed in
`[GlobalSetup]`, so each iteration performs identical work. A benchmark whose workload varies between
iterations reports its own variance as though it were the engine's.

## Recorded results

One recorded comparison, for scale rather than as a budget: the library before and after the review that
rewrote text blocks as string slices (constant-time splits), bounded decoder allocation and added message
splitting. Both runs used the same machine and settings, so read the ratios, not the absolute numbers.

- **Method:** `dotnet run -c Release --project src/Utilities/Bcrdt/Tests/Bit.Bcrdt.Benchmarks -- --inProcess --job short --filter "*TextEditing*" "*Serialization*" "*Merge*"`,
  once against the previous code and once against the current code. `ShortRun` is three iterations, so the error
  columns are wide; differences inside them are noise.
- **Machine:** Intel Core i9-14900K, Windows 11 24H2, .NET 10.0.13 x64, BenchmarkDotNet 0.15.8, October 2026.

| Benchmark (100 000-character document) | Before | After |
|---|---|---|
| Append 100 000 characters, one keystroke per transaction | 66.8 ms, 274 MB | 67.0 ms, 266 MB |
| Insert at a scattered position | 5.6 µs, 53 KB | 1.1 µs, 6.9 KB |
| Delete a large range in one transaction | 40.5 µs, 1 036 KB | 11.1 µs, 9.5 KB |
| `EncodeStateAsUpdate` for the whole document | 84.8 µs, 303 KB | 67.7 µs, 103 KB |
| `ApplyUpdate` of that payload into an empty document | 72.1 µs, 822 KB | 54.6 µs, 432 KB |
| Merge a divergent replica's whole history (50 000 edits each) | 14.9 ms, 18.7 MB | 7.5 ms, 13.4 MB |

The same run first caught a regression in the rewrite: appending a keystroke to a long block re-encoded the whole
block, and the 100 000-keystroke case took 4.5 s and allocated 19 GB. It is fixed, and
`PerformanceBudgetTests.A_keystroke_costs_the_same_whether_the_text_is_short_or_long` now fails if it returns.
