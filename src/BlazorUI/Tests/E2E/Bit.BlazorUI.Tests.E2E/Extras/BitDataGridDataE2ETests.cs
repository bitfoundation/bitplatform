using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// BitDataGrid's data modes on /e2e/data-grid/data: row virtualization of a 5,000-row list (a bounded DOM, the full
/// scroll height, the rows of any scroll position with their absolute aria-rowindex), column virtualization of 40
/// columns following a sideways scroll, OnRead with paging (the request's window, sorts, filters and search, the
/// Loading state and aria-busy, a superseded request cancelled), OnRead with virtualization (windows fetched as the
/// viewport scrolls, the result's aggregates in the footer), OnLoadMore infinite scrolling (batches appended near the
/// end, the skeleton row while one loads, the end message after a short batch), and an IQueryable source.
/// </summary>
[TestClass]
public class BitDataGridDataE2ETests : BitDataGridE2ETestBase
{
    private const string Url = "/e2e/data-grid/data";

    // The id and aria-rowindex of the first row wholly in view under the sticky header.
    private Task<int[]> FirstRowInViewAsync(string gridId) => Viewport(gridId).EvaluateAsync<int[]>("""
        vp => {
            const view = vp.getBoundingClientRect();
            const header = vp.querySelector('.bit-dtg-header').getBoundingClientRect();
            const row = [...vp.querySelectorAll('.bit-dtg-body > .bit-dtg-row[aria-rowindex]:not(.bit-dtg-placeholder-row)')]
                .find(r => { const b = r.getBoundingClientRect(); return b.top >= header.bottom - 1 && b.bottom <= view.bottom; });
            const id = row ? parseInt(row.querySelector('[aria-colindex="1"]')?.textContent ?? '') : NaN;
            return Number.isNaN(id) ? [0, 0] : [id, parseInt(row.getAttribute('aria-rowindex'))];
        }
        """);

    [TestMethod]
    public async Task Virtualize_ATallList_RendersOnlyTheRowsInView()
    {
        await OpenAsync(Url);

        const string id = "grid-virtual";
        await Expect(Cell(id, 0, 2)).ToHaveTextAsync("Item 0001");
        await Expect(Table(id)).ToHaveAttributeAsync("aria-rowcount", "5001");
        // Virtualize's spacers sit among the rows, so the body drops the rowgroup role.
        await Expect(Page.Locator($"#{id} .bit-dtg-body")).Not.ToHaveAttributeAsync("role", new Regex(".*"));

        var rendered = await DataRows(id).CountAsync();
        Assert.IsTrue(rendered < 80, $"{rendered} of 5,000 rows are in the DOM.");

        var scrollHeight = await Viewport(id).EvaluateAsync<double>("el => el.scrollHeight");
        Assert.IsTrue(scrollHeight > 5000 * 36 * 0.95, $"The scroll height ({scrollHeight}px) does not reserve every row.");

        // Halfway down, the rows of halfway down are in view, numbered by their place in the whole list.
        await Viewport(id).EvaluateAsync("el => el.scrollTop = el.scrollHeight / 2");
        int[] first = [];
        await UntilAsync(async () => (first = await FirstRowInViewAsync(id))[0] is > 2300 and < 2700, "The rows halfway down the list were not rendered in view.");
        Assert.AreEqual(first[0] + 1, first[1], "A row's aria-rowindex is not its place in the whole list.");
        Assert.IsTrue(await DataRows(id).CountAsync() < 80, "Scrolling piled rows up in the DOM.");

        // And at the end, the last row, in view and numbered last. Read in one go, since the virtualizer may still be
        // settling its window while the viewport takes its last scroll position.
        string? lastRow = null;
        await UntilAsync(async () =>
        {
            lastRow = await Viewport(id).EvaluateAsync<string?>("""
                vp => {
                    vp.scrollTop = vp.scrollHeight;
                    const view = vp.getBoundingClientRect();
                    const row = [...vp.querySelectorAll('.bit-dtg-body > .bit-dtg-row')].find(r => r.textContent.includes('Item 5000'));
                    if (!row) return null;
                    const box = row.getBoundingClientRect();
                    return box.top >= view.top && box.bottom <= view.bottom + 1 ? row.getAttribute('aria-rowindex') : null;
                }
                """);
            return lastRow is not null;
        }, "The last row was never rendered in view at the end of the list.");
        Assert.AreEqual("5001", lastRow);
        Assert.IsTrue(await DataRows(id).CountAsync() < 80, "Scrolling to the end piled rows up in the DOM.");
    }

