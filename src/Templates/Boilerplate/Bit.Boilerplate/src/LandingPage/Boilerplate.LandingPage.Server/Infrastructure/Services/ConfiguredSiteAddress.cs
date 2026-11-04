namespace Boilerplate.LandingPage.Server.Infrastructure.Services;

// Prerendered links use Site:Url, so a request through another host name still gets the public address
public sealed class ConfiguredSiteAddress(LandingServerSettings settings, NavigationManager navigationManager) : ISiteAddress
{
    public Uri BaseUri => settings.Site.Url ?? new Uri(navigationManager.BaseUri);
}
