using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;

namespace Bit.Bmotion.Demo.Server.Services;

/// <summary>
/// A browser-free <see cref="IBmotionInterop"/> that lets the MCP tools run the real Bit.Bmotion
/// engine on the server, with no DOM anywhere.
/// <para>
/// It is the reason <c>SimulateBmotionTransition</c> and <c>AnalyzeBmotionAnimation</c> can answer
/// with numbers instead of adjectives: the engine takes exactly the decisions it takes in a
/// browser - whether an animation can be handed to the compositor, how a spring is sampled, how
/// long it keeps ticking - and this fake records them rather than forwarding them to JavaScript.
/// The alternative would be a second implementation of the physics living in the demo, which is
/// the one thing guaranteed to drift away from the library it documents.
/// </para>
/// <para>
/// <see cref="IsInProcess"/> reports <c>true</c>, which is what the engine reads to decide that a
/// per-frame loop exists (Blazor WebAssembly). Frames are then advanced by calling the engine's
/// <c>ComputeFrame</c> directly - see <see cref="BmotionMotionLab"/> - instead of by a real
/// <c>requestAnimationFrame</c> ticker, so a simulation runs as fast as the CPU allows rather than
/// in real time.
/// </para>
/// </summary>
internal sealed class HeadlessBmotionInterop : IBmotionInterop
{
    /// <summary>One compositor (Web Animations API) hand-off the engine attempted.</summary>
    /// <param name="ElementId">The element the engine offloaded.</param>
    /// <param name="Keyframes">The pre-sampled CSS keyframes, as handed to the browser.</param>
    /// <param name="Timing">The WAAPI timing object: duration, delay, easing, iterations, direction.</param>
    internal sealed record WaapiCall(string ElementId, object Keyframes, object Timing);

    private readonly List<WaapiCall> _waapiCalls = [];
    private int _elementSeq;

    /// <summary>Every compositor offload the engine asked for, in order. Empty means it stayed on the frame loop.</summary>
    public IReadOnlyList<WaapiCall> WaapiCalls => _waapiCalls;

    /// <summary>
    /// Whether the browser is pretended to support CSS <c>linear()</c> easing. Springs can only be
    /// offloaded to the compositor when it does, so turning this off is how a simulation asks what
    /// happens on a browser that lacks it.
    /// </summary>
    public bool SupportsLinearEasing { get; init; } = true;

    /// <summary>What <see cref="PrefersReducedMotionAsync"/> answers - the OS accessibility setting.</summary>
    public bool PrefersReducedMotion { get; init; }

    /// <summary>
    /// <c>true</c>: the engine believes a synchronous per-frame loop is available, as on Blazor
    /// WebAssembly. Set it to <c>false</c> to observe the Blazor Server behaviour instead, where
    /// everything that needs the loop collapses to an instant state change.
    /// </summary>
    public bool IsInProcess { get; init; } = true;

    // -- The frame loop --------------------------------------------------------
    // Deliberately inert: a real ticker would race the simulation, which advances the clock itself
    // so it can sample at a chosen resolution and stop the moment the animation settles.

    public ValueTask StartRafLoopAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(DotNetObjectReference<T> dotnetRef) where T : class
        => ValueTask.CompletedTask;

    public ValueTask StopRafLoopAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(DotNetObjectReference<T>? dotnetRef = null) where T : class
        => ValueTask.CompletedTask;

    // -- Reduced motion --------------------------------------------------------

    public ValueTask<bool> PrefersReducedMotionAsync() => ValueTask.FromResult(PrefersReducedMotion);

    public ValueTask WatchReducedMotionAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(DotNetObjectReference<T> dotnetRef) where T : class
        => ValueTask.CompletedTask;

    public ValueTask UnwatchReducedMotionAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(DotNetObjectReference<T> dotnetRef) where T : class
        => ValueTask.CompletedTask;

    // -- Styles and elements ---------------------------------------------------
    // The engine also returns each frame's styles from ComputeFrame, which is where the simulation
    // reads them; there is nothing for this method to write them to.

    public ValueTask ApplyStylesAsync(string elementId, object styles) => ValueTask.CompletedTask;

    public ValueTask PopLayoutAsync(string elementId, double currentX, double currentY) => ValueTask.CompletedTask;

