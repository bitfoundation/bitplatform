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
/// </summary>
public sealed class BmotionViewTransition : IAsyncDisposable
{
    private readonly IBmotionInterop _interop;
    private DotNetObjectReference<BmotionViewTransition>? _dotnet;
    private Func<Task>? _pending;
    private bool _inProgress;

    public BmotionViewTransition(IBmotionInterop interop)
    {
        ArgumentNullException.ThrowIfNull(interop);
        _interop = interop;
    }

    /// <summary>
    /// Runs <paramref name="updateDom"/> inside a native view transition where supported.
    /// Returns <c>true</c> when the native API drove the transition, <c>false</c> when it isn't
    /// supported and the update ran without one.
    /// </summary>
    public async ValueTask<bool> StartAsync(Func<Task> updateDom)
    {
        ArgumentNullException.ThrowIfNull(updateDom);
        // One transition at a time: _pending is shared state read back by RunUpdateAsync, so an
        // overlapping StartAsync would clobber the in-flight callback. Reject rather than corrupt.
        if (_inProgress)
            throw new InvalidOperationException(
                "A view transition is already in progress; await the previous StartAsync before starting another.");
        _inProgress = true;
        _pending = updateDom;
        _dotnet ??= DotNetObjectReference.Create(this);
        try
        {
            return await _interop.StartViewTransitionAsync(_dotnet, nameof(RunUpdateAsync));
        }
        finally
        {
            _pending = null;
            _inProgress = false;
        }
    }

    /// <summary>Synchronous-callback overload of <see cref="StartAsync(Func{Task})"/>.</summary>
    public ValueTask<bool> StartAsync(Action updateDom)
    {
        ArgumentNullException.ThrowIfNull(updateDom);
        return StartAsync(() => { updateDom(); return Task.CompletedTask; });
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
