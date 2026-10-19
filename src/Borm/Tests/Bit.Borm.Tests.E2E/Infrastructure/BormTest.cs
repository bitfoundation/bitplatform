using System.Text;
using Microsoft.Playwright;

namespace Bit.Borm.Tests.E2E.Infrastructure;

/// <summary>
/// Base of every suite: gives each test a browser context of its own against the shared demo, waits for a page to
/// run in the browser, and holds the locators and probes the suites share. Every probe reads what the browser
/// actually did - where the focus is, what a control holds, what <c>getComputedStyle</c> resolved - never the markup
/// the component meant to produce. Fails the running test on anything the page reported as fatal.
/// </summary>
/// <remarks>
/// The fatal-error check is load-bearing. An exception that escapes a handler takes the WebAssembly renderer down,
/// and every later keystroke is silently dropped: the test then only ever sees a message that never appears or a
/// step that never changes, and times out pointing at the wrong thing. So every uncaught page error, and every
/// console error in which Blazor reports an unhandled exception, fails the test that was running and is quoted.
/// </remarks>
public abstract class BormTest
{
    /// <summary>The colour the stylesheet paints errors with under the light theme (<c>--bit-borm-error</c>).</summary>
    protected const string ErrorColour = "rgb(220, 38, 38)";

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
    /// A fresh browser context, closed when the test ends. The suite is hermetic: a request that leaves the demo's
    /// origin is answered with an empty response here, so a runner without internet access neither waits out a
    /// connection timeout nor logs a load error for it.
    /// </summary>
    protected async Task<IBrowserContext> NewContextAsync(BrowserNewContextOptions? options = null)
    {
        var context = await Browser.NewContextAsync(options ?? DefaultContextOptions());
        _contexts.Add(context);

        var origin = BaseUrl;
        await context.RouteAsync(url => url.StartsWith(origin, StringComparison.OrdinalIgnoreCase) is false,
            route => route.FulfillAsync(new RouteFulfillOptions
            {
                Status = 200,
                Body = string.Empty,
                ContentType = route.Request.ResourceType == "stylesheet" ? "text/css" : "text/plain",
            }));

        return context;
    }

    /// <summary>
    /// A desktop-sized viewport with a light colour scheme, so the stylesheet's <c>prefers-color-scheme</c> theme is
    /// the same on every machine, and reduced motion, which the stylesheet answers by dropping the border and
    /// background transitions: a colour read right after a field turned invalid is its resting colour, not a frame of
    /// the 150 ms fade towards it.
    /// </summary>
    protected static BrowserNewContextOptions DefaultContextOptions(ColorScheme colorScheme = ColorScheme.Light, int width = 1280) => new()
    {
        ViewportSize = new ViewportSize { Width = width, Height = 900 },
        DeviceScaleFactor = 1,
        ColorScheme = colorScheme,
        ReducedMotion = ReducedMotion.Reduce,
    };

