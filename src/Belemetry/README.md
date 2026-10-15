# bit Belemetry

One lightweight telemetry SDK for every Blazor app - WebAssembly, Server, Auto, static SSR and Hybrid (MAUI, WPF,
WinForms) - covering **both sides** of the app:

- **Browser:** page views, clicks, web vitals, the document load, resource timing, long tasks, JS errors, and
  fetch/XHR dependencies with W3C trace-context propagation.
- **.NET:** `ILogger` logs, `Activity` traces and `Meter` metrics.

It speaks **OpenTelemetry** - the data model, the semantic conventions and the OTLP protocol - but does **not** depend
on the OpenTelemetry NuGet packages: it is built on the BCL's own `ActivityListener`, `MeterListener` and
`ILoggerProvider`, with hand-written OTLP and Application Insights encoders and a single dependency-free script. It
exports to:

- **OTLP/HTTP** (protobuf or JSON): the OpenTelemetry Collector, the .NET Aspire dashboard, Grafana (Tempo, Loki,
  Mimir), Jaeger, Honeycomb, Datadog, New Relic, Elastic, SigNoz, Uptrace, and anything else that speaks OTLP.
- **Azure Monitor / Application Insights**, natively (`/v2.1/track`): page views, requests, dependencies, exceptions,
  events and metrics land in their usual tables, with Live Metrics on top.
- **Console**, **in-memory**, or your own `IBelemetryExporter`.

And it is **GDPR-ready**: nothing is sent before the user consents (by default), and every part - errors, page views,
interactions, performance, dependencies, sessions, user tracking, logs, traces, metrics, offline persistence, custom
events - can be granted and revoked independently, at runtime.

The documentation website lives in [`Bit.Belemetry.Demo`](Bit.Belemetry.Demo/) - run the
[Server](Bit.Belemetry.Demo/Server/) project (`dotnet run`). It exports to itself, so its **Live telemetry** page shows
your own clicks, page views, web vitals and errors arriving as you use the site. The same server hosts an **MCP
server** at `/mcp` for AI agents: setup per host, the exact API, a configuration generator and reviewer, and the
semantic-convention and Azure-mapping references.

---

## Contents

