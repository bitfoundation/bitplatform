namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// BitDataGrid's master-detail rows on /e2e/data-grid/detail: the toggle column (its name, aria-expanded and glyph), the
/// detail row under its row holding a nested grid of its own, the two-way ExpandedDetailItems from both sides,
/// OnDetailToggle with its item and state, ExpandAllDetailsAsync / CollapseAllDetailsAsync, ToggleDetailAsync and
/// IsDetailExpanded, ExpandDetailOnRowClick with ShowDetailToggle off (the row becoming the keyboard control for Enter
/// and Space), with Single selection, and Enter on a navigable cell.
/// </summary>
[TestClass]
public class BitDataGridDetailE2ETests : BitDataGridE2ETestBase
{
    private const string Url = "/e2e/data-grid/detail";
    private const string Id = "grid-detail";

    private ILocator DetailToggle(string gridId, int row) => Cell(gridId, row, 1).Locator("button");

    private ILocator DetailRows(string gridId) => Page.Locator($"#{gridId} > .bit-dtg-viewport > .bit-dtg-table > .bit-dtg-body > .bit-dtg-detail-row");

    [TestMethod]
    public async Task Detail_TheToggle_ExpandsARowWithItsNestedGrid()
    {
        await OpenAsync(Url);

        var toggle = DetailToggle(Id, 0);
        await Expect(HeaderCell(Id, 1)).ToContainTextAsync("Details");
        await Expect(toggle).ToHaveAccessibleNameAsync("Toggle details");
        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(DetailRows(Id)).ToHaveCountAsync(0);

        await toggle.ClickAsync();

        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(toggle.Locator("span")).ToHaveTextAsync("▾");
        await Expect(DetailRows(Id)).ToHaveCountAsync(1);
        await Expect(DetailRows(Id).First).ToHaveAttributeAsync("role", "row");
        await Expect(DetailRows(Id).First.Locator("> .bit-dtg-detail-content")).ToHaveAttributeAsync("aria-colspan", "3");
        await Expect(Text("expanded-items")).ToHaveTextAsync("Acme");
        await Expect(Text("toggle-count")).ToHaveTextAsync("1");
        await Expect(Text("toggle-log")).ToHaveTextAsync("Acme:True");

        // The detail row sits right under its row, and holds a grid of its own.
        var nested = "nested-Acme";
        await Expect(Table(nested)).ToHaveAccessibleNameAsync("Products of Acme");
        await ExpectColumnAsync(nested, 1, "Laptop", "Lamp", "Cookbook");
        Assert.IsTrue((await BoxAsync(DetailRows(Id).First)).Y > (await BoxAsync(DataRow(Id, 0))).Y);
        Assert.IsTrue((await BoxAsync(DetailRows(Id).First)).Y < (await BoxAsync(DataRow(Id, 1))).Y);
        // The parent's rows stay its own: the nested rows are not counted among them.
        await Expect(DataRows(Id)).ToHaveCountAsync(3);

        await toggle.ClickAsync();
        await Expect(DetailRows(Id)).ToHaveCountAsync(0);
        await Expect(Text("toggle-log")).ToHaveTextAsync("Acme:False");
        await Expect(Text("expanded-items")).ToHaveTextAsync("");

        // A click on the row itself toggles nothing without ExpandDetailOnRowClick.
        await Cell(Id, 1, 2).ClickAsync();
        await Expect(DetailRows(Id)).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Detail_TheBoundExpandedItems_DriveTheDetailRowsFromThePage()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-set-expanded").ClickAsync();

        await Expect(DetailToggle(Id, 1)).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(DetailRows(Id)).ToHaveCountAsync(1);
        await ExpectColumnAsync("nested-Globex", 1, "Novel", "Drone");
        // A change made by the page is not reported back as a toggle.
        await Expect(Text("toggle-count")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Detail_ExpandAllAndCollapseAll_ToggleEveryRowAndReportEach()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-expand-details").ClickAsync();
        await Expect(DetailRows(Id)).ToHaveCountAsync(3);
        await Expect(Text("expanded-items")).ToHaveTextAsync("Acme,Globex,Initech");
        await Expect(Text("toggle-count")).ToHaveTextAsync("3");

        await Page.Locator("#btn-collapse-details").ClickAsync();
        await Expect(DetailRows(Id)).ToHaveCountAsync(0);
        await Expect(Text("toggle-count")).ToHaveTextAsync("6");
        await Expect(Text("expanded-items")).ToHaveTextAsync("");
    }

    [TestMethod]
    public async Task Detail_ToggleDetailAsyncAndIsDetailExpanded_WorkFromCode()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-read-detail").ClickAsync();
        await Expect(Text("detail-read")).ToHaveTextAsync("collapsed");

        await Page.Locator("#btn-toggle-api").ClickAsync();
        await Expect(DetailToggle(Id, 2)).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Text("toggle-log")).ToHaveTextAsync("Initech:True");

