using Microsoft.Playwright.TestAdapter;

namespace Boilerplate.WorkbenchTests.Infrastructure;

/// <summary>
/// A browser test against the shared Workbench, in a fresh browser context with nothing saved. Only the Workbench
/// answers it, so a map tile or anything else from the internet can't make a test slow or flaky.
/// </summary>
[DoNotParallelize]
public class WorkbenchPageTest : PageTest
{
    private static readonly TimeSpan defaultTimeout = TimeSpan.FromSeconds(30);

    protected WorkbenchTestServer Server { get; private set; } = default!;

    /// <summary>The Workbench these tests run against.</summary>
    protected virtual Task<WorkbenchTestServer> ServerToTest => WorkbenchTestsInitializer.SharedServer;

    [TestInitialize]
    public async Task WorkbenchPageSetup()
    {
        Server = await ServerToTest;

        Context.SetDefaultTimeout((float)defaultTimeout.TotalMilliseconds);
        Assertions.SetDefaultExpectTimeout((float)defaultTimeout.TotalMilliseconds);

        await Context.RouteAsync(url => new Uri(url).IsLoopback is false, route => route.AbortAsync());
    }

    public override BrowserNewContextOptions ContextOptions()
    {
        var options = base.ContextOptions();

        options.Locale = "en-US";
        options.ViewportSize = new() { Width = 1600, Height = 1000 };

        return options;
    }

    /// <summary>Headless runs use chromium-headless-shell, which is all these tests need and starts faster.</summary>
    public override async Task<BrowserTypeLaunchOptions?> LaunchOptionsAsync()
    {
        var options = new BrowserTypeLaunchOptions(PlaywrightSettingsProvider.LaunchOptions);

        var isHeadedRun = options.Headless is false
            || Environment.GetEnvironmentVariable("HEADED") is "1"
            || string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PWDEBUG")) is false;

        if (isHeadedRun is false && PlaywrightSettingsProvider.BrowserName is Microsoft.Playwright.BrowserType.Chromium)
        {
            options.Channel = "chromium-headless-shell";
        }

        return options;
    }

    /// <summary>Opens a page in the shell and waits until every view on the canvas has settled.</summary>
    protected async Task OpenWorkbench(string pageKey, string query = "")
    {
        await Page.GotoAsync(new Uri(Server.Address, $"workbench/{pageKey}{query}").ToString());

        await WaitForViews();
    }

    /// <summary>Waits until the canvas shows its views and none of them is still loading.</summary>
    protected async Task WaitForViews()
    {
        await Expect(Page.Locator(".pane-stage.ready").First).ToBeVisibleAsync();
        await Expect(Page.Locator(".pane-stage:not(.ready)")).ToHaveCountAsync(0);
    }

    /// <summary>Opens a page alone, the way each view loads it, and waits until it reports it has settled.</summary>
    protected async Task OpenPreview(string pageKey, string query = "")
    {
        await Page.GotoAsync(new Uri(Server.Address, $"preview/{pageKey}{query}").ToString());

        await Page.WaitForFunctionAsync("() => window.__workbenchReady === true");
    }

    /// <summary>The iframe of the view with this id on the shell.</summary>
    protected IFrameLocator View(string paneId = "1") => Page.FrameLocator($"iframe[data-pane-id='{paneId}']");
}
