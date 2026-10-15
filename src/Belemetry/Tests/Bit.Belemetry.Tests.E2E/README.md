# Bit.Belemetry cross-host test suites

The unit tests in `Bit.Belemetry.Tests` (MSTest + bUnit) and `bit-belemetry-js` (vitest + happy-dom) cover the
model, the serializers, the consent state machine and the script's modules one by one. They leave out everything that
only exists once a real host and a real browser are involved:

- `<BelemetryScript />` rendered into a real HTTP response in each render mode, and the script auto-starting from it
  before Blazor boots;
- real browser APIs: `PerformanceObserver` (web vitals), `history` and Blazor's enhanced navigation (page views),
  `fetch` with a `traceparent` the server actually receives, `window.onerror`, `localStorage`/`sessionStorage`;
- the .NET pipeline running inside the WebAssembly runtime, and the Relay transport of a BlazorWebView;
- real exports over HTTP, decoded by a receiver, in both the OTLP/JSON and the Azure Monitor envelope formats.

The browser script never throws into the page by design, so a broken integration rarely fails loudly: it just records
nothing. These suites therefore assert on **what arrives at the receiver**.

## Layout

| Project | What it is |
|---|---|
| `Bit.Belemetry.Tests.Harness` | Razor class library: the harness pages (stable element ids, one `#status` element), the Belemetry configuration every host uses (`AddBelemetryHarness`) and `harness.js` (browser-side actions that also work under static SSR). net8.0 / net9.0 / net10.0. |
| `Bit.Belemetry.Tests.Harness.Web` (+ `.Client`) | One Blazor Web App serving the harness in any render mode, picked with `--BelemetryHarness:Mode=`: `ssr`, `server`, `server-noprerender`, `wasm`, `wasm-noprerender`, `auto`, `auto-noprerender`. It is also the **capturing receiver** every harness host exports to. |
| `Bit.Belemetry.Tests.Harness.Hybrid` | WinForms `BlazorWebView` host (same WebView core as MAUI), for the Relay transport. Windows only. |
| `Bit.Belemetry.Tests.Hosting` | In-process HTTP tests (`WebApplicationFactory`) against the harness host in every mode and against the three hosting samples. All three target frameworks. |
| `Bit.Belemetry.Tests.E2E` | Playwright suites: every web render mode in Chromium, and the hybrid host through WebView2's DevTools port. |

### The capturing receiver

`Bit.Belemetry.Tests.Harness.Web` maps, next to the harness pages:

