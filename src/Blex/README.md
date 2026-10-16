# bit Blex

Lightweight, source-generator-powered **reactive state management for Blazor and .NET MAUI** - with **Redux DevTools time-travel** built in.

Blex fills a real gap in the Blazor ecosystem. Fluxor is the de-facto Redux library but is widely criticized for boilerplate (separate Action / Reducer / Effect / Feature classes per operation) and for having no first-class DevTools time-travel. Blex keeps the good parts of the Flux model - a single observable state tree, named actions, middleware - while a Roslyn source generator removes the ceremony and a tiny JS bridge wires you straight into the Redux DevTools browser extension.

Blex's documentation website lives in [`Bit.Blex.Demo`](Bit.Blex.Demo/) - run the
[Server](Bit.Blex.Demo/Server/) project (`dotnet run`) for the full site: every feature explained
with a live, runnable example on the same page. It is a Blazor WebAssembly app with prerendering,
and it is itself a Blex app, so the **Inspector** dock at the bottom of every page shows the real
action feed, state tree, undo stack and `OnError` sink.

That same server hosts an **MCP server** at `/mcp`, so an AI agent can work against Blex's real API
instead of guessing at it - or at Fluxor's. Point an MCP client at `https://blex.bitplatform.dev/mcp`
(`http://localhost:5280/mcp` locally); every tool is also a plain HTTP GET under `/api/mcp/...`. It offers:

- **Search** across everything at once (`SearchBlex`) - this guide, the docs pages, every public member,
  the generator's diagnostics and the demo's sources - with the exact follow-up call on each hit.
- **Store inspection** (`InspectBlexStore`): runs the real source generator over a store and returns every
  `BLEX` diagnostic plus the exact members it generates, so an agent calls `Increment()` because the
  generator said so.
- **Setup** per hosting model (`GetBlexSetupGuide`): Server, WebAssembly, Auto, standalone WebAssembly,
  MAUI Blazor Hybrid and native MAUI.
- **The exact API** (`GetBlexApi`), reflected out of the shipped assemblies with every member's type,
  default value and XML documentation, and **the diagnostics** (`GetBlexDiagnostics`) with a rejected and
  a fixed store each.
- **The guide and the docs** as text (`GetBlexGuideSection`, `GetBlexDocsPage`) and the demo's own
  source (`GetBlexSourceFile`), plus resources (`blex://...`), four prompts, server instructions and
  argument completion.

---

## Why Blex

| | Fluxor | Blex |
|---|---|---|
| Define a piece of state | Feature + State class | one `[BlexState]` field |
| Define an action | Action class + Reducer method | one `[BlexAction]` method |
| Action payloads in DevTools/middleware | manual | automatic (`ctx.Args`, DevTools payload) |
| Derived state | manual / selectors | `[BlexComputed]` (memoized) |
| Derived state *across* stores | manual | `manager.CreateSelector(...)` (lazy, shared, memoized) |
| Async side-effects | Effect classes | `[BlexEffect]` (auto loading/error) |
| Effect cancellation / concurrency | manual | `CancellationToken` + `Latest`/`Drop`/`Queue` + `DebounceMilliseconds` |
| Ad-hoc batched mutations | ✗ | `store.Batch(name, ...)` (Pinia `$patch`-style) |
| Reset to initial state | manual | `store.ResetState()` |
| Middleware | yes | yes (with veto/filter hooks + payload access) |
| Action failure hook | ✗ | `OnActionError` (Pinia `$onAction`'s `onError`) |
| Granular re-render | manual selectors | selector `Subscribe(...)` (+ prev/current, fireImmediately, `BlexComparers`) |
| Normalized collections | manual | `BlexEntityAdapter` / `BlexEntityState` (+ sorting, `UpdateMany`, `Map`) |
| Persistence | 3rd-party | `[BlexStore(Persist = true)]` + debounce + versioning/migrations + hydration status |
| Undo / redo | ✗ | `BlexHistory` (in-app, labeled entries, `Jump`, `groupBy`) |
| Redux DevTools time-travel | ✗ | ✓ built in (+ state/action sanitizers) |
| Error isolation hook | ✗ | `options.OnError` |
| Test helpers | ✗ | `Bit.Blex.Testing` harness (multi-store `Register`, `Failures`, `WaitForAsync`) |
| Boilerplate | high | minimal (generated) |

## Install

```bash
dotnet add package Bit.Blex             # runtime
dotnet add package Bit.Blex.Generators  # source generator (analyzer only)
dotnet add package Bit.Blex.Blazor      # Blazor integration (BlexProvider, DevTools bridge)
dotnet add package Bit.Blex.Maui        # .NET MAUI integration (XAML binding, Preferences persistence)
dotnet add package Bit.Blex.Testing     # optional, for unit tests
```

The runtime packages target **net8.0, net9.0 and net10.0**. `Bit.Blex.Generators` is the Roslyn
generator that writes the store plumbing; it ships under `analyzers/dotnet/cs` only, so it adds
nothing to your app's output. Reference it from every project that declares `[BlexStore]` classes.

## The whole store

```csharp
[BlexStore(Name = "counter")]
public partial class CounterStore
{
    [BlexState] private int _count;
    [BlexState] private int _step = 1;

    [BlexComputed] private int  ComputeDoubleCount() => Count * 2;
    [BlexComputed] private bool ComputeIsEven()      => Count % 2 == 0;

    [BlexAction] private void OnIncrement()       => Count += Step;
    [BlexAction] private void OnSetStep(int step) => Step = step;
    [BlexAction] private void OnReset()           { Count = 0; Step = 1; }
}
```

The generator emits the reactive `Count`/`Step` properties, the memoized `DoubleCount`/`IsEven`
accessors, the public `Increment()`/`SetStep(int)`/`Reset()` action wrappers, JSON snapshot
support and the `BlexStoreBase` base type.

### Conventions

- **State**: `[BlexState] private T _foo;` → public reactive property `Foo`.
- **Computed**: `[BlexComputed]` on a parameterless `ComputeXxx()` / `GetXxx()` method → memoized property `Xxx`,
  automatically invalidated whenever state changes.
- **Actions**: `[BlexAction]` on a method named `OnXxx` → public `Xxx(...)` wrapper that batches all the
  mutations inside it into a single, named, time-travel-recorded action. `async Task` methods are
  supported (they update the UI as they go but record as one action). Override the name with
  `[BlexAction(Name = "...")]`. Action arguments are captured as the action's payload (visible to
  middleware, subscribers and DevTools).
