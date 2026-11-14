# Bit.BevTools end-to-end tests

The tests in `Bit.BevTools.Tests` run `Bit.BevTools.Loader` without a browser: `LoaderTests` builds a service
collection, opens BevTools through `BevToolsLoader.OpenAsync` and renders `<BevToolsLoaderPanel />` with bUnit, with
`Bit.BevTools` already loaded into the test process. That covers what the loader decides and leaves out everything that
only exists in a published WebAssembly app:

- only a publish trims the client, fingerprints `Bit.BevTools.wasm` and writes the `BlazorWebAssemblyLazyLoad` mark into
  the boot configuration, so nothing proves that the app really starts without `Bit.BevTools`, that `OpenAsync` finds
  it through `LazyAssemblyLoader` and downloads it once, or that the trimmer kept what BevTools reflects over once it
  arrives;
- the attach never meets a renderer that has been running for a while: the components that already exist, read from
  it, and the pages that render afterwards;
- the server never prerenders the layout, where the loader runs in a process that serves many users.

This suite publishes nothing itself: it boots the lazy loading sample (`Samples/Lazy/Bit.BevTools.Samples.Lazy`, a Blazor
Web App whose pages run on WebAssembly) from its publish output, in the Production environment, and drives it in
Chromium with Playwright. It asserts what the browser fetched (the requests for `_framework/Bit.BevTools.<hash>.wasm`),
what the pages show, and what BevTools' own panel shows about the app, and it fails a test on any console error,
unhandled exception or failed request.

## What it proves

| Suite | What it covers |
|---|---|
| `LazyLoadingTests` | The published lazy loading sample - trimmed, `Bit.BevTools.wasm` marked `BlazorWebAssemblyLazyLoad`, `AddBitBevToolsLoaderServices` in the browser instead of `AddBitBevToolsServices`, `<BevToolsLoaderPanel />` in the layout and the app's own "Open dev tools" button - as the README's "Opening it on demand, and lazy loading" section sets it up: the app runs, moves between its pages and renders without ever downloading `Bit.BevTools.wasm`; the button has `BevToolsLoader.OpenAsync` download it once, attach it to the running app and open its panel, whose component tree lists the components that existed before the attach (and none of BevTools' own) and the page rendered after it, and whose Routing tab names the current page; closing and opening it again, and navigating meanwhile, downloads nothing again. A deep link is prerendered by the server, which registers the loader too and, as a process that serves many users, offers no button outside Development; the browser's copy of the page does, and opens BevTools on that page. Published with AOT (`BEVTOOLS_E2E_LAZY_AOT=1`), the sample leaves the lazy mark out as the README says an AOT build must, and the same tests check that the loader then takes `Bit.BevTools` from the assemblies the app started with, downloading nothing on demand. |

## Running locally

From `src/Utilities/BevTools` (inside `src`, so `src/global.json` selects the Microsoft.Testing.Platform runner):

```bash
# The suite runs the lazy loading sample's publish output: publish it first, in the configuration the tests run in.
dotnet publish Samples/Lazy/Bit.BevTools.Samples.Lazy/Bit.BevTools.Samples.Lazy.csproj -c Debug
dotnet build Tests/Bit.BevTools.Tests.E2E/Bit.BevTools.Tests.E2E.csproj

# Once per machine (and after a Playwright update): the browser Playwright drives.
pwsh Tests/Bit.BevTools.Tests.E2E/bin/Debug/net11.0/playwright.ps1 install chromium

cd Tests/Bit.BevTools.Tests.E2E && dotnet test

# One test
dotnet test --filter "FullyQualifiedName~A_deep_link"
```

The sample is started by the first test, from its publish folder (`bin/<configuration>/net11.0/publish`) in the
Production environment, where BevTools waits to be attached on demand
(`src/Utilities/Shared/Tests/E2E/LazyLoadingSample.cs`, the harness every lazy-loadable library shares), shared by the
tests and stopped when the run ends. `BEVTOOLS_E2E_LAZY_BASE_URL` points the tests at a copy that is already running
instead: one started from its publish folder with `dotnet Bit.BevTools.Samples.Lazy.dll --urls http://127.0.0.1:5494`.
Not one started with `dotnet run`, which runs in Development: there the server registers BevTools at startup, as the
README's setup says, and prerenders the "Open dev tools" button the tests expect only once the page runs in the browser.

Each test gets a browser context of its own, in which every request that leaves the sample's origin is answered with
an empty response, so a machine without internet access runs the suite just the same. The tests run one at a time.
When no browser can be launched or the sample has not been published, every test reports **Inconclusive** with the
reason. On a build server (`CI=true`) the same tests **fail** instead, so a suite that never ran cannot pass.

The AOT leg needs the `wasm-tools` workload, installed from inside `src` so it lands in the SDK `src/global.json`
selects:

```bash
dotnet workload install wasm-tools
dotnet publish Samples/Lazy/Bit.BevTools.Samples.Lazy/Bit.BevTools.Samples.Lazy.csproj -c Release -p:RunAOTCompilation=true
dotnet build Tests/Bit.BevTools.Tests.E2E/Bit.BevTools.Tests.E2E.csproj -c Release
cd Tests/Bit.BevTools.Tests.E2E && BEVTOOLS_E2E_LAZY_AOT=1 dotnet test -c Release --no-build
```

The workflow `.github/workflows/ci.bevtools.e2e.yml` runs both in Release: the `e2e` job against the trimmed
publish and the `lazy-loading-aot` job against the AOT-compiled one. It is started by hand (`workflow_dispatch`) or from
All Utilities E2E CI (`all.utilities.e2e.ci.yml`), never on a pull request: the build-and-unit pipeline is
`ci.bevtools.yml`.

```bash
dotnet publish src/Utilities/BevTools/Samples/Lazy/Bit.BevTools.Samples.Lazy/Bit.BevTools.Samples.Lazy.csproj -c Release
dotnet build src/Utilities/BevTools/Tests/Bit.BevTools.Tests.E2E/Bit.BevTools.Tests.E2E.csproj -c Release
pwsh src/Utilities/BevTools/Tests/Bit.BevTools.Tests.E2E/bin/Release/net11.0/playwright.ps1 install --with-deps chromium
cd src/Utilities/BevTools/Tests/Bit.BevTools.Tests.E2E && dotnet test -c Release --no-build --report-trx --report-trx-filename bevtools-e2e.trx
```

### Environment variables

| Variable | Effect |
|---|---|
| `BEVTOOLS_E2E_CONFIGURATION` | The configuration the lazy loading sample was published in; defaults to the test assembly's own. |
| `BEVTOOLS_E2E_LAZY_BASE_URL` | Drive an already running lazy loading sample instead of booting its publish output. |
| `BEVTOOLS_E2E_LAZY_AOT=1` | The lazy loading sample was published with `-p:RunAOTCompilation=true`, which leaves its lazy mark out: the tests then check that `Bit.BevTools` comes with the app and nothing is downloaded on demand. |
| `BEVTOOLS_E2E_BROWSER` | The engine: `chromium` (the default), `firefox` or `webkit`, each the one Playwright installed (`playwright.ps1 install firefox webkit`). |
| `BEVTOOLS_E2E_CHANNEL` / `BEVTOOLS_E2E_EXECUTABLE` | Use an installed Chrome/Edge (`chrome`, `msedge`), Playwright's headless shell (`chromium-headless-shell`), or a specific Chromium binary. Without either, the full Chromium Playwright installed is used, then its headless shell, falling back to an installed Chrome, then Edge. |
| `BEVTOOLS_E2E_HEADED=1` | Show the browser. |