| Endpoint | What it does |
|---|---|
| `POST /test-collector/v1/{traces,logs,metrics}` | OTLP/HTTP. JSON bodies are decoded; protobuf bodies are kept as requests only. |
| `POST /test-collector/v2.1/track` | Azure Monitor ingestion (the harness's connection string points its `IngestionEndpoint` here). |
| `GET /test-collector/received[?session=&marker=]` | Everything decoded so far as JSON: requests, spans, logs, metrics, envelopes, echoes. `session` keeps the records of one browser session (`session.id` / `ai.session.id`), `marker` those carrying that `harness.marker`. |
| `POST /test-collector/reset` | Forgets everything. |
| `GET /test-api/echo` | Answers with the request headers, so a test sees the `traceparent` the fetch instrumentation added. |
| `GET /test-api/server-telemetry?marker=` | Writes a server-side `ILogger` record, an `Activity` and a `Meter` measurement. |

The harness exports with short intervals (browser and .NET flush every 500 ms, metrics every 2 s), persistence off,
and environment variables ignored. Consent is controllable from a test with `?consent=all|none|<bit mask>` (applied by
`harness.js` as soon as the script started), the `#js-consent-*` buttons (any mode), or the `#consent-*` buttons
(`IBelemetryConsent`, interactive modes).

`?marker=<value>` puts `harness.marker = <value>` on every record a .NET button of the home page produces, so a test
finds its own records in a receiver shared with other tests.

## What runs where

| Area | Hosting (HTTP) | E2E web modes | E2E hybrid |
|---|---|---|---|
| `<BelemetryScript />` prerendered in the head with a parsable config (every contract key) in every render mode | yes | - | - |
| The config reflects the options (service, transport, exporters, ignore list of exporter URLs, no server instance id) | yes | - | - |
| `_content/Bit.Belemetry/bit-belemetry.js` served as JavaScript with the documented exports | yes | - | - |
| Server pipeline on: `ILogger`, `Activity` (with the ASP.NET Core parent span) and `Meter` exported, as OTLP and envelopes | yes | - | - |
| Server pipeline off (default): nothing from .NET exported | yes | - | - |
| Nothing sent before consent; granting sends the held records | - | yes | - |
| Revoking stops sending and clears the stored session, pseudo id and user id | - | yes | C# revoke reaches the script |
| Page views: initial load, client-side / enhanced navigation, counted once | - | yes | - |
| Clicks with `data-bit-belemetry-name`; `[data-bit-belemetry-ignore]` skipped | - | yes | relayed |
| A JS error becomes an `exception` record and an `Exception` envelope | - | yes | - |
| `fetch` client span; same-origin request carries `traceparent` (seen by the server); exports not traced | - | yes | - |
| Web vitals (TTFB, FCP, LCP, CLS) when the page is hidden; `Metric` envelopes | - | yes | - |
| `PageView` envelope with the Azure tags | - | yes | relayed |
| C# `IBelemetry.TrackEvent`: through the browser (Server), through the .NET pipeline (WebAssembly) | - | yes | yes |
| .NET `ILogger` and `Activity` records from the WebAssembly runtime, with the page's session | - | wasm | `ILogger` |
| The hosting samples boot, render the config and serve their assets | yes (net10.0) | - | - |

## Running locally

```bash
cd src/Belemetry

# HTTP-level suite, all target frameworks
dotnet test Tests/Bit.Belemetry.Tests.Hosting/Bit.Belemetry.Tests.Hosting.csproj

# Browser suites (builds the harness hosts first; the hybrid host only on Windows)
dotnet build Tests/Bit.Belemetry.Tests.E2E/Bit.Belemetry.Tests.E2E.csproj
pwsh Tests/Bit.Belemetry.Tests.E2E/bin/Debug/net10.0/playwright.ps1 install chromium
dotnet test Tests/Bit.Belemetry.Tests.E2E/Bit.Belemetry.Tests.E2E.csproj

# One mode only
dotnet test Tests/Bit.Belemetry.Tests.E2E/Bit.Belemetry.Tests.E2E.csproj --filter "FullyQualifiedName~.WebAssemblyModeTests."
```

`bit-belemetry.js` is generated: building `Bit.Belemetry` runs esbuild (`npm run build` in that folder does the same).

Run a harness host by hand with one of its launch profiles (`ssr`, `server`, `server-pipeline`, `wasm`, `auto`), or
`dotnet run --project Tests/Bit.Belemetry.Tests.Harness.Web -- --BelemetryHarness:Mode=server-noprerender`, and open
`/test-collector/received` next to the page to watch what it exports.

### Environment variables

| Variable | Effect |
|---|---|
| `BELEMETRY_E2E_HOSTS` | Comma-separated subset of `ssr`, `server`, `wasm`, `auto`, `hybrid` to run; the other suites report Inconclusive. Defaults to all. |
| `BELEMETRY_E2E_FRAMEWORK` | Target framework of the web harness host: `net10.0` (default), `net9.0`, `net8.0`. |
| `BELEMETRY_E2E_CONFIGURATION` | Build configuration of the hosts; defaults to the test assembly's. |
| `BELEMETRY_E2E_SKIP_BUILD=1` | Do not build the hosts before the run. |
| `BELEMETRY_E2E_PUBLISHED_HOST` | Run a `dotnet publish` output of the web host instead of the build output. |
| `BELEMETRY_E2E_CHANNEL` / `BELEMETRY_E2E_EXECUTABLE` | Use an installed Chrome/Edge or a specific Chromium binary. The channel defaults to `chromium`. |
| `BELEMETRY_E2E_HEADED=1` | Show the browser. |

The web hosts listen on `http://localhost:<free port>` rather than `127.0.0.1`: the script's privacy scrubbing replaces
IP addresses in every attribute, URLs included, and the suites assert on the URLs the page reports.

## Hybrid notes

The hybrid suite starts `Bit.Belemetry.Tests.Harness.Hybrid.exe` with
`--collector-origin <the ssr host> --remote-debugging-port <port> --user-data-folder <private folder>`, which the host
applies through `BlazorWebViewInitializing` (the WebView2 API), then attaches with `ConnectOverCDPAsync`. A window opens
while it runs. No web host is registered in that app, so Belemetry detects Hybrid and defaults to the Relay transport:
the WebView's records reach the receiver from the app process.

It needs the WebView2 Runtime (Windows 11 has it). When the WebView cannot start, the host exits with the reason on
stderr, and the failing test quotes it. On other operating systems the hybrid tests report Inconclusive.
