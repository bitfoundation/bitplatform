using Bit.Bortable.Tests.E2E.Infrastructure;
using Bit.Utilities.Testing;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bortable.Tests.E2E;

/// <summary>
/// An app that lazy loads Bit.Bortable on WebAssembly, set up as the README's "Lazy loading on WebAssembly" section says:
/// the lazy loading sample (<c>Samples/Lazy</c>), published - trimmed, with its runtime switches in the boot
/// configuration - and driven in a browser. Its home page has no list and never downloads <c>Bit.Bortable.wasm</c>; its
/// board has the router download it before the page renders, and its lists, which find no registration but the
/// <c>Bit.Bortable.Unregistered</c> switch, create their coordinator themselves and hand items to each other. A deep link
/// to the board is prerendered by the server, which registers the <c>LazyAssemblyLoader</c> its router injects. Published
/// with AOT (<c>BORTABLE_E2E_LAZY_AOT=1</c>), the sample loads Bit.Bortable with the app, as the README says an AOT build
/// must, and works just the same.
/// </summary>
[TestClass]
public sealed class LazyLoadingTests : BortableTest
{
    private LazyLoadingSample? _sample;

    protected override string BaseUrl => _sample!.BaseUrl;

    [TestInitialize]
    public async Task StartSampleAsync()
    {
        string? unavailableReason = null;
        try
        {
            _sample = await LazyLoadingSample.GetAsync("Bit.Bortable", "BORTABLE", E2EEnvironment.Configuration);
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
    public async Task The_home_page_never_downloads_Bit_Bortable_and_the_board_downloads_it_before_its_lists_render()
    {
        var page = await (await NewContextAsync()).NewPageAsync();
        var problems = TrackProblems(page, BaseUrl);
        var requests = _sample!.Track(page);

        await GotoAsync(page, "/");
        await WaitForListsAsync(page);
        _sample.AssertNotLoaded(requests);

        // In the app: the router's OnNavigateAsync downloads the assembly, then the board renders.
        await page.GetByRole(AriaRole.Link, new() { Name = "Board", Exact = true }).ClickAsync();
        await page.WaitForURLAsync(BaseUrl + "/board");
        await Expect(page.Locator("h1")).ToHaveTextAsync("Board");
        await WaitForListsAsync(page);
        _sample.AssertLoadedOnce(requests);

        await AssertDragAcrossTheBoardAsync(page);

        var found = Snapshot(problems);
        Assert.IsEmpty(found, string.Join(Environment.NewLine, found));
    }

    [TestMethod]
    public async Task A_deep_link_to_the_board_is_prerendered_and_its_lists_sort_once_the_browser_takes_over()
    {
        var page = await (await NewContextAsync()).NewPageAsync();
        var problems = TrackProblems(page, BaseUrl);
        var requests = _sample!.Track(page);

        // The server prerenders the router - LazyAssemblyLoader resolved there too - and the board's lists, with their
        // items, from its own registration.
        var response = await GotoAsync(page, "/board");
        var actualValue = response?.Status;
        Assert.AreEqual(200, actualValue);
        var prerendered = await response!.TextAsync();
        Assert.Contains("data-bortable-list", prerendered);
        Assert.Contains("Review", prerendered);

        await WaitForListsAsync(page);
        _sample.AssertLoadedOnce(requests);

        await AssertDragAcrossTheBoardAsync(page);

        var found = Snapshot(problems);
        Assert.IsEmpty(found, string.Join(Environment.NewLine, found));
    }

    /// <summary>
    /// A card dragged from one list into the other, which only works when both lists joined the one coordinator they
    /// created without a registration.
    /// </summary>
    private static async Task AssertDragAcrossTheBoardAsync(IPage page)
    {
        await AssertOrderAsync(page, "lazy-todo", "lazy-todo-order", "Write", "Review", "Ship");
        await AssertOrderAsync(page, "lazy-done", "lazy-done-order", "Plan");

        await DragAsync(page,
            (await BoxAsync(Item(page, "lazy-todo", "Review"))).Centre,
            (await BoxAsync(Item(page, "lazy-done", "Plan"))).At(0.5, 0.3));

        await AssertOrderAsync(page, "lazy-todo", "lazy-todo-order", "Write", "Ship");
        await AssertOrderAsync(page, "lazy-done", "lazy-done-order", "Review", "Plan");
    }
}
