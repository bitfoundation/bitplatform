using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// BitDataGrid's inline editing on /e2e/data-grid/editing. Row mode: the command column, the editor of each data type,
/// where the focus goes, Save writing the buffered values (OnRowSave), Cancel and Escape restoring them (OnRowCancel),
/// Enter committing from a navigable cell, Validate blocking Save with an accessible error, the toolbar's Add with
/// NewItemFactory (OnRowCreate), Delete (OnRowDelete), BeginEdit from OnRowDoubleClick and the editing API
/// (BeginEditAsync, CommitEditAsync, CancelEditAsync, AddNewRowAsync, DeleteRowAsync, EditingItem, EditingColumnId).
/// Cell mode: Enter, F2, a double-click and typing open one cell, Backspace opens it empty, Enter, Tab / Shift+Tab and
/// leaving it commit, Escape cancels, Delete deletes the row, a validation error keeps it open. An EditTemplate binding
/// straight to the item and restored on Escape, and an Escape inside a modal cancelling the edit before the modal closes.
/// </summary>
[TestClass]
public class BitDataGridEditingE2ETests : BitDataGridE2ETestBase
{
    private const string Url = "/e2e/data-grid/editing";
    private const string RowGrid = "grid-row-edit";
    private const string CellGrid = "grid-cell-edit";

    private ILocator CommandButton(string gridId, int row, string name)
        => DataRow(gridId, row).Locator("> .bit-dtg-cell-command").GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    private ILocator Editor(string gridId, int row, int ariaColIndex) => Cell(gridId, row, ariaColIndex).Locator(".bit-dtg-editor");

    private Task<bool> IsAllSelectedAsync(ILocator input)
        => input.EvaluateAsync<bool>("el => el.selectionStart === 0 && el.selectionEnd === el.value.length && el.value.length > 0");

    [TestMethod]
    public async Task RowEdit_TheEditButton_OpensAnEditorOfEachDataTypeAndFocusesTheFirst()
    {
        await OpenAsync(Url);

        await Expect(HeaderCell(RowGrid, 7)).ToHaveTextAsync("Actions");
        await Expect(DataRow(RowGrid, 0).Locator("> .bit-dtg-cell-command button")).ToHaveTextAsync(["Edit", "Delete"]);

        await CommandButton(RowGrid, 0, "Edit").ClickAsync();

        await Expect(DataRow(RowGrid, 0)).ToHaveClassAsync(HasClass("bit-dtg-editing"));
        await Expect(DataRow(RowGrid, 0).Locator("> .bit-dtg-cell-command button")).ToHaveTextAsync(["Save", "Cancel"]);
        await Expect(Editor(RowGrid, 0, 1)).ToHaveCountAsync(0);
        await Expect(Editor(RowGrid, 0, 2)).ToHaveAttributeAsync("type", "text");
        await Expect(Editor(RowGrid, 0, 2)).ToHaveValueAsync("Laptop");
        await Expect(Editor(RowGrid, 0, 2)).ToHaveAccessibleNameAsync("Name");
        await Expect(Editor(RowGrid, 0, 3)).ToHaveJSPropertyAsync("tagName", "SELECT");
        await Expect(Editor(RowGrid, 0, 3)).ToHaveValueAsync("Electronics");
        await Expect(Editor(RowGrid, 0, 4)).ToHaveAttributeAsync("type", "number");
        await Expect(Editor(RowGrid, 0, 4)).ToHaveValueAsync("1200");
        await Expect(Editor(RowGrid, 0, 5)).ToHaveAttributeAsync("type", "checkbox");
        await Expect(Editor(RowGrid, 0, 5)).Not.ToBeCheckedAsync();
        await Expect(Editor(RowGrid, 0, 6)).ToHaveAttributeAsync("type", "datetime-local");
        await Expect(Editor(RowGrid, 0, 6)).ToHaveValueAsync("2023-05-01T00:00");

        // The Edit button went away, so the focus went into the first editor with its text selected for typing over.
        await ExpectFocusedAsync(Editor(RowGrid, 0, 2));
        Assert.IsTrue(await IsAllSelectedAsync(Editor(RowGrid, 0, 2)));

        // The row being edited takes the editing wash.
        await Expect(Cell(RowGrid, 0, 1)).Not.ToHaveCSSAsync("background-image", new Regex(@"rgba\(0, 0, 0, 0\)"));
        // Its cells claim the Escape that cancels the edit.
        await Expect(Cell(RowGrid, 0, 2)).ToHaveAttributeAsync("data-bit-esc", "claim");
        await Expect(Cell(RowGrid, 1, 2)).ToHaveAttributeAsync("data-bit-esc", "claim");
    }

