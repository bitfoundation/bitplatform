//+:cnd:noEmit
namespace Boilerplate.Client.Core.Components;

public partial class AppRouteDataPublisher : AppComponentBase
{
    [Parameter] public RouteData? RouteData { get; set; }

    //#if (brouter == true)
    private RouteData? lastPublishedRouteData;
    protected override async Task OnParamsSetAsync()
    {
        await base.OnParamsSetAsync();

        // RouteData is a fresh instance per navigation; skip redundant publishes on plain re-renders.
        if (ReferenceEquals(lastPublishedRouteData, RouteData)) return;
        lastPublishedRouteData = RouteData;

        PubSubService.Publish(ClientAppMessages.ROUTE_DATA_UPDATED, RouteData);
    }
    //#else
    //#if (IsInsideProjectTemplate == true)
    /*
    //#endif
    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        PubSubService.Publish(ClientAppMessages.ROUTE_DATA_UPDATED, RouteData);
    }
        //#if (IsInsideProjectTemplate == true)
    */
    //#endif
    //#endif
}
