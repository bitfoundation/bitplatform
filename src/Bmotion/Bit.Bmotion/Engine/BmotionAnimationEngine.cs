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
/// the Blazor WebAssembly UI thread. Completion continuations are scheduled on
/// <see cref="TaskScheduler.Default"/>, which on single-threaded WASM still runs on that same
/// thread. <b>Do not enable WebAssembly multithreading (<c>&lt;WasmEnableThreads&gt;</c>)</b> with
/// this library: the engine has no synchronization and concurrent access would corrupt its state.
/// </para>
/// </remarks>
public sealed class BmotionAnimationEngine : IAsyncDisposable
{
    private readonly BmotionInterop _interop;
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

    public BmotionAnimationEngine(BmotionInterop interop, ILogger<BmotionAnimationEngine>? logger = null)
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

    /// <summary>Register an element and optionally seed its initial CSS state.</summary>
    public void RegisterElement(string elementId, Dictionary<string, object?>? initialValues = null)
    {
        if (!_elements.TryGetValue(elementId, out var state))
        {
            state = new BmotionElementAnimationState
            {
                // Without synchronous interop (Blazor Server) there is no rAF loop: animations
                // either offload to the browser compositor (WAAPI) or collapse to instant sets.
                ForceInstant = !_interop.IsInProcess,
            };
            _elements[elementId] = state;
        }
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
    /// </summary>
    internal async ValueTask AnimateToAsync(
        string elementId,
        Dictionary<string, object?> values,
        BmotionTransitionConfig? transition,
        Func<Task>? onComplete = null,
        bool setAsBase = false)
    {
        if (!_elements.TryGetValue(elementId, out var state)) return;
        if (setAsBase) state.SetBaseAnimation(values, transition);

        // Fold any compositor animation touching the same properties back into state first,
        // so whichever path runs next starts from the element's live values.
        await InterruptWaapiOverlapsAsync(elementId, state, values.Keys);

        var offload = await TryBuildWaapiOffloadAsync(state, values, transition);

        TaskCompletionSource<bool>? tcs = null;
        if (onComplete != null)
        {
            tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            // .Unwrap() so the nested onComplete() Task is observed rather than dropped
            // (keeps the documented fire-and-forget behaviour of this method).
            // The result flag is true only on natural completion; a superseded/cancelled
            // animation resolves with false so OnAnimationComplete is NOT raised for it.
            _ = tcs.Task.ContinueWith(
                    t => t.Result ? onComplete() : Task.CompletedTask,
                    TaskScheduler.Default)
                .Unwrap();
        }

        if (offload != null)
        {
            RegisterWaapiPlan(elementId, state, offload.Plan);
            // Compositor playback is supervised in the background; this method stays fire-and-forget.
            _ = SuperviseOffloadAsync(elementId, state, values, transition, offload, tcs);
            return;
        }

        // The awaits above can interleave with a concurrent caller that registered a compositor
        // plan on the same keys after this call's interrupt pass (see RegisterWaapiPlan, which
        // closes the same race for the WAAPI branch). Sweep again synchronously so the rAF
        // drivers started below are the properties' only owner.
        InterruptWaapiOverlapsSync(elementId, state, values.Keys);
        state.AnimateTo(values, transition, tcs);
        await EnsureTickingAsync(elementId, state);
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

        await InterruptWaapiOverlapsAsync(elementId, state, values.Keys);

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var offload = await TryBuildWaapiOffloadAsync(state, values, transition);
        if (offload != null)
        {
            RegisterWaapiPlan(elementId, state, offload.Plan);
            await SuperviseOffloadAsync(elementId, state, values, transition, offload, tcs);
            await tcs.Task;
            return;
        }

        // Same concurrent-plan sweep as AnimateToAsync's rAF branch (see comment there).
        InterruptWaapiOverlapsSync(elementId, state, values.Keys);
        state.AnimateTo(values, transition, tcs);
        await EnsureTickingAsync(elementId, state);
        await tcs.Task;
    }

    /// <summary>Instantly set values without any animation.</summary>
    public void SetInstant(string elementId, Dictionary<string, object?> values)
    {
        if (_elements.TryGetValue(elementId, out var state))
        {
            // Fold overlapping compositor animations back into state synchronously from the
            // mirrored plans; the JS-side cancel is fire-and-forget.
            var tokens = state.RealizeWaapiPlans(values.Keys);
            if (tokens != null) CancelWaapiTokens(elementId, tokens, commit: false);

            state.SetInstant(values);
            // Kick the loop for a single frame so the change is flushed to the DOM even when
            // the element is otherwise at rest (an instant Set has dirty values but no active
            // animation, so without this it would never be emitted).
            KickFlush(elementId, state);
        }
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
            KickFlush(elementId, state);
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
            var tokens = state.RealizeWaapiPlans(keys);
            if (tokens != null)
            {
                CancelWaapiTokens(elementId, tokens, commit: true);
                KickFlush(elementId, state);
            }
            state.Cancel(properties);
        }
    }

