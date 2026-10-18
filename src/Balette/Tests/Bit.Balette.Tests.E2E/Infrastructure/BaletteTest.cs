using Microsoft.Playwright;

namespace Bit.Balette.Tests.E2E.Infrastructure;

/// <summary>
/// Base of every suite: gives each test a browser context of its own against the shared demo, waits for a page to
/// be ready for keys, and holds the locators and probes the suites share. Fails the running test on anything the
/// page reported as fatal.
/// </summary>
/// <remarks>
/// The fatal-error check is load-bearing. An exception that escapes a handler takes the WebAssembly renderer down,
/// and every later keystroke is silently dropped: the test then only ever sees a row that never becomes active or
/// a dialog that never closes, and times out pointing at the wrong thing. So every uncaught page error, and every
/// console error in which Blazor reports an unhandled exception, fails the test that was running and is quoted.
/// </remarks>
public abstract class BaletteTest
{
    // What Blazor writes to the console when something went wrong for real: the WebAssembly renderer's
    // "Unhandled exception rendering component", and the critical log line every unhandled exception produces.
    private static readonly string[] FatalConsoleFragments =
    [
        "Unhandled exception",
        "crit: ",
    ];

    private readonly List<IBrowserContext> _contexts = [];
    private readonly List<string> _fatal = [];
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

        string[] fatal;
        lock (_fatal) fatal = [.. _fatal];
        _fatal.Clear();

