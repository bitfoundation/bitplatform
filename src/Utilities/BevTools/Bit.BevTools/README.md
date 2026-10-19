# Bit.BevTools

In-app developer tools for Blazor: a live component tree, a render profiler that explains *why* a component rendered,
a unified activity timeline, state inspection with diffs, network and JS interop tracking, an error center, a DI
inspector and an evidence-based diagnostics engine - with no changes to application source.

## Quick start

```csharp
// Program.cs
using Bit.BevTools;

builder.Services.AddBitBevToolsServices();
```

```razor
@* MainLayout.razor (or any interactive component) *@
@using Bit.BevTools

<BevToolsPanel />
```

BevTools turns itself on only when the host environment is `Development` (override with `options.Enabled`; in a
WebAssembly client pass `builder.HostEnvironment.IsDevelopment()`), renders nothing during prerendering, and stays
dormant in production builds. Press **Ctrl+Shift+D** or click the badge to open it; **Ctrl+K** opens search and the
command palette. The browser bridge and stylesheet ship as static web assets and are loaded by the panel itself.

For Blazor Server and Interactive Auto apps add `Bit.BevTools.Server` and call `AddBitBevToolsServerServices()` in the
server project instead, which adds circuit diagnostics. In Blazor WebAssembly add `<MetricsSupport>true</MetricsSupport>`
to the project to get render-diff and lifecycle timings.

Targets .NET 8, 9 and 10; the UI-event render causes and the framework timings need .NET 10, and the About panel says
which capabilities this app has and why. Full guide: https://github.com/bitfoundation/bitplatform/tree/develop/src/BevTools
