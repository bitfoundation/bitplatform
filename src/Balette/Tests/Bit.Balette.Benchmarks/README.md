# Bit.Balette search benchmarks

A [BenchmarkDotNet](https://benchmarkdotnet.org) console harness for the per-keystroke path of the bit Balette
command palette: fuzzy matching a single field, matching a command across all of its fields, ranking the matches,
and a whole keystroke over registries of 200, 2,000 and 20,000 commands.

Matching and ranking run in C# on every keystroke, and on WebAssembly that C# usually runs interpreted, so this is
the code that decides whether typing in the palette feels instant. The changelog of the original BlazorCommandPalette project
reports that optimizing this path brought the median keystroke over 2,000 commands from about 180 ms to about 20 ms
on WebAssembly. These benchmarks run the same code on the .NET JIT, where it is far faster, so use them to compare
changes and catch regressions, not to predict WebAssembly latency.

Synthetic registries are shaped like the palette of a real application rather than random strings: half are actions
such as "Export Report as CSV", half are navigation entries to named records such as the customer "Aurora
Logistics", grouped by area, with subtitles, aliases, descriptions and tags on a realistic share of them. The
matcher therefore sees real word boundaries, shared prefixes and near misses.

## What it measures

| Class | What it covers |
|-------|----------------|
| `FuzzyMatcherBenchmarks` | `BaletteFuzzyMatcher.Match` on one field: short and long queries, titles, a description and a 2,000-character text, smart case, non-ASCII text, and the fast reject of a query that is not a subsequence. It runs for every searchable field of every command on every keystroke. |
| `CommandMatchBenchmarks` | Matching one command that has every searchable field populated, for single and multi-term queries and the `'exact`, `^prefix`, `suffix$` and `!negate` operators. Each query is matched once parsed in advance, as the palette does, and once through the public string overload, which parses it on every call. |
| `RankerBenchmarks` | `BaletteRanker.Rank` over 200, 2,000 and 20,000 matches: the public overload, which sorts everything; the best 150 (`MaxResults`), which is what the palette asks for; and the empty query, where only priority and frecency decide. |
| `KeystrokeBenchmarks` | One keystroke end to end over 200, 2,000 and 20,000 commands: snapshot the registry, parse the query once, match every command and rank the matches down to 150. It covers an empty query, a first keystroke, a word, two terms and a query that matches nothing. |
| `ShortcutBenchmarks` | Parsing shortcut strings (`Mod+K`, `Ctrl+Shift+P`, `G I`) and computing the normalized form a pressed shortcut is compared against. This is not on the search path; it is here to show it stays negligible. |

## Running it

```bash
# From the repository's src folder. Release is required - a Debug build's numbers are meaningless.
dotnet run -c Release --project Balette/Tests/Bit.Balette.Benchmarks

# A single set, selected with BenchmarkDotNet's glob filter:
dotnet run -c Release --project Balette/Tests/Bit.Balette.Benchmarks -- --filter "*Keystroke*"

# How the numbers below were captured (3 measured iterations per case, about 7 minutes for the whole suite):
dotnet run -c Release --project Balette/Tests/Bit.Balette.Benchmarks -- --filter "*" --job short

# A quick smoke run (one iteration, in-process, numbers not meaningful):
dotnet run -c Release --project Balette/Tests/Bit.Balette.Benchmarks -- --filter "*FuzzyMatcher*" --job dry --inProcess
```

Run with no `--filter` to get an interactive picker. Results (Markdown, CSV and HTML reports) are written to
`BenchmarkDotNet.Artifacts` in the current directory. In the keystroke results the empty query shows as an empty
`Query` cell.

## Indicative results

Captured with BenchmarkDotNet's short job (3 warmup and 3 measured iterations, one launch) on .NET 10.0.13, x64, an
Intel Core i9-14900K under Windows 11. Sizes are binary: KB means 1,024 bytes and MB 2^20 bytes, as
BenchmarkDotNet reports them. Three iterations give wide error bars and absolute numbers depend on hardware; the
ratios and the allocation figures are the meaningful part, so re-run locally, without `--job short` for tighter
numbers.

### One keystroke

Time per keystroke:

| Commands | Empty query | `s` | `sett` | `open sett` | `qjz` (no match) |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 200 | 47 us | 93 us | 58 us | 43 us | 28 us |
| 2,000 | 216 us | 834 us | 705 us | 663 us | 325 us |
| 20,000 | 4.3 ms | 13.5 ms | 7.9 ms | 7.3 ms | 3.8 ms |

Allocated per keystroke:

| Commands | Empty query | `s` | `sett` | `open sett` | `qjz` (no match) |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 200 | 49 KB | 66 KB | 15 KB | 10 KB | 7 KB |
| 2,000 | 316 KB | 480 KB | 124 KB | 86 KB | 49 KB |
| 20,000 | 3.3 MB | 4.9 MB | 1.2 MB | 944 KB | 670 KB |

The first keystroke is the expensive one: `s` matches 95% of the registry, so nearly every command is scored on
every field and yields a match to rank. Each further letter narrows the result, and a field that cannot contain the
query is rejected by a subsequence scan before any scoring, which is why a query that matches nothing costs under
half of a first keystroke. The empty query skips matching but still creates a match for every command. At 20,000
commands, the lists and arrays sized to the registry cross the 85,000-byte large object threshold, and every query
shows gen 2 collections.

### Ranking

| Matches | All, sorted (public overload) | Best 150 | Best 150, empty query |
| ---: | ---: | ---: | ---: |
| 200 | 10.1 us | 9.4 us | 21.7 us |
| 2,000 | 332 us | 87 us | 188 us |
| 20,000 | 9.1 ms | 756 us | 1.08 ms |

Sorting only the rows that get rendered keeps ranking roughly linear in the number of matches, while a full sort
grows faster than that. At 20,000 matches the best 150 cost under a tenth of a full sort and allocate 329 KB instead
of 2.2 MB. Just above the limit a heap would cost more than it saves, so below twice the limit the ranker sorts
everything and keeps the front, which is why the best 150 of 200 cost what the full sort does. An empty query ranks more slowly than a typed one, because unscored commands tie on rank and fall through to
the culture-aware title comparison.

### Matching one command

The command has a title, a subtitle, a group, four keywords, a 107-character description and two tags.

| Query | Time | Allocated | Parsing on every call |
| --- | ---: | ---: | ---: |
| `tog` | 545 ns | 168 B | 674 ns, 384 B |
| `explr` (keyword and description) | 1.30 us | 184 B | 1.40 us, 400 B |
| `tog side vis` | 3.14 us | 512 B | 3.09 us, 872 B |
| `'side` | 122 ns | 328 B | 141 ns, 576 B |
| `^tog` | 78 ns | 208 B | 109 ns, 456 B |
| `bility$` | 84 ns | 224 B | 129 ns, 480 B |
| `side !debug` (kept) | 1.48 us | 376 B | 1.56 us, 720 B |
| `side !explorer` (dropped) | 1.40 us | 248 B | 1.45 us, 600 B |
| `qjz` (no field matches) | 369 ns | 0 B | 363 ns, 216 B |

Operator terms replace the fuzzy matcher with an ordinal comparison, so they cost a fraction of a fuzzy term, and a
miss allocates nothing. Parsing the query once per keystroke instead of once per command saves 216 to 360 bytes per
command, which at 2,000 commands is 0.4 to 0.7 MB of garbage per keystroke. The time saved is smaller: up to about
130 ns per command on these queries, and within the noise of a short run for the longer ones.

### Fuzzy matcher, one field

| Case | Time | Allocated |
| --- | ---: | ---: |
| Reject, not a subsequence of a 40-character title | 65 ns | 0 B |
| `gs` in "Go to Settings" | 95 ns | 32 B |
| `über` in "Übersicht öffnen" | 153 ns | 40 B |
| `PSB` in a 40-character title, smart case | 248 ns | 40 B |
| `sett` in a 33-character title | 300 ns | 40 B |
| `tpsb`, an acronym over a 40-character title | 333 ns | 40 B |
| A word in a 107-character description | 908 ns | 48 B |
| A 20-letter query in a 40-character title | 1.12 us | 104 B |
| An 8-letter query in 2,000 characters of prose | 35 us | 223 KB |

A hit allocates only the array of matched positions it returns. The score matrices are reused per thread up to 16K
cells. A 2,000-character field is past that, so every call allocates fresh matrices and costs 35 us, against under
a microsecond for a description. Long articles are better kept out of searchable fields.

### Shortcuts

Parsing a shortcut takes 90 to 110 ns and 376 to 488 bytes, and computing its normalized form 26 ns and 200 bytes.
