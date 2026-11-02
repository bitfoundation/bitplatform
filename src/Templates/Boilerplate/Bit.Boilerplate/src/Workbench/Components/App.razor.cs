using Microsoft.AspNetCore.Components.Web;

namespace Boilerplate.Workbench.Components;

/// <summary>
/// The host page of every request: the shell, the flows board and the /preview pages its iframes load.
/// </summary>
public partial class App
{
    private static readonly IComponentRenderMode RenderMode = new InteractiveServerRenderMode(prerender: true);

    [Inject] private IStringLocalizer<AppStrings> AppLocalizer { get; set; } = default!;

    [CascadingParameter] public HttpContext HttpContext { get; set; } = default!;
}
