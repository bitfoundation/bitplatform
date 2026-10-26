using System.Globalization;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bdnd.Tests.E2E.Infrastructure;

/// <summary>
/// Base of every suite: gives each test a browser context of its own against the shared demo, waits until a
/// page runs in the browser and its draggables and drop targets are registered with the browser module, drives
/// native HTML5 drags with the real mouse, and fails a test whose page reported a console error, an unhandled
/// exception or a failed request on the way.
/// </summary>
public abstract class BdndTest
{
    /// <summary>
    /// Runs before any script of every page and frame, and does two things.
    /// <para>
    /// The browser module registers a draggable or a drop target by adding element-level listeners to its host
    /// (<c>dragstart</c> for a source, <c>dragenter</c> for a target), and that registration is the moment a host
    /// starts working: until then a drag from it is not tagged and the managed handler ignores it, and a target
    /// refuses every drop. Nothing in the DOM says it happened, so the listeners are counted here, which lets
    /// <see cref="WaitForBdndAsync(IFrame)"/> wait for exactly that instead of sleeping. Blazor's own listeners are
    /// on the document, so only the module's are counted.
    /// </para>
    /// <para>
    /// It also records the native drag events as they finish bubbling, on the window: by then the module's
    /// element-level listeners have tagged the <c>dragstart</c>, and accepted or refused each <c>dragover</c>
    /// (<c>preventDefault</c> and <c>dropEffect</c>), so the record is what the browser itself acts on.
    /// </para>
    /// </summary>
    internal const string BrowserProbe =
        """
        (() => {
          const state = {
            sources: new WeakSet(), registrations: 0, last: 0,
            dragstarts: [], lastDragOver: null, drops: [], dragends: 0,
          };
          Object.defineProperty(window, '__bdndE2E', { value: state });
          window.addEventListener('dragstart', e => {
            const dt = e.dataTransfer, types = dt ? [...dt.types] : [];
            const tag = types.find(t => t.startsWith('application/x-bit-bdnd-')) ?? null;
            state.dragstarts.push({
              x: e.clientX, y: e.clientY, types, tag,
              tagData: tag ? dt.getData(tag) : null, effectAllowed: dt ? dt.effectAllowed : null,
            });
          });
          window.addEventListener('dragover', e => {
            state.lastDragOver = {
              target: e.target, x: e.clientX, y: e.clientY,
              prevented: e.defaultPrevented, dropEffect: e.dataTransfer ? e.dataTransfer.dropEffect : null,
            };
          });
          window.addEventListener('drop', e => state.drops.push({ target: e.target, prevented: e.defaultPrevented }));
          window.addEventListener('dragend', () => state.dragends++);
          const add = EventTarget.prototype.addEventListener, remove = EventTarget.prototype.removeEventListener;
          EventTarget.prototype.addEventListener = function (type, listener, options) {
            if (this instanceof Element && (type === 'dragstart' || type === 'dragenter')) {
              if (type === 'dragstart') state.sources.add(this);
              state.registrations++;
              state.last = performance.now();
            }
            return add.call(this, type, listener, options);
          };
          EventTarget.prototype.removeEventListener = function (type, listener, options) {
            if (this instanceof Element && (type === 'dragstart' || type === 'dragenter')) {
              if (type === 'dragstart') state.sources.delete(this);
              state.last = performance.now();
            }
            return remove.call(this, type, listener, options);
          };
        })();
        """;

    /// <summary>Every element a draggable renders as its host carries a <c>draggable</c> attribute; a drag handle does too, but it is registered through its draggable.</summary>
    protected const string DraggableHosts = "[draggable]:not([data-bit-bdnd-handle])";

    private readonly List<IBrowserContext> _contexts = [];
    private readonly List<List<string>> _problems = [];
    private PlaywrightSession.Session? _session;

    public TestContext TestContext { get; set; } = default!;

    /// <summary>The origin the demo is served from, without a trailing slash.</summary>
    protected string BaseUrl => Session.Host!.BaseUrl;

    protected IBrowser Browser => Session.Browser!;

    /// <summary>Whether the suite drives Chromium, the only engine whose DevTools protocol synthesizes touches.</summary>
    protected bool IsChromium => Browser.BrowserType.Name == "chromium";

    /// <summary>Skips a test that needs Chromium's DevTools protocol on the other engines.</summary>
    protected void RequireChromium(string why)
    {
        if (IsChromium is false) Assert.Inconclusive($"{Browser.BrowserType.Name}: {why}");
    }

