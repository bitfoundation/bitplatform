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

    private Func<BmScrollInfo, Task>? _onScroll;
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
    /// Progress (0–1) of the tracked target element between the configured offsets, or
    /// <c>null</c> when no target is tracked (see <see cref="BmScrollOptions"/>).
    /// </summary>
    public double? TargetProgress { get; private set; }

    // ── Motion-value projections (motion.dev's useScroll return values) ──────
    // Created lazily; once touched they update on every scroll event, so they compose with
    // Transform(...) / Motion.Spring(...) and bind to elements via the Bmotion Values parameter.
    private BmValue<double>? _progressXValue, _progressYValue, _scrollXValue, _scrollYValue, _targetProgressValue;

    /// <summary>Horizontal scroll progress (0–1) as a composable motion value.</summary>
    public BmValue<double> ProgressXValue => _progressXValue ??= Bm.Value(ProgressX);

    /// <summary>Vertical scroll progress (0–1) as a composable motion value.</summary>
    public BmValue<double> ProgressYValue => _progressYValue ??= Bm.Value(ProgressY);

    /// <summary>Horizontal pixel offset as a composable motion value.</summary>
    public BmValue<double> ScrollXValue => _scrollXValue ??= Bm.Value(ScrollX);

    /// <summary>Vertical pixel offset as a composable motion value.</summary>
    public BmValue<double> ScrollYValue => _scrollYValue ??= Bm.Value(ScrollY);

    /// <summary>Tracked target progress (0–1) as a composable motion value.</summary>
    public BmValue<double> TargetProgressValue
        => _targetProgressValue ??= Bm.Value(TargetProgress ?? 0);

    /// <summary>Start observing window scroll events.</summary>
    public Task ObserveAsync(Func<BmScrollInfo, Task> onChange)
        => ObserveAsync(new BmScrollOptions(), onChange);

    /// <summary>Start observing window scroll events (synchronous callback).</summary>
    public Task ObserveAsync(Action<BmScrollInfo> onChange)
    {
        ArgumentNullException.ThrowIfNull(onChange);
        return ObserveAsync(info => { onChange(info); return Task.CompletedTask; });
    }

    /// <summary>Start observing scroll events on the given container element.</summary>
    /// <param name="containerId">HTML element id of the scroll container.</param>
    /// <param name="onChange">Callback invoked on every scroll event.</param>
    public Task ObserveAsync(string containerId, Func<BmScrollInfo, Task> onChange)
        => ObserveAsync(new BmScrollOptions { ContainerId = containerId }, onChange);

    /// <summary>Synchronous overload.</summary>
    public Task ObserveAsync(string containerId, Action<BmScrollInfo> onChange)
    {
        ArgumentNullException.ThrowIfNull(onChange);
        return ObserveAsync(containerId, info => { onChange(info); return Task.CompletedTask; });
    }

    /// <summary>
    /// Start observing with full options - including tracking a target element's progress
    /// through the container between two scroll offsets (motion.dev's <c>useScroll</c> with
    /// <c>target</c>/<c>offset</c>). The tracked value is reported via
    /// <see cref="BmScrollInfo.TargetProgress"/> / <see cref="TargetProgressValue"/>.
    /// </summary>
    public async Task ObserveAsync(BmScrollOptions options, Func<BmScrollInfo, Task> onChange)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(onChange);
        // Remove any existing subscription so only one stays active.
        foreach (var existing in _subscriptionKeys)
            await _interop.UnobserveScrollAsync(existing);
        _subscriptionKeys.Clear();

        _onScroll = onChange;
        var key = await _interop.ObserveScrollAsync(options.ContainerId, Dotnet, options.ToJsObject());
        if (key != null) _subscriptionKeys.Add(key);
    }

    /// <summary>Synchronous overload.</summary>
    public Task ObserveAsync(BmScrollOptions options, Action<BmScrollInfo> onChange)
    {
        ArgumentNullException.ThrowIfNull(onChange);
        return ObserveAsync(options, info => { onChange(info); return Task.CompletedTask; });
    }

    // ── JS → C# callback ─────────────────────────────────────────────────────

    [JSInvokable]
    public async Task OnScroll(BmScrollInfo info)
    {
        // info crosses the JS→C# boundary; guard against a null payload so the property reads below
        // don't throw a NullReferenceException inside the interop callback.
        if (info is null) return;
        ProgressX = info.ProgressX;
        ProgressY = info.ProgressY;
        ScrollX   = info.ScrollX;
        ScrollY   = info.ScrollY;
        TargetProgress = info.TargetProgress;

        // Push into any materialized motion values so derived values / element bindings update.
        _progressXValue?.SetSync(info.ProgressX);
        _progressYValue?.SetSync(info.ProgressY);
        _scrollXValue?.SetSync(info.ScrollX);
        _scrollYValue?.SetSync(info.ScrollY);
        if (info.TargetProgress is { } targetProgress)
            _targetProgressValue?.SetSync(targetProgress);
        else
            // Keep the motion value consistent with TargetProgressValue's null → 0 convention
            // instead of leaving a stale last value when tracking reports no target.
            _targetProgressValue?.SetSync(0);
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
