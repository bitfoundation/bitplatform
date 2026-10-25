namespace Bit.Bmotion;

/// <summary>
/// Queryable, DI-injected view of what the current Blazor render mode can animate. Use it to make
/// degradation on Blazor Server explicit instead of silently collapsing to instant state changes:
/// <code>
/// [Inject] BmotionCapabilities Caps { get; set; } = default!;
///
/// @if (!Caps.IsPrerendering &amp;&amp; !Caps.SupportsFrameLoop) { &lt;text&gt;Drag/inertia are disabled on Server.&lt;/text&gt; }
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
    /// <c>true</c> on Blazor WebAssembly, where the synchronous per-frame rAF loop drives drag,
    /// inertia, color/dimension interpolation, keyframe arrays and motion values. <c>false</c> on
    /// Blazor Server, where those features degrade to instant state changes.
    /// <para>
    /// While <see cref="IsPrerendering"/> this describes the server doing the prerendering - always
    /// <c>false</c> - not the runtime that takes over afterwards, so check that first.
    /// </para>
    /// </summary>
    public bool SupportsFrameLoop { get; }

    /// <summary>
    /// <c>true</c> everywhere: compositor-eligible tween/spring animations on transform and opacity
    /// offload to the browser's Web Animations API over async interop, so they play on Server too.
    /// </summary>
    public bool SupportsCompositor => true;

    /// <summary>
    /// <c>true</c> while the page is being prerendered or statically rendered on the server: nothing
    /// animates in this pass (see <see cref="BmPrerenderMode"/> for what elements render instead), and
    /// the other flags describe the server rather than the runtime that takes over once interactive.
    /// </summary>
    public bool IsPrerendering => _interop.IsPrerendering;
}
