using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Diagnostics.CodeAnalysis;

namespace Bit.Bmotion;

/// <summary>
/// Test/DI seam over the browser-API bridge in <c>bit-bmotion.js</c>. Every animation-engine and
/// component interaction with JS flows through this interface, so tests can inject a scriptable
/// fake that records interop calls and simulates JS→.NET callbacks without a real browser.
/// The production implementation is <see cref="BmotionInterop"/>, registered in
/// <see cref="BitBmotion.AddBitBmotionServices(Microsoft.Extensions.DependencyInjection.IServiceCollection)"/>.
/// </summary>
public interface IBmotionInterop : IAsyncDisposable
{
    /// <summary>
    /// <c>true</c> when the JS runtime supports synchronous interop (Blazor WebAssembly). The
    /// per-frame rAF engine and drag handlers rely on synchronous JS↔.NET calls; on Server this
    /// is <c>false</c>: animations play as Web Animations (timelines or recorded frames) over
    /// async interop, and what only the frame loop can play lands at once.
    /// </summary>
    bool IsInProcess { get; }

    /// <summary>
    /// <c>true</c> while the components are being prerendered or statically rendered on the server:
    /// no browser is attached, so no interop call can succeed. Defaults to <c>false</c>.
    /// </summary>
    bool IsPrerendering => false;

    /// <summary>
    /// How long, in ms, the CSS animation of the prerendered element with id
    /// <paramref name="elementId"/> has been running, read synchronously while the runtime renders
    /// the markup that replaces it; <c>null</c> when that isn't possible (only WebAssembly has
    /// synchronous interop) or nothing is playing on it any more. Defaults to <c>null</c>.
    /// </summary>
    double? GetPrerenderedAnimationElapsedMs(string elementId) => null;

    // Every generic T below propagates DotNetObjectReference<T>'s trim annotation so trimmed/AOT
    // apps keep T's public ([JSInvokable]) methods for the JS→.NET callbacks.

    // ── rAF loop ──────────────────────────────────────────────────────────────
    ValueTask StartRafLoopAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(DotNetObjectReference<T> dotnetRef) where T : class;
    ValueTask StopRafLoopAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(DotNetObjectReference<T>? dotnetRef = null) where T : class;

    // ── Reduced motion (accessibility) ────────────────────────────────────────
    ValueTask<bool> PrefersReducedMotionAsync();
    ValueTask WatchReducedMotionAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(DotNetObjectReference<T> dotnetRef) where T : class;
    ValueTask UnwatchReducedMotionAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(DotNetObjectReference<T> dotnetRef) where T : class;

    // ── Style application ─────────────────────────────────────────────────────
    ValueTask ApplyStylesAsync(string elementId, object styles);
    ValueTask PopLayoutAsync(string elementId, double currentX, double currentY);
    ValueTask UnpopLayoutAsync(string elementId);

    /// <summary>
    /// Makes an exiting element <c>inert</c> (<paramref name="inert"/> <c>true</c>) or, when its exit is cancelled,
    /// interactive again - removing only an <c>inert</c> it added. Defaults to a no-op.
    /// </summary>
    ValueTask SetInertAsync(string elementId, bool inert) => ValueTask.CompletedTask;

    // ── Keyboard reordering and announcements ─────────────────────────────────
    /// <summary>
    /// Lets a keyboard user reorder the item: Space/Enter on it calls <c>OnReorderPickUp</c> / <c>OnReorderDrop</c>
    /// on <paramref name="dotnetRef"/>, the arrow keys along <paramref name="axis"/> <c>OnReorderMove</c> while it is
    /// held (<c>x</c>, <c>y</c>, or <c>both</c> for a grid, whose up and down keys call <c>OnReorderMoveRow</c>), and
    /// Escape <c>OnReorderCancel</c>. Makes the item focusable when it isn't. Defaults to a no-op.
    /// </summary>
    ValueTask AttachReorderKeysAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(
        string elementId, DotNetObjectReference<T> dotnetRef, string axis, string? handleSelector) where T : class
        => ValueTask.CompletedTask;

    /// <summary>Moves focus to the element (without scrolling it into view). Defaults to a no-op.</summary>
    ValueTask FocusAsync(string elementId) => ValueTask.CompletedTask;

    /// <summary>Has screen readers announce <paramref name="text"/> through a polite live region. Defaults to a no-op.</summary>
    ValueTask AnnounceAsync(string text) => ValueTask.CompletedTask;