    /// <summary>
    /// Sets the playback rate for an element's animations: 1 = realtime, 0 = paused,
    /// 2 = twice as fast. Negative and non-finite rates are coerced to 0.
    /// </summary>
    public void SetPlaybackRate(string elementId, double rate)
    {
        if (_elements.TryGetValue(elementId, out var state))
            state.PlaybackRate = double.IsFinite(rate) && rate >= 0 ? rate : 0;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Gesture layer management
    // ═══════════════════════════════════════════════════════════════════════════

    internal async ValueTask ActivateGestureLayerAsync(
        string elementId, string gesture,
        Dictionary<string, object?> values, BmotionTransitionConfig? transition)
    {
        if (!_elements.TryGetValue(elementId, out var state)) return;
        // Gesture layers animate through the rAF engine; take back any compositor animation
        // touching the same properties so the layer starts from live values.
        await InterruptWaapiOverlapsAsync(elementId, state, values.Keys);
        state.ActivateGestureLayer(gesture, values, transition);
        await EnsureTickingAsync(elementId, state);
    }

    public async ValueTask DeactivateGestureLayerAsync(string elementId, string gesture)
    {
        if (!_elements.TryGetValue(elementId, out var state)) return;
        // Deactivation animates back toward the base target, which may overlap any plan.
        await InterruptWaapiOverlapsAsync(elementId, state, null);
        state.DeactivateGestureLayer(gesture);
        await EnsureTickingAsync(elementId, state);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Drag position (called synchronously from JS - Blazor WASM only)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Updates the drag position in the element's transform state from a
    /// synchronous JS pointer-move call. The position will be included in the
    /// next <see cref="ComputeFrame"/> output.
    /// </summary>
    public void SetDragPosition(string elementId, double x, double y)
    {
        if (_elements.TryGetValue(elementId, out var state))
            state.SetDragPosition(x, y);
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

        bool inertiaXStarted = false, inertiaYStarted = false;
        if (momentum)
        {
            // velX/velY arrive from JS already scaled to "px per frame" (~16 ms). The * 50 factor
            // converts that frame-relative figure into the larger projected-distance velocity the
            // exponential-decay inertia driver expects (tuned so a natural flick throws roughly the
            // distance Framer Motion produces for the same gesture).
            const double inertiaVelocityScale = 50.0;
            if (axis != "y" && Math.Abs(velX) > 0.5)
            {
                var inertiaX = new BmotionTransitionConfig
                {
                    Type = BmotionTransitionType.Inertia,
                    InertiaVelocity = velX * inertiaVelocityScale,
                    InertiaMin = constraints?.Left,
                    InertiaMax = constraints?.Right,
                };
                var valuesX = new Dictionary<string, object?> { ["x"] = posX };
                state.AnimateTo(valuesX, inertiaX);
                inertiaXStarted = true;
            }

            if (axis != "x" && Math.Abs(velY) > 0.5)
            {
                var inertiaY = new BmotionTransitionConfig
                {
                    Type = BmotionTransitionType.Inertia,
                    InertiaVelocity = velY * inertiaVelocityScale,
                    InertiaMin = constraints?.Top,
                    InertiaMax = constraints?.Bottom,
                };
                var valuesY = new Dictionary<string, object?> { ["y"] = posY };
                state.AnimateTo(valuesY, inertiaY);
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
                { Type = BmotionTransitionType.Spring, Stiffness = 400, Damping = 35 };

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
                var snapValues = new Dictionary<string, object?>();
                if (axis != "y" && !inertiaXStarted) snapValues["x"] = cx;
                if (axis != "x" && !inertiaYStarted) snapValues["y"] = cy;
                state.AnimateTo(snapValues, snapT);
            }
        }

        if (state.HasActiveAnimations)
            await EnsureTickingAsync(elementId, state);
    }

    /// <summary>Returns the current CSS transform string for the element (used by FLIP).</summary>
    public string? GetCurrentTransformString(string elementId)
    {
        if (!_elements.TryGetValue(elementId, out var state)) return null;
        return BmotionTransformComposer.Build(state.Transforms);
    }

    /// <summary>Returns the <see cref="BmotionElementAnimationState"/> for an element, or null.</summary>
    internal BmotionElementAnimationState? GetState(string elementId)
        => _elements.GetValueOrDefault(elementId);

    // ═══════════════════════════════════════════════════════════════════════════
    // WAAPI compositor offload
    //
    // Eligible animations (transform/opacity, tween or zero-velocity spring, single-value
    // targets) are pre-sampled in C# and handed to the browser as a ready-made Web Animation:
    // playback runs on the compositor with zero per-frame interop, and - because only async
    // interop is needed - these animations also work on Blazor Server. The engine keeps a
    // mirrored "plan" (the same sampled curve) so interruptions can compute the element's
    // current values without reading the DOM.
    // ═══════════════════════════════════════════════════════════════════════════

    private int _waapiTokenSeq;
    private bool? _linearEasingSupported;

    private sealed record WaapiOffload(
        BmotionElementAnimationState.WaapiPlan Plan,
        object[] Keyframes,
        Dictionary<string, object?> Timing);

    /// <summary>Whether this environment can run the per-frame rAF loop (Blazor WebAssembly).</summary>
    internal bool SupportsFrameLoop => _interop.IsInProcess;

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
    private async ValueTask InterruptWaapiOverlapsAsync(
        string elementId, BmotionElementAnimationState state, IReadOnlyCollection<string>? keys)
    {
        if (!state.HasWaapiPlans) return;
        var tokens = state.RealizeWaapiPlans(keys);
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

    /// <summary>
    /// Synchronous variant of <see cref="InterruptWaapiOverlapsAsync"/> for callers that must not
    /// yield between the sweep and taking ownership of the properties: realizes overlapping plans
    /// into state inline and cancels them on the JS side fire-and-forget (commit-first, so the
    /// element holds its current values until the new owner's first write lands).
    /// </summary>
    private void InterruptWaapiOverlapsSync(
        string elementId, BmotionElementAnimationState state, IReadOnlyCollection<string>? keys)
    {
        var tokens = state.RealizeWaapiPlans(keys);
        if (tokens != null) CancelWaapiTokens(elementId, tokens, commit: true);
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

    /// <summary>
    /// Builds a compositor offload for the animation when it is eligible, otherwise null
    /// (the caller then uses the rAF path / instant fallback).
    /// </summary>
    private async ValueTask<WaapiOffload?> TryBuildWaapiOffloadAsync(
        BmotionElementAnimationState state,
        Dictionary<string, object?> values,
        BmotionTransitionConfig? transition)
    {
        var config = transition ?? new BmotionTransitionConfig();

        // Features the compositor path can't express - stay on the rAF engine.
        if (config.OnUpdate != null) return null;
        if (config.Properties is { Count: > 0 }) return null;
        if (config.RepeatDelay > 0) return null;
        if (config.RepeatType == BmRepeatType.Reverse && (config.IsInfiniteRepeat || config.Repeat > 0)) return null;
        if (config.Times != null) return null;
        if (config.Type == BmotionTransitionType.Inertia) return null;
        // A spring with initial velocity produces a per-distance curve that a shared normalized
        // sample table can't represent.
        if (config.Type == BmotionTransitionType.Spring && config.Velocity != 0) return null;
        if (config.Type == BmotionTransitionType.Tween && config.Duration <= 0) return null;

        // Every animated property must be a transform component or opacity with a single
        // finite numeric target.
        var targets = new Dictionary<string, (double From, double To)>(StringComparer.OrdinalIgnoreCase);
        bool touchesTransform = false;
        foreach (var (key, raw) in values)
        {
            if (raw == null) continue;
            bool isTransform = BmotionTransformComposer.IsTransformProp(key);
            if (!isTransform && !string.Equals(key, "opacity", StringComparison.OrdinalIgnoreCase)) return null;

            double to;
            switch (raw)
            {
                case double d: to = d; break;
                case int i: to = i; break;
                case float f: to = f; break;
                case long l: to = l; break;
                default: return null; // keyframe arrays / strings stay on the rAF path
            }
            if (!double.IsFinite(to)) return null;

            double from = isTransform
                ? state.Transforms.GetValueOrDefault(key,
                    key is "scale" or "scaleX" or "scaleY" ? 1.0 : 0.0)
                : state.NumericValues.GetValueOrDefault(key, 1.0); // opacity defaults to 1
            targets[key] = (from, to);
            touchesTransform |= isTransform;
        }
        if (targets.Count == 0) return null;

        // transform is a single CSS property: the compositor can't own it while rAF drivers
        // are animating other transform components on the same element.
        if (touchesTransform && state.HasActiveTransformDriver()) return null;

        // Sample the curve. Springs need linear() easing support; tweens prefer it but can
        // fall back to their exact CSS easing.
        double durationMs;
        double[] samples;
        string easing;
        if (config.Type == BmotionTransitionType.Spring)
        {
            if (!await LinearEasingSupportedAsync()) return null;
            var sampled = SampleSpringProgress(config);
            if (sampled is null) return null;
            (samples, durationMs) = sampled.Value;
            easing = BuildLinearEasing(samples);
        }
        else
        {
            samples = SampleTweenProgress(config);
            durationMs = config.Duration * 1000;
            easing = await LinearEasingSupportedAsync()
                ? BuildLinearEasing(samples)
                : BmEaseFunctions.ToCssString(config);
        }

        // Two keyframes composed from the FULL transform state so untouched components persist,
        // with identical function order in both frames (required for piecewise interpolation).
        var fromStyles = new Dictionary<string, object>();
        var toStyles = new Dictionary<string, object>();
        if (touchesTransform)
        {
            var fromT = new Dictionary<string, double>(state.Transforms, StringComparer.OrdinalIgnoreCase);
            foreach (var (key, (from, _)) in targets)
                if (BmotionTransformComposer.IsTransformProp(key)) fromT[key] = from;
            var toT = new Dictionary<string, double>(fromT, StringComparer.OrdinalIgnoreCase);
            foreach (var (key, (_, to)) in targets)
                if (BmotionTransformComposer.IsTransformProp(key)) toT[key] = to;

            var fromStr = BmotionTransformComposer.Build(fromT);
            var toStr = BmotionTransformComposer.Build(toT);
            fromStyles["transform"] = string.IsNullOrEmpty(fromStr) ? "none" : fromStr;
            toStyles["transform"] = string.IsNullOrEmpty(toStr) ? "none" : toStr;
        }
        if (targets.TryGetValue("opacity", out var opacity))
        {
            fromStyles["opacity"] = BmotionCssFormat.Num(opacity.From);
            toStyles["opacity"] = BmotionCssFormat.Num(opacity.To);
        }

        bool infinite = config.IsInfiniteRepeat;
        string direction = config.RepeatType == BmRepeatType.Mirror ? "alternate" : "normal";
        double delayMs = config.Delay * 1000;

        var plan = new BmotionElementAnimationState.WaapiPlan
        {
            Token = ++_waapiTokenSeq,
            StartMs = Environment.TickCount64,
            DelayMs = delayMs,
            DurationMs = durationMs,
            Progress = samples,
            Values = targets,
            Iterations = infinite ? -1 : config.Repeat,
            Mirror = direction == "alternate",
        };

        var timing = new Dictionary<string, object?>
        {
            ["duration"] = durationMs,
            ["delay"] = delayMs,
            ["easing"] = easing,
            ["iterations"] = infinite ? -1 : config.Repeat + 1,
            ["direction"] = direction,
        };

        return new WaapiOffload(plan, [fromStyles, toStyles], timing);
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

        if (finished)
        {
            state.TryCompleteWaapiPlan(offload.Plan.Token);
            tcs?.TrySetResult(true);
            return;
        }

        // Not finished: either an interruption already realized+removed the plan (a newer owner
        // has the element), or playback never started (element missing / easing rejected).
        bool wasStillRegistered = state.TryRealizeWaapiPlan(offload.Plan.Token, out double elapsedMs);
        if (wasStillRegistered && elapsedMs < 50)
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

    private static double[] SampleTweenProgress(BmotionTransitionConfig config)
    {
        var ease = BmEaseFunctions.Get(config);
        const int sampleCount = 33;
        var samples = new double[sampleCount];
        for (int i = 0; i < sampleCount; i++)
            samples[i] = ease((double)i / (sampleCount - 1));
        return samples;
    }

    /// <summary>
    /// Simulates the spring on a normalized 0→1 distance (valid for any distance when initial
    /// velocity is zero - the ODE is linear) and returns the eased-progress samples plus the
    /// settling duration. Null when the spring doesn't settle within 20 s.
    /// </summary>
    private static (double[] Samples, double DurationMs)? SampleSpringProgress(BmotionTransitionConfig config)
    {
        var sampling = config.Clone();
        sampling.Delay = 0;
        sampling.Repeat = 0;
        sampling.RepeatInfinite = false;
        sampling.RepeatDelay = 0;

        double value = 0;
        BmotionSpringDriver driver;
        try { driver = new BmotionSpringDriver(0, 1, sampling, v => value = v); }
        catch { return null; }

        const double stepMs = 1000.0 / 60;
        const int maxSamples = 20 * 60;
        var samples = new List<double>(128);
        double t = 0;
        bool done = false;
        for (int i = 0; i < maxSamples && !done; i++)
        {
            done = driver.Tick(t);
            samples.Add(value);
            t += stepMs;
        }
        if (!done || samples.Count < 2) return null;
        samples[^1] = 1; // land exactly on the target
        return (samples.ToArray(), (samples.Count - 1) * stepMs);
    }

    private static string BuildLinearEasing(double[] samples)
    {
        var sb = new System.Text.StringBuilder("linear(");
        for (int i = 0; i < samples.Length; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(Math.Round(samples[i], 4).ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        return sb.Append(')').ToString();
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

        foreach (var (id, state) in _elements)
        {
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
