using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Bmotion.Tests.TestInfra;

/// <summary>
/// A scriptable, browser-free <see cref="IBmotionInterop"/> for unit/bUnit tests. It records every
/// interop call and captures the <see cref="DotNetObjectReference{T}"/> instances the engine and
/// components hand to JS, so a test can synthesize the JS→.NET callbacks
/// (<c>ComputeFrame</c>, <c>OnPointerEnter</c>, <c>OnIntersect</c>, …) that a real browser would fire.
/// <para>
/// Two views of every captured reference are kept: the target object (<c>…Refs</c>, convenient for
/// invoking callbacks) and the <see cref="DotNetObjectReference{T}"/> itself (<c>…References</c>),
/// because a real bridge only ever holds the latter - so a test can tell when the owner disposed
/// the reference it handed out (see <see cref="IsDisposed"/>).
/// </para>
/// </summary>
internal sealed class FakeBmotionInterop : IBmotionInterop
{
    /// <summary>One recorded interop call: the JS method name and its non-ref arguments.</summary>
    public sealed record Call(string Method, object?[] Args);

    private readonly List<Call> _calls = new();
    private int _scrollKeySeq;

    /// <summary>All interop calls in order, for assertions.</summary>
    public IReadOnlyList<Call> Calls => _calls;

    /// <summary>Simulates WASM (synchronous interop) when true, Blazor Server when false.</summary>
    public bool IsInProcess { get; set; } = true;

    /// <summary>Simulates a prerendering / static-SSR scope (no browser attached) when true.</summary>
    public bool IsPrerendering { get; set; }

    /// <summary>Value returned by <see cref="GetPrerenderedAnimationElapsedMs"/>.</summary>
    public double? PrerenderedAnimationElapsedMs { get; set; }

    /// <summary>The prerendered element ids <see cref="GetPrerenderedAnimationElapsedMs"/> was asked about.</summary>
    public List<string> PrerenderedAnimationQueries { get; } = new();

    public double? GetPrerenderedAnimationElapsedMs(string elementId)
    {
        PrerenderedAnimationQueries.Add(elementId);
        return PrerenderedAnimationElapsedMs;
    }

    /// <summary>
    /// What <see cref="GetWaapiPlayhead"/> answers: where the browser has really got with an offloaded animation, and
    /// whether its start is final. Null (the default) is "can't tell", which leaves the engine on its own clock.
    /// </summary>
    public Func<string, int, (double PlayedMs, bool Final)?>? WaapiPlayhead { get; set; }

    /// <summary>How many times the engine has asked <see cref="GetWaapiPlayhead"/>.</summary>
    public int WaapiPlayheadReads { get; private set; }

    public (double PlayedMs, bool Final)? GetWaapiPlayhead(string elementId, int token)
    {
        WaapiPlayheadReads++;
        return WaapiPlayhead?.Invoke(elementId, token);
    }

    /// <summary>Value returned by <see cref="PrefersReducedMotionAsync"/>.</summary>
    public bool PrefersReducedMotion { get; set; }

    /// <summary>Value returned by <see cref="SupportsLinearEasingAsync"/>.</summary>
    public bool SupportsLinearEasing { get; set; } = true;

    /// <summary>Value returned by <see cref="RegisterElementAsync"/>.</summary>
    public bool RegisterElementResult { get; set; } = true;

    /// <summary>
    /// Value returned by <see cref="PlayScrollTimelineAsync"/>: <c>true</c> = a native scroll
    /// timeline drove it, <c>false</c> = the scroll-scrub fallback, <c>null</c> = could not start.
    /// </summary>
    public bool? ScrollTimelineResult { get; set; } = true;

    /// <summary>Optional rect provider for <see cref="GetBoundingRectAsync"/> keyed by element id.</summary>
    public Func<string, BmotionBoundingRect?>? BoundingRectProvider { get; set; }

