# Capability matrix

What BevTools can observe, how, and where it stops. The same matrix is rendered live in the About panel with
the runtime-verified status of each row (for example, whether HandleEvent activities have actually been observed on
this host). Nothing in the UI is approximated without saying so.

Legend: **Available** — works out of the box · **Partial** — works with stated gaps · **Instrumentation** — obtained by
BevTools instrumenting framework internals (dev-only reflection or wrappers) · **Integration** — the application or
library must call an API · **Runtime** — depends on framework support (.NET 10 activities/metrics) · **Unreliable** —
deliberately not shown as fact · **N/A** — not obtainable.

BevTools ships for .NET 8, 9 and 10. The **Runtime** rows need .NET 10: on .NET 8 and 9 the framework emits none of
those activities and meters, so the About panel reports them as *Requires runtime support* and says "Requires .NET 10"
rather than "not observed yet" (renders inside events are then labeled *StateHasChanged*). The render mode name needs
.NET 9 (`ComponentBase.AssignedRenderMode`); on .NET 8 the renderer is inferred from the JS runtime the host provides.

## Components

| Information | Status | How / why |
|---|---|---|
| Component instances (all render modes) | Available | Public `IComponentActivator` hook wraps any existing activator; every instance the renderer creates is registered. |
| Parent/child hierarchy | Instrumentation | `Renderer._componentStateByComponent` → public `ComponentState.LogicalParentComponentState` (.NET 8+). Cached expression-compiled accessor; reported unavailable if the member disappears. |
| Parameters, cascading values, `[SupplyParameterFrom*]` | Available | Read on demand from attributed properties; lazy inspection; redaction by name/attribute. |
| Component state (private fields) | Partial | Read on demand; diffed per render only for the selected component (or all with `CaptureComponentStateOnRender`). Mutations that do not lead to a render are not observed. |
| Injected services with lifetime | Available | `[Inject]` properties + captured DI registrations. |
| Source location | Partial | Inferred `TypeName.razor`; exact paths only inside exception stack traces (needs PDBs). |
| Disposal | Partial | Detected when the renderer no longer knows the instance (checked on refresh), so timing is approximate. |
| Component ↔ DOM element highlighting | N/A | Blazor exposes no component-to-DOM map to .NET; not implemented rather than guessed. |

## Rendering

| Information | Status | How / why |
|---|---|---|
| Render count, `BuildRenderTree` duration per instance | Instrumentation | The private `_renderFragment` of each `ComponentBase` is replaced with a measuring wrapper at creation. Components implementing `IComponent` directly are listed but not measured (shown as *not measured*). |
| Render batches (one DOM diff) | Instrumentation | Batch boundary = `RenderBatchBuilder.UpdatedComponentDiffs.Count == 0` at render start. Fallback: idle-time heuristic (flagged *Unreliable*). |
| Batch diff duration and DOM edit count | Runtime | `aspnetcore.components.render_diff.duration/size` meters (.NET 10). On Blazor Server the framework records them when the browser acknowledges the render, so BevTools matches each measurement back to the batch that produced it. **Blazor WebAssembly compiles `System.Diagnostics.Metrics` out by default**: add `<MetricsSupport>true</MetricsSupport>` to the WebAssembly project, otherwise the About panel reports this row as *Not available* and says why. |
| Cause: parent re-render | Available* | An ancestor rendered earlier in the same batch (renderer re-applied parameters). *Needs hierarchy + batches. |
| Cause: UI event (with attribute, component, method) | Runtime | `Microsoft.AspNetCore.Components.HandleEvent` activity; BevTools adds an `ActivityListener` **and registers the framework's own `ComponentsActivitySource`** (`AddComponentsTracing`), which server-side rendering registers but WebAssembly and custom hosts do not. Interactions with the BevTools panel itself are excluded. Before the first activity is seen the About panel says so. |
| Cause: navigation | Partial | `NavigationManager.LocationChanged` always; `Navigate` activity when emitted. |
| Cause: explicit `StateHasChanged` / async continuation | Partial | Everything not attributable above. The caller is not identified (no stack walk by design). |
| Forced re-render (parent re-applied identical parameters) | Partial | For the component selected in the inspector (or all, with `CaptureComponentStateOnRender`), parameter and cascading values are flattened two levels deep (≤ 25 collection items, one time budget per render) before each render and diffed against the previous captured render. A parent-caused render with no observable change is marked *forced* and counted. Delegates compare by target method, so a recreated lambda with the same body counts as unchanged. Render fragments compare by instance, and one that reads component state as it runs never counts as unchanged, so new child content is never called forced. `[BevToolsSensitive]` parameters compare as redacted. Not decided for the first captured render (nothing to compare against). |
| `SetParametersAsync` cost (OnInitialized/OnParametersSet) | Runtime | `aspnetcore.components.update_parameters.duration`, per component type. Same WebAssembly caveat as the diff meters. |
| Event handler duration | Runtime | Activity duration; also `handle_event.duration` meter. |

## Events and timeline

| Information | Status |
|---|---|
| Unified timeline (renders, batches, events, navigation, state, HTTP, JS, errors, circuit, browser, diagnostics, custom) | Available |
| Cause/effect links (event → renders/HTTP/JS/state; parent render → child render) | Available where the cause is known (see above) |
| Bounded history with configurable size | Available (`MaxEvents`, ring buffer, oldest dropped) |
| Custom events from libraries | Integration (`IBevToolsTimeline`) |

## State