    [TestMethod]
    public async Task VirtualizeColumns_ManyColumns_RenderOnlyTheOnesInViewAndFollowTheScroll()
    {
        await OpenAsync(Url);

        const string id = "grid-virtual-columns";
        var renderedHeaders = HeaderRow(id).Locator("> [role=columnheader]:not([aria-hidden=true])");
        await UntilAsync(async () => await renderedHeaders.CountAsync() < 20, "The columns out of view were rendered.");
        await Expect(HeaderRow(id).Locator("> [aria-colindex='3']")).ToHaveTextAsync("C02");
        await Expect(HeaderRow(id).GetByText("C40")).ToHaveCountAsync(0);
        await Expect(Table(id)).ToHaveAttributeAsync("aria-colcount", "41");

        await Viewport(id).EvaluateAsync("el => el.scrollLeft = el.scrollWidth");

        await Expect(HeaderRow(id).GetByText("C40")).ToHaveCountAsync(1);
        await Expect(HeaderRow(id).GetByText("C02", new() { Exact = true })).ToHaveCountAsync(0);
        // The frozen column stays rendered and the row cells follow their headers.
        await Expect(HeaderRow(id).Locator("> [aria-colindex='1']")).ToHaveTextAsync("Id");
        await Expect(Cell(id, 0, 41)).ToHaveTextAsync("140");
        Assert.IsTrue(await renderedHeaders.CountAsync() < 20);
    }

    [TestMethod]
    public async Task Server_OnRead_IsAskedForEachPageWithTheView()
    {
        await OpenAsync(Url);

        const string id = "grid-server";
        await Expect(Text("server-last")).ToHaveTextAsync("skip 0, take 10, sorts , filters 0, search , total 95");
        await Expect(Cell(id, 0, 2)).ToHaveTextAsync("Item 0001");
        await Expect(Pagers(id).Locator(".bit-dtg-pager-info > span")).ToHaveTextAsync("1–10 of 95");
        await Expect(Table(id)).ToHaveAttributeAsync("aria-rowcount", "97");

        await Pagers(id).Locator("button[aria-label='Next page']").ClickAsync();
        await Expect(Text("server-last")).ToHaveTextAsync(new Regex("^skip 10, take 10,"));
        await Expect(Cell(id, 0, 2)).ToHaveTextAsync("Item 0011");

        await HeaderButton(id, "Name").ClickAsync();
        await HeaderButton(id, "Name").ClickAsync();
        await Expect(Text("server-last")).ToHaveTextAsync(new Regex("sorts Name:Descending,"));
        await Expect(Cell(id, 0, 2)).ToHaveTextAsync(new Regex("^Item 00(95|85)$"));

        await FilterRow(id).Locator("[aria-label='Filter by Name']").FillAsync("Item 004");
        await Expect(Text("server-last")).ToHaveTextAsync(new Regex("^skip 0, take 10, sorts Name:Descending, filters 1, search , total 10$"));
        await Expect(Cell(id, 0, 2)).ToHaveTextAsync("Item 0049");

        await Toolbar(id).Locator("input.bit-dtg-search-input").FillAsync("0045");
        await Expect(Text("server-last")).ToHaveTextAsync(new Regex("search 0045, total 1$"));
        await ExpectColumnAsync(id, 2, "Item 0045");
    }

    [TestMethod]
    public async Task Server_TheLoadingParameter_ShowsTheSpinnerWhileARequestIsInFlight()
    {
        await OpenAsync(Url);

        const string id = "grid-server";
        await Expect(Cell(id, 0, 2)).ToHaveTextAsync("Item 0001");
        await Expect(Table(id)).Not.ToHaveAttributeAsync("aria-busy", new Regex(".*"));

        await Pagers(id).Locator("button[aria-label='Last page']").ClickAsync();

        await Expect(Table(id)).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(Page.Locator($"#{id} .bit-dtg-loading")).ToContainTextAsync("Loading…");

        await Expect(Cell(id, 0, 2)).ToHaveTextAsync("Item 0091");
        await Expect(Table(id)).Not.ToHaveAttributeAsync("aria-busy", new Regex(".*"));
        await Expect(DataRows(id)).ToHaveCountAsync(5);
    }

