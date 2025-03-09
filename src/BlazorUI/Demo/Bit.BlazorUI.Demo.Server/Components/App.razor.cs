using Microsoft.AspNetCore.Components;

namespace Bit.BlazorUI.Demo.Server.Components;

public partial class App
{
    [CascadingParameter] HttpContext HttpContext { get; set; } = default!;
}
