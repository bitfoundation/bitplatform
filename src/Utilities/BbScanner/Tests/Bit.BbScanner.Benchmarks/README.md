# Bit.BbScanner decoder benchmarks

A [BenchmarkDotNet](https://benchmarkdotnet.org) console harness for the number that decides whether a scanner keeps
up with the camera: what one frame costs, end to end and stage by stage, and what it allocates.

The frames are 640 by 480 grayscale images with the symbol centred, rendered by the test kit's encoders - the same
symbols the unit tests round trip against - so every benchmark measures a frame the decoder actually reads.

## What it measures

| Class | What it covers |
|-------|----------------|
| `DecoderBenchmarks` | `BbBarcodeDecoder.Decode` on a frame holding a small QR code, a large one, a noisy one, a Data Matrix, a Code 128, an EAN-13, and nothing at all - each with every format enabled, with QR Code only and with the linear symbologies only, and the bytes each frame allocates. A frame with no symbol in it is the common case while a user is still aiming. |
| `StageBenchmarks` | The stages one at a time: grayscale conversion from RGBA (with and without subsampling), adaptive block binarization, binarizing one scan line, QR detection and decode, Data Matrix detection on an empty frame, and Code 128 on a row with no symbol. |
| `SymbolBenchmarks` | The algorithmic pieces on their own, independent of image size: a QR and a Data Matrix module grid to payload - parsing, de-interleaving, Reed-Solomon and the bit stream. |

## Running it

```bash
# From the repository's src folder. Release is required - a Debug build's numbers are meaningless.
dotnet run -c Release --project Utilities/BbScanner/Tests/Bit.BbScanner.Benchmarks

# A single set, selected with BenchmarkDotNet's glob filter:
dotnet run -c Release --project Utilities/BbScanner/Tests/Bit.BbScanner.Benchmarks -- --filter "*DecoderBenchmarks*"

# A quick smoke run (one iteration, in-process, numbers not meaningful):
dotnet run -c Release --project Utilities/BbScanner/Tests/Bit.BbScanner.Benchmarks -- --filter "*StageBenchmarks*" --job dry --inProcess
```

## The performance gate

`--budget` is a quick check, and `bit.ci.BbScanner.perf.yml` runs it - when started by hand (Actions > "CI BbScanner
Performance" > Run workflow) or from All Utilities CI, not on pull requests, whose runners' timings are too
noisy to fail on: each standard frame - and the
empty frame under the live and the still image defaults, and the still image ones with every symbology - is decoded the
way a live scanner decodes it, four frames to a sample so that work done every few frames is counted, and its time and
allocations are compared with the baseline in `budgets.json`. Time is measured as a multiple of a fixed piece of work
timed in the same run, so a slower machine moves both alike; but the baseline comes from a developer's machine and other
processors keep slightly different ratios, so a case is reported at twice its baseline and fails at three times, after
two more attempts. Allocations do not depend on the machine and are held to a quarter over the baseline plus a
kilobyte. It takes a few seconds and exits with 1 on a regression.

```bash
# From the repository's src folder.
dotnet run -c Release --project Utilities/BbScanner/Tests/Bit.BbScanner.Benchmarks -- --budget

# After a change meant to move the numbers: measure (best of three rounds) and rewrite budgets.json.
dotnet run -c Release --project Utilities/BbScanner/Tests/Bit.BbScanner.Benchmarks -- --update-budgets
```

It catches a stage that became several times slower or a frame that started allocating, not a few percent: for those,
compare BenchmarkDotNet runs.

## Verification

Before measuring anything the harness decodes every frame once and fails when one of them no longer decodes
(`Verification.cs`): a benchmark that silently stops finding the symbol measures the failure path instead. Results
(Markdown, CSV and HTML reports) are written to `BenchmarkDotNet.Artifacts` in the current directory.

The indicative numbers, captured on .NET 10, x64, are in the
[Performance](../../README.md#performance) section of the guide. Absolute numbers on WebAssembly are several times
larger; the ratios are what transfer.
