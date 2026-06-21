using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Diagnostics.CodeAnalysis;

namespace Bit.Bmotion;
/// <summary>
/// Slim C# wrapper around the browser-API bridge in <c>BitBmotion.js</c>.
/// Only calls browser-native APIs; all animation logic lives in the C# engine.
/// </summary>
public sealed class BmotionInterop : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask;

    // Generic JS-interop helpers forward DotNetObjectReference<T> to JS, where T is always a
    // concrete component/service whose [JSInvokable] members are kept by the runtime, so the
    // unannotated generic T raises no real trim/AOT concern. Scoped to these call sites instead
    // of a project-wide NoWarn so genuine IL2091 regressions elsewhere stay visible.
    private const string JsRefTrimJustification =
        "DotNetObjectReference<T> only marshals a [JSInvokable]-annotated component/service ref to JS; " +
        "the runtime preserves those members, so the unannotated generic T is safe under trimming.";

    public BmotionInterop(IJSRuntime js)
    {
        ArgumentNullException.ThrowIfNull(js);
        IsInProcess = js is IJSInProcessRuntime;
        _moduleTask = new Lazy<Task<IJSObjectReference>>(
            () => js.InvokeAsync<IJSObjectReference>(
                "import", "./_content/Bit.Bmotion/bit-bmotion.js").AsTask());
    }

    /// <summary>
    /// <c>true</c> when the JS runtime supports synchronous interop (Blazor WebAssembly).
    /// Bit.Bmotion's animation loop and drag handlers rely on synchronous JS↔.NET calls, so the
    /// library only functions on WebAssembly. This is checked before the rAF loop starts.
    /// </summary>
    public bool IsInProcess { get; }

    private async ValueTask<IJSObjectReference> Module() => await _moduleTask.Value;

    // ── rAF loop ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Start the JS rAF loop. The loop calls <c>dotnetRef.invokeMethod('ComputeFrame', timestamp)</c>
    /// synchronously each tick (Blazor WASM) and applies the returned style updates.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2091", Justification = JsRefTrimJustification)]
    public async ValueTask StartRafLoopAsync<T>(DotNetObjectReference<T> dotnetRef) where T : class
        => await (await Module()).InvokeVoidAsync("startRafLoop", dotnetRef);

    /// <summary>Stop the JS rAF loop for the given engine reference (or all engines when null).</summary>
    [UnconditionalSuppressMessage("Trimming", "IL2091", Justification = JsRefTrimJustification)]
    public async ValueTask StopRafLoopAsync<T>(DotNetObjectReference<T>? dotnetRef = null) where T : class
    {
        if (!_moduleTask.IsValueCreated) return;
        await (await Module()).InvokeVoidAsync("stopRafLoop", dotnetRef);
    }

    // ── Reduced motion (accessibility) ────────────────────────────────────────

    /// <summary>
    /// Returns whether the user's OS/browser has <c>prefers-reduced-motion: reduce</c> set.
    /// </summary>
    public async ValueTask<bool> PrefersReducedMotionAsync()
        => await (await Module()).InvokeAsync<bool>("prefersReducedMotion");

    /// <summary>
    /// Subscribes to live changes of the <c>prefers-reduced-motion</c> media query. JS calls
    /// <c>OnReducedMotionChanged(bool)</c> on the engine ref whenever the OS preference changes.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2091", Justification = JsRefTrimJustification)]
    public async ValueTask WatchReducedMotionAsync<T>(DotNetObjectReference<T> dotnetRef) where T : class
        => await (await Module()).InvokeVoidAsync("watchReducedMotion", dotnetRef);

    /// <summary>Unsubscribes the engine ref from <c>prefers-reduced-motion</c> change notifications.</summary>
    [UnconditionalSuppressMessage("Trimming", "IL2091", Justification = JsRefTrimJustification)]
    public async ValueTask UnwatchReducedMotionAsync<T>(DotNetObjectReference<T> dotnetRef) where T : class
    {
        if (!_moduleTask.IsValueCreated) return;
        await (await Module()).InvokeVoidAsync("unwatchReducedMotion", dotnetRef);
    }

    // ── Style application ─────────────────────────────────────────────────────

    /// <summary>Instantly apply a CSS styles object to a DOM element (for <c>set()</c> calls).</summary>
    public async ValueTask ApplyStylesAsync(string elementId, object styles)
        => await (await Module()).InvokeVoidAsync("applyStyles", elementId, styles);

    // ── Element registration ──────────────────────────────────────────────────

    public async ValueTask RegisterElementAsync(string elementId)
        => await (await Module()).InvokeVoidAsync("registerElement", elementId);

    public async ValueTask UnregisterElementAsync(string elementId)
    {
        if (!_moduleTask.IsValueCreated) return;
        await (await Module()).InvokeVoidAsync("unregisterElement", elementId);
    }

    // ── Gesture event listeners ───────────────────────────────────────────────

    /// <summary>
    /// Attach pointer / focus / drag event listeners to an element.
    /// JS forwards raw browser events to the DotNet ref via async callbacks.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2091", Justification = JsRefTrimJustification)]
    public async ValueTask AttachEventListenersAsync<T>(
        string elementId, object events, DotNetObjectReference<T> dotnetRef) where T : class
        => await (await Module()).InvokeVoidAsync("attachEventListeners", elementId, events, dotnetRef);

    // ── Viewport observation ──────────────────────────────────────────────────

    [UnconditionalSuppressMessage("Trimming", "IL2091", Justification = JsRefTrimJustification)]
    public async ValueTask ObserveViewportAsync<T>(
        string elementId, DotNetObjectReference<T> dotnetRef, bool once) where T : class
        => await (await Module()).InvokeVoidAsync("observeViewport", elementId, dotnetRef,
               new Dictionary<string, object?> { ["once"] = once, ["margin"] = "0px", ["threshold"] = 0.0 });

    [UnconditionalSuppressMessage("Trimming", "IL2091", Justification = JsRefTrimJustification)]
    public async ValueTask ObserveViewportWithOptionsAsync<T>(
        string elementId, DotNetObjectReference<T> dotnetRef, BmotionViewportOptions options) where T : class
        => await (await Module()).InvokeVoidAsync("observeViewport", elementId, dotnetRef, options.ToJsObject());

    public async ValueTask UnobserveViewportAsync(string elementId)
    {
        if (!_moduleTask.IsValueCreated) return;
        await (await Module()).InvokeVoidAsync("unobserveViewport", elementId);
    }

    // ── FLIP layout ───────────────────────────────────────────────────────────

    /// <summary>Returns the element's current bounding rect (for C# FLIP delta computation).</summary>
    public async ValueTask<BmotionBoundingRect?> GetBoundingRectAsync(string elementId)
        => await (await Module()).InvokeAsync<BmotionBoundingRect?>("getBoundingRect", elementId);

    /// <summary>Run a FLIP animation via the Web Animations API.</summary>
    public async ValueTask PlayWaapiFlipAsync(
        string elementId, double dx, double dy, double sx, double sy,
        double durationMs, string easingStr, string? finalTransform)
        => await (await Module()).InvokeVoidAsync(
            "playWaapiFlip", elementId, dx, dy, sx, sy, durationMs, easingStr, finalTransform);

    // ── Scroll ────────────────────────────────────────────────────────────────

    [UnconditionalSuppressMessage("Trimming", "IL2091", Justification = JsRefTrimJustification)]
    public async ValueTask<string?> ObserveScrollAsync<T>(
        string? containerId, DotNetObjectReference<T> dotnetRef) where T : class
        => await (await Module()).InvokeAsync<string?>("observeScroll", containerId, dotnetRef);

    public async ValueTask UnobserveScrollAsync(string key)
    {
        if (!_moduleTask.IsValueCreated) return;
        await (await Module()).InvokeVoidAsync("unobserveScroll", key);
    }

    // ── Programmatic animate() API ─────────────────────────────────────────────

    /// <summary>
    /// Resolves all DOM elements matching <paramref name="selector"/>, assigns stable IDs
    /// if needed, and returns those IDs so the <see cref="BmotionAnimationEngine"/> can address them.
    /// </summary>
    public async ValueTask<string[]> ResolveOrRegisterBySelectorAsync(string selector)
        => await (await Module()).InvokeAsync<string[]>("resolveOrRegisterBySelector", selector);

    /// <summary>
    /// Resolves the DOM element for <paramref name="elementReference"/>, assigns a stable ID
    /// if needed, and returns that ID.
    /// </summary>
    public async ValueTask<string> ResolveOrRegisterByRefAsync(ElementReference elementReference)
        => await (await Module()).InvokeAsync<string>("resolveOrRegisterByRef", elementReference);

    // ── Dispose ───────────────────────────────────────────────────────────────

    public async ValueTask DisposeAsync()
    {
        if (!_moduleTask.IsValueCreated) return;
        var module = await Module();
        // Note: each engine already removes itself from the shared JS rAF loop in its own
        // DisposeAsync (StopRafLoopAsync(_dotnet)), so we must not issue the global stopRafLoop(null)
        // here - that would tear down any other engines still sharing the module-level JS loop.
        await module.DisposeAsync();
    }
}
