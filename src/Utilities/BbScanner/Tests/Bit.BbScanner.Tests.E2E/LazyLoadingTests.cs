using Bit.BbScanner.Tests.E2E.Infrastructure;
using Bit.Utilities.Testing;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.BbScanner.Tests.E2E;

/// <summary>
/// An app that lazy loads Bit.BbScanner on WebAssembly, set up as the README's "Lazy loading on WebAssembly" section
/// says: the lazy loading sample (<c>Samples/Lazy</c>), published - trimmed, with Bit.BbScanner marked lazy and nothing of
/// it registered - and driven in a browser whose camera plays a QR code. Its home page scans nothing and never downloads
/// <c>Bit.BbScanner.wasm</c>, yet already has the stylesheet the package's JS initializer adds; its scanner page has the
/// router download the assembly before the page renders, and the scanner, which finds nothing registered, imports its
/// module, opens the camera and reads the code. A deep link to it is prerendered by the server, which registers the
/// <c>LazyAssemblyLoader</c> its router injects. Published with AOT (<c>BBSCANNER_E2E_LAZY_AOT=1</c>), the sample loads
/// Bit.BbScanner with the app, as the README says an AOT build must, and works just the same.
/// </summary>
[TestClass]
public sealed class LazyLoadingTests : BbScannerTest
{
    private LazyLoadingSample? _sample;

    protected override string BaseUrl => _sample!.BaseUrl;

    [TestInitialize]
    public async Task StartSampleAsync()
    {
        string? unavailableReason = null;
        try
        {
            _sample = await LazyLoadingSample.GetAsync("Bit.BbScanner", "BBSCANNER", E2EEnvironment.Configuration);
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
    public async Task The_home_page_never_downloads_Bit_BbScanner_and_the_scanner_page_downloads_it_once_before_it_scans()
    {
        await using var session = await OpenAsync(SingleQrClip);
        var page = session.Page;
        var requests = _sample!.Track(page);
        var failedRequests = TrackFailedRequests(page, BaseUrl);

        await session.GoToAsync("/");
        _sample.AssertNotLoaded(requests);

        // The JS initializer needs no assembly: the stylesheet is on the page before Bit.BbScanner.wasm is.
        await Expect(page.Locator("link#bit-bbscanner-styles")).ToHaveCountAsync(1);
        Assert.AreEqual(0, await session.TracksOpenedAsync());

        // In the app: the router's OnNavigateAsync downloads the assembly, then the scanner renders and opens the camera.
        await page.GetByRole(AriaRole.Link, new() { Name = "Scan", Exact = true }).ClickAsync();
        await page.WaitForURLAsync(BaseUrl + "/scan");
        await Expect(page.Locator("h1")).ToHaveTextAsync("Scan");

        await AssertTheScannerReadsTheCameraAsync(session);

        // Away and back: the scanner that leaves releases the camera, the router asks for the assembly again, nothing is
        // downloaded again, and a new scanner reuses the module the first one imported.
        await page.GetByRole(AriaRole.Link, new() { Name = "Home", Exact = true }).ClickAsync();
        await Expect(page.Locator("h1")).ToHaveTextAsync("Lazy loading");
        await page.WaitForFunctionAsync("() => window.__bscanTracks.live.size === 0", null, new() { Timeout = 15_000 });
        await page.GetByRole(AriaRole.Link, new() { Name = "Scan", Exact = true }).ClickAsync();
        await Expect(page.Locator("h1")).ToHaveTextAsync("Scan");

        await AssertTheScannerReadsTheCameraAsync(session);

        _sample.AssertLoadedOnce(requests);
        AssertNoProblems(session, failedRequests);
    }

    [TestMethod]
    public async Task A_deep_link_to_the_scanner_page_is_prerendered_and_scans_once_the_browser_takes_over()
    {
        await using var session = await OpenAsync(SingleQrClip);
        var page = session.Page;
        var requests = _sample!.Track(page);
        var failedRequests = TrackFailedRequests(page, BaseUrl);

        // The server prerenders the router - LazyAssemblyLoader resolved there too - and the scanner's markup, inert until
        // the browser takes over.
        var response = await page.GotoAsync(BaseUrl + "/scan", new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 90_000 });
        var actualValue = response?.Status;
        Assert.AreEqual(200, actualValue);
        var prerendered = await response!.TextAsync();
        Assert.Contains("class=\"bit-bbscanner\"", prerendered);
        Assert.Contains("Nothing read yet", prerendered);

        await session.WaitForInteractiveAsync();

        await AssertTheScannerReadsTheCameraAsync(session);

        _sample.AssertLoadedOnce(requests);
        AssertNoProblems(session, failedRequests);
    }

    /// <summary>
    /// The scanner opened the fake camera and read its QR code, which only works when the trimmed library imported its
    /// module and copied frames through its <c>[JSImport]</c> bindings without a registration; the camera it reports was
    /// read from the page's JSON through the library's source generated context, and its look is the injected
    /// stylesheet's.
    /// </summary>
    private static async Task AssertTheScannerReadsTheCameraAsync(ScannerSession session)
    {
        var page = session.Page;

        await session.WaitForStatusAsync(["scanning"], 60_000);
        await Expect(page.Locator("#result")).ToHaveTextAsync("QrCode: SINGLE-QR", new() { Timeout = 60_000 });
        await Expect(page.Locator("#camera")).ToHaveTextAsync("640 x 480");
        Assert.AreEqual(1, await session.TracksLiveAsync());

        var accent = await page.EvaluateAsync<string>(
            "() => getComputedStyle(document.querySelector('.bit-bbscanner')).getPropertyValue('--bit-bbscanner-accent').trim()");
        Assert.AreEqual("#4f8cff", accent, "bit-bbscanner.css does not apply to the scanner");
    }

    // ScannerSession keeps the console's errors and the page's unhandled exceptions; these are the rest of what fails a
    // run: a request to the sample that failed, or that it answered with an error.
    private static List<string> TrackFailedRequests(IPage page, string origin)
    {
        var failed = new List<string>();
        void Add(string problem) { lock (failed) failed.Add(problem); }

        page.RequestFailed += (_, request) =>
        {
            if (request.Url.StartsWith(origin, StringComparison.OrdinalIgnoreCase)) Add($"request failed: {request.Url} ({request.Failure})");
        };
        page.Response += (_, response) =>
        {
            if (response.Status >= 400 && response.Url.StartsWith(origin, StringComparison.OrdinalIgnoreCase)) Add($"HTTP {response.Status}: {response.Url}");
        };

        return failed;
    }

    private static void AssertNoProblems(ScannerSession session, List<string> failedRequests)
    {
        string[] found;
        lock (failedRequests)
        {
            found = [.. session.ConsoleErrors.Select(e => "console error: " + e), .. session.PageErrors.Select(e => "unhandled error: " + e), .. failedRequests];
        }

        Assert.IsEmpty(found, "the page reported:" + Environment.NewLine + string.Join(Environment.NewLine, found));
    }
}
