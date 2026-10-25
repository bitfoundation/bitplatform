using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// BitDataGrid's grouping on /e2e/data-grid/grouping: the header's group toggle (its name, aria-pressed and active
/// state) and only on the groupable columns, the group rows with their key, count and per-group aggregates, the pager
/// stepping aside while grouped, collapsing and expanding one group, nested groups and their indent,
/// ExpandAllGroupsAsync / CollapseAllGroupsAsync, GroupsInitiallyCollapsed, the grouping API (GroupByAsync, UngroupAsync,
/// ClearGroupsAsync), OnGroupChange with its descriptors, the footer's grand totals, and the keyboard skipping the rows of
/// a collapsed group.
/// </summary>
[TestClass]
public class BitDataGridGroupingE2ETests : BitDataGridE2ETestBase
{
    private const string Url = "/e2e/data-grid/grouping";
    private const string Id = "grid-group";

    private ILocator GroupRows(string gridId) => Page.Locator($"#{gridId} > .bit-dtg-viewport > .bit-dtg-table > .bit-dtg-body > .bit-dtg-group-row");

    private ILocator GroupToggle(string gridId, int group) => GroupRows(gridId).Nth(group).Locator(".bit-dtg-group-cell > button");

    private ILocator GroupButton(string title) => HeaderRow(Id).GetByRole(AriaRole.Button, new() { Name = $"Group by {title}", Exact = true });

    [TestMethod]
    public async Task Grouping_TheHeaderToggle_GroupsByTheColumn()
    {
        await OpenAsync(Url);

        // Only the groupable columns carry the toggle.
        await Expect(HeaderRow(Id).Locator(".bit-dtg-group-btn")).ToHaveCountAsync(2);
        await Expect(GroupButton("Category")).ToHaveAttributeAsync("aria-pressed", "false");
        await Expect(Pagers(Id)).ToHaveCountAsync(1);
        await Expect(DataRows(Id)).ToHaveCountAsync(4);

        await GroupButton("Category").ClickAsync();

        var toggle = HeaderCell(Id, 3).Locator(".bit-dtg-group-btn");
        await Expect(toggle).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(toggle).ToHaveAccessibleNameAsync("Ungroup by Category");
        await Expect(toggle).ToHaveClassAsync(HasClass("bit-dtg-active"));
        await Expect(Text("group-log")).ToHaveTextAsync("Category:Ascending");
        await Expect(Text("group-count")).ToHaveTextAsync("1");
        await Expect(LiveRegion(Id)).ToContainTextAsync("Grouped by Category");

        await Expect(GroupRows(Id).Locator(".bit-dtg-group-title")).ToHaveTextAsync(["Category: Electronics", "Category: Books", "Category: Clothing", "Category: Home", "Category: Toys"]);
        await ExpectColumnAsync(Id, 2, "Laptop", "Camera", "Novel", "Cookbook", "Jacket", "Sneaker", "Lamp", "Mug", "Drone", "Puzzle");

        // Grouped rows are not paged.
        await Expect(Pagers(Id)).ToHaveCountAsync(0);

        // The toggle ungroups again.
        await toggle.ClickAsync();
        await Expect(GroupRows(Id)).ToHaveCountAsync(0);
        await Expect(Pagers(Id)).ToHaveCountAsync(1);
        await Expect(Text("group-log")).ToHaveTextAsync("");
        await Expect(LiveRegion(Id)).ToContainTextAsync("Grouping by Category removed");
    }

    [TestMethod]
    public async Task Grouping_AGroupRow_ShowsItsCountAndItsAggregates()
    {
        await OpenAsync(Url);

        await GroupButton("Category").ClickAsync();

        var first = GroupRows(Id).Nth(0);
        await Expect(first).ToHaveAttributeAsync("role", "row");
        await Expect(first.Locator(".bit-dtg-group-cell")).ToHaveAttributeAsync("aria-colspan", "7");
        await Expect(first.Locator(".bit-dtg-group-count")).ToHaveTextAsync("(2)");
        await Expect(first.Locator(".bit-dtg-group-agg")).ToHaveTextAsync(["Name: count 2", "Stock: Σ 17", "Price: avg 925", "Rating: min 4"]);
        await Expect(GroupRows(Id).Nth(1).Locator(".bit-dtg-group-agg")).ToHaveTextAsync(["Name: count 2", "Stock: Σ 180", "Price: avg 20", "Rating: min 3"]);

        // The footer totals the whole set.
        await Expect(FooterRow(Id).Locator("> [role=gridcell]")).ToHaveTextAsync(["", "count 10", "", "", "Σ 550", "avg 246", "min 1"]);
    }

    [TestMethod]
    public async Task Grouping_CollapsingAGroup_HidesItsRows()
    {
        await OpenAsync(Url);

        await GroupButton("Category").ClickAsync();

        var toggle = GroupToggle(Id, 0);
        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(toggle).ToHaveAccessibleNameAsync("Collapse group Category: Electronics");

        await toggle.ClickAsync();

        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(toggle).ToHaveAccessibleNameAsync("Expand group Category: Electronics");
        await Expect(toggle.Locator("span")).ToHaveTextAsync("▸");
        await ExpectColumnAsync(Id, 2, "Novel", "Cookbook", "Jacket", "Sneaker", "Lamp", "Mug", "Drone", "Puzzle");

        await toggle.ClickAsync();
        await Expect(DataRows(Id)).ToHaveCountAsync(10);
        await Expect(toggle.Locator("span")).ToHaveTextAsync("▾");
    }

