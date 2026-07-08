namespace Bit.Bmotion;

/// <summary>
/// Queryable, DI-injected view of what the current Blazor render mode can animate. Use it to make
/// degradation on Blazor Server explicit instead of silently collapsing to instant state changes:
/// <code>
/// [Inject] BmotionCapabilities Caps { get; set; } = default!;
///
/// @if (!Caps.SupportsFrameLoop) { &lt;text&gt;Drag/inertia are disabled on Server.&lt;/text&gt; }
/// </code>
/// </summary>
public sealed class BmotionCapabilities
{
    internal BmotionCapabilities(IBmotionInterop interop)
    {
        ArgumentNullException.ThrowIfNull(interop);
        SupportsFrameLoop = interop.IsInProcess;
    }

    /// <summary>
    /// <c>true</c> on Blazor WebAssembly, where the synchronous per-frame rAF loop drives drag,
    /// inertia, color/dimension interpolation, keyframe arrays and motion values. <c>false</c> on
    /// Blazor Server, where those features degrade to instant state changes.
    /// </summary>
    public bool SupportsFrameLoop { get; }

    /// <summary>
    /// <c>true</c> everywhere: compositor-eligible tween/spring animations on transform and opacity
    /// offload to the browser's Web Animations API over async interop, so they play on Server too.
    /// </summary>
    public bool SupportsCompositor => true;
}
