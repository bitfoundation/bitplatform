using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// BitDataGrid's programmatic control, state and localization on /e2e/data-grid/api: SortByAsync (replacing, additive,
/// None) and ClearSortsAsync with OnSortChange, ApplyFilterAsync / ApplyRangeFilterAsync / ClearFilterAsync /
/// ClearFiltersAsync with OnFilterChange and the filter box showing a value applied from code, GroupByAsync /
/// UngroupAsync / ClearGroupsAsync with OnGroupChange, GoToPageAsync / SetPageSizeAsync with OnPageChange,
/// MoveColumnAsync, SearchAsync, RefreshAsync picking up an in-place change of the list, the ActiveSorts / ActiveFilters /
/// ActiveGroups / ActiveSearch / TotalCount / TotalPages / CurrentPage readouts, OnStateChange with GetState and
/// ApplyStateAsync restoring sorts, filters, page, widths and hidden columns on a new grid across a JSON round trip, and
/// the Strings that localize every text and accessible name while the rest keep their English default.
/// </summary>
[TestClass]
public class BitDataGridApiE2ETests : BitDataGridE2ETestBase
{
    private const string Url = "/e2e/data-grid/api";
    private const string Id = "grid-api";

    private ILocator Range(string gridId) => Pagers(gridId).Locator(".bit-dtg-pager-info > span");

    private ILocator Current(string gridId) => Pagers(gridId).Locator(".bit-dtg-pager-current");

    [TestMethod]
    public async Task Sort_FromCode_SortsReplacesAddsAndClears()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-sort-price-desc").ClickAsync();
        await Expect(HeaderCell(Id, 4)).ToHaveAttributeAsync("aria-sort", "descending");
        await Expect(Cell(Id, 0, 2)).ToHaveTextAsync("Product 23");
        await Expect(Text("sort-log")).ToHaveTextAsync("Price:Descending:1");

        await Page.Locator("#btn-sort-name-additive").ClickAsync();
        await Expect(Text("sort-log")).ToHaveTextAsync("Price:Descending:1,Name:Ascending:2");
        await Expect(HeaderCell(Id, 2).Locator(".bit-dtg-sort-priority")).ToHaveTextAsync("2");

        await Page.Locator("#btn-sort-price-none").ClickAsync();
        await Expect(Text("sort-log")).ToHaveTextAsync("Name:Ascending:1");
        await Expect(HeaderCell(Id, 4)).Not.ToHaveAttributeAsync("aria-sort", new Regex(".*"));

