using Microsoft.Playwright;

namespace Bit.Bink.Tests.E2E.Infrastructure;

/// <summary>
/// Base of every suite: gives each test a browser context of its own against the shared demo, waits for a
/// page to be ready to draw on, and holds the pixel probes the suites share.
/// </summary>
public abstract class BinkTest
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
    protected async Task<IBrowserContext> NewContextAsync(BrowserNewContextOptions? options = null)
    {
        var context = await Browser.NewContextAsync(options ?? new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 900 },
            DeviceScaleFactor = 1,
            HasTouch = true,
            IsMobile = false,
        });
        _contexts.Add(context);
        return context;
    }

    /// <summary>Opens a page of the demo in a context of its own, and waits until its canvas can be drawn on.</summary>
    protected async Task<IPage> OpenAsync(string path)
    {
        var page = await (await NewContextAsync()).NewPageAsync();
        await GotoAsync(page, path);
        await WaitForCanvasAsync(page);
        return page;
    }

    protected Task<IResponse?> GotoAsync(IPage page, string path) =>
        page.GotoAsync(BaseUrl + path, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 90_000 });

    /// <summary>
    /// Waits until the page runs in the browser and its canvas is attached. Every page is prerendered, so the
    /// canvas markup is on screen long before anything listens to it: the prerendered canvas even has a backing
    /// store, the 300 by 150 one every canvas starts with. The browser layer sizing the canvas to its box is
    /// what says it is attached, and a stroke drawn before that would be silently lost.
    /// </summary>
    protected static async Task WaitForCanvasAsync(IPage page)
    {
        await page.WaitForFunctionAsync(
            """
            () => {
              if (document.querySelector('.app')?.dataset.interactive !== 'true') return false;
              const canvases = [...document.querySelectorAll('canvas[data-bink-layer="document"]')];
              return canvases.length > 0 && canvases.every(c => c.width > 0 && c.style.width !== '');
            }
            """, null, new PageWaitForFunctionOptions { Timeout = 90_000 });
    }

    /// <summary>
    /// Counts pixels that differ from the page background, which is the only honest way to prove ink was drawn.
    /// Counting opaque pixels would count the background itself. The top-left pixel is taken as background:
    /// nothing is ever drawn in the very corner in these tests.
    /// </summary>
    protected static Task<int> CountInkAsync(IPage page) => page.EvaluateAsync<int>(
        """
        () => {
          const canvas = document.querySelector('canvas[data-bink-layer="document"]');
          const ctx = canvas.getContext('2d', { willReadFrequently: true });
          const data = ctx.getImageData(0, 0, canvas.width, canvas.height).data;
          const br = data[0], bg = data[1], bb = data[2], ba = data[3];
          let count = 0;
          for (let i = 0; i < data.length; i += 4) {
            if (Math.abs(data[i] - br) + Math.abs(data[i + 1] - bg) + Math.abs(data[i + 2] - bb) + Math.abs(data[i + 3] - ba) > 24) count++;
          }
          return count;
        }
        """);

    /// <summary>The bounding box, in canvas backing-store pixels, of everything that differs from the background.</summary>
    protected static Task<int[]> InkBoxAsync(IPage page) => page.EvaluateAsync<int[]>(
        """
        () => {
          const canvas = document.querySelector('canvas[data-bink-layer="document"]');
          const ctx = canvas.getContext('2d', { willReadFrequently: true });
          const w = canvas.width, h = canvas.height;
          const data = ctx.getImageData(0, 0, w, h).data;
          const br = data[0], bg = data[1], bb = data[2], ba = data[3];
          let minX = w, minY = h, maxX = -1, maxY = -1;
          for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
            const i = (y * w + x) * 4;
            if (Math.abs(data[i] - br) + Math.abs(data[i + 1] - bg) + Math.abs(data[i + 2] - bb) + Math.abs(data[i + 3] - ba) > 24) {
              if (x < minX) minX = x; if (x > maxX) maxX = x;
              if (y < minY) minY = y; if (y > maxY) maxY = y;
            }
          }
          return [minX, minY, maxX, maxY];
        }
        """);

    /// <summary>Draws a mouse stroke through <paramref name="points"/>, given relative to the input surface.</summary>
    protected static async Task DrawStrokeAsync(IPage page, params (float X, float Y)[] points)
    {
        var box = (await page.Locator(".bit-bink-input").First.BoundingBoxAsync())!;

        await page.Mouse.MoveAsync(box.X + points[0].X, box.Y + points[0].Y);
        await page.Mouse.DownAsync();
        for (int i = 1; i < points.Length; i++)
        {
            await page.Mouse.MoveAsync(box.X + points[i].X, box.Y + points[i].Y, new MouseMoveOptions { Steps = 4 });
        }
        await page.Mouse.UpAsync();
        // Let the animation frame that carries the batch run, plus the redraw it triggers.
        await page.WaitForTimeoutAsync(250);
    }

    /// <summary>The element of the active document's focus, by class name.</summary>
    protected static Task<string> FocusedClassAsync(IPage page) =>
        page.EvaluateAsync<string>("() => document.activeElement?.className ?? ''");

    protected static void AssertInRange(double value, double low, double high, string what) =>
        Assert.IsTrue(value >= low && value <= high, $"{what} is {value}, expected it between {low} and {high}.");
}
