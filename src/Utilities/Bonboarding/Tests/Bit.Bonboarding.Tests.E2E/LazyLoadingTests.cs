using Bit.Bonboarding.Tests.E2E.Infrastructure;
using Bit.Utilities.Testing;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bonboarding.Tests.E2E;

/// <summary>
/// An app that lazy loads Bit.Bonboarding on WebAssembly, set up as the README's "Lazy loading on WebAssembly" section
/// says: the lazy loading sample (<c>Samples/Lazy</c>), published - trimmed, with Bit.Bonboarding marked lazy, the
/// <c>Bit.Bonboarding.LazyLoad</c> switch on and none of its services registered in the browser - and driven in a
/// browser. Its home page has no tour and never downloads <c>Bit.Bonboarding.wasm</c>; its admin page has the router
/// download it before the page renders, inside a layout only the admin pages use, which hosts the tour and the one
/// <c>BonboardingRoot</c>. They find no registration and create the services themselves: the tour starts by itself
/// once and frames the element each step names, the layout's button replays it and the page reads its stored progress,
/// both through <c>GetBonboardingService()</c>. A deep link to the admin page is prerendered by the server, which
/// registers Bonboarding's services and the <c>LazyAssemblyLoader</c> its router injects. Published with AOT
/// (<c>BONBOARDING_E2E_LAZY_AOT=1</c>), the sample loads Bit.Bonboarding with the app, as the README says an AOT build
/// must, and works just the same.
/// </summary>
[TestClass]
public sealed class LazyLoadingTests : BonboardingTest
{
    private LazyLoadingSample? _sample;

    protected override string BaseUrl => _sample!.BaseUrl;

    [TestInitialize]
    public async Task StartSampleAsync()
    {
        try
        {
            _sample = await LazyLoadingSample.GetAsync("Bit.Bonboarding", "BONBOARDING", E2EEnvironment.Configuration);
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidOperationException or TimeoutException)
        {
            var reason = $"the lazy loading sample could not be started in the {E2EEnvironment.Configuration} configuration: {ex.Message}";
            // A build server must not report a suite that never ran as anything but a failure.
            if (E2EEnvironment.IsContinuousIntegration) Assert.Fail("The browser tests could not run: " + reason);
            Assert.Inconclusive("The browser tests could not run: " + reason);
        }
    }

    [TestMethod]
    public async Task The_home_page_never_downloads_Bit_Bonboarding_and_the_admin_page_downloads_it_once_before_its_tour_starts()
    {
        var (page, failedRequests) = await NewPageAsync();
        var requests = _sample!.Track(page);

        await GotoAsync(page, "/");
        await WaitForInteractiveAsync(page);
        _sample.AssertNotLoaded(requests);

        // In the app: the router's OnNavigateAsync downloads the assembly, then the admin page renders and its tour starts.
        await page.GetByRole(AriaRole.Link, new() { Name = "Admin", Exact = true }).ClickAsync();
        await page.WaitForURLAsync(BaseUrl + "/admin");
        await Expect(page.Locator("h1")).ToHaveTextAsync("Admin");

        await AssertTheTourWorksAsync(page);

        // Away and back: the router asks for the assembly again, and nothing is downloaded again. The layout and its tour
        // are new, and the tour, run to the end once, does not start by itself again.
        await page.GetByRole(AriaRole.Link, new() { Name = "Home", Exact = true }).ClickAsync();
        await Expect(page.Locator("h1")).ToHaveTextAsync("Lazy loading");
        await page.GetByRole(AriaRole.Link, new() { Name = "Admin", Exact = true }).ClickAsync();
        await Expect(page.Locator("h1")).ToHaveTextAsync("Admin");

        // The progress is read after the tour asked for it as it rendered, so by then the tour has decided.
        await page.GetByRole(AriaRole.Button, new() { Name = "Read the stored progress" }).ClickAsync();
        await Expect(page.Locator("#progress")).ToHaveTextAsync("Completed");
        Assert.IsTrue(await HostIsEmptyAsync(page), "The tour was run to the end, yet it started by itself again.");

        _sample.AssertLoadedOnce(requests);
        AssertNoProblems(failedRequests);
    }