        if (fatal.Length > 0)
            Assert.Fail($"The page reported {fatal.Length} fatal error(s):{Environment.NewLine}{string.Join(Environment.NewLine, fatal)}" +
                        $"{Environment.NewLine}Recent demo server output:{Environment.NewLine}{Session.Host?.RecentOutput}");
    }

    /// <summary>
    /// A fresh browser context, closed when the test ends. A light colour scheme, so the palette's "auto" theme is
    /// the same on every machine, and reduced motion, so a box measured right after the dialog opened is its
    /// resting box and not a frame of the 140 ms pop-in.
    /// </summary>
    protected async Task<IBrowserContext> NewContextAsync(BrowserNewContextOptions? options = null)
    {
        var context = await Browser.NewContextAsync(options ?? DefaultContextOptions());
        _contexts.Add(context);
        return context;
    }

    protected static BrowserNewContextOptions DefaultContextOptions() => new()
    {
        ViewportSize = new ViewportSize { Width = 1280, Height = 900 },
        DeviceScaleFactor = 1,
        ColorScheme = ColorScheme.Light,
        ReducedMotion = ReducedMotion.Reduce,
    };

    /// <summary>Opens a page of the demo in a context of its own, and waits until it takes keys.</summary>
    protected async Task<IPage> OpenAsync(string path, BrowserNewContextOptions? options = null)
    {
        var page = await NewPageAsync(await NewContextAsync(options));
        await GotoAsync(page, path);
        await WaitForReadyAsync(page);
        return page;
    }

    /// <summary>A page whose fatal errors fail the test (see the remarks on this class).</summary>
    protected async Task<IPage> NewPageAsync(IBrowserContext context)
    {
        var page = await context.NewPageAsync();
        page.Console += (_, message) =>
        {
            if (message.Type == "error" && FatalConsoleFragments.Any(f => message.Text.Contains(f, StringComparison.Ordinal)))
                lock (_fatal) _fatal.Add("console: " + message.Text);
        };
        page.PageError += (_, error) => { lock (_fatal) _fatal.Add("unhandled: " + error); };
        return page;
    }

    protected Task<IResponse?> GotoAsync(IPage page, string path) =>
        page.GotoAsync(BaseUrl + path, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 90_000 });

    /// <summary>
    /// Waits until the page runs in the browser and the palette's browser layer listens for keys. Every page is
    /// prerendered, so its markup - the trigger and the mode buttons included - is on screen long before anything
    /// handles a click, and the layer that hears Ctrl+K starts a moment after the page turns interactive.
    /// </summary>
    protected static Task WaitForReadyAsync(IPage page) => page.WaitForFunctionAsync(
        """
        () => {
          const app = document.querySelector('.app');
          return app?.dataset.interactive === 'true' && app?.dataset.paletteReady === 'true';
        }
        """, null, new PageWaitForFunctionOptions { Timeout = 90_000 });

    protected static async Task ReloadAsync(IPage page)
    {
        await page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 90_000 });
        await WaitForReadyAsync(page);
    }

    // ---- The palette's stable hooks -------------------------------------------------------------------------------

    protected static ILocator Dialog(IPage page) => page.Locator("dialog[data-balette='dialog']");

    protected static ILocator Input(IPage page) => page.Locator("[data-balette='input']");

    protected static ILocator List(IPage page) => page.Locator("[data-balette='list']");

    protected static ILocator Items(IPage page) => page.Locator("[data-balette='item']");

    protected static ILocator Item(IPage page, string id) => page.Locator($"[data-balette='item'][data-value='{id}']");

    protected static ILocator ActiveItem(IPage page) => page.Locator("[data-balette='item'][data-active='true']");

    protected static ILocator GroupHeadings(IPage page) => page.Locator("[data-balette='group-heading']");

    protected static ILocator Crumbs(IPage page) => page.Locator("[data-balette='breadcrumbs'] [data-balette='crumb']");

    protected static ILocator ModeChip(IPage page) => page.Locator("[data-balette='mode']");

    protected static ILocator Toast(IPage page) => page.Locator("[data-balette='toast']");

    protected static ILocator Prompt(IPage page) => page.Locator("[data-balette='prompt']");

    protected static ILocator Detail(IPage page) => page.Locator("[data-balette='detail']");

    /// <summary>The polite live region the palette announces result counts in.</summary>
    protected static ILocator LiveRegion(IPage page) => page.Locator("dialog[data-balette='dialog'] [aria-live='polite']");

    // ---- Driving it -------------------------------------------------------------------------------------------------

    /// <summary>Presses Ctrl+K with the palette closed, and waits until it is open with the input focused.</summary>
    protected static async Task OpenPaletteAsync(IPage page)
    {
        await page.Keyboard.PressAsync("Control+k");
        await ExpectOpenAsync(page);
    }

    /// <summary>The native dialog is shown (a closed one is display: none) and the input has the focus.</summary>
    protected static async Task ExpectOpenAsync(IPage page)
    {
        await Assertions.Expect(Dialog(page)).ToBeVisibleAsync();
        await Assertions.Expect(Input(page)).ToBeFocusedAsync();
    }

    protected static Task ExpectClosedAsync(IPage page) => Assertions.Expect(Dialog(page)).ToBeHiddenAsync();

    /// <summary>Types into the palette's input one key at a time, as a person would: every key is its own input event.</summary>
    protected static Task TypeAsync(IPage page, string text) => Input(page).PressSequentiallyAsync(text);

    protected static Task ExpectFirstItemAsync(IPage page, string id) =>
        Assertions.Expect(Items(page).First).ToHaveAttributeAsync("data-value", id);

    /// <summary>
    /// Waits until <paramref name="id"/> is the active row, and checks the combobox says so: the input's
    /// aria-activedescendant names that row, which is marked selected, and the focus never left the input.
    /// </summary>
    protected static async Task ExpectActiveAsync(IPage page, string id)
    {
        await Assertions.Expect(ActiveItem(page)).ToHaveAttributeAsync("data-value", id);
        var state = await page.EvaluateAsync<string[]>(
            """
            () => {
              const input = document.querySelector('[data-balette="input"]');
              const active = document.querySelector('[data-balette="item"][data-active="true"]');
              return [input.getAttribute('aria-activedescendant') ?? '', active?.id ?? '', active?.getAttribute('aria-selected') ?? '',
                      document.activeElement === input ? 'focused' : 'not focused'];
            }
            """);
        Assert.AreEqual(state[1], state[0], "aria-activedescendant does not name the active row");
        Assert.AreEqual("true", state[2], "the active row is not aria-selected");
        Assert.AreEqual("focused", state[3], "the focus left the input");
    }

    /// <summary>The ids of the rows listed right now, in order.</summary>
    protected static Task<string[]> ItemIdsAsync(IPage page) =>
        page.EvaluateAsync<string[]>("() => [...document.querySelectorAll('[data-balette=\"item\"]')].map(e => e.dataset.value)");

    /// <summary>What has the focus, described by its data-balette hook, else its tag, aria-label and id.</summary>
    protected static Task<string> FocusedAsync(IPage page) => page.EvaluateAsync<string>(
        """
        () => {
          const e = document.activeElement;
          if (!e) return '';
          return e.dataset?.balette ? 'balette:' + e.dataset.balette : `${e.tagName.toLowerCase()}[${e.getAttribute('aria-label') ?? e.textContent?.trim() ?? ''}]`;
        }
        """);

    protected static void AssertInRange(double value, double low, double high, string what) =>
        Assert.IsTrue(value >= low && value <= high, $"{what} is {value}, expected it between {low} and {high}.");
}
