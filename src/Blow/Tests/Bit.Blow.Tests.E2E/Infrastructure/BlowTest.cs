using System.Globalization;
using Microsoft.Playwright;

namespace Bit.Blow.Tests.E2E.Infrastructure;

/// <summary>
/// Base of every suite: gives each test a browser context of its own against the shared demo, waits for a
/// flow to be measured and ready for input, and holds the geometry probes the suites share. Every probe reads
/// what the browser actually laid out and painted (bounding boxes, the computed transform, the points an SVG
/// path really runs through), never the markup the component meant to produce.
/// </summary>
public abstract class BlowTest
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

    /// <summary>
    /// A fresh browser context, closed when the test ends. The suite is hermetic: a request that leaves the
    /// demo's origin (the demo's web font, today) is answered with an empty response here, so a runner
    /// without internet access neither waits out a connection timeout nor logs a load error for it.
    /// </summary>
    protected async Task<IBrowserContext> NewContextAsync(BrowserNewContextOptions? options = null)
    {
        var context = await Browser.NewContextAsync(options ?? new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 900 },
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

    /// <summary>
    /// Opens a page of the demo in a context of its own, waits until its flows are measured, and scrolls the
    /// first flow fully into view.
    /// </summary>
    protected async Task<IPage> OpenAsync(string path, BrowserNewContextOptions? options = null)
    {
        var page = await (await NewContextAsync(options)).NewPageAsync();
        await GotoAsync(page, path);
        await WaitForFlowAsync(page);
        await ScrollFlowIntoViewAsync(page);
        return page;
    }

    /// <summary>
    /// The demo sizes its canvas to the window and puts a header above it, so the bottom of a flow (the
    /// minimap, the controls) starts out below the fold, where the mouse cannot reach it. Scrolling moves the
    /// pane on screen without resizing anything; the browser layer's scroll listener tells the canvas, which
    /// re-reads the pane rectangle every pointer and wheel position is converted with. That costs a frame and a
    /// call into .NET, and nothing in the DOM changes when it is done, hence the short wait.
    /// </summary>
    protected static async Task ScrollFlowIntoViewAsync(IPage page)
    {
        var scrolled = await page.EvaluateAsync<bool>(
            """
            () => {
              const pane = document.querySelector('.bit-blow');
              if (!pane) return false;
              const before = window.scrollY;
              pane.scrollIntoView({ block: 'nearest', inline: 'nearest' });
              return window.scrollY !== before;
            }
            """);
        if (scrolled) await page.WaitForTimeoutAsync(250);
    }

    protected Task<IResponse?> GotoAsync(IPage page, string path) =>
        page.GotoAsync(BaseUrl + path, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 90_000 });

    /// <summary>
    /// Waits until the page runs in the browser and its flows have settled. Every page is prerendered, so the
    /// nodes and edges are on screen long before anything listens to them, and before the browser layer has
    /// measured a single node: edges are first drawn to fallback anchors, then redrawn to the measured handles,
    /// and a flow that fits its view moves once more after that. The layout's <c>data-interactive</c> marker
    /// says the handlers are attached; a quiet DOM afterwards says the measure-and-fit passes are done.
    /// </summary>
    protected static async Task WaitForFlowAsync(IPage page)
    {
        await page.WaitForFunctionAsync(
            "() => document.querySelector('[data-interactive]')?.dataset.interactive === 'true'",
            null, new PageWaitForFunctionOptions { Timeout = 90_000 });

        await WaitForQuietAsync(page);
    }

    /// <summary>
    /// Waits until no flow on the page has changed for a moment: a gesture's effect, a re-measure or a viewport
    /// transition has been rendered. Gives up quietly after a few seconds, for a page that animates for good.
    /// </summary>
    protected static Task WaitForQuietAsync(IPage page, int quietMs = 300) => page.EvaluateAsync(
        """
        (quietMs) => new Promise(resolve => {
          const roots = [...document.querySelectorAll('.bit-blow')];
          if (roots.length === 0) { requestAnimationFrame(() => requestAnimationFrame(resolve)); return; }
          let timer;
          const done = () => { observer.disconnect(); clearTimeout(cap); resolve(); };
          const arm = () => { clearTimeout(timer); timer = setTimeout(done, quietMs); };
          const observer = new MutationObserver(arm);
          roots.forEach(r => observer.observe(r, { subtree: true, childList: true, attributes: true, characterData: true }));
          const cap = setTimeout(done, 5000);
          arm();
        })
        """, quietMs);

    /// <summary>
    /// Waits until every edge drawn with the default path starts and ends on the centre of a handle. That only
    /// holds once the browser layer has measured the handles: before it, edges run to fallback anchors.
    /// </summary>
    protected static Task WaitForEdgesAnchoredAsync(IPage page) => page.WaitForFunctionAsync(
        """
        () => {
          const centres = [...document.querySelectorAll('.bit-blow-handle')].map(h => {
            const r = h.getBoundingClientRect();
            return [r.left + r.width / 2, r.top + r.height / 2];
          });
          const near = p => centres.some(c => Math.hypot(c[0] - p.x, c[1] - p.y) < 1.5);
          const paths = [...document.querySelectorAll('.bit-blow-edge > .bit-blow-edge-path')];
          return paths.length > 0 && paths.every(p => {
            if (!p.getAttribute('d')) return false;
            const m = p.getScreenCTM(), length = p.getTotalLength();
            return near(p.getPointAtLength(0).matrixTransform(m)) && near(p.getPointAtLength(length).matrixTransform(m));
          });
        }
        """, null, new PageWaitForFunctionOptions { Timeout = 20_000 });

    // ---- geometry probes -------------------------------------------------------------------------

    /// <summary>The on-screen box of the element <paramref name="selector"/> picks (the first one).</summary>
    protected static async Task<Box> BoxAsync(IPage page, string selector)
    {
        var b = await page.EvaluateAsync<double[]?>(
            """
            (selector) => {
              const el = document.querySelector(selector);
              if (!el) return null;
              const r = el.getBoundingClientRect();
              return [r.left, r.top, r.width, r.height];
            }
            """, selector);
        Assert.IsNotNull(b, $"nothing on the page matches {selector}");
        return new Box(b[0], b[1], b[2], b[3]);
    }

    protected static Task<Box> NodeBoxAsync(IPage page, string nodeId) => BoxAsync(page, $".bit-blow-node[data-id=\"{nodeId}\"]");

    protected static async Task<Point> HandleCentreAsync(IPage page, string nodeId, string type) =>
        (await BoxAsync(page, $".bit-blow-node[data-id=\"{nodeId}\"] .bit-blow-handle[data-handle-type=\"{type}\"]")).Centre;

    /// <summary>The on-screen box of a flow's pane (its <c>.bit-blow</c> root).</summary>
    protected static async Task<Box> PaneBoxAsync(IPage page, int flow = 0)
    {
        var b = await page.EvaluateAsync<double[]>(
            "(i) => { const r = document.querySelectorAll('.bit-blow')[i].getBoundingClientRect(); return [r.left, r.top, r.width, r.height]; }", flow);
        return new Box(b[0], b[1], b[2], b[3]);
    }

    /// <summary>
    /// Where an edge's visible path really starts and ends on screen, read from the painted geometry (the path's
    /// own points through its screen transform) rather than parsed out of its <c>d</c> attribute.
    /// </summary>
    protected static async Task<(Point Start, Point End)> EdgeEndsAsync(IPage page, string edgeId)
    {
        var e = await page.EvaluateAsync<double[]?>(
            """
            (id) => {
              const path = document.querySelector(`.bit-blow-edge[data-id="${CSS.escape(id)}"] > .bit-blow-edge-path`);
              if (!path || !path.getAttribute('d')) return null;
              const m = path.getScreenCTM(), length = path.getTotalLength();
              const a = path.getPointAtLength(0).matrixTransform(m), b = path.getPointAtLength(length).matrixTransform(m);
              return [a.x, a.y, b.x, b.y];
            }
            """, edgeId);
        Assert.IsNotNull(e, $"edge {edgeId} is not drawn");
        return (new Point(e[0], e[1]), new Point(e[2], e[3]));
    }

    /// <summary>The ids of the nodes, or the edges, currently in the DOM.</summary>
    protected static Task<string[]> IdsAsync(IPage page, string kind = "node", int flow = 0) => page.EvaluateAsync<string[]>(
        "([kind, i]) => [...document.querySelectorAll('.bit-blow')[i].querySelectorAll(`.bit-blow-${kind}[data-id]`)].map(e => e.dataset.id)",
        new object[] { kind, flow });

    /// <summary>The ids of the selected nodes.</summary>
    protected static Task<string[]> SelectedNodesAsync(IPage page) => page.EvaluateAsync<string[]>(
        "() => [...document.querySelectorAll('.bit-blow-node.selected')].map(e => e.dataset.id)");

    /// <summary>The viewport as the browser actually paints it: the computed transform of the viewport layer.</summary>
    protected static async Task<Viewport> ViewportAsync(IPage page, int flow = 0)
    {
        var m = await page.EvaluateAsync<double[]>(
            """
            (i) => {
              const vp = document.querySelectorAll('.bit-blow-viewport')[i];
              const m = new DOMMatrix(getComputedStyle(vp).transform);
              return [m.e, m.f, m.a];
            }
            """, flow);
        return new Viewport(m[0], m[1], m[2]);
    }

    /// <summary>
    /// The visible area the minimap outlines, in flow units: the inner rectangle of its mask, which the
    /// component sizes from the pane size it last measured.
    /// </summary>
    protected static async Task<Box> MiniMapWindowAsync(IPage page)
    {
        var d = await page.Locator(".bit-blow-minimap-mask").First.GetAttributeAsync("d");
        Assert.IsNotNull(d, "the minimap has no mask");
        // "M{x},{y}v{height}h{width}v{-height}z" is the second subpath.
        var inner = d.Split('M')[2];
        var numbers = System.Text.RegularExpressions.Regex.Matches(inner, @"-?\d+(\.\d+)?(E-?\d+)?")
            .Select(m => double.Parse(m.Value, CultureInfo.InvariantCulture)).ToArray();
        return new Box(numbers[0], numbers[1], numbers[3], numbers[2]);
    }

    // ---- input --------------------------------------------------------------------------------------

    /// <summary>
    /// Presses at <paramref name="from"/>, moves to <paramref name="to"/> in steps and releases. The pane only
    /// listens for pointer moves while a gesture runs (it attaches the listener on the render the press causes),
    /// so the first move waits a moment after the press, as a hand would.
    /// </summary>
    protected static async Task DragAsync(IPage page, Point from, Point to, int steps = 10, bool release = true)
    {
        await page.Mouse.MoveAsync((float)from.X, (float)from.Y);
        await page.Mouse.DownAsync();
        await page.WaitForTimeoutAsync(60);
        await page.Mouse.MoveAsync((float)to.X, (float)to.Y, new MouseMoveOptions { Steps = steps });
        await page.WaitForTimeoutAsync(60);
        if (release is false) return;
        await page.Mouse.UpAsync();
        await WaitForQuietAsync(page);
    }

    protected static async Task WheelAsync(IPage page, Point at, double deltaY)
    {
        await page.Mouse.MoveAsync((float)at.X, (float)at.Y);
        await page.Mouse.WheelAsync(0, (float)deltaY);
        await WaitForQuietAsync(page);
    }

    /// <summary>
    /// Imports the library's browser module, the same instance the component uses, so a test can call one of
    /// its exports against the live DOM. <paramref name="body"/> sees it as <c>m</c>.
    /// </summary>
    protected static Task<T> WithModuleAsync<T>(IPage page, string body, object? arg = null) => page.EvaluateAsync<T>(
        $"async (arg) => {{ const m = await import('/_content/Bit.Blow/bit-blow.js'); {body} }}", arg);

    // ---- waiting and asserting ---------------------------------------------------------------------

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

    protected static void AssertNear(double expected, double actual, double tolerance, string what) =>
        Assert.IsTrue(Math.Abs(expected - actual) <= tolerance,
            $"{what}: expected {expected:0.##} (within {tolerance:0.##}) but was {actual:0.##}");

    protected static void AssertNear(Point expected, Point actual, double tolerance, string what) =>
        Assert.IsTrue(expected.DistanceTo(actual) <= tolerance,
            $"{what}: expected {expected} (within {tolerance:0.##}px) but was {actual}");

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

    protected readonly record struct Point(double X, double Y)
    {
        public Point Offset(double dx, double dy) => new(X + dx, Y + dy);

        public double DistanceTo(Point other) => Math.Sqrt((X - other.X) * (X - other.X) + (Y - other.Y) * (Y - other.Y));

        public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"({X:0.#}, {Y:0.#})");
    }

    protected readonly record struct Box(double X, double Y, double Width, double Height)
    {
        public Point Centre => new(X + Width / 2, Y + Height / 2);

        public double Right => X + Width;

        public double Bottom => Y + Height;

        public bool Contains(Point p) => p.X >= X && p.X <= Right && p.Y >= Y && p.Y <= Bottom;

        public bool Contains(Box b) => b.X >= X - 0.5 && b.Right <= Right + 0.5 && b.Y >= Y - 0.5 && b.Bottom <= Bottom + 0.5;

        public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"[{X:0.#}, {Y:0.#}, {Width:0.#} x {Height:0.#}]");
    }

    /// <summary>The viewport transform: a screen point is <c>pane + (X, Y) + flow * Zoom</c>.</summary>
    protected readonly record struct Viewport(double X, double Y, double Zoom)
    {
        public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"(x {X:0.##}, y {Y:0.##}, zoom {Zoom:0.###})");
    }
}