        await Page.Locator("#btn-read-detail").ClickAsync();
        await Expect(Text("detail-read")).ToHaveTextAsync("expanded");
    }

    [TestMethod]
    public async Task RowClick_WithoutTheToggleColumn_TheRowIsTheToggleForThePointerAndTheKeyboard()
    {
        await OpenAsync(Url);

        const string id = "grid-detail-rowclick";
        await Expect(HeaderCells(id)).ToHaveCountAsync(2);
        var row = DataRow(id, 0);
        await Expect(row).ToHaveClassAsync(HasClass("bit-dtg-row-expandable"));
        await Expect(row).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(row).ToHaveAttributeAsync("tabindex", "0");
        await Expect(row).ToHaveCSSAsync("cursor", "pointer");

        await Cell(id, 0, 1).ClickAsync();
        await Expect(row).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(DetailRows(id).Locator(".detail-text")).ToHaveTextAsync("Detail of Acme");

        await DataRow(id, 1).FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(DataRow(id, 1)).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(DetailRows(id)).ToHaveCountAsync(2);

        await Page.Keyboard.PressAsync(" ");
        await Expect(DataRow(id, 1)).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(DetailRows(id)).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task RowClick_WithSingleSelection_SelectsAndTogglesTheRow()
    {
        await OpenAsync(Url);

        const string id = "grid-detail-select";
        await Cell(id, 1, 2).ClickAsync();

        await Expect(DataRow(id, 1)).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(DataRow(id, 1)).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Text("detail-selected")).ToHaveTextAsync("Globex");

        await Cell(id, 1, 2).ClickAsync();
        await Expect(DataRow(id, 1)).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(DataRow(id, 1)).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [TestMethod]
    public async Task Navigation_EnterOnACell_TogglesTheDetailAndTheToggleLeavesTheTabOrder()
    {
        await OpenAsync(Url);

        const string id = "grid-detail-nav";
        await Expect(DetailToggle(id, 0)).ToHaveAttributeAsync("tabindex", "-1");

        await Cell(id, 0, 2).ClickAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(DetailToggle(id, 0)).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(DetailRows(id)).ToHaveCountAsync(1);

        await Page.Keyboard.PressAsync("Enter");
        await Expect(DetailRows(id)).ToHaveCountAsync(0);
        await ExpectFocusedAsync(Cell(id, 0, 2));
    }

    [TestMethod]
    public async Task Detail_ExpandCollapseAndSetDetailExpandedAsync_DriveOneRowFromCode()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-expand-acme").ClickAsync();
        await Expect(DetailToggle(Id, 0)).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Text("toggle-log")).ToHaveTextAsync("Acme:True");

        // Expanding an expanded row is no change, and reports none.
        await Page.Locator("#btn-expand-acme").ClickAsync();
        await Expect(Text("toggle-count")).ToHaveTextAsync("1");

        await Page.Locator("#btn-set-globex").ClickAsync();
        await Expect(DetailToggle(Id, 1)).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Text("expanded-items")).ToHaveTextAsync("Acme,Globex");

        await Page.Locator("#btn-collapse-acme").ClickAsync();
        await Expect(DetailToggle(Id, 0)).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Text("toggle-log")).ToHaveTextAsync("Acme:False");
        await Expect(DetailRows(Id)).ToHaveCountAsync(1);
        await Expect(Text("toggle-count")).ToHaveTextAsync("3");
    }
}
