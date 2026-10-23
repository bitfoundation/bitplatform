# Architecture

## Goals that shaped the design

1. **No application source changes** for the core experience (tree, renders, events, HTTP, errors).
2. **Nothing faked.** Every number has a documented source; gaps are labeled in the UI (see `capabilities.md`).
3. **Minimal impact on the inspected app**: bounded buffers, a fixed and measured per-render cost (one render sample
   plus one timeline event, no string formatting on the hot path), lazy inspection, a polling UI that re-renders only
   when a version counter changed, and its own cost measured and reported. See [performance.md](performance.md).
4. **Session isolation**: one `BevToolsSession` per circuit (Server), per app (WebAssembly), per request (static SSR).
5. **Extensible**: panels, state providers, rules, commands and formatters through the contracts in
   `Bit.BevTools/Abstractions`.
6. **One agent, several frontends**: what a session knows is answered through one protocol, so the in-app panel, the
   companion window and the browser extension show the same data with the same redaction, and none can show more.
7. **No secrets required, production restricted**: redaction at capture time by name, by attribute and by shape;
   configuration never walked; a value-access level that defaults to reading nothing where other users' data is
   reachable; opening it in production takes an explicit allowance and an authorization callback.

## Layers

```
Bit.BevTools                     public contracts (Abstractions/) · runtime instrumentation · session stores ·
                                 inspection · diagnostics · protocol · UI (Razor) · page hub and companion (TypeScript)
Bit.BevTools.Server              circuit instrumentation (reference extension)
Bit.BevTools.Loader              startup hooks for attaching on demand, the attach policy and its authorization gate
Bit.BevTools.Extension           browser extension: the companion in a DevTools panel
```

The split mirrors the browser's own developer tools: an **agent** per .NET runtime (the session and its
instrumentation), a **protocol** it answers (`BevToolsProtocol`), and **frontends**:

```
 .NET runtime (circuit / WebAssembly)        page                           frontends
 BevToolsSession ── BevJsBridge ──────────► page hub (bit-bevtools.js) ──► in-app panel (Razor, same runtime)
   stores, rules     Protocol(method,args)    window.__BIT_BEVTOOLS__  ──► companion window (window.opener)
   inspection        page reports in          targets · traffic        ──► extension (inspectedWindow.eval)
```

The in-app Razor panel stays the primary frontend: it works in every host, Blazor Hybrid included, reads live objects
directly, and is where extensions contribute Razor panels. The companion and the extension are one TypeScript frontend
over the protocol: they add every runtime of a page in one view, and a view that renders nothing into the app, which on
Blazor Server keeps BevTools' own render batches off the circuit being measured.

The contracts use no Blazor types, so they can move back into a package of their own (with `[TypeForwardedTo]`) if a
consumer that cannot take a Blazor dependency appears; until then a library integrates by referencing `Bit.BevTools`.