    public ValueTask UnpopLayoutAsync(string elementId) => ValueTask.CompletedTask;

    public ValueTask<bool> RegisterElementAsync(string elementId) => ValueTask.FromResult(true);

    public ValueTask UnregisterElementAsync(string elementId) => ValueTask.CompletedTask;

    // -- Gestures and observers ------------------------------------------------
    // Nothing here can fire without a pointer, a viewport or a scroll container, none of which
    // exist on the server. They are inert rather than throwing, so an animation that merely
    // declares a gesture can still be analysed.

    public ValueTask AttachEventListenersAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(string elementId, object events, DotNetObjectReference<T> dotnetRef) where T : class
        => ValueTask.CompletedTask;

    public ValueTask StartDragAsync(string elementId, long pointerId, double clientX, double clientY)
        => ValueTask.CompletedTask;

    public ValueTask ObserveViewportAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(string elementId, DotNetObjectReference<T> dotnetRef, bool once) where T : class
        => ValueTask.CompletedTask;

    public ValueTask ObserveViewportWithOptionsAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(string elementId, DotNetObjectReference<T> dotnetRef, BmViewport options) where T : class
        => ValueTask.CompletedTask;

    public ValueTask UnobserveViewportAsync(string elementId) => ValueTask.CompletedTask;

    // -- Layout (FLIP) ---------------------------------------------------------
    // No element has a box on the server, so a FLIP measurement has no answer. Null is what the
    // real bridge reports for an element that is not in the document.

    public ValueTask<BmotionBoundingRect?> GetBoundingRectAsync(string elementId, BmotionMeasureOptions options = default)
        => ValueTask.FromResult<BmotionBoundingRect?>(null);

    public ValueTask PlayWaapiFlipAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(
        string elementId, double dx, double dy, double sx, double sy, double durationMs, string easingStr,
        string? finalTransform, double originX, double originY, DotNetObjectReference<T>? dotnetRef) where T : class
        => ValueTask.CompletedTask;

    // -- Compositor offload ----------------------------------------------------

    public ValueTask<bool> SupportsLinearEasingAsync() => ValueTask.FromResult(SupportsLinearEasing);

    /// <summary>
    /// Records the hand-off and reports it as played. That the engine called this at all is the
    /// answer <c>AnalyzeBmotionAnimation</c> is after: an animation the compositor can own plays on
    /// Blazor Server too, because it needs one async interop call and no frame loop.
    /// </summary>
    public ValueTask<bool> PlayWaapiAnimationAsync(string elementId, int token, object keyframes, object timing)
    {
        _waapiCalls.Add(new WaapiCall(elementId, keyframes, timing));

        return ValueTask.FromResult(true);
    }

    public ValueTask CancelWaapiAnimationAsync(string elementId, int token, bool commit) => ValueTask.CompletedTask;

    // -- Scroll ----------------------------------------------------------------

    public ValueTask<bool?> PlayScrollTimelineAsync(string elementId, int token, object keyframes, object timeline)
        => ValueTask.FromResult<bool?>(true);

    public ValueTask CancelScrollTimelineAsync(string elementId, int token) => ValueTask.CompletedTask;

    public ValueTask<string?> ObserveScrollAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(string? containerId, DotNetObjectReference<T> dotnetRef, object? options = null) where T : class
        => ValueTask.FromResult<string?>(null);

    public ValueTask UnobserveScrollAsync(string key) => ValueTask.CompletedTask;

    // -- Element resolution ----------------------------------------------------

    /// <summary>
    /// Hands out a fresh synthetic id per call. A selector matches nothing on the server, and
    /// returning no ids would make every programmatic animation a no-op - which would look
    /// identical to an animation the engine decided not to run, and defeat the whole simulation.
    /// </summary>
    public ValueTask<string[]> ResolveOrRegisterBySelectorAsync(string selector)
        => ValueTask.FromResult<string[]>([NextElementId()]);

    public ValueTask<string> ResolveOrRegisterByRefAsync(ElementReference elementReference)
        => ValueTask.FromResult(NextElementId());

    // -- View transitions ------------------------------------------------------

    public ValueTask<bool> StartViewTransitionAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(DotNetObjectReference<T> dotnetRef, string callbackName) where T : class
        => ValueTask.FromResult(false);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private string NextElementId() => $"bm-headless-{++_elementSeq}";
}
