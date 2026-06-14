using Bit.Bmotion.Interop;
using Bit.Bmotion.Models;
using Microsoft.JSInterop;

namespace Bit.Bmotion.Services;

/// <summary>
/// Tracks scroll progress (0–1) for a container element or the window.
/// Analogous to Framer Motion's <c>useScroll</c>.
///
/// Usage:
/// <code>
/// @inject ScrollTracker Scroll
///
/// protected override async Task OnAfterRenderAsync(bool firstRender)
/// {
///     if (firstRender) await Scroll.ObserveAsync(null, info => scrollY = info.ProgressY);
/// }
/// </code>
/// </summary>
public sealed class ScrollTracker : IAsyncDisposable
{
    private readonly MotionInterop _interop;
    private readonly List<string> _subscriptionKeys = new();
    private readonly DotNetObjectReference<ScrollTracker> _dotnet;

    private Func<ScrollInfo, Task>? _onScroll;
    private bool _disposed;

    public ScrollTracker(MotionInterop interop)
    {
        _interop = interop;
        _dotnet  = DotNetObjectReference.Create(this);
    }
    /// <summary>Horizontal scroll progress 0–1.</summary>
    public double ProgressX { get; private set; }

    /// <summary>Vertical scroll progress 0–1.</summary>
    public double ProgressY { get; private set; }

    /// <summary>Raw pixel scroll offset.</summary>
    public double ScrollX { get; private set; }
    public double ScrollY { get; private set; }

    /// <summary>
    /// Start observing scroll events on the given container (or the window if null).
    /// </summary>
    /// <param name="containerId">HTML element id, or null for window.</param>
    /// <param name="onChange">Callback invoked on every scroll event.</param>
    public async Task ObserveAsync(string? containerId, Func<ScrollInfo, Task> onChange)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        // Remove any existing subscription so only one stays active.
        foreach (var existing in _subscriptionKeys)
            await _interop.UnobserveScrollAsync(existing);
        _subscriptionKeys.Clear();

        _onScroll = onChange;
        var key = await _interop.ObserveScrollAsync(containerId, _dotnet!);
        if (key != null) _subscriptionKeys.Add(key);
    }

    /// <summary>Synchronous overload.</summary>
    public Task ObserveAsync(string? containerId, Action<ScrollInfo> onChange)
        => ObserveAsync(containerId, info => { onChange(info); return Task.CompletedTask; });

    // ── JS → C# callback ─────────────────────────────────────────────────────

    [JSInvokable]
    public async Task OnScroll(ScrollInfo info)
    {
        ProgressX = info.ProgressX;
        ProgressY = info.ProgressY;
        ScrollX   = info.ScrollX;
        ScrollY   = info.ScrollY;
        if (_onScroll != null)
            await _onScroll(info);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var key in _subscriptionKeys)
            await _interop.UnobserveScrollAsync(key);
        _subscriptionKeys.Clear();
        _onScroll = null;
        _dotnet?.Dispose();
        // Note: MotionInterop itself is DI-scoped and disposed by the DI container
    }
}
