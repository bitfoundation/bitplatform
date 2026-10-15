# Bit.Belemetry

One lightweight telemetry SDK for Blazor (WebAssembly, Server, Auto, static SSR, Hybrid/MAUI; net8.0/9.0/10.0).
It covers both sides of the app:

- **Browser:** page views, interactions, web vitals, JS errors, and fetch/XHR dependencies.
- **.NET:** `ILogger` logs, `Activity` traces and `Meter` metrics.

It does this **without the OpenTelemetry NuGet packages**. It follows the OpenTelemetry data model and semantic
conventions, and exports through:

- OTLP/HTTP, as protobuf or JSON: OTel Collector, Aspire dashboard, Grafana, Jaeger, Honeycomb, ...
- The native Azure Monitor ingestion endpoint (`/v2.1/track`).
- The console.
- Any custom `IBelemetryExporter`.

Every feature is gated by runtime consent (GDPR), per feature.

`README.md` in this folder is the reference guide, and the MCP server serves it too, so keep it accurate.

## Layout

| Path | What it is |
| --- | --- |
| `Bit.Belemetry/` | The package: Razor class library plus `Scripts/*.ts`, bundled by esbuild into `wwwroot/bit-belemetry.js` (generated, git-ignored) |
| `Bit.Belemetry/buildTransitive/` | `Bit.Belemetry.props`, packed as the package's `build`, `buildMultiTargeting` and `buildTransitive` props: it defaults `MetricsSupport` to `true` (WebAssembly turns it off) and imports the static web assets props the Razor SDK's own file would. Projects here that reference the library as a project and run .NET in WebAssembly or MAUI import it themselves |
| `Bit.Belemetry/Options/` | `BelemetryOptions` and its sub-option classes |
| `Bit.Belemetry/Consent/` | `BelemetryFeatures`, `IBelemetryConsent`, the consent state machine |
| `Bit.Belemetry/Model/` | Public, OTel-shaped data model: spans, log records, metrics, resource, scope, attributes |
| `Bit.Belemetry/Pipeline/` | Collectors (ActivityListener, MeterListener, ILoggerProvider), processors, batching, sampling |
| `Bit.Belemetry/Exporters/` | OTLP (protobuf + JSON serializers), Azure Monitor (envelopes, live metrics), console, in-memory, HTTP sender + retry + persistence |
| `Bit.Belemetry/Interop/` | The JS bridge (module import, config, relay, unload drain, context sync) |
| `Bit.Belemetry/Components/` | `<BelemetryScript />` (static `<head>` tag) and `<BelemetryInitializer />` (interactive bridge) |
| `Bit.Belemetry.Demo/` | Documentation site (Client) and its host (Server). The host also runs the MCP server at `/mcp` and an in-process OTLP receiver that the demo exports to |
| `Samples/` | Minimal hosting samples: `Core` (shared pages), `Server`, `Wasm`, `Auto`, `Maui` |
| `Tests/` | `Bit.Belemetry.Tests` (MSTest + bunit), `Tests.Hosting`, `Tests.E2E` (Playwright) with `Tests.Harness.*`, `Tests.Mcp`, `bit-belemetry-js` (vitest) |

## Coding style

- Follow the `.editorconfig` at the root of `src`. Every file is **UTF-8 with BOM**, except `*.sh`.
- Use file-scoped namespaces, one top-level type per file named after the type, and primary constructors where they read well.
- Comments explain *why* (trimming, WASM single-threadedness, browser quirks, spec references), not *what*.
- Every public type and member carries XML documentation. The MCP server reflects it.
- **No reflection-based serialization.**
  - OTLP and Azure payloads are written by hand with `Utf8JsonWriter` or `ProtobufWriter`.
  - Interop DTOs go through the source-generated `BelemetryJsonContext`.
  - The package is `IsTrimmable` and `IsAotCompatible`.
- **No threads.** Browser WebAssembly is single-threaded, so the pipeline runs on async loops with timers only.
  `new Thread`, blocking waits and `.Result` are forbidden.