    // ── Element registration ──────────────────────────────────────────────────
    ValueTask<bool> RegisterElementAsync(string elementId);
    ValueTask UnregisterElementAsync(string elementId);

    // ── Gesture event listeners ───────────────────────────────────────────────
    ValueTask AttachEventListenersAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(string elementId, object events, DotNetObjectReference<T> dotnetRef) where T : class;
    ValueTask StartDragAsync(string elementId, long pointerId, double clientX, double clientY);

    // ── Viewport observation ──────────────────────────────────────────────────
    ValueTask ObserveViewportAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(string elementId, DotNetObjectReference<T> dotnetRef, bool once) where T : class;
    ValueTask ObserveViewportWithOptionsAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(string elementId, DotNetObjectReference<T> dotnetRef, BmViewport options) where T : class;
    ValueTask UnobserveViewportAsync(string elementId);

    // ── FLIP layout ───────────────────────────────────────────────────────────
    /// <summary>
    /// Measures the element for a FLIP comparison. <paramref name="options"/> selects the
    /// coordinate space so a snapshot and the measurement it is compared against agree (see
    /// <see cref="BmotionMeasureOptions"/>).
    /// </summary>
    ValueTask<BmotionBoundingRect?> GetBoundingRectAsync(string elementId, BmotionMeasureOptions options = default);

    /// <summary>
    /// <see cref="GetBoundingRectAsync"/> for the snapshot taken before a re-render, which also holds the element
    /// where it is through that render until its FLIP arrives - or <see cref="ReleaseLayoutHoldAsync"/>. Without
    /// synchronous interop the FLIP is a round trip behind the render, and the element would show at its new place
    /// in between. The hold measures the new box the way the FLIP will: with <paramref name="translate"/> and
    /// <paramref name="scale"/> (the layout mode) about the anchor (<paramref name="anchorX"/>,
    /// <paramref name="anchorY"/>). Defaults to <see cref="GetBoundingRectAsync"/>, holding nothing.
    /// </summary>
    ValueTask<BmotionBoundingRect?> MeasureAndHoldLayoutAsync(string elementId, BmotionMeasureOptions options,
        bool translate, bool scale, double anchorX, double anchorY)
        => GetBoundingRectAsync(elementId, options);

    /// <summary>Lets go of a layout hold (see <see cref="MeasureAndHoldLayoutAsync"/>) whose FLIP didn't play. Defaults to doing nothing.</summary>
    ValueTask ReleaseLayoutHoldAsync(string elementId) => ValueTask.CompletedTask;

    /// <summary>
    /// Plays the FLIP animation. <paramref name="originX"/>/<paramref name="originY"/> are the
    /// projection anchor (0-1 of the element's box) the deltas were measured from, and
    /// <paramref name="dotnetRef"/> - when given - receives <c>OnLayoutAnimationCompleted</c> once
    /// the animation finishes or is superseded.
    /// </summary>
    ValueTask PlayWaapiFlipAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(
        string elementId, double dx, double dy, double sx, double sy, double durationMs, string easingStr,
        string? finalTransform, double originX, double originY, DotNetObjectReference<T>? dotnetRef) where T : class;

    /// <summary>
    /// Crossfades two holders of a LayoutId as the layout animation plays: <paramref name="leadId"/>, the one taking
    /// it over, fades in; <paramref name="followId"/>, the one on its way out, fades out while it travels onto the
    /// lead's box - by (<paramref name="dx"/>, <paramref name="dy"/>) and (<paramref name="sx"/>,
    /// <paramref name="sy"/>) about the anchor (<paramref name="originX"/>, <paramref name="originY"/>) - and stays
    /// there until it is removed. Defaults to doing nothing.
    /// </summary>
    ValueTask PlayLayoutCrossfadeAsync(string leadId, string followId, double dx, double dy, double sx, double sy,
        double durationMs, string easingStr, double originX, double originY) => ValueTask.CompletedTask;

    /// <summary>Takes an element that came back off its crossfade out (see <see cref="PlayLayoutCrossfadeAsync"/>). Defaults to doing nothing.</summary>
    ValueTask CancelLayoutCrossfadeAsync(string elementId) => ValueTask.CompletedTask;

    // ── Reading the page ──────────────────────────────────────────────────────
    /// <summary>
    /// Reads, in one call, the computed values of the <paramref name="current"/> properties (the starting values
    /// the engine has never seen) and what the stylesheet gives the <paramref name="stylesheet"/> properties with
    /// their inline declarations lifted (the values a gesture hands back to it). Property names are the engine's
    /// camelCase keys; <c>d</c>/<c>points</c> read the SVG attribute. Defaults to <c>null</c> (nothing read) for
    /// interop implementations without a browser.
    /// </summary>
    ValueTask<BmotionStyleRead?> ReadStylesAsync(string elementId, string[] current, string[] stylesheet)
        => ValueTask.FromResult<BmotionStyleRead?>(null);

