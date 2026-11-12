using Bit.Bdnd.Tests.E2E.Infrastructure;
using Bit.Utilities.Testing;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bdnd.Tests.E2E;

/// <summary>
/// An app that lazy loads Bit.Bdnd on WebAssembly, set up as the README's "Lazy loading on WebAssembly" section says:
/// the lazy loading sample (<c>Samples/Lazy</c>), published - trimmed, with Bit.Bdnd marked lazy and none of its services
/// registered in the browser - and driven in a browser. Its home page drags nothing and never downloads
/// <c>Bit.Bdnd.wasm</c>; its board has the router download it before the page renders, and its cards and column, which
/// find no registration, create the interop themselves, which the page's own code takes from <c>GetBdndInterop()</c>
/// to measure the column. A deep link to the board is prerendered by the server, which registers the
/// <c>LazyAssemblyLoader</c> its router injects. Published with AOT (<c>BDND_E2E_LAZY_AOT=1</c>), the sample loads
/// Bit.Bdnd with the app, as the README says an AOT build must, and works just the same.
/// </summary>
[TestClass]
public sealed class LazyLoadingTests : BdndTest
{
    private LazyLoadingSample? _sample;

    protected override string BaseUrl => _sample!.BaseUrl;

    [TestInitialize]
    public async Task StartSampleAsync()
    {
        string? unavailableReason = null;
        try
        {
            _sample = await LazyLoadingSample.GetAsync("Bit.Bdnd", "BDND", E2EEnvironment.Configuration);
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
    public async Task The_home_page_never_downloads_Bit_Bdnd_and_the_board_downloads_it_once_before_its_cards_render()
    {
        var page = await NewPageAsync();
        var requests = _sample!.Track(page);

        await GotoAsync(page, "/");
        await WaitForInteractiveAsync(page.MainFrame);
        _sample.AssertNotLoaded(requests);

        // In the app: the router's OnNavigateAsync downloads the assembly, then the board renders.
        await page.GetByRole(AriaRole.Link, new() { Name = "Board", Exact = true }).ClickAsync();
        await page.WaitForURLAsync(BaseUrl + "/board");
        await Expect(page.Locator("h1")).ToHaveTextAsync("Board");
        await WaitForBdndAsync(page.MainFrame);

        await AssertTheBoardWorksAsync(page);

        // Back and forth: the router asks for the assembly again, and nothing is downloaded again.
        await page.GetByRole(AriaRole.Link, new() { Name = "Home", Exact = true }).ClickAsync();
        await Expect(page.Locator("h1")).ToHaveTextAsync("Lazy loading");
        await page.GetByRole(AriaRole.Link, new() { Name = "Board", Exact = true }).ClickAsync();
        await Expect(page.Locator("h1")).ToHaveTextAsync("Board");
        await WaitForBdndAsync(page.MainFrame);

        _sample.AssertLoadedOnce(requests);
    }

    [TestMethod]
    public async Task A_deep_link_to_the_board_is_prerendered_and_its_cards_drag_once_the_browser_takes_over()
    {
        var page = await NewPageAsync();
        var requests = _sample!.Track(page);

        // The server prerenders the router - LazyAssemblyLoader resolved there too - and the board, from its own
        // registration.
        var response = await GotoAsync(page, "/board");
        var actualValue = response?.Status;
        Assert.AreEqual(200, actualValue);
        var prerendered = await response!.TextAsync();
        Assert.Contains("Review", prerendered);
        Assert.Contains("Nothing yet", prerendered);

        await WaitForInteractiveAsync(page.MainFrame);
        await WaitForBdndAsync(page.MainFrame);

        await AssertTheBoardWorksAsync(page);

        _sample.AssertLoadedOnce(requests);
    }

    /// <summary>
    /// A card dragged into the Done column, which only works when the trimmed library's interop reached the browser
    /// module without a registration, and the column measured through <c>GetBdndInterop()</c>, which only reads back
    /// when the trimmer kept what System.Text.Json reads the rectangle into.
    /// </summary>
    private static async Task AssertTheBoardWorksAsync(IPage page)
    {
        var done = page.Locator("#done");
        await Expect(done).ToHaveTextAsync("Nothing yet");

        await DragAsync(page, page.Locator(".card", new() { HasText = "Review" }), page.Locator(".target"),
            () => Expect(done).ToHaveTextAsync("Release to drop"));

        await Expect(done).ToHaveTextAsync("Review");
        await Expect(page.Locator(".card")).ToHaveTextAsync(["Write", "Ship"]);

        var column = await BoxAsync(page.Locator(".column").Nth(1));
        await page.GetByRole(AriaRole.Button, new() { Name = "Measure the Done column" }).ClickAsync();
        await Expect(page.Locator("#done-size")).ToHaveTextAsync($"{Math.Round(column.Width)} x {Math.Round(column.Height)}");
    }
}