- **The exporter must not instrument itself.** Every export runs inside `BelemetrySuppression.Begin()`, so the
  `System.Net.Http` activities it creates are not sampled. The JS fetch/XHR instrumentation ignores exporter URLs
  and any request that already carries a `traceparent` header (in WASM, .NET's HttpClient has already traced it).

## Contracts shared by C# and TypeScript

These are the wire contracts between `Bit.Belemetry/**/*.cs` and `Bit.Belemetry/Scripts/*.ts`. Change both sides
together, plus the tests on both sides.

### Feature bits (`BelemetryFeatures` in C#, `Features` in TS)

| Name | Bit | Covers |
| --- | --- | --- |
| `Errors` | 1 | JS errors and unhandled rejections; .NET logs at Error/Critical or carrying an exception; `TrackException` |
| `PageViews` | 2 | Page views (history/enhanced navigation, `TrackPageView`) |
| `Interactions` | 4 | Clicks and other user interactions |
| `Performance` | 8 | Web vitals (LCP, INP, CLS, FCP, TTFB), document-load span, resource timing, long tasks |
| `Dependencies` | 16 | Browser fetch/XHR client spans |
| `Sessions` | 32 | A session id that survives reloads (in `sessionStorage`). Without it, the session id lives in memory only |
| `UserTracking` | 64 | `enduser.pseudo.id` (random id persisted in `localStorage`) and `user.id` (authenticated id from `SetUser`) |
| `Logs` | 128 | .NET logs below Error; browser console capture below error |
| `Traces` | 256 | .NET `Activity` spans |
| `Metrics` | 512 | .NET `Meter` metrics |
| `Persistence` | 1024 | Keeping unsent telemetry on the device (`localStorage` / files) for retry after going offline or reloading |
| `Events` | 2048 | Custom events (`TrackEvent`) |

Groups:
- `Diagnostics` = Errors, Logs, Traces, Dependencies
- `Analytics` = PageViews, Interactions, Sessions, UserTracking, Events
- `Monitoring` = Performance, Metrics
- `All` = 4095

Effective features = configured features (the options) AND granted consent. The grant is narrowed to the
configured features on both sides (`IBelemetryConsent.Granted`, `getConsent()`, the persisted choice): a grant of a
feature switched off in configuration is not recorded, so a feature enabled later needs fresh consent.

How consent works:
- **Before a decision** (no persisted choice, and the app hasn't called `SetConsent` yet), only `Consent.Default`
  is granted (default `None`).
- Records for other features are **held in memory** (bounded by `Consent.PendingLimit`, default 500), not sent.
- **After the decision**, held records the user granted are sent and the rest are dropped.
- **Revoking a feature** purges its queued records. Revoking `Persistence` also clears stored requests, revoking
  `Sessions` clears the stored session, and revoking `UserTracking` clears the pseudo id and the user id.

### Storage keys (browser)

| Key | Storage | Gated by | Content |
| --- | --- | --- | --- |
| `bit-belemetry-consent` | localStorage | always (strictly necessary: records the choice itself) | `{"v":1,"features":<int>,"time":<ms>}` |
| `bit-belemetry-session` | sessionStorage | `Sessions` | `{"id":"<32 hex>","start":<ms>,"last":<ms>}` |
| `bit-belemetry-pseudo-id` | localStorage | `UserTracking` | 32 hex chars |
| `bit-belemetry-user-id` | sessionStorage | `UserTracking` | authenticated id from `setUser` |
| `bit-belemetry-queue` | localStorage | `Persistence` | `[{"id","exporter","url","contentType","body"(base64),"time"}]`, capped by `persistence.maxBytes` |

Persisted requests never carry headers (API keys), in the browser queue or in the .NET file store
(`{LocalApplicationData}/bit-belemetry/{service.name}/*.blmreq`, `BelemetryHttpRequest` JSON without `headers`). The
headers are applied from the exporter's current options when a stored request is sent: in .NET from the exporter
named by `exporter`, in the browser from the browser exporter whose URL prefix matches `url`.

### Browser config (C# `BelemetryBrowserConfig`, serialized camelCase; TS `BelemetryConfig`)

`<BelemetryScript />` writes this into `<script type="application/json" id="bit-belemetry-config">`. The module
auto-starts from it on load. `<BelemetryInitializer />` passes the same object to `start(config)`. `start` is
idempotent: a second call only updates the consent defaults and the bridge.

```jsonc
{
  "service": { "name": "MyApp", "version": "1.0.0", "namespace": null, "instanceId": null, "environment": "Production" },
  "resource": { "any.attribute": "value" },             // extra resource attributes
  "sdkVersion": "10.6.2",
  "transport": "direct",                                 // "direct" (JS exports itself) | "relay" (JS hands records to .NET)
  "exporters": [
    { "type": "otlp", "endpoint": "http://localhost:4318", "tracesEndpoint": null, "logsEndpoint": null,
      "protocol": "http/protobuf", "headers": { "x-api-key": "..." }, "compression": "none", "signals": 3 },   // protocol: http/protobuf | http/json ; compression: none | gzip
    { "type": "azureMonitor", "connectionString": "InstrumentationKey=...;IngestionEndpoint=https://...", "maskIp": true,
      "compression": "none", "signals": 3 },
    { "type": "console" }
  ],
  "features": 4095,                                      // instrumentations switched on by configuration
  "consent": { "default": 0, "persist": true, "honorGlobalPrivacyControl": true, "pendingLimit": 500 },
  "pageViews": { "trackHashChanges": false },
  "interactions": { "events": ["click"], "captureText": false, "maxTextLength": 64 },
  "performance": { "webVitals": true, "documentLoad": true, "resources": false, "longTasks": false },
  "dependencies": { "fetch": true, "xhr": true, "propagateTraceContextTo": ["same-origin"], "ignoreUrls": ["/_framework/", "/_blazor", "/_content/"] },
  "errors": { "ignoreMessages": ["ResizeObserver loop"] },
  "logs": { "captureConsole": false, "consoleLevels": ["error", "warn"] },
  "privacy": { "redactQueryParameters": ["token","code","password","pwd","secret","key","apikey","api_key","auth","authorization","session","sid","sig","signature","email","access_token","id_token","refresh_token"],
               "stripQueryString": false, "stripFragment": true, "scrubPatterns": true, "collectUserAgent": true, "blockedAttributes": [] },
  "session": { "inactivityTimeoutMinutes": 30, "maxDurationMinutes": 1440 },
  "sampling": { "sessionRatio": 1.0 },
  "batch": { "maxQueueSize": 1024, "maxBatchSize": 100, "flushIntervalMs": 5000 },
  "persistence": { "enabled": true, "maxBytes": 1048576, "maxAgeHours": 48, "retryIntervalMs": 60000 },
  "debug": false
}
```

Notes on the fields:
- `propagateTraceContextTo` entries are `"same-origin"`, `"*"`, or a regex source matched against the absolute URL.
- `ignoreUrls` and `ignoreMessages` are regex sources.
- An exporter `endpoint` may be relative (`BrowserEndpoint = "/otlp"`); the script resolves it against the page URL,
  both to post and to recognize its own export requests.
- `signals` is the exporter's `BelemetrySignals` restricted to what the browser produces: traces 1, logs 2 (web vitals
  are log records). Absent or null means both. An exporter with neither is left out of the config.
- `blockedAttributes` are exact, case-sensitive attribute keys removed from every record and span event, before the
  ids (`session.id`, ...) are added.
- `retryIntervalMs` is `Persistence.RetryInterval` (non-positive falls back to 60000, floor 1000): how often the page
  resends its stored requests, besides on start and on the `online` event.
- `service.instanceId` is null from `<BelemetryScript />`: a browser tab is its own instance, so the script
  generates a random `service.instance.id` per page load.

### Browser record model (TS internal, and the relay payload C# `BelemetryRelayBatch`)

Times are **milliseconds since the Unix epoch** (fractional, `performance.timeOrigin + performance.now()`).
Attributes are a flat JSON object: string, number, boolean, or an array of one of those.

```jsonc
{
  "resource": { "browser.language": "en-US", "user_agent.original": "...", "telemetry.sdk.language": "webjs", ... },
  "spans": [ { "traceId": "<32 hex>", "spanId": "<16 hex>", "parentSpanId": "<16 hex>|null", "name": "GET",
               "kind": 3, "startTime": 1.0, "endTime": 2.0, "attributes": {}, "status": { "code": 2, "message": "..." },
               "events": [ { "name": "...", "time": 1.5, "attributes": {} } ], "scope": "bit.belemetry.http", "feature": 16 } ],
  "logs":  [ { "time": 1.0, "severityNumber": 17, "severityText": "ERROR", "body": "message", "eventName": "browser.page_view|null",
               "attributes": {}, "traceId": null, "spanId": null, "scope": "bit.belemetry.errors", "feature": 1 } ]
}
```

- `kind` uses the OTLP `SpanKind` numbers (1 internal, 2 server, 3 client, 4 producer, 5 consumer).
- `status.code` uses the OTLP numbers (0 unset, 1 ok, 2 error).
- `severityNumber` uses the OTLP `SeverityNumber`.

### JS module API (`_content/Bit.Belemetry/bit-belemetry.js`, ES module; also on `window.BitBelemetry`)

| Export | Purpose |
| --- | --- |
| `start(config)` | Idempotent start. Reads persisted consent, sets up the session, and installs the instrumentations the config and consent allow |
| `setConsent(features)` / `getConsent()` / `isConsentDecided()` | Runtime consent. `setConsent` persists the choice (if `consent.persist`), purges revoked data and (un)installs instrumentations. The grant is narrowed to `config.features` (and by Global Privacy Control) |
| `setUser(id or null)` | Authenticated user id. Only kept and sent with `UserTracking` |
| `trackEvent(name, attributes?)` | Custom event. A log record with `eventName = name` and feature `Events` |
| `trackPageView(name?, url?, attributes?)` | Manual page view |
| `trackException(type, message, stack?, attributes?)` | Manual exception, feature `Errors` |
| `flush()` | Returns a promise that resolves when the queued records have been handed to the transport |
| `getContext()` | `{ consent, decided, sessionId, pseudoId, userId }` |
| `attachDotNet(dotNetRef, { relay: bool, unloadDrain: bool })` | Registers the .NET bridge (`BelemetryJsCallbacks`) |
| `sendRequests(requests)` | Sends pre-serialized requests built by .NET in WASM (`[{exporter,url,contentType,contentEncoding,headers,body(base64)}]`) using keepalive fetch, retry and persistence |

.NET `[JSInvokable]` callbacks (on the internal `BelemetryJsCallbacks` relay class):

- `"Belemetry.Relay"` (async). Receives a `BelemetryRelayBatch`.
- `"Belemetry.ContextChanged"`. Receives a `BelemetryJsContext` whenever consent, session or user changes.
- `"Belemetry.DrainForUnload"` (synchronous, WASM only). Called on `pagehide` / `visibilitychange:hidden`. Returns
  the pending .NET records serialized as requests, which JS sends with `fetch(..., { keepalive: true })` because
  async .NET work does not survive unload.

### Semantic conventions emitted by the browser

| Record | Shape |
| --- | --- |
| Page view | Log record, `eventName = "browser.page_view"`. Attributes: `url.full`, `url.path`, `page.title`, `page.referrer` (redacted), `browser.navigation.type` (`initial`/`push`/`replace`/`pop`/`hash`/`manual`) |
| Click | Log record, `eventName = "browser.user_interaction"`. Attributes: `event.type`, `target.element`, `target.id`, `target.name` (`data-bit-belemetry-name`, `aria-label`, then `name`), `target.text` (only with `captureText`), `target.xpath`. Elements under `[data-bit-belemetry-ignore]` are skipped |
| Web vital | Log record, `eventName = "browser.web_vital"`. Attributes: `web_vital.name` (`LCP`/`INP`/`CLS`/`FCP`/`TTFB`), `web_vital.value`, `web_vital.delta`, `web_vital.rating` (`good`/`needs-improvement`/`poor`), `web_vital.id`, `web_vital.navigation_type` |
| Document load | Internal span `documentLoad`, with events `domInteractive`, `domContentLoadedEventEnd`, `loadEventEnd`, ... from `PerformanceNavigationTiming` |
| Resource timing | Internal span `resourceFetch` with `url.full`, `http.response.body.size`, `network.protocol.name` |
| Long task | Internal span `longtask`, feature `Performance` |
| fetch/XHR | Client span `{METHOD}`. Attributes: `http.request.method`, `url.full`, `server.address`, `server.port`, `http.response.status_code`, `error.type`. Adds `traceparent` only for targets matched by `propagateTraceContextTo` |
| JS error | Log record, severity 17 (ERROR), `eventName = "exception"`. Attributes: `exception.type`, `exception.message`, `exception.stacktrace` |
| Common (every record) | `session.id`. `enduser.pseudo.id` and `user.id` only with `UserTracking` |
| Resource | `service.name`, `service.version`, `service.namespace`, `service.instance.id`, `deployment.environment.name`, `telemetry.sdk.name = "bit-belemetry"`, `telemetry.sdk.language = "webjs"`, `telemetry.sdk.version`, `browser.language`, `browser.mobile`, `browser.platform`, `browser.brands`, `user_agent.original` (if `collectUserAgent`) |

### Azure Monitor mapping (identical in C# and TS)

- **Endpoint:** `{IngestionEndpoint}/v2.1/track`. The default is `https://dc.services.visualstudio.com`. Body is a
  JSON array of envelopes, `Content-Type: application/json`.
- **Envelope:** `{ "ver":1, "name":"Microsoft.ApplicationInsights.<Type>", "time":"<ISO 8601>", "sampleRate":100,
  "iKey":"<ikey>", "tags":{...}, "data":{ "baseType":"<Type>Data", "baseData":{ "ver":2, ... } } }`
- **Tags:**

  | Tag | Value |
  | --- | --- |
  | `ai.cloud.role` | `[service.namespace]/service.name`, or `service.name` alone without a namespace (the Azure Monitor OpenTelemetry exporter's shape, so both land on one Application Map node) |
  | `ai.cloud.roleInstance` | `service.instance.id` |
  | `ai.application.ver` | `service.version` |
  | `ai.user.agent` | `user_agent.original` (browser resource) |
  | `ai.operation.id` | traceId |
  | `ai.operation.parentId` | parent span id (span) or span id (log) |
  | `ai.operation.name` | Requests: the request name (`{http.request.method} {http.route}` when both are set, else the span name). Page views: the page name (`page.title`, else `url.path`, else `url.full`). Not set on other records |
  | `ai.session.id` | `session.id` |
  | `ai.user.id` | `enduser.pseudo.id` |
  | `ai.user.authUserId` | `user.id` |
  | `ai.device.type` | `"Browser"` (JS) / `"PC"` / `"Phone"` |
  | `ai.location.ip` | `"0.0.0.0"` when `maskIp` |
  | `ai.internal.sdkVersion` | `"bitblm:<version>"` (.NET) or `"bitblmjs:<version>"` (JS) |

- **Record types:**

  | Source record | Envelope type | Notes |
  | --- | --- | --- |
  | Span, kind SERVER/CONSUMER | `Request` | `id`, `name` (as `ai.operation.name`), `duration` (`d.hh:mm:ss.fffffff`), `responseCode`, `success`, `url` |
  | Any other span | `RemoteDependency` | `id`, `name`, `resultCode`, `data`, `duration`, `success`, `type` (`Ajax` for browser HTTP, `HTTP` for .NET, `InProc` otherwise), `target` |
  | Log with `exception.*` | `Exception` | `exceptions:[{id:1,outerId:0,typeName,message,hasFullStack,stack}]`, `severityLevel` |
  | `eventName == "browser.page_view"` | `PageView` | `name`, `url`, `id` |
  | `eventName == "browser.web_vital"` | `Metric` | `metrics:[{name:"<web_vital.name>",kind:0,value,count:1}]` |
  | Any other `eventName` | `Event` | `name` = eventName |
  | Any other log | `Message` | `message`, `severityLevel` (0 Verbose, 1 Information, 2 Warning, 3 Error, 4 Critical) |
  | Metric point | `Metric` | Gauge: last value. Sum: delta. Histogram: delta sum/count/min/max |

- **Properties:** every attribute not consumed by a field becomes a property, stringified. Keys are truncated to
  150 characters and values to 8192.
- **Retries:** status codes 408, 429, 439, 500, 502, 503 and 504 are retried (no other 5xx). A 206 response retries
  only the items the response lists with those codes.
- **Compression:** `compression: "gzip"` sends `Content-Encoding: gzip`, from the browser too (the ingestion
  endpoint's CORS preflight allows that header). Unload batches go uncompressed (compression is async).

## Tests

| Project | Covers | Run |
| --- | --- | --- |
| `Tests/Bit.Belemetry.Tests` | Model, collectors, aggregation, processors, consent state machine, privacy scrubbing, sampling, both OTLP serializers (protobuf bytes decoded by `Google.Protobuf` types generated from the vendored official `.proto` files in `Tests/Bit.Belemetry.Tests/Protos`), Azure envelopes, the sender's retry and persistence, components (bunit) | `dotnet test` from that folder |
| `Tests/Bit.Belemetry.Tests.Hosting` | Every render mode prerenders `<BelemetryScript />` with the right config, the script is served, and a real export reaches an in-process OTLP receiver when the server pipeline is enabled | `dotnet test` from that folder |
| `Tests/Bit.Belemetry.Tests.E2E` | Real browser: page views, clicks, errors, web vitals, fetch + `traceparent`, consent gating, purge on revoke, and unload flush, against the harness hosts and a capturing OTLP/Azure receiver | `dotnet test` from that folder (needs `playwright.ps1 install chromium`) |
| `Tests/Bit.Belemetry.Tests.Mcp` | The demo's MCP server | `dotnet test` from that folder |
| `Tests/bit-belemetry-js` | The TS units (protobuf writer, OTLP JSON, Azure mapping, consent, privacy, session, sampling, queue), run under vitest + happy-dom | `npm test` from that folder |
