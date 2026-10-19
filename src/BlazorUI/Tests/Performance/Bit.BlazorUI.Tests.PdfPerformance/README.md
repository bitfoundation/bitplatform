# PDF engine and source benchmarks

This console benchmark measures parsing, page-tree construction, first-page HTML/canvas builds, and direct stream/lexer reads. It records every elapsed-time/allocation sample and output hashes as JSON. It uses the native C# engine; file I/O, browser painting, HTTP transfer, and forced GC between samples are outside the timings. Rendering metrics over the reference model have warm resource caches; `loadAndFirstPage*` creates a fresh document for each sample.

`directStreamByteReadControl` measures an 8 MiB buffer through the array-backed `BitPdfStream.GetByte()` path as a control for timing noise. It does not isolate `BitPdfByteSourceReader.ReadByte()`; parsing and rendering workloads exercise the byte-source integration. Earlier JSON output named this same control `hotByteRead`. The workload is unchanged.

Build from this directory, where `src/global.json` selects the repository SDK:

```powershell
dotnet build -c Release -p:GeneratePackageOnBuild=false
dotnet bin/Release/net10.0/Bit.BlazorUI.Tests.PdfPerformance.dll 15 10 ../../../Demo/Client/Bit.BlazorUI.Demo.Client.Core/wwwroot/samples/article.pdf > C:/outside-checkout/article-results.json
```

Pass additional PDF paths to include more fixtures. Use meaningful documents with actual page resources. Keep large/private fixtures and result files outside the checkout. The bundled `hello-world.pdf`, `sample-pdf.pdf`, and `article.pdf` are useful reproducible inputs. Include damaged xref/recovery workloads when changing raw scans.

To compare revisions, use the same harness, SDK, runtime, configuration, fixtures, and machine. `PdfExtrasProject` can select the Extras project in another worktree without changing the harness:

```powershell
dotnet build -c Release -p:GeneratePackageOnBuild=false -p:PdfExtrasProject=C:/path/to/baseline/src/BlazorUI/Bit.BlazorUI.Extras/Bit.BlazorUI.Extras.csproj -o C:/outside-checkout/baseline-benchmark
```

Save each engine's compiled output separately. Alternate baseline/after process order across several pairs, with warmup and repeated samples. Report medians and distributions, allocation changes, matching hashes, and timing noise. This is a manual diagnostic, with no timing assertions in the test suite.

For an additional controlled JIT experiment, set `$env:DOTNET_TieredCompilation='0'` identically for both engines. Optional `PDF_BENCH_AFFINITY` is a decimal processor mask on Windows/Linux; it affects only the benchmark process. Record these settings with the results, and do not equate a diagnostic configuration with every deployment runtime.

Allocated bytes are cumulative managed allocations on the synchronous engine thread, including renderer/decoded-resource allocations; they are not peak resident PDF-byte cache usage. The reported process peak working set includes fixture buffers, decoded resources, runtime and rendering state. These results do not prove HTTP Range partial loading or a total-memory ceiling.

Use separate processes to compare actual opening costs and peak memory of buffered files and the seekable backend:

```powershell
dotnet bin/Release/net10.0/Bit.BlazorUI.Tests.PdfPerformance.dll --source buffered 15 10 C:/outside-checkout/large.pdf > C:/outside-checkout/buffered-source.json
dotnet bin/Release/net10.0/Bit.BlazorUI.Tests.PdfPerformance.dll --source seekable 15 10 C:/outside-checkout/large.pdf > C:/outside-checkout/seekable-source.json
```

These modes include file opening, reads, the page tree and the metadata/sidebar preparation used by the viewer. Timings stop before disposal. SHA256 calculation streams the fixture in both modes. The seekable process never creates a whole-file array for setup or comparison. Use one large fixture per process when comparing opening peak working set; alternate process order and retain repeated samples. `rawStoragePeakBytes` reports the buffered input length or the seekable source's peak accounted raw bytes (cached, pinned, in-flight and cross-block copies). The current internal cache budget is 16 MiB with 64 KiB blocks. Decoded resources, rendering output, cache metadata and explicit caller-owned arrays are separate allocations, so neither this counter nor the process working set is a bound on total viewer memory.

The stream benchmark compiles calls to the actual internal backend once before sampling. It does not add a public `BitPdfDocument.Load(Stream)` API. Compare output hashes, page counts and warnings across modes as well as timings and allocations. The original invocation above continues measuring the buffered engine without file transfer or initial array allocation in its timings.
