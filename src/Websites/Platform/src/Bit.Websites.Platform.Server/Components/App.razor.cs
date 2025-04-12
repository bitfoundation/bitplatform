using Microsoft.AspNetCore.Components;

namespace Bit.Websites.Platform.Server.Components;

public partial class App
{
    [CascadingParameter] HttpContext HttpContext { get; set; } = default!;
}
