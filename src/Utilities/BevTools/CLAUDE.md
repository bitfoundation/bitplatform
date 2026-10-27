# Bit.BevTools

Developer tools for Blazor (WebAssembly, Server, Interactive Auto, static SSR; net10.0/11.0): component tree with
event wiring, lifecycle and render modes, render profiler, timeline, state (authentication included), routing, network
with WebSocket/SignalR traffic, JS interop, errors, DI inspector, circuit, runtime and prerendering diagnostics, rules,
session and trace export. Three frontends read one protocol: the in-app panel, the companion window and the browser
extension.
`README.md` in this folder is the reference guide; `docs/` holds the capability matrix, the architecture and the
measured overhead - keep all of them accurate when behaviour changes.

## Layout

| Path | What it is |
| --- | --- |
| `Bit.BevTools/` | Instrumentation, session stores, inspection, diagnostics, export and the UI (`BevToolsPanel` and the panels), plus `BitBevTools.AddBitBevToolsServices` |
| `Bit.BevTools/Abstractions/` | The integration contract (`IBevToolsExtension`, `IBevStateProvider`, `IBevDiagnosticRule`, `IBevToolsCommand`, `IBevToolsTimeline`, `BevToolsEvent`, the attributes). Additions only, never removals or signature changes. It uses no Blazor types, so it can move back into a package of its own (with `[TypeForwardedTo]`) if a Blazor-free consumer appears; keep it that way |
| `Bit.BevTools/Scripts/bit-bevtools.ts` | The page hub: one target per BevTools runtime on the page, page-level facts (errors, failed resources, CSP, WebSocket traffic, enhanced navigation), and `window.__BIT_BEVTOOLS__` for external frontends. Compiled by MSBuild to `wwwroot/bit-bevtools.js` (generated, git-ignored; minified in Release) |
| `Bit.BevTools/Scripts/bit-bevtools-companion.ts` | The companion frontend (companion window and browser extension), compiled to `wwwroot/bit-bevtools-companion.js`; `wwwroot/companion.html` and its stylesheet are sources. Renders application strings as text nodes only |
| `Bit.BevTools/Protocol/` | `BevToolsProtocol`: what external frontends ask a runtime, answered from the same stores and through the same inspector as the panels |
| `Bit.BevTools/Routing/`, `Auth/`, `WebApp/`, `Runtime/` | Route data and route table, the authentication state provider, the prerendering handoff, per-platform runtime facts |
| `Bit.BevTools.Server/` | Circuit instrumentation and the Circuit panel, itself written as a BevTools extension |
| `Bit.BevTools.Loader/` | The half an app references at startup when it opens BevTools on demand or lazy loads it: startup hooks (activator, HTTP, framework diagnostics, service collection), `BevToolsLoader`, `BevToolsLoaderPanel`, `BevToolsHttpHookHandler`. It ships in every startup download, AOT builds included, so it is trimmable, AOT compatible and reflection free, and it never references `Bit.BevTools`: `Bit.BevTools` references it, and implements its internal contract (`Internal/BevToolsLoaderContract.cs`) through `[assembly: BevToolsEntryPoint]` |
| `Bit.BevTools.Cli/` | The `bevtools` .NET tool for CI: `check` (`BevSessionCheck`) and `compare` (`BevProfileComparison`) over export files; a thin shell, the logic lives in `Bit.BevTools`. Reads files only, never a running app |
| `Bit.BevTools.Extension/` | Manifest V3 browser extension (Chromium, Firefox): the companion in a DevTools panel over `inspectedWindow.eval`; no permissions, no content script. `node build.mjs` assembles `dist/` |
| `Demo/` | Blazor Web App (Interactive Auto, `Server` + `Client`) whose `/problems` page lists real, deliberately planted problems to diagnose |
| `Samples/` | One small app per host, all rendering the pages of `Samples/Core` with `<BevToolsPanel />` in its `Routes.razor`: `Server`, `Wasm`, `Auto` (each with its `.Client`) and `MAUI` (Blazor Hybrid). Each host's `Program.cs` (`MauiProgram.cs`) owns its BevTools registration, so the setups can be compared; they show the recommended one, not the demo's always-on one |
| `Tests/Bit.BevTools.Tests` | MSTest + bUnit on net10.0 and net11.0 |
| `Tests/bit-bevtools-js` | vitest suite for the built bundles: the bridge and hub (node), the companion (happy-dom) and the extension source |
| `Tests/Bit.BevTools.Benchmarks` | Overhead harness: the same workload through a real renderer with and without BevTools |

## Principles that every change must respect

1. **Never fake data.** If something cannot be observed reliably, show that it cannot (see `CapabilityReport`), do
   not approximate it. Every number in the UI has a documented source.
