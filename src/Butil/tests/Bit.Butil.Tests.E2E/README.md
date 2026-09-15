# Bit.Butil end-to-end tests

MSTest + Microsoft.Playwright suite that drives two deterministic harness pages (`/e2e` and `/e2e-observers`, in
`Bit.Butil.Samples.Core/Pages`) in a real browser. The same tests run against **every way a Blazor app can host
Bit.Butil** - the host is picked per run with `BUTIL_E2E_HOST`. Uses the **Microsoft.Testing.Platform** runner
(mandated by the repo `global.json`) via `EnableMSTestRunner`.

## Why every host

Bit.Butil's C# is the same everywhere, but what carries a call to the browser is not:

| Host (`BUTIL_E2E_HOST`) | What it is | What only it exercises |
| --- | --- | --- |
| `standalone` (default) | `Bit.Butil.Samples.Web`, a standalone Blazor WebAssembly app | the in-process runtime, `FastInvoke`, the query-string lazy switch |
| `server`, `server-noprerender` | `Bit.Butil.Tests.Harness.Web` in InteractiveServer | every call and callback as a SignalR message; byte arrays and `DotNetObjectReference`s over a circuit; `RemoteJSRuntime` becoming initialized after prerender |
| `wasm`, `wasm-noprerender` | the same host in InteractiveWebAssembly | a WebAssembly client booted from a Web App, after a prerender |
| `auto`, `auto-noprerender` | the same host in InteractiveAuto | a visit that may land on either runtime |
| `hybrid` | `Bit.Butil.Tests.Harness.Hybrid`, a WinForms BlazorWebView (the WebView core MAUI uses) | `WebViewJSRuntime`, assets from the WebView's virtual host, a page that is never prerendered. Windows only |

Static SSR and the prerender pass have no interactivity to drive, so they are covered over HTTP by
`Bit.Butil.Tests.Hosting` (including a sweep that calls every member of every service during a static render).

## First-time setup

```powershell
dotnet build .\Bit.Butil.Tests.E2E.csproj

# Install the Playwright-managed Chromium (skip if you'll use a system browser, see below).
pwsh .\bin\Debug\net10.0\playwright.ps1 install chromium
```

## Running

Run from inside `src` (so `src/global.json` selects the test runner):

```powershell
# The standalone WebAssembly sample (builds and starts it with dotnet run).
dotnet test --project tests\Bit.Butil.Tests.E2E\Bit.Butil.Tests.E2E.csproj

# One render mode of the Web App host (builds the host first).
$env:BUTIL_E2E_HOST = "server"
dotnet test --project tests\Bit.Butil.Tests.E2E\Bit.Butil.Tests.E2E.csproj

# BlazorWebView (Windows, needs the WebView2 Runtime; opens app windows while it runs).
$env:BUTIL_E2E_HOST = "hybrid"
dotnet test --project tests\Bit.Butil.Tests.E2E\Bit.Butil.Tests.E2E.csproj
```

Run a Web App host by hand with one of its launch profiles (`ssr`, `server`, `wasm`, `auto`, `server-lazy`), or
`dotnet run --project tests/Bit.Butil.Tests.Harness.Web -- --ButilHarness:Mode=wasm-noprerender --ButilHarness:Scripts=lazy`.

### Environment variables

The test base class manages Playwright directly and reads configuration from env vars (more reliable than
runsettings under the MTP runner):

| Variable | Effect |
| --- | --- |
| `BUTIL_E2E_HOST` | Which host the run drives - see the table above. A name that is not one of them fails the run. |
| `BUTIL_E2E_FRAMEWORK` | Target framework of the Web App host: `net10.0` (default), `net9.0`, `net8.0`. |
| `BUTIL_E2E_CONFIGURATION` | Build configuration of the harness hosts; defaults to the test assembly's. |
| `BUTIL_E2E_SKIP_BUILD` | `1`: the harness host was built beforehand (as CI does); do not rebuild it. |
| `BUTIL_E2E_PUBLISHED_HOST` | A `dotnet publish` output of `Bit.Butil.Tests.Harness.Web` to run instead of the build output (see the publish gate). |
| `BUTIL_E2E_HYBRID_INSTANCES` | How many BlazorWebView windows run side by side (default 2). A test holds one window, so this is the hybrid run's parallelism. |
| `BUTIL_E2E_BASE_URL` | `standalone` only: point at an already-running deployment (e.g. a trimmed publish of the sample) instead of launching one. |
| `BUTIL_E2E_CHANNEL` | Launch an installed browser channel instead of the bundled Chromium - e.g. `chrome`, `msedge`. Handy when the Playwright download is blocked. |
| `BUTIL_E2E_EXECUTABLE` | Full path to a chromium-family executable. |
| `BUTIL_E2E_HEADED` | Set to `1` to watch the run in a visible window. |

