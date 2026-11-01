using System.ComponentModel;
using System.Runtime.ExceptionServices;
using System.Text.Json.Nodes;

namespace Bit.Blex;

/// <summary>
/// Base class for all Blex stores. Provides change notification, batched dispatch and the
/// hooks the source generator wires up. You normally never derive from this directly --
/// decorate a partial class with <see cref="BlexStoreAttribute"/> and the generator does it for you.
/// </summary>
/// <remarks>
/// Dispatch is assumed to be single-threaded, matching Blazor's rendering model; the counters that
/// drive batching are not synchronized. An action is recorded when it ends, and what it records is
/// what its own asynchronous flow changed: actions that overlap -- two runs of a <c>Parallel</c>
/// effect, a click handled while an effect awaits -- are each recorded as their own action, while
/// an action dispatched from inside another one's body (in the same flow) is part of it.
/// </remarks>
public abstract class BlexStoreBase : IBlexStore, INotifyPropertyChanged, IDisposable
{
    // XAML binding engines (MAUI, WPF, WinUI) treat an empty property name as "every property
    // changed". That is the honest granularity here: memoized computed properties can change
    // whenever any state field changes, so a per-field raise would leave computed bindings stale.
    private static readonly PropertyChangedEventArgs AllPropertiesChanged = new(string.Empty);

    // Dispatchable by name on every store, generated or not -- the base class implements it.
    private static readonly BlexAction<BlexStoreBase> ResetStateAction =
        new("ResetState", static store =>
        {
            store.ResetState();
            return Task.CompletedTask;
        });

    /// <summary>
    /// One action of this store in progress, with every action dispatched from inside its body: they are
    /// recorded together, as the action that began it -- unless one of them outlives it (an effect it
    /// started and did not await), which then records what it changes after that on its own.
    /// </summary>
    private sealed class ActionFrame
    {
        /// <summary>How many of the frame's dispatches have not ended yet; at zero the frame is over.</summary>
        public int Depth;

        /// <summary>Whether state changed since the frame last recorded.</summary>
        public bool Dirty;
    }

    // The action frame of the current asynchronous flow. Flowing with the execution context rather
    // than counted per store is what tells an assignment in an action's own body (after an await,
    // say) from one made elsewhere while that action awaits -- the second is an action of its own.
    private readonly AsyncLocal<ActionFrame?> _action = new();

    // Frames of this store in progress in any flow: an unannounced change waits for all of them.
    private int _openActions;

    private int _notifyDepth;
    private bool _disposed;
    private BlexManager? _manager;
    private JsonObject? _initialState;
    private string? _instanceKey;
    private string? _keyedName;
    private IReadOnlyList<BlexActionDescriptor>? _actionDescriptors;

    /// <inheritdoc />
    /// <remarks>
    /// Generated stores return their declared name (<c>[BlexStoreAttribute(Name = "...")]</c>, or the
    /// class name) passed through <see cref="WithInstanceKey"/>, so a scoped instance is named
    /// <c>"{name}:{key}"</c>.
    /// </remarks>
    public abstract string Name { get; }

    /// <summary>
    /// The key telling this instance apart from other instances of the same store, or <c>null</c> for
    /// the usual single instance. Assigned by <see cref="BlexManager.CreateScope{TStore}(TStore, string)"/>.
    /// </summary>
    public string? InstanceKey => _instanceKey;

    /// <summary>
    /// Returns <paramref name="name"/> -- the store's declared name -- with the
    /// <see cref="InstanceKey"/> appended as <c>"{name}:{key}"</c> for a scoped instance, or unchanged
    /// otherwise. The generated <see cref="Name"/> returns it; a hand-written store that should support
    /// <see cref="BlexManager.CreateScope{TStore}(TStore, string)">scoping</see> must return it too.
    /// </summary>
    protected string WithInstanceKey(string name)
    {
        if (_instanceKey is null)
            return name;

        // Name is read on every dispatch (qualified names, the state tree, storage keys); compose once.
        return _keyedName ??= name + ":" + _instanceKey;
    }

