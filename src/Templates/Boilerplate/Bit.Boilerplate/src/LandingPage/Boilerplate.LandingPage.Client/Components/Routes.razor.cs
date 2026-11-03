namespace Boilerplate.LandingPage.Client.Components;

public partial class Routes
{
    [AutoInject] private NavigationManager navigationManager = default!;

    private Task OnNavigateAsync(NavigationContext context)
    {
        if (RendererInfo.IsInteractive is false)
            return Task.CompletedTask;

        var (culture, _) = LandingCultures.Split(navigationManager.ToAbsoluteUri(context.Path).AbsolutePath);

        // WebAssembly started in one culture with only its resources, so a link into the other language reloads the page
        if (culture.Name != CultureInfo.CurrentUICulture.Name)
        {
            navigationManager.NavigateTo(context.Path, forceLoad: true);
        }

        return Task.CompletedTask;
    }
}
