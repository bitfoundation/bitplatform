using Microsoft.AspNetCore.Components;

namespace Boilerplate.Server.Web.Components;

/// <summary>
/// Paints the visitor's persisted accent (main theme) color into the first response, so the
/// prerendered page does not flash the packaged palette before the client applies the accent.
/// Renders nothing unless a non-default accent is stored - see <see cref="AppAccentColorService.BuildPrerenderCss"/>.
/// </summary>
public partial class AccentColorPrerenderStyle
{
    [CascadingParameter] public HttpContext HttpContext { get; set; } = default!;

    private string? accentCss;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        accentCss = AppAccentColorService.BuildPrerenderCss(HttpContext.Request.Cookies[AppAccentColorService.CookieName]);
    }
}
