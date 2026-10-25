using Microsoft.Playwright;

namespace Bit.Bonboarding.Tests.E2E.Infrastructure;

/// <summary>
/// Base of every suite: gives each test a browser context of its own against the shared demo, waits for a page
/// to run in the browser, and holds the probes the suites share - where the spotlight and the popover really
/// are on screen, which step is showing, and what reached the console.
/// </summary>
public abstract class BonboardingTest
{
    private readonly List<IBrowserContext> _contexts = [];
    private PlaywrightSession.Session? _session;

    public TestContext TestContext { get; set; } = default!;

    /// <summary>The origin the demo is served from, without a trailing slash.</summary>
    protected string BaseUrl => Session.Host!.BaseUrl;

    protected IBrowser Browser => Session.Browser!;

    /// <summary>Console errors and uncaught page errors of every page this test opened.</summary>
    protected List<string> ConsoleErrors { get; } = [];

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
    protected async Task<IBrowserContext> NewContextAsync(BrowserNewContextOptions? options = null)
    {
        var context = await Browser.NewContextAsync(options ?? DefaultContextOptions());
        _contexts.Add(context);
        return context;
    }

    /// <summary>A desktop window, light scheme, motion allowed: the baseline every test starts from.</summary>
    protected static BrowserNewContextOptions DefaultContextOptions() => new()
    {
        ViewportSize = new ViewportSize { Width = 1280, Height = 900 },
        DeviceScaleFactor = 1,
        ColorScheme = ColorScheme.Light,
        ReducedMotion = ReducedMotion.NoPreference,
    };

    /// <summary>Opens a page of the demo in a context of its own, and waits until it runs in the browser.</summary>
    protected async Task<IPage> OpenAsync(string path, BrowserNewContextOptions? options = null)
    {
        var page = await (await NewContextAsync(options)).NewPageAsync();
        page.Console += (_, message) =>
        {
            if (message.Type == "error") ConsoleErrors.Add(message.Text);
        };
        page.PageError += (_, error) => ConsoleErrors.Add(error);

        await GotoAsync(page, path);
        await WaitForInteractiveAsync(page);
        return page;
    }

    protected Task<IResponse?> GotoAsync(IPage page, string path) =>
        page.GotoAsync(BaseUrl + path, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 90_000 });

    /// <summary>
    /// Waits until the page runs in the browser. Every page is prerendered, so its buttons are on screen long
    /// before anything listens to them; the layout flags the moment it turns interactive.
    /// </summary>
    protected static Task WaitForInteractiveAsync(IPage page) =>
        page.WaitForFunctionAsync("() => document.querySelector('.page')?.dataset.interactive === 'true'",
            null, new PageWaitForFunctionOptions { Timeout = 90_000 });

    /// <summary>The popover, once it is placed and showing a step titled <paramref name="title"/>.</summary>
    protected static async Task<ILocator> WaitForStepAsync(IPage page, string title)
    {
        var popover = page.Locator(".bit-bonboarding-popover[data-bonboarding-placed='true']");
        await Assertions.Expect(popover.Locator(".bit-bonboarding-title")).ToHaveTextAsync(title);
        await WaitForSettledAsync(page);
        return popover;
    }

    /// <summary>
    /// Waits until the popover and the spotlight stop moving: the transitions between steps, and a smooth scroll
    /// bringing the target into view, both animate. Two identical frames in a row mean it has settled.
    /// </summary>
    protected static Task WaitForSettledAsync(IPage page) => page.EvaluateAsync(
        """
        () => new Promise(resolve => {
          let last = '', stable = 0, frames = 0;
          const tick = () => {
            const rects = ['.bit-bonboarding-popover', '.bit-bonboarding-spotlight']
              .map(s => document.querySelector(s)?.getBoundingClientRect())
              .map(r => r ? [r.x, r.y, r.width, r.height].map(v => Math.round(v)).join(',') : '-')
              .join('|');
            stable = rects === last ? stable + 1 : 0;
            last = rects;
            if (stable >= 3 || ++frames > 240) resolve(); else requestAnimationFrame(tick);
          };
          requestAnimationFrame(tick);
        })
        """);

    /// <summary>The box of the first element matching <paramref name="selector"/>, in viewport pixels.</summary>
    protected static async Task<LocatorBoundingBoxResult> BoxOfAsync(IPage page, string selector)
    {
        var box = await page.Locator(selector).First.BoundingBoxAsync();
        Assert.IsNotNull(box, $"'{selector}' has no box on screen.");
        return box;
    }

    /// <summary>True when the tour host renders nothing at all - no overlay, no blocker, no popover.</summary>
    protected static async Task<bool> HostIsEmptyAsync(IPage page) =>
        await page.Locator(".bit-bonboarding-root").CountAsync() == 0;

    /// <summary>The class and the accessible text of the element that has focus.</summary>
    protected static Task<string> FocusedAsync(IPage page) => page.EvaluateAsync<string>(
        "() => { const e = document.activeElement; return e ? `${e.tagName.toLowerCase()}.${e.className}|${(e.getAttribute('aria-label') || e.textContent || '').trim()}` : ''; }");

    protected static void AssertNear(double expected, double actual, double tolerance, string what) =>
        Assert.IsTrue(Math.Abs(expected - actual) <= tolerance, $"{what} is {actual}, expected {expected} (±{tolerance}).");

    /// <summary>Fails with every console error the test's pages logged.</summary>
    protected void AssertNoConsoleErrors(string where) =>
        Assert.IsEmpty(ConsoleErrors, $"{where} logged console errors:{Environment.NewLine}{string.Join(Environment.NewLine, ConsoleErrors)}");
}
