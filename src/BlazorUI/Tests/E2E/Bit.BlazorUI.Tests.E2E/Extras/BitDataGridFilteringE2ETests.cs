using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// BitDataGrid's filtering and quick search on /e2e/data-grid/filtering: the filter row and its editor per data type
/// (text, number, enum, boolean, date - a DateTime equality matching the whole day), every operator of the operator
/// picker for text, numbers and dates (the value-less ones included, and the column that opts out of the picker),
/// FilterDebounce at its default, at 0 and negative, combined filters, the no-matches message, the toolbar's Clear
/// filters and where it leaves the focus, OnFilterChange with its descriptors, a FilterTemplate driving the filter
/// context (ApplyAsync with In, ApplyRangeAsync, ClearAsync), and the search box: its markup, matching the rendered
/// text, Searchable off, the clear button, the two-way SearchText from both sides, SearchAsync, SearchDebounce, the
/// reset to the first page and the announcements.
/// </summary>
[TestClass]
public class BitDataGridFilteringE2ETests : BitDataGridE2ETestBase
{
    private const string Url = "/e2e/data-grid/filtering";

    private static readonly string[] AllNames = ["Laptop", "Novel", "Jacket", "Lamp", "Drone", "Camera", "Cookbook", "Sneaker", "Mug", "Puzzle"];

    private ILocator FilterBox(string gridId, string title) => FilterRow(gridId).Locator($"[aria-label='Filter by {title}']");

    private ILocator OperatorPicker(string gridId, string title) => FilterRow(gridId).Locator($"select[aria-label='Filter operator for {title}']");