    [TestMethod]
    public async Task Server_ANewerRequest_CancelsTheOneInFlight()
    {
        await OpenAsync(Url);

        const string id = "grid-server";
        await Expect(Cell(id, 0, 2)).ToHaveTextAsync("Item 0001");

        // A second filter while the first one's request is still in flight supersedes it: the two input events are
        // dispatched together, so the first request is in flight for certain when the second arrives.
        await FilterRow(id).Locator("[aria-label='Filter by Name']").EvaluateAsync("""
            el => {
                for (const value of ['Item 00', 'Item 007']) {
                    el.value = value;
                    el.dispatchEvent(new Event('input', { bubbles: true }));
                }
            }
            """);

        await Expect(Text("server-last")).ToHaveTextAsync(new Regex("filters 1, search , total 10$"));
        await ExpectColumnAsync(id, 2, Enumerable.Range(70, 10).Select(i => $"Item {i:0000}").ToArray());
        var cancelled = int.Parse(await Text("server-cancelled").InnerTextAsync());
        Assert.IsTrue(cancelled >= 1, "No superseded request was cancelled.");
    }

    [TestMethod]
    public async Task ServerVirtualize_Scrolling_FetchesTheWindowsInViewAndTheFooterShowsTheServersTotals()
    {
        await OpenAsync(Url);

        const string id = "grid-server-virtual";
        await Expect(Cell(id, 0, 2)).ToHaveTextAsync("Item 0001");
        await Expect(FooterRow(id).Locator("> [aria-colindex='3']")).ToHaveTextAsync("Σ 2001000 in all");
        await Expect(Table(id)).ToHaveAttributeAsync("aria-rowcount", "2002");
        Assert.IsTrue(await DataRows(id).CountAsync() < 100);

        await Viewport(id).EvaluateAsync("el => el.scrollTop = el.scrollHeight * 0.75");

        int[] first = [];
        await UntilAsync(async () => (first = await FirstRowInViewAsync(id))[0] is > 1300 and < 1700, "The rows three quarters down were not fetched into view.");
        Assert.AreEqual(first[0] + 1, first[1], "A row's aria-rowindex is not its place in the whole set.");
        Assert.IsTrue(int.Parse(await Text("virtual-max-skip").InnerTextAsync()) > 1000, "No window of the scrolled-to rows was asked for.");
        Assert.IsTrue(await DataRows(id).CountAsync() < 100, "The scrolled-past rows piled up in the DOM.");
    }

    [TestMethod]
    public async Task Infinite_ScrollingToTheEnd_LoadsTheNextBatchUntilAShortOneEndsTheList()
    {
        await OpenAsync(Url);

        const string id = "grid-infinite";
        await Expect(DataRows(id)).ToHaveCountAsync(20);
        await Expect(Text("infinite-last")).ToHaveTextAsync("skip 0, take 20, got 20");
        await Expect(Pagers(id)).ToHaveCountAsync(0);

        await Viewport(id).EvaluateAsync("el => el.scrollTop = el.scrollHeight");
        await Expect(Page.Locator($"#{id} .bit-dtg-placeholder-row")).ToHaveCountAsync(1);
        await Expect(Table(id)).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(DataRows(id)).ToHaveCountAsync(40);
        await Expect(Text("infinite-last")).ToHaveTextAsync("skip 20, take 20, got 20");
        await Expect(Table(id)).Not.ToHaveAttributeAsync("aria-busy", new Regex(".*"));

        await Viewport(id).EvaluateAsync("el => el.scrollTop = el.scrollHeight");
        await Expect(DataRows(id)).ToHaveCountAsync(55);
        await Expect(Text("infinite-last")).ToHaveTextAsync("skip 40, take 20, got 15");
        await Expect(Page.Locator($"#{id} .bit-dtg-infinite-end")).ToHaveTextAsync("- End of results -");

        // A short batch was the last one: scrolling on asks for nothing more.
        await Viewport(id).EvaluateAsync("el => el.scrollTop = el.scrollHeight");
        await NextFramesAsync();
        await Expect(Text("infinite-batches")).ToHaveTextAsync("3");
        await Expect(Cell(id, 54, 2)).ToHaveTextAsync("Item 0055");
    }

