using Microsoft.JSInterop;

namespace Bit.Bmotion;
/// <summary>
/// Tracks scroll progress (0–1) for a container element or the window.
/// Analogous to Framer Motion's <c>useScroll</c>.
///
/// <para>
/// <b>Lifetime / disposal:</b> this service is registered <c>Transient</c> and is meant to be
/// owned by a single component. When injected with <c>@inject</c>, Blazor resolves it from the
/// root scope and only disposes it at app shutdown - so the <b>consuming component must dispose it
/// explicitly</b> (implement <see cref="IAsyncDisposable"/> and call <see cref="DisposeAsync"/> in
/// the component's <c>DisposeAsync</c>), otherwise its JS scroll subscription and
/// <c>DotNetObjectReference</c> leak until the app ends. The reference is created lazily, so an
/// injected-but-unused tracker holds no JS resources.
/// </para>
///
/// Usage:
/// <code>
/// @implements IAsyncDisposable
/// @inject BmotionScrollTracker Scroll
///
/// protected override async Task OnAfterRenderAsync(bool firstRender)
/// {
///     if (firstRender) await Scroll.ObserveAsync(null, info => scrollY = info.ProgressY);
/// }
///
/// public ValueTask DisposeAsync() => Scroll.DisposeAsync();
/// </code>
/// </summary>
public sealed class BmotionScrollTracker : IAsyncDisposable
{
    private readonly BmotionInterop _interop;
    private readonly List<string> _subscriptionKeys = new();
    private DotNetObjectReference<BmotionScrollTracker>? _dotnet;

    private Func<BmotionScrollInfo, Task>? _onScroll;
    private bool _disposed;

    public BmotionScrollTracker(BmotionInterop interop)
    {
        ArgumentNullException.ThrowIfNull(interop);
        _interop = interop;
    }

    // Created on first use so an injected-but-unused tracker doesn't allocate a JS-object reference.
    private DotNetObjectReference<BmotionScrollTracker> Dotnet
        => _dotnet ??= DotNetObjectReference.Create(this);
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
    public async Task ObserveAsync(string? containerId, Func<BmotionScrollInfo, Task> onChange)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(onChange);
        // Remove any existing subscription so only one stays active.
        foreach (var existing in _subscriptionKeys)
            await _interop.UnobserveScrollAsync(existing);
        _subscriptionKeys.Clear();

        _onScroll = onChange;
        var key = await _interop.ObserveScrollAsync(containerId, Dotnet);
        if (key != null) _subscriptionKeys.Add(key);
    }

    /// <summary>Synchronous overload.</summary>
    public Task ObserveAsync(string? containerId, Action<BmotionScrollInfo> onChange)
    {
        ArgumentNullException.ThrowIfNull(onChange);
        return ObserveAsync(containerId, info => { onChange(info); return Task.CompletedTask; });
    }

    // ── JS → C# callback ─────────────────────────────────────────────────────

    [JSInvokable]
    public async Task OnScroll(BmotionScrollInfo info)
    {
        // info crosses the JS→C# boundary; guard against a null payload so the property reads below
        // don't throw a NullReferenceException inside the interop callback.
        if (info is null) return;
        ProgressX = info.ProgressX;
        ProgressY = info.ProgressY;
        ScrollX   = info.ScrollX;
        ScrollY   = info.ScrollY;
        if (_onScroll != null)
        {
            // Guard the user callback so a faulting handler can't fault the JS-invokable flow
            // (which would destabilise the interop bridge / host circuit).
            try { await _onScroll(info); }
            catch { /* swallow user-callback failures to keep the scroll bridge alive */ }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            foreach (var key in _subscriptionKeys)
                await _interop.UnobserveScrollAsync(key);
        }
        finally
        {
            // Always release local resources, even if a JS unobserve call faults during teardown,
            // so the DotNetObjectReference and callback don't leak.
            _subscriptionKeys.Clear();
            _onScroll = null;
            _dotnet?.Dispose();
            // Note: BmotionInterop itself is DI-scoped and disposed by the DI container
        }
    }
}