    [TestMethod]
    public async Task RowEdit_Save_WritesTheBufferedValuesAndRaisesOnRowSave()
    {
        await OpenAsync(Url);

        await CommandButton(RowGrid, 0, "Edit").ClickAsync();
        await Editor(RowGrid, 0, 2).FillAsync("Laptop Pro");
        await Editor(RowGrid, 0, 3).SelectOptionAsync("Toys");
        await Editor(RowGrid, 0, 4).FillAsync("999");
        await Editor(RowGrid, 0, 5).CheckAsync();
        await FillDateAsync(Editor(RowGrid, 0, 6), "2024-12-25T10:30");

        // Nothing reaches the row before it is saved.
        await Expect(Text("row-save-count")).ToHaveTextAsync("0");

        await CommandButton(RowGrid, 0, "Save").ClickAsync();

        await Expect(DataRow(RowGrid, 0).Locator("> [role=gridcell]:not(.bit-dtg-cell-command)")).ToHaveTextAsync(["1", "Laptop Pro", "Toys", "999", "True", "2024-12-25"]);
        await Expect(DataRow(RowGrid, 0)).Not.ToHaveClassAsync(HasClass("bit-dtg-editing"));
        await Expect(Text("row-save-count")).ToHaveTextAsync("1");
        await Expect(Text("row-save-arg")).ToHaveTextAsync("1|Laptop Pro|Toys|999|True|2024-12-25");
        await ExpectFocusedAsync(CommandButton(RowGrid, 0, "Edit"));
    }

