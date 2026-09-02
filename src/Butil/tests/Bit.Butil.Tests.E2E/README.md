# Bit.Butil end-to-end tests

MSTest + Microsoft.Playwright suite that boots `Bit.Butil.Samples.Web` (Blazor WASM) as a child process and exercises two deterministic harness pages. Uses the **Microsoft.Testing.Platform** runner (mandated by the repo `global.json`) via `EnableMSTestRunner`.

## First-time setup

```powershell
# Restore + build.
dotnet build .\Bit.Butil.Tests.E2E.csproj

# Install the Playwright-managed Chromium (skip if you'll use a system browser, see below).
pwsh .\bin\Debug\net10.0\playwright.ps1 install chromium
```

## Running

```powershell
dotnet test .\Bit.Butil.Tests.E2E.csproj
```

### Browser selection (environment variables)

The test base class manages Playwright directly and reads configuration from env vars (this is more reliable than runsettings under the MTP runner):

| Variable | Effect |
| --- | --- |
| `BUTIL_E2E_CHANNEL` | Launch an installed browser channel instead of the bundled Chromium - e.g. `chrome`, `msedge`. Handy when the Playwright download is blocked. |
| `BUTIL_E2E_EXECUTABLE` | Full path to a chromium-family executable. |
| `BUTIL_E2E_HEADED` | Set to `1` to watch the run in a visible window. |
| `BUTIL_E2E_BASE_URL` | Point at an already-running demo server (e.g. `https://localhost:5041`) instead of auto-launching one. |

Example - run against a system Chrome with no Playwright download:

```powershell
$env:BUTIL_E2E_CHANNEL = "chrome"
dotnet test .\Bit.Butil.Tests.E2E.csproj
```

## Layout

* `Infrastructure/DemoServerFixture.cs` - assembly-level `[SetUpFixture]` that starts the demo on a free TCP port and tears it down at the end of the run.
* `Infrastructure/ButilHarnessTestBase.cs` - self-managing Playwright base class (launch + context + page) that reads the env vars above.
* `Infrastructure/ButilPageTest.cs` / `ButilObserversPageTest.cs` - thin bases pinning each harness route.
* `*Tests.cs` - narrowly-scoped tests grouped by Butil surface.
* `LazyScriptsTests.cs` - opens `/e2e?lazy=1`, which starts the sample with no `bit-butil.js` on the page and
  `BitButil.UseLazyScripts()` on (see `Bit.Butil.Samples.Web/Program.cs` and `index.html`), and proves the
  per-module `import()` path in a real browser: modules arrive on first use, one at a time, one file each
  (a module carries its own dependencies, so there is no request per helper), nothing that was not called
  is fetched, and the calls behave as in bundle mode.
* `InteropContractTests.cs` - runs `Infrastructure/verify-interop-contract.mjs` under Node: every
  `BitButil.x.y` identifier the C# side invokes must resolve against the bundle **and** against its own
  lazy-loadable module file evaluated on its own.

### Running against a trimmed publish

`Bit.Butil.Samples.Web` imports Bit.Butil's consumer-side build logic by hand (see its csproj), so a
`dotnet publish -c Release` of it goes through the same publish-time bundle trimming a NuGet consumer gets -
its `bit-butil.js` holds only the modules the trimmed `Bit.Butil.dll` still calls. Serve the published
`wwwroot` with any static server (SPA fallback to `index.html`) and point the suite at it to run every
test, bundle and lazy, against that output:

```powershell
$env:BUTIL_E2E_BASE_URL = "http://127.0.0.1:5199"
dotnet test .\Bit.Butil.Tests.E2E.csproj
```
* `ci/bit.ci.Butil.e2e.yml` - a ready-made workflow (triggered by any `src/Butil/**` change) that is **not currently enabled**: CI does not run this suite today. To turn it on, copy the file into `.github/workflows/` and enable it there.

## Harness pages

Two deterministic pages live in `Bit.Butil.Samples.Core/Pages`:

* `/e2e` - storage (round-trip, typed JSON, removeItem, length/key/containsKey, clear), cookie, crypto (UUID, random bytes, SHA-256, AES-GCM, AES-CBC, HMAC, ECDSA, PBKDF2), performance.now, window (base64, inner size, secure context, matchMedia), document (title, visibility/charset/url), location (href + protocol/pathname/origin), history (pushState, replaceState + state, scrollRestoration),
  AbortController (abort + reason, listeners including a late one, a shared signal, `Any`, `Timeout`, and
  that disposing releases without aborting), WebSocket (sub-protocol negotiation, text and binary round
  trips, state/url/buffered, closing from either end with an application code, and a refused scheme).
  The WebSocket controls talk to the echo endpoint `Infrastructure/WebSocketEchoFixture.cs` hosts in the
  test process - the sample under test is a standalone WebAssembly host with no server side to add one
  to, and a public echo service would make the suite need the internet. Its URL reaches the page as
  `/e2e?ws=...`. Worker (echo with a name, transferred bytes both ways, an uncaught throw that leaves the
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
  exact numbers. Scheduler (a single frame, a loop that stops when disposed, an idle callback reporting
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

## Why a custom harness page?

Real Butil pages (`/clipboard`, `/notification`, …) trigger permission prompts that can't be granted reliably in headless. The harness pages only exercise APIs that work with no user gesture and expose outputs through one stable `#status` element so the test selectors don't have to chase per-feature DOM.
