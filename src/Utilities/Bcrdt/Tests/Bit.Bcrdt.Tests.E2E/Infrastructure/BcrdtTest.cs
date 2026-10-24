using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Bit.Bcrdt.Tests.E2E.Infrastructure;

/// <summary>
/// Base of every suite: gives each test a browser context of its own against the shared demo, waits for a page to
/// run in the browser and for its replicas to be open and connected, and holds the locators and probes the suites
/// share. Fails the running test on anything the page reported as fatal.
/// </summary>
/// <remarks>
/// The fatal-error check is load-bearing. An exception that escapes a handler or a change notification takes the
/// WebAssembly renderer down, and every later edit is silently dropped: the test then only ever sees a replica that
/// never catches up, and times out pointing at the merge rather than at the exception. So every uncaught page error,
/// and every console error in which Blazor reports an unhandled exception, fails the test that was running and is
/// quoted.
/// </remarks>
public abstract class BcrdtTest
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
    /// A fresh browser context, closed when the test ends: its own WebAssembly instance, and with it its own
    /// in-memory network and replica stores, so nothing one test typed is there when the next one opens a page.
    /// Wide enough for the text page's three replicas to sit side by side.
    /// </summary>
    protected async Task<IBrowserContext> NewContextAsync()
    {
        var context = await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1440, Height = 1000 },
            ColorScheme = ColorScheme.Light,
            ReducedMotion = ReducedMotion.Reduce,
        });
        _contexts.Add(context);
        return context;
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

    /// <summary>Opens a page of the demo in a context of its own, and waits until it runs in the browser.</summary>
    protected async Task<IPage> OpenAsync(string path)
    {
        var page = await NewPageAsync(await NewContextAsync());
        await GotoAsync(page, path);
        await WaitForInteractiveAsync(page);
        return page;
    }

    /// <summary>
    /// Opens one of the side-by-side pages and waits until its <paramref name="replicas"/> replicas are open and
    /// connected to each other: every panel's badge reads "synced".
    /// </summary>
    protected async Task<IPage> OpenReplicasAsync(string path, int replicas)
    {
        var page = await OpenAsync(path);
        await WaitForReplicasAsync(page, replicas);
        return page;
    }

    protected Task<IResponse?> GotoAsync(IPage page, string path) =>
        page.GotoAsync(BaseUrl + path, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 90_000 });

    /// <summary>
    /// Waits until the page runs in the browser. Every page is prerendered, so its heading and its explanations are
    /// on screen long before anything listens to a click. The layout's <c>data-interactive</c> marker turns true
    /// once the WebAssembly renderer has taken the page over. Not the router's <c>FocusOnNavigate</c>: it joins the
    /// tree only with the first navigation (Routes.razor), so on arrival nothing marks the <c>h1</c>.
    /// </summary>
    protected static Task WaitForInteractiveAsync(IPage page) => page.WaitForFunctionAsync(
        "() => document.querySelector('.page')?.dataset.interactive === 'true'",
        null, new PageWaitForFunctionOptions { Timeout = 90_000 });

    /// <summary>
    /// Waits until a side-by-side page shows <paramref name="count"/> replica panels and every one of them is
    /// connected. The panels only render once the page runs interactively and the replicas have opened (the
    /// prerendered page shows an "Opening…" placeholder instead), and a replica reports "synced" once its handshake
    /// with the others is done.
    /// </summary>
    protected static async Task WaitForReplicasAsync(IPage page, int count)
    {
        await Assertions.Expect(Panels(page)).ToHaveCountAsync(count, new() { Timeout = 60_000 });
        await Assertions.Expect(page.Locator("section.panel .panel-head .badge"))
            .ToHaveTextAsync(Enumerable.Repeat("synced", count).ToArray(), new() { Timeout = 30_000 });
    }

    // ---- The demo's replica panels ------------------------------------------------------------------------------

    /// <summary>Every replica panel on the page, in the order the page lists its replicas.</summary>
    protected static ILocator Panels(IPage page) => page.Locator("section.panel");

    /// <summary>
    /// The panel of the replica called <paramref name="label"/> ("Replica A"). Matched on the panel's own name
    /// rather than on any text in it: each panel's presence list names the other replicas too.
    /// </summary>
    protected static ILocator Replica(IPage page, string label) => Panels(page).Filter(new LocatorFilterOptions
    {
        Has = page.Locator(".panel-name", new PageLocatorOptions { HasTextRegex = new Regex($"^\\s*{Regex.Escape(label)}\\s*$") }),
    });

    /// <summary>The panel's <c>BcrdtTextArea</c>.</summary>
    protected static ILocator Editor(ILocator panel) => panel.Locator("textarea");

    /// <summary>The connection badge in the panel's header: synced, syncing, connecting, offline...</summary>
    protected static ILocator Badge(ILocator panel) => panel.Locator(".panel-head .badge");

    /// <summary>The names in the panel's presence list.</summary>
    protected static ILocator PresenceList(ILocator panel) => panel.Locator(".panel-presence .presence-list");

    /// <summary>Takes a replica offline with its own button: a real disconnect, which the badge reports.</summary>
    protected static async Task GoOfflineAsync(ILocator panel)
    {
        var toggle = panel.Locator(".panel-toggle");
        await Assertions.Expect(toggle).ToHaveTextAsync("Go offline");
        await toggle.ClickAsync();
        await Assertions.Expect(Badge(panel)).ToHaveTextAsync("offline");
        await Assertions.Expect(toggle).ToHaveTextAsync("Go online");
    }

    /// <summary>Brings a replica back online with its own button, and waits until its handshake is done.</summary>
    protected static async Task GoOnlineAsync(ILocator panel)
    {
        var toggle = panel.Locator(".panel-toggle");
        await Assertions.Expect(toggle).ToHaveTextAsync("Go online");
        await toggle.ClickAsync();
        await Assertions.Expect(toggle).ToHaveTextAsync("Go offline");
        await Assertions.Expect(Badge(panel)).ToHaveTextAsync("synced", new() { Timeout = 30_000 });
    }

    /// <summary>
    /// Types at the end of a text control one key at a time, as a person would: every key is its own input event,
    /// which the editor turns into its own minimal insertion.
    /// </summary>
    protected static async Task TypeAtEndAsync(ILocator editor, string text)
    {
        await editor.ClickAsync();
        await editor.PressAsync("Control+End");
        await editor.PressSequentiallyAsync(text);
    }

    /// <summary>
    /// Waits until every editor holds the same text and that text contains every one of
    /// <paramref name="fragments"/>, and returns it. Fails quoting what each editor held last.
    /// </summary>
    protected static async Task<string> ExpectConvergedAsync(IReadOnlyList<ILocator> editors, params string[] fragments)
    {
        string[] values = [];
        await EventuallyAsync(async () =>
        {
            values = await Task.WhenAll(editors.Select(e => e.InputValueAsync()));
            return values.Distinct(StringComparer.Ordinal).Count() == 1 && fragments.All(f => values[0].Contains(f, StringComparison.Ordinal));
        }, () => $"the {editors.Count} editors converged on one text containing {Quote(fragments)}; they hold {Quote(values)}", 30_000);
        return values[0];
    }

    /// <summary>
    /// Waits until the raw state the page projects for each replica (the JSON blocks under the panels) is the same
    /// character for character: the convergence property, read from the engine rather than from the controls.
    /// </summary>
    protected static async Task ExpectSameProjectionsAsync(IPage page, int replicas = 2)
    {
        var projections = page.Locator("pre.json");
        await Assertions.Expect(projections).ToHaveCountAsync(replicas);
        IReadOnlyList<string> texts = [];
        await EventuallyAsync(async () =>
        {
            texts = await projections.AllInnerTextsAsync();
            return texts.Distinct(StringComparer.Ordinal).Count() == 1;
        }, () => $"every replica projects the same state; they project {Quote(texts)}");
    }

    // ---- Waiting and asserting ----------------------------------------------------------------------------------

    /// <summary>Polls <paramref name="condition"/> until it holds, failing with <paramref name="what"/> when it never does.</summary>
    protected static async Task EventuallyAsync(Func<Task<bool>> condition, Func<string> what, int timeoutMs = 20_000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (true)
        {
            if (await condition()) return;
            if (DateTime.UtcNow > deadline) Assert.Fail($"timed out waiting until {what()}");
            await Task.Delay(100);
        }
    }

    /// <summary>
    /// Everything a page reports going wrong while a test drives it: console errors, uncaught exceptions, failed
    /// requests to the demo and error statuses from it. <paramref name="expected"/> names a problem the test
    /// provokes on purpose (the 404 of an unknown address), which is then not reported.
    /// </summary>
    protected static List<string> TrackProblems(IPage page, string origin, Func<string, bool>? expected = null)
    {
        var problems = new List<string>();
        void Add(string problem)
        {
            if (expected?.Invoke(problem) is true) return;
            lock (problems) problems.Add(problem);
        }

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

    protected static string Quote(IEnumerable<string> values) => "[" + string.Join(", ", values.Select(v => $"\"{v}\"")) + "]";
}
