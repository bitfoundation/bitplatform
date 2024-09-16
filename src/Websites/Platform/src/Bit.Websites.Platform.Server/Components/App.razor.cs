using Microsoft.AspNetCore.Components;

namespace Bit.Websites.Platform.Server.Components;

[StreamRendering(enabled: true)]
public partial class App
{
    [CascadingParameter] HttpContext HttpContext { get; set; } = default!;
}
