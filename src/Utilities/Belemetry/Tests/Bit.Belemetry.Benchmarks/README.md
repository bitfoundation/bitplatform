# Bit.Belemetry benchmarks

What instrumented code pays for Belemetry, measured rather than assumed: the privacy rules that run on every
attribute of every record, the synchronous recording paths an app's own code goes through (an `ILogger` entry, an
`Activity`, a `Counter` or `Histogram` measurement), and the encoders the export loop runs per request.

Nothing here fails on its own and nothing runs it automatically: the numbers are read against the table below. The
behavior the optimizations rest on - a value that matches no pattern comes back as the same instance, a URL with
nothing to redact is not rebuilt, the pre-checks' boundaries still match - is asserted by `BelemetryPrivacyTests` in
`Bit.Belemetry.Tests`, which runs on every `dotnet test`.

## Running

```bash
# Everything (about two minutes with the short job).
dotnet run -c Release --project src/Utilities/Belemetry/Tests/Bit.Belemetry.Benchmarks -- --filter "*" --job short

# One class.
dotnet run -c Release --project src/Utilities/Belemetry/Tests/Bit.Belemetry.Benchmarks -- --filter "*Privacy*"
```

Release is mandatory: BenchmarkDotNet refuses to measure an unoptimized assembly.

## Last results

BenchmarkDotNet 0.15.8, `ShortRun` (3 warm-ups, 3 iterations - expect a few percent of noise), .NET 10.0.13 x64,
Intel Core i9-10900, Windows 10. In WebAssembly everything is several times slower (an interpreter or AOT without
the JIT's optimizations); the ratios between the cases hold roughly.

| Case | Mean | Allocated | Notes |
| --- | ---: | ---: | --- |
| HttpClient span: build | 166 ns | 656 B | the baseline the privacy cases include |
| HttpClient span: build + privacy | 950 ns | 2016 B | URL with a `token` query parameter to redact |
| HttpClient span, URL without a query: build + privacy | 347 ns | 656 B | the common case: nothing rebuilt, nothing allocated |
| Structured log: build | 121 ns | 480 B | |
| Structured log: build + privacy | 794 ns | 744 B | an e-mail address to scrub, in the body and an attribute |
| `ILogger`: structured entry | 869 ns | 945 B | conversion, consent, privacy, limits, queue |
| `ILogger`: below the minimum level | 28 ns | 56 B | |
| `Activity`: start + stop, two tags | 537 ns | 1225 B | includes the BCL's own activity cost |
| `Counter.Add`, two tags | 135 ns | 336 B | the attribute-set key |
| `Histogram.Record`, one tag | 67 ns | 64 B | |
| OTLP protobuf: 512 logs | 296 us | 334 KB | |
| OTLP JSON: 512 logs | 661 us | 801 KB | |
| OTLP protobuf: 512 spans | 405 us | 377 KB | |
| Azure Monitor: 512 logs | 920 us | 1424 KB | |

### What the measurements changed

Round 1 of the improvement plan made the .NET privacy rules scrub every string attribute, as the browser script
does. The first measurement put that at ~867 ns per HttpClient span and ~973 ns per structured log record (net of
building the record), nearly all of it in the card-number and IPv4 regexes, which ran on any value containing a digit
(`"1.1"`, `/orders/42`). Three changes, each kept only after it measured faster:

1. Exact pre-checks - a card number needs 13 digits, an IPv4 address 3 dots - so most values skip both regexes.
2. A fast path for URLs with no query, fragment or credentials and nothing a pattern could match: returned as is.
3. One cached rule per attribute key (id, redacted name, host, URL, query, version-named, or plain), instead of a
   string comparison per rule for every attribute of every record.

Net privacy cost per span: 867 ns -> 487 ns with a query (`?page=2&sort=desc`, nothing in it to redact; the benchmark
has since moved to a URL with a `token` to redact) (-44%), 181 ns and no extra allocation without one; per
structured log record 973 ns -> 673 ns (-31%); the full `ILogger` entry 1066 ns -> 869 ns (-18%).

## Production diagnostics

`DiagnosticsBenchmarks` runs each case with `BelemetryOptions.Diagnostics` off and on (error limits off, so every
exception takes the full path, as each failure's first occurrence does). BenchmarkDotNet 0.15.8 does not recognize
.NET 11 as a runtime yet (`GetRuntimeVersion not implemented for NotRecognized`), so on this project's `net11.0` target
run it in-process:

```bash
dotnet run -c Release --project src/Utilities/Belemetry/Tests/Bit.Belemetry.Benchmarks -- --filter "*Diagnostics*" --job short --inProcess
```

`ShortRun`, in-process, .NET 11.0.0-rc.3, x64, Windows 11 (2026-11-04):

| Case | Off | On | Allocated off / on | Notes |
| --- | ---: | ---: | ---: | --- |
| `ILogger`: information (exported, below the timeline's `Warning`) | 257 ns | 254 ns | 681 / 681 B | no cost: the timeline's level is checked first |
| `ILogger`: warning (exported, and on the timeline) | 241 ns | 474 ns | 689 / 1089 B | the event, its sanitized template and category |
| `ILogger`: error with an inner exception | 4.1 us | 9.8 us | 5.6 / 14.0 KB | two exceptions sanitized (message, stack without paths), fingerprinted and grouped; capped by the error limits in a loop |
| Framework `HandleEvent` activity: start + stop | 2 ns | 411 ns | 0 / 992 B | without a listener the framework creates no activity; with diagnostics it does, once per UI event |
| Capture of a full timeline (350 events), no browser | - | 17 us | - / 19 KB | |
| Capture + export (the bundle's bytes) | - | 1.0-1.6 ms | - / 1.0 MB | ZIP of ~350 events; an explicit user action |
| Fingerprint: type, message, stack | 2.9 us | 2.9 us | 5.3 / 5.3 KB | |

The first run measured the information entry at 405 ns and 1081 B with diagnostics on: an entry produced for the
export was recorded on the timeline below its level. Fixed (`BelemetryDiagnosticFollowUpTests`), together with a
fingerprint that parsed the stack three times (error entry 10.8 us -> 9.8 us).

