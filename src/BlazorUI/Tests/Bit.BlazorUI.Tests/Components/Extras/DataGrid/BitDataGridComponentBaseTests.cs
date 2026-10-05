using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.DataGrid;

/// <summary>
/// Covers what the grid takes from BitComponentBase and the BitParams infrastructure - the root attributes, Disabled,
/// BitDataGridParams, the per-part Classes/Styles - and the keyboard alternatives to the pointer gestures on the
/// column headers (the resize separator, Ctrl+Arrow and the column chooser's move buttons), plus OnStateChange.
/// </summary>
[TestClass]
public class BitDataGridComponentBaseTests : BunitTestContext
{
    public class Row
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Price { get; set; }
        public bool Discontinued { get; set; }
    }

    private static List<Row> Rows() =>
    [
        new() { Id = 1, Name = "Banana", Price = 2.5 },
        new() { Id = 2, Name = "Apple", Price = 5, Discontinued = true },
        new() { Id = 3, Name = "Cherry", Price = 10 },
    ];

    private static RenderFragment Columns(bool reorderable = true, string? width = "100px") => builder =>
    {
        var seq = 0;
        foreach (var field in new[] { "Id", "Name", "Price" })
        {
            builder.OpenComponent<BitDataGridColumn<Row>>(seq++);
            builder.AddComponentParameter(seq++, "Field", field);
            if (width is not null) builder.AddComponentParameter(seq++, "Width", width);
            if (reorderable is false) builder.AddComponentParameter(seq++, "Reorderable", false);
            builder.CloseComponent();
        }
    };

    private IRenderedComponent<BitDataGrid<Row>> RenderGrid(Action<ComponentParameterCollectionBuilder<BitDataGrid<Row>>>? configure = null, RenderFragment? columns = null)
    {
        return RenderComponent<BitDataGrid<Row>>(parameters =>
        {
            parameters.Add(p => p.Items, Rows());
            parameters.Add(p => p.KeyField, r => r.Id);
            parameters.Add(p => p.ChildContent, columns ?? Columns());
            configure?.Invoke(parameters);
        });
    }

    private static IReadOnlyList<string> HeaderTitles(IRenderedComponent<BitDataGrid<Row>> component)
        => component.FindAll(".bit-dtg-header-row .bit-dtg-htext").Select(h => h.TextContent.Trim()).ToList();

    // ------------------------------------------------------------------ root

    [TestMethod]
    public void BitDataGridShouldRenderTheBaseClassAttributesOnItsRoot()
    {
        // An HTML attribute is captured by BitComponentBase from the unmatched parameters, so it is written the way
        // markup writes it rather than through the parameter builder, which refuses an unmatched parameter.
        var component = Context.Render(builder =>
        {
            builder.OpenComponent<BitDataGrid<Row>>(0);
            builder.AddAttribute(1, "Items", Rows());
            builder.AddAttribute(2, "ChildContent", Columns());
            builder.AddAttribute(3, "Id", "products-grid");
            builder.AddAttribute(4, "Class", "custom-class");
            builder.AddAttribute(5, "Style", "margin: 1px");
            builder.AddAttribute(6, "Dir", (BitDir?)BitDir.Rtl);
            builder.AddAttribute(7, "data-test", "grid");
            builder.CloseComponent();
        });

        var root = component.Find(".bit-dtg");

        Assert.AreEqual("products-grid", root.Id);
        Assert.AreEqual("grid", root.GetAttribute("data-test"));
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        StringAssert.Contains(root.GetAttribute("style"), "margin: 1px");

        foreach (var cls in new[] { "custom-class", "bit-rtl", "bit-dtg-rtl", "bit-dtg-bordered", "bit-dtg-striped", "bit-dtg-hoverable" })
        {
            Assert.IsTrue(root.ClassList.Contains(cls), cls);
        }
    }

    [TestMethod]
    public void BitDataGridShouldHideWithVisibilityCollapsed()
    {
        var component = RenderGrid(parameters => parameters.Add(p => p.Visibility, BitVisibility.Collapsed));

        StringAssert.Contains(component.Find(".bit-dtg").GetAttribute("style"), "display:none");
    }

    [TestMethod]
    public void BitDataGridShouldNameTheGridElementWithAriaLabel()
    {
        var component = RenderGrid(parameters => parameters.Add(p => p.AriaLabel, "Products"));

        Assert.AreEqual("Products", component.Find("[role=grid]").GetAttribute("aria-label"));
        Assert.IsFalse(component.Find(".bit-dtg").HasAttribute("aria-label"));
    }

    [TestMethod]
    public void BitDataGridShouldResetTheRootClassesWhenTheAppearanceChanges()
    {
        var component = RenderGrid();

        component.Render(parameters => parameters.Add(p => p.Striped, false).Add(p => p.Bordered, false));

        var root = component.Find(".bit-dtg");
        Assert.IsFalse(root.ClassList.Contains("bit-dtg-striped"));
        Assert.IsFalse(root.ClassList.Contains("bit-dtg-bordered"));
    }

    // ----------------------------------------------------------- disabled

    [TestMethod]
    public void BitDataGridShouldDisableEveryControlWhenNotEnabled()
    {
        var component = RenderGrid(parameters =>
        {
            parameters.Add(p => p.Disabled, true);
            parameters.Add(p => p.SelectionMode, BitDataGridSelectionMode.Multiple);
            parameters.Add(p => p.Filterable, true);
            parameters.Add(p => p.Resizable, true);
            parameters.Add(p => p.Reorderable, true);
            parameters.Add(p => p.Pageable, true);
            parameters.Add(p => p.PageSize, 2);
            parameters.Add(p => p.ShowSearchBox, true);
        });

        Assert.IsTrue(component.Find(".bit-dtg").ClassList.Contains("bit-dis"));
        Assert.AreEqual("true", component.Find("[role=grid]").GetAttribute("aria-disabled"));

        // The header keeps its title but offers no sort, resize or drag.
        Assert.AreEqual(0, component.FindAll("button.bit-dtg-htext").Count);
        Assert.AreEqual(0, component.FindAll(".bit-dtg-resizer").Count);
        Assert.IsTrue(component.FindAll(".bit-dtg-header-row .bit-dtg-hcell[draggable]").All(h => h.GetAttribute("draggable") == "false"));

        foreach (var control in component.FindAll(".bit-dtg button, .bit-dtg input, .bit-dtg select"))
        {
            Assert.IsTrue(control.HasAttribute("disabled"), control.OuterHtml);
        }
    }

    [TestMethod]
    public void BitDataGridShouldIgnoreRowClicksAndSelectionKeysWhenNotEnabled()
    {
        var clicks = 0;
        var component = RenderGrid(parameters =>
        {
            parameters.Add(p => p.Disabled, true);
            parameters.Add(p => p.SelectionMode, BitDataGridSelectionMode.Single);
            parameters.Add(p => p.CellNavigation, true);
            parameters.Add(p => p.OnRowClick, (Row _) => clicks++);
        });

        component.FindAll(".bit-dtg-body .bit-dtg-row")[0].Click();
        component.FindAll(".bit-dtg-body .bit-dtg-cell[tabindex]")[0].KeyDown(new KeyboardEventArgs { Key = " " });

        Assert.AreEqual(0, clicks);
        Assert.AreEqual(0, component.FindAll(".bit-dtg-selected").Count);
    }

    [TestMethod]
    public void BitDataGridShouldKeepCellNavigationWhenNotEnabled()
    {
        var component = RenderGrid(parameters =>
        {
            parameters.Add(p => p.Disabled, true);
            parameters.Add(p => p.CellNavigation, true);
            parameters.Add(p => p.Dir, BitDir.Ltr);
        });

        component.FindAll(".bit-dtg-body .bit-dtg-cell[tabindex]")[0].KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

        var focusable = component.FindAll(".bit-dtg-body .bit-dtg-cell[tabindex='0']");
        Assert.AreEqual(1, focusable.Count);
        Assert.AreEqual("2", focusable[0].TextContent.Trim());
    }

    // ------------------------------------------------------------ params

    [TestMethod]
    public void BitDataGridShouldTakeItsDefaultsFromTheCascadingParameters()
    {
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new IBitComponentParams[]
            {
                new BitDataGridParams
                {
                    Striped = false,
                    ShowRowNumbers = true,
                    Pageable = true,
                    PageSize = 2,
                    Strings = new BitDataGridStrings { GridLabel = "Shared label" },
                    Classes = new BitDataGridClassStyles { HeaderRow = "shared-header" },
                    Class = "shared-root",
                }
            });
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<BitDataGrid<Row>>(0);
                builder.AddComponentParameter(1, "Items", Rows());
                builder.AddComponentParameter(2, "ChildContent", Columns());
                builder.CloseComponent();

                builder.OpenComponent<BitDataGrid<Row>>(3);
                builder.AddComponentParameter(4, "Items", Rows());
                builder.AddComponentParameter(5, "ChildContent", Columns());
                builder.AddComponentParameter(6, "Striped", true);
                builder.AddComponentParameter(7, "PageSize", 3);
                builder.CloseComponent();
            }));
        });

        var roots = component.FindAll(".bit-dtg");
        Assert.AreEqual(2, roots.Count);

        var first = roots[0];
        Assert.IsFalse(first.ClassList.Contains("bit-dtg-striped"));
        Assert.IsTrue(first.ClassList.Contains("shared-root"));
        Assert.AreEqual("Shared label", first.QuerySelector("[role=grid]")!.GetAttribute("aria-label"));
        Assert.IsNotNull(first.QuerySelector(".bit-dtg-header-row.shared-header"));
        Assert.IsNotNull(first.QuerySelector(".bit-dtg-hcell-rownumber"));
        Assert.AreEqual(2, first.QuerySelectorAll(".bit-dtg-body > .bit-dtg-row").Length);

        // A parameter the grid sets itself wins over the cascade.
        var second = roots[1];
        Assert.IsTrue(second.ClassList.Contains("bit-dtg-striped"));
        Assert.AreEqual(3, second.QuerySelectorAll(".bit-dtg-body > .bit-dtg-row").Length);
    }

    [TestMethod]
    public void BitDataGridShouldRestoreAParameterTheCascadeStopsSupplying()
    {
        var withParams = new IBitComponentParams[] { new BitDataGridParams { Striped = false } };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, withParams);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<BitDataGrid<Row>>(0);
                builder.AddComponentParameter(1, "Items", Rows());
                builder.AddComponentParameter(2, "ChildContent", Columns());
                builder.CloseComponent();
            }));
        });

        Assert.IsFalse(component.Find(".bit-dtg").ClassList.Contains("bit-dtg-striped"));

        component.Render(parameters => parameters.Add(p => p.Parameters, Array.Empty<IBitComponentParams>()));

        Assert.IsTrue(component.Find(".bit-dtg").ClassList.Contains("bit-dtg-striped"));
    }

    // ------------------------------------------------------- class styles

    [TestMethod]
    public void BitDataGridShouldApplyTheClassesAndStylesOfItsParts()
    {
        var component = RenderGrid(parameters =>
        {
            parameters.Add(p => p.SelectionMode, BitDataGridSelectionMode.Multiple);
            parameters.Add(p => p.SelectedItems, new List<Row> { new() { Id = 1 } });
            parameters.Add(p => p.Pageable, true);
            parameters.Add(p => p.ShowToolbar, true);
            parameters.Add(p => p.Filterable, true);
            parameters.Add(p => p.Classes, new BitDataGridClassStyles
            {
                Root = "c-root",
                Toolbar = "c-toolbar",
                Viewport = "c-viewport",
                HeaderRow = "c-header-row",
                HeaderCell = "c-header-cell",
                FilterRow = "c-filter-row",
                Row = "c-row",
                SelectedRow = "c-selected",
                Cell = "c-cell",
                Pager = "c-pager",
            });
            parameters.Add(p => p.Styles, new BitDataGridClassStyles
            {
                Root = "color: red;",
                Pager = "padding: 0;",
                Row = "opacity: 0.9",
                SelectedRow = "outline: 1px solid;",
                Cell = "letter-spacing: 1px;",
                HeaderCell = "font-style: italic;",
                Viewport = "max-height: 10rem;",
            });
        });

        Assert.IsTrue(component.Find(".bit-dtg").ClassList.Contains("c-root"));
        StringAssert.Contains(component.Find(".bit-dtg").GetAttribute("style"), "color: red;");
        Assert.IsNotNull(component.Find(".bit-dtg-toolbar.c-toolbar"));
        Assert.IsNotNull(component.Find(".bit-dtg-viewport.c-viewport"));
        StringAssert.Contains(component.Find(".bit-dtg-viewport").GetAttribute("style"), "max-height: 10rem;");
        Assert.IsNotNull(component.Find(".bit-dtg-header-row.c-header-row"));
        Assert.IsNotNull(component.Find(".bit-dtg-filter-row.c-filter-row"));
        Assert.AreEqual(3, component.FindAll(".bit-dtg-header-row .bit-dtg-hcell.c-header-cell").Count);
        StringAssert.Contains(component.FindAll(".bit-dtg-header-row .bit-dtg-hcell.c-header-cell")[0].GetAttribute("style"), "font-style: italic;");
        Assert.IsNotNull(component.Find(".bit-dtg-pager.c-pager"));
        Assert.AreEqual("padding: 0;", component.Find(".bit-dtg-pager").GetAttribute("style"));

        var rows = component.FindAll(".bit-dtg-body > .bit-dtg-row");
        Assert.IsTrue(rows.All(r => r.ClassList.Contains("c-row")));
        Assert.AreEqual(1, rows.Count(r => r.ClassList.Contains("c-selected")));
        Assert.IsTrue(rows[0].ClassList.Contains("c-selected"));
        StringAssert.Contains(rows[0].GetAttribute("style"), "opacity: 0.9;outline: 1px solid;");
        Assert.IsFalse(rows[1].GetAttribute("style")!.Contains("outline"));

        var cells = component.FindAll(".bit-dtg-body .bit-dtg-cell.c-cell");
        Assert.AreEqual(9, cells.Count);
        StringAssert.Contains(cells[0].GetAttribute("style"), "letter-spacing: 1px;");
    }

    [TestMethod]
    public void BitDataGridShouldApplyTheEmptyAndLoadingClasses()
    {
        var component = RenderComponent<BitDataGrid<Row>>(parameters =>
        {
            parameters.Add(p => p.Items, new List<Row>());
            parameters.Add(p => p.ChildContent, Columns());
            parameters.Add(p => p.Classes, new BitDataGridClassStyles { Empty = "c-empty", Loading = "c-loading" });
        });

        Assert.IsNotNull(component.Find(".bit-dtg-empty.c-empty"));

        component.Render(parameters => parameters.Add(p => p.Loading, true));

        Assert.IsNotNull(component.Find(".bit-dtg-loading.c-loading"));
    }

    // ------------------------------------------------------------- a11y

    [TestMethod]
    public void BitDataGridShouldSpellOutTheSortPriorityAndHideTheArrows()
    {
        var component = RenderGrid();

        component.FindAll("button.bit-dtg-htext")[1].Click();
        component.FindAll("button.bit-dtg-htext")[2].Click(new MouseEventArgs { CtrlKey = true });

        var header = component.FindAll(".bit-dtg-header-row .bit-dtg-hcell")[2];
        Assert.AreEqual("true", header.QuerySelector(".bit-dtg-sort-icon")!.GetAttribute("aria-hidden"));
        Assert.AreEqual("true", header.QuerySelector(".bit-dtg-sort-priority")!.GetAttribute("aria-hidden"));
        Assert.AreEqual("sort priority 2", header.QuerySelector(".bit-dtg-visually-hidden")!.TextContent);
    }

    [TestMethod]
    public void BitDataGridShouldNameTheHeadersOfTheHandleAndToggleColumns()
    {
        var component = RenderGrid(parameters =>
        {
            parameters.Add(p => p.RowReorderable, true);
            parameters.Add(p => p.DetailTemplate, (Row r) => (RenderFragment)(b => b.AddContent(0, r.Name)));
        });

        var hidden = component.FindAll(".bit-dtg-header-row .bit-dtg-visually-hidden").Select(e => e.TextContent).ToList();

        CollectionAssert.Contains(hidden, "Reorder");
        CollectionAssert.Contains(hidden, "Details");
    }

    [TestMethod]
    public void BitDataGridShouldMakeTheColumnChooserADisclosure()
    {
        var component = RenderGrid(parameters => parameters.Add(p => p.ShowColumnChooser, true));

        var toggle = component.Find(".bit-dtg-toolbar button[aria-controls]");
        Assert.IsFalse(toggle.HasAttribute("aria-haspopup"));
        Assert.AreEqual("false", toggle.GetAttribute("aria-expanded"));

        toggle.Click();

        Assert.AreEqual("true", component.Find(".bit-dtg-toolbar button[aria-controls]").GetAttribute("aria-expanded"));

        component.Find(".bit-dtg-column-chooser").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.AreEqual(0, component.FindAll(".bit-dtg-column-chooser").Count);
        Assert.AreEqual("false", component.Find(".bit-dtg-toolbar button[aria-controls]").GetAttribute("aria-expanded"));
    }

    // ------------------------------------------------------ keyboard resize

    [TestMethod]
    public void BitDataGridShouldRenderTheResizeHandleAsAFocusableSeparator()
    {
        var component = RenderGrid(parameters => parameters.Add(p => p.Resizable, true));

        var resizer = component.FindAll(".bit-dtg-resizer")[1];

        Assert.AreEqual("separator", resizer.GetAttribute("role"));
        Assert.AreEqual("0", resizer.GetAttribute("tabindex"));
        Assert.AreEqual("vertical", resizer.GetAttribute("aria-orientation"));
        Assert.AreEqual("Resize Name", resizer.GetAttribute("aria-label"));
        Assert.AreEqual("100", resizer.GetAttribute("aria-valuenow"));
        Assert.AreEqual("60", resizer.GetAttribute("aria-valuemin"));
        Assert.IsFalse(resizer.HasAttribute("aria-valuemax"));
    }

    [TestMethod]
    public void BitDataGridShouldResizeAColumnFromTheKeyboard()
    {
        var states = new List<BitDataGridState>();
        var component = RenderGrid(parameters =>
        {
            parameters.Add(p => p.Resizable, true);
            parameters.Add(p => p.Dir, BitDir.Ltr);
            parameters.Add(p => p.OnStateChange, (BitDataGridState s) => states.Add(s));
        });

        component.FindAll(".bit-dtg-resizer")[1].KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        Assert.AreEqual("110", component.FindAll(".bit-dtg-resizer")[1].GetAttribute("aria-valuenow"));

        component.FindAll(".bit-dtg-resizer")[1].KeyDown(new KeyboardEventArgs { Key = "ArrowLeft", ShiftKey = true });
        Assert.AreEqual("60", component.FindAll(".bit-dtg-resizer")[1].GetAttribute("aria-valuenow"));

        component.FindAll(".bit-dtg-resizer")[1].KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });
        Assert.AreEqual("60", component.FindAll(".bit-dtg-resizer")[1].GetAttribute("aria-valuenow"), "MinWidth bounds the keyboard too.");

        Assert.IsTrue(states.Count >= 2);
        Assert.AreEqual(60, states[^1].Columns.Single(c => c.ColumnId == "Name").Width);
    }

    [TestMethod]
    public void BitDataGridShouldMirrorTheResizeKeysInRightToLeft()
    {
        var component = RenderGrid(parameters =>
        {
            parameters.Add(p => p.Resizable, true);
            parameters.Add(p => p.Dir, BitDir.Rtl);
        });

        component.FindAll(".bit-dtg-resizer")[1].KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });

        Assert.AreEqual("110", component.FindAll(".bit-dtg-resizer")[1].GetAttribute("aria-valuenow"));
    }

    [TestMethod]
    public void BitDataGridShouldTakeAColumnToItsLimitsWithHomeAndEnd()
    {
        RenderFragment columns = builder =>
        {
            builder.OpenComponent<BitDataGridColumn<Row>>(0);
            builder.AddComponentParameter(1, "Field", "Name");
            builder.AddComponentParameter(2, "Width", "100px");
            builder.AddComponentParameter(3, "MinWidth", 80);
            builder.AddComponentParameter(4, "MaxWidth", 300);
            builder.CloseComponent();
        };

        var component = RenderGrid(parameters => parameters.Add(p => p.Resizable, true), columns);

        component.Find(".bit-dtg-resizer").KeyDown(new KeyboardEventArgs { Key = "End" });
        Assert.AreEqual("300", component.Find(".bit-dtg-resizer").GetAttribute("aria-valuenow"));
        Assert.AreEqual("300", component.Find(".bit-dtg-resizer").GetAttribute("aria-valuemax"));

        component.Find(".bit-dtg-resizer").KeyDown(new KeyboardEventArgs { Key = "Home" });
        Assert.AreEqual("80", component.Find(".bit-dtg-resizer").GetAttribute("aria-valuenow"));
    }

    // -------------------------------------------------------- column moves

    [TestMethod]
    public void BitDataGridShouldMoveAColumnWithCtrlArrowOnItsHeader()
    {
        var component = RenderGrid(parameters =>
        {
            parameters.Add(p => p.Reorderable, true);
            parameters.Add(p => p.Dir, BitDir.Ltr);
        });

        component.FindAll(".bit-dtg-header-row .bit-dtg-hcell")[0].KeyDown(new KeyboardEventArgs { Key = "ArrowRight", CtrlKey = true });

        CollectionAssert.AreEqual(new[] { "Name", "Id", "Price" }, HeaderTitles(component).ToArray());
        StringAssert.StartsWith(component.Find(".bit-dtg-visually-hidden[role=status]").TextContent, "Id moved to position 2 of 3");

        // Plain arrows (no Ctrl) and a column at the end move nothing.
        component.FindAll(".bit-dtg-header-row .bit-dtg-hcell")[2].KeyDown(new KeyboardEventArgs { Key = "ArrowRight", CtrlKey = true });
        component.FindAll(".bit-dtg-header-row .bit-dtg-hcell")[0].KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });

        CollectionAssert.AreEqual(new[] { "Name", "Id", "Price" }, HeaderTitles(component).ToArray());
    }

    [TestMethod]
    public void BitDataGridShouldMirrorCtrlArrowInRightToLeft()
    {
        var component = RenderGrid(parameters =>
        {
            parameters.Add(p => p.Reorderable, true);
            parameters.Add(p => p.Dir, BitDir.Rtl);
        });

        component.FindAll(".bit-dtg-header-row .bit-dtg-hcell")[0].KeyDown(new KeyboardEventArgs { Key = "ArrowLeft", CtrlKey = true });

        CollectionAssert.AreEqual(new[] { "Name", "Id", "Price" }, HeaderTitles(component).ToArray());
    }

    [TestMethod]
    public void BitDataGridShouldNotMoveAColumnThatIsNotReorderable()
    {
        var component = RenderGrid(parameters => parameters.Add(p => p.Dir, BitDir.Ltr));

        component.FindAll(".bit-dtg-header-row .bit-dtg-hcell")[0].KeyDown(new KeyboardEventArgs { Key = "ArrowRight", CtrlKey = true });

        CollectionAssert.AreEqual(new[] { "Id", "Name", "Price" }, HeaderTitles(component).ToArray());
    }

    [TestMethod]
    public void BitDataGridShouldMoveColumnsFromTheColumnChooser()
    {
        var component = RenderGrid(parameters =>
        {
            parameters.Add(p => p.Reorderable, true);
            parameters.Add(p => p.ShowColumnChooser, true);
        });

        component.Find(".bit-dtg-toolbar button[aria-controls]").Click();

        var moves = component.FindAll(".bit-dtg-chooser-move");
        Assert.AreEqual(6, moves.Count);
        Assert.AreEqual("Move Id earlier", moves[0].GetAttribute("aria-label"));
        Assert.IsTrue(moves[0].HasAttribute("disabled"), "The first column has nowhere earlier to go.");
        Assert.IsTrue(moves[5].HasAttribute("disabled"), "The last column has nowhere later to go.");

        // Move Id later.
        moves[1].Click();

        CollectionAssert.AreEqual(new[] { "Name", "Id", "Price" }, HeaderTitles(component).ToArray());
        Assert.AreEqual("Move Name earlier", component.FindAll(".bit-dtg-chooser-move")[0].GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitDataGridShouldOfferNoMoveButtonsWithoutReordering()
    {
        var component = RenderGrid(parameters => parameters.Add(p => p.ShowColumnChooser, true));

        component.Find(".bit-dtg-toolbar button[aria-controls]").Click();

        Assert.AreEqual(0, component.FindAll(".bit-dtg-chooser-move").Count);
    }

    [TestMethod]
    public async Task BitDataGridShouldMoveAColumnFromCode()
    {
        var states = new List<BitDataGridState>();
        var component = RenderGrid(parameters => parameters.Add(p => p.OnStateChange, (BitDataGridState s) => states.Add(s)));

        await component.InvokeAsync(() => component.Instance.MoveColumnAsync("Price", 0));

        CollectionAssert.AreEqual(new[] { "Price", "Id", "Name" }, HeaderTitles(component).ToArray());
        Assert.AreEqual(1, states.Count);
        Assert.AreEqual(0, states[0].Columns.Single(c => c.ColumnId == "Price").Order);

        // An unknown id is ignored, and the index is clamped.
        await component.InvokeAsync(() => component.Instance.MoveColumnAsync("Nope", 0));
        await component.InvokeAsync(() => component.Instance.MoveColumnAsync("Id", 99));

        CollectionAssert.AreEqual(new[] { "Price", "Name", "Id" }, HeaderTitles(component).ToArray());
    }

    // ------------------------------------------------------- state change

    [TestMethod]
    public void BitDataGridShouldRaiseOnStateChangeAfterAUserChange()
    {
        var states = new List<BitDataGridState>();
        var component = RenderGrid(parameters => parameters.Add(p => p.OnStateChange, (BitDataGridState s) => states.Add(s)));

        Assert.AreEqual(0, states.Count, "The first render is not a change.");

        component.FindAll("button.bit-dtg-htext")[1].Click();

        Assert.AreEqual(1, states.Count);
        Assert.AreEqual("Name", states[0].Sorts.Single().ColumnId);
    }

    [TestMethod]
    public void BitDataGridShouldRaiseOnStateChangeWhenAColumnIsHidden()
    {
        var states = new List<BitDataGridState>();
        var component = RenderGrid(parameters =>
        {
            parameters.Add(p => p.ShowColumnChooser, true);
            parameters.Add(p => p.OnStateChange, (BitDataGridState s) => states.Add(s));
        });

        component.Find(".bit-dtg-toolbar button[aria-controls]").Click();
        component.FindAll(".bit-dtg-chooser-label input")[0].Change(false);

        Assert.AreEqual(1, states.Count);
        Assert.IsFalse(states[0].Columns.Single(c => c.ColumnId == "Id").Visible);
    }

    [TestMethod]
    public async Task BitDataGridShouldNotRaiseOnStateChangeWhenAStateIsApplied()
    {
        var states = new List<BitDataGridState>();
        var component = RenderGrid(parameters => parameters.Add(p => p.OnStateChange, (BitDataGridState s) => states.Add(s)));

        var state = component.Instance.GetState();
        state.Sorts.Add(new BitDataGridSortDescriptor { ColumnId = "Price", Direction = BitDataGridSortDirection.Descending, Priority = 1 });

        await component.InvokeAsync(() => component.Instance.ApplyStateAsync(state));

        Assert.AreEqual(0, states.Count);
        Assert.AreEqual(1, component.Instance.ActiveSorts.Count);
    }

    // ------------------------------------------------------------- layout

    [TestMethod]
    public void BitDataGridShouldKeepTheStickyBandsClearOfFocusedContent()
    {
        RenderFragment columns = builder =>
        {
            builder.OpenComponent<BitDataGridColumn<Row>>(0);
            builder.AddComponentParameter(1, "Field", "Id");
            builder.AddComponentParameter(2, "Width", "80px");
            builder.AddComponentParameter(3, "Frozen", true);
            builder.CloseComponent();

            builder.OpenComponent<BitDataGridColumn<Row>>(4);
            builder.AddComponentParameter(5, "Field", "Name");
            builder.AddComponentParameter(6, "Width", "200px");
            builder.CloseComponent();

            builder.OpenComponent<BitDataGridColumn<Row>>(7);
            builder.AddComponentParameter(8, "Field", "Price");
            builder.AddComponentParameter(9, "Width", "90px");
            builder.AddComponentParameter(10, "FrozenEnd", true);
            builder.CloseComponent();
        };

        var component = RenderGrid(parameters => parameters.Add(p => p.SelectionMode, BitDataGridSelectionMode.Multiple), columns);

        // The selection column (44px) is frozen too.
        var style = component.Find(".bit-dtg-viewport").GetAttribute("style")!;
        StringAssert.Contains(style, "--bit-dtg-frozen-start:124px;");
        StringAssert.Contains(style, "--bit-dtg-frozen-end:90px;");

        // The heights of the sticky header and footer are measured in the browser.
        Context.JSInterop.VerifyInvoke("BitBlazorUI.DataGrid.observeStickyBands");
    }

    [TestMethod]
    public void BitDataGridShouldHoldADeclaredWidthBetweenTheColumnBounds()
    {
        RenderFragment columns = builder =>
        {
            builder.OpenComponent<BitDataGridColumn<Row>>(0);
            builder.AddComponentParameter(1, "Field", "Name");
            builder.AddComponentParameter(2, "Width", "220px");
            builder.AddComponentParameter(3, "MinWidth", 120);
            builder.AddComponentParameter(4, "MaxWidth", 400);
            builder.CloseComponent();

            builder.OpenComponent<BitDataGridColumn<Row>>(5);
            builder.AddComponentParameter(6, "Field", "Price");
            builder.AddComponentParameter(7, "MaxWidth", 300);
            builder.CloseComponent();
        };

        var component = RenderGrid(columns: columns);

        var style = component.Find("[role=grid]").GetAttribute("style")!;

        // A minmax() let the declared width fall to MinWidth, and fr is not allowed inside min().
        StringAssert.Contains(style, "clamp(120px, 220px, 400px)");
        StringAssert.Contains(style, "minmax(60px, 300px)");
        Assert.IsFalse(style.Contains("min(1fr"));
    }

    [TestMethod]
    public void BitDataGridShouldIndentTreesAndGroupsThroughCssVariables()
    {
        var component = RenderGrid(parameters => parameters.Add(p => p.Groupable, true));

        component.FindAll(".bit-dtg-group-btn")[0].Click();

        var groupCell = component.Find(".bit-dtg-group-cell");
        Assert.IsFalse(groupCell.GetAttribute("style")!.Contains("padding"), "The indent comes from --bit-DataGrid-group-indent, not an inline padding.");
        StringAssert.Contains(component.Find(".bit-dtg-group-row").GetAttribute("style"), "--bit-dtg-group-level:0");
    }
}
