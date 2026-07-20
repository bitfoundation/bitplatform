namespace Boilerplate.Client.Core.Components.Pages;

/// <summary>
/// bit Brouter route-lifecycle hooks for pages deriving from <see cref="AppPageBase"/>.
///
/// <para>
/// Brouter treats routing as more than "match a URL and mount a component". A route can be marked
/// <c>KeepAlive</c> (see <c>Routes.razor</c>): instead of being torn down when you navigate away, the
/// page's component instance is retained and merely <em>hidden</em>, then <em>revealed</em> again on
/// return - so scroll position, grid paging/sorting/filtering, unsaved input, in-flight state, etc. all
/// survive the round-trip. And whether a page is kept alive or not, Brouter gives it a much richer
/// lifecycle than Blazor's bare <c>OnInitialized</c>/<c>Dispose</c> pair.
/// </para>
/// </summary>
public abstract partial class AppPageBase : IBrouterRoute
{
    /// <summary>
    /// <inheritdoc cref="IBrouterRoute.OnActivatedAsync"/>
    /// </summary>
    protected virtual async ValueTask OnActivated(BrouterRouteActivation activation) { }
    public async ValueTask OnActivatedAsync(BrouterRouteActivation activation)
    {
        try
        {
            await OnActivated(activation);
        }
        catch (Exception exp)
        {
            ExceptionHandler.Handle(exp);
        }
    }

    /// <summary>
    /// <inheritdoc cref="IBrouterRoute.OnDeactivatedAsync"/>
    /// </summary>
    protected virtual async ValueTask OnDeactivated(BrouterRouteDeactivation deactivation) { }
    public async ValueTask OnDeactivatedAsync(BrouterRouteDeactivation deactivation)
    {
        try
        {
            await OnDeactivated(deactivation);
        }
        catch (Exception exp)
        {
            ExceptionHandler.Handle(exp);
        }
    }

    /// <summary>
    /// <inheritdoc cref="IBrouterRoute.OnDeactivatingAsync"/>
    /// </summary>
    protected virtual async ValueTask OnDeactivating(BrouterRouteDeactivatingContext context) { }
    public async ValueTask OnDeactivatingAsync(BrouterRouteDeactivatingContext context)
    {
        try
        {
            await OnDeactivating(context);
        }
        catch (Exception exp)
        {
            ExceptionHandler.Handle(exp);
        }
    }

    /// <summary>
    /// <inheritdoc cref="IBrouterRoute.OnRenavigatedAsync"/>
    /// </summary>
    protected virtual async ValueTask OnRenavigated(BrouterRouteRenavigation renavigation) { }
    public async ValueTask OnRenavigatedAsync(BrouterRouteRenavigation renavigation)
    {
        try
        {
            await OnRenavigated(renavigation);
        }
        catch (Exception exp)
        {
            ExceptionHandler.Handle(exp);
        }
    }

    /// <summary>
    /// <inheritdoc cref="IBrouterRoute.OnRenavigatingAsync"/>
    /// </summary>
    protected virtual async ValueTask OnRenavigating(BrouterRouteRenavigatingContext context) { }
    public async ValueTask OnRenavigatingAsync(BrouterRouteRenavigatingContext context)
    {
        try
        {
            await OnRenavigating(context);
        }
        catch (Exception exp)
        {
            ExceptionHandler.Handle(exp);
        }
    }

    // The following code enables Brouter route-lifecycle hooks for pages inheriting from AppPageBase.

    [CascadingParameter] private BrouterRouteContext? routeContext { get; set; }
    private bool brouterRegistered;

    public override Task SetParametersAsync(ParameterView parameters)
    {
        var task = base.SetParametersAsync(parameters);

        if (brouterRegistered is false && routeContext is not null)
        {
            routeContext.Register(this);
            brouterRegistered = true;
        }

        return task;
    }

    protected override ValueTask DisposeAsync(bool disposing)
    {
        if (brouterRegistered)
        {
            routeContext?.Unregister(this);
            brouterRegistered = false;
        }

        return base.DisposeAsync(disposing);
    }
}