## Layout

* `AssemblySetup.cs` - starts the WebSocket echo endpoint, builds the harness host the run needs, and starts it once
  for the whole run.
* `Infrastructure/HarnessTarget.cs` - the host this run drives, and how a test gets a page of its own on it:
  a browser context against a shared web host (`WebHarnessLease`), or a BlazorWebView window rented from a pool
  (`HybridHarnessLease`, `HybridHostPool`). Lazy script loading is a process-wide toggle in the library, so a Web App
  or hybrid run starts a second host process for `LazyScriptsTests`.
* `Infrastructure/ButilHarnessTestBase.cs` - opens the fixture's harness route, waits until the page reports
  `ready` (which it only does after its first interactive render, so a prerendered page is never clicked before its
  handlers are attached), and fails the test when the Blazor runtime reports an unhandled exception - a terminated
  circuit otherwise shows up only as a status that never arrives.
* `Infrastructure/ButilPageTest.cs` / `ButilObserversPageTest.cs` - thin bases pinning each harness route.
* `*Tests.cs` - narrowly-scoped tests grouped by Butil surface.
* `LazyScriptsTests.cs` - opens `/e2e?lazy=1` with no `bit-butil.js` on the page and lazy scripts on, and proves the
  per-module `import()` path in a real browser: modules arrive on first use, one at a time, one file each (a module
  carries its own dependencies, so there is no request per helper), nothing that was not called is fetched, and the
  calls behave as in bundle mode.
* `InteropContractTests.cs` - runs `Infrastructure/verify-interop-contract.mjs` under Node: every `BitButil.x.y`
  identifier the C# side invokes must resolve against the bundle **and** against its own lazy-loadable module file
  evaluated on its own. Host-independent.
* `publish-gate.sh` - the trimmed/AOT publish of the Web App host, see below.

## Publish gate (trimming / AOT)

```bash
# Publishes the Web App host with its WebAssembly client trimmed (and the bundle trimmed by the reference scan),
# failing on any trim/AOT analysis warning raised inside Bit.Butil.
bash tests/Bit.Butil.Tests.E2E/publish-gate.sh ../../artifacts/butil-harness-trimmed
# AOT leg (needs the wasm-tools workload):
bash tests/Bit.Butil.Tests.E2E/publish-gate.sh ../../artifacts/butil-harness-aot -p:RunAOTCompilation=true

BUTIL_E2E_HOST=wasm BUTIL_E2E_PUBLISHED_HOST=$PWD/../../artifacts/butil-harness-trimmed \
  dotnet test --project tests/Bit.Butil.Tests.E2E/Bit.Butil.Tests.E2E.csproj
```

The standalone sample can be run trimmed as well: `Bit.Butil.Samples.Web` imports Bit.Butil's consumer-side build
logic by hand (see its csproj), so a `dotnet publish -c Release` of it goes through the same publish-time bundle
trimming a NuGet consumer gets. Serve the published `wwwroot` with any static server (SPA fallback to `index.html`)
and set `BUTIL_E2E_BASE_URL` to it.

CI runs all of the above in `.github/workflows/bit.ci.Butil.e2e.yml`.

## Harness pages

Two deterministic pages live in `Bit.Butil.Samples.Core/Pages`:

