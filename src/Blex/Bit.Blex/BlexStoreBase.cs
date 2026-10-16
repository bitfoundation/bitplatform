using System.ComponentModel;
using System.Text.Json.Nodes;

namespace Bit.Blex;

/// <summary>
/// Base class for all Blex stores. Provides change notification, batched dispatch and the
/// hooks the source generator wires up. You normally never derive from this directly --
/// decorate a partial class with <see cref="BlexStoreAttribute"/> and the generator does it for you.
/// </summary>
/// <remarks>
/// Dispatch is assumed to be single-threaded, matching Blazor's rendering model. The depth
/// counters that drive batching (<c>_recordDepth</c>/<c>_notifyDepth</c>) are not synchronized.
/// In particular, while a <see cref="DispatchAsync(string, Func{Task})"/> action is awaiting,
/// any state mutation made from elsewhere is folded into that in-flight action rather than
/// recorded as its own action. Avoid mutating a store from outside while one of its async
/// actions is in progress.
/// </remarks>
public abstract class BlexStoreBase : IBlexStore, INotifyPropertyChanged, IDisposable
{
    // XAML binding engines (MAUI, WPF, WinUI) treat an empty property name as "every property
    // changed". That is the honest granularity here: memoized computed properties can change
    // whenever any state field changes, so a per-field raise would leave computed bindings stale.
    private static readonly PropertyChangedEventArgs AllPropertiesChanged = new(string.Empty);

    private int _recordDepth;
    private int _notifyDepth;
    private bool _dirty;
    private bool _disposed;
    private BlexManager? _manager;
    private JsonObject? _initialState;

    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public virtual bool Persist => false;

    /// <inheritdoc />
    public event Action? StateChanged;

    /// <summary>
    /// Raised together with <see cref="StateChanged"/>, always with an empty property name
    /// ("all properties changed"), so XAML bindings (.NET MAUI, WPF, WinUI) can bind directly to
    /// generated state and computed properties. Blazor hosts can ignore this event.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <inheritdoc />
    public abstract JsonObject SerializeState();

    /// <inheritdoc />
    public abstract void DeserializeState(JsonObject state);

    /// <summary>True while the manager is applying a time-travel/restore snapshot.</summary>
    protected bool IsRestoring { get; private set; }

    /// <summary>
    /// Whether anything (middleware, DevTools, subscribers) observes dispatched actions.
    /// Generated wrappers use this to skip argument capture when nobody is listening.
    /// </summary>
    protected bool IsObserved => _manager?.HasObservers ?? false;

    /// <summary>Called by the generator to invalidate memoized computed values.</summary>
    protected virtual void InvalidateComputed()
    {
    }

    /// <summary>
    /// Raises <see cref="StateChanged"/> followed by <see cref="PropertyChanged"/> (with an empty
    /// property name, meaning "all properties changed"). Every notification funnels through here
    /// so both eventing models always stay in sync.
    /// </summary>
    /// <remarks>
    /// Subscribers are invoked one at a time and their exceptions are isolated and reported through
    /// <see cref="BlexManager.OnError"/>, matching how the manager treats middleware and action
    /// subscribers: one component throwing while it re-renders must not starve the others -- nor
    /// the persistence/history/DevTools observers that run after the notification -- of the change.
    /// </remarks>
    private void NotifyStateChanged()
    {
        try
        {
            if (StateChanged is { } stateChanged)
                InvokeIsolated(stateChanged);
        }
        finally
        {
            // The mutation was applied either way, so XAML bindings get their raise regardless of
            // what the StateChanged subscribers did.
            if (PropertyChanged is { } propertyChanged)
                InvokeIsolated(propertyChanged);
        }
    }

    private void InvokeIsolated(Delegate handler)
    {
        var targets = handler.GetInvocationList();

        // The overwhelmingly common case is a single subscriber; skip the loop bookkeeping there.
        if (targets.Length == 1)
        {
            Invoke(targets[0]);
            return;
        }

        for (var i = 0; i < targets.Length; i++)
            Invoke(targets[i]);

        void Invoke(Delegate target)
        {
            try
            {
                switch (target)
                {
                    case Action action:
                        action();
                        break;
                    case PropertyChangedEventHandler propertyChanged:
                        propertyChanged(this, AllPropertiesChanged);
                        break;
                }
            }
            catch (Exception ex)
            {
                ReportSubscriberError(ex);
            }
        }
    }