2. **Never observe yourself.** BevTools' own components, events, interop calls and WebSocket
   reports must not appear as application activity, nor work BevTools does through application code
   (`SessionResolver.BeginBevToolsWork`, e.g. asking the `AuthenticationStateProvider`). `PanelInteractionTests.BevTools_own_ui_never_appears_as_application_activity` guards this. Ownership is
   decided by assembly and by namespace: every BevTools component lives directly in `Bit.BevTools` (the server
   package's included), and only there - `Bit.BevTools.Demo.*`, `Bit.BevTools.Tests` or an application's
   `Bit.BevTools.Anything` is application code. Never match the product namespace as a prefix. The one other owner is a
   panel an extension contributes (`BevToolsPanelDescriptor`), with what it renders: the framework's activities and
   meters name only a component type, so `ComponentRegistry.IsBevToolsOnlyType` tells their measurements apart
   (`ExtensibilityTests.An_extension_panel_is_never_recorded_as_application_activity`).
3. **Never leak secrets.** Redaction happens at capture time (`BevRedactor`: names, `[BevToolsSensitive]`, credential
   shapes, sensitive pairs; browser-side storage and URL redaction), and `BevObjectInspector` never walks configuration
   or key material. Every application value reaches a panel, export, trace or protocol answer through the inspector,
   which applies `ValueAccess` (`None` by default outside Development in a server process). Exports, traces and the
   protocol may never show more than the panels do; a new frontend reads through `BevToolsProtocol`, never around it.
   Production inspection is opt-in: outside Development in a server process it takes `AllowInMultiUserHost` and an
   `Authorize` callback (`BevToolsAttachPolicy`).
4. **Bounded and cheap.** Every buffer is a ring buffer; the per-render hot path (`RenderTracker.BeginRender` /
   `EndRender`) formats no strings and allocates one sample plus one event. Run the benchmark before and after touching
   it: `dotnet run -c Release --project Tests/Bit.BevTools.Benchmarks`.
5. **Optional reflection.** Every accessor over Blazor internals lives in `BlazorReflection`, is null when the member
   is missing, and degrades to a reported capability gap rather than an exception. The assembly is deliberately not
   trim- or AOT-safe (`IsTrimmable`/`IsAotCompatible` are off); it is a development tool, and keeps itself whole in an
   app that trims and uses it (the embedded `ILLink.Descriptors.xml`; the loader's `DynamicDependency` reaches it when
   only the loader names it), while an app that never uses it ships none of it. `Bit.BevTools.Loader` is the opposite:
   no reflection over Blazor, no trim or AOT warnings (build it with `-p:RunTrimAnalyzers=true`).

## Coding style

Follow the `.editorconfig` at the root of `src` (every new `.cs`, `.razor`, `.ts`, `.md`, `.csproj` and `.slnx` file
starts with a BOM). Comments explain *why* a thing is shaped the way it is, not what the next line does.

- **One namespace**: everything in all four packages (the CLI included) is in `Bit.BevTools`, whatever folder it sits in; razor files
  declare `@namespace Bit.BevTools`. Tests use `Bit.BevTools.Tests`, the demo `Bit.BevTools.Demo.Client` / `.Server`.
- **Prefixed public types**: `BevTools*` for the types that are the tool itself (`BevToolsSession`, `BevToolsPanel`,
  `IBevToolsExtension`), `Bev*` for the rest (`BevComponentRecord`, `BevDiagnostic`, `BevFmt`), and razor components
  too, since the Razor SDK makes them public. A consumer importing `Bit.BevTools` must never get a bare common name.
- **Multi-targeting**: APIs newer than .NET 10 go behind `NET11_0_OR_GREATER`, with a fallback that is reported, not hidden.

## Tests

Every behaviour has a test at the level where it can fail for real:

- unit tests for stores, inspection, redaction, rules and matching;
- bUnit tests through a real renderer for instrumentation (hierarchy, causes, batches, diffs, disposal, JS wrapping);
- bUnit tests that click the BevTools UI itself (`PanelInteractionTests`, `ProfilerTests`); set the renderer with
  `this.UseRenderer("Server")`;
- vitest tests for the browser bridge, the hub, the companion and the extension source (`Tests/bit-bevtools-js`),
  against the built bundles.

A test that asserts on the exact activity, meter and tag names of `Microsoft.AspNetCore.Components` is deliberate: a
framework rename should break a test, not silently degrade every render cause to *StateHasChanged*. Tests
share process-wide state (the activity/meter listeners, the session resolver), so the assembly runs them sequentially,
and a test that needs that state in a particular shape sets it up itself (`ActivityObserver.MarkBatchMetricsObserved`)
instead of relying on whichever test ran before it.

| Suite | Run |
| --- | --- |
| .NET (all target frameworks) | `cd src/Utilities/BevTools/Tests/Bit.BevTools.Tests && dotnet test` - from inside `src`, so `global.json`'s Microsoft.Testing.Platform runner applies |
| Browser bridge, hub, companion, extension | `cd src/Utilities/BevTools/Tests/bit-bevtools-js && npm ci && npm test`, after building `Bit.BevTools` |
| Browser extension | `cd src/Utilities/BevTools/Bit.BevTools.Extension && node build.mjs`, then load `dist/` unpacked |
| Demo | `dotnet run --project src/Utilities/BevTools/Demo/Server`, then open `/problems` |
| Samples | `dotnet run --project src/Utilities/BevTools/Samples/Server` (or `Samples/Wasm/Bit.BevTools.Samples.Wasm`, `Samples/Auto/Bit.BevTools.Samples.Auto`); `Samples/MAUI` builds for every platform the machine has workloads for |

## Reporting a problem

Open BevTools in the affected app, reproduce, then use *About → Copy report (Markdown)* or *Export session (JSON)* and
attach the result. Both contain only what the panels show, with the same redaction, but read them before posting.