    [TestMethod]
    public async Task A_deep_link_to_the_admin_page_is_prerendered_and_its_tour_runs_once_the_browser_takes_over()
    {
        var (page, failedRequests) = await NewPageAsync();
        var requests = _sample!.Track(page);

        // The server prerenders the router - LazyAssemblyLoader resolved there too - and the admin page, its tour and
        // BonboardingRoot from its own registration: nothing of the tour shows before the browser takes over.
        var response = await GotoAsync(page, "/admin");
        Assert.AreEqual(200, response?.Status);
        var prerendered = await response!.TextAsync();
        Assert.Contains("data-bonboarding-anchor=\"invite\"", prerendered);
        Assert.Contains("Read the stored progress", prerendered);
        Assert.DoesNotContain("bit-bonboarding-popover", prerendered);

        await WaitForInteractiveAsync(page);

        await AssertTheTourWorksAsync(page);

        _sample.AssertLoadedOnce(requests);
        AssertNoProblems(failedRequests);
    }

    /// <summary>
    /// The tour, started by itself, frames the element each step names and runs to the end with Next and Done, which
    /// only works when the trimmed library's components created their services without a registration and its module
    /// measured the page. The page then reads the stored record back through <c>GetBonboardingService()</c>, which only
    /// reads "Completed" when the trimmer kept what the record is written from and read into, and the layout's button
    /// starts the tour again through it, from its first step.
    /// </summary>
    private static async Task AssertTheTourWorksAsync(IPage page)
    {
        await AssertStepFramesAsync(page, "Invite your team", "invite");
        // The primary button takes focus when a step is shown.
        StringAssert.Contains(await FocusedAsync(page), "Next");
        await NextAsync(page);
        await AssertStepFramesAsync(page, "Replay it any time", "replay");
        await NextAsync(page);
        await Expect(page.Locator(".bit-bonboarding-root")).ToHaveCountAsync(0);

        await page.GetByRole(AriaRole.Button, new() { Name = "Read the stored progress" }).ClickAsync();
        await Expect(page.Locator("#progress")).ToHaveTextAsync("Completed");

        await page.GetByRole(AriaRole.Button, new() { Name = "Replay the tour" }).ClickAsync();
        await AssertStepFramesAsync(page, "Invite your team", "invite");
        await NextAsync(page);
        await WaitForStepAsync(page, "Replay it any time");
        await NextAsync(page);
        await Expect(page.Locator(".bit-bonboarding-root")).ToHaveCountAsync(0);
    }

    private static async Task AssertStepFramesAsync(IPage page, string title, string anchor)
    {
        await WaitForStepAsync(page, title);

        var target = await BoxOfAsync(page, $"[data-bonboarding-anchor='{anchor}']");
        var spotlight = await BoxOfAsync(page, ".bit-bonboarding-spotlight");

        // The default spotlight padding is 8 pixels on every side.
        AssertNear(target.X - 8, spotlight.X, 1.5, $"The spotlight's left edge, at '{title}'");
        AssertNear(target.Y - 8, spotlight.Y, 1.5, $"The spotlight's top edge, at '{title}'");
        AssertNear(target.Width + 16, spotlight.Width, 1.5, $"The spotlight's width, at '{title}'");
        AssertNear(target.Height + 16, spotlight.Height, 1.5, $"The spotlight's height, at '{title}'");
    }

    private static Task NextAsync(IPage page) => page.Locator("[data-bonboarding-action='next']").ClickAsync();

    /// <summary>
    /// A page in a context of its own, its console errors and unhandled exceptions collected as <see cref="OpenAsync"/>
    /// collects them, and the requests to the sample that failed or that it answered with an error. Opened without
    /// navigating, so the assembly downloads can be tracked from the very first request.
    /// </summary>
    private async Task<(IPage Page, List<string> FailedRequests)> NewPageAsync()
    {
        var page = await (await NewContextAsync()).NewPageAsync();
        var failed = new List<string>();
        void Fail(string problem) { lock (failed) failed.Add(problem); }

        page.Console += (_, message) =>
        {
            if (message.Type == "error") ConsoleErrors.Add(message.Text);
        };
        page.PageError += (_, error) => ConsoleErrors.Add(error);
        page.RequestFailed += (_, request) =>
        {
            if (request.Url.StartsWith(BaseUrl, StringComparison.OrdinalIgnoreCase)) Fail($"request failed: {request.Url} ({request.Failure})");
        };
        page.Response += (_, response) =>
        {
            if (response.Status >= 400 && response.Url.StartsWith(BaseUrl, StringComparison.OrdinalIgnoreCase)) Fail($"HTTP {response.Status}: {response.Url}");
        };

        return (page, failed);
    }

    private void AssertNoProblems(List<string> failedRequests)
    {
        AssertNoConsoleErrors("the lazy loading sample");

        lock (failedRequests)
            Assert.IsEmpty(failedRequests, "the lazy loading sample's requests failed:" + Environment.NewLine + string.Join(Environment.NewLine, failedRequests));
    }
}
