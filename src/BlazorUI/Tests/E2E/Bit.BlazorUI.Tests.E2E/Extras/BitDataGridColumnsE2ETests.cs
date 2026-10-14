using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// BitDataGrid's column layout on /e2e/data-grid/columns: resizing by dragging a header's edge with a real mouse (clamped
/// to MinWidth/MaxWidth, mirrored right to left), by a double-click and by the keyboard on the focusable separator
/// (arrows, Shift, Home/End, Enter) with its aria-value*, a column that opts out, AutoFitColumnAsync /
/// AutoFitAllColumnsAsync; reordering by dragging a header (HTML5 drag and drop) and by Ctrl+Arrow with the focus kept and
/// the move announced, past a column that opts out; the column chooser (its disclosure button, hiding a column, the last
/// column kept, the move buttons, Escape handing the focus back); Frozen and FrozenEnd columns pinned while the viewport
/// scrolls sideways; header Group banners with aria-colspan; per-row ColSpan; and a column's Visible changed at runtime.
/// </summary>
[TestClass]
public class BitDataGridColumnsE2ETests : BitDataGridE2ETestBase
{
    private const string Url = "/e2e/data-grid/columns";

    private ILocator Resizer(string gridId, int ariaColIndex) => HeaderCell(gridId, ariaColIndex).Locator(".bit-dtg-resizer");

    private async Task<double> WidthAsync(string gridId, int ariaColIndex) => (await BoxAsync(HeaderCell(gridId, ariaColIndex))).Width;

