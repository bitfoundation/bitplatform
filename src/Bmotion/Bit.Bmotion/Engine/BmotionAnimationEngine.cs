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
        }
        catch
        {
            // Detection is best-effort: if the browser probe fails we default to
            // animating normally rather than letting it break element initialisation.
            OsPrefersReducedMotion = false;
        }
    }

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
        if (initialValues != null)
            state.SetInstant(initialValues);
    }

    /// <summary>Remove an element and cancel all its animations.</summary>
    public void UnregisterElement(string elementId)
    {
        if (_elements.TryGetValue(elementId, out var state))
        {
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
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            state.AnimateTo(values, transition, tcs);
            await EnsureLoopRunningAsync();
            // .Unwrap() so the nested onComplete() Task is observed rather than dropped
            // (keeps the documented fire-and-forget behaviour of this method).
            _ = tcs.Task.ContinueWith(_ => onComplete(), TaskScheduler.Default).Unwrap();
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
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
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
            _ = EnsureLoopRunningAsync();
        }
    }

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
            _ = EnsureLoopRunningAsync();
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
        Dictionary<string, Dictionary<string, string>>? result = null;
        bool anyActive = false;

        foreach (var (id, state) in _elements)
        {
            var updates = state.Tick(timestamp);
            if (updates is { Count: > 0 })
            {
                result ??= new Dictionary<string, Dictionary<string, string>>();
                result[id] = updates;
            }
            if (state.HasActiveAnimations) anyActive = true;
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
        _ = _interop.StopRafLoopAsync();
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var (_, state) in _elements)
            state.CancelAll();
        _elements.Clear();
        StopLoopInternal();
        _dotnet?.Dispose();
        // BmotionInterop is owned and disposed by the DI container (it is registered scoped),
        // so the engine must not dispose it here or it would be disposed twice.
        await ValueTask.CompletedTask;
    }
}
