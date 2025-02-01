using Microsoft.AspNetCore.Components;

namespace Bit.BlazorUI.Demo.Server.Components;

#if !DEBUG
[StreamRendering(enabled: true)]
#endif
public partial class App
{
    [CascadingParameter] HttpContext HttpContext { get; set; } = default!;
}