    /// <summary>Opens a page of the demo in a context of its own, and waits until it runs in the browser.</summary>
    protected async Task<IPage> OpenAsync(string path, BrowserNewContextOptions? options = null)
    {
        var page = await NewPageAsync(await NewContextAsync(options));
        await GotoAsync(page, path);
        await WaitForInteractiveAsync(page);
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
    /// Waits until the page runs in the browser and has settled. Every page is prerendered, so its form - inputs,
    /// buttons and all - is on screen long before anything handles a keystroke: a value typed then is overwritten
    /// when WebAssembly takes the page over, and a click is simply lost. The layout's <c>data-interactive</c> marker
    /// says the handlers are attached; a quiet DOM afterwards says the first interactive renders (the focus moved
    /// to the heading, an options provider answering) are done.
    /// </summary>
    protected static async Task WaitForInteractiveAsync(IPage page)
    {
        await page.WaitForFunctionAsync(
            "() => document.querySelector('.app-shell')?.dataset.interactive === 'true'",
            null, new PageWaitForFunctionOptions { Timeout = 90_000 });

        await WaitForQuietAsync(page);
    }

    /// <summary>
    /// Waits until nothing in the page's content has changed for a moment: a keystroke's write, the validation it
    /// triggered and the render that shows it have all landed. Gives up quietly after a few seconds, for a page that
    /// keeps changing for good.
    /// </summary>
    protected static Task WaitForQuietAsync(IPage page, int quietMs = 250) => page.EvaluateAsync(
        """
        (quietMs) => new Promise(resolve => {
          const root = document.querySelector('.app-main') ?? document.body;
          let timer;
          const done = () => { observer.disconnect(); clearTimeout(cap); resolve(); };
          const arm = () => { clearTimeout(timer); timer = setTimeout(done, quietMs); };
          const observer = new MutationObserver(arm);
          observer.observe(root, { subtree: true, childList: true, attributes: true, characterData: true });
          const cap = setTimeout(done, 5000);
          arm();
        })
        """, quietMs);

    // ---- Borm's stable hooks ---------------------------------------------------------------------------------------

    /// <summary>
    /// The DOM id Borm gives the control bound to <paramref name="path"/>: the prefix, then the path with every
    /// character that is not a letter, a digit or an underscore turned into one, and the closing brackets dropped -
    /// <c>Lines[0].Description</c> is <c>borm_Lines_0_Description</c>.
    /// </summary>
    protected static string ElementId(string path, string prefix = "borm_")
    {
        var id = new StringBuilder(prefix);
        foreach (var c in path)
        {
            if (c == ']') continue;
            id.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');
        }
        return id.ToString();
    }

    /// <summary>The control bound to <paramref name="path"/> (for a radio, tag or multi-select field, its group).</summary>
    protected static ILocator Control(IPage page, string path, string prefix = "borm_") => page.Locator("#" + ElementId(path, prefix));

    /// <summary>The error messages listed under the field bound to <paramref name="path"/>.</summary>
    protected static ILocator Errors(IPage page, string path, string prefix = "borm_") =>
        page.Locator($"#{ElementId(path, prefix)}_error > .bit-borm-message--error");

    /// <summary>The <c>.bit-borm-field</c> wrapper of the field bound to <paramref name="path"/>.</summary>
    protected static ILocator FieldOf(IPage page, string path, string prefix = "borm_") =>
        page.Locator(".bit-borm-field", new() { Has = Control(page, path, prefix) });

    /// <summary>The first form on the page.</summary>
    protected static ILocator Form(IPage page) => page.Locator("form.bit-borm-form").First;

    protected static ILocator SubmitButton(IPage page) => Form(page).Locator("button[type=submit]");

    protected static ILocator FormButton(IPage page, string name) =>
        Form(page).GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    protected static ILocator Summary(IPage page) => page.Locator(".bit-borm-summary");

    /// <summary>The rows of the repeater bound to <paramref name="path"/>, in order.</summary>
    protected static ILocator Rows(IPage page, string path) => page.Locator($"#{ElementId(path)} > .bit-borm-array__item");

    /// <summary>The demo's "Saved ✓" panel, which only a valid submit shows.</summary>
    protected static ILocator Result(IPage page) => page.Locator(".demo-result");

    // ---- Driving it ---------------------------------------------------------------------------------------------------

    /// <summary>
    /// Types <paramref name="text"/> over what the control holds and leaves it, as a person does. A field that writes
    /// on <c>change</c> (the default) only hears about the value when the browser raises that event, which it does
    /// when the control loses the focus.
    /// </summary>
    protected static async Task EnterAsync(ILocator control, string text)
    {
        await control.FillAsync(text);
        await control.BlurAsync();
    }

    /// <summary>What has the focus: its id, else its tag and classes.</summary>
    protected static Task<string> FocusedAsync(IPage page) => page.EvaluateAsync<string>(
        """
        () => {
          const e = document.activeElement;
          if (!e) return '';
          return e.id ? '#' + e.id : `${e.tagName.toLowerCase()}.${[...e.classList].join('.')}`;
        }
        """);

    /// <summary>A computed style of the element <paramref name="locator"/> picks, by its CSS name (custom properties included).</summary>
    protected static Task<string> ComputedAsync(ILocator locator, string property) =>
        locator.EvaluateAsync<string>("(e, p) => getComputedStyle(e).getPropertyValue(p).trim()", property);

    // ---- Waiting and asserting ----------------------------------------------------------------------------------------

    /// <summary>Polls <paramref name="condition"/> until it holds, failing with <paramref name="what"/> when it never does.</summary>
    protected static async Task EventuallyAsync(Func<Task<bool>> condition, string what, int timeoutMs = 10_000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (true)
        {
            if (await condition()) return;
            if (DateTime.UtcNow > deadline) Assert.Fail($"timed out waiting until {what}");
            await Task.Delay(50);
        }
    }

    /// <summary>Everything a page reports going wrong while a test drives it.</summary>
    protected static List<string> TrackProblems(IPage page, string origin)
    {
        var problems = new List<string>();
        void Add(string problem) { lock (problems) problems.Add(problem); }

        page.Console += (_, message) => { if (message.Type == "error") Add($"console error: {message.Text} ({message.Location})"); };
        page.PageError += (_, error) => Add("unhandled error: " + error);
        page.RequestFailed += (_, request) =>
        {
            if (request.Url.StartsWith(origin, StringComparison.OrdinalIgnoreCase)) Add($"request failed: {request.Url} ({request.Failure})");
        };
        page.Response += (_, response) =>
        {
            if (response.Status >= 400 && response.Url.StartsWith(origin, StringComparison.OrdinalIgnoreCase)) Add($"HTTP {response.Status}: {response.Url}");
        };

        return problems;
    }
}
