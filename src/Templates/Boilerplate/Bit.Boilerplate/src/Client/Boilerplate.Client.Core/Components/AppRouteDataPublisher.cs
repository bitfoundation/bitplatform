
namespace Boilerplate.Client.Core.Components;

public partial class AppRouteDataPublisher : AppComponentBase
{
    [Parameter] public RouteData? RouteData { get; set; }

    protected override Task OnInitAsync()
    {
        PubSubService.Publish(ClientPubSubMessages.ROUTE_DATA_UPDATED, RouteData);

        return base.OnInitAsync();
    }
}
