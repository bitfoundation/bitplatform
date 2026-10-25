using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// BitDataGrid's tree data on /e2e/data-grid/tree: ChildrenSelector turning the grid into a treegrid whose rows carry
/// their aria-level and aria-expanded and indent by level, TreeInitiallyExpanded both ways, the node toggles,
/// ExpandAllAsync / CollapseAllAsync, sorting each level while keeping the hierarchy, the quick search and a column filter
/// pruning the tree to the matching branches, the tree keyboard (the forward arrow opens a node, the backward arrow
/// closes it or goes to the parent), and a lazily loaded tree: HasChildrenSelector's toggles, the busy toggle while
/// ChildrenProvider loads, the children cached once loaded, and the search box and filter row it goes without.
/// </summary>
[TestClass]
public class BitDataGridTreeE2ETests : BitDataGridE2ETestBase
{
    private const string Url = "/e2e/data-grid/tree";
    private const string Id = "grid-tree";

    private static readonly string[] AllRows = ["src", "app", "main.cs", "util.cs", "readme.md", "docs", "guide.md", "license"];

    private ILocator TreeToggle(string gridId, int row) => Cell(gridId, row, 1).Locator(".bit-dtg-tree-toggle");

    // The first column also holds the indent and the toggle, so the names are read off the value the cell renders.
    private Task ExpectNamesAsync(string gridId, params string[] names)
        => Expect(Cells(gridId, 1).Locator(".bit-dtg-cell-text")).ToHaveTextAsync(names);

    [TestMethod]
    public async Task Tree_TheRows_CarryTheirLevelAndExpandedState()
    {
        await OpenAsync(Url);

        await Expect(Table(Id)).ToHaveAttributeAsync("role", "treegrid");
        await ExpectNamesAsync(Id, AllRows);

        var levels = await DataRows(Id).EvaluateAllAsync<string[]>("rows => rows.map(r => r.getAttribute('aria-level'))");
        CollectionAssert.AreEqual(new[] { "1", "2", "3", "3", "2", "1", "2", "1" }, levels);
        var expanded = await DataRows(Id).EvaluateAllAsync<string?[]>("rows => rows.map(r => r.getAttribute('aria-expanded'))");
        CollectionAssert.AreEqual(new string?[] { "true", "true", null, null, null, "true", null, null }, expanded);

        await Expect(TreeToggle(Id, 0)).ToHaveAccessibleNameAsync("Toggle children");
        await Expect(TreeToggle(Id, 2)).ToHaveCountAsync(0);
        await Expect(Cell(Id, 2, 1).Locator(".bit-dtg-tree-leaf")).ToHaveCountAsync(1);

        // Each level indents further.
        var src = await BoxAsync(Cell(Id, 0, 1).Locator(".bit-dtg-cell-text"));
        var app = await BoxAsync(Cell(Id, 1, 1).Locator(".bit-dtg-cell-text"));
        var main = await BoxAsync(Cell(Id, 2, 1).Locator(".bit-dtg-cell-text"));
        Assert.IsTrue(app.X > src.X && main.X > app.X, $"The levels are not indented ({src.X}, {app.X}, {main.X}).");
    }

    [TestMethod]
    public async Task Tree_TheToggle_CollapsesAndExpandsANode()
    {
        await OpenAsync(Url);

        await TreeToggle(Id, 0).ClickAsync();
        await ExpectNamesAsync(Id, "src", "docs", "guide.md", "license");
        await Expect(DataRow(Id, 0)).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(TreeToggle(Id, 0)).ToHaveAttributeAsync("aria-expanded", "false");

        await TreeToggle(Id, 0).ClickAsync();
        await ExpectNamesAsync(Id, AllRows);
    }

    [TestMethod]
    public async Task Tree_ExpandAllAndCollapseAll_FlipEveryNode()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-tree-collapse").ClickAsync();
        await ExpectNamesAsync(Id, "src", "docs", "license");