    /// <summary>Routes a throwing notification subscriber to the manager, or to stderr when detached.</summary>
    private void ReportSubscriberError(Exception exception)
    {
        if (_manager is { } manager)
            manager.ReportError("subscriber", exception, Name);
        else
            Console.Error.WriteLine($"[Blex] subscriber failed ({Name}): {exception.Message}");
    }

    /// <summary>
    /// Invalidates computed values and raises <see cref="StateChanged"/>. Useful for manual
    /// (non-generated) store implementations after restoring state out-of-band.
    /// </summary>
    protected void NotifyRestored()
    {
        InvalidateComputed();
        NotifyStateChanged();
    }

    internal void Attach(BlexManager manager)
    {
        _manager = manager;
        // First registration captures the store's initial state so ResetState can return to it.
        _initialState ??= SerializeState();
    }

    /// <summary>Detaches from the manager so actions are no longer observed (see <see cref="BlexManager.Unregister"/>).</summary>
    internal void Detach() => _manager = null;

    /// <summary>
    /// Assigns a state backing field. Raises change notification immediately unless inside a
    /// synchronous <see cref="Dispatch(string, Action)"/> batch, and records a standalone
    /// "Set X" action when not inside any dispatch. Used by generated setters.
    /// </summary>
    protected void SetState<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        // A record depth above zero means an action of this store is running -- either lexically
        // around this assignment or still awaiting -- and the assignment belongs to it. Unlike a
        // named action (see BeginAction), a raw property write carries no way to tell "the action's
        // own body" from "somebody else mutating mid-await", so it is always attributed to the
        // action in flight rather than re-offered to the filters as a separate "Set X".
        if (_recordDepth == 0 && _manager is { HasObservers: true } manager)
        {
            // A standalone set is its own action; give middleware a chance to veto it before
            // applying. The label/args are only materialized when something is listening.
            // (_notifyDepth is always 0 here: a non-zero notify depth implies a record depth.)
            var label = "Set " + propertyName;
            var args = new[] { new BlexActionArg(propertyName ?? "value", (object?)value) };
            if (!manager.BeforeAction(this, label, args))
                return;

            field = value;
            InvalidateComputed();
            try
            {
                NotifyStateChanged();
            }
            finally
            {
                // Record even when a subscriber throws: the mutation *was* applied, and skipping
                // the record would leave persistence/history/DevTools out of sync.
                manager.RecordAction(this, label, args);
            }

            return;
        }

        field = value;

        // Inside a sync batch we defer the render until the action completes (single re-render).
        // The dirty flag only matters inside a dispatch; a standalone set with nothing listening
        // records nothing, and must not leak the flag into a later dispatch.
        if (_recordDepth > 0)
            _dirty = true;

        if (_notifyDepth == 0)
        {
            InvalidateComputed();
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Assigns a transient effect-lifecycle field (loading/error). Raises change notification so the
    /// UI can react, but never records a time-travel action. Used by generated effect wrappers.
    /// </summary>
    protected void SetEffectState<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        field = value;
        InvalidateComputed();
        NotifyStateChanged();
    }

    /// <summary>
    /// Increments an effect's pending-run counter and notifies. With overlapping runs of the same
    /// effect, the generated <c>IsLoading</c> property stays <c>true</c> until the last run ends.
    /// </summary>
    protected void BeginEffect(ref int pending)
    {
        pending++;
        if (pending == 1)
        {
            InvalidateComputed();
            NotifyStateChanged();
        }
    }

