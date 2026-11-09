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

- **OTLP** (HTTP with protobuf or JSON, or gRPC): the OpenTelemetry Collector, the .NET Aspire dashboard, Grafana
  (Tempo, Loki, Mimir), Jaeger, Honeycomb, Datadog, New Relic, Elastic, SigNoz, Uptrace, and anything else that
  speaks OTLP.
- **Azure Monitor / Application Insights**, natively (`/v2.1/track`): page views, requests, dependencies, exceptions,
  events and metrics land in their usual tables, with Live Metrics on top.
- **Console**, **in-memory**, or your own `IBelemetryExporter`.

When a user reports a problem, **production diagnostics** turn what the app did - routes, components, event
handlers, failed requests, browser errors, circuit transitions, grouped exceptions, release and trace ids - into a
support bundle the user previews and confirms before anything leaves the app.

And it is **GDPR-ready**: nothing is sent before the user consents (by default), and every part - errors, page views,
interactions, performance, dependencies, sessions, user tracking, logs, traces, metrics, offline persistence, custom
events - can be granted and revoked independently, at runtime.

The documentation website lives in [`Demo`](Demo/) - run the
[Server](Demo/Server/) project (`dotnet run`). It exports to itself, so its **Live telemetry** page shows
your own clicks, page views, web vitals and errors arriving as you use the site. The same server hosts an **MCP
server** at `/mcp` for AI agents: setup per host, the exact API, a configuration generator and reviewer, and the
semantic-convention and Azure-mapping references.

---

## Contents

