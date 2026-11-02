using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using static Microsoft.AspNetCore.Components.Web.RenderMode;

namespace Bit.Bmotion.Samples.Wasm.Components;

public partial class App
{
    [CascadingParameter] private HttpContext HttpContext { get; set; } = default!;

    // Interactive everywhere except the error page, which renders statically.
    private IComponentRenderMode? PageRenderMode => HttpContext.AcceptsInteractiveRouting() ? InteractiveWebAssembly : null;
}