    /// <summary>
    /// Resolves a selector to the element ids the bridge would have registered for it. Defaults to a
    /// single <c>"bm-sel-1"</c>; set it to model multi-element (or empty) matches.
    /// </summary>
    public Func<string, string[]> SelectorResolver { get; set; } = static _ => ["bm-sel-1"];

    /// <summary>Resolves an <see cref="ElementReference"/> to an element id. Defaults to <c>"bm-ref-1"</c>.</summary>
    public Func<ElementReference, string> RefResolver { get; set; } = static _ => "bm-ref-1";

    /// <summary>
    /// Hook invoked when the engine starts a WAAPI compositor animation. Return the completion
    /// result (<c>true</c> = natural finish, <c>false</c> = cancelled/failed). Defaults to a pending
    /// task so a test can decide when/if the offload resolves; return a completed task for eager finish.
    /// </summary>
    public Func<string, int, object, object, Task<bool>>? PlayWaapiHandler { get; set; }

    // Captured DotNet references, so tests can invoke [JSInvokable] callbacks directly. The loop ref
    // is cleared on stop, mirroring the bridge dropping the engine from its rAF set.
    public object? RafLoopRef { get; private set; }
    public object? RafLoopReference { get; private set; }
    public object? ReducedMotionRef { get; private set; }
    public object? ReducedMotionReference { get; private set; }
    public object? ViewTransitionReference { get; private set; }
    public readonly Dictionary<string, object> EventListenerRefs = new();
    public readonly Dictionary<string, object> EventListenerReferences = new();
    public readonly Dictionary<string, object> ViewportRefs = new();
    public readonly Dictionary<string, object> ViewportReferences = new();
    public readonly Dictionary<string, object> FlipRefs = new();
    public readonly Dictionary<string, object> FlipReferences = new();
    public readonly Dictionary<string, object> ScrollRefs = new();
    public readonly Dictionary<string, object> ScrollReferences = new();

    private void Record(string method, params object?[] args) => _calls.Add(new Call(method, args));

    /// <summary>True if any recorded call used <paramref name="method"/>.</summary>
    public bool WasCalled(string method) => _calls.Any(c => c.Method == method);

    /// <summary>Number of recorded calls to <paramref name="method"/>.</summary>
    public int CountOf(string method) => _calls.Count(c => c.Method == method);

    /// <summary>The arguments of every recorded call to <paramref name="method"/>, in order.</summary>
    public IReadOnlyList<object?[]> ArgsOf(string method) => _calls.Where(c => c.Method == method).Select(c => c.Args).ToArray();

    /// <summary>
    /// Whether a captured <see cref="DotNetObjectReference{T}"/> has been disposed by its owner. A
    /// disposed reference throws on <c>Value</c>, which is exactly what a JS callback through it
    /// would hit in a real app.
    /// </summary>
    public static bool IsDisposed(object dotnetObjectReference)
    {
        var value = dotnetObjectReference.GetType().GetProperty("Value")
            ?? throw new ArgumentException("Not a DotNetObjectReference.", nameof(dotnetObjectReference));
        try
        {
            value.GetValue(dotnetObjectReference);
            return false;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is ObjectDisposedException)
        {
            return true;
        }
    }

    // ── rAF loop ──────────────────────────────────────────────────────────────
    /// <summary>How many of the next loop starts fail, as a bridge that isn't reachable yet makes them.</summary>
    public int RafLoopStartFailures { get; set; }

    public ValueTask StartRafLoopAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(DotNetObjectReference<T> dotnetRef) where T : class
    {
        if (RafLoopStartFailures > 0)
        {
            RafLoopStartFailures--;
            return ValueTask.FromException(new JSException("the bridge isn't loaded yet"));
        }
        RafLoopRef = dotnetRef.Value;
        RafLoopReference = dotnetRef;
        Record("startRafLoop");
        return ValueTask.CompletedTask;
    }

    public ValueTask StopRafLoopAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(DotNetObjectReference<T>? dotnetRef = null) where T : class
    {
        // The bridge drops the stopped engine (or every engine, for null) from its loop, so nothing
        // may call ComputeFrame through the old ref any more.
        if (dotnetRef is null || ReferenceEquals(dotnetRef, RafLoopReference))
        {
            RafLoopRef = null;
            RafLoopReference = null;
        }
        Record("stopRafLoop");
        return ValueTask.CompletedTask;
    }

