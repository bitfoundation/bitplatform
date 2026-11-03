namespace Boilerplate.LandingPage.Client.Components.Pages;

public partial class MissingPage
{
    [Parameter] public string? Path { get; set; }

    protected override void OnInitialized()
    {
        NavigationManager.NotFound();

        base.OnInitialized();
    }
}