    private async Task DragResizerAsync(string gridId, int ariaColIndex, double dx)
    {
        var box = await BoxAsync(Resizer(gridId, ariaColIndex));
        var x = box.X + box.Width / 2;
        var y = box.Y + box.Height / 2;
        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        // The drag follows the pointer over an overlay the grid renders once the press has reached it.
        await Expect(Page.Locator(".bit-dtg-resize-overlay")).ToHaveCountAsync(1);
        await Page.Mouse.MoveAsync((float)(x + dx / 2), y, new() { Steps = 4 });
        await Page.Mouse.MoveAsync((float)(x + dx), y, new() { Steps = 4 });
        await Page.Mouse.UpAsync();
        await Expect(Page.Locator(".bit-dtg-resize-overlay")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Resize_TheHandle_IsALabelledFocusableSeparator()
    {
        await OpenAsync(Url);

        var resizer = Resizer("grid-resize", 2);
        await Expect(resizer).ToHaveAttributeAsync("role", "separator");
        await Expect(resizer).ToHaveAttributeAsync("tabindex", "0");
        await Expect(resizer).ToHaveAttributeAsync("aria-orientation", "vertical");
        await Expect(resizer).ToHaveAccessibleNameAsync("Resize Name");
        await Expect(resizer).ToHaveAttributeAsync("aria-valuenow", "160");
        await Expect(resizer).ToHaveAttributeAsync("aria-valuemin", "100");
        await Expect(resizer).ToHaveAttributeAsync("aria-valuemax", "300");
        await Expect(resizer).ToHaveCSSAsync("cursor", "col-resize");

        // A column that opts out has no handle.
        await Expect(Resizer("grid-resize", 3)).ToHaveCountAsync(0);
        Assert.AreEqual(160, await WidthAsync("grid-resize", 2), 1);
    }

    [TestMethod]
    public async Task Resize_DraggingTheHeaderEdge_ResizesTheColumnWithinItsLimits()
    {
        await OpenAsync(Url);

        await DragResizerAsync("grid-resize", 2, 60);
        Assert.AreEqual(220, await WidthAsync("grid-resize", 2), 2);
        await Expect(Resizer("grid-resize", 2)).ToHaveAttributeAsync("aria-valuenow", "220");
        Assert.AreEqual(220, (await BoxAsync(Cell("grid-resize", 0, 2))).Width, 2, "The cells did not follow their header.");

        await DragResizerAsync("grid-resize", 2, 200);
        Assert.AreEqual(300, await WidthAsync("grid-resize", 2), 2, "The column grew past its MaxWidth.");

        await DragResizerAsync("grid-resize", 2, -400);
        Assert.AreEqual(100, await WidthAsync("grid-resize", 2), 2, "The column shrank past its MinWidth.");

        // A drag on the handle resizes and never sorts.
        await Expect(HeaderCell("grid-resize", 2)).Not.ToHaveAttributeAsync("aria-sort", new Regex(".*"));
    }

    [TestMethod]
    public async Task Resize_RightToLeft_DraggingTheEdgeLeftWidensTheColumn()
    {
        await OpenAsync(Url);

        Assert.AreEqual(160, await WidthAsync("grid-resize-rtl", 2), 1);
        await DragResizerAsync("grid-resize-rtl", 2, -50);
        Assert.AreEqual(210, await WidthAsync("grid-resize-rtl", 2), 2);
    }

    [TestMethod]
    public async Task Resize_TheKeyboard_ResizesFromTheFocusedHandle()
    {
        await OpenAsync(Url);

        var resizer = Resizer("grid-resize", 2);
        await resizer.FocusAsync();

        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(resizer).ToHaveAttributeAsync("aria-valuenow", "170");
        await Page.Keyboard.PressAsync("Shift+ArrowRight");
        await Expect(resizer).ToHaveAttributeAsync("aria-valuenow", "220");
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(resizer).ToHaveAttributeAsync("aria-valuenow", "210");
        Assert.AreEqual(210, await WidthAsync("grid-resize", 2), 2);

        await Page.Keyboard.PressAsync("End");
        await Expect(resizer).ToHaveAttributeAsync("aria-valuenow", "300");
        await Page.Keyboard.PressAsync("Home");
        await Expect(resizer).ToHaveAttributeAsync("aria-valuenow", "100");

        // Enter fits the content: the short names need less than MaxWidth, and no less than MinWidth is given.
        await Page.Keyboard.PressAsync("End");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(resizer).Not.ToHaveAttributeAsync("aria-valuenow", "300");
        var fitted = await resizer.GetAttributeAsync("aria-valuenow");
        Assert.IsTrue(double.Parse(fitted ?? "0") is >= 100 and < 300, $"Enter fitted the column to {fitted}px.");

        // Fitting a fitted column changes nothing.
        await Page.Keyboard.PressAsync("Enter");
        await NextFramesAsync();
        await Expect(resizer).ToHaveAttributeAsync("aria-valuenow", fitted!);

        // The keys resized: they did not sort the column, and the focus stayed on the handle.
        await Expect(HeaderCell("grid-resize", 2)).Not.ToHaveAttributeAsync("aria-sort", new Regex(".*"));
        await ExpectFocusedAsync(resizer);
    }

    [TestMethod]
    public async Task Resize_ADoubleClick_FitsTheColumnToItsContentBothWays()
    {
        await OpenAsync(Url);

        // Supplier is far wider than its values: fitting shrinks it.
        Assert.AreEqual(400, await WidthAsync("grid-resize", 4), 1);
        await Resizer("grid-resize", 4).DblClickAsync();
        await UntilAsync(async () => await WidthAsync("grid-resize", 4) < 200, "The double-click did not shrink Supplier to its content.");
        var fitted = await WidthAsync("grid-resize", 4);
        Assert.IsTrue(fitted > 40, $"Supplier was fitted to {fitted}px, narrower than its values.");

        // Fitting it again leaves it as it is.
        await Resizer("grid-resize", 4).DblClickAsync();
        await NextFramesAsync();
        Assert.AreEqual(fitted, await WidthAsync("grid-resize", 4), 1, "A second fit changed a fitted column.");

        // Description is far narrower than its text: fitting grows it to show the whole of it.
        Assert.AreEqual(60, await WidthAsync("grid-resize", 5), 1);
        await Resizer("grid-resize", 5).DblClickAsync();
        await UntilAsync(async () => await WidthAsync("grid-resize", 5) > 150, "The double-click did not grow Description to its content.");
        var text = Cell("grid-resize", 0, 5).Locator(".bit-dtg-cell-text");
        Assert.IsFalse(await text.EvaluateAsync<bool>("el => el.scrollWidth > el.clientWidth"), "The fitted column still clips its text.");
    }

    [TestMethod]
    public async Task Resize_TheAutoFitApi_FitsAColumnAndEveryColumn()
    {
        await OpenAsync(Url);

        await Resizer("grid-resize", 2).FocusAsync();
        await Page.Keyboard.PressAsync("End");
        await Expect(Resizer("grid-resize", 2)).ToHaveAttributeAsync("aria-valuenow", "300");

        await Page.Locator("#btn-autofit-name").ClickAsync();
        await Expect(Resizer("grid-resize", 2)).Not.ToHaveAttributeAsync("aria-valuenow", "300");

        await Page.Locator("#btn-autofit-all").ClickAsync();
        await UntilAsync(async () => await WidthAsync("grid-resize", 4) < 200 && await WidthAsync("grid-resize", 5) > 150,
            "AutoFitAllColumnsAsync did not fit Supplier and Description.");
    }

    [TestMethod]
    public async Task Reorder_DraggingAHeader_MovesTheColumn()
    {
        await OpenAsync(Url);

        const string id = "grid-reorder";
        await Expect(HeaderCell(id, 2)).ToHaveAttributeAsync("draggable", "true");
        await Expect(HeaderCell(id, 2)).ToHaveAttributeAsync("data-col", "Name");

        await HeaderRow(id).Locator("[data-col='Name']").DragToAsync(HeaderRow(id).Locator("[data-col='Category']"));

        await Expect(HeaderCells(id)).ToHaveTextAsync(["Id", "Category", "Name", "Supplier"]);
        await Expect(DataRow(id, 0).Locator("> [role=gridcell]")).ToHaveTextAsync(["1", "Electronics", "Laptop", "Acme"]);
    }

    [TestMethod]
    public async Task Reorder_CtrlArrowOnAHeader_MovesTheColumnAndKeepsTheFocus()
    {
        await OpenAsync(Url);

        const string id = "grid-reorder";
        await HeaderButton(id, "Name").FocusAsync();
        await Page.Keyboard.PressAsync("Control+ArrowRight");

        await Expect(HeaderCells(id)).ToHaveTextAsync(["Id", "Category", "Name", "Supplier"]);
        await ExpectFocusedAsync(HeaderButton(id, "Name"));
        await Expect(LiveRegion(id)).ToContainTextAsync("Name moved to position 3 of 4");

        // Supplier opts out, so nothing can move past it: the column stays, and so does the focus.
        await Page.Keyboard.PressAsync("Control+ArrowRight");
        await Expect(HeaderCells(id)).ToHaveTextAsync(["Id", "Category", "Name", "Supplier"]);
        await ExpectFocusedAsync(HeaderButton(id, "Name"));
        await Expect(HeaderCell(id, 4)).Not.ToHaveAttributeAsync("data-col", new Regex(".*"));
        await Expect(HeaderCell(id, 4)).ToHaveAttributeAsync("draggable", "false");

        await Page.Keyboard.PressAsync("Control+ArrowLeft");
        await Expect(HeaderCells(id)).ToHaveTextAsync(["Id", "Name", "Category", "Supplier"]);
        await ExpectFocusedAsync(HeaderButton(id, "Name"));
        // Ctrl+Arrow moved the column and sorted nothing.
        await Expect(HeaderCell(id, 2)).Not.ToHaveAttributeAsync("aria-sort", new Regex(".*"));
    }

    [TestMethod]
    public async Task Chooser_TheColumnsButton_OpensAPanelThatHidesColumns()
    {
        await OpenAsync(Url);

        const string id = "grid-chooser";
        var button = Toolbar(id).GetByRole(AriaRole.Button, new() { Name = "Columns" });
        await Expect(button).ToHaveAttributeAsync("aria-expanded", "false");

        await button.ClickAsync();
        await Expect(button).ToHaveAttributeAsync("aria-expanded", "true");
        var panel = Page.Locator($"#{id} > .bit-dtg-column-chooser");
        await Expect(panel).ToHaveAttributeAsync("role", "group");
        await Expect(panel).ToHaveAccessibleNameAsync("Choose columns");
        await Expect(panel).ToHaveAttributeAsync("data-bit-esc", "claim");
        await Expect(button).ToHaveAttributeAsync("aria-controls", (await panel.GetAttributeAsync("id"))!);
        await Expect(panel.Locator(".bit-dtg-chooser-label")).ToHaveTextAsync(["Id", "Name", "Category"]);

        await panel.GetByLabel("Category", new() { Exact = true }).UncheckAsync();
        await Expect(HeaderCells(id)).ToHaveTextAsync(["Id", "Name"]);
        await Expect(Table(id)).ToHaveAttributeAsync("aria-colcount", "2");

        await panel.GetByLabel("Id", new() { Exact = true }).UncheckAsync();
        await Expect(HeaderCells(id)).ToHaveTextAsync(["Name"]);
        // The last visible column cannot be hidden.
        await Expect(panel.GetByLabel("Name", new() { Exact = true })).ToBeDisabledAsync();
        await Expect(panel.GetByLabel("Name", new() { Exact = true })).ToHaveAttributeAsync("aria-disabled", "true");

        await panel.GetByLabel("Category", new() { Exact = true }).CheckAsync();
        await Expect(HeaderCells(id)).ToHaveTextAsync(["Name", "Category"]);
        await Expect(panel.GetByLabel("Name", new() { Exact = true })).ToBeEnabledAsync();
    }

    [TestMethod]
    public async Task Chooser_TheMoveButtons_ReorderTheColumnsAndEscapeClosesThePanel()
    {
        await OpenAsync(Url);

        const string id = "grid-chooser";
        var button = Toolbar(id).GetByRole(AriaRole.Button, new() { Name = "Columns" });
        await button.ClickAsync();
        var panel = Page.Locator($"#{id} > .bit-dtg-column-chooser");

        await Expect(panel.GetByRole(AriaRole.Button, new() { Name = "Move Id earlier" })).ToBeDisabledAsync();
        await Expect(panel.GetByRole(AriaRole.Button, new() { Name = "Move Category later" })).ToBeDisabledAsync();

        var later = panel.GetByRole(AriaRole.Button, new() { Name = "Move Id later" });
        await later.ClickAsync();
        await Expect(HeaderCells(id)).ToHaveTextAsync(["Name", "Id", "Category"]);
        await Expect(panel.Locator(".bit-dtg-chooser-label")).ToHaveTextAsync(["Name", "Id", "Category"]);
        // The item moved with its column, and the focus with it.
        await ExpectFocusedAsync(later);
        await Expect(LiveRegion(id)).ToContainTextAsync("Id moved to position 2 of 3");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(panel).ToHaveCountAsync(0);
        await Expect(button).ToHaveAttributeAsync("aria-expanded", "false");
        await ExpectFocusedAsync(button);
    }

    [TestMethod]
    public async Task Frozen_ScrollingSideways_KeepsThePinnedColumnsAtTheirEdges()
    {
        await OpenAsync(Url);

        const string id = "grid-frozen";
        await Expect(Cell(id, 0, 1)).ToHaveClassAsync(HasClass("bit-dtg-sticky"));
        await Expect(Cell(id, 0, 6)).ToHaveClassAsync(HasClass("bit-dtg-frozen-end"));
        await Expect(Cell(id, 0, 1)).ToHaveCSSAsync("position", "sticky");

        var view = await BoxAsync(Viewport(id));
        var clientWidth = await Viewport(id).EvaluateAsync<double>("el => el.clientWidth");
        var clientLeft = await Viewport(id).EvaluateAsync<double>("el => el.clientLeft");
        var nameBefore = await BoxAsync(Cell(id, 0, 2));
        Assert.AreEqual(view.X + clientLeft + clientWidth, (await BoxAsync(Cell(id, 0, 6))).X + (await BoxAsync(Cell(id, 0, 6))).Width, 1.5, "The end-frozen column is not at the right edge.");

        await Viewport(id).EvaluateAsync("el => el.scrollLeft = 300");
        await NextFramesAsync();

        var id1 = await BoxAsync(Cell(id, 0, 1));
        var rating = await BoxAsync(Cell(id, 0, 6));
        var nameAfter = await BoxAsync(Cell(id, 0, 2));
        Assert.AreEqual(view.X + clientLeft, id1.X, 1.5, "The frozen column scrolled away.");
        Assert.AreEqual(view.X + clientLeft + clientWidth, rating.X + rating.Width, 1.5, "The end-frozen column left the right edge.");
        Assert.AreEqual(nameBefore.X - 300, nameAfter.X, 1.5, "The other columns did not scroll.");

        // The header's pinned cells stay with their columns.
        Assert.AreEqual(id1.X, (await BoxAsync(HeaderCell(id, 1))).X, 1.5);
    }

    [TestMethod]
    public async Task HeaderGroups_ConsecutiveColumns_ShareABanner()
    {
        await OpenAsync(Url);

        const string id = "grid-header-groups";
        var bannerRow = Page.Locator($"#{id} .bit-dtg-group-header-row");
        await Expect(bannerRow).ToHaveAttributeAsync("aria-rowindex", "1");
        await Expect(HeaderRow(id)).ToHaveAttributeAsync("aria-rowindex", "2");

        var banners = bannerRow.Locator("> [role=columnheader]");
        await Expect(banners).ToHaveTextAsync(["", "Identity", "Commercials"]);
        await Expect(banners.Nth(0)).ToHaveClassAsync(HasClass("bit-dtg-group-header-empty"));
        await Expect(banners.Nth(1)).ToHaveAttributeAsync("aria-colspan", "2");
        await Expect(banners.Nth(1)).ToHaveAttributeAsync("aria-colindex", "2");
        await Expect(banners.Nth(2)).ToHaveAttributeAsync("aria-colindex", "4");

        var identity = await BoxAsync(banners.Nth(1));
        Assert.AreEqual((await WidthAsync(id, 2)) + (await WidthAsync(id, 3)), identity.Width, 2);
        Assert.AreEqual((await BoxAsync(HeaderCell(id, 2))).X, identity.X, 1.5);
        await Expect(Table(id)).ToHaveAttributeAsync("aria-rowcount", "7");
    }

    [TestMethod]
    public async Task ColSpan_ACellThatSpans_CoversTheNextColumnAndSaysSo()
    {
        await OpenAsync(Url);

        const string id = "grid-colspan";
        var spanning = Cell(id, 2, 2);
        await Expect(spanning).ToHaveAttributeAsync("aria-colspan", "2");
        await Expect(spanning).ToHaveTextAsync("Jacket - discontinued");
        await Expect(Cell(id, 2, 3)).ToHaveCountAsync(0);
        await Expect(DataRow(id, 2).Locator("> [role=gridcell]")).ToHaveCountAsync(3);
        await Expect(Cell(id, 1, 2)).Not.ToHaveAttributeAsync("aria-colspan", new Regex(".*"));

        Assert.AreEqual((await WidthAsync(id, 2)) + (await WidthAsync(id, 3)), (await BoxAsync(spanning)).Width, 2);

        // The keyboard steps over the span.
        await Cell(id, 2, 1).ClickAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await ExpectFocusedAsync(spanning);
        await Page.Keyboard.PressAsync("ArrowRight");
        await ExpectFocusedAsync(Cell(id, 2, 4));
    }

    [TestMethod]
    public async Task Visible_ChangedByThePage_HidesAndShowsTheColumn()
    {
        await OpenAsync(Url);

        const string id = "grid-visible";
        await Expect(HeaderCells(id)).ToHaveTextAsync(["Id", "Name", "Category", "Supplier"]);

        await Page.Locator("#btn-toggle-category").ClickAsync();
        await Expect(HeaderCells(id)).ToHaveTextAsync(["Id", "Name", "Supplier"]);
        await Expect(DataRow(id, 0).Locator("> [role=gridcell]")).ToHaveTextAsync(["1", "Laptop", "Acme"]);

        await Page.Locator("#btn-toggle-category").ClickAsync();
        await Expect(HeaderCells(id)).ToHaveTextAsync(["Id", "Name", "Category", "Supplier"]);
    }
}
