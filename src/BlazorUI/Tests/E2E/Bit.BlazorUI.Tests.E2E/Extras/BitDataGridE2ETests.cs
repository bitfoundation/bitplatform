using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// The core of BitDataGrid on /e2e/data-grid: the prerender, the Property/Field/Template columns and their Format and
/// Align, sorting by header clicks (the ascending/descending/unsorted cycle, AllowUnsorted, SortDescendingFirst, SortBy,
/// Comparer, Ctrl/Shift multi-sort with priority badges, MultiSort and Sortable off, the keyboard) and OnSortChange,
/// the accessible names and the grid's ARIA structure, the footer aggregates and the header/footer templates, the empty
/// and loading states, Bordered/Striped/Hoverable, the sticky header and footer, ShowHeader, row numbers, RowClass /
/// RowStyle / the cell selectors / tooltips, wrapping and row heights, the row and cell events, Id/Class/Style/Styles/
/// Classes/HtmlAttributes, Visibility, the public css variables, the BitParams cascade, right-to-left (explicit and
/// inherited), Disabled, and the re-render and unmount. The other features are covered by the BitDataGrid*E2ETests
/// classes beside this one, each on a page of its own.
/// </summary>
[TestClass]
public class BitDataGridE2ETests : BitDataGridE2ETestBase
{
    private const string Url = "/e2e/data-grid";

    private static readonly string[] OriginalNames = ["Laptop", "Novel", "Jacket", "Lamp", "Drone", "Camera", "Cookbook", "Sneaker", "Mug", "Puzzle"];

