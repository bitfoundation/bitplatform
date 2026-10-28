using Microsoft.JSInterop;

namespace Bit.Bmotion;

/// <summary>
/// A thin wrapper over the browser's native <b>View Transitions API</b>
/// (<c>document.startViewTransition</c>) - the standards-based route/shared-element alternative that
/// complements Bmotion's <c>LayoutId</c> FLIP. Inject it and wrap a state change:
/// <code>
/// [Inject] BmotionViewTransition ViewTransition { get; set; } = default!;
///
/// await ViewTransition.StartAsync(() => { _selectedTab = tab; StateHasChanged(); });
/// </code>
/// The browser snapshots the DOM before and after the callback and cross-fades between them
/// (name shared elements with the CSS <c>view-transition-name</c> property to morph them). When the
/// API is unsupported the callback simply runs without a transition, so it degrades cleanly.
/// <para>
/// <b>Reduced motion:</b> the browser plays a view transition whatever the user's motion preference, unless the
/// stylesheet says otherwise - and the slides and zooms people author for them are exactly the motion
/// <c>prefers-reduced-motion</c> asks to avoid. So when the library's policy
/// (<see cref="BitBmotionOptions.ReducedMotion"/>) reduces motion, the update runs without a transition;
/// <see cref="BmViewTransitionOptions.ReduceMotion"/> overrides that per call.
/// </para>
/// </summary>
public sealed class BmotionViewTransition : IAsyncDisposable
{
    private readonly IBmotionInterop _interop;
    private readonly BmotionAnimationEngine? _engine;
    private readonly BitBmotionOptions? _options;
    private DotNetObjectReference<BmotionViewTransition>? _dotnet;
    private Func<Task>? _pending;
    private bool _inProgress;

    public BmotionViewTransition(IBmotionInterop interop)
    {
        ArgumentNullException.ThrowIfNull(interop);
        _interop = interop;
    }

    /// <summary>
    /// The constructor dependency injection uses: with the engine (for the OS motion preference) and the library
    /// options (for the reduced-motion policy).
    /// </summary>
    public BmotionViewTransition(IBmotionInterop interop, BmotionAnimationEngine engine, BitBmotionOptions options)
        : this(interop)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(options);
        _engine = engine;
        _options = options;
    }

    /// <summary>
    /// Runs <paramref name="updateDom"/> inside a native view transition where supported.
    /// Returns <c>true</c> when the native API drove the transition, <c>false</c> when it isn't
    /// supported, or motion is reduced, and the update ran without one.
    /// </summary>
    public ValueTask<bool> StartAsync(Func<Task> updateDom) => StartAsync(updateDom, null);

    /// <summary>
    /// Runs <paramref name="updateDom"/> inside a native view transition, with <paramref name="options"/> (types, a
    /// reduced-motion override). Returns <c>true</c> when the native API drove the transition.
    /// </summary>
    public async ValueTask<bool> StartAsync(Func<Task> updateDom, BmViewTransitionOptions? options)
    {
        ArgumentNullException.ThrowIfNull(updateDom);
        // One transition at a time: _pending is shared state read back by RunUpdateAsync, so an
        // overlapping StartAsync would clobber the in-flight callback. Reject rather than corrupt.
        if (_inProgress)
            throw new InvalidOperationException(
                "A view transition is already in progress; await the previous StartAsync before starting another.");
        _inProgress = true;
        try
        {
            if (await ShouldReduceMotionAsync(options))
            {
                await updateDom();
                return false;
            }

            _pending = updateDom;
            _dotnet ??= DotNetObjectReference.Create(this);
            return options?.Types is { Count: > 0 } types
                ? await _interop.StartViewTransitionAsync(_dotnet, nameof(RunUpdateAsync), [.. types])
                : await _interop.StartViewTransitionAsync(_dotnet, nameof(RunUpdateAsync));
        }
        finally
        {
            _pending = null;
            _inProgress = false;
        }
    }

    /// <summary>Synchronous-callback overload of <see cref="StartAsync(Func{Task})"/>.</summary>
    public ValueTask<bool> StartAsync(Action updateDom) => StartAsync(updateDom, null);

    /// <summary>Synchronous-callback overload of <see cref="StartAsync(Func{Task}, BmViewTransitionOptions?)"/>.</summary>
    public ValueTask<bool> StartAsync(Action updateDom, BmViewTransitionOptions? options)
    {
        ArgumentNullException.ThrowIfNull(updateDom);
        return StartAsync(() => { updateDom(); return Task.CompletedTask; }, options);
    }

    // The library's policy, as BmotionConfig-free code sees it: a service sits outside every <BmotionConfig>, so
    // IgnoreUnlessConfigured (the back-compat default) never reduces here.
    private async ValueTask<bool> ShouldReduceMotionAsync(BmViewTransitionOptions? options)
    {
        if (options?.ReduceMotion is bool explicitly) return explicitly;
        switch (_options?.ReducedMotion)
        {
            case BmReducedMotionMode.Always:
                return true;
            case BmReducedMotionMode.User when _engine is not null:
                await _engine.EnsureReducedMotionDetectedAsync();
                return _engine.OsPrefersReducedMotion;
            default:
                return false;
        }
    }

    /// <summary>Invoked by JS during the transition's DOM-update phase. Not for direct use.</summary>
    [JSInvokable]
    public async Task RunUpdateAsync()
    {
        if (_pending is { } update) await update();
    }

    public ValueTask DisposeAsync()
    {
        _dotnet?.Dispose();
        _dotnet = null;
        return ValueTask.CompletedTask;
    }
}
