using Bit.Balette.Tests.E2E.Infrastructure;
using Bit.Utilities.Testing;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Balette.Tests.E2E;

/// <summary>
/// An app that lazy loads Bit.Balette on WebAssembly, set up as the README's "Lazy loading on WebAssembly" section says:
/// the lazy loading sample (<c>Samples/Lazy</c>), published - trimmed, with Bit.Balette marked lazy and none of its
/// services registered in the browser - and driven in a browser. Its home page has no palette and never downloads
/// <c>Bit.Balette.wasm</c>; its admin page has the router download it before the page renders, and the palette and the
/// trigger of the layout only that page uses, which find no registration, create their services themselves - the ones
/// the page's own code takes from <c>GetBaletteService()</c> to register its command. A deep link to the admin page is
/// prerendered by the server, which registers Balette's services and the <c>LazyAssemblyLoader</c> its router injects.
/// Published with AOT (<c>BALETTE_E2E_LAZY_AOT=1</c>), the sample loads Bit.Balette with the app, as the README says an
/// AOT build must, and works just the same.
/// </summary>
/// <remarks>
/// The sample always runs on WebAssembly: <c>BALETTE_E2E_RENDER_MODE</c> switches the demo only.
/// </remarks>
[TestClass]
public sealed class LazyLoadingTests : BaletteTest
{
    private LazyLoadingSample? _sample;

    protected override string BaseUrl => _sample!.BaseUrl;

    [TestInitialize]
    public async Task StartSampleAsync()
    {
        try
        {
            _sample = await LazyLoadingSample.GetAsync("Bit.Balette", "BALETTE", E2EEnvironment.Configuration);
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
    public async Task The_home_page_never_downloads_Bit_Balette_and_the_admin_page_downloads_it_once_before_its_palette_renders()
    {
        var page = await NewPageAsync(await NewContextAsync());
        var requests = _sample!.Track(page);
        var problems = TrackProblems(page, BaseUrl);

        await GotoAsync(page, "/");
        await WaitForInteractiveAsync(page);
        _sample.AssertNotLoaded(requests);

        // In the app: the router's OnNavigateAsync downloads the assembly, then the admin page renders with the palette.
        await page.GetByRole(AriaRole.Link, new() { Name = "Admin", Exact = true }).ClickAsync();
        await page.WaitForURLAsync(BaseUrl + "/admin");
        await Expect(page.Locator("h1")).ToHaveTextAsync("Admin");
        await WaitForReadyAsync(page);

        // The trigger opens the palette the page registered its command with.
        await page.Locator("[data-balette='trigger']").ClickAsync();
        await ExpectOpenAsync(page);
        await ApprovePendingRequestsAsync(page);

        // Ctrl+K opens it too, and the layout's command leaves for the home page...
        await OpenPaletteAsync(page);
        await TypeAsync(page, "home");
        await ExpectFirstItemAsync(page, "navigation.home");
        await page.Keyboard.PressAsync("Enter");
        await page.WaitForURLAsync(BaseUrl + "/");
        await Expect(page.Locator("h1")).ToHaveTextAsync("Lazy loading");

        // ...and back: the router asks for the assembly again, and nothing is downloaded again. The page registers its
        // command anew with the palette the layout holds again, in the services the first ones created.
        await page.GetByRole(AriaRole.Link, new() { Name = "Admin", Exact = true }).ClickAsync();
        await Expect(page.Locator("h1")).ToHaveTextAsync("Admin");
        await WaitForReadyAsync(page);
        await OpenPaletteAsync(page);
        await ApprovePendingRequestsAsync(page);

        _sample.AssertLoadedOnce(requests);
        AssertNoProblems(problems);
    }

    [TestMethod]
    public async Task A_deep_link_to_the_admin_page_is_prerendered_and_its_palette_runs_the_page_command_once_the_browser_takes_over()
    {
        var page = await NewPageAsync(await NewContextAsync());
        var requests = _sample!.Track(page);
        var problems = TrackProblems(page, BaseUrl);

        // The server prerenders the router - LazyAssemblyLoader resolved there too - and the page, the trigger and the
        // closed palette, from its own registration.
        var response = await GotoAsync(page, "/admin");
        Assert.AreEqual(200, response?.Status);
        var prerendered = await response!.TextAsync();
        Assert.Contains("3 pending requests", prerendered);
        Assert.Contains("data-balette=\"trigger\"", prerendered);
        Assert.Contains("data-balette=\"dialog\"", prerendered);

        await WaitForReadyAsync(page);
        await OpenPaletteAsync(page);
        await ApprovePendingRequestsAsync(page);

        _sample.AssertLoadedOnce(requests);
        AssertNoProblems(problems);
    }

    /// <summary>
    /// With the palette open on the admin page, runs the command the page registered through <c>GetBaletteService()</c>,
    /// which only reaches this palette when the page and the palette share the services the palette created itself.
    /// </summary>
    private static async Task ApprovePendingRequestsAsync(IPage page)
    {
        var pending = page.Locator("#pending");
        await Expect(pending).ToHaveTextAsync("3 pending requests");

        await TypeAsync(page, "approve");
        await ExpectFirstItemAsync(page, "requests.approve");
        await page.Keyboard.PressAsync("Enter");

        await ExpectClosedAsync(page);
        await Expect(pending).ToHaveTextAsync("No pending requests");
    }

    /// <summary>The home page has no palette to wait for, only the layout's marker.</summary>
    private static Task WaitForInteractiveAsync(IPage page) => page.WaitForFunctionAsync(
        "() => document.querySelector('.page')?.dataset.interactive === 'true'",
        null, new PageWaitForFunctionOptions { Timeout = 90_000 });

    /// <summary>
    /// Every console error, unhandled exception, failed request and error response of the sample the page reports -
    /// stricter than the base class, which fails a test on the fatal ones only: an app that lazy loads must run clean.
    /// </summary>
    private static List<string> TrackProblems(IPage page, string origin)
    {
        var problems = new List<string>();
        void Add(string problem) { lock (problems) problems.Add(problem); }

        page.Console += (_, message) =>
        {
            if (message.Type == "error") Add($"console error: {message.Text}");
            // The warning that the services are not registered, which the sample silences with the README's filter on
            // the logger's category: a category renamed would let it through again.
            else if (message.Text.Contains("services are not registered", StringComparison.Ordinal)) Add($"console {message.Type}: {message.Text}");
        };
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

    private static void AssertNoProblems(List<string> problems)
    {
        string[] found;
        lock (problems) found = [.. problems];
        Assert.IsEmpty(found, "the page reported:" + Environment.NewLine + string.Join(Environment.NewLine, found));
    }
}