- Directly assigning a generated property (e.g. `store.Count = 5`) is recorded as a `Set Count` action.
- **Batching from outside**: `store.Batch("Apply preset", () => { store.Count = 10; store.Step = 5; })`
  groups ad-hoc mutations into one named action with a single re-render (the `$patch`/`runInAction`
  equivalent).
- **Reset**: `store.ResetState()` returns the store to the state it had when first registered,
  recorded as a normal, vetoable `ResetState` action.

## Setup

```csharp
// Program.cs
builder.Services.AddBitBlexServices(options =>
{
    options.DevToolsName = "My App";
    options.UseMiddleware(ctx => Console.WriteLine($"[blex] {ctx.QualifiedName}"));
});
builder.Services.AddBitBlexStore<CounterStore>();
builder.Services.AddBitBlexStore<TodoStore>();
```

```razor
@* App.razor - wrap your router once *@
<BlexProvider>
    <Router ... />
</BlexProvider>
```

```razor
@* Counter.razor *@
@inherits BlexComponentBase
@inject CounterStore Store

<p>Count: @Store.Count (double: @Store.DoubleCount)</p>
<button @onclick="Store.Increment">+@Store.Step</button>

@code {
    protected override void OnInitialized() => Subscribe(Store);
}
```

`BlexComponentBase.Subscribe(...)` re-renders the component whenever a subscribed store changes
and unsubscribes automatically on dispose.

## Granular subscriptions (selectors)

`Subscribe(store)` re-renders on any change to that store. For stores with many independent fields,
subscribe to a projection instead so unrelated changes don't re-render the component:

```csharp
protected override void OnInitialized()
    => Subscribe(Store, () => Store.Count); // re-renders only when Count changes
```

The same primitive is available outside Blazor, with optional previous-value delivery (MobX
`reaction`-style) and `fireImmediately`:

```csharp
using var sub = store.Subscribe(() => store.Count, count => Console.WriteLine(count));
using var log = store.Subscribe(() => store.Count,
    (prev, curr) => Console.WriteLine($"{prev} -> {curr}"), fireImmediately: true);
```

`BlexManager` carries the same pair of overloads for a projection that spans stores, so a
cross-store derived value doesn't have to be recomputed on every action of every store:

```csharp
using var sub = manager.Subscribe(
    () => cart.Total > profile.CreditLimit,
    overLimit => _banner = overLimit ? "Over your limit" : null);
```

Keep a manager-level projection cheap - it runs after every action in the application, so read
already-memoized `[BlexComputed]` properties rather than recomputing over collections. It
is also re-evaluated after state *restores* (undo/redo, DevTools time-travel, an incoming cross-tab
change), which move state without dispatching anything - watching actions alone would leave the
subscription reporting a value the application no longer holds.

