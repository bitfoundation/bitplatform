# Architecture

## Goals that shaped the design

1. **No application source changes** for the core experience (tree, renders, events, HTTP, errors).
2. **Nothing faked.** Every number has a documented source; gaps are labeled in the UI (see `capabilities.md`).
3. **Minimal impact on the inspected app**: bounded buffers, a fixed and measured per-render cost (one render sample
   plus one timeline event, no string formatting on the hot path), lazy inspection, a polling UI that re-renders only
   when a version counter changed, and its own cost measured and reported. See [performance.md](performance.md).
4. **Session isolation**: one `BevToolsSession` per circuit (Server), per app (WebAssembly), per request (static SSR).
5. **Extensible**: panels, state providers, rules, commands and formatters through `Bit.BevTools.Abstractions`.

## Layers

```
Bit.BevTools.Abstractions        public contracts, no Blazor dependency
Bit.BevTools                     runtime instrumentation · session stores · inspection · diagnostics · UI
Bit.BevTools.Server              circuit instrumentation (reference extension)
```

All three packages share the `Bit.BevTools` namespace, the way every bit platform product keeps one namespace per
product; the folders below organise the source, not the API. Every public type carries the product name - `BevTools*`
for the types that are the tool itself, a short `Bev` prefix for the rest - so importing `Bit.BevTools` never brings a
bare `Diagnostic` or `CommandPalette` into an application's scope.

### Runtime instrumentation (`Bit.BevTools/Instrumentation`)

| Piece | Mechanism | Notes |
|---|---|---|
| `BevToolsComponentActivator` | Public `IComponentActivator` registered in DI (scoped). Wraps a previously registered activator (custom ones, bUnit) as inner. | Registers a `BevComponentRecord` and instruments the instance. Types under `[BevToolsIgnore]` or matching ignore predicates are skipped. BevTools' own components are recognised by assembly and by extension panel descriptor; see *Never observing itself*. |
| `RenderTracker` | Replaces `ComponentBase._renderFragment` with a measuring wrapper (`FieldInfo.SetValue` on the readonly field). | Measures `BuildRenderTree`, detects batch boundaries through `RenderBatchBuilder.UpdatedComponentDiffs.Count`, attributes causes, captures optional field diffs, and patches `[Inject] IJSRuntime` on the first render (the earliest point after the framework injects it). Batch diff measurements that arrive late — on Blazor Server the framework records them when the browser acknowledges the render — are matched back to the batch that produced them through a queue of batches still awaiting one. |
| `BlazorReflection` | Expression-compiled accessors created once; each is null when the member is missing. | The capability report reflects which accessors work. |
| `ActivityObserver` | Static `ActivityListener` on `Microsoft.AspNetCore.Components` and `MeterListener` on the two components meters. `AddBitBevToolsServices` also registers the framework's own `ComponentsActivitySource`/`ComponentsMetrics` (public `AddComponentsTracing`/`AddComponentsMetrics`), which only server-side rendering registers on its own. | Events are attached to activities via `Activity.SetCustomProperty`; sessions are resolved with `SessionResolver`. The framework tags its activities on *stop*, so the component type is read from the display name while the handler is still running and corrected from the tags afterwards. Interactions with the BevTools UI itself are recognised and never recorded. |
| `SessionResolver` | Ambient session lookup: explicit ambient scope → the renderer synchronization context (an O(1) map, which is the common case) → `Dispatcher.CheckAccess()` over live sessions → the only live session, **never on Blazor Server**. | Lets process-wide hooks (HTTP handler pool, logger provider, meters) find the right session without per-session registration. The Server exclusion is deliberate: "the only circuit connected right now" is not evidence that background work belongs to that user. |
| `BevToolsHttpMessageHandler` + `BevToolsHttpFilter` | `IHttpMessageHandlerBuilderFilter` appends the handler as the **innermost** `DelegatingHandler` of every `IHttpClientFactory` pipeline, next to the primary handler. | Every attempt a retry/resilience handler makes is a real network request and is recorded as one; sitting outermost would hide retries behind a single record. Handlers are pooled across scopes, hence session resolution at send time. |
| `BevTrackingJSRuntime` / `BevTrackingJSObjectReference` | Decorators with in-process variants so `IJSInProcessRuntime` casts keep working. | Applied per component; global DI decoration is unsafe because `CircuitFactory` casts `IJSRuntime` to `RemoteJSRuntime`. |
| `BevToolsLoggerProvider` | `ILoggerProvider` capturing Warning+ from `Microsoft.AspNetCore.Components*`, `Microsoft.JSInterop`, SignalR. | Catches unhandled circuit exceptions, event-handler exceptions and ErrorBoundary logs without touching the renderer. |
| Browser bridge (`bit-bevtools.js`) | ES module imported by the panel; `DotNetObjectReference` callbacks. | JS errors, online/visibility, reconnect state, storage inventory on request, JS→.NET call batching, toggle shortcut. |

