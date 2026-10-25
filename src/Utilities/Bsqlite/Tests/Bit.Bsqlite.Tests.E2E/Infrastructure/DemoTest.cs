using Microsoft.Playwright;

namespace Bit.Bsqlite.Tests.E2E.Infrastructure;

/// <summary>
/// Base of the suites that drive the demo website rather than the library's modules: the demo server is
/// started on first use (see <see cref="PlaywrightSession.GetDemoAsync"/>), and every page is opened in a
/// context of its own whose console errors are collected.
/// </summary>
public abstract class DemoTest : BsqliteTest
{
    private PlaywrightSession.DemoSession? _demo;

    /// <summary>The origin the demo answers at, without a trailing slash.</summary>
    protected string DemoUrl => (_demo?.Host ?? throw new InvalidOperationException("The demo is started in TestInitialize.")).BaseUrl;

    [TestInitialize]
    public async Task StartDemoAsync()
    {
        _demo = await PlaywrightSession.GetDemoAsync();
        if (_demo.Unavailable is not { } reason) return;

        if (E2EEnvironment.IsContinuousIntegration) Assert.Fail("The demo tests could not run: " + reason);
        Assert.Inconclusive("The demo tests could not run: " + reason);
    }

    /// <summary>
    /// A page of the demo in a fresh context - so a fresh origin storage, with no database and no sticky
    /// binding yet - with every console error and uncaught exception recorded in <paramref name="errors"/>.
    /// </summary>
    protected async Task<IPage> NewDemoPageAsync(List<string> errors)
    {
        var context = await NewContextAsync(new BrowserNewContextOptions { BaseURL = DemoUrl });
        var page = await context.NewPageAsync();

        page.Console += (_, message) =>
        {
            if (message.Type == "error") errors.Add(message.Text);
        };
        page.PageError += (_, error) => errors.Add(error);

        return page;
    }

    /// <summary>
    /// Navigates and waits until the page runs in the browser: every page is prerendered first, and the site's
    /// shell (Layout/MainLayout.razor) turns its data-interactive marker to true once it is hydrated.
    /// </summary>
    protected static async Task<IResponse?> GotoInteractiveAsync(IPage page, string path)
    {
        var response = await page.GotoAsync(path, new PageGotoOptions { Timeout = 90_000 });
        await page.WaitForSelectorAsync(".page[data-interactive='true']", new PageWaitForSelectorOptions { Timeout = 90_000 });
        return response;
    }

    /// <summary>
    /// Waits until the session bar reports an open database and returns the backend it landed on. Every page that
    /// runs against the database opens with the bar (Components/StorageSwitcher.razor); the landing page opens its
    /// database without one.
    /// </summary>
    protected static async Task<string> WaitForSessionAsync(IPage page, string? expected = null)
    {
        await page.WaitForFunctionAsync(
            """
            expected => {
              const session = document.querySelector('section[data-session]')?.dataset.session;
              return expected === null ? !!session && session !== 'closed' : session === expected;
            }
            """, expected, new PageWaitForFunctionOptions { Timeout = 60_000 });

        return (await page.Locator("section[data-session]").GetAttributeAsync("data-session"))!;
    }
}