| Information | Status | How / why |
|---|---|---|
| Registered state containers, snapshots, change history, before/after diffs | Integration | Implement `IBevStateProvider` or register an adapter (`AddBevToolsStateProvider`). Diffs compare bounded flattened snapshots (depth 3, 25 items per collection). |
| Cascading values | Available | From `[CascadingParameter]` properties. |
| Time travel (restoring state) | N/A | Deliberately not implemented: restoring arbitrary object graphs is unsafe. History browsing of snapshots is available. |

## Network

| Information | Status | How / why |
|---|---|---|
| Requests via `IHttpClientFactory`: method, URL, status, duration, sizes, headers | Available | `IHttpMessageHandlerBuilderFilter` adds a `DelegatingHandler` to every pipeline. Sensitive headers/query values redacted. |
| Hand-built `HttpClient` | Integration | Add `BevToolsHttpMessageHandler` manually. |
| Bodies | N/A | Never captured. |
| Originating UI event | Partial | Linked when a HandleEvent activity is executing; lifecycle/timer requests show no trigger. |
| Browser-level waterfall, WebSocket frames | N/A | Use the browser's DevTools. |

## JS interop

| Information | Status | How / why |
|---|---|---|
| .NET → JS from components (identifier, duration, args types, result, errors, component) | Instrumentation | `[Inject] IJSRuntime` is replaced with a tracking wrapper on the first render — the earliest point after the framework performs property injection. Calls made from `OnInitialized`/`OnInitializedAsync` therefore run before the wrapper exists and are not attributed. Module references obtained through a wrapped runtime are wrapped too (`TrackJsModuleReferences`, on by default); BevTools unwraps them again when they are passed back as arguments through a tracked runtime, but cannot do so for references nested inside other arguments — turn the option off if a module reference must round-trip through code BevTools does not see. |
| .NET → JS from services | Integration | `jsRuntime.WithBevToolsTracking(session)`. Global decoration is unsafe (framework casts `IJSRuntime` to `RemoteJSRuntime`/`WebAssemblyJSRuntime`). |
| JS → .NET | Instrumentation | `DotNet.invokeMethodAsync/invokeMethod` wrapped by the browser bridge; reported in 500 ms batches. |

## Errors

| Information | Status |
|---|---|
| Render exceptions with component | Available (render wrapper) |
| Event handler / circuit / ErrorBoundary / JS interop failures | Available (Blazor's own logger categories at Warning+) |
| JS errors, unhandled promise rejections | Instrumentation (browser bridge) |
| HTTP failures | Available |
| Preceding UI event and last activity | Available |
| Source location | Partial (from stack trace when PDBs are present) |

## DI

| Information | Status | How / why |
|---|---|---|
| Registrations, lifetimes, implementation, keyed | Available | `IServiceCollection` captured at `AddBitBevToolsServices` time (enumerated lazily, so later registrations appear). |
| Constructor dependencies, consumers, cycles, captive dependencies, large graphs | Available | Static analysis of implementation constructors. Factory/instance registrations have no visible dependencies. |
| Runtime resolutions | N/A | The container has no resolution events. |
| Instance values | N/A | Never read (configuration/secrets). |

## Circuit (Blazor Server)

| Information | Status | How / why |
|---|---|---|
| Lifecycle, connection up/down, disconnect count, duration | Instrumentation | `CircuitHandler` registered by `AddBitBevToolsServerServices`. |
| Inbound message count and processing time (slow messages flagged) | Instrumentation | `CircuitHandler.CreateInboundActivityHandler`. |
| Active circuits in the process (aggregates only) | Available | Singleton registry; ids truncated to 8 characters; only lifecycle counters of other circuits are visible — their components, events and state stay private to their own sessions. On a shared development server this still means other developers' circuit activity is visible; the panel says so. |
| Reconnect attempts | Instrumentation | `components:reconnect-state-changed` DOM event. |
| Message sizes, latency, payloads | N/A | Not exposed by the framework. |

## Browser

| Information | Status |
|---|---|
| Online/offline, visibility, user agent, JS heap (Chromium), navigation timing, resource count | Instrumentation (bridge) |
| localStorage / sessionStorage inventory (on request) | Instrumentation (bridge). Values are redacted **in the browser** when the key matches a sensitive pattern or the value looks like a token (JWT shape), so a secret never reaches the .NET side at all. |
| IndexedDB, generic DOM events | N/A by design (no Blazor-specific value; use browser DevTools) |

## BevTools itself

| Information | Status |
|---|---|
| Overhead: bookkeeping per render, UI render time, inspection, diagnostics, tree refresh | Available (About panel) |
| Pause / resume capture | Available. While paused no render, timeline, HTTP, JS interop or state-change record is written; errors are still collected, failed HTTP requests and JS interop calls included (their timeline link says the capture was paused). Renders that happen while paused are counted as *skipped* so the next recorded render keeps its correct cause. |
| Profiler baseline ("since reset") | Available. Raw counters keep running; the profiler, inspector and export show the delta since the reset on request. |
| Export | Available. `BevSessionExporter.ToJson` (`bit-bevtools/1`) and `ToMarkdownReport` contain exactly what the panels show, with the same redaction; browser storage previews are omitted. |
| Session isolation | Available. One session per circuit/app. Ambient activity (HTTP, logs, metrics) is routed by the renderer's synchronization context, then by `Dispatcher.CheckAccess()`. On Blazor Server there is deliberately **no** "only live session" fallback: attributing background work to whichever circuit happens to be connected would be a guess, and a guess about another user's data. |
