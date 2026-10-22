using System.Text.Json.Nodes;

namespace Bit.Blex;

/// <summary>
/// Coordinates automatic persistence: rehydrates persistent stores on startup and saves their
/// slice through an <see cref="IBlexStorage"/> after every action (optionally debounced).
/// A store opts in via <c>[BlexStoreAttribute(Persist = true)]</c>. Restored state (undo/redo, time-travel)
/// is also written back so a reload never resurrects pre-restore state. Writes are serialized
/// in dispatch order so a stale payload can never overwrite a newer one.
/// Single-threaded dispatch is assumed (Blazor's model).
/// </summary>
/// <remarks>
/// A persistent store registered after startup -- a lazily loaded feature, a
/// <see cref="BlexManager.CreateScope{TStore}(TStore, string)">scoped</see> instance -- is hydrated as
/// soon as it registers, and its saves wait until that has happened, exactly as every store's do at
/// startup; changes made meanwhile are written once the read finds nothing stored to restore instead.
/// A hydration that fails keeps the store's saves waiting until <see cref="RehydrateAsync"/> reads it
/// after all. A store that leaves the manager writes any debounced save it still owes first. A scoped
/// instance whose key the manager assigned is never persisted: no later instance could find its state.
/// </remarks>
public sealed class BlexStatePersistor : IAsyncDisposable, IDisposable
{
    private const string VersionKey = "__blexVersion";
    private const string StateKey = "state";

    private readonly BlexManager _manager;
    private readonly IBlexStorage _storage;
    private readonly BlexPersistenceOptions _options;
    private readonly HashSet<string> _pending = [];

    // Stores whose persisted state has been read (applied, or found absent). A store missing from it
    // when it registers after startup still needs hydrating; held by reference, so a new instance
    // under a name seen before is hydrated in its own right.
    private readonly HashSet<IBlexStore> _hydrated = new(ReferenceEqualityComparer.Instance);

    // Stores registered after startup whose stored state has not been read yet, by name (saves are
    // scheduled by name). Their saves wait: writing a store's declared defaults before its stored state
    // has been read would overwrite the user's data. The hold names its instance so a store leaving
    // mid-hydration cannot release the hold of the next instance to arrive under the same name.
    private readonly Dictionary<string, HydrationHold> _hydrating = [];

    private sealed class HydrationHold(IBlexStore store)
    {
        public IBlexStore Store { get; } = store;

        // False once the late hydration failed: the store then waits for RehydrateAsync to read it.
        public bool InFlight { get; set; } = true;

        // A save was asked for while the hold was on.
        public bool SaveRequested { get; set; }
    }

    // Debounced saves can resume off the dispatch thread when no SynchronizationContext exists
    // (plain hosts); this gate keeps the pending set, the debounce source and the write chain
    // consistent in that case.
    private readonly Lock _saveGate = new();
    private Task _writeChain = Task.CompletedTask;
    private Task _externalChain = Task.CompletedTask;
    private Task _lateChain = Task.CompletedTask;
    private CancellationTokenSource? _debounce;
    private DateTimeOffset? _pendingSince;
    private bool _subscribed;
    private bool _watchingRegistry;
    private bool _suspended;
    private bool _watching;
    private TaskCompletionSource? _hydration;

    /// <summary>Creates a persistor over a manager and storage sink.</summary>
    public BlexStatePersistor(BlexManager manager, IBlexStorage storage, BlexPersistenceOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(storage);
        _manager = manager;
        _storage = storage;
        _options = options ?? new BlexPersistenceOptions();
    }

    private string KeyFor(IBlexStore store) => _options.KeyPrefix + store.Name;

    /// <summary>
    /// Whether <paramref name="store"/> is read from and written to storage: it is marked
    /// <c>Persist = true</c>, and is not a scoped instance whose key the manager made up. Those keys
    /// count from the start again in every session and every tab, so state saved under one would only
    /// ever be found by an unrelated instance that drew the same number.
    /// </summary>
    private static bool Persists(IBlexStore store)
        => store.Persist && store is not BlexStoreBase { HasGeneratedInstanceKey: true };

