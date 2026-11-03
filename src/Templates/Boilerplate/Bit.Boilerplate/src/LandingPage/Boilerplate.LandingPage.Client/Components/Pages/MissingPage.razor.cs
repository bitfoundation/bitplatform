namespace Boilerplate.LandingPage.Client.Components.Pages;

public partial class MissingPage
{
    [Parameter] public string? Path { get; set; }

    protected override void OnInitialized()
    {
        // A catch-all, so an unknown path gets NotFoundPage and a 404 status rather than an empty response
        NavigationManager.NotFound();

        base.OnInitialized();
    }
}
