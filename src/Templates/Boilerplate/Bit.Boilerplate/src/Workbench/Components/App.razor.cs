using Microsoft.AspNetCore.Components.Web;

namespace Boilerplate.Workbench.Components;

public partial class App
{
    private static readonly IComponentRenderMode RenderMode = new InteractiveServerRenderMode(prerender: false);

    [CascadingParameter] public HttpContext HttpContext { get; set; } = default!;
}
