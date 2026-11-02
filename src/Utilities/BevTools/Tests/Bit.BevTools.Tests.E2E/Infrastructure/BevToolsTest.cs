using Microsoft.Playwright;

namespace Bit.BevTools.Tests.E2E.Infrastructure;

/// <summary>
/// Base of every suite: gives each test a browser context of its own against the app the suite drives, waits until a
/// page runs in the browser, and fails a test whose page reported a console error, an unhandled exception or a failed
/// request on the way.
/// </summary>
public abstract class BevToolsTest
{
    private readonly List<IBrowserContext> _contexts = [];
    private readonly List<List<string>> _problems = [];
    private PlaywrightSession.Session? _session;

    public TestContext TestContext { get; set; } = default!;

    /// <summary>The origin the app under test is served from, without a trailing slash.</summary>
    protected abstract string BaseUrl { get; }

    protected IBrowser Browser => (_session ?? throw new InvalidOperationException("The session is opened in TestInitialize.")).Browser!;

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
        try
        {
            // Every page a test opened must have stayed free of errors, whatever the test asserted itself. A test
            // that failed already keeps its own message.
            if (TestContext.CurrentTestOutcome == UnitTestOutcome.Passed)
            {
                string[] found = [.. _problems.SelectMany(p => { lock (p) return p.ToArray(); })];
                Assert.IsEmpty(found, "the page reported:" + Environment.NewLine + string.Join(Environment.NewLine, found));
            }
        }
        finally
        {
            foreach (var context in _contexts)
            {
                try { await context.CloseAsync(); }
                catch (PlaywrightException) { /* the browser went away first */ }
            }
            _contexts.Clear();
            _problems.Clear();
        }
    }

    /// <summary>
    /// A fresh page in a browser context of its own, closed when the test ends, whose console errors, unhandled
    /// exceptions and failed requests fail the test (see <see cref="CloseContextsAsync"/>). The suite is hermetic:
    /// a request that leaves the app's origin is answered with an empty response here, so a runner without internet
    /// access neither waits out a connection timeout nor logs a load error.
    /// </summary>
    protected async Task<IPage> NewPageAsync()
    {
        var context = await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 900 },
        });
        _contexts.Add(context);

        var origin = BaseUrl;
        await context.RouteAsync(url => url.StartsWith(origin, StringComparison.OrdinalIgnoreCase) is false,
            route => route.FulfillAsync(new RouteFulfillOptions
            {
                Status = 200,
                Body = string.Empty,
                ContentType = route.Request.ResourceType == "stylesheet" ? "text/css" : "text/plain",
            }));

        var page = await context.NewPageAsync();
        _problems.Add(TrackProblems(page, origin));
        return page;
    }

    protected Task<IResponse?> GotoAsync(IPage page, string path) =>
        page.GotoAsync(BaseUrl + path, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 90_000 });

    /// <summary>
    /// Waits until the page runs in the browser. Every page is prerendered, so it is on screen long before anything
    /// listens to it; the layout's <c>data-interactive</c> marker turns true once the WebAssembly renderer has taken it
    /// over.
    /// </summary>
    protected static Task WaitForInteractiveAsync(IPage page) => page.WaitForFunctionAsync(
        "() => document.querySelector('.page')?.dataset.interactive === 'true'",
        null, new PageWaitForFunctionOptions { Timeout = 90_000 });

    /// <summary>Everything a page reports going wrong while a test drives it.</summary>
    private static List<string> TrackProblems(IPage page, string origin)
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
