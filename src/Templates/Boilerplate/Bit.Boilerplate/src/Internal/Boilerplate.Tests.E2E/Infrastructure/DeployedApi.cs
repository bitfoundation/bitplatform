namespace Boilerplate.Tests.E2E.Infrastructure;

/// <summary>
/// Which deployed API a <see cref="TestHost.CreateScope"/> aims its typed http clients at, and the web origin those
/// clients send as <c>X-Origin</c> (so generated links, e.g. in Hangfire job arguments, point at the matching app).
/// </summary>
public sealed class DeployedApi
{
    public Uri Address { get; set; } = new(DeployedApps.AdminPanelApi);

    public string Origin { get; set; } = DeployedApps.AdminPanel;

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