    /// <summary>
    /// <see cref="ReadStylesAsync(string, string[], string[])"/>, and also the natural size of the <paramref name="auto"/>
    /// properties (<c>width</c>/<c>height</c>): their computed value with the inline declaration set to <c>auto</c>
    /// for an instant - what an animation to <c>"auto"</c> plays towards. Defaults to reading without them.
    /// </summary>
    ValueTask<BmotionStyleRead?> ReadStylesAsync(string elementId, string[] current, string[] stylesheet, string[] auto)
        => ReadStylesAsync(elementId, current, stylesheet);

    // ── WAAPI compositor offload ──────────────────────────────────────────────
    ValueTask<bool> SupportsLinearEasingAsync();

    /// <summary>
    /// Whether the browser animates between <c>oklab()</c> colours in OKLab, not just parses them - which lets an
    /// OKLab colour transition play in the browser. Defaults to <c>false</c>.
    /// </summary>
    ValueTask<bool> SupportsOklabColorAsync() => ValueTask.FromResult(false);
    ValueTask<bool> PlayWaapiAnimationAsync(string elementId, int token, object keyframes, object timing);
    ValueTask CancelWaapiAnimationAsync(string elementId, int token, bool commit);

    /// <summary>
    /// Sets the playback rate of every compositor animation playing on the element (1 = realtime,
    /// 0 = paused) - what <c>Pause</c>/<c>SetSpeed</c> do to the animations the browser plays.
    /// Defaults to a no-op for interop implementations without a browser.
    /// </summary>
    ValueTask SetWaapiPlaybackRateAsync(string elementId, double rate) => ValueTask.CompletedTask;

    /// <summary>
    /// Moves every compositor animation playing on the element to <paramref name="playedMs"/> of its own time (delay
    /// included) - a seek. Defaults to a no-op for interop implementations without a browser.
    /// </summary>
    ValueTask SeekWaapiAnimationsAsync(string elementId, double playedMs) => ValueTask.CompletedTask;

    // ── Scroll-driven timelines ───────────────────────────────────────────────
    /// <summary>
    /// Plays a pre-sampled animation progressed by scroll position. <c>true</c> = a native
    /// <c>ScrollTimeline</c>/<c>ViewTimeline</c> drove it, <c>false</c> = the scroll-scrub
    /// fallback did, <c>null</c> = it could not start.
    /// </summary>
    ValueTask<bool?> PlayScrollTimelineAsync(string elementId, int token, object keyframes, object timeline);

    /// <summary>Stops a scroll-driven animation and releases any scroll listener behind it.</summary>
    ValueTask CancelScrollTimelineAsync(string elementId, int token);

    // ── Scroll ────────────────────────────────────────────────────────────────
    ValueTask<string?> ObserveScrollAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(string? containerId, DotNetObjectReference<T> dotnetRef, object? options = null) where T : class;
    ValueTask UnobserveScrollAsync(string key);

    // ── Programmatic animate() API ─────────────────────────────────────────────
    ValueTask<string[]> ResolveOrRegisterBySelectorAsync(string selector);
    ValueTask<string> ResolveOrRegisterByRefAsync(ElementReference elementReference);

    // ── View Transitions API ───────────────────────────────────────────────────
    /// <summary>
    /// Wraps <c>document.startViewTransition</c> around a C# DOM-update callback (invoked by name
    /// on <paramref name="dotnetRef"/>). Returns <c>true</c> when the native API drove the
    /// transition, <c>false</c> when it isn't supported and the callback ran without one.
    /// </summary>
    ValueTask<bool> StartViewTransitionAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(DotNetObjectReference<T> dotnetRef, string callbackName) where T : class;

    /// <summary>
    /// <see cref="StartViewTransitionAsync{T}(DotNetObjectReference{T}, string)"/> with view transition
    /// <paramref name="types"/> (<c>startViewTransition({ update, types })</c>); a browser without typed transitions
    /// plays an untyped one. Defaults to ignoring the types.
    /// </summary>
    ValueTask<bool> StartViewTransitionAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(DotNetObjectReference<T> dotnetRef, string callbackName, string[] types) where T : class
        => StartViewTransitionAsync(dotnetRef, callbackName);
}