- [Install](#install)
- [Set up per host](#set-up-per-host)
- [Exporters](#exporters)
- [Consent and GDPR](#consent-and-gdpr)
- [What is collected](#what-is-collected)
- [Manual telemetry from C#](#manual-telemetry-from-c)
- [.NET instrumentation](#net-instrumentation)
- [Browser instrumentation](#browser-instrumentation)
- [Privacy](#privacy)
- [Processors](#processors)
- [Offline persistence](#offline-persistence)
- [The server pipeline](#the-server-pipeline)
- [Transport: Direct and Relay](#transport-direct-and-relay)
- [Environment variables](#environment-variables)
- [Semantic conventions](#semantic-conventions)
- [Azure Monitor mapping](#azure-monitor-mapping)
- [Using the script without .NET](#using-the-script-without-net)
- [How it works](#how-it-works)
- [Troubleshooting](#troubleshooting)

---

## Install

```bash
dotnet add package Bit.Belemetry
```

```csharp
using Bit.Belemetry;

builder.Services.AddBitBelemetryServices(options =>
{
    options.ServiceName = "MyApp";
    options.Environment = builder.Environment.EnvironmentName;

    options.AddOtlpExporter("http://localhost:4318");                        // Aspire dashboard / OTel Collector
    options.AddAzureMonitorExporter(builder.Configuration["ApplicationInsights:ConnectionString"]!);

    options.Consent.Default = BelemetryFeatures.None;                         // opt-in (the default)
});
```

`AddBitBelemetryServices` is idempotent and follows the Options pattern: call it in every project that hosts Blazor,
and every configure callback passed across calls runs, in order, on the same `BelemetryOptions`.

---

## Set up per host

### Blazor Web App (Server, WebAssembly or Auto render modes, and static SSR)

Register in **both** projects (server and client):

```csharp
// Server/Program.cs and Client/Program.cs
builder.Services.AddBitBelemetryServices(options => { /* same configuration */ });
```

Add the script to the `<head>` of `App.razor` - it starts before Blazor boots, so it sees the initial page load, the
web vitals and early errors, and it also covers static SSR pages that never become interactive:

```razor
<head>
    ...
    <BelemetryScript />
</head>
```

And connect .NET once the app is interactive, in `Routes.razor` (or the main layout):

```razor
<Router AppAssembly="typeof(Program).Assembly">...</Router>
<BelemetryInitializer />
```

With the WebAssembly or Auto render modes, the .NET pipeline (logs, traces, metrics) runs **in the browser** and its
export is flushed with `keepalive` when the page unloads. On the server, Belemetry only serves the script unless you
turn on the [server pipeline](#the-server-pipeline).

### Standalone Blazor WebAssembly

```csharp
builder.Services.AddBitBelemetryServices(options =>
{
    options.ServiceName = "MyApp";
    options.AddOtlpExporter(o =>
    {
        o.Endpoint = new Uri("https://otel.example.com");
        o.Protocol = BelemetryOtlpProtocol.HttpProtobuf;
    });
});
```

```razor
@* App.razor *@
<Router AppAssembly="typeof(App).Assembly">...</Router>
<BelemetryInitializer />
```

Optionally add the module to `index.html` too:

```html
<script type="module" src="_content/Bit.Belemetry/bit-belemetry.js"></script>
```

The browser then downloads it in parallel with the .NET runtime and `window.BitBelemetry` exists before Blazor starts,
but the script itself only starts when `<BelemetryInitializer />` hands it the configuration. (It starts on load only
from the `<script type="application/json" id="bit-belemetry-config">` element that `<BelemetryScript />` renders in a
Blazor Web App; you can write that element into `index.html` yourself, in the shape of the browser config - it then
fixes the exporters, and the later `start` from .NET only updates consent and the transport.) Starting late costs
little: web vitals, the document-load span, resource timing and long tasks are read from the browser's buffered
performance entries. What a late start misses is a JavaScript error thrown before it.

### Blazor Hybrid (MAUI, WPF, WinForms)

```csharp
builder.Services.AddBitBelemetryServices(options =>
{
    options.ServiceName = "MyApp.Maui";
    options.AddOtlpExporter("https://otel.example.com");
});
```

```razor
@* Routes.razor / Main.razor *@
<BelemetryInitializer />
```

In Hybrid the browser script **relays** its records to .NET over JS interop (no CORS, no credentials in the page),
and the .NET pipeline exports everything, persisting it on the device while offline. Flush before the app is
suspended:

```csharp
protected override async void OnSleep() => await services.GetRequiredService<IBelemetry>().FlushAsync();
```

---

## Exporters

Any number of exporters can be combined. The browser script exports with the same configuration (unless
`IncludeInBrowser = false`).

### OTLP

```csharp
options.AddOtlpExporter(o =>
{
    o.Endpoint = new Uri("http://localhost:4318");              // /v1/traces, /v1/logs, /v1/metrics are appended
    o.Protocol = BelemetryOtlpProtocol.HttpProtobuf;            // or HttpJson
    o.Headers["x-honeycomb-team"] = "...";                       // vendor API keys
    o.Compression = BelemetryCompression.Gzip;
    o.BrowserEndpoint = new Uri("https://otel.example.com");    // public, CORS-enabled route for the browser (may be relative: "/otlp")
    o.Signals = BelemetrySignals.All;                           // honored by the browser too (it sends traces and logs)
    // o.TracesEndpoint / LogsEndpoint / MetricsEndpoint: full per-signal URLs, used verbatim
});
```

The browser posts to the collector from the page origin, so the receiver must allow it. For the OpenTelemetry
Collector:

```yaml
receivers:
  otlp:
    protocols:
      http:
        endpoint: 0.0.0.0:4318
        cors:
          allowed_origins: ["https://myapp.example.com"]
          allowed_headers: ["*"]
```

For the **.NET Aspire dashboard**, set `DASHBOARD__OTLP__CORS__ALLOWEDORIGINS` to your app's origin and use its OTLP
HTTP endpoint (protobuf). Headers set on an exporter are visible to anyone who opens the page - for vendors that need
an API key, use a collector in between, or the [Relay transport](#transport-direct-and-relay).

### Azure Monitor (Application Insights)

```csharp
options.AddAzureMonitorExporter("InstrumentationKey=...;IngestionEndpoint=https://...", o =>
{
    o.MaskIp = true;                     // default: ai.location.ip = 0.0.0.0, no IP-based geolocation
    o.Compression = BelemetryCompression.Gzip;   // also from the browser (the endpoint's CORS allows Content-Encoding)
    o.Signals = BelemetrySignals.All;    // honored by the browser too
    o.EnableLiveMetrics = true;          // Live Metrics stream (not in the browser)
    o.AccessTokenProvider = async ct =>  // Entra ID auth, for resources with local auth disabled
        (await credential.GetTokenAsync(new(["https://monitor.azure.com//.default"]), ct)).Token;
});
```

Belemetry takes no dependency on Azure.Identity - the token provider is a plain delegate. An exporter with a token
provider is never handed to the browser script.

> Live Metrics follows the Application Insights QuickPulse protocol; treat it as a preview feature.

### Console, in-memory and custom exporters

```csharp
options.AddConsoleExporter();                        // development

var memory = new BelemetryInMemoryExporter();        // tests and in-app viewers
options.AddExporter(memory);

options.AddExporter(new MyExporter());               // any IBelemetryExporter
```

```csharp
public sealed class MyExporter : IBelemetryExporter
{
    public string Name => "mine";

    public ValueTask<BelemetryExportResult> ExportAsync(BelemetryBatch batch, CancellationToken cancellationToken)
    {
        foreach (var span in batch.Spans) { /* ... */ }
        return ValueTask.FromResult(BelemetryExportResult.Success);
    }
}
```

---

## Consent and GDPR

Every record belongs to one **feature**, and is only collected when the feature is enabled by configuration
(`BelemetryOptions.Features`, all by default) **and** granted by the user. The grant never exceeds the configuration:
`IBelemetryConsent.Granted` (and `getConsent()` in the browser) is always a subset of `Available`, and granting a
feature the configuration switched off records nothing - so a feature you enable later needs the user's consent.

| Feature | Covers |
| --- | --- |
| `Errors` | JS errors and unhandled rejections; .NET logs at Error/Critical or with an exception; `TrackException` |
| `PageViews` | Page views (navigation, `TrackPageView`) |
| `Interactions` | Clicks and other interactions |
| `Performance` | Web vitals, document load, resource timing, long tasks |
| `Dependencies` | Browser fetch/XHR spans |
| `Sessions` | A session id that survives reloads (`sessionStorage`); without it the session id lives in memory only |
| `UserTracking` | The pseudonymous id `enduser.pseudo.id` (`localStorage`) and the authenticated `user.id` |
| `Logs` | .NET logs below Error, console capture |
| `Traces` | .NET `Activity` spans |
| `Metrics` | .NET `Meter` metrics |
| `Persistence` | Keeping unsent telemetry on the device for a later retry |
| `Events` | Custom events (`TrackEvent`) |

Groups: `Diagnostics` (Errors, Logs, Traces, Dependencies), `Analytics` (PageViews, Interactions, Sessions,
UserTracking, Events), `Monitoring` (Performance, Metrics), `All`.

```csharp
@inject IBelemetryConsent Consent

<button @onclick="() => Consent.SetAsync(BelemetryFeatures.All)">Accept all</button>
<button @onclick="() => Consent.SetAsync(BelemetryFeatures.Errors)">Only error reports</button>
<button @onclick="() => Consent.SetAsync(BelemetryFeatures.None)">Reject</button>

@if (Consent.IsDecided is false) { <ConsentBanner /> }
```

How it behaves:

- **Opt-in by default.** Before the user decides, only `Consent.Default` is granted (`None` unless you set it, e.g. to
  `Errors` when error reporting is covered by legitimate interest, or to `All` for apps that need no consent).
- **Nothing undecided leaves the device.** Records of not-yet-granted features are held in memory (up to
  `Consent.PendingLimit`) and sent if the user grants them, dropped if not.
- **Revoking is immediate.** Queued records of a revoked feature are discarded, and its device storage is cleared:
  `Persistence` clears the offline queue, `Sessions` the stored session, `UserTracking` the pseudo id and user id.
- **The choice is remembered** (`Consent.Persist`, default on) in `localStorage` (and in a file in Hybrid apps) -
  recording the choice itself is strictly necessary.
- **Global Privacy Control** is honored (`Consent.HonorGlobalPrivacyControl`, default on): a browser sending GPC never
  gets the `Analytics` features.
- **No cookies.** Belemetry sets none.
- Without consent for `Traces`, activities are still created for **propagation only**, so trace context keeps flowing
  to downstream services, but nothing is recorded.

On WebAssembly and Hybrid consent is app-wide. On Blazor Server it belongs to the circuit's user (it lives in their
browser), so `IBelemetryConsent` needs an interactive render mode there.

---

## What is collected

| | WebAssembly | Server | Static SSR | Hybrid |
| --- | --- | --- | --- | --- |
| Browser instrumentation | yes | yes | yes | yes (relayed) |
| .NET logs, traces, metrics | in the browser | opt-in ([server pipeline](#the-server-pipeline)) | opt-in | in the app |
| Unload flush | keepalive | keepalive | keepalive | app lifecycle |
| Offline persistence | localStorage | localStorage | localStorage | files |

---

## Manual telemetry from C#

```csharp
@inject IBelemetry Belemetry

Belemetry.TrackEvent("checkout.completed", new Dictionary<string, object?> { ["cart.items"] = 3, ["cart.total"] = 42.5 });
Belemetry.TrackPageView("Checkout - payment step");
Belemetry.TrackException(ex, new Dictionary<string, object?> { ["order.id"] = orderId });
await Belemetry.SetUserAsync(userId);       // user.id, only kept with UserTracking consent
await Belemetry.FlushAsync();
```

On WebAssembly and Hybrid these go through the .NET pipeline; on Blazor Server they are routed through the user's
browser, so their consent applies.

---

## .NET instrumentation

### Logs

Every `ILogger` entry at or above `Logs.MinimumLevel` (Information) becomes a log record with its structured values as
attributes, the active scopes (`Logs.IncludeScopes`), the exception (`exception.type`, `exception.message`,
`exception.stacktrace`) and the current trace context. Per-category levels use the standard configuration under the
`Belemetry` alias:

```json
{
  "Logging": {
    "Belemetry": {
      "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning" }
    }
  }
}
```

Belemetry's own diagnostics are logged under the category `Bit.Belemetry`, which is never exported (an export failure
would otherwise be exported by the pipeline it describes). Every other category is collected, including your own
types in `Bit.Belemetry.*` namespaces; `Logs.ExcludedCategories` drops more by prefix.

### Traces

Activities of the sources in `Traces.Sources` (default `System.Net.Http` and `Microsoft.AspNetCore.Components*`; a
trailing `*` matches a prefix) are recorded. Sampling is parent-based with `Traces.SamplingRatio` for new traces,
compatible with the OpenTelemetry `TraceIdRatioBased` sampler.

```csharp
options.Traces.Sources.Add("MyApp*");
options.Traces.SamplingRatio = 0.25;

static readonly ActivitySource Source = new("MyApp.Checkout");
using var activity = Source.StartActivity("PlaceOrder");
```

### Metrics

Instruments of the meters in `Metrics.Meters` (default `System.Net.Http`, `Microsoft.AspNetCore.Components*`,
`System.Runtime`) are aggregated - counters and up-down counters as sums, histograms with explicit buckets (instrument
advice is honored on .NET 9+), gauges as last values - and exported every `Metrics.ExportInterval` (60 s), with
`Metrics.Temporality` (cumulative by default; Azure Monitor always gets deltas) and a `Metrics.CardinalityLimit` per
instrument (2000; overflow goes to an `otel.metric.overflow` point).

```csharp
options.Metrics.Meters.Add("MyApp");
options.Metrics.HistogramBoundaries = [0, 10, 50, 100, 500, 1000];
```

---

## Browser instrumentation

| Option (`options.Browser.*`) | Default | |
| --- | --- | --- |
| `Enabled` | `true` | the script at all |
| `TrackHashChanges` | `false` | fragment-only changes count as page views |
| `InteractionEvents` | `click` | DOM events recorded as interactions |
| `CaptureInteractionText` | `false` | record the clicked element's text (may contain personal data) |
| `WebVitals` / `DocumentLoad` | `true` | LCP, INP, CLS, FCP, TTFB / the load span with navigation timing |
| `ResourceTiming` / `LongTasks` | `false` | spans per fetched resource / main-thread tasks over 50 ms |
| `Fetch` / `Xhr` | `true` | dependency spans |
| `PropagateTraceContextTo` | `same-origin` | where `traceparent` is added (`same-origin`, `*`, or regexes) |
| `IgnoreUrls` | Blazor framework routes | regexes never recorded as dependencies |
| `IgnoreErrorMessages` | `ResizeObserver loop` | regexes of errors never recorded |
| `CaptureConsole` / `ConsoleLevels` | `false` / `error`, `warn` | console capture |
| `SessionInactivityTimeout` / `SessionMaxDuration` | 30 min / 24 h | session rotation |
| `SessionSampleRatio` | `1.0` | fraction of sessions recorded |
| `FlushInterval`, `MaxBatchSize`, `MaxQueueSize` | 5 s, 100, 1024 | batching |
| `Debug` | `false` | the script's own diagnostics in `console.debug` |

Name interactions with `data-bit-belemetry-name`, and keep a region out entirely with `data-bit-belemetry-ignore`:

```html
<button data-bit-belemetry-name="checkout.pay">Pay</button>
<section data-bit-belemetry-ignore>...</section>
```

Input values are never read.

---

## Privacy

Applied on both sides before anything is queued (`options.Privacy.*`):

- `RedactQueryParameters`: values of sensitive query parameters (`token`, `code`, `password`, `secret`, `key`,
  `access_token`, `email`, ...) become `REDACTED` in every URL.
- `StripQueryString` (off) and `StripFragment` (on - fragments may carry OAuth tokens).
- `ScrubPatterns` (on): e-mail addresses, JWTs, bearer tokens, card numbers (Luhn-checked) and IPv4 addresses are
  replaced in messages, stack traces and URLs.
- `CollectUserAgent` (on): the user agent string in the browser resource.
- `BlockedAttributes`: attribute keys (exact, case-sensitive) removed from every record and span event, by the
  script and by .NET. The ids added afterwards (`session.id`, `enduser.pseudo.id`, `user.id`) are governed by consent.
- Azure Monitor's `MaskIp` (on) stops IP-based geolocation; Belemetry never adds the client IP itself.

IP scrubbing leaves the **host** of a URL and `server.address` alone, even when they are IP literals: they name the
server that was called (`http://10.0.0.5:5000/api` stays readable in a dependency), not the user. The rest of the URL
is still scrubbed, credentials in a URL are always dropped, and IPs in any other text (messages, stack traces,
`client.address` in the browser) are replaced with `[ip]`.

---

## Processors

```csharp
public sealed class TenantProcessor(ITenantAccessor tenants) : IBelemetryProcessor
{
    public bool OnSpan(BelemetrySpan span)
    {
        span.Attributes.Set("tenant.id", tenants.Current);
        return span.Name != "GET /health";          // false drops it
    }

    public bool OnLog(BelemetryLogRecord log) => log.Body?.Contains("noise") is not true;
}

options.AddProcessor(new TenantProcessor(...));
```

Processors run on every .NET-side record, including those relayed from the browser.

---

## Offline persistence

With `Persistence` consent and `options.Persistence.Enabled` (default), requests that fail with a network error or a
retryable status (408, 429, 439, 500, 502, 503 or 504; other statuses are dropped) are kept - in `localStorage` in the
browser (`BrowserMaxBytes`, 1 MB), in files in Hybrid and on servers (`Directory`, `MaxBytes` 20 MB) - and retried
every `RetryInterval` (1 min, on both sides), when the page starts and when the browser comes back online, for up to
`MaxAge` (48 h).

Stored requests never contain headers, so API keys are not written to disk or `localStorage`. The exporter's current
headers are added when a stored request is sent, so a rotated key applies to the backlog too. (In WebAssembly, a .NET
request stored by the browser is resent with the headers of the browser exporter posting to the same URL.)

---

## The server pipeline

On an ASP.NET Core host, Belemetry's .NET pipeline is **off by default** - such hosts usually have their own
OpenTelemetry setup - and Belemetry only serves the browser script. Turn it on to collect the server's logs, traces
(including `Microsoft.AspNetCore` request spans) and metrics with the same exporters:

```csharp
options.Server.CollectDotnetTelemetry = true;
options.Server.Features = BelemetryFeatures.Errors | BelemetryFeatures.Logs | BelemetryFeatures.Traces | BelemetryFeatures.Metrics;
```

The server pipeline serves every user at once, so it is governed by `Server.Features` (operator telemetry), not by
any one user's consent. User consent keeps governing the browser script and the `IBelemetry` calls of each circuit.

ASP.NET Core's request activity (`Microsoft.AspNetCore.Hosting.HttpRequestIn`) carries no HTTP tags of its own on
.NET 8 and 9. Belemetry fills them in without a middleware or a reference to ASP.NET Core, from the tags ASP.NET Core
puts on its `http.server.request.duration` measurement: `http.request.method`, `url.scheme`, `http.route`,
`http.response.status_code`, `network.protocol.version` and `error.type`. The span is named `{method} {route}` (or
`{method}` without a route, unless the app renamed the activity), and a 5xx marks it as an error. On .NET 10 Belemetry
also turns off ASP.NET Core's `Microsoft.AspNetCore.Hosting.SuppressActivityOpenTelemetryData` switch (unless the app
set it either way), which adds `url.path`, `server.address` and `server.port`.

---

## Transport: Direct and Relay

`options.Browser.Transport`:

- **Direct** (default except Hybrid): the script exports to the configured endpoints itself with `fetch`. The receiver
  must allow the page origin (CORS), and exporter headers are visible in the page.
- **Relay** (default in Hybrid): the script hands its records to .NET over JS interop, and the .NET pipeline exports
  them. No CORS, no credentials in the page. Needs `<BelemetryInitializer />`; on Blazor Server, records travel over
  the circuit (interactive pages only).

---

## Environment variables

Outside the browser, with `options.UseEnvironmentVariables` (default on), the standard variables fill in what the code
leaves unset - this is how the Aspire AppHost and most container platforms wire telemetry in. A value set in code
always wins, as in the OpenTelemetry SDKs (headers merge per key):

| Variable | Effect |
| --- | --- |
| `OTEL_SERVICE_NAME` | `service.name` |
| `OTEL_RESOURCE_ATTRIBUTES` | extra resource attributes (`k=v,k2=v2`) |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | adds an OTLP exporter when none is configured, and fills unset endpoints |
| `OTEL_EXPORTER_OTLP_HEADERS` | OTLP headers (`k=v,k2=v2`) |
| `OTEL_EXPORTER_OTLP_PROTOCOL` | `http/protobuf` or `http/json`, for exporters whose `Protocol` is not set in code |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | adds an Azure Monitor exporter when none is configured |

---

## Semantic conventions

Resource: `service.name`, `service.version`, `service.namespace`, `service.instance.id`,
`deployment.environment.name`, `telemetry.sdk.name = bit-belemetry`, `telemetry.sdk.language` (`dotnet` / `webjs`),
`telemetry.sdk.version`, plus `os.*`, `host.arch`, `process.runtime.*` (.NET) or `browser.*`, `user_agent.original`
(browser).

| Record | Shape |
| --- | --- |
| Page view | log record, event `browser.page_view`: `url.full`, `url.path`, `page.title`, `page.referrer`, `browser.navigation.type` |
| Interaction | log record, event `browser.user_interaction`: `event.type`, `target.element`, `target.id`, `target.name`, `target.xpath` |
| Web vital | log record, event `browser.web_vital`: `web_vital.name`, `web_vital.value`, `web_vital.delta`, `web_vital.rating`, `web_vital.id` |
| Document load | internal span `documentLoad` with navigation-timing events |
| fetch/XHR | client span: `http.request.method`, `url.full`, `server.address`, `server.port`, `http.response.status_code`, `error.type` |
| JS error / exception | log record, severity ERROR, event `exception`: `exception.type`, `exception.message`, `exception.stacktrace` |
| `ILogger` | log record with the category as scope, `log.event.id`, `log.event.name`, the structured values |
| Every record | `session.id`; `enduser.pseudo.id` and `user.id` with `UserTracking` |

---

## Azure Monitor mapping

| Record | Application Insights |
| --- | --- |
| Server/consumer span | `requests` |
| Other spans | `dependencies` (`Ajax` from the browser, `HTTP`, the database system, `InProc`) |
| Log with an exception | `exceptions` |
| `browser.page_view` | `pageViews` |
| `browser.web_vital` | `customMetrics` (`LCP`, `INP`, `CLS`, `FCP`, `TTFB`) |
| Other events | `customEvents` |
| Other logs | `traces` |
| Metrics | `customMetrics` (deltas; histograms with count, min, max) |

Tags: `ai.cloud.role` (`[service.namespace]/service.name`, or `service.name` alone - as the Azure Monitor
OpenTelemetry exporter writes it), `ai.cloud.roleInstance`, `ai.application.ver` (`service.version`), `ai.user.agent`
(browser), `ai.device.type` (`Browser`, `PC` or `Phone`), `ai.operation.id` (trace id), `ai.operation.parentId`,
`ai.operation.name` (requests: `{method} {route}` or the span name; page views: the page name), `ai.session.id`,
`ai.user.id` (pseudo id), `ai.user.authUserId` (user id), `ai.location.ip` (`0.0.0.0` with `MaskIp`),
`ai.internal.sdkVersion`.

Retried statuses: 408, 429, 439, 500, 502, 503 and 504, and, in a 206 partial success, only the items rejected with
one of them.

---

## Using the script without .NET

`bit-belemetry.js` is an ES module and also sets `window.BitBelemetry`:

```js
BitBelemetry.setConsent(Belemetry.Features?.All ?? 4095);
BitBelemetry.trackEvent('video.played', { 'video.id': 'intro' });
BitBelemetry.setUser('42');
await BitBelemetry.flush();
console.log(BitBelemetry.getContext());   // { consent, decided, sessionId, pseudoId, userId }
```

---

## How it works

- **Collectors:** an `ActivityListener`, a `MeterListener` and an `ILoggerProvider` - the BCL primitives the
  OpenTelemetry SDK itself is built on - feed one pipeline that applies consent, privacy rules and processors, batches,
  and exports on async timers (never threads: browser WebAssembly has one).
- **Encoders:** OTLP protobuf (a minimal hand-written writer, conformance-tested against the official `.proto`
  files), OTLP JSON, and Application Insights envelopes - all without reflection, so the package is trimmable and
  AOT-compatible.
- **Delivery:** retries with backoff that honor `Retry-After`, Azure partial-success (206) handling, offline
  persistence, and an unload drain in which WebAssembly hands its serialized batches to the browser to send with
  `keepalive`.
- **No self-instrumentation:** the exporter's own HTTP calls are suppressed from tracing, and the script ignores
  exporter URLs and requests already traced by .NET.

---

## Troubleshooting

- **Nothing arrives:** check consent first (`IBelemetryConsent.Granted`, or `BitBelemetry.getContext()` in the
  browser console) - by default nothing is sent until the user consents. Then set `options.Browser.Debug = true` and
  `options.Debug = true`, and add `options.AddConsoleExporter()`.
- **CORS errors in the browser console:** the receiver does not allow the page origin - configure CORS on the
  collector, use `BrowserEndpoint`, or switch to the Relay transport.
- **No .NET telemetry on the server:** that is the default; see [the server pipeline](#the-server-pipeline).
- **Duplicate HTTP spans in WebAssembly:** they should not happen - the script skips requests that already carry a
  `traceparent` from .NET's `HttpClient`. If a custom handler strips it, add the URL to `Browser.IgnoreUrls`.