    [TestMethod]
    public async Task Queryable_AnIQueryableSource_FiltersAndPagesAtTheSource()
    {
        await OpenAsync(Url);

        const string id = "grid-queryable";
        await Expect(Pagers(id).Locator(".bit-dtg-pager-info > span")).ToHaveTextAsync("1–5 of 23");
        await Expect(Pagers(id).Locator(".bit-dtg-pager-current")).ToHaveTextAsync("Page 1 of 5");

        await FilterRow(id).Locator("[aria-label='Filter by Name']").FillAsync("Item 001");
        await Expect(Pagers(id).Locator(".bit-dtg-pager-info > span")).ToHaveTextAsync("1–5 of 10");
        await ExpectColumnAsync(id, 2, "Item 0010", "Item 0011", "Item 0012", "Item 0013", "Item 0014");

        await Pagers(id).Locator("button[aria-label='Next page']").ClickAsync();
        await ExpectColumnAsync(id, 2, "Item 0015", "Item 0016", "Item 0017", "Item 0018", "Item 0019");

        await HeaderButton(id, "Name").ClickAsync();
        await HeaderButton(id, "Name").ClickAsync();
        await Pagers(id).Locator("button[aria-label='First page']").ClickAsync();
        await ExpectColumnAsync(id, 2, "Item 0019", "Item 0018", "Item 0017", "Item 0016", "Item 0015");
    }

    // How long, in seconds, the arrow an unsorted header shows under the pointer takes to fade in.
    private Task<double> SortHintTransitionAsync(string gridId, string title)
        => HeaderButton(gridId, title).EvaluateAsync<double>("""
            el => {
                const d = getComputedStyle(el, '::after').transitionDuration;
                return d.endsWith('ms') ? parseFloat(d) / 1000 : parseFloat(d);
            }
            """);

    [TestMethod]
    public async Task Motion_TheSkeletonShimmersAndTheSortHintFades()
    {
        await OpenAsync(Url);

        var skeleton = Page.Locator("#grid-skeleton .bit-dtg-placeholder-row .bit-dtg-skeleton");
        await Expect(skeleton).ToBeVisibleAsync();
        await Expect(Table("grid-skeleton")).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(skeleton).ToHaveCSSAsync("animation-name", "bit-dtg-shimmer");
        Assert.IsTrue(await SortHintTransitionAsync("grid-server", "Name") >= 0.05, "The sort hint does not fade in.");

        await Page.Locator("#btn-release-skeleton").ClickAsync();
        await Expect(DataRows("grid-skeleton")).ToHaveCountAsync(8);
        await Expect(skeleton).ToHaveCountAsync(0);
        await Expect(Page.Locator("#grid-skeleton .bit-dtg-infinite-end")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task ReducedMotion_TheSkeletonStopsAndTheSortHintNoLongerFades_UnlessForced()
    {
        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        await OpenAsync(Url);

        var skeleton = Page.Locator("#grid-skeleton .bit-dtg-placeholder-row .bit-dtg-skeleton");
        await Expect(skeleton).ToBeVisibleAsync();
        await Expect(skeleton).ToHaveCSSAsync("animation-name", "none");
        var duration = await SortHintTransitionAsync("grid-server", "Name");
        Assert.IsTrue(duration < 0.01, $"The sort hint still fades in over {duration}s under reduced motion.");

        // ForceAnimation (the bit-fam class it puts on the root) brings the shimmer back.
        await Expect(Grid("grid-skeleton-fam")).ToHaveClassAsync(HasClass("bit-fam"));
        await Expect(Page.Locator("#grid-skeleton-fam .bit-dtg-skeleton")).ToHaveCSSAsync("animation-name", "bit-dtg-shimmer");
    }
}
