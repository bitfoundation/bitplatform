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
/// browser - whether the browser can play an animation, as a two-keyframe timeline or as a
/// recording, how a spring is sampled, how long it keeps ticking - and this fake records them
/// rather than forwarding them to JavaScript. The alternative would be a second implementation of
/// the physics living in the demo, which is the one thing guaranteed to drift away from the
/// library it documents.
/// </para>
/// <para>
/// <see cref="IsInProcess"/> reports <c>true</c> by default, which is what the engine reads to
/// decide that a per-frame loop exists (Blazor WebAssembly). Frames are then advanced by calling
/// the engine's <c>ComputeFrame</c> directly - see <see cref="BmotionMotionLab"/> - instead of by a
/// real <c>requestAnimationFrame</c> ticker, so a simulation runs as fast as the CPU allows rather
/// than in real time.
/// </para>
/// </summary>
internal sealed class HeadlessBmotionInterop : IBmotionInterop
{
    /// <summary>One Web Animation the engine handed the browser.</summary>
    /// <param name="ElementId">The element it plays on.</param>
    /// <param name="Keyframes">
    /// The CSS keyframes, as handed to the browser: two for a timeline (one on Blazor Server, where
    /// the browser starts from what is on screen), and more for a recording - each of those carrying
    /// its own <c>offset</c>.
    /// </param>
    /// <param name="Timing">
    /// The timing object: duration, delay, easing (the curve itself for a timeline, <c>linear</c>
    /// for a recording), iterations and direction.
    /// </param>
    internal sealed record WaapiCall(string ElementId, object Keyframes, object Timing);

    private readonly List<WaapiCall> _waapiCalls = [];
    private int _elementSeq;

    /// <summary>Every Web Animation the engine handed the browser, in order. Empty means none: the frame loop played it, or it landed at once.</summary>
    public IReadOnlyList<WaapiCall> WaapiCalls => _waapiCalls;

    /// <summary>
    /// Whether the browser is pretended to support CSS <c>linear()</c> easing. A spring, or a curve
    /// no CSS keyword describes, plays as a timeline only when it does, so turning this off is how a
    /// simulation asks what happens on a browser that lacks it.
    /// </summary>
    public bool SupportsLinearEasing { get; init; } = true;

    /// <summary>
    /// Whether the browser is pretended to support <c>oklab()</c> colours. A colour transition in
    /// <see cref="BmColorSpace.Oklab"/> plays as a timeline only where it does - the browser then
    /// mixes it in OKLab itself - and is recorded elsewhere. Every current browser does, so this is
    /// on unless a simulation asks otherwise.
    /// </summary>
    public bool SupportsOklabColor { get; init; } = true;

    /// <summary>
    /// What the page reports as the computed value of each property the engine has never seen,
    /// keyed by the engine's camelCase names (<c>opacity</c>, <c>width</c>, <c>backgroundColor</c>,
    /// <c>--glow</c>).
    /// <para>
    /// The engine reads these off the computed style before an animation starts (see
    /// <see cref="ReadStylesAsync"/>), and whether the browser can play the animation depends on the
    /// starting value as much as on the target: a length in the same unit at both ends, a filter
    /// with the same functions. Left unanswered, every such property would start from nothing,
    /// which the engine can only jump from - and a simulation would report a jump that no page with
    /// a stylesheet makes.
    /// </para>
    /// </summary>
    public IReadOnlyDictionary<string, string>? ComputedStyles { get; init; }

    /// <summary>What <see cref="PrefersReducedMotionAsync"/> answers - the OS accessibility setting.</summary>
    public bool PrefersReducedMotion { get; init; }

    /// <summary>
    /// <c>true</c>: the engine believes a synchronous per-frame loop is available, as on Blazor
    /// WebAssembly. Set it to <c>false</c> to observe Blazor Server instead, where there is no frame
    /// loop: the engine hands the browser every animation it can, and only what the frame loop
    /// alone can play collapses to an instant state change.
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

    // -- Reading the page ------------------------------------------------------

    /// <summary>
    /// Answers from <see cref="ComputedStyles"/>, for the starting values and for the values a
    /// gesture hands back to the stylesheet alike: there is one stylesheet here, and it gives each
    /// property the value it lists. A property it does not list goes unanswered, as it would for a
    /// page whose stylesheet never mentions it.
    /// </summary>
    public ValueTask<BmotionStyleRead?> ReadStylesAsync(string elementId, string[] current, string[] stylesheet)
    {
        return ValueTask.FromResult<BmotionStyleRead?>(new BmotionStyleRead
        {
            Current = Read(current),
            Stylesheet = Read(stylesheet)
        });

        Dictionary<string, string> Read(string[] keys)
        {
            var values = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var key in keys)
            {
                if (ComputedStyles?.TryGetValue(key, out var value) is true) values[key] = value;
            }

            return values;
        }
    }

    // -- Browser playback (Web Animations API) ---------------------------------

    public ValueTask<bool> SupportsLinearEasingAsync() => ValueTask.FromResult(SupportsLinearEasing);

    public ValueTask<bool> SupportsOklabColorAsync() => ValueTask.FromResult(SupportsOklabColor);

    /// <summary>
    /// Records the hand-off and reports it as played. That the engine called this at all is half of
    /// what <c>AnalyzeBmotionAnimation</c> is after: an animation the browser is handed plays on
    /// Blazor Server too, because it needs one async interop call and no frame loop. The keyframes
    /// and timing are the other half - they are what tells a timeline from a recording.
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
