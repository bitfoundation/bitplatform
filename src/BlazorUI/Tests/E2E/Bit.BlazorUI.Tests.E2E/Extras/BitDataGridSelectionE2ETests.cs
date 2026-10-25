using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// BitDataGrid's selection, keyboard cell navigation and clipboard on /e2e/data-grid/selection: Single selection by a
/// row click and by Space (Single turns cell navigation on), Multiple selection by the checkbox column, the select-all
/// box and its indeterminate state, Shift ranges, IsRowSelectionDisabled, RowHeader naming the checkboxes, the two-way
/// SelectedItems from both sides, SelectAllAsync / ClearSelectionAsync, the APG grid keyboard (one tab stop with a
/// roving tabindex, the arrows, Home/End, Ctrl+Home/End, PageUp/PageDown, the focus ring, the cancelled scrolling
/// defaults), Space / Shift+Space / Ctrl+A, and Ctrl+C / CopyToClipboardAsync writing tab-separated text.
/// </summary>
[TestClass]
public class BitDataGridSelectionE2ETests : BitDataGridE2ETestBase
{
    private const string Url = "/e2e/data-grid/selection";

    // A clipboard of the page's own, so what the grid writes can be read back in every engine, permission or not.
    private const string ClipboardStub = """
        Object.defineProperty(navigator, 'clipboard', {
            configurable: true,
            value: {
                writeText: text => { (window.__dtgCopied ||= []).push(text); return Promise.resolve(); },
                readText: () => Promise.resolve((window.__dtgCopied || []).at(-1) ?? '')
            }
        });
        """;

    private Task<string[]> CopiedAsync() => Page.EvaluateAsync<string[]>("() => window.__dtgCopied ?? []");

    [TestMethod]
    public async Task Single_ARowClick_SelectsThatRowAlone()
    {
        await OpenAsync(Url);

        const string id = "grid-single";
        await Expect(Table(id)).Not.ToHaveAttributeAsync("aria-multiselectable", new Regex(".*"));
        await Expect(HeaderRow(id).Locator("input[type=checkbox]")).ToHaveCountAsync(0);
        await Expect(DataRow(id, 1)).ToHaveAttributeAsync("aria-selected", "false");

        await Cell(id, 1, 2).ClickAsync();
        await Expect(DataRow(id, 1)).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(DataRow(id, 1)).ToHaveClassAsync(HasClass("bit-dtg-selected"));
        await Expect(Text("single-selected")).ToHaveTextAsync("Novel");
        await Expect(Text("single-count")).ToHaveTextAsync("1");
        // The selection is a wash laid over the cell, which an unselected row does not have.
        await Expect(Cell(id, 1, 1)).ToHaveCSSAsync("background-image", new Regex("gradient"));
        await Expect(Cell(id, 1, 1)).Not.ToHaveCSSAsync("background-image", new Regex(@"rgba\(0, 0, 0, 0\)"));
        await Expect(Cell(id, 0, 1)).ToHaveCSSAsync("background-image", new Regex(@"rgba\(0, 0, 0, 0\)"));

        await Cell(id, 3, 1).ClickAsync();
        await Expect(DataRow(id, 3)).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(DataRow(id, 1)).ToHaveAttributeAsync("aria-selected", "false");
        await Expect(Text("single-selected")).ToHaveTextAsync("Lamp");
        await Expect(Page.Locator("#grid-single .bit-dtg-selected")).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task Single_ARowWhoseSelectionIsDisabled_CannotBeSelected()
    {
        await OpenAsync(Url);

        const string id = "grid-single";
        await Cell(id, 0, 2).ClickAsync();
        await Expect(Text("single-selected")).ToHaveTextAsync("Laptop");

        await Cell(id, 2, 2).ClickAsync();
        await Expect(DataRow(id, 2)).ToHaveAttributeAsync("aria-selected", "false");
        await Expect(DataRow(id, 0)).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Text("single-selected")).ToHaveTextAsync("Laptop");
    }

