using Microsoft.AspNetCore.Components;

namespace Bit.BlazorUI.Demo.Server.Components;

[StreamRendering(enabled: true)]
public partial class App
{
    [CascadingParameter] HttpContext HttpContext { get; set; } = default!;
}
