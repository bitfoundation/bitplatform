using Bit.Bink.Tests.E2E.Infrastructure;
using Bit.Utilities.Testing;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bink.Tests.E2E;

/// <summary>
/// An app that lazy loads Bit.Bink on WebAssembly, set up as the README's "Lazy loading on WebAssembly" section says:
/// the lazy loading sample (<c>Samples/Lazy</c>), published - trimmed, with Bit.Bink marked lazy and nothing of it
/// registered anywhere - and driven in a browser. Its home page draws nothing and never downloads
/// <c>Bit.Bink.wasm</c>, though Bink's stylesheet is already on it; its whiteboard has the router download the assembly
/// before the page renders, and its canvas, which takes only what every host registers, attaches, takes the zero-copy
/// render path and draws. A deep link to the whiteboard is prerendered by the server, which registers the
/// <c>LazyAssemblyLoader</c> its router injects. Published with AOT (<c>BINK_E2E_LAZY_AOT=1</c>), the sample loads
/// Bit.Bink with the app, as the README says an AOT build must, and works just the same.
/// </summary>
[TestClass]
public sealed class LazyLoadingTests : BinkTest
{
    private LazyLoadingSample? _sample;

    protected override string BaseUrl => _sample!.BaseUrl;

    [TestInitialize]
    public async Task StartSampleAsync()
    {
        string? unavailableReason = null;
        try
        {
            _sample = await LazyLoadingSample.GetAsync("Bit.Bink", "BINK", E2EEnvironment.Configuration);
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidOperationException or TimeoutException)
        {
            unavailableReason = $"the lazy loading sample could not be started in the {E2EEnvironment.Configuration} configuration: {ex.Message}";
        }

        if (unavailableReason is not null)
        {            // A build server must not report a suite that never ran as anything but a failure.
            if (E2EEnvironment.IsContinuousIntegration) Assert.Fail("The browser tests could not run: " + unavailableReason);
            Assert.Inconclusive("The browser tests could not run: " + unavailableReason);
        }
    }

    [TestMethod]
    public async Task The_home_page_never_downloads_Bit_Bink_and_the_whiteboard_downloads_it_once_before_its_canvas_renders()
    {
        var (page, problems) = await NewPageAsync();
        var requests = _sample!.Track(page);

        await GotoAsync(page, "/");
        await WaitForInteractiveAsync(page);
        _sample.AssertNotLoaded(requests);

        // The stylesheet and the initializer that injects it are static web assets, not part of the assembly.
        await Expect(page.Locator("link#bit-bink-styles")).ToHaveCountAsync(1);

        // In the app: the router's OnNavigateAsync downloads the assembly, then the whiteboard renders.
        await page.GetByRole(AriaRole.Link, new() { Name = "Whiteboard", Exact = true }).ClickAsync();
        await page.WaitForURLAsync(BaseUrl + "/whiteboard");
        await Expect(page.Locator("h1")).ToHaveTextAsync("Whiteboard");
        await WaitForCanvasAsync(page);

        await AssertTheCanvasDrawsAsync(page);

        // Back and forth: the router asks for the assembly again, and nothing is downloaded again.
        await page.GetByRole(AriaRole.Link, new() { Name = "Home", Exact = true }).ClickAsync();
        await Expect(page.Locator("h1")).ToHaveTextAsync("Lazy loading");
        await page.GetByRole(AriaRole.Link, new() { Name = "Whiteboard", Exact = true }).ClickAsync();
        await Expect(page.Locator("h1")).ToHaveTextAsync("Whiteboard");
        await WaitForCanvasAsync(page);
        await AssertTheCanvasDrawsAsync(page);

        _sample.AssertLoadedOnce(requests);
        AssertNoProblems(problems);
    }

    [TestMethod]
    public async Task A_deep_link_to_the_whiteboard_is_prerendered_and_draws_once_the_browser_takes_over()
    {
        var (page, problems) = await NewPageAsync();
        var requests = _sample!.Track(page);

        // The server prerenders the router - LazyAssemblyLoader resolved there too - and the canvas, which stays inert
        // until the browser attaches it.
        var response = await GotoAsync(page, "/whiteboard");
        var actualValue = response?.Status;
        Assert.AreEqual(200, actualValue);
        var prerendered = await response!.TextAsync();
        Assert.Contains("data-bink-layer=\"document\"", prerendered);
        Assert.Contains("not attached yet", prerendered);

        await WaitForCanvasAsync(page);

        await AssertTheCanvasDrawsAsync(page);

        _sample.AssertLoadedOnce(requests);
        AssertNoProblems(problems);
    }

    /// <summary>
    /// A mouse stroke drawn and then cleared. Ink on the canvas means pointer input reached the trimmed engine through
    /// the browser module the canvas imported without a registration, and its draw commands came back; the page's own
    /// count reads the document, and its Clear button calls the canvas.
    /// </summary>
    private static async Task AssertTheCanvasDrawsAsync(IPage page)
    {
        var strokes = page.Locator("#strokes");
        await Expect(page.Locator("#render-path")).ToHaveTextAsync("zero-copy render");
        await Expect(strokes).ToHaveTextAsync("0");
        Assert.AreEqual(0, await CountInkAsync(page), "the canvas has ink before anything was drawn");

        await DrawStrokeAsync(page, (60, 60), (160, 120), (260, 70), (360, 140));

        await Expect(strokes).ToHaveTextAsync("1");
        var ink = await CountInkAsync(page);
        Assert.IsGreaterThan(500, ink, $"expected the stroke to cover a few hundred pixels but found {ink}");

        await page.GetByRole(AriaRole.Button, new() { Name = "Clear", Exact = true }).ClickAsync();
        await Expect(strokes).ToHaveTextAsync("0");
        // The canvas repaints on its next animation frame, not with the page's render.
        for (var deadline = DateTime.UtcNow.AddSeconds(5); (ink = await CountInkAsync(page)) > 0 && DateTime.UtcNow < deadline;)
            await page.WaitForTimeoutAsync(50);
        Assert.AreEqual(0, ink, "Clear left ink on the canvas");
    }

    /// <summary>
    /// A page in a fresh browser context, with everything it reports going wrong: console errors, unhandled
    /// exceptions, failed requests and error responses from the sample.
    /// </summary>
    private async Task<(IPage Page, List<string> Problems)> NewPageAsync()
    {
        var page = await (await NewContextAsync()).NewPageAsync();
        var origin = BaseUrl;
        var problems = new List<string>();
        void Add(string problem) { lock (problems) problems.Add(problem); }

        page.Console += (_, message) => { if (message.Type == "error") Add($"console: {message.Text} ({message.Location})"); };
        page.PageError += (_, error) => Add("unhandled: " + error);
        page.RequestFailed += (_, request) => Add($"request failed: {request.Url} ({request.Failure})");
        page.Response += (_, response) =>
        {
            if (response.Status >= 400 && response.Url.StartsWith(origin, StringComparison.OrdinalIgnoreCase)) Add($"HTTP {response.Status}: {response.Url}");
        };

        return (page, problems);
    }

    private static Task WaitForInteractiveAsync(IPage page) => page.WaitForFunctionAsync(
        "() => document.querySelector('.page')?.dataset.interactive === 'true'",
        null, new PageWaitForFunctionOptions { Timeout = 90_000 });

    private static void AssertNoProblems(List<string> problems)
    {
        string[] found;
        lock (problems) found = [.. problems];
        Assert.IsEmpty(found, "the page reported:" + Environment.NewLine + string.Join(Environment.NewLine, found));
    }
}