    /// <summary>
    /// The store's declared name, without a scoped instance's key: what metrics are tagged with, so a store
    /// with a thousand scoped instances is still one series.
    /// </summary>
    internal string DeclaredName
    {
        get
        {
            var name = Name;
            return _instanceKey is { } key && name.EndsWith(":" + key, StringComparison.Ordinal)
                ? name[..^(key.Length + 1)]
                : name;
        }
    }

    internal void AssignInstanceKey(string? key, bool generated)
    {
        // The name keys the state tree, DevTools and storage; changing it under a registered store
        // would orphan its slice and its persisted state.
        if (_manager is not null)
            throw new InvalidOperationException($"The store '{Name}' is already registered; an instance key can only be given before it is registered.");

        _instanceKey = key;
        _keyedName = null;
        HasGeneratedInstanceKey = generated;
    }

    /// <summary>
    /// Whether <see cref="InstanceKey"/> was assigned by the manager rather than given. Such an instance
    /// is never persisted: the next session counts its keys from the start too, so state saved under
    /// one would be found by whichever unrelated instance happens to draw the same number.
    /// </summary>
    internal bool HasGeneratedInstanceKey { get; private set; }

    /// <summary>
    /// The clock of the manager this store is registered with (<see cref="BlexManager.TimeProvider"/>),
    /// or the system clock while it is not registered. Generated <c>DebounceMilliseconds</c> effects wait
    /// on it; read it in an action for timestamps a test can control.
    /// </summary>
    protected TimeProvider TimeProvider => _manager?.TimeProvider ?? global::System.TimeProvider.System;

    /// <summary><see cref="TimeProvider"/>, for the library's own components (render throttling).</summary>
    internal TimeProvider Clock => TimeProvider;

    /// <summary>
    /// Every action of this store as a <see cref="BlexActionDescriptor"/>: the generated actions and
    /// effects (the same instances as the store's nested <c>Actions</c> class), a <c>"Set X"</c>
    /// descriptor per assignable state property, and <c>"ResetState"</c>. What
    /// <see cref="BlexManager.DispatchAsync(string, JsonObject?)"/> dispatches by name.
    /// </summary>
    public IReadOnlyList<BlexActionDescriptor> ActionDescriptors
        => _actionDescriptors ??= [.. DescribeActions(), ResetStateAction];

    /// <summary>Returns the descriptor of the action dispatched as <paramref name="actionName"/>, or <c>null</c>.</summary>
    public BlexActionDescriptor? FindAction(string actionName)
    {
        ArgumentNullException.ThrowIfNull(actionName);
        foreach (var action in ActionDescriptors)
        {
            if (string.Equals(action.Name, actionName, StringComparison.Ordinal))
                return action;
        }

        return null;
    }

    /// <summary>
    /// Describes the store's own actions for <see cref="ActionDescriptors"/>. Overridden by the
    /// generator; the base implementation describes none.
    /// </summary>
    protected virtual IReadOnlyList<BlexActionDescriptor> DescribeActions() => [];

    /// <summary>
    /// Whether state may only change inside one of the store's actions
    /// (<c>[BlexStoreAttribute(EnforceActions = true)]</c>). The generator makes the state setters
    /// private for such a store; this is the runtime half, which rejects an assignment made by the
    /// store's own code outside any action once the store is registered.
    /// </summary>
    protected virtual bool EnforcesActions => false;

    /// <summary>
    /// The property names of state marked <c>[BlexStateAttribute(Persist = false)]</c>: left out of
    /// what durable persistence writes and stripped from what it reads, while still part of snapshots,
    /// DevTools, undo/redo and the prerender handoff. Overridden by the generator.
    /// </summary>
    protected virtual IReadOnlyCollection<string> TransientStateKeys => [];

