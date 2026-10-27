using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Bit.Bmotion;
/// <summary>
/// Central C# animation engine - the JS equivalent of the full <c>BitBmotion.js</c>
/// animation loop, now running in Blazor WebAssembly.
///
/// One instance is shared across the whole component tree (DI scoped).
/// The slim JS bridge calls <see cref="ComputeFrame"/> synchronously each
/// <c>requestAnimationFrame</c> tick and receives back a dictionary of
/// CSS style updates to apply to the DOM.
/// </summary>
/// <remarks>
/// <para>
/// <b>Threading:</b> the engine is intentionally lock-free. All of its mutable state
/// (the element map, per-element driver dictionaries, completion batches, dirty flags) is only
/// safe to touch from a single thread, and every entry point - the rAF <see cref="ComputeFrame"/>
/// tick, the synchronous JS drag callbacks, and the awaited animate APIs - is expected to run on
/// the Blazor WebAssembly UI thread (or, on Blazor Server, the circuit's renderer dispatcher).
/// Completion continuations are scheduled back onto the context the animation was started from
/// (see <see cref="BmotionContinuations"/>), never onto the thread pool. <b>Do not enable WebAssembly multithreading (<c>&lt;WasmEnableThreads&gt;</c>)</b> with
/// this library: the engine has no synchronization and concurrent access would corrupt its state.
/// </para>
/// </remarks>
public sealed class BmotionAnimationEngine : IAsyncDisposable
{
    private readonly IBmotionInterop _interop;
    private readonly ILogger<BmotionAnimationEngine>? _logger;
    private readonly Dictionary<string, BmotionElementAnimationState> _elements = new();
    private DotNetObjectReference<BmotionAnimationEngine>? _dotnet;
    private bool _loopRunning;
    private readonly SemaphoreSlim _loopStartGate = new(1, 1);
    private bool _disposed;
    private bool _reducedMotionDetected;
    // A single in-flight detection attempt shared by all concurrent callers so the browser probe
    // and live-change subscription run exactly once (reset to null on failure to allow a retry).
    private Task? _reducedMotionDetection;

    // Reused across frames so the rAF tick doesn't allocate a fresh outer dictionary every ~16 ms.
    // Marshaled synchronously to JS before the next ComputeFrame runs (single-threaded Blazor WASM).
    private readonly Dictionary<string, Dictionary<string, string>> _frameResult = new();
    // Reused across frames: the elements ComputeFrame ticks, copied out of _elements first.
    private readonly List<KeyValuePair<string, BmotionElementAnimationState>> _tickBuffer = new();

