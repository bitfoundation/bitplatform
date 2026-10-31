using Microsoft.AspNetCore.Components.Web;

namespace Boilerplate.Workbench.Components;

public partial class App
{
    private static readonly IComponentRenderMode RenderMode = new InteractiveServerRenderMode(prerender: true);

    [CascadingParameter] public HttpContext HttpContext { get; set; } = default!;
}
