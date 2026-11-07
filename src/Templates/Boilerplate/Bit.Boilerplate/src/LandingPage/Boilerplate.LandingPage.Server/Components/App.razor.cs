namespace Boilerplate.LandingPage.Server.Components;

public partial class App
{
    [AutoInject] private LandingServerSettings settings = default!;

    [CascadingParameter] private HttpContext HttpContext { get; set; } = default!;

    // Crawlers read the prerendered page and benchmarks measure it, so scripts only cost them, unless nothing is prerendered
    private bool OmitScripts => settings.WebAppRender.PrerenderEnabled && (HttpContext.Request.IsLightHouseRequest() || HttpContext.Request.IsCrawlerClient());

    private readonly BitThemeSsrOptions themeOptions = new()
    {
        Persist = true,
        ViewTransition = true,
        ThemeColorMeta = true
    };
}
