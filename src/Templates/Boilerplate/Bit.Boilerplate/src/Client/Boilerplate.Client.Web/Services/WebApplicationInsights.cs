using BlazorApplicationInsights;

namespace Boilerplate.Client.Web.Services;

public partial class WebApplicationInsights : ApplicationInsights
{
    [AutoInject] public IServiceProvider ServiceProvider { get; set; }
}
