namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// BitDataGrid's paging on /e2e/data-grid/paging: the pager's range and page texts, its first/previous/next/last buttons
/// and their disabled states, the page-size picker, PageSizeOptions, every PagerPlacement (Bottom, Top, TopAndBottom
/// kept in step, and another side falling back to the bottom), OnPageChange, the absolute aria-rowindex of a later
/// page, the paging API (GoToPageAsync clamped, SetPageSizeAsync, CurrentPage/TotalPages/TotalCount), a PageSize set
/// by the page at runtime, and a grid that is not Pageable.
/// </summary>
[TestClass]
public class BitDataGridPagingE2ETests : BitDataGridE2ETestBase
{
    private const string Url = "/e2e/data-grid/paging";

    private ILocator PagerButton(string gridId, string label, int pager = 0)
        => Pagers(gridId).Nth(pager).Locator($"button[aria-label='{label}']");

    private ILocator Range(string gridId, int pager = 0) => Pagers(gridId).Nth(pager).Locator(".bit-dtg-pager-info > span");

    private ILocator Current(string gridId, int pager = 0) => Pagers(gridId).Nth(pager).Locator(".bit-dtg-pager-current");

    private ILocator PageSize(string gridId, int pager = 0) => Pagers(gridId).Nth(pager).Locator("select.bit-dtg-page-size");

    [TestMethod]
    public async Task Pager_TheFirstPage_ShowsItsRangeAndDisablesTheWayBack()
    {
        await OpenAsync(Url);

        const string id = "grid-pager";
        await Expect(Pagers(id)).ToHaveCountAsync(1);
        await Expect(Range(id)).ToHaveTextAsync("1–10 of 45");
        await Expect(Current(id)).ToHaveTextAsync("Page 1 of 5");
        await Expect(DataRows(id)).ToHaveCountAsync(10);
        await ExpectColumnAsync(id, 2, Enumerable.Range(1, 10).Select(i => $"Item {i:00}").ToArray());
        await Expect(Table(id)).ToHaveAttributeAsync("aria-rowcount", "46");

        foreach (var label in new[] { "First page", "Previous page" })
        {
            await Expect(PagerButton(id, label)).ToBeDisabledAsync();
            await Expect(PagerButton(id, label)).ToHaveAttributeAsync("aria-disabled", "true");
        }
        foreach (var label in new[] { "Next page", "Last page" })
        {
            await Expect(PagerButton(id, label)).ToBeEnabledAsync();
            await Expect(PagerButton(id, label)).Not.ToHaveAttributeAsync("aria-disabled", "true");
        }

        await Expect(PageSize(id)).ToHaveAccessibleNameAsync("Rows per page");
        await Expect(PageSize(id).Locator("option")).ToHaveTextAsync(["10 / page", "20 / page", "50 / page", "100 / page"]);
        await Expect(PageSize(id)).ToHaveValueAsync("10");

        // The pager comes after the rows.
        Assert.IsTrue((await BoxAsync(Pagers(id))).Y > (await BoxAsync(Viewport(id))).Y);
    }

