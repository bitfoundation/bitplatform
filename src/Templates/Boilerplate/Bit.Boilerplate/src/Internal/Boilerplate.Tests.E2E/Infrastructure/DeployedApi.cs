namespace Boilerplate.Tests.E2E.Infrastructure;

/// <summary>
/// Which API a <see cref="DeployedApiClientProvider.CreateApiClientFor"/> aims its http clients at, and the origin they send as
/// <c>X-Origin</c> so server-generated links point at the matching app.
/// </summary>
public sealed class DeployedApi
{
    public Uri ApiAddress { get; set; } = new(DeployedApps.AdminPanelApi);

    public string WebAppOrigin { get; set; } = DeployedApps.AdminPanel;

    public static (Uri Address, string Origin) For(string apiAddress)
    {
        if (apiAddress == DeployedApps.AdminPanelApi)
            return (new(DeployedApps.AdminPanelApi), DeployedApps.AdminPanel);
        if (apiAddress == DeployedApps.TodoApi)
            return (new(DeployedApps.TodoApi), DeployedApps.Todo);
        if (apiAddress == DeployedApps.Sales)
            return (new(DeployedApps.Sales), DeployedApps.Sales);

        return (new(apiAddress), apiAddress);
    }
}