### Why render causes are trustworthy

A render sample's cause is decided in this order, and the label says which rule matched:

1. First render → *Initial*.
2. An ancestor rendered earlier **in the same batch** → *Parent re-rendered (parameters re-applied)*. This is exactly
   what the renderer does: the parent's diff calls `SetParametersAsync` on the child, which queues the child in the same
   batch.
3. A `HandleEvent`/`Navigate` activity is current on the session → *Event: onclick → Type.Method* / *Navigation*.
4. Otherwise → *StateHasChanged outside an event*: timer, async continuation, state-container notification or explicit
   call. BevTools does not walk the stack to name the caller; the timeline's *Just before* section shows the surrounding
   activity instead.

### Needed or forced?

A parent-caused render answers *why*; the follow-up is whether the child got anything new. For the component that is
being inspected (or every component when `CaptureComponentStateOnRender` is on) `RenderTracker` flattens the
`[Parameter]`, `[CascadingParameter]` and `[SupplyParameterFrom*]` values before each render — display value plus two
levels of members, bounded to 25 collection items and one wall-clock budget per render — and diffs them against the
previous captured render. A parent-caused render with no observable difference is a **forced re-render**
(`BevRenderSample.IsWastedRender`): Blazor only skips re-rendering when every parameter is a primitive it can compare, so
a recreated list, lambda or anonymous object forces the child every time. The verdict is deliberately
*observable-only*: delegates compare by target method, redacted parameters compare equal, and the first captured render
is never judged. Render fragments are the exception, because their output is not observable without running them: they
compare by instance, and one whose target is a component (or a closure holding one) reads state as it runs and never
compares equal, so new child content is never called forced. The count is kept per instance
(`ParentRendersWithUnchangedParameters`, with `RendersWithComparedParameters` as its denominator; both are session-long
counters, unlike the bounded sample ring), shown in the inspector, exported, and used by the *Renders driven only by the
parent* rule to escalate from information to warning with the sentence "all N compared renders received values
identical to the previous render". The escalation gets its own timeline row.

### Pausing capture

`BevToolsSession.IsCapturing` gates every capture path — `Timeline.Record`, the render sample, the HTTP handler, the
JS interop wrapper, state-change notifications, JS→.NET batches — so a paused session's buffers are exactly what the
developer was reading. Errors are still collected, failed HTTP requests and JS interop calls included: losing one
because the timeline was frozen would be worse than an inconsistent row, so they land in the error center with no
timeline link. A UI event or navigation that starts while paused is never adopted as a trigger, so nothing points at an
event that is not in the timeline. Renders that happen while paused increment a
*skipped* counter on the record; cause detection uses "has rendered" rather than "has a recorded render", so the first
render after resuming is not mislabeled as the initial one. The pause marker is the only event written while paused.

### Never observing itself

A tool that reports its own activity is worse than one that reports nothing: it buries the developer's data under the
clicks they made to look for it, and every refresh tick adds more. So BevTools' own UI is excluded from every panel —
tree, profiler, timeline, JS interop, diagnostics — and its cost is reported as overhead in the About panel instead.

Ownership is decided in two steps, because one is not enough:

