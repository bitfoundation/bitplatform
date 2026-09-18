using Microsoft.AspNetCore.Components;

namespace Boilerplate.Tests.E2E.Infrastructure.Services;

internal sealed class TestNavigationManager : NavigationManager
{
    public TestNavigationManager()
    {
        Initialize(DeployedApps.AdminPanel, DeployedApps.AdminPanel);
    }

    protected override void NavigateToCore(string uri, bool forceLoad)
    {
    }

    protected override void NavigateToCore(string uri, NavigationOptions options)
    {
    }
}