    // ── Reduced motion ────────────────────────────────────────────────────────
    /// <summary>
    /// When set, <see cref="PrefersReducedMotionAsync"/> waits for it before answering - a probe
    /// round trip the test holds open (e.g. to unmount a component mid-initialisation).
    /// </summary>
    public Task? PrefersReducedMotionGate { get; set; }

    public async ValueTask<bool> PrefersReducedMotionAsync()
    {
        Record("prefersReducedMotion");
        if (PrefersReducedMotionGate is { } gate) await gate;
        return PrefersReducedMotion;
    }

    public ValueTask WatchReducedMotionAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(DotNetObjectReference<T> dotnetRef) where T : class
    {
        ReducedMotionRef = dotnetRef.Value;
        ReducedMotionReference = dotnetRef;
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

    /// <summary>What each reorder item's keyboard listener calls back into, by element id.</summary>
    public readonly Dictionary<string, object> ReorderKeyRefs = new();

    public ValueTask AttachReorderKeysAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(
        string elementId, DotNetObjectReference<T> dotnetRef, string axis, string? handleSelector) where T : class
    {
        Record("attachReorderKeys", elementId, axis, handleSelector);
        ReorderKeyRefs[elementId] = dotnetRef.Value;
        return ValueTask.CompletedTask;
    }

    public ValueTask DetachReorderKeysAsync(string elementId)
    {
        Record("detachReorderKeys", elementId);
        ReorderKeyRefs.Remove(elementId);
        return ValueTask.CompletedTask;
    }

    public ValueTask FocusAsync(string elementId)
    {
        Record("focusElement", elementId);
        return ValueTask.CompletedTask;
    }

    public ValueTask AnnounceAsync(string text)
    {
        Record("announce", text);
        return ValueTask.CompletedTask;
    }

    public ValueTask SetInertAsync(string elementId, bool inert)
    {
        Record("setInert", elementId, inert);
        return ValueTask.CompletedTask;
    }

    public ValueTask UnpopLayoutAsync(string elementId)
    {
        Record("unpopLayout", elementId);
        return ValueTask.CompletedTask;
    }

    // ── Element registration ──────────────────────────────────────────────────
    /// <summary>
    /// When set, <see cref="RegisterElementAsync"/> waits for it before answering, so a test can
    /// hold a component inside its initialisation while something else happens (an unmount).
    /// </summary>
    public Task? RegisterElementGate { get; set; }

    public async ValueTask<bool> RegisterElementAsync(string elementId)
    {
        Record("registerElement", elementId);
        if (RegisterElementGate is { } gate) await gate;
        return RegisterElementResult;
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
        EventListenerReferences[elementId] = dotnetRef;
        Record("attachEventListeners", elementId, events);
        return ValueTask.CompletedTask;
    }

    public ValueTask StartDragAsync(string elementId, long pointerId, double clientX, double clientY)
    {
        Record("startDrag", elementId, pointerId, clientX, clientY);
        return ValueTask.CompletedTask;
    }

    // ── Viewport observation ──────────────────────────────────────────────────
    // Both overloads record a BmViewport, so assertions read the effective options the same way
    // whichever overload the component picked (the bridge receives the same options object shape).
    public ValueTask ObserveViewportAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(string elementId, DotNetObjectReference<T> dotnetRef, bool once) where T : class
    {
        ViewportRefs[elementId] = dotnetRef.Value;
        ViewportReferences[elementId] = dotnetRef;
        Record("observeViewport", elementId, new BmViewport { Once = once });
        return ValueTask.CompletedTask;
    }

    public ValueTask ObserveViewportWithOptionsAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(string elementId, DotNetObjectReference<T> dotnetRef, BmViewport options) where T : class
    {
        ViewportRefs[elementId] = dotnetRef.Value;
        ViewportReferences[elementId] = dotnetRef;
        Record("observeViewport", elementId, options);
        return ValueTask.CompletedTask;
    }

    public ValueTask UnobserveViewportAsync(string elementId)
    {
        Record("unobserveViewport", elementId);
        return ValueTask.CompletedTask;
    }

    // ── FLIP layout ───────────────────────────────────────────────────────────
    /// <summary>
    /// When set, <see cref="GetBoundingRectAsync"/> waits for it before measuring - the round trip
    /// every measurement is on Blazor Server, held open so a test controls when it lands.
    /// </summary>
    public Task? BoundingRectGate { get; set; }

    public async ValueTask<BmotionBoundingRect?> GetBoundingRectAsync(string elementId, BmotionMeasureOptions options = default)
    {
        Record("getBoundingRect", elementId, options);
        if (BoundingRectGate is { } gate) await gate;
        return BoundingRectProvider?.Invoke(elementId);
    }

    public ValueTask PlayWaapiFlipAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(
        string elementId, double dx, double dy, double sx, double sy, double durationMs, string easingStr,
        string? finalTransform, double originX, double originY, DotNetObjectReference<T>? dotnetRef) where T : class
    {
        if (dotnetRef is not null)
        {
            FlipRefs[elementId] = dotnetRef.Value;
            FlipReferences[elementId] = dotnetRef;
        }
        Record("playWaapiFlip", elementId, dx, dy, sx, sy, durationMs, easingStr, finalTransform, originX, originY);
        return ValueTask.CompletedTask;
    }

    public async ValueTask<BmotionBoundingRect?> MeasureAndHoldLayoutAsync(string elementId, BmotionMeasureOptions options,
        bool translate, bool scale, double anchorX, double anchorY)
    {
        Record("measureAndHoldLayout", elementId, options, translate, scale, anchorX, anchorY);
        if (BoundingRectGate is { } gate) await gate;
        return BoundingRectProvider?.Invoke(elementId);
    }

    public ValueTask ReleaseLayoutHoldAsync(string elementId)
    {
        Record("releaseLayoutHold", elementId);
        return ValueTask.CompletedTask;
    }

    public ValueTask PlayLayoutCrossfadeAsync(string leadId, string followId, double dx, double dy, double sx, double sy,
        double durationMs, string easingStr, double originX, double originY)
    {
        Record("playLayoutCrossfade", leadId, followId, dx, dy, sx, sy, durationMs, easingStr, originX, originY);
        return ValueTask.CompletedTask;
    }

    public ValueTask CancelLayoutCrossfadeAsync(string elementId)
    {
        Record("cancelLayoutCrossfade", elementId);
        return ValueTask.CompletedTask;
    }

    // ── Reading the page ──────────────────────────────────────────────────────
    /// <summary>
    /// The computed style the fake page shows: <c>(elementId, property) → value</c>, or <c>null</c> for a property
    /// it has no value for. Defaults to nothing read.
    /// </summary>
    public Func<string, string, string?>? ComputedStyle { get; set; }

    /// <summary>What the fake stylesheet gives a property with its inline declaration lifted. Defaults to <see cref="ComputedStyle"/>.</summary>
    public Func<string, string, string?>? StylesheetStyle { get; set; }

    /// <summary>When set, <see cref="ReadStylesAsync"/> waits for it - the round trip a read is on Blazor Server, held open.</summary>
    public Task? ReadStylesGate { get; set; }

    /// <summary>The natural (<c>auto</c>) size of a property: <c>(elementId, property) → value</c>. Defaults to nothing read.</summary>
    public Func<string, string, string?>? AutoStyle { get; set; }

    public async ValueTask<BmotionStyleRead?> ReadStylesAsync(string elementId, string[] current, string[] stylesheet)
    {
        Record("readStyles", elementId, current, stylesheet);
        if (ReadStylesGate is { } gate) await gate;
        return Read(elementId, current, stylesheet, []);
    }

    public async ValueTask<BmotionStyleRead?> ReadStylesAsync(string elementId, string[] current, string[] stylesheet, string[] auto)
    {
        Record("readStyles", elementId, current, stylesheet, auto);
        if (ReadStylesGate is { } gate) await gate;
        return Read(elementId, current, stylesheet, auto);
    }

    private BmotionStyleRead? Read(string elementId, string[] current, string[] stylesheet, string[] auto)
    {
        if (ComputedStyle is null && StylesheetStyle is null && AutoStyle is null) return null;
        Dictionary<string, string> Collect(string[] keys, Func<string, string, string?>? source)
        {
            var read = new Dictionary<string, string>();
            foreach (var key in keys)
                if (source?.Invoke(elementId, key) is { } value) read[key] = value;
            return read;
        }
        return new BmotionStyleRead
        {
            Current = Collect(current, ComputedStyle),
            Stylesheet = Collect(stylesheet, StylesheetStyle ?? ComputedStyle),
            Auto = Collect(auto, AutoStyle),
        };
    }

    // ── WAAPI compositor offload ──────────────────────────────────────────────
    public ValueTask<bool> SupportsLinearEasingAsync()
    {
        Record("supportsLinearEasing");
        return ValueTask.FromResult(SupportsLinearEasing);
    }

    /// <summary>Value returned by <see cref="SupportsOklabColorAsync"/>.</summary>
    public bool SupportsOklab { get; set; } = true;

    public ValueTask<bool> SupportsOklabColorAsync() => ValueTask.FromResult(SupportsOklab);

    /// <summary>A compositor animation the fake browser is "playing" (default handler only).</summary>
    public sealed record WaapiAnimation(string ElementId, int Token, TaskCompletionSource<bool> Completion);

    /// <summary>Every compositor animation started through the default handler, in start order.</summary>
    public List<WaapiAnimation> WaapiAnimations { get; } = new();

    /// <summary>
    /// <c>false</c> models a browser without the Web Animations API: every animation the engine hands it fails to
    /// start, and the engine plays it on the frame loop instead - which is how the tests of runtime-independent
    /// semantics (gesture reverts, playback control, frame output) drive the frame loop by hand.
    /// </summary>
    public bool WebAnimationsSupported { get; set; } = true;

    public ValueTask<bool> PlayWaapiAnimationAsync(string elementId, int token, object keyframes, object timing)
    {
        Record("playWaapiAnimation", elementId, token, keyframes, timing);
        if (!WebAnimationsSupported) return ValueTask.FromResult(false);
        if (PlayWaapiHandler is { } handler)
            return new ValueTask<bool>(handler(elementId, token, keyframes, timing));

        // Default: never resolves on its own (mirrors a still-running compositor animation) until
        // the test finishes it (FinishWaapi) or the engine cancels it. Continuations run inline,
        // which keeps the engine on one logical thread the way single-threaded WASM does.
        var animation = new WaapiAnimation(elementId, token, new TaskCompletionSource<bool>());
        WaapiAnimations.Add(animation);
        return new ValueTask<bool>(animation.Completion.Task);
    }

    /// <summary>
    /// Lets every still-playing compositor animation on <paramref name="elementId"/> (all elements
    /// when null) run to its natural end. Returns how many were finished.
    /// </summary>
    public int FinishWaapi(string? elementId = null)
    {
        var finished = 0;
        foreach (var animation in WaapiAnimations.ToArray())
        {
            if (elementId is not null && animation.ElementId != elementId) continue;
            if (animation.Completion.TrySetResult(true)) finished++;
        }
        return finished;
    }

    public ValueTask CancelWaapiAnimationAsync(string elementId, int token, bool commit)
    {
        Record("cancelWaapiAnimation", elementId, token, commit);
        // A cancelled Web Animation's play promise resolves false - the bridge's documented contract.
        foreach (var animation in WaapiAnimations.Where(a => a.ElementId == elementId && a.Token == token).ToArray())
            animation.Completion.TrySetResult(false);
        return ValueTask.CompletedTask;
    }

    // Implemented (rather than left to the interface's no-op default) so a test can see the rate
    // the engine handed the browser's compositor animations.
    public ValueTask SeekWaapiAnimationsAsync(string elementId, double playedMs)
    {
        Record("seekWaapiAnimations", elementId, playedMs);
        return ValueTask.CompletedTask;
    }

    public ValueTask SetWaapiPlaybackRateAsync(string elementId, double rate)
    {
        Record("setWaapiPlaybackRate", elementId, rate);
        return ValueTask.CompletedTask;
    }

    // ── Scroll-driven timelines ───────────────────────────────────────────────
    public ValueTask<bool?> PlayScrollTimelineAsync(string elementId, int token, object keyframes, object timeline)
    {
        Record("playScrollTimelineAnimation", elementId, token, keyframes, timeline);
        return ValueTask.FromResult(ScrollTimelineResult);
    }

    public ValueTask CancelScrollTimelineAsync(string elementId, int token)
    {
        Record("cancelScrollTimelineAnimation", elementId, token);
        return ValueTask.CompletedTask;
    }

    // ── Scroll ────────────────────────────────────────────────────────────────
    /// <summary>
    /// When set, <see cref="ObserveScrollAsync"/> waits for it before answering, so a test can hold
    /// a subscription mid-setup while the tracker is disposed.
    /// </summary>
    public Task? ObserveScrollGate { get; set; }

    public async ValueTask<string?> ObserveScrollAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(string? containerId, DotNetObjectReference<T> dotnetRef, object? options = null) where T : class
    {
        var key = "scroll-key-" + (++_scrollKeySeq);
        // Keyed by subscription so a test can fire OnScroll through the exact ref JS would hold.
        ScrollRefs[key] = dotnetRef.Value;
        ScrollReferences[key] = dotnetRef;
        Record("observeScroll", containerId, options, key);
        if (ObserveScrollGate is { } gate) await gate;
        return key;
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
        return ValueTask.FromResult(SelectorResolver(selector));
    }

    public ValueTask<string> ResolveOrRegisterByRefAsync(ElementReference elementReference)
    {
        Record("resolveOrRegisterByRef", elementReference);
        return ValueTask.FromResult(RefResolver(elementReference));
    }

    /// <summary>Simulates native support for the View Transitions API (false = fallback path).</summary>
    public bool SupportsViewTransitions { get; set; }

    public async ValueTask<bool> StartViewTransitionAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(DotNetObjectReference<T> dotnetRef, string callbackName) where T : class
    {
        ViewTransitionReference = dotnetRef;
        Record("startViewTransition", callbackName);
        // Simulate the browser invoking the C# DOM-update callback during the transition. JS can
        // only reach a method through its [JSInvokable] identifier (the method name unless one is
        // given), so resolve it the same way - a callback name the bridge couldn't invoke must fail
        // here rather than silently "work" through plain reflection.
        var method = FindJsInvokable(typeof(T), callbackName)
            ?? throw new InvalidOperationException(
                $"'{typeof(T).Name}' has no [JSInvokable] method with identifier '{callbackName}'.");
        if (method.Invoke(dotnetRef.Value, null) is Task task) await task;
        return SupportsViewTransitions;
    }

    /// <summary>The view transition types the last typed <c>startViewTransition</c> was given.</summary>
    public string[]? ViewTransitionTypes { get; private set; }

    public ValueTask<bool> StartViewTransitionAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(DotNetObjectReference<T> dotnetRef, string callbackName, string[] types) where T : class
    {
        ViewTransitionTypes = types;
        return StartViewTransitionAsync(dotnetRef, callbackName);
    }

    private static MethodInfo? FindJsInvokable([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type, string identifier)
        => type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .SingleOrDefault(m => m.GetCustomAttribute<JSInvokableAttribute>() is { } attr
                                  && (attr.Identifier ?? m.Name) == identifier);

    public ValueTask DisposeAsync()
    {
        Record("dispose");
        return ValueTask.CompletedTask;
    }
}
