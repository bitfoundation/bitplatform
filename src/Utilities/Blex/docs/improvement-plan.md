# Blex improvement plan

A living record of the audit of `src/Utilities/Blex`, the gaps found, what was done about them and why. `README.md`
stays the user-facing reference; this file records decisions and what is still open.

Status legend: **done** (implemented, tested, documented), **deferred** (worth doing, not in this round),
**rejected** (deliberately not done, with the reason), **exists** (the plan asked for it; Blex already had it).

---

## 1. Baseline (round 1, 2026-11-11)

| Check | Result |
| --- | --- |
| `Tests/Bit.Blex.Tests` (net11.0) | 788 passed |
| `Tests/Bit.Blex.Tests.Generators` (net11.0) | 104 passed |
| E2E (Playwright, hybrid, publish gate) | not run (on-demand suite, `bit.ci.Blex.e2e.yml`) |

Blex at the start of the round already covered most of the requested surface: source-generated stores with
memoized computeds (dependency-tracked per state property), typed action descriptors and dispatch by name,
effects with `Parallel`/`Latest`/`Drop`/`Queue`, debounce, retries with backoff and timeouts, `EnforceActions`,
`RollbackOnError`, cross-store `Transaction`, cross-store selectors and selector families, `WhenAsync`,
`IObservable` interop, scoped stores, an entity adapter, persistence (debounce, versions per app and per store,
migrations, merge, partialize, transient fields, protection/encryption, cross-tab sync, hydration status, late
stores), undo/redo (filters, grouping, checkpoints, scoped histories), Redux DevTools (sanitizers, dispatcher,
pause/lock), a TanStack-style query cache (mutations, optimistic rollback, infinite queries, offline,
prerender handoff, cross-tab invalidation), OpenTelemetry spans and metrics, `ILogger` logging, a testing
harness (multi-store, failures, replay, fake clock), Blazor components that need no base class
(`<BlexSelect>`, `<BlexView>`, `<BlexScope>`), render throttling, MAUI XAML binding and Preferences persistence,
and an MCP server for agents. The plan's own note holds: the existing implementation is the source of truth,
and this round improves it rather than re-creating features under new names.

## 2. Gap analysis (prioritized)

Correctness first, then developer experience, then breadth. Findings come from reading the code and from three
independent read-only audits (query cache, persistence, manager/history/Blazor), each finding traced to a
concrete failure and reproduced by a test before it was fixed.

### P0 - correctness bugs (all done)

| # | Area | Defect | Fix |
| --- | --- | --- | --- |
| 1 | Persistence | A hydration held a global "suspended" counter across every storage await, so any action of *any* store made while `RehydrateAsync` (or a cross-tab apply) was in progress was never saved. | Per-store save holds during a read (as late stores already had), echo suppression per store and only for the apply itself. |
| 2 | Persistence | `StartAsync` subscribed to actions only after reading every store, so an edit made while startup hydration was still reading (the page is interactive by then) was never saved - including a late scoped store's, which the README promises to write. | Subscribe before reading; each store's own hold protects its unread state. A store whose read failed keeps holding its saves until a later read gets through. |
| 3 | Persistence | A change from another tab was read while this tab's own write was still in flight, so the two tabs ended on different states. | The cross-tab read waits for the write chain, like hydration already did. |
| 4 | Persistence | A save released after synchronous disposal started a debounce timer nothing would cancel. | Written at once after disposal. |
| 5 | Persistence (MAUI) | A debounced save was lost when the OS stopped a backgrounded app (nothing disposes the container). | `BlexPreferencesStorage` implements `IBlexStorageSuspension`; `BlexPreferencesStorage.NotifySuspending()` from `OnSleep`; the sample does it. |
| 6 | Query | An optimistic rollback restored data but not the error and stale mark: a failed refetch came back as fresh success, and a failed first fetch as "error" with no error. | The rollback restores status, error and invalidation exactly. |
| 7 | Query | A per-query `GcTime` shorter than the client default, or `Timeout.InfiniteTimeSpan`, was ignored (the entry started at the default and took the max). | The first query's GC time sets it; never-collect values outlast finite ones; the default applies only when no query asked. |
| 8 | Query | A cancelled fetch whose client reports cancellation in its own words (gRPC `RpcException(Cancelled)`, `IOException`) was committed as an error. | Any failure after the fetch's own token was cancelled settles as a cancellation. |
| 9 | Query | An invalidation did not reach prerender data no component had claimed yet, which was then shown as fresh. | Invalidation by key marks matching unclaimed handoffs; the first query refetches while showing them. |
| 10 | Query | Prerender timestamps from a server clock running ahead counted as fresh even with `StaleTime = 0`. | Handed-over timestamps are clamped to the client's now. |
| 11 | Scoped stores | A generated scope key (`"1"`) could equal an id given as a key, merging two instances into one name: undo, time-travel and storage wrote into either. | Generated keys are `#1`, `#2`, ... |
| 12 | Undo | A reactor subscribed before the history recorded its follow-up first, folding cause and follow-up into one undo step depending on subscription order. | The history observes actions in an internal stage after DevTools and before every subscriber. |
| 13 | DevTools | Any provider's disposal disconnected whichever bridge was connected (several providers per scope are supported). | `DisconnectDevTools(IBlexDevTools)` detaches only its own bridge; the connector disconnects itself. |
| 14 | DevTools | A connector disposed while `connect` was pending left a live extension subscription calling a released reference; a provider disposed during hydration still connected afterwards. | The connector closes the connection once its handle arrives; the provider checks disposal first. |
| 15 | Errors | Failure reports were de-duplicated by exception identity across the whole manager, so two unrelated actions failing with one exception instance (two effects awaiting one faulted task) reported once. | De-duplicated per dispatch frame: an outer frame skips only the failure a nested action of its own reported. |
| 16 | Subscriptions | A `manager.Subscribe` handler disposed by an earlier handler still ran for that action. | Disposed handlers are skipped mid-dispatch. |

