using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.DataGrid;

/// <summary>
/// The grid's structure as assistive technology reads it - row and column indices across grouped views, banners and
/// the footer, row headers, names - and the keyboard model that keeps the cells its one tab stop.
/// </summary>
[TestClass]
public class BitDataGridStructureTests : BunitTestContext
{
    public class Row
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty;
        public double Price { get; set; }
    }

    public class Node
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<Node>? Children { get; set; }
    }

    private static List<Row> CreateRows() =>
    [
        new() { Id = 1, Name = "Apple", Kind = "Fruit", Price = 2 },
        new() { Id = 2, Name = "Carrot", Kind = "Vegetable", Price = 1 },
        new() { Id = 3, Name = "Banana", Kind = "Fruit", Price = 3 },
    ];

    private static RenderFragment Column<T>(string field, Action<RenderTreeBuilderParameters>? configure = null) => builder =>
    {
        builder.OpenComponent<BitDataGridColumn<T>>(0);
        builder.AddComponentParameter(1, "Field", field);
        configure?.Invoke(new RenderTreeBuilderParameters(builder));
        builder.CloseComponent();
    };

    public sealed class RenderTreeBuilderParameters(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
    {
        public void Add(string name, object? value) => builder.AddComponentParameter(2, name, value);
    }

    private static RenderFragment Columns(params RenderFragment[] columns) => builder =>
    {
        foreach (var column in columns) builder.AddContent(0, column);
    };

    private IRenderedComponent<BitDataGrid<Row>> RenderGrid(
        RenderFragment? columns = null,
        Action<ComponentParameterCollectionBuilder<BitDataGrid<Row>>>? configure = null,
        List<Row>? items = null)
        => RenderComponent<BitDataGrid<Row>>(parameters =>
        {
            parameters.Add(p => p.Items, items ?? CreateRows());
            parameters.Add(p => p.KeyField, (Func<Row, object>)(r => r.Id));
            parameters.Add(p => p.ChildContent, columns ?? Columns(Column<Row>("Name"), Column<Row>("Kind"), Column<Row>("Price")));
            configure?.Invoke(parameters);
        });

    private static IReadOnlyList<IElement> BodyRows(IRenderedComponent<BitDataGrid<Row>> component)
        => component.FindAll(".bit-dtg-body > .bit-dtg-row");

    private static string? Status(IRenderedComponent<BitDataGrid<Row>> component)
        => component.Find("[role=status]").TextContent.Replace("​", "");

    [TestMethod]
    public async Task AGroupedViewNumbersItsRowsInTheOrderTheyRender()
    {
        var component = RenderGrid(configure: p => p.Add(x => x.ShowRowNumbers, true));
        await component.InvokeAsync(() => component.Instance.GroupByAsync("Kind"));

        // Fruit (Apple, Banana), then Vegetable (Carrot), each under its group row, after the one header row.
        var rows = BodyRows(component);
        CollectionAssert.AreEqual(new[] { "2", "3", "4", "5", "6" }, rows.Select(r => r.GetAttribute("aria-rowindex")).ToArray());
        Assert.IsTrue(rows[0].ClassList.Contains("bit-dtg-group-row"));
        Assert.IsTrue(rows[3].ClassList.Contains("bit-dtg-group-row"));
        Assert.AreEqual("6", component.Find("[role=grid]").GetAttribute("aria-rowcount"));

        // The row numbers count down the grid as shown, not by the position each row had before grouping.
        CollectionAssert.AreEqual(new[] { "1", "2", "3" },
            component.FindAll(".bit-dtg-cell-rownumber").Select(c => c.TextContent.Trim()).ToArray());

        // A collapsed group's rows are not in the grid, so they are not counted either.
        rows[0].QuerySelector("button")!.Click();
        CollectionAssert.AreEqual(new[] { "2", "3", "4" }, BodyRows(component).Select(r => r.GetAttribute("aria-rowindex")).ToArray());
        Assert.AreEqual("4", component.Find("[role=grid]").GetAttribute("aria-rowcount"));
    }

    [TestMethod]
    public async Task AGroupedViewExportsAndCopiesInTheOrderItShows()
    {
        Context.JSInterop.Setup<bool>("BitBlazorUI.DataGrid.copyToClipboard", _ => true).SetResult(true);
        var component = RenderGrid(columns: Columns(Column<Row>("Name"), Column<Row>("Kind")),
            configure: p => p.Add(x => x.SelectionMode, BitSelectionMode.Multiple));
        await component.InvokeAsync(() => component.Instance.GroupByAsync("Kind"));

        var csv = component.Instance.ToCsv();
        CollectionAssert.AreEqual(new[] { "Name,Kind", "Apple,Fruit", "Banana,Fruit", "Carrot,Vegetable" },
            csv.Split("\r\n", StringSplitOptions.RemoveEmptyEntries));

        await component.InvokeAsync(() => component.Instance.SelectAllAsync());
        await component.InvokeAsync(() => component.Instance.CopyToClipboardAsync());
        var payload = (string)Context.JSInterop.Invocations["BitBlazorUI.DataGrid.copyToClipboard"].Single().Arguments[0]!;
        CollectionAssert.AreEqual(new[] { "Name\tKind", "Apple\tFruit", "Banana\tFruit", "Carrot\tVegetable" },
            payload.Split("\r\n", StringSplitOptions.RemoveEmptyEntries));
    }

    [TestMethod]
    public async Task TheRowBeingAddedIsTheFirstRowOfTheBody()
    {
        var component = RenderGrid(configure: p =>
        {
            p.Add(x => x.Editable, true);
            p.Add(x => x.NewItemFactory, () => new Row { Id = 99 });
        });
        await component.InvokeAsync(() => component.Instance.AddNewRowAsync());

        CollectionAssert.AreEqual(new[] { "2", "3", "4", "5" }, BodyRows(component).Select(r => r.GetAttribute("aria-rowindex")).ToArray());
        Assert.AreEqual("5", component.Find("[role=grid]").GetAttribute("aria-rowcount"));

        // Grouped, the group rows count on after it as well.
        await component.InvokeAsync(() => component.Instance.CancelEditAsync());
        await component.InvokeAsync(() => component.Instance.GroupByAsync("Kind"));
        await component.InvokeAsync(() => component.Instance.AddNewRowAsync());
        CollectionAssert.AreEqual(new[] { "2", "3", "4", "5", "6", "7" }, BodyRows(component).Select(r => r.GetAttribute("aria-rowindex")).ToArray());
        Assert.AreEqual("7", component.Find("[role=grid]").GetAttribute("aria-rowcount"));
    }

    [TestMethod]
    public async Task TheKeyboardMovesThroughAGroupedViewInTheOrderItRenders()
    {
        var component = RenderGrid(configure: p => p.Add(x => x.CellNavigation, true));
        await component.InvokeAsync(() => component.Instance.GroupByAsync("Kind"));

        IElement FirstCell(string name) => component.FindAll(".bit-dtg-body > .bit-dtg-row:not(.bit-dtg-group-row)")
            .Single(r => r.TextContent.Contains(name)).QuerySelector("[aria-colindex]")!;

        FirstCell("Apple").FocusIn();
        FirstCell("Apple").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

        // Banana is next on screen (same group), although Carrot comes before it in the data.
        Assert.AreEqual("0", FirstCell("Banana").GetAttribute("tabindex"));
        FirstCell("Banana").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        Assert.AreEqual("0", FirstCell("Carrot").GetAttribute("tabindex"));
    }

    [TestMethod]
    public async Task DeletingARowOfAGroupedViewHandsTheFocusToTheRowShownAfterIt()
    {
        var items = CreateRows();
        var component = RenderGrid(items: items, configure: p =>
        {
            p.Add(x => x.CellNavigation, true);
            p.Add(x => x.Editable, true);
            p.Add(x => x.OnRowDelete, (Row r) => { items.Remove(r); });
        });
        await component.InvokeAsync(() => component.Instance.GroupByAsync("Kind"));

        IElement FirstCell(string name) => component.FindAll(".bit-dtg-body > .bit-dtg-row:not(.bit-dtg-group-row)")
            .Single(r => r.TextContent.Contains(name)).QuerySelector("[aria-colindex]")!;

        // Banana is second on screen; once it is gone, Carrot is - read off the view the delete left, not the one before.
        FirstCell("Banana").FocusIn();
        FirstCell("Banana").KeyDown(new KeyboardEventArgs { Key = "Delete" });
        Assert.AreEqual(2, items.Count);
        Assert.AreEqual("0", FirstCell("Carrot").GetAttribute("tabindex"));
        Assert.AreEqual("-1", FirstCell("Apple").GetAttribute("tabindex"));
    }

    [TestMethod]
    public void DirStillSwitchesTheGridsDirectionAfterTheFirstRender()
    {
        var component = RenderGrid(configure: p => p.Add(x => x.Dir, BitDir.Ltr));
        Assert.IsFalse(component.Find(".bit-dtg").ClassList.Contains("bit-dtg-rtl"));

        component.Render(p => p.Add(x => x.Dir, BitDir.Rtl));
        Assert.IsTrue(component.Find(".bit-dtg").ClassList.Contains("bit-rtl"));
        Assert.IsTrue(component.Find(".bit-dtg").ClassList.Contains("bit-dtg-rtl"));
    }

    [TestMethod]
    public void TheFooterIsTheLastRowAndKeepsThePinnedColumnsPinned()
    {
        var columns = Columns(
            Column<Row>("Name", c => c.Add("Frozen", true)),
            Column<Row>("Kind"),
            Column<Row>("Price", c => { c.Add("FrozenEnd", true); }),
            Column<Row>("Id", c => c.Add("Aggregate", BitDataGridAggregateType.Count)));
        var component = RenderGrid(columns, p =>
        {
            p.Add(x => x.ShowFooter, true);
            p.Add(x => x.SelectionMode, BitSelectionMode.Multiple);
        });

        var grid = component.Find("[role=grid]");
        var footer = component.Find(".bit-dtg-footer-row");
        Assert.AreEqual("5", grid.GetAttribute("aria-rowcount"), "a header row, three data rows and the footer");
        Assert.AreEqual("5", footer.GetAttribute("aria-rowindex"));

        var cells = footer.QuerySelectorAll("[role=gridcell]");
        Assert.IsTrue(cells[0].ClassList.Contains("bit-dtg-sticky"), "the selection column's footer cell is pinned with it");
        Assert.AreEqual("1", cells[0].GetAttribute("aria-colindex"));
        Assert.IsTrue(cells[1].ClassList.Contains("bit-dtg-sticky"), "a Frozen column's total stays under it");
        StringAssert.Contains(cells[1].GetAttribute("style"), "inset-inline-start");
        Assert.IsTrue(cells[3].ClassList.Contains("bit-dtg-frozen-end"));
        StringAssert.Contains(cells[3].GetAttribute("style"), "inset-inline-end");

        // The FrozenEnd marker reaches every row, so the stylesheet can rule the band's edge.
        Assert.IsTrue(component.Find(".bit-dtg-header-row [aria-colindex='4']").ClassList.Contains("bit-dtg-frozen-end"));
        Assert.IsTrue(BodyRows(component)[0].QuerySelector("[aria-colindex='4']")!.ClassList.Contains("bit-dtg-frozen-end"));
    }

    [TestMethod]
    public void ColumnGroupBannersTellAssistiveTechWhichColumnsTheyCover()
    {
        var columns = Columns(
            Column<Row>("Id"),
            Column<Row>("Name", c => c.Add("Group", "Product")),
            Column<Row>("Kind", c => c.Add("Group", "Product")),
            Column<Row>("Price"));
        var component = RenderGrid(columns, p => p.Add(x => x.SelectionMode, BitSelectionMode.Multiple));

        var banners = component.FindAll(".bit-dtg-group-header-row [role=columnheader]");
        CollectionAssert.AreEqual(new[] { "1", "2", "3", "5" }, banners.Select(b => b.GetAttribute("aria-colindex")).ToArray());
        CollectionAssert.AreEqual(new string?[] { null, null, "2", null }, banners.Select(b => b.GetAttribute("aria-colspan")).ToArray());
        Assert.AreEqual("Product", banners[2].TextContent.Trim());
    }

    [TestMethod]
    public void ARowHeaderColumnHeadsItsRowsAndNamesTheirCheckboxes()
    {
        var columns = Columns(Column<Row>("Id"), Column<Row>("Name", c => c.Add("RowHeader", true)), Column<Row>("Price"));
        var component = RenderGrid(columns, p => p.Add(x => x.SelectionMode, BitSelectionMode.Multiple));

        var first = BodyRows(component)[0];
        Assert.AreEqual(1, first.QuerySelectorAll("[role=rowheader]").Length);
        Assert.AreEqual("Apple", first.QuerySelector("[role=rowheader]")!.TextContent.Trim());
        StringAssert.Contains(first.QuerySelector(".bit-dtg-cell-select input")!.GetAttribute("aria-label"), "Apple");
    }

    [TestMethod]
    public void AriaLabelledByNamesTheGridInsteadOfTheDefaultLabel()
    {
        var component = RenderGrid(configure: p =>
        {
            p.Add(x => x.AriaLabelledBy, "products-heading");
            p.Add(x => x.AriaDescribedBy, "products-help");
        });

        var grid = component.Find("[role=grid]");
        Assert.AreEqual("products-heading", grid.GetAttribute("aria-labelledby"));
        Assert.AreEqual("products-help", grid.GetAttribute("aria-describedby"));
        Assert.IsNull(grid.GetAttribute("aria-label"));

        component.Render(p => p.Add(x => x.AriaLabel, "Products"));
        Assert.AreEqual("Products", component.Find("[role=grid]").GetAttribute("aria-label"), "an explicit AriaLabel is still rendered");
    }

    [TestMethod]
    public void WithCellNavigationThePerRowControlsLeaveTheTabOrder()
    {
        RenderFragment<Row> detail = row => builder => builder.AddContent(0, row.Name);
        var component = RenderGrid(configure: p =>
        {
            p.Add(x => x.SelectionMode, BitSelectionMode.Multiple);
            p.Add(x => x.Editable, true);
            p.Add(x => x.DetailTemplate, detail);
        });

        var row = BodyRows(component)[0];
        Assert.IsTrue(row.QuerySelectorAll("button, input").All(c => c.GetAttribute("tabindex") is null),
            "without cell navigation every control is a tab stop of its own");

        component.Render(p => p.Add(x => x.CellNavigation, true));
        row = BodyRows(component)[0];
        Assert.IsTrue(row.QuerySelectorAll("button, input").All(c => c.GetAttribute("tabindex") == "-1"),
            "the cells are the one tab stop, and their keys do what these controls do");

        // An open row edit keeps its Save and Cancel in the tab order.
        row.QuerySelectorAll(".bit-dtg-cell-command button")[0].Click();
        Assert.IsTrue(BodyRows(component)[0].QuerySelectorAll(".bit-dtg-cell-command button").All(b => b.GetAttribute("tabindex") is null));
    }

    [TestMethod]
    public void EnterOnACellTogglesTheRowsDetail()
    {
        RenderFragment<Row> detail = row => builder => builder.AddContent(0, $"Details of {row.Name}");
        var component = RenderGrid(configure: p =>
        {
            p.Add(x => x.CellNavigation, true);
            p.Add(x => x.DetailTemplate, detail);
        });

        var cell = BodyRows(component)[0].QuerySelectorAll(".bit-dtg-cell:not(.bit-dtg-cell-detail)")[0];
        cell.KeyDown(new KeyboardEventArgs { Key = "Enter" });
        Assert.AreEqual(1, component.FindAll(".bit-dtg-detail-row").Count);

        BodyRows(component)[0].QuerySelectorAll(".bit-dtg-cell:not(.bit-dtg-cell-detail)")[0].KeyDown(new KeyboardEventArgs { Key = "Enter" });
        Assert.AreEqual(0, component.FindAll(".bit-dtg-detail-row").Count);

        // Details with no toggle column and no row click are driven from code alone, and Enter leaves them be.
        component.Render(p => p.Add(x => x.ShowDetailToggle, false));
        BodyRows(component)[0].QuerySelectorAll(".bit-dtg-cell")[0].KeyDown(new KeyboardEventArgs { Key = "Enter" });
        Assert.AreEqual(0, component.FindAll(".bit-dtg-detail-row").Count);
    }

    private IRenderedComponent<BitDataGrid<Node>> RenderTree()
    {
        var roots = new List<Node>
        {
            new() { Id = 1, Name = "Fruit", Children = [new() { Id = 2, Name = "Apple" }, new() { Id = 3, Name = "Banana" }] },
            new() { Id = 4, Name = "Vegetables", Children = [new() { Id = 5, Name = "Carrot" }] },
        };
        return RenderComponent<BitDataGrid<Node>>(parameters =>
        {
            parameters.Add(p => p.Items, roots);
            parameters.Add(p => p.KeyField, (Func<Node, object>)(n => n.Id));
            parameters.Add(p => p.ChildrenSelector, (Func<Node, IEnumerable<Node>?>)(n => n.Children));
            parameters.Add(p => p.CellNavigation, true);
            parameters.Add(p => p.ChildContent, Columns(Column<Node>("Name"), Column<Node>("Id")));
        });
    }

    private static IElement TreeCell(IRenderedComponent<BitDataGrid<Node>> component, string name)
        => component.FindAll(".bit-dtg-body > .bit-dtg-row").Single(r => r.QuerySelector(".bit-dtg-cell-text")!.TextContent.Trim() == name)
            .QuerySelector("[aria-colindex='1']")!;

    private static string[] TreeRows(IRenderedComponent<BitDataGrid<Node>> component)
        => component.FindAll(".bit-dtg-body > .bit-dtg-row").Select(r => r.QuerySelector(".bit-dtg-cell-text")!.TextContent.Trim()).ToArray();

    [TestMethod]
    public void TheArrowKeysOpenAndCloseTreeNodesFromTheirFirstCell()
    {
        var component = RenderTree();
        CollectionAssert.AreEqual(new[] { "Fruit", "Vegetables" }, TreeRows(component));

        TreeCell(component, "Fruit").FocusIn();
        TreeCell(component, "Fruit").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        CollectionAssert.AreEqual(new[] { "Fruit", "Apple", "Banana", "Vegetables" }, TreeRows(component));
        Assert.AreEqual("0", TreeCell(component, "Fruit").GetAttribute("tabindex"), "the focus stays on the node it opened");

        // Once open, the forward arrow moves on to the next cell as anywhere else.
        TreeCell(component, "Fruit").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        Assert.AreEqual("-1", TreeCell(component, "Fruit").GetAttribute("tabindex"));

        // The backward arrow on a child goes up to its parent, and on the open parent closes it.
        TreeCell(component, "Banana").FocusIn();
        TreeCell(component, "Banana").KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });
        Assert.AreEqual("0", TreeCell(component, "Fruit").GetAttribute("tabindex"));
        TreeCell(component, "Fruit").KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });
        CollectionAssert.AreEqual(new[] { "Fruit", "Vegetables" }, TreeRows(component));
    }

    [TestMethod]
    public void TheArrowKeysAreMirroredInARightToLeftTree()
    {
        var component = RenderTree();
        component.Render(p => p.Add(x => x.Dir, BitDir.Rtl));

        TreeCell(component, "Fruit").FocusIn();
        TreeCell(component, "Fruit").KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });
        CollectionAssert.AreEqual(new[] { "Fruit", "Apple", "Banana", "Vegetables" }, TreeRows(component));
    }

    [TestMethod]
    public void ClearButtonsHandTheFocusOnInsteadOfDroppingIt()
    {
        var component = RenderGrid(configure: p =>
        {
            p.Add(x => x.ShowSearchBox, true);
            p.Add(x => x.Filterable, true);
            p.Add(x => x.FilterDebounce, 0);
        });

        component.Find(".bit-dtg-search-input").Input("app");
        component.WaitForAssertion(() => component.Find(".bit-dtg-search-clear"));
        component.Find(".bit-dtg-search-clear").Click();
        component.WaitForAssertion(() => CollectionAssert.AreEqual(new[] { ".bit-dtg-search-input" },
            (string[])Context.JSInterop.Invocations.Last(i => i.Identifier == "BitBlazorUI.DataGrid.focusFirst").Arguments[1]!));

        component.Find(".bit-dtg-filter-row input").Input("a");
        component.FindAll(".bit-dtg-toolbar button").Single(b => b.TextContent.Contains("Clear filters")).Click();
        var focus = Context.JSInterop.Invocations.Last(i => i.Identifier == "BitBlazorUI.DataGrid.focusFirst");
        StringAssert.StartsWith(((string[])focus.Arguments[1]!)[0], ".bit-dtg-filter-row");
    }

    [TestMethod]
    public void AChooserItemFollowsItsColumnAndKeepsTheFocus()
    {
        var component = RenderGrid(configure: p =>
        {
            p.Add(x => x.ShowColumnChooser, true);
            p.Add(x => x.Reorderable, true);
        });
        component.FindAll(".bit-dtg-toolbar button").Single(b => b.TextContent.Contains("Columns")).Click();

        var items = component.FindAll(".bit-dtg-chooser-item");
        CollectionAssert.AreEqual(new[] { "Name", "Kind", "Price" }, items.Select(i => i.GetAttribute("data-col-id")).ToArray());

        items[1].QuerySelector("[data-move='-1']")!.Click();
        CollectionAssert.AreEqual(new[] { "Kind", "Name", "Price" },
            component.FindAll(".bit-dtg-chooser-item").Select(i => i.GetAttribute("data-col-id")).ToArray());

        var targets = (string[])Context.JSInterop.Invocations.Last(i => i.Identifier == "BitBlazorUI.DataGrid.focusFirst").Arguments[1]!;
        Assert.AreEqual(".bit-dtg-chooser-item[data-col-id=\"Kind\"] [data-move=\"-1\"]", targets[0]);
        Assert.AreEqual(".bit-dtg-chooser-item[data-col-id=\"Kind\"] [data-move]", targets[1], "at the start, the other button takes it");
    }

    [TestMethod]
    public void TypingIntoATextOrNumberCellOpensItsEditorWithWhatWasTyped()
    {
        var columns = Columns(Column<Row>("Name"), Column<Row>("Kind", c => c.Add("Editable", false)), Column<Row>("Price"));
        var component = RenderGrid(columns, p =>
        {
            p.Add(x => x.Editable, true);
            p.Add(x => x.EditMode, BitDataGridEditMode.Cell);
        });

        var cells = BodyRows(component)[0].QuerySelectorAll("[aria-colindex]").Where(c => !c.ClassList.Contains("bit-dtg-cell-command")).ToList();
        CollectionAssert.AreEqual(new string?[] { "text", null, "number" }, cells.Select(c => c.GetAttribute("data-bit-dtg-typable")).ToArray(),
            "only an editable cell with a built-in text or number editor opens by typing");

        // A letter is no way to start a number.
        cells[2].KeyDown(new KeyboardEventArgs { Key = "x" });
        Assert.IsNull(component.Instance.EditingItem);

        cells[2].KeyDown(new KeyboardEventArgs { Key = "7" });
        Assert.AreEqual("Price", component.Instance.EditingColumnId);
        var focus = Context.JSInterop.Invocations.Last(i => i.Identifier == "BitBlazorUI.DataGrid.focusEditor");
        Assert.AreEqual("Price", focus.Arguments[1]);
        Assert.AreEqual(true, focus.Arguments[2], "the editor takes the typed text in place of the value");

        // Enter and F2 open an editor that keeps its value.
        component.InvokeAsync(() => component.Instance.CancelEditAsync()).GetAwaiter().GetResult();
        BodyRows(component)[0].QuerySelectorAll("[aria-colindex]")[0].KeyDown(new KeyboardEventArgs { Key = "F2" });
        Assert.AreEqual(false, Context.JSInterop.Invocations.Last(i => i.Identifier == "BitBlazorUI.DataGrid.focusEditor").Arguments[2]);
    }

    [TestMethod]
    public void TypingLeavesSpaceShortcutsAndReadOnlyGridsAlone()
    {
        var component = RenderGrid(configure: p =>
        {
            p.Add(x => x.Editable, true);
            p.Add(x => x.CellNavigation, true);
            p.Add(x => x.SelectionMode, BitSelectionMode.Multiple);
        });

        var name = BodyRows(component)[0].QuerySelectorAll(".bit-dtg-cell:not(.bit-dtg-cell-select)")[0];
        name.KeyDown(new KeyboardEventArgs { Key = " " });
        name.KeyDown(new KeyboardEventArgs { Key = "c", CtrlKey = true });
        Assert.IsNull(component.Instance.EditingItem, "Space selects and Ctrl+C copies");
        Assert.AreEqual(1, component.FindAll(".bit-dtg-row.bit-dtg-selected").Count);

        // Row mode opens the whole row, with the focus in the typed cell's editor.
        BodyRows(component)[0].QuerySelectorAll(".bit-dtg-cell:not(.bit-dtg-cell-select)")[0].KeyDown(new KeyboardEventArgs { Key = "Backspace" });
        Assert.IsNotNull(component.Instance.EditingItem);
        Assert.AreEqual("Name", Context.JSInterop.Invocations.Last(i => i.Identifier == "BitBlazorUI.DataGrid.focusEditor").Arguments[1]);

        component.Render(p => p.Add(x => x.Disabled, true));
        Assert.AreEqual(0, component.FindAll("[data-bit-dtg-typable]").Count, "a disabled grid opens no editor");
    }

    [TestMethod]
    public void SingleSelectionCanBeMadeFromTheKeyboard()
    {
        var component = RenderGrid(configure: p => p.Add(x => x.SelectionMode, BitSelectionMode.Single));

        // A click is not the only way in: the cells take the focus, and Space selects the focused row.
        var cell = BodyRows(component)[1].QuerySelector("[aria-colindex]")!;
        Assert.AreEqual(1, component.FindAll("[role=gridcell][tabindex='0']").Count);
        cell.KeyDown(new KeyboardEventArgs { Key = " " });
        Assert.IsTrue(BodyRows(component)[1].ClassList.Contains("bit-dtg-selected"));
        Assert.AreEqual(1, component.FindAll(".bit-dtg-selected").Count);
    }

    [TestMethod]
    public async Task ChangingThePageSizeIsAnnounced()
    {
        var component = RenderGrid(configure: p =>
        {
            p.Add(x => x.Pageable, true);
            p.Add(x => x.PageSize, 1);
        });

        await component.InvokeAsync(() => component.Instance.SetPageSizeAsync(2));
        Assert.AreEqual(string.Format(new BitDataGridStrings().AnnouncementPage, 1, 2), Status(component));
    }
}