    /// <summary>
    /// Whether <see cref="RehydrateAsync"/> has completed at least once, so the stores hold
    /// whatever was persisted rather than their declared defaults (zustand's <c>hasHydrated</c>).
    /// </summary>
    /// <remarks>
    /// The distinction matters whenever hydration is not instantaneous -- browser storage behind JS
    /// interop, a deferred <see cref="BlexPersistenceOptions.SkipHydration"/> start, a Blazor Server
    /// prerender. Until this turns <c>true</c>, a UI bound to a persisted store is rendering
    /// defaults, not the user's data: show a skeleton rather than briefly flashing "0 items" at
    /// someone whose cart is full.
    /// </remarks>
    public bool HasHydrated { get; private set; }

    /// <summary>
    /// Raised the first time hydration completes (and again after each later
    /// <see cref="RehydrateAsync"/>), so a component can re-render once real state has landed.
    /// Handler exceptions are isolated and reported through <see cref="BlexManager.OnError"/>.
    /// </summary>
    public event Action? Hydrated;

    /// <summary>
    /// A task that completes when hydration has finished -- already completed when
    /// <see cref="HasHydrated"/> is <c>true</c>. The awaitable form of that flag, for code that
    /// must not read a persisted store until it holds real data.
    /// </summary>
    /// <example><code>
    /// await persistor.WhenHydratedAsync();
    /// if (settings.Theme == "dark") ...
    /// </code></example>
    public Task WhenHydratedAsync()
    {
        if (HasHydrated)
            return Task.CompletedTask;

        lock (_saveGate)
        {
            if (HasHydrated)
                return Task.CompletedTask;

            _hydration ??= new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            return _hydration.Task;
        }
    }

    /// <summary>Marks hydration complete, releasing anything awaiting <see cref="WhenHydratedAsync"/>.</summary>
    private void CompleteHydration()
    {
        HasHydrated = true;
        ReleaseHydrationWaiters();
        _manager.InvokeIsolated<object?>(Hydrated, null, "persistence", "hydrated handler");
    }

    /// <summary>
    /// Completes any pending <see cref="WhenHydratedAsync"/> task. Also called on disposal: a
    /// persistor torn down before it ever hydrated must not leave callers awaiting forever --
    /// they can still read <see cref="HasHydrated"/> to see that it never happened.
    /// </summary>
    private void ReleaseHydrationWaiters()
    {
        TaskCompletionSource? waiters;
        lock (_saveGate)
        {
            waiters = _hydration;
            _hydration = null;
        }

        waiters?.TrySetResult();
    }

    /// <summary>
    /// Loads and applies any previously persisted state for every persistent store, then starts
    /// listening for changes. Restoration does not record new actions. A corrupt or
    /// non-migratable payload is reported, discarded and skipped -- it never prevents startup.
    /// Exceptions from the storage provider itself (e.g. JS interop unavailable during
    /// prerendering) propagate so the host can retry later; the method is safe to call again.
    /// </summary>
    /// <remarks>
    /// Only stores already registered with the manager are rehydrated. In hosts without
    /// <c>&lt;BlexProvider&gt;</c>, resolve every store from DI (which registers it) before
    /// calling this method.
    /// </remarks>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        // Watched before hydration starts, so a store that registers while it is reading storage is
        // never missed: until the first hydration completes such a store is left to its catch-up, and
        // after it the store is hydrated on arrival.
        if (!_watchingRegistry)
        {
            _manager.StoreRegistered += OnStoreRegistered;
            _manager.StoreUnregistered += OnStoreUnregistered;
            _watchingRegistry = true;
        }

        if (!_options.SkipHydration)
            await RehydrateAsync(cancellationToken).ConfigureAwait(false);

        if (!_subscribed)
        {
            _manager.ActionDispatched += OnAction;
            _manager.StoreRestored += OnStoreRestored;
            _subscribed = true;
        }

