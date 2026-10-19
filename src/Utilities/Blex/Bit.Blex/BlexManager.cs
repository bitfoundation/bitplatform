using System.Diagnostics.CodeAnalysis;
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
    private bool _connected;
    private bool _devToolsPaused;
    private bool _changesLocked;

    /// <summary>Creates a manager with the supplied middleware (order preserved).</summary>
    public BlexManager(IEnumerable<IBlexMiddleware>? middleware = null)
    {
        _middleware = middleware?.ToList() ?? [];
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

    /// <summary>Routes an isolated error to <see cref="OnError"/>, or <see cref="Console.Error"/> when unset.</summary>
    internal void ReportError(string source, Exception exception, string? detail = null)
    {
        var handler = OnError;
        if (handler is null)
        {
            Console.Error.WriteLine($"[Blex] {source} failed{(detail is null ? "" : $" ({detail})")}: {exception.Message}");
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
    /// reporting a value the application no longer holds.
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

        StateRestored += OnRestored;
        var subscription = new BlexSubscription(() =>
        {
            StateRestored -= OnRestored;
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
    /// none of them care about costs nothing.
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
                sb.Attach(this);
        }

        if (duplicate)
        {
            ReportError(
                "register",
                new InvalidOperationException($"More than one store is named '{store.Name}'."),
                $"duplicate store name '{store.Name}'; state slices, DevTools entries and persistence keys will collide -- set a unique [BlexStoreAttribute(Name = \"...\")]");
        }
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
        }
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
        _devToolsPaused = false;
        _changesLocked = false;
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
        _devToolsPaused = false;
        _changesLocked = false;
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
    /// Runs the before-action pipeline. Returns <c>false</c> if any middleware vetoed the action,
    /// in which case the caller must skip the mutation. Cheap no-op when no middleware is registered.
    /// </summary>
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

        var context = new BlexActionContext(source, actionName, CaptureGlobalState, ++_sequence, args);

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

    /// <summary>
    /// Reports that an action body threw. Runs the middleware error hooks and raises
    /// <see cref="ActionFailed"/>, both isolated: reporting a failure must never replace it with a
    /// different one.
    /// </summary>
    // The exception most recently reported, so one failure travelling out through nested actions
    // is reported once rather than once per frame. Held weakly: the reference exists only to
    // compare against the next report, and keeping a strong one would pin an exception (and its
    // whole captured stack) alive for the lifetime of the manager.
    private readonly WeakReference<Exception> _lastReportedError = new(null!);

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

        var context = new BlexActionErrorContext(source, actionName, args, exception);
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
    /// Handles a raw message from the Redux DevTools extension (JSON serialized). Drives time-travel:
    /// JUMP_TO_STATE / JUMP_TO_ACTION / ROLLBACK / COMMIT / RESET.
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
                break;

            case "RESET":
                if (_initialState is not null)
                    RestoreGlobalState(_initialState);
                _devTools?.Init(SanitizeState(CaptureGlobalState()));
                break;

            case "COMMIT":
                _committedState = CaptureGlobalState();
                _devTools?.Init(SanitizeState(_committedState));
                break;

            // "Pause recording" stops the monitor's timeline without freezing the app: actions keep
            // applying and keep reaching middleware and subscribers, they are just not streamed.
            case "PAUSE_RECORDING":
                _devToolsPaused = AsBoolean(message["payload"]?["status"]) ?? !_devToolsPaused;
                break;

            // "Lock changes" freezes the application: every action is vetoed until it is unlocked.
            case "LOCK_CHANGES":
                _changesLocked = AsBoolean(message["payload"]?["status"]) ?? !_changesLocked;
                break;

            case "IMPORT_STATE":
                var computed = message["payload"]?["nextLiftedState"]?["computedStates"]?.AsArray();
                var last = computed?.LastOrDefault();
                if (last?["state"] is JsonObject importState)
                    RestoreGlobalState(importState);
                break;
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
    /// Raised after <see cref="RestoreGlobalState"/> applies a snapshot (time-travel, undo/redo).
    /// Persistence uses this to write the restored state back to storage so a reload does not
    /// resurrect the pre-restore state. Handler exceptions are isolated and reported through
    /// <see cref="OnError"/>.
    /// </summary>
    public event Action? StateRestored;

    /// <summary>
    /// Applies a global state tree to every store without recording new actions. A store whose
    /// slice fails to deserialize is reported via <see cref="OnError"/> and skipped; the other
    /// stores still restore.
    /// </summary>
    public void RestoreGlobalState(JsonObject global)
    {
        ArgumentNullException.ThrowIfNull(global);

        // A restore triggered from inside an observer (e.g. a reactor calling history.Undo())
        // mutates state just like a new action would; pin in-flight snapshots first so they
        // keep the state that belonged to their own actions.
        if (_inFlight.Count > 0)
            FreezeInFlightSnapshots();

        // Read the registry once: ApplyRestoredState raises StateChanged, and a handler that
        // registers/unregisters a store swaps in a new array rather than mutating this one.
        foreach (var store in _stores)
        {
            if (global[store.Name] is JsonObject slice && store is BlexStoreBase sb)
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

        InvokeIsolated<object?>(StateRestored, null, "restore", "state restored handler");
    }
}
