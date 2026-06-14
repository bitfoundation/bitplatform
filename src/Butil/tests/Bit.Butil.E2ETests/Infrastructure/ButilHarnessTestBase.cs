using Microsoft.Playwright;
using NUnit.Framework;

namespace Bit.Butil.E2ETests.Infrastructure;
/// <summary>
/// Self-managing Playwright base class. Unlike <c>Microsoft.Playwright.NUnit.PageTest</c> this
/// doesn't depend on runsettings being threaded through the test host (which doesn't happen
/// reliably under the Microsoft.Testing.Platform runner). Instead it reads configuration from
/// environment variables so the same binary runs against bundled Chromium, a system Chrome
/// channel, or an explicit executable path:
/// <list type="bullet">
///   <item><c>BUTIL_E2E_CHANNEL</c> - e.g. <c>chrome</c> / <c>msedge</c> (uses an installed browser).</item>
///   <item><c>BUTIL_E2E_EXECUTABLE</c> - full path to a chromium-family executable.</item>
///   <item><c>BUTIL_E2E_HEADED</c> - set to <c>1</c> to watch the run.</item>
/// </list>
/// </summary>
public abstract class ButilHarnessTestBase
{
    private IPlaywright _playwright = default!;
    private IBrowser _browser = default!;
    private IBrowserContext _context = default!;

    protected IPage Page { get; private set; } = default!;

    /// <summary>The harness route a derived fixture drives, e.g. "/e2e".</summary>
    protected abstract string HarnessRoute { get; }

    [SetUp]
    public async Task SetUp()
    {
        _playwright = await Playwright.CreateAsync();

        var launchOptions = new BrowserTypeLaunchOptions
        {
            Headless = Environment.GetEnvironmentVariable("BUTIL_E2E_HEADED") != "1"
        };

        var channel = Environment.GetEnvironmentVariable("BUTIL_E2E_CHANNEL");
        if (!string.IsNullOrWhiteSpace(channel)) launchOptions.Channel = channel;

        var executable = Environment.GetEnvironmentVariable("BUTIL_E2E_EXECUTABLE");
        if (!string.IsNullOrWhiteSpace(executable)) launchOptions.ExecutablePath = executable;

        _browser = await _playwright.Chromium.LaunchAsync(launchOptions);
        _context = await _browser.NewContextAsync(new()
        {
            BaseURL = DemoServerFixture.BaseUrl,
            IgnoreHTTPSErrors = true,
            ViewportSize = new() { Width = 1280, Height = 720 }
        });
        Page = await _context.NewPageAsync();

        await Page.GotoAsync(HarnessRoute, new() { Timeout = 60_000 });
        await Assertions.Expect(Page.Locator("#status")).ToHaveTextAsync("ready", new() { Timeout = 60_000 });
    }

    [TearDown]
    public async Task TearDown()
    {
        try
        {
            if (_context is not null) await _context.CloseAsync();
            if (_browser is not null) await _browser.CloseAsync();
        }
        finally
        {
            _playwright?.Dispose();
        }
    }

    /// <summary>Clicks an element by id and waits for #status to contain the expected prefix.</summary>
    protected async Task ClickAndExpectAsync(string id, string statusPrefix, int timeoutMs = 15_000)
    {
        await Page.Locator($"#{id}").ClickAsync();
        await Assertions.Expect(Page.Locator("#status")).ToContainTextAsync(statusPrefix, new() { Timeout = timeoutMs });
    }

    protected async Task<string> CurrentStatusAsync()
        => (await Page.Locator("#status").TextContentAsync())?.Trim() ?? string.Empty;
}
