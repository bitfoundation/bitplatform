namespace Boilerplate.LandingPage.Server.Infrastructure.Services;

public sealed class ConfiguredSiteAddress(LandingServerSettings settings, NavigationManager navigationManager) : ISiteAddress
{
    public Uri BaseUri => settings.Site.Url ?? new Uri(navigationManager.BaseUri);
}