    /// <summary>Decrements an effect's pending-run counter and notifies when it reaches zero.</summary>
    protected void EndEffect(ref int pending)
    {
        pending--;
        if (pending == 0)
        {
            InvalidateComputed();
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Runs <paramref name="mutation"/> as a single named action: synchronous state changes inside
    /// it are batched into one notification and one time-travel entry.
    /// </summary>
    protected void Dispatch(string actionName, Action mutation)
        => Dispatch(actionName, mutation, null);

    /// <summary>
    /// Runs <paramref name="mutation"/> as a single named action carrying the given arguments
    /// (surfaced to middleware, subscribers and DevTools). Used by generated wrappers.
    /// </summary>
    protected void Dispatch(string actionName, Action mutation, BlexActionArg[]? args)
    {
        ArgumentNullException.ThrowIfNull(mutation);

        // Gate the veto on the *synchronous* nesting depth, not the record depth -- see the remarks
        // on BeginAction. A sync action started while an async action of this store is still
        // awaiting must still face the filters.
        if (!BeginAction(actionName, args))
            return;

        _recordDepth++;
        _notifyDepth++;
        try
        {
            mutation();
        }
        catch (Exception ex)
        {
            _manager?.RecordActionError(this, actionName, args, ex);
            throw;
        }
        finally
        {
            _notifyDepth--;
            _recordDepth--;

            // Flush the deferred notification when this was the outermost *sync* batch, even if
            // an async action is still in flight above us (it keeps _recordDepth > 0 but does not
            // hold _notifyDepth, so its nested sync actions must render on completion).
            // A non-zero notify depth implies a record depth, so the notify-flush check covers
            // every dirty case here.
            if (_dirty && _notifyDepth == 0)
            {
                InvalidateComputed();
                var record = _recordDepth == 0;
                if (record)
                    _dirty = false; // reset before notifying so a throwing subscriber can't leak it

                try
                {
                    NotifyStateChanged();
                }
                finally
                {
                    if (record)
                        _manager?.RecordAction(this, actionName, args);
                }
            }
        }
    }

    /// <summary>
    /// Groups ad-hoc mutations into a single named action from outside the store's own
    /// <c>[BlexActionAttribute]</c> methods -- one notification, one time-travel entry, one middleware pass.
    /// The equivalent of Pinia's <c>$patch</c> / MobX's <c>runInAction</c>.
    /// </summary>
    /// <example><code>store.Batch("Apply preset", () => { store.Count = 10; store.Step = 5; });</code></example>
    public void Batch(string actionName, Action mutations)
    {
        ArgumentNullException.ThrowIfNull(actionName);
        Dispatch(actionName, mutations, null);
    }

    /// <summary>
    /// Restores the store to the state it had when it was first registered with the manager,
    /// recorded as a normal <c>"ResetState"</c> action (so it is vetoable, observable, persisted
    /// and time-travelable). No-op when the store has never been registered.
    /// </summary>
    public void ResetState()
    {
        if (_initialState is null)
            return;

        // A reset that changes nothing must record nothing -- the rule every other mutation path
        // already follows (SetState with an equal value, a no-op adapter call). Without this, a
        // "reset" button pressed on an untouched form would notify every subscriber, write to
        // storage and push an undo entry for a mutation that never happened.
        //
        // Compared as text rather than with JsonNode.DeepEquals, which only exists from .NET 9:
        // both trees come from the same generated SerializeState, so their property order matches.
        if (SerializeState().ToJsonString() == _initialState.ToJsonString())
            return;

        Dispatch("ResetState", () =>
        {
            DeserializeState((JsonObject)_initialState.DeepClone());
            _dirty = true;
        }, null);
    }

    /// <summary>
    /// Async variant. State changes update the UI as they happen (e.g. a loading flag before an
    /// await), but the whole operation is recorded as one named action for time-travel.
    /// </summary>
    /// <remarks>
    /// Because the operation spans <c>await</c> points, any state mutation that occurs from
    /// outside this store while the action is awaiting is attributed to this action. Treat a
    /// store as owned by its in-flight async action until it completes.
    /// </remarks>
    protected Task DispatchAsync(string actionName, Func<Task> mutation)
        => DispatchAsync(actionName, mutation, null);

    /// <summary>
    /// Async variant carrying action arguments (surfaced to middleware, subscribers and DevTools).
    /// </summary>
    protected Task DispatchAsync(string actionName, Func<Task> mutation, BlexActionArg[]? args)
    {
        ArgumentNullException.ThrowIfNull(mutation);
        return BeginAction(actionName, args)
            ? DispatchApprovedAsync(actionName, mutation, args)
            : Task.CompletedTask;
    }

    /// <summary>
    /// Runs the before-action pipeline for an action that is about to start and reports whether it
    /// may proceed (<c>false</c> means a middleware vetoed it). Call this <em>before</em> touching
    /// any observable state; an asynchronous body then runs through
    /// <see cref="DispatchApprovedAsync"/> so the decision is taken exactly once, and
    /// <see cref="Dispatch(string, Action, BlexActionArg[])"/> consults it itself. Generated effect
    /// wrappers use it so a vetoed effect leaves no trace at all -- no loading flicker, no cleared
    /// error, no cancelled predecessor.
    /// </summary>
    protected bool BeginAction(string actionName, BlexActionArg[]? args)
    {
        // Gate the veto on the *synchronous* nesting depth, not the record depth. A record depth
        // above zero can mean either "lexically nested inside a running action" (must not re-veto:
        // the enclosing action already passed the filters) or "another async action of this store
        // is still awaiting" -- and in the latter case skipping the check would silently bypass
        // every veto filter, which is the common case since Parallel is the default effect
        // concurrency. Only a synchronous Dispatch body raises _notifyDepth, so it distinguishes
        // the two exactly.
        return _notifyDepth != 0
            || _manager is null
            || _manager.BeforeAction(this, actionName, args);
    }

    /// <summary>
    /// Async dispatch for a body whose veto decision has already been taken by
    /// <see cref="BeginAction"/>. Records the action when the outermost dispatch completes.
    /// </summary>
    protected async Task DispatchApprovedAsync(string actionName, Func<Task> mutation, BlexActionArg[]? args)
    {
        ArgumentNullException.ThrowIfNull(mutation);

        _recordDepth++;
        try
        {
            await mutation().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _manager?.RecordActionError(this, actionName, args, ex);
            throw;
        }
        finally
        {
            _recordDepth--;
            if (_recordDepth == 0 && _dirty)
            {
                _dirty = false;
                _manager?.RecordAction(this, actionName, args);
            }
        }
    }

    /// <summary>
    /// Applies a snapshot produced by <see cref="SerializeState"/> without recording an action,
    /// then invalidates computed values and raises <see cref="StateChanged"/> exactly once.
    /// This is the safe public entry point for hydrating a store manually (prefer it over
    /// calling <see cref="DeserializeState"/> directly, which performs the raw field writes only).
    /// </summary>
    public void RestoreState(JsonObject state)
    {
        ArgumentNullException.ThrowIfNull(state);
        ApplyRestoredState(state);
    }

    /// <summary>
    /// Applies a restored snapshot without recording a new action. Invoked by the manager during
    /// time-travel. Raises <see cref="StateChanged"/> exactly once.
    /// </summary>
    internal void ApplyRestoredState(JsonObject state)
    {
        IsRestoring = true;
        try
        {
            DeserializeState(state);
        }
        finally
        {
            IsRestoring = false;
        }

        InvalidateComputed();
        NotifyStateChanged();
    }

    /// <summary>Whether <see cref="Dispose()"/> has run on this store.</summary>
    protected bool IsDisposed => _disposed;

    /// <summary>
    /// Cancels every in-flight <c>[BlexEffectAttribute]</c> run (and any effect still waiting out
    /// its debounce interval). Overridden by the generator; the base implementation does nothing.
    /// </summary>
    protected virtual void CancelEffects()
    {
    }

    /// <summary>
    /// Releases the store: in-flight effects are cancelled so their continuations do not resume
    /// against a torn-down scope, and pending debounce timers are stopped.
    /// </summary>
    /// <remarks>
    /// A DI container disposes the stores it created (a Blazor Server circuit ending, a test scope
    /// closing), so this is what stops an effect started moments before teardown from carrying on
    /// against a dead renderer. Cancellation through the effect's own token is a normal outcome, so
    /// a cancelled run records no error. The store stays usable and attached: disposal is a
    /// best-effort quiesce, not a hard kill, and calling it twice is a no-op.
    /// </remarks>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Override to release additional resources. The base implementation cancels in-flight effects.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed || !disposing)
            return;

        _disposed = true;
        try
        {
            CancelEffects();
        }
        catch (Exception ex)
        {
            // Disposal runs during container teardown; a throwing cancellation must not take the
            // rest of the scope's disposal down with it.
            if (_manager is { } manager)
                manager.ReportError("dispose", ex, Name);
            else
                Console.Error.WriteLine($"[Blex] dispose failed ({Name}): {ex.Message}");
        }
    }
}
