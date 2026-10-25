using System;
using System.Linq;
using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Performance;

/// <summary>
/// Browser tests of what <see cref="BitVirtualize{TItem}"/> puts in the DOM as it is scrolled: only a window of the
/// items is rendered, the wheel and a scroll offset set on the list bring in the window that offset shows - the right
/// items, at the right place - ScrollToIndexAsync and ScrollToEndAsync land where they say, and a list fed by an
/// items provider asks for the window it scrolls to and replaces its placeholders with what arrives. The lists of
/// <c>/regression/virtualize-behaviour</c> lay 40px rows out in a 400px viewport, ten rows in view at a time.
/// </summary>
[TestClass]
[TestCategory("Browser")]
[RequiresBrowser] // opt-in: RUN_BROWSER_TESTS=1 dotnet test --filter FullyQualifiedName~BitVirtualizeBrowserTests
public class BitVirtualizeBrowserTests : PerformanceTestBase
{
    private const string PageUrl = "/regression/virtualize-behaviour";

    // Ten rows in view, the overscan on either side and a row cut in half at each edge: anything near the size of
    // the list (10,000) would mean it is not virtualized at all.
    private const int MaxRenderedRows = 30;

    [TestMethod]
    public async Task BitVirtualize_InitialRender_PutsOnlyTheFirstWindowInTheDom()
    {
        await Open();

        var rows = await RenderedItems("memory");

        Assert.IsTrue(rows.Length >= 10, $"the ten rows in view are rendered, got {rows.Length}");
        Assert.IsTrue(rows.Length <= MaxRenderedRows, $"only a window is rendered, got {rows.Length}");
        Assert.AreEqual(0, rows.Min());
        CollectionAssert.AreEqual(Enumerable.Range(0, rows.Length).ToArray(), rows.OrderBy(r => r).ToArray(), "a contiguous window from the first item");

        await ExpectRowAt("memory", 0, 0);
        await ExpectRowAt("memory", 9, 360);

        // The spacer gives the scrollbar the whole list.
        var scrollHeight = await List("memory").EvaluateAsync<double>("el => el.scrollHeight");
        Assert.AreEqual(10_000 * 40, scrollHeight, 2, "the list scrolls over all of its items");
    }

    [TestMethod]
    public async Task BitVirtualize_MouseWheel_BringsInTheNextWindow()
    {
        await Open();

        var box = await List("memory").BoundingBoxAsync();
        Assert.IsNotNull(box);

        await Page.Mouse.MoveAsync(box.X + box.Width / 2, box.Y + box.Height / 2);

        for (var i = 0; i < 5; i++)
        {
            await Page.Mouse.WheelAsync(0, 400);
        }

        await Page.WaitForFunctionAsync("() => document.querySelector(`[data-probe='memory'] .bit-vir`).scrollTop >= 1200",
            null, new() { Timeout = DefaultTimeout });

        await ExpectWindowFollowsTheScroll("memory");
    }

    [TestMethod]
    public async Task BitVirtualize_ScrollOffset_RendersTheItemsThatOffsetShows()
    {
        await Open();

        // Item 500 starts 20,000px down the list; the one in the middle of the viewport is 505.
        await List("memory").EvaluateAsync("el => el.scrollTop = 20000");

        await ExpectRowAt("memory", 500, 0);
        await ExpectRowAt("memory", 505, 200);
        await ExpectWindowFollowsTheScroll("memory");

        var rows = await RenderedItems("memory");
        Assert.IsFalse(rows.Contains(0), "the first window is gone");
        Assert.IsTrue(rows.Length <= MaxRenderedRows, $"still only a window, got {rows.Length}");

        await Expect(Report("memory", "range")).ToHaveTextAsync(new System.Text.RegularExpressions.Regex("^500-"));
    }

    [TestMethod]
    public async Task BitVirtualize_ScrollToIndexAndToEnd_LandOnTheirItems()
    {
        await Open();

        await Probe("memory").Locator(".scroll-to-index").ClickAsync();

        await ExpectRowAt("memory", 1234, 0);
        await Expect(Report("memory", "range")).ToHaveTextAsync(new System.Text.RegularExpressions.Regex("^1234-"));

        await Probe("memory").Locator(".scroll-to-end").ClickAsync();

        // The last row sits on the bottom edge of the viewport, which is inside the border of the list.
        var clientHeight = await List("memory").EvaluateAsync<double>("el => el.clientHeight");
        await ExpectRowAt("memory", 9999, clientHeight - 40);
        await Expect(Report("memory", "at-end")).ToHaveTextAsync("True");
        Assert.IsTrue((await RenderedItems("memory")).Length <= MaxRenderedRows);
    }

