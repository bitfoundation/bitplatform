namespace Boilerplate.LandingPage.Client.Infrastructure.Services;

public sealed class NavigationSiteAddress(NavigationManager navigationManager) : ISiteAddress
{
    public Uri BaseUri => new(navigationManager.BaseUri);
}