    [TestMethod]
    public async Task Pager_TheButtons_MoveBetweenThePages()
    {
        await OpenAsync(Url);

        const string id = "grid-pager";

        await PagerButton(id, "Next page").ClickAsync();
        await Expect(Range(id)).ToHaveTextAsync("11–20 of 45");
        await Expect(Current(id)).ToHaveTextAsync("Page 2 of 5");
        await Expect(Cell(id, 0, 2)).ToHaveTextAsync("Item 11");
        await Expect(DataRow(id, 0)).ToHaveAttributeAsync("aria-rowindex", "12");
        await Expect(Text("page-change-count")).ToHaveTextAsync("1");
        await Expect(Text("page-change-last")).ToHaveTextAsync("2");
        await Expect(LiveRegion(id)).ToContainTextAsync("Page 2 of 5");
        await Expect(PagerButton(id, "Previous page")).ToBeEnabledAsync();

        await PagerButton(id, "Last page").ClickAsync();
        await Expect(Range(id)).ToHaveTextAsync("41–45 of 45");
        await Expect(Current(id)).ToHaveTextAsync("Page 5 of 5");
        await Expect(DataRows(id)).ToHaveCountAsync(5);
        await Expect(PagerButton(id, "Next page")).ToBeDisabledAsync();
        await Expect(PagerButton(id, "Last page")).ToBeDisabledAsync();
        await Expect(Text("page-change-last")).ToHaveTextAsync("5");

        await PagerButton(id, "Previous page").ClickAsync();
        await Expect(Current(id)).ToHaveTextAsync("Page 4 of 5");
        await Expect(Cell(id, 0, 2)).ToHaveTextAsync("Item 31");

        await PagerButton(id, "First page").ClickAsync();
        await Expect(Current(id)).ToHaveTextAsync("Page 1 of 5");
        await Expect(Cell(id, 0, 2)).ToHaveTextAsync("Item 01");
        await Expect(Text("page-change-count")).ToHaveTextAsync("4");
        await Expect(Text("page-change-last")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Pager_ThePageSizePicker_ChangesThePageSizeAndGoesBackToTheFirstPage()
    {
        await OpenAsync(Url);

        const string id = "grid-pager";
        await PagerButton(id, "Next page").ClickAsync();
        await Expect(Current(id)).ToHaveTextAsync("Page 2 of 5");

        await PageSize(id).SelectOptionAsync("20");

        await Expect(Range(id)).ToHaveTextAsync("1–20 of 45");
        await Expect(Current(id)).ToHaveTextAsync("Page 1 of 3");
        await Expect(DataRows(id)).ToHaveCountAsync(20);
        await Expect(Text("page-change-last")).ToHaveTextAsync("1");

        await PageSize(id).SelectOptionAsync("50");
        await Expect(Current(id)).ToHaveTextAsync("Page 1 of 1");
        await Expect(DataRows(id)).ToHaveCountAsync(45);
        await Expect(PagerButton(id, "Next page")).ToBeDisabledAsync();
    }

    [TestMethod]
    public async Task Placement_Top_PutsThePagerAboveTheRows()
    {
        await OpenAsync(Url);

        const string id = "grid-pager-top";
        await Expect(Pagers(id)).ToHaveCountAsync(1);
        Assert.IsTrue((await BoxAsync(Pagers(id))).Y < (await BoxAsync(Viewport(id))).Y, "The pager is not above the rows.");
        await Expect(Current(id)).ToHaveTextAsync("Page 1 of 3");
    }

    [TestMethod]
    public async Task Placement_TopAndBottom_RendersTwoPagersInStep()
    {
        await OpenAsync(Url);

        const string id = "grid-pager-both";
        await Expect(Pagers(id)).ToHaveCountAsync(2);
        var view = await BoxAsync(Viewport(id));
        Assert.IsTrue((await BoxAsync(Pagers(id).Nth(0))).Y < view.Y);
        Assert.IsTrue((await BoxAsync(Pagers(id).Nth(1))).Y > view.Y);

        await PagerButton(id, "Next page", pager: 0).ClickAsync();
        await Expect(Current(id, 0)).ToHaveTextAsync("Page 2 of 3");
        await Expect(Current(id, 1)).ToHaveTextAsync("Page 2 of 3");

        await PagerButton(id, "Last page", pager: 1).ClickAsync();
        await Expect(Current(id, 0)).ToHaveTextAsync("Page 3 of 3");
        await Expect(Range(id, 0)).ToHaveTextAsync("21–25 of 25");
        await Expect(Range(id, 1)).ToHaveTextAsync("21–25 of 25");
    }

    [TestMethod]
    public async Task Placement_AnotherSide_LeavesThePagerUnderTheRows()
    {
        await OpenAsync(Url);

        const string id = "grid-pager-start";
        await Expect(Pagers(id)).ToHaveCountAsync(1);
        Assert.IsTrue((await BoxAsync(Pagers(id))).Y > (await BoxAsync(Viewport(id))).Y);
    }

    [TestMethod]
    public async Task PageSizeOptions_TheChoices_AreTheOnesGiven()
    {
        await OpenAsync(Url);

        const string id = "grid-page-options";
        await Expect(PageSize(id).Locator("option")).ToHaveTextAsync(["5 / page", "15 / page"]);
        await Expect(DataRows(id)).ToHaveCountAsync(5);

        await PageSize(id).SelectOptionAsync("15");
        await Expect(DataRows(id)).ToHaveCountAsync(15);
        await Expect(Current(id)).ToHaveTextAsync("Page 1 of 2");
    }

    [TestMethod]
    public async Task Api_GoToPageAndSetPageSize_DriveThePagerFromCode()
    {
        await OpenAsync(Url);

        const string id = "grid-pager";

        await Page.Locator("#btn-go-page-3").ClickAsync();
        await Expect(Current(id)).ToHaveTextAsync("Page 3 of 5");
        await Expect(Cell(id, 0, 2)).ToHaveTextAsync("Item 21");
        await Expect(Text("page-change-last")).ToHaveTextAsync("3");

        await Page.Locator("#btn-read-state").ClickAsync();
        await Expect(Text("pager-state")).ToHaveTextAsync("page 3 of 5, total 45");

        // Clamped to the last page.
        await Page.Locator("#btn-go-page-99").ClickAsync();
        await Expect(Current(id)).ToHaveTextAsync("Page 5 of 5");

        await Page.Locator("#btn-page-size-20").ClickAsync();
        await Expect(Current(id)).ToHaveTextAsync("Page 1 of 3");
        await Expect(PageSize(id)).ToHaveValueAsync("20");
        await Expect(DataRows(id)).ToHaveCountAsync(20);

        await Page.Locator("#btn-read-state").ClickAsync();
        await Expect(Text("pager-state")).ToHaveTextAsync("page 1 of 3, total 45");
    }

    [TestMethod]
    public async Task PageSize_ChangedByThePage_ReRendersThePages()
    {
        await OpenAsync(Url);

        const string id = "grid-pager";
        await Page.Locator("#btn-param-page-size-5").ClickAsync();

        await Expect(DataRows(id)).ToHaveCountAsync(5);
        await Expect(Current(id)).ToHaveTextAsync("Page 1 of 9");
        await Expect(PageSize(id)).ToHaveValueAsync("5");
    }

    [TestMethod]
    public async Task Pageable_Off_RendersEveryRowAndNoPager()
    {
        await OpenAsync(Url);

        await Expect(Pagers("grid-not-pageable")).ToHaveCountAsync(0);
        await Expect(DataRows("grid-not-pageable")).ToHaveCountAsync(25);
    }
}