        await Page.Locator("#btn-clear-sorts").ClickAsync();
        await Expect(Text("sort-log")).ToHaveTextAsync("");
        await Expect(HeaderCell(Id, 2)).Not.ToHaveAttributeAsync("aria-sort", new Regex(".*"));
        await Expect(LiveRegion(Id)).ToContainTextAsync("Sorting cleared");
    }

    [TestMethod]
    public async Task Filter_FromCode_AppliesRangesClearsOneAndClearsAll()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-filter-price").ClickAsync();
        await Expect(Range(Id)).ToHaveTextAsync("1–5 of 13");
        await Expect(Text("filter-log")).ToHaveTextAsync("Price:GreaterThan:500");
        await Expect(Cell(Id, 0, 2)).ToHaveTextAsync("Product 11");
        // The filter box shows the value applied from code.
        await Expect(FilterRow(Id).Locator("[aria-label='Filter by Price']")).ToHaveValueAsync("500");

        await Page.Locator("#btn-filter-range").ClickAsync();
        await Expect(Text("filter-log")).ToHaveTextAsync("Price:GreaterThanOrEqual:100,Price:LessThan:300");
        await ExpectColumnAsync(Id, 2, "Product 02", "Product 03", "Product 04", "Product 05");

        await Page.Locator("#btn-filter-name").ClickAsync();
        await Expect(Text("filter-log")).ToHaveTextAsync("Price:GreaterThanOrEqual:100,Price:LessThan:300,Name:StartsWith:P");
        await Expect(DataRows(Id)).ToHaveCountAsync(4);

        await Page.Locator("#btn-clear-filter-price").ClickAsync();
        await Expect(Text("filter-log")).ToHaveTextAsync("Name:StartsWith:P");
        await Expect(Range(Id)).ToHaveTextAsync("1–5 of 23");

        await Page.Locator("#btn-clear-filters").ClickAsync();
        await Expect(Text("filter-log")).ToHaveTextAsync("");

        // A filter with no operator is treated as omitted: every row still matches.
        await Page.Locator("#btn-filter-unspecified").ClickAsync();
        await Expect(Range(Id)).ToHaveTextAsync("1\u20135 of 23");
    }

    [TestMethod]
    public async Task Group_FromCode_GroupsUngroupsAndClears()
    {
        await OpenAsync(Url);

        var groupRows = Page.Locator("#grid-api .bit-dtg-body > .bit-dtg-group-row");

        await Page.Locator("#btn-group-category").ClickAsync();
        await Expect(groupRows.Locator(".bit-dtg-group-title")).ToHaveTextAsync(["Category: Electronics", "Category: Books", "Category: Toys"]);
        await Expect(groupRows.Locator(".bit-dtg-group-count")).ToHaveTextAsync(["(8)", "(8)", "(7)"]);
        await Expect(Text("group-log")).ToHaveTextAsync("Category:Ascending");

        await Page.Locator("#btn-ungroup-category").ClickAsync();
        await Expect(groupRows).ToHaveCountAsync(0);
        await Expect(Text("group-log")).ToHaveTextAsync("");

        await Page.Locator("#btn-group-category").ClickAsync();
        await Page.Locator("#btn-clear-groups").ClickAsync();
        await Expect(groupRows).ToHaveCountAsync(0);
        await Expect(Pagers(Id)).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task Paging_FromCode_GoesToAPageAndChangesThePageSize()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-page-3").ClickAsync();
        await Expect(Current(Id)).ToHaveTextAsync("Page 3 of 5");
        await Expect(Cell(Id, 0, 2)).ToHaveTextAsync("Product 11");
        await Expect(Text("page-log")).ToHaveTextAsync("3;");

        await Page.Locator("#btn-page-size-10").ClickAsync();
        await Expect(Current(Id)).ToHaveTextAsync("Page 1 of 3");
        await Expect(DataRows(Id)).ToHaveCountAsync(10);
        await Expect(Text("page-log")).ToHaveTextAsync("3;1;");
    }

    [TestMethod]
    public async Task Columns_MoveColumnAsync_MovesAColumnAndRaisesOnStateChange()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-move-price").ClickAsync();

        await ExpectHeaderTitlesAsync(Id, "Price", "Id", "Name", "Category");
        await Expect(DataRow(Id, 0).Locator("> [role=gridcell]")).ToHaveTextAsync(["50", "1", "Product 01", "Electronics"]);
        await Expect(Text("api-state-changes")).Not.ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Search_SearchAsync_SearchesAndFillsTheBox()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-search").ClickAsync();

        await Expect(Toolbar(Id).Locator("input.bit-dtg-search-input")).ToHaveValueAsync("Product 1");
        await Expect(Range(Id)).ToHaveTextAsync("1–5 of 10");
        await ExpectColumnAsync(Id, 2, "Product 10", "Product 11", "Product 12", "Product 13", "Product 14");
    }

    [TestMethod]
    public async Task Refresh_RefreshAsync_PicksUpAChangeMadeToTheListInPlace()
    {
        await OpenAsync(Url);

        // A change made to the list where nothing renders (a timer, a service) is not on screen yet.
        await Page.EvaluateAsync("() => window.__dtgApiPage.invokeMethodAsync('MutateQuietly')");
        await NextFramesAsync();
        await Expect(Range(Id)).ToHaveTextAsync("1–5 of 23");
        await Expect(Cell(Id, 0, 2)).ToHaveTextAsync("Product 01");

        // RefreshAsync, called with no render of the page around the grid, brings it in.
        await Page.EvaluateAsync("() => window.__dtgApiPage.invokeMethodAsync('RefreshQuietly')");
        await Expect(Range(Id)).ToHaveTextAsync("1–5 of 24");
        await Expect(Cell(Id, 0, 2)).ToHaveTextAsync("Added in place");

        // The page's RefreshAsync button does the same from a click.
        await Page.Locator("#btn-refresh").ClickAsync();
        await Expect(Range(Id)).ToHaveTextAsync("1–5 of 24");
    }

    [TestMethod]
    public async Task Readouts_TheActiveStateProperties_DescribeTheView()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-read").ClickAsync();
        await Expect(Text("api-read")).ToHaveTextAsync("sorts=;filters=;groups=;search=;total=23;pages=5;page=1");

        await Page.Locator("#btn-sort-price-desc").ClickAsync();
        await Page.Locator("#btn-filter-price").ClickAsync();
        await Page.Locator("#btn-search").ClickAsync();
        await Expect(Range(Id)).ToHaveTextAsync("1–5 of 9");
        await Page.Locator("#btn-page-size-10").ClickAsync();
        await Page.Locator("#btn-read").ClickAsync();
        await Expect(Text("api-read")).ToHaveTextAsync("sorts=Price:Descending;filters=Price:GreaterThan;groups=;search=Product 1;total=9;pages=1;page=1");

        await Page.Locator("#btn-group-category").ClickAsync();
        await Page.Locator("#btn-read").ClickAsync();
        await Expect(Text("api-read")).ToContainTextAsync("groups=Category;");
    }

    [TestMethod]
    public async Task State_ApplyStateAsync_RestoresTheViewOnANewGridAfterAJsonRoundTrip()
    {
        await OpenAsync(Url);

        const string id = "grid-state";
        await HeaderButton(id, "Name").ClickAsync();
        await HeaderButton(id, "Name").ClickAsync();
        await Expect(HeaderCell(id, 2)).ToHaveAttributeAsync("aria-sort", "descending");
        await Expect(Text("state-status")).ToHaveTextAsync("saved");

        await FilterRow(id).Locator("[aria-label='Filter by Name']").FillAsync("Product 1");
        await Pagers(id).Locator("button[aria-label='Next page']").ClickAsync();
        await Expect(Pagers(id).Locator(".bit-dtg-pager-current")).ToHaveTextAsync("Page 2 of 2");

        await HeaderCell(id, 2).Locator(".bit-dtg-resizer").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(HeaderCell(id, 2).Locator(".bit-dtg-resizer")).ToHaveAttributeAsync("aria-valuenow", "170");

        await Toolbar(id).GetByRole(AriaRole.Button, new() { Name = "Columns" }).ClickAsync();
        await Page.Locator($"#{id} .bit-dtg-column-chooser").GetByLabel("Category", new() { Exact = true }).UncheckAsync();
        await ExpectHeaderTitlesAsync(id, "Id", "Name", "Price");

        await Expect(Text("state-json")).ToContainTextAsync("\"CurrentPage\":2");
        var json = await Text("state-json").InnerTextAsync();
        StringAssert.Contains(json, "\"ColumnId\":\"Name\"");
        StringAssert.Contains(json, "\"Width\":170");
        StringAssert.Contains(json, "\"Visible\":false");

        await Page.Locator("#btn-recreate-restore").ClickAsync();
        await Expect(Text("state-status")).ToHaveTextAsync("restored");

        await ExpectHeaderTitlesAsync(id, "Id", "Name", "Price");
        await Expect(HeaderCell(id, 2)).ToHaveAttributeAsync("aria-sort", "descending");
        await Expect(FilterRow(id).Locator("[aria-label='Filter by Name']")).ToHaveValueAsync("Product 1");
        await Expect(Pagers(id).Locator(".bit-dtg-pager-current")).ToHaveTextAsync("Page 2 of 2");
        await Expect(HeaderCell(id, 2).Locator(".bit-dtg-resizer")).ToHaveAttributeAsync("aria-valuenow", "170");
        Assert.AreEqual(170, (await BoxAsync(HeaderCell(id, 2))).Width, 1.5);
        // Applying a state is not a change of the user's to report.
        await Expect(Text("state-status")).ToHaveTextAsync("restored");

        await Page.Locator("#btn-recreate-fresh").ClickAsync();
        await ExpectHeaderTitlesAsync(id, "Id", "Name", "Category", "Price");
        await Expect(HeaderCell(id, 2)).Not.ToHaveAttributeAsync("aria-sort", new Regex(".*"));
        await Expect(Pagers(id).Locator(".bit-dtg-pager-current")).ToHaveTextAsync("Page 1 of 5");
    }

    [TestMethod]
    public async Task Localization_TheStrings_LocalizeEveryTextAndNameTheyCover()
    {
        await OpenAsync(Url);

        const string id = "grid-localized";
        await Expect(Table(id)).ToHaveAccessibleNameAsync("Datentabelle");
        await Expect(Range(id)).ToHaveTextAsync("1–8 von 23");
        await Expect(Current(id)).ToHaveTextAsync("Seite 1 von 3");
        await Expect(Pagers(id).Locator("select.bit-dtg-page-size")).ToHaveAccessibleNameAsync("Zeilen pro Seite");
        await Expect(Pagers(id).Locator("select.bit-dtg-page-size option").First).ToHaveTextAsync("8 pro Seite");
        await Expect(Pagers(id).GetByRole(AriaRole.Button, new() { Name = "Nächste Seite" })).ToBeEnabledAsync();
        await Expect(Pagers(id).GetByRole(AriaRole.Button, new() { Name = "Erste Seite" })).ToBeDisabledAsync();

        var search = Toolbar(id).Locator("input.bit-dtg-search-input");
        await Expect(search).ToHaveAttributeAsync("placeholder", "Suchen…");
        await Expect(search).ToHaveAccessibleNameAsync("Alle Spalten durchsuchen");
        await Expect(FilterRow(id).Locator("[aria-label='Filtern nach Name']")).ToHaveAttributeAsync("placeholder", "Filtern…");

        await HeaderButton(id, "Name").ClickAsync();
        await Expect(LiveRegion(id)).ToContainTextAsync("Sortiert nach Name, aufsteigend");

        await search.FillAsync("zzz");
        await Expect(MessageRow(id)).ToHaveTextAsync("Keine Treffer für die aktuellen Filter.");
        await Expect(LiveRegion(id)).ToContainTextAsync("Suche nach zzz. Zeilen: 0");
        // What the Strings leave out keeps its English default.
        await Expect(Toolbar(id).Locator(".bit-dtg-search-clear")).ToHaveAccessibleNameAsync("Clear search");
    }
}