    // Plain protected rather than protected internal: an override's modifiers would then have to
    // depend on whether the store's assembly can see Blex's internals, which generated code cannot know.
    internal IReadOnlyCollection<string> GetTransientStateKeys() => TransientStateKeys;

    /// <inheritdoc />
    public virtual bool Persist => false;

    /// <summary>
    /// The version of this store's saved state (set via <c>[BlexStoreAttribute(Version = n)]</c>). When greater
    /// than zero the persistor uses it for this store instead of <see cref="BlexPersistenceOptions.Version"/>;
    /// <c>0</c> (the default) follows the app's version. Overridden by the generator.
    /// </summary>
    public virtual int PersistVersion => 0;

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
    /// Whether anything (middleware, DevTools, subscribers, <see cref="BlexManager.ActionFailed"/>
    /// handlers) observes dispatched actions. Generated wrappers use this to skip argument capture
    /// when nobody is listening.
    /// </summary>
    protected bool IsObserved => _manager?.CapturesArgs ?? false;

    /// <summary>Called by the generator to invalidate memoized computed values.</summary>
    protected virtual void InvalidateComputed()
    {
    }

    /// <summary>
    /// Invalidates the memoized computed values that read <paramref name="propertyName"/> -- every one by default. The
    /// generator overrides it with the dependencies it found in each computed's body, so writing one field does not
    /// throw away a value derived from others.
    /// </summary>
    protected virtual void InvalidateComputed(string? propertyName) => InvalidateComputed();

    /// <summary>
    /// Raises <see cref="StateChanged"/> followed by <see cref="PropertyChanged"/> (with an empty
    /// property name, meaning "all properties changed"). Every notification funnels through here
    /// so both eventing models always stay in sync.
    /// </summary>
    /// <param name="announced">
    /// Whether an action record or a restore event announces this change to the manager's subscriptions
    /// -- right after this notification, or, inside an async action still running, when that action is
    /// recorded. When nothing will (an effect's lifecycle flags), the manager is told here, or a
    /// cross-store subscription would go on reporting the value from before it.
    /// </param>
    /// <remarks>
    /// Subscribers are invoked one at a time and their exceptions are isolated and reported through
    /// <see cref="BlexManager.OnError"/>, matching how the manager treats middleware and action
    /// subscribers: one component throwing while it re-renders must not starve the others -- nor
    /// the persistence/history/DevTools observers that run after the notification -- of the change.
    /// </remarks>
    private void NotifyStateChanged(bool announced)
    {
        // Before any subscriber runs: one that re-renders synchronously may read a cross-store
        // selector, which must not hand it the value memoized before this change.
        MarkChanged();
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

        if (!announced)
            AnnounceUnrecorded();
    }

    // An unannounced change made while an action of this store was still running: announced with that action's
    // record, or on its own once the action ends without one.
    private bool _announcementDeferred;

    /// <summary>
    /// Tells the manager's subscriptions about a change no action record announces -- now, or, while an action of
    /// this store is still running, when it ends: a reactor's follow-up must not be recorded before the action whose
    /// in-flight writes the re-evaluation would otherwise expose.
    /// </summary>
    private void AnnounceUnrecorded()
    {
        if (_openActions > 0)
            _announcementDeferred = true;
        else
            _manager?.OnUnannouncedChange(this);
    }

    /// <summary>
    /// The action of this store running in the current flow, or <c>null</c> outside one. A frame that is over
    /// can still be the flow's value -- work an action started and did not await keeps the context it was
    /// started in -- and counts as none: what that work assigns afterwards is not part of the action.
    /// </summary>
    private ActionFrame? CurrentAction => _action.Value is { Depth: > 0 } frame ? frame : null;

    /// <summary>
    /// Enters an action: the current flow's frame when one is running (the action is part of it), or a new one.
    /// Returns whether it began the frame -- and with it the duty to clear the flow's frame on exit.
    /// </summary>
    private ActionFrame EnterAction(out bool begins)
    {
        var frame = CurrentAction;
        begins = frame is null;
        if (frame is null)
        {
            frame = new ActionFrame();
            _action.Value = frame;
            _openActions++;
        }

        frame.Depth++;
        return frame;
    }

