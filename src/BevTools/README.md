# Bit.BevTools

An in-app inspection and diagnostics tool for Blazor applications: a live component tree, a render profiler that
explains *why* a component rendered, a unified activity timeline, state inspection with diffs, network and JS interop
tracking, an error center, a DI inspector, circuit diagnostics for Blazor Server, and an evidence-based diagnostics
engine. Built as developer infrastructure with a public extension API, minimal runtime overhead and no changes to
application source.

> Targets **.NET 8, 9 and 10** · Blazor WebAssembly, Blazor Server, Interactive Auto and static SSR. The richest
> answers (UI-event render causes, handler durations, render-diff and lifecycle timings) come from the framework's own
> diagnostics, which arrived in **.NET 10**; on .NET 8 and 9 those rows say so instead of showing nothing.

---

## Table of Contents

- [Installation](#installation)
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
| What is BevTools itself costing me? | About → overhead |
| How do I share what I found? | About → *Export session (JSON)* / *Copy report (Markdown)*, or `>export` in the palette |

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

The design behind it - how render causes are decided, how late diff measurements are matched to their batch, how
BevTools keeps from observing itself, session isolation per circuit - is in [docs/architecture.md](docs/architecture.md),
and its measured cost in [docs/performance.md](docs/performance.md).

---

## Security

Development-only by default. Member, header, query and storage names matching sensitive patterns (`password`, `token`,
`authorization`, `cookie`, `connectionstring`, …) are redacted; `[BevToolsSensitive]` redacts explicitly;
`[BevToolsIgnore]` hides members or whole components. Request/response bodies and DI instance values are never read.
Circuit ids are truncated. Nothing is sent anywhere: all data stays in the session's memory and is bounded by ring
buffers (`MaxEvents`, `MaxRendersPerComponent`, …). Exports contain exactly what the panels show, with the same
redaction, and omit browser storage previews.

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

A library that only wants to expose itself to BevTools references `Bit.BevTools.Abstractions`, which has no Blazor
dependency. Application code can also emit timeline events through `IBevToolsTimeline`, expose state with
`IBevStateProvider` (or `services.AddBevToolsStateProvider<T>()`), attribute background work with
`session.UseAmbient()`, pause and resume capture (`session.PauseCapture()` / `ResumeCapture()`) and export everything
with `BevSessionExporter`. Commands get `CopyTextAsync` / `DownloadTextAsync` on their context to hand text to the
browser. The `Bit.BevTools.Server` package is itself an extension (Circuit panel, circuit rules, command).

---

## Project layout

```
Bit.BevTools.Abstractions     public integration API (events, state providers, rules, commands, panels, capabilities)
Bit.BevTools                  instrumentation, session stores, diagnostics, inspection, export, BevTools UI (Razor
                              class library) and the browser bridge (Scripts/bit-bevtools.ts -> wwwroot/bit-bevtools.js)
Bit.BevTools.Server           circuit handler, circuit registry, Circuit panel (reference extension)
Demo                          Blazor Web App (Interactive Auto, Server + Client) with a "Problems to diagnose" index
Tests/Bit.BevTools.Tests      MSTest + bUnit on net8.0/net9.0/net10.0: instrumentation, tree, rendering, timeline,
                              state, network, interop, diagnostics, DI graph, concurrency, overhead, memory bounds,
                              scale, panel interaction, framework-diagnostics attribution, extensibility, forced-render
                              detection, capture control, profiler baseline/grouping, export
Tests/bit-bevtools-js         vitest suite for the browser bridge: isolation, pre-.NET redaction, downloads, shortcuts
Tests/Bit.BevTools.Benchmarks reproducible overhead measurement (dotnet run -c Release)
docs/                         capability matrix, architecture, performance
```

Run the demo with `dotnet run --project src/BevTools/Demo/Server`, then open `/problems`. The tests:

```bash
cd src/BevTools/Tests/Bit.BevTools.Tests && dotnet test     # run from inside src, so global.json's test runner applies
cd src/BevTools/Tests/bit-bevtools-js && npm ci && npm test # after a build of Bit.BevTools, which produces bit-bevtools.js
```

See [CLAUDE.md](CLAUDE.md) for the principles every change has to respect.

---

## Status

The component tree and inspector, parameters, render tracking and profiler, event timeline, error inspection, state
inspection, network, JS interop, search/commands, DI inspection, circuit diagnostics and the rules engine. See
[docs/architecture.md](docs/architecture.md) for the design and the list of deliberate non-goals (component ↔ DOM
highlighting, request bodies, SignalR payloads).
