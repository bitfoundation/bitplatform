namespace Bit.Brouter;

/// <summary>
/// Optional convenience base class for routed components (and any of their descendants) that want
/// the route lifecycle without wiring it by hand: it consumes the cascaded
/// <see cref="BrouterRouteContext"/>, registers itself as an <see cref="IBrouterRoute"/> handler,
/// unregisters on dispose, and exposes the callbacks as <c>OnActivated</c>/<c>OnActivatedAsync</c>
/// style virtuals mirroring <c>ComponentBase</c>'s own lifecycle pairs. After an activation or
/// renavigation completes, <c>StateHasChanged</c> is called automatically (a page revealed again
/// almost always needs a repaint); deactivation deliberately does not re-render - hidden or dying
/// content should not repaint.
/// </summary>
/// <remarks>
/// Works on every render path (<see cref="Broute.Component"/>, <see cref="Broute.Content"/>
/// fragments, <see cref="Brouter.Found"/>-rendered pages) and at any depth under the routed
/// content, because it relies on the cascade rather than on router instantiation. Components that
/// can't afford the base class implement <see cref="IBrouterRoute"/> directly instead: router-
/// instantiated page components are then discovered automatically, and anything else registers by
/// hand on the cascaded context. Outside routed content (no cascade), all callbacks are simply
/// never invoked and <see cref="IsActive"/> reports <c>true</c>.
/// </remarks>
public abstract class BrouterRouteBase : ComponentBase, IBrouterRoute, IDisposable
{
    [CascadingParameter] private BrouterRouteContext? RouteContext { get; set; }

    private bool _registered;
    private bool _disposed;

    /// <summary>
    /// Whether this component's route content is currently the active (visible) routed content.
    /// <c>true</c> when rendered outside a route (no context to report otherwise).
    /// </summary>
    protected bool IsActive => RouteContext?.IsActive ?? true;

    /// <inheritdoc/>
    public override Task SetParametersAsync(ParameterView parameters)
    {
        var task = base.SetParametersAsync(parameters);

        // The cascading parameter is assigned synchronously inside base.SetParametersAsync, so the
        // context - a stable, fixed cascade - is available here on the very first parameter set.
        if (_registered is false && RouteContext is not null)
        {
            RouteContext.Register(this);
            _registered = true;
        }

        return task;
    }

    /// <summary>Synchronous activation callback; see <see cref="IBrouterRoute.OnActivatedAsync"/>.</summary>
    protected virtual void OnActivated(BrouterRouteActivation activation) { }

    /// <summary>Asynchronous activation callback; see <see cref="IBrouterRoute.OnActivatedAsync"/>.</summary>
    protected virtual Task OnActivatedAsync(BrouterRouteActivation activation) => Task.CompletedTask;

    /// <summary>Synchronous deactivation callback; see <see cref="IBrouterRoute.OnDeactivatedAsync"/>.</summary>
    protected virtual void OnDeactivated(BrouterRouteDeactivation deactivation) { }

    /// <summary>Asynchronous deactivation callback; see <see cref="IBrouterRoute.OnDeactivatedAsync"/>.</summary>
    protected virtual Task OnDeactivatedAsync(BrouterRouteDeactivation deactivation) => Task.CompletedTask;

    /// <summary>Synchronous renavigation callback; see <see cref="IBrouterRoute.OnRenavigatedAsync"/>.</summary>
    protected virtual void OnRenavigated(BrouterRouteRenavigation renavigation) { }

    /// <summary>Asynchronous renavigation callback; see <see cref="IBrouterRoute.OnRenavigatedAsync"/>.</summary>
    protected virtual Task OnRenavigatedAsync(BrouterRouteRenavigation renavigation) => Task.CompletedTask;

    /// <summary>Synchronous navigation-lock callback; see <see cref="IBrouterRoute.OnDeactivatingAsync"/>.</summary>
    protected virtual void OnDeactivating(BrouterRouteDeactivatingContext context) { }

    /// <summary>
    /// Asynchronous navigation-lock callback; see <see cref="IBrouterRoute.OnDeactivatingAsync"/>.
    /// Awaited by the navigation pipeline: the pending navigation is held open until it completes,
    /// so it may await a custom confirmation dialog before calling <c>context.Cancel()</c>.
    /// </summary>
    protected virtual Task OnDeactivatingAsync(BrouterRouteDeactivatingContext context) => Task.CompletedTask;

    /// <summary>Synchronous renavigation-lock callback; see <see cref="IBrouterRoute.OnRenavigatingAsync"/>.</summary>
    protected virtual void OnRenavigating(BrouterRouteRenavigatingContext context) { }

    /// <summary>
    /// Asynchronous renavigation-lock callback; see <see cref="IBrouterRoute.OnRenavigatingAsync"/>.
    /// Awaited by the navigation pipeline, like <see cref="OnDeactivatingAsync(BrouterRouteDeactivatingContext)"/>.
    /// </summary>
    protected virtual Task OnRenavigatingAsync(BrouterRouteRenavigatingContext context) => Task.CompletedTask;

    async ValueTask IBrouterRoute.OnActivatedAsync(BrouterRouteActivation activation)
    {
        OnActivated(activation);
        await OnActivatedAsync(activation);
        // Reactivated pages almost always changed state (resumed work, refreshed data) - repaint.
        // Runs on the renderer's dispatcher: lifecycle callbacks are invoked there, and the await
        // above resumes on the captured context. The component may have been disposed or
        // deactivated again while the async callback was in flight - dead or hidden content must
        // not repaint.
        if (_disposed is false && IsActive) StateHasChanged();
    }

    async ValueTask IBrouterRoute.OnDeactivatedAsync(BrouterRouteDeactivation deactivation)
    {
        OnDeactivated(deactivation);
        await OnDeactivatedAsync(deactivation);
        // No StateHasChanged: hidden content shouldn't repaint (on Blazor Server that's a diff over
        // the wire for a page nobody sees), and Disposing content is about to unmount anyway.
    }

    async ValueTask IBrouterRoute.OnRenavigatedAsync(BrouterRouteRenavigation renavigation)
    {
        OnRenavigated(renavigation);
        await OnRenavigatedAsync(renavigation);
        // Same guard as the activation path: skip the repaint for disposed/deactivated content.
        if (_disposed is false && IsActive) StateHasChanged();
    }

    async ValueTask IBrouterRoute.OnDeactivatingAsync(BrouterRouteDeactivatingContext context)
    {
        OnDeactivating(context);
        await OnDeactivatingAsync(context);
        // No automatic StateHasChanged: the navigation hasn't been decided yet, and a lock that
        // shows a confirmation dialog repaints itself as part of showing it.
    }

    async ValueTask IBrouterRoute.OnRenavigatingAsync(BrouterRouteRenavigatingContext context)
    {
        OnRenavigating(context);
        await OnRenavigatingAsync(context);
    }

    /// <summary>Override to add teardown; the base unregisters the lifecycle handler.</summary>
    protected virtual void Dispose(bool disposing) { }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_registered)
        {
            RouteContext?.Unregister(this);
        }

        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
