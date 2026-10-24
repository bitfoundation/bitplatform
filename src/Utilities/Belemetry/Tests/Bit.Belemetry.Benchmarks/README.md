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
| HttpClient span: build + privacy | 653 ns | 1752 B | URL with a query to redact |
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

Net privacy cost per span: 867 ns -> 487 ns with a query to redact (-44%), 181 ns and no extra allocation without
one; per structured log record 973 ns -> 673 ns (-31%); the full `ILogger` entry 1066 ns -> 869 ns (-18%).
