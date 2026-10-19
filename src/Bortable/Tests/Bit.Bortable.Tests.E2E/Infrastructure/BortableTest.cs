using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bortable.Tests.E2E.Infrastructure;

/// <summary>
/// Base of every suite: gives each test a browser context of its own against the shared demo, waits for a page's
/// lists to be ready for a drag, drives native HTML5 drags with the real mouse, and holds the probes the suites
/// share. Every assertion reads what the page actually shows: the order readouts the demo binds to the lists, the
/// items in DOM order, the classes the drag states put on them.
/// </summary>
public abstract class BortableTest
{
    /// <summary>The marker every list renders on its container.</summary>
    protected const string ListSelector = "[data-bortable-list]";

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

    /// <summary>
    /// A fresh browser context, closed when the test ends. The suite is hermetic: a request that leaves the
    /// demo's origin (the demo's web font, today) is answered with an empty response here, so a runner
    /// without internet access neither waits out a connection timeout nor logs a load error for it.
    /// </summary>
    /// <remarks>
    /// The viewport is tall enough for the longest demo page (the nested tree, the plugin pages with a list above
    /// two more) to fit without scrolling: a native drag cannot be aimed at a point below the fold, and the
    /// browser's own auto-scroll near the edge would move the lists under a pointer the test holds still.
    /// </remarks>
    protected async Task<IBrowserContext> NewContextAsync(BrowserNewContextOptions? options = null)
    {
        var context = await Browser.NewContextAsync(options ?? new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 1400 },
            DeviceScaleFactor = 1,
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

        return context;
    }

    /// <summary>Opens a page of the demo in a context of its own and waits until its lists take a drag.</summary>
    protected async Task<IPage> OpenAsync(string path, BrowserNewContextOptions? options = null)
    {
        var page = await (await NewContextAsync(options)).NewPageAsync();
        await GotoAsync(page, path);
        await WaitForListsAsync(page);
        return page;
    }

