# Buffered PDF engine benchmark

This console benchmark measures parsing, page-tree construction, first-page HTML/canvas builds, and hot byte/lexer reads. It records every elapsed-time/allocation sample and output hashes as JSON. It uses the native C# engine; file I/O, browser painting, HTTP transfer, and forced GC between samples are outside the timings. Rendering metrics over the reference model have warm resource caches; `loadAndFirstPage*` creates a fresh document for each sample.

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