    private PlaywrightSession.Session Session => _session ?? throw new InvalidOperationException("The session is opened in TestInitialize.");

    /// <summary>Whether the tests drive the demo in the suite's browser (the hybrid tests drive an app of their own).</summary>
    protected virtual bool UsesDemo => true;

    [TestInitialize]
    public async Task OpenSessionAsync()
    {
        if (UsesDemo is false) return;
        _session = await PlaywrightSession.GetAsync();
        if (_session.Unavailable is not { } reason)
        {
            await PlaywrightSession.EnsureBrowserAsync(_session);
            return;
        }

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
    /// a request that leaves the demo's origin is answered with an empty response here, so a runner without
    /// internet access neither waits out a connection timeout nor logs a load error.
    /// </summary>
    protected async Task<IPage> NewPageAsync(bool hasTouch = false)
    {
        var context = await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 900 },
            DeviceScaleFactor = 1,
            HasTouch = hasTouch,
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
        await context.AddInitScriptAsync(BrowserProbe);

        var page = await context.NewPageAsync();
        _problems.Add(TrackProblems(page, origin));
        return page;
    }

    /// <summary>
    /// Opens a page of the demo and waits until it runs in the browser and, unless <paramref name="bdnd"/> is false
    /// (a page whose drags all happen in an iframe), its draggables and drop targets have been registered with the
    /// browser module (see <see cref="WaitForBdndAsync"/>).
    /// </summary>
    protected async Task<IPage> OpenAsync(string path, bool bdnd = true, bool hasTouch = false)
    {
        var page = await NewPageAsync(hasTouch);
        var response = await GotoAsync(page, path);
        Assert.AreEqual(200, response?.Status, $"{path} answered {response?.Status}");
        await WaitForInteractiveAsync(page.MainFrame);
        if (bdnd) await WaitForBdndAsync(page.MainFrame);
        return page;
    }

