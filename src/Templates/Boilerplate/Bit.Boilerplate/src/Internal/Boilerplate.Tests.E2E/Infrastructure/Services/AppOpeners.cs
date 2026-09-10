namespace Boilerplate.Tests.E2E.Infrastructure.Services;

/// <summary>
/// Turns an <see cref="App"/> into a live page: the web opener navigates the test's own page, the hybrid ones launch
/// the installed app and attach over CDP. Null means no build on that platform, which
/// <see cref="AppTestBase.OpenApp"/> reports as inconclusive rather than failed.
/// </summary>
public interface IAppOpener
{
    Task<IPage?> TryOpen(AppTestBase test, App app);
}

public sealed class WebAppOpener : IAppOpener
{
    public async Task<IPage?> TryOpen(AppTestBase test, App app)
    {
        await test.Page.GotoAsync(DeployedApps.AddressOf(app));

        return test.Page;
    }
}

public sealed class WindowsAppOpener : IAppOpener
{
    public async Task<IPage?> TryOpen(AppTestBase test, App app)
    {
        var windowsAppId = app switch
        {
            App.Todo => DeployedApps.TodoWindowsAppId,
            App.AdminPanel => DeployedApps.AdminPanelWindowsAppId,
            App.Sales => DeployedApps.SalesWindowsAppId,
            _ => null,
        };

        if (windowsAppId is null)
            return null;

        var (page, stop) = await test.Playwright.LaunchWindowsApp(windowsAppId);

        test.RegisterForCleanup(stop);

        return page;
    }
}

public sealed class AndroidAppOpener : IAppOpener
{
    public async Task<IPage?> TryOpen(AppTestBase test, App app)
    {
        var applicationId = app switch
        {
            App.Todo => DeployedApps.TodoAndroidAppId,
            App.AdminPanel => DeployedApps.AdminPanelAndroidAppId,
            _ => null,
        };

        if (applicationId is null)
            return null;

        var (page, stop) = await test.Playwright.LaunchAndroidApp(applicationId);

        test.RegisterForCleanup(stop);

        return page;
    }
}