    [TestMethod]
    public async Task BitVirtualize_ItemsProvider_LoadsTheWindowItIsScrolledTo()
    {
        await Open();

        await ExpectRowAt("provider", 0, 0);

        await List("provider").ScrollIntoViewIfNeededAsync();
        await List("provider").EvaluateAsync("el => el.scrollTop = 40000");

        // The placeholders stand in for the window until it arrives, and the window is the one scrolled to.
        await ExpectRowAt("provider", 1000, 0);
        await Expect(Probe("provider").Locator(".row[data-item='1000']")).ToHaveTextAsync("Record 1000");
        await Expect(Probe("provider").Locator(".placeholder")).ToHaveCountAsync(0);

        var rows = await RenderedItems("provider");
        Assert.IsTrue(rows.Length <= MaxRenderedRows, $"only a window is rendered, got {rows.Length}");

        await Probe("provider").Locator(".show-requests").ClickAsync();
        await Expect(Report("provider", "requests")).Not.ToBeEmptyAsync();

        var requests = (await Report("provider", "requests").InnerTextAsync()).Split(';', StringSplitOptions.RemoveEmptyEntries);
        var covering = requests.Select(r => r.Split('+').Select(int.Parse).ToArray())
                               .Where(r => r[0] <= 1000 && r[0] + r[1] > 1009)
                               .ToArray();
        Assert.IsTrue(covering.Length > 0, $"a window covering the rows in view was asked for: {string.Join(";", requests)}");
        Assert.IsTrue(requests.All(r => int.Parse(r.Split('+')[1]) <= 100), $"no request asks for the whole list: {string.Join(";", requests)}");
    }



    private async Task Open()
    {
        await Page.GotoAsync($"{BaseUrl}{PageUrl}");
        await WaitForStatus("Ready");

        await Expect(Probe("memory").Locator(".row[data-item='0']")).ToBeVisibleAsync();
    }

    private ILocator Probe(string probe) => Page.Locator($"[data-probe='{probe}']");

    private ILocator List(string probe) => Probe(probe).Locator(".bit-vir");

    private ILocator Report(string probe, string name) => Probe(probe).Locator($".report .{name}");

    private Task<int[]> RenderedItems(string probe)
    {
        return Probe(probe).EvaluateAsync<int[]>("el => [...el.querySelectorAll('.bit-vir-itm .row')].map(r => +r.dataset.item)");
    }

    // The row of an item is rendered, and is laid out where the reader sees it: `top` pixels below the top of the
    // viewport of the list.
    private Task ExpectRowAt(string probe, int item, double top)
    {
        return Page.WaitForFunctionAsync(@"([probe, item, top]) => {
                const list = document.querySelector(`[data-probe='${probe}'] .bit-vir`);
                const row = list.querySelector(`.row[data-item='${item}']`);
                if (!row) return false;
                const viewport = list.getBoundingClientRect().top + list.clientTop;
                return Math.abs(row.getBoundingClientRect().top - viewport - top) < 1.5;
            }", new object[] { probe, item, top }, new() { Timeout = DefaultTimeout });
    }

    // Whatever the scroll offset, the rows in view are the ones it puts there: the row the reader sees at every
    // 40px of the viewport is the item the offset makes it, and the window rendered covers the viewport.
    private async Task ExpectWindowFollowsTheScroll(string probe)
    {
        await Page.WaitForFunctionAsync(@"(probe) => {
                const list = document.querySelector(`[data-probe='${probe}'] .bit-vir`);
                const box = list.getBoundingClientRect();
                const first = Math.floor(list.scrollTop / 40);
                for (let i = 0; i < 10; i++) {
                    const y = box.top + list.clientTop + (first + i) * 40 - list.scrollTop + 20;
                    if (y >= box.bottom) break;
                    const hit = document.elementFromPoint(box.left + 50, y)?.closest('.row');
                    if (!hit || +hit.dataset.item !== first + i) return false;
                }
                return true;
            }", probe, new() { Timeout = DefaultTimeout });
    }
}