    [TestMethod]
    public async Task RowEdit_CancelAndEscape_RestoreTheRow()
    {
        await OpenAsync(Url);

        await CommandButton(RowGrid, 1, "Edit").ClickAsync();
        await Editor(RowGrid, 1, 2).FillAsync("Changed");
        await CommandButton(RowGrid, 1, "Cancel").ClickAsync();

        await Expect(Cell(RowGrid, 1, 2)).ToHaveTextAsync("Novel");
        await Expect(Text("row-cancel-count")).ToHaveTextAsync("1");
        await Expect(Text("row-cancel-arg")).ToHaveTextAsync("Novel");
        await ExpectFocusedAsync(CommandButton(RowGrid, 1, "Edit"));

        await CommandButton(RowGrid, 1, "Edit").ClickAsync();
        await Editor(RowGrid, 1, 2).FillAsync("Changed again");
        await Page.Keyboard.PressAsync("Escape");

        await Expect(Cell(RowGrid, 1, 2)).ToHaveTextAsync("Novel");
        await Expect(Text("row-cancel-count")).ToHaveTextAsync("2");
        await Expect(Text("row-save-count")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task RowEdit_EnterOnANavigableCell_OpensTheRowAndEnterInAnEditorCommits()
    {
        await OpenAsync(Url);

        await Cell(RowGrid, 2, 2).ClickAsync();
        await Page.Keyboard.PressAsync("Enter");

        await Expect(DataRow(RowGrid, 2)).ToHaveClassAsync(HasClass("bit-dtg-editing"));
        await ExpectFocusedAsync(Editor(RowGrid, 2, 2));
        await Page.Keyboard.TypeAsync("Coat");
        await Page.Keyboard.PressAsync("Enter");

        await Expect(Cell(RowGrid, 2, 2)).ToHaveTextAsync("Coat");
        await Expect(Text("row-save-arg")).ToHaveTextAsync("3|Coat|Clothing|90|True|2021-11-20");
        await ExpectFocusedAsync(Cell(RowGrid, 2, 2));
    }

    [TestMethod]
    public async Task RowEdit_Validate_BlocksSaveAndDescribesTheError()
    {
        await OpenAsync(Url);

        await CommandButton(RowGrid, 0, "Edit").ClickAsync();
        await Editor(RowGrid, 0, 2).FillAsync("");

        var nameError = Cell(RowGrid, 0, 2).Locator(".bit-dtg-editor-error");
        await Expect(nameError).ToHaveTextAsync("Name is required.");
        await Expect(nameError).ToHaveAttributeAsync("role", "alert");
        await Expect(Editor(RowGrid, 0, 2)).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Editor(RowGrid, 0, 2)).ToHaveAccessibleDescriptionAsync("Name is required.");
        await Expect(CommandButton(RowGrid, 0, "Save")).ToBeDisabledAsync();
        await Expect(CommandButton(RowGrid, 0, "Save")).ToHaveAttributeAsync("aria-disabled", "true");

        await Editor(RowGrid, 0, 4).FillAsync("-5");
        await Expect(Cell(RowGrid, 0, 4).Locator(".bit-dtg-editor-error")).ToHaveTextAsync("Price cannot be negative.");

        await Editor(RowGrid, 0, 2).FillAsync("Fixed");
        await Expect(nameError).ToHaveCountAsync(0);
        await Expect(Editor(RowGrid, 0, 2)).Not.ToHaveAttributeAsync("aria-invalid", new Regex(".*"));
        await Expect(CommandButton(RowGrid, 0, "Save")).ToBeDisabledAsync();

        await Editor(RowGrid, 0, 4).FillAsync("5");
        await Expect(CommandButton(RowGrid, 0, "Save")).ToBeEnabledAsync();
        await CommandButton(RowGrid, 0, "Save").ClickAsync();
        await Expect(Text("row-save-arg")).ToHaveTextAsync("1|Fixed|Electronics|5|False|2023-05-01");
    }

    [TestMethod]
    public async Task RowEdit_TheAddButton_OpensANewRowFromTheFactory()
    {
        await OpenAsync(Url);

        var add = Toolbar(RowGrid).GetByRole(AriaRole.Button, new() { Name = "+ Add" });
        await add.ClickAsync();

        await Expect(Text("row-create-count")).ToHaveTextAsync("1");
        await Expect(Text("row-create-arg")).ToHaveTextAsync("100");
        await Expect(DataRows(RowGrid)).ToHaveCountAsync(6);
        await Expect(DataRow(RowGrid, 0)).ToHaveClassAsync(HasClass("bit-dtg-editing"));
        await Expect(Cell(RowGrid, 0, 1)).ToHaveTextAsync("100");
        await ExpectFocusedAsync(Editor(RowGrid, 0, 2));

        await Page.Keyboard.TypeAsync("Gizmo");
        await Editor(RowGrid, 0, 4).FillAsync("5");
        await CommandButton(RowGrid, 0, "Save").ClickAsync();

        await Expect(Text("row-save-arg")).ToHaveTextAsync("100|Gizmo|Electronics|5|False|2025-01-01");
        await Expect(DataRows(RowGrid)).ToHaveCountAsync(6);
        await Expect(Cell(RowGrid, 5, 2)).ToHaveTextAsync("Gizmo");
    }

    [TestMethod]
    public async Task RowEdit_TheDeleteButton_RaisesOnRowDeleteAndTheRowGoes()
    {
        await OpenAsync(Url);

        await CommandButton(RowGrid, 3, "Delete").ClickAsync();

        await Expect(Text("row-delete-count")).ToHaveTextAsync("1");
        await Expect(Text("row-delete-arg")).ToHaveTextAsync("Lamp");
        await ExpectColumnAsync(RowGrid, 2, "Laptop", "Novel", "Jacket", "Drone");
        await Expect(LiveRegion(RowGrid)).ToContainTextAsync("Row deleted");
    }

    [TestMethod]
    public async Task RowEdit_ARowDoubleClick_BeginsTheEditThroughOnRowDoubleClick()
    {
        await OpenAsync(Url);

        await Cell(RowGrid, 4, 3).DblClickAsync();

        await Expect(DataRow(RowGrid, 4)).ToHaveClassAsync(HasClass("bit-dtg-editing"));
        await Expect(Editor(RowGrid, 4, 2)).ToHaveValueAsync("Drone");
        await Expect(DataRows(RowGrid).And(Page.Locator(".bit-dtg-editing"))).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task RowEdit_TheEditingApi_OpensCommitsCancelsAddsAndDeletes()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-begin-edit-api").ClickAsync();
        await Expect(DataRow(RowGrid, 1)).ToHaveClassAsync(HasClass("bit-dtg-editing"));
        await ExpectFocusedAsync(Editor(RowGrid, 1, 4));
        await Page.Locator("#btn-read-edit").ClickAsync();
        await Expect(Text("row-edit-state")).ToHaveTextAsync("item=Novel;column=none");

        await Editor(RowGrid, 1, 4).FillAsync("20");
        await Page.Locator("#btn-commit-api").ClickAsync();
        await Expect(Text("row-save-arg")).ToHaveTextAsync("2|Novel|Books|20|False|2022-01-15");
        await Page.Locator("#btn-read-edit").ClickAsync();
        await Expect(Text("row-edit-state")).ToHaveTextAsync("item=none;column=none");

        await Page.Locator("#btn-begin-edit-api").ClickAsync();
        await Editor(RowGrid, 1, 4).FillAsync("30");
        await Page.Locator("#btn-cancel-api").ClickAsync();
        await Expect(Cell(RowGrid, 1, 4)).ToHaveTextAsync("20");
        await Expect(Text("row-cancel-arg")).ToHaveTextAsync("Novel");

        await Page.Locator("#btn-add-api").ClickAsync();
        await Expect(DataRows(RowGrid)).ToHaveCountAsync(6);
        await Expect(Text("row-create-arg")).ToHaveTextAsync("100");
        await Page.Locator("#btn-cancel-api").ClickAsync();
        await Expect(DataRows(RowGrid)).ToHaveCountAsync(5);

        await Page.Locator("#btn-delete-api").ClickAsync();
        await Expect(Text("row-delete-arg")).ToHaveTextAsync("Laptop");
        await Expect(DataRows(RowGrid)).ToHaveCountAsync(4);
    }

    [TestMethod]
    public async Task CellEdit_EnterOpensOneCellAndEnterCommitsIt()
    {
        await OpenAsync(Url);

        // A cell commits on its own, so the command column keeps only Delete.
        await Expect(DataRow(CellGrid, 0).Locator("> .bit-dtg-cell-command button")).ToHaveTextAsync(["Delete"]);

        await Cell(CellGrid, 0, 2).ClickAsync();
        await Page.Keyboard.PressAsync("Enter");

        await Expect(Cell(CellGrid, 0, 2)).ToHaveClassAsync(HasClass("bit-dtg-cell-editing"));
        await Expect(Cell(CellGrid, 0, 2)).ToHaveAttributeAsync("data-bit-dtg-edit", "Name");
        await Expect(Editor(CellGrid, 0, 3)).ToHaveCountAsync(0);
        await Expect(DataRow(CellGrid, 0)).Not.ToHaveClassAsync(HasClass("bit-dtg-editing"));
        await ExpectFocusedAsync(Editor(CellGrid, 0, 2));
        Assert.IsTrue(await IsAllSelectedAsync(Editor(CellGrid, 0, 2)));

        await Page.Keyboard.TypeAsync("Tablet");
        await Page.Keyboard.PressAsync("Enter");

        await Expect(Cell(CellGrid, 0, 2)).ToHaveTextAsync("Tablet");
        await Expect(Cell(CellGrid, 0, 2)).Not.ToHaveClassAsync(HasClass("bit-dtg-cell-editing"));
        await Expect(Text("cell-save-count")).ToHaveTextAsync("1");
        await Expect(Text("cell-save-arg")).ToHaveTextAsync("1|Tablet|Electronics|1200|False|2023-05-01");
        await ExpectFocusedAsync(Cell(CellGrid, 0, 2));
    }

    [TestMethod]
    public async Task CellEdit_F2AndADoubleClick_OpenACellAndEscapeCancels()
    {
        await OpenAsync(Url);

        await Cell(CellGrid, 1, 4).ClickAsync();
        await Page.Keyboard.PressAsync("F2");
        await ExpectFocusedAsync(Editor(CellGrid, 1, 4));
        await Expect(Editor(CellGrid, 1, 4)).ToHaveValueAsync("15");
        await Editor(CellGrid, 1, 4).FillAsync("7");
        await Expect(Cell(CellGrid, 1, 4)).ToHaveAttributeAsync("data-bit-esc", "claim");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Cell(CellGrid, 1, 4)).ToHaveTextAsync("15");
        await Expect(Text("cell-cancel-count")).ToHaveTextAsync("1");
        await ExpectFocusedAsync(Cell(CellGrid, 1, 4));
        await Expect(Cell(CellGrid, 1, 4)).Not.ToHaveAttributeAsync("data-bit-esc", new Regex(".*"));

        await Cell(CellGrid, 1, 3).DblClickAsync();
        await Expect(Editor(CellGrid, 1, 3)).ToHaveJSPropertyAsync("tagName", "SELECT");
        await ExpectFocusedAsync(Editor(CellGrid, 1, 3));
        await Editor(CellGrid, 1, 3).SelectOptionAsync("Home");
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Cell(CellGrid, 1, 3)).ToHaveTextAsync("Books");
    }

    [TestMethod]
    public async Task CellEdit_TypingIntoACell_OpensItWithWhatWasTyped()
    {
        await OpenAsync(Url);

        await Cell(CellGrid, 1, 2).ClickAsync();
        await Page.Keyboard.TypeAsync("Zed");
        await Expect(Editor(CellGrid, 1, 2)).ToHaveValueAsync("Zed");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Cell(CellGrid, 1, 2)).ToHaveTextAsync("Zed");

        await Cell(CellGrid, 2, 4).ClickAsync();
        await Page.Keyboard.TypeAsync("42");
        await Expect(Editor(CellGrid, 2, 4)).ToHaveValueAsync("42");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Cell(CellGrid, 2, 4)).ToHaveTextAsync("42");
        await Expect(Text("cell-save-arg")).ToHaveTextAsync("3|Jacket|Clothing|42|True|2021-11-20");
    }

    [TestMethod]
    public async Task CellEdit_Backspace_OpensTheCellEmptyAndValidateKeepsItOpen()
    {
        await OpenAsync(Url);

        await Cell(CellGrid, 3, 2).ClickAsync();
        await Page.Keyboard.PressAsync("Backspace");

        await Expect(Editor(CellGrid, 3, 2)).ToHaveValueAsync("");
        await Expect(Cell(CellGrid, 3, 2).Locator(".bit-dtg-editor-error")).ToHaveTextAsync("Name is required.");

        // Enter cannot commit an invalid value: the cell stays open.
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Cell(CellGrid, 3, 2)).ToHaveClassAsync(HasClass("bit-dtg-cell-editing"));
        await Expect(Text("cell-save-count")).ToHaveTextAsync("0");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Cell(CellGrid, 3, 2)).ToHaveTextAsync("Lamp");
    }

    [TestMethod]
    public async Task CellEdit_TabAndShiftTab_CommitAndOpenTheNextEditableCell()
    {
        await OpenAsync(Url);

        await Cell(CellGrid, 0, 2).ClickAsync();
        await Page.Keyboard.PressAsync("Enter");
        // A Tab the browser sees before the editor has rendered is still the cell's own, and moves the focus out of the
        // grid: the editor is waited for, as a user waits to see it open.
        await ExpectFocusedAsync(Editor(CellGrid, 0, 2));
        await Page.Keyboard.TypeAsync("A1");
        await Page.Keyboard.PressAsync("Tab");

        await Expect(Cell(CellGrid, 0, 2)).ToHaveTextAsync("A1");
        await Expect(Text("cell-save-count")).ToHaveTextAsync("1");
        await Expect(Cell(CellGrid, 0, 3)).ToHaveClassAsync(HasClass("bit-dtg-cell-editing"));
        await ExpectFocusedAsync(Editor(CellGrid, 0, 3));

        await Page.Keyboard.PressAsync("Shift+Tab");
        await Expect(Cell(CellGrid, 0, 2)).ToHaveClassAsync(HasClass("bit-dtg-cell-editing"));
        await ExpectFocusedAsync(Editor(CellGrid, 0, 2));
        await Expect(Text("cell-save-count")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task CellEdit_LeavingTheCell_CommitsIt()
    {
        await OpenAsync(Url);

        await Cell(CellGrid, 4, 2).ClickAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Page.Keyboard.TypeAsync("Blurred");
        await Page.Locator("h1").ClickAsync();

        await Expect(Cell(CellGrid, 4, 2)).ToHaveTextAsync("Blurred");
        await Expect(Cell(CellGrid, 4, 2)).Not.ToHaveClassAsync(HasClass("bit-dtg-cell-editing"));
        await Expect(Text("cell-save-arg")).ToHaveTextAsync("5|Blurred|Toys|299|False|2024-03-05");
    }

    [TestMethod]
    public async Task CellEdit_TheDeleteKey_DeletesTheFocusedRowAndKeepsTheFocusInTheGrid()
    {
        await OpenAsync(Url);

        await Cell(CellGrid, 3, 2).ClickAsync();
        await Page.Keyboard.PressAsync("Delete");

        await Expect(Text("cell-delete-count")).ToHaveTextAsync("1");
        await ExpectColumnAsync(CellGrid, 2, "Laptop", "Novel", "Jacket", "Drone");
        await ExpectFocusedAsync(Cell(CellGrid, 3, 2));
    }

    [TestMethod]
    public async Task EditTemplate_TheCustomEditor_WritesThroughAndEscapeRestoresTheRow()
    {
        await OpenAsync(Url);

        const string id = "grid-edit-template";
        await CommandButton(id, 0, "Edit").ClickAsync();

        var editor = Cell(id, 0, 2).Locator("input.custom-editor");
        await ExpectFocusedAsync(editor);
        await editor.FillAsync("Custom value");
        await Page.Keyboard.PressAsync("Escape");

        await Expect(Cell(id, 0, 2)).ToHaveTextAsync("Laptop");
        await ExpectFocusedAsync(CommandButton(id, 0, "Edit"));

        await CommandButton(id, 1, "Edit").ClickAsync();
        await Cell(id, 1, 2).Locator("input.custom-editor").FillAsync("Saved through");
        await CommandButton(id, 1, "Save").ClickAsync();
        await Expect(Cell(id, 1, 2)).ToHaveTextAsync("Saved through");
    }

    [TestMethod]
    public async Task Escape_InAModal_CancelsTheEditFirstAndClosesTheModalOnTheNextPress()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-open-modal").ClickAsync();
        await Expect(Grid("grid-modal-edit")).ToBeVisibleAsync();

        await Cell("grid-modal-edit", 0, 2).ClickAsync();
        await Page.Keyboard.PressAsync("Enter");
        await ExpectFocusedAsync(Editor("grid-modal-edit", 0, 2));

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Text("modal-cancel-count")).ToHaveTextAsync("1");
        await Expect(Editor("grid-modal-edit", 0, 2)).ToHaveCountAsync(0);
        await Expect(Text("modal-state")).ToHaveTextAsync("open");
        await Expect(Grid("grid-modal-edit")).ToBeVisibleAsync();
        // The cell has the focus back and no longer claims the key.
        await ExpectFocusedAsync(Cell("grid-modal-edit", 0, 2));
        await Expect(Cell("grid-modal-edit", 0, 2)).Not.ToHaveAttributeAsync("data-bit-esc", new Regex(".*"));

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Text("modal-state")).ToHaveTextAsync("closed");
        await Expect(Grid("grid-modal-edit")).Not.ToBeVisibleAsync();
    }

    private const string TypesGrid = "grid-types";

    private ILocator TypeFilter(string title) => FilterRow(TypesGrid).Locator($"[aria-label='Filter by {title}']");

    [TestMethod]
    public async Task Types_TheFilterRow_GetsTheEditorOfEveryDataType()
    {
        await OpenAsync(Url);

        await Expect(TypeFilter("Text")).ToHaveAttributeAsync("placeholder", "Filter…");
        await Expect(TypeFilter("Number")).ToHaveAttributeAsync("placeholder", "=");
        await Expect(TypeFilter("Number")).ToHaveAttributeAsync("inputmode", "decimal");
        await Expect(TypeFilter("Flag").Locator("option")).ToHaveTextAsync(["All", "True", "False"]);
        await Expect(TypeFilter("Maybe Flag").Locator("option")).ToHaveTextAsync(["All", "True", "False"]);
        foreach (var date in new[] { "Day", "When", "At" })
            await Expect(TypeFilter(date)).ToHaveAttributeAsync("type", "date");
        await Expect(TypeFilter("Color").Locator("option")).ToHaveTextAsync(["All", "Red", "Green", "Blue"]);
        await Expect(TypeFilter("Maybe Color").Locator("option")).ToHaveTextAsync(["All", "Red", "Green", "Blue"]);
        // DataType overrides what the bound type would pick: a number filtered as text.
        await Expect(TypeFilter("Text Number")).ToHaveAttributeAsync("placeholder", "Filter…");
        await Expect(TypeFilter("Text Number")).Not.ToHaveAttributeAsync("inputmode", new Regex(".*"));
    }

    [TestMethod]
    public async Task Types_EachFilter_MatchesTheWayItsDataTypeDoes()
    {
        await OpenAsync(Url);

        async Task Check(ILocator filter, string value, bool select, params string[] texts)
        {
            if (select) await filter.SelectOptionAsync(value);
            else await FillDateAsync(filter, value);
            await ExpectColumnAsync(TypesGrid, 2, texts);
            if (select) await filter.SelectOptionAsync("");
            else await FillDateAsync(filter, "");
            await Expect(DataRows(TypesGrid)).ToHaveCountAsync(3);
        }

        await Check(TypeFilter("Number"), "15", false, "beta");
        await Check(TypeFilter("Day"), "2024-02-03", false, "beta");
        await Check(TypeFilter("When"), "2024-03-04", false, "gamma");
        // A DateTimeOffset matches by its own calendar day, whatever its offset.
        await Check(TypeFilter("At"), "2024-01-02", false, "alpha");
        await Check(TypeFilter("Maybe Flag"), "true", true, "beta");
        await Check(TypeFilter("Maybe Flag"), "false", true, "gamma");
        await Check(TypeFilter("Maybe Color"), "Blue", true, "beta");
        await Check(TypeFilter("Color"), "Blue", true, "gamma");
        // Filtered as text, 12 and 21 contain a 1.
        await Check(TypeFilter("Text Number"), "1", false, "alpha", "beta");
    }

    [TestMethod]
    public async Task Types_TheRowEditors_FollowTheDataTypesAndConvertBack()
    {
        await OpenAsync(Url);

        await CommandButton(TypesGrid, 0, "Edit").ClickAsync();

        await Expect(Editor(TypesGrid, 0, 2)).ToHaveAttributeAsync("type", "text");
        await Expect(Editor(TypesGrid, 0, 3)).ToHaveAttributeAsync("type", "number");
        await Expect(Editor(TypesGrid, 0, 4)).ToHaveAttributeAsync("type", "checkbox");
        await Expect(Editor(TypesGrid, 0, 4)).ToBeCheckedAsync();
        // A nullable bool has three states, so it is a select whose empty option is null.
        await Expect(Editor(TypesGrid, 0, 5)).ToHaveClassAsync(HasClass("bit-dtg-editor-tristate"));
        await Expect(Editor(TypesGrid, 0, 5)).ToHaveValueAsync("");
        await Expect(Editor(TypesGrid, 0, 6)).ToHaveAttributeAsync("type", "date");
        await Expect(Editor(TypesGrid, 0, 6)).ToHaveValueAsync("2024-01-02");
        await Expect(Editor(TypesGrid, 0, 7)).ToHaveAttributeAsync("type", "datetime-local");
        await Expect(Editor(TypesGrid, 0, 7)).ToHaveValueAsync("2024-01-02T10:30");
        await Expect(Editor(TypesGrid, 0, 8)).ToHaveAttributeAsync("type", "datetime-local");
        await Expect(Editor(TypesGrid, 0, 8)).ToHaveValueAsync("2024-01-02T23:00");
        await Expect(Editor(TypesGrid, 0, 9).Locator("option")).ToHaveTextAsync(["Red", "Green", "Blue"]);
        await Expect(Editor(TypesGrid, 0, 10).Locator("option")).ToHaveTextAsync(["", "Red", "Green", "Blue"]);
        await Expect(Editor(TypesGrid, 0, 11)).ToHaveAttributeAsync("type", "text");

        await Editor(TypesGrid, 0, 5).SelectOptionAsync("false");
        await FillDateAsync(Editor(TypesGrid, 0, 6), "2025-06-07");
        await FillDateAsync(Editor(TypesGrid, 0, 7), "2025-06-07T09:15");
        await FillDateAsync(Editor(TypesGrid, 0, 8), "2025-06-07T18:45");
        await Editor(TypesGrid, 0, 10).SelectOptionAsync("Green");

        // A value that does not convert to the column's type is refused with an error.
        await Editor(TypesGrid, 0, 11).FillAsync("abc");
        await Expect(Cell(TypesGrid, 0, 11).Locator(".bit-dtg-editor-error")).ToHaveTextAsync("Invalid value for Text Number.");
        await Expect(CommandButton(TypesGrid, 0, "Save")).ToBeDisabledAsync();
        await Expect(Editor(TypesGrid, 0, 11)).ToHaveValueAsync("abc");
        await Editor(TypesGrid, 0, 11).FillAsync("34");
        await Expect(CommandButton(TypesGrid, 0, "Save")).ToBeEnabledAsync();

        await CommandButton(TypesGrid, 0, "Save").ClickAsync();

        // The offset the value had is kept for the new local time.
        await Expect(Text("typed-save")).ToHaveTextAsync("alpha|5|True|False|2025-06-07|2025-06-07 09:15|2025-06-07 18:45 +03:00|Red|Green|34");
        await Expect(Cell(TypesGrid, 0, 8)).ToHaveTextAsync("2025-06-07 18:45 +03:00");

        // A nullable value can be cleared back to null.
        await CommandButton(TypesGrid, 1, "Edit").ClickAsync();
        await Editor(TypesGrid, 1, 5).SelectOptionAsync("");
        await Editor(TypesGrid, 1, 10).SelectOptionAsync("");
        await CommandButton(TypesGrid, 1, "Save").ClickAsync();
        await Expect(Text("typed-save")).ToHaveTextAsync(new Regex(@"^beta\|15\|False\|null\|.*\|Green\|null\|21$"));
    }

    [TestMethod]
    public async Task CellEdit_EditingItemAndEditingColumnId_NameTheOpenCell()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-read-cell-edit").ClickAsync();
        await Expect(Text("cell-edit-state")).ToHaveTextAsync("item=none;column=none");

        await Cell(CellGrid, 1, 4).ClickAsync();
        await Page.Keyboard.PressAsync("F2");
        await ExpectFocusedAsync(Editor(CellGrid, 1, 4));
        await Page.Locator("#btn-read-cell-edit").ClickAsync();
        await Expect(Text("cell-edit-state")).ToHaveTextAsync("item=Novel;column=Price");
    }

    [TestMethod]
    public async Task Escape_InAModal_ClosesTheColumnChooserFirstAndAModifiedEscapeIsTheModals()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-open-modal").ClickAsync();
        const string id = "grid-modal-edit";
        await Toolbar(id).GetByRole(AriaRole.Button, new() { Name = "Columns" }).ClickAsync();
        var chooser = Page.Locator($"#{id} > .bit-dtg-column-chooser");
        await chooser.Locator("input").First.FocusAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(chooser).ToHaveCountAsync(0);
        await Expect(Text("modal-state")).ToHaveTextAsync("open");

        // An Escape with a modifier cancels nothing in the grid: it is the surface's, and the modal closes on it.
        await Cell(id, 0, 2).ClickAsync();
        await Page.Keyboard.PressAsync("Enter");
        await ExpectFocusedAsync(Editor(id, 0, 2));
        await Page.Keyboard.PressAsync("Shift+Escape");
        await Expect(Text("modal-state")).ToHaveTextAsync("closed");
        await Expect(Text("modal-cancel-count")).ToHaveTextAsync("0");
    }
}
