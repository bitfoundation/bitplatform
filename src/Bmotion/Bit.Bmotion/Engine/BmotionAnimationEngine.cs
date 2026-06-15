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
public sealed class BmotionAnimationEngine : IAsyncDisposable
{
    private readonly BmotionInterop _interop;
    private readonly Dictionary<string, BmotionElementAnimationState> _elements = new();
    private DotNetObjectReference<BmotionAnimationEngine>? _dotnet;
    private bool _loopRunning;
    private bool _reducedMotionDetected;

    // Reused across frames so the rAF tick doesn't allocate a fresh outer dictionary every ~16 ms.
    // Marshaled synchronously to JS before the next ComputeFrame runs (single-threaded Blazor WASM).
    private readonly Dictionary<string, Dictionary<string, string>> _frameResult = new();

    public BmotionAnimationEngine(BmotionInterop interop) => _interop = interop;

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
    public async ValueTask EnsureReducedMotionDetectedAsync()
    {
        if (_reducedMotionDetected) return;
        _reducedMotionDetected = true;
        try
        {
            OsPrefersReducedMotion = await _interop.PrefersReducedMotionAsync();
            // Subscribe to live OS changes so toggling prefers-reduced-motion at runtime is honoured
            // (the value was previously cached for the engine's whole lifetime).
            _dotnet ??= DotNetObjectReference.Create(this);
            await _interop.WatchReducedMotionAsync(_dotnet);
        }
        catch
        {
            // Detection is best-effort: if the browser probe fails we default to
            // animating normally rather than letting it break element initialisation.
            OsPrefersReducedMotion = false;
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
            state = new BmotionElementAnimationState();
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

    /// <summary>Start animating to the given values. Returns immediately (fire-and-forget).</summary>
    public async ValueTask AnimateToAsync(
        string elementId,
        Dictionary<string, object?> values,
        BmotionTransitionConfig? transition,
        Func<Task>? onComplete = null)
    {
        if (!_elements.TryGetValue(elementId, out var state)) return;
        state.SetBaseAnimation(values, transition);
        if (onComplete != null)
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            state.AnimateTo(values, transition, tcs);
            await EnsureLoopRunningAsync();
            // .Unwrap() so the nested onComplete() Task is observed rather than dropped
            // (keeps the documented fire-and-forget behaviour of this method).
            // The result flag is true only on natural completion; a superseded/cancelled
            // animation resolves with false so OnAnimationComplete is NOT raised for it.
            _ = tcs.Task.ContinueWith(
                    t => t.Result ? onComplete() : Task.CompletedTask,
                    TaskScheduler.Default)
                .Unwrap();
        }
        else
        {
            state.AnimateTo(values, transition);
            await EnsureLoopRunningAsync();
        }
    }

    /// <summary>Animate to the given values and await animation completion.</summary>
    public async ValueTask AnimateToAwaitAsync(
        string elementId,
        Dictionary<string, object?> values,
        BmotionTransitionConfig? transition)
    {
        if (!_elements.TryGetValue(elementId, out var state)) return;
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        state.SetBaseAnimation(values, transition);
        state.AnimateTo(values, transition, tcs);
        await EnsureLoopRunningAsync();
        await tcs.Task;
    }

    /// <summary>Instantly set values without any animation.</summary>
    public void SetInstant(string elementId, Dictionary<string, object?> values)
    {
        if (_elements.TryGetValue(elementId, out var state))
        {
            state.SetInstant(values);
            // Kick the loop for a single frame so the change is flushed to the DOM even when
            // the element is otherwise at rest (an instant Set has dirty values but no active
            // animation, so without this it would never be emitted).
            KickLoop();
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
    /// Finish all animations on an element immediately, snapping every property to its target
    /// (end) value, then flush the final frame to the DOM.
    /// </summary>
    public void Complete(string elementId)
    {
        if (_elements.TryGetValue(elementId, out var state))
        {
            state.CompleteAll();
            KickLoop();
        }
    }

    /// <summary>Stop animations on specific properties (or all when <paramref name="properties"/> is null/empty).</summary>
    public void Stop(string elementId, string[]? properties)
    {
        if (_elements.TryGetValue(elementId, out var state))
            state.Cancel(properties);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Gesture layer management
    // ═══════════════════════════════════════════════════════════════════════════

    public async ValueTask ActivateGestureLayerAsync(
        string elementId, string gesture,
        Dictionary<string, object?> values, BmotionTransitionConfig? transition)
    {
        if (!_elements.TryGetValue(elementId, out var state)) return;
        state.ActivateGestureLayer(gesture, values, transition);
        await EnsureLoopRunningAsync();
    }

    public async ValueTask DeactivateGestureLayerAsync(string elementId, string gesture)
    {
        if (!_elements.TryGetValue(elementId, out var state)) return;
        state.DeactivateGestureLayer(gesture);
        await EnsureLoopRunningAsync();
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
        return _elements.TryGetValue(elementId, out var state)
            ? state.GetCurrentXY()
            : (0, 0);
    }

    /// <summary>
    /// Completes a drag and optionally starts inertia animations.
    /// </summary>
    public async ValueTask EndDragAsync(
        string elementId,
        double velX, double velY,
        bool momentum,
        BmotionDragConstraints? constraints,
        string? axis,
        BmotionTransitionConfig? snapTransition)
    {
        if (!_elements.TryGetValue(elementId, out var state)) return;

        state.EndDrag();

        var (posX, posY) = state.GetCurrentXY();

        bool inertiaXStarted = false, inertiaYStarted = false;
        if (momentum)
        {
            if (axis != "y" && Math.Abs(velX) > 0.5)
            {
                var inertiaX = new BmotionTransitionConfig
                {
                    Type = BmotionTransitionType.Inertia,
                    InertiaVelocity = velX * 50,
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
                    InertiaVelocity = velY * 50,
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
            await EnsureLoopRunningAsync();
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

    private void StopLoopInternal()
    {
        if (!_loopRunning) return;
        _loopRunning = false;
        // Pass our own engine ref so only this engine is removed from the shared JS loop,
        // leaving any other Blazor-root engines ticking.
        _ = _interop.StopRafLoopAsync(_dotnet);
    }

    /// <summary>
    /// Fire-and-forget loop start that observes (and swallows) any fault. Used by synchronous entry
    /// points (Set / SetInstant / Complete) so an unsupported-platform throw can't surface as an
    /// unobserved task exception.
    /// </summary>
    private void KickLoop()
    {
        _ = KickLoopAsync();

        async Task KickLoopAsync()
        {
            try { await EnsureLoopRunningAsync(); }
            catch { /* loop start is best-effort here; awaited entry points surface real errors */ }
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var (_, state) in _elements)
            state.CancelAll();
        _elements.Clear();

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
        // BmotionInterop is owned and disposed by the DI container (it is registered scoped),
        // so the engine must not dispose it here or it would be disposed twice.
    }
}
