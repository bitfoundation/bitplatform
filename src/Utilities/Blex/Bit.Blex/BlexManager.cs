using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Bit.Blex;

/// <summary>
/// The central state manager. Aggregates every registered <see cref="IBlexStore"/> into a single
/// global state tree, runs the middleware pipeline, feeds the DevTools bridge and applies
/// time-travel snapshots. Resolve it from DI if you need cross-store coordination; most apps
/// only interact with individual stores.
/// </summary>
/// <remarks>
/// This type is designed for single-threaded dispatch, matching Blazor's rendering model. The
/// internal <c>_gate</c> only protects the store registry and global-state capture; the dispatch
/// pipeline itself (sequence counter, middleware, DevTools) assumes actions are not dispatched
/// concurrently. Coordinate access externally if you dispatch from multiple threads.
/// </remarks>
public sealed class BlexManager
{
    // Copy-on-write: the registry is replaced rather than mutated, so anything already walking
    // Stores (persistence saving slices, a restore in progress, user code) keeps a stable snapshot
    // even when a lazily-loaded feature registers or tears down a store mid-enumeration.
    private volatile IBlexStore[] _stores = [];
    private readonly List<IBlexMiddleware> _middleware;
    private readonly Lock _gate = new();
    private IBlexDevTools? _devTools;
    private JsonObject? _initialState;
    private JsonObject? _committedState;
    private int _sequence;
    private int _scopeSequence;
    private int _restoreBatchDepth;
    private bool _connected;
    private bool _devToolsPaused;
    private bool _changesLocked;
    private long _stateVersion;
    private TimeProvider _timeProvider = TimeProvider.System;

    // Stores that implement IBlexStore without deriving from BlexStoreBase only announce a change through
    // their own StateChanged event; the handler the manager hooks there, kept so it can be unhooked.
    private readonly Dictionary<IBlexStore, Action> _foreignHandlers = new(ReferenceEqualityComparer.Instance);

    /// <summary>Creates a manager with the supplied middleware (order preserved).</summary>
    public BlexManager(IEnumerable<IBlexMiddleware>? middleware = null)
    {
        _middleware = middleware?.ToList() ?? [];
    }