    [TestMethod]
    public async Task FilterRow_EveryColumn_GetsTheEditorOfItsDataType()
    {
        await OpenAsync(Url);

        const string id = "grid-filter";
        var row = FilterRow(id);
        await Expect(row).ToHaveAttributeAsync("role", "row");
        await Expect(row).ToHaveAttributeAsync("aria-rowindex", "2");
        await Expect(Table(id)).ToHaveAttributeAsync("aria-rowcount", "12");

        // The column that opts out has an empty cell.
        await Expect(row.Locator("> [aria-colindex='1'] :is(input, select)")).ToHaveCountAsync(0);

        await Expect(FilterBox(id, "Name")).ToHaveAttributeAsync("placeholder", "Filter…");
        await Expect(FilterBox(id, "Stock")).ToHaveAttributeAsync("placeholder", "=");
        await Expect(FilterBox(id, "Stock")).ToHaveAttributeAsync("inputmode", "decimal");
        await Expect(FilterBox(id, "Release Date")).ToHaveAttributeAsync("type", "date");
        await Expect(FilterBox(id, "Category").Locator("option")).ToHaveTextAsync(["All", "Electronics", "Books", "Clothing", "Home", "Toys"]);
        await Expect(FilterBox(id, "Discontinued").Locator("option")).ToHaveTextAsync(["All", "True", "False"]);
        await Expect(FilterBox(id, "Name")).ToHaveAccessibleNameAsync("Filter by Name");

        // Without operators no picker renders.
        await Expect(row.Locator("select.bit-dtg-filter-op")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Filter_TypingText_KeepsTheRowsContainingItIgnoringCase()
    {
        await OpenAsync(Url);

        await FilterBox("grid-filter", "Name").FillAsync("LA");

        await ExpectColumnAsync("grid-filter", 2, "Laptop", "Lamp");
        await Expect(Text("filter-log")).ToHaveTextAsync("Name:Contains:LA");
        await Expect(Text("filter-count")).ToHaveTextAsync("1");
        await Expect(LiveRegion("grid-filter")).ToContainTextAsync("Filter applied on Name. Rows: 2");
        await Expect(Table("grid-filter")).ToHaveAttributeAsync("aria-rowcount", "4");

        // Emptying the box removes the filter.
        await FilterBox("grid-filter", "Name").FillAsync("");
        await ExpectColumnAsync("grid-filter", 2, AllNames);
        await Expect(Text("filter-log")).ToHaveTextAsync("");
        await Expect(LiveRegion("grid-filter")).ToContainTextAsync("Filter on Name cleared");
    }

    // Types the text into a box one character at a time as input events dispatched together, the way a burst of typing
    // reaches the grid, but with no machine load able to spread the keystrokes past a debounce.
    private static Task TypeBurstAsync(ILocator box, string text) => box.EvaluateAsync("""
        (el, text) => {
            el.focus();
            for (let i = 1; i <= text.length; i++) {
                el.value = text.slice(0, i);
                el.dispatchEvent(new Event('input', { bubbles: true }));
            }
        }
        """, text);

    [TestMethod]
    public async Task Filter_TheDefaultDebounce_TurnsABurstOfTypingIntoOneFilter()
    {
        await OpenAsync(Url);

        await TypeBurstAsync(FilterBox("grid-filter", "Name"), "lam");

        await ExpectColumnAsync("grid-filter", 2, "Lamp");
        await Expect(Text("filter-log")).ToHaveTextAsync("Name:Contains:lam");
        await Expect(Text("filter-count")).ToHaveTextAsync("1");
        await Expect(FilterBox("grid-filter", "Name")).ToHaveValueAsync("lam");

        // Real typing reaches the same filter, and what was typed stays in the box through the renders it caused.
        await FilterBox("grid-filter", "Name").FillAsync("");
        await FilterBox("grid-filter", "Name").PressSequentiallyAsync("lap", new() { Delay = 40 });
        await ExpectColumnAsync("grid-filter", 2, "Laptop");
        await Expect(FilterBox("grid-filter", "Name")).ToHaveValueAsync("lap");
    }

    [TestMethod]
    public async Task Filter_TheEnumBooleanNumberAndDateEditors_ApplyEquality()
    {
        await OpenAsync(Url);

        const string id = "grid-filter";

        await FilterBox(id, "Category").SelectOptionAsync("Books");
        await ExpectColumnAsync(id, 2, "Novel", "Cookbook");
        await Expect(Text("filter-log")).ToHaveTextAsync("Category:Equals:Books");

        await FilterBox(id, "Category").SelectOptionAsync("");
        await ExpectColumnAsync(id, 2, AllNames);

        await FilterBox(id, "Discontinued").SelectOptionAsync("true");
        await ExpectColumnAsync(id, 2, "Jacket", "Cookbook");
        await Expect(Text("filter-log")).ToHaveTextAsync("Discontinued:Equals:True");
        await FilterBox(id, "Discontinued").SelectOptionAsync("");

        await FilterBox(id, "Stock").FillAsync("45");
        await ExpectColumnAsync(id, 2, "Sneaker");
        await Expect(Text("filter-log")).ToHaveTextAsync("Stock:Equals:45");
        await FilterBox(id, "Stock").FillAsync("");

        // A DateTime equality is the whole day, so the row with a time of day matches too.
        await FillDateAsync(FilterBox(id, "Release Date"), "2023-02-10");
        await ExpectColumnAsync(id, 2, "Lamp");
        await Expect(Text("filter-log")).ToHaveTextAsync("ReleaseDate:GreaterThanOrEqual:2023-02-10,ReleaseDate:LessThan:2023-02-11");
    }

    [TestMethod]
    public async Task Filter_SeveralColumns_CombineAndShowTheNoMatchesMessage()
    {
        await OpenAsync(Url);

        const string id = "grid-filter";
        await FilterBox(id, "Category").SelectOptionAsync("Books");
        await FilterBox(id, "Discontinued").SelectOptionAsync("true");

        await ExpectColumnAsync(id, 2, "Cookbook");
        await Expect(Text("filter-log")).ToHaveTextAsync("Category:Equals:Books,Discontinued:Equals:True");

        await FilterBox(id, "Name").FillAsync("zzz");
        await Expect(DataRows(id)).ToHaveCountAsync(0);
        await Expect(MessageRow(id)).ToHaveTextAsync("No records match the current filters.");
    }

    [TestMethod]
    public async Task Toolbar_ClearFilters_AppearsWhileFilteredAndClearsEverything()
    {
        await OpenAsync(Url);

        const string id = "grid-filter";
        var clear = Toolbar(id).GetByRole(AriaRole.Button, new() { Name = "Clear filters" });

        // ShowToolbar with nothing in it yet takes no room.
        await Expect(Toolbar(id)).ToHaveCSSAsync("display", "none");
        await Expect(clear).ToHaveCountAsync(0);

        await FilterBox(id, "Name").FillAsync("o");
        await FilterBox(id, "Category").SelectOptionAsync("Books");
        await ExpectColumnAsync(id, 2, "Novel", "Cookbook");
        await Expect(Toolbar(id)).ToHaveCSSAsync("display", "flex");

        await clear.ClickAsync();

        await ExpectColumnAsync(id, 2, AllNames);
        await Expect(clear).ToHaveCountAsync(0);
        await Expect(FilterBox(id, "Name")).ToHaveValueAsync("");
        await Expect(FilterBox(id, "Category")).ToHaveValueAsync("");
        await Expect(Text("filter-log")).ToHaveTextAsync("");
        await Expect(LiveRegion(id)).ToContainTextAsync("All filters cleared");
        // The button went away with the filters, so the focus moved on to where the next filter is typed.
        await ExpectFocusedAsync(FilterBox(id, "Name"));
    }

    [TestMethod]
    public async Task Operators_ThePicker_OffersTheOperatorsOfEachDataType()
    {
        await OpenAsync(Url);

        const string id = "grid-ops";
        await Expect(OperatorPicker(id, "Name").Locator("option")).ToHaveTextAsync(["Contains", "Doesn't contain", "Starts with", "Ends with", "=", "≠", "Is any of", "Is none of", "Is blank", "Is not blank"]);
        await Expect(OperatorPicker(id, "Stock").Locator("option")).ToHaveTextAsync(["=", "≠", ">", "≥", "<", "≤", "Is any of", "Is none of", "Is blank", "Is not blank"]);
        await Expect(OperatorPicker(id, "Release Date").Locator("option")).ToHaveTextAsync(["=", "≠", ">", "≥", "<", "≤", "Is blank", "Is not blank"]);

        // Each picker starts on its type's default, and the column that opts out, the enum and the boolean have none.
        await Expect(OperatorPicker(id, "Name")).ToHaveValueAsync("Contains");
        await Expect(OperatorPicker(id, "Stock")).ToHaveValueAsync("Equals");
        await Expect(OperatorPicker(id, "Supplier")).ToHaveCountAsync(0);
        await Expect(OperatorPicker(id, "Category")).ToHaveCountAsync(0);
        await Expect(OperatorPicker(id, "Discontinued")).ToHaveCountAsync(0);
        await Expect(FilterBox(id, "Supplier")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Operators_EveryTextOperator_FiltersTheRows()
    {
        await OpenAsync(Url);

        (string Op, string Value, string[] Expected)[] cases =
        [
            ("Contains", "o", ["Laptop", "Novel", "Drone", "Cookbook"]),
            ("DoesNotContain", "o", ["Jacket", "Lamp", "Camera", "Sneaker", "Mug", "Puzzle"]),
            ("StartsWith", "la", ["Laptop", "Lamp"]),
            ("EndsWith", "e", ["Drone", "Puzzle"]),
            ("Equals", "mug", ["Mug"]),
            ("NotEquals", "Mug", ["Laptop", "Novel", "Jacket", "Lamp", "Drone", "Camera", "Cookbook", "Sneaker", "Puzzle"]),
            ("In", "Mug, Lamp", ["Lamp", "Mug"]),
            ("NotIn", "Mug,Lamp", ["Laptop", "Novel", "Jacket", "Drone", "Camera", "Cookbook", "Sneaker", "Puzzle"]),
        ];

        foreach (var (op, value, expected) in cases)
        {
            await OperatorPicker("grid-ops", "Name").SelectOptionAsync(op);
            await FilterBox("grid-ops", "Name").FillAsync(value);
            await ExpectColumnAsync("grid-ops", 1, expected);
            await Expect(Text("ops-log")).ToHaveTextAsync(new Regex($"^Name:{op}:"));
            await FilterBox("grid-ops", "Name").FillAsync("");
            await Expect(DataRows("grid-ops")).ToHaveCountAsync(10);
        }

        // The set operators read a comma-separated list, so their box says so.
        await OperatorPicker("grid-ops", "Name").SelectOptionAsync("In");
        await Expect(FilterBox("grid-ops", "Name")).ToHaveAttributeAsync("placeholder", "a, b, c…");
        await FilterBox("grid-ops", "Name").FillAsync("Mug, Lamp");
        await Expect(Text("ops-log")).ToHaveTextAsync("Name:In:[Mug;Lamp]");
    }

    [TestMethod]
    public async Task Operators_EveryNumberOperator_FiltersTheRows()
    {
        await OpenAsync(Url);

        (string Op, string Value, string[] Expected)[] cases =
        [
            ("Equals", "45", ["Sneaker"]),
            ("NotEquals", "45", ["Laptop", "Novel", "Jacket", "Lamp", "Drone", "Camera", "Cookbook", "Mug", "Puzzle"]),
            ("GreaterThan", "100", ["Novel", "Mug"]),
            ("GreaterThanOrEqual", "120", ["Novel", "Mug"]),
            ("LessThan", "8", ["Laptop", "Jacket"]),
            ("LessThanOrEqual", "8", ["Laptop", "Jacket", "Drone"]),
            ("In", "5, 8", ["Laptop", "Drone"]),
            ("NotIn", "5,8", ["Novel", "Jacket", "Lamp", "Camera", "Cookbook", "Sneaker", "Mug", "Puzzle"]),
        ];

        foreach (var (op, value, expected) in cases)
        {
            await OperatorPicker("grid-ops", "Stock").SelectOptionAsync(op);
            await FilterBox("grid-ops", "Stock").FillAsync(value);
            await ExpectColumnAsync("grid-ops", 1, expected);
            await Expect(Text("ops-log")).ToHaveTextAsync(new Regex($"^Stock:{op}:"));
            await FilterBox("grid-ops", "Stock").FillAsync("");
            await Expect(DataRows("grid-ops")).ToHaveCountAsync(10);
        }
    }

    [TestMethod]
    public async Task Operators_EveryDateOperator_FiltersTheRows()
    {
        await OpenAsync(Url);

        (string Op, string Value, string[] Expected)[] cases =
        [
            ("Equals", "2023-05-01", ["Laptop"]),
            ("NotEquals", "2023-05-01", ["Novel", "Jacket", "Lamp", "Drone", "Camera", "Cookbook", "Sneaker", "Mug", "Puzzle"]),
            ("GreaterThan", "2023-08-22", ["Drone", "Sneaker"]),
            ("GreaterThanOrEqual", "2023-08-22", ["Drone", "Camera", "Sneaker"]),
            ("LessThan", "2021-04-18", ["Cookbook"]),
            ("LessThanOrEqual", "2021-04-18", ["Cookbook", "Puzzle"]),
        ];

        foreach (var (op, value, expected) in cases)
        {
            await OperatorPicker("grid-ops", "Release Date").SelectOptionAsync(op);
            await FillDateAsync(FilterBox("grid-ops", "Release Date"), value);
            await ExpectColumnAsync("grid-ops", 1, expected);
            await FillDateAsync(FilterBox("grid-ops", "Release Date"), "");
            await Expect(DataRows("grid-ops")).ToHaveCountAsync(10);
        }
    }

    [TestMethod]
    public async Task Operators_IsBlankAndIsNotBlank_ApplyAtOnceWithoutAnEditor()
    {
        await OpenAsync(Url);

        const string id = "grid-ops";

        await OperatorPicker(id, "Note").SelectOptionAsync("IsEmpty");
        await ExpectColumnAsync(id, 1, "Novel", "Jacket", "Lamp", "Camera", "Cookbook", "Sneaker", "Mug", "Puzzle");
        await Expect(FilterBox(id, "Note")).ToHaveCountAsync(0);
        await Expect(OperatorPicker(id, "Note")).ToHaveClassAsync(HasClass("bit-dtg-filter-op-only"));
        await Expect(Text("ops-log")).ToHaveTextAsync("Note:IsEmpty:null");

        await OperatorPicker(id, "Note").SelectOptionAsync("IsNotEmpty");
        await ExpectColumnAsync(id, 1, "Laptop", "Drone");

        // Back on a value operator, the editor returns and nothing is applied until something is typed.
        await OperatorPicker(id, "Note").SelectOptionAsync("Contains");
        await Expect(FilterBox(id, "Note")).ToBeVisibleAsync();
        await Expect(DataRows(id)).ToHaveCountAsync(10);

        // A column without blanks has nothing to show for Is blank.
        await OperatorPicker(id, "Stock").SelectOptionAsync("IsEmpty");
        await Expect(DataRows(id)).ToHaveCountAsync(0);
        await Expect(MessageRow(id)).ToHaveTextAsync("No records match the current filters.");
        await OperatorPicker(id, "Stock").SelectOptionAsync("IsNotEmpty");
        await Expect(DataRows(id)).ToHaveCountAsync(10);
    }

    [TestMethod]
    public async Task Operators_ChangingTheOperator_ReappliesTheTextAlreadyTyped()
    {
        await OpenAsync(Url);

        await FilterBox("grid-ops", "Name").FillAsync("o");
        await ExpectColumnAsync("grid-ops", 1, "Laptop", "Novel", "Drone", "Cookbook");

        await OperatorPicker("grid-ops", "Name").SelectOptionAsync("DoesNotContain");

        await ExpectColumnAsync("grid-ops", 1, "Jacket", "Lamp", "Camera", "Sneaker", "Mug", "Puzzle");
        await Expect(FilterBox("grid-ops", "Name")).ToHaveValueAsync("o");
        await Expect(Text("ops-log")).ToHaveTextAsync("Name:DoesNotContain:o");
    }

    [TestMethod]
    public async Task Debounce_Zero_AppliesEveryKeystroke()
    {
        await OpenAsync(Url);

        await FilterBox("grid-ops", "Supplier").PressSequentiallyAsync("acm", new() { Delay = 30 });

        await ExpectColumnAsync("grid-ops", 1, "Laptop", "Lamp", "Cookbook");
        await Expect(Text("ops-count")).ToHaveTextAsync("3");
        await Expect(Text("ops-log")).ToHaveTextAsync("Supplier:Contains:acm");
    }

    [TestMethod]
    public async Task Debounce_Negative_AppliesOnlyOnEnterOrBlur()
    {
        // The box applies on its change event, which WebKit does not raise for Enter in a text input (Chromium and
        // Firefox do): there only the blur applies the filter.
        if (IsWebKit) Assert.Inconclusive("WebKit raises no change event for Enter in a text input, which a negative FilterDebounce applies on.");

        await OpenAsync(Url);

        // A grid with the default debounce is the yardstick. The same text reaches it through an input event of its
        // own (the focus stays in the box under test, whose blur would apply it): once it has filtered, a debounce
        // has run out since the typing, and the grid that waits for Enter still has not filtered.
        var box = FilterBox("grid-debounce-negative", "Name");
        await box.PressSequentiallyAsync("la");
        await FilterBox("grid-debounce-default", "Name").EvaluateAsync("el => { el.value = 'la'; el.dispatchEvent(new Event('input', { bubbles: true })); }");
        await ExpectColumnAsync("grid-debounce-default", 1, "Laptop", "Lamp");
        await Expect(Text("debounce-default-count")).ToHaveTextAsync("1");

        await ExpectFocusedAsync(box);
        await Expect(DataRows("grid-debounce-negative")).ToHaveCountAsync(10);
        await Expect(Text("debounce-negative-count")).ToHaveTextAsync("0");

        await FilterBox("grid-debounce-negative", "Name").PressAsync("Enter");
        await ExpectColumnAsync("grid-debounce-negative", 1, "Laptop", "Lamp");
        await Expect(Text("debounce-negative-count")).ToHaveTextAsync("1");

        // A blur applies what is typed too.
        await FilterBox("grid-debounce-negative", "Name").FillAsync("mu");
        await Page.Locator("h1").ClickAsync();
        await ExpectColumnAsync("grid-debounce-negative", 1, "Mug");
        await Expect(Text("debounce-negative-count")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task FilterTemplate_TheContext_AppliesInRangesAndClears()
    {
        await OpenAsync(Url);

        const string id = "grid-filter-template";
        await Expect(Text("filter-context")).ToHaveTextAsync("Category|Category|Filter by Category|Category|False|Unspecified|0|False");
        await Expect(FilterRow(id).Locator(".category-filter")).ToHaveAttributeAsync("aria-label", "Filter by Category");

        await Page.Locator("#btn-filter-books-toys").ClickAsync();
        await ExpectColumnAsync(id, 1, "Novel", "Drone", "Cookbook", "Puzzle");
        await Expect(Text("template-log")).ToHaveTextAsync("Category:In:[Books;Toys]");
        await Expect(Text("filter-context")).ToHaveTextAsync("Category|Category|Filter by Category|Category|True|In|1|False");

        await Page.Locator("#btn-filter-range").ClickAsync();
        await ExpectColumnAsync(id, 1, "Cookbook", "Puzzle");
        await Expect(Text("template-log")).ToHaveTextAsync("Category:In:[Books;Toys],Stock:GreaterThanOrEqual:30,Stock:LessThan:100");
        await Expect(Text("range-context")).ToHaveTextAsync("True|2");

        await Page.Locator("#btn-filter-clear").ClickAsync();
        await ExpectColumnAsync(id, 1, "Lamp", "Cookbook", "Sneaker", "Puzzle");
        await Expect(Text("filter-context")).ToHaveTextAsync(new Regex(@"\|False\|Unspecified\|0\|False$"));
    }

    [TestMethod]
    public async Task Search_TheSearchBox_IsALabelledSearchField()
    {
        await OpenAsync(Url);

        var box = Toolbar("grid-search").Locator("input.bit-dtg-search-input");
        await Expect(box).ToHaveAttributeAsync("type", "search");
        await Expect(box).ToHaveAttributeAsync("placeholder", "Search…");
        await Expect(box).ToHaveAccessibleNameAsync("Search all columns");
        await Expect(Page.GetByRole(AriaRole.Searchbox, new() { Name = "Search all columns" }).First).ToBeVisibleAsync();
        await Expect(Toolbar("grid-search").Locator(".bit-dtg-search-clear")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Search_Typing_FiltersEverySearchableColumnByItsRenderedText()
    {
        await OpenAsync(Url);

        var box = Toolbar("grid-search").Locator("input.bit-dtg-search-input");

        await TypeBurstAsync(box, "acme");
        await ExpectColumnAsync("grid-search", 2, "Laptop", "Lamp", "Cookbook");
        await Expect(Text("search-text")).ToHaveTextAsync("acme");
        // A burst of typing is one search.
        await Expect(Text("search-count")).ToHaveTextAsync("1");
        await Expect(Text("active-search")).ToHaveTextAsync("acme");
        await Expect(LiveRegion("grid-search")).ToContainTextAsync("Searching for acme. Rows: 3");

        // An enum column matches by the name it shows.
        await box.FillAsync("electronics");
        await ExpectColumnAsync("grid-search", 2, "Laptop", "Camera");

        // Stock is not searchable: Novel's 120 matches nothing.
        await box.FillAsync("120");
        await Expect(DataRows("grid-search")).ToHaveCountAsync(0);
        await Expect(MessageRow("grid-search")).ToHaveTextAsync("No records match the current filters.");
    }

    [TestMethod]
    public async Task Search_TheClearButton_ClearsTheSearchAndKeepsTheFocusInTheBox()
    {
        await OpenAsync(Url);

        var box = Toolbar("grid-search").Locator("input.bit-dtg-search-input");
        await box.FillAsync("globex");
        await ExpectColumnAsync("grid-search", 2, "Novel", "Drone", "Puzzle");

        var clear = Toolbar("grid-search").Locator(".bit-dtg-search-clear");
        await Expect(clear).ToHaveAccessibleNameAsync("Clear search");
        await clear.ClickAsync();

        await Expect(DataRows("grid-search")).ToHaveCountAsync(5);
        await Expect(box).ToHaveValueAsync("");
        await ExpectFocusedAsync(box);
        await Expect(clear).ToHaveCountAsync(0);
        await Expect(Text("search-text")).ToHaveTextAsync("");
        await Expect(LiveRegion("grid-search")).ToContainTextAsync("Search cleared");
    }

    [TestMethod]
    public async Task Search_TheBoundSearchText_DrivesTheSearchFromThePageAndFromSearchAsync()
    {
        await OpenAsync(Url);

        var box = Toolbar("grid-search").Locator("input.bit-dtg-search-input");

        await Page.Locator("#btn-set-search").ClickAsync();
        await Expect(box).ToHaveValueAsync("acme");
        await ExpectColumnAsync("grid-search", 2, "Laptop", "Lamp", "Cookbook");

        await Page.Locator("#btn-search-api").ClickAsync();
        await Expect(box).ToHaveValueAsync("globex");
        await ExpectColumnAsync("grid-search", 2, "Novel", "Drone", "Puzzle");
        await Expect(Text("search-text")).ToHaveTextAsync("globex");
        await Expect(Text("active-search")).ToHaveTextAsync("globex");
    }

    [TestMethod]
    public async Task Search_ANewTerm_GoesBackToTheFirstPage()
    {
        await OpenAsync(Url);

        await Pagers("grid-search").Locator("button[aria-label='Next page']").ClickAsync();
        await Expect(Pagers("grid-search").Locator(".bit-dtg-pager-current")).ToHaveTextAsync("Page 2 of 2");

        await Toolbar("grid-search").Locator("input.bit-dtg-search-input").FillAsync("ac");

        await Expect(Pagers("grid-search").Locator(".bit-dtg-pager-current")).ToHaveTextAsync("Page 1 of 1");
        await ExpectColumnAsync("grid-search", 2, "Laptop", "Jacket", "Lamp", "Cookbook");
    }

    [TestMethod]
    public async Task Search_ZeroDebounce_SearchesOnEveryKeystroke()
    {
        await OpenAsync(Url);

        await Toolbar("grid-search-instant").Locator("input.bit-dtg-search-input").PressSequentiallyAsync("lam", new() { Delay = 30 });

        await ExpectColumnAsync("grid-search-instant", 1, "Lamp");
        await Expect(Text("instant-count")).ToHaveTextAsync("3");
        await Expect(Text("instant-text")).ToHaveTextAsync("lam");
    }
}