* `/e2e` - storage (round-trip, typed JSON, removeItem, length/key/containsKey, clear), cookie, crypto (UUID, random bytes, SHA-256, AES-GCM, AES-CBC, HMAC, ECDSA, PBKDF2), performance.now, window (base64, inner size, secure context, matchMedia), document (title, visibility/charset/url), location (href + protocol/pathname/origin), history (pushState, replaceState + state, scrollRestoration),
  AbortController (abort + reason, listeners including a late one, a shared signal, `Any`, `Timeout`, and
  that disposing releases without aborting), WebSocket (sub-protocol negotiation, text and binary round
  trips, state/url/buffered, closing from either end with an application code, and a refused scheme).
  The WebSocket controls talk to the echo endpoint `Infrastructure/WebSocketEchoFixture.cs` hosts in the
  test process - the standalone sample has no server side to add one to, and a public echo service would make
  the suite need the internet. Its URL reaches the page as `/e2e?ws=...`, on every host. Worker (echo with a name,
  transferred bytes both ways, an uncaught throw that leaves the
  worker running, a port handed over, a shared worker's connection count, and posting to a terminated one)
  and MessageChannel (queue-then-start ordering, binary across a port, and a transferred port going dead on
  the sending side). The worker scripts live in `Bit.Butil.Samples.Web/wwwroot/workers` - a worker runs a
  script you supply, so the harness has to supply one. WindowMessaging (a round trip through an iframe,
  bytes, a port transferred to the frame, a top-level page being its own parent, and a message from an
  origin not on the allow-list never reaching the callback), against
  `Bit.Butil.Samples.Web/wwwroot/frames/e2e-frame.html`. Streams (a response body read to its end,
  reading locking the stream so `Tee` refuses, `Tee` giving both branches every chunk, a gzip/gunzip
  round trip into a C# sink, writing by hand, and a 404 reporting its status), against
  `Bit.Butil.Samples.Web/wwwroot/data/stream-sample.txt` - exactly 1024 bytes, so the assertions are
  exact numbers. (The Web App and hybrid hosts serve those same files at the same paths, from that same folder.)
  Scheduler (a single frame, a loop that stops when disposed, an idle callback reporting
  slack on an idle page and `DidTimeout` on a busy one, a posted task, a task whose signal is already
  aborted, and `Yield`/`IsInputPending`). Canvas (buffer sizing, draw-then-export, and capture with the
  aspect ratio preserved) - measured by reading the dimensions back out of the exported PNG's own IHDR
  header, so the assertion is the picture rather than "some bytes came back". Dom (query, create and
  append, element-wise traversal, `SetText` not parsing markup, and - the canary for the one Blazor
  internal Butil leans on - `AsElementReference` producing a reference the element extensions resolve)
  and ShadowDom (querying inside an open root, the document not reaching in, and a closed root being
  unreachable). Css (computed values, `Supports`/`Escape`, a stylesheet rule applying and being deleted,
  and a custom highlight counting its occurrences). DataTransfer - the drop is dispatched as a synthetic
  `DragEvent` carrying a real `DataTransfer`, since a headless browser has no mouse to drag with, and
  everything after the event is the path a user's drop takes. WebRtc, where both peers live in the page
  and hand each other their ICE candidates directly: a real loopback connection, a data channel round
  trip, and `getStats`, with no network and nothing to prompt for.
* `/e2e-observers` - PerformanceObserver, performance mark/measure/getEntries/clearMarks, StorageManager, NetworkInformation, IntersectionObserver, ResizeObserver, MutationObserver, BroadcastChannel, IndexedDB, CacheStorage, Web Locks, Object URLs, CookieStore, navigator/userAgent/screen platform getters.

Both expose stable element ids and funnel results through a single `#status` element so test selectors stay simple.
`#status` reads `starting` until the page's first interactive render and `ready` from then on; on `/e2e` it also
carries `data-platform` (`browser` or `dotnet`) and `data-renderer` (`RendererInfo.Name`, .NET 9+), which
`HostingModeTests` uses to prove a run is exercising the host it asked for.

## Why a custom harness page?

Real Butil pages (`/clipboard`, `/notification`, …) trigger permission prompts that can't be granted reliably in headless. The harness pages only exercise APIs that work with no user gesture and expose outputs through one stable `#status` element so the test selectors don't have to chase per-feature DOM.

## Hybrid notes

The hybrid host is started with `--remote-debugging-port <port> --user-data-folder <private folder>`, which it
applies through `BlazorWebViewInitializing` (the WebView2 API), and the suite attaches with `ConnectOverCDPAsync`.
The `WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS` environment variable is not an option: since WebView2 Runtime 150, an
elevated process - such as a GitHub-hosted runner - ignores `--remote-debugging-port` given that way.

A BlazorWebView has exactly one page, so a test rents a whole window for its duration; the pool clears the page's
storage and any device emulation before lending a window again, and the navigation to the harness route reloads
the page with a fresh DI scope. It needs the WebView2 Runtime (Windows 11 has it; Windows Server images, including
GitHub's `windows-latest`, may only have the Edge browser - CI installs the runtime first). When the WebView cannot
start, the host exits with the reason on stderr, and the failing test quotes it.