* **By type, at activation**: the BevTools assembly, the `Bit.BevTools` namespace itself (every BevTools component,
  the server package's included, lives directly in it - so `Bit.BevTools.Demo.*` or an application's
  `Bit.BevTools.Something` stays application code), and the component type of any panel an extension contributed
  (which lives in the extension's own assembly).
* **By position, once the hierarchy resolves**: a record whose parent is BevTools-owned becomes BevTools-owned.
  The panels render `Virtualize`, `CascadingValue` and `DynamicComponent`, which an application renders too, so their
  type says nothing about whose they are — only the tree does. The parent is unknown at activation (the instance is not
  attached to the renderer yet), and known by the first render, which is early enough: `RenderTracker` resolves the
  renderer and hierarchy before it decides to sample a render or to wrap the component's `IJSRuntime`.

Browser-side, `bit-bevtools.js` recognises its own `DotNetObjectReference` and skips it when wrapping `DotNet.invokeMethod*`,
and `ActivityObserver` marks `HandleEvent` activities raised by BevTools components as ignored.

### Session stores (`Bit.BevTools/Session`)

`BevToolsSession` (scoped) owns: `Timeline` (ring buffer of `BevToolsEvent`, monotonic ids, O(log n) lookup by id),
`ComponentRegistry` (weak identity map + lazily resolved hierarchy + disposal detection + cached tree), `ErrorCenter` (fingerprint dedupe),
`HttpStore`, `JsInteropStore` (records + per-identifier aggregates), `StateStore` (providers, flattened snapshots,
diffs, history), `DiagnosticsEngine`, `CommandRegistry`, `BevToolsUiState`, `BevOverheadMeter`, `BevBrowserState`.
Every mutation bumps `Session.Version`; the UI polls the version on a timer (250 ms open, 1 s closed) and re-renders
only on change, so idle BevTools costs a comparison per tick. Opening the panel forces a refresh immediately rather
than waiting for the next tick. Built-in tabs snapshot in `OnParametersSet` and have no other parameters, so the
host cascades a revision counter; without it Blazor skips `SetParametersAsync` and the open tab stays stale until
remounted. Live updates can be turned off in the header; Refresh always re-reads.

Components are held **weakly**: BevTools must never keep an application component — and through it a subtree, its
services and its renderer — alive. Records are swept on component churn (at most once a second) rather than only when
the UI is open, so a session that never opens BevTools still releases what the application dropped. Disposed records
are kept for `DisposedComponentRetentionSeconds` so the timeline and errors can still name them, and the number of
tracked components is capped by `MaxTrackedComponents` with the truncation reported in the About panel.

### Inspection (`Bit.BevTools/Inspection`)

`BevObjectInspector` describes values as `BevInspectedNode`s and resolves children by re-walking a path from the root, so
nothing is serialized eagerly. It handles cycles along the expansion path, throwing getters, depth/size caps, special
types (delegates, `RenderFragment`, `EventCallback`, tasks, streams, service providers), extension formatters and
redaction (`BevRedactor`: name patterns + `[BevToolsSensitive]`). `Flatten`/`Diff` produce bounded path→display maps for
state and component-field change detection.

### Export (`Bit.BevTools/Export`)

`BevSessionExporter` turns a session into a `bit-bevtools/1` JSON document (`ToJson`) or a short Markdown report
(`ToMarkdownReport`). It is written with `Utf8JsonWriter` rather than the reflection serializer so that arbitrary
values an extension put into an event's `Data` bag can never make an export fail: primitives and string lists are
written as themselves, everything else as bounded text. The export reads only what the panels read — the same stores,
already redacted at capture time — and omits browser storage previews. `IBevToolsCommandContext.DownloadTextAsync` /
`CopyTextAsync` hand the result to the browser bridge (`downloadText`, `copyText` in `bit-bevtools.js`).

### Diagnostics (`Bit.BevTools/Diagnostics`)

Rules implement `IBevDiagnosticRule.Evaluate(IBevDiagnosticContext)` over component snapshots and recent events; findings are
deduplicated by fingerprint, dismissable and mirrored into the timeline once. Built-ins: render storm (with cause
breakdown), renders driven only by the parent (the steady avoidable re-render that no rate threshold catches), render
cascade, slow render, slow event handler, duplicate HTTP, slow HTTP, repeated HTTP failure, slow JS interop, excessive
JS interop, repeated error; the server package adds circuit instability/blocking; the DI
graph adds captive dependency, cycle, transient-disposable and large-constructor findings. Thresholds live in
`BevDiagnosticsOptions`; rules can be disabled by id.

### UI (`Bit.BevTools/UI`)

`BevToolsPanel` is the only public component. It renders nothing during prerendering or when disabled, cascades itself
as the `IBevToolsCommandContext` to panels, hosts the refresh loop and the browser bridge, and persists preferences in
`localStorage`. Panels are plain Blazor components reading the session directly; lists use `Virtualize`; the tree and
command palette are keyboard-navigable; theme follows `prefers-color-scheme` unless overridden. Extension panels are
rendered with `DynamicComponent` and can be restricted to a platform (`RequiredPlatform = "Server"`).

### Multi-circuit behaviour (Server)

Everything per circuit is scoped. Process-wide pieces (activity/meter listeners, HTTP handler pool, logger provider,
`BevCircuitRegistry`) attribute work to a session through `SessionResolver`; when the session cannot be inferred (background
threads with several live circuits) the data is dropped rather than misattributed, and `session.UseAmbient()` lets
application code attribute such work explicitly. Other circuits are visible only as aggregates (truncated id, state,
counts).

## API surface and stability

Three tiers, because a developer tool that breaks its integrators is not infrastructure:

| Tier | What | Promise |
|---|---|---|
| **Contract** — `Bit.BevTools.Abstractions` | `IBevToolsExtension`, `IBevDiagnosticRule`, `IBevToolsCommand`, `IBevStateProvider`, `IBevToolsTimeline`, `BevToolsEvent`, `BevDiagnostic`, `BevComponentSnapshot`, the attributes. No Blazor dependency. | Treated as versioned public API. Additions are additive; removals are breaking changes. |
| **Setup** | `AddBitBevToolsServices`, `AddBitBevToolsServerServices`, `BitBevToolsOptions`, `<BevToolsPanel />`. | Stable. Options gain properties; defaults change only with a reason stated in the release notes. |
| **Read models** — the `Model/` and `Session/` types, `BevObjectInspector`, `BevSessionExporter`, `BevFmt`, `BevValueTree` | What the built-in panels read and render. Public so an extension panel can present the same data the same way. | Usable, but shaped by the UI's needs; expect additive change and occasional refinement. The JSON export format is versioned (`format` field) so readers can tell versions apart. |

Razor components other than `BevToolsPanel` are public because the Razor SDK makes them so, not because they are a
supported surface. Depend on `BevValueTree`, `BevInspectorSection` and `BevFmt` if they help; treat the panels themselves as
internal.

## Deliberate non-goals

* Component ↔ DOM highlighting (no reliable mapping from .NET).
* Request/response bodies, DI instance values, full circuit ids (security).
* Time-travel state restoration (unsafe for arbitrary graphs); snapshot history is provided instead.
* Recreating browser DevTools panels (network waterfall, DOM, WebSocket frames).

## Testing strategy

* Unit: ring buffer, timeline, inspector (cycles, redaction, caps, diffs), redactor, command matching, DI graph analysis,
  diagnostics rules with synthetic events, HTTP handler with stub pipelines, JS interop wrappers with fakes, state store.
* Instrumentation through a real renderer (bUnit): registration, hierarchy, render counts, causes, batches, state diffs,
  disposal detection, JS runtime patching, render exceptions, session resolution from the dispatcher.
* Scale: a thousand components, a 50 000-deep parent chain, render storms, a full event buffer, repeated create/destroy.
* UI: every panel opens, the component tree selects, the browser bridge reads storage (bUnit with a mocked module).
* Framework diagnostics: the exact activity, meter and tag names of `Microsoft.AspNetCore.Components`, so a rename
  breaks a test instead of silently degrading every render cause to *StateHasChanged*.
* Isolation and security: sessions never see each other, server-side background HTTP is not attributed to a circuit,
  a disabled or non-Development host registers nothing, the DI inspector never reads instance values.
* Extensibility: one third-party-style extension contributing a panel, rule, command, state provider and formatter.
* Forced-render detection: identical recreated parameters are marked forced, changed collection items are reported by
  path, `[BevToolsSensitive]` fields and parameters never appear in a diff, the parent-driven rule escalates only with
  evidence.
* Capture control: every capture path respects pause, the application is untouched, errors survive, the first render
  after resuming keeps its cause, state diffs span the pause.
* Profiler: baseline resets the delta without touching raw counters, grouping by type aggregates and drills down, the
  header pause button and the tree's ARIA attributes work when clicked.
* Export: valid JSON with every section, a `Data` value whose `ToString` throws does not break it, redacted secrets
  never appear in JSON or Markdown, BevTools' own UI is excluded.
* Non-functional: parallel writers, session churn, bounded buffers, per-render overhead ceiling, disabled-mode footprint,
  plus a reproducible benchmark (`Tests/Bit.BevTools.Benchmarks`). CI (`.github/workflows/bit.ci.BevTools.yml`)
  builds the solution, runs the .NET suite on net8.0, net9.0 and net10.0, the browser-bridge tests, and a benchmark
  smoke run.
