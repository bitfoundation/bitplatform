using Microsoft.Playwright;

namespace Bit.BlazeDb.Tests.E2E.Infrastructure;

/// <summary>
/// Base of every suite: gives each test a browser context of its own against the shared demo - and with
/// it an Origin Private File System, an IndexedDB and a set of Web Locks of its own - and waits for a page
/// to have its database open before the test touches it.
/// </summary>
public abstract class BlazeDbTest
{
    private readonly List<IBrowserContext> _contexts = [];
    private PlaywrightSession.Session? _session;

    public TestContext TestContext { get; set; } = default!;

    /// <summary>The origin the demo is served from, without a trailing slash.</summary>
    protected string BaseUrl => Session.Host!.BaseUrl;

    protected IBrowser Browser => Session.Browser!;

    private PlaywrightSession.Session Session => _session ?? throw new InvalidOperationException("The session is opened in TestInitialize.");

    [TestInitialize]
    public async Task OpenSessionAsync()
    {
        _session = await PlaywrightSession.GetAsync();
        if (_session.Unavailable is not { } reason) return;

        // A build server must not report a suite that never ran as anything but a failure.
        if (E2EEnvironment.IsContinuousIntegration) Assert.Fail("The browser tests could not run: " + reason);
        Assert.Inconclusive("The browser tests could not run: " + reason);
    }

    [TestCleanup]
    public async Task CloseContextsAsync()
    {
        foreach (var context in _contexts)
        {
            try { await context.CloseAsync(); }
            catch (PlaywrightException) { /* the browser went away first */ }
        }
        _contexts.Clear();
    }

    /// <summary>A fresh browser context, closed when the test ends.</summary>
    protected async Task<IBrowserContext> NewContextAsync()
    {
        var context = await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 900 },
        });
        _contexts.Add(context);
        return context;
    }

    /// <summary>Opens a page of the demo in a context of its own, and waits until its database is open.</summary>
    protected async Task<IPage> OpenAsync(string path)
    {
        var page = await (await NewContextAsync()).NewPageAsync();
        await GotoAsync(page, path);
        return page;
    }

    /// <summary>Navigates <paramref name="page"/> and waits until its database is open.</summary>
    protected async Task GotoAsync(IPage page, string path)
    {
        await page.GotoAsync(BaseUrl + path, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 90_000 });
        await WaitForDatabaseAsync(page);
    }

    /// <summary>
    /// Waits until the WebAssembly app has replaced the host page's boot screen and the demo database is
    /// open, and returns where it ended up: Opfs, or one of the in-memory fallbacks. Nothing is prerendered,
    /// so the shell's arrival is the app running; its data-storage attribute is the database being ready.
    /// </summary>
    protected static async Task<string> WaitForDatabaseAsync(IPage page)
    {
        await page.WaitForFunctionAsync(
            "() => { const shell = document.querySelector('.shell'); return shell && shell.dataset.storage && shell.dataset.storage !== 'Opening'; }",
            null, new PageWaitForFunctionOptions { Timeout = 90_000 });

        return await page.Locator(".shell").GetAttributeAsync("data-storage") ?? string.Empty;
    }

    /// <summary>Collects every uncaught error and console error a page reports, for a test to assert there were none.</summary>
    protected static List<string> CollectErrors(IPage page)
    {
        var errors = new List<string>();

        page.PageError += (_, error) => errors.Add($"page error: {error}");
        page.Console += (_, message) =>
        {
            if (message.Type == "error") errors.Add($"console error: {message.Text}");
        };

        return errors;
    }
}