    protected Task<IResponse?> GotoAsync(IPage page, string path) =>
        page.GotoAsync(BaseUrl + path, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 90_000 });

    /// <summary>
    /// Waits until the page runs in the browser and its lists can take a drag. Every page is prerendered, so the
    /// lists are on screen long before anything listens to them. The layout's <c>data-interactive</c> marker says
    /// the handlers are attached; but a list only finds the drop position (and installs its drag handle) through
    /// <c>bit-bortable.js</c>, which the lists import after their first interactive render, and until that import
    /// has resolved every dragover is answered with "keep the current order". Nothing in the DOM changes when it
    /// resolves, so the wait is for the module to have been fetched and the page to have stayed quiet a moment
    /// after: the import resolves, and .NET picks the module up, within the same few milliseconds.
    /// </summary>
    protected static async Task WaitForListsAsync(IPage page)
    {
        await page.WaitForFunctionAsync(
            "() => document.querySelector('[data-interactive]')?.dataset.interactive === 'true'",
            null, new PageWaitForFunctionOptions { Timeout = 90_000 });

        if (await page.Locator(ListSelector).CountAsync() > 0)
        {
            await page.WaitForFunctionAsync(
                // The import map points the module at its fingerprinted file, bit-bortable.<hash>.js.
                @"() => performance.getEntriesByType('resource').some(e => /^\/_content\/Bit\.Bortable\/bit-bortable(\.\w+)?\.js$/.test(new URL(e.name).pathname))",
                null, new PageWaitForFunctionOptions { Timeout = 30_000 });
        }

        await WaitForQuietAsync(page);
    }

    // ---- waiting for the page to settle ----------------------------------------------------------------

    /// <summary>
    /// Installs (once per document) the mutation counter <see cref="SettleAsync"/> and <see cref="WaitForQuietAsync"/>
    /// read, and returns the count so far. It watches the whole document: a drag changes the lists, the readouts
    /// under them and, on the events page, the log, and any of those is a sign that a handler acted.
    /// </summary>
    private static Task<int> MutationCountAsync(IPage page) => page.EvaluateAsync<int>(
        """
        () => {
          if (!window.__bortableE2E) {
            const state = { count: 0, last: performance.now() };
            new MutationObserver(records => { state.count += records.length; state.last = performance.now(); })
              .observe(document.documentElement, { subtree: true, childList: true, attributes: true, characterData: true });
            window.__bortableE2E = state;
          }
          return window.__bortableE2E.count;
        }
        """);

    /// <summary>
    /// Waits until nothing in the page has changed for <paramref name="quietMs"/>: a drag's effect has been rendered,
    /// and the FLIP animations it started (150 ms on every demo list) have finished and cleaned up after themselves.
    /// Gives up quietly after a few seconds, for a page that never stops changing.
    /// </summary>
    protected static async Task WaitForQuietAsync(IPage page, int quietMs = 300)
    {
        await MutationCountAsync(page);
        await page.EvaluateAsync(
            """
            (quietMs) => new Promise(resolve => {
              const state = window.__bortableE2E;
              const started = performance.now();
              const check = () => {
                const now = performance.now();
                if (now - state.last >= quietMs && now - started >= quietMs || now - started > 5000) { resolve(); return; }
                setTimeout(check, 25);
              };
              check();
            })
            """, quietMs);
    }

    // ---- locating items ------------------------------------------------------------------------------

    /// <summary>
    /// The item of the list <paramref name="listId"/> labelled <paramref name="label"/>: one of the list's own items
    /// (its direct children, never the drop placeholder), whose text is exactly the label. Labels repeat across the
    /// lists of a page ("Item 1" is on both sides of the plugin demos), so an item is always looked up in its list.
    /// The match ignores leading symbols, so a row whose handle glyph precedes its text is found by the text.
    /// </summary>
    protected static ILocator Item(IPage page, string listId, string label) =>
        page.Locator($"#{listId} > [data-bortable-item]:not([data-bortable-placeholder])")
            .Filter(new LocatorFilterOptions { HasTextRegex = new Regex(@"^\W*" + Regex.Escape(label) + @"\s*$") });

    /// <summary>The drop placeholder a list shows while an item from another list is previewed in it.</summary>
    protected static ILocator Placeholder(IPage page, string listId) =>
        page.Locator($"#{listId} > [data-bortable-placeholder]");

    /// <summary>
    /// The labels of a list's items in DOM order. With <paramref name="withPlaceholder"/> the drop placeholder is
    /// listed too, as <c>[placeholder: label]</c>, where it sits. A nested item is labelled by its own name, not by the
    /// text of the list it holds; a row with a handle by its text without the handle glyph.
    /// </summary>
    protected static Task<string[]> LabelsAsync(IPage page, string listId, bool withPlaceholder = false) => page.EvaluateAsync<string[]>(
        """
        ([listId, withPlaceholder]) => {
          const list = document.getElementById(listId);
          if (!list) return [];
          const label = el => {
            const name = el.querySelector(':scope > .nested-item__name');
            if (name) return name.textContent.trim();
            // Text and elements only: Blazor marks the bounds of components and regions with comment nodes.
            return [...el.childNodes]
              .filter(n => n.nodeType === Node.TEXT_NODE || n.nodeType === Node.ELEMENT_NODE && !n.matches('.handle'))
              .map(n => n.textContent).join('').trim();
          };
          return [...list.children]
            .filter(c => c.hasAttribute('data-bortable-item'))
            .filter(c => withPlaceholder || !c.hasAttribute('data-bortable-placeholder'))
            .map(c => c.hasAttribute('data-bortable-placeholder') ? `[placeholder: ${label(c)}]` : label(c));
        }
        """, new object[] { listId, withPlaceholder });

    /// <summary>The on-screen box of the element <paramref name="locator"/> picks.</summary>
    protected static async Task<Box> BoxAsync(ILocator locator)
    {
        var b = await locator.BoundingBoxAsync();
        Assert.IsNotNull(b, $"{locator} is not on screen");
        return new Box(b.X, b.Y, b.Width, b.Height);
    }

    /// <summary>The on-screen box of a list's container.</summary>
    protected static Task<Box> ListBoxAsync(IPage page, string listId) => BoxAsync(page.Locator($"#{listId}"));

    // ---- input ---------------------------------------------------------------------------------------

    /// <summary>
    /// Drags from <paramref name="from"/> to <paramref name="to"/> with the real mouse and drops there: a native
    /// HTML5 drag, exactly as a hand makes one (see <see cref="PressAsync"/> and <see cref="MoveAsync"/>).
    /// </summary>
    protected static async Task DragAsync(IPage page, Point from, Point to, int steps = 12)
    {
        await PressAsync(page, from);
        await MoveAsync(page, to, steps);
        await ReleaseAsync(page);
    }

    /// <summary>
    /// Presses at <paramref name="at"/> and moves a few pixels away with the button held. Chromium starts a native
    /// drag (and fires <c>dragstart</c>) only once the pointer has moved more than 4 pixels from the press, and only
    /// on an element that is <c>draggable</c> at that moment; anything else becomes a text selection, and the
    /// moves that follow fire no drag events at all.
    /// </summary>
    protected static async Task PressAsync(IPage page, Point at)
    {
        AssertInViewport(page, at);
        await page.Mouse.MoveAsync((float)at.X, (float)at.Y);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync((float)at.X + 6, (float)at.Y + 6, new MouseMoveOptions { Steps = 3 });
        await SettleAsync(page, at.Offset(6, 6));
    }

    /// <summary>
    /// Moves the held pointer to <paramref name="to"/> in steps, as a hand would (every list on the way sees the
    /// item pass), and holds it there until the lists have acted on the final position.
    /// </summary>
    protected static async Task MoveAsync(IPage page, Point to, int steps = 12)
    {
        AssertInViewport(page, to);
        await page.Mouse.MoveAsync((float)to.X, (float)to.Y, new MouseMoveOptions { Steps = steps });
        await SettleAsync(page, to);
    }

    /// <summary>
    /// A pointer position outside the viewport is clamped by the browser to its edge, where the drag would land
    /// somewhere else entirely; fail with the reason instead (a demo page that grew past the viewport, usually).
    /// </summary>
    private static void AssertInViewport(IPage page, Point point)
    {
        var size = page.ViewportSize;
        if (size is null) return;
        Assert.IsTrue(point.X >= 0 && point.Y >= 0 && point.X < size.Width && point.Y < size.Height,
            $"the pointer target {point} is outside the {size.Width} x {size.Height} viewport");
    }

    /// <summary>Releases the button where the pointer is (a drop, when a drag is running) and waits for its effect.</summary>
    protected static async Task ReleaseAsync(IPage page)
    {
        await page.Mouse.UpAsync();
        await WaitForQuietAsync(page);
    }

    /// <summary>
    /// Holds the pointer at <paramref name="at"/> until the lists stop changing. A hand's browser repeats
    /// <c>dragover</c> every 50 ms while the pointer rests; under Playwright, Chromium fires it only when the pointer
    /// moves. And a list skips a dragover that arrives while it is still handling the previous one in .NET (the drop
    /// position is measured by a JS interop call), or that lands on an item still sliding into place. So the last
    /// position of a move may never have been acted on. Each nudge (a pixel off and back) fires a fresh dragover at
    /// the same point after the page went quiet; once a nudge changes nothing, every list has seen where the pointer
    /// is. Without a drag running, the first nudge changes nothing and this returns at once.
    /// </summary>
    protected static async Task SettleAsync(IPage page, Point at)
    {
        await WaitForQuietAsync(page, 200);
        for (int i = 0; i < 15; i++)
        {
            var before = await MutationCountAsync(page);
            await page.Mouse.MoveAsync((float)at.X + 1, (float)at.Y);
            await page.Mouse.MoveAsync((float)at.X, (float)at.Y);
            await WaitForQuietAsync(page, 200);
            if (await MutationCountAsync(page) == before) return;
        }
    }

    // ---- waiting and asserting ---------------------------------------------------------------------

    /// <summary>
    /// Asserts the order of a list twice over: the readout the demo binds to the list (what .NET holds), and the
    /// list's items in DOM order (what the browser shows). Both poll, so a render still on its way is waited for.
    /// </summary>
    protected static async Task AssertOrderAsync(IPage page, string listId, string readoutId, params string[] expected)
    {
        await Expect(page.Locator($"#{readoutId}")).ToHaveTextAsync(string.Join(", ", expected));
        await AssertLabelsAsync(page, listId, expected);
    }

    /// <summary>
    /// Asserts what a list shows right now, mid-drag included (see <see cref="LabelsAsync"/>), polling: a render a
    /// slow runner has not painted yet when the pointer settled is waited for.
    /// </summary>
    protected static async Task AssertLabelsAsync(IPage page, string listId, string[] expected, bool withPlaceholder = false, string? because = null)
    {
        string[] shown = [];
        await EventuallyAsync(async () => (shown = await LabelsAsync(page, listId, withPlaceholder)).SequenceEqual(expected),
            $"#{listId} shows [{string.Join(", ", expected)}] (it shows [{string.Join(", ", shown)}]){(because is null ? "" : ": " + because)}");
    }

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

    /// <summary>A class name as a pattern for <c>ToHaveClassAsync</c>, which matches the whole attribute.</summary>
    protected static Regex HasClass(string name) => new($@"(^|\s){Regex.Escape(name)}(\s|$)");

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

    protected static string[] Snapshot(List<string> problems)
    {
        lock (problems) return [.. problems];
    }

    protected readonly record struct Point(double X, double Y)
    {
        public Point Offset(double dx, double dy) => new(X + dx, Y + dy);

        public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"({X:0.#}, {Y:0.#})");
    }

    protected readonly record struct Box(double X, double Y, double Width, double Height)
    {
        public Point Centre => new(X + Width / 2, Y + Height / 2);

        public double Right => X + Width;

        public double Bottom => Y + Height;

        /// <summary>A point inside the box, as fractions of its width and height from its top left corner.</summary>
        public Point At(double fx, double fy) => new(X + Width * fx, Y + Height * fy);

        public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"[{X:0.#}, {Y:0.#}, {Width:0.#} x {Height:0.#}]");
    }
}
