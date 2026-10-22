# Bit.BevTools

Developer tools for Blazor applications: a live component tree with the handlers and callbacks each component bound,
a render profiler that explains *why* a component rendered, a unified activity timeline, state inspection with diffs
(the authentication state included), routing, network, WebSocket/SignalR traffic and JS interop tracking, an error
center, a DI inspector, render-mode and prerendering diagnostics for Blazor Web Apps, circuit diagnostics for Blazor
Server, performance traces for Perfetto, and an evidence-based diagnostics engine. Built as developer infrastructure
with a public extension API, minimal runtime overhead and no changes to application source.

It runs in three places, all reading the same data through the same redaction: **in the app** as an overlay
(`<BevToolsPanel />`, every host including Blazor Hybrid), in a **companion window** that shows every BevTools runtime
of a page at once (a Server circuit and WebAssembly side by side) and opens exported sessions offline, and as a
**browser extension** that adds a *Blazor* panel to the browser's DevTools.

> Targets **.NET 8, 9 and 10** · Blazor WebAssembly, Blazor Server, Interactive Auto and static SSR. The richest
> answers (UI-event render causes, handler durations, render-diff and lifecycle timings) come from the framework's own
> diagnostics, which arrived in **.NET 10**; on .NET 8 and 9 those rows say so instead of showing nothing.

---

## Table of Contents

