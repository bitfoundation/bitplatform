namespace Boilerplate.LandingPage.Client.Components.Pages;

public partial class NotFoundPage
{
    private LandingSite? site;

    protected override void OnParametersSet()
    {
        site = LandingSites.FromPath(NavigationManager.ToAbsoluteUri(NavigationManager.Uri).AbsolutePath);

        base.OnParametersSet();
    }
}
