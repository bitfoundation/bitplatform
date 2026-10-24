# bit Blex

Lightweight, source-generator-powered **reactive state management for Blazor and .NET MAUI** - with **Redux DevTools time-travel** built in.

Blex fills a real gap in the Blazor ecosystem. Fluxor is the de-facto Redux library but is widely criticized for boilerplate (separate Action / Reducer / Effect / Feature classes per operation) and for having no first-class DevTools time-travel. Blex keeps the good parts of the Flux model - a single observable state tree, named actions, middleware - while a Roslyn source generator removes the ceremony and a tiny JS bridge wires you straight into the Redux DevTools browser extension.

Blex's documentation website lives in [`Demo`](Demo/) - run the
[Server](Demo/Server/) project (`dotnet run`) for the full site: every feature explained
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
| Typed actions | the action class | generated `Store.Actions.X` descriptors (typed payloads, match, dispatch) |
| Action payloads in DevTools/middleware | manual | automatic (`ctx.Args`, DevTools payload) |
| Dispatch an action as data | yes | `manager.DispatchAsync("store/Action", payload)` (DevTools dispatcher, replay) |
| State only changes through actions | ✓ (immutable state, reducers) | opt-in: `[BlexStore(EnforceActions = true)]` (compile-time + runtime) |
| Derived state | manual / selectors | `[BlexComputed]` (memoized) |
| Derived state *across* stores | manual | `manager.CreateSelector(...)` (lazy, shared, memoized) |
| Async side-effects | Effect classes | `[BlexEffect]` (auto loading/error) |
| Effect cancellation / concurrency | manual | `CancellationToken` + `Latest`/`Drop`/`Queue` + `DebounceMilliseconds` |
| Effect retries / timeouts | manual | `RetryCount` (exponential backoff) + `TimeoutMilliseconds` |
| Server-state cache (queries, mutations, optimistic rollback) | ✗ | `BlexQueryClient` / `UseQuery` / `<BlexQueryView>` (TanStack Query model, prerender handoff) |
| Failed action leaves no partial state | ✗ | opt-in `[BlexStore(RollbackOnError = true)]` |
| Safe changes from timers / SignalR on Blazor Server | manual `InvokeAsync` | `manager.InvokeAsync` + off-dispatcher changes reported |
| Logging / OpenTelemetry | ✗ | `options.UseLogging()`, `ActivitySource`/`Meter` `"Bit.Blex"` |
| Ad-hoc batched mutations | ✗ | `store.Batch(name, ...)` (Pinia `$patch`-style) |
| Reset to initial state | manual | `store.ResetState()` |
| Middleware | yes | yes (with veto/filter hooks + payload access) |
| Action failure hook | ✗ | `OnActionError` (Pinia `$onAction`'s `onError`) |
| Granular re-render | manual selectors | selector `Subscribe(...)` (+ prev/current, fireImmediately, `BlexComparers`) |
| Selective rendering without a base class | manual (`IStateSelection` + `StateHasChanged`) | `<BlexSelect>` / `<BlexView>` |
| Rx interop | ✗ | `store.Observe(...)` / `manager.ObserveActions()` as `IObservable<T>` |
| Several instances of one store | ✗ | scoped stores: `manager.CreateScope(...)` / `<BlexScope>` |
| Normalized collections | manual | `BlexEntityAdapter` / `BlexEntityState` (+ sorting, `UpdateMany`, `Map`) |
| Persistence | 3rd-party | `[BlexStore(Persist = true)]` + debounce + versioning/migrations + hydration status + `[BlexState(Persist = false)]` |
| Undo / redo | ✗ | `BlexHistory` (in-app, labeled entries, `Jump`, `groupBy`, scoped to chosen stores) |
| Redux DevTools time-travel | ✗ | ✓ built in (+ state/action sanitizers, dispatcher) |
| Error isolation hook | ✗ | `options.OnError` |
| Test helpers | ✗ | `Bit.Blex.Testing` harness (multi-store `Register`, `Failures`, `WaitForAsync`, typed log, replay, fake clock) |
| Boilerplate | high | minimal (generated) |

## Install

```bash
dotnet add package Bit.Blex             # runtime, source generator and Blazor integration (BlexProvider, DevTools bridge)
dotnet add package Bit.Blex.Maui        # .NET MAUI integration (XAML binding, Preferences persistence)
dotnet add package Bit.Blex.Testing     # optional, for unit tests
```

The runtime packages target **net8.0, net9.0 and net10.0**. `Bit.Blex` is all a Blazor app needs:

- **The source generator** that writes the store plumbing ships inside it, under `analyzers/dotnet/cs`, so it
  adds nothing to your app's output and there is no separate generator package to install. NuGet does not
  flow a package's analyzers to projects that only get it transitively, so reference `Bit.Blex` directly
  from every project that declares `[BlexStore]` classes.
- **The Blazor integration** - `<BlexProvider>`, `BlexComponentBase`, browser-storage persistence and the
  Redux DevTools bridge - lives in the `Bit.Blex.Blazor` namespace of the same assembly. Its two small JS
  modules are static web assets served from `_content/Bit.Blex/`, imported on demand by browser-storage
  persistence and the DevTools bridge.

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
accessors, the public `Increment()`/`SetStep(int)`/`Reset()` action wrappers, typed descriptors of
those actions (`CounterStore.Actions.Increment`, ...), JSON snapshot support and the `BlexStoreBase`
base type.

### Conventions

- **State**: `[BlexState] private T _foo;` → public reactive property `Foo`.
- **Computed**: `[BlexComputed]` on a parameterless `ComputeXxx()` / `GetXxx()` method → memoized property `Xxx`,
  invalidated by a write to state it reads - the generator follows each body, through other computeds too. One
  that reads through something it cannot follow (a helper method, a service field, `this` passed along) is
  invalidated by every write.
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
- **Typed actions**: every action and effect also gets a descriptor in the nested `Actions` class -
  see [Typed actions](#typed-actions).

### Enforcing actions

Direct assignment is convenient, but it lets any component that holds the store change it - as an
anonymous `Set Count` rather than a named, deliberate action. `EnforceActions` closes that door, the way
Vuex's strict mode and MobX's `enforceActions` do:

```csharp
[BlexStore(Name = "cart", EnforceActions = true)]
public partial class CartStore
{
    [BlexState] private decimal _total;

    [BlexAction] private void OnAdd(decimal price) => Total += price;
}

cart.Add(9.99m);    // fine
cart.Total = 0;     // CS0272: the setter is private
```

The generated setters become `private`, so an assignment from outside the store does not compile. The
store's own code is policed at runtime: an assignment it makes outside any action throws once the store is
registered (a constructor setting up initial state is fine). Restores, `ResetState` and effects are
unaffected. Two-way binding needs a setter, so bind through an action instead:
`<input @bind:get="Store.Query" @bind:set="Store.SetQuery" />`.

### When an action throws

By default an action behaves like the C# method it is: what it changed before the throw stays changed and,
since it changed state, is notified, recorded and persisted. `[BlexStore(RollbackOnError = true)]` makes
every synchronous action a transaction instead - a throw puts the store's fields back, and nothing is
notified, recorded, persisted or added to the undo history; the exception still reaches the caller and
`OnActionError`. Each action level is its own transaction, so a nested action whose failure the caller
catches is undone while the caller's own changes stay. An action of another store dispatched from the body
stands - only this store is put back - and the undo steps it recorded hold this store's rolled-back state,
never the undone one. The snapshot is a shallow copy of the `[BlexState]`
fields (one small array per action, no serialization): it undoes assignments, covers this store only, and
leaves async actions alone - they have already shown the UI what they changed before their first `await`.

```csharp
[BlexStore(Name = "ledger", RollbackOnError = true)]
public partial class LedgerStore
{
    [BlexState] private decimal _balance;

    [BlexAction]
    private void OnWithdraw(decimal amount)
    {
        Balance -= amount;
        if (Balance < 0) throw new InvalidOperationException("Insufficient funds.");   // Balance is put back
    }
}
```

Across stores, `manager.Transaction(...)` makes the same promise for a unit of work: if the body throws,
every store it changed is put back the way it was when the transaction began. The actions inside run and
record as usual; the putting back is one restore, like an undo - subscriptions re-evaluate once,
persistence writes the restored state back, and the undo steps those actions recorded no longer hold what
was taken back. Every store is serialized when it begins, so keep it for multi-store operations;
synchronous only, like `RollbackOnError`:

```csharp
manager.Transaction(() =>
{
    inventory.Reserve(sku, quantity);
    cart.Add(sku, quantity);
    wallet.Charge(price * quantity);   // throws: the reservation and the cart line are taken back too
});
```

A `[BlexComputed]` value read inside an action already reflects the action's earlier writes: the
notification waits for the action to finish, but invalidation happens at the write, so validation after a
write never reads a value memoized before it.

### Catching in-place mutations

`Items.Add(item)` assigns nothing, so no subscriber, DevTools entry, undo step or storage write ever sees
it. `options.DetectInPlaceMutations = true` reports every action that changed its store's state without
assigning a property, through `OnError` (source `"mutation"`) with the fix in the message. It serializes
the store around each action - turn it on in development only.

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

### Threads and the dispatcher

Stores are not synchronized, and neither is the dispatch pipeline: a Blazor Server circuit and a
BlazorWebView keep them consistent by running every event handler, lifecycle method and render on one
dispatcher. A `System.Threading.Timer` callback, a SignalR `HubConnection.On` handler, a `Task.Run` body or
a continuation after `ConfigureAwait(false)` races all of them when it changes a store.
`<BlexProvider>` binds the renderer's dispatcher to `BlexManager.Dispatcher`, and `manager.InvokeAsync`
gets there from a service that has no component to call `InvokeAsync` on:

```csharp
hub.On<Order>("orderPlaced", order => manager.InvokeAsync(() => orders.Add(order)));
```

A store changed outside the dispatcher anyway is reported through `OnError` (source `"dispatcher"`, the
store's name as the detail). The change still applies; the report points at a race that would otherwise
surface only as rare corruption in production. `options.ReportOffDispatcherChanges = false` turns it off.
In bUnit tests that render `<BlexProvider>`, the test thread is outside the renderer's dispatcher just as a
timer would be: change stores through `cut.InvokeAsync(...)` (as bUnit recommends for any state change), or
turn the report off for the suite.
Blazor WebAssembly is single-threaded and never reports; outside Blazor nothing is checked unless you assign
an `IBlexDispatcher`.

## Typed actions

An action is a method call to the code that dispatches it, but everything else that cares about it -
a subscriber, a filter, a test, DevTools - sees it as data. The generator gives each store a nested
`Actions` class with one typed descriptor per action and effect, so that data is typed too:

```csharp
[BlexStore(Name = "orders")]
public partial class OrdersStore
{
    [BlexState] private int _units;

    [BlexAction] private void OnClear() { ... }                   // Actions.Clear : BlexAction<OrdersStore>
    [BlexAction] private void OnAdd(int units) { ... }            // Actions.Add   : BlexAction<OrdersStore, int>
    [BlexAction] private void OnPlace(string name, int quantity)  // Actions.Place : BlexAction<OrdersStore, (string Name, int Quantity)>
        { ... }
}
```

A single parameter is the payload as is; several become a tuple named after the parameters. Match an
action by its descriptor instead of a string - a rename then breaks the build rather than silently
ending a subscription - and read its payload as the type the action takes:

```csharp
using var sub = manager.SubscribeToAction(OrdersStore.Actions.Place,
    (orders, order) => notifications.Push($"{order.Quantity} x {order.Name}"));

options.UseFilter(OrdersStore.Actions.Add, (orders, units) => units is > 0 and <= 100);

if (OrdersStore.Actions.Place.TryGetPayload(ctx, out var order)) { ... }   // in any middleware or subscriber
await OrdersStore.Actions.Place.InvokeAsync(orders, ("lamp", 2));           // dispatch it
```

The same descriptors make every action dispatchable by its qualified name with a JSON payload - which
is what the [DevTools dispatcher](#time-travel-debugging) and [replay in tests](#testing) are built on.
It runs through the generated wrapper, so filters, middleware and DevTools see an ordinary action:

```csharp
await manager.DispatchAsync("orders/Place", new JsonObject { ["name"] = "desk", ["quantity"] = 1 });
await manager.DispatchAsync("orders/Set Units", new JsonObject { ["Units"] = 0 });   // a state assignment
await manager.DispatchAsync("orders/ResetState");
```

An optional parameter the payload leaves out takes its declared default, as in a direct call.
`store.ActionDescriptors` lists everything a store can dispatch this way. A store that already has a
member called `Actions` keeps compiling and goes without the typed class (`BLEX020`). Since the name is
what identifies an action here, two actions dispatched under one name - two display labels alike, or a
label such as `"Set Count"` that a direct assignment already records - are a build error (`BLEX021`).

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
subscription reporting a value the application no longer holds - and after the changes no action
records: an effect's `IsLoading`/`Error`, a rollback. The callback runs after middleware and DevTools
have seen the action behind the change, so a follow-up it dispatches is recorded after its cause; what an
async action changes is therefore delivered when that action completes (components and `CreateSelector`
see it at once).

A projection that returns a *collection* defeats the point on its own: `() => store.Items.Where(...)`
builds a new object every time it runs, so the default reference comparison always reports a change.
`BlexComparers` supplies the content comparers that fix it (zustand's `shallow`):

```csharp
Subscribe(Store, () => Store.Todos.All.Where(t => !t.Done), BlexComparers.Sequence<Todo>());
using var sub = store.Subscribe(() => store.SelectedIds, Refresh, BlexComparers.Set<int>());
```

### Selective rendering in markup

A component subscribing to a store re-renders as a whole. `<BlexSelect>` narrows that to a fragment:
only its content re-renders, and only when the selected value changes - the host component neither
subscribes nor re-renders, so it does not need to inherit `BlexComponentBase` at all (useful when it
already has a base class of its own):

```razor
<header>
    @* the page around it never re-renders for the cart *@
    <BlexSelect Store="Cart" Selector="() => Cart.Total" Context="total">
        <strong>@total.ToString("C")</strong>
    </BlexSelect>
</header>
```

The selector may capture the host's parameters (an id, a filter): it is re-read whenever the host
re-renders. `Comparer` takes a `BlexComparers` comparer for collection projections. For content that
follows a whole store, `<BlexView Store="Cart">` (or `Stores="[Cart, Profile]"`) re-renders its content
on every change of those stores.

### Throttling renders

State that changes many times a second - a live feed, a progress counter, a drag - would re-render the
components following it that often; on Blazor Server each render is a diff sent over the wire. A
`RenderInterval` caps it (Fluxor's `MaximumStateChangedNotificationsPerSecond`): the first change after a
quiet interval still renders at once, and the changes within the interval collapse into one trailing
render of the latest state, so the last change is never missed. It is a parameter of `<BlexSelect>` and
`<BlexView>` and a property of `BlexComponentBase`; renders for any other reason (parameters, events) are
not affected. Waits run on the manager's `TimeProvider`.

```razor
<BlexView Store="Ticker" RenderInterval="TimeSpan.FromMilliseconds(100)">   @* at most ten renders a second *@
    @Ticker.Price
</BlexView>
```

```csharp
protected override void OnInitialized()
{
    RenderInterval = TimeSpan.FromMilliseconds(100);
    Subscribe(Ticker);
}
```

### Observables

For code that composes change streams with Reactive Extensions, `Observe` and `ObserveActions` return the
BCL's `IObservable<T>` - no Rx dependency in Blex, but every Rx operator accepts them:

```csharp
store.Observe(() => store.Query)          // the current value, then each distinct change
     .Throttle(TimeSpan.FromMilliseconds(300))
     .Subscribe(query => store.Search(query));

manager.ObserveActions().Where(OrdersStore.Actions.Place.Matches).Subscribe(ctx => ...);
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
emits a `CancelXxx()` method (on a debounced effect it also cancels the invocation still waiting out its
quiet period, and a debounced effect without a token gets one for that alone). `Concurrency` selects how
overlapping invocations behave, mirroring
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

### Retries and timeouts

Networks fail transiently. `RetryCount` runs a failed body again, waiting `RetryDelayMilliseconds`
(default 1000) before the first retry and doubling the wait before each one after it, up to 30 seconds.
`TimeoutMilliseconds` bounds every attempt: one that overruns has its token cancelled and fails with a
`TimeoutException`, which is retried like any other failure.

```csharp
[BlexEffect(RetryCount = 3, RetryDelayMilliseconds = 500, TimeoutMilliseconds = 10_000)]
private async Task OnLoadOrders(CancellationToken ct)
    => Orders = await _api.GetOrdersAsync(ct);
```

The attempts are one run: `XxxIsLoading` stays on across them, the action is recorded once, and only the
final failure lands in `XxxError` and reaches `OnActionError`. Cancellation through the effect's own
token (`CancelXxx()`, a superseding `Latest` run, disposal) is never retried. Waits run on the manager's
`TimeProvider`, and a replay skips them. A retry runs the body from the top, so assign results once they
have arrived rather than accumulating them. A timeout needs a `CancellationToken` parameter (`BLEX023`):
the token is how an overrunning attempt is told to stop.

Every invocation is offered to the middleware/veto pipeline independently, including one started
while another async action of the same store is still awaiting. A vetoed invocation leaves no
trace: it never flips `IsLoading`, never clears `Error`, never supersedes a `Latest` run in flight
and never takes a `Queue` slot. Each invocation is also *recorded* on its own, when it ends, with
what its own flow changed: two overlapping `Parallel` runs are two entries in DevTools, the
middleware's `OnAction` and the undo history, and so is an action handled while an effect awaits.
Only an action dispatched from inside another one's body is part of it.

## Server state (queries)

Stores hold state the app *owns*. A server's answer is different - a copy of someone else's state with a
shelf life - and wants a cache rather than a store: fetched once and shared by every component that shows
it, refreshed in the background once stale, refetched after a change, dropped when nobody has looked at it
for a while. `BlexQueryClient` (namespace `Bit.Blex.Query`, same package) is that cache - the model TanStack
Query made standard, in idiomatic C#. It is kept out of the state tree, persistence, history and DevTools on
purpose: undoing or persisting a server copy only brings back a stale one.

```csharp
builder.Services.AddBitBlexQueries(options => options.StaleTime = TimeSpan.FromSeconds(30));
```

```razor
@inherits BlexComponentBase
@inject TodoApi Api

@if (_todo.IsLoading) { <Spinner /> }
else if (_todo.Data is { } todo) { <h2>@todo.Title</h2> }
else if (_todo.Error is { } error) { <p>@error.Message</p> }

@code {
    [Parameter] public int Id { get; set; }
    private BlexQuery<Todo> _todo = default!;

    protected override void OnParametersSet()
        => _todo = UseQuery(["todos", Id], ct => Api.GetTodoAsync(Id, ct));
}
```

A key is an ordered list of parts, general to specific. Call `UseQuery` from `OnParametersSet`, before any
`await`: a call site keeps an observer per key it asks for, and once a parameter pass is over it releases the
keys it stopped asking for - so a key that follows a parameter is swapped, and one call in a loop over items
keeps one observer per item. `<BlexQueryView TData Key Fetch>` does the same from markup
(`LoadingContent`, `ErrorContent`, `ChildContent` with the data). An observer reports `Data`, `Status`
(`Pending`/`Error`/`Success`) and, independently, `FetchStatus` (`Idle`/`Fetching`) - so `IsLoading` (nothing
to show yet) and `IsRefetching` (data shown, newer on its way) differ - plus `IsStale`, `DataUpdatedAt`,
`FailureCount`/`FailureReason`, `RefetchAsync()` and `WhenSettledAsync()`.

- **One request per key**: observers that mount while a fetch runs join it. An explicit refetch or an
  invalidation cancels a running fetch and starts over, and only the latest fetch commits - a slow response
  never overwrites a newer one. A failed refetch keeps the data it had alongside the error.
- **Freshness and lifetime** (`BlexQueryOptions`, defaults from `AddBitBlexQueries`): `StaleTime` (0),
  `GcTime` (5 min), `RetryCount`/`RetryDelay` (3, 1 s, exponential to 30 s), `Enabled` (dependent queries),
  `RefetchInterval` (polling), `RefetchOnMount`/`RefetchOnFocus`, `KeepPreviousData` (paging without a
  loading flash, flagged `IsPlaceholderData`). Every wait runs on the manager's `TimeProvider`; timers run on
  its `Dispatcher`.
- **Invalidation and direct access**: `InvalidateAsync(["todos"])` covers every key starting with those
  parts (`exact: true` for one), refetching the observed ones; an unobserved one keeps a fetch it already had
  running, but its answer stays stale, so the next observer fetches again. `SetQueryData`, `GetQueryData`,
  `Cancel`, `Remove`/`Clear` (an observer still watching a removed query starts over on a fresh one - the
  sign-out move). `FetchAsync` is the read-through for effects and services - it throws
  `OperationCanceledException` when its fetch is cancelled rather than hand back the data from before;
  `PrefetchAsync` warms the cache.
- **Paged and infinite queries**: `UseInfiniteQuery` / `client.InfiniteQuery(key, fetchPage, initialPageParam,
  getNextPageParam)` keeps the pages loaded so far as one cached query - `Pages`, `HasNextPage`,
  `FetchNextPageAsync()`, `IsFetchingNextPage` (TanStack's `useInfiniteQuery`, forward only).
  `getNextPageParam` returns `null` when there is no next page, so use a nullable param type (`int?`). A
  refetch loads every page again, in order; the next page waits for a running fetch and follows the pages it
  brings, and asking twice while it loads costs one request. A failed page keeps the ones before it.
- **Offline**: while `client.IsOnline` is `false`, fetches and mutations wait - `FetchStatus.Paused` /
  `IsPaused`, not failed - and go on once the network returns, rather than spending their retries on
  requests that cannot succeed; a paused mutation's optimistic update shows meanwhile (TanStack's network
  mode). `<BlexProvider>` follows the browser's `offline`/`online` events where requests leave from the
  device (WebAssembly, Hybrid) - not on Blazor Server, whose requests leave from the server. Native MAUI calls
  `NotifyOffline()`/`NotifyReconnected()` from `Connectivity.ConnectivityChanged`. `NetworkMode = Always` (per
  query, per mutation or as the client default) is for data that does not come from the network.
- **Across tabs**: `options.SyncAcrossTabs = true` passes key invalidations on to the app's other tabs over a
  `BroadcastChannel` (through `<BlexProvider>`, Blazor Server included), so a change saved in one tab refetches
  the lists every other tab shows. Keys travel as their text (`["todos",42]`) and match part by part, prefixes
  included; invalidations by predicate stay local.
- **Focus and reconnect**: `<BlexProvider>` listens for the tab becoming visible again and the browser coming
  back online, and refetches the stale queries an observer with `RefetchOnFocus` watches. Outside a browser
  (native MAUI), call `NotifyFocused()`/`NotifyReconnected()` where your app learns of it.

Mutations send changes, with automatic optimistic rollback:

```csharp
_add = Watch(queries.Mutation<string, Product>(
    (name, ct) => api.AddProductAsync(name, ct),
    new()
    {
        OnMutate = name => queries.Optimistic<IReadOnlyList<Product>>(
            ["products"], products => [.. products ?? [], new Product(-1, name, 0)]),
        Invalidates = ["products"],
    }));

await _add.MutateAsync("Desk");   // never throws; IsPending / IsSuccess / Error
```

A failure rolls the optimistic value back - but only while the cache still holds it; when something newer
has landed meanwhile (another mutation's optimistic value, a refetch) the key is invalidated instead, which
keeps overlapping optimistic updates from corrupting each other. The mutation stays pending until the
invalidated queries have refetched, so "saved" never shows next to the list from before the save.
`Watch(...)` re-renders a `BlexComponentBase` on any `INotifyPropertyChanged` source.

**Prerendering**: `<BlexProvider>` hands the queries a prerender fetched to the interactive render through
`PersistentComponentState`. Await `query.WhenSettledAsync()` while prerendering so the response carries the
data, and give the query a `StaleTime` so the handed-over data is not refetched on arrival.

Numbered pages need no special support: put the page number in the key and set `KeepPreviousData`, so the page
on screen stays (flagged `IsPlaceholderData`) while the next one loads, and `PrefetchAsync` the page after it.

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
  actions into one write (flushed on dispose, on demand via `persistor.FlushAsync()`, and - with the
  browser storage, or any storage implementing `IBlexStorageSuspension` - as the page is hidden or
  unloaded, so a change made just before a reload or tab close is not lost).
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
- **Transient fields** - `[BlexState(Persist = false)]` keeps one field of a persistent store out of
  storage, declared where the field is: it is never written, and a value stored for it before it was
  excluded is ignored on hydration and on cross-tab sync. It is still part of the store's snapshot, so
  DevTools, undo/redo and the prerender handoff see it as usual.
- **Partial persistence** - `options.Partialize` projects each snapshot on its way to storage, so
  transient fields (a search box's text, a cached response) never get written. Keys you omit come
  back as their declared defaults rather than being wiped. Return `null` to skip that store's write
  entirely.
- **Stores that arrive later** - a persistent store registered after startup (a lazily loaded
  feature, a [scoped store](#scoped-stores)) is hydrated the moment it registers, and its saves wait until
  that has happened - writing its declared defaults first would overwrite the user's data. A store
  that leaves the manager writes the debounced save it still owes on its way out.
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
- **Protection** - an `IBlexStateProtector` (`options.Protector`, or one registered in DI when it needs
  other services) encrypts and tamper-proofs every payload on its way to storage and checks it on the way
  back. A value it returns `null` for - one that will never read: saved in the clear, written by something
  else, protected with a key that is gone, tampered with - is reported and discarded. When it throws (a key
  service out of reach or timed out), nothing is lost: the failure is reported, the value is kept, the other
  stores hydrate, and that store holds its saves until `RehydrateAsync` gets through. A payload that fails to
  protect is never written in the clear. On Blazor Server, ASP.NET Core Data Protection keeps the key on the
  server, so the browser holds only ciphertext:

```csharp
sealed class DataProtectionProtector(IDataProtectionProvider provider) : IBlexStateProtector
{
    private readonly IDataProtector _protector = provider.CreateProtector("Bit.Blex.State");

    public ValueTask<string> ProtectAsync(string store, string payload, CancellationToken ct = default)
        => new(_protector.Protect(payload));

    public ValueTask<string?> UnprotectAsync(string store, string payload, CancellationToken ct = default)
    {
        // Data Protection payloads start with "CfDJ8"; anything else was saved in the clear.
        if (!payload.StartsWith("CfDJ8", StringComparison.Ordinal))
            return new((string?)null);

        try
        {
            return new(_protector.Unprotect(payload));
        }
        catch (CryptographicException ex) when (ex.InnerException is null)
        {
            // The key is gone or the payload was tampered with: it will never read. A failure that may pass (the key
            // store out of reach) comes wrapped instead; it propagates, and the value is kept.
            return new((string?)null);
        }
    }
}

builder.Services.AddSingleton<IBlexStateProtector, DataProtectionProtector>();
builder.Services.AddBitBlexLocalStoragePersistence();
```

```csharp
[BlexStore(Name = "search", Persist = true)]
public partial class SearchStore
{
    [BlexState] private string[] _recent = [];
    [BlexState(Persist = false)] private string _query = "";   // per-visit, never persisted
}
```

```csharp
builder.Services.AddBitBlexLocalStoragePersistence(options =>
{
    options.Partialize = (storeName, state) =>
    {
        if (storeName == "search" && state["Recent"] is JsonArray recent && recent.Count > 20)
            state["Recent"] = new JsonArray([.. recent.Take(20).Select(n => n?.DeepClone())]);

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
manager.SubscribeToAction(CounterStore.Actions.Add, (counter, amount) => { ... });   // typed
manager.SubscribeToAction("Increment", ctx => { ... });                               // by name
manager.SubscribeAsync(async ctx => await otherStore.Reload());
```

For "once, when this holds" rather than "every time this happens", `manager.WhenAsync(condition, ct)`
completes at once when the condition holds, and otherwise after the first change (an action, a restore, an
effect starting or finishing) that makes it true - MobX's `when`, Redux Toolkit's listener `condition`. A
change counts as soon as it is notified, even inside an async action that will be recorded only when it
ends, so an effect can wait for something its own store's actions do. A throwing condition faults the task;
cancelling stops the wait:

```csharp
await manager.WhenAsync(() => session.IsSignedIn, ct);   // in an effect: wait for sign-in, then check out
```

A subscription answers "tell me when this changed". For "give me this value, cheaply, from several
places at once" - what `[BlexComputed]` does inside one store - `CreateSelector` lifts the
same memoization to the manager. It is lazy and shared: three components binding to one cross-store
total pay for one computation per change rather than three, and it subscribes to nothing - it compares a
version the manager advances whenever any registered store changes, before that store's subscribers
run, so creating one adds no work to dispatch and a component re-rendering for a change never reads
the value from before it:

```csharp
using var summary = manager.CreateSelector(() => $"{orders.Units} units, {cart.Total:C}");
var text = summary.Value;   // recomputed at most once per change, however many readers
```

When the derived value takes an argument - the line with this id, the total of that category -
`CreateSelectorFamily` memoizes one per argument and keeps at most `maxSize` of them (128 by default),
dropping the least recently read: a selector per id created by hand would keep every id the list ever
showed alive. `Remove(id)` drops one at once, when its entity is gone:

```csharp
using var lineTotal = manager.CreateSelectorFamily((int id) => cart.Line(id).Total * tax.Rate, maxSize: 500);
var total = lineTotal[42];   // computed once per change for 42, shared by every row reading it
```

Both stay correct when state moves without an action: undo/redo, DevTools time-travel, persistence
hydration, a change arriving from another tab and the prerender handoff all raise
`manager.StateRestored`, which re-evaluates manager subscriptions (selectors are already stale by then). Ahead of it,
`manager.StoreRestored` names each store that moved, for observers that only need to revisit that one -
persistence writes back just the restored store, and undo/redo compares just its slice.

## Scoped stores

A store registered with `AddBitBlexStore` exists once per DI scope, keyed by its name. State that belongs
to one part of the page rather than to the app - the form inside each of two open dialogs, one editor
per tab, a wizard that must start fresh every time - needs an instance of its own. A scoped store is one:
registered under `"{name}:{key}"`, a full citizen of the state tree, DevTools, middleware and history for
as long as it lives, and unregistered and disposed when it goes away.

```razor
<BlexScope TStore="DocumentStore" Key="@DocumentId">
    <Toolbar />   @* [CascadingParameter] public DocumentStore Document { get; set; } *@
    <Editor />
</BlexScope>
```

`<BlexScope>` creates the store from DI (constructor dependencies resolved as usual; `Factory` builds one
DI cannot), cascades it to every descendant, and disposes it with the component. Changing `Key` replaces
the instance; omitting it assigns a unique one. Outside Blazor:

```csharp
using var editor = manager.CreateScope(new DocumentStore(api), key: "42");
editor.Store.Rename("Draft");   // recorded as "document:42/Rename"
```

A scoped store marked `Persist = true` is hydrated as soon as it registers and saved under its keyed name,
so give it a stable key (the document's id) for the next instance to find its state. Its saves wait for
that read, so a quick edit never overwrites stored state nobody has seen yet; if nothing was stored, the
edit is written once the read comes back empty. An instance whose key was assigned for it is never
persisted: those keys count from `1` again in every session and every tab, so its state could only ever be
found by an unrelated instance. Scoped stores are not registered with `AddBitBlexStore` and are not part
of the prerender handoff.

## Middleware: observe and veto

Middleware sees every action after it applies (including its argument payload via `ctx.Args`), and
can veto an action before it runs - also based on the payload:

```csharp
builder.Services.AddBitBlexServices(options =>
{
    options.UseMiddleware(ctx => Console.WriteLine($"{ctx.QualifiedName}({string.Join(", ", ctx.Args)})"));
    options.UseFilter(ctx => !IsReadOnly);   // return false to cancel
    options.UseFilter(CartStore.Actions.Add, (cart, price) => price > 0);   // one action, typed payload
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

### Logging and tracing

`options.UseLogging()` writes to the host's `ILogger` (category `Bit.Blex`): every action at `Debug` (or the
level you pass; `LogLevel.None` logs failures only), every action failure as an error, and every failure Blex
isolates as a warning - instead of `Console.Error` - before your `OnError` sees it too. Names only: action
arguments and state never reach the log.

Tracing and metrics use the standard `ActivitySource`/`Meter` APIs, named `BlexDiagnostics.SourceName`
(`"Bit.Blex"`), and cost two flag reads per action when nothing listens:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(t => t.AddSource(BlexDiagnostics.SourceName))
    .WithMetrics(m => m.AddMeter(BlexDiagnostics.SourceName));
```

Every action, effect and query fetch is a span (`"cart/Checkout"`, `"query todos"`); an action that runs inside
another is its child, a failed one is marked `Error` with its exception type, a cancelled one is tagged
`blex.cancelled`, and an effect's retries are span events. Metrics: `blex.actions`, `blex.action.errors` and
`blex.action.duration` (seconds) by `blex.store` and `blex.action`; `blex.query.fetches` and
`blex.query.duration` by `blex.query.resource` and `blex.query.outcome`. A store is tagged with its declared
name - a scoped instance's key goes on the span only - and a query with its key's first part, so series stay
few and ids stay out of exported telemetry.

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

Snapshots are taken per store and shared: recording an action serializes only the stores it changed, every
undo step holds a reference to the unchanged stores' slices rather than a copy, and an undo or redo restores
(and notifies, and writes back to storage) only the stores whose state actually differs - so a step costs what
changed, however many stores the app has. (A store changed in place, without assigning a property, is not
re-captured; see [Catching in-place mutations](#catching-in-place-mutations).)

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

### Scoping undo to what the user is editing

By default the history spans every store, and an undo restores the whole tree - so undoing an edit can
also sign the user out, switch the theme back or empty a cart a background refresh just filled. A scope
keeps undo inside the stores it covers: only their actions become steps, and only their slices are
restored.

```csharp
// The application's history, minus the stores undo must never revert:
builder.Services.AddBitBlexHistory(scope: s => s is not SessionStore and not ThemeStore);

// Or an undo stack per store type, alongside it (starts recording when first resolved):
builder.Services.AddBitBlexHistory<EditorStore>();
```

```razor
@inject BlexHistory<EditorStore> EditorHistory
<button @onclick="EditorHistory.Undo" disabled="@(!EditorHistory.CanUndo)">Undo</button>
```

`BlexHistory<TStore>` covers every instance of the type, [scoped](#scoped-stores) ones included. A restore
that changes nothing a history covers - another history's undo, a store hydrating - leaves its stack and
labels alone. Outside DI: `new BlexHistory(manager, scope: s => s is EditorStore)`.

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

The log speaks in [typed actions](#typed-actions) too, so a test asserts on payloads as the types the
actions take:

```csharp
harness.Store.Place("pen", 2);
Assert.IsTrue(harness.Log.Contains(OrdersStore.Actions.Place));
var order = harness.Log.PayloadsOf(OrdersStore.Actions.Place)[0];   // (string Name, int Quantity)
Assert.AreEqual(2, order.Quantity);
```

### Deterministic time

Debounced effects and debounced persistence wait on `manager.TimeProvider`, and action timestamps read
it. Give the harness a fake clock (`FakeTimeProvider` from `Microsoft.Extensions.TimeProvider.Testing`)
and a 300 ms debounce is tested by advancing the clock - no real waiting, no flakiness:

```csharp
var time = new FakeTimeProvider();
using var harness = BlexTestHarness.For<SearchStore>();
harness.TimeProvider = time;

var search = harness.Store.Search("blex");     // [BlexEffect(DebounceMilliseconds = 300)]
time.Advance(TimeSpan.FromMilliseconds(299));
Assert.AreEqual(0, harness.Log.Count);
time.Advance(TimeSpan.FromMilliseconds(1));
await search;
```

In an app, `options.TimeProvider` sets it, or a `TimeProvider` registered in the container; stores read
it as `TimeProvider` inside their actions, for timestamps a test controls.

### Record and replay

The same actions applied to the same initial state must arrive at the same state. `ReplayAsync`
dispatches a recorded log again - through the generated wrappers, so filters, middleware and effects behave
as they did the first time - which proves a store deterministic, and turns the actions behind a bug report
into a test:

```csharp
using var original = BlexTestHarness.For<CartStore>();
original.Store.Add("apple");
original.Store.Add("pear");

using var replica = BlexTestHarness.For<CartStore>();
await replica.Manager.ReplayAsync(original.Log.Actions);
Assert.AreEqual(original.Snapshot().ToJsonString(), replica.Snapshot().ToJsonString());
```

Generated actions and effects, `Set X` assignments and `ResetState` replay; a `Batch` carries a closure
rather than data, so it cannot, and the replay says which action stopped it. An action that ran inside
another one's body (`cart.Checkout()` calling `orders.Place()`, recorded with `IsNested`) is skipped,
because replaying the outer action runs it again. Each action is awaited before the next, and effects
skip their `DebounceMilliseconds` wait during a replay, so it never waits on the clock - a fake one
nobody advances included.

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
`DebounceMilliseconds` · `BLEX020` a store member already named `Actions`, so the typed action
descriptors are not generated (warning) · `BLEX021` two actions dispatched under one name ·
`BLEX022` negative `RetryCount`/`RetryDelayMilliseconds`/`TimeoutMilliseconds` · `BLEX023`
`TimeoutMilliseconds` on an effect without a `CancellationToken` parameter.

## Time-travel debugging

1. Install the [Redux DevTools](https://github.com/reduxjs/redux-devtools) browser extension.
2. Run the app and open DevTools - you'll see an instance named after `DevToolsName`.
3. Every action streams in with its argument payload and the resulting state tree.
4. Use the slider / jump buttons to rewind and replay your application state live.

Under the hood the JS bridge (`_content/Bit.Blex/bit-blex-devtools.js`) talks to `window.__REDUX_DEVTOOLS_EXTENSION__`,
sends each action via `send(action, state)`, and applies `JUMP_TO_STATE` / `JUMP_TO_ACTION` /
`ROLLBACK` / `RESET` / `COMMIT` / `IMPORT_STATE` messages back onto the stores.

The monitor's **dispatcher** fires actions at the running app, because every generated action can be
[dispatched by name](#typed-actions). Type the action as JSON, named the way the monitor labels it:

```json
{ "type": "cart/Add", "payload": { "price": 9.99 } }
```

The arguments may also sit inline beside `type`, and a one-parameter action takes its argument as the
payload itself, Redux style - a value (`{ "type": "cart/Add", "payload": 9.99 }`) or an object
(`{ "type": "orders/Place", "payload": { "Name": "mug", "Quantity": 2 } }`), which is read as the argument
unless it names the parameter. Optional arguments left out take their defaults. It runs through the
filters and middleware like any other
action; a typo, an unknown action or an action that throws is reported through `OnError` rather than
breaking the bridge.

The monitor's two recording controls are honoured as well, and they differ in kind. **Pause
recording** is about the timeline: actions still apply and still reach middleware and subscribers,
they are simply not streamed, so you can set up the state you want to inspect without filling the
monitor with noise. **Lock changes** is about the app: every action is vetoed until it is unlocked,
while time-travel restores keep working - which is the point. Both are readable as
`manager.IsDevToolsPaused` / `manager.AreChangesLocked` - `manager.DevToolsStatusChanged` says when either
moves, since neither is an action a component would re-render on - and disconnecting the bridge clears them, so
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

MAUI Blazor Hybrid apps use `Bit.Blex` exactly like any Blazor app: `AddBitBlexServices(...)` in
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
builder.AddBitBlexQueries();                   // optional: the server-state cache, as a singleton
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
| `Bit.Blex` | The main package. Core runtime: `BlexStoreBase`, attributes, dispatch, middleware, persistence, entity adapter, undo/redo, the `BlexManager` cross-store manager. Blazor integration under `Blazor/` (namespace `Bit.Blex.Blazor`): `BlexComponentBase`, `<BlexProvider>`, browser-storage persistence, Redux DevTools bridge (TypeScript in `Scripts/`, compiled into `wwwroot/` on build). Packs the generator below under `analyzers/dotnet/cs`. |
| `Bit.Blex.Generators` | Roslyn incremental source generator (netstandard2.0). Not a package of its own: it ships inside `Bit.Blex`. |
| `Bit.Blex.Maui` | .NET MAUI integration: `UseBitBlex()` startup initializer, `Preferences`-backed persistence, XAML-bindable stores. |
| `Bit.Blex.Testing` | Test harness and assertions (`BlexTestHarness`, `BlexActionLog`). |
| `Demo` | Documentation website (`Client` + prerendering `Server` host): every feature explained with a live, runnable demo. |
| `Samples/Bit.Blex.Samples.Wasm` | Minimal Blazor WebAssembly sample (Counter, Todos, Weather). |
| `Tests/Bit.Blex.Tests` | MSTest tests for the runtime, the Blazor/MAUI integrations and the generated code, on every target framework. |
| `Tests/Bit.Blex.Tests.Generators` | Generator-driver tests: all `BLEX` diagnostics + emission snapshots. |
| `Tests/Bit.Blex.Tests.Mcp` | MCP server tests, over the real protocol against the demo server's own `Program.cs`. |
| `Tests/Bit.Blex.Tests.E2E` (+ `Tests/Bit.Blex.Tests.Harness*`) | Playwright suites in every Blazor render mode, Blazor Hybrid (WebView2) and trimmed/AOT publishes. See its [README](Tests/Bit.Blex.Tests.E2E/README.md). |
| `Tests/Bit.Blex.Benchmarks` | BenchmarkDotNet suites (dispatch, fan-out, serialization, entity adapter). |

Inside this repository the demo, sample, tests and benchmarks reference the generator project
directly as an analyzer (`OutputItemType="Analyzer" ReferenceOutputAssembly="false"`): a
`ProjectReference` to `Bit.Blex` brings the runtime only, while the package brings the generator too.

## Build & test

```bash
dotnet build src/Utilities/Blex/Bit.Blex.slnx
cd src/Utilities/Blex/Tests/Bit.Blex.Tests && dotnet test
cd src/Utilities/Blex/Tests/Bit.Blex.Tests.Generators && dotnet test
cd src/Utilities/Blex/Tests/Bit.Blex.Tests.Mcp && dotnet test
cd src/Utilities/Blex/Tests/Bit.Blex.Tests.E2E && dotnet test              # needs Playwright Chromium, see its README
dotnet run --project src/Utilities/Blex/Demo/Server                        # documentation site
dotnet run --project src/Utilities/Blex/Samples/Bit.Blex.Samples.Wasm      # minimal sample app
dotnet run -c Release --project src/Utilities/Blex/Tests/Bit.Blex.Benchmarks
```

Run `dotnet test` from inside `src` so `src/global.json`'s Microsoft.Testing.Platform runner setting
applies.
