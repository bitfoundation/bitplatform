namespace Bit.Bmotion;

/// <summary>
/// Queryable, DI-injected view of what the current Blazor render mode can animate. The browser plays almost every
/// animation on every render mode; use this for the few things that need the per-frame loop, so their absence on
/// Blazor Server is explicit rather than a silent snap:
/// <code>
/// [Inject] BmotionCapabilities Caps { get; set; } = default!;
///
/// @if (!Caps.IsPrerendering &amp;&amp; !Caps.SupportsFrameLoop) { &lt;text&gt;The shape morph lands at once on Server.&lt;/text&gt; }
/// </code>
/// </summary>
public sealed class BmotionCapabilities
{
    private readonly IBmotionInterop _interop;

    internal BmotionCapabilities(IBmotionInterop interop)
    {
        ArgumentNullException.ThrowIfNull(interop);
        _interop = interop;
        SupportsFrameLoop = interop.IsInProcess;
    }

    /// <summary>
    /// <c>true</c> on Blazor WebAssembly, where the synchronous per-frame loop runs. <c>false</c> on Blazor Server,
    /// where what only it can play lands on its target at once: per-frame callbacks (<c>OnUpdate</c>, <c>OnFrame</c>)
    /// and SVG <c>d</c>/<c>points</c> morphs. Everything else - transforms, opacity, colours, sizes, filters,
    /// keyframes, springs, inertia, arcs - the browser plays on both, and drag follows the pointer a round trip
    /// behind on Server.
    /// <para>
    /// While <see cref="IsPrerendering"/> this describes the server doing the prerendering - always
    /// <c>false</c> - not the runtime that takes over afterwards, so check that first.
    /// </para>
    /// </summary>
    public bool SupportsFrameLoop { get; }

    /// <summary>
    /// <c>true</c> everywhere: animations the browser can reproduce exactly are handed to its Web Animations API over
    /// async interop - as a two-keyframe timeline, or recorded frame by frame - so they play on Server too.
    /// </summary>
    public bool SupportsCompositor => true;

    /// <summary>
    /// <c>true</c> while the page is being prerendered or statically rendered on the server: nothing
    /// animates in this pass (see <see cref="BmPrerenderMode"/> for what elements render instead), and
    /// the other flags describe the server rather than the runtime that takes over once interactive.
    /// </summary>
    public bool IsPrerendering => _interop.IsPrerendering;
}