    /// <summary>
    /// Leaves an action, and reports whether it records now: the one that began the frame always does (when
    /// anything changed) -- it has ended -- and so does whichever dispatch of the frame ends last.
    /// </summary>
    private bool ExitAction(ActionFrame frame, bool began)
    {
        frame.Depth--;
        var record = (began || frame.Depth == 0) && frame.Dirty;
        if (record)
            frame.Dirty = false;

        if (frame.Depth == 0)
            _openActions--;

        // A synchronous action shares its caller's context: the frame must not outlive it there. (In an async
        // action the assignment only reaches its own context, which is about to end anyway.)
        if (began)
            _action.Value = null;

        return record;
    }

    /// <summary>Called as an action of this store ends, after it recorded itself (or not); settles once none is left.</summary>
    private void SettleDeferredAnnouncement(bool recorded)
    {
        if (!_announcementDeferred || _openActions != 0)
            return;

        _announcementDeferred = false;

        // The record announced it; without one, nothing else will.
        if (!recorded)
            _manager?.OnUnannouncedChange(this);
    }

    /// <summary>
    /// Stamps an applied state change on this store and on the manager, whose memoized selectors compare
    /// stamps instead of listening for events, and which reports a change made outside its
    /// <see cref="BlexManager.Dispatcher"/>.
    /// </summary>
    private void MarkChanged()
    {
        ChangeVersion++;
        _manager?.OnStoreChanged(this);
    }