    /// <summary>
    /// The clock Blex reads time from: action timestamps, the <c>DebounceMilliseconds</c> wait of
    /// generated effects and the persistence debounce. Defaults to <see cref="TimeProvider.System"/>.
    /// </summary>
    /// <remarks>
    /// Swap in a fake clock (for example <c>FakeTimeProvider</c> from
    /// <c>Microsoft.Extensions.TimeProvider.Testing</c>) to make debounced effects and debounced saves
    /// deterministic in tests: advance the clock instead of waiting out the real interval. Set it
    /// before the work it should govern starts; a wait already in progress keeps the clock it began on.
    /// In a DI host, <see cref="BlexOptions.TimeProvider"/> or a registered <see cref="System.TimeProvider"/>
    /// sets it.
    /// </remarks>
    public TimeProvider TimeProvider
    {
        get => _timeProvider;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _timeProvider = value;
        }
    }

    /// <summary>
    /// All registered stores. The returned list is an immutable snapshot: registering or
    /// unregistering a store while it is being enumerated is safe and does not affect the walk
    /// already in progress.
    /// </summary>
    public IReadOnlyList<IBlexStore> Stores => _stores;

    /// <summary>
    /// Optional sanitizer applied to the state tree just before it is sent to the DevTools extension
    /// (for display only). Use to redact secrets. Note: because DevTools time-travel echoes the
    /// displayed state back, redacting values can corrupt restores - prefer redacting only fields you
    /// never need to jump back to, or disable DevTools entirely in production.
    /// </summary>
    public Func<JsonObject, JsonObject>? DevToolsStateSanitizer { get; set; }

    /// <summary>Optional sanitizer applied to the action label sent to the DevTools extension.</summary>
    public Func<string, string>? DevToolsActionSanitizer { get; set; }

    /// <summary>
    /// Optional sink for non-fatal errors that Blex isolates from the dispatch pipeline
    /// (throwing subscribers, middleware, persistence writes, restores, sanitizers, ...).
    /// When unset, errors are written to <see cref="Console.Error"/>. Handlers must not throw;
    /// a throwing handler is itself swallowed to keep dispatch alive.
    /// </summary>
    public Action<BlexError>? OnError { get; set; }

    /// <summary>
    /// Whether anything observes dispatched actions (middleware, DevTools or subscribers).
    /// Lets hot paths skip payload capture when nobody is listening.
    /// </summary>
    internal bool HasObservers => _middleware.Count > 0 || _connected || ActionDispatched is not null;

    /// <summary>
    /// Whether generated wrappers should capture action arguments: whenever anything observes actions,
    /// and also when only an <see cref="ActionFailed"/> handler is attached -- a failure report that
    /// cannot name the input that failed is missing the one thing it is for.
    /// </summary>
    internal bool CapturesArgs => HasObservers || ActionFailed is not null;

    /// <summary>
    /// Whether the Redux DevTools monitor is paused ("Pause recording"). Actions still apply and
    /// still reach middleware and subscribers; they are simply not streamed to the extension.
    /// </summary>
    public bool IsDevToolsPaused => _devToolsPaused;

    /// <summary>
    /// Whether the Redux DevTools monitor has frozen the application ("Lock changes"). Every action
    /// is vetoed while this is on, exactly as a <see cref="BlexOptions.UseFilter">filter</see>
    /// returning <c>false</c> would -- time-travel restores still apply, which is the point.
    /// </summary>
    public bool AreChangesLocked => _changesLocked;

    /// <summary>
    /// Raised when <see cref="IsDevToolsPaused"/> or <see cref="AreChangesLocked"/> changes -- the monitor's
    /// "Pause recording" and "Lock changes" buttons, or the connection opening or closing. Neither is an action
    /// or a state change, so a component showing them has nothing else to re-render on: while changes are
    /// locked, nothing moves at all. Handler exceptions are isolated and reported through <see cref="OnError"/>.
    /// </summary>
    public event Action? DevToolsStatusChanged;

    /// <summary>Sets the monitor's two flags, raising <see cref="DevToolsStatusChanged"/> when either moved.</summary>
    private void SetDevToolsStatus(bool paused, bool locked)
    {
        if (paused == _devToolsPaused && locked == _changesLocked)
            return;

        _devToolsPaused = paused;
        _changesLocked = locked;
        InvokeIsolated<object?>(DevToolsStatusChanged, null, "devtools", "status changed handler");
    }

    /// <summary>
    /// Advances whenever the state of any registered store changes -- by an action, a restore, an
    /// effect's lifecycle flags or an assignment still waiting for its batch to notify -- and before
    /// that store's subscribers run. Memoized selectors compare it rather than subscribing to anything.
    /// </summary>
    internal long StateVersion => _stateVersion;

    /// <summary>Stamps a state change of a registered store (see <see cref="StateVersion"/>).</summary>
    internal void MarkStateChanged() => _stateVersion++;

    /// <summary>
    /// Stamps a change to <paramref name="store"/>'s state and, when a <see cref="Dispatcher"/> is bound,
    /// reports a change made outside it.
    /// </summary>
    internal void OnStoreChanged(IBlexStore store)
    {
        _stateVersion++;

        if (ReportOffDispatcherChanges && Dispatcher is { } dispatcher && !dispatcher.CheckAccess())
        {
            ReportError(
                "dispatcher",
                new InvalidOperationException(
                    $"The store '{store.Name}' changed outside the dispatcher its state belongs to. Stores are not synchronized: a change from a timer, a background task, a SignalR handler or a continuation after ConfigureAwait(false) races the renderer and every other change. Make it through BlexManager.InvokeAsync(...) (or a component's InvokeAsync)."),
                store.Name);
        }

        // A whole-tree restore is one change, announced by StateRestored after its last store: a waiter must not
        // see the tree half restored.
        if (_restoreBatchDepth == 0)
            StoreChanged?.Invoke(store);
    }

    /// <summary>
    /// Raised for every change a registered store applies, as its subscribers are about to hear of it -- inside an
    /// async action still running too, which records (and so raises <see cref="ActionDispatched"/>) only once it
    /// ends. <see cref="WhenAsync"/> waits on it: an effect awaiting a condition its own store's actions make true
    /// would otherwise wait for its own end. Handlers must not throw.
    /// </summary>
    internal event Action<IBlexStore>? StoreChanged;

    /// <summary>
    /// Raised as soon as an action of a <c>RollbackOnError</c> store threw and its state was put back, with the
    /// <see cref="StateVersion"/> the action began at: a snapshot taken since (another store's action recording
    /// mid-body) may hold the state the rollback undid. Undo/redo repairs its snapshots on it. Handlers must not throw.
    /// </summary>
    internal event Action<BlexStoreBase, long>? StoreRolledBack;

    internal void OnRolledBack(BlexStoreBase store, long startedAt) => StoreRolledBack?.Invoke(store, startedAt);

    /// <summary>
    /// The execution context the state belongs to (in Blazor, the renderer's <c>Dispatcher</c>), or
    /// <c>null</c> when there is none to keep to. <c>&lt;BlexProvider&gt;</c> binds it on its first render.
    /// </summary>
    /// <remarks>
    /// Stores are not synchronized, and neither is the dispatch pipeline: a Blazor Server circuit and a
    /// BlazorWebView keep them consistent by running everything on one dispatcher. Code that is not
    /// already there -- a <c>System.Threading.Timer</c> callback, a SignalR <c>HubConnection.On</c> handler,
    /// a <c>Task.Run</c> body -- goes through <see cref="InvokeAsync(Action)"/>.
    /// </remarks>
    public IBlexDispatcher? Dispatcher { get; set; }

    /// <summary>
    /// Whether a store changing outside the bound <see cref="Dispatcher"/> is reported through
    /// <see cref="OnError"/> (source <c>"dispatcher"</c>). Defaults to <c>true</c>; without a bound
    /// dispatcher there is nothing to check against.
    /// </summary>
    /// <remarks>
    /// The change itself still applies -- it already has by the time it can be noticed -- but it raced
    /// the renderer and every other change, which is the kind of failure that only shows up as rare,
    /// unreproducible corruption in production. The report names the store so the caller can be found.
    /// </remarks>
    public bool ReportOffDispatcherChanges { get; set; } = true;

    /// <summary>
    /// Development aid: reports, through <see cref="OnError"/> (source <c>"mutation"</c>), an action that
    /// changed its store's state without assigning any state property -- <c>Items.Add(item)</c> instead of
    /// <c>Items = [.. Items, item]</c>. Such a change reaches no subscriber, no DevTools entry, no undo step
    /// and no storage. Defaults to <c>false</c>.
    /// </summary>
    /// <remarks>
    /// The check serializes the store before and after every outermost action, so leave it off in
    /// production. It catches an action that assigned nothing at all; one that assigned one property and
    /// mutated another in place is announced anyway, through the property it did assign.
    /// </remarks>
    public bool DetectInPlaceMutations { get; set; }

    /// <summary>
    /// Runs <paramref name="work"/> on the bound <see cref="Dispatcher"/> -- immediately when the caller is
    /// already there or none is bound. Use it to change state from outside a component: a timer, a
    /// background service, a SignalR handler.
    /// </summary>
    /// <returns>A task that completes once <paramref name="work"/> has run, faulted with its exception if it threw.</returns>
    /// <example><code>
    /// hub.On&lt;Order&gt;("orderPlaced", order => manager.InvokeAsync(() => orders.Add(order)));
    /// </code></example>
    public Task InvokeAsync(Action work)
    {
        ArgumentNullException.ThrowIfNull(work);
        if (Dispatcher is { } dispatcher && !dispatcher.CheckAccess())
        {
            return dispatcher.InvokeAsync(() =>
            {
                work();
                return Task.CompletedTask;
            });
        }

        try
        {
            work();
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            // Faulted either way, so a caller's handling does not depend on where it was called from.
            return Task.FromException(ex);
        }
    }

    /// <summary>
    /// Runs asynchronous <paramref name="work"/> on the bound <see cref="Dispatcher"/> -- immediately when
    /// the caller is already there or none is bound -- so it starts, and resumes after each <c>await</c>,
    /// where the state belongs.
    /// </summary>
    /// <returns>A task that completes with <paramref name="work"/>.</returns>
    public Task InvokeAsync(Func<Task> work)
    {
        ArgumentNullException.ThrowIfNull(work);
        if (Dispatcher is { } dispatcher && !dispatcher.CheckAccess())
            return dispatcher.InvokeAsync(work);

        try
        {
            return work();
        }
        catch (Exception ex)
        {
            return Task.FromException(ex);
        }
    }

    /// <summary>
    /// Raised after a registered store notified a change that no action record and no restore event
    /// announces: an effect's lifecycle flags, a rollback, a hand-written store's own notification. (What
    /// an async action changes is announced by its record, once it completes.) Projections that
    /// re-evaluate on actions and restores listen to it too, or they would keep reporting the value
    /// from before such a change.
    /// </summary>
    internal event Action<IBlexStore>? UnannouncedChange;

    internal void OnUnannouncedChange(IBlexStore store)
    {
        if (UnannouncedChange is { } handler)
            InvokeIsolated(handler, store, "subscriber", store.Name);
    }

    /// <summary>Routes an isolated error to <see cref="OnError"/>, or <see cref="Console.Error"/> when unset.</summary>
    internal void ReportError(string source, Exception exception, string? detail = null)
    {
        var handler = OnError;
        if (handler is null)
        {
            Console.Error.WriteLine(EscapeControlCharacters($"[Blex] {source} failed{(detail is null ? "" : $" ({detail})")}: {exception.Message}"));
            return;
        }

        try
        {
            handler(new BlexError(source, exception, detail));
        }
        catch
        {
            // The error handler is the last line of defense; nothing left to do.
        }
    }

    internal static string EscapeControlCharacters(string text)
    {
        var escaped = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            if (char.IsControl(c) || char.GetUnicodeCategory(c) is UnicodeCategory.LineSeparator or UnicodeCategory.ParagraphSeparator)
                escaped.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
            else
                escaped.Append(c);
        }

        return escaped.ToString();
    }

    /// <summary>
    /// Returns the registered store with the given <see cref="IBlexStore.Name"/>, or <c>null</c>.
    /// Names are the keys of the global state tree, so this is the counterpart to indexing
    /// <see cref="CaptureGlobalState"/>.
    /// </summary>
    public IBlexStore? GetStore(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        foreach (var store in _stores)
        {
            if (store.Name == name)
                return store;
        }

        return null;
    }

    /// <summary>Returns the first registered store of type <typeparamref name="TStore"/>, or <c>null</c>.</summary>
    /// <remarks>
    /// With several <see cref="CreateScope{TStore}(TStore, string)">scoped</see> instances of one store
    /// type registered, which one is first is unspecified; look a scoped instance up by its keyed
    /// <see cref="GetStore(string)">name</see> instead.
    /// </remarks>
    public TStore? GetStore<TStore>() where TStore : class, IBlexStore
    {
        foreach (var store in _stores)
        {
            if (store is TStore typed)
                return typed;
        }

        return null;
    }

    /// <summary>Raised whenever any store changes (after the per-store event).</summary>
    /// <remarks>
    /// Handlers are invoked one at a time and their exceptions are isolated and reported through
    /// <see cref="OnError"/>, so a raw <c>+=</c> handler that throws cannot stop the handlers behind
    /// it -- persistence and undo/redo both observe this event.
    /// </remarks>
    public event Action<BlexActionContext>? ActionDispatched;

    /// <summary>
    /// Invokes a multicast event one handler at a time, containing and reporting any exception, so
    /// that one misbehaving observer cannot starve the rest of the pipeline.
    /// </summary>
    internal void InvokeIsolated<T>(Delegate? handler, T argument, string source, string? detail)
    {
        if (handler is null)
            return;

        var targets = handler.GetInvocationList();
        for (var i = 0; i < targets.Length; i++)
        {
            try
            {
                switch (targets[i])
                {
                    case Action<T> typed:
                        typed(argument);
                        break;
                    case Action bare:
                        bare();
                        break;
                }
            }
            catch (Exception ex)
            {
                ReportError(source, ex, detail);
            }
        }
    }

    /// <summary>
    /// Subscribes to dispatched actions, optionally filtered. Returns a token; dispose it to stop
    /// listening. Handler exceptions are isolated so one reactor can't break dispatch. This is the
    /// building block for cross-store coordination (react to one store's action, mutate another).
    /// </summary>
    public IDisposable Subscribe(Action<BlexActionContext> handler, Func<BlexActionContext, bool>? filter = null)
    {
        ArgumentNullException.ThrowIfNull(handler);
        void Wrapped(BlexActionContext ctx)
        {
            try
            {
                // The filter is user code too, and it runs on the dispatch path: a throwing
                // predicate must be contained exactly like a throwing handler.
                if (filter is not null && !filter(ctx))
                    return;

                handler(ctx);
            }
            catch (Exception ex)
            {
                ReportError("subscriber", ex, ctx.QualifiedName);
            }
        }

        ActionDispatched += Wrapped;
        return new BlexSubscription(() => ActionDispatched -= Wrapped);
    }

    /// <summary>Subscribes to actions originating from a specific store type.</summary>
    public IDisposable SubscribeTo<TStore>(Action<BlexActionContext> handler) where TStore : IBlexStore
        => Subscribe(handler, ctx => ctx.Store is TStore);

    /// <summary>
    /// Subscribes to actions by name. Matches either the bare action name (e.g. <c>"Increment"</c>)
    /// or the qualified name (e.g. <c>"counter/Increment"</c>).
    /// </summary>
    public IDisposable SubscribeToAction(string actionName, Action<BlexActionContext> handler)
    {
        ArgumentNullException.ThrowIfNull(actionName);
        return Subscribe(handler, ctx => ctx.ActionName == actionName || ctx.QualifiedName == actionName);
    }

    /// <summary>
    /// Subscribes to one parameterless action, by its generated descriptor
    /// (<c>CounterStore.Actions.Increment</c>) rather than by name, so a rename breaks the build instead
    /// of silently ending the subscription. The handler receives the store instance that dispatched it.
    /// </summary>
    public IDisposable SubscribeToAction<TStore>(BlexAction<TStore> action, Action<TStore> handler)
        where TStore : class, IBlexStore
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(handler);
        return Subscribe(ctx => handler((TStore)ctx.Store), action.Matches);
    }

    /// <summary>
    /// Subscribes to one action by its generated descriptor (<c>CounterStore.Actions.Add</c>), handing
    /// the handler the store that dispatched it and the action's payload as the type the action takes
    /// -- the argument itself for a single-parameter action, a named tuple for several.
    /// </summary>
    /// <example><code>
    /// using var sub = manager.SubscribeToAction(OrdersStore.Actions.Place,
    ///     (orders, order) => notifications.Push($"{order.Quantity} x {order.Name}"));
    /// </code></example>
    public IDisposable SubscribeToAction<TStore, TPayload>(BlexAction<TStore, TPayload> action, Action<TStore, TPayload> handler)
        where TStore : class, IBlexStore
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(handler);
        return Subscribe(ctx => handler((TStore)ctx.Store, action.GetPayload(ctx)), action.Matches);
    }

    /// <summary>
    /// Observes a projection across the whole application and invokes <paramref name="onChanged"/>
    /// only when the selected value actually changes -- the cross-store counterpart to
    /// <see cref="BlexStoreSubscriptionExtensions.Subscribe{T}(IBlexStore, Func{T}, Action{T}, IEqualityComparer{T}, bool)"/>.
    /// Dispose the returned token to stop observing.
    /// </summary>
    /// <param name="selector">
    /// Projects the observed value. It runs after every action of every store, so keep it cheap --
    /// read already-memoized computed properties rather than recomputing over collections.
    /// </param>
    /// <param name="onChanged">Invoked with <c>(previous, current)</c> whenever the projection changes.</param>
    /// <param name="comparer">Custom equality; defaults to <see cref="EqualityComparer{T}.Default"/>.</param>
    /// <param name="fireImmediately">When <c>true</c>, invokes the callback once with the value at subscription time.</param>
    /// <remarks>
    /// Without this, a value derived from two stores has to be recomputed on every action of every
    /// store -- the cross-store equivalent of re-rendering the whole app on any change. Selector and
    /// callback exceptions are isolated and reported through <see cref="OnError"/>, like every other
    /// subscription; a throwing selector leaves the last known value in place so one bad read does
    /// not permanently wedge the subscription.
    /// <para>
    /// The projection is re-evaluated after state <em>restores</em> too (undo/redo, DevTools
    /// time-travel, an incoming cross-tab change), not only after actions. Those paths change state
    /// without dispatching anything, so a subscription that watched actions alone would keep
    /// reporting a value the application no longer holds. The same goes for the changes no action
    /// records: an effect's <c>IsLoading</c>/<c>Error</c>, and a store rolled back by
    /// <c>RollbackOnError</c>.
    /// </para>
    /// <para>
    /// The callback runs once middleware and DevTools have seen the action that caused the change, so
    /// a follow-up action it dispatches is recorded after the one it reacted to. What an async action
    /// changes is therefore announced when that action completes and is recorded, not at each
    /// <c>await</c> -- components bound to the store, and <see cref="CreateSelector{T}"/>, see it at once.
    /// (The projection reads live state, so a re-evaluation another store's action triggers meanwhile can
    /// already see such a change.)
    /// </para>
    /// </remarks>
    public IDisposable Subscribe<T>(
        Func<T> selector,
        Action<T, T> onChanged,
        IEqualityComparer<T>? comparer = null,
        bool fireImmediately = false)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(onChanged);
        var equality = comparer ?? EqualityComparer<T>.Default;
        var last = selector();

        void Evaluate()
        {
            var current = selector();
            if (equality.Equals(current, last))
                return;

            var previous = last;
            last = current;
            onChanged(previous, current);
        }

        // The action path is already isolated by Subscribe; the restore path needs its own guard.
        var actions = Subscribe(_ => Evaluate());
        void OnRestored()
        {
            try
            {
                Evaluate();
            }
            catch (Exception ex)
            {
                ReportError("subscriber", ex, "selector after restore");
            }
        }

        // Changes no action record or restore announces; already isolated by OnUnannouncedChange.
        void OnUnannounced(IBlexStore _) => Evaluate();

        StateRestored += OnRestored;
        UnannouncedChange += OnUnannounced;
        var subscription = new BlexSubscription(() =>
        {
            StateRestored -= OnRestored;
            UnannouncedChange -= OnUnannounced;
            actions.Dispose();
        });

        if (!fireImmediately)
            return subscription;

        try
        {
            onChanged(last, last);
        }
        catch
        {
            // The caller never receives the token when this throws, so the handler would stay
            // attached for the lifetime of the manager. Detach before letting it surface.
            subscription.Dispose();
            throw;
        }

        return subscription;
    }

    /// <summary>
    /// Single-value overload of
    /// <see cref="Subscribe{T}(Func{T}, Action{T, T}, IEqualityComparer{T}, bool)"/>, for callbacks
    /// that do not care about the previous value.
    /// </summary>
    public IDisposable Subscribe<T>(
        Func<T> selector,
        Action<T> onChanged,
        IEqualityComparer<T>? comparer = null,
        bool fireImmediately = false)
    {
        ArgumentNullException.ThrowIfNull(onChanged);
        return Subscribe<T>(selector, (_, current) => onChanged(current), comparer, fireImmediately);
    }

    /// <summary>
    /// Creates a memoized derived value over the whole application state -- what
    /// <c>[BlexComputedAttribute]</c> is to a single store, lifted to the manager. The projection
    /// runs at most once per action, lazily, however many readers ask for it in between. Dispose the
    /// returned selector to detach.
    /// </summary>
    /// <param name="selector">Projects the derived value. Runs on the reading thread.</param>
    /// <remarks>
    /// The counterpart to <see cref="Subscribe{T}(Func{T}, Action{T}, IEqualityComparer{T}, bool)"/>,
    /// which pushes changes to one observer and evaluates eagerly after every action. Use this when
    /// the value is <em>pulled</em> by several readers -- three components binding to the same
    /// cross-store total then pay for one computation per action instead of three, and an action
    /// none of them care about costs nothing. It subscribes to nothing: it compares a version the
    /// manager advances on every change of a registered store, so creating one adds no work to dispatch.
    /// </remarks>
    /// <example><code>
    /// using var overLimit = manager.CreateSelector(() => cart.Total > profile.CreditLimit);
    /// if (overLimit.Value) ShowBanner();
    /// </code></example>
    public BlexSelector<T> CreateSelector<T>(Func<T> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        return new BlexSelector<T>(this, selector);
    }

    /// <summary>
    /// Creates a <see cref="CreateSelector{T}">selector</see> per argument -- the line with id 42, the total of one
    /// category -- each memoized like a selector, created on first read and evicted least recently used beyond
    /// <paramref name="maxSize"/> arguments, so a list that has shown ten thousand ids does not keep ten thousand alive.
    /// </summary>
    /// <param name="selector">Projects the derived value for one argument. Runs on the reading thread.</param>
    /// <param name="maxSize">How many arguments to hold a value for at most. Defaults to 128.</param>
    /// <param name="comparer">Compares arguments; defaults to <see cref="EqualityComparer{T}.Default"/>.</param>
    /// <example><code>
    /// using var lineTotal = manager.CreateSelectorFamily((int id) => cart.Lines.First(l => l.Id == id).Total * tax.Rate);
    /// var total = lineTotal[42];
    /// </code></example>
    public BlexSelectorFamily<TArg, T> CreateSelectorFamily<TArg, T>(Func<TArg, T> selector, int maxSize = 128, IEqualityComparer<TArg>? comparer = null)
        where TArg : notnull
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxSize, 1);
        return new BlexSelectorFamily<TArg, T>(this, selector, maxSize, comparer);
    }

    /// <summary>
    /// Completes once <paramref name="condition"/> holds -- at once when it already does, otherwise after
    /// the first change of any registered store that makes it true. The workflow step an effect awaits:
    /// "once the user has signed in", "once the upload finished", "once both stores are loaded".
    /// </summary>
    /// <param name="condition">
    /// Re-checked after every change of a registered store and every restore; keep it cheap. A change made while an
    /// async action is still running counts as soon as it is notified, not once that action is recorded -- so an
    /// effect can wait for something its own store's actions do.
    /// </param>
    /// <param name="cancellationToken">Stops waiting; the task is then cancelled.</param>
    /// <returns>
    /// A task that completes when the condition holds, is cancelled with <paramref name="cancellationToken"/>,
    /// or faults with the condition's own exception.
    /// </returns>
    /// <example><code>
    /// [BlexEffect]
    /// private async Task OnCheckout(CancellationToken ct)
    /// {
    ///     await _manager.WhenAsync(() => _session.IsSignedIn, ct);   // MobX's when, RTK's condition
    ///     await _api.CheckoutAsync(Lines, ct);
    /// }
    /// </code></example>
    public Task WhenAsync(Func<bool> condition, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(condition);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        try
        {
            if (condition())
                return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            return Task.FromException(ex);
        }

        var waiter = new ConditionWaiter(this, condition);
        waiter.Start(cancellationToken);
        return waiter.Task;
    }

    /// <summary>
    /// Runs <paramref name="body"/> -- actions of several stores -- as one unit: if it throws, every store it changed is
    /// put back the way it was when the transaction began, and the exception reaches the caller. What
    /// <c>[BlexStore(RollbackOnError = true)]</c> does for one store's action, across stores.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The actions inside run, notify and record as usual -- middleware and DevTools see each of them. On a throw, the
    /// stores whose state moved are restored as one restore, like an undo: subscriptions re-evaluate once, persistence
    /// writes the restored state back (so a reload does not resurrect the half-done change), and undo steps the actions
    /// recorded no longer hold the state that was taken back. Stores the body did not change are left alone.
    /// </para>
    /// <para>
    /// Every registered store is serialized when the transaction begins, so it costs about what one DevTools snapshot
    /// does: meant for a multi-store operation, not for a hot loop. Synchronous only -- an async body would have shown
    /// the UI its intermediate state, and putting stores back after an await could overwrite changes made meanwhile.
    /// Call it around actions, not from inside one; hand-written <see cref="IBlexStore"/>s are not restored.
    /// </para>
    /// </remarks>
    /// <example><code>
    /// manager.Transaction(() =>
    /// {
    ///     inventory.Reserve(sku, quantity);
    ///     cart.Add(sku, quantity);
    ///     wallet.Charge(price * quantity);   // throws: the reservation and the cart line are taken back too
    /// });
    /// </code></example>
    public void Transaction(Action body)
    {
        ArgumentNullException.ThrowIfNull(body);
        Transaction<object?>(() =>
        {
            body();
            return null;
        });
    }

    /// <summary>Runs <paramref name="body"/> as one unit and returns its result; see <see cref="Transaction(Action)"/>.</summary>
    public T Transaction<T>(Func<T> body)
    {
        ArgumentNullException.ThrowIfNull(body);

        var stores = _stores;
        var before = new List<(BlexStoreBase Store, long Version, JsonObject State)>(stores.Length);
        foreach (var store in stores)
        {
            if (store is BlexStoreBase sb)
                before.Add((sb, sb.ChangeVersion, sb.SerializeState()));
        }

        var startedAt = StateVersion;
        try
        {
            return body();
        }
        catch
        {
            var changed = new Dictionary<string, JsonObject>(StringComparer.Ordinal);
            var rolledBack = new List<BlexStoreBase>();
            foreach (var (store, version, state) in before)
            {
                if (store.ChangeVersion == version || !_stores.Contains(store))
                    continue;

                changed[store.Name] = state;
                rolledBack.Add(store);
            }

            if (changed.Count > 0)
            {
                RestoreSlices(changed);

                // The undo steps recorded since it began hold what was just taken back.
                foreach (var store in rolledBack)
                    OnRolledBack(store, startedAt);
            }

            throw;
        }
    }

    /// <summary>One <see cref="WhenAsync"/>: re-checks its condition on every change until it holds, then lets go.</summary>
    private sealed class ConditionWaiter(BlexManager manager, Func<bool> condition)
    {
        private readonly TaskCompletionSource _done = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private CancellationTokenRegistration _cancellation;

        public Task Task => _done.Task;

        public void Start(CancellationToken cancellationToken)
        {
            // Every applied change (not ActionDispatched, which a change inside a running async action reaches only
            // once that action ends), restores, and a hand-written store's own notifications.
            manager.StoreChanged += OnChange;
            manager.StateRestored += Check;
            manager.UnannouncedChange += OnChange;
            if (cancellationToken.CanBeCanceled)
            {
                _cancellation = cancellationToken.Register(() =>
                {
                    if (_done.TrySetCanceled(cancellationToken))
                        Stop();
                });
            }
        }

        private void OnChange(IBlexStore _) => Check();

        private void Check()
        {
            if (_done.Task.IsCompleted)
                return;

            try
            {
                if (!condition())
                    return;

                _done.TrySetResult();
            }
            catch (Exception ex)
            {
                // The caller asked for this condition; a failing one is theirs to see, not to have retried.
                _done.TrySetException(ex);
            }

            Stop();
        }

        private void Stop()
        {
            manager.StoreChanged -= OnChange;
            manager.StateRestored -= Check;
            manager.UnannouncedChange -= OnChange;
            _cancellation.Dispose();
        }
    }

    /// <summary>
    /// Subscribes with an asynchronous reactor (e.g. to trigger an effect on another store). The
    /// returned task is observed; failures are logged rather than surfaced as unobserved exceptions.
    /// </summary>
    public IDisposable SubscribeAsync(Func<BlexActionContext, Task> handler, Func<BlexActionContext, bool>? filter = null)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return Subscribe(ctx => ObserveAsync(handler(ctx)), filter);
    }

    private void ObserveAsync(Task task)
    {
        if (task.IsCompletedSuccessfully)
            return;

        _ = Awaited(this, task);

        static async Task Awaited(BlexManager manager, Task t)
        {
            try
            {
                await t.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                manager.ReportError("async reactor", ex);
            }
        }
    }

    private sealed class BlexSubscription(Action dispose) : IDisposable
    {
        private Action? _dispose = dispose;

        public void Dispose()
        {
            _dispose?.Invoke();
            _dispose = null;
        }
    }

    /// <summary>Registers a store with the manager. Idempotent.</summary>
    /// <remarks>
    /// Store names key the global state tree, the DevTools slices and the persistence storage keys,
    /// so they must be unique. Registering a second store under a name already in use is reported
    /// through <see cref="OnError"/> (rather than throwing) and the store is still registered, but
    /// the two will shadow each other -- give one of them an explicit <c>[BlexStoreAttribute(Name = "...")]</c>.
    /// </remarks>
    public void Register(IBlexStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        var duplicate = false;
        lock (_gate)
        {
            var current = _stores;
            foreach (var existing in current)
            {
                if (ReferenceEquals(existing, store))
                    return;
                if (existing.Name == store.Name)
                    duplicate = true;
            }

            var next = new IBlexStore[current.Length + 1];
            Array.Copy(current, next, current.Length);
            next[current.Length] = store;
            _stores = next;

            if (store is BlexStoreBase sb)
            {
                sb.Attach(this);
            }
            else
            {
                // A hand-written store tells nobody but its own subscribers; listen there so selectors
                // and subscriptions over it stay current.
                void OnForeignChange()
                {
                    MarkStateChanged();
                    OnUnannouncedChange(store);
                }

                _foreignHandlers[store] = OnForeignChange;
                store.StateChanged += OnForeignChange;
            }

            // Whatever a selector memoized may have read the store's state before it joined.
            MarkStateChanged();
        }

        if (duplicate)
        {
            ReportError(
                "register",
                new InvalidOperationException($"More than one store is named '{store.Name}'."),
                $"duplicate store name '{store.Name}'; state slices, DevTools entries and persistence keys will collide -- set a unique [BlexStoreAttribute(Name = \"...\")]");
        }

        InvokeIsolated(StoreRegistered, store, "register", store.Name);
    }

    /// <summary>
    /// Raised after a store joins the manager -- at startup, and later for a lazily loaded feature or a
    /// <see cref="CreateScope{TStore}(TStore, string)">scoped</see> instance. Persistence uses it to
    /// hydrate a persistent store that arrives after startup. Handler exceptions are isolated and
    /// reported through <see cref="OnError"/>.
    /// </summary>
    public event Action<IBlexStore>? StoreRegistered;

    /// <summary>
    /// Raised after a store leaves the manager (see <see cref="Unregister"/>). Persistence uses it to
    /// write a debounced save the store still owes before it is gone. Handler exceptions are isolated
    /// and reported through <see cref="OnError"/>.
    /// </summary>
    public event Action<IBlexStore>? StoreUnregistered;

    /// <summary>
    /// Registers <paramref name="store"/> as one instance among several of the same store type, under
    /// the name <c>"{name}:{key}"</c>, and returns a scope that unregisters and disposes it again.
    /// </summary>
    /// <param name="store">A store that is not registered with any manager yet.</param>
    /// <param name="key">
    /// Distinguishes this instance from the others: an order id, a document id, a tab index. Give a
    /// stable one to a store marked <c>Persist = true</c>, so the next instance for the same entity
    /// finds its saved state. When omitted, a key unique within this manager is assigned -- and since
    /// nothing could ever find state saved under it again (the next session, or another tab, counts its
    /// keys from the start too), an instance keyed that way is never persisted, whatever its store says.
    /// </param>
    /// <remarks>
    /// Stores are otherwise one per DI scope, keyed by their generated name, so a second instance --
    /// the form inside one of two open dialogs, one of several editor tabs -- would collide with the
    /// first in the state tree, DevTools and storage. A scope gives each instance its own slice while
    /// keeping it a full citizen: its actions reach middleware, subscribers, DevTools and history, and
    /// persistence hydrates it the moment it registers.
    /// </remarks>
    /// <example><code>
    /// using var editor = manager.CreateScope(new DocumentStore(api), key: documentId);
    /// editor.Store.Rename("Draft");   // recorded as "document:42/Rename"
    /// </code></example>
    public BlexStoreScope<TStore> CreateScope<TStore>(TStore store, string? key = null)
        where TStore : BlexStoreBase
    {
        ArgumentNullException.ThrowIfNull(store);
        if (key is not null && string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("An instance key must not be empty.", nameof(key));

        var generated = key is null;
        key ??= Interlocked.Increment(ref _scopeSequence).ToString(CultureInfo.InvariantCulture);

        var unkeyed = store.Name;
        store.AssignInstanceKey(key, generated);
        if (store.Name == unkeyed)
        {
            // A hand-written store whose Name ignores the key would silently share one slice, one
            // DevTools entry and one storage key with every other instance.
            store.AssignInstanceKey(null, generated: false);
            throw new NotSupportedException(
                $"The store '{unkeyed}' cannot be scoped: its Name does not include the instance key. Generated stores support it; a hand-written one must return WithInstanceKey(\"...\") from Name.");
        }

        Register(store);
        return new BlexStoreScope<TStore>(this, store, key);
    }

    /// <summary>
    /// Removes a store from the manager (e.g. when a lazily-loaded feature is torn down).
    /// The store keeps working standalone; its actions are simply no longer observed.
    /// </summary>
    public void Unregister(IBlexStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        lock (_gate)
        {
            var current = _stores;
            var index = Array.IndexOf(current, store);
            if (index < 0)
                return;

            var next = new IBlexStore[current.Length - 1];
            Array.Copy(current, next, index);
            Array.Copy(current, index + 1, next, index, current.Length - index - 1);
            _stores = next;

            if (store is BlexStoreBase sb)
                sb.Detach();
            else if (_foreignHandlers.Remove(store, out var handler))
                store.StateChanged -= handler;

            // A selector over the store would otherwise keep a memo nothing invalidates any more.
            MarkStateChanged();
        }

        InvokeIsolated(StoreUnregistered, store, "unregister", store.Name);
    }

    /// <summary>
    /// Resets every registered store to the state it had when it was first registered -- the
    /// application-wide counterpart to <see cref="BlexStoreBase.ResetState"/> (Pinia's
    /// <c>$reset</c> applied across the whole tree). Each store resets as its own normal,
    /// vetoable, observable, persisted and time-travelable <c>ResetState</c> action, so a filter
    /// can exempt individual stores.
    /// </summary>
    /// <remarks>
    /// A store that fails to reset (a throwing deserializer, a throwing subscriber) is reported
    /// through <see cref="OnError"/> and skipped; the remaining stores still reset. Stores that
    /// were never registered through this manager are untouched.
    /// </remarks>
    public void ResetAll()
    {
        // Snapshot the registry: each reset raises StateChanged, and a handler that registers or
        // unregisters a store swaps in a new array rather than mutating this one.
        foreach (var store in _stores)
        {
            if (store is not BlexStoreBase sb)
                continue;

            try
            {
                sb.ResetState();
            }
            catch (Exception ex)
            {
                ReportError("reset", ex, store.Name);
            }
        }
    }

    /// <summary>Attaches a DevTools sink and sends the current state as the initial snapshot.</summary>
    public void ConnectDevTools(IBlexDevTools devTools)
    {
        ArgumentNullException.ThrowIfNull(devTools);
        _devTools = devTools;
        var state = CaptureGlobalState();
        _initialState ??= state;
        _committedState = state;
        _connected = true;
        SetDevToolsStatus(paused: false, locked: false);
        devTools.Init(SanitizeState(state));
    }

    /// <summary>Detaches the DevTools sink (e.g. when the bridge is disposed).</summary>
    /// <remarks>
    /// Clears the monitor's pause/lock flags: a monitor closed (or a circuit torn down) while
    /// "Lock changes" was on would otherwise leave the application permanently frozen with no
    /// remaining way to unfreeze it.
    /// </remarks>
    public void DisconnectDevTools()
    {
        _devTools = null;
        _connected = false;
        SetDevToolsStatus(paused: false, locked: false);
    }

    /// <summary>Builds the global state tree: <c>{ "&lt;StoreName&gt;": &lt;state&gt;, ... }</c>.</summary>
    public JsonObject CaptureGlobalState()
    {
        var root = new JsonObject();
        foreach (var store in _stores)
            root[store.Name] = store.SerializeState();

        return root;
    }

    /// <summary>
    /// Actions whose observers are currently being notified. A reactor that dispatches a
    /// follow-up action would otherwise change state before a later observer of the *original*
    /// action lazily captures its snapshot; freezing in-flight snapshots first keeps attribution
    /// correct.
    /// </summary>
    private readonly List<BlexActionContext> _inFlight = [];

    private void FreezeInFlightSnapshots()
    {
        for (var i = 0; i < _inFlight.Count; i++)
            _ = _inFlight[i].GlobalState;
    }

    // The action bodies running in the current asynchronous flow, innermost first. An action recorded
    // while one of this manager's is running was dispatched from inside it -- a cross-store call such as
    // cart.Checkout() calling orders.Place() -- so replaying the outer action runs it again. Flowing with
    // the execution context rather than counted keeps an unrelated action, started elsewhere while an
    // async action awaits, from passing for a nested one.
    private static readonly AsyncLocal<ActionFrame?> RunningActions = new();

    internal sealed class ActionFrame(BlexManager manager, ActionFrame? outer)
    {
        public BlexManager Manager { get; } = manager;

        public ActionFrame? Outer { get; } = outer;
    }

    /// <summary>
    /// Marks an action body of one of this manager's stores as running in the current flow until the
    /// returned scope is exited. Free when nothing records actions: only a recorded action can be nested.
    /// </summary>
    internal ActionBodyScope EnterActionBody()
    {
        if (!HasObservers)
            return default;

        var outer = RunningActions.Value;
        RunningActions.Value = new ActionFrame(this, outer);
        return new ActionBodyScope(outer);
    }

    /// <summary>An action body entered by <see cref="EnterActionBody"/>; exit it before the action records itself.</summary>
    internal readonly struct ActionBodyScope
    {
        private readonly ActionFrame? _outer;
        private readonly bool _entered;

        internal ActionBodyScope(ActionFrame? outer)
        {
            _outer = outer;
            _entered = true;
        }

        public void Exit()
        {
            if (_entered)
                RunningActions.Value = _outer;
        }
    }

    private bool IsInsideAction()
    {
        for (var frame = RunningActions.Value; frame is not null; frame = frame.Outer)
        {
            if (ReferenceEquals(frame.Manager, this))
                return true;
        }

        return false;
    }

    // The manager a replay (Bit.Blex.Testing's ReplayAsync) dispatches into, in the replay's own flow.
    private static readonly AsyncLocal<BlexManager?> Replaying = new();

    /// <summary>
    /// Whether the current flow is replaying recorded actions into this manager. Effects skip their
    /// debounce wait then: it only decides which of several rapid invocations runs, a log holds only the
    /// ones that did, and a replay awaits each before starting the next -- so waiting would change nothing
    /// but the time a replay takes, and on a fake clock it would never end.
    /// </summary>
    internal bool IsReplaying => ReferenceEquals(Replaying.Value, this);

    /// <summary>Marks the current flow as replaying into this manager until the returned scope is disposed.</summary>
    internal ReplayScope BeginReplay()
    {
        var previous = Replaying.Value;
        Replaying.Value = this;
        return new ReplayScope(previous);
    }

    /// <summary>A replay begun by <see cref="BeginReplay"/>.</summary>
    internal readonly struct ReplayScope(BlexManager? previous) : IDisposable
    {
        public void Dispose() => Replaying.Value = previous;
    }

    /// <summary>
    /// Runs the before-action pipeline. Returns <c>false</c> if any middleware vetoed the action,
    /// in which case the caller must skip the mutation. Cheap no-op when no middleware is registered.
    /// </summary>
    internal bool BeforeAction(IBlexStore source, string actionName, IReadOnlyList<BlexActionArg>? args = null)
    {
        // This runs just before a new mutation; pin the snapshots of any actions still being
        // observed so their lazily-captured GlobalState reflects *their* state, not this one's.
        if (_inFlight.Count > 0)
            FreezeInFlightSnapshots();

        // "Lock changes" in the DevTools monitor freezes the running application: every action is
        // refused until it is unlocked. Restores are deliberately not gated, so the slider keeps
        // working while the app is frozen -- which is the whole point of the button.
        if (_changesLocked)
            return false;

        if (_middleware.Count == 0)
            return true;

        var context = new BlexPreActionContext(source, actionName, args);
        foreach (var mw in _middleware)
        {
            try
            {
                mw.BeforeAction(context);
            }
            catch (Exception ex)
            {
                // A misbehaving filter must not break dispatch.
                ReportError("middleware", ex, context.QualifiedName);
            }

            if (context.IsCancelled)
                return false;
        }

        return !context.IsCancelled;
    }

    internal void RecordAction(IBlexStore source, string actionName, IReadOnlyList<BlexActionArg>? args = null)
    {
        // Skip the whole pipeline when nothing is listening (no middleware, no DevTools, no
        // subscribers). The global state itself is captured lazily by the context, so observers
        // that never read it (e.g. persistence) don't pay for full-tree serialization either.
        if (!HasObservers)
            return;

        var context = new BlexActionContext(source, actionName, CaptureGlobalState, ++_sequence, args, _timeProvider.GetUtcNow(), IsInsideAction());

        _inFlight.Add(context);
        try
        {
            foreach (var mw in _middleware)
            {
                try
                {
                    mw.OnAction(context);
                }
                catch (Exception ex)
                {
                    // Middleware must not break dispatch. Errors are isolated and reported.
                    ReportError("middleware", ex, context.QualifiedName);
                }
            }

            // DevTools must see this action before any subscriber reacts: a reactor that
            // dispatches a follow-up action re-enters RecordAction, and sending that first would
            // invert the extension's timeline (breaking time-travel ordering).
            if (_connected && !_devToolsPaused && _devTools is not null)
            {
                var global = context.GlobalState;
                _initialState ??= global;
                _devTools.Send(SanitizeAction(context.QualifiedName), SanitizeState(global), BuildDevToolsPayload(context.Args));
            }

            InvokeIsolated(ActionDispatched, context, "subscriber", context.QualifiedName);
        }
        finally
        {
            _inFlight.RemoveAt(_inFlight.Count - 1);
        }
    }

    /// <summary>
    /// Raised when an action's body throws. The exception still propagates to the caller (an
    /// effect's generated wrapper captures it into <c>XxxError</c>); this is the observation hook
    /// for logging, telemetry and error reporting -- the counterpart to Pinia's <c>$onAction</c>
    /// <c>onError</c> and Redux's error-reporting middleware.
    /// </summary>
    /// <remarks>
    /// Cancellation is not a failure: an <see cref="OperationCanceledException"/> from an action
    /// or effect does not raise this event. Handler exceptions are isolated and reported through
    /// <see cref="OnError"/>.
    /// </remarks>
    public event Action<BlexActionErrorContext>? ActionFailed;

    // The exception most recently reported, so one failure travelling out through nested actions
    // is reported once rather than once per frame. Held weakly: the reference exists only to
    // compare against the next report, and keeping a strong one would pin an exception (and its
    // whole captured stack) alive for the lifetime of the manager.
    private readonly WeakReference<Exception> _lastReportedError = new(null!);

    /// <summary>
    /// Reports that an action body threw. Runs the middleware error hooks and raises
    /// <see cref="ActionFailed"/>, both isolated: reporting a failure must never replace it with a
    /// different one.
    /// </summary>
    internal void RecordActionError(IBlexStore source, string actionName, IReadOnlyList<BlexActionArg>? args, Exception exception)
    {
        // A cancelled action (CancelXxx(), a superseded Latest run, a disposed store) is a normal
        // outcome that the effect wrapper deliberately does not record as an error either.
        if (exception is OperationCanceledException)
            return;

        // An action calling another action -- or an effect body calling one -- means the same
        // exception passes through several dispatch frames on its way out. Report the innermost
        // one, which names the action that actually threw, and let the outer frames re-throw
        // silently. A reporter that fired once per frame would multiply every nested failure.
        if (_lastReportedError.TryGetTarget(out var previous) && ReferenceEquals(previous, exception))
            return;

        _lastReportedError.SetTarget(exception);

        if (_middleware.Count == 0 && ActionFailed is null)
            return;

        var context = new BlexActionErrorContext(source, actionName, args, exception, _timeProvider.GetUtcNow());
        foreach (var mw in _middleware)
        {
            try
            {
                mw.OnActionError(context);
            }
            catch (Exception ex)
            {
                ReportError("middleware", ex, context.QualifiedName);
            }
        }

        InvokeIsolated(ActionFailed, context, "subscriber", context.QualifiedName);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "BlexJson.Options is the app's trimming seam (BlexJson.Configure sets a source-generated TypeInfoResolver), and an argument that cannot be serialized is caught and shown as <unserializable>.")]
    [UnconditionalSuppressMessage("AOT", "IL3050",
        Justification = "BlexJson.Options is the app's trimming seam (BlexJson.Configure sets a source-generated TypeInfoResolver), and an argument that cannot be serialized is caught and shown as <unserializable>.")]
    private JsonObject? BuildDevToolsPayload(IReadOnlyList<BlexActionArg> args)
    {
        if (args.Count == 0)
            return null;

        var payload = new JsonObject();
        foreach (var arg in args)
        {
            JsonNode? node;
            try
            {
                node = JsonSerializer.SerializeToNode(arg.Value, BlexJson.Options);
            }
            catch
            {
                node = "<unserializable>";
            }

            payload[arg.Name] = node;
        }

        // Run the payload through the state sanitizer as well so key-based redaction
        // (RedactDevToolsKeys) covers action arguments too.
        return SanitizeState(payload);
    }

    private JsonObject SanitizeState(JsonObject state)
    {
        if (DevToolsStateSanitizer is null)
            return state;
        try
        {
            return DevToolsStateSanitizer(state) ?? state;
        }
        catch (Exception ex)
        {
            // Fail closed: a broken redaction sanitizer must not leak the data it was meant to
            // hide, so an empty tree is sent instead of the unsanitized one.
            ReportError("devtools", ex, "state sanitizer failed; state withheld from DevTools");
            return new JsonObject();
        }
    }

    private string SanitizeAction(string label)
    {
        if (DevToolsActionSanitizer is null)
            return label;
        try
        {
            return DevToolsActionSanitizer(label) ?? label;
        }
        catch (Exception ex)
        {
            // Fail closed, mirroring SanitizeState.
            ReportError("devtools", ex, "action sanitizer failed; label withheld from DevTools");
            return "<sanitizer-error>";
        }
    }

    /// <summary>
    /// Dispatches an action by its qualified name (<c>"counter/Add"</c>), with its arguments given as a
    /// JSON object keyed by parameter name (<c>{ "amount": 5 }</c>) -- the shape DevTools shows an
    /// action's payload in. The action runs through its generated wrapper, so it passes the filters,
    /// reaches middleware and is recorded exactly as a direct call would be.
    /// </summary>
    /// <param name="qualifiedName">
    /// <c>"{store name}/{action name}"</c>: a generated action or effect, <c>"Set {Property}"</c> for a
    /// state property, or <c>"ResetState"</c>.
    /// </param>
    /// <param name="payload">The arguments; omit for a parameterless action, and leave out an optional one to take its default.</param>
    /// <returns>The action's task; already completed for a synchronous action.</returns>
    /// <exception cref="InvalidOperationException">No registered store has an action with that name.</exception>
    /// <remarks>
    /// This is what makes actions data: a log recorded in one session can be replayed in another
    /// (<c>Bit.Blex.Testing</c>'s <c>ReplayAsync</c>), and the Redux DevTools dispatcher can fire an action
    /// against the running app. Arguments are matched by parameter name and converted with
    /// <see cref="BlexJson.Options"/>; an optional parameter the payload leaves out takes its declared default.
    /// </remarks>
    public Task DispatchAsync(string qualifiedName, JsonObject? payload = null)
    {
        ArgumentNullException.ThrowIfNull(qualifiedName);
        var (store, action) = ResolveActionOrThrow(qualifiedName);
        return action.InvokeAsync(store, ToArgs(payload));
    }

    /// <summary>
    /// Dispatches an action by its qualified name with already-materialized arguments -- typically
    /// the <see cref="BlexActionContext.Args"/> recorded when it ran before.
    /// </summary>
    /// <param name="qualifiedName"><c>"{store name}/{action name}"</c>, as <see cref="BlexActionContext.QualifiedName"/> reports it.</param>
    /// <param name="args">The arguments, matched to the action's parameters by name.</param>
    public Task DispatchAsync(string qualifiedName, IReadOnlyList<BlexActionArg> args)
    {
        ArgumentNullException.ThrowIfNull(qualifiedName);
        ArgumentNullException.ThrowIfNull(args);
        var (store, action) = ResolveActionOrThrow(qualifiedName);
        return action.InvokeAsync(store, args);
    }

    private (IBlexStore Store, BlexActionDescriptor Action) ResolveActionOrThrow(string qualifiedName)
        => ResolveAction(qualifiedName)
            ?? throw new InvalidOperationException($"No registered store has an action named '{qualifiedName}'. Use \"store/Action\", as DevTools labels it.");

    /// <summary>
    /// Finds the store and action a qualified name refers to. Matched against each store's name as a
    /// prefix rather than split at a '/', because both a store name and an action label may contain one.
    /// </summary>
    private (IBlexStore Store, BlexActionDescriptor Action)? ResolveAction(string qualifiedName)
    {
        foreach (var store in _stores)
        {
            var name = store.Name;
            if (store is BlexStoreBase sb
                && qualifiedName.Length > name.Length + 1
                && qualifiedName[name.Length] == '/'
                && qualifiedName.StartsWith(name, StringComparison.Ordinal)
                && sb.FindAction(qualifiedName[(name.Length + 1)..]) is { } action)
            {
                return (store, action);
            }
        }

        return null;
    }

    private static BlexActionArg[] ToArgs(JsonObject? payload)
    {
        if (payload is null || payload.Count == 0)
            return [];

        var args = new BlexActionArg[payload.Count];
        var i = 0;
        foreach (var (key, value) in payload)
            args[i++] = new BlexActionArg(key, value);

        return args;
    }

    /// <summary>
    /// Handles a raw message from the Redux DevTools extension (JSON serialized). Drives time-travel
    /// (JUMP_TO_STATE / JUMP_TO_ACTION / ROLLBACK / COMMIT / RESET / IMPORT_STATE), the recording
    /// controls (PAUSE_RECORDING / LOCK_CHANGES) and the dispatcher (ACTION).
    /// </summary>
    public void HandleDevToolsMessage(string messageJson)
    {
        try
        {
            HandleDevToolsMessageCore(messageJson);
        }
        catch (Exception ex)
        {
            // The extension is untrusted input reaching us through a JS interop callback: a message
            // whose shape drifted (a non-string "type", a payload that isn't an object, ...) must
            // not throw back into interop and tear the circuit down.
            ReportError("devtools", ex, "handling extension message");
        }
    }

    private void HandleDevToolsMessageCore(string messageJson)
    {
        JsonNode? message;
        try
        {
            message = JsonNode.Parse(messageJson);
        }
        catch
        {
            return;
        }

        if (message is null)
            return;

        var type = AsString(message["type"]);

        // The extension sends START when its monitor is (re)opened. Re-sending the current tree as
        // the initial snapshot is what makes a monitor opened after the app booted show state
        // instead of staying blank until the next action.
        if (type == "START")
        {
            if (_connected && _devTools is not null)
            {
                var state = CaptureGlobalState();
                _initialState ??= state;
                _committedState ??= state;
                _devTools.Init(SanitizeState(state));
            }

            return;
        }

        // The monitor's dispatcher: the text typed into it, which names an action of this app.
        if (type == "ACTION")
        {
            DispatchFromDevTools(message["payload"]);
            return;
        }

        if (type != "DISPATCH")
            return;

        var payloadType = AsString(message["payload"]?["type"]);
        switch (payloadType)
        {
            case "JUMP_TO_STATE":
            case "JUMP_TO_ACTION":
                ApplyStateString(AsString(message["state"]));
                break;

            case "ROLLBACK":
                // Revert to the last committed snapshot if we have one; otherwise fall back to
                // the state the extension supplied with the message.
                if (_committedState is not null)
                    RestoreGlobalState(_committedState);
                else
                    ApplyStateString(AsString(message["state"]));

                // The monitor discards the actions after the commit only when it is re-initialized: without
                // this its timeline keeps them, and jumping to one resurrects the state just rolled back.
                _devTools?.Init(SanitizeState(CaptureGlobalState()));
                break;

            case "RESET":
                if (_initialState is not null)
                    RestoreGlobalState(_initialState);

                // The monitor now starts from here, so this is also what a later ROLLBACK returns to -- not a
                // commit made before the reset, which the monitor no longer shows.
                _committedState = CaptureGlobalState();
                _devTools?.Init(SanitizeState(_committedState));
                break;

            case "COMMIT":
                _committedState = CaptureGlobalState();
                _devTools?.Init(SanitizeState(_committedState));
                break;

            // "Pause recording" stops the monitor's timeline without freezing the app: actions keep
            // applying and keep reaching middleware and subscribers, they are just not streamed.
            case "PAUSE_RECORDING":
                SetDevToolsStatus(AsBoolean(message["payload"]?["status"]) ?? !_devToolsPaused, _changesLocked);
                break;

            // "Lock changes" freezes the application: every action is vetoed until it is unlocked.
            case "LOCK_CHANGES":
                SetDevToolsStatus(_devToolsPaused, AsBoolean(message["payload"]?["status"]) ?? !_changesLocked);
                break;

            case "IMPORT_STATE":
                var lifted = message["payload"]?["nextLiftedState"] as JsonObject;
                var computed = lifted?["computedStates"]?.AsArray();
                var last = computed?.LastOrDefault();
                if (last?["state"] is not JsonObject importState)
                    break;

                RestoreGlobalState(importState);

                // The imported session's base is what a ROLLBACK returns to from here on.
                if (lifted!["committedState"] is JsonObject committed)
                    _committedState = (JsonObject)committed.DeepClone();

                // The monitor shows an imported session only once the app hands the lifted state back to it.
                _devTools?.SendLiftedState((JsonObject)lifted.DeepClone());
                break;
        }
    }

    /// <summary>
    /// Dispatches the action typed into the DevTools dispatcher. The extension hands the text over as
    /// typed, so it is parsed here as JSON: <c>{ "type": "counter/Add", "payload": { "amount": 5 } }</c>,
    /// the arguments inline beside <c>type</c>, or -- for a one-parameter action -- the argument itself as
    /// the <c>payload</c>, Redux style: <c>{ "type": "orders/Place", "payload": { "Name": "mug" } }</c>. An
    /// object payload is read as the argument itself unless it names the parameter.
    /// </summary>
    /// <remarks>
    /// Nothing typed into the monitor can be allowed to throw back into the interop callback, and the
    /// author is looking at the monitor, not at the app -- so every way it can go wrong is reported
    /// through <see cref="OnError"/> with what to type instead.
    /// </remarks>
    private void DispatchFromDevTools(JsonNode? payload)
    {
        JsonObject? action;
        try
        {
            action = payload switch
            {
                JsonObject obj => obj,
                JsonValue value when value.TryGetValue<string>(out var text) => JsonNode.Parse(text) as JsonObject,
                _ => null,
            };
        }
        catch (JsonException ex)
        {
            ReportError("devtools", ex, "the dispatched action is not JSON; write it as {\"type\": \"store/Action\", \"payload\": { \"argument\": value }}");
            return;
        }

        var name = AsString(action?["type"]);
        if (action is null || name is null)
        {
            ReportError("devtools", new InvalidOperationException("The dispatched action has no \"type\"."),
                "write it as {\"type\": \"store/Action\", \"payload\": { \"argument\": value }}");
            return;
        }

        if (ResolveAction(name) is not { } resolved)
        {
            ReportError("devtools", new InvalidOperationException($"No registered store has an action named '{name}'."), $"dispatching '{name}'");
            return;
        }

        var (store, descriptor) = resolved;
        BlexActionArg[] args;
        if (action["payload"] is JsonObject arguments && !IsWholeArgument(descriptor, arguments))
        {
            args = ToArgs(arguments);
        }
        else if (action.TryGetPropertyValue("payload", out var single) && descriptor.ParameterNames.Count == 1)
        {
            args = [new BlexActionArg(descriptor.ParameterNames[0], single)];
        }
        else
        {
            args = [.. action.Where(p => p.Key != "type").Select(p => new BlexActionArg(p.Key, p.Value))];
        }

        Task task;
        try
        {
            task = descriptor.InvokeAsync(store, args);
        }
        catch (Exception ex)
        {
            ReportError("devtools", ex, $"dispatching '{name}'");
            return;
        }

        if (task.IsCompleted is false || task.IsFaulted)
            _ = ObserveDevToolsDispatch(task, name);

        // A one-parameter action taking an object (an Order) is handed that object as its payload far
        // more often than an object naming the parameter ({ "order": {...} }); only the latter has the
        // parameter's name among its keys. An empty object names nothing and stands for no arguments.
        static bool IsWholeArgument(BlexActionDescriptor descriptor, JsonObject payload)
        {
            if (descriptor.ParameterNames.Count != 1 || payload.Count == 0)
                return false;

            foreach (var (key, _) in payload)
            {
                if (string.Equals(key, descriptor.ParameterNames[0], StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }
    }

    private async Task ObserveDevToolsDispatch(Task task, string name)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Cancelled, as any dispatch may be; not a failure.
        }
        catch (Exception ex)
        {
            ReportError("devtools", ex, $"dispatching '{name}'");
        }
    }

    /// <summary>
    /// Reads a node as a string without throwing when the extension sent a different JSON kind
    /// (<c>JsonNode.GetValue&lt;string&gt;()</c> would throw on a number/object/array).
    /// </summary>
    private static string? AsString(JsonNode? node)
        => node is JsonValue value && value.TryGetValue<string>(out var s) ? s : null;

    /// <summary>
    /// Reads a node as a boolean without throwing when the extension sent a different JSON kind,
    /// returning <c>null</c> when there is no usable value (the caller then toggles).
    /// </summary>
    private static bool? AsBoolean(JsonNode? node)
        => node is JsonValue value && value.TryGetValue<bool>(out var b) ? b : null;

    private void ApplyStateString(string? stateJson)
    {
        if (string.IsNullOrEmpty(stateJson))
            return;

        try
        {
            if (JsonNode.Parse(stateJson) is JsonObject obj)
                RestoreGlobalState(obj);
        }
        catch (Exception ex)
        {
            // A malformed or schema-drifted DevTools snapshot must not crash the interop callback.
            ReportError("devtools", ex, "time-travel restore");
        }
    }

    /// <summary>
    /// Raised after state moved without an action: <see cref="RestoreGlobalState"/> (time-travel,
    /// undo/redo) once per call, and a single store's restore -- persistence hydration, a change
    /// arriving from another tab, the prerender handoff, <see cref="BlexStoreBase.RestoreState"/> --
    /// once per store. Cross-store selectors and subscriptions use it to re-evaluate; an observer that
    /// only needs to revisit the stores that moved listens to <see cref="StoreRestored"/> instead.
    /// Handler exceptions are isolated and reported through <see cref="OnError"/>.
    /// </summary>
    public event Action? StateRestored;

    /// <summary>
    /// Raised after one store's state was restored without an action, with that store: for each store a
    /// <see cref="RestoreGlobalState"/> applies, and for a single store's restore (hydration, a change from
    /// another tab, the prerender handoff, <see cref="BlexStoreBase.RestoreState"/>). Raised before the
    /// <see cref="StateRestored"/> that follows. Persistence uses it to write back the restored store --
    /// and only that one -- so a reload does not resurrect the pre-restore state; undo/redo uses it to
    /// compare the one slice that moved. Handler exceptions are isolated and reported through
    /// <see cref="OnError"/>.
    /// </summary>
    public event Action<IBlexStore>? StoreRestored;

    /// <summary>Pins in-flight snapshots before a single store's state is restored.</summary>
    internal void PrepareForRestore()
    {
        if (_restoreBatchDepth == 0 && _inFlight.Count > 0)
            FreezeInFlightSnapshots();
    }

    /// <summary>
    /// Announces that <paramref name="store"/>'s state was restored. Without it, every observer that
    /// keeps derived state -- a cross-store selector's memo, a manager subscription's last value, the
    /// undo baseline -- would go on reporting the value from before a hydration or a cross-tab change,
    /// because no action ever told it otherwise.
    /// </summary>
    internal void OnStoreRestored(IBlexStore store)
    {
        InvokeIsolated(StoreRestored, store, "restore", store.Name);

        // A whole-tree restore raises StateRestored once, after its last store.
        if (_restoreBatchDepth > 0)
            return;

        InvokeIsolated<object?>(StateRestored, null, "restore", "state restored handler");
    }

    /// <summary>
    /// Applies a global state tree to every store without recording new actions. A store whose
    /// slice fails to deserialize is reported via <see cref="OnError"/> and skipped; the other
    /// stores still restore.
    /// </summary>
    public void RestoreGlobalState(JsonObject global)
    {
        ArgumentNullException.ThrowIfNull(global);
        RestoreSlicesCore(name => global[name] as JsonObject);
    }

    /// <summary>
    /// Restores <see cref="RestoreGlobalState"/>-style from slices keyed by store name that are not parented
    /// to a tree, so one slice can be shared by several snapshots (undo/redo). Each is applied as a copy.
    /// </summary>
    internal void RestoreSlices(IReadOnlyDictionary<string, JsonObject> slices)
        => RestoreSlicesCore(name => slices.TryGetValue(name, out var slice) ? slice : null);

    private void RestoreSlicesCore(Func<string, JsonObject?> sliceOf)
    {
        // A restore triggered from inside an observer (e.g. a reactor calling history.Undo())
        // mutates state just like a new action would; pin in-flight snapshots first so they
        // keep the state that belonged to their own actions.
        if (_inFlight.Count > 0)
            FreezeInFlightSnapshots();

        // Read the registry once: ApplyRestoredState raises StateChanged, and a handler that
        // registers/unregisters a store swaps in a new array rather than mutating this one.
        _restoreBatchDepth++;
        try
        {
            foreach (var store in _stores)
            {
                if (sliceOf(store.Name) is { } slice && store is BlexStoreBase sb)
                {
                    try
                    {
                        // Clone so the store owns its node (a JsonNode can't be parented twice).
                        var clone = (JsonObject)slice.DeepClone();
                        sb.ApplyRestoredState(clone);
                    }
                    catch (Exception ex)
                    {
                        ReportError("restore", ex, store.Name);
                    }
                }
            }
        }
        finally
        {
            _restoreBatchDepth--;
        }

        InvokeIsolated<object?>(StateRestored, null, "restore", "state restored handler");
    }
}