### P1 - developer experience (done this round)

| Item | Status | Notes |
| --- | --- | --- |
| Typed action results | **done** | `[BlexAction]` methods may return `T`, `Task<T>` or `ValueTask<T>`; the wrapper returns it (the plan's "typed success outcome" in its C#-native form: a return value). A vetoed action returns `default`. BLEX011 ("return value is discarded") is retired. |
| Custom equality per state | **done** | `[BlexState(Comparer = nameof(member))]`; BLEX029 validates the member at compile time. Makes polling free when the data is unchanged. |
| Effect throttle | **done** | `[BlexEffect(ThrottleMilliseconds = n)]`, leading edge, dropped invocations leave no trace, replay never throttles; BLEX027/BLEX028. |

### P2 - deferred (worth doing, not this round)

| Item | Why deferred |
| --- | --- |
| Async optimistic actions in stores (rollback of `Task` actions) | Async actions interleave by design (each overlapping run records separately), so a store-level snapshot could undo another run's changes. Server writes already have optimistic updates with safe rollback in `BlexQueryClient.Mutation`/`Optimistic`. Needs a design that scopes the rollback to what the action itself wrote. |
| Merge-kept live values written after hydration | When `options.Merge` keeps a value assigned before hydration, storage holds the older value until the next action. Low impact; fix needs a diff between the applied slice and what was read. |
| Analyzer for direct field writes (`_count = 1` bypassing the setter) | Real footgun (no notification), but needs an analyzer rather than the generator; `DetectInPlaceMutations` already reports the runtime effect. |
| Generated `[JsonSerializable]` context for state types | Would make trimming/AOT zero-config; needs care not to clash with an app's own context. `BlexJson.Configure` is the documented seam today. |
| Brouter integration (URL-synced state, route-scoped stores) | Belongs in Brouter (the dependent library, like `Bit.BlazeDb.Blex`), and needs Brouter's query-parameter API to settle first. |
| Borm integration (form drafts persisted, undo across a form) | Same dependency direction; Borm's form state already round-trips through a `[BlexState]` record today. |
| Bcrdt integration | CRDT documents own their history and conflict resolution; binding them to stores needs a design that keeps undo out of CRDT history. `Bit.BlazeDb.Bcrdt` is the existing persistence link. |

### Rejected

| Item | Reason |
| --- | --- |
| Splitting a framework-independent core package out of `Bit.Blex` | The core types already depend on nothing Blazor-specific (the Blazor integration lives in its own namespace). Moving it to a new package would break every consumer's references for no runtime gain; the only cost of the current shape is a transitive `Microsoft.AspNetCore.Components.Web` reference in native MAUI apps. |
| Signals / atoms / observable collections as new primitives | `[BlexState]` + `[BlexComputed]` (dependency-tracked) and `CreateSelector`/`CreateSelectorFamily` cover fine-grained derivation; a second state model would fragment DevTools, persistence and history, which are built on stores. |
| Re-creating `Blex.BlazeDb` / `Blex.BevTools` packages | They exist as `Bit.BlazeDb.Blex` and `Bit.BevTools.Blex`, owned by the library that depends on Blex - the direction that avoids circular references. |
| A Belemetry-specific package | Blex already emits standard `ActivitySource`/`Meter` telemetry named `Bit.Blex` (actions, effects, retries, query fetches) with names-only tagging; any collector, Belemetry's included, consumes it without an adapter. |
| Throwing on a vetoed value-returning action | On Blazor Server an exception from an event handler ends the circuit; a filter (read-only mode, DevTools "Lock changes") must stay a no-op for callers. `default` is documented instead. |

## 3. Results (round 1)

| Check | Result |
| --- | --- |
| `Tests/Bit.Blex.Tests` (net11.0 and net10.0) | 824 passed each (36 new) |
| `Tests/Bit.Blex.Tests.Generators` (net11.0) | 111 passed (7 new) |
| `Tests/Bit.Blex.Tests.Mcp` (net11.0) | 127 passed (catalog and generator agree on BLEX027-029, BLEX011 retired) |
| BlazeDb Blex integration / BevTools Blex tests | 8 / 6 passed |
| `Samples/MAUI` | not built here (no MAUI workloads installed); the change is an `Application.OnSleep` override |

Every P0 regression test was run against the unfixed code first and failed there (the guard tests that pin the
behavior a fix must keep - a store not read yet keeps its stored state, a store whose read failed keeps
holding - passed on both).

### Review of the round's own changes

An independent review of the diff found four defects in it, each fixed with a test that failed (one with a stack
overflow) before the fix:

- `Dispatch<T>` passed an expression lambda to `Dispatch`, which bound back to `Dispatch<T>` itself and recursed
  forever; it now passes a block lambda (generated wrappers were not affected).
- Subscribing before the startup read stayed in place when the read failed, so a store registered before the
  retry could save its defaults over unread stored state; a failed start stops saving again.
- A `RehydrateAsync` overlapping the startup read left the stores that read was reading to it but announced
  hydration before it finished; overlapping reads are now chained.
- A DevTools connector disposed while a `connect` call was pending leaked its module and object reference when
  that call then failed; it releases them on that path too.

A sixth point was confirmed as intended: a scoped store closed while its read is pending drops the edits it held,
because whether storage had state for it is exactly what never got found out.
