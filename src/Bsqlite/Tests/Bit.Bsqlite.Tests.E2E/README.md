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
  tabs sharing a file through Web Locks, no import of a database Bit.Besql left behind;
- the demo website - prerendered on the server, hydrated on WebAssembly - never runs.

This suite does all of that in a real browser with Playwright. The library suites drive the library's own modules
through small test pages, the same way an application's `IJSRuntime` does; `DemoTests` boots the demo server and drives
its pages.

## What it proves

| Suite | What it covers |
|---|---|
| `EngineTests`, `EngineBehaviourTests`, `EngineConformanceTests` | Build negotiation and the pinned SQLite version; one database per worker; the statement cache, engine-reported changes, counters, open-time configuration, snapshots; the conformance rules on the in-memory VFS. |
| `BinaryWireTests`, `InteropTests`, `WorkerTests` | The binary batch and reply format end to end, the interop envelope that keeps SQLite result codes across the boundary, and the worker's request queue, failures, guards and lifecycle. |
| `FunctionsTests` | The `ef_*` decimal functions, aggregates, the `EF_DECIMAL` collation and `REGEXP`, against .NET's answers. |
| `LiveQueryTests` | Change notifications from the update and commit hooks, in one tab and across tabs. |
| `OpfsTests`, `IndexedDbTests`, `CacheStorageTests`, `StorageIsolationTests` | Each backend's persistence, crash safety, admin surface and multi-tab behaviour, the Bit.Besql import, and that two backends never see each other's data under the same name. |
| `SubPathTests` | Every module is reached relative to the document, so an app under `<base href="/nested/">` works unchanged. |
| `DemoTests` | Every demo page is prerendered, hydrates and opens its database without a console error; moving the database between backends from the session bar keeps the data and the binding sticks; the browser and the server seed the same catalog through the same model. |
| `SoakTests` (category `Soak`) | Per storage, a worker killed mid-transaction a thousand times, and four then eight tabs committing at once, each ending on `PRAGMA integrity_check`. Opt-in: minutes, not seconds. |
| `BenchmarkTests` (category `Benchmark`) | Runs `TestPages/benchmarks.html` per backend and prints its numbers. Opt-in, and never a gate. |

The test pages are served by an in-process Kestrel host (`Infrastructure/AssetHost.cs`) that maps each library's
`wwwroot` under `_content/<package>/` the way a Blazor app does, and the whole layout again under `/nested/`. It sends no
COOP/COEP headers on purpose: the library must work without cross-origin isolation.

## Running locally

From `src/Bsqlite` (inside `src`, so `src/global.json` selects the Microsoft.Testing.Platform runner):

```bash
# Building the suite builds the libraries too: it serves their compiled modules and the engine their restore vendored.
# The demo tests run the demo server's build output, so build that first, in the configuration the tests run in.
dotnet build Bit.Bsqlite.Demo/Server/Bit.Bsqlite.Demo.Server.csproj
dotnet build Tests/Bit.Bsqlite.Tests.E2E/Bit.Bsqlite.Tests.E2E.csproj

# Once per machine (and after a Playwright update): the browser Playwright drives.
pwsh Tests/Bit.Bsqlite.Tests.E2E/bin/Debug/net10.0/playwright.ps1 install chromium

cd Tests/Bit.Bsqlite.Tests.E2E && dotnet test

# One test class, or one test
dotnet test --filter "FullyQualifiedName~.OpfsTests."
dotnet test --filter "FullyQualifiedName~Persistence_survives_a_worker_restart"
```

CI does the same in Release (`.github/workflows/bit.ci.Bsqlite.e2e.yml`), only when started by hand (Actions >
"bit platform CI - Bsqlite E2E" > Run workflow), and uploads the test results (TRX) as an artifact:

```bash
dotnet build src/Bsqlite/Bit.Bsqlite.Demo/Server/Bit.Bsqlite.Demo.Server.csproj -c Release
dotnet build src/Bsqlite/Tests/Bit.Bsqlite.Tests.E2E/Bit.Bsqlite.Tests.E2E.csproj -c Release
pwsh src/Bsqlite/Tests/Bit.Bsqlite.Tests.E2E/bin/Release/net10.0/playwright.ps1 install --with-deps chromium
cd src/Bsqlite/Tests/Bit.Bsqlite.Tests.E2E && dotnet test -c Release --no-build --report-trx --report-trx-filename bsqlite-e2e.trx
```

The pull-request pipeline, `bit.ci.Bsqlite.yml`, runs the build, unit, MCP server and JavaScript tests only.

Without PowerShell 7, the browser can be installed with the Node.js runtime Playwright ships:
`bin/Debug/net10.0/.playwright/node/<platform>/node bin/Debug/net10.0/.playwright/package/cli.js install chromium`.

The tests run one at a time, each in a browser context of its own - a fresh origin storage, so no database, OPFS file
or sticky binding leaks from one test into the next. The demo server is started on the first demo test
(`dotnet Bit.Bsqlite.Demo.Server.dll --urls http://127.0.0.1:<free port>`, in the Development environment so the build
output finds its static web assets) and stopped when the run ends. The demo's Google Fonts requests are answered locally
with nothing, so a runner that cannot reach them is not slowed down or failed by it.

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
| `BSQLITE_SOAK=1` | Run the soak tests. `BSQLITE_SOAK_TABS` (default 8), `BSQLITE_SOAK_ROWS` (default 50) and `BSQLITE_SOAK_KILLS` (default 1000) size them; smaller runs help while investigating a failure. |
| `BSQLITE_E2E_BENCHMARKS=1` | Run the benchmark page per backend. The numbers are written to the test output (`--output detailed`, or the TRX). |