- [Installation](#installation)
- [Panel, companion window, browser extension](#panel-companion-window-browser-extension)
- [What it answers](#what-it-answers)
- [Honest capability model](#honest-capability-model)
- [Security](#security)
- [Extending](#extending)
- [Project layout](#project-layout)
- [Status](#status)

---

## Installation

```bash
dotnet add package Bit.BevTools          # Blazor WebAssembly / any Blazor host
dotnet add package Bit.BevTools.Server   # adds circuit instrumentation for Blazor Server
dotnet add package Bit.BevTools.Loader   # opens BevTools on demand, and lets WebAssembly lazy load it
```

Register the services in `Program.cs`:

```csharp
using Bit.BevTools;

builder.Services.AddBitBevToolsServerServices();   // or AddBitBevToolsServices() in WebAssembly / other hosts
```

In an **Interactive Auto** solution, call `AddBitBevToolsServerServices()` in the server project and
`AddBitBevToolsServices()` in the `.Client` project, so BevTools works whichever render mode the page ends up in.
In a WebAssembly client, pass the host environment explicitly so Development-only activation does not depend on
server process environment variables:

```csharp
builder.Services.AddBitBevToolsServices(o => o.Enabled = builder.HostEnvironment.IsDevelopment());
```

In **Blazor WebAssembly**, add `<MetricsSupport>true</MetricsSupport>` to the project if you want render-diff and
lifecycle timings: the WebAssembly SDK compiles `System.Diagnostics.Metrics` out by default. The About panel says so
when it is missing, and everything else still works without it.

Then place the panel once, inside an interactive component:

```razor
@* _Imports.razor *@
@using Bit.BevTools

@* MainLayout.razor (or any interactive component) *@
<BevToolsPanel />
```

That is the whole setup. BevTools is enabled only when the host environment is `Development` (override with
`options.Enabled`), renders nothing during prerendering, and stays dormant in production builds. The browser bridge
(`_content/Bit.BevTools/bit-bevtools.js`) and its stylesheet ship as static web assets of the package and are loaded by
the panel itself, so no `<script>` or `<link>` tag is needed.
Press **Ctrl+Shift+D** or click the badge to open it; **Ctrl+K** opens search and the command palette.

### Opening it on demand, and lazy loading

An app that wants BevTools in production too, without anyone downloading it until they ask for it, references
**`Bit.BevTools.Loader`** next to `Bit.BevTools` and lets Blazor WebAssembly lazy load the latter:

```xml
<PackageReference Include="Bit.BevTools" />
<PackageReference Include="Bit.BevTools.Loader" />

<!-- the Blazor WebAssembly project; leave it out where BevTools should register at startup -->
<BlazorWebAssemblyLazyLoad Include="Bit.BevTools.wasm" />
```

```csharp
builder.Services.AddBitBevToolsLoaderServices(o => o.RegisterAtStartup = builder.HostEnvironment.IsDevelopment());
```

```razor
@* MainLayout.razor, instead of <BevToolsPanel /> *@
<BevToolsLoaderPanel />

@* the app's own "Open dev tools" button *@
@inject BevToolsLoader BevToolsLoader
@if (BevToolsLoader.CanOpen)
{
    <button @onclick="BevToolsLoader.OpenAsync">Open dev tools</button>
}
```

The loader is small, trimmable and AOT compatible, and it is the only BevTools assembly the startup code touches: an
app that lazy loads BevTools must not name a `Bit.BevTools` type in code that runs before the load, which is why the
loader repeats the few options it forwards (`ShowBadge`, `ToggleShortcut`, `TrackHttp`, `UseFrameworkInstrumentation`).

- **Registered at startup.** When `Bit.BevTools` is among the assemblies the app starts with and should run from the
  first render (`RegisterAtStartup`; Development by default), the loader registers it exactly as
  `AddBitBevToolsServices` does and `<BevToolsLoaderPanel />` renders the panel from the start.
- **Attached on demand.** Otherwise it installs hooks that cost next to nothing until BevTools is attached: a
  pass-through `IComponentActivator` (the renderer takes its activator when it is created, so this is the only way in
  without reflection), pass-through handlers in every `IHttpClientFactory` pipeline, the framework's ActivitySource and
  meters (.NET 10; a renderer reads them when it is created too), and the service collection for the Services panel.
  `BevToolsLoader.OpenAsync()` then loads `Bit.BevTools` (through `LazyAssemblyLoader` in Blazor WebAssembly), attaches
  it to the running app and opens the panel. It reads the components that already exist from the renderer and sees
  everything that happens from then on; what happened before stays unrecorded. The session ends with the scope.
- **Hand-built `HttpClient`s** record through `BevToolsHttpHookHandler`, which passes requests through until BevTools
  runs. Put it closest to the transport handler so every retry attempt is recorded.
- **Blazor Server.** In a process that serves many users, `CanOpen` is false outside Development unless
  `AllowInMultiUserHost` is set **together with** `Authorize`, a callback that decides who may open it: the panel would
  otherwise show whoever opens it the server side objects of their circuit. `OpenAsync` asks `Authorize` before
  anything is loaded, on every open, and treats an exception as a refusal; `IsAuthorizedAsync()` lets the app show its
  button only to those people. Such a session reads no application values (`ValueAccess` is `None`, see
  [Security](#security)) unless the app says otherwise, and every attach writes a warning to the app's own log
  (category `Bit.BevTools`). WebAssembly and Blazor Hybrid only ever show users their own process.

```csharp
builder.Services.AddBitBevToolsLoaderServices(o =>
{
    o.AllowInMultiUserHost = true;
    o.Authorize = async services =>
    {
        var state = await services.GetRequiredService<AuthenticationStateProvider>().GetAuthenticationStateAsync();
        return state.User.IsInRole("Developer");
    };
});
```

An app that trims keeps `Bit.BevTools` whole once it uses it, the loader's reference by name included (an
`ILLink.Descriptors.xml` embedded in the assembly), so a lazily loaded copy keeps everything it reflects over; an app
that references it but never uses it ships none of it. AOT builds cannot lazy load and start with it instead. In Blazor
WebAssembly, render-diff and lifecycle timings still need `<MetricsSupport>true</MetricsSupport>`; consider it for
Development only.

Without the loader, `BitBevTools.Attach(serviceProvider)` attaches BevTools from a component rendered after the load
(then render `<BevToolsPanel />` from that scope). It falls back to reflection where the loader has hooks: it replaces
the renderer's activator and wraps the handler of the `HttpClient` registered in DI, and cannot see `IHttpClientFactory`
clients or the DI graph; in an app that called `AddBitBevToolsLoaderServices` it uses the loader's hooks instead. In a
server process outside Development it stays off unless `BitBevToolsOptions.Enabled` is true. Either way the session ends
with its scope, and a BevTools registered at startup but left off does not stand in the way of an attach. The About
panel lists exactly what an attached session can see.

---

## Panel, companion window, browser extension

BevTools is split the way the browser's own developer tools are: an **agent** in each .NET runtime (the session that
instruments the renderer), a **protocol** it answers (`BevToolsProtocol`), and **frontends** that read it.

```
 .NET runtime (circuit or WebAssembly)            page                         frontends
 ─────────────────────────────────────    ───────────────────────────    ──────────────────────────────
 BevToolsSession ── BevJsBridge ─────────► page hub (bit-bevtools.js) ──► in-app panel (Razor)
   instrumentation   [JSInvokable]           window.__BIT_BEVTOOLS__  ──► companion window (opener)
   stores, rules     Protocol(...)           targets · traffic        ──► browser extension (DevTools)
```

| Frontend | Where | Strengths |
|---|---|---|
| **In-app panel** `<BevToolsPanel />` | Inside the app, every host (Server, WebAssembly, Auto, Blazor Hybrid) | Live object inspection, Razor panels contributed by extensions, nothing to install |
| **Companion window** `⧉` in the panel header, or `_content/Bit.BevTools/companion.html` | A window of its own, same origin | Every runtime of the page at once; renders nothing into the app, so on Blazor Server it does not add BevTools' own render batches to the circuit it measures; opens exported sessions offline (*Open file*, or drop the file) |
| **Browser extension** `Bit.BevTools.Extension` | A *Blazor* panel in Chromium or Firefox DevTools | The same frontend as the companion, where developers already look; asks for no permission and injects no content script ([README](Bit.BevTools.Extension/README.md)) |

**Several runtimes on one page.** A Blazor Web App can run a Server circuit and the WebAssembly runtime on the same
page. Each runtime that renders a `<BevToolsPanel />` attaches to the page hub as a *target*; page-level facts (JS
errors, failed resource loads, CSP violations, WebSocket traffic, enhanced navigation) reach every target, JS → .NET
calls go to the runtime that received them, and the panel header offers a switch to the other runtime's panel. The
companion window and the extension list every target. A runtime appears there once its panel has rendered; set
`Ui.ShowBadge = false` to keep that panel invisible on the page and use only the external frontends.

**Performance traces.** *About → Export performance trace* (or `>trace` in the palette, or *⬇ Trace* in the companion)
downloads the timeline in the Chrome Trace Event format, which [ui.perfetto.dev](https://ui.perfetto.dev) and the
Chrome DevTools Performance panel open as they are: render batches with every component render nested inside, UI
events and navigations, HTTP requests and JS interop calls on tracks of their own, state changes, errors and findings
as markers, and arrows from each event to the batches and requests it caused. The palette's *Export performance trace
since the profiler baseline* exports only what happened after the last baseline reset.

---

## What it answers

| Question | Where |
|---|---|
| Which components exist, with what parameters, cascading values, state and services? | Components |
| Why did this component render, and how many times / how long? | Components → Rendering, Profiler |
| Did it *need* to render, or did the parent re-apply identical parameters? | Components → Rendering (*forced re-renders*), Diagnostics |
| What did this one click cost, across every component? | Profiler → *Reset*, click, read *since reset* |
| What do all 500 rows of my list cost together? | Profiler → *by type* |
| What happened when I clicked, and what did it cause (renders, HTTP, JS, state)? | Timeline (select an event: *caused by* / *caused*) |
| What changed in my state container and which click did it? | State (history with before/after diffs) |
| Which HTTP requests ran, how long, which failed, what triggered them? | Network |
| Which JS interop calls are slow or excessive, and from which component? | JS Interop |
| Where did this exception come from and what preceded it? | Errors |
| What is registered in DI, with which lifetimes, dependencies and mistakes? | Services |
| Is my circuit blocked, reconnecting, dying? | Circuit (Server), Browser |
| Which handlers does this component bind, and which callbacks does it pass to its children? | Components → *Event handlers & callbacks* (read from its latest render tree) |
| When was it created, initialized, first rendered; is a render queued? | Components → *Lifecycle* |
| Where does each render mode start, and what renders on which runtime? | Components (⚡ on each boundary), Runtime |
| What did prerendering do, and is my data loaded twice? | Runtime → *Prerendering*, Diagnostics (*Data fetched twice*) |
| Which route is this: page, template, route values, query; which routes exist and what do they require? | Routing |
| Who is signed in, with which roles and claims, and since when? | State → *Authentication*, Routing |
| How big are the render batches my circuit sends, and how long does the browser take to apply them? | Network → *WebSockets*, Diagnostics |
| What did my app's SignalR hubs send and receive? | Network → *WebSockets* (by message kind; never payloads) |
| Did a `_framework` file fail to load, or a Content Security Policy block something? | Errors (*resource*, *csp*) |
| How is the thread pool / heap / WebAssembly runtime doing? | Runtime (per platform: Server process, WebAssembly runtime, .NET runtime) |
| What is BevTools itself costing me? | About → overhead |
| How do I share what I found? | About → *Export session (JSON)* / *Copy report (Markdown)* / *Export performance trace*, or `>export`, `>trace` in the palette |

Diagnostics rules turn symptoms into causes with evidence, e.g. *"ProductList rendered 87 times in 2 s: 81 × parent
re-render (Products), 6 × UI event (oninput → FilterBar.OnQuery). Consider ShouldRender…"*.

**Pause** (header button or `>pause`) freezes every buffer so what you are reading is not evicted by a busy
application; errors are still collected, and the resume marker says how many renders were skipped meanwhile.

---

## Honest capability model

BevTools never fakes data. Every piece of information is classified in the About panel and in
[docs/capabilities.md](docs/capabilities.md) as *Available*, *Partially available*, *Requires instrumentation*,
*Requires application integration*, *Requires runtime support* (.NET 10 activities/metrics), *Not technically
reliable* or *Not available*, with the reason.

How the information is obtained, in short:

* **Component instances**: the public `IComponentActivator` hook. No reflection, no source changes.
* **Hierarchy, batches, render timing**: cached reflection accessors over `ComponentBase` / `Renderer` internals
  (public `ComponentState` since .NET 8). Every accessor is optional; when a member is missing the capability is reported
  as unavailable instead of crashing.
* **UI events, navigation, lifecycle and diff timings**: the framework's own `Microsoft.AspNetCore.Components`
  ActivitySource and meters (.NET 10). `AddBitBevToolsServices` registers them through the public
  `ComponentsMetricsServiceCollectionExtensions.AddComponentsTracing/AddComponentsMetrics`, because outside
  server-side rendering the framework does not register them itself — without that, WebAssembly could not answer
  *why did this render?* at all. Turn it off with `options.UseFrameworkInstrumentation = false`. On .NET 8 and 9 there
  is nothing to register, and those capabilities are reported as needing .NET 10.
* **HTTP**: `IHttpMessageHandlerBuilderFilter` (all `IHttpClientFactory` clients). Bodies are never captured.
* **JS interop**: `IJSRuntime` injected into components is wrapped on the first render (calls made earlier, from
  `OnInitialized`, are not attributed); `IJSRuntime` is *not* decorated in DI because the framework casts it to its
  concrete type.
* **Errors**: render wrapper + Blazor's own logger categories + browser `error`/`unhandledrejection`.
* **Circuits**: `CircuitHandler` (lifecycle, connection state, inbound message processing time) +
  `components:reconnect-state-changed` in the browser.
* **Event handlers and callbacks**: the frames of the component's latest render, which the renderer keeps
  (`ComponentState.CurrentRenderTree`, through `BlazorReflection`): attribute frames with event handlers, `@bind`'s
  change handlers, and `EventCallback` parameters on child component frames, each named by the method it runs.
* **Render modes**: what the renderer assigned to each component (`@rendermode`, `[RenderMode*]`), read once, on its
  first render.
* **Routing**: the `RouteView`'s `RouteData` and the `Router`'s assemblies - public parameters of components BevTools
  already tracks; the template that matched from .NET 10 (`RouteData.Template`), inferred before that only when a page
  has a single `@page`, and labeled so.
* **Authentication**: the scope's `AuthenticationStateProvider` and its `AuthenticationStateChanged` event, found by
  name (BevTools does not depend on the Authorization package); asking it for the current state is BevTools' own work,
  so a provider that calls an API meanwhile does not show up as application traffic.
* **Prerendering**: the prerendering panel persists a bounded summary through `PersistentComponentState`, the
  framework's own channel to the interactive render, which takes it back.
* **WebSocket traffic**: the browser bridge counts the page's WebSocket frames by direction and decodes the SignalR
  envelope only (message type and target, the render batch id and size, the ids that mark BevTools' own reports),
  for the JSON and MessagePack/blazorpack hub protocols; arguments are skipped, never kept.
* **Browser failures**: capture-phase `error` events for elements that failed to load, `securitypolicyviolation`,
  and Blazor's `enhancedload` for enhanced navigation.

The design behind it - how render causes are decided, how late diff measurements are matched to their batch, how
BevTools keeps from observing itself, session isolation per circuit - is in [docs/architecture.md](docs/architecture.md),
and its measured cost in [docs/performance.md](docs/performance.md).

---

## Security

An application never has to expose a secret to use BevTools, and production inspection is opt-in and restricted.

**Where it runs.** Development only by default. Outside Development it runs only where the app turns it on:
`BitBevToolsOptions.Enabled`, or opened on demand (Bit.BevTools.Loader, `BitBevTools.Attach`). In a process that serves
many users (Blazor Server, prerendering, static rendering) opening it on demand takes `AllowInMultiUserHost` *and* an
`Authorize` callback that decides who may look, asked on every open; each such attach is logged by the app
(`Bit.BevTools` category, Warning). A bare `BitBevTools.Attach` there needs `Enabled = true` from the app's own code.

**How much it reads: `BitBevToolsOptions.ValueAccess`.** Structure, counts, timings, causes, events, requests and errors
are recorded at every level; what differs is how much of the application's own values is read.

| Level | Parameters, fields, state, claims | Default |
|---|---|---|
| `Full` | Read and shown, expandable member by member, redacted | Development, and every single-user host (WebAssembly, Blazor Hybrid): people only ever see their own data there |
| `Shallow` | Scalars only (numbers, booleans, enums, dates, strings that do not look like credentials); objects show their type and size, never their members or `ToString` | — |
| `None` | Names and types only; header values and browser storage previews are not read either | Outside Development in a process that serves many users, where a circuit's objects can reach other users' data (a singleton cache) |

Forced re-render detection needs `Full` (it compares parameters member by member) and says *not compared* below it.
The prerendering summary travels in the page's HTML, so it is only persisted with `Full`.

**What is redacted, at every level.** Redaction happens where data is captured, so nothing downstream - panel, export,
trace, protocol, companion, extension - can show more:

* Names: members, dictionary keys, headers, query and fragment parameters, storage keys and claim types matching the
  sensitive patterns (`password`, `token`, `authorization`, `cookie`, `connectionstring`, `apikey`, `secret`, …);
  `[BevToolsSensitive]` on fields, properties and auto-properties; `[BevToolsIgnore]` hides members or whole components.
* Shapes, whatever they are called: JSON Web Tokens, bearer tokens, PEM private keys, connection strings and key/value
  text carrying a password or key, and the token formats of AWS, GitHub, Slack, Stripe, Google and OpenAI-style keys.
  Free text - exception and log messages, JavaScript errors, stack traces - keeps its readable part
  (`Server=db;Password=«redacted»`).
* Pairs: the value of a `KeyValuePair`, claim or cookie whose name is sensitive (`Authorization: …`, an `access_token`
  claim), including in the pair's `ToString`.
* Never read at all: `IConfiguration` and its providers and sources, `IOptions<T>`/`IOptionsMonitor<T>`/
  `IOptionsSnapshot<T>`, credentials and key material (`NetworkCredential`, `SecureString`, certificates, keys), request
  and response bodies, DI instance values, WebSocket payloads (only sizes and SignalR message kinds), and browser
  storage values that look sensitive (redacted in the browser, before they reach .NET).

Circuit ids are truncated; SignalR connection ids in socket URLs are redacted in the browser. Nothing is sent anywhere:
all data stays in the session's memory, bounded by ring buffers (`MaxEvents`, `MaxRendersPerComponent`, …). Exports,
traces and protocol answers contain exactly what the panels show, with the same redaction; exports omit browser
storage previews.

**External frontends.** The page hub (`window.__BIT_BEVTOOLS__`) exists only while an enabled runtime has attached to
the page, and gives page script nothing it could not already reach (the panel's own JS interop reference). The
companion window reads it from the page that opened it, same origin only. The browser extension asks for no
permission, has no content script, and reaches the page only through DevTools' `inspectedWindow.eval` while DevTools is
open on that tab; request data it sends is always a string literal, never code.

---

## Extending

```csharp
public sealed class MyLibraryBevTools : IBevToolsExtension
{
    public string Name => "MyLibrary";
    public void Configure(IBevToolsExtensionBuilder b) => b
        .AddPanel(new BevToolsPanelDescriptor("mylib", "MyLibrary", typeof(MyPanel)))
        .AddStateProvider(sp => new StoreAdapter(sp.GetRequiredService<MyStore>()))
        .AddDiagnosticRule(new MyRule())
        .AddCommand(new MyCommand())
        .AddValueFormatter(typeof(Money), v => ((Money)v).ToString("C"))
        .HideComponentsInNamespace("MyLibrary.Internal");
}

services.AddBitBevToolsServices(o => o.Extensions.Add(new MyLibraryBevTools()));
```

A library that wants to expose itself to BevTools references `Bit.BevTools`, which carries the extension contracts:

- `IBevToolsExtension` / `IBevToolsExtensionBuilder` - contribute panels, state providers, diagnostic rules, commands
  and value formatters, or hide internal components.
- `IBevStateProvider` - expose a state container to the State panel, with snapshots, change history and diffs.
- `IBevDiagnosticRule` - turn symptoms into findings with evidence.
- `IBevToolsCommand` - add commands to the command palette.
- `IBevToolsTimeline` / `BevToolsEvent` - write your own events into the activity timeline.
- `[BevToolsSensitive]` / `[BevToolsIgnore]` - redact members, or hide members and whole components.

Application code can also emit timeline events through `IBevToolsTimeline`, expose state with
`IBevStateProvider` (or `services.AddBevToolsStateProvider<T>()`), attribute background work with
`session.UseAmbient()`, pause and resume capture (`session.PauseCapture()` / `ResumeCapture()`) and export everything
with `BevSessionExporter` (session JSON, Markdown) or `BevTraceExporter` (Chrome trace). Commands get `CopyTextAsync` /
`DownloadTextAsync` on their context to hand text to the browser. The `Bit.BevTools.Server` package is itself an
extension (Circuit panel, circuit rules, command).

A tool of your own can read a page the way the companion does: `window.__BIT_BEVTOOLS__.targets()` lists the runtimes,
and `request(targetId, method, params)` speaks `BevToolsProtocol` (`hello`, `snapshot`, `inspect`, `expand`, `command`,
`export`; documented on the class). A snapshot is the `bit-bevtools/1` export, optionally limited to sections and to
the timeline events after a given id.

---

## Project layout

```
Bit.BevTools                  instrumentation, session stores, diagnostics, inspection, export, BevTools UI (Razor
                              class library), the page hub (Scripts/bit-bevtools.ts -> wwwroot/bit-bevtools.js) and the
                              companion (Scripts/bit-bevtools-companion.ts, wwwroot/companion.html)
  Abstractions/               public integration API (events, state providers, rules, commands, panels, capabilities)
  Protocol/                   BevToolsProtocol: what the companion window and the browser extension ask
  Routing/ Auth/ WebApp/      route data and route table, authentication state, prerendering handoff
  Runtime/                    per-platform runtime facts (Server process, WebAssembly runtime, .NET runtime)
Bit.BevTools.Server           circuit handler, circuit registry, Circuit panel (reference extension)
Bit.BevTools.Loader           trimmable startup half for opening BevTools on demand: hooks, BevToolsLoader, the panel host
Bit.BevTools.Extension        the browser extension (Manifest V3, Chromium and Firefox): the companion in a DevTools panel
Demo                          Blazor Web App (Interactive Auto, Server + Client) with a "Problems to diagnose" index
Tests/Bit.BevTools.Tests      MSTest + bUnit on net8.0/net9.0/net10.0: instrumentation, tree, rendering, timeline,
                              state, network, interop, diagnostics, DI graph, concurrency, overhead, memory bounds,
                              scale, panel interaction, framework-diagnostics attribution, extensibility, forced-render
                              detection, capture control, profiler baseline/grouping, export, value safety, routing,
                              authentication, wiring, prerendering, traces, protocol, traffic
Tests/bit-bevtools-js         vitest suite: the bridge (isolation, pre-.NET redaction, downloads, shortcuts), the hub
                              (several runtimes, traffic decoding, browser failures), the companion (DOM, happy-dom)
                              and the extension source
Tests/Bit.BevTools.Benchmarks reproducible overhead measurement (dotnet run -c Release)
docs/                         capability matrix, architecture, performance
```

Run the demo with `dotnet run --project src/Utilities/BevTools/Demo/Server`, then open `/problems`. The tests:

```bash
cd src/Utilities/BevTools/Tests/Bit.BevTools.Tests && dotnet test     # run from inside src, so global.json's test runner applies
cd src/Utilities/BevTools/Tests/bit-bevtools-js && npm ci && npm test # after a build of Bit.BevTools, which produces the bundles
cd src/Utilities/BevTools/Bit.BevTools.Extension && node build.mjs    # the unpacked extension, in dist/
```

See [CLAUDE.md](CLAUDE.md) for the principles every change has to respect.

---

## Status

The component tree and inspector (parameters, cascading values, state, services, event handlers and callbacks,
lifecycle, render modes), render tracking and profiler, event timeline, error inspection (resource and CSP failures
included), state inspection (authentication included), routing, network with WebSocket/SignalR traffic, JS interop,
search/commands, DI inspection, circuit, runtime and prerendering diagnostics, the rules engine, session and trace
export, the companion window and the browser extension. See [docs/architecture.md](docs/architecture.md) for the
design and the list of deliberate non-goals (component ↔ DOM highlighting, request bodies, SignalR payloads,
time-travel state restoration).