    [TestMethod]
    public async Task Single_SpaceOnAFocusedCell_SelectsItsRow()
    {
        await OpenAsync(Url);

        const string id = "grid-single";
        // Single selection makes the cells navigable, so the keyboard has a way to select.
        await Expect(Cell(id, 0, 1)).ToHaveAttributeAsync("tabindex", "0");
        await Expect(Cell(id, 1, 1)).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(Cell(id, 0, 2)).ToHaveAttributeAsync("role", "rowheader");

        await Cell(id, 0, 1).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await ExpectFocusedAsync(Cell(id, 1, 1));
        await Page.Keyboard.PressAsync(" ");

        await Expect(DataRow(id, 1)).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Text("single-selected")).ToHaveTextAsync("Novel");
    }

    [TestMethod]
    public async Task Multiple_TheCheckboxes_SelectRowsAndAreNamedByTheRowHeader()
    {
        await OpenAsync(Url);

        const string id = "grid-multi";
        await Expect(Table(id)).ToHaveAttributeAsync("aria-multiselectable", "true");
        await Expect(HeaderCell(id, 1).Locator("input[type=checkbox]")).ToHaveAccessibleNameAsync("Select all");
        await Expect(Cell(id, 0, 1).Locator("input")).ToHaveAccessibleNameAsync("Select row: Laptop");
        await Expect(Cell(id, 0, 3)).ToHaveAttributeAsync("role", "rowheader");

        await Cell(id, 0, 1).Locator("input").CheckAsync();
        await Expect(DataRow(id, 0)).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Text("multi-selected")).ToHaveTextAsync("Laptop");

        await Cell(id, 3, 1).Locator("input").CheckAsync();
        await Expect(Text("multi-selected")).ToHaveTextAsync("Laptop,Lamp");

        // A click on the row itself selects nothing in Multiple mode.
        await Cell(id, 1, 3).ClickAsync();
        await Expect(DataRow(id, 1)).ToHaveAttributeAsync("aria-selected", "false");

        await Cell(id, 0, 1).Locator("input").UncheckAsync();
        await Expect(Text("multi-selected")).ToHaveTextAsync("Lamp");
        await Expect(DataRow(id, 0)).ToHaveAttributeAsync("aria-selected", "false");
    }

    [TestMethod]
    public async Task Multiple_ARowWhoseSelectionIsDisabled_HasADisabledCheckbox()
    {
        await OpenAsync(Url);

        var checkbox = Cell("grid-multi", 2, 1).Locator("input");
        await Expect(checkbox).ToBeDisabledAsync();
        await Expect(checkbox).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Cell("grid-multi", 1, 1).Locator("input")).ToBeEnabledAsync();
        await Expect(Cell("grid-multi", 1, 1).Locator("input")).Not.ToHaveAttributeAsync("aria-disabled", new Regex(".*"));
    }

    [TestMethod]
    public async Task Multiple_SelectAll_SelectsTheSelectableRowsOfThePageAndShowsTheMixedState()
    {
        await OpenAsync(Url);

        const string id = "grid-multi";
        var selectAll = HeaderCell(id, 1).Locator("input[type=checkbox]");

        await Cell(id, 0, 1).Locator("input").CheckAsync();
        await Expect(selectAll).Not.ToBeCheckedAsync();
        await WaitForJsAsync("() => document.querySelector('#grid-multi .bit-dtg-header-row input[type=checkbox]').indeterminate === true");

        await selectAll.CheckAsync();
        await Expect(Text("multi-selected")).ToHaveTextAsync("Laptop,Novel,Lamp,Drone,Camera");
        await WaitForJsAsync("() => document.querySelector('#grid-multi .bit-dtg-header-row input[type=checkbox]').indeterminate === false");
        await Expect(selectAll).ToBeCheckedAsync();
        await Expect(LiveRegion(id)).ToContainTextAsync("5 rows selected");
        await Expect(DataRow(id, 2)).ToHaveAttributeAsync("aria-selected", "false");

        // The next page is not part of it.
        await Pagers(id).Locator("button[aria-label='Next page']").ClickAsync();
        await Expect(Cell(id, 0, 3)).ToHaveTextAsync("Cookbook");
        await Expect(selectAll).Not.ToBeCheckedAsync();
        await Pagers(id).Locator("button[aria-label='Previous page']").ClickAsync();
        await Expect(Cell(id, 0, 3)).ToHaveTextAsync("Laptop");
        await Expect(selectAll).ToBeCheckedAsync();

        await selectAll.UncheckAsync();
        await Expect(Text("multi-selected")).ToHaveTextAsync("");
        await Expect(LiveRegion(id)).ToContainTextAsync("Selection cleared");
    }

    [TestMethod]
    public async Task Multiple_AShiftClick_SelectsTheRangeFromTheLastClickedRow()
    {
        await OpenAsync(Url);

        const string id = "grid-multi";
        await Cell(id, 0, 1).Locator("input").ClickAsync();
        await Cell(id, 4, 1).Locator("input").ClickAsync(new() { Modifiers = [KeyboardModifier.Shift] });

        // Jacket sits in the range but cannot be selected.
        await Expect(Text("multi-selected")).ToHaveTextAsync("Laptop,Novel,Lamp,Drone");
        await Expect(DataRow(id, 2)).ToHaveAttributeAsync("aria-selected", "false");
        await Expect(DataRow(id, 5)).ToHaveAttributeAsync("aria-selected", "false");
    }

    [TestMethod]
    public async Task Multiple_TheBoundSelectionAndTheApi_DriveTheCheckboxesFromThePage()
    {
        await OpenAsync(Url);

        const string id = "grid-multi";

        await Page.Locator("#btn-set-selection").ClickAsync();
        await Expect(Cell(id, 1, 1).Locator("input")).ToBeCheckedAsync();
        await Expect(Cell(id, 3, 1).Locator("input")).ToBeCheckedAsync();
        await Expect(Cell(id, 0, 1).Locator("input")).Not.ToBeCheckedAsync();
        await Expect(DataRow(id, 1)).ToHaveAttributeAsync("aria-selected", "true");

        // SelectAllAsync takes every selectable row of the view, the other page included.
        await Page.Locator("#btn-select-all-api").ClickAsync();
        await Expect(Text("multi-selected")).ToHaveTextAsync("Laptop,Novel,Lamp,Drone,Camera,Sneaker,Mug,Puzzle");
        await Expect(LiveRegion(id)).ToContainTextAsync("8 rows selected");

        await Page.Locator("#btn-clear-selection-api").ClickAsync();
        await Expect(Text("multi-selected")).ToHaveTextAsync("");
        await Expect(Page.Locator("#grid-multi .bit-dtg-body input:checked")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Keyboard_SpaceShiftSpaceAndCtrlA_SelectFromTheFocusedCell()
    {
        await OpenAsync(Url);

        const string id = "grid-key-select";
        // The checkboxes leave the tab order: the cells are the grid's one tab stop.
        await Expect(Cell(id, 0, 1).Locator("input")).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(Grid(id)).ToHaveAttributeAsync("data-bit-dtg-select-all", "true");

        await Cell(id, 0, 3).ClickAsync();
        await Page.Keyboard.PressAsync(" ");
        await Expect(Text("key-selected")).ToHaveTextAsync("Laptop");

        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.Keyboard.PressAsync("ArrowDown");
        await ExpectFocusedAsync(Cell(id, 2, 3));
        await Page.Keyboard.PressAsync("Shift+Space");
        await Expect(Text("key-selected")).ToHaveTextAsync("Laptop,Novel,Jacket");

        await Page.Keyboard.PressAsync("Space");
        await Expect(Text("key-selected")).ToHaveTextAsync("Laptop,Novel");

        await Page.Keyboard.PressAsync("ControlOrMeta+a");
        await Expect(Text("key-selected")).ToHaveTextAsync("Laptop,Novel,Jacket,Lamp,Drone,Camera,Cookbook,Sneaker,Mug,Puzzle");
        await Expect(LiveRegion(id)).ToContainTextAsync("10 rows selected");
        // The grid took Ctrl+A over: the document's own select-all painted nothing.
        Assert.AreEqual("", await Page.EvaluateAsync<string>("() => window.getSelection().toString()"));
    }

    [TestMethod]
    public async Task Clipboard_CtrlC_CopiesTheSelectedRowsAsTabSeparatedText()
    {
        await Page.AddInitScriptAsync(ClipboardStub);
        await OpenAsync(Url);

        const string id = "grid-key-select";
        await Expect(Grid(id)).ToHaveAttributeAsync("data-bit-dtg-copy", "true");

        await Cell(id, 0, 1).Locator("input").CheckAsync();
        await Cell(id, 3, 1).Locator("input").CheckAsync();
        await Cell(id, 5, 3).ClickAsync();
        await Page.Keyboard.PressAsync("ControlOrMeta+c");

        await Expect(LiveRegion(id)).ToContainTextAsync("2 rows copied to the clipboard");
        var copied = await CopiedAsync();
        Assert.AreEqual(1, copied.Length);
        // The header line names the exported columns only: the template column has no value to write.
        Assert.AreEqual("ID\tName\tStock\r\n1\tLaptop\t5\r\n4\tLamp\t30\r\n", copied[0]);
    }

    [TestMethod]
    public async Task Clipboard_WithNothingSelected_CopiesTheFocusedRowAndTheApiReportsTheCount()
    {
        await Page.AddInitScriptAsync(ClipboardStub);
        await OpenAsync(Url);

        const string id = "grid-key-select";
        await Cell(id, 1, 2).ClickAsync();
        await Page.Keyboard.PressAsync("ControlOrMeta+c");

        await WaitForJsAsync("() => (window.__dtgCopied ?? []).length === 1");
        Assert.AreEqual("ID\tName\tStock\r\n2\tNovel\t120\r\n", (await CopiedAsync())[0]);

        // CopyToClipboardAsync() copies the selection only: none yet, so nothing.
        await Page.Locator("#btn-copy-api").ClickAsync();
        await Expect(Text("copy-result")).ToHaveTextAsync("copied 0");

        await Cell(id, 8, 1).Locator("input").CheckAsync();
        await Page.Locator("#btn-copy-api").ClickAsync();
        await Expect(Text("copy-result")).ToHaveTextAsync("copied 1");
        Assert.AreEqual("ID\tName\tStock\r\n9\tMug\t200\r\n", (await CopiedAsync())[^1]);
    }

    [TestMethod]
    public async Task Clipboard_AGridWithoutClipboardCopy_LeavesCtrlCToTheBrowser()
    {
        await Page.AddInitScriptAsync(ClipboardStub);
        await OpenAsync(Url);

        await Expect(Grid("grid-no-copy")).Not.ToHaveAttributeAsync("data-bit-dtg-copy", new Regex(".*"));
        await Expect(Grid("grid-no-copy")).Not.ToHaveAttributeAsync("data-bit-dtg-select-all", new Regex(".*"));

        await Page.EvaluateAsync("() => window.addEventListener('keydown', e => window.__dtgPrevented = e.defaultPrevented)");
        await Cell("grid-no-copy", 0, 2).ClickAsync();
        await Page.Keyboard.PressAsync("ControlOrMeta+c");
        Assert.IsFalse(await Page.EvaluateAsync<bool>("() => window.__dtgPrevented"), "Ctrl+C was taken from the browser.");
        await Page.Keyboard.PressAsync("ArrowDown");
        await ExpectFocusedAsync(Cell("grid-no-copy", 1, 2));

        Assert.AreEqual(0, (await CopiedAsync()).Length);
    }

    [TestMethod]
    public async Task Navigation_TheGrid_IsOneTabStopThatLandsOnTheFirstCell()
    {
        await OpenAsync(Url);

        const string id = "grid-nav";
        await Expect(Page.Locator("#grid-nav [role=gridcell][tabindex='0']")).ToHaveCountAsync(1);
        await Expect(Cell(id, 0, 1)).ToHaveAttributeAsync("tabindex", "0");

        await Page.Locator("#btn-before").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        if (IsFirefox)
        {
            // Firefox makes every scroll container a tab stop, the grid's viewport included: one more Tab gets in.
            await ExpectFocusedAsync(Viewport(id));
            await Page.Keyboard.PressAsync("Tab");
        }
        await ExpectFocusedAsync(Cell(id, 0, 1));

        await Page.Keyboard.PressAsync("ArrowRight");
        await ExpectFocusedAsync(Cell(id, 0, 2));
        await Expect(Cell(id, 0, 2)).ToHaveAttributeAsync("tabindex", "0");
        await Expect(Cell(id, 0, 1)).ToHaveAttributeAsync("tabindex", "-1");

        await Page.Keyboard.PressAsync("Tab");
        await ExpectFocusedAsync(Page.Locator("#btn-after"));

        // Coming back lands on the cell the focus left.
        await Page.Keyboard.PressAsync("Shift+Tab");
        await ExpectFocusedAsync(Cell(id, 0, 2));
    }

    [TestMethod]
    public async Task Navigation_TheArrowKeys_MoveCellByCellAndStopAtTheEdges()
    {
        await OpenAsync(Url);

        const string id = "grid-nav";
        await Page.EvaluateAsync("() => window.addEventListener('keydown', e => window.__dtgPrevented = e.defaultPrevented)");
        await Cell(id, 0, 1).ClickAsync();

        await Page.Keyboard.PressAsync("ArrowUp");
        await ExpectFocusedAsync(Cell(id, 0, 1));
        await Page.Keyboard.PressAsync("ArrowLeft");
        await ExpectFocusedAsync(Cell(id, 0, 1));

        await Page.Keyboard.PressAsync("ArrowDown");
        await ExpectFocusedAsync(Cell(id, 1, 1));
        Assert.IsTrue(await Page.EvaluateAsync<bool>("() => window.__dtgPrevented"), "The arrow key's scrolling default was not cancelled.");

        await Page.Keyboard.PressAsync("ArrowRight");
        await ExpectFocusedAsync(Cell(id, 1, 2));
        await Page.Keyboard.PressAsync("ArrowRight");
        await Page.Keyboard.PressAsync("ArrowRight");
        await Page.Keyboard.PressAsync("ArrowRight");
        await ExpectFocusedAsync(Cell(id, 1, 4));

        await Page.Keyboard.PressAsync("ArrowUp");
        await ExpectFocusedAsync(Cell(id, 0, 4));

        // The focused cell shows the focus ring.
        await Expect(Cell(id, 0, 4)).ToHaveCSSAsync("outline-style", "solid");
        Assert.AreNotEqual("0px", await StyleOfAsync(Cell(id, 0, 4), "outline-width"));
    }

    [TestMethod]
    public async Task Navigation_HomeEndAndCtrlHomeEnd_JumpToTheEdges()
    {
        await OpenAsync(Url);

        const string id = "grid-nav";
        await Cell(id, 2, 2).ClickAsync();

        await Page.Keyboard.PressAsync("End");
        await ExpectFocusedAsync(Cell(id, 2, 4));
        await Page.Keyboard.PressAsync("Home");
        await ExpectFocusedAsync(Cell(id, 2, 1));

        await Page.Keyboard.PressAsync("Control+End");
        var last = DataRows(id).Last.Locator("> [aria-colindex='4']");
        await ExpectFocusedAsync(last);
        await Expect(DataRows(id).Last).ToHaveAttributeAsync("aria-rowindex", "31");
        await Expect(last).ToBeInViewportAsync();

        await Page.Keyboard.PressAsync("Control+Home");
        await ExpectFocusedAsync(Cell(id, 0, 1));
        await Expect(Cell(id, 0, 1)).ToBeInViewportAsync();
    }

    [TestMethod]
    public async Task Navigation_PageDownAndPageUp_MoveTenRowsAndKeepTheCellOutFromUnderTheHeader()
    {
        await OpenAsync(Url);

        const string id = "grid-nav";
        await Cell(id, 0, 2).ClickAsync();

        await Page.Keyboard.PressAsync("PageDown");
        await ExpectFocusedAsync(Cell(id, 10, 2));
        await Page.Keyboard.PressAsync("PageDown");
        await ExpectFocusedAsync(Cell(id, 20, 2));
        await Page.Keyboard.PressAsync("PageDown");
        await ExpectFocusedAsync(Cell(id, 29, 2));

        await Page.Keyboard.PressAsync("PageUp");
        await ExpectFocusedAsync(Cell(id, 19, 2));
        await NextFramesAsync();

        // The cell scrolled into view stops below the sticky header rather than under it.
        var header = await BoxAsync(HeaderRow(id));
        var cell = await BoxAsync(Cell(id, 19, 2));
        var view = await BoxAsync(Viewport(id));
        Assert.IsTrue(cell.Y >= header.Y + header.Height - 1, $"The focused cell (y {cell.Y}) is under the header (bottom {header.Y + header.Height}).");
        Assert.IsTrue(cell.Y + cell.Height <= view.Y + view.Height + 1, "The focused cell is below the viewport.");
    }

    [TestMethod]
    public async Task KeyField_RowsReloadedAsNewInstances_StaySelected()
    {
        await OpenAsync(Url);

        const string id = "grid-multi";
        await Cell(id, 1, 1).Locator("input").CheckAsync();
        await Cell(id, 3, 1).Locator("input").CheckAsync();
        await Expect(Text("multi-selected")).ToHaveTextAsync("Novel,Lamp");

        await Page.Locator("#btn-reload").ClickAsync();

        await Expect(Cell(id, 1, 1).Locator("input")).ToBeCheckedAsync();
        await Expect(Cell(id, 3, 1).Locator("input")).ToBeCheckedAsync();
        await Expect(DataRow(id, 1)).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Cell(id, 0, 1).Locator("input")).Not.ToBeCheckedAsync();
    }

    [TestMethod]
    public async Task Clipboard_CopyToClipboardAsyncWithAFallbackRow_CopiesItWhenNothingIsSelected()
    {
        await Page.AddInitScriptAsync(ClipboardStub);
        await OpenAsync(Url);

        await Page.Locator("#btn-copy-fallback").ClickAsync();
        await Expect(Text("copy-result")).ToHaveTextAsync("copied 1");
        Assert.AreEqual("ID\tName\tStock\r\n3\tJacket\t0\r\n", (await CopiedAsync())[^1]);

        // A selection wins over the fallback row.
        await Cell("grid-key-select", 0, 1).Locator("input").CheckAsync();
        await Page.Locator("#btn-copy-fallback").ClickAsync();
        await WaitForJsAsync("() => (window.__dtgCopied ?? []).length === 2");
        Assert.AreEqual("ID\tName\tStock\r\n1\tLaptop\t5\r\n", (await CopiedAsync())[^1]);
    }
}
