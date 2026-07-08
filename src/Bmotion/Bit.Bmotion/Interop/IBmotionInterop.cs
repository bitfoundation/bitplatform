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
    /// is <c>false</c> and only the async compositor (WAAPI) path is available.
    /// </summary>
    bool IsInProcess { get; }

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
    ValueTask<BmotionBoundingRect?> GetBoundingRectAsync(string elementId);
    ValueTask PlayWaapiFlipAsync(string elementId, double dx, double dy, double sx, double sy, double durationMs, string easingStr, string? finalTransform);

    // ── WAAPI compositor offload ──────────────────────────────────────────────
    ValueTask<bool> SupportsLinearEasingAsync();
    ValueTask<bool> PlayWaapiAnimationAsync(string elementId, int token, object keyframes, object timing);
    ValueTask CancelWaapiAnimationAsync(string elementId, int token, bool commit);

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
}
