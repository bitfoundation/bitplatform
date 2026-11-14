# Bit.Bsqlite end-to-end tests

The tests in `Bit.Bsqlite.Tests` run the whole .NET stack - ADO.NET, EF Core, storage selection, the wire format - on
desktop .NET, with Microsoft.Data.Sqlite standing in for the engine (`Bit.Bsqlite.Testing`'s in-process transport) and
the JavaScript side scripted. That covers the .NET half well and leaves out everything that only exists in a browser:

- `bit-bsqlite-host.ts`, the worker, the engine loader and the wire codec never execute, so nothing proves that a batch
  survives the trip into a dedicated worker and back, or that the binary format the .NET side writes is the one the
  worker reads;
- none of the three wa-sqlite builds is ever instantiated, so nothing proves the synchronous build serves OPFS, that
  the asynchronous VFSes get JSPI or Asyncify as the browser allows, or that the function-table patch lets `EF_DECIMAL`
  register;
- OPFS, IndexedDB and Cache Storage are never touched: no persistence across a worker restart, no crash safety, no two
  tabs sharing a file through Web Locks;
- the demo website - prerendered on the server, hydrated on WebAssembly - never runs.

This suite does all of that in a real browser with Playwright. The library suites drive the library's own modules
through small test pages, the same way an application's `IJSRuntime` does; `DemoTests` boots the demo server and drives
its pages; `LazyLoadingTests` boots the published lazy loading sample (`Samples/Lazy`), an app that downloads Bit.Bsqlite
and EF Core only when a page that uses its database opens.

## What it proves

| Suite | What it covers |
|---|---|
| `EngineTests`, `EngineBehaviourTests`, `EngineConformanceTests` | Build negotiation and the pinned SQLite version; the SQL features the build has and leaves out; one database per worker; the statement cache, engine-reported changes, counters, open-time configuration, snapshots; the conformance rules on the in-memory VFS. |
| `BinaryWireTests`, `InteropTests`, `WorkerTests` | The binary batch and reply format end to end, the interop envelope that keeps SQLite result codes across the boundary, and the worker's request queue, failures, guards and lifecycle. |
| `FunctionsTests` | The `ef_*` decimal functions, aggregates, the `EF_DECIMAL` collation and `REGEXP`, against .NET's answers. |
| `LiveQueryTests` | Change notifications from the update and commit hooks, in one tab and across tabs. |
| `OpfsTests`, `IndexedDbTests`, `CacheStorageTests`, `StorageIsolationTests` | Each backend's persistence, crash safety, admin surface and multi-tab behaviour, a snapshot of an open database taken on a worker that has just opened it, and that two backends never see each other's data under the same name. |
| `SubPathTests` | Every module is reached relative to the document, so an app under `<base href="/nested/">` works unchanged. |
| `DemoTests` | Every page in the docs panel, and the landing page, is prerendered, hydrates and opens its database without a console error; an unknown route answers 404 with the site's own page; moving the database between backends from the session bar keeps the data and the binding sticks, even while the page is still loading, and is not offered onto in-memory; a refused write shows SQLite's reason without Blazor's error bar; a backup downloaded from the admin page restores over the database; an unsaved order edit stays out of the grid; a file that is not a database is refused before it is restored; the browser and the server seed the same catalog through the same model. |
| `LazyLoadingTests` | The published lazy loading sample - trimmed, with `Bit.Bsqlite.wasm`, the EF Core assemblies `docs/getting-started.md`'s "Lazy loading on WebAssembly" lists and the library with the app's context and migration marked `BlazorWebAssemblyLazyLoad` and loaded by the router's `OnNavigateAsync`, and nothing of Bsqlite's registered in the browser - as that section sets it up: its home page downloads none of them; navigating to the products page downloads each once (and not again on the way back), and a deep link to it is prerendered by the server (which registers the services with the same `AppDatabase.Register` and resolves the router's `LazyAssemblyLoader`) and then downloads them; either way the page builds Bsqlite's services itself through `GetBitBsqliteServices`, opens and migrates its database and adds a product, which a reload reads back from the browser's storage - all without a console error, an unhandled exception or a failed request. Published with AOT (`BSQLITE_E2E_LAZY_AOT=1`), the sample leaves the lazy marks out as the guide says an AOT build must, and the same tests check that it loads them with the app and nothing on demand. |
| `SoakTests` (category `Soak`) | Per storage, a worker killed mid-transaction a thousand times, and four then eight tabs committing at once, each ending on `PRAGMA integrity_check`. Opt-in: minutes, not seconds. |
| `BenchmarkTests` (category `Benchmark`) | Runs `TestPages/benchmarks.html` per backend and prints its numbers. Opt-in, and never a gate. |

The test pages are served by an in-process Kestrel host (`Infrastructure/AssetHost.cs`) that maps the library's
`wwwroot` under `_content/Bit.Bsqlite/` the way a Blazor app does, and the whole layout again under `/nested/`. It sends no
COOP/COEP headers on purpose: the library must work without cross-origin isolation.

## Running locally

From `src/Utilities/Bsqlite` (inside `src`, so `src/global.json` selects the Microsoft.Testing.Platform runner):

```bash
# Building the suite builds the library too: it serves its compiled modules and the engine and VFSes its restore vendored.
# The demo tests run the demo server's build output, and the lazy loading tests the lazy loading sample's publish
# output, so build and publish those first, in the configuration the tests run in.
dotnet build Demo/Server/Bit.Bsqlite.Demo.Server.csproj
dotnet publish Samples/Lazy/Bit.Bsqlite.Samples.Lazy/Bit.Bsqlite.Samples.Lazy.csproj -c Debug
dotnet build Tests/Bit.Bsqlite.Tests.E2E/Bit.Bsqlite.Tests.E2E.csproj

# Once per machine (and after a Playwright update): the browser Playwright drives.
pwsh Tests/Bit.Bsqlite.Tests.E2E/bin/Debug/net11.0/playwright.ps1 install chromium

cd Tests/Bit.Bsqlite.Tests.E2E && dotnet test

# One test class, or one test
dotnet test --filter "FullyQualifiedName~.OpfsTests."
dotnet test --filter "FullyQualifiedName~Persistence_survives_a_worker_restart"
```

CI does the same in Release (`.github/workflows/bit.ci.Bsqlite.e2e.yml`), only when started by hand (Actions >
"CI Bsqlite E2E" > Run workflow), and uploads the test results (TRX) and the demo's output as an
artifact. Its demo tests drive the demo published the way `bsqlite.demo.cd.yml` publishes it rather than a build
output: only a publish trims the WebAssembly client, so an interop payload the trimmer breaks (an anonymous type whose
constructor lost its parameter names, a getter nothing but System.Text.Json reads) fails there and nowhere else. The
lazy loading tests drive the sample published for the same reason, and because only a publish writes the lazy marks into
the boot configuration:

```bash
dotnet publish src/Utilities/Bsqlite/Demo/Server/Bit.Bsqlite.Demo.Server.csproj -c Release -o demo -p:InvariantGlobalization=true
dotnet publish src/Utilities/Bsqlite/Samples/Lazy/Bit.Bsqlite.Samples.Lazy/Bit.Bsqlite.Samples.Lazy.csproj -c Release
dotnet build src/Utilities/Bsqlite/Tests/Bit.Bsqlite.Tests.E2E/Bit.Bsqlite.Tests.E2E.csproj -c Release
pwsh src/Utilities/Bsqlite/Tests/Bit.Bsqlite.Tests.E2E/bin/Release/net11.0/playwright.ps1 install --with-deps chromium
(cd demo && dotnet Bit.Bsqlite.Demo.Server.dll --urls http://127.0.0.1:5000 &)
cd src/Utilities/Bsqlite/Tests/Bit.Bsqlite.Tests.E2E && BSQLITE_E2E_DEMO_URL=http://127.0.0.1:5000 dotnet test -c Release --no-build --report-trx --report-trx-filename bsqlite-e2e.trx
```

The workflow's `lazy-loading-aot` job publishes the lazy loading sample AOT-compiled instead (it needs the `wasm-tools`
workload), as an app may ship it, and runs `LazyLoadingTests` alone against it:

```bash
dotnet publish src/Utilities/Bsqlite/Samples/Lazy/Bit.Bsqlite.Samples.Lazy/Bit.Bsqlite.Samples.Lazy.csproj -c Release -p:RunAOTCompilation=true
cd src/Utilities/Bsqlite/Tests/Bit.Bsqlite.Tests.E2E && BSQLITE_E2E_LAZY_AOT=1 dotnet test -c Release --no-build --filter "FullyQualifiedName~LazyLoadingTests"
```

Locally, delete the sample's `bin/Release` and `obj/Release` folders before publishing it without AOT again: a publish over
the AOT one's intermediates can ship the assemblies with their IL stripped, and the runtime then fails to start
(`Assertion: should not be reached at ... interp.c`).

The pull-request pipeline, `bit.ci.Bsqlite.yml`, runs the build, unit, MCP server and JavaScript tests only.

Without PowerShell 7, the browser can be installed with the Node.js runtime Playwright ships:
`bin/Debug/net11.0/.playwright/node/<platform>/node bin/Debug/net11.0/.playwright/package/cli.js install chromium`.

The tests run one at a time, each in a browser context of its own - a fresh origin storage, so no database, OPFS file
or sticky binding leaks from one test into the next. The demo server is started on the first demo test
(`dotnet Bit.Bsqlite.Demo.Server.dll --urls http://127.0.0.1:<free port>`, in the Development environment so the build
output finds its static web assets) and stopped when the run ends. The site loads nothing from another origin - its
typefaces are the system's - so a runner without internet access runs the demo tests the same way.

The lazy loading sample is started the same way, from its publish folder (`bin/<configuration>/net11.0/publish`) in the
Production environment, by the first test that drives it (`src/Utilities/Shared/Tests/E2E/LazyLoadingSample.cs`, the
harness every lazy-loadable library shares); `BSQLITE_E2E_LAZY_BASE_URL` points those tests at a copy that is already
running. A sample that has not been published makes them inconclusive - or failed, on a build server - and leaves the
other suites alone.

When no browser can be launched, the assets have not been built or the demo does not start, every affected test
reports **Inconclusive** with the reason. On a build server (`CI=true`) the same tests **fail** instead, so a suite that
never ran cannot pass.

### Other browsers

Chromium is the default and the only browser CI installs. Firefox and WebKit matter most for the storage matrix -
neither has JSPI, so both run the asynchronous VFSes on the Asyncify build - and run the same suite after
`playwright.ps1 install firefox webkit`, with `BSQLITE_E2E_BROWSER=firefox` or `webkit`. Playwright's own WebKit has no
`navigator.storage`, so its OPFS tests report inconclusive, and it drops Cache Storage entries once no context holds the
cache open, so the Cache Storage persistence tests skip there too.

### Environment variables

| Variable | Effect |
|---|---|
| `BSQLITE_E2E_BROWSER` | `chromium` (default), `firefox` or `webkit`. |
| `BSQLITE_E2E_CHANNEL` / `BSQLITE_E2E_EXECUTABLE` | Use an installed Chrome/Edge (`chrome`, `msedge`) or a specific browser binary. Without either, the Chromium Playwright installed is used, falling back to an installed Chrome, then Edge. |
| `BSQLITE_E2E_HEADED=1` | Show the browser. |
| `BSQLITE_E2E_CONFIGURATION` | Build configuration of the demo server to boot; defaults to the test assembly's own. |
| `BSQLITE_E2E_DEMO_URL` | Drive an already running demo (for example one started from the IDE) instead of booting one. |
| `BSQLITE_E2E_LAZY_BASE_URL` | Drive an already running lazy loading sample instead of booting its publish output. |
| `BSQLITE_E2E_LAZY_AOT=1` | The lazy loading sample was published with `-p:RunAOTCompilation=true`, which leaves its lazy marks out: `LazyLoadingTests` then checks that Bit.Bsqlite and EF Core come with the app and nothing is downloaded on demand. |
| `BSQLITE_SOAK=1` | Run the soak tests. `BSQLITE_SOAK_TABS` (default 8), `BSQLITE_SOAK_ROWS` (default 50) and `BSQLITE_SOAK_KILLS` (default 1000) size them; smaller runs help while investigating a failure. |
| `BSQLITE_E2E_BENCHMARKS=1` | Run the benchmark page per backend. The numbers are written to the test output (`--output detailed`, or the TRX). |