    /// <summary>
    /// Advances on every change to this store's state (including its effects' lifecycle flags); equal
    /// stamps mean nothing has changed in between. Never decreases; not synchronized, like dispatch.
    /// </summary>
    internal long ChangeVersion { get; private set; }

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
            Console.Error.WriteLine(BlexManager.EscapeControlCharacters($"[Blex] subscriber failed ({Name}): {exception.Message}"));
    }

    /// <summary>
    /// Invalidates computed values and raises <see cref="StateChanged"/>. Useful for manual
    /// (non-generated) store implementations after restoring state out-of-band.
    /// </summary>
    protected void NotifyRestored()
    {
        InvalidateComputed();
        NotifyStateChanged(announced: false);
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

        var action = CurrentAction;

        // EnforceActions: outside code cannot reach the (private) setter at all, so an assignment
        // here outside any action is the store's own non-action code. Only once registered: a
        // constructor setting up its initial state is not a mutation anyone observes.
        if (action is null && _manager is not null && EnforcesActions)
        {
            throw new InvalidOperationException(
                $"The store '{Name}' enforces actions, so '{propertyName}' can only change inside one of its actions. Move the assignment into a [BlexAction] method.");
        }

        // An action of this store running in this flow -- around the assignment, or the async action
        // whose body this is, after an await -- owns the assignment. Anything else is an action of its
        // own: an assignment made elsewhere while an async action of this store awaits is not that
        // action's doing, and is offered to the filters and recorded as its own "Set X".
        if (action is null && _manager is { HasObservers: true } manager)
        {
            // A standalone set is its own action; give middleware a chance to veto it before
            // applying. The label/args are only materialized when something is listening.
            // (_notifyDepth is always 0 here: a synchronous batch is an action of this flow.)
            var label = "Set " + propertyName;
            var args = new[] { new BlexActionArg(propertyName ?? "value", (object?)value) };
            if (!manager.BeforeAction(this, label, args))
                return;

            var telemetry = BlexDiagnostics.StartAction(this, label);
            field = value;
            InvalidateComputed(propertyName);
            try
            {
                NotifyStateChanged(announced: true);
            }
            finally
            {
                // Record even when a subscriber throws: the mutation *was* applied, and skipping
                // the record would leave persistence/history/DevTools out of sync.
                manager.RecordAction(this, label, args);
                telemetry.Stop();
            }

            return;
        }

        field = value;

        // Inside a sync batch we defer the render until the action completes (single re-render).
        // The dirty flag only matters inside a dispatch; a standalone set with nothing listening
        // records nothing, and must not leak the flag into a later dispatch.
        if (action is not null)
            action.Dirty = true;

        if (_notifyDepth == 0)
        {
            // Inside an async action of this store that is still running, the change is announced when
            // that action is recorded: a reactor dispatching a follow-up must not get it recorded before
            // the action that caused it (DevTools order, undo steps). Outside one, nothing records actions
            // at all, and the manager is told here. Components see the change now either way.
            InvalidateComputed(propertyName);
            NotifyStateChanged(announced: action is not null);

            if (action is null)
                BlexDiagnostics.CountUnrecordedSet(this, propertyName);
        }
        else
        {
            // The notification waits for the batch, but the field has already changed: code later in
            // the same action body reading a computed or a cross-store selector -- validation after a
            // write, say -- must see it, not the value memoized before. Invalidating is flag writes;
            // recomputing stays lazy. Only stamped here; the batch's notification checks the
            // dispatcher, once for the whole action.
            InvalidateComputed(propertyName);
            ChangeVersion++;
            _manager?.MarkStateChanged();
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
        NotifyStateChanged(announced: false);
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
            NotifyStateChanged(announced: false);
        }
    }

    /// <summary>Decrements an effect's pending-run counter and notifies when it reaches zero.</summary>
    protected void EndEffect(ref int pending)
    {
        pending--;
        if (pending == 0)
        {
            InvalidateComputed();
            NotifyStateChanged(announced: false);
        }
    }

    /// <summary>
    /// Waits out an effect's <c>DebounceMilliseconds</c> quiet period on <see cref="TimeProvider"/>. Used by
    /// generated effect wrappers. Completes at once while recorded actions are being replayed into the
    /// manager, which awaits every action before the next: there is nothing left for the wait to decide.
    /// </summary>
    protected Task DebounceEffectAsync(int milliseconds, CancellationToken cancellationToken)
    {
        if (_manager is { IsReplaying: true })
            return cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) : Task.CompletedTask;

        return Task.Delay(TimeSpan.FromMilliseconds(milliseconds), TimeProvider, cancellationToken);
    }

    // The longest wait exponential backoff grows to (unless the first one is already longer).
    private const int MaxRetryDelayMilliseconds = 30_000;

    /// <summary>
    /// Runs an effect body up to <c>1 + <paramref name="retryCount"/></c> times, backing off between
    /// attempts, and bounds each attempt by <paramref name="timeoutMilliseconds"/>. Used by generated
    /// wrappers of effects with <c>RetryCount</c> or <c>TimeoutMilliseconds</c>; the attempts run inside
    /// one recorded action, and only the last failure leaves it.
    /// </summary>
    /// <param name="attempt">Runs the body once, observing the token it is handed.</param>
    /// <param name="cancellationToken">
    /// The effect's own token. Its cancellation (<c>CancelXxx()</c>, a superseding run, disposal) is never
    /// retried: it ends the run, during an attempt or during the wait before the next one.
    /// </param>
    /// <param name="retryCount">How many failed attempts are retried.</param>
    /// <param name="retryDelayMilliseconds">The wait before the first retry; it doubles each time, up to 30 seconds.</param>
    /// <param name="timeoutMilliseconds">
    /// How long one attempt may take; <c>0</c> sets no limit. An attempt that overruns has its token
    /// cancelled and fails with a <see cref="TimeoutException"/>.
    /// </param>
    /// <remarks>
    /// Waits run on <see cref="TimeProvider"/>, so a test advances a fake clock instead of waiting; a
    /// replay of recorded actions skips the backoff, as it skips debouncing. A store that is disposed
    /// stops retrying.
    /// </remarks>
    protected async Task RunEffectAttemptsAsync(
        Func<CancellationToken, Task> attempt,
        CancellationToken cancellationToken,
        int retryCount,
        int retryDelayMilliseconds,
        int timeoutMilliseconds)
    {
        ArgumentNullException.ThrowIfNull(attempt);

        for (var failures = 0; ; failures++)
        {
            ExceptionDispatchInfo failure;
            try
            {
                // No ConfigureAwait(false): the body changes state, which belongs on the dispatch context.
                await RunAttemptAsync(attempt, cancellationToken, timeoutMilliseconds).ConfigureAwait(true);
                return;
            }
            catch (Exception ex) when (failures < retryCount && !cancellationToken.IsCancellationRequested && !_disposed)
            {
                failure = ExceptionDispatchInfo.Capture(ex);
            }

            var wait = TimeSpan.Zero;
            if (retryDelayMilliseconds > 0 && _manager is not { IsReplaying: true })
            {
                var cap = Math.Max(MaxRetryDelayMilliseconds, retryDelayMilliseconds);
                wait = TimeSpan.FromMilliseconds(Math.Min((long)retryDelayMilliseconds << Math.Min(failures, 20), cap));
            }

            BlexDiagnostics.EffectRetry(failures + 1, failure.SourceException, wait);
            if (wait > TimeSpan.Zero)
                await Task.Delay(wait, TimeProvider, cancellationToken).ConfigureAwait(true);

            // Disposed while waiting (an effect without a token has nothing else to stop it): the run
            // ends with the failure it was about to retry.
            if (_disposed)
                failure.Throw();
        }
    }

    private async Task RunAttemptAsync(Func<CancellationToken, Task> attempt, CancellationToken cancellationToken, int timeoutMilliseconds)
    {
        if (timeoutMilliseconds <= 0)
        {
            await attempt(cancellationToken).ConfigureAwait(true);
            return;
        }

        using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMilliseconds), TimeProvider);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
        try
        {
            await attempt(linked.Token).ConfigureAwait(true);
        }
        catch (OperationCanceledException ex) when (timeout.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            // Our deadline, not the effect's own cancellation: a failure, which the effect records (and may retry).
            throw new TimeoutException($"The attempt did not complete within {timeoutMilliseconds} ms.", ex);
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

        // RollbackOnError: what to put back if the body throws.
        var rollback = CaptureRollbackState();
        var rolledBack = false;

        // When it began, on the manager's clock: a snapshot taken since may hold the state a rollback undoes.
        var startedAt = rollback is null ? 0 : _manager?.StateVersion ?? 0;
        var inPlace = _notifyDepth == 0 && _manager is { DetectInPlaceMutations: true } ? BeginInPlaceCheck() : default;
        var telemetry = BlexDiagnostics.StartAction(this, actionName);

        var frame = EnterAction(out var began);

        // Whether a change was already waiting to be recorded -- by the async action of this flow this one
        // runs inside -- before this one began, which a rollback must leave as it found it.
        var dirtyBefore = frame.Dirty;
        _notifyDepth++;
        var body = _manager?.EnterActionBody() ?? default;
        try
        {
            mutation();
            if (inPlace.Snapshot is not null)
                EndInPlaceCheck(inPlace, actionName);
        }
        catch (Exception ex)
        {
            telemetry.Fail(ex);
            if (rollback is not null)
            {
                ApplyRollbackState(rollback);
                frame.Dirty = dirtyBefore;
                rolledBack = true;

                // Before the error hooks run: one reading a computed or a cross-store selector must see the
                // state as rolled back, not a value derived from what was just undone; a WhenAsync waiter re-checks it.
                InvalidateComputed();
                MarkChanged();
                _manager?.OnRolledBack(this, startedAt);
            }

            _manager?.RecordActionError(this, actionName, args, ex);
            throw;
        }
        finally
        {
            // Before this action records itself, so it is not taken for one nested in its own body.
            body.Exit();
            _notifyDepth--;

            // Read before leaving: leaving resets the flag when this action records.
            var changed = frame.Dirty;
            var record = ExitAction(frame, began);

            // A subscription that saw the undone state mid-body -- through a nested action of another store,
            // which recorded -- re-evaluates and lands back on what is really there. Inside an async action of
            // this store, that action's end announces it.
            if (rolledBack)
                AnnounceUnrecorded();

            var recorded = false;

            // Flush the deferred notification when this was the outermost *sync* batch, even inside an
            // async action of this flow still running around it (that one does not hold _notifyDepth, so
            // its nested sync actions must render on completion). A non-zero notify depth means an
            // enclosing sync action of this flow, which flushes for both.
            if (changed && _notifyDepth == 0)
            {
                // No InvalidateComputed here: every write already invalidated what reads it, as it happened.
                // The flag was reset before notifying (ExitAction), so a throwing subscriber can't leak it.
                try
                {
                    // Announced by this action's record -- or, when an async action of this store is still
                    // running above it, by that action's record once it completes.
                    NotifyStateChanged(announced: true);
                }
                finally
                {
                    if (record)
                    {
                        _manager?.RecordAction(this, actionName, args);
                        recorded = true;
                    }
                }
            }
            else if (record)
            {
                // A frame this action began while a sync batch of another flow was open around it -- which a
                // single-threaded dispatcher never interleaves -- still owes its record.
                _manager?.RecordAction(this, actionName, args);
                recorded = true;
            }

            SettleDeferredAnnouncement(recorded);

            // Last: the span covers what the action set off -- its subscribers, and any action they dispatched.
            telemetry.Stop();
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
            try
            {
                DeserializeState((JsonObject)_initialState.DeepClone());
            }
            finally
            {
                // Raw field writes, not assignments through the generated setters: stamp the change the way a
                // setter would -- even when deserializing failed partway -- or the snapshot caches and the
                // in-place check would not know the state moved.
                InvalidateComputed();
                ChangeVersion++;
                _manager?.MarkStateChanged();
                if (CurrentAction is { } action)
                    action.Dirty = true;
            }
        }, null);
    }

    /// <summary>
    /// Async variant. State changes update the UI as they happen (e.g. a loading flag before an
    /// await), but the whole operation is recorded as one named action for time-travel.
    /// </summary>
    /// <remarks>
    /// What the action records is what its own body changed, across its awaits. Another action of the
    /// store started while this one awaits -- a second run of the same effect, a click handled in the
    /// meantime -- is recorded as its own action, not folded into this one.
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
        // Gate the veto on the *synchronous* nesting depth. An action lexically nested in a running
        // synchronous one must not re-veto: the enclosing action already passed the filters. Any other
        // action -- one started while another async action of this store awaits included, the common
        // case since Parallel is the default effect concurrency -- faces them. Only a synchronous
        // Dispatch body raises _notifyDepth, so it distinguishes the two exactly.
        return _notifyDepth != 0
            || _manager is null
            || _manager.BeforeAction(this, actionName, args);
    }

    /// <summary>
    /// Async dispatch for a body whose veto decision has already been taken by
    /// <see cref="BeginAction"/>. Records the action when it completes, unless it runs inside another
    /// action of this flow, which records it.
    /// </summary>
    protected async Task DispatchApprovedAsync(string actionName, Func<Task> mutation, BlexActionArg[]? args)
    {
        ArgumentNullException.ThrowIfNull(mutation);

        // Entered here, before the first await, so the body -- invoked below -- runs in the frame; the
        // assignment reaches only this method's own context, never its caller's.
        var frame = EnterAction(out var began);
        var inPlace = began && _manager is { DetectInPlaceMutations: true } ? BeginInPlaceCheck() : default;
        var telemetry = BlexDiagnostics.StartAction(this, actionName);
        var body = _manager?.EnterActionBody() ?? default;
        try
        {
            await mutation().ConfigureAwait(true);
            EndInPlaceCheck(inPlace, actionName);
        }
        catch (Exception ex)
        {
            telemetry.Fail(ex);
            _manager?.RecordActionError(this, actionName, args, ex);
            throw;
        }
        finally
        {
            body.Exit();
            var recorded = false;
            if (ExitAction(frame, began))
            {
                _manager?.RecordAction(this, actionName, args);
                recorded = true;
            }

            SettleDeferredAnnouncement(recorded);
            telemetry.Stop();
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
    /// time-travel, and by persistence, the prerender handoff and <see cref="RestoreState"/>. Raises
    /// <see cref="StateChanged"/> exactly once, then <see cref="BlexManager.StoreRestored"/> and
    /// <see cref="BlexManager.StateRestored"/> -- the latter once for the whole tree when the manager
    /// is restoring every store.
    /// </summary>
    internal void ApplyRestoredState(JsonObject state)
    {
        var manager = _manager;
        manager?.PrepareForRestore();

        IsRestoring = true;
        try
        {
            DeserializeState(state);
        }
        finally
        {
            IsRestoring = false;

            // Stamped even when deserializing failed partway: some fields may already have moved.
            ChangeVersion++;
            manager?.MarkStateChanged();
        }

        InvalidateComputed();
        try
        {
            NotifyStateChanged(announced: true);
        }
        finally
        {
            manager?.OnStoreRestored(this);
        }
    }

    /// <summary>Whether <see cref="Dispose()"/> has run on this store.</summary>
    protected bool IsDisposed => _disposed;

    /// <summary>The state an action started from, for <see cref="BlexManager.DetectInPlaceMutations"/>.</summary>
    private readonly record struct InPlaceCheck(string? Snapshot, long Version);

    private InPlaceCheck BeginInPlaceCheck()
    {
        if (_manager is not { DetectInPlaceMutations: true } manager)
            return default;

        try
        {
            return new InPlaceCheck(SerializeState().ToJsonString(), ChangeVersion);
        }
        catch (Exception ex)
        {
            // A development aid must never stop an action the pipeline already approved.
            manager.ReportError("mutation", ex, $"{Name}: snapshot for the in-place mutation check");
            return default;
        }
    }

    /// <summary>
    /// Reports an action that changed the store's state without assigning any of its properties -- an item
    /// added to a list in place, a property set on a mutable object. Nothing notified, recorded or persisted
    /// that change, so the UI, DevTools, undo and storage all went on showing the state from before it.
    /// </summary>
    private void EndInPlaceCheck(InPlaceCheck check, string actionName)
    {
        // Nothing to compare, or the action assigned something and was announced like any other.
        if (check.Snapshot is null || ChangeVersion != check.Version || _manager is not { } manager)
            return;

        try
        {
            if (SerializeState().ToJsonString() == check.Snapshot)
                return;
        }
        catch (Exception ex)
        {
            manager.ReportError("mutation", ex, $"{Name}/{actionName}");
            return;
        }

        manager.ReportError(
            "mutation",
            new InvalidOperationException(
                $"The action '{Name}/{actionName}' changed the store's state without assigning any [BlexState] property - an item added to a collection in place, or a property set on a mutable object. Nothing was notified, recorded or persisted. Assign a new value instead: Items = [.. Items, item]."),
            $"{Name}/{actionName}");
    }

    /// <summary>
    /// A shallow copy of the state fields, taken as a synchronous action starts so a throw can put them
    /// back (<c>[BlexStoreAttribute(RollbackOnError = true)]</c>), or <c>null</c> when the store does not
    /// roll back. Overridden by the generator.
    /// </summary>
    protected virtual object?[]? CaptureRollbackState() => null;

    /// <summary>Puts back the fields <see cref="CaptureRollbackState"/> copied. Overridden by the generator.</summary>
    protected virtual void ApplyRollbackState(object?[] state)
    {
    }

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
                Console.Error.WriteLine(BlexManager.EscapeControlCharacters($"[Blex] dispose failed ({Name}): {ex.Message}"));
        }
    }
}