        await Page.Locator("#btn-tree-expand").ClickAsync();
        await ExpectNamesAsync(Id, AllRows);
    }

    [TestMethod]
    public async Task Tree_InitiallyCollapsed_OpensOneLevelAtATime()
    {
        await OpenAsync(Url);

        const string id = "grid-tree-collapsed";
        await ExpectNamesAsync(id, "src", "docs", "license");

        await TreeToggle(id, 0).ClickAsync();
        await ExpectNamesAsync(id, "src", "app", "readme.md", "docs", "license");
        await Expect(DataRow(id, 1)).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [TestMethod]
    public async Task Tree_Sorting_OrdersEachLevelAndKeepsTheHierarchy()
    {
        await OpenAsync(Url);

        await HeaderButton(Id, "Name").ClickAsync();
        await ExpectNamesAsync(Id, "docs", "guide.md", "license", "src", "app", "main.cs", "util.cs", "readme.md");

        await HeaderButton(Id, "Name").ClickAsync();
        await ExpectNamesAsync(Id, "src", "readme.md", "app", "util.cs", "main.cs", "license", "docs", "guide.md");
    }

    [TestMethod]
    public async Task Tree_SearchAndFilter_PruneTheTreeToTheMatchingBranches()
    {
        await OpenAsync(Url);

        var search = Toolbar(Id).Locator("input.bit-dtg-search-input");
        await search.FillAsync("main");
        await ExpectNamesAsync(Id, "src", "app", "main.cs");

        await search.FillAsync("");
        await ExpectNamesAsync(Id, AllRows);

        await FilterRow(Id).Locator("[aria-label='Filter by Name']").FillAsync("guide");
        await ExpectNamesAsync(Id, "docs", "guide.md");

        // A node that matches itself keeps its whole subtree.
        await FilterRow(Id).Locator("[aria-label='Filter by Name']").FillAsync("app");
        await ExpectNamesAsync(Id, "src", "app", "main.cs", "util.cs");
    }

    [TestMethod]
    public async Task Tree_TheArrowKeys_OpenAndCloseNodesAndGoToTheParent()
    {
        await OpenAsync(Url);

        await Cell(Id, 0, 1).ClickAsync();

        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(DataRow(Id, 0)).ToHaveAttributeAsync("aria-expanded", "false");
        await ExpectNamesAsync(Id, "src", "docs", "guide.md", "license");
        await ExpectFocusedAsync(Cell(Id, 0, 1));

        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(DataRow(Id, 0)).ToHaveAttributeAsync("aria-expanded", "true");
        await ExpectNamesAsync(Id, AllRows);

        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.Keyboard.PressAsync("ArrowDown");
        await ExpectFocusedAsync(Cell(Id, 2, 1));

        // The backward arrow on a leaf goes up to its parent.
        await Page.Keyboard.PressAsync("ArrowLeft");
        await ExpectFocusedAsync(Cell(Id, 1, 1));
        await Expect(Cell(Id, 1, 1)).ToContainTextAsync("app");

        // On an open node's first cell the forward arrow, past opening it, moves on like any other.
        await Page.Keyboard.PressAsync("ArrowRight");
        await ExpectFocusedAsync(Cell(Id, 1, 2));
        await Expect(TreeToggle(Id, 1)).ToHaveAttributeAsync("tabindex", "-1");
    }

    [TestMethod]
    public async Task LazyTree_ChildrenProvider_LoadsANodeOnItsFirstExpandAndCachesIt()
    {
        await OpenAsync(Url);

        const string id = "grid-tree-lazy";
        await ExpectNamesAsync(id, "photos", "music", "notes.txt");
        await Expect(TreeToggle(id, 0)).ToHaveCountAsync(1);
        await Expect(TreeToggle(id, 1)).ToHaveCountAsync(1);
        await Expect(TreeToggle(id, 2)).ToHaveCountAsync(0);

        // A lazily loaded tree cannot search or filter the children it has not loaded.
        await Expect(Toolbar(id)).ToHaveCountAsync(0);
        await Expect(FilterRow(id)).ToHaveCountAsync(0);

        await TreeToggle(id, 0).ClickAsync();
        await Expect(Text("lazy-log")).ToHaveTextAsync("loading photos");
        await Expect(TreeToggle(id, 0)).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(TreeToggle(id, 0)).ToBeDisabledAsync();
        await Expect(TreeToggle(id, 0).Locator("span")).ToHaveTextAsync("…");

        await ExpectNamesAsync(id, "photos", "photos-a", "photos-b", "music", "notes.txt");
        await Expect(TreeToggle(id, 0)).Not.ToHaveAttributeAsync("aria-busy", new Regex(".*"));
        await Expect(DataRow(id, 0)).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Text("lazy-loads")).ToHaveTextAsync("1");

        await TreeToggle(id, 0).ClickAsync();
        await ExpectNamesAsync(id, "photos", "music", "notes.txt");
        await TreeToggle(id, 0).ClickAsync();
        await ExpectNamesAsync(id, "photos", "photos-a", "photos-b", "music", "notes.txt");
        await Expect(Text("lazy-loads")).ToHaveTextAsync("1");
    }
}