        await StartWatchingAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Begins applying state written by other instances (other tabs), when
    /// <see cref="BlexPersistenceOptions.SyncAcrossTabs"/> is on and the storage can report it.
    /// </summary>
    private async Task StartWatchingAsync(CancellationToken cancellationToken)
    {
        if (!_options.SyncAcrossTabs || _watching || _storage is not IBlexExternalStorageChange watchable)
            return;

        _watching = true;
        watchable.ExternalChange += OnExternalChange;
        try
        {
            await watchable.StartWatchingAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // Setting up the watch failed (interop unavailable during prerender, say). Roll back so
            // a later StartAsync can try again rather than leaving a subscription that never fires.
            watchable.ExternalChange -= OnExternalChange;
            _watching = false;
            throw;
        }
    }

    /// <summary>
    /// Applies state another instance wrote. Fired from the storage's own notification, so it must
    /// not throw and must not schedule a save: the value came <em>from</em> storage, and writing it
    /// straight back would bounce the change between tabs.
    /// </summary>
    private void OnExternalChange(string? key)
    {
        // Chained rather than fired off independently: two changes arriving in quick succession
        // would otherwise race on their reads, and the older payload could land last.
        lock (_saveGate)
        {
            _externalChain = ApplyExternalChangeAsync(_externalChain, key);
        }
    }

    private async Task ApplyExternalChangeAsync(Task previous, string? key)
    {
        await previous.ConfigureAwait(true);
        await ApplyExternalChangeAsync(key).ConfigureAwait(true);
    }

    private async Task ApplyExternalChangeAsync(string? key)
    {
        try
        {
            // A null key means the whole area changed (a wholesale clear), so re-read everything.
            var target = key is null ? null : StoreNameFor(key);
            if (key is not null && target is null)
                return; // some other application's key sharing the same storage area

            foreach (var store in _manager.Stores)
            {
                if (!Persists(store) || store is not BlexStoreBase sb)
                    continue;
                if (target is not null && store.Name != target)
                    continue;

                var raw = await _storage.GetAsync(KeyFor(store)).ConfigureAwait(true);

                JsonObject? slice = null;
                if (!string.IsNullOrEmpty(raw))
                {
                    try
                    {
                        slice = Unwrap(store.Name, raw);
                    }
                    catch (Exception ex)
                    {
                        _manager.ReportError("persistence", ex, $"external change for '{store.Name}'");
                        continue;
                    }
                }

                slice = Merge(store, sb, StripTransient(sb, slice));
                if (slice is null)
                    continue;

                // Suspended: applying is a restore, not an action, and echoing it back to storage
                // would bounce the same value between tabs forever.
                var wasSuspended = _suspended;
                _suspended = true;
                try
                {
                    sb.ApplyRestoredState(slice);
                }
                catch (Exception ex)
                {
                    _manager.ReportError("persistence", ex, $"applying external change to '{store.Name}'");
                    continue;
                }
                finally
                {
                    _suspended = wasSuspended;
                }

                NotifyExternalChange(store.Name, slice);
            }
        }
        catch (Exception ex)
        {
            // Nothing awaits this task, so an escaping exception would surface as an unobserved one.
            _manager.ReportError("persistence", ex, "external change");
        }
    }

    /// <summary>Maps a storage key back to a store name, or <c>null</c> when it is not ours.</summary>
    private string? StoreNameFor(string key)
    {
        var prefix = _options.KeyPrefix ?? "";
        if (!key.StartsWith(prefix, StringComparison.Ordinal))
            return null;

        var name = key.Substring(prefix.Length);
        foreach (var store in _manager.Stores)
        {
            if (Persists(store) && store.Name == name)
                return name;
        }

        return null;
    }

    private void NotifyExternalChange(string storeName, JsonObject state)
    {
        if (_options.OnExternalChange is not { } handler)
            return;

        try
        {
            handler(storeName, state);
        }
        catch (Exception ex)
        {
            _manager.ReportError("persistence", ex, $"external-change callback for '{storeName}'");
        }
    }

