namespace Boilerplate.Client.Core.Components.Common;

/// <summary>
/// Republishes the page data when a bit Brouter keep-alive route is revealed again.
///
/// <para>
/// A <c>KeepAlive</c> route (see <c>Routes.razor</c>) is not disposed when you navigate away - it is hidden and then
/// revealed with all its state intact. This component's dedupe state is part of that state, so on the way back
/// <c>OnParametersSet</c> either does not run at all or hits the "same message as last time" early return, and
/// nothing is published. The Header is not re-created either - it lives in MainLayout, which sits outside the router
/// and subscribes exactly once - so it would go on showing the title and back button of the page the user just left.
/// </para>
///
/// <para>
/// This is not routed content the router instantiates, so it registers a handler on the cascaded route context
/// rather than implementing the interface for auto-discovery. See <c>AppPageBase.BrouterLifecycles.cs</c>, which
/// does the same thing for the page itself.
/// </para>
/// </summary>
public partial class AppPageData : IBrouterRoute, IDisposable
{
    [CascadingParameter] private BrouterRouteContext? routeContext { get; set; }
    private bool brouterRegistered;

    public ValueTask OnActivatedAsync(BrouterRouteActivation activation)
    {
        // The first activation is the ordinary mount, which OnParametersSet has already published for.
        if (activation.IsFirstActivation is false)
        {
            _lastPublishedMessage = null;
            Publish();
        }

        return ValueTask.CompletedTask;
    }

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

    public void Dispose()
    {
        if (brouterRegistered)
        {
            routeContext?.Unregister(this);
            brouterRegistered = false;
        }

        GC.SuppressFinalize(this);
    }
}
