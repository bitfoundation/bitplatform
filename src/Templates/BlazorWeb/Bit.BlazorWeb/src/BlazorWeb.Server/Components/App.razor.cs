using Microsoft.AspNetCore.Components;

namespace BlazorWeb.Server.Components;

[StreamRendering(enabled: true)]
public partial class App
{
    [CascadingParameter] HttpContext HttpContext { get; set; } = default!;
}
