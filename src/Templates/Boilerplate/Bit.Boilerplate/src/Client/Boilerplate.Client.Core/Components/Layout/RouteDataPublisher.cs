
namespace Boilerplate.Client.Core.Components.Layout;

public partial class RouteDataPublisher : AppComponentBase
{
    [Parameter] public RouteData? RouteData { get; set; }

    protected override Task OnInitAsync()
    {
        PubSubService.Publish(PubSubMessages.ROUTE_DATA_UPDATED, RouteData);

        return base.OnInitAsync();
    }
}