    protected Task<IResponse?> GotoAsync(IPage page, string path) =>
        page.GotoAsync(BaseUrl + path, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 90_000 });

    /// <summary>
    /// Waits until the document of <paramref name="frame"/> runs in the browser. Every page is prerendered, so its
    /// boxes and bins are on screen long before anything listens to them; the layout's <c>data-interactive</c>
    /// marker turns true once the WebAssembly renderer has taken the page over.
    /// </summary>
    protected static Task WaitForInteractiveAsync(IFrame frame) => frame.WaitForFunctionAsync(
        "() => document.querySelector('.page')?.dataset.interactive === 'true'",
        null, new FrameWaitForFunctionOptions { Timeout = 90_000 });

    /// <summary>
    /// Waits until the browser module has registered something in the document of <paramref name="frame"/>, every
    /// draggable host in it has been registered, and no registration has come or gone for a moment. Called when a
    /// page opens and before every drag, since a drop can re-render draggables (a knight on a new square is a new
    /// draggable, registered asynchronously after the render that created it).
    /// </summary>
    protected static Task WaitForBdndAsync(IFrame frame) => frame.WaitForFunctionAsync(
        """
        (hosts) => {
          const s = window.__bdndE2E;
          return !!s && s.registrations > 0
            && [...document.querySelectorAll(hosts)].every(h => s.sources.has(h))
            && performance.now() - s.last >= 150;
        }
        """, DraggableHosts, new FrameWaitForFunctionOptions { Timeout = 20_000, PollingInterval = 50 });

    /// <summary>
    /// Waits until nothing in the page's document has changed for a moment: the re-render a drag, a hover or a drop
    /// caused has reached the DOM. Gives up quietly after a few seconds.
    /// </summary>
    protected static Task WaitForQuietAsync(IPage page, int quietMs = 200) => page.EvaluateAsync(
        """
        (quietMs) => new Promise(resolve => {
          let timer;
          const done = () => { observer.disconnect(); clearTimeout(cap); resolve(); };
          const arm = () => { clearTimeout(timer); timer = setTimeout(done, quietMs); };
          const observer = new MutationObserver(arm);
          observer.observe(document.body, { subtree: true, childList: true, attributes: true, characterData: true });
          const cap = setTimeout(done, 5000);
          arm();
        })
        """, quietMs);

    // ---- dragging with the mouse ----------------------------------------------------------------------
    //
    // Chromium turns Playwright's mouse into a native drag: once the pointer has moved a few pixels with the button
    // down on a draggable element, the browser raises dragstart; from then on a move onto another element raises
    // dragenter and dragleave, a move over the same element a dragover, and the release is a drop where the last
    // answer was "accepted", followed by dragend on the source. The events are real, with a real DataTransfer, so
    // they go through the browser module and Blazor's dispatcher exactly as a hand-made drag does. The pointer goes
    // to whole pixels only, because the browser reports drag events in whole pixels: a distance a test computes
    // from the points it moved to is then exactly the distance the library computes from the events.

    /// <summary>
    /// Presses on <paramref name="from"/> and moves far enough for the browser to start a native drag, without
    /// releasing. Waits first until the draggables in <paramref name="frame"/> (the page's own document by
    /// default) are registered, so the drag is one the library knows about.
    /// </summary>
    protected static async Task StartDragAsync(IPage page, Point from, IFrame? frame = null, double dx = 6, double dy = 6)
    {
        await WaitForBdndAsync(frame ?? page.MainFrame);
        from = from.Rounded();
        await page.Mouse.MoveAsync((float)from.X, (float)from.Y);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync((float)(from.X + dx), (float)(from.Y + dy), new MouseMoveOptions { Steps = 3 });
    }

    /// <inheritdoc cref="StartDragAsync(IPage, Point, IFrame?, double, double)"/>
    protected static async Task StartDragAsync(IPage page, ILocator source) =>
        await StartDragAsync(page, (await BoxAsync(source)).Centre);

    /// <summary>Moves the pointer of a drag in progress to <paramref name="to"/>.</summary>
    protected static Task MoveAsync(IPage page, Point to, int steps = 8)
    {
        to = to.Rounded();
        return page.Mouse.MoveAsync((float)to.X, (float)to.Y, new MouseMoveOptions { Steps = steps });
    }

    /// <inheritdoc cref="MoveAsync(IPage, Point, int)"/>
    protected static async Task MoveAsync(IPage page, ILocator to, int steps = 8) =>
        await MoveAsync(page, (await BoxAsync(to)).Centre, steps);

    /// <summary>
    /// A whole drag from the centre of <paramref name="source"/> to the centre of <paramref name="target"/>: press,
    /// start, move over the target, wait until <paramref name="whileOver"/> holds (the hover the test expects, as
    /// rendered), release.
    /// </summary>
    protected static async Task DragAsync(IPage page, ILocator source, ILocator target, Func<Task>? whileOver = null)
    {
        var to = (await BoxAsync(target)).Centre;
        await StartDragAsync(page, source);
        await DragToAsync(page, to, whileOver);
    }

    /// <summary>
    /// Moves a drag in progress to <paramref name="to"/>, waits for <paramref name="whileOver"/>, and releases
    /// exactly there. A target decides whether it takes a drop on every <c>dragover</c>, from what .NET last told
    /// the browser module about the dragged item, and the browser only raises <c>drop</c> where the last
    /// <c>dragover</c> was accepted. A hand pauses over a target long enough for .NET's answer to arrive, and the
    /// browser keeps raising <c>dragover</c> meanwhile; here the test waits for the hover it expects to be rendered,
    /// and two last small moves raise more <c>dragover</c>s (see <see cref="DragOverAtAsync"/>), decided with
    /// everything .NET has said by then, before the release.
    /// </summary>
    protected static async Task DragToAsync(IPage page, Point to, Func<Task>? whileOver = null)
    {
        to = to.Rounded();
        await MoveAsync(page, to);
        if (whileOver is not null) await whileOver();
        await WaitForQuietAsync(page, 100);
        await MoveAsync(page, to.Offset(1, 1), steps: 1);
        await MoveAsync(page, to, steps: 1);
        await page.Mouse.UpAsync();
        await WaitForQuietAsync(page);
    }

    /// <summary>
    /// Keeps a drag in progress hovering <paramref name="at"/> until <paramref name="condition"/> holds. A browser
    /// raises <c>dragover</c> again and again while a drag rests over an element; Playwright raises one per move,
    /// so the pointer is nudged by a pixel each round. A sortable list needs that: a card measures itself before it
    /// reorders, and skips the hovers that arrive while it measures.
    /// </summary>
    protected static async Task HoverUntilAsync(IPage page, Point at, Func<Task<bool>> condition, string what)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        for (var round = 0; ; round++)
        {
            await MoveAsync(page, at.Offset(0, round % 2), steps: 1);
            await WaitForQuietAsync(page, 100);
            if (await condition()) return;
            if (DateTime.UtcNow > deadline) Assert.Fail($"timed out hovering at {at} until {what}");
        }
    }

    // ---- what the browser saw -------------------------------------------------------------------------

    /// <summary>Every <c>dragstart</c> the document of <paramref name="frame"/> raised, as it left the module.</summary>
    protected static Task<DragStartRecord[]> DragStartsAsync(IFrame frame) => frame.EvaluateAsync<DragStartRecord[]>(
        "() => window.__bdndE2E.dragstarts");

    /// <summary>
    /// The last <c>dragover</c> of the document <paramref name="over"/> is in, as the module answered it, and whether
    /// it was raised inside <paramref name="over"/>. For an event a test raised itself, which is dispatched before
    /// the call that raised it returns.
    /// </summary>
    protected static Task<DragOverRecord> LastDragOverAsync(ILocator over) => over.EvaluateAsync<DragOverRecord>(
        $"(el) => {{ const d = window.__bdndE2E.lastDragOver; {DescribeDragOver} }}");

    /// <summary>
    /// A fresh <c>dragover</c> of a drag in progress, raised with the pointer at <paramref name="at"/> (in page
    /// coordinates), as the module answered it, and whether it was raised inside <paramref name="over"/>.
    /// </summary>
    /// <remarks>
    /// Chromium raises only <c>dragenter</c> and <c>dragleave</c> on the move that brings the pointer onto another
    /// element, and the <c>dragover</c> on the next move over the same element; a resting pointer gets a
    /// <c>dragover</c> every few milliseconds in a real drag, but none under Playwright, which raises one event per
    /// move. So the pointer steps off by a pixel and back, until the browser has raised a <c>dragover</c> exactly at
    /// the point (it may still be working through the events of earlier moves when Playwright's move returns).
    /// </remarks>
    protected static async Task<DragOverRecord> DragOverAtAsync(IPage page, ILocator over, Point at)
    {
        at = at.Rounded();
        await over.EvaluateAsync("() => { window.__bdndE2E.lastDragOver = null; }");
        (int X, int Y)[] steps = [(1, 0), (0, 1), (-1, 0), (0, -1)];
        for (var attempt = 0; attempt < 12; attempt++)
        {
            var (dx, dy) = steps[attempt % steps.Length];
            await MoveAsync(page, at.Offset(dx, dy), steps: 1);
            await MoveAsync(page, at, steps: 1);
            for (var deadline = DateTime.UtcNow.AddSeconds(1); DateTime.UtcNow < deadline; await Task.Delay(50))
            {
                if (await over.EvaluateAsync<bool>(
                    "(el, [x, y]) => { const d = window.__bdndE2E.lastDragOver; return !!d && d.x === x && d.y === y; }",
                    new[] { at.X, at.Y }))
                {
                    return await LastDragOverAsync(over);
                }
            }
        }
        Assert.Fail($"the browser raised no dragover at {at}");
        return default!;
    }

    private const string DescribeDragOver =
        """
        if (!d) return { inside: false, prevented: false, dropEffect: null, target: null };
        const t = d.target;
        return {
          inside: el.contains(t), prevented: d.prevented, dropEffect: d.dropEffect,
          target: `<${t.nodeName.toLowerCase()} class="${t.className ?? ''}">${(t.textContent ?? '').trim().slice(0, 40)} at (${d.x}, ${d.y})`,
        };
        """;

    /// <summary>How many <c>drop</c> events reached the page, and how many of them the module kept the browser from acting on.</summary>
    protected static async Task<(int Count, int Prevented)> DropsAsync(IFrame frame)
    {
        var drops = await frame.EvaluateAsync<int[]>(
            "() => [window.__bdndE2E.drops.length, window.__bdndE2E.drops.filter(d => d.prevented).length]");
        return (drops[0], drops[1]);
    }

    // Touch, through the DevTools protocol: Chromium turns the touches into pointer events of type touch.

    /// <summary>A DevTools session on <paramref name="page"/>, through which <see cref="TouchAsync"/> touches it.</summary>
    protected static async Task<ICDPSession> TouchScreenAsync(IPage page) => await page.Context.NewCDPSessionAsync(page);

    /// <summary>
    /// Raises a touch event of <paramref name="type"/> (<c>touchStart</c>, <c>touchMove</c>, <c>touchEnd</c>,
    /// <c>touchCancel</c>), with one finger at <paramref name="at"/> - or none, for an end or a cancel.
    /// </summary>
    protected static Task TouchAsync(ICDPSession cdp, string type, Point? at = null) =>
        cdp.SendAsync("Input.dispatchTouchEvent", new Dictionary<string, object>
        {
            ["type"] = type,
            ["touchPoints"] = at is { } p
                ? new object[] { new Dictionary<string, object> { ["x"] = p.X, ["y"] = p.Y } }
                : Array.Empty<object>(),
        });

    /// <summary>Moves a finger from <paramref name="from"/> to <paramref name="to"/> in <paramref name="steps"/> touch moves.</summary>
    protected static async Task TouchMoveAsync(ICDPSession cdp, Point from, Point to, int steps = 8)
    {
        for (var i = 1; i <= steps; i++)
        {
            await TouchAsync(cdp, "touchMove", new Point(from.X + (to.X - from.X) * i / steps, from.Y + (to.Y - from.Y) * i / steps));
        }
    }

    /// <summary>
    /// A <c>DataTransfer</c> created in the page, standing in for one the operating system would hand over: a drag
    /// from outside the browser cannot be started with the mouse.
    /// </summary>
    protected static Task<IJSHandle> NativeDataTransferAsync(IPage page, string fill) =>
        page.EvaluateHandleAsync($"() => {{ const dt = new DataTransfer(); {fill}; return dt; }}");

    /// <summary>Raises a native drag event of <paramref name="type"/> on <paramref name="target"/>, carrying <paramref name="dataTransfer"/>.</summary>
    protected static Task DispatchDragAsync(ILocator target, string type, IJSHandle dataTransfer) =>
        target.DispatchEventAsync(type, new Dictionary<string, object> { ["dataTransfer"] = dataTransfer });

    // ---- probes -------------------------------------------------------------------------------------

    /// <summary>The on-screen box of the element <paramref name="locator"/> finds.</summary>
    protected static async Task<Box> BoxAsync(ILocator locator)
    {
        await Expect(locator).ToBeVisibleAsync();
        var b = await locator.BoundingBoxAsync();
        Assert.IsNotNull(b, $"{locator} has no box");
        return new Box(b.X, b.Y, b.Width, b.Height);
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

    protected static void AssertNear(Point expected, Point actual, double tolerance, string what) =>
        Assert.IsTrue(expected.DistanceTo(actual) <= tolerance,
            $"{what}: expected {expected} (within {tolerance:0.##}px) but was {actual}");

    /// <summary>Everything a page reports going wrong while a test drives it.</summary>
    protected static List<string> TrackProblems(IPage page, string origin)
    {
        var problems = new List<string>();
        void Add(string problem) { lock (problems) problems.Add(problem); }

        page.Console += (_, message) => { if (message.Type == "error") Add($"console error: {message.Text} ({message.Location})"); };
        page.PageError += (_, error) =>
        {
            // Firefox: blazor.web.js, sending on the circuit's socket as a Blazor Server page unloads (a forced load
            // to another page) - the framework's, and harmless
            if (error.Contains("WebSocket is not in the OPEN state", StringComparison.Ordinal)) return;
            Add("unhandled error: " + error);
        };
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

    /// <summary>A <c>dragstart</c>: where it started, the types it carried, the library's tag and its data, and the effects it allows.</summary>
    protected sealed class DragStartRecord
    {
        public double X { get; set; }

        public double Y { get; set; }

        public string[] Types { get; set; } = [];

        public string? Tag { get; set; }

        public string? TagData { get; set; }

        public string? EffectAllowed { get; set; }
    }

    /// <summary>A <c>dragover</c> as the module answered it: accepted (<c>preventDefault</c>) and with which drop effect.</summary>
    protected sealed class DragOverRecord
    {
        public bool Inside { get; set; }

        public bool Prevented { get; set; }

        public string? DropEffect { get; set; }

        /// <summary>The element the event was raised on, for failure messages.</summary>
        public string? Target { get; set; }

        public override string ToString() => $"dragover on {Target}: prevented {Prevented}, dropEffect {DropEffect}";
    }

    protected readonly record struct Point(double X, double Y)
    {
        public Point Offset(double dx, double dy) => new(X + dx, Y + dy);

        public Point Rounded() => new(Math.Round(X), Math.Round(Y));

        public double DistanceTo(Point other) => Math.Sqrt((X - other.X) * (X - other.X) + (Y - other.Y) * (Y - other.Y));

        public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"({X:0.#}, {Y:0.#})");
    }

    protected readonly record struct Box(double X, double Y, double Width, double Height)
    {
        public Point Centre => new(X + Width / 2, Y + Height / 2);

        public Point TopLeft => new(X, Y);

        public double Right => X + Width;

        public double Bottom => Y + Height;

        public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"[{X:0.#}, {Y:0.#}, {Width:0.#} x {Height:0.#}]");
    }
}