Both packages share the `Bit.BevTools` namespace, the way every bit platform product keeps one namespace per
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
| `SessionResolver` | Ambient session lookup: explicit ambient scope → the renderer synchronization context (an O(1) map, which is the common case) → `Dispatcher.CheckAccess()` over live sessions → the only live session, **never in a server process** (a host with an ASP.NET Core `IServer`, whatever the session's platform reports). | Lets process-wide hooks (HTTP handler pool, logger provider, meters) find the right session without per-session registration. The server exclusion is deliberate: "the only circuit connected right now" is not evidence that background work belongs to that user. It is decided by the host rather than by `Platform`, which stays unknown for a circuit until something reports it (always on .NET 8) and is never reported by a request scope that renders nothing. |
| `BevToolsHttpMessageHandler` + `BevToolsHttpFilter` | `IHttpMessageHandlerBuilderFilter` appends the handler as the **innermost** `DelegatingHandler` of every `IHttpClientFactory` pipeline, next to the primary handler, and inserts a small handler at the **outermost** end that resolves the issuing session and passes it in `HttpRequestMessage.Options`. | Every attempt a retry/resilience handler makes is a real network request and is recorded as one; sitting outermost would hide retries behind a single record. Handlers are pooled across scopes, hence session resolution at send time - at the outer end, while the caller's synchronization context is still current: a handler in between that awaits with `ConfigureAwait(false)` (a retry delay, a token fetch) runs the recorder on the thread pool, where the session can no longer be inferred. |
| `BevTrackingJSRuntime` / `BevTrackingJSObjectReference` | Decorators with in-process variants so `IJSInProcessRuntime` casts keep working. | Applied per component; global DI decoration is unsafe because `CircuitFactory` casts `IJSRuntime` to `RemoteJSRuntime`. |
| `BevToolsLoggerProvider` | `ILoggerProvider` capturing Warning+ from `Microsoft.AspNetCore.Components*`, `Microsoft.JSInterop`, SignalR. | Catches unhandled circuit exceptions, event-handler exceptions and ErrorBoundary logs without touching the renderer. Messages are redacted (`BevRedactor.RedactText`) before they are stored. |
| Page hub (`bit-bevtools.js`) | ES module imported by each panel; one instance per page, whatever the number of runtimes; `DotNetObjectReference` callbacks per target. | See *The page hub* below. |
| `BevComponentWiringReader` | The component's latest frames (`ComponentState.CurrentRenderTree` → `RenderTreeBuilder.GetFrames()`), read on demand by the inspector. | Lists DOM event handlers (attribute frames with an event handler id), `@bind` change handlers (`AttributeEventUpdatesAttributeName`) and callbacks passed to child component frames; handlers are named by method, lambdas by the method declaring them. Never on the render path. |
| `BevRoutingReader`, `BevAuthenticationStateProvider`, `BevRuntimeInfo` | Public parameters of tracked `RouteView`/`Router` instances; the scope's `AuthenticationStateProvider` (by name); public runtime APIs. | Routing is read on demand; the authentication state is a built-in state provider, so its changes get history, diffs and timeline rows like any state. |

### Why render causes are trustworthy

A render sample's cause is decided in this order, and the label says which rule matched:

1. First render → *Initial*.
2. An ancestor rendered earlier **in the same batch** → *Parent re-rendered (parameters re-applied)*. This is exactly
   what the renderer does: the parent's diff calls `SetParametersAsync` on the child, which queues the child in the same
   batch.
3. A `HandleEvent`/`Navigate` activity is running on the session, or it has stopped and this batch renders what it had
   queued → *Event: onclick → Type.Method* / *Navigation*. The second half exists because the framework stops a
   synchronous handler's activity before it renders the components the handler queued; `ActivityObserver` reads the
   renderer's queue (`RenderBatchBuilder.ComponentRenderQueue`) when the activity stops, and the trigger keeps the one
   batch - the one in progress or the next - that renders one of them. Nothing else counts once the activity stopped:
   `Activity.Current` flows with the execution context, so a timer, background loop or fire-and-forget task started
   inside an event or navigation still carries its activity long after it ended, and what that work renders, requests
   or logs is not the trigger's.
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
is never judged - nor the first after renders that were not compared (the component was not inspected, or capture was
paused), whose snapshots are dropped rather than compared against. Render fragments are the exception, because their output is not observable without running them: they
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
redaction (`BevRedactor`: name patterns + `[BevToolsSensitive]`, applied to members, dictionary keys and component fields
- an auto-property's backing field is judged by its property, which is where the attribute is written). `Flatten`/`Diff`
produce bounded path→display maps for state and component-field change detection.

`BevLambdaTargets` names inline handlers. The framework reports a lambda by its compiled name; BevTools finds the
lambda (on the component, or in a nested closure class), decodes its IL with the `OpCodes` table - or its async state
machine's `MoveNext` - and keeps the call targets that are methods of the component's own types, not properties and not
the framework's. Exactly one is a name; anything else is "lambda in X.BuildRenderTree". Results are cached per method
(at most 2000), so an event pays for the IL once.

### Export (`Bit.BevTools/Export`)

`BevSessionExporter` turns a session into a `bit-bevtools/1` JSON document (`ToJson`) or a short Markdown report
(`ToMarkdownReport`). It is written with `Utf8JsonWriter` rather than the reflection serializer so that arbitrary
values an extension put into an event's `Data` bag can never make an export fail: primitives and string lists are
written as themselves, everything else as the bounded display the inspector gives it in the timeline panel. Every value
goes through the inspector first, so a sensitive key, or a type with sensitive members (whose `ToString` a record would
fill with every member), is no more visible in the file than in the panel. The export reads only what the panels read — the same stores,
already redacted at capture time — and omits browser storage previews. `IBevToolsCommandContext.DownloadTextAsync` /
`CopyTextAsync` hand the result to the browser bridge (`downloadText`, `copyText` in `bit-bevtools.js`).

The `profile` section (`BevRenderProfile`) is what two recordings are compared on: renders, render time, max and p95
per component *type*, since instance ids mean nothing across recordings. It is counted from the session-long per-instance
counters, plus per-type totals the registry keeps for components it let go after `DisposedComponentRetentionSeconds`
(`ComponentRegistry.GoneTotals`, reset with the baseline and cleared with *Clear*), so a page navigated away from a
minute ago still counts. `BevProfileComparison` compares two profiles; the Profiler runs it against the live session
and keeps the uploaded profile to compare again after another try. Protocol snapshots leave the section out unless they
name it, as building it reads every retained sample.

### Diagnostics (`Bit.BevTools/Diagnostics`)

Rules implement `IBevDiagnosticRule.Evaluate(IBevDiagnosticContext)` over component snapshots and recent events; findings are
deduplicated by fingerprint, dismissable, mirrored into the timeline once and bounded by `MaxFindings` (the ones no rule
has reported for the longest time go first, since several rules report one finding per occurrence). Built-ins: render storm (with cause
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

### Attaching to a running app (`Bit.BevTools.Loader`, `RuntimeAttachment`)

Everything above assumes BevTools was registered while the app started. An app that lazy loads `Bit.BevTools.wasm`
cannot do that: registering references the assembly. Four things are only reachable while the app starts, so a
second, trimmable and AOT compatible assembly, `Bit.BevTools.Loader`, installs them then and hands them over later:

| Startup-only | Why | Loader hook |
|---|---|---|
| The renderer's `IComponentActivator` | read once, when the renderer is created | `BevToolsActivatorHook`, a pass-through activator BevTools steps into on attach |
| `IHttpClientFactory` pipelines | built from filters registered in DI | pass-through handlers, outermost (session capture) and innermost (recording) |
| `ComponentsActivitySource` / `ComponentsMetrics` (.NET 10) | read once, when the renderer is created | registered at startup (`UseFrameworkInstrumentation`) |
| The `IServiceCollection` | gone once the container is built | kept by the loader for the Services panel |

The loader never references `Bit.BevTools`; `Bit.BevTools` references the loader. They meet once, through
`[assembly: BevToolsEntryPoint]` on the loaded assembly, and talk through internal interfaces (`IBevToolsEntryPoint`,
`IBevToolsAttachment`, `IBevToolsHttpRecorder`). `BevToolsLoader.OpenAsync` loads the assembly (from the startup
assemblies, else through `LazyAssemblyLoader`), and `RuntimeAttachment` creates a session outside DI keyed by the scope's
`IServiceProvider`. The first `BevToolsPanel` of that scope attaches it to its renderer: it steps into the activator hook,
reads the existing components from the renderer's state map, and marks those that already rendered so their next render
is not reported as their first. The loader's scoped service disposes the session with the scope. Every scope the loader
opens shares one `BevToolsRegistry`, and with it one service graph, as the sessions of a BevTools registered at startup do.

`BitBevTools.Attach` does the same without the loader and falls back to reflection where the loader has hooks: it
replaces the renderer's activator (`ComponentFactory._componentActivator`) and wraps the handler of the `HttpClient`
registered in DI (`HttpMessageInvoker._handler`), unless it is transient or its pipeline already records (a factory
client carries the loader's hook). In an app that installed the loader's hooks it uses them, and the collection the
loader kept. Each accessor lives in `BlazorReflection`/`RuntimeAttachment`, degrades to a reported gap, and
`CapabilityReport` says which path a session took.

Attaching is enabled, being the app's decision, except in a process that serves many users outside Development, where
the panel would show any visitor the server side objects of their circuit. `BevToolsAttachPolicy` in the loader holds
that rule and both `BevToolsLoader.CanOpen` and the attach apply it, telling a server process by its `IServer`
registration, so the button an app offers never leads to a refused attach. A session registered at startup and left off
gives way to an attached one. Nothing tells BevTools when a scope ends - a panel can be gone long before its circuit
closes, or never rendered - so every attach, and a timer while attached sessions are left, disposes those whose scope
refuses to resolve services any more; otherwise `SessionResolver` would keep them, and their scopes, for good.

### The page hub (`Scripts/bit-bevtools.ts`)

A Blazor Web App can run a Server circuit and the WebAssembly runtime on one page, and both import the same module, so
the bridge is a page-level hub with one *target* per attached runtime rather than a single attached reference:

* **Page-level facts go to every target**: JS errors and unhandled rejections, elements that failed to load (a
  capture-phase `error` listener, since those events do not bubble), CSP violations, online/visibility, reconnect
  state, Blazor's `enhancedload`, WebSocket traffic. They belong to the page, not to one runtime.
* **JS → .NET calls go to the runtime that received them**: a `DotNetObject` carries its call dispatcher, and so does
  each target's own reference. A static `DotNet.invokeMethodAsync` goes to the default dispatcher, which a page with two
  runtimes does not have (the framework rejects such calls there), so it is attributed only when one target exists.
* **The shortcut goes to the focused runtime**: the one whose panel is open (`data-bev-target` on its root), else the one
  attached last. The panel header's runtime switcher asks the hub to show one panel and hide the others.
* **WebSocket traffic** is counted from `WebSocket.prototype.send` (which finds a socket that existed before BevTools
  attached on its next send; SignalR pings every 15 s) and a wrapped constructor (new sockets, from their first frame).
  Each frame is classified by direction and SignalR message kind, decoding the envelope only: a minimal MessagePack
  reader skips every argument it does not need, and the JSON protocol is read with patterns over the envelope.
  `JS.RenderBatch` is sized by its binary argument and timed until the browser's `OnRenderCompleted` for the same id.
  BevTools' own traffic is identified exactly and counted apart: a `BeginInvokeDotNetFromJS` whose object id is a
  target's reference and the `JS.EndInvokeDotNet` that answers its call id (its reports); a `JS.BeginInvokeJS` that
  targets the runtime's BevTools module, whose JS object id the panel hands over on attach, and the
  `EndInvokeJSFromDotNet` that answers its handle (an export handed to the browser for download is hundreds of KB);
  and a render batch whose DOM changes all landed inside `.bev-root` (the panel rendering). Render batches are
  attributed when the browser acknowledges them (`OnRenderCompleted`, sent right after the batch was applied), from
  a `MutationObserver` over the document: a batch that changed the app's DOM, or any change between acknowledgements
  that is not BevTools', counts as the app's, so the error is always towards reporting. Writes that leave a value as
  it was are not changes: page scripts that react to every render (the demo's re-applies its heading anchors) would
  otherwise make every panel render look like the app's. The browser acknowledges a batch while the frame that
  carried it is still being dispatched - before this module's listener for that frame runs - so an acknowledgement
  is classified when it is sent and the batch settled when its frame is decoded, its apply time measured from the
  message event's timestamp. None of BevTools' own traffic
  triggers a report (it would otherwise schedule the next one forever). Reports go to .NET at most every 2 s, and only
  when application traffic changed. Found in a real browser: before this, exporting a session over a circuit
  produced "large message" warnings about BevTools' own downloads.
* **The hook** `window.__BIT_BEVTOOLS__` (frozen; `targets`, `request`, `traffic`, `subscribe`, `show`) exists while a
  target is attached, and is the only way in for external frontends. No message channel is opened on the page. The
  last detach removes every listener and restores `DotNet` and `WebSocket` as they were.

URLs the hub shows itself (sockets, failed resources, CSP reports) are redacted in the browser with the patterns .NET
sends on attach, plus SignalR's connection id (`id`) and SAS signatures (`sig`).

### The protocol and the external frontends (`Protocol/`, `Scripts/bit-bevtools-companion.ts`, `Bit.BevTools.Extension`)

`BevToolsProtocol.Handle(session, method, json)` answers `hello`, `snapshot` (the `bit-bevtools/1` export, optionally
limited to sections and to the timeline after an event id, the newest 200 events always resent because their activity
can still complete; `{ unchanged: true }` when the session version did not move), `inspect` and `expand` (a
component's members as inspected nodes, children on demand by path), `command` (pause, resume, clear, baseline,
evaluate, dismiss), `export` (JSON, Markdown, trace), and `locate` / `resolve` (a component's path in the page and back,
see *Components in the page*). It runs on the renderer's dispatcher like the panel, through
the same inspector, so it reads exactly what the panel reads. Inspecting from outside captures the component's state
on its renders, as selecting it in the panel does.

The companion is one frontend with three sources: the opener's hub (companion window), `inspectedWindow.eval`
(extension; a request starts in the page and its answer, a JSON string, is collected from a page-side mailbox, since
eval returns only synchronous values), and a file (an export, read only). It polls the shown runtime every 1.5 s while
visible, merges timeline increments by id, and renders application strings as text nodes only. The extension has no
permissions and no content script; everything it evaluates passes request data as JSON string literals.

### Components in the page (`Inspection/BevDomMap.cs`, the hub's *components in the page* section)

Blazor exposes no component-to-DOM map, but its browser renderer builds one implicitly: every node it inserts carries,
under `Symbol()` keys, the array of its logical children and a pointer to its logical parent, and every interactive
root container carries a `true` marker while its component is attached. Each frame of a component's current render
tree becomes exactly one logical child - an element, a text node, or a comment that contains a child component or a
markup block - regions are flattened, attributes and reference captures produce nothing (`BrowserRenderer.insertFrame`).

So `BevDomMap.Locate` walks a component's `ParentComponentState` chain to its root and, for each parent, finds the
child's component frame in the parent's latest frames, recording each step as *(logical index, kind)*. The hub starts
at the root container and follows the steps, comparing every node's tag (or `#text`, `#c`) with the step; the first
mismatch ends the walk with an error. `Resolve` is the inverse: the hub climbs from a node to its root container
recording the same steps, and .NET follows them down the frames, entering each child component's own frames,
stopping in markup or text, and verifying every kind. Both run on the renderer's dispatcher and read the frames in
place; their cost is reported as *page mapping* overhead.

The keys have no description. The hub learns them from the panel's own element, whose logical parent is the panel
component's container comment: no physical parent is ever a comment, so a DOM implementation's or another library's
symbol-keyed tree on the same nodes cannot be mistaken for Blazor's. Only when no panel has a comment parent is an
element parent carrying a marker accepted: the panel rendered as a WebAssembly root of its own. Boolean keys found on
what the panel itself rendered are excluded from root detection, and a container is a live root while it carries an
attached mark and no detached one: Blazor sets the attached mark `false` on disposal and leaves a Web App root's
preserve-content mark `true`. The root the panel renders in is the *anchor*; every runtime's anchor is left out of the
other roots on both sides, which are matched by signature (the count and kinds of their first logical children): a path
that fits only one identifies it; identical roots are matched by position only when the page holds exactly the
runtime's own (and the answer says so); when two runtimes both claim a node, the answer is "ambiguous", not a pick.

Hovering a row of the component tree is handled by the hub (a delegated listener and a `locate` request), not by a
Blazor event per row, so hovering sends nothing on a circuit beyond BevTools' own protocol traffic. Blazor events the
panel does raise (clicks, typing) are marked while the browser dispatches them, so the `DispatchEventAsync` message
that carries each one counts as BevTools' own traffic, and changes to the overlay are ignored when the hub decides
whose a render batch was.

Picking runs in the hub: capture-phase listeners on `window` swallow pointer, click and touch events outside BevTools'
own UI until a click or Escape, so the application never sees the picking click. The overlay is a `.bev-root` element
appended to `body`, outside every element Blazor manages, so its DOM changes count as BevTools' own for render-batch
classification; the crosshair cursor comes from a stylesheet rule keyed on the overlay, not from writing the app's DOM.
An outline follows its component on scroll and resize. *Flash renders* is driven from the panel's refresh loop: the
components that rendered since the previous tick (at most 60) are located and outlined, coloured by how many times
they rendered; the request travels after the render batch on the same connection, so the browser has the DOM first.

### Forms (`Session/BevFormTracker.cs`)

An EditForm's render fragment gets one extra step at activation: after its first render, when its EditContext exists,
the form is watched. Other EditContexts (an input under a hand-made cascading EditContext) are found when the tree is
next read. Watching adds two handlers to the EditContext: `OnFieldChanged` counts changes per field name, and
`OnValidationRequested` reads the outcome - BevTools subscribes before the validator components inside the form
initialize, so at the first validation it reads after a `Task.Yield` and moves its handler to the end of the list, and
from then on reads right after every validator ran, before `OnValidSubmit`. EditContexts are held weakly: the
field-change handler reads the context from its sender, and nothing BevTools keeps refers to one. A form whose EditForm
or input is disposed is forgotten and its handlers removed, and an EditForm that gets a new EditContext is watched again. Everything reported about the form's events leaves BevTools' own handlers out, and the framework's
form-mapping subscription is not counted as a validator (found on a Server circuit: an EditForm with a `FormName` has it).

### Source locations (`Inspection/BevSourceLocator.cs`)

Read on demand from portable PDBs with `System.Reflection.Metadata` (part of the shared framework): a method's first
sequence point (an async method's in its state machine's `MoveNext`), and for a type the documents its members are
written in, the `.razor` file first. Symbols are trusted only when they belong to this build: an embedded PDB; a PDB
beside the assembly or at the path its CodeView entry records, whose `DebugMetadataHeader` id matches that entry's;
on WebAssembly, where assemblies have no files, the PDB the runtime's `getConfig().resources.pdb` lists, fetched by
the hub and handed over (`Provide`) when the inspector first needs it (the protocol asks the panel to do the same and
answers `sourcesPending` meanwhile). Readers are cached per assembly for the life of the process.

### Leaks (`Session/BevLeakTracker.cs`, `Inspection/BevRetentionAnalyzer.cs`)

When the registry notices a disposal it hands the component to the leak tracker, which keeps a weak reference, the
time, and the number of full collections so far. *Retained* means alive after two full collections since then and a
grace period (work the component started may hold it briefly), or after the full collections a developer asked for. A
retained instance is analysed once: delegates and framework collections on what it was handed (injected services,
cascading values, NavigationManager, AuthenticationStateProvider), the JS runtime's `_trackedRefsById`, and its own
timers, all by reference identity. Instances with no cause found are checked against every frame buffer the renderer
keeps (both render trees of every live component, whole, and the batch's reference frames): a component its parent
stopped rendering stays in the parent's previous tree until the parent renders again, which is not a leak. That
behaviour was measured on a plain renderer with and without BevTools; the tests run on such a renderer too, since a
bUnit context keeps every component it rendered until it is disposed. The finding is the
`memory.retained-components` rule; the Runtime panel lists them and runs the forced check.

### Value access and redaction (`Inspection/`)

`BevObjectInspector` is the single path every application value takes to a panel, an export, a trace or a protocol
answer, so the rules live there and at capture:

* **Access level** (`BevValueAccess`, resolved per session): `None` hides every value but keeps its type; `Shallow`
  shows scalars and stops at objects (no members, no `ToString`, which a record would fill with every member); `Full`
  walks with redaction. The default reads nothing outside Development in a process that serves many users: there a
  circuit's objects can reach shared state holding other users' data. BevTools' own event metadata (causes, ids) is
  shown at `Shallow` even under `None` (`DescribeMetadata`). Forced-render detection is skipped below `Full` - with
  values hidden every render would look unchanged - so those renders read *not compared*, never *forced*.
* **Redaction by shape** (`BevRedactor.LooksLikeSecret` / `RedactText`) complements names: JWTs, bearer tokens, PEM
  keys, credential key/value text, provider token formats. Free text keeps its readable part; text that still looks
  like a secret after redaction is hidden whole.
* **Pairs** (a `KeyValuePair`, a claim, a cookie: a `Key`/`Name`/`Type` beside a `Value`) redact the value when the name
  is sensitive, in children, flattened diffs and summaries alike.
* **Never walked**: configuration (`IConfiguration`, its providers and sources, `IOptions*<T>`) and credentials or key
  material. Configuration is where secrets sit under names no pattern predicts, so it is not read rather than filtered.

### The prerendering handoff (`WebApp/BevPrerender.cs`)

Prerendering runs in a session of its own that ends with the response, so the interactive session cannot see it. While
prerendering, the panel registers a `PersistentComponentState` callback, for the interactive render mode that applies
to it (its own, or the nearest ancestor's: the state of an Auto page is persisted for both runtimes); when the
response persists state, the callback writes a bounded summary - components, render totals, the slowest components,
the requests made, the errors - and the interactive panel takes it back. The *Data fetched twice* rule compares the
summary's requests with the interactive session's first 30 s. Only under `Full` access, since the summary travels in
the page's HTML.

### Performance traces (`Export/BevTraceExporter.cs`)

The timeline as Chrome Trace Event JSON: complete events on a renderer track, each batch spanning its renders (its own
event holds only the summed build time), so renders nest in their batch; UI events, HTTP and JS interop on lanes
allocated so that slices on one track never overlap; markers for state, errors, circuit/browser and findings; flow
arrows from an event to the batches and requests it caused. Timestamps come from each event's high-resolution start,
except for events recorded after they happened (adopted on stop, reported in batches), whose wall-clock time is used.

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
| **Contract** — `Bit.BevTools/Abstractions` | `IBevToolsExtension`, `IBevDiagnosticRule`, `IBevToolsCommand`, `IBevStateProvider`, `IBevToolsTimeline`, `BevToolsEvent`, `BevDiagnostic`, `BevComponentSnapshot`, the attributes. No Blazor types. | Treated as versioned public API. Additions are additive; removals are breaking changes. |
| **Setup** | `AddBitBevToolsServices`, `AddBitBevToolsServerServices`, `BitBevToolsOptions`, `<BevToolsPanel />`, `BitBevTools.Attach`; in `Bit.BevTools.Loader`: `AddBitBevToolsLoaderServices`, `BitBevToolsLoaderOptions`, `BevToolsLoader`, `<BevToolsLoaderPanel />`, `BevToolsHttpHookHandler`. | Stable. Options gain properties; defaults change only with a reason stated in the release notes (this release: `ValueAccess` defaults to `None` outside Development in a server process, and `AllowInMultiUserHost` takes `Authorize`). |
| **Read models** — the `Model/` and `Session/` types, `BevObjectInspector`, `BevSessionExporter`, `BevTraceExporter`, `BevFmt`, `BevValueTree` | What the built-in panels read and render. Public so an extension panel can present the same data the same way. | Usable, but shaped by the UI's needs; expect additive change and occasional refinement. The JSON export format is versioned (`format` field) so readers can tell versions apart, and so is the trace (`otherData.format`). |
| **Protocol** — `BevToolsProtocol`, `window.__BIT_BEVTOOLS__` | What external frontends speak: methods, their arguments, their answers (a snapshot is the export). | Versioned (`BevToolsProtocol.Version`, the hook's `protocol`); additions are additive, a frontend checks `hello` before it relies on anything newer. |

Razor components other than `BevToolsPanel` are public because the Razor SDK makes them so, not because they are a
supported surface. Depend on `BevValueTree`, `BevInspectorSection` and `BevFmt` if they help; treat the panels themselves as
internal.

## Deliberate non-goals

* Request/response bodies, DI instance values, full circuit ids (security).
* Time-travel state restoration (unsafe for arbitrary graphs); snapshot history is provided instead.
* Recreating browser DevTools panels (network waterfall, DOM, WebSocket frame payloads): the WebSockets view counts
  sizes and SignalR message kinds, which is the Blazor-specific part, and leaves the frames themselves to the browser.
* Editing component state from the inspector: setting a field of a live component behind the app's back is a way to
  produce states the app cannot reach, and the inspector never mutates values.

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
* Value safety: credential shapes under innocent names, pairs with sensitive names, configuration and options never
  walked, `Shallow` and `None` access, messages redacted at capture, the loader's authorization gate (refusing,
  throwing, asked again on open) and the `None` default outside Development in a server process.
* Application model: event handlers, `@bind` and child callbacks read from a real render tree; routing through a real
  `Router`; authentication through bUnit's provider with a change history; the prerendering handoff through bUnit's
  persistent state; the *data fetched twice* and *large render batch* rules; traces whose renders nest in their batch.
* Protocol: hello, snapshots (unchanged, sections, timeline increments), inspect and expand through the same
  redaction, commands and exports.
* Browser (vitest): the hub with two runtimes, page facts reaching both, detach of one; WebSocket decoding of
  blazorpack and JSON frames, render batch sizing and timing, BevTools' own reports kept apart and never re-reported;
  resource and CSP failures with redacted URLs; the frozen hook and no message listener; the companion in happy-dom
  (runtimes, live inspection, text-only rendering of hostile names, commands, offline files); the extension source
  (mailbox round trips, request data that cannot become code).
* Non-functional: parallel writers, session churn, bounded buffers, per-render overhead ceiling, disabled-mode footprint,
  plus a reproducible benchmark (`Tests/Bit.BevTools.Benchmarks`). CI (`.github/workflows/bit.ci.BevTools.yml`)
  builds the solution, runs the .NET suite on net8.0, net9.0 and net10.0, the JavaScript tests, assembles the
  extension, and makes a benchmark smoke run.