- [Install](#install)
- [Configuration from appsettings](#configuration-from-appsettings)
- [Set up per host](#set-up-per-host)
- [Exporters](#exporters)
- [Consent and GDPR](#consent-and-gdpr)
- [What is collected](#what-is-collected)
- [Manual telemetry from C#](#manual-telemetry-from-c)
- [.NET instrumentation](#net-instrumentation)
- [Browser instrumentation](#browser-instrumentation)
- [Privacy](#privacy)
- [Processors](#processors)
- [Production diagnostics and support bundles](#production-diagnostics-and-support-bundles)
- [Offline persistence](#offline-persistence)
- [The server pipeline](#the-server-pipeline)
- [Transport: Direct and Relay](#transport-direct-and-relay)
- [Environment variables](#environment-variables)
- [Semantic conventions](#semantic-conventions)
- [Azure Monitor mapping](#azure-monitor-mapping)
- [Using the script without .NET](#using-the-script-without-net)
- [How it works](#how-it-works)
- [Troubleshooting](#troubleshooting)
- [Upgrade notes](#upgrade-notes)

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

Add `@using Bit.Belemetry` to `_Imports.razor` in every project whose components use it. Without it,
`<BelemetryScript />` compiles as an unknown HTML element (only a warning), and telemetry silently never starts.

What the core cannot see on its own ships as separate packages, so an app only takes the dependencies it uses:
`Bit.Belemetry.EntityFrameworkCore` (a span per database command) and `Bit.Belemetry.Hangfire` (a span per job
execution, parented to the enqueue). See [Entity Framework Core and Hangfire](#entity-framework-core-and-hangfire).

---

## Configuration from appsettings

Every option can come from configuration instead of code. Bind the section the way a trimmed or AOT app binds any
options - the configuration binder's source generator (`<EnableConfigurationBindingGenerator>true</EnableConfigurationBindingGenerator>`
in the app's project) handles `BelemetryOptions` without reflection, and Belemetry itself takes no dependency on the
binder:

```csharp
builder.Services.AddBitBelemetryServices(options =>
{
    builder.Configuration.GetSection("Belemetry").Bind(options);
    options.AddExporter(new MyExporter());   // what only code can add: custom exporters, processors, token providers
});
```

```json
{
  "Belemetry": {
    "ServiceName": "MyApp",
    "Environment": "Production",
    "Consent": { "Default": "Errors" },
    "Browser": { "TrackPageVisitTime": true },
    "Traces": { "Sources": [ "MyApp.*" ], "IgnoredActivityNames": [ "Microsoft.AspNetCore.Components.HandleEvent" ] },
    "Server": { "CollectDotnetTelemetry": true, "IgnoredRequestPaths": [ "/health", "/alive" ] },
    "OtlpExporters": [ { "Endpoint": "https://otel.example.com", "Protocol": "HttpProtobuf", "Headers": { "x-api-key": "..." } } ],
    "AzureMonitorExporters": [ { "ConnectionString": "InstrumentationKey=...", "MinimumLogLevel": "Warning" } ]
  }
}
```

Two binder rules worth knowing: a bound list **adds** to the option's defaults (`Privacy.RedactQueryParameters` keeps
its built-in names and gains yours), and a bound array replaces nothing either, which is why `Metrics.HistogramBoundaries`
is empty by default and means "the OpenTelemetry defaults" until you set it. Enums and flags bind by name
(`"Features": "Errors, Logs, Traces"`), and `TimeSpan`s by `hh:mm:ss`.

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

What that costs the download, measured on a trimmed Release publish of `Samples/Wasm` (brotli, as served):

| Part | Size |
| --- | --- |
| `Bit.Belemetry.wasm` (the .NET pipeline, both encoders, the bridge) | 107 KB |
| `bit-belemetry.js` (the browser script) | 16 KB |
| `System.Diagnostics.DiagnosticSource.wasm`, when the app did not already ship it | 29 KB |

`System.Text.Json` and `System.Net.Http` are already part of every Blazor WebAssembly app. The package is
`IsTrimmable` and `IsAotCompatible`; the E2E suite's publish gate fails on any trim or AOT warning raised inside it.

In the browser, metrics need `System.Diagnostics.Metrics`, which the WebAssembly SDK turns off by default
(`MetricsSupport=false`) to save size. The Bit.Belemetry package turns it back on for you: its build props set
`MetricsSupport` to `true` unless the project sets it. A project that sets it to `false` gets no metrics, and Belemetry
logs a warning at startup.

`HttpClient` spans are a switch of their own, which the WebAssembly SDK also turns off (`HttpActivityPropagationSupport`)
and the package leaves alone. Off, `HttpClient`'s requests go out through the browser's `fetch`, and the browser script
records them as its own client spans (`Dependencies`, with `traceparent` only where `Browser.PropagateTraceContextTo`
allows). Set `<HttpActivityPropagationSupport>true</HttpActivityPropagationSupport>` to have .NET trace them as
`System.Net.Http` spans inside the current activity's trace instead - but then `HttpClient` sends `traceparent` to every
origin it calls, and a cross-origin API that does not allow the header in CORS fails.

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
and the .NET pipeline exports everything, persisting it on the device while offline. MAUI, WPF and Windows Forms
share the same `BlazorWebView` core, so the setup is the same in all three; the repository's `Samples/Maui` and
`Samples/WinForms` show it. Flush before the app is suspended (or, in a desktop app, before its window closes):

```csharp
// App.xaml.cs, with the app's IServiceProvider injected into its constructor as _services
protected override async void OnSleep()
{
    base.OnSleep();

    try
    {
        // IBelemetry is scoped; the queue it flushes is the app-wide pipeline.
        await using var scope = _services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IBelemetry>().FlushAsync();
    }
    catch (Exception ex)
    {
        // async void: an exception here would crash the app on its way to the background.
        System.Diagnostics.Debug.WriteLine($"Flushing telemetry failed: {ex}");
    }
}
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
    o.Protocol = BelemetryOtlpProtocol.HttpProtobuf;            // or HttpJson, or Grpc (see below)
    o.Headers["x-honeycomb-team"] = "...";                       // vendor API keys (a server keeps these out of the page)
    o.BrowserHeaders["x-ingest-key"] = "...";                    // what the browser script may send - public by definition
    o.Compression = BelemetryCompression.Gzip;
    o.BrowserEndpoint = new Uri("https://otel.example.com");    // public, CORS-enabled route for the browser (may be relative: "/otlp")
    o.Signals = BelemetrySignals.All;                           // honored by the browser too (it sends traces and logs)
    o.MinimumLogLevel = LogLevel.Warning;                       // this exporter gets warnings and up; others get everything
    // o.TracesEndpoint / LogsEndpoint / MetricsEndpoint: full per-signal URLs, used verbatim
});
```

**gRPC.** `BelemetryOtlpProtocol.Grpc` speaks OTLP/gRPC over HTTP/2 without a gRPC library: a unary call is an HTTP/2
POST with the 5-byte message prefix, and the status comes back in the trailers. It is what the OpenTelemetry SDKs
default to and what the .NET Aspire AppHost's `WithOtlpExporter()` hands a resource (`OTEL_EXPORTER_OTLP_PROTOCOL=grpc`
and the dashboard's gRPC endpoint; the default port is 4317). `Compression` becomes the gRPC message compression
(`grpc-encoding: gzip`). A browser cannot speak gRPC, so a gRPC exporter reaches the page only through an OTLP/HTTP
`BrowserEndpoint`; in a WebAssembly app the .NET pipeline cannot either, so there the whole exporter becomes OTLP/HTTP
to that endpoint (which must be absolute), and one without is skipped with a startup warning. On Android and iOS the
gRPC calls go through a `SocketsHttpHandler` of their own, since the platforms' native handlers cannot do HTTP/2
without TLS.

**One level per backend.** `MinimumLogLevel` is the per-exporter counterpart of a logging provider's level: with it,
Application Insights can receive warnings and errors only while an OTLP backend receives everything, from one
pipeline. It applies to the plain log records of the .NET pipeline (its own and the browser's relayed ones); events -
page views, interactions, web vitals, custom events, exceptions - are not logs to a log level and always pass, as do
spans and metrics.

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
HTTP endpoint (protobuf).

**Headers and the page.** Whatever the browser script exports with is readable by anyone who opens the page. A server
(Blazor Server, static SSR, the server half of Auto) therefore never hands an exporter's `Headers` to the script -
they are server configuration, often a real API key, or the Aspire dashboard's key from `OTEL_EXPORTER_OTLP_HEADERS` -
and the page sends only `BrowserHeaders`, for a key that is meant to be public. Belemetry warns at startup about an
exporter whose page now exports without the headers it has. In WebAssembly and Hybrid the configuration already lives
on the device, so the script sends `Headers` too (with `BrowserHeaders` on top): never put a credential in a client's
exporter. For vendors that need a secret key, use a collector in between, or the
[Relay transport](#transport-direct-and-relay).

**User info in an endpoint.** The page never gets an endpoint's user info, on any host: `https://user:token@collector.example.com`,
set in code or through `OTEL_EXPORTER_OTLP_ENDPOINT`, reaches the browser script as `https://collector.example.com/`,
and a server warns at startup about an exporter whose page now exports without it. Belemetry's own warnings and
diagnostics name every endpoint without it too.
User info authenticates no export anyway - neither .NET's `HttpClient` nor the browser sends it - so put the
credential in `Headers` (an `Authorization` header) instead.

### Azure Monitor (Application Insights)

```csharp
options.AddAzureMonitorExporter("InstrumentationKey=...;IngestionEndpoint=https://...", o =>
{
    o.MaskIp = true;                     // default: ai.location.ip = 0.0.0.0, no IP-based geolocation
    o.Compression = BelemetryCompression.Gzip;   // also from the browser (the endpoint's CORS allows Content-Encoding)
    o.Signals = BelemetrySignals.All;    // honored by the browser too
    o.EnableLiveMetrics = true;          // Live Metrics stream (not in the browser)
    o.MinimumLogLevel = LogLevel.Warning;    // Application Insights gets warnings and up (spans and metrics unaffected)
    o.AccessTokenProvider = async ct =>  // Entra ID auth, for resources with local auth disabled
        (await credential.GetTokenAsync(new(["https://monitor.azure.com//.default"]), ct)).Token;
});
```

Belemetry takes no dependency on Azure.Identity - the token provider is a plain delegate. An exporter with a token
provider is never handed to the browser script.

> Live Metrics follows the Application Insights QuickPulse protocol (API version `2024-04-01-preview`); treat it as a
> preview feature. In a Hybrid app it streams only while the user grants `Metrics`, and it follows the service's
> endpoint redirects over HTTPS only.
>
> Besides the request, dependency and exception rates and the process CPU and memory, it applies what the portal
> configures:
>
> - **Sample telemetry.** Each stream's filters pick the requests, dependencies, exceptions, custom events and traces
>   (log messages, and the named events of spans) it shows. A stream takes up to 3 samples of a type at once and earns one more every 2 seconds, saving up to 30,
>   unless the service sets other quotas.
> - **Custom charts.** A chart's filters, projection (`Count()`, `Duration`, `CustomDimensions.{key}`) and
>   aggregation are computed for every data point.
>
> Filters compare text case-sensitively, except `Contains` and `DoesNotContain`. A sample carries the properties of
> the Application Insights item it stands for - the record's attributes after privacy and processors, without the user
> and session ids or the attributes the item's own fields hold, plus what the item adds (a page visit's `PageName`, an
> event's `message`, ...) - and `CustomDimensions.{key}` filters and charts read the same. Samples and custom charts
> only take what the exporter itself sends, as its `Signals` and `MinimumLogLevel` allow; the built-in request,
> dependency and exception rates count every span and exception the pipeline accepts, whatever these settings. A sample
> waiting for the next data point is not sent once the user revokes its feature, even if they grant it again first.
> When the portal changes the configuration, the streams and charts it leaves as they were keep their quotas and the
> values they gathered. What Belemetry cannot apply (performance counters, custom metrics, an unknown field, filters
> that are not a list) is left out and reported back to the portal as a configuration error; a configuration it cannot
> read at all is reported too, and nothing is sampled until the portal sends another. The stream goes through a
> registered `BelemetryHttpMessageHandlerProvider`, like the rest of the export traffic.

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
| `Errors` | JS errors and unhandled rejections; .NET logs at Error/Critical or with an exception; `TrackException`; unhandled .NET exceptions and unobserved task exceptions; exceptions recorded on spans (`Activity.AddException`) - without `Errors` they are taken off the span, which is still sent |
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
  revoking any feature clears the offline queue (a stored request can hold that feature's records), `Sessions` the
  stored session, `UserTracking` the pseudo id and user id, which also leave the records still queued. What already
  left the queue and holds a revoked feature's records (or the user ids) is dropped too: not sent if it has not been
  yet, not retried, not stored - and the other records of its batch go with it. A revoke of `Persistence` or
  `Sessions`, which no record belongs to, drops nothing in flight.
- **The choice is remembered** (`Consent.Persist`, default on) in `localStorage` (and in a file in Hybrid apps) -
  recording the choice itself is strictly necessary. Other open tabs of the app adopt it as it is made, so a banner
  answered in one tab closes in the rest, and a revoke there stops them sending too.
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

The user ids are consent's to add: without `UserTracking`, a `user.id` or `enduser.pseudo.id` passed as an attribute,
a log scope, a structured log value or a metric tag is dropped (from span events and links too), and a log message that
would render one is exported with its template as the body (`Paid by {user.id}`) - also when `UserTracking` is revoked
after the message was logged and before it was exported.

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

### Unhandled exceptions

An exception nothing caught reaches no `ILogger`, so the .NET pipeline listens for them itself
(`Errors.CaptureUnhandledExceptions`, on by default):

- `AppDomain.UnhandledException` - the crash of a MAUI, WPF or WinForms app, or a background thread of a server that
  threw. The record is a FATAL `exception` event (`Critical` in Application Insights) with the body `An unhandled
  exception ended the process.`.
- `TaskScheduler.UnobservedTaskException` - a faulted task that was never awaited, raised when the garbage collector
  finds it. The record is an ERROR `exception` event with the body `A faulted task's exception was never observed.`;
  the task's own exception is reported rather than its `AggregateException` when it is the only one. The event is not
  marked observed: that stays the app's choice.

Both carry `exception.type`, `exception.message`, `exception.stacktrace` and the trace context of the thread that
threw, under the scope `Bit.Belemetry.UnhandledExceptions`. They are `Errors` records: consent and the
[error limits](#error-floods-and-runaway-values) apply as to any other, and production diagnostics count them as
unhandled exceptions (the `UnhandledExceptions` trigger).

The process ends right after `AppDomain.UnhandledException`, before anything async completes. So Belemetry writes the
crash record, with everything still queued before it, to the [offline store](#offline-persistence) synchronously,
and the next launch sends it with the rest of the store (its first resend pass is 10 s after start). That needs
`Persistence`: granted by the user on a client, in `Server.Features` on a server. Without it nothing is written to
disk and the record is lost with the process. Records held for a consent decision are never written either. In
WebAssembly the page outlives the .NET runtime, so the queue is handed to the script instead, which sends it as it
sends the unload flush. Only the HTTP exporters (OTLP, Azure Monitor) are saved; custom exporters and the console
exporter get the records only if the process goes on.

On Windows desktop, an exception on the UI thread goes to `Application.ThreadException` (WinForms) or
`Dispatcher.UnhandledException` (WPF) first, and reaches `AppDomain.UnhandledException` only when they do not handle
it. Log what such a handler handles to record it.

### Traces

Activities of the sources in `Traces.Sources` (default `System.Net.Http`, `Microsoft.AspNetCore.Components*` and
`Bit.Belemetry.*` - the instrumentation packages; a trailing `*` matches a prefix) are recorded. Sampling is
parent-based with `Traces.SamplingRatio` for new traces, compatible with the OpenTelemetry `TraceIdRatioBased`
sampler. Names in `Traces.IgnoredActivityNames` are never created at all - their children attach to the parent - which
is the cheapest way to drop a chatty source, such as Blazor's per-event spans.

Blazor's component spans (`Navigate`, `HandleEvent`) exist on a server; in WebAssembly and Hybrid only with
`Diagnostics.Enabled` (Belemetry registers the framework's component source for diagnostics). With diagnostics on, they
are exported there too when `Traces.Sources` matches `Microsoft.AspNetCore.Components` (the default), and the HttpClient
spans a click or navigation starts are their children.

```csharp
options.Traces.Sources.Add("MyApp*");
options.Traces.SamplingRatio = 0.25;
options.Traces.IgnoredActivityNames.Add("Microsoft.AspNetCore.Components.HandleEvent");

static readonly ActivitySource Source = new("MyApp.Checkout");
using var activity = Source.StartActivity("PlaceOrder");
```

Sampling at the end of a span (keep every failure and every slow span, a fraction of the rest) is a
[processor](#processors). `BelemetrySampler` makes the same deterministic, trace-wide decision the ratio sampler
makes, so the kept fraction is whole traces, not scattered spans:

```csharp
public sealed class TailSampler : IBelemetryProcessor
{
    public bool OnSpan(BelemetrySpan span) =>
        span.Status == BelemetryStatusCode.Error
        || span.Duration > TimeSpan.FromSeconds(1)
        || BelemetrySampler.ShouldSample(span.TraceId, 0.05);
}
```

### Metrics

Instruments of the meters in `Metrics.Meters` (default `System.Net.Http`, `Microsoft.AspNetCore.Components*`,
`System.Runtime`) are aggregated - counters and up-down counters as sums, histograms with explicit buckets (instrument
advice is honored), gauges as last values - and exported every `Metrics.ExportInterval` (60 s), with
`Metrics.Temporality` (cumulative by default; Azure Monitor always gets deltas) and a `Metrics.CardinalityLimit` per
instrument (2000; overflow goes to an `otel.metric.overflow` point).

```csharp
options.Metrics.Meters.Add("MyApp");
options.Metrics.HistogramBoundaries = [0, 10, 50, 100, 500, 1000];   // empty (the default) means BelemetryMetricsOptions.DefaultHistogramBoundaries
```

**Belemetry's own health.** Add the `Bit.Belemetry` meter to watch for data loss on a dashboard instead of finding
out from a gap:

```csharp
options.Metrics.Meters.Add("Bit.Belemetry");
```

| Instrument | Attributes | Counts |
| --- | --- | --- |
| `belemetry.records.dropped` | `belemetry.reason`, `belemetry.signal` (`traces`/`logs`) | records dropped before export: `queue_full`, `pending_full` (the hold before a consent decision), `not_consented`, `consent_revoked`, `error_limit`, `relay_limit`, `filtered` (a processor returned false) |
| `belemetry.records.exported` | `belemetry.exporter`, `belemetry.outcome` | records handed to an exporter - for Azure Monitor the items it sends, so a span event sent as an item of its own counts and a metric point with nothing new does not: `delivered`, `rejected` (refused for good - the items a partial success refused included), `stored` (kept for a retry; `delivered` again when the retry succeeds), `failed` (lost), `withdrawn` (consent for a feature of its records was revoked after it left the queue: neither sent again nor stored) |

In the browser, `BitBelemetry.getStats()` gives the script's counterpart: `{ dropped: { <reason>: n }, requests:
{ delivered, rejected, kept, dropped, withdrawn }, queued, pending }` since the page loaded (with `sampled_out` among the
reasons).

Metrics depend on the `System.Diagnostics.Metrics` feature switch (`MetricsSupport`). The WebAssembly SDK turns it off
by default; the Bit.Belemetry package turns it back on unless the project sets it. With the switch off, no metrics are
exported while logs and traces still flow, and Belemetry logs a warning at startup. To drop metrics on purpose, also
remove `BelemetryFeatures.Metrics` from `options.Features`, which silences the warning. A project that references
`Bit.Belemetry.csproj` instead of the package skips the package's build props: import
`buildTransitive/Bit.Belemetry.props` from it, as this repository's samples do.

### Entity Framework Core and Hangfire

Neither EF Core nor Hangfire starts activities of its own, and the OpenTelemetry instrumentations for them only run
inside the OpenTelemetry SDK. Belemetry ships the two bridges as packages of their own, so an app without a database
or a job server takes neither dependency; their spans land on `Bit.Belemetry.*` sources, which the core collects by
default, and any other `ActivityListener` can record them too.

```bash
dotnet add package Bit.Belemetry.EntityFrameworkCore
dotnet add package Bit.Belemetry.Hangfire
```

```csharp
services.AddDbContext<AppDbContext>(options => options
    .UseSqlServer(connectionString)
    .UseBitBelemetry(o =>
    {
        o.CaptureQueryText = false;                                    // db.query.text, off by default
        o.Filter = (command, data) => data.Context is not JobDbContext;   // skip a job store's own commands
    }));

services.AddHangfire(configuration => configuration
    .UseSqlServerStorage(connectionString)
    .UseBitBelemetry(o =>
    {
        o.PropagateTraceContext = true;                        // the enqueue's trace is the execution's parent
        o.Filter = job => job.Type != typeof(HeartbeatJob);    // skip the noisy ones
    }));
```

- **EF Core:** a client span per command, named after the database, with `db.system.name`, `db.namespace`,
  `server.address`, `server.port`, `efcore.command.source` (`SaveChanges`, `LinqQuery`, `Migrations`, ...) and, on a
  failure, `error.type` and an error status. Application Insights shows them as database dependencies. The span covers
  the execution, not the reading of the result set, as the OpenTelemetry instrumentation's does.
- **Hangfire:** a consumer span `JOB {Type}.{Method}` per execution with `job.id`, `job.type`, `job.method`,
  `job.queue` and `job.created_at`, parented to the trace that enqueued the job (the W3C trace context travels in the
  job's parameters, so the enqueue and the execution may run in different processes), and marked as an error with
  `error.type` when the job throws. Application Insights shows them as requests. Reading the context back costs one
  storage round trip per execution, like any job parameter.

---

## Browser instrumentation

| Option (`options.Browser.*`) | Default | |
| --- | --- | --- |
| `Enabled` | `true` | the script at all |
| `TrackHashChanges` | `false` | fragment-only changes count as page views |
| `TrackPageVisitTime` | `false` | how long each page was visited, as a `browser.page_visit_time` event (Application Insights: `PageVisitTime`) |
| `PageRoutes` | none | route templates (`/orders/{id:int}`) page paths are matched against, besides the discovered ones - see [Page route templates](#page-route-templates) |
| `DiscoverPageRoutes` | `true` | add the `@page` routes of the app's components to `PageRoutes` |
| `PageRouteAssemblies` | none | (code only) the assemblies discovery reads, exactly: your router's `AppAssembly` and `AdditionalAssemblies` |
| `InteractionEvents` | `click` | DOM events recorded as interactions |
| `CaptureInteractionText` | `false` | record the clicked element's text (may contain personal data) |
| `TrackRageClicks` | `true` | three clicks on one element within a second also emit one `browser.rage_click` event per burst - the frustration signal of a user who expected something to happen |
| `WebVitals` / `DocumentLoad` | `true` | LCP, INP, CLS, FCP, TTFB / the load span with navigation timing |
| `ResourceTiming` / `LongTasks` | `false` | spans per fetched resource / main-thread tasks over 50 ms |
| `Fetch` / `Xhr` | `true` | dependency spans |
| `PropagateTraceContextTo` | `same-origin` | where `traceparent` is added (`same-origin`, `*`, or regexes) |
| `IgnoreUrls` | Blazor framework routes | regexes never recorded as dependencies |
| `IgnoreErrorMessages` | `ResizeObserver loop`, `^Script error\.?$` | regexes of errors never recorded (the second is a cross-origin script's error without CORS: no message, no stack, nothing to act on) |
| `CaptureConsole` / `ConsoleLevels` | `false` / `error`, `warn` | console capture |
| `SessionInactivityTimeout` / `SessionMaxDuration` | 30 min / 24 h | session rotation |
| `SessionSampleRatio` | `1.0` | fraction of sessions recorded |
| `FlushInterval`, `MaxBatchSize`, `MaxQueueSize` | 5 s, 100, 1024 | batching |
| `MaxRelayedPerMinute` | 3000 | records one circuit may relay per minute on Blazor Server (see [Transport](#transport-direct-and-relay)) |
| `Debug` | `false` | the script's own diagnostics in `console.debug` |

### Page route templates

A page view carries `url.template`: the route of the page, not its address, so that `/orders/42` and `/orders/43`
are both `/orders/{id:int}` - one page in per-page charts, alerts and the Application Insights `pageViews` table, not
one per order. Every page view the script records has one. A `TrackPageView` .NET records itself (WebAssembly, Hybrid,
a server with no page connected) has one when it has a URL: the one you pass (a relative one is below the base path),
else the current page's (`NavigationManager.Uri`); only where there is neither - no URL passed, outside a page - it has
none. It is, in this order:

1. the `url.template` you pass to `TrackPageView` (a dialog or a wizard step has no route of its own);
2. the first of the app's routes that matches the path, as the Blazor router picks it (literal segments before
   constrained parameters, before parameters, before catch-alls; case-insensitive; constraints such as `int`, `guid`
   or `length(3)` checked; a segment of several parts, `{name}.{ext}`, split from the right), relative to the
   `<base href>` and written after it (`/shop/orders/{id:int}` under `<base href="/shop/" />`);
3. otherwise the path with the segments that look like ids replaced: numbers by `{n}`, GUIDs by `{guid}`, long
   hexadecimal or mixed letters-and-digits tokens by `{hex}` or `{id}` (`/orders/{n}`). A slug or a user name stays.

.NET runs the same rules, with the framework's own parsers for the constraints; the script approximates those (a
`datetime` value, for one, is a numeric date, a time, or a date with a month name - as .NET's invariant culture reads
them). A `regex` constraint that could take exponential time on a crafted path - a repeated group holding a quantifier
or an alternation, such as `(a+)+`, or a backreference - is not run in the browser, which cannot stop it (.NET gives
it a timeout): the parameter takes any value there, as a constraint of the app's own does on both sides.

The app's routes are found once per process: the `@page` (`[Route]`) templates of the components in the entry
assembly and in the component libraries it references (a server's `.Client` project, a shared page library for MAUI) -
close to the assemblies an app hands its router, but not exactly: a referenced library's own pages count even if the
router was not given it. For the router's exact list, set `Browser.PageRouteAssemblies` to its `AppAssembly` and
`AdditionalAssemblies`:

```csharp
options.Browser.PageRouteAssemblies.AddRange([typeof(App).Assembly, typeof(Client._Imports).Assembly]);
```

An app without an entry assembly (.NET for Android) is searched from its loaded assemblies instead, and an app whose
search finds no route at all logs a warning under `Bit.Belemetry`: set `PageRouteAssemblies` there. The routes are
written into the browser config only when page views are recorded. A page whose script was started already - an Auto
app's WebAssembly client in the page its server rendered - keeps the routes its server found and adds those configured
in the client (`PageRoutes` and the `PageRouteAssemblies`' routes), without a second search. Add routes discovery
cannot see (a lazy-loaded WebAssembly assembly's) to `Browser.PageRoutes`, or switch discovery off with
`Browser.DiscoverPageRoutes = false` and list them yourself.

A page view is named after the name you give `TrackPageView`, else its page's title (`<PageTitle>`), else its template
(it was its path) - in its log record's body, the Application Insights `pageViews` name and `ai.operation.name`, and the
`PageName` of its `PageVisitTime` - on every host, whether the script or .NET records it.

### Error floods and runaway values

An error thrown in a loop - a timer, a render loop, a retry - would fill the queue, evict every other record, and cost
the backend one record per occurrence from every client. So both the script and the .NET pipeline of a WebAssembly or
Hybrid app apply `options.Errors`:

- `DuplicateWindow` (5 s): an error identical to one just recorded - same scope (logger category), type, message and
  first stack frame - is suppressed for this long, so a loop is recorded once per window.
- `MaxPerMinute` (100): at most this many error records per minute, with a burst of as many; the excess is dropped.

An exception recorded on a span (`Activity.AddException`) is an error record too. One the rules drop stays on its
span (an OTLP span still shows it) but is not sent to Azure Monitor as an item of its own.

The server pipeline serves every user at once and is not capped. Zero switches either rule off.

`options.MaxValueLength` (32768 characters) cuts longer attribute values, log bodies and span status messages - after
privacy scrubbing, so a cut never hides an e-mail address from it. It is the OpenTelemetry attribute value length
limit, applied to bodies too, so one serialized object or huge stack trace cannot make a request too large to deliver.
Zero keeps strings whole.

Name interactions with `data-bit-belemetry-name`, and keep a region out entirely with `data-bit-belemetry-ignore`:

```html
<button data-bit-belemetry-name="checkout.pay">Pay</button>
<section data-bit-belemetry-ignore>...</section>
```

Input values are never read. An ignored region stays out everywhere: clicks in it are skipped, those in a shadow root
under it too, and with `CaptureInteractionText` its text is left out of the text of a clicked element around it.

---

## Privacy

Applied on both sides before anything is queued, and by .NET to every metric point it collects (`options.Privacy.*`):

- `RedactQueryParameters`: values of sensitive query parameters (`token`, `code`, `password`, `secret`, `key`,
  `access_token`, `email`, ..., and the signatures and credentials of pre-signed storage URLs - `X-Amz-Signature`,
  `X-Amz-Credential`, `X-Amz-Security-Token`, `X-Goog-Signature`, Azure SAS `sig`) become `REDACTED` in every URL.
- `RedactedAttributes`: attribute names whose values become `REDACTED`, whatever they look like. A name matches a key
  whole or its last dot-separated segment, case-insensitively, so the default `password` covers a structured log value
  `{Password}` and `user.password`, and `authorization` covers `http.request.header.authorization` - while
  `password_policy` is left alone; a trailing `*` matches a prefix (`http.request.header.*`). Defaults: `password`, `passwd`, `pwd`, `secret`,
  `client_secret`, `token`, `access_token`, `refresh_token`, `id_token`, `apikey`, `api_key`, `x-api-key`,
  `authorization`, `proxy-authorization`, `cookie`, `set-cookie`. A log message that renders such a value (or a
  blocked one) is exported with its template as the body (`Sign-in for {User} with {Password}`), so the value does not
  reach the backend in the message either.
- `StripQueryString` (off; it also drops `url.query`) and `StripFragment` (on - fragments may carry OAuth tokens).
  `url.query` gets the same parameter redaction as a URL.
- `ScrubPatterns` (on): bearer tokens, JWTs, e-mail addresses, card numbers (Luhn-checked) and IPv4 addresses are
  replaced in log bodies, span status messages and **every string attribute** - structured log values
  (`logger.LogInformation("User {Email}", email)`) and event attributes included - on both sides. Kept verbatim: the
  ids (`session.id`, `enduser.pseudo.id`, `user.id`), `server.address` and `user_agent.original` (whose four-part
  versions read as addresses). A key named like a version (`AppVersion`, `app.version`, `build_version`) keeps its
  four-part values too; the other patterns still apply to it.
- `CollectUserAgent` (on): the user agent string in the browser resource.
- `BlockedAttributes`: attribute keys (exact, case-sensitive) removed from every record, span event, span link and
  metric point, by the script and by .NET. The ids (`session.id`, `enduser.pseudo.id`, `user.id`) are governed by
  consent and untouched by every other privacy rule, but a blocked one is never added, and is removed from relayed
  browser records.
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

Processors run on every .NET-side record, including those relayed from the browser, after the privacy rules. One that
throws is skipped for that record (the record is kept), and reported once, as a warning in the `Bit.Belemetry`
category. With the Direct transport the browser's records never reach .NET; process them in the page instead (below).

### In the browser

```js
// Runs on every browser record as it is exported (or relayed), after consent and privacy. Change it in place;
// return false to drop it. A processor added right after the script started still sees the initial page view.
const remove = BitBelemetry.addProcessor(record => {
    if (record.eventName === 'browser.user_interaction' && record.attributes['target.name'] === 'noise') return false;
    record.attributes['app.release.channel'] = 'beta';
});

// Attributes on every record from now on (merged; null removes a key, setAttributes(null) removes them all). They
// go through privacy like any attribute, a record's own attributes win, and the consent-governed ids (session.id,
// enduser.pseudo.id, user.id) cannot be set this way - use setUser.
BitBelemetry.setAttributes({ 'app.tenant': 'contoso', 'feature.checkout_v2': true });
```

The record is the browser record model: a span has `traceId`, `spanId`, `name`, `kind`, `startTime`, `endTime`,
`status` and `events`; a log record has `time`, `severityNumber`, `body` and `eventName`; both have `attributes`,
`scope` and `feature`. Processors must be fast and synchronous: while the page unloads nothing else runs.

## Production diagnostics and support bundles

Telemetry tells you *that* something failed. When a user reports a problem, you need to know **what the Blazor app was
doing**: which route and component, which event handler, which requests failed before it, what the page reported, how
the circuit behaved, which release - and the trace ids to look the rest up in your backend. Production diagnostics
keep exactly that, in memory and bounded, and turn it into a **support bundle** the user (or a support agent) previews,
narrows and confirms before anything leaves the app.

What it is not: no session replay, no screenshots, no DOM contents, keystrokes, form values or clipboard; no
arbitrary object or memory dumps; no hosted service. A bundle is a ZIP of JSON documents and a Markdown report that
you download or hand to your own support integration.

### Turn it on

```csharp
builder.Services.AddBitBelemetryServices(o =>
{
    o.Diagnostics.Enabled = true;                                          // off by default

    // Optional: capture automatically (for review, never sent on its own) on unhandled exceptions, circuit failures and
    // exceptions a classifier marks critical.
    o.Diagnostics.AutoCaptureTriggers = BelemetryDiagnosticTriggers.Default;

    // Optional release markers, attached to every capture.
    o.Diagnostics.Release.Commit = Environment.GetEnvironmentVariable("GIT_COMMIT");
    o.Diagnostics.Release.Metadata["ring"] = "canary";
});
```

```json
{ "Belemetry": { "Diagnostics": { "Enabled": true, "AutoCaptureTriggers": "Default", "Release": { "Commit": "4f2a91c" } } } }
```

Call it in every project that hosts Blazor, as for the rest of Belemetry. It needs **no exporter**: an app can use
support bundles without sending any telemetry. Without one, though, Belemetry does not collect .NET spans, so the
timeline has no `HttpClient` or server request spans and none of the app's own `ActivitySource`s (the `Http` and `Trace`
categories hold the browser's requests only) - even when another tracer, such as an OpenTelemetry SDK, records them.
Logs, exceptions, the framework's navigation and UI events and the browser's evidence are there either way. Keep
`<BelemetryInitializer />` in `Routes.razor` (it registers a Blazor Server circuit's timeline and connects the browser's).

The defaults are the production-safe ones:

| Option | Default | What it bounds |
| --- | --- | --- |
| `MaxEvents` / `MaxFailureEvents` | 300 / 50 | The activity and failure rings (failures have their own, so chatter cannot evict the error being diagnosed). Per circuit on a server |
| `MaxMemoryBytes` | 1 MiB | Estimated memory of one timeline; the oldest activity is evicted first |
| `MinimumLogLevel` | `Warning` | `ILogger` entries recorded (independent of the export's level) |
| `IncludeSuccessfulActivity` | `true` | Successful navigations, UI events and requests kept as context |
| `IncludeLogMessages` / `IncludeExceptionMessages` | `false` / `true` | Log entries keep their template (`Order {OrderId} failed`), not the rendered message (and always their template when the message renders a redacted key such as a circuit id); exceptions keep their scrubbed message - off, neither the message nor its template is kept, and the stack text keeps its frames only (grouping still uses the message, hashed) |
| `CreateFrameworkActivities` | null: yes on WebAssembly and Hybrid, no on a server | Whether Belemetry makes the framework create its `Navigate`/`HandleEvent` activities when its own traces do not cover them (see below) |
| `BrowserMaxEvents` | 200 | The browser script's own timeline |
| `MaxBundleBytes` | 4 MiB | A bundle's JSON, snapshots, groups and components included; past it the oldest activity is left out, then the oldest failures (never the subject) - and the oldest snapshots first when they alone do not fit - and the manifest says how much |
| `MaxExceptionGroups` | 50 | Groups remembered (least recently seen forgotten first) |
| `PostTriggerWindow` / `AutoCaptureCooldown` | 2 s / 10 min | What follows a trigger is included; one automatic capture per failure per cooldown |
| `MaxPendingCaptures` / `CaptureRetention` | 3 / 30 min | Automatic captures waiting for review (a new one replaces the oldest) |
| `Snapshots.*` | depth 6, 50 items, 1024 chars, 32 KiB, 2 s | One snapshot provider's output |

### Report a problem: capture, preview, confirm, export

```razor
@inject IBelemetryDiagnostics Diagnostics

<button @onclick="ReportAsync">Report a problem</button>

@if (_capture is not null)
{
    @* Sections with a checkbox each, the findings, every event with a Remove button, what is missing, the report as it
       will be sent - then "Confirm and download" or "Cancel". Unstyled: style the bit-belemetry-preview-* classes. *@
    <BelemetryCapturePreview Capture="_capture" OnConfirmed="_ => _capture = null" OnCancelled="() => _capture = null" />
}

@code {
    private BelemetryDiagnosticCapture? _capture;

    private async Task ReportAsync() =>
        _capture = await Diagnostics.CaptureAsync(new() { Reason = "user-report", IncludeSnapshots = true });
}
```

Without the component, the same flow in code - for example to send the bundle to your own support API:

```csharp
var capture = await diagnostics.CaptureAsync(new() { Reason = "support-ticket", Exception = errorBoundaryException });
if (capture.Succeeded is false) return;                       // capture.Error says why (diagnostics off, ...)

// Preview: capture.Sections, capture.Report (findings), capture.Events, capture.MissingEvidence, capture.ToMarkdown()
capture.ExcludeSection(BelemetryBundleSections.Snapshots);   // each change withdraws a previous confirmation
capture.RedactAttribute("tenant");
capture.SetDescription(userText);                            // user-provided, through the privacy patterns

capture.Confirm();                                           // the user agreed to share what they saw
await using var body = new MemoryStream();
await capture.ExportAsync(body);                             // throws unless confirmed
// ... POST body to your support endpoint, or: await diagnostics.DownloadAsync(capture);
```

`DownloadAsync` saves `capture.SuggestedFileName` in the browser (WebAssembly and Server; on Server the bundle is
streamed over the circuit), and in WebView2 on Windows. It returns false in a Hybrid app on Android, iOS or Mac Catalyst,
whose web view ignores downloads: write `ExportAsync` to a file or a share sheet there (the MAUI sample's
`SampleSettings.SaveBundle`), and pass `Download="false"` to `<BelemetryCapturePreview>`.

An `ErrorBoundary` is the natural place to offer it: pass its `CurrentException` as `Exception`, and the report is
about that failure. With `AutoCaptureTriggers`, `IBelemetryDiagnostics.CaptureAvailable` fires when an automatic
capture is ready (raised outside any render: use `InvokeAsync(StateHasChanged)`), and `PendingCaptures` holds them;
`Dismiss` one once it was handled.

### What a capture holds

| Section | Content |
| --- | --- |
| Release | `service.name`, version, environment, commit, build id, configuration version, metadata, host kind, runtime, OS family, architecture, browser brands/platform/language |
| Exceptions | Groups by fingerprint: type, message template, anchor frame, classification, count, first/last seen, releases, and the occurrences (at most the last five) of the events the bundle holds - an older failure the timeline no longer holds counts, but its message, trace and route are not exported |
| Timeline (.NET) | Navigation (route templates from the router), component UI events (`HandleEvent`: component type, method, event attribute), exceptions (type, scrubbed message, stack without file paths, inner exceptions, origin), logs (category, template), HTTP client and server spans (method, status, URL template, error type), framework circuit activities, the app's own events and recorded snapshots |
| Browser | Page views (route template), fetch/XHR (method, status, URL template, trace ids), JS errors and rejections (type, message, stack), clicks (event type and element only), long tasks, reconnection transitions, page hide/restore |
| Components | The component types the timeline names, ranked by association - `trace` (the failure happened in that component's event-handler trace), `stack` (inferred from the first application frame), `framework` - with render statistics when measured, and ancestry when a component-context provider (BevTools) knows it |
| Snapshots | The app's snapshot providers' sanitized output, only when the capture asks for them |
| Report | The issue report: title, summary, exception, release, route, triggering action, navigation, failed requests, interop failures, circuit transitions, components, trace ids, prerequisites, missing evidence, and every finding marked **observed**, **inferred** or **user-provided** |

Observed facts come straight from the evidence (an exception logged inside a `HandleEvent` activity *is* that
handler's failure). Inferred ones say so: a request that failed two seconds before the exception "may be the cause"; a
route templated from the page's URL is marked as such; a component named by a stack frame is an inference. Missing
evidence is listed, never filled in: an unreachable browser, events left out for consent or size, a hierarchy nobody
provided.

### Where the evidence comes from

| Source | WebAssembly | Server | Hybrid |
| --- | --- | --- | --- |
| `ILogger` (≥ `MinimumLogLevel`), exceptions (renderer, `ErrorBoundary`, circuit, app) | yes | the circuit's own | yes |
| `Navigate` / `HandleEvent` framework activities (.NET 10+; registered by `AddBitBelemetryServices`) | yes | the circuit's own, when something creates them (below) | yes |
| .NET `HttpClient` spans | as browser fetch spans | with traces collected (`Server.CollectDotnetTelemetry`) | with `Traces` |
| The browser timeline (fetch/XHR, JS errors, page views, clicks, reconnection) | yes | yes, the user's tab | yes (the WebView) |
| Server request spans and errors of the page's own requests, by trace id | other process: trace ids | yes | - |
| Render statistics (`ComponentRenderStatistics`; the framework's lifecycle meter) | call `AddComponentsMetrics()` | yes | call `AddComponentsMetrics()` |

**The framework's component activities.** They name the component, the event handler and the route of a failure. An
activity that exists becomes the parent of the spans started under it, and an unsampled parent makes a parent-based
sampler - OpenTelemetry's default - drop them. So Belemetry makes the framework create them only where that changes
nothing for the app's traces: when its own exported traces cover `Microsoft.AspNetCore.Components` (then they decide,
and a name in `Traces.IgnoredActivityNames` is never created), or, without them, on WebAssembly and Hybrid, where
Belemetry is the app's tracer. On a server with an OpenTelemetry setup of its own, either have that setup listen to
`Microsoft.AspNetCore.Components` (Belemetry observes what exists), or set `Diagnostics.CreateFrameworkActivities = true`
knowing that spans under a UI event then follow its (unsampled) flag.

**Blazor Server: one timeline per circuit.** The server serves everyone, so each circuit records only what is
attributable to it: code that ran on its renderer's synchronization context, or under one of its UI events or
navigations *while that event still runs* (work that left the renderer with `ConfigureAwait(false)` still carries the
event's activity). Anything else - a background job, another user's request, a timer a click handler started that
keeps running after it - is never put on any user's timeline. Every scope of a circuit (an `OwningComponentBase`'s
included) shares its timeline. The browser half comes from
that user's own tab over the circuit. A server error logged while one of the page's own requests ran is added by the
trace id the page generated and propagated. Record circuit transitions with a scoped `CircuitHandler`:

```csharp
public sealed class CircuitDiagnostics(IBelemetryDiagnostics diagnostics) : CircuitHandler
{
    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        diagnostics.Record("circuit.connection_down", severity: BelemetrySeverity.Warn, category: BelemetryDiagnosticCategories.Circuit);
        return Task.CompletedTask;
    }
}

builder.Services.AddScoped<CircuitHandler, CircuitDiagnostics>();
```

Never record the circuit id: it is a bearer secret (the sanitizer redacts `CircuitId` keys anyway). The browser
script also records the reconnect dialog's transitions (`components-reconnect-state-changed`), when the app has a
reconnect UI of its own (`#components-reconnect-modal`, as the .NET 10 template's).

**Prerendering and render-mode changes.** The browser timeline lives in the page from its first load (the script
starts from `<BelemetryScript />`), so a prerendered page that turns interactive keeps one browser timeline. The .NET
side of a prerendering request is its own short-lived timeline.

### Exception grouping and releases

`BelemetryExceptionFingerprint` groups deterministically, independent of any backend: the exception type, the
message template (`Order {n} not found` - GUIDs, timestamps, long hex, quoted values, numbers of four digits or more
and URL path ids replaced; short numbers stay, a 404 is not a 500) and up to three meaningful frames (application
frames, without file paths, line numbers, arguments or compiler-generated names; JavaScript frames as
`function@file.js`). The algorithm is versioned (`BelemetryExceptionFingerprint.Version`), and every exported group
says which version made it. Groups span releases by default; `GroupAcrossReleases = false` keeps each release's
occurrences apart. Every occurrence keeps its release, time, trace id, route and component.

Classify exceptions of your own domain with an `IBelemetryExceptionClassifier` (a singleton): a category, a grouping
override, `Critical` (an automatic capture trigger), or `Ignore` for an expected, noisy failure.

```csharp
public sealed class PaymentsClassifier : IBelemetryExceptionClassifier
{
    public BelemetryExceptionClassification? Classify(BelemetryDiagnosticException exception) =>
        exception.Type == "MyApp.PaymentDeclinedException" ? new() { Category = "payments", Critical = true } : null;
}

builder.Services.AddSingleton<IBelemetryExceptionClassifier, PaymentsClassifier>();
```

### Snapshots of application state

Belemetry never serializes components, services or arbitrary objects. A snapshot provider describes what support
needs, as plain values, when a capture asks for it (`IncludeSnapshots = true`) - and it is sanitized, bounded and
isolated (one that throws or exceeds its timeout is reported, and the others still run):

```csharp
public sealed class CartSnapshot(Cart cart) : IBelemetrySnapshotProvider
{
    public string Name => "cart";

    public ValueTask<IReadOnlyDictionary<string, object?>?> CaptureAsync(BelemetrySnapshotContext context, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyDictionary<string, object?>?>(new Dictionary<string, object?>
        {
            ["itemCount"] = cart.Items.Count,
            ["currency"] = cart.Currency,
            ["step"] = cart.CheckoutStep,                     // enums, dates, numbers, strings, nested dictionaries and lists
        });
}

builder.Services.AddBitBelemetrySnapshotProvider<CartSnapshot>();    // scoped: per circuit on Blazor Server
```

`await diagnostics.RecordSnapshotAsync("before-checkout")` keeps the providers' snapshots on the timeline, so a later
capture shows the state before an operation next to the state at the end. A snapshot is a description, not a
restorable state.

### Privacy, consent and security

- **Consent applies as for telemetry.** The timeline holds what the pipeline accepted for a feature the user granted
  (or has not decided on yet - then it is held, and dropped if not granted); a capture includes granted features only,
  checked again when it is previewed and exported; a revoke purges the timeline and discards pending captures (one
  being built across the revoke too). `ClearAsync` forgets the .NET timeline and the page's, with its stored copy. On a server, the server's own records answer to
  `Server.Features`, as the server pipeline does. Consent your banner asks for covers the timeline: it never leaves
  the device unless the user confirms a bundle.
- **One sanitizer for everything**, applied when a value is recorded and again at export: blocked and redacted keys
  (the privacy options' lists, `Diagnostics.RedactedKeys`, and any key that *contains* `password`, `secret`, `token`,
  `apikey`, `authorization`, `cookie`, `credential`, `connectionstring`, `signature`, card names), your
  `IBelemetryDiagnosticSanitizer`s (a singleton; one that throws excludes the value), pattern scrubbing (e-mail,
  bearer, JWT, card, IPv4 - **always on here**, whatever `Privacy.ScrubPatterns` says for export), and size limits.
- **The export's rules first.** The timeline taps each record after the privacy rules and the app's processors: an
  exception message the export blocks or a processor rewrites is blocked or rewritten here too, in the browser as in
  .NET (a session sampled out of the export still runs the processors before its timeline keeps a record).
- **What the user removes stays removed.** Excluding an event or a section in the preview takes its exception
  occurrences out of `exceptions.json` too, and a redacted key is redacted in the report as well as in the timeline.
- **Metadata, not values.** Events keep the attributes their category needs (method, status, route template,
  component type, ...) plus `AllowedAttributes`. URLs become templates (`/orders/{n}`) with only
  `AllowedQueryParameters`, no fragment, no credentials. Never request or response bodies, headers, cookies, user ids,
  session ids or DOM text.
- **A bundle is data.** JSON (with HTML-sensitive characters escaped) and Markdown whose values are code spans or
  escaped text - no HTML, links, scripts or serialized objects - and a manifest with every file's SHA-256.
- **A browser is untrusted input on a server.** The page's timeline is bounded and sanitized again in .NET, trace ids
  are validated, and a page can trigger at most what the capture limits allow.

### Correlating with your backend

Trace ids are the link. The report lists the subject's trace and those of failed requests and failures; the timeline
carries trace, span and parent span ids (never invented: an event without a real parent has none). Look them up in
the OTLP backend Belemetry - or your own OpenTelemetry setup - exports to: the browser's fetch spans propagate
`traceparent` to the origins in `PropagateTraceContextTo` (same origin by default), so a page's request and the
server's span share one trace. A session id is neither a trace id nor a user id, and none of them is in a bundle.

### BevTools

[BevTools](../BevTools/) is the interactive side. The `Bit.BevTools.Belemetry` package
(`builder.Services.AddBitBevToolsBelemetry()`) adds a **Support** panel that lists this session's timeline and exception
groups, opens the component tree at the component a failure is associated with, filters the timeline to a trace,
previews and exports bundles and validates imported ones - and gives captures the component hierarchy and render counts
BevTools observes (`IBelemetryComponentContextProvider`). BevTools' own UI events (its panels' buttons and tabs) stay off
the timeline (`IBelemetryDiagnosticComponentFilter`, which any tool rendered in the app can implement). Belemetry works
without it: BevTools is a development tool, and in production a capture names the component types the framework
reported and says the hierarchy is unavailable.

### The bundle format

A ZIP (deterministic: sorted keys, fixed entry times) holding:

| File | Content |
| --- | --- |
| `manifest.json` | `format` (`bit-belemetry-support-bundle`), `schemaVersion` (1), generator, capture id and time, host, trigger, ordering note, fingerprint version, sections (status, items, omitted, note), every file's size and SHA-256, privacy summary (redactions, the user's preview changes, consent), limits, dropped evidence by reason, missing evidence |
| `report.md` / `report.json` | The issue report, human- and machine-readable |
| `release.json`, `exceptions.json`, `timeline.json`, `components.json`, `snapshots.json` | The sections, when included |

Events are ordered by time, then by sequence; sequences count per source (browser events are prefixed `b` in
references), and `elapsedMs` is each source's monotonic clock. `BelemetrySupportBundle.ReadAsync` reads and validates
a bundle - format, version (a newer one is refused), checksums (a listed file without one is an error, and is not read) -
with bounded decompression and no extraction to disk, ignoring files the format does not define.

### Troubleshooting incomplete captures

- **Empty timeline:** `Diagnostics.Enabled`, consent (the timeline follows it), `MinimumLogLevel`, `Categories`.
- **"Browser evidence: the browser script was not reachable":** the capture ran during prerendering or after the
  circuit dropped, the script did not load, or the page's config predates `Diagnostics.Enabled` (reload).
- **No components:** the framework reports `HandleEvent`/`Navigate` from .NET 10 in interactive render modes only;
  static SSR has none. On a server they exist only when something creates them (`CreateFrameworkActivities`, or a
  tracer listening to `Microsoft.AspNetCore.Components`). No hierarchy without a component-context provider (BevTools).
- **A server event missing from a circuit's capture:** it ran neither on the circuit's renderer nor under one of its
  UI events (a background service, a `Task.Run` started outside an event). That is the isolation working; record it
  explicitly with `IBelemetryDiagnostics.Record` from the circuit's scope when it matters.
- **Events "left out at the bundle size limit" or "evicted":** raise `MaxBundleBytes`, `MaxEvents` or
  `MaxMemoryBytes`, or capture closer to the failure (`Lookback`).

---

## Offline persistence

With `Persistence` consent and `options.Persistence.Enabled` (default), requests that fail with a network error or a
retryable status are kept - for OTLP 429, 502, 503 and 504, the only ones the OTLP specification allows retrying; for
Azure Monitor also 408, 439 and 500, which its ingestion endpoint asks to retry; any other status is the backend's final
word and the request is dropped, with a warning - in `localStorage` in the
browser (`BrowserMaxBytes`, 1 MB), in files in Hybrid and on servers (`Directory`, `MaxBytes` 20 MB) - and retried
every `RetryInterval` (1 min, on both sides), when the page starts and when the browser comes back online, for up to
`MaxAge` (48 h). A retry pass sends the stored requests oldest first, one attempt each. A request that fails holds
back the rest of its URL's backlog until the next pass - that backend is still unreachable, and hammering it would only
waste the network - but not the requests to other backends. The browser takes each stored request out of
`localStorage` only while sending it, so the backlog survives the page going away mid-pass (and a request being sent
without `keepalive` goes back to storage when the page is hidden).

When the page is hidden or unloads, the script sends what is queued with `fetch(..., { keepalive: true })`, which
outlives the page - but a browser caps the bodies of all of a page's keepalive requests in flight at 64 KiB together.
The script tracks that budget: a request that does not fit is stored (with `Persistence` consent) and sent on the next
visit, or sent as a plain request without it (which completes if the page was only hidden). Delivery during unload is
best effort; nothing can guarantee it.

Stored requests never contain headers, so API keys are not written to disk or `localStorage`. The exporter's current
headers are added when a stored request is sent, so a rotated key applies to the backlog too. (In WebAssembly, a .NET
request stored by the browser is resent with the headers of the browser exporter posting to the same URL.) A request
stored for an endpoint the exporter no longer posts to is dropped, so the current key never goes to an old address.

A .NET process that ends with an unhandled exception saves its queue to the store as it goes down, and the next
launch sends it - see [Unhandled exceptions](#unhandled-exceptions).

---

## The server pipeline

On an ASP.NET Core host, Belemetry's .NET pipeline is **off by default** - such hosts usually have their own
OpenTelemetry setup - and Belemetry only serves the browser script. Turn it on to collect the server's logs, traces
(including `Microsoft.AspNetCore` request spans) and metrics with the same exporters:

```csharp
options.Server.CollectDotnetTelemetry = true;
options.Server.Features = BelemetryFeatures.Errors | BelemetryFeatures.Logs | BelemetryFeatures.Traces | BelemetryFeatures.Metrics;
options.Server.IgnoredRequestPaths.AddRange(["/health", "/alive", "/_framework", "/_content", "/_blazor"]);   // no request spans for probes and Blazor's own traffic
```

`IgnoredRequestPaths` matches `url.path`; and since the span is dropped when it ends, a child it
had - the database check behind `/health` - is still exported, with a parent that never arrives.

The server pipeline serves every user at once, so it is governed by `Server.Features` (operator telemetry), not by
any one user's consent. User consent keeps governing the browser script and the `IBelemetry` calls of each circuit.
A host without Blazor at all - an API, a worker - works the same way: it has no JS runtime, so `IBelemetry` goes
straight to the server pipeline.

A server also describes where it runs, the way the OpenTelemetry resource detectors do, from environment variables
and `/proc` alone (no metadata-service call): `host.name`, `process.pid`, `process.executable.name`, `process.owner`,
`container.id` (Linux), and on Azure App Service, Functions or Container Apps `cloud.provider`, `cloud.platform`,
`cloud.region`, `cloud.resource_id`, `host.id` and `azure.*`. Its `service.instance.id` is the platform's instance
name (`WEBSITE_INSTANCE_ID`, `CONTAINER_APP_REPLICA_NAME`) or the machine name, so Application Insights keeps counting
the same instance across restarts - a device or a browser tab keeps a random id, which identifies nobody.

ASP.NET Core's request activity (`Microsoft.AspNetCore.Hosting.HttpRequestIn`) carries no HTTP tags of its own by
default. Belemetry fills them in without a middleware or a reference to ASP.NET Core, from the tags ASP.NET Core
puts on its `http.server.request.duration` measurement: `http.request.method`, `url.scheme`, `http.route`,
`http.response.status_code`, `network.protocol.version` and `error.type`. The span is named `{method} {route}` (or
`{method}` without a route, unless the app renamed the activity), and a 5xx marks it as an error. Belemetry
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

On Blazor Server the page at the other end of a circuit can be anything that speaks the interop protocol, and the
queue it relays into is shared by every user, so the server treats relayed batches as untrusted input: it accepts them
only with the Relay transport, limits each circuit to a burst of `Browser.MaxQueueSize + Consent.PendingLimit`
records and `Browser.MaxRelayedPerMinute` (3000) per minute after it (the excess is dropped, with one warning per
circuit), drops features the configuration switched off, and decides the service identity itself - `service.name`,
`service.namespace`, `service.version` and `deployment.environment.name` come from the server, the rest of the
resource from the page, and none of the server's own host or process attributes are added (as with the Direct
transport).

---

## Environment variables

Outside the browser, with `options.UseEnvironmentVariables` (default on), the standard variables fill in what the code
leaves unset - this is how the Aspire AppHost and most container platforms wire telemetry in. A value set in code
always wins, as in the OpenTelemetry SDKs (headers merge per key):

| Variable | Effect |
| --- | --- |
| `OTEL_SDK_DISABLED` | `true` (any case) turns Belemetry off entirely - an operator's kill switch, so unlike the others it wins over code |
| `OTEL_SERVICE_NAME` | `service.name` |
| `OTEL_RESOURCE_ATTRIBUTES` | extra resource attributes (`k=v,k2=v2`) |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | adds an OTLP exporter when none is configured, and fills unset endpoints |
| `OTEL_EXPORTER_OTLP_TRACES_ENDPOINT`, `_LOGS_ENDPOINT`, `_METRICS_ENDPOINT` | per-signal endpoints (any of them adds the exporter too); with gRPC, a base address the service method is appended to |
| `OTEL_EXPORTER_OTLP_HEADERS` | OTLP headers (`k=v,k2=v2`) |
| `OTEL_EXPORTER_OTLP_PROTOCOL` | `http/protobuf`, `http/json` or `grpc`, for exporters whose `Protocol` is not set in code; anything else is reported at startup and ignored |
| `OTEL_EXPORTER_OTLP_COMPRESSION` | `gzip` or `none`, for exporters whose `Compression` is not set in code |
| `OTEL_EXPORTER_OTLP_TIMEOUT` | the request timeout in milliseconds, for exporters whose `Timeout` is not set in code |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | adds an Azure Monitor exporter when none is configured |
| `WEBSITE_*`, `REGION_NAME`, `CONTAINER_APP_*` | the Azure App Service / Functions / Container Apps resource attributes and instance id of a server (see [the server pipeline](#the-server-pipeline)) |

---

## Semantic conventions

Resource: `service.name`, `service.version`, `service.namespace`, `service.instance.id`,
`deployment.environment.name`, `telemetry.sdk.name = bit-belemetry`, `telemetry.sdk.language` (`dotnet` / `webjs`),
`telemetry.sdk.version`, plus `os.*`, `host.arch`, `process.runtime.*`, `process.pid` (.NET), `host.name`,
`process.executable.name`, `process.owner`, `container.id`, `cloud.*` (servers only) or `browser.*`,
`user_agent.original` (browser).

| Record | Shape |
| --- | --- |
| Page view | log record, event `browser.page_view`, named (in its body) after the name given to `trackPageView`, else the page title, else its route template: `url.full`, `url.path`, `url.template` (the route, see [Page route templates](#page-route-templates)), `page.title`, `page.referrer`, `browser.navigation.type` (`initial`, `push`, `replace`, `pop`, `hash`, `manual`, or `restore` - the page came back from the back/forward cache, which fires no history event) |
| Page visit time | log record, event `browser.page_visit_time` (with `Browser.TrackPageVisitTime`): `url.full`, `url.path`, `url.template`, `page.title`, `page.visit.duration` (ms) |
| Interaction | log record, event `browser.user_interaction`: `event.type`, `target.element`, `target.id`, `target.name`, `target.xpath` |
| Rage click | log record, event `browser.rage_click` (with `Browser.TrackRageClicks`): the target attributes, `event.type = rage_click`, `interaction.click_count` |
| Web vital | log record, event `browser.web_vital`: `web_vital.name`, `web_vital.value`, `web_vital.delta`, `web_vital.rating`, `web_vital.id`, `web_vital.navigation_type`. A back/forward-cache restore starts a new generation under new ids, as Google's web-vitals library does: FCP and LCP measured from the restore to the next painted frame, CLS and INP from zero, navigation type `back-forward-cache` |
| Document load | internal span `documentLoad` with navigation-timing events |
| fetch/XHR | client span: `http.request.method`, `url.full`, `server.address`, `server.port`, `http.response.status_code`, `error.type`. An unknown method is `_OTHER` (span name `HTTP`) with `http.request.method_original`; a request the app aborted keeps `error.type = AbortError` but no error status; `TimeoutError` and `NetworkError` (XHR) are errors |
| JS error / exception | log record, severity ERROR, event `exception`: `exception.type`, `exception.message`, `exception.stacktrace` |
| `ILogger` | log record with the category as scope, `log.event.id`, `log.event.name`, the structured values |
| EF Core command | client span named after the database: `db.system.name`, `db.namespace`, `server.address`, `server.port`, `efcore.command.source`, `db.query.text` (opt-in), `error.type` |
| Hangfire execution | consumer span `JOB {Type}.{Method}`: `job.id`, `job.type`, `job.method`, `job.queue`, `job.created_at`, `error.type` |
| Every record | `session.id`; `enduser.pseudo.id` and `user.id` with `UserTracking` |

---

## Azure Monitor mapping

| Record | Application Insights |
| --- | --- |
| Server/consumer span | `requests` |
| Other spans | `dependencies` (`Ajax` from the browser, `HTTP`, the database system, `InProc`) |
| Log with an exception | `exceptions` |
| Exception recorded on a span (`Activity.AddException`, an `exception` span event) | `exceptions`, under the span, as the Azure Monitor OpenTelemetry exporter writes it - but once only when the same exception (type, message and stack trace) is also logged under the span (`LogError(ex)` plus `AddException(ex)`): the log's item stands for both, unless this exporter does not send that log (`Signals`, `MinimumLogLevel`, or the log was dropped before the export) |
| Any other span event | `traces`, under the span, with the event name as the message - as the Azure Monitor OpenTelemetry exporter writes it |
| `browser.page_view` | `pageViews`, named after its body (the name given to `TrackPageView`), else `page.title`, else `url.template` (with `url.template` among the custom dimensions) |
| `browser.web_vital` | `customMetrics` (`LCP`, `INP`, `CLS`, `FCP`, `TTFB`) |
| `browser.page_visit_time` | `customEvents` as `PageVisitTime` with the `PageVisitTime` measurement and `PageName` (the page view's name)/`PageUrl` properties - the Application Insights JS SDK's shape |
| Other events | `customEvents` |
| Other logs | `traces` |
| Metrics | `customMetrics` (deltas; histograms with count, min, max) |

Tags: `ai.cloud.role` (`[service.namespace]/service.name`, or `service.name` alone - as the Azure Monitor
OpenTelemetry exporter writes it), `ai.cloud.roleInstance`, `ai.application.ver` (`service.version`), `ai.user.agent`
and `ai.device.locale` (browser), `ai.device.type` (`Browser`, `PC` or `Phone`), `ai.operation.id` (trace id), `ai.operation.parentId`,
`ai.operation.name` (requests and their events: `{method} {route}` or the span name; page views: the page name), `ai.session.id`,
`ai.user.id` (pseudo id), `ai.user.authUserId` (user id), `ai.location.ip` (`0.0.0.0` with `MaskIp`),
`ai.internal.sdkVersion`.

Retried statuses: 408, 429, 439, 500, 502, 503 and 504, and, in a 206 partial success, only the items rejected with
one of them.

---

## Using the script without .NET

`bit-belemetry.js` is an ES module and also sets `window.BitBelemetry`:

```js
BitBelemetry.setConsent(BitBelemetry.Features.All);
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
- **Delivery:** retries with backoff that honor `Retry-After` (on the statuses each backend's protocol calls
  transient), Azure partial-success (206) handling, OTLP `partial_success` responses reported as warnings (the
  specification forbids retrying them), offline persistence, and an unload drain in which WebAssembly hands its
  serialized batches to the browser to send with `keepalive`. .NET exports carry `User-Agent: Bit.Belemetry/<version>`
  (a page cannot set it).
- **No self-instrumentation:** the exporter's own HTTP calls are suppressed from tracing, and the script ignores
  exporter URLs and requests already traced by .NET.

---

## Troubleshooting

- **Nothing arrives:** check consent first (`IBelemetryConsent.Granted`, or `BitBelemetry.getContext()` in the
  browser console) - by default nothing is sent until the user consents. `BitBelemetry.getStats()` says what the
  script dropped and why (`not_consented`, `sampled_out`, `queue_full`, ...) and what became of its requests
  (`rejected` means the backend refused them - a key or an endpoint; on .NET those are logged as warnings). Then set
  `options.Browser.Debug = true` and `options.Debug = true`, and add `options.AddConsoleExporter()`.
- **CORS errors in the browser console:** the receiver does not allow the page origin - configure CORS on the
  collector, use `BrowserEndpoint`, or switch to the Relay transport.
- **No .NET telemetry on the server:** that is the default; see [the server pipeline](#the-server-pipeline).
- **Nothing reaches the Aspire dashboard:** the AppHost hands its resources a gRPC endpoint. That works with
  `OTEL_EXPORTER_OTLP_PROTOCOL=grpc` (which it sets) and `BelemetryOtlpProtocol.Grpc`; an exporter with its protocol
  fixed to HTTP in code posts HTTP to the gRPC port and fails quietly. Either let the environment decide the protocol,
  or give the AppHost `WithOtlpExporter(OtlpProtocol.HttpProtobuf)`. Belemetry reports an unknown protocol value at
  startup, as a warning in the `Bit.Belemetry` category.
- **Logs and traces from WebAssembly, but no metrics:** `System.Diagnostics.Metrics` is off in the app, and Belemetry
  logs a warning at startup saying so. The package turns it on by default, so look for
  `<MetricsSupport>false</MetricsSupport>` in the client project, or a project reference to Bit.Belemetry instead of
  the package (see [Metrics](#metrics)).
- **Duplicate HTTP spans in WebAssembly:** they should not happen - the script skips requests that already carry a
  `traceparent` from .NET's `HttpClient`. If a custom handler strips it, add the URL to `Browser.IgnoreUrls`.
- **No `System.Net.Http` spans in WebAssembly:** the WebAssembly SDK turns `HttpClient`'s tracing off
  (`HttpActivityPropagationSupport`), and its requests show up as the browser script's fetch spans instead. Turning it
  on has a cost: `HttpClient` then sends `traceparent` to every origin, which a cross-origin API must allow in CORS.

---

## Upgrade notes

Behavior that changed with the hardening release, and how to get the previous behavior where that is still possible.
The reasons are recorded in [`Belemetry-Improvement-Plan.md`](Belemetry-Improvement-Plan.md).

**Security and privacy**

- **A server no longer publishes exporter `Headers` in the page.** On Blazor Server, static SSR and the server half of
  Auto, an OTLP exporter's `Headers` (and `OTEL_EXPORTER_OTLP_HEADERS`) stay on the server; the browser script exports
  without them, and a startup warning names the exporter. Move a key that is meant to be public to `BrowserHeaders`,
  or switch to the Relay transport. WebAssembly and Hybrid are unchanged.
- **The page never gets an OTLP endpoint's user info.** `https://user:token@...` reaches the browser script without
  `user:token@`, on every host, and a server warns at startup. Neither `HttpClient` nor the browser ever sent it: put
  the credential in `Headers` (an `Authorization` header).
- **Every string attribute is scrubbed on the .NET side too**, as the browser script always did: structured log
  values and event attributes lose e-mail addresses, tokens, card numbers and IPv4 addresses. `client.address` becomes
  `[ip]`. Turn pattern scrubbing off with `Privacy.ScrubPatterns = false`.
- **Credential-like attribute names are `REDACTED`** (`Privacy.RedactedAttributes`: password, secret, token, API key,
  authorization, cookie names). Remove a name from the list in code (`options.Privacy.RedactedAttributes.Remove("token")`)
  to keep its values.
- **Pre-signed storage URLs lose their signatures** (`X-Amz-Signature`, `X-Goog-Signature`, ...; `RedactQueryParameters`).
- **A server lists no exporter URLs in the page's `ignoreUrls`.** A WebAssembly client's .NET exports carry a fetch
  option the script recognizes, so they are not traced even by a script the server started (Auto).
- **On Blazor Server, relayed batches are untrusted input**: accepted only with the Relay transport, rate-limited per
  circuit, and stamped with the server's service identity but not its host, process or cloud attributes.

**Delivery**

- **OTLP retries 429, 502, 503 and 504 only**, as the OTLP specification requires; 408, 439 and 500 from an OTLP
  backend are final (dropped, not persisted, with a warning). Azure Monitor keeps its own list.
- **Rejected requests and OTLP partial successes are warnings** in the `Bit.Belemetry` category (they were Debug).
- **During unload, a request that cannot get `keepalive`** (over the 64 KiB or 9-request budget) is stored for the
  next visit instead of being sent and refused.
- **Stored requests are resent oldest first, one attempt each**; a failure holds back only the backlog of its URL.
- **.NET exports send `User-Agent: Bit.Belemetry/<version>`** unless an exporter header sets one.

**Collected data**

- **Errors are deduplicated and capped** on client hosts: an identical error within `Errors.DuplicateWindow` (5 s) is
  suppressed, and at most `Errors.MaxPerMinute` (100) are recorded per minute. `TimeSpan.Zero` / `0` switch them off.
- **`Script error.` is ignored by default** (`Browser.IgnoreErrorMessages`).
- **Azure Monitor gets the events of a span:** an exception recorded on it (`Activity.AddException`) as an `exceptions`
  item, any other event (the document-load span's timings, for one) as a `traces` item - as the Azure Monitor
  OpenTelemetry exporter sends them; they were dropped. Expect more items. An exception also logged under its span
  is still one item, and `belemetry.records.exported` counts them for Azure Monitor.
- **An exception recorded on a span needs `Errors`**: without it the exception (even a stack trace alone) comes off the
  span (for every exporter, OTLP included) and the span is sent without it. The error limits apply to it too - in the
  browser as well, to an exception an app processor adds to a span.
- **Strings longer than `MaxValueLength` (32768) are cut.** `0` keeps them whole.
- **An aborted fetch/XHR is not an error span** (it keeps `error.type = AbortError`); unknown HTTP methods are
  `_OTHER`.
- **New records:** `browser.rage_click` events (`Browser.TrackRageClicks`), page views of type `restore` and a new
  web-vitals generation after a back/forward-cache restore.
- **Unhandled .NET exceptions are recorded** (`AppDomain.UnhandledException`, `TaskScheduler.UnobservedTaskException`)
  as `Errors` events, and a crashing process saves its queue for the next launch (with `Persistence`). Switch it off
  with `Errors.CaptureUnhandledExceptions = false`, e.g. when the app records them itself. See
  [Unhandled exceptions](#unhandled-exceptions).
- **Page views carry `url.template`, and an untitled one is named after it** (`/orders/{id:int}`, was `/orders/42`):
  in the log record's body, the Application Insights `pageViews` name and `ai.operation.name`, and the `PageName` of
  `PageVisitTime`. Group by `url.path` for the old breakdown. See [Page route templates](#page-route-templates).
- **A page view is named after the name given to `TrackPageView` on every host**, before its page's title: in
  Application Insights the name was the title (or the path) when the script recorded it (Server), and a
  `PageVisitTime`'s `PageName` follows. A .NET `TrackPageView` without a URL takes the current page's
  (`NavigationManager.Uri`), as the script takes `location.href`.
- **`OTEL_SDK_DISABLED=true` turns Belemetry off**, overriding code.

**Production diagnostics** (opt-in, `BelemetryOptions.Diagnostics`; see
[Production diagnostics and support bundles](#production-diagnostics-and-support-bundles))

- **`AddBitBelemetryServices` registers the framework's component `ActivitySource`** (`AddComponentsTracing`, public
  and idempotent) in WebAssembly and Hybrid too, as the server already did. With `Traces` collected, the
  `Microsoft.AspNetCore.Components.Navigate` and `HandleEvent` spans (in the default `Microsoft.AspNetCore.Components*`
  sources) are now exported from those hosts, and logs written inside an event handler carry its trace id. Drop them
  with `Traces.IgnoredActivityNames`.
- **A browser session sampled out of the export** (`SessionSampleRatio`) still keeps its local diagnostic timeline when
  diagnostics are on; nothing more is exported.
- **New APIs:** `IBelemetryDiagnostics`, `BelemetryDiagnosticsOptions`, `BelemetryDiagnosticCapture`,
  `<BelemetryCapturePreview>`, `BelemetrySupportBundle`, `BelemetryIssueReport`, `BelemetryExceptionFingerprint`,
  `IBelemetrySnapshotProvider` (`AddBitBelemetrySnapshotProvider<T>()`), `IBelemetryExceptionClassifier`,
  `IBelemetryDiagnosticSanitizer`, `IBelemetryComponentContextProvider`, `IBelemetryDiagnosticComponentFilter`; in the
  script `getDiagnostics`, `getDiagnosticsStream`, `clearDiagnostics` and `downloadFile`.

**New APIs:** `BelemetryOtlpExporterOptions.BrowserHeaders`, `BelemetryOptions.Errors`, `BelemetryOptions.MaxValueLength`,
`BelemetryPrivacyOptions.RedactedAttributes`, `BelemetryBrowserOptions.TrackRageClicks`, the `Bit.Belemetry` meter, and
in the script `addProcessor`, `setAttributes` and `getStats`.
