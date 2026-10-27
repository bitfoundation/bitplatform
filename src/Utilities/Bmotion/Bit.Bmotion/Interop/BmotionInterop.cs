using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Diagnostics.CodeAnalysis;

namespace Bit.Bmotion;
/// <summary>
/// Slim C# wrapper around the browser-API bridge in <c>BitBmotion.js</c>.
/// Only calls browser-native APIs; all animation logic lives in the C# engine.
/// </summary>
public sealed class BmotionInterop : IBmotionInterop
{
    private readonly IJSRuntime _js;
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask;

    public BmotionInterop(IJSRuntime js)
    {
        ArgumentNullException.ThrowIfNull(js);
        _js = js;
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

    /// <summary>
    /// <c>true</c> while the components are being prerendered or statically rendered on the server.
    /// Read live: a circuit's runtime only becomes usable once the circuit attaches to it.
    /// </summary>
    public bool IsPrerendering => IsStaticRuntime(_js);

    // A scope with no browser gets one of two framework-internal runtimes: static SSR's placeholder
    // (every call throws), or - in apps with Server interactivity - a circuit runtime no circuit has
    // attached to. Neither type is public, so they are recognised by name.
    [UnconditionalSuppressMessage("Trimming", "IL2075",
        Justification = "RemoteJSRuntime only exists in Blazor Server apps, which are never trimmed; a missing property reads as interactive.")]
    private static bool IsStaticRuntime(IJSRuntime js)
    {
        if (OperatingSystem.IsBrowser()) return false;
        var type = js.GetType();
        if (type.Name == "UnsupportedJavaScriptRuntime") return true;
        if (type.Name == "RemoteJSRuntime")
            return type.GetProperty("IsInitialized")?.GetValue(js) is false;
        return false;
    }

    /// <summary>
    /// How long the CSS animation of the prerendered element with id <paramref name="elementId"/>
    /// has been running - read off the element itself, which is still in the DOM while the runtime
    /// renders the markup that replaces it, so it is right however late that markup arrived
    /// (streamed, or brought in by enhanced navigation). Synchronous, so WebAssembly only.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Deserializes only JsonElement, which System.Text.Json handles without reflection over user types.")]
    [UnconditionalSuppressMessage("AOT", "IL3050",
        Justification = "Deserializes only JsonElement, which needs no runtime code generation.")]
    public double? GetPrerenderedAnimationElapsedMs(string elementId)
    {
        if (_js is not IJSInProcessRuntime js) return null;
        try
        {
            using var element = js.Invoke<IJSInProcessObjectReference>("document.getElementById", elementId);
            using var animations = element.Invoke<IJSInProcessObjectReference>("getAnimations");
            // Every animation in the element's list started when the element did; a local time
            // counts from there, delay included - the same origin the prerendered delays use.
            var timing = animations.Invoke<System.Text.Json.JsonElement>("0.effect.getComputedTiming");
            return timing.TryGetProperty("localTime", out var local) && local.TryGetDouble(out var ms)
                ? Math.Max(0, ms)
                : null;
        }
        catch
        {
            // No such element, or no animation still playing on it: there is nothing to continue,
            // so the element lands at rest.
            return null;
        }
    }

    /// <summary>
    /// Where the browser has really got with the offloaded animation <paramref name="token"/> on
    /// <paramref name="elementId"/> - how far it has played, in ms of animation time, as of now, and whether
    /// its start is final (the bridge's <c>waapiPlayhead</c>). Synchronous, so WebAssembly only; <c>null</c>
    /// elsewhere, before the bridge has loaded (nothing it plays can exist yet), and when no such animation
    /// is playing.
    /// </summary>
    public (double PlayedMs, bool Final)? GetWaapiPlayhead(string elementId, int token)
    {
        if (!_moduleTask.IsValueCreated || _moduleTask.Value is not { IsCompletedSuccessfully: true } loaded) return null;
        if (loaded.Result is not IJSInProcessObjectReference module) return null;
        try
        {
            return module.Invoke<double[]?>("waapiPlayhead", elementId, token) is [var played, var final] && double.IsFinite(played)
                ? (played, final != 0)
                : null;
        }
        catch { return null; }   // a bridge that can't answer leaves the engine on its own clock
    }

    private async ValueTask<IJSObjectReference> Module() => await _moduleTask.Value;

    // ── rAF loop ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Start the JS rAF loop. The loop calls <c>dotnetRef.invokeMethod('ComputeFrame', timestamp)</c>
    /// synchronously each tick (Blazor WASM) and applies the returned style updates.
    /// </summary>
    public async ValueTask StartRafLoopAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(DotNetObjectReference<T> dotnetRef) where T : class
        => await (await Module()).InvokeVoidAsync("startRafLoop", dotnetRef);

    /// <summary>Stop the JS rAF loop for the given engine reference (or all engines when null).</summary>
    public async ValueTask StopRafLoopAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(DotNetObjectReference<T>? dotnetRef = null) where T : class
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
    public async ValueTask WatchReducedMotionAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(DotNetObjectReference<T> dotnetRef) where T : class
        => await (await Module()).InvokeVoidAsync("watchReducedMotion", dotnetRef);

    /// <summary>Unsubscribes the engine ref from <c>prefers-reduced-motion</c> change notifications.</summary>
    public async ValueTask UnwatchReducedMotionAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(DotNetObjectReference<T> dotnetRef) where T : class
    {
        if (!_moduleTask.IsValueCreated) return;
        await (await Module()).InvokeVoidAsync("unwatchReducedMotion", dotnetRef);
    }

    // ── Style application ─────────────────────────────────────────────────────

    /// <summary>Instantly apply a CSS styles object to a DOM element (for <c>set()</c> calls).</summary>
    public async ValueTask ApplyStylesAsync(string elementId, object styles)
        => await (await Module()).InvokeVoidAsync("applyStyles", elementId, styles);

    /// <summary>
    /// Pops an element out of the layout flow (position: absolute pinned at its current spot)
    /// for a popLayout exit. <paramref name="currentX"/>/<paramref name="currentY"/> are the
    /// element's current transform translation, backed out of the measured position.
    /// </summary>
    public async ValueTask PopLayoutAsync(string elementId, double currentX, double currentY)
        => await (await Module()).InvokeVoidAsync("popLayout", elementId, currentX, currentY);

    /// <summary>Restores the inline styles replaced by <see cref="PopLayoutAsync"/> (exit cancelled).</summary>
    public async ValueTask UnpopLayoutAsync(string elementId)
        => await (await Module()).InvokeVoidAsync("unpopLayout", elementId);

    public async ValueTask SetInertAsync(string elementId, bool inert)
        => await (await Module()).InvokeVoidAsync("setInert", elementId, inert);

    public async ValueTask AttachReorderKeysAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(
        string elementId, DotNetObjectReference<T> dotnetRef, string axis, string? handleSelector) where T : class
        => await (await Module()).InvokeVoidAsync("attachReorderKeys", elementId, dotnetRef, axis, handleSelector);

    public async ValueTask DetachReorderKeysAsync(string elementId)
        => await (await Module()).InvokeVoidAsync("detachReorderKeys", elementId);

    public async ValueTask FocusAsync(string elementId)
        => await (await Module()).InvokeVoidAsync("focusElement", elementId);

    public async ValueTask AnnounceAsync(string text)
        => await (await Module()).InvokeVoidAsync("announce", text);

    // ── Element registration ──────────────────────────────────────────────────

    /// <summary>Marks the element for the JS bridge; <c>false</c> when no element with the id exists.</summary>
    public async ValueTask<bool> RegisterElementAsync(string elementId)
        => await (await Module()).InvokeAsync<bool>("registerElement", elementId);

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
    public async ValueTask AttachEventListenersAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(
        string elementId, object events, DotNetObjectReference<T> dotnetRef) where T : class
        => await (await Module()).InvokeVoidAsync("attachEventListeners", elementId, events, dotnetRef);

    /// <summary>
    /// Starts a drag on an element from an external pointer event (see <see cref="BmDragControls"/>).
    /// No-op when the element has no drag attached.
    /// </summary>
    public async ValueTask StartDragAsync(string elementId, long pointerId, double clientX, double clientY)
        => await (await Module()).InvokeVoidAsync("startDrag", elementId, pointerId, clientX, clientY);

    // ── Viewport observation ──────────────────────────────────────────────────

    public async ValueTask ObserveViewportAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(
        string elementId, DotNetObjectReference<T> dotnetRef, bool once) where T : class
        => await (await Module()).InvokeVoidAsync("observeViewport", elementId, dotnetRef,
               new Dictionary<string, object?> { ["once"] = once, ["margin"] = "0px", ["threshold"] = 0.0 });

    public async ValueTask ObserveViewportWithOptionsAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(
        string elementId, DotNetObjectReference<T> dotnetRef, BmViewport options) where T : class
        => await (await Module()).InvokeVoidAsync("observeViewport", elementId, dotnetRef, options.ToJsObject());

    public async ValueTask UnobserveViewportAsync(string elementId)
    {
        if (!_moduleTask.IsValueCreated) return;
        await (await Module()).InvokeVoidAsync("unobserveViewport", elementId);
    }

    // ── FLIP layout ───────────────────────────────────────────────────────────

    /// <summary>Returns the element's current bounding rect (for C# FLIP delta computation).</summary>
    public async ValueTask<BmotionBoundingRect?> GetBoundingRectAsync(
        string elementId, BmotionMeasureOptions options = default)
        // The default measurement sends no options object at all, so the overwhelmingly common
        // FLIP path marshals exactly what it always did.
        => options.IsDefault
            ? await (await Module()).InvokeAsync<BmotionBoundingRect?>("getBoundingRect", elementId)
            : await (await Module()).InvokeAsync<BmotionBoundingRect?>("getBoundingRect", elementId,
                new Dictionary<string, object?>
                {
                    ["trackScroll"] = options.TrackScroll,
                    ["fixedRoot"] = options.FixedRoot,
                });

    /// <summary>Run a FLIP animation via the Web Animations API.</summary>
    public async ValueTask PlayWaapiFlipAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(
        string elementId, double dx, double dy, double sx, double sy,
        double durationMs, string easingStr, string? finalTransform,
        double originX, double originY, DotNetObjectReference<T>? dotnetRef) where T : class
        => await (await Module()).InvokeVoidAsync(
            "playWaapiFlip", elementId, dx, dy, sx, sy, durationMs, easingStr, finalTransform,
            originX, originY, dotnetRef);

    public async ValueTask<BmotionBoundingRect?> MeasureAndHoldLayoutAsync(string elementId, BmotionMeasureOptions options,
        bool translate, bool scale, double anchorX, double anchorY)
        => await (await Module()).InvokeAsync<BmotionBoundingRect?>("measureAndHoldLayout", elementId,
            new Dictionary<string, object?>
            {
                ["trackScroll"] = options.TrackScroll,
                ["fixedRoot"] = options.FixedRoot,
            },
            translate, scale, anchorX, anchorY);

    public async ValueTask ReleaseLayoutHoldAsync(string elementId)
        => await (await Module()).InvokeVoidAsync("releaseLayoutHold", elementId);

    public async ValueTask PlayLayoutCrossfadeAsync(string leadId, string followId, double dx, double dy, double sx, double sy,
        double durationMs, string easingStr, double originX, double originY)
        => await (await Module()).InvokeVoidAsync(
            "playLayoutCrossfade", leadId, followId, dx, dy, sx, sy, durationMs, easingStr, originX, originY);

    public async ValueTask CancelLayoutCrossfadeAsync(string elementId)
        => await (await Module()).InvokeVoidAsync("cancelLayoutCrossfade", elementId);

    // ── WAAPI compositor offload ──────────────────────────────────────────────

    /// <summary>Whether the browser supports the <c>linear()</c> easing function (spring offload).</summary>
    public async ValueTask<bool> SupportsLinearEasingAsync()
        => await (await Module()).InvokeAsync<bool>("supportsLinearEasing");

    /// <summary>Whether the browser supports <c>oklab()</c> colours (OKLab colour transitions in the browser).</summary>
    public async ValueTask<bool> SupportsOklabColorAsync()
        => await (await Module()).InvokeAsync<bool>("supportsOklabColor");

    /// <summary>
    /// Reads computed values, and the values the stylesheet gives properties with their inline declarations
    /// lifted, in one call (see <see cref="IBmotionInterop.ReadStylesAsync(string, string[], string[])"/>).
    /// </summary>
    public async ValueTask<BmotionStyleRead?> ReadStylesAsync(string elementId, string[] current, string[] stylesheet)
        => await (await Module()).InvokeAsync<BmotionStyleRead?>("readStyles", elementId, current, stylesheet);

    /// <summary>Also measures the natural size of the <paramref name="auto"/> properties (see <see cref="IBmotionInterop"/>).</summary>
    public async ValueTask<BmotionStyleRead?> ReadStylesAsync(string elementId, string[] current, string[] stylesheet, string[] auto)
        => await (await Module()).InvokeAsync<BmotionStyleRead?>("readStyles", elementId, current, stylesheet, auto);

    /// <summary>
    /// Plays a pre-sampled compositor animation via <c>element.animate()</c>. Resolves
    /// <c>true</c> on natural completion, <c>false</c> when cancelled or failed to start.
    /// </summary>
    public async ValueTask<bool> PlayWaapiAnimationAsync(
        string elementId, int token, object keyframes, object timing)
        => await (await Module()).InvokeAsync<bool>("playWaapiAnimation", elementId, token, keyframes, timing);

    /// <summary>Cancels one compositor animation; <paramref name="commit"/> snapshots current values inline first.</summary>
    public async ValueTask CancelWaapiAnimationAsync(string elementId, int token, bool commit)
    {
        if (!_moduleTask.IsValueCreated) return;
        await (await Module()).InvokeVoidAsync("cancelWaapiAnimation", elementId, token, commit);
    }

    /// <summary>Sets the playback rate of the element's compositor animations (1 = realtime, 0 = paused).</summary>
    public async ValueTask SeekWaapiAnimationsAsync(string elementId, double playedMs)
    {
        if (!_moduleTask.IsValueCreated) return;
        await (await Module()).InvokeVoidAsync("seekWaapiAnimations", elementId, playedMs);
    }

    public async ValueTask SetWaapiPlaybackRateAsync(string elementId, double rate)
    {
        if (!_moduleTask.IsValueCreated) return;
        await (await Module()).InvokeVoidAsync("setWaapiPlaybackRate", elementId, rate);
    }

    // ── Scroll-driven timelines ───────────────────────────────────────────────

    /// <summary>
    /// Plays a pre-sampled animation progressed by scroll position. Returns <c>true</c> when a
    /// native scroll timeline drove it, <c>false</c> when the scroll-scrub fallback did, and
    /// <c>null</c> when it could not start at all.
    /// </summary>
    public async ValueTask<bool?> PlayScrollTimelineAsync(
        string elementId, int token, object keyframes, object timeline)
        => await (await Module()).InvokeAsync<bool?>(
            "playScrollTimelineAnimation", elementId, token, keyframes, timeline);

    /// <summary>Stops a scroll-driven animation and releases any scroll listener behind it.</summary>
    public async ValueTask CancelScrollTimelineAsync(string elementId, int token)
    {
        if (!_moduleTask.IsValueCreated) return;
        await (await Module()).InvokeVoidAsync("cancelScrollTimelineAnimation", elementId, token);
    }

    // ── Scroll ────────────────────────────────────────────────────────────────

    public async ValueTask<string?> ObserveScrollAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(
        string? containerId, DotNetObjectReference<T> dotnetRef, object? options = null) where T : class
        => await (await Module()).InvokeAsync<string?>("observeScroll", containerId, dotnetRef, options);

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

    // ── View Transitions API ───────────────────────────────────────────────────

    /// <summary>Wraps <c>document.startViewTransition</c> around a C# DOM-update callback.</summary>
    public async ValueTask<bool> StartViewTransitionAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(DotNetObjectReference<T> dotnetRef, string callbackName) where T : class
        => await (await Module()).InvokeAsync<bool>("startViewTransition", dotnetRef, callbackName);

    /// <summary>Wraps <c>document.startViewTransition({ update, types })</c> around a C# DOM-update callback.</summary>
    public async ValueTask<bool> StartViewTransitionAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(DotNetObjectReference<T> dotnetRef, string callbackName, string[] types) where T : class
        => await (await Module()).InvokeAsync<bool>("startViewTransition", dotnetRef, callbackName, types);

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
