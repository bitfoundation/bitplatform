using Microsoft.Playwright;

namespace Bit.Bmage.Tests.E2E.Infrastructure;

/// <summary>
/// Base of every suite: gives a test pages of its own on the shared demo host, closes them afterwards,
/// and holds the helpers the suites share - opening a route once it is interactive, the sample upload,
/// and loading that upload into an editor.
/// </summary>
/// <remarks>
/// The demo is prerendered: a page's HTML, headings included, is on screen long before the WebAssembly
/// runtime has attached a single handler. A file set on a drop zone, or a click, in between is silently
/// lost - so <see cref="OpenAsync"/> waits for the layout's <c>data-interactive</c> flag, which the
/// prerendered markup carries as "false" and the first interactive render flips.
/// </remarks>
public abstract class DemoTest
{
    /// <summary>The viewport the original suite was written against: wide enough for the docs shell's nav panel and rail.</summary>
    protected static readonly ViewportSize DesktopViewport = new() { Width = 1400, Height = 900 };

    private readonly List<IBrowserContext> _contexts = [];

    public TestContext TestContext { get; set; } = default!;

    /// <summary>The demo's origin, without a trailing slash.</summary>
    protected string BaseUrl { get; private set; } = string.Empty;

    [TestInitialize]
    public async Task ResolveHostAsync() => BaseUrl = await DemoHost.BaseUrlAsync();

    [TestCleanup]
    public async Task ClosePagesAsync()
    {
        foreach (var context in _contexts)
        {
            try { await context.CloseAsync(); }
            catch (PlaywrightException) { /* the browser is already gone */ }
        }

        _contexts.Clear();
    }

    /// <summary>A blank page in a browser context of its own. Inconclusive when no browser could be launched.</summary>
    protected async Task<IPage> NewPageAsync(ViewportSize? viewport = null)
    {
        var (browser, failure) = await PlaywrightSession.BrowserAsync();
        if (browser is null) Assert.Inconclusive(failure ?? "No browser could be launched.");

        var context = await browser.NewContextAsync(new() { ViewportSize = viewport ?? DesktopViewport });
        _contexts.Add(context);
        return await context.NewPageAsync();
    }

    /// <summary>Opens a route and waits for the application to become interactive.</summary>
    protected async Task<IPage> OpenAsync(string route, ViewportSize? viewport = null)
    {
        var page = await NewPageAsync(viewport);
        await GotoAsync(page, route);
        return page;
    }

    /// <summary>Navigates an existing page to a route and waits for the application to become interactive.</summary>
    protected async Task GotoAsync(IPage page, string route)
    {
        await page.GotoAsync($"{BaseUrl}/{route}", new PageGotoOptions { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await WaitForInteractiveAsync(page);
        await page.WaitForSelectorAsync("h1", new PageWaitForSelectorOptions { Timeout = 60_000 });
    }

    /// <summary>Waits until the prerendered markup has been taken over by the WebAssembly runtime.</summary>
    protected static Task WaitForInteractiveAsync(IPage page) =>
        page.WaitForSelectorAsync(".app-shell[data-interactive='true']", new PageWaitForSelectorOptions { Timeout = 120_000 });

    /// <summary>
    /// Drops the sample image on the editor's own file input and waits for its first preview, which is
    /// when the editor's tools become enabled. The editor is then scrolled to the top of the window, so
    /// the pointer-driven tests can reach every part of its canvas.
    /// </summary>
    protected static async Task LoadSampleIntoEditorAsync(IPage page)
    {
        var file = SampleImage.WriteToTempFile();
        try
        {
            await page.SetInputFilesAsync(".bmg-editor input[type=file]", file);
            await page.WaitForSelectorAsync(".bmg-canvas", new PageWaitForSelectorOptions { Timeout = 60_000 });
            await page.WaitForFunctionAsync("() => document.querySelector('.bmg-canvas')?.width > 1", null,
                new PageWaitForFunctionOptions { Timeout = 60_000 });
        }
        finally
        {
            File.Delete(file);
        }

        await ScrollEditorIntoViewAsync(page);
    }

    /// <summary>
    /// Brings the whole editor on screen, just under the sticky header. The docs pages put an introduction
    /// above it, and a drag aimed at a point below the fold would land on nothing.
    /// </summary>
    protected static async Task ScrollEditorIntoViewAsync(IPage page)
    {
        await page.Locator(".editor-frame").EvaluateAsync("e => e.scrollIntoView({ block: 'start', behavior: 'instant' })");
        // One frame for layout to settle before anyone reads a bounding box.
        await page.EvaluateAsync("() => new Promise(r => requestAnimationFrame(() => requestAnimationFrame(r)))");
    }

    /// <summary>Collects everything a page reports as an error: uncaught exceptions and console errors.</summary>
    protected static List<string> CollectErrors(IPage page)
    {
        var errors = new List<string>();
        page.PageError += (_, e) => { lock (errors) errors.Add(e); };
        page.Console += (_, e) => { if (e.Type == "error") lock (errors) errors.Add(e.Text); };
        return errors;
    }
}
