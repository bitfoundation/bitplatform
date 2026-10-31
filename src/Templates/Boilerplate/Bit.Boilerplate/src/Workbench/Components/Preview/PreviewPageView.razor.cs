using RouteData = Microsoft.AspNetCore.Components.RouteData;

namespace Boilerplate.Workbench.Components.Preview;

public partial class PreviewPageView
{
    private RouteData routeData = default!;

    [Parameter] public WorkbenchPageInfo Page { get; set; } = default!;

    protected override void OnParametersSet()
    {
        if (routeData?.PageType != Page.ComponentType)
        {
            routeData = new(Page.ComponentType, new Dictionary<string, object?>());
        }
    }
}