    /// <summary>
    /// Reads and applies persisted state for every persistent store, without changing whether the
    /// persistor is listening for saves. <see cref="StartAsync"/> calls this unless
    /// <see cref="BlexPersistenceOptions.SkipHydration"/> is set; call it directly to hydrate later
    /// (once the user, tenant or feature flags are known), or to re-read storage after an external
    /// change. Safe to call more than once.
    /// </summary>
    /// <remarks>
    /// Saving is suspended for the duration so rehydration does not immediately rewrite storage.
    /// A corrupt or non-migratable payload is reported, discarded and skipped; exceptions from the
    /// storage provider itself propagate so the host can retry later.
    /// </remarks>
    public async Task RehydrateAsync(CancellationToken cancellationToken = default)
    {
        // Suspend saving while we restore so rehydration doesn't immediately rewrite storage.
        var wasSuspended = _suspended;
        _suspended = true;
        try
        {
            foreach (var store in _manager.Stores)
            {
                // A store whose late hydration is still in flight is left to it: reading it again here
                // could have the older read land last, over changes made in between.
                if (Persists(store) && store is BlexStoreBase sb && !IsLateHydrationInFlight(store))
                    await HydrateStoreAsync(store, sb, cancellationToken).ConfigureAwait(true);
            }
        }
        finally
        {
            _suspended = wasSuspended;
        }

        CompleteHydration();

        // A store that registered while the loop above was reading storage is not in its snapshot of
        // the registry, and until now it was too early to hydrate it on arrival.
        CatchUpLateStores();
    }

    /// <summary>
    /// Reads, migrates, merges and applies one store's persisted state, then lifts the store's save
    /// hold if it has one. Storage access errors propagate (the storage may simply not be ready yet);
    /// payload errors are contained.
    /// </summary>
    private async Task HydrateStoreAsync(IBlexStore store, BlexStoreBase sb, CancellationToken cancellationToken)
    {
        // Not ConfigureAwait(false): what follows applies state, which -- like every action -- has to happen
        // on the dispatcher (a Blazor Server circuit's) rather than race the user's actions from the pool.
        var raw = await _storage.GetAsync(KeyFor(store), cancellationToken).ConfigureAwait(true);
        lock (_saveGate)
        {
            // It left the manager while storage was read: its state is no longer anyone's to restore, and
            // OnStoreUnregistered has already forgotten it.
            if (!_manager.Stores.Contains(store))
                return;

            _hydrated.Add(store);
        }

        var slice = await ReadSliceAsync(store, sb, raw, cancellationToken).ConfigureAwait(true);
        if (slice is not null)
        {
            // Suspended for the apply itself as well: it is a restore, and writing the value straight
            // back to the storage it was just read from is pointless.
            var wasSuspended = _suspended;
            _suspended = true;
            try
            {
                sb.ApplyRestoredState(slice);
            }
            catch (Exception ex)
            {
                // Applying can throw from a StateChanged subscriber even though the payload
                // itself is fine -- report, but keep the stored data.
                _manager.ReportError("persistence", ex, $"applying '{store.Name}'");
            }
            finally
            {
                _suspended = wasSuspended;
            }
        }

        // Before the callback, so an action it dispatches is saved like any other. With nothing stored,
        // the changes made while the hold was on are the store's whole state and still have to reach
        // storage; stored state that was applied replaced them, and is what storage already holds.
        ReleaseHydrationHold(store, writeHeldSave: slice is null);
        NotifyRehydrated(store.Name, slice);
    }

    /// <summary>
    /// Unwraps, migrates, strips and merges a stored payload: the state to apply, or <c>null</c> when
    /// there is none. An unreadable payload is reported and removed from storage.
    /// </summary>
    private async Task<JsonObject?> ReadSliceAsync(IBlexStore store, BlexStoreBase sb, string? raw, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(raw))
            return null;