A projection that returns a *collection* defeats the point on its own: `() => store.Items.Where(...)`
builds a new object every time it runs, so the default reference comparison always reports a change.
`BlexComparers` supplies the content comparers that fix it (zustand's `shallow`):

```csharp
Subscribe(Store, () => Store.Todos.All.Where(t => !t.Done), BlexComparers.Sequence<Todo>());
using var sub = store.Subscribe(() => store.SelectedIds, Refresh, BlexComparers.Set<int>());
```

## Effects (async with managed loading/error)

`[BlexEffect]` marks an async method (returning `Task`/`ValueTask`) whose loading and error lifecycle is
generated for you. The body is still recorded as a single, named, time-travelable action.

```csharp
[BlexEffect]
private async Task OnLoadUser(int id)
{
    var user = await _api.GetUserAsync(id);
    User = user;
}
```

The generator emits `LoadUser(int)` plus reactive `LoadUserIsLoading` (bool) and `LoadUserError`
(`Exception?`) properties. The wrapper keeps `IsLoading` true while any run is in flight (overlapping
runs are reference-counted) and captures any thrown exception into `Error` instead of propagating it.

### Cancellation and concurrency

Give the effect a trailing `CancellationToken` parameter and the generator supplies the token and
emits a `CancelXxx()` method. `Concurrency` selects how overlapping invocations behave, mirroring
the RxJS flattening operators used by NgRx effects:

```csharp
[BlexEffect(Concurrency = BlexEffectConcurrency.Latest)]   // switchMap: new call cancels the previous
private async Task OnSearch(string query, CancellationToken ct)
{
    Results = await _api.SearchAsync(query, ct);
}
// generated: Task Search(string query)  +  void CancelSearch()
//            bool SearchIsLoading       +  Exception? SearchError
```

| Mode | Semantics | Typical use |
|---|---|---|
| `Parallel` (default) | all runs proceed concurrently | independent fetches |
| `Latest` | new run cancels the previous (`switchMap`) | type-ahead search |
| `Drop` | ignored while one is running (`exhaustMap`) | double-click-proof submits |
| `Queue` | runs strictly in arrival order (`concatMap`) | ordered writes |

Cancellation through the effect's own token (via `CancelXxx()` or `Latest` supersession) is a
normal outcome and never populates `Error`. A *foreign* `OperationCanceledException` - an
`HttpClient` timeout, or any cancellation when the effect has no token parameter - is a real
failure and is recorded in `Error`.

A store is `IDisposable`, and its container disposes it - a Blazor Server circuit ending, a test
scope closing. That cancels every in-flight effect and stops any pending debounce timer, so a
request started moments before teardown does not carry on resuming against a dead renderer. It
counts as the effect's own cancellation, so it populates no `Error`; the store itself stays usable,
because disposal ordering must not be able to strand a final persistence flush.

`Latest` stops a stale response from landing, but it still sends a request per keystroke.
`DebounceMilliseconds` waits for a quiet period first, so only the invocation the user actually
paused on runs at all - the two compose into the complete type-ahead recipe:

```csharp
[BlexEffect(DebounceMilliseconds = 300, Concurrency = BlexEffectConcurrency.Latest)]
private async Task OnTypeAhead(string query, CancellationToken ct)
    => Results = await _api.SearchAsync(query, ct);
```

The wait happens before anything observable, which is the point of generating it rather than
hand-rolling it: a superseded invocation never flips `IsLoading`, never clears `Error`, is never
offered to middleware and records no action. A spinner bound to `IsLoading` therefore does not blink
once per keystroke, and DevTools does not fill up with searches that never happened. The gate is per
effect, not per argument.

Every invocation is offered to the middleware/veto pipeline independently, including one started
while another async action of the same store is still awaiting. A vetoed invocation leaves no
trace: it never flips `IsLoading`, never clears `Error`, never supersedes a `Latest` run in flight
and never takes a `Queue` slot. Time-travel *recording*, however, is coalesced: overlapping runs on
one store are folded into the outermost in-flight action, so DevTools shows a single entry for
them. Use `Queue`, `Drop` or `Latest` - or separate stores - when each run needs its own entry.

## Normalized collections (entity adapter)

`BlexEntityAdapter<TEntity, TKey>` generates CRUD operations over an immutable, id-keyed
`BlexEntityState<TEntity, TKey>` - the same idea as Redux Toolkit's `createEntityAdapter`.

```csharp
[BlexStore(Name = "todos")]
public partial class TodoStore
{
    private static readonly BlexEntityAdapter<Todo, int> Adapter = new(t => t.Id);
    [BlexState] private BlexEntityState<Todo, int> _todos = Adapter.GetInitialState();

    [BlexComputed] private int ComputeRemaining() => Todos.All.Count(t => !t.Done);

    [BlexAction] private void OnUpsert(Todo todo) => Todos = Adapter.UpsertOne(Todos, todo);
    [BlexAction] private void OnToggle(int id)    => Todos = Adapter.UpdateOne(Todos, id, t => t with { Done = !t.Done });
    [BlexAction] private void OnRemove(int id)    => Todos = Adapter.RemoveOne(Todos, id);
}
```

`BlexEntityState` exposes `Ids`, `Entities`, `All`, `Count`, `Contains(id)`, `Find(id)`,
`TryGet(id, out entity)` (the unambiguous lookup for struct entities, where `default` is a
legitimate value) and the indexer `state[id]` (for the lookups where absence is a bug rather than a
case to handle), and
round-trips through JSON for snapshots and persistence. Every operation that changes nothing -
removing an absent id, upserting an empty sequence, an updater that returns an equal entity,
`SetAll` with an unchanged payload - returns the *same* instance, so it raises no notification and
records no action. Polling a list endpoint is therefore free while the data is unchanged. The
adapter also offers `AddMany`, `UpsertMany`, `UpdateMany`, `Map` (transform every entity),
`SetOne`/`SetMany`, `RemoveMany`, `RemoveAll` and `SetAll`, plus an optional sort comparer that
keeps `Ids` ordered after every operation:

```csharp
private static readonly BlexEntityAdapter<Todo, int> Adapter =
    new(t => t.Id, Comparer<Todo>.Create((a, b) => a.DueDate.CompareTo(b.DueDate)));
```

The adapter also exposes what it was built with - `Adapter.SelectId(entity)` and
`Adapter.SortComparer` - so a caller keys an entity the way the adapter does instead of repeating
the key expression at every call site, where a second copy can silently diverge.

`SetOne`/`SetMany` replace an entity only if its id is already present - unlike `UpsertOne`, they
never insert. That is what you want for a late save response that must not resurrect a row the user
deleted while it was in flight:

```csharp
[BlexEffect]
private async Task OnSave(Todo todo)
{
    var saved = await _api.PutAsync(todo);
    Todos = Adapter.SetOne(Todos, saved);   // applied only if the id is still there
}
```

A payload restored from storage is reconciled rather than trusted: ids with no matching entity are
dropped, entities missing from the id list are appended, and duplicate ids are collapsed - so a
hand-edited or partially written payload cannot throw mid-render.

## Persistence

Mark a store with `[BlexStore(Persist = true)]` and wire up a storage provider; the store is rehydrated
on startup and saved after every action.

```csharp
[BlexStore(Name = "settings", Persist = true)]
public partial class SettingsStore { [BlexState] private string _theme = "light"; ... }
```

```csharp
// Program.cs (Blazor WebAssembly)
builder.Services.AddBitBlexLocalStoragePersistence();   // or AddBitBlexSessionStoragePersistence()
```

`<BlexProvider>` restores persisted state on init. It also bridges to Blazor's
`PersistentComponentState` automatically (set `PersistComponentState="false"` to opt out), handing
prerendered state to the interactive render to avoid the prerender "double render" flicker. Under
Blazor Server prerendering (where JS interop is unavailable), hydration is automatically retried on
first render instead of crashing startup. For non-Blazor hosts, implement `IBlexStorage` and call
`AddBitBlexPersistence()`.

Persistence is production-hardened:

- **Corrupt data never breaks startup** - an unreadable payload is reported through `OnError`,
  discarded, and removed from storage.
- **A failed write is reported, not swallowed** - quota exhaustion, private mode or storage being
  disabled reaches `OnError` with source `"persistence"`, so the app can tell the user their
  changes are not being saved instead of pretending they were.
- **Debounce** - `options.DebounceInterval = TimeSpan.FromMilliseconds(300)` coalesces bursts of
  actions into one write (flushed on dispose, or on demand via `persistor.FlushAsync()`).
- **Versioning & migrations** - bump `options.Version` when a persisted store's shape changes and
  supply `options.Migrate` to upgrade (or discard) old payloads, zustand-persist style:

```csharp
builder.Services.AddBitBlexLocalStoragePersistence(options =>
{
    options.Version = 2;
    options.Migrate = (storeName, fromVersion, state) =>
    {
        if (storeName == "settings" && fromVersion < 2)
            state["Theme"] = "system";   // rename/upgrade old values
        return state;                    // return null to discard instead
    };
});
```

- **Restore write-back** - undo/redo and DevTools time-travel write the restored state back to
  storage, so a reload never resurrects the pre-restore state.
- **Ordered writes** - saves are serialized in dispatch order; a stale payload can't overwrite a
  newer one.
- **Partial persistence** - `options.Partialize` projects each snapshot on its way to storage, so
  transient fields (a search box's text, a cached response) never get written. Keys you omit come
  back as their declared defaults rather than being wiped. Return `null` to skip that store's write
  entirely.
- **Rehydration hooks** - `options.OnRehydrated` fires once per persistent store with the slice that
  was applied, or `null` when nothing was stored - which is how you tell a returning user from a
  first visit.
- **Deferred hydration** - `options.SkipHydration = true` starts saving without restoring; call
  `persistor.RehydrateAsync()` once sign-in resolves, or once the user confirms.
- **Hydration status** - `persistor.HasHydrated`, the `Hydrated` event and `await
  persistor.WhenHydratedAsync()` tell you whether a persisted store holds the user's data or its
  declared defaults, so a full cart never briefly renders as "0 items" (zustand's `hasHydrated`).
- **Merge hook** - `options.Merge` decides how a stored payload meets the live store when the
  default shallow merge isn't enough: a nested object combined key by key, or a value the live store
  must win on because it was assigned before hydration finished. Runs for cross-tab changes too.
- **Cross-tab sync** - `options.SyncAcrossTabs = true` applies state written by another tab as soon
  as it lands. Without it two open tabs quietly fight: each saves after every action, so whichever
  acted last wins and the other's changes vanish on its next reload. Incoming state is applied as a
  restore (no time-travel entry, and never written straight back, which would bounce it between
  tabs) and is migrated exactly as at startup. `options.OnExternalChange` reports it. Backed by the
  browser's `storage` event for `localStorage`; any storage can opt in by implementing
  `IBlexExternalStorageChange`.

```csharp
builder.Services.AddBitBlexLocalStoragePersistence(options =>
{
    options.Partialize = (storeName, state) =>
    {
        if (storeName == "search")
            state.Remove("Query");     // per-visit, never persisted

        return state;                  // return null to skip this store's write
    };

    options.OnRehydrated = (storeName, state) =>
    {
        if (state is null) _telemetry.FirstVisit();
    };
});
```

Until hydration finishes, a component bound to a persisted store renders *declared defaults* rather
than the user's data - so branch on it instead of rendering a number you do not have yet:

```razor
@inject BlexStatePersistor Persistor
@inject CartStore Cart

@if (!Persistor.HasHydrated)
{
    <CartSkeleton />                        @* not "0 items" - we do not know yet *@
}
else
{
    <CartSummary Total="Cart.Total" />
}

@code {
    protected override void OnInitialized() => Persistor.Hydrated += Rerender;

    private void Rerender() => _ = InvokeAsync(StateHasChanged);

    protected override void Dispose(bool disposing) => Persistor.Hydrated -= Rerender;
}
```

```csharp
// Or await it, when the decision is not a render:
await Persistor.WhenHydratedAsync();
```

## Cross-store coordination

React to one store's actions from elsewhere (e.g. trigger an effect on another store):

```csharp
manager.SubscribeTo<CounterStore>(ctx => { /* runs after each CounterStore action */ });
manager.SubscribeToAction("Increment", ctx => { ... });
manager.SubscribeAsync(async ctx => await otherStore.Reload());
```

A subscription answers "tell me when this changed". For "give me this value, cheaply, from several
places at once" - what `[BlexComputed]` does inside one store - `CreateSelector` lifts the
same memoization to the manager. It is lazy and shared: three components binding to one cross-store
total pay for one computation per action rather than three, and an action none of them read costs a
flag write:

```csharp
using var summary = manager.CreateSelector(() => $"{orders.Units} units, {cart.Total:C}");
var text = summary.Value;   // recomputed at most once per action, however many readers
```

## Middleware: observe and veto

Middleware sees every action after it applies (including its argument payload via `ctx.Args`), and
can veto an action before it runs - also based on the payload:

```csharp
builder.Services.AddBitBlexServices(options =>
{
    options.UseMiddleware(ctx => Console.WriteLine($"{ctx.QualifiedName}({string.Join(", ", ctx.Args)})"));
    options.UseFilter(ctx => !IsReadOnly);   // return false to cancel
    options.OnError = err => _logger.LogWarning(err.Exception, "[blex:{Source}] {Detail}", err.Source, err.Detail);
});
```

`UseMiddleware<T>()` resolves from DI (use it for anything holding per-user state - the instance and
delegate overloads live on the singleton options, so a Blazor Server circuit shares them with every
other circuit). Whichever kinds you mix, the pipeline runs in **registration order**, which matters
because the first veto short-circuits the rest. Middleware registered straight into the service
collection rather than through the options is appended after everything registered here.

A third hook fires when an action *body* throws - the counterpart to Pinia's `$onAction` `onError`,
and where a crash reporter belongs. An effect swallows its exception into `XxxError` so the UI can
render it, which means nothing else would ever hear about the failure:

```csharp
options.OnActionError = ctx => _logger.LogError(ctx.Exception, "[blex] {Action}", ctx.QualifiedName);

// or as a pipeline stage, alongside BeforeAction/OnAction:
public void OnActionError(BlexActionErrorContext ctx) => _telemetry.TrackException(ctx.Exception);

// or as an event, for code that holds the manager rather than the options:
manager.ActionFailed += ctx => _telemetry.TrackException(ctx.Exception);
```

An action calling another action - or an effect body calling one - sends the same exception out
through several dispatch frames. Only the innermost one, which names the action that actually threw,
reports it, so a crash reporter sees one nested failure once rather than once per frame.

The hook observes; it cannot handle. The exception continues on its way exactly as it would have, so
adding a reporter never changes what the application does. Cancellation is not a failure: an
`OperationCanceledException` from `CancelXxx()`, from a superseded `Latest` run, or from disposing a
store never reaches it - just as it never reaches `XxxError`.

`OnError` receives every non-fatal failure Blex isolates from the dispatch pipeline - without it
they go to `Console.Error`. Isolation is per handler and covers the whole observer surface:
`StateChanged`/`PropertyChanged` subscribers (including selector subscriptions and
`BlexComponentBase`), `manager.Subscribe(...)` handlers *and* their filters, raw
`ActionDispatched`/`StateRestored` handlers, `BlexHistory.Changed` and its filter, middleware,
persistence writes, restores, sanitizers, per-store `ResetAll` failures, and the prerender-to-
interactive handoff. One component throwing while it re-renders can never starve the subscribers
behind it, nor the persistence and undo/redo observers that run after them - and a store whose
prerendered slice fails to apply costs a second render, not a failed startup.

Store names key the global state tree, the DevTools slices *and* the persistence storage keys, so
they must be unique. Registering two stores under one name is reported through `OnError` rather
than silently letting them shadow each other - give one an explicit `[BlexStore(Name = "...")]`.

## Undo / redo

`BlexHistory` provides in-app undo/redo over the whole application state, independent of the
DevTools extension:

```csharp
builder.Services.AddBitBlexHistory();   // <BlexProvider> starts recording automatically
```

```razor
@inject BlexHistory History
<button @onclick="History.Undo" disabled="@(!History.CanUndo)">Undo @History.NextUndoLabel</button>
<button @onclick="History.Redo" disabled="@(!History.CanRedo)">Redo @History.NextRedoLabel</button>
```

`NextUndoLabel`/`NextRedoLabel` name the action about to be undone/redone (e.g. "Undo
counter/Increment"); `UndoCount`/`RedoCount` expose stack depths. When persistence is enabled,
undo/redo writes the restored state back to storage.

By default every action earns its own undo step, which is rarely what a user means once a text box
is involved - typing "hello" into a bound property would record five steps. Pass a filter
(redux-undo's `filter`) to decide what counts:

```csharp
// Direct property assignments record as "Set X" - typically one per keystroke.
builder.Services.AddBitBlexHistory(filter: ctx => !ctx.ActionName.StartsWith("Set "));
```

A filtered action still applies and still moves the present forward; it simply doesn't become a
separate step, so the next undo reverts it together with the recorded action before it. Undo
therefore never lands on a state the user never saw. A filter that throws fails open - the action is
recorded anyway and the failure is reported through `OnError`.

A filter alone leaves the filtered work with no boundary of its own, so the next recorded action
swallows it. `History.Checkpoint(label)` commits the current state as an undo step on demand - call
it where the user expects a boundary (a pause in typing, a field losing focus, a wizard step).
Consecutive checkpoints with nothing in between collapse into one, so a debounced "user stopped
typing" timer can call it freely.

```csharp
private void OnBlur() => History.Checkpoint("Edit title");
```

Where a filter removes an action from the stack entirely, `groupBy` (redux-undo's) keeps it but folds
it into the step already open, so a burst of related actions costs a single undo. Only *consecutive*
actions merge - a differently-keyed action, an ungrouped one, a checkpoint, an undo or a redo all
close the open group:

```csharp
builder.Services.AddBitBlexHistory(
    groupBy: ctx => ctx.ActionName.StartsWith("Set ") ? ctx.QualifiedName : null);
```

`Undo()`/`Redo()` are the one-step cases of `Jump(n)`. `JumpToPast`/`JumpToFuture` address the stacks
directly and `UndoLabels`/`RedoLabels` name the steps in navigation order, which is what a history
panel renders. Intermediate states are never applied, so a ten-step undo costs one render, one
storage write and one `Changed` raise rather than ten of each:

```razor
@for (var i = 0; i < History.UndoLabels.Count; i++)
{
    var steps = i + 1;
    <button @onclick="() => History.Jump(-steps)">↶ @History.UndoLabels[i]</button>
}
```

## Resetting

`store.ResetState()` returns one store to the state it had when it was registered;
`manager.ResetAll()` does it across every registered store - the usual sign-out move. Each store
resets as its own normal, vetoable, observable, persisted and undoable `ResetState` action, so
middleware can exempt individual stores and DevTools shows one entry per store. A store already
holding its initial state is skipped entirely: resetting an untouched form notifies nobody, writes
nothing and adds no undo step.

```csharp
manager.ResetAll();

// Keep one store out of it:
builder.Services.AddBitBlexServices(options => options.UseFilter(
    ctx => !(ctx.Store.Name == "theme" && ctx.ActionName == "ResetState")));
```

## Testing

`Bit.Blex.Testing` provides a zero-setup harness that records dispatched actions:

```csharp
using var harness = BlexTestHarness.For<CounterStore>();
harness.Store.Increment();
Assert.Equal(new[] { "Increment" }, harness.Log.Names);
Assert.Equal(1, harness.Snapshot()["Count"]!.GetValue<int>());

// Recorded actions include their argument payloads:
harness.Store.Add(5);
Assert.Equal(5, harness.Log.Last!.Args[0].Value);

// Await state conditions instead of sprinkling Task.Delay:
var load = harness.Store.LoadUser(42);
await harness.Store.WaitForAsync(() => !harness.Store.LoadUserIsLoading);
await load;
```

Cross-store coordination deserves tests too, and hand-wiring a manager for them defeats the point of
a harness. `Register` brings a second store onto the same manager - its actions land in the same log,
its slice in the same `State` tree, and it is disposed with the harness. `harness.Manager` is right
there for the wiring under test, and `manager.WaitForAsync` waits on a condition that spans stores
(re-checked after every action *and* every restore, where `store.WaitForAsync` watches one store):

```csharp
using var harness = BlexTestHarness.For<OrdersStore>();
var notifications = harness.Register(new NotificationsStore());

using var wiring = harness.Manager.SubscribeTo<OrdersStore>(
    ctx => notifications.Push($"order: {ctx.ActionName}"));

harness.Store.Place("Widget", 2);
Assert.Equal(new[] { "orders/Place", "notifications/Push" }, harness.Log.QualifiedNames);

await harness.Manager.WaitForAsync(() => notifications.Unread == 1);
```

An effect captures its exception into `XxxError` instead of propagating it, so a test that only
watches for a throw sees nothing at all. `Failures` records every action whose body threw, with the
arguments it was called with:

```csharp
using var harness = BlexTestHarness.For<DataStore>();

await harness.Store.Load("orders");        // an effect that fails
Assert.NotNull(harness.Store.LoadError);

var failure = Assert.Single(harness.Failures);
Assert.Equal("data/Load", failure.QualifiedName);
Assert.Equal("orders", failure.Args[0].Value);
```

A store is rarely alone in production, so the harness can bring the pipeline with it - test the
store together with the guard rails that wrap it:

```csharp
using var harness = BlexTestHarness.WithFilter<CounterStore>(_ => false);
harness.Store.Increment();
Assert.Equal(0, harness.Store.Count);   // vetoed
Assert.Empty(harness.Log.Actions);

using var audited = BlexTestHarness.WithMiddleware<CounterStore>(new AuditMiddleware());
```

## Trimming and AOT

Store snapshots (`SerializeState`/`DeserializeState`, persistence, DevTools) go through
`System.Text.Json`. By default that is the reflection-based serializer, whose metadata a trimmed
publish - the Blazor WebAssembly release default - is free to strip for types it cannot see being
used. Point the shared options at a source-generated context covering your state types and every
generated snapshot uses it instead:

```csharp
[JsonSerializable(typeof(CartLine))]
[JsonSerializable(typeof(BlexEntityState<Todo, int>))]
internal partial class AppJsonContext : JsonSerializerContext;

// Program.cs, before any store is resolved or snapshotted:
BlexJson.Configure(o => o.TypeInfoResolver = AppJsonContext.Default);
```

Register the *state field* types, not the store classes - the store itself is never serialized, only
the value of each `[BlexState]` field. Primitive-only stores need nothing beyond the
built-in resolvers.

## Compile-time diagnostics

The generator validates store shapes and fails fast with precise errors instead of emitting broken
code: `BLEX001` store not partial · `BLEX002/003` underivable action/computed names ·
`BLEX004` computed with parameters · `BLEX005` generated-member collisions (including against
your own members and `BlexStoreBase`) · `BLEX006` nested/generic/static stores · `BLEX007` non-async
effects · `BLEX008` static/readonly members · `BLEX009` `Latest` effect without a
`CancellationToken` (warning) · `BLEX010` `async void` actions · `BLEX011` discarded action
return values (warning) · `BLEX012` state field/property name conflicts · `BLEX013` by-ref
parameters · `BLEX014` generic action/effect methods · `BLEX015` record stores ·
`BLEX016` conflicting base class · `BLEX017` `void` computed methods · `BLEX018` a Blex member
attribute used outside a `[BlexStore]` class (warning) - the one mistake that otherwise
compiles cleanly and silently generates nothing at all · `BLEX019` negative
`DebounceMilliseconds`.

## Time-travel debugging

1. Install the [Redux DevTools](https://github.com/reduxjs/redux-devtools) browser extension.
2. Run the app and open DevTools - you'll see an instance named after `DevToolsName`.
3. Every action streams in with its argument payload and the resulting state tree.
4. Use the slider / jump buttons to rewind and replay your application state live.

Under the hood the `Bit.Blex.Blazor` JS bridge talks to `window.__REDUX_DEVTOOLS_EXTENSION__`,
sends each action via `send(action, state)`, and applies `JUMP_TO_STATE` / `JUMP_TO_ACTION` /
`ROLLBACK` / `RESET` / `COMMIT` / `IMPORT_STATE` messages back onto the stores.

The monitor's two recording controls are honoured as well, and they differ in kind. **Pause
recording** is about the timeline: actions still apply and still reach middleware and subscribers,
they are simply not streamed, so you can set up the state you want to inspect without filling the
monitor with noise. **Lock changes** is about the app: every action is vetoed until it is unlocked,
while time-travel restores keep working - which is the point. Both are readable as
`manager.IsDevToolsPaused` / `manager.AreChangesLocked`, and disconnecting the bridge clears them, so
a monitor closed while locked cannot leave the application permanently frozen.

For production, set `<BlexProvider EnableDevTools="false">` to disable the connection entirely, or
redact sensitive values from the monitor with sanitizers:

```csharp
builder.Services.AddBitBlexServices(options =>
{
    options.RedactDevToolsKeys("token", "password");      // replace matching keys with <redacted>
                                                          // (applies to action payloads too)
    options.DevToolsActionSanitizer = label => label;     // or rewrite action labels
});
```

`RedactDevToolsKeys` is additive and composes: calling it again *widens* the redaction set rather
than replacing it, and it wraps any `DevToolsStateSanitizer` assigned before it. Register redaction
last - assigning `DevToolsStateSanitizer` afterwards replaces it outright.

## .NET MAUI

All runtime packages ship plain `net8.0`/`net9.0`/`net10.0` builds that resolve from every MAUI platform
target (`net8.0-android`, `net8.0-ios`, `net8.0-maccatalyst`, `net8.0-windows`) - no workloads
involved on the library side.

### Blazor Hybrid

MAUI Blazor Hybrid apps use `Bit.Blex` + `Bit.Blex.Blazor` exactly like any Blazor app: `AddBitBlexServices(...)` in
`MauiProgram.cs` and `<BlexProvider>` wrapping the root component inside the `BlazorWebView`.
There is no browser extension inside a WebView, so the DevTools bridge detects its absence and
disables itself; set `<BlexProvider EnableDevTools="false">` to skip loading it entirely.
`AddBitBlexLocalStoragePersistence()` works (the WebView provides `localStorage`), or add
`Bit.Blex.Maui` and call `AddBitBlexPreferencesPersistence()` to persist to OS-native app preferences
instead of the WebView profile.

### Native (XAML)

`Bit.Blex.Maui` makes stores first-class XAML citizens. Every store implements
`INotifyPropertyChanged` - raised with an empty property name ("all properties changed"), so
bindings to `[BlexComputed]` properties stay fresh too - which means XAML can bind directly to
generated state, computed and effect-lifecycle properties:

```csharp
// MauiProgram.cs
builder.UseBitBlex();                          // manager + startup initializer
builder.AddBitBlexStore<CounterStore>();
builder.AddBitBlexPreferencesPersistence();    // OS-native Preferences storage
```

Register through the `MauiAppBuilder` extensions rather than `builder.Services`: a native MAUI app
has no DI scopes, so these register Blex as **singletons**, which is both the honest lifetime and
what keeps the wiring legal if you build the container with `ValidateScopes` enabled (it rejects
resolving a scoped service from the root provider - exactly what the startup initializer must do).
The core `builder.Services.AddBitBlexStore<T>()` still works and still defaults to scoped for Blazor;
pass `ServiceLifetime.Singleton` if you prefer to call it directly.

```xml
<ContentPage ... x:DataType="stores:CounterStore">
    <VerticalStackLayout>
        <Label Text="{Binding Count}" />
        <Label Text="{Binding DoubleCount}" />
    </VerticalStackLayout>
</ContentPage>
```

```csharp
public partial class MainPage : ContentPage
{
    private readonly CounterStore _store;

    public MainPage(CounterStore store)
    {
        InitializeComponent();
        BindingContext = _store = store;
    }

    private void OnIncrementClicked(object? sender, EventArgs e) => _store.Increment();
}
```

`UseBitBlex()` registers a startup initializer that mirrors what `<BlexProvider>` does in Blazor:
when `MauiApp.Build()` runs it attaches every `AddBitBlexStore` store to the manager, rehydrates
persisted state from `Preferences`, and starts `BlexHistory` recording (when registered) - all
before the first page appears. `AddBitBlexPreferencesPersistence()` supports the same
debounce/versioning/migration options as the browser-storage providers, and an unreadable
payload is reported through `options.OnError` and discarded rather than crashing startup.

To persist somewhere else (files, SQLite), register your own `IBlexStorage` before calling
`AddBitBlexPreferencesPersistence()` - the first registration wins, so give it the same lifetime as
the rest of your Blex registrations. An `IBlexStorage` that completes asynchronously should be
hydrated from app code (`await persistor.StartAsync()`) instead of relying on the synchronous
startup initializer.

If the startup initializer cannot resolve its services (typically a lifetime mismatch under
`ValidateScopes`), it reports through `options.OnError` and lets the app start rather than failing
`MauiApp.Build()`.

## Projects

| Project | Description |
|---|---|
| `Bit.Blex` | Core runtime (no JS dependency): `BlexStoreBase`, attributes, dispatch, middleware, persistence, entity adapter, undo/redo, the `BlexManager` cross-store manager. |
| `Bit.Blex.Generators` | Roslyn incremental source generator. |
| `Bit.Blex.Blazor` | Blazor integration: `BlexComponentBase`, `<BlexProvider>`, browser-storage persistence, Redux DevTools bridge (TypeScript in `Scripts/`, compiled into `wwwroot/` on build). |
| `Bit.Blex.Maui` | .NET MAUI integration: `UseBitBlex()` startup initializer, `Preferences`-backed persistence, XAML-bindable stores. |
| `Bit.Blex.Testing` | Test harness and assertions (`BlexTestHarness`, `BlexActionLog`). |
| `Bit.Blex.Demo` | Documentation website (`Client` + prerendering `Server` host): every feature explained with a live, runnable demo. |
| `Samples/Bit.Blex.Samples.Wasm` | Minimal Blazor WebAssembly sample (Counter, Todos, Weather). |
| `Tests/Bit.Blex.Tests` | MSTest tests for the runtime, the Blazor/MAUI integrations and the generated code, on every target framework. |
| `Tests/Bit.Blex.Tests.Generators` | Generator-driver tests: all `BLEX` diagnostics + emission snapshots. |
| `Tests/Bit.Blex.Tests.Mcp` | MCP server tests, over the real protocol against the demo server's own `Program.cs`. |
| `Tests/Bit.Blex.Tests.E2E` (+ `Tests/Bit.Blex.Tests.Harness*`) | Playwright suites in every Blazor render mode, Blazor Hybrid (WebView2) and trimmed/AOT publishes. See its [README](Tests/Bit.Blex.Tests.E2E/README.md). |
| `Tests/Bit.Blex.Benchmarks` | BenchmarkDotNet suites (dispatch, fan-out, serialization, entity adapter). |

Inside this repository the demo, sample, tests and benchmarks reference the generator project
directly as an analyzer (`OutputItemType="Analyzer" ReferenceOutputAssembly="false"`).

## Build & test

```bash
dotnet build src/Blex/Bit.Blex.slnx
cd src/Blex/Tests/Bit.Blex.Tests && dotnet test
cd src/Blex/Tests/Bit.Blex.Tests.Generators && dotnet test
cd src/Blex/Tests/Bit.Blex.Tests.Mcp && dotnet test
cd src/Blex/Tests/Bit.Blex.Tests.E2E && dotnet test              # needs Playwright Chromium, see its README
dotnet run --project src/Blex/Bit.Blex.Demo/Server               # documentation site
dotnet run --project src/Blex/Samples/Bit.Blex.Samples.Wasm      # minimal sample app
dotnet run -c Release --project src/Blex/Tests/Bit.Blex.Benchmarks
```

Run `dotnet test` from inside `src` so `src/global.json`'s Microsoft.Testing.Platform runner setting
applies.
