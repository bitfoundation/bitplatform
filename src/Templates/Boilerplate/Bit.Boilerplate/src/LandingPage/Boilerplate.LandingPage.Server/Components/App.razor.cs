namespace Boilerplate.LandingPage.Server.Components;

public partial class App
{
    [AutoInject] private LandingServerSettings settings = default!;

    private readonly BitThemeSsrOptions themeOptions = new()
    {
        Persist = true,
        ViewTransition = true,
        ThemeColorMeta = true
    };
}
