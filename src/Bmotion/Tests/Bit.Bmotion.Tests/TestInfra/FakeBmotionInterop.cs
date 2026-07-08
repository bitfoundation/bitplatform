using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Diagnostics.CodeAnalysis;

namespace Bit.Bmotion.Tests.TestInfra;

/// <summary>
/// A scriptable, browser-free <see cref="IBmotionInterop"/> for unit/bUnit tests. It records every
/// interop call and captures the <see cref="DotNetObjectReference{T}"/> instances the engine and
/// components hand to JS, so a test can synthesize the JS→.NET callbacks
/// (<c>ComputeFrame</c>, <c>OnPointerEnter</c>, <c>OnIntersect</c>, …) that a real browser would fire.
/// </summary>
internal sealed class FakeBmotionInterop : IBmotionInterop
{
    /// <summary>One recorded interop call: the JS method name and its non-ref arguments.</summary>
    public sealed record Call(string Method, object?[] Args);

    private readonly List<Call> _calls = new();

    /// <summary>All interop calls in order, for assertions.</summary>
    public IReadOnlyList<Call> Calls => _calls;

    /// <summary>Simulates WASM (synchronous interop) when true, Blazor Server when false.</summary>
    public bool IsInProcess { get; set; } = true;

    /// <summary>Value returned by <see cref="PrefersReducedMotionAsync"/>.</summary>
    public bool PrefersReducedMotion { get; set; }

    /// <summary>Value returned by <see cref="SupportsLinearEasingAsync"/>.</summary>
    public bool SupportsLinearEasing { get; set; } = true;

    /// <summary>Value returned by <see cref="RegisterElementAsync"/>.</summary>
    public bool RegisterElementResult { get; set; } = true;

    /// <summary>Optional rect provider for <see cref="GetBoundingRectAsync"/> keyed by element id.</summary>
    public Func<string, BmotionBoundingRect?>? BoundingRectProvider { get; set; }

    /// <summary>
    /// Hook invoked when the engine starts a WAAPI compositor animation. Return the completion
    /// result (<c>true</c> = natural finish, <c>false</c> = cancelled/failed). Defaults to a pending
    /// task so a test can decide when/if the offload resolves; return a completed task for eager finish.
    /// </summary>
    public Func<string, int, object, object, Task<bool>>? PlayWaapiHandler { get; set; }

    // Captured DotNet references, so tests can invoke [JSInvokable] callbacks directly.
    public object? RafLoopRef { get; private set; }
    public object? ReducedMotionRef { get; private set; }
    public readonly Dictionary<string, object> EventListenerRefs = new();
    public readonly Dictionary<string, object> ViewportRefs = new();

    private void Record(string method, params object?[] args) => _calls.Add(new Call(method, args));

    /// <summary>True if any recorded call used <paramref name="method"/>.</summary>
    public bool WasCalled(string method) => _calls.Any(c => c.Method == method);

    /// <summary>Number of recorded calls to <paramref name="method"/>.</summary>
    public int CountOf(string method) => _calls.Count(c => c.Method == method);

    // ── rAF loop ──────────────────────────────────────────────────────────────
    public ValueTask StartRafLoopAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(DotNetObjectReference<T> dotnetRef) where T : class
    {
        RafLoopRef = dotnetRef.Value;
        Record("startRafLoop");
        return ValueTask.CompletedTask;
    }

    public ValueTask StopRafLoopAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(DotNetObjectReference<T>? dotnetRef = null) where T : class
    {
        Record("stopRafLoop");
        return ValueTask.CompletedTask;
    }

    // ── Reduced motion ────────────────────────────────────────────────────────
    public ValueTask<bool> PrefersReducedMotionAsync()
    {
        Record("prefersReducedMotion");
        return ValueTask.FromResult(PrefersReducedMotion);
    }

    public ValueTask WatchReducedMotionAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(DotNetObjectReference<T> dotnetRef) where T : class
    {
        ReducedMotionRef = dotnetRef.Value;
        Record("watchReducedMotion");
        return ValueTask.CompletedTask;
    }

    public ValueTask UnwatchReducedMotionAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(DotNetObjectReference<T> dotnetRef) where T : class
    {
        Record("unwatchReducedMotion");
        return ValueTask.CompletedTask;
    }

    // ── Style application ─────────────────────────────────────────────────────
    public ValueTask ApplyStylesAsync(string elementId, object styles)
    {
        Record("applyStyles", elementId, styles);
        return ValueTask.CompletedTask;
    }

    public ValueTask PopLayoutAsync(string elementId, double currentX, double currentY)
    {
        Record("popLayout", elementId, currentX, currentY);
        return ValueTask.CompletedTask;
    }

    public ValueTask UnpopLayoutAsync(string elementId)
    {
        Record("unpopLayout", elementId);
        return ValueTask.CompletedTask;
    }