    public BmotionAnimationEngine(IBmotionInterop interop, ILogger<BmotionAnimationEngine>? logger = null)
    {
        _interop = interop;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Reduced-motion (accessibility)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// The OS-level <c>prefers-reduced-motion</c> preference, detected once via the
    /// browser. <c>false</c> until <see cref="EnsureReducedMotionDetectedAsync"/> has run.
    /// </summary>
    public bool OsPrefersReducedMotion { get; private set; }

    /// <summary>
    /// Detects the user's <c>prefers-reduced-motion</c> setting from the browser the
    /// first time it is called and caches the result for the lifetime of this engine.
    /// </summary>
    public ValueTask EnsureReducedMotionDetectedAsync()
    {
        if (_reducedMotionDetected) return ValueTask.CompletedTask;
        // Concurrent first-callers can all pass the check above before any of them completes the
        // probe. Gate them behind a single shared detection task so the browser probe and the
        // live-change subscription run once rather than racing duplicate setups.
        _reducedMotionDetection ??= DetectReducedMotionAsync();
        return new ValueTask(_reducedMotionDetection);
    }

    private async Task DetectReducedMotionAsync()
    {
        try
        {
            OsPrefersReducedMotion = await _interop.PrefersReducedMotionAsync();
            // Mark detection complete as soon as the initial probe succeeds: the probed value is
            // valid regardless of whether the live-change subscription below can be set up.
            _reducedMotionDetected = true;
        }
        catch
        {
            // Detection is best-effort: if the browser probe fails we default to
            // animating normally rather than letting it break element initialisation.
            OsPrefersReducedMotion = false;
            // Clear the shared task so a later caller can retry the probe.
            _reducedMotionDetection = null;
            return;
        }

        try
        {
            // Subscribe to live OS changes so toggling prefers-reduced-motion at runtime is honoured.
            // Best-effort: a watch-setup failure must not discard the successfully probed value above.
            _dotnet ??= DotNetObjectReference.Create(this);
            await _interop.WatchReducedMotionAsync(_dotnet);
        }
        catch
        {
            // Watch subscription failed; the initial preference stays valid and detection stays complete.
        }
    }

    /// <summary>JS → C# callback fired when the OS <c>prefers-reduced-motion</c> setting changes.</summary>
    [JSInvokable]
    public void OnReducedMotionChanged(bool prefersReduced) => OsPrefersReducedMotion = prefersReduced;

    // ═══════════════════════════════════════════════════════════════════════════
    // Element lifecycle
    // ═══════════════════════════════════════════════════════════════════════════

    // The states of elements only programmatic animations (BmotionAnimateService) ever owned, kept
    // once the last of those animations released them. The element still shows those values, so the
    // next animation on it must start from them - a fresh state would start it from identity (x 0,
    // scale 1) and drop every value it doesn't animate. Bounded: an entry is a few dictionaries.
    private readonly Dictionary<string, BmotionElementAnimationState> _retained = new();
    private const int MaxRetained = 256;
    // Stamps each retained state, so a full cache evicts the one retained longest ago.
    private long _retainedSeq;

    /// <summary>Register an element and optionally seed its initial CSS state.</summary>
    public void RegisterElement(string elementId, Dictionary<string, object?>? initialValues = null)
        => RegisterElement(elementId, initialValues, programmatic: false);

    /// <param name="elementId">The element's id.</param>
    /// <param name="initialValues">Values to seed its state with, applied instantly.</param>
    /// <param name="programmatic">
    /// Registered by a programmatic animation rather than a <c>&lt;Bmotion&gt;</c>: such an element
    /// resumes the state it was released with (see <see cref="_retained"/>).
    /// </param>
    internal void RegisterElement(string elementId, Dictionary<string, object?>? initialValues, bool programmatic)
    {
        if (!_elements.TryGetValue(elementId, out var state))
        {
            if (!(programmatic && _retained.Remove(elementId, out state)))
            {
                // A component mounting with this id renders it from scratch.
                _retained.Remove(elementId);
                state = new BmotionElementAnimationState
                {
                    // Without synchronous interop (Blazor Server) there is no rAF loop: animations
                    // either offload to the browser compositor (WAAPI) or collapse to instant sets.
                    ForceInstant = !_interop.IsInProcess,
                    // With it, the browser can be asked where an offloaded animation has really got to before
                    // anything is read off it. Without it, the animation that takes over starts from what the
                    // page shows instead (TryBuildTimelineAsync), so the engine's clock is never shown.
                    BrowserPlayedMs = _interop.IsInProcess ? token => _interop.GetWaapiPlayedMs(elementId, token) : null,
                };
            }
            _elements[elementId] = state;
        }
        if (!programmatic) state.OwnedByComponent = true;
        // Reference-counted: the same element may be owned by a <Bmotion> and one or more
        // concurrent AnimateAsync calls at once.
        state.RefCount++;
        if (initialValues != null)
            state.SetInstant(initialValues);
    }

    /// <summary>Release one owner; cancels animations and removes the element only when the last owner releases it.</summary>
    public void UnregisterElement(string elementId)
    {
        if (_elements.TryGetValue(elementId, out var state))
        {
            if (state.RefCount > 0) state.RefCount--;
            // Other owners (e.g. an overlapping animation) still hold the element - keep it alive
            // so their in-flight animations aren't stranded by a premature teardown.
            if (state.RefCount > 0) return;
            state.CancelAll();
            _elements.Remove(elementId);
            if (!state.OwnedByComponent)
            {
                // Oldest first: a dictionary's own order is not insertion order once entries were removed.
                if (_retained.Count >= MaxRetained) _retained.Remove(_retained.MinBy(kv => kv.Value.RetainedAt).Key);
                state.RetainedAt = ++_retainedSeq;
                _retained[elementId] = state;
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Animation control
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Start animating to the given values. Returns immediately (fire-and-forget).
    /// <para>
    /// Set <paramref name="setAsBase"/> to <c>true</c> only for the element's resting target
    /// (the <c>Animate</c>/variant state a gesture layer should revert to). One-off programmatic
    /// animations and drag snap-backs leave it <c>false</c> so they don't clobber the gesture base
    /// (which would strand unrelated animated properties when a gesture later deactivates).
    /// </para>
    /// <para>
    /// <paramref name="baseTransition"/> is what a gesture reverting to this target plays with, when
    /// that differs from <paramref name="transition"/>: an orchestration delay (a variant child's
    /// stagger slot, an afterChildren wait) times this one play only, not every later hover-out. Pass
    /// a default config rather than null for "the engine's defaults", since null means "same as
    /// <paramref name="transition"/>".
    /// </para>
    /// <para>
    /// <paramref name="onSettled"/> runs once the animation is over however it ended - completed,
    /// superseded or stopped - where <paramref name="onComplete"/> runs only on a natural finish.
    /// </para>
    /// </summary>
    internal async ValueTask AnimateToAsync(
        string elementId,
        Dictionary<string, object?> values,
        BmotionTransitionConfig? transition,
        Func<Task>? onComplete = null,
        bool setAsBase = false,
        BmotionTransitionConfig? baseTransition = null,
        Action? onSettled = null)
    {
        if (!_elements.TryGetValue(elementId, out var state)) { onSettled?.Invoke(); return; }
        if (setAsBase) state.SetBaseAnimation(values, baseTransition ?? transition);

        TaskCompletionSource<bool>? tcs = null;
        if (onComplete != null || onSettled != null)
        {
            tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            // Unwrapped so the nested onComplete() Task is observed rather than dropped (keeps the
            // documented fire-and-forget behaviour of this method), and raised on the caller's
            // context so a Blazor Server consumer's handler can re-render.
            // The result flag is true only on natural completion; a superseded/cancelled
            // animation resolves with false so OnAnimationComplete is NOT raised for it.
            _ = tcs.Task.OnCallerContext(t =>
            {
                // onSettled runs whichever way it ended (completed, superseded, stopped).
                onSettled?.Invoke();
                return t.Result && onComplete != null ? onComplete() : Task.CompletedTask;
            });
        }

        await PlayAsync(elementId, state, values, transition, tcs);
    }

    /// <summary>Animate to the given values and await animation completion.</summary>
    internal async ValueTask AnimateToAwaitAsync(
        string elementId,
        Dictionary<string, object?> values,
        BmotionTransitionConfig? transition,
        bool setAsBase = false)
    {
        if (!_elements.TryGetValue(elementId, out var state)) return;
        if (setAsBase) state.SetBaseAnimation(values, transition);

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        await PlayAsync(elementId, state, values, transition, tcs);
        await tcs.Task;
    }

    /// <summary>Instantly set values without any animation.</summary>
    public void SetInstant(string elementId, Dictionary<string, object?> values)
    {
        if (_elements.TryGetValue(elementId, out var state))
        {
            // Fold overlapping compositor animations back into state synchronously from the
            // mirrored plans; the JS-side cancel is fire-and-forget.
            var tokens = state.RealizeWaapiPlans(values.Keys, continueOthers: true);
            if (tokens != null) CancelWaapiTokens(elementId, tokens, commit: false);

            state.SetInstant(values);
            // Kick the loop for a single frame so the change is flushed to the DOM even when
            // the element is otherwise at rest (an instant Set has dirty values but no active
            // animation, so without this it would never be emitted).
            KickFlush(elementId, state);
        }
    }

    /// <summary>
    /// Records an element's resting target without animating to it - for an element the runtime
    /// takes over already at rest because it was prerendered there. The values are already in the
    /// DOM, so nothing is flushed; the target only becomes the base a gesture layer reverts to,
    /// exactly as an <c>Animate</c> played with <c>setAsBase</c> would have left it.
    /// </summary>
    internal void SetBaseAnimation(string elementId, Dictionary<string, object?> values, BmotionTransitionConfig? transition)
    {
        if (_elements.TryGetValue(elementId, out var state))
            state.SetBaseAnimation(values, transition);
    }

    /// <summary>
    /// Pauses rAF transform writes for an element for <paramref name="durationMs"/> so a WAAPI FLIP
    /// layout animation can own the transform without the engine fighting it each frame.
    /// </summary>
    public void SuspendTransformWrites(string elementId, double durationMs)
    {
        if (_elements.TryGetValue(elementId, out var state))
            state.SuspendTransformWrites(durationMs);
    }

    /// <summary>
    /// Returns a snapshot of the element's current CSS declarations (transform + numeric + string +
    /// path values), or <c>null</c> when the element is unknown. Used to re-flush live styles after
    /// a Blazor re-render rewrites the element's <c>style</c> attribute.
    /// </summary>
    public Dictionary<string, string>? GetCurrentStyles(string elementId)
        => _elements.TryGetValue(elementId, out var state) ? state.BuildSnapshotStyles() : null;

    /// <summary>Returns <c>true</c> if an element is currently registered with the engine.</summary>
    public bool IsRegistered(string elementId) => _elements.ContainsKey(elementId);

    /// <summary>
    /// A read-only snapshot of every registered element's live animation state, for the
    /// <c>&lt;BmotionInspector&gt;</c> debug overlay. Each element reports its active drivers and
    /// current transform / numeric / string values. Call from the UI thread (like all engine APIs).
    /// </summary>
    public IReadOnlyList<BmotionElementDiagnostics> GetDiagnostics()
    {
        var list = new List<BmotionElementDiagnostics>(_elements.Count);
        foreach (var (id, state) in _elements)
        {
            var browser = state.WaapiPlanKeys.ToArray();
            list.Add(new BmotionElementDiagnostics(
                id,
                state.ActiveDriverCount,
                state.HasActiveAnimations || browser.Length > 0,
                state.IsDragging,
                state.ActiveDriverKeys.ToArray(),
                new Dictionary<string, double>(state.Transforms),
                new Dictionary<string, double>(state.NumericValues),
                new Dictionary<string, string>(state.StringValues))
            {
                BrowserProperties = browser,
            });
        }
        return list;
    }

    /// <summary>
    /// Sets (or clears) a per-frame callback invoked with the CSS declarations flushed to the
    /// element each frame. Used by the <c>Bmotion.OnUpdate</c> parameter.
    /// </summary>
    internal void SetOnFrame(string elementId, Action<IReadOnlyDictionary<string, string>>? callback)
    {
        if (_elements.TryGetValue(elementId, out var state))
            state.OnFrame = callback;
    }

    /// <summary>
    /// Finish all animations on an element immediately, snapping every property to its target
    /// (end) value, then flush the final frame to the DOM.
    /// </summary>
    public void Complete(string elementId)
    {
        if (_elements.TryGetValue(elementId, out var state))
        {
            var tokens = state.CompleteWaapiPlans();
            if (tokens != null) CancelWaapiTokens(elementId, tokens, commit: false);
            state.CompleteAll();
            // Write the final values now, from a snapshot: a programmatic animation's controls
            // release (unregister) the element straight after Complete, before the next frame - or
            // flush - would have found it, and the cancelled compositor animation would leave it
            // showing its pre-animation inline style.
            if (state.BuildSnapshotStyles() is { Count: > 0 } final) _ = ApplyFinalAsync(final);
            KickFlush(elementId, state);
        }

        async Task ApplyFinalAsync(Dictionary<string, string> styles)
        {
            try { await _interop.ApplyStylesAsync(elementId, styles); }
            catch { /* the element may be gone */ }
        }
    }

    // ── Seeking (BmAnimationControls) ─────────────────────────────────────────

    /// <summary>
    /// The element's values for <paramref name="keys"/> as an animation about to start on them would start from -
    /// reading off the page any it has never seen - which is where a seek puts the element back to before it plays the
    /// animation again from part-way in.
    /// </summary>
    internal async ValueTask<Dictionary<string, object?>> CaptureStartAsync(string elementId, IReadOnlyCollection<string> keys)
    {
        if (!_elements.TryGetValue(elementId, out var state)) return new();
        // A browser animation still playing on these keys is where the new one starts from: folded in now (as the
        // start would do a moment later anyway) rather than read as the value it set out from.
        InterruptWaapiOverlaps(elementId, state, keys);
        if (state.UnknownKeys(keys) is { } unknown)
            await ReadStylesheetAsync(elementId, state, new Dictionary<string, object?>(), unknown, null, null, null);
        return CaptureValues(elementId, keys);
    }

    /// <summary>
    /// Puts the element back to <paramref name="start"/> so an animation can be played again from part-way in: what
    /// plays on it is cancelled without committing, and leaves no velocity behind for the replay to inherit.
    /// </summary>
    internal void ResetForSeek(string elementId, Dictionary<string, object?> start)
    {
        if (!_elements.TryGetValue(elementId, out var state)) return;
        var tokens = state.RealizeWaapiPlans(null);
        if (tokens != null) CancelWaapiTokens(elementId, tokens, commit: false);
        state.CancelAll();
        state.SetInstant(start);
        state.ForgetVelocity();
        KickFlush(elementId, state);
    }

    /// <summary>
    /// Moves the browser animations playing on the element to <paramref name="playedMs"/> of animation time (delay
    /// included) - in place and exact, cheap enough to scrub with. False when the element has anything else playing (a
    /// frame-loop driver) or nothing at all: the seek then plays the animation again from there instead.
    /// </summary>
    internal bool TrySeekBrowserAnimations(string elementId, double playedMs)
    {
        if (!_elements.TryGetValue(elementId, out var state) || !state.HasWaapiPlans || state.HasActiveAnimations) return false;
        state.SeekWaapiPlans(Environment.TickCount64, playedMs);
        _ = SeekAsync();
        return true;

        async Task SeekAsync()
        {
            try { await _interop.SeekWaapiAnimationsAsync(elementId, playedMs); }
            catch { /* the element may be gone */ }
        }
    }

    /// <summary>Stop animations on specific properties (or all when <paramref name="properties"/> is null/empty).</summary>
    public void Stop(string elementId, string[]? properties)
    {
        if (_elements.TryGetValue(elementId, out var state))
        {
            // Freeze compositor animations in place too: realize their current values,
            // snapshot them inline (commit) and cancel.
            var keys = properties is { Length: > 0 } ? properties : null;
            // Stopping some properties leaves the rest of their animation playing.
            var tokens = state.RealizeWaapiPlans(keys, continueOthers: true);
            if (tokens != null)
            {
                CancelWaapiTokens(elementId, tokens, commit: true);
                KickFlush(elementId, state);
            }
            state.Cancel(properties);
        }
    }

    /// <summary>
    /// Sets (or clears) the element's transform-string rewrite - see <see cref="BmTransformTemplate"/>.
    /// </summary>
    internal void SetTransformTemplate(string elementId, BmTransformTemplate? template)
    {
        if (_elements.TryGetValue(elementId, out var state))
            state.TransformTemplate = template;
    }

    /// <summary>
    /// Sets the playback rate for an element's animations: 1 = realtime, 0 = paused,
    /// 2 = twice as fast. Negative and non-finite rates are coerced to 0.
    /// </summary>
    public void SetPlaybackRate(string elementId, double rate)
    {
        if (!_elements.TryGetValue(elementId, out var state)) return;
        rate = double.IsFinite(rate) && rate >= 0 ? rate : 0;
        state.PlaybackRate = rate;
        // The compositor animations play on the browser's clock: change their rate there too, and
        // in the mirrored plans, so an interruption later still reads where they really are.
        if (!state.HasWaapiPlans) return;
        state.SetWaapiPlansRate(rate);
        _ = ApplyRateAsync();

        async Task ApplyRateAsync()
        {
            try { await _interop.SetWaapiPlaybackRateAsync(elementId, rate); }
            catch { /* the element may be gone */ }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Gesture layer management
    // ═══════════════════════════════════════════════════════════════════════════

    internal async ValueTask ActivateGestureLayerAsync(
        string elementId, string gesture,
        Dictionary<string, object?> values, BmotionTransitionConfig? transition)
    {
        if (!_elements.TryGetValue(elementId, out var state)) return;
        // The layer is registered before anything is awaited: a pointerleave arriving while this
        // activation is still reading or interrupting must find the layer to remove, or the overlay
        // would apply after the gesture already ended and stay on.
        if (state.ActivateGestureLayer(gesture, values, transition) is not { } request) return;
        var generation = ++_gestureGeneration;
        _gestureGenerations[(elementId, gesture)] = generation;
        // A gesture's target is not the element's own: what it borrows from the stylesheet stays borrowed.
        await PlayAsync(elementId, state, request.Values, request.Transition, tcs: null, claim: false,
            isCurrent: () => _gestureGenerations.TryGetValue((elementId, gesture), out var g) && g == generation);
    }

    public async ValueTask DeactivateGestureLayerAsync(string elementId, string gesture)
    {
        if (!_elements.TryGetValue(elementId, out var state)) return;
        _gestureGenerations.Remove((elementId, gesture));
        if (state.DeactivateGestureLayer(gesture) is not { } request) return;
        await PlayAsync(elementId, state, request.Values, request.Transition, tcs: null, claim: false,
            stylesheetKeys: request.StylesheetKeys);
    }

    // Which activation of a gesture on an element is the live one: an activation that resumes after its gesture
    // was deactivated (or activated again) must not play.
    private readonly Dictionary<(string ElementId, string Gesture), long> _gestureGenerations = new();
    private long _gestureGeneration;

    // ═══════════════════════════════════════════════════════════════════════════
    // Drag position (called synchronously from JS - Blazor WASM only)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Updates the drag position in the element's transform state from a
    /// synchronous JS pointer-move call. The position will be included in the
    /// next <see cref="ComputeFrame"/> output.
    /// </summary>
    /// <summary>
    /// A drag starts. On WebAssembly the drag owns the element's transform from the press, not only from its first
    /// move: an overlay it starts with (<c>WhileDrag</c>) then plays on the frame loop, composed with the drag, rather
    /// than in the browser - whose transform keyframes would hold the element at its starting place, the drag's moves
    /// hidden underneath, until they ended. On Blazor Server the frame loop has nothing to animate with, and the bridge
    /// shows the drag on top of a browser-played overlay anyway (the CSS translate property), so nothing changes there.
    /// </summary>
    internal void BeginDrag(string elementId)
    {
        if (_interop.IsInProcess && _elements.TryGetValue(elementId, out var state)) state.BeginDrag();
    }

    public void SetDragPosition(string elementId, double x, double y)
    {
        if (!_elements.TryGetValue(elementId, out var state)) return;
        state.SetDragPosition(x, y);
        // The drag is what moves the element now, so something has to write it: on WebAssembly the
        // frame loop (without this, dragging an otherwise idle element left it where it was until
        // the release started an animation), on Blazor Server an async flush of each reported position.
        if (!_loopRunning || !_interop.IsInProcess) KickFlush(elementId, state);
    }

    /// <summary>
    /// The values the element shows now for <paramref name="keys"/>, for the ones the engine knows -
    /// what an exit that is cancelled part-way returns them to.
    /// </summary>
    internal Dictionary<string, object?> CaptureValues(string elementId, IEnumerable<string> keys)
    {
        var captured = new Dictionary<string, object?>();
        if (!_elements.TryGetValue(elementId, out var state)) return captured;
        foreach (var key in keys)
            if (state.CurrentValue(key) is { } value) captured[key] = value;
        return captured;
    }

    /// <summary>Returns the current transform x/y for an element (used at drag start).</summary>
    public (double x, double y) GetCurrentXY(string elementId)
    {
        if (!_elements.TryGetValue(elementId, out var state)) return (0, 0);
        // Drag start: the JS bridge has just committed+cancelled any compositor animations on
        // this element, so fold their current values (from the mirrored plans) into state.
        state.RealizeWaapiPlans(null);
        return state.GetCurrentXY();
    }

    /// <summary>
    /// Completes a drag and optionally starts inertia animations.
    /// </summary>
    // The spring that brings a drag back inside its constraints - a slow release out of bounds, or a throw at a bound.
    private const double DragSnapStiffness = 400;
    private const double DragSnapDamping = 35;

    internal async ValueTask EndDragAsync(
        string elementId,
        double velX, double velY,
        bool momentum,
        BmDragConstraints? constraints,
        string? axis,
        BmotionTransitionConfig? snapTransition)
    {
        if (!_elements.TryGetValue(elementId, out var state)) return;

        state.EndDrag();

        var (posX, posY) = state.GetCurrentXY();

        // A DragTransition authored as an inertia (Bm.Inertia(...)) configures the momentum itself -
        // its decay knobs and, most usefully, its ModifyTarget snap hook (Bm.SnapTo). Authored as a
        // spring/tween it only describes the constraint snap-back, so the momentum keeps its
        // defaults. Reading it here is what makes snap-to-grid / carousel paging work on release.
        var inertiaTemplate = snapTransition?.Type == BmotionTransitionType.Inertia ? snapTransition : null;
        // The snap-back branch below needs a spring; an inertia template is not one.
        if (inertiaTemplate != null) snapTransition = null;

        // Both axes play as one animation with a transition per axis: the transform is one CSS property, so two
        // separate animations of it (inertia on x, a snap-back on y) would cancel each other in the browser.
        var target = new Dictionary<string, object?>();
        var perAxis = new Dictionary<string, BmotionTransitionConfig>(StringComparer.Ordinal);

        bool inertiaXStarted = false, inertiaYStarted = false;
        // Momentum with a snap target must run even for a slow release: without it a gentle drag
        // would come to rest wherever the pointer left it, defeating the snap entirely.
        bool alwaysCoast = inertiaTemplate?.ModifyTarget is not null;
        if (momentum)
        {
            // velX/velY arrive from JS already scaled to "px per frame" (~16 ms). The * 50 factor
            // converts that frame-relative figure into the larger projected-distance velocity the
            // exponential-decay inertia driver expects (tuned so a natural flick throws roughly the
            // distance Framer Motion produces for the same gesture).
            const double inertiaVelocityScale = 50.0;

            BmotionTransitionConfig BuildInertia(double velocity, double? min, double? max)
            {
                var c = inertiaTemplate?.Clone() ?? new BmotionTransitionConfig();
                c.Type = BmotionTransitionType.Inertia;
                c.InertiaVelocity = velocity * inertiaVelocityScale;
                // Drag bounds are measured live (element-bounds constraints resolve in JS), so they
                // always win over any Min/Max authored on the template.
                c.InertiaMin = min;
                c.InertiaMax = max;
                // A throw that reaches a bound bounces off it with the spring a slow release out of bounds snaps
                // back with (below), unless the DragTransition says otherwise - one feel for both.
                c.BounceStiffness ??= DragSnapStiffness;
                c.BounceDamping ??= DragSnapDamping;
                return c;
            }

            if (axis != "y" && (Math.Abs(velX) > 0.5 || alwaysCoast))
            {
                target["x"] = posX;
                perAxis["x"] = BuildInertia(velX, constraints?.Left, constraints?.Right);
                inertiaXStarted = true;
            }

            if (axis != "x" && (Math.Abs(velY) > 0.5 || alwaysCoast))
            {
                target["y"] = posY;
                perAxis["y"] = BuildInertia(velY, constraints?.Top, constraints?.Bottom);
                inertiaYStarted = true;
            }
        }

        // Snap-back runs independently of momentum: when momentum produced no inertia animation
        // for an axis (velocity below threshold or disabled) the element can still be out of
        // bounds, so any axis without an active inertia animation is corrected here.
        if (constraints != null)
        {
            // Snap to constraint bounds
            double cx = posX, cy = posY;
            bool snap = false;
            var snapT = snapTransition ?? new BmotionTransitionConfig
                { Type = BmotionTransitionType.Spring, Stiffness = DragSnapStiffness, Damping = DragSnapDamping };

            if (axis != "y" && !inertiaXStarted)
            {
                if (constraints.Left.HasValue && cx < constraints.Left.Value) { cx = constraints.Left.Value; snap = true; }
                if (constraints.Right.HasValue && cx > constraints.Right.Value) { cx = constraints.Right.Value; snap = true; }
            }
            if (axis != "x" && !inertiaYStarted)
            {
                if (constraints.Top.HasValue && cy < constraints.Top.Value) { cy = constraints.Top.Value; snap = true; }
                if (constraints.Bottom.HasValue && cy > constraints.Bottom.Value) { cy = constraints.Bottom.Value; snap = true; }
            }

            if (snap)
            {
                if (axis != "y" && !inertiaXStarted) { target["x"] = cx; perAxis["x"] = snapT; }
                if (axis != "x" && !inertiaYStarted) { target["y"] = cy; perAxis["y"] = snapT; }
            }
        }

        if (target.Count == 0) return;
        // Every key carries its own transition; the base only stands in for keys that have none (there are none).
        var release = new BmotionTransitionConfig { Properties = perAxis };
        await PlayAsync(elementId, state, target, release, tcs: null);
    }

    /// <summary>Returns the current CSS transform string for the element (used by FLIP).</summary>
    public string? GetCurrentTransformString(string elementId)
    {
        if (!_elements.TryGetValue(elementId, out var state)) return null;
        return state.ComposeTransform(state.Transforms);
    }

    /// <summary>
    /// The transform the element will be showing <paramref name="afterMs"/> from now: its current
    /// components with every compositor animation in flight advanced that far (to its end, if it is
    /// over by then). What a FLIP of that length ends on - it masks the compositor animation while it
    /// plays, so it must land exactly where that animation will be when the FLIP lifts, not where it
    /// began and not at an end it has not reached yet.
    /// </summary>
    internal string? GetTransformStringAfter(string elementId, double afterMs)
    {
        if (!_elements.TryGetValue(elementId, out var state)) return null;
        return state.ComposeTransform(state.TransformsAfter(afterMs));
    }

    /// <summary>The element's playback rate (1 = realtime, 0 = paused); 1 for an unknown element.</summary>
    internal double GetPlaybackRate(string elementId)
        => _elements.TryGetValue(elementId, out var state) ? state.PlaybackRate : 1;

    /// <summary>Returns the <see cref="BmotionElementAnimationState"/> for an element, or null.</summary>
    internal BmotionElementAnimationState? GetState(string elementId)
        => _elements.GetValueOrDefault(elementId);

    // ═══════════════════════════════════════════════════════════════════════════
    // Browser playback (Web Animations)
    //
    // Whatever the browser can reproduce exactly is handed to it as a ready-made Web Animation
    // (IMPROVEMENT_PLAN.md, AD-1): a two-keyframe timeline with the curve as its easing when every
    // key shares one tween or zero-velocity spring, otherwise the frame loop's own output recorded
    // frame by frame. Playback needs no per-frame interop - transforms and opacity even run on the
    // compositor - and only async interop, so it works on Blazor Server too. The engine keeps a
    // mirrored "plan" (the same samples) so interruptions can compute the element's current
    // values, and velocities, without reading the DOM.
    // ═══════════════════════════════════════════════════════════════════════════

    private int _waapiTokenSeq;
    private bool? _linearEasingSupported;

    private sealed record WaapiOffload(
        BmotionElementAnimationState.WaapiPlan Plan,
        object[] Keyframes,
        Dictionary<string, object?> Timing);

    /// <summary>Whether this environment can run the per-frame rAF loop (Blazor WebAssembly).</summary>
    internal bool SupportsFrameLoop => _interop.IsInProcess;

    private bool _serverDegradationWarned;

    // Warns once (per engine) when an animation only the frame loop can play degrades to an
    // instant change on Blazor Server. Skips genuinely-instant transitions (duration-0 tweens) so a
    // set()-like call doesn't produce a false positive.
    private void WarnIfDegradingOnServer(
        string elementId, Dictionary<string, object?> values, BmotionTransitionConfig? transition)
    {
        if (_interop.IsInProcess || _serverDegradationWarned || _logger is null) return;
        bool instant = transition is { Type: BmotionTransitionType.Tween, Duration: <= 0 };
        if (instant || values.Count == 0) return;
        _serverDegradationWarned = true;
        _logger.LogWarning(
            "Bit.Bmotion: an animation on '{ElementId}' collapsed to an instant change on Blazor Server "
            + "(no per-frame loop; {Properties} need it - a per-frame callback, or an SVG d/points morph). "
            + "Inject BmotionCapabilities to detect this - SupportsFrameLoop is false here.",
            elementId, string.Join(", ", values.Keys));
    }

    /// <summary>
    /// rAF-loop start on WebAssembly; on Blazor Server (no sync interop) drives the element's
    /// zero-duration drivers to completion and flushes the result through async interop instead.
    /// </summary>
    private async ValueTask EnsureTickingAsync(string elementId, BmotionElementAnimationState state)
    {
        if (_interop.IsInProcess)
        {
            await EnsureLoopRunningAsync();
            return;
        }
        await FlushRemoteAsync(elementId, state);
    }

    private async ValueTask FlushRemoteAsync(string elementId, BmotionElementAnimationState state)
    {
        // ForceInstant collapses every driver to zero duration, so a single tick settles and
        // emits the final values. Copy the reused tick buffer before the async interop call.
        var updates = state.Tick(Environment.TickCount64);
        if (updates is { Count: > 0 })
        {
            try { await _interop.ApplyStylesAsync(elementId, new Dictionary<string, string>(updates)); }
            catch { /* element may be gone during teardown */ }
        }
    }

    /// <summary>
    /// Realizes and cancels every compositor animation overlapping <paramref name="keys"/>
    /// (<c>null</c> = all), so state reflects the element's live values before a new owner
    /// (animation, gesture, instant set) takes over.
    /// </summary>
    /// <remarks>
    /// A plan overlapping only some of its keys (an <c>x</c> plan when <c>y</c> takes over the
    /// single <c>transform</c> property) keeps its other keys going on the frame loop - unless
    /// <paramref name="continueOthers"/> is off, for a new owner that takes the whole property.
    /// </remarks>
    private async ValueTask InterruptWaapiOverlapsAsync(
        string elementId, BmotionElementAnimationState state, IReadOnlyCollection<string>? keys,
        bool continueOthers = true)
    {
        if (!state.HasWaapiPlans) return;
        var tokens = state.RealizeWaapiPlans(keys, continueOthers);
        if (tokens == null) return;
        foreach (var token in tokens)
        {
            // commit=true snapshots the current values inline before cancelling, so nothing
            // flashes in the gap before the engine's own write lands.
            try { await _interop.CancelWaapiAnimationAsync(elementId, token, commit: true); }
            catch { /* best-effort */ }
        }
        await EnsureTickingAsync(elementId, state);
    }

    private async ValueTask<bool> LinearEasingSupportedAsync()
    {
        if (_linearEasingSupported is null)
        {
            try { _linearEasingSupported = await _interop.SupportsLinearEasingAsync(); }
            catch { _linearEasingSupported = false; }
        }
        return _linearEasingSupported.Value;
    }

    private bool? _oklabSupported;

    private async ValueTask<bool> OklabSupportedAsync()
    {
        if (_oklabSupported is null)
        {
            try { _oklabSupported = await _interop.SupportsOklabColorAsync(); }
            catch { _oklabSupported = false; }
        }
        return _oklabSupported.Value;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Playing an animation
    //
    // Every start - Animate, a gesture overlay or its release, a drag release, an exit, a programmatic call -
    // comes through PlayAsync, so they all read unseen starting values the same way, hand velocity over the
    // same way, and reach the browser on the same terms. See IMPROVEMENT_PLAN.md, AD-1.
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Starts animating <paramref name="values"/>: folds the browser animations it overlaps back into the state,
    /// reads the starting values the engine has never seen off the computed style, lands zero-length keys at once,
    /// hands what the browser can reproduce exactly to it as a Web Animation, and runs the rest on the frame loop
    /// (or, without one, lands it at once). Returns once the animation has started; <paramref name="tcs"/>
    /// resolves when it is over - <c>true</c> only for a natural finish.
    /// </summary>
    /// <param name="elementId">The element's id.</param>
    /// <param name="state">The element's state (the engine's entry for <paramref name="elementId"/>).</param>
    /// <param name="values">What to animate: engine keys to single values or keyframe sequences.</param>
    /// <param name="transition">How; <c>null</c> for the engine's default tween.</param>
    /// <param name="tcs">Resolved when the animation is over; <c>null</c> when nobody is waiting.</param>
    /// <param name="claim">
    /// The values are the element's own (an <c>Animate</c>, a programmatic call) rather than a gesture's, so
    /// nothing they set goes back to the stylesheet.
    /// </param>
    /// <param name="stylesheetKeys">
    /// Keys going back to the stylesheet: their target is re-read from it now, and once the animation finishes
    /// their inline declaration is removed.
    /// </param>
    /// <param name="isCurrent">
    /// Whether this start still stands - checked after the reads, which a later start may have overtaken.
    /// </param>
    private async ValueTask PlayAsync(
        string elementId, BmotionElementAnimationState state,
        Dictionary<string, object?> values, BmotionTransitionConfig? transition,
        TaskCompletionSource<bool>? tcs, bool claim = true,
        IReadOnlyCollection<string>? stylesheetKeys = null, Func<bool>? isCurrent = null)
    {
        if (claim) state.Claim(values.Keys);

        // The browser animations this one overlaps stop where they are, handing their values and velocities over.
        // When that happened is when this animation starts from (see the hand-off below).
        long interruptedAt = InterruptWaapiOverlaps(elementId, state, values.Keys) ? Environment.TickCount64 : 0;

        // What the engine has never seen is read off the page rather than guessed, and a size that is "auto" now, or
        // is to become it, is measured: it plays from and to pixels, and lands on "auto" itself.
        var unknown = state.UnknownKeys(values.Keys);
        var fromAuto = state.AutoKeys(values.Keys);
        var toAuto = AutoTargets(values);
        List<string>? landsOnAuto = null;
        if (unknown is not null || fromAuto is not null || toAuto is not null || stylesheetKeys is { Count: > 0 })
        {
            // The read writes the measured targets into what it is given: a copy, as the caller's may be stored - a
            // base or gesture-layer target, which must keep its "auto" for the next time it plays.
            values = new Dictionary<string, object?>(values, values.Comparer);
            landsOnAuto = await ReadStylesheetAsync(elementId, state, values, unknown, stylesheetKeys, fromAuto, toAuto);
            if ((isCurrent is not null && !isCurrent()) || !IsLive(elementId, state))
            {
                tcs?.TrySetResult(false);
                return;
            }
            // The read can interleave with another start on the same keys, which may have handed them to the
            // browser since: take them back, so this one is their only owner.
            if (InterruptWaapiOverlaps(elementId, state, values.Keys)) interruptedAt = Environment.TickCount64;
        }

        var (loopValues, instantValues, browserValues) = Partition(state, values, transition);

        // Zero-length keys land now - before any browser animation is built, so one that also writes the
        // transform composes them in. A size that lands on "auto" without playing is simply "auto".
        if (instantValues is not null)
        {
            if (landsOnAuto is not null)
                foreach (var key in landsOnAuto)
                    if (instantValues.ContainsKey(key)) instantValues[key] = "auto";
            state.SetInstant(instantValues);
        }

        WaapiOffload? offload = null;
        if (browserValues is not null)
        {
            offload = await TryBuildBrowserPlanAsync(state, browserValues, transition);
            if (!IsLive(elementId, state))
            {
                tcs?.TrySetResult(false);
                return;
            }
            if (offload is null)
            {
                loopValues ??= new Dictionary<string, object?>();
                foreach (var (key, value) in browserValues) loopValues[key] = value;
                browserValues = null;
            }
        }

        // One path and nothing to hand back: the caller's completion is the path's own. Otherwise each path settles
        // a part, and the caller hears once all of them have (and the stylesheet gets its keys back after).
        bool release = stylesheetKeys is { Count: > 0 };
        bool split = release || (offload is not null && loopValues is not null);
        var parts = split ? new List<Task<bool>>(2) : null;
        TaskCompletionSource<bool>? Part()
        {
            if (!split) return tcs;
            var part = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            parts!.Add(part.Task);
            return part;
        }

        if (offload is not null)
        {
            if (interruptedAt != 0 && _interop.IsInProcess)
            {
                // It takes over from animations frozen at the instant they were interrupted, and starts from the
                // values they had then: playing from that same instant (the bridge noted it when it cancelled them),
                // rather than from whenever building this one let it start, keeps the time spent here - recording a
                // spring, say - from showing as a stall. Without a frame loop the browser starts from the value on
                // screen instead (the implicit first keyframe), so there is no instant to go back to.
                offload.Timing["continues"] = true;
                offload.Plan.StartMs = interruptedAt;
            }
            offload.Plan.Completion = Part();
            RegisterWaapiPlan(elementId, state, offload.Plan);
            foreach (var key in offload.Plan.Keys) state.StartWritten(key);
            _ = SuperviseOffloadAsync(elementId, state, browserValues!, transition, offload, offload.Plan.Completion);
        }

        if (loopValues is not null)
        {
            // Without a frame loop this collapses to an instant change: warned once, so it is diagnosable.
            WarnIfDegradingOnServer(elementId, loopValues, transition);
            state.AnimateTo(loopValues, transition, Part());
        }

        // Noted once whatever plays them is running (starting it forgets an earlier landing on the same keys).
        if (landsOnAuto is not null)
            foreach (var key in landsOnAuto)
                if (instantValues?.ContainsKey(key) != true) state.LandOnAuto(key);

        if (loopValues is not null || instantValues is not null) await EnsureTickingAsync(elementId, state);

        if (!split)
        {
            // Nothing was started at all (every key landed at once): that is a finish.
            if (offload is null && loopValues is null) tcs?.TrySetResult(true);
            return;
        }
        _ = SettleAsync();

        async Task SettleAsync()
        {
            bool finished = true;
            foreach (var part in parts!) finished &= await part;
            if (finished && release && IsLive(elementId, state) && state.ReleaseToStylesheet(stylesheetKeys!))
                KickFlush(elementId, state);
            tcs?.TrySetResult(finished);
        }
    }

    private bool IsLive(string elementId, BmotionElementAnimationState state)
        => _elements.TryGetValue(elementId, out var live) && ReferenceEquals(live, state);

    /// <summary>
    /// Folds every browser animation overlapping <paramref name="keys"/> back into the state - value and velocity
    /// - and cancels it, committing what it shows inline first so nothing flashes before the new owner's first
    /// write. The cancels are not awaited: interop calls reach the browser in order, so whatever this call sends
    /// next lands after them.
    /// </summary>
    private bool InterruptWaapiOverlaps(string elementId, BmotionElementAnimationState state, IReadOnlyCollection<string> keys)
    {
        if (!state.HasWaapiPlans) return false;
        var tokens = state.RealizeWaapiPlans(keys, continueOthers: true);
        if (tokens is null) return false;
        CancelWaapiTokens(elementId, tokens, commit: true);
        // Keys of an interrupted plan this call doesn't take carry on on the frame loop.
        if (_interop.IsInProcess && state.HasActiveAnimations) KickFlush(elementId, state);
        return true;
    }

    /// <summary>
    /// Reads, in one interop call, the computed values of <paramref name="unknown"/> keys (seeded into the state as
    /// borrowed from the stylesheet) and of the <paramref name="fromAuto"/> sizes (where an animation from
    /// <c>"auto"</c> starts), the values the stylesheet gives <paramref name="stylesheetKeys"/> with their inline
    /// declarations lifted (written into <paramref name="values"/> as their targets), and the natural size of the
    /// <paramref name="toAuto"/> sizes (written into <paramref name="values"/> in place of <c>"auto"</c>). Returns the
    /// sizes whose animation, now playing to a measured size, lands on <c>"auto"</c>. A failed read leaves the state
    /// as it was: the animation then starts from the engine's defaults (and a size snaps to <c>"auto"</c>), as it
    /// always did.
    /// </summary>
    private async ValueTask<List<string>?> ReadStylesheetAsync(
        string elementId, BmotionElementAnimationState state, Dictionary<string, object?> values,
        List<string>? unknown, IReadOnlyCollection<string>? stylesheetKeys, List<string>? fromAuto, List<string>? toAuto)
    {
        var current = fromAuto is null ? unknown : [.. unknown ?? [], .. fromAuto];
        BmotionStyleRead? read;
        try
        {
            read = toAuto is null
                ? await _interop.ReadStylesAsync(elementId, current?.ToArray() ?? [], stylesheetKeys?.ToArray() ?? [])
                : await _interop.ReadStylesAsync(elementId, current?.ToArray() ?? [], stylesheetKeys?.ToArray() ?? [], [.. toAuto]);
        }
        catch { read = null; }

        if (read?.Current is { } computed)
        {
            if (unknown is not null)
                foreach (var key in unknown)
                    if (computed.TryGetValue(key, out var value)) state.SeedFromStylesheet(key, value);
            if (fromAuto is not null)
                foreach (var key in fromAuto)
                    if (computed.TryGetValue(key, out var value)) state.SeedMeasured(key, value);
        }

        if (stylesheetKeys is not null)
            foreach (var key in stylesheetKeys)
            {
                if (read?.Stylesheet is not { } sheet || !sheet.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
                    continue; // the captured origin (or nothing) stays the target
                values[key] = key == "opacity" && BmotionCssFormat.TryParse(value, out double number) ? number : value.Trim();
            }

        List<string>? landsOnAuto = null;
        if (toAuto is not null && read?.Auto is { } natural)
            foreach (var key in toAuto)
            {
                if (!natural.TryGetValue(key, out var size) || !BmotionElementAnimationState.TryParseCssDimension(size.Trim(), out _, out _))
                    continue; // not laid out (display: none): nothing to play to
                switch (values[key])
                {
                    case string:
                        values[key] = size.Trim();
                        (landsOnAuto ??= []).Add(key);
                        break;
                    case string[] frames:
                        var measured = Array.ConvertAll(frames, f => BmotionElementAnimationState.IsAuto(f) ? size.Trim() : f);
                        values[key] = measured;
                        if (BmotionElementAnimationState.IsAuto(frames[^1])) (landsOnAuto ??= []).Add(key);
                        break;
                }
            }
        return landsOnAuto;
    }

    /// <summary>The sizes among <paramref name="values"/> that animate to (or through) <c>"auto"</c>.</summary>
    private static List<string>? AutoTargets(Dictionary<string, object?> values)
    {
        List<string>? auto = null;
        foreach (var (key, value) in values)
        {
            if (!BmotionElementAnimationState.IsAutoSizeKey(key)) continue;
            bool isAuto = value switch
            {
                string s => BmotionElementAnimationState.IsAuto(s),
                string[] frames => Array.Exists(frames, BmotionElementAnimationState.IsAuto),
                _ => false,
            };
            if (isAuto) (auto ??= []).Add(key);
        }
        return auto;
    }

    /// <summary>
    /// Splits an animation into the keys only the frame loop can play, the zero-length keys that land at once
    /// (at their resting value), and the keys the browser may play.
    /// </summary>
    private (Dictionary<string, object?>? Loop, Dictionary<string, object?>? Instant, Dictionary<string, object?>? Browser)
        Partition(BmotionElementAnimationState state, Dictionary<string, object?> values, BmotionTransitionConfig? transition)
    {
        var config = transition ?? new BmotionTransitionConfig();
        bool inProcess = _interop.IsInProcess;
        // A per-frame callback has frames to report only on the frame loop (as motion.dev runs an animation with
        // an onUpdate there); a paused or re-timed element plays new animations at its rate there too. Without a
        // frame loop the browser is the only way to animate at all, and plays them at the element's rate.
        bool allOnLoop = HasOnUpdate(config, values.Keys) || (inProcess && (state.OnFrame is not null || state.PlaybackRate != 1));
        // The transform is one CSS property: while a drag writes it, or a frame-loop driver animates a component of
        // it, the browser can't own it.
        bool transformOnLoop = state.IsDragging || (inProcess && state.HasActiveTransformDriver());

        Dictionary<string, object?>? loop = null, instant = null, browser = null;
        foreach (var (key, value) in values)
        {
            if (value is null) continue;
            var perKey = config.Properties?.GetValueOrDefault(key) ?? config;
            if (IsInstant(perKey))
            {
                if (BmotionElementAnimationState.RestingValueOf(value) is { } resting) (instant ??= new())[key] = resting;
                continue;
            }
            // SVG geometry is an attribute, which Web Animations can't animate.
            bool onLoop = allOnLoop || key is "d" or "points"
                || (transformOnLoop && BmotionTransformComposer.IsTransformProp(key));
            if (onLoop) (loop ??= new())[key] = value;
            else (browser ??= new())[key] = value;
        }
        return (loop, instant, browser);

        static bool IsInstant(BmotionTransitionConfig c)
            => c is { Type: BmotionTransitionType.Tween, Duration: <= 0, Delay: <= 0, Repeat: 0, RepeatInfinite: false, OnUpdate: null };

        static bool HasOnUpdate(BmotionTransitionConfig c, IEnumerable<string> keys)
        {
            if (c.OnUpdate is not null) return true;
            if (c.Properties is null) return false;
            foreach (var key in keys)
                if (c.Properties.TryGetValue(key, out var perKey) && perKey.OnUpdate is not null) return true;
            return false;
        }
    }

    /// <summary>
    /// Builds the Web Animation that plays <paramref name="values"/> exactly as the frame loop would: the compact
    /// timeline shape when one exact curve covers every key, otherwise a recording. Null when neither applies; the
    /// caller then runs the animation on the frame loop.
    /// </summary>
    private async ValueTask<WaapiOffload?> TryBuildBrowserPlanAsync(
        BmotionElementAnimationState state, Dictionary<string, object?> values, BmotionTransitionConfig? transition)
    {
        var config = transition ?? new BmotionTransitionConfig();
        // Per-property transitions that all resolve to one (a reduced-motion fade keeps only opacity, with its
        // normal transition) are just that one transition.
        config = SharedTransition(values, config) ?? config;
        return await TryBuildTimelineAsync(state, values, config) ?? TryBuildSampled(state, values, config);
    }

    /// <summary>
    /// The one transition every key of <paramref name="values"/> plays with - its own per-property override, or the
    /// base - or <c>null</c> when they differ.
    /// </summary>
    private static BmotionTransitionConfig? SharedTransition(Dictionary<string, object?> values, BmotionTransitionConfig config)
    {
        if (config.Properties is not { Count: > 0 } perKey) return config;
        BmotionTransitionConfig? shared = null;
        foreach (var (key, value) in values)
        {
            if (value is null) continue;
            var own = perKey.GetValueOrDefault(key) ?? config;
            if (shared is null) shared = own;
            else if (!ReferenceEquals(shared, own)) return null;
        }
        return shared;
    }

    // The keys the engine keeps as numbers: transform components, opacity, and the path-drawing trio.
    private static bool IsNumericKey(string key)
        => BmotionTransformComposer.IsTransformProp(key) || key is "opacity" or "pathLength" or "pathOffset" or "pathSpacing";

    /// <summary>
    /// The timeline shape: two keyframes per key and the curve as the timing easing - so a tween or a spring at
    /// rest costs two keyframes whatever its length. It applies when every key shares one tween (or zero-velocity
    /// spring) that loops, if it repeats at all, and every value is one the browser interpolates exactly as the
    /// engine does.
    /// </summary>
    private async ValueTask<WaapiOffload?> TryBuildTimelineAsync(
        BmotionElementAnimationState state, Dictionary<string, object?> values, BmotionTransitionConfig config)
    {
        if (config.Properties is { Count: > 0 } perKey && values.Keys.Any(perKey.ContainsKey)) return null;
        if (config.Eases is not null) return null;
        if (config.Path is not null && BmotionArcTargets.Applies(values)) return null;
        // Repeat delays, reversing repeats, uneven keyframe times, inertia, a spring with initial velocity, a
        // zero-length tween - shared with the prerendered CSS animations.
        if (!BmotionBrowserTiming.CanPlay(config)) return null;
        // A mirrored repeat plays its way back with the same curve (B→A eased forwards); the browser's alternate
        // direction would play the way out backwards instead - a different curve unless it is symmetric.
        if (config.RepeatType == BmRepeatType.Mirror && (config.IsInfiniteRepeat || config.Repeat > 0)) return null;

        var numeric = new Dictionary<string, (double From, double To)>(StringComparer.OrdinalIgnoreCase);
        var strings = new Dictionary<string, (string FromCss, string ToCss, BmotionElementAnimationState.WaapiPlan.StringTrack Track)>(StringComparer.Ordinal);
        bool fromIsCurrent = true;
        foreach (var (key, raw) in values)
        {
            if (raw is null) continue;
            if (IsNumericKey(key))
            {
                if (!TryNumericEndpoints(state, key, raw, out double from, out double to, out bool current)) return null;
                // A spring that inherits velocity plays a curve of its own on every key.
                if (config.Type == BmotionTransitionType.Spring && state.HandoffVelocity(key) != 0) return null;
                numeric[key] = (from, to);
                fromIsCurrent &= current;
            }
            else if (raw is string to)
            {
                if (await TryStringEndpointsAsync(state, key, to, config.ColorSpace) is not { } track) return null;
                strings[key] = track;
            }
            else return null;
        }
        if (numeric.Count == 0 && strings.Count == 0) return null;

        // transform is a single CSS property: the browser can't own it while a frame-loop driver animates a
        // component of it (Partition keeps such animations on the loop; this guards the race).
        bool touchesTransform = numeric.Keys.Any(BmotionTransformComposer.IsTransformProp);
        if (touchesTransform && (state.HasActiveTransformDriver() || state.IsDragging)) return null;

        bool linearSupported = await LinearEasingSupportedAsync();
        if (config.Type == BmotionTransitionType.Spring && !linearSupported) return null;
        if (BmotionBrowserTiming.SampleProgress(config) is not { } sampled) return null;
        var (samples, durationMs) = sampled;
        // The frame loop holds a colour between its two ends while a spring overshoots them (CreateColorDriver); the
        // browser would carry it along the curve, past the target. Colours alone play the curve held within its ends.
        // Beside values that do overshoot, one timing easing can't serve both, and the recording plays what the frame
        // loop draws for each.
        if (config.Type == BmotionTransitionType.Spring && BmotionBrowserTiming.Overshoots(samples)
            && strings.Keys.Any(BmotionElementAnimationState.IsColorProp))
        {
            if (numeric.Count > 0 || !strings.Keys.All(BmotionElementAnimationState.IsColorProp)) return null;
            samples = BmotionBrowserTiming.HeldWithinEnds(samples);
        }
        // Without linear() support, a curve CSS can't express (elastic/bounce) runs exactly on the frame loop
        // instead of shipping a wrong easing.
        if (BmotionBrowserTiming.ChooseEasing(config, samples, linearSupported, preferKeyword: false) is not { } easing) return null;

        var fromFrame = ComposeFrame(state, numeric, k => numeric[k].From, touchesTransform);
        var toFrame = ComposeFrame(state, numeric, k => numeric[k].To, touchesTransform);
        foreach (var (key, (fromCss, toCss, _)) in strings)
        {
            fromFrame[key] = fromCss;
            toFrame[key] = toCss;
        }

        bool infinite = config.IsInfiniteRepeat;
        double delayMs = config.Delay * 1000;
        var plan = new BmotionElementAnimationState.WaapiPlan
        {
            Token = ++_waapiTokenSeq,
            StartMs = Environment.TickCount64,
            DelayMs = delayMs,
            DurationMs = durationMs,
            Progress = samples,
            Ease = config.Type != BmotionTransitionType.Spring && config.StepCount > 0 ? BmEaseFunctions.Get(config) : null,
            Values = numeric,
            Strings = strings.Count == 0 ? null : strings.ToDictionary(kv => kv.Key, kv => kv.Value.Track, StringComparer.Ordinal),
            Iterations = infinite ? -1 : config.Repeat,
            Mirror = false,
        };

        var timing = new Dictionary<string, object?>
        {
            ["duration"] = durationMs,
            ["delay"] = delayMs,
            ["easing"] = easing,
            ["iterations"] = infinite ? -1 : config.Repeat + 1,
            ["direction"] = "normal",
        };
        ApplyRate(state, plan, timing);

        // Without synchronous interop the browser runs a network hop behind the C# clock, so the engine's idea of
        // "now" is ahead of what is on screen: starting from the value actually showing (the implicit first
        // keyframe) keeps that hop from ever showing as a jump. With the engine's own starting values only - an
        // explicit first keyframe ([0, 100]) is the author's and is kept - and not where the page shows "auto",
        // which doesn't interpolate: those keys alone get their measured start.
        Dictionary<string, object>[] keyframes = _interop.IsInProcess || !fromIsCurrent ? [fromFrame, toFrame]
            : ShownAutoFrame(state, fromFrame) is { } autoFrame ? [autoFrame, toFrame] : [toFrame];
        return new WaapiOffload(plan, keyframes, timing);
    }

    // The part of a first keyframe the page can't start from on its own: the sizes it shows as "auto".
    private static Dictionary<string, object>? ShownAutoFrame(BmotionElementAnimationState state, IReadOnlyDictionary<string, object> first)
    {
        Dictionary<string, object>? frame = null;
        foreach (var (property, value) in first)
            if (state.ShowsAuto(property)) (frame ??= new Dictionary<string, object>(StringComparer.Ordinal))[property] = value;
        if (frame is not null && first.TryGetValue("offset", out var offset)) frame["offset"] = offset;
        return frame;
    }

    /// <summary>
    /// A numeric key's two endpoints: from the element's current value to a single target, or a two-frame
    /// sequence (whose <see cref="Bm.Current"/> wildcard is the current value). <paramref name="fromIsCurrent"/>
    /// tells whether the start is the element's current value rather than one the author gave.
    /// </summary>
    private static bool TryNumericEndpoints(
        BmotionElementAnimationState state, string key, object raw, out double from, out double to, out bool fromIsCurrent)
    {
        double current = state.CurrentValue(key) as double? ?? (key is "opacity" or "pathLength" ? 1.0 : 0.0);
        from = current;
        to = 0;
        fromIsCurrent = true;
        switch (raw)
        {
            case double d: to = d; break;
            case int i: to = i; break;
            case float f: to = f; break;
            case long l: to = l; break;
            case double[] { Length: 1 } one: to = one[0]; break;
            case double[] { Length: 2 } two:
                fromIsCurrent = double.IsNaN(two[0]);
                from = fromIsCurrent ? current : two[0];
                to = two[1];
                break;
            default: return false;
        }
        return double.IsFinite(from) && double.IsFinite(to);
    }

    /// <summary>
    /// A string key's two endpoints when the browser interpolates them exactly as the engine does: a colour
    /// (re-encoded as <c>rgba()</c>, or as <c>oklab()</c> for an OKLab transition, which the browser then mixes in
    /// that space), a length in the same unit at both ends, or a complex value of the same shape on a property the
    /// browser interpolates (filters, shadows, clip paths, ...). Null otherwise.
    /// </summary>
    private async ValueTask<(string FromCss, string ToCss, BmotionElementAnimationState.WaapiPlan.StringTrack Track)?> TryStringEndpointsAsync(
        BmotionElementAnimationState state, string key, string to, BmColorSpace space)
    {
        if (state.CurrentValue(key) is not string from) return null;
        // An unregistered custom property interpolates discretely: only a recording reproduces the engine there.
        if (key.StartsWith("--", StringComparison.Ordinal)) return null;

        if (BmotionElementAnimationState.IsColorProp(key))
        {
            if (BmotionColorInterpolator.Parse(from) is not { } a || BmotionColorInterpolator.Parse(to) is not { } b) return null;
            string fromCss, toCss;
            if (space == BmColorSpace.Oklab)
            {
                if (!await OklabSupportedAsync()) return null;
                fromCss = BmotionColorInterpolator.ToOklabCss(a);
                toCss = BmotionColorInterpolator.ToOklabCss(b);
            }
            else
            {
                fromCss = BmotionColorInterpolator.ToRgbaCss(a);
                toCss = BmotionColorInterpolator.ToRgbaCss(b);
            }
            var colour = new BmotionElementAnimationState.WaapiPlan.StringTrack(p => BmotionColorInterpolator.Lerp(a, b, p, space), to)
            {
                RewriteFinal = true,
            };
            return (fromCss, toCss, colour);
        }

        if (BmotionElementAnimationState.TryParseCssDimension(from, out double f, out string fromUnit) &&
            BmotionElementAnimationState.TryParseCssDimension(to, out double t, out string toUnit) &&
            string.Equals(fromUnit, toUnit, StringComparison.OrdinalIgnoreCase))
        {
            // commitStyles writes the computed value (rem/em become px), so the authored one is written back after.
            var length = new BmotionElementAnimationState.WaapiPlan.StringTrack(p => BmotionCssFormat.Num(f + (t - f) * p) + toUnit, to)
            {
                RewriteFinal = true,
            };
            return (from, to, length);
        }

        // Different units: the browser interpolates the two ends as calc() of both, as the mirror does.
        if (IsInterpolableProperty(key) && BmotionElementAnimationState.MixedLengthMix(from, to) is { } lengths)
            return (from, to, new BmotionElementAnimationState.WaapiPlan.StringTrack(lengths, to) { RewriteFinal = true });

        // A "none" end plays as the other end's neutral values, sent explicitly so the browser and the mirror agree;
        // the authored "none" is written once it is over.
        var (mixFrom, mixTo) = BmotionStringMixer.MixEnds(key, from, to);
        if (IsInterpolableProperty(key) && BmotionStringMixer.TryCreateMix(mixFrom, mixTo, key) is { } mix)
        {
            var mixed = new BmotionElementAnimationState.WaapiPlan.StringTrack(p => p >= 1 ? to : mix(p), to) { RewriteFinal = true };
            return (mixFrom, mixTo, mixed);
        }
        return null;
    }

    // Properties whose value lists the browser interpolates component by component, the way the C# string mixer does.
    // Anything else (gradients, keywords, custom properties) is recorded instead.
    private static readonly HashSet<string> _interpolableProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "filter", "backdropFilter", "boxShadow", "textShadow", "clipPath", "transformOrigin", "perspectiveOrigin",
        "borderRadius", "borderTopLeftRadius", "borderTopRightRadius", "borderBottomLeftRadius", "borderBottomRightRadius",
        "backgroundPosition", "backgroundSize", "objectPosition", "maskPosition", "maskSize",
        "offsetDistance", "offsetRotate", "offsetAnchor", "offsetPosition", "strokeDasharray", "strokeDashoffset", "strokeWidth",
        "width", "height", "minWidth", "maxWidth", "minHeight", "maxHeight", "top", "left", "right", "bottom", "inset",
        "margin", "marginTop", "marginRight", "marginBottom", "marginLeft",
        "padding", "paddingTop", "paddingRight", "paddingBottom", "paddingLeft",
        "gap", "rowGap", "columnGap", "fontSize", "letterSpacing", "wordSpacing", "lineHeight", "textIndent",
        "borderWidth", "outlineWidth", "outlineOffset", "flexBasis", "fontWeight", "translate", "rotate", "scale",
    };

    private static bool IsInterpolableProperty(string key) => _interpolableProperties.Contains(key);

    /// <summary>
    /// One timeline keyframe: the full transform (every component the element has, the animated ones at
    /// <paramref name="valueOf"/>, so untouched components persist and every frame lists the same functions), the
    /// opacity, and the stroke dash pair path drawing composes.
    /// </summary>
    private static Dictionary<string, object> ComposeFrame(
        BmotionElementAnimationState state, Dictionary<string, (double From, double To)> numeric,
        Func<string, double> valueOf, bool touchesTransform)
    {
        var frame = new Dictionary<string, object>(StringComparer.Ordinal);
        if (touchesTransform)
        {
            var transforms = new Dictionary<string, double>(state.Transforms, StringComparer.OrdinalIgnoreCase);
            foreach (var key in numeric.Keys)
                if (BmotionTransformComposer.IsTransformProp(key)) transforms[key] = valueOf(key);
            var composed = state.ComposeTransform(transforms);
            frame["transform"] = string.IsNullOrEmpty(composed) ? "none" : composed;
        }
        if (numeric.ContainsKey("opacity")) frame["opacity"] = BmotionCssFormat.Num(valueOf("opacity"));
        if (numeric.ContainsKey("pathLength") || numeric.ContainsKey("pathOffset") || numeric.ContainsKey("pathSpacing"))
        {
            double Path(string key, double fallback)
                => numeric.ContainsKey(key) ? valueOf(key) : state.NumericValues.GetValueOrDefault(key, fallback);
            double len = Math.Clamp(Path("pathLength", 1), 0, 1);
            frame["strokeDasharray"] = BmotionCssFormat.Num(len) + " " + BmotionCssFormat.Num(Path("pathSpacing", 1));
            frame["strokeDashoffset"] = BmotionCssFormat.Num(1 - len - Path("pathOffset", 0));
        }
        return frame;
    }

    /// <summary>
    /// The sampled shape: the animation recorded frame by frame on a fork of the element's state (see
    /// <see cref="BmotionFrameRecorder"/>) and played linearly - whatever the frame loop can play, played exactly as
    /// it would. A uniform delay plays as the animation's delay rather than as recorded frames, and an endless loop
    /// (or mirror) records one cycle that repeats. Null when it can't be recorded: an endlessly reversing
    /// repeat, per-property transitions that repeat forever, or more than <see cref="BmotionFrameRecorder.MaxMs"/>.
    /// </summary>
    private WaapiOffload? TryBuildSampled(
        BmotionElementAnimationState state, Dictionary<string, object?> values, BmotionTransitionConfig config)
    {
        bool perKey = config.Properties is { Count: > 0 } properties && values.Keys.Any(properties.ContainsKey);
        if (perKey && values.Keys.Any(k => config.Properties!.TryGetValue(k, out var c) && c.IsInfiniteRepeat)) return null;

        var record = config.Clone();
        double delayMs = 0, holdMs = 0;
        int iterations = 1;
        if (!perKey)
        {
            delayMs = config.Delay * 1000; // a negative delay starts the recording that far in, as it does a timeline
            record.Delay = 0;
        }
        if (config.IsInfiniteRepeat)
        {
            if (perKey || config.RepeatType == BmRepeatType.Reverse) return null;
            // One cycle - a mirror's is the way out and the way back - followed by its repeat delay, on a loop.
            record.RepeatInfinite = false;
            record.Repeat = config.RepeatType == BmRepeatType.Mirror ? 1 : 0;
            holdMs = Math.Max(0, config.RepeatDelay * 1000);
            iterations = -1;
        }

        BmotionFrameRecorder.Recording? recording;
        try { recording = BmotionFrameRecorder.Record(state, values, record, holdMs); }
        catch { return null; } // a value the drivers reject (non-finite config) - the frame loop reports it
        if (recording is null || recording.FrameCount < 2) return null;

        int n = recording.FrameCount;
        var numeric = new Dictionary<string, (double From, double To)>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, frames) in recording.Numeric) numeric[key] = (frames[0], frames[^1]);
        var strings = new Dictionary<string, BmotionElementAnimationState.WaapiPlan.StringTrack>(StringComparer.Ordinal);
        foreach (var (key, frames) in recording.Strings)
        {
            var f = frames;
            strings[key] = new BmotionElementAnimationState.WaapiPlan.StringTrack(
                p => f[Math.Clamp((int)Math.Round(p * (f.Length - 1)), 0, f.Length - 1)], f[^1])
            {
                RewriteFinal = true,
            };
        }

        var plan = new BmotionElementAnimationState.WaapiPlan
        {
            Token = ++_waapiTokenSeq,
            StartMs = Environment.TickCount64,
            DelayMs = delayMs,
            DurationMs = Math.Max(recording.DurationMs, 1),
            Progress = [0, 1],
            Values = numeric,
            Frames = recording.Numeric,
            Strings = strings.Count == 0 ? null : strings,
            // A plan counts repeats; the timing counts plays.
            Iterations = iterations < 0 ? -1 : 0,
            Mirror = false,
        };

        // The browser interpolates linearly between keyframes, so only the frames it couldn't reproduce that way need
        // sending (each with its offset): a settling spring is mostly gentle curve, and this is what keeps a
        // recording's interop payload - and the main-thread time to marshal it - small. The mirror keeps every frame.
        var kept = BmotionFrameRecorder.Simplify(recording);
        var keyframes = new List<Dictionary<string, object>>(kept.Count);
        // Without synchronous interop the first frame is the browser's current value (see TryBuildTimelineAsync).
        foreach (int i in kept)
        {
            var frame = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (var (property, value) in recording.CssAt(i))
                // An identity transform composes to "", which is no keyframe value; "none" is.
                frame[property] = property == "transform" && value.Length == 0 ? "none" : value;
            frame["offset"] = (double)i / (n - 1);
            if (i == 0 && !_interop.IsInProcess)
            {
                // Only what the page can't start from itself (a size it shows as "auto") is sent.
                if (ShownAutoFrame(state, frame) is { } autoFrame) keyframes.Add(autoFrame);
                continue;
            }
            keyframes.Add(frame);
        }

        var timing = new Dictionary<string, object?>
        {
            ["duration"] = plan.DurationMs,
            ["delay"] = delayMs,
            ["easing"] = "linear",
            ["iterations"] = iterations,
            ["direction"] = "normal",
        };
        ApplyRate(state, plan, timing);
        return new WaapiOffload(plan, keyframes.ToArray(), timing);
    }

    // Only reachable without a frame loop (see Partition): the browser plays it at the element's rate.
    private static void ApplyRate(
        BmotionElementAnimationState state, BmotionElementAnimationState.WaapiPlan plan, Dictionary<string, object?> timing)
    {
        if (state.PlaybackRate == 1) return;
        timing["playbackRate"] = state.PlaybackRate;
        plan.SetRate(plan.StartMs, state.PlaybackRate);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Scroll-driven timelines
    // Same pre-sampled keyframes as the compositor offload, but progressed by scroll position
    // instead of by time. The browser owns the whole animation from there: on a native
    // ScrollTimeline/ViewTimeline it never touches the main thread at all, and on the bridge's
    // fallback a passive scroll listener scrubs it - either way with no .NET interop per frame.
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Binds <paramref name="values"/> to <paramref name="timeline"/> for an element. Returns the
    /// animation's token (pass it to <see cref="DetachScrollTimelineAsync"/>), or <c>0</c> when the
    /// target isn't expressible as browser keyframes - only transform components and opacity are,
    /// so anything else has to stay on the time-based engine.
    /// </summary>
    internal async ValueTask<int> AttachScrollTimelineAsync(
        string elementId, Dictionary<string, object?> values, BmScrollTimeline timeline)
    {
        if (!_elements.TryGetValue(elementId, out var state)) return 0;
        if (!TryCollectFrameArrays(state, values, out var frameArrays, out var targets,
                out bool touchesTransform, out int keyframeLen))
            return 0;

        // The browser is about to own these properties outright, so hand it an element nothing
        // else is writing: fold back and cancel any compositor plan, and stop any rAF driver, on
        // the same keys first.
        await InterruptWaapiOverlapsAsync(elementId, state, values.Keys, continueOthers: false);
        state.Cancel(values.Keys.ToArray());

        int frameCount = Math.Max(2, keyframeLen);
        if (frameCount > 2)
            foreach (var key in frameArrays.Keys.ToArray())
                frameArrays[key] = ResampleUniform(frameArrays[key], frameCount);

        var frameStyles = BuildFrameStyles(state, frameArrays, targets, touchesTransform, frameCount);

        int token = ++_waapiTokenSeq;
        bool? native;
        try
        {
            native = await _interop.PlayScrollTimelineAsync(elementId, token, frameStyles, timeline.ToJsObject());
        }
        catch
        {
            return 0; // bridge unavailable (e.g. teardown) - the element simply doesn't animate
        }
        if (native is null) return 0;

        if (native == false && timeline.Smoothing is null) // a smoothed one is always scrubbed
            _logger?.LogDebug(
                "Bit.Bmotion: element '{ElementId}' is scroll-driven through the scroll-scrub fallback; "
                + "this browser has no native ScrollTimeline/ViewTimeline.", elementId);

        return token;
    }

    /// <summary>Releases a scroll-driven animation started by <see cref="AttachScrollTimelineAsync"/>.</summary>
    internal async ValueTask DetachScrollTimelineAsync(string elementId, int token)
    {
        if (token == 0) return;
        try { await _interop.CancelScrollTimelineAsync(elementId, token); }
        catch { /* teardown races the cancel; the element is going away anyway */ }
    }

    /// <summary>
    /// Collects each animated property's keyframes as a numeric array, resolving the "from" end
    /// from the element's live state. Returns <c>false</c> when anything about the target makes it
    /// ineligible for the browser (a non-transform/opacity property, a string keyframe, a
    /// non-finite value, or keyframe arrays of differing lengths - which would break the single
    /// aligned grid the WAAPI keyframes and the C# interruption mirror share).
    /// </summary>
    private static bool TryCollectFrameArrays(
        BmotionElementAnimationState state,
        Dictionary<string, object?> values,
        out Dictionary<string, double[]> frameArrays,
        out Dictionary<string, (double From, double To)> targets,
        out bool touchesTransform,
        out int keyframeLen)
    {
        targets = new Dictionary<string, (double From, double To)>(StringComparer.OrdinalIgnoreCase);
        frameArrays = new Dictionary<string, double[]>(StringComparer.OrdinalIgnoreCase);
        touchesTransform = false;
        keyframeLen = 0;

        foreach (var (key, raw) in values)
        {
            if (raw == null) continue;
            bool isTransform = BmotionTransformComposer.IsTransformProp(key);
            if (!isTransform && !string.Equals(key, "opacity", StringComparison.OrdinalIgnoreCase)) return false;

            double from = isTransform
                ? state.Transforms.GetValueOrDefault(key,
                    key is "scale" or "scaleX" or "scaleY" ? 1.0 : 0.0)
                : state.NumericValues.GetValueOrDefault(key, 1.0); // opacity defaults to 1

            double[] frames;
            switch (raw)
            {
                case double d: frames = [from, d]; break;
                case int i: frames = [from, i]; break;
                case float f: frames = [from, f]; break;
                case long l: frames = [from, l]; break;
                case double[] { Length: >= 2 } arr:
                    frames = (double[])arr.Clone();
                    // A leading Bm.Current (NaN) wildcard resolves to the element's current value.
                    if (double.IsNaN(frames[0])) frames[0] = from;
                    foreach (var v in frames) if (!double.IsFinite(v)) return false;
                    if (keyframeLen == 0) keyframeLen = frames.Length;
                    else if (keyframeLen != frames.Length) return false; // mixed keyframe lengths
                    break;
                case double[] { Length: 1 } one: frames = [from, one[0]]; break;
                default: return false; // string keyframes stay on the rAF path
            }
            if (!double.IsFinite(frames[^1])) return false;
            targets[key] = (frames[0], frames[^1]);
            frameArrays[key] = frames;
            touchesTransform |= isTransform;
        }
        return targets.Count > 0;
    }

    /// <summary>
    /// Composes the browser-side keyframes. Each frame is built from the element's FULL transform
    /// state so untouched components persist, and every frame lists the same transform functions in
    /// the same order - which is what lets the browser interpolate them piecewise rather than
    /// falling back to a discrete swap.
    /// </summary>
    private static Dictionary<string, object>[] BuildFrameStyles(
        BmotionElementAnimationState state,
        Dictionary<string, double[]> frameArrays,
        Dictionary<string, (double From, double To)> targets,
        bool touchesTransform,
        int frameCount)
    {
        var frameStyles = new Dictionary<string, object>[frameCount];
        var opacityKey = targets.Keys.FirstOrDefault(k => k.Equals("opacity", StringComparison.OrdinalIgnoreCase));
        for (int j = 0; j < frameCount; j++)
        {
            var frame = new Dictionary<string, object>();
            if (touchesTransform)
            {
                var tj = new Dictionary<string, double>(state.Transforms, StringComparer.OrdinalIgnoreCase);
                foreach (var (key, frames) in frameArrays)
                    if (BmotionTransformComposer.IsTransformProp(key)) tj[key] = frames[j];
                var str = state.ComposeTransform(tj);
                frame["transform"] = string.IsNullOrEmpty(str) ? "none" : str;
            }
            if (opacityKey != null)
                frame["opacity"] = BmotionCssFormat.Num(frameArrays[opacityKey][j]);
            frameStyles[j] = frame;
        }
        return frameStyles;
    }

    // Resamples a keyframe array onto <paramref name="n"/> uniformly-spaced points via linear
    // interpolation. Identity for arrays already at n uniform frames; expands a 2-frame [from,to]
    // to n linear points. Keeps every animated key on one aligned grid for the WAAPI keyframes.
    private static double[] ResampleUniform(double[] frames, int n)
    {
        if (frames.Length == n) return frames;
        var result = new double[n];
        for (int j = 0; j < n; j++)
            result[j] = BmotionElementAnimationState.WaapiPlan.KeyframeLerp(frames, (double)j / (n - 1));
        return result;
    }

    /// <summary>
    /// Registers a built offload's plan for an element. The AnimateTo* entry points interrupt
    /// overlapping plans up front, but they then await (offload build, easing probe) before
    /// registering - so two concurrent callers can both pass that interrupt pass. Sweep and
    /// cancel any plan that appeared in the meantime synchronously with the registration, so
    /// two compositor animations never compete for the same properties.
    /// </summary>
    private void RegisterWaapiPlan(
        string elementId, BmotionElementAnimationState state, BmotionElementAnimationState.WaapiPlan plan)
    {
        var stale = state.RealizeWaapiPlans(plan.Values.Keys);
        if (stale != null) CancelWaapiTokens(elementId, stale, commit: false);
        state.AddWaapiPlan(plan);
    }

    /// <summary>
    /// Starts compositor playback for an already-registered plan and settles state / the
    /// completion source when it finishes, is interrupted, or fails to start (falling back to
    /// the rAF / instant path in that last case).
    /// </summary>
    private async Task SuperviseOffloadAsync(
        string elementId,
        BmotionElementAnimationState state,
        Dictionary<string, object?> values,
        BmotionTransitionConfig? transition,
        WaapiOffload offload,
        TaskCompletionSource<bool>? tcs)
    {
        bool finished;
        try { finished = await _interop.PlayWaapiAnimationAsync(elementId, offload.Plan.Token, offload.Keyframes, offload.Timing); }
        catch { finished = false; }

        // Interrupted for some of its keys, the rest carry on on the frame loop - which now owns
        // the completion (see BmotionElementAnimationState.ContinuePlan).
        if (!finished && offload.Plan.HandedOff) return;

        if (finished)
        {
            // The browser committed what it interpolated; an authored final value it re-encoded (an oklch()
            // colour, a rem length) is written back over it, and a size played to its natural size becomes "auto".
            if (state.TryCompleteWaapiPlan(offload.Plan.Token, out bool rewrite) && rewrite)
                KickFlush(elementId, state);
            tcs?.TrySetResult(true);
            return;
        }

        // Not finished: either an interruption already realized+removed the plan (a newer owner
        // has the element), or playback never started (element missing / easing rejected).
        const double neverStartedWithinMs = 50;
        bool wasStillRegistered = state.TryRealizeWaapiPlan(offload.Plan.Token, out double elapsedMs, neverStartedWithinMs);
        if (wasStillRegistered && elapsedMs < neverStartedWithinMs)
        {
            // Playback never started - replay through the regular path so the animation isn't lost.
            try
            {
                state.AnimateTo(values, transition, tcs);
                await EnsureTickingAsync(elementId, state);
            }
            catch
            {
                tcs?.TrySetResult(false);
            }
            return;
        }

        tcs?.TrySetResult(false);
    }

    /// <summary>Fire-and-forget cancellation of compositor animations (used from sync entry points).</summary>
    private void CancelWaapiTokens(string elementId, List<int> tokens, bool commit)
    {
        _ = CancelAsync();

        async Task CancelAsync()
        {
            foreach (var token in tokens)
            {
                try { await _interop.CancelWaapiAnimationAsync(elementId, token, commit); }
                catch { /* best-effort */ }
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Detached value animations (drive BmValue instances - no DOM element)
    // ═══════════════════════════════════════════════════════════════════════════

    private sealed record DetachedAnim(IBmotionAnimationDriver Driver, TaskCompletionSource<bool>? Tcs);

    // Keyed by an owner object (typically the BmValue being driven) so a new animation on
    // the same value supersedes the previous one, mirroring per-property element animations.
    private readonly Dictionary<object, DetachedAnim> _detachedAnims = new();

    /// <summary>
    /// Starts a driver that isn't attached to a DOM element; <paramref name="apply"/> receives
    /// each interpolated value (typically forwarding into a <see cref="BmValue{T}"/>).
    /// Supersedes any detached animation already running for <paramref name="key"/>.
    /// </summary>
    internal void StartDetached(
        object key, double from, double to, BmotionTransitionConfig config,
        Action<double> apply, TaskCompletionSource<bool>? tcs = null)
    {
        CancelDetached(key);
        IBmotionAnimationDriver driver = config.Type switch
        {
            BmotionTransitionType.Spring => new BmotionSpringDriver(from, to, config, apply),
            BmotionTransitionType.Inertia => new BmotionInertiaDriver(from, config, apply),
            _ => new BmotionTweenDriver(from, to, config, apply),
        };
        _detachedAnims[key] = new DetachedAnim(driver, tcs);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Timeline schedulers (BmSequence segment starts)
    // ═══════════════════════════════════════════════════════════════════════════

    // Sequences schedule their segment starts here rather than on wall-clock timers, so a
    // timeline's playback rate governs the gaps between segments as well as the segments
    // themselves. Ticked at the top of every frame - see ComputeFrame.
    private readonly List<BmotionSequenceScheduler> _schedulers = new();

    /// <summary>Registers a timeline scheduler to be advanced by the frame loop.</summary>
    internal void AddScheduler(BmotionSequenceScheduler scheduler) => _schedulers.Add(scheduler);

    /// <summary>Stops and drops a timeline scheduler (a stopped or completed sequence).</summary>
    internal void RemoveScheduler(BmotionSequenceScheduler scheduler)
    {
        scheduler.Cancel();
        _schedulers.Remove(scheduler);
    }

    /// <summary>
    /// Moves the target of the detached spring running for <paramref name="key"/> to <paramref name="to"/>, keeping its
    /// position, velocity and clock (<see cref="BmotionSpringDriver.TryRetarget"/>). False when no spring is running for
    /// it - none was started, it has settled, or it is a tween or repeats - and the caller starts a new one instead.
    /// </summary>
    internal bool TryRetargetDetached(object key, double to)
        => _detachedAnims.TryGetValue(key, out var anim) && anim.Driver is BmotionSpringDriver spring && spring.TryRetarget(to);

    /// <summary>Cancels the detached animation for <paramref name="key"/>, if any (resolves its task with false).</summary>
    internal void CancelDetached(object key)
    {
        if (_detachedAnims.Remove(key, out var anim))
        {
            anim.Driver.Cancel();
            anim.Tcs?.TrySetResult(false);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // rAF loop - ComputeFrame is called synchronously from JS each tick
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Called synchronously by the JS rAF ticker every ~16 ms (Blazor WASM).
    /// Returns a dictionary: elementId → { cssPropertyName → cssValue }.
    /// Returns <c>null</c> when there are no style changes this frame. (The loop keeps running
    /// until the engine explicitly calls <c>stopRafLoop</c> once no element has active work.)
    /// </summary>
    [JSInvokable]
    public Dictionary<string, Dictionary<string, string>>? ComputeFrame(double timestamp)
    {
        // Clear the reused outer buffer so entries from the previous frame don't leak through.
        _frameResult.Clear();
        Dictionary<string, Dictionary<string, string>>? result = null;
        bool anyActive = false;
        List<string>? faulted = null;

        // Timeline schedulers tick first: a segment falling due this frame should start animating
        // in this same frame rather than lagging one behind.
        if (_schedulers.Count > 0)
        {
            // Snapshot: a fired segment can register or drop schedulers (a nested sequence, or a
            // Stop() from its own completion) while we're iterating.
            foreach (var scheduler in _schedulers.ToArray())
            {
                if (scheduler.Tick(timestamp)) _schedulers.Remove(scheduler);
            }
            if (_schedulers.Count > 0) anyActive = true;
        }

        // Detached value animations tick BEFORE the elements: their apply callbacks typically set
        // BmValues whose subscribers mark element properties dirty, and ticking them first
        // lets those changes flush to the DOM in this same frame instead of lagging one behind.
        if (_detachedAnims.Count > 0)
        {
            foreach (var (key, anim) in _detachedAnims.ToArray())
            {
                // Skip if a re-entrant apply callback replaced or removed this entry already.
                if (!_detachedAnims.TryGetValue(key, out var current) || !ReferenceEquals(current, anim)) continue;
                bool done;
                try
                {
                    done = anim.Driver.Tick(timestamp);
                }
                catch
                {
                    // A faulted detached driver must not take down the rAF tick; drop it.
                    _detachedAnims.Remove(key);
                    anim.Tcs?.TrySetResult(false);
                    continue;
                }
                if (done)
                {
                    _detachedAnims.Remove(key);
                    anim.Tcs?.TrySetResult(true);
                }
            }
            if (_detachedAnims.Count > 0) anyActive = true;
        }

        // A snapshot, not the live map: a state's OnFrame runs user code inside this loop, and a
        // re-render it triggers renders synchronously on WebAssembly - mounting or disposing
        // elements, i.e. adding to or removing from _elements mid-enumeration.
        _tickBuffer.Clear();
        _tickBuffer.AddRange(_elements);
        foreach (var (id, state) in _tickBuffer)
        {
            if (!_elements.TryGetValue(id, out var live) || !ReferenceEquals(live, state)) continue;
            try
            {
                var updates = state.Tick(timestamp);
                if (updates is { Count: > 0 })
                {
                    result ??= _frameResult;
                    result[id] = updates;
                }
                if (state.HasActiveAnimations) anyActive = true;
            }
            catch
            {
                // A single malformed value or driver fault must not take down the whole loop
                // (a thrown exception would propagate into the synchronous JS rAF tick and
                // permanently stop animation for every element). Isolate and evict the bad
                // element instead, then keep ticking the rest.
                (faulted ??= new List<string>()).Add(id);
            }
        }

        if (faulted != null)
        {
            foreach (var id in faulted)
            {
                if (_elements.TryGetValue(id, out var badState))
                {
                    try { badState.CancelAll(); } catch { /* best-effort cleanup */ }
                    _elements.Remove(id);
                    // Surface the eviction: the owning component still believes it's registered, so
                    // without this signal a faulted element would silently stop animating forever.
                    // Bmotion re-registers on its next parameter update (see IsRegistered check).
                    _logger?.LogWarning(
                        "Bmotion evicted element '{ElementId}' after its animation tick threw. " +
                        "Animations on it are stopped until it re-registers.", id);
                }
            }
        }

        if (!anyActive)
            StopLoopInternal();

        return result;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Loop lifecycle
    // ═══════════════════════════════════════════════════════════════════════════

    public async ValueTask EnsureLoopRunningAsync()
    {
        if (_loopRunning) return;

        // Concurrent callers (e.g. several elements registering on the same frame) can all pass the
        // check above before any of them flips _loopRunning, which would start the rAF loop more
        // than once. Serialize startup behind a gate so only the first caller starts it; the rest
        // re-check _loopRunning after acquiring the gate and become no-ops.
        await _loopStartGate.WaitAsync();
        try
        {
            if (_loopRunning) return;

            // Bit.Bmotion's animation loop relies on synchronous JS→.NET interop (the JS rAF ticker
            // calls ComputeFrame synchronously). That is only available on Blazor WebAssembly; on
            // Blazor Server / SSR the call would throw an opaque error, so fail fast with a clear one.
            if (!_interop.IsInProcess)
                throw new PlatformNotSupportedException(
                    "Bit.Bmotion requires synchronous JS interop and is only supported on Blazor WebAssembly. " +
                    "It cannot run on Blazor Server or during server-side prerendering.");

            _dotnet ??= DotNetObjectReference.Create(this);
            await _interop.StartRafLoopAsync(_dotnet);
            // Only flag the loop as running once startup actually succeeded; if the interop call
            // throws, the flag stays false so a later call can retry instead of silently no-op'ing.
            _loopRunning = true;
        }
        finally
        {
            _loopStartGate.Release();
        }
    }

    private void StopLoopInternal()
    {
        if (!_loopRunning) return;
        // Clear the flag synchronously so a re-entrant ComputeFrame this frame doesn't schedule a
        // second stop, but defer the actual JS teardown behind _loopStartGate (the same gate
        // EnsureLoopRunningAsync uses to start the loop). Serializing stop and start prevents a
        // delayed stop from racing a restart - tearing down a freshly started loop while
        // _loopRunning is true would otherwise leave the engine stuck (flagged running, JS stopped).
        _loopRunning = false;
        _ = StopRafLoopGatedAsync();

        async Task StopRafLoopGatedAsync()
        {
            try
            {
                await _loopStartGate.WaitAsync();
            }
            catch (ObjectDisposedException)
            {
                // DisposeAsync may dispose the gate while this fire-and-forget task is still
                // pending. Bail out gracefully rather than surfacing an unobserved exception -
                // teardown already stopped the JS loop explicitly.
                return;
            }
            try
            {
                // A restart may have re-flipped _loopRunning to true after we cleared it (and
                // already (re)started the JS loop). Skip the stale stop so we don't tear it down.
                if (_loopRunning) return;
                // Pass our own engine ref so only this engine is removed from the shared JS loop,
                // leaving any other Blazor-root engines ticking.
                await _interop.StopRafLoopAsync(_dotnet);
            }
            catch { /* mid-session stop is best-effort; teardown is ordered explicitly in DisposeAsync */ }
            finally
            {
                // DisposeAsync may dispose the gate (line below) while this fire-and-forget task is
                // still unwinding through the await above. Guard Release() so a disposal race can't
                // surface as an unobserved ObjectDisposedException on this path.
                try { _loopStartGate.Release(); }
                catch (ObjectDisposedException) { /* gate disposed during teardown; nothing to release */ }
            }
        }
    }

    /// <summary>
    /// Fire-and-forget flush used by synchronous entry points (Set / SetInstant / Complete):
    /// kicks the rAF loop on WebAssembly, or drives an instant remote flush on Blazor Server.
    /// Faults are observed and swallowed so an unsupported-platform throw can't surface as an
    /// unobserved task exception.
    /// </summary>
    private void KickFlush(string elementId, BmotionElementAnimationState state)
    {
        _ = KickAsync();

        async Task KickAsync()
        {
            try { await EnsureTickingAsync(elementId, state); }
            catch { /* flush is best-effort here; awaited entry points surface real errors */ }
        }
    }

    public async ValueTask DisposeAsync()
    {
        // Guard against repeated disposal (manual call + DI container teardown): the second pass
        // would otherwise WaitAsync on / Dispose the already-disposed _loopStartGate.
        if (_disposed) return;
        _disposed = true;

        foreach (var (_, state) in _elements)
            state.CancelAll();
        _elements.Clear();
        _retained.Clear();

        foreach (var (_, anim) in _detachedAnims)
        {
            anim.Driver.Cancel();
            anim.Tcs?.TrySetResult(false);
        }
        _detachedAnims.Clear();

        // Serialize teardown behind the same gate EnsureLoopRunningAsync/StopLoopInternal use so a
        // disposal can't interleave with an in-flight loop start - otherwise we could dispose
        // _dotnet while StartRafLoopAsync is still marshaling it, or dispose the gate while a
        // pending gated stop is using it. Acquiring it first drains any active start/stop.
        bool gateHeld = false;
        try
        {
            await _loopStartGate.WaitAsync();
            gateHeld = true;
        }
        catch (ObjectDisposedException) { /* already disposed; nothing to serialize against */ }

        try
        {
            // Await the loop stop before disposing _dotnet so the JS call doesn't marshal a
            // disposed DotNetObjectReference. StopLoopInternal's fire-and-forget path is fine for
            // mid-session stops, but during teardown we must order it explicitly.
            _loopRunning = false;
            if (_dotnet != null)
            {
                try { await _interop.StopRafLoopAsync(_dotnet); } catch { /* ignore during teardown */ }
                try { await _interop.UnwatchReducedMotionAsync(_dotnet); } catch { /* ignore during teardown */ }
                _dotnet.Dispose();
                _dotnet = null;
            }
        }
        finally
        {
            if (gateHeld) _loopStartGate.Release();
        }

        _loopStartGate.Dispose();
        // BmotionInterop is owned and disposed by the DI container (it is registered scoped),
        // so the engine must not dispose it here or it would be disposed twice.
    }
}