    [TestMethod]
    public async Task Grouping_ASecondColumn_NestsItsGroupsInsideTheFirst()
    {
        await OpenAsync(Url);

        await GroupButton("Category").ClickAsync();
        await GroupButton("Supplier").ClickAsync();

        await Expect(Text("group-log")).ToHaveTextAsync("Category:Ascending,Supplier:Ascending");
        await Expect(GroupRows(Id)).ToHaveCountAsync(14);
        await Expect(GroupRows(Id).Locator(".bit-dtg-group-title").First).ToHaveTextAsync("Category: Electronics");
        await Expect(GroupRows(Id).Nth(1).Locator(".bit-dtg-group-title")).ToHaveTextAsync("Supplier: Acme");
        await ExpectColumnAsync(Id, 2, "Laptop", "Camera", "Cookbook", "Novel", "Jacket", "Sneaker", "Lamp", "Mug", "Drone", "Puzzle");

        await Expect(GroupRows(Id).Nth(0)).ToHaveAttributeAsync("style", new Regex(@"--bit-dtg-group-level:\s*0"));
        await Expect(GroupRows(Id).Nth(1)).ToHaveAttributeAsync("style", new Regex(@"--bit-dtg-group-level:\s*1"));
        var outer = await BoxAsync(GroupRows(Id).Nth(0).Locator(".bit-dtg-group-title"));
        var inner = await BoxAsync(GroupRows(Id).Nth(1).Locator(".bit-dtg-group-title"));
        Assert.IsTrue(inner.X > outer.X + 5, "A nested group is not indented under its parent.");
    }

    [TestMethod]
    public async Task Grouping_ExpandAllAndCollapseAll_FlipEveryGroup()
    {
        await OpenAsync(Url);

        await GroupButton("Category").ClickAsync();
        await Page.Locator("#btn-collapse-groups").ClickAsync();

        await Expect(DataRows(Id)).ToHaveCountAsync(0);
        await Expect(GroupRows(Id)).ToHaveCountAsync(5);
        var expanded = await GroupRows(Id).Locator(".bit-dtg-group-cell > button").EvaluateAllAsync<string[]>("bs => bs.map(b => b.getAttribute('aria-expanded'))");
        CollectionAssert.AreEqual(Enumerable.Repeat("false", 5).ToArray(), expanded);

        // New groups follow the choice.
        await GroupButton("Supplier").ClickAsync();
        await Expect(GroupRows(Id)).ToHaveCountAsync(5);

        await Page.Locator("#btn-expand-groups").ClickAsync();
        await Expect(DataRows(Id)).ToHaveCountAsync(10);
        await Expect(GroupRows(Id)).ToHaveCountAsync(14);
    }

    [TestMethod]
    public async Task Grouping_GroupsInitiallyCollapsed_OpensTheGroupsClosed()
    {
        await OpenAsync(Url);

        const string id = "grid-group-collapsed";
        await Page.Locator("#btn-group-collapsed").ClickAsync();

        await Expect(GroupRows(id)).ToHaveCountAsync(5);
        await Expect(DataRows(id)).ToHaveCountAsync(0);
        await Expect(GroupToggle(id, 0)).ToHaveAttributeAsync("aria-expanded", "false");

        await GroupToggle(id, 3).ClickAsync();
        await ExpectColumnAsync(id, 1, "Lamp", "Mug");
    }

    [TestMethod]
    public async Task Grouping_TheApi_GroupsUngroupsAndClears()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-group-by-supplier").ClickAsync();
        await Expect(Text("group-log")).ToHaveTextAsync("Supplier:Ascending");
        await Expect(GroupRows(Id).Locator(".bit-dtg-group-title")).ToHaveTextAsync(["Supplier: Acme", "Supplier: Globex", "Supplier: Initech", "Supplier: Umbrella"]);
        await ExpectColumnAsync(Id, 2, "Laptop", "Lamp", "Cookbook", "Novel", "Drone", "Puzzle", "Jacket", "Camera", "Sneaker", "Mug");
        await Expect(HeaderCell(Id, 4).Locator(".bit-dtg-group-btn")).ToHaveAttributeAsync("aria-pressed", "true");

        await Page.Locator("#btn-ungroup-supplier").ClickAsync();
        await Expect(GroupRows(Id)).ToHaveCountAsync(0);
        await Expect(Text("group-log")).ToHaveTextAsync("");

        await GroupButton("Category").ClickAsync();
        await Page.Locator("#btn-group-by-supplier").ClickAsync();
        await Expect(GroupRows(Id)).ToHaveCountAsync(14);

        await Page.Locator("#btn-clear-groups").ClickAsync();
        await Expect(GroupRows(Id)).ToHaveCountAsync(0);
        await Expect(Pagers(Id)).ToHaveCountAsync(1);
        await Expect(Text("group-log")).ToHaveTextAsync("");
        await Expect(LiveRegion(Id)).ToContainTextAsync("Grouping cleared");
        await Expect(Text("group-count")).ToHaveTextAsync("5");
    }

    [TestMethod]
    public async Task Grouping_TheKeyboard_SkipsTheRowsOfACollapsedGroup()
    {
        await OpenAsync(Url);

        await GroupButton("Category").ClickAsync();
        await GroupToggle(Id, 1).ClickAsync();
        await ExpectColumnAsync(Id, 2, "Laptop", "Camera", "Jacket", "Sneaker", "Lamp", "Mug", "Drone", "Puzzle");

        await Cell(Id, 1, 2).ClickAsync();
        await Page.Keyboard.PressAsync("ArrowDown");

        await ExpectFocusedAsync(Cell(Id, 2, 2));
        await Expect(Cell(Id, 2, 2)).ToHaveTextAsync("Jacket");
    }
}