    // ── Element registration ──────────────────────────────────────────────────
    public ValueTask<bool> RegisterElementAsync(string elementId)
    {
        Record("registerElement", elementId);
        return ValueTask.FromResult(RegisterElementResult);
    }

    public ValueTask UnregisterElementAsync(string elementId)
    {
        Record("unregisterElement", elementId);
        return ValueTask.CompletedTask;
    }

    // ── Gesture event listeners ───────────────────────────────────────────────
    public ValueTask AttachEventListenersAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(string elementId, object events, DotNetObjectReference<T> dotnetRef) where T : class
    {
        EventListenerRefs[elementId] = dotnetRef.Value;
        Record("attachEventListeners", elementId, events);
        return ValueTask.CompletedTask;
    }

    public ValueTask StartDragAsync(string elementId, long pointerId, double clientX, double clientY)
    {
        Record("startDrag", elementId, pointerId, clientX, clientY);
        return ValueTask.CompletedTask;
    }

    // ── Viewport observation ──────────────────────────────────────────────────
    public ValueTask ObserveViewportAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(string elementId, DotNetObjectReference<T> dotnetRef, bool once) where T : class
    {
        ViewportRefs[elementId] = dotnetRef.Value;
        Record("observeViewport", elementId, once);
        return ValueTask.CompletedTask;
    }

    public ValueTask ObserveViewportWithOptionsAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(string elementId, DotNetObjectReference<T> dotnetRef, BmViewport options) where T : class
    {
        ViewportRefs[elementId] = dotnetRef.Value;
        Record("observeViewport", elementId, options);
        return ValueTask.CompletedTask;
    }

    public ValueTask UnobserveViewportAsync(string elementId)
    {
        Record("unobserveViewport", elementId);
        return ValueTask.CompletedTask;
    }

    // ── FLIP layout ───────────────────────────────────────────────────────────
    public ValueTask<BmotionBoundingRect?> GetBoundingRectAsync(string elementId)
    {
        Record("getBoundingRect", elementId);
        return ValueTask.FromResult(BoundingRectProvider?.Invoke(elementId));
    }

    public ValueTask PlayWaapiFlipAsync(string elementId, double dx, double dy, double sx, double sy, double durationMs, string easingStr, string? finalTransform)
    {
        Record("playWaapiFlip", elementId, dx, dy, sx, sy, durationMs, easingStr, finalTransform);
        return ValueTask.CompletedTask;
    }

    // ── WAAPI compositor offload ──────────────────────────────────────────────
    public ValueTask<bool> SupportsLinearEasingAsync()
    {
        Record("supportsLinearEasing");
        return ValueTask.FromResult(SupportsLinearEasing);
    }

    public ValueTask<bool> PlayWaapiAnimationAsync(string elementId, int token, object keyframes, object timing)
    {
        Record("playWaapiAnimation", elementId, token, keyframes, timing);
        var task = PlayWaapiHandler?.Invoke(elementId, token, keyframes, timing);
        // Default: never resolves on its own (mirrors a still-running compositor animation).
        return new ValueTask<bool>(task ?? new TaskCompletionSource<bool>().Task);
    }

    public ValueTask CancelWaapiAnimationAsync(string elementId, int token, bool commit)
    {
        Record("cancelWaapiAnimation", elementId, token, commit);
        return ValueTask.CompletedTask;
    }

    // ── Scroll ────────────────────────────────────────────────────────────────
    public ValueTask<string?> ObserveScrollAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(string? containerId, DotNetObjectReference<T> dotnetRef, object? options = null) where T : class
    {
        Record("observeScroll", containerId, options);
        return ValueTask.FromResult<string?>("scroll-key-" + (_calls.Count));
    }

    public ValueTask UnobserveScrollAsync(string key)
    {
        Record("unobserveScroll", key);
        return ValueTask.CompletedTask;
    }

    // ── Programmatic animate() API ─────────────────────────────────────────────
    public ValueTask<string[]> ResolveOrRegisterBySelectorAsync(string selector)
    {
        Record("resolveOrRegisterBySelector", selector);
        return ValueTask.FromResult(new[] { "bm-sel-1" });
    }

    public ValueTask<string> ResolveOrRegisterByRefAsync(ElementReference elementReference)
    {
        Record("resolveOrRegisterByRef");
        return ValueTask.FromResult("bm-ref-1");
    }

    /// <summary>Simulates native support for the View Transitions API (false = fallback path).</summary>
    public bool SupportsViewTransitions { get; set; }

    public async ValueTask<bool> StartViewTransitionAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(DotNetObjectReference<T> dotnetRef, string callbackName) where T : class
    {
        Record("startViewTransition", callbackName);
        // Simulate the browser invoking the C# DOM-update callback during the transition.
        var method = typeof(T).GetMethod(callbackName);
        if (method?.Invoke(dotnetRef.Value, null) is Task task) await task;
        return SupportsViewTransitions;
    }

    public ValueTask DisposeAsync()
    {
        Record("dispose");
        return ValueTask.CompletedTask;
    }
}
