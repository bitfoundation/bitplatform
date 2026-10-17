# Bit.Blex.Benchmarks

[BenchmarkDotNet](https://benchmarkdotnet.org/) micro-benchmarks for the Blex hot paths. They
measure per-operation time and allocations under heavy, repeated load and print result tables to
the console (and export CSV/HTML/Markdown to `BenchmarkDotNet.Artifacts/`).

> Benchmarks must run in **Release**. Debug builds are unoptimized and give misleading numbers;
> BenchmarkDotNet will warn if you try.

## Running

From the repository root:

```bash
# Interactive menu to pick a suite
dotnet run -c Release --project src/Blex/Tests/Bit.Blex.Benchmarks

# Run everything (takes several minutes)
dotnet run -c Release --project src/Blex/Tests/Bit.Blex.Benchmarks -- --filter *

# Run one suite
dotnet run -c Release --project src/Blex/Tests/Bit.Blex.Benchmarks -- --filter *Dispatch*

# List all benchmarks without running them
dotnet run -c Release --project src/Blex/Tests/Bit.Blex.Benchmarks -- --list flat

# Fast smoke run (fewer iterations; for validation, not for reporting)
dotnet run -c Release --project src/Blex/Tests/Bit.Blex.Benchmarks -- --filter *Serialization* --job short
```

`--filter` accepts glob patterns matched against the fully-qualified benchmark name, so
`*StateMutation*`, `*Dispatch_FullPipeline*`, etc. all work.

## Suites

| Suite                          | What it measures |
|--------------------------------|------------------|
| `StateMutationBenchmarks`      | Per-assignment cost of `SetState` (changing vs no-op), single/batched/async actions |
| `DispatchPipelineBenchmarks`   | Idle fast-path vs full middleware pipeline, scaled by registered store count |
| `GlobalStateBenchmarks`        | `CaptureGlobalState` and `RestoreGlobalState` as store count grows (10/100/500) |
| `SerializationBenchmarks`      | Per-store `SerializeState` / `DeserializeState` / round trip |
| `NotificationFanOutBenchmarks` | One state change as `StateChanged` subscriber count grows (0..1000) |

## Reading the results

- **Mean** - average time per single operation (e.g. one `Increment()`).
- **Allocated** - managed bytes allocated per operation; watch this for GC pressure under load.
- **Ratio / Alloc Ratio** - relative to the suite's `[Baseline]` benchmark.
- **Gen0/1/2** - GC collections per 1000 operations.

These are measurement tools, not pass/fail gates - numbers vary by machine. Compare runs on the
same hardware to spot regressions.