    [TestMethod]
    public async Task Prerender_TheServerHtml_ContainsTheGridAndItsRows()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Url}");
        var html = await response.TextAsync();

        Assert.IsTrue(response.Ok);
        StringAssert.Contains(html, "id=\"grid-basic\"");
        StringAssert.Contains(html, "role=\"grid\"");
        StringAssert.Contains(html, "Laptop");
        StringAssert.Contains(html, ">Loading<", "The prerendered page carries the status of a page not yet interactive.");

        await OpenAsync(Url);
        await Expect(DataRows("grid-basic")).ToHaveCountAsync(10);
    }

    [TestMethod]
    public async Task Columns_PropertyFieldTemplateAndFormat_RenderTheirValues()
    {
        await OpenAsync(Url);

        await Expect(HeaderCells("grid-basic")).ToHaveTextAsync(["ID", "Name", "Category", "Supplier", "Stock", "Release Date", "Rating", "Price", "Value", "Discontinued"]);

        // Property and Field bind alike, Format applies to the value, and a Template renders its own markup.
        await Expect(DataRow("grid-basic", 0).Locator("> [role=gridcell]")).ToHaveCountAsync(10);
        var first = (await DataRow("grid-basic", 0).Locator("> [role=gridcell]").AllInnerTextsAsync()).Select(t => t.Trim()).ToArray();
        CollectionAssert.AreEqual(new[] { "1", "Laptop", "Electronics", "Acme", "005", "2023-05-01" }, first[..6]);
        StringAssert.Matches(first[6], new Regex(@"^4[.,]5$"));
        CollectionAssert.AreEqual(new[] { "1200", "6000", "False" }, first[7..]);
        await Expect(Cell("grid-basic", 0, 9).Locator(".value-cell")).ToHaveTextAsync("6000");
        await ExpectColumnAsync("grid-basic", 2, OriginalNames);

        // A bound value renders into the wrapper that ellipsises it; a templated one does not.
        await Expect(Cell("grid-basic", 0, 2).Locator(".bit-dtg-cell-text")).ToHaveCountAsync(1);
        await Expect(Cell("grid-basic", 0, 9).Locator(".bit-dtg-cell-text")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Columns_Align_PositionsTheCellContent()
    {
        await OpenAsync(Url);

        await Expect(Cell("grid-basic", 0, 1)).ToHaveClassAsync(HasClass("bit-dtg-end"));
        await Expect(Cell("grid-basic", 0, 1)).ToHaveCSSAsync("justify-content", "flex-end");
        await Expect(HeaderCell("grid-basic", 1)).ToHaveClassAsync(HasClass("bit-dtg-end"));

        await Expect(Cell("grid-basic", 0, 10)).ToHaveClassAsync(HasClass("bit-dtg-center"));
        await Expect(Cell("grid-basic", 0, 10)).ToHaveCSSAsync("justify-content", "center");

        await Expect(Cell("grid-basic", 0, 2)).Not.ToHaveClassAsync(new Regex("bit-dtg-(end|center|left|right)"));
        Assert.AreEqual("normal", await StyleOfAsync(Cell("grid-basic", 0, 2), "justify-content"));

        // The ID cell's text sits at the cell's end edge, the Name's at its start.
        var idCell = await BoxAsync(Cell("grid-basic", 0, 1));
        var idText = await BoxAsync(Cell("grid-basic", 0, 1).Locator(".bit-dtg-cell-text"));
        Assert.IsTrue(idText.X - idCell.X > idCell.Width / 2, "The end-aligned value is not at the end of its cell.");
    }

    [TestMethod]
    public async Task Sorting_HeaderClicks_CycleAscendingDescendingAndUnsorted()
    {
        await OpenAsync(Url);

        var header = HeaderCell("grid-basic", 2);
        var button = HeaderButton("grid-basic", "Name");

        await Expect(header).Not.ToHaveAttributeAsync("aria-sort", new Regex(".*"));

        await button.ClickAsync();
        await Expect(header).ToHaveAttributeAsync("aria-sort", "ascending");
        await Expect(header.Locator(".bit-dtg-sort-icon")).ToHaveTextAsync("▲");
        await ExpectColumnAsync("grid-basic", 2, "Camera", "Cookbook", "Drone", "Jacket", "Lamp", "Laptop", "Mug", "Novel", "Puzzle", "Sneaker");
        await Expect(Text("basic-sort-count")).ToHaveTextAsync("1");
        await Expect(Text("basic-sort-log")).ToHaveTextAsync("Name:Ascending:1");
        await Expect(LiveRegion("grid-basic")).ToContainTextAsync("Sorted by Name, ascending");

        await button.ClickAsync();
        await Expect(header).ToHaveAttributeAsync("aria-sort", "descending");
        await Expect(header.Locator(".bit-dtg-sort-icon")).ToHaveTextAsync("▼");
        await ExpectColumnAsync("grid-basic", 2, "Sneaker", "Puzzle", "Novel", "Mug", "Laptop", "Lamp", "Jacket", "Drone", "Cookbook", "Camera");
        await Expect(Text("basic-sort-count")).ToHaveTextAsync("2");
        await Expect(Text("basic-sort-log")).ToHaveTextAsync("Name:Descending:1");
        await Expect(LiveRegion("grid-basic")).ToContainTextAsync("Sorted by Name, descending");

        await button.ClickAsync();
        await Expect(header).Not.ToHaveAttributeAsync("aria-sort", new Regex(".*"));
        await Expect(header.Locator(".bit-dtg-sort-icon")).ToHaveCountAsync(0);
        await ExpectColumnAsync("grid-basic", 2, OriginalNames);
        await Expect(Text("basic-sort-count")).ToHaveTextAsync("3");
        await Expect(Text("basic-sort-log")).ToHaveTextAsync("");
        await Expect(LiveRegion("grid-basic")).ToContainTextAsync("Sorting by Name removed");
    }

    [TestMethod]
    public async Task Sorting_AllowUnsortedFalse_TheThirdClickStartsTheCycleOver()
    {
        await OpenAsync(Url);

        var header = HeaderCell("grid-basic", 7);
        var button = HeaderButton("grid-basic", "Rating");

        await button.ClickAsync();
        await Expect(header).ToHaveAttributeAsync("aria-sort", "ascending");
        await Expect(Cell("grid-basic", 0, 2)).ToHaveTextAsync("Mug");

        await button.ClickAsync();
        await Expect(header).ToHaveAttributeAsync("aria-sort", "descending");
        await Expect(Cell("grid-basic", 0, 2)).ToHaveTextAsync("Drone");

        await button.ClickAsync();
        await Expect(header).ToHaveAttributeAsync("aria-sort", "ascending");
        await Expect(Cell("grid-basic", 0, 2)).ToHaveTextAsync("Mug");
        await Expect(Text("basic-sort-count")).ToHaveTextAsync("3");
    }

    [TestMethod]
    public async Task Sorting_SortDescendingFirst_TheFirstClickSortsDescending()
    {
        await OpenAsync(Url);

        var header = HeaderCell("grid-basic", 8);
        var button = HeaderButton("grid-basic", "Price");

        await Expect(header).ToHaveClassAsync(HasClass("bit-dtg-sort-desc-first"));

        await button.ClickAsync();
        await Expect(header).ToHaveAttributeAsync("aria-sort", "descending");
        await Expect(Cells("grid-basic", 8)).ToHaveTextAsync(["1200", "650", "299", "110", "90", "45", "25", "20", "15", "10"]);

        await button.ClickAsync();
        await Expect(header).ToHaveAttributeAsync("aria-sort", "ascending");
        await Expect(Cell("grid-basic", 0, 2)).ToHaveTextAsync("Mug");

        await button.ClickAsync();
        await Expect(header).Not.ToHaveAttributeAsync("aria-sort", new Regex(".*"));
        await ExpectColumnAsync("grid-basic", 2, OriginalNames);
    }

    [TestMethod]
    public async Task Sorting_CtrlAndShiftClicks_AddSortLevelsWithPriorityBadges()
    {
        await OpenAsync(Url);

        await HeaderButton("grid-basic", "Category").ClickAsync();
        await HeaderButton("grid-basic", "Name").ClickAsync(new() { Modifiers = [KeyboardModifier.Control] });

        await Expect(HeaderCell("grid-basic", 3)).ToHaveAttributeAsync("aria-sort", "ascending");
        await Expect(HeaderCell("grid-basic", 2)).ToHaveAttributeAsync("aria-sort", "ascending");
        await ExpectColumnAsync("grid-basic", 2, "Camera", "Laptop", "Cookbook", "Novel", "Jacket", "Sneaker", "Lamp", "Mug", "Drone", "Puzzle");
        await Expect(HeaderCell("grid-basic", 3).Locator(".bit-dtg-sort-priority")).ToHaveTextAsync("1");
        await Expect(HeaderCell("grid-basic", 2).Locator(".bit-dtg-sort-priority")).ToHaveTextAsync("2");
        await Expect(HeaderCell("grid-basic", 2).Locator(".bit-dtg-visually-hidden")).ToHaveTextAsync("sort priority 2");
        await Expect(Text("basic-sort-log")).ToHaveTextAsync("Category:Ascending:1,Name:Ascending:2");

        // Shift adds to the sort as Ctrl does: the second click on Name turns it descending and keeps Category.
        await HeaderButton("grid-basic", "Name").ClickAsync(new() { Modifiers = [KeyboardModifier.Shift] });
        await Expect(HeaderCell("grid-basic", 2)).ToHaveAttributeAsync("aria-sort", "descending");
        await ExpectColumnAsync("grid-basic", 2, "Laptop", "Camera", "Novel", "Cookbook", "Sneaker", "Jacket", "Mug", "Lamp", "Puzzle", "Drone");
        await Expect(Text("basic-sort-log")).ToHaveTextAsync("Category:Ascending:1,Name:Descending:2");

        // A plain click replaces the whole sort, and the badges go with the second level.
        await HeaderButton("grid-basic", "Supplier").ClickAsync();
        await Expect(HeaderCell("grid-basic", 4)).ToHaveAttributeAsync("aria-sort", "ascending");
        await Expect(HeaderCell("grid-basic", 3)).Not.ToHaveAttributeAsync("aria-sort", new Regex(".*"));
        await Expect(HeaderCell("grid-basic", 2)).Not.ToHaveAttributeAsync("aria-sort", new Regex(".*"));
        await Expect(HeaderRow("grid-basic").Locator(".bit-dtg-sort-priority")).ToHaveCountAsync(0);
        await Expect(Text("basic-sort-log")).ToHaveTextAsync("Supplier:Ascending:1");
        await Expect(Text("basic-sort-count")).ToHaveTextAsync("4");
    }

    [TestMethod]
    public async Task Sorting_TemplateColumnWithSortBy_SortsByItsKey()
    {
        await OpenAsync(Url);

        await HeaderButton("grid-basic", "Value").ClickAsync();

        await Expect(HeaderCell("grid-basic", 9)).ToHaveAttributeAsync("aria-sort", "ascending");
        await ExpectColumnAsync("grid-basic", 2, "Jacket", "Lamp", "Puzzle", "Cookbook", "Novel", "Mug", "Drone", "Sneaker", "Laptop", "Camera");
        await Expect(Text("basic-sort-log")).ToHaveTextAsync("Value:Ascending:1");
    }

    [TestMethod]
    public async Task Sorting_Comparer_OrdersTheColumnItsOwnWay()
    {
        await OpenAsync(Url);

        await HeaderButton("grid-basic", "Stock").ClickAsync();

        // The comparer puts the sold-out row last, where the plain ordering would put it first.
        await Expect(Cells("grid-basic", 5)).ToHaveTextAsync(["005", "008", "012", "030", "045", "060", "070", "120", "200", "000"]);
        await Expect(Cell("grid-basic", 9, 2)).ToHaveTextAsync("Jacket");
    }

    [TestMethod]
    public async Task Sorting_AColumnThatIsNotSortable_RendersNoButtonAndIgnoresClicks()
    {
        await OpenAsync(Url);

        var header = HeaderCell("grid-basic", 10);
        await Expect(header.Locator("button")).ToHaveCountAsync(0);
        await Expect(header.Locator("span.bit-dtg-htext")).ToHaveTextAsync("Discontinued");
        await Expect(header).Not.ToHaveClassAsync(HasClass("bit-dtg-sortable"));

        await header.ClickAsync();
        await Expect(header).Not.ToHaveAttributeAsync("aria-sort", new Regex(".*"));
        await Expect(Text("basic-sort-count")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Sorting_MultiSortOff_AModifierClickReplacesTheSort()
    {
        await OpenAsync(Url);

        await HeaderButton("grid-multisort-off", "Category").ClickAsync();
        await HeaderButton("grid-multisort-off", "Name").ClickAsync(new() { Modifiers = [KeyboardModifier.Control] });

        await Expect(HeaderCell("grid-multisort-off", 1)).ToHaveAttributeAsync("aria-sort", "ascending");
        await Expect(HeaderCell("grid-multisort-off", 2)).Not.ToHaveAttributeAsync("aria-sort", new Regex(".*"));
        await Expect(HeaderRow("grid-multisort-off").Locator(".bit-dtg-sort-priority")).ToHaveCountAsync(0);
        await ExpectColumnAsync("grid-multisort-off", 1, "Camera", "Cookbook", "Drone", "Jacket", "Lamp", "Laptop", "Mug", "Novel", "Puzzle", "Sneaker");
    }

    [TestMethod]
    public async Task Sorting_SortableOffOnTheGrid_OnlyAColumnThatOptsInSorts()
    {
        await OpenAsync(Url);

        await Expect(HeaderCell("grid-unsortable", 1).Locator("button")).ToHaveCountAsync(0);
        await HeaderCell("grid-unsortable", 1).ClickAsync();
        await Expect(HeaderCell("grid-unsortable", 1)).Not.ToHaveAttributeAsync("aria-sort", new Regex(".*"));

        await HeaderButton("grid-unsortable", "Category").ClickAsync();
        await Expect(HeaderCell("grid-unsortable", 2)).ToHaveAttributeAsync("aria-sort", "ascending");
        await ExpectColumnAsync("grid-unsortable", 1, "Laptop", "Camera", "Novel", "Cookbook", "Jacket", "Sneaker", "Lamp", "Mug", "Drone", "Puzzle");
    }

    [TestMethod]
    public async Task Sorting_EnterAndSpaceOnAHeader_SortItFromTheKeyboard()
    {
        await OpenAsync(Url);

        var button = HeaderButton("grid-basic", "Name");
        await button.FocusAsync();

        await Page.Keyboard.PressAsync("Enter");
        await Expect(HeaderCell("grid-basic", 2)).ToHaveAttributeAsync("aria-sort", "ascending");

        await Page.Keyboard.PressAsync(" ");
        await Expect(HeaderCell("grid-basic", 2)).ToHaveAttributeAsync("aria-sort", "descending");
        await ExpectFocusedAsync(button);

        // Space on a header button sorts and never scrolls the page.
        Assert.AreEqual(0, await Page.EvaluateAsync<double>("() => document.scrollingElement.scrollTop"));
    }

    [TestMethod]
    public async Task Accessibility_TheGrid_CarriesItsRoleNameCountsAndIndexes()
    {
        await OpenAsync(Url);

        var table = Table("grid-basic");
        await Expect(table).ToHaveAttributeAsync("role", "grid");
        await Expect(table).ToHaveAttributeAsync("aria-label", "Products");
        await Expect(table).ToHaveAccessibleNameAsync("Products");
        await Expect(table).ToHaveAttributeAsync("aria-rowcount", "11");
        await Expect(table).ToHaveAttributeAsync("aria-colcount", "10");
        await Expect(table).Not.ToHaveAttributeAsync("aria-multiselectable", new Regex(".*"));
        await Expect(table).Not.ToHaveAttributeAsync("aria-busy", new Regex(".*"));

        await Expect(HeaderRow("grid-basic")).ToHaveAttributeAsync("role", "row");
        await Expect(HeaderRow("grid-basic")).ToHaveAttributeAsync("aria-rowindex", "1");
        await Expect(Page.Locator("#grid-basic .bit-dtg-header")).ToHaveAttributeAsync("role", "rowgroup");
        await Expect(Page.Locator("#grid-basic .bit-dtg-body")).ToHaveAttributeAsync("role", "rowgroup");

        var colIndexes = await HeaderCells("grid-basic").EvaluateAllAsync<string[]>("cells => cells.map(c => c.getAttribute('aria-colindex'))");
        CollectionAssert.AreEqual(Enumerable.Range(1, 10).Select(i => i.ToString()).ToArray(), colIndexes);

        var rowIndexes = await DataRows("grid-basic").EvaluateAllAsync<string[]>("rows => rows.map(r => r.getAttribute('aria-rowindex'))");
        CollectionAssert.AreEqual(Enumerable.Range(2, 10).Select(i => i.ToString()).ToArray(), rowIndexes);

        await Expect(Cell("grid-basic", 0, 2)).ToHaveAttributeAsync("role", "gridcell");
        // Without a SelectionMode the rows say nothing about a selection, and a click selects nothing.
        await Expect(DataRow("grid-basic", 0)).Not.ToHaveAttributeAsync("aria-selected", new Regex(".*"));
        await Cell("grid-basic", 0, 2).ClickAsync();
        await Expect(DataRow("grid-basic", 0)).Not.ToHaveAttributeAsync("aria-selected", new Regex(".*"));
        await Expect(DataRow("grid-basic", 0)).Not.ToHaveClassAsync(HasClass("bit-dtg-selected"));
        await Expect(Cell("grid-basic", 0, 2)).Not.ToHaveAttributeAsync("tabindex", new Regex(".*"));
        await Expect(LiveRegion("grid-basic")).ToHaveAttributeAsync("aria-live", "polite");
    }

    [TestMethod]
    public async Task Accessibility_AriaLabelledByAndDescribedBy_NameAndDescribeTheGrid()
    {
        await OpenAsync(Url);

        var table = Table("grid-labelledby");
        await Expect(table).ToHaveAttributeAsync("aria-labelledby", "aria-heading");
        await Expect(table).Not.ToHaveAttributeAsync("aria-label", new Regex(".*"));
        await Expect(table).ToHaveAttributeAsync("aria-describedby", "aria-description");
        await Expect(table).ToHaveAccessibleNameAsync("Inventory heading");
        await Expect(table).ToHaveAccessibleDescriptionAsync("Use the arrow keys to move between cells.");

        await Expect(Table("grid-default-label")).ToHaveAttributeAsync("aria-label", "Data grid");
    }

    [TestMethod]
    public async Task Aggregates_TheFooter_ShowsEveryAggregateAndTheTemplates()
    {
        await OpenAsync(Url);

        var footer = FooterRow("grid-aggregates");
        await Expect(footer).ToHaveAttributeAsync("role", "row");
        await Expect(footer).ToHaveAttributeAsync("aria-rowindex", "12");
        await Expect(footer.Locator("> [role=gridcell]")).ToHaveTextAsync(["count 10", "", "Σ 550", "avg 246", "max 1200", "min 2020-06-30", "Suppliers: 4 (Custom)"]);
        await Expect(footer.Locator(".ftr-tpl")).ToBeVisibleAsync();

        // A HeaderTemplate replaces the title, and the native tooltip that would repeat it.
        await Expect(HeaderCell("grid-aggregates", 2).Locator(".hdr-tpl")).ToHaveTextAsync("Product name");
        await Expect(HeaderCell("grid-aggregates", 2).Locator(".bit-dtg-htext")).Not.ToHaveAttributeAsync("title", new Regex(".*"));
        await Expect(HeaderCell("grid-aggregates", 3).Locator(".bit-dtg-htext")).ToHaveAttributeAsync("title", "Stock");
    }

    [TestMethod]
    public async Task Sticky_ScrollingTheViewport_KeepsTheHeaderAndTheFooterInView()
    {
        await OpenAsync(Url);

        var viewport = Viewport("grid-sticky");
        await Expect(Page.Locator("#grid-sticky .bit-dtg-header")).ToHaveCSSAsync("position", "sticky");
        await Expect(Page.Locator("#grid-sticky .bit-dtg-footer")).ToHaveCSSAsync("position", "sticky");

        await viewport.EvaluateAsync("el => el.scrollTop = 500");
        await NextFramesAsync();

        var view = await BoxAsync(viewport);
        var header = await BoxAsync(HeaderRow("grid-sticky"));
        var footer = await BoxAsync(FooterRow("grid-sticky"));
        var clientHeight = await viewport.EvaluateAsync<double>("el => el.clientHeight");
        var clientTop = await viewport.EvaluateAsync<double>("el => el.clientTop");

        Assert.AreEqual(view.Y + clientTop, header.Y, 1.5, "The header scrolled away with the rows.");
        Assert.AreEqual(view.Y + clientTop + clientHeight, footer.Y + footer.Height, 1.5, "The footer is not pinned to the bottom of the viewport.");

        // The first rows went under the header: the row beneath it is far down the list.
        var firstRow = await BoxAsync(DataRow("grid-sticky", 0));
        Assert.IsTrue(firstRow.Y + firstRow.Height < header.Y + 1, "The first row is still in view after scrolling.");
        await Expect(FooterRow("grid-sticky").Locator("> [aria-colindex='1']")).ToHaveTextAsync("count 40");
    }

    [TestMethod]
    public async Task Empty_NoItems_ShowTheDefaultMessage()
    {
        await OpenAsync(Url);

        var message = MessageRow("grid-empty");
        await Expect(message).ToHaveAttributeAsync("role", "row");
        await Expect(message.Locator(".bit-dtg-empty")).ToHaveTextAsync("No records to display.");
        await Expect(message.Locator(".bit-dtg-empty")).ToHaveAttributeAsync("aria-colspan", "2");
        await Expect(DataRows("grid-empty")).ToHaveCountAsync(0);
        await Expect(Table("grid-empty")).ToHaveAttributeAsync("aria-rowcount", "1");
    }

    [TestMethod]
    public async Task Empty_TheEmptyTemplate_RendersAndItsButtonLoadsTheData()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#grid-empty-template .custom-empty")).ToContainTextAsync("Nothing here yet");
        await Expect(Page.Locator("#grid-empty-template .bit-dtg-empty")).Not.ToContainTextAsync("No records");

        await Page.Locator("#btn-empty-load").ClickAsync();

        await Expect(DataRows("grid-empty-template")).ToHaveCountAsync(10);
        await Expect(Page.Locator("#grid-empty-template .custom-empty")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Loading_TheLoadingParameter_ReplacesTheRowsWithASpinnerAndMarksTheGridBusy()
    {
        await OpenAsync(Url);

        await Expect(DataRows("grid-loading")).ToHaveCountAsync(10);

        await Page.Locator("#btn-loading").ClickAsync();
        await Expect(Text("loading-state")).ToHaveTextAsync("loading");

        var loading = Page.Locator("#grid-loading .bit-dtg-loading");
        await Expect(loading).ToContainTextAsync("Loading…");
        await Expect(loading.Locator(".bit-dtg-spinner")).ToBeVisibleAsync();
        Assert.AreNotEqual("none", await StyleOfAsync(loading.Locator(".bit-dtg-spinner"), "animation-name"));
        await Expect(DataRows("grid-loading")).ToHaveCountAsync(0);
        await Expect(Table("grid-loading")).ToHaveAttributeAsync("aria-busy", "true");

        await Expect(Page.Locator("#grid-loading-template .custom-loading")).ToHaveTextAsync("Fetching the latest products");
        await Expect(Page.Locator("#grid-loading-template .bit-dtg-spinner")).ToHaveCountAsync(0);

        await Page.Locator("#btn-loading").ClickAsync();
        await Expect(DataRows("grid-loading")).ToHaveCountAsync(10);
        await Expect(Table("grid-loading")).Not.ToHaveAttributeAsync("aria-busy", new Regex(".*"));
        await Expect(Page.Locator("#grid-loading .bit-dtg-loading")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Styling_Bordered_DrawsTheOuterBorderAndTheColumnLines()
    {
        await OpenAsync(Url);

        var grid = Grid("grid-styling");
        await Expect(grid).ToHaveClassAsync(HasClass("bit-dtg-bordered"));
        await Expect(grid).ToHaveCSSAsync("border-top-width", "1px");
        await Expect(grid).ToHaveCSSAsync("border-top-color", "rgb(0, 128, 0)");
        await Expect(Cell("grid-styling", 0, 1)).ToHaveCSSAsync("border-right-width", "1px");

        await Page.Locator("#btn-bordered").ClickAsync();

        await Expect(grid).Not.ToHaveClassAsync(HasClass("bit-dtg-bordered"));
        await Expect(grid).ToHaveCSSAsync("border-top-width", "0px");
        await Expect(Cell("grid-styling", 0, 1)).ToHaveCSSAsync("border-right-width", "0px");
        // The lines between rows stay.
        await Expect(DataRow("grid-styling", 0)).ToHaveCSSAsync("border-bottom-width", "1px");
    }

    [TestMethod]
    public async Task Styling_Striped_ShadesEveryOtherRow()
    {
        await OpenAsync(Url);

        await Expect(Grid("grid-styling")).ToHaveClassAsync(HasClass("bit-dtg-striped"));
        await Expect(Cell("grid-styling", 0, 1)).ToHaveCSSAsync("background-color", "rgb(255, 255, 255)");
        await Expect(Cell("grid-styling", 1, 1)).ToHaveCSSAsync("background-color", "rgb(0, 0, 255)");
        await Expect(Cell("grid-styling", 3, 1)).ToHaveCSSAsync("background-color", "rgb(0, 0, 255)");

        await Page.Locator("#btn-striped").ClickAsync();

        await Expect(Grid("grid-styling")).Not.ToHaveClassAsync(HasClass("bit-dtg-striped"));
        await Expect(Cell("grid-styling", 1, 1)).ToHaveCSSAsync("background-color", "rgb(255, 255, 255)");
    }

    [TestMethod]
    public async Task Styling_Hoverable_HighlightsTheRowUnderThePointer()
    {
        await OpenAsync(Url);

        await Expect(Grid("grid-styling")).ToHaveClassAsync(HasClass("bit-dtg-hoverable"));
        await Cell("grid-styling", 0, 2).HoverAsync();
        await Expect(Cell("grid-styling", 0, 1)).ToHaveCSSAsync("background-color", "rgb(255, 0, 0)");
        await Expect(Cell("grid-styling", 1, 1)).ToHaveCSSAsync("background-color", "rgb(0, 0, 255)");

        await Page.Locator("#btn-hoverable").ClickAsync();
        await Expect(Grid("grid-styling")).Not.ToHaveClassAsync(HasClass("bit-dtg-hoverable"));

        await Cell("grid-styling", 0, 2).HoverAsync();
        await Expect(Cell("grid-styling", 0, 1)).ToHaveCSSAsync("background-color", "rgb(255, 255, 255)");
    }

    [TestMethod]
    public async Task ShowHeader_Off_RendersNoHeaderRows()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#grid-header-off .bit-dtg-header")).ToHaveCountAsync(0);
        await Expect(DataRows("grid-header-off")).ToHaveCountAsync(3);
        await Expect(Cell("grid-header-off", 0, 1)).ToHaveTextAsync("Laptop");
    }

    [TestMethod]
    public async Task RowNumbers_ShowRowNumbers_NumbersTheRowsAcrossPages()
    {
        await OpenAsync(Url);

        var header = HeaderCell("grid-row-numbers", 1);
        await Expect(header).ToHaveAttributeAsync("aria-label", "Row number");
        await Expect(header.Locator("[aria-hidden=true]")).ToHaveTextAsync("#");
        await ExpectColumnAsync("grid-row-numbers", 1, "1", "2", "3", "4");
        await ExpectColumnAsync("grid-row-numbers", 2, "1", "2", "3", "4");

        await Pagers("grid-row-numbers").Locator("button[aria-label='Next page']").ClickAsync();

        await ExpectColumnAsync("grid-row-numbers", 1, "5", "6", "7", "8");
        await Expect(Cells("grid-row-numbers", 1).First).ToHaveClassAsync(HasClass("bit-dtg-cell-rownumber"));
    }

    [TestMethod]
    public async Task RowStyling_RowClassRowStyleAndTheCellSelectors_StyleFromTheData()
    {
        await OpenAsync(Url);

        // Jacket (row 3) is sold out; Laptop and Camera cost more than 500.
        await Expect(DataRow("grid-row-styling", 2)).ToHaveClassAsync(HasClass("out-of-stock"));
        await Expect(DataRow("grid-row-styling", 1)).Not.ToHaveClassAsync(HasClass("out-of-stock"));
        await Expect(DataRow("grid-row-styling", 0)).ToHaveCSSAsync("font-weight", "700");
        await Expect(DataRow("grid-row-styling", 1)).ToHaveCSSAsync("font-weight", "400");

        await Expect(HeaderCell("grid-row-styling", 1)).ToHaveClassAsync(HasClass("hdr-x"));
        await Expect(Cell("grid-row-styling", 0, 1)).ToHaveClassAsync(HasClass("cell-x"));

        await Expect(Cell("grid-row-styling", 0, 4)).ToHaveClassAsync(HasClass("low-stock"));
        await Expect(Cell("grid-row-styling", 0, 4)).ToHaveCSSAsync("color", "rgb(200, 100, 0)");
        await Expect(Cell("grid-row-styling", 1, 4)).Not.ToHaveClassAsync(HasClass("low-stock"));
        Assert.AreNotEqual("rgb(200, 100, 0)", await StyleOfAsync(Cell("grid-row-styling", 1, 4), "color"));
    }

    [TestMethod]
    public async Task Tooltips_ShowCellTooltips_TitleTheCellsWithTheirText()
    {
        await OpenAsync(Url);

        await Expect(Cell("grid-row-styling", 0, 2)).ToHaveAttributeAsync("title", "Laptop");
        await Expect(Cell("grid-row-styling", 0, 4)).ToHaveAttributeAsync("title", "5");
        // A column opts out, and a templated cell owns its markup.
        await Expect(Cell("grid-row-styling", 0, 3)).Not.ToHaveAttributeAsync("title", new Regex(".*"));
        await Expect(Cell("grid-row-styling", 0, 5)).Not.ToHaveAttributeAsync("title", new Regex(".*"));
        await Expect(Cell("grid-row-styling", 0, 5)).ToHaveTextAsync("Laptop!");

        // A grid without the switch titles nothing.
        await Expect(Cell("grid-basic", 0, 2)).Not.ToHaveAttributeAsync("title", new Regex(".*"));
    }

    [TestMethod]
    public async Task Wrap_WrapCellText_GrowsTheRowAndAColumnCanOptOut()
    {
        await OpenAsync(Url);

        var row = DataRow("grid-wrap", 0);
        await Expect(Cell("grid-wrap", 0, 2)).ToHaveCSSAsync("white-space", "nowrap");
        var before = await BoxAsync(row);
        Assert.IsTrue(before.Height < 46, $"An unwrapped row is {before.Height}px tall.");

        await Page.Locator("#btn-wrap").ClickAsync();
        await Expect(Text("wrap-state")).ToHaveTextAsync("on");

        await Expect(Cell("grid-wrap", 0, 2)).ToHaveClassAsync(HasClass("bit-dtg-cell-wrap"));
        await Expect(Cell("grid-wrap", 0, 2)).ToHaveCSSAsync("white-space", "normal");
        await Expect(HeaderCell("grid-wrap", 2)).ToHaveClassAsync(HasClass("bit-dtg-hcell-wrap"));
        await Expect(Cell("grid-wrap", 0, 3)).Not.ToHaveClassAsync(HasClass("bit-dtg-cell-wrap"));
        await Expect(Cell("grid-wrap", 0, 3)).ToHaveCSSAsync("white-space", "nowrap");

        var after = await BoxAsync(row);
        Assert.IsTrue(after.Height > before.Height + 10, $"The wrapped row did not grow ({before.Height}px -> {after.Height}px).");
    }

    [TestMethod]
    public async Task RowHeight_RowHeightAndRowHeightSelector_SetTheRowsMinimumHeight()
    {
        await OpenAsync(Url);

        await Expect(DataRow("grid-wrap", 1)).ToHaveCSSAsync("min-height", "40px");
        await Expect(DataRow("grid-row-height", 0)).ToHaveCSSAsync("min-height", "80px");
        await Expect(DataRow("grid-row-height", 1)).ToHaveCSSAsync("min-height", "44px");
        await Expect(DataRow("grid-basic", 0)).ToHaveCSSAsync("min-height", "36px");

        var tall = await BoxAsync(DataRow("grid-row-height", 0));
        var short_ = await BoxAsync(DataRow("grid-row-height", 1));
        Assert.IsTrue(tall.Height >= 80, $"The first row is {tall.Height}px tall.");
        Assert.IsTrue(short_.Height >= 44 && short_.Height < 60, $"The second row is {short_.Height}px tall.");
    }

    [TestMethod]
    public async Task CellEvents_ClickDoubleClickAndContextMenu_ReportTheCellAndTheRow()
    {
        await OpenAsync(Url);

        await Cell("grid-cell-events", 0, 2).ClickAsync();
        await Expect(Text("cell-click-count")).ToHaveTextAsync("1");
        await Expect(Text("cell-click-arg")).ToHaveTextAsync("Laptop|Name|Product|Laptop|button=0|title=Product");
        await Expect(Text("row-click-count")).ToHaveTextAsync("1");
        await Expect(Text("row-click-arg")).ToHaveTextAsync("Laptop");

        await Cell("grid-cell-events", 1, 3).DblClickAsync();
        await Expect(Text("cell-dblclick-count")).ToHaveTextAsync("1");
        await Expect(Text("cell-dblclick-arg")).ToHaveTextAsync("Novel|Stock|Stock|120|button=0|title=");
        await Expect(Text("row-dblclick-count")).ToHaveTextAsync("1");
        await Expect(Text("row-dblclick-arg")).ToHaveTextAsync("Novel");
        // A double-click is two clicks first.
        await Expect(Text("cell-click-count")).ToHaveTextAsync("3");

        await Page.EvaluateAsync("() => window.addEventListener('contextmenu', e => window.__dtgContextMenuPrevented = e.defaultPrevented)");
        await Cell("grid-cell-events", 2, 1).ClickAsync(new() { Button = MouseButton.Right });
        await Expect(Text("cell-contextmenu-count")).ToHaveTextAsync("1");
        await Expect(Text("cell-contextmenu-arg")).ToHaveTextAsync("Jacket|Id|ID|3|button=2|title=ID");
        Assert.IsTrue(await Page.EvaluateAsync<bool>("() => window.__dtgContextMenuPrevented === true"), "The browser's own context menu was not suppressed.");
    }

    [TestMethod]
    public async Task StyleAndClass_TheRootAndEveryPart_TakeTheirClassesAndStyles()
    {
        await OpenAsync(Url);

        var grid = Grid("grid-styled");
        await Expect(grid).ToHaveClassAsync(HasClass("bit-dtg"));
        await Expect(grid).ToHaveClassAsync(HasClass("custom-root"));
        await Expect(grid).ToHaveClassAsync(HasClass("c-root"));
        await Expect(grid).ToHaveCSSAsync("margin-top", "7px");
        await Expect(grid).ToHaveCSSAsync("padding-top", "3px");
        await Expect(grid).ToHaveAttributeAsync("data-e2e", "styled-root");

        await Expect(Toolbar("grid-styled")).ToHaveClassAsync(HasClass("c-toolbar"));
        await Expect(Toolbar("grid-styled")).ToHaveCSSAsync("padding-left", "11px");
        await Expect(Toolbar("grid-styled").Locator(".custom-toolbar")).ToHaveTextAsync("My toolbar");
        await Expect(Viewport("grid-styled")).ToHaveClassAsync(HasClass("c-viewport"));
        await Expect(Viewport("grid-styled")).ToHaveCSSAsync("outline-color", "rgb(1, 2, 3)");
        await Expect(HeaderRow("grid-styled")).ToHaveClassAsync(HasClass("c-header-row"));
        await Expect(HeaderRow("grid-styled")).ToHaveCSSAsync("letter-spacing", "2px");
        await Expect(HeaderCell("grid-styled", 2)).ToHaveClassAsync(HasClass("c-header-cell"));
        await Expect(HeaderCell("grid-styled", 2)).ToHaveCSSAsync("text-transform", "uppercase");
        await Expect(FilterRow("grid-styled")).ToHaveClassAsync(HasClass("c-filter-row"));
        await Expect(FilterRow("grid-styled")).ToHaveCSSAsync("opacity", "0.9");
        await Expect(DataRow("grid-styled", 1)).ToHaveClassAsync(HasClass("c-row"));
        await Expect(DataRow("grid-styled", 1)).ToHaveCSSAsync("font-style", "italic");
        await Expect(DataRow("grid-styled", 0)).ToHaveClassAsync(HasClass("c-selected-row"));
        await Expect(DataRow("grid-styled", 0)).ToHaveCSSAsync("text-decoration-line", "underline");
        await Expect(DataRow("grid-styled", 1)).Not.ToHaveClassAsync(HasClass("c-selected-row"));
        await Expect(Cell("grid-styled", 0, 2)).ToHaveClassAsync(HasClass("c-cell"));
        await Expect(Cell("grid-styled", 0, 2)).ToHaveCSSAsync("padding-left", "13px");
        await Expect(FooterRow("grid-styled")).ToHaveClassAsync(HasClass("c-footer-row"));
        await Expect(FooterRow("grid-styled")).ToHaveCSSAsync("font-variant-caps", "small-caps");
        await Expect(Pagers("grid-styled")).ToHaveClassAsync(HasClass("c-pager"));
        await Expect(Pagers("grid-styled")).ToHaveCSSAsync("justify-content", "center");

        await Expect(Page.Locator("#grid-styled-empty .bit-dtg-empty")).ToHaveClassAsync(HasClass("c-empty"));
        await Expect(Page.Locator("#grid-styled-empty .bit-dtg-empty")).ToHaveCSSAsync("color", "rgb(9, 8, 7)");
        await Expect(Page.Locator("#grid-styled-loading .bit-dtg-loading")).ToHaveClassAsync(HasClass("c-loading"));
        await Expect(Page.Locator("#grid-styled-loading .bit-dtg-loading")).ToHaveCSSAsync("color", "rgb(7, 8, 9)");
    }

    [TestMethod]
    public async Task Visibility_HiddenAndCollapsed_HideTheGridTheirOwnWay()
    {
        await OpenAsync(Url);

        var grid = Grid("grid-visibility");
        await Expect(grid).ToBeVisibleAsync();

        await Page.Locator("#btn-vis-hidden").ClickAsync();
        await Expect(grid).ToHaveCSSAsync("visibility", "hidden");
        await Expect(grid).Not.ToBeVisibleAsync();
        Assert.IsTrue((await BoxAsync(grid)).Height > 0, "A hidden grid still takes its room.");

        await Page.Locator("#btn-vis-collapsed").ClickAsync();
        await Expect(grid).ToHaveCSSAsync("display", "none");
        Assert.IsNull(await grid.BoundingBoxAsync());

        await Page.Locator("#btn-vis-visible").ClickAsync();
        await Expect(grid).ToBeVisibleAsync();
        await Expect(grid).ToHaveCSSAsync("display", "flex");
    }

    [TestMethod]
    public async Task CssVariables_SetOnAnAncestor_RestyleTheGrid()
    {
        await OpenAsync(Url);

        const string id = "grid-css-vars";
        await Expect(HeaderCell(id, 2)).ToHaveCSSAsync("background-color", "rgb(10, 20, 30)");
        await Expect(HeaderCell(id, 2)).ToHaveCSSAsync("color", "rgb(250, 240, 230)");
        await Expect(HeaderCell(id, 2)).ToHaveCSSAsync("font-weight", "300");
        await Expect(Grid(id)).ToHaveCSSAsync("font-size", "19px");
        await Expect(Grid(id)).ToHaveCSSAsync("color", "rgb(40, 50, 60)");
        await Expect(Grid(id)).ToHaveCSSAsync("border-top-left-radius", "0px");
        await Expect(Grid(id)).ToHaveCSSAsync("line-height", "38px");
        await Expect(Grid(id)).ToHaveCSSAsync("font-family", "monospace");
        await Expect(Cell(id, 1, 3)).ToHaveCSSAsync("padding", "3px 9px");
        await Expect(Cell(id, 0, 3)).ToHaveCSSAsync("background-image", new Regex(@"rgba\(0, 200, 0, 0\.5\)"));
        await Expect(Cell(id, 1, 3)).ToHaveCSSAsync("background-image", new Regex("none|rgba\\(0, 0, 0, 0\\)"));
        await Expect(FooterRow(id).Locator("> .bit-dtg-cell").Last).ToHaveCSSAsync("background-color", "rgb(70, 80, 90)");
        await Expect(Cell(id, 0, 1).Locator("input[type=checkbox]")).ToHaveCSSAsync("accent-color", "rgb(255, 0, 255)");

        // One set on the Style of a grid restyles that grid.
        await Expect(HeaderCell("grid-css-vars-style", 1)).ToHaveCSSAsync("background-color", "rgb(1, 99, 2)");
        Assert.AreNotEqual("rgb(1, 99, 2)", await StyleOfAsync(HeaderCell("grid-basic", 1), "background-color"));
    }

    [TestMethod]
    public async Task Params_TheBitParamsCascade_SetsDefaultsAndAGridsOwnValueWins()
    {
        await OpenAsync(Url);

        var a = "grid-params-a";
        await Expect(Grid(a)).Not.ToHaveClassAsync(HasClass("bit-dtg-striped"));
        await Expect(HeaderRow(a).Locator(".bit-dtg-hcell-rownumber")).ToHaveCountAsync(1);
        await Expect(FilterRow(a)).ToHaveCountAsync(1);
        await Expect(DataRows(a)).ToHaveCountAsync(5);
        await Expect(Pagers(a).Locator("select.bit-dtg-page-size option")).ToHaveTextAsync(["5 / page", "10 / page"]);

        var b = "grid-params-b";
        await Expect(Grid(b)).ToHaveClassAsync(HasClass("bit-dtg-striped"));
        await Expect(DataRows(b)).ToHaveCountAsync(2);
        await Expect(HeaderRow(b).Locator(".bit-dtg-hcell-rownumber")).ToHaveCountAsync(1);
        await Expect(FilterRow(b)).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task Rtl_DirRtl_MirrorsTheLayout()
    {
        await OpenAsync(Url);

        var grid = Grid("grid-rtl");
        await Expect(grid).ToHaveAttributeAsync("dir", "rtl");
        await Expect(grid).ToHaveClassAsync(HasClass("bit-rtl"));
        await Expect(grid).ToHaveClassAsync(HasClass("bit-dtg-rtl"));

        var id = await BoxAsync(HeaderCell("grid-rtl", 1));
        var name = await BoxAsync(HeaderCell("grid-rtl", 2));
        Assert.IsTrue(id.X > name.X, "The first column does not start at the right in a right-to-left grid.");

        var view = await BoxAsync(Viewport("grid-rtl"));
        Assert.AreEqual(view.X + view.Width, id.X + id.Width, 2, "The first column is not against the right edge.");

        // The frozen column is pinned to the inline start, which is the right.
        await Expect(Cell("grid-rtl", 0, 1)).ToHaveCSSAsync("right", "0px");

        // The resize handle sits on the inline end of its header, which is its left edge.
        var resizer = await BoxAsync(HeaderCell("grid-rtl", 2).Locator(".bit-dtg-resizer"));
        Assert.AreEqual(name.X, resizer.X, 2);

        // An ancestor's direction is followed without the class of an explicit one.
        var inherited = Grid("grid-rtl-inherited");
        await Expect(inherited).Not.ToHaveClassAsync(HasClass("bit-rtl"));
        await Expect(inherited).Not.ToHaveAttributeAsync("dir", new Regex(".*"));
        Assert.IsTrue((await BoxAsync(HeaderCell("grid-rtl-inherited", 1))).X > (await BoxAsync(HeaderCell("grid-rtl-inherited", 2))).X);

        // And Ltr reads left to right.
        await Expect(Grid("grid-ltr")).ToHaveAttributeAsync("dir", "ltr");
        Assert.IsTrue((await BoxAsync(HeaderCell("grid-ltr", 1))).X < (await BoxAsync(HeaderCell("grid-ltr", 2))).X);
    }

    [TestMethod]
    [DataRow("grid-rtl")]
    [DataRow("grid-rtl-inherited")]
    public async Task Rtl_TheArrowKeys_FollowTheReadingDirection(string gridId)
    {
        await OpenAsync(Url);

        await Cell(gridId, 0, 1).ClickAsync();
        await ExpectFocusedAsync(Cell(gridId, 0, 1));

        await Page.Keyboard.PressAsync("ArrowLeft");
        await ExpectFocusedAsync(Cell(gridId, 0, 2));

        await Page.Keyboard.PressAsync("ArrowRight");
        await ExpectFocusedAsync(Cell(gridId, 0, 1));
    }

    [TestMethod]
    public async Task Disabled_TheGrid_SaysSoInItsMarkup()
    {
        await OpenAsync(Url);

        const string id = "grid-disabled";
        await Expect(Grid(id)).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Grid(id)).Not.ToHaveAttributeAsync("data-bit-dtg-select-all", new Regex(".*"));
        await Expect(Table(id)).ToHaveAttributeAsync("aria-disabled", "true");

        // The sort, the group toggles and the resize handles are gone or disabled.
        await Expect(HeaderRow(id).Locator("button.bit-dtg-htext")).ToHaveCountAsync(0);
        await Expect(HeaderRow(id).Locator(".bit-dtg-resizer")).ToHaveCountAsync(0);
        var groupButtons = HeaderRow(id).Locator(".bit-dtg-group-btn");
        await Expect(groupButtons.First).ToBeDisabledAsync();
        await Expect(groupButtons.First).ToHaveAttributeAsync("aria-disabled", "true");

        // Every native control is disabled both ways.
        var controls = Page.Locator($"#{id} input, #{id} select, #{id} > .bit-dtg-pager button");
        var count = await controls.CountAsync();
        Assert.IsTrue(count > 8, $"Only {count} controls found.");
        for (var i = 0; i < count; i++)
        {
            await Expect(controls.Nth(i)).ToBeDisabledAsync();
            await Expect(controls.Nth(i)).ToHaveAttributeAsync("aria-disabled", "true");
        }

        Assert.AreNotEqual(await StyleOfAsync(Grid("grid-basic"), "color"), await StyleOfAsync(Grid(id), "color"), "A disabled grid takes the disabled text color.");

        await Page.Locator("#btn-disable").ClickAsync();
        await Expect(Grid(id)).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Table(id)).Not.ToHaveAttributeAsync("aria-disabled", new Regex(".*"));
        await Expect(Grid(id)).ToHaveAttributeAsync("data-bit-dtg-select-all", "true");
        await Expect(HeaderRow(id).Locator("button.bit-dtg-htext")).ToHaveCountAsync(3);
        await Expect(FilterRow(id).Locator("input").First).ToBeEnabledAsync();
        await Expect(FilterRow(id).Locator("input").First).Not.ToHaveAttributeAsync("aria-disabled", new Regex(".*"));
    }

    [TestMethod]
    public async Task Disabled_RealClicks_RunNoneOfThePagesHandlers()
    {
        await OpenAsync(Url);

        const string id = "grid-disabled";
        // Forced past Playwright's own actionability check, which takes a cell of an aria-disabled grid for a
        // disabled control: the click is still a real one, at the cell's center.
        await Cell(id, 0, 1).Locator("input").ClickAsync(new() { Force = true });
        await Cell(id, 0, 3).ClickAsync(new() { Force = true });
        await Cell(id, 1, 4).ClickAsync(new() { Force = true });

        await Expect(Text("disabled-row-clicks")).ToHaveTextAsync("0");
        await Expect(Text("disabled-page-clicks")).ToHaveTextAsync("0");
        await Expect(DataRow(id, 0)).ToHaveAttributeAsync("aria-selected", "false");

        // The data stays readable: a focused cell still moves.
        await ExpectFocusedAsync(Cell(id, 1, 4));
        await Page.Keyboard.PressAsync("ArrowDown");
        await ExpectFocusedAsync(Cell(id, 2, 4));

        await Page.Locator("#btn-disable").ClickAsync();
        await Expect(Text("disabled-state")).ToHaveTextAsync("enabled");

        await Cell(id, 0, 3).ClickAsync();
        await Expect(Text("disabled-row-clicks")).ToHaveTextAsync("1");
        await Expect(Text("disabled-page-clicks")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Lifecycle_ParameterChanges_ReRenderTheGrid()
    {
        await OpenAsync(Url);

        await Expect(DataRows("grid-lifecycle")).ToHaveCountAsync(3);

        await Page.Locator("#btn-add-item").ClickAsync();
        await Expect(DataRows("grid-lifecycle")).ToHaveCountAsync(4);
        await Expect(Cell("grid-lifecycle", 3, 2)).ToHaveTextAsync("Added 4");
        await Expect(Table("grid-lifecycle")).ToHaveAttributeAsync("aria-rowcount", "6");

        await Page.Locator("#btn-rename-column").ClickAsync();
        await Expect(HeaderButton("grid-lifecycle", "Renamed")).ToBeVisibleAsync();
        await Expect(FilterRow("grid-lifecycle").Locator("input[aria-label='Filter by Renamed']")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Lifecycle_Unmounting_LeavesNoServerOrConsoleError()
    {
        await OpenAsync(Url);

        await Cell("grid-lifecycle", 0, 2).ClickAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await FilterRow("grid-lifecycle").Locator("input").Last.FillAsync("La");

        // The grid goes away with the keystroke still waiting out its debounce. A real click would move the focus off
        // the box first, and the blur applies the filter - which, where it renders before the button comes back up (the
        // page running in the browser), shortens the page under the pointer so the click misses the button.
        await Page.Locator("#btn-unmount").DispatchEventAsync("click");

        await Expect(Text("lifecycle-state")).ToHaveTextAsync("unmounted");
        await Expect(Grid("grid-lifecycle")).ToHaveCountAsync(0);
        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    // Every BitTextAlign in its declared order: the class it renders (if any) and where its content lands, left to right
    // and right to left. Only Start, End, Left, Right and Center are honoured; the rest stay at the leading edge.
    private static readonly (string Name, string? Class, string Ltr, string Rtl)[] Aligns =
    [
        ("Start", null, "left", "right"),
        ("End", "bit-dtg-end", "right", "left"),
        ("Left", "bit-dtg-left", "left", "left"),
        ("Right", "bit-dtg-right", "right", "right"),
        ("Center", "bit-dtg-center", "center", "center"),
        ("Justify", null, "left", "right"),
        ("JustifyAll", null, "left", "right"),
        ("MatchParent", null, "left", "right"),
        ("Inherit", null, "left", "right"),
        ("Initial", null, "left", "right"),
        ("Revert", null, "left", "right"),
        ("RevertLayer", null, "left", "right"),
        ("Unset", null, "left", "right"),
    ];

    [TestMethod]
    [DataRow("ltr")]
    [DataRow("rtl")]
    public async Task Align_EveryBitTextAlign_PlacesTheContentWhereItSays(string dir)
    {
        await OpenAsync(Url);

        var id = $"grid-align-{dir}";
        await Expect(HeaderCells(id)).ToHaveCountAsync(Aligns.Length);

        // Where the probe sits in each cell: left, right or center.
        var sides = await DataRow(id, 0).EvaluateAsync<string[]>("""
            row => [...row.querySelectorAll(':scope > [role=gridcell]')].map(cell => {
                const c = cell.getBoundingClientRect(), p = cell.querySelector('.align-probe').getBoundingClientRect();
                const left = p.left - c.left, right = c.right - p.right;
                return Math.abs(left - right) < 4 ? 'center' : left < right ? 'left' : 'right';
            })
            """);

        for (var i = 0; i < Aligns.Length; i++)
        {
            var (name, cls, ltr, rtl) = Aligns[i];
            var cell = Cell(id, 0, i + 1);
            await Expect(HeaderCell(id, i + 1)).ToHaveTextAsync(name);
            if (cls is null) await Expect(cell).Not.ToHaveClassAsync(new Regex("bit-dtg-(end|center|left|right)"));
            else await Expect(cell).ToHaveClassAsync(HasClass(cls));
            Assert.AreEqual(dir == "ltr" ? ltr : rtl, sides[i], $"Align {name} put the content on the wrong side ({dir}).");
        }
    }

    [TestMethod]
    public async Task StyleAndClass_TheGroupDetailAndChooserParts_TakeTheirClassesAndStyles()
    {
        await OpenAsync(Url);

        const string id = "grid-parts";
        await Page.Locator("#btn-parts-group").ClickAsync();
        var groupCell = Page.Locator($"#{id} .bit-dtg-group-cell").First;
        await Expect(groupCell).ToHaveClassAsync(HasClass("c-group-row"));
        await Expect(groupCell).ToHaveCSSAsync("color", "rgb(5, 6, 7)");

        await Page.Locator("#btn-parts-detail").ClickAsync();
        var detail = Page.Locator($"#{id} .bit-dtg-detail-content");
        await Expect(detail).ToHaveTextAsync("Detail of Laptop");
        await Expect(detail).ToHaveClassAsync(HasClass("c-detail-row"));
        await Expect(detail).ToHaveCSSAsync("color", "rgb(8, 9, 10)");

        await Toolbar(id).GetByRole(AriaRole.Button, new() { Name = "Columns" }).ClickAsync();
        var chooser = Page.Locator($"#{id} > .bit-dtg-column-chooser");
        await Expect(chooser).ToHaveClassAsync(HasClass("c-chooser"));
        await Expect(chooser).ToHaveCSSAsync("color", "rgb(11, 12, 13)");
    }

    [TestMethod]
    public async Task CssVariables_TheStateVariables_RestyleSelectionFocusEditingGroupsDetailsTreesAndDisabled()
    {
        await OpenAsync(Url);

        const string id = "grid-vars-states";
        await Expect(DataRow(id, 0)).ToHaveCSSAsync("color", "rgb(0, 90, 0)");
        await Expect(DataRow(id, 1)).Not.ToHaveCSSAsync("color", "rgb(0, 90, 0)");

        var placeholder = await FilterRow(id).Locator("input").First.EvaluateAsync<string>("el => getComputedStyle(el, '::placeholder').color");
        Assert.AreEqual("rgb(160, 0, 160)", placeholder);

        // Single selection makes the cells navigable: the focused one shows the focus color. (The detail toggle
        // column comes first.)
        await Cell(id, 2, 2).ClickAsync();
        await Expect(Cell(id, 2, 2)).ToHaveCSSAsync("outline-color", "rgb(0, 0, 200)");

        await Page.Locator("#btn-vars-edit").ClickAsync();
        await Expect(DataRow(id, 1)).ToHaveClassAsync(HasClass("bit-dtg-editing"));
        await Expect(Cell(id, 1, 2)).ToHaveCSSAsync("background-image", new Regex(@"rgba\(200, 0, 0, 0\.5\)"));
        await DataRow(id, 1).Locator(".bit-dtg-cell-command").GetByRole(AriaRole.Button, new() { Name = "Cancel" }).ClickAsync();

        await Page.Locator("#btn-vars-detail").ClickAsync();
        await Expect(Page.Locator($"#{id} .bit-dtg-detail-content")).ToHaveCSSAsync("background-color", "rgb(190, 180, 170)");

        await Page.Locator("#btn-vars-group").ClickAsync();
        await Expect(Page.Locator($"#{id} .bit-dtg-group-row")).Not.ToHaveCountAsync(0);
        await Page.Locator("#btn-vars-group2").ClickAsync();
        await Expect(Page.Locator($"#{id} .bit-dtg-group-row").Nth(1)).ToHaveAttributeAsync("style", new Regex(@"--bit-dtg-group-level:\s*1"));
        var groups = Page.Locator($"#{id} .bit-dtg-group-cell");
        await Expect(groups.First).ToHaveCSSAsync("background-color", "rgb(220, 210, 200)");
        var outer = double.Parse((await StyleOfAsync(groups.Nth(0), "padding-left")).Replace("px", ""), System.Globalization.CultureInfo.InvariantCulture);
        var inner = double.Parse((await StyleOfAsync(groups.Nth(1), "padding-left")).Replace("px", ""), System.Globalization.CultureInfo.InvariantCulture);
        Assert.AreEqual(40, inner - outer, 0.5, "A nested group is not indented by the group indent.");

        var indents = Page.Locator("#grid-vars-tree .bit-dtg-tree-indent");
        await Expect(indents.Nth(1)).ToHaveCSSAsync("width", "50px");
        await Expect(indents.Nth(2)).ToHaveCSSAsync("width", "100px");

        await Expect(Grid("grid-vars-disabled")).ToHaveCSSAsync("color", "rgb(123, 124, 125)");
    }

    [TestMethod]
    public async Task Rtl_ACascadedDirAndDirAuto_ReachTheGrid()
    {
        await OpenAsync(Url);

        var cascaded = Grid("grid-rtl-cascaded");
        await Expect(cascaded).ToHaveAttributeAsync("dir", "rtl");
        await Expect(cascaded).ToHaveClassAsync(HasClass("bit-rtl"));
        Assert.IsTrue((await BoxAsync(HeaderCell("grid-rtl-cascaded", 1))).X > (await BoxAsync(HeaderCell("grid-rtl-cascaded", 2))).X);

        await Expect(Grid("grid-dir-auto")).ToHaveAttributeAsync("dir", "auto");
        await Expect(Grid("grid-dir-auto")).Not.ToHaveClassAsync(HasClass("bit-rtl"));
    }

    [TestMethod]
    public async Task TabIndex_OnTheGrid_MakesItsRootATabStop()
    {
        await OpenAsync(Url);

        var root = Grid("grid-dir-auto");
        await Expect(root).ToHaveAttributeAsync("tabindex", "0");
        await root.FocusAsync();
        await ExpectFocusedAsync(root);
        await Expect(Grid("grid-basic")).Not.ToHaveAttributeAsync("tabindex", new Regex(".*"));
    }
}
