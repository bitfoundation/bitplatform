using System.Linq;
using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Performance;

/// <summary>
/// Browser tests of <see cref="BitInfiniteScrolling{TItem}"/> as a reader scrolls it: reaching the end of the list
/// loads the next page, page after page, until the source runs out, and then it shows its end and asks for nothing
/// more - and scrolling to the end over and over while a page is still on its way never asks for the same page twice
/// or renders an item twice. The list of <c>/regression/infinite-scrolling-behaviour</c> holds 300px of 30px rows and
/// pages through 50 items 20 at a time, each page answered after 300ms; it records the item every request skips to.
/// </summary>
[TestClass]
[TestCategory("Browser")]
[RequiresBrowser] // opt-in: RUN_BROWSER_TESTS=1 dotnet test --filter FullyQualifiedName~BitInfiniteScrollingBrowserTests
public class BitInfiniteScrollingBrowserTests : PerformanceTestBase
{
    private const string PageUrl = "/regression/infinite-scrolling-behaviour";

    [TestMethod]
    public async Task BitInfiniteScrolling_ReachingTheEnd_LoadsTheNextPageUntilTheSourceRunsOut()
    {
        await Open();

        // The first page fills more than the list shows, so nothing else is asked for until the reader scrolls.
        await Expect(Rows()).ToHaveCountAsync(20);
        await Expect(Report("skips")).ToHaveTextAsync("0");
        await Expect(List()).ToHaveAttributeAsync("aria-busy", "false");

        await WheelToTheEnd();
        await Expect(Rows()).ToHaveCountAsync(40);
        await Expect(Report("skips")).ToHaveTextAsync("0,20");
        await Expect(Report("ends")).ToHaveTextAsync("0");

        await WheelToTheEnd();
        await Expect(Rows()).ToHaveCountAsync(50);
        await Expect(Report("skips")).ToHaveTextAsync("0,20,40");

        // The short page is the last one: the list says so and stops asking.
        await Expect(List().Locator(".bit-isc-end")).ToHaveTextAsync("The end of the list.");
        await Expect(Report("ends")).ToHaveTextAsync("1");
        await Expect(Report("loaded")).ToHaveTextAsync("50");

        await WheelToTheEnd();
        await WheelToTheEnd();

        // There is nothing in flight to wait for: a request would have been recorded the moment it was made, so a
        // round trip of the page after the scrolling shows whether one was.
        await Page.WaitForTimeoutAsync(500);
        await Expect(Report("skips")).ToHaveTextAsync("0,20,40");
        await Expect(List().Locator(".bit-isc-ldg")).ToHaveCountAsync(0);

        await ExpectItemsInOrder();
    }

    [TestMethod]
    public async Task BitInfiniteScrolling_FastScrolling_NeverLoadsAPageTwice()
    {
        await Open();

        await Expect(Rows()).ToHaveCountAsync(20);

        var box = await List().BoundingBoxAsync();
        Assert.IsNotNull(box);
        await Page.Mouse.MoveAsync(box.X + box.Width / 2, box.Y + box.Height / 2);

        // A reader flinging the list at its end for as long as it takes to reach the end of the source: every turn
        // of the wheel lands while a page is still on its way, and the end comes into view again after every page.
        for (var i = 0; i < 60; i++)
        {
            await Page.Mouse.WheelAsync(0, 2000);
            await List().EvaluateAsync("el => el.scrollTop = el.scrollHeight");

            if (await List().Locator(".bit-isc-end").CountAsync() > 0) break;

            await Page.WaitForTimeoutAsync(50);
        }

        await Expect(List().Locator(".bit-isc-end")).ToBeVisibleAsync();
        await Expect(Report("skips")).ToHaveTextAsync("0,20,40");
        await Expect(Rows()).ToHaveCountAsync(50);
        await Expect(Report("ends")).ToHaveTextAsync("1");

        await ExpectItemsInOrder();
    }



    private async Task Open()
    {
        await Page.GotoAsync($"{BaseUrl}{PageUrl}");
        await WaitForStatus("Ready");
    }

    private ILocator List() => Page.Locator("[data-probe='list'] .bit-isc");

    private ILocator Rows() => List().Locator(".row");

    private ILocator Report(string name) => Page.Locator($"[data-probe='list'] .report .{name}");

    // The mouse wheel over the list, turned until its end is in view - or until a page that came into view on the way
    // has arrived. One turn goes as far as the engine lets it (Firefox stops short of a page of the list), so the
    // reader keeps turning it.
    private async Task WheelToTheEnd()
    {
        var box = await List().BoundingBoxAsync();
        Assert.IsNotNull(box);

        await Page.Mouse.MoveAsync(box.X + box.Width / 2, box.Y + box.Height / 2);

        var rows = await Rows().CountAsync();
        const string reached = @"(rows) => {
                const el = document.querySelector(`[data-probe='list'] .bit-isc`);
                return el.scrollTop + el.clientHeight >= el.scrollHeight - 2 || el.querySelectorAll('.row').length > rows;
            }";

        for (var i = 0; i < 20; i++)
        {
            await Page.Mouse.WheelAsync(0, 600);

            if (await Page.EvaluateAsync<bool>(reached, rows)) return;

            await Page.WaitForTimeoutAsync(50);
        }

        await Page.WaitForFunctionAsync(reached, rows, new() { Timeout = DefaultTimeout });
    }

    // Every item once, in the order of the source.
    private async Task ExpectItemsInOrder()
    {
        var items = await List().EvaluateAsync<int[]>("el => [...el.querySelectorAll('.row')].map(r => +r.dataset.item)");

        CollectionAssert.AreEqual(Enumerable.Range(0, 50).ToArray(), items);
    }
}