        JsonObject? slice = null;
        try
        {
            slice = Unwrap(store.Name, raw);
        }
        catch (Exception ex)
        {
            // Unreadable payload: report, discard, and drop the stored entry.
            _manager.ReportError("persistence", ex, $"rehydrating '{store.Name}'");
            await TryRemoveAsync(store, cancellationToken).ConfigureAwait(true);
        }

        return Merge(store, sb, StripTransient(sb, slice));
    }

    /// <summary>
    /// Hydrates a persistent store that joins the manager after hydration has completed. Before that,
    /// the store is left to <see cref="RehydrateAsync"/>, whose catch-up includes it.
    /// </summary>
    private void OnStoreRegistered(IBlexStore store)
    {
        if (!HasHydrated || !Persists(store) || store is not BlexStoreBase)
            return;

        ScheduleLateHydration(store);
    }

    private void CatchUpLateStores()
    {
        foreach (var store in _manager.Stores)
        {
            if (Persists(store) && store is BlexStoreBase)
                ScheduleLateHydration(store);
        }
    }

    private void ScheduleLateHydration(IBlexStore store)
    {
        lock (_saveGate)
        {
            if (_hydrated.Contains(store)
                || (_hydrating.TryGetValue(store.Name, out var held) && ReferenceEquals(held.Store, store)))
            {
                return;
            }

            _hydrating[store.Name] = new HydrationHold(store);

            // Chained, like external changes: one store at a time, in arrival order, and FlushAsync
            // has a single task to wait on.
            _lateChain = HydrateLateAsync(_lateChain, store);
        }
    }

    private bool IsLateHydrationInFlight(IBlexStore store)
    {
        lock (_saveGate)
        {
            return _hydrating.TryGetValue(store.Name, out var hold) && ReferenceEquals(hold.Store, store) && hold.InFlight;
        }
    }

    /// <summary>Removes the save hold of <paramref name="store"/> -- and only of that instance. Caller holds <see cref="_saveGate"/>.</summary>
    private HydrationHold? RemoveHydrationHoldLocked(IBlexStore store)
    {
        if (!_hydrating.TryGetValue(store.Name, out var hold) || !ReferenceEquals(hold.Store, store))
            return null;

        _hydrating.Remove(store.Name);
        return hold;
    }

    /// <summary>
    /// Lifts the save hold of <paramref name="store"/>, writing the save it held back when
    /// <paramref name="writeHeldSave"/> is set.
    /// </summary>
    private void ReleaseHydrationHold(IBlexStore store, bool writeHeldSave)
    {
        lock (_saveGate)
        {
            if (RemoveHydrationHoldLocked(store) is { SaveRequested: true } && writeHeldSave)
                Schedule(store.Name);
        }
    }

    private async Task HydrateLateAsync(Task previous, IBlexStore store)
    {
        await previous.ConfigureAwait(true);

        try
        {
            // It may have left again while it waited its turn. Hydrating lifts the store's hold.
            if (_manager.Stores.Contains(store) && store is BlexStoreBase sb)
                await HydrateStoreAsync(store, sb, CancellationToken.None).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            // Nothing awaits this task. Its saves stay held -- writing its declared defaults over stored
            // data that could not be read would turn a transient read failure into data loss -- until
            // a RehydrateAsync manages to read it.
            lock (_saveGate)
            {
                if (_hydrating.TryGetValue(store.Name, out var hold) && ReferenceEquals(hold.Store, store))
                    hold.InFlight = false;
            }

            _manager.ReportError("persistence", ex, $"hydrating '{store.Name}'; its changes are not saved until RehydrateAsync reads it");
        }
    }

    /// <summary>
    /// Writes the pending save of a store that is leaving the manager, while it can still be
    /// serialized: once it is gone the debounced save would find no store under its name and drop
    /// the user's last changes on the floor.
    /// </summary>
    private void OnStoreUnregistered(IBlexStore store)
    {
        try
        {
            lock (_saveGate)
            {
                _hydrated.Remove(store);

                // A store still waiting for its stored state holds its name here; the next instance under
                // that name gets a fresh attempt. What it held back is dropped: whether storage had state
                // for it is exactly what never got found out.
                RemoveHydrationHoldLocked(store);

                if (_pending.Remove(store.Name))
                    SaveLocked(store.Name, store);
            }
        }
        catch (Exception ex)
        {
            _manager.ReportError("persistence", ex, $"saving '{store.Name}' as it unregistered");
        }
    }

    /// <summary>
    /// Removes the fields a store keeps out of storage (<c>[BlexStateAttribute(Persist = false)]</c>)
    /// from a payload on its way in or out. Returns the same object for chaining.
    /// </summary>
    private static JsonObject? StripTransient(BlexStoreBase store, JsonObject? state)
    {
        if (state is null)
            return null;

        foreach (var key in store.GetTransientStateKeys())
            state.Remove(key);

        return state;
    }

    /// <summary>
    /// Runs <see cref="BlexPersistenceOptions.Merge"/> over a stored slice, or returns it unchanged
    /// when no hook is configured. A throwing hook discards the payload: applying a half-merged
    /// state would be worse than starting from the store's own defaults, and it is reported either
    /// way.
    /// </summary>
    private JsonObject? Merge(IBlexStore store, BlexStoreBase instance, JsonObject? persisted)
    {
        if (persisted is null || _options.Merge is not { } merge)
            return persisted;

        try
        {
            var merged = merge(store.Name, persisted, instance.SerializeState());
            // A hook that hands back a node still parented to something else (a sub-object of one
            // of its arguments) cannot be re-parented; detach rather than throwing at the caller.
            return merged is null || merged.Parent is null ? merged : (JsonObject)merged.DeepClone();
        }
        catch (Exception ex)
        {
            _manager.ReportError("persistence", ex, $"merging '{store.Name}'");
            return null;
        }
    }

    /// <summary>Reports a store's rehydration outcome, containing a throwing callback.</summary>
    private void NotifyRehydrated(string storeName, JsonObject? state)
    {
        if (_options.OnRehydrated is not { } handler)
            return;

        try
        {
            handler(storeName, state);
        }
        catch (Exception ex)
        {
            // A user callback must not abort hydration for the stores behind it.
            _manager.ReportError("persistence", ex, $"rehydrated callback for '{storeName}'");
        }
    }

    /// <summary>
    /// Parses a stored payload, unwrapping the version envelope and running migrations.
    /// Returns <c>null</c> when the payload should be discarded.
    /// </summary>
    private JsonObject? Unwrap(string storeName, string raw)
    {
        if (JsonNode.Parse(raw) is not JsonObject parsed)
            return null;

        var storedVersion = 0;
        var state = parsed;

        // An envelope has exactly { "__blexVersion": n, "state": {...} }; requiring the exact
        // shape prevents misreading a raw store payload that happens to contain a key named
        // "__blexVersion".
        if (parsed.Count == 2
            && parsed.TryGetPropertyValue(VersionKey, out var versionNode)
            && parsed.TryGetPropertyValue(StateKey, out var stateNode))
        {
            storedVersion = versionNode?.GetValue<int>() ?? 0;
            state = stateNode as JsonObject ?? new JsonObject();
            state = (JsonObject)state.DeepClone(); // detach from the envelope parent
        }

        if (storedVersion == _options.Version)
            return state;

        return _options.Migrate?.Invoke(storeName, storedVersion, state);
    }

    private string Wrap(JsonObject state)
    {
        if (_options.Version <= 0)
            return state.ToJsonString();

        var envelope = new JsonObject
        {
            [VersionKey] = _options.Version,
            [StateKey] = state,
        };
        return envelope.ToJsonString();
    }

    private async ValueTask TryRemoveAsync(IBlexStore store, CancellationToken cancellationToken)
    {
        try
        {
            await _storage.RemoveAsync(KeyFor(store), cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // Removal of a corrupt entry is best-effort.
        }
    }

    private void OnAction(BlexActionContext context)
    {
        if (_suspended || !Persists(context.Store))
            return;

        try
        {
            Schedule(context.Store.Name);
        }
        catch (Exception ex)
        {
            // This handler sits directly on ActionDispatched; an escaping exception would
            // break the dispatch pipeline for every other observer.
            _manager.ReportError("persistence", ex, $"scheduling '{context.Store.Name}'");
        }
    }

    private void OnStoreRestored(IBlexStore store)
    {
        if (_suspended || !Persists(store))
            return;

        try
        {
            // A restore (undo/redo, time-travel, RestoreState) moved this store without an action; write
            // it back so a reload matches what the user sees. Only this one: the others did not move.
            Schedule(store.Name);
        }
        catch (Exception ex)
        {
            _manager.ReportError("persistence", ex, $"scheduling restored '{store.Name}'");
        }
    }

    private void Schedule(string storeName)
    {
        lock (_saveGate)
        {
            // Its stored state has not been read yet; saving now would overwrite it. Noted instead, and
            // made once the read finds nothing stored (see HydrateStoreAsync).
            if (_hydrating.TryGetValue(storeName, out var hold))
            {
                hold.SaveRequested = true;
                return;
            }

            _pending.Add(storeName);

            if (_options.DebounceInterval is not { } interval)
            {
                SavePendingLocked();
                return;
            }

            // Trailing-edge debounce would postpone forever under a steady action stream;
            // enforce the optional max-latency cap.
            var now = _manager.TimeProvider.GetUtcNow();
            _pendingSince ??= now;
            if (_options.DebounceMaxDelay is { } maxDelay && now - _pendingSince >= maxDelay)
            {
                CancelDebounceLocked();
                SavePendingLocked();
                return;
            }

            CancelDebounceLocked();
            _debounce = new CancellationTokenSource();
            _ = DebouncedSaveAsync(interval, _manager.TimeProvider, _debounce.Token);
        }
    }

    private void CancelDebounceLocked()
    {
        if (_debounce is { } previous)
        {
            previous.Cancel();
            previous.Dispose();
            _debounce = null;
        }
    }

    private async Task DebouncedSaveAsync(TimeSpan interval, TimeProvider clock, CancellationToken token)
    {
        try
        {
            // No ConfigureAwait(false): stay on the captured context (the Blazor dispatcher)
            // when one exists, so store serialization runs on the dispatch thread. The manager's
            // clock, so a test can advance through the interval instead of waiting it out.
            await Task.Delay(interval, clock, token);
        }
        catch (OperationCanceledException)
        {
            return; // superseded by a newer action or disposed
        }

        lock (_saveGate)
        {
            SavePendingLocked();
        }
    }

    /// <summary>Serializes each pending store now and chains the writes in order. Caller holds <see cref="_saveGate"/>.</summary>
    private void SavePendingLocked()
    {
        _pendingSince = null;
        if (_pending.Count == 0)
            return;

        // Snapshot first: SerializeState is user code and may dispatch, which re-enters
        // Schedule (the lock is reentrant) and would otherwise mutate the set mid-enumeration.
        var pending = new string[_pending.Count];
        _pending.CopyTo(pending);
        _pending.Clear();

        foreach (var name in pending)
        {
            var store = _manager.GetStore(name);
            if (store is not null)
                SaveLocked(name, store);
        }
    }

    /// <summary>Serializes one store now and chains its write. Caller holds <see cref="_saveGate"/>.</summary>
    private void SaveLocked(string name, IBlexStore store)
    {
        string json;
        try
        {
            var state = store.SerializeState();
            if (store is BlexStoreBase sb)
                StripTransient(sb, state);

            if (_options.Partialize is { } partialize)
            {
                // A null projection means "do not persist this store right now"; leave whatever
                // is already in storage alone rather than writing an empty payload over it.
                var projected = partialize(name, state);
                if (projected is null)
                    return;

                // Returning a nested node of the snapshot (state["Inner"]) is a natural thing
                // to write, but a JsonNode cannot be parented twice, so Wrap's envelope would
                // reject it. Detach first rather than making the caller remember.
                state = projected.Parent is null ? projected : (JsonObject)projected.DeepClone();
            }

            json = Wrap(state);
        }
        catch (Exception ex)
        {
            _manager.ReportError("persistence", ex, $"serializing '{name}'");
            return;
        }

        var key = KeyFor(store);
        _writeChain = WriteAsync(_writeChain, key, json);
    }

    private async Task WriteAsync(Task previous, string key, string json)
    {
        await previous.ConfigureAwait(false);
        try
        {
            await _storage.SetAsync(key, json).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _manager.ReportError("persistence", ex, $"writing '{key}'");
        }
    }

    /// <summary>
    /// Forces any debounced pending state to be written now and waits for all queued writes to
    /// complete -- and for the hydrations and incoming changes still in flight, with any save they
    /// release. Useful before navigating away or in tests.
    /// </summary>
    public async Task FlushAsync()
    {
        while (true)
        {
            Task writes;
            Task external;
            Task late;
            lock (_saveGate)
            {
                CancelDebounceLocked();
                SavePendingLocked();
                writes = _writeChain;

                // Incoming cross-tab changes are part of "settle everything in flight" too: without
                // this, a test (or a page about to navigate away) could observe a change that has been
                // reported but not yet applied. So is the hydration of a store that registered late.
                external = _externalChain;
                late = _lateChain;
            }

            if (writes.IsCompletedSuccessfully && external.IsCompletedSuccessfully && late.IsCompletedSuccessfully)
                return;

            // Not ConfigureAwait(false): the next round serializes stores, which belongs on the dispatcher.
            await Task.WhenAll(writes, external, late).ConfigureAwait(true);

            // Round again: a late store that finished hydrating may have released a save it held back.
        }
    }

    /// <summary>Removes every persistent store's saved slice from storage.</summary>
    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        Task chain;
        lock (_saveGate)
        {
            CancelDebounceLocked();
            _pending.Clear();
            _pendingSince = null;
            chain = _writeChain;
        }

        await chain.ConfigureAwait(false);
        foreach (var store in _manager.Stores)
        {
            if (Persists(store))
                await _storage.RemoveAsync(KeyFor(store), cancellationToken).ConfigureAwait(false);
        }
    }

    private void Unsubscribe()
    {
        ReleaseHydrationWaiters();

        if (_watching && _storage is IBlexExternalStorageChange watchable)
        {
            watchable.ExternalChange -= OnExternalChange;
            _watching = false;
        }

        if (_watchingRegistry)
        {
            _manager.StoreRegistered -= OnStoreRegistered;
            _manager.StoreUnregistered -= OnStoreUnregistered;
            _watchingRegistry = false;
        }

        if (!_subscribed)
            return;

        _manager.ActionDispatched -= OnAction;
        _manager.StoreRestored -= OnStoreRestored;
        _subscribed = false;
    }

    /// <summary>Stops listening, flushes any pending writes and waits for them to complete.</summary>
    public async ValueTask DisposeAsync()
    {
        Unsubscribe();
        await FlushAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Synchronous disposal, for DI containers that are torn down with <see cref="IDisposable"/>.
    /// </summary>
    /// <remarks>
    /// A container registered as a singleton is disposed through whichever interface the host
    /// calls, and <c>ServiceProvider.Dispose()</c> throws outright on a service that only offers
    /// <see cref="IAsyncDisposable"/> -- so both have to exist. This one stops listening and hands
    /// any debounced state to the storage, but does not wait for the writes to finish: blocking a
    /// UI thread on storage during shutdown risks a deadlock. A synchronous
    /// <see cref="IBlexStorage"/> (MAUI <c>Preferences</c>, a file) completes inline either way;
    /// prefer <see cref="DisposeAsync"/> when the storage is genuinely asynchronous.
    /// </remarks>
    public void Dispose()
    {
        Unsubscribe();
        lock (_saveGate)
        {
            CancelDebounceLocked();
            SavePendingLocked();
        }
    }
}
