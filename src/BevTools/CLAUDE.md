# Bit.BevTools

In-app developer tools for Blazor (WebAssembly, Server, Interactive Auto, static SSR; net8.0/9.0/10.0): component
tree, render profiler, timeline, state, network, JS interop, errors, DI inspector, circuit diagnostics and rules.
`README.md` in this folder is the reference guide; `docs/` holds the capability matrix, the architecture and the
measured overhead - keep all of them accurate when behaviour changes.

## Layout

| Path | What it is |
| --- | --- |
| `Bit.BevTools.Abstractions/` | The integration contract (`IBevToolsExtension`, `IBevStateProvider`, `IBevDiagnosticRule`, `IBevToolsCommand`, `IBevToolsTimeline`, `BevToolsEvent`, the attributes). No Blazor dependency: additions only, never removals or signature changes |
| `Bit.BevTools/` | Instrumentation, session stores, inspection, diagnostics, export and the UI (`BevToolsPanel` and the panels), plus `BitBevTools.AddBitBevToolsServices` |
| `Bit.BevTools/Scripts/bit-bevtools.ts` | The browser bridge, compiled by MSBuild to `wwwroot/bit-bevtools.js` (generated, git-ignored; minified in Release) |
| `Bit.BevTools.Server/` | Circuit instrumentation and the Circuit panel, itself written as a BevTools extension |
| `Demo/` | Blazor Web App (Interactive Auto, `Server` + `Client`) whose `/problems` page lists real, deliberately planted problems to diagnose |
| `Tests/Bit.BevTools.Tests` | MSTest + bUnit on net8.0, net9.0 and net10.0 |
| `Tests/bit-bevtools-js` | vitest suite for the built `wwwroot/bit-bevtools.js` |
| `Tests/Bit.BevTools.Benchmarks` | Overhead harness: the same workload through a real renderer with and without BevTools |

## Principles that every change must respect

1. **Never fake data.** If something cannot be observed reliably, show that it cannot (see `CapabilityReport`), do
   not approximate it. Every number in the UI has a documented source. That includes the runtime: what needs .NET 10
   says "Requires .NET 10" on .NET 8 and 9 (`CapabilityReport.FrameworkInstrumentationExists`), never "not observed yet".
2. **Never observe yourself.** BevTools' own components, events and interop calls must not appear as application
   activity. `PanelInteractionTests.BevTools_own_ui_never_appears_as_application_activity` guards this. Ownership is
   decided by assembly and by namespace: every BevTools component lives directly in `Bit.BevTools` (the server
   package's included), and only there - `Bit.BevTools.Demo.*`, `Bit.BevTools.Tests` or an application's
   `Bit.BevTools.Anything` is application code. Never match the product namespace as a prefix.
3. **Never leak secrets.** Redaction happens at capture time (`BevRedactor`, `[BevToolsSensitive]`, browser-side storage
   redaction). Anything that displays or exports data must go through the same paths - exports may never show more
   than the panels do.
4. **Bounded and cheap.** Every buffer is a ring buffer; the per-render hot path (`RenderTracker.BeginRender` /
   `EndRender`) formats no strings and allocates one sample plus one event. Run the benchmark before and after touching
   it: `dotnet run -c Release --project Tests/Bit.BevTools.Benchmarks`.
5. **Optional reflection.** Every accessor over Blazor internals lives in `BlazorReflection`, is null when the member
   is missing, and degrades to a reported capability gap rather than an exception. The assembly is deliberately not
   trim- or AOT-safe (`IsTrimmable`/`IsAotCompatible` are off); it is a development tool.

## Coding style

Follow the `.editorconfig` at the root of `src` (every new `.cs`, `.razor`, `.ts`, `.md`, `.csproj` and `.slnx` file
starts with a BOM). Comments explain *why* a thing is shaped the way it is, not what the next line does.

- **One namespace**: everything in the three packages is in `Bit.BevTools`, whatever folder it sits in; razor files
  declare `@namespace Bit.BevTools`. Tests use `Bit.BevTools.Tests`, the demo `Bit.BevTools.Demo.Client` / `.Server`.
- **Prefixed public types**: `BevTools*` for the types that are the tool itself (`BevToolsSession`, `BevToolsPanel`,
  `IBevToolsExtension`), `Bev*` for the rest (`BevComponentRecord`, `BevDiagnostic`, `BevFmt`), and razor components
  too, since the Razor SDK makes them public. A consumer importing `Bit.BevTools` must never get a bare common name.
- **Multi-targeting**: APIs newer than .NET 8 go behind `NET9_0_OR_GREATER` (`RendererInfo`, `AssignedRenderMode`) or
  `NET10_0_OR_GREATER` (`AddComponentsTracing`/`AddComponentsMetrics`), with a fallback that is reported, not hidden.
  C# 14 binds `array.Reverse()` to the void span overload below .NET 10; `Internal/ArrayReverseExtensions.cs` restores
  the LINQ meaning there, so keep calling it on the store snapshots as usual.

## Tests

Every behaviour has a test at the level where it can fail for real:

- unit tests for stores, inspection, redaction, rules and matching;
- bUnit tests through a real renderer for instrumentation (hierarchy, causes, batches, diffs, disposal, JS wrapping);
- bUnit tests that click the BevTools UI itself (`PanelInteractionTests`, `ProfilerTests`); set the renderer with
  `this.UseRenderer("Server")` (a no-op on .NET 8, where the panel infers it);
- vitest tests for the browser bridge (`Tests/bit-bevtools-js`), against the built bundle.

A test that asserts on the exact activity, meter and tag names of `Microsoft.AspNetCore.Components` is deliberate: a
framework rename should break a test, not silently degrade every render cause to *StateHasChanged*. Those tests exist
only on .NET 10 (`#if NET10_0_OR_GREATER`), next to their counterpart asserting what earlier runtimes report. Tests
share process-wide state (the activity/meter listeners, the session resolver), so the assembly runs them sequentially,
and a test that needs that state in a particular shape sets it up itself (`ActivityObserver.MarkBatchMetricsObserved`)
instead of relying on whichever test ran before it.

| Suite | Run |
| --- | --- |
| .NET (all target frameworks) | `cd src/BevTools/Tests/Bit.BevTools.Tests && dotnet test` - from inside `src`, so `global.json`'s Microsoft.Testing.Platform runner applies |
| Browser bridge | `cd src/BevTools/Tests/bit-bevtools-js && npm ci && npm test`, after building `Bit.BevTools` |
| Demo | `dotnet run --project src/BevTools/Demo/Server`, then open `/problems` |

## Reporting a problem

Open BevTools in the affected app, reproduce, then use *About → Copy report (Markdown)* or *Export session (JSON)* and
attach the result. Both contain only what the panels show, with the same redaction, but read them before posting.
