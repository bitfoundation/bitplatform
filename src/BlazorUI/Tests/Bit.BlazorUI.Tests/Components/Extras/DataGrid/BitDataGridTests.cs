using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.DataGrid;

[TestClass]
public class BitDataGridTests : BunitTestContext
{
    public class TestRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Price { get; set; }
    }

    private static List<TestRow> CreateRows() => new()
    {
        new TestRow { Id = 1, Name = "Banana", Price = 2.5 },
        new TestRow { Id = 2, Name = "Apple", Price = -5 },
        new TestRow { Id = 3, Name = "Cherry", Price = 10 },
        new TestRow { Id = 4, Name = "Date", Price = 7 },
        new TestRow { Id = 5, Name = "Elderberry", Price = 1 },
    };

    private static RenderFragment DefaultColumns() => builder =>
    {
        builder.OpenComponent<BitDataGridColumn<TestRow>>(0);
        builder.AddComponentParameter(1, "Field", "Name");
        builder.CloseComponent();
        builder.OpenComponent<BitDataGridColumn<TestRow>>(2);
        builder.AddComponentParameter(3, "Field", "Price");
        builder.CloseComponent();
    };

    private IRenderedComponent<BitDataGrid<TestRow>> RenderGrid(
        List<TestRow>? items = null,
        Action<ComponentParameterCollectionBuilder<BitDataGrid<TestRow>>>? configure = null)
    {
        return RenderComponent<BitDataGrid<TestRow>>(parameters =>
        {
            parameters.Add(p => p.Items, items ?? CreateRows());
            parameters.Add(p => p.ChildContent, DefaultColumns());
            configure?.Invoke(parameters);
        });
    }

    private static IReadOnlyList<string> FirstCellTexts(IRenderedComponent<BitDataGrid<TestRow>> component)
        => component.FindAll(".bit-dtg-body > .bit-dtg-row:not(.bit-dtg-message-row):not(.bit-dtg-placeholder-row)")
            .Select(r => r.QuerySelector(".bit-dtg-cell")!.TextContent.Trim())
            .ToList();

    [TestMethod]
    public void RendersOneRowPerItemWithHumanizedHeaders()
    {
        var component = RenderGrid();

        var rows = component.FindAll(".bit-dtg-body > .bit-dtg-row");
        Assert.AreEqual(5, rows.Count);

        var headers = component.FindAll(".bit-dtg-header-row .bit-dtg-htext").Select(h => h.TextContent.Trim()).ToList();
        CollectionAssert.Contains(headers, "Name");
        CollectionAssert.Contains(headers, "Price");
    }

    [TestMethod]
    public void ShowsEmptyMessageWhenNoItems()
    {
        var component = RenderGrid(new List<TestRow>());

        var empty = component.Find(".bit-dtg-empty");
        StringAssert.Contains(empty.TextContent, "No records to display.");
    }

    [TestMethod]
    public void StringsParameterLocalizesUi()
    {
        var component = RenderGrid(new List<TestRow>(), parameters =>
        {
            parameters.Add(p => p.Strings, new BitDataGridStrings { EmptyText = "Keine Daten." });
        });

        var empty = component.Find(".bit-dtg-empty");
        StringAssert.Contains(empty.TextContent, "Keine Daten.");
    }

    [TestMethod]
    public void HeaderClickSortsAscendingThenDescending()
    {
        var component = RenderGrid();

        component.FindAll(".bit-dtg-header-row .bit-dtg-htext")[0].Click();
        Assert.AreEqual("Apple", FirstCellTexts(component)[0]);

        component.FindAll(".bit-dtg-header-row .bit-dtg-htext")[0].Click();
        Assert.AreEqual("Elderberry", FirstCellTexts(component)[0]);
    }

    [TestMethod]
    public async Task SortByAsyncSortsProgrammatically()
    {
        var component = RenderGrid();

        await component.InvokeAsync(() => component.Instance.SortByAsync("Price", BitDataGridSortDirection.Descending));

        Assert.AreEqual("Cherry", FirstCellTexts(component)[0]);
        Assert.AreEqual(1, component.Instance.ActiveSorts.Count);
    }

    [TestMethod]
    public async Task TemplateOnlyColumnSortsViaSortBy()
    {
        var items = CreateRows();
        RenderFragment columns = builder =>
        {
            builder.OpenComponent<BitDataGridColumn<TestRow>>(0);
            builder.AddComponentParameter(1, "ColumnId", "custom");
            builder.AddComponentParameter(2, "Title", "Custom");
            builder.AddComponentParameter(3, "SortBy", (Func<TestRow, object?>)(r => r.Price));
            builder.AddComponentParameter(4, "Template", (RenderFragment<TestRow>)(row => b => b.AddContent(0, row.Name)));
            builder.CloseComponent();
        };

        var component = RenderComponent<BitDataGrid<TestRow>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.ChildContent, columns);
        });

        await component.InvokeAsync(() => component.Instance.SortByAsync("custom", BitDataGridSortDirection.Ascending));

        // Ascending by price: Apple (-5) first, Cherry (10) last.
        Assert.AreEqual("Apple", FirstCellTexts(component)[0]);
        Assert.AreEqual("Cherry", FirstCellTexts(component)[^1]);
    }

    [TestMethod]
    public async Task ApplyFilterAsyncFiltersRows()
    {
        var component = RenderGrid();

        await component.InvokeAsync(() => component.Instance.ApplyFilterAsync("Name", BitDataGridFilterOperator.Contains, "err"));

        var names = FirstCellTexts(component);
        Assert.AreEqual(2, names.Count); // Cherry, Elderberry
        CollectionAssert.Contains(names.ToList(), "Cherry");
        CollectionAssert.Contains(names.ToList(), "Elderberry");

        await component.InvokeAsync(() => component.Instance.ClearFilterAsync("Name"));
        Assert.AreEqual(5, FirstCellTexts(component).Count);
    }

    [TestMethod]
    public async Task PagingSlicesRowsAndNavigates()
    {
        var component = RenderGrid(configure: parameters =>
        {
            parameters.Add(p => p.Pageable, true);
            parameters.Add(p => p.PageSize, 2);
        });

        Assert.AreEqual(2, FirstCellTexts(component).Count);
        Assert.AreEqual(3, component.Instance.TotalPages);

        await component.InvokeAsync(() => component.Instance.GoToPageAsync(3));

        Assert.AreEqual(1, FirstCellTexts(component).Count);
        Assert.AreEqual(3, component.Instance.CurrentPage);
    }

    [TestMethod]
    public async Task SetPageSizeAsyncDoesNotMutatePageSizeParameter()
    {
        var component = RenderGrid(configure: parameters =>
        {
            parameters.Add(p => p.Pageable, true);
            parameters.Add(p => p.PageSize, 2);
        });

        await component.InvokeAsync(() => component.Instance.SetPageSizeAsync(4));

        // The parameter itself must stay untouched; only the effective page size changes.
        Assert.AreEqual(2, component.Instance.PageSize);
        Assert.AreEqual(4, FirstCellTexts(component).Count);
        Assert.AreEqual(2, component.Instance.TotalPages);
    }

    [TestMethod]
    public void RowSelectionRaisesSelectedItemsChanged()
    {
        IReadOnlyList<TestRow>? selected = null;
        var component = RenderGrid(configure: parameters =>
        {
            parameters.Add(p => p.SelectionMode, BitDataGridSelectionMode.Multiple);
            parameters.Add(p => p.SelectedItemsChanged, EventCallback.Factory.Create<IReadOnlyList<TestRow>>(this, v => selected = v));
        });

        var rowCheckboxes = component.FindAll(".bit-dtg-cell-select input");
        Assert.AreEqual(5, rowCheckboxes.Count);

        rowCheckboxes[0].Change(true);
        Assert.IsNotNull(selected);
        Assert.AreEqual(1, selected!.Count);
        Assert.AreEqual("Banana", selected[0].Name);
    }

    [TestMethod]
    public void SelectAllSelectsAllPageRows()
    {
        IReadOnlyList<TestRow>? selected = null;
        var component = RenderGrid(configure: parameters =>
        {
            parameters.Add(p => p.SelectionMode, BitDataGridSelectionMode.Multiple);
            parameters.Add(p => p.SelectedItemsChanged, EventCallback.Factory.Create<IReadOnlyList<TestRow>>(this, v => selected = v));
        });

        component.Find(".bit-dtg-header-row .bit-dtg-cell-select input, .bit-dtg-header-row input[type=checkbox]").Change(true);

        Assert.IsNotNull(selected);
        Assert.AreEqual(5, selected!.Count);
    }

    [TestMethod]
    public async Task GetStateAndApplyStateRoundTrip()
    {
        var component = RenderGrid(configure: parameters =>
        {
            parameters.Add(p => p.Pageable, true);
            parameters.Add(p => p.PageSize, 2);
        });

        await component.InvokeAsync(async () =>
        {
            await component.Instance.SortByAsync("Name", BitDataGridSortDirection.Descending);
            await component.Instance.ApplyFilterAsync("Name", BitDataGridFilterOperator.Contains, "e");
            await component.Instance.SetPageSizeAsync(4);
        });
        var state = component.Instance.GetState();

        Assert.AreEqual(1, state.Sorts.Count);
        Assert.AreEqual(1, state.Filters.Count);
        Assert.AreEqual(4, state.PageSize);
        Assert.AreEqual(2, state.Columns.Count);

        // A fresh grid restored from the snapshot reproduces the same view.
        var restored = RenderGrid(configure: parameters =>
        {
            parameters.Add(p => p.Pageable, true);
            parameters.Add(p => p.PageSize, 2);
        });
        await restored.InvokeAsync(() => restored.Instance.ApplyStateAsync(state));

        var names = FirstCellTexts(restored);
        var expected = CreateRows()
            .Where(r => r.Name.Contains('e', StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .Select(r => r.Name)
            .ToList();
        CollectionAssert.AreEqual(expected, names.ToList());
    }

    [TestMethod]
    public void ToCsvDoesNotCorruptNegativeNumbersButEscapesFormulas()
    {
        var items = new List<TestRow>
        {
            new() { Id = 1, Name = "=cmd()", Price = -5 },
        };
        var component = RenderGrid(items);

        var csv = component.Instance.ToCsv();

        StringAssert.Contains(csv, "'=cmd()");
        StringAssert.Contains(csv, "-5");
        Assert.IsFalse(csv.Contains("'-5"), "negative numbers must not get a formula-guard prefix");
    }

    [TestMethod]
    public void HidingColumnViaParameterUpdatesRenderedColumns()
    {
        var visible = true;
        RenderFragment Columns() => builder =>
        {
            builder.OpenComponent<BitDataGridColumn<TestRow>>(0);
            builder.AddComponentParameter(1, "Field", "Name");
            builder.CloseComponent();
            builder.OpenComponent<BitDataGridColumn<TestRow>>(2);
            builder.AddComponentParameter(3, "Field", "Price");
            builder.AddComponentParameter(4, "Visible", visible);
            builder.CloseComponent();
        };

        var component = RenderComponent<BitDataGrid<TestRow>>(parameters =>
        {
            parameters.Add(p => p.Items, CreateRows());
            parameters.Add(p => p.ChildContent, Columns());
        });

        Assert.AreEqual(2, component.FindAll(".bit-dtg-header-row .bit-dtg-hcell").Count);

        visible = false;
        component.Render(parameters => parameters.Add(p => p.ChildContent, Columns()));

        Assert.AreEqual(1, component.FindAll(".bit-dtg-header-row .bit-dtg-hcell").Count);
    }

    [TestMethod]
    public void EditingBuffersChangesUntilSave()
    {
        var items = CreateRows();
        var component = RenderGrid(items, parameters => parameters.Add(p => p.Editable, true));

        // Enter edit mode on the first row and type into the Name editor.
        component.FindAll(".bit-dtg-cell-command button")[0].Click();
        component.Find("input.bit-dtg-editor[type=text]").Input("Changed");

        // The live object must stay untouched while the edit is in progress.
        Assert.AreEqual("Banana", items[0].Name);

        // Save applies the buffer.
        component.FindAll(".bit-dtg-cell-command button")[0].Click();
        Assert.AreEqual("Changed", items[0].Name);
    }

    [TestMethod]
    public void CancelDiscardsBufferedEdits()
    {
        var items = CreateRows();
        var component = RenderGrid(items, parameters => parameters.Add(p => p.Editable, true));

        component.FindAll(".bit-dtg-cell-command button")[0].Click();
        component.Find("input.bit-dtg-editor[type=text]").Input("Changed");
        component.FindAll(".bit-dtg-cell-command button")[1].Click(); // Cancel

        Assert.AreEqual("Banana", items[0].Name);
        Assert.AreEqual(0, component.FindAll(".bit-dtg-editing").Count, "edit mode should have ended");
    }

    [TestMethod]
    public void InvalidEditShowsErrorAndBlocksSave()
    {
        var items = CreateRows();
        RenderFragment columns = builder =>
        {
            builder.OpenComponent<BitDataGridColumn<TestRow>>(0);
            builder.AddComponentParameter(1, "Field", "Name");
            builder.AddComponentParameter(2, "Validate",
                (Func<TestRow, object?, string?>)((_, v) => string.IsNullOrEmpty(v as string) ? "Required" : null));
            builder.CloseComponent();
        };
        var component = RenderComponent<BitDataGrid<TestRow>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Editable, true);
            parameters.Add(p => p.ChildContent, columns);
        });

        component.FindAll(".bit-dtg-cell-command button")[0].Click();
        component.Find("input.bit-dtg-editor[type=text]").Input("");

        var error = component.Find(".bit-dtg-editor-error");
        Assert.AreEqual("Required", error.TextContent.Trim());

        var save = component.FindAll(".bit-dtg-cell-command button")[0];
        Assert.IsTrue(save.HasAttribute("disabled"), "Save must be disabled while invalid");

        // Even a forced click must not commit or leave edit mode.
        save.Click();
        Assert.AreEqual("Banana", items[0].Name);
        Assert.AreEqual(1, component.FindAll(".bit-dtg-editing").Count, "row must stay in edit mode");
    }

    [TestMethod]
    public void FilterOperatorMenuAppliesChosenOperator()
    {
        var component = RenderGrid(configure: parameters =>
        {
            parameters.Add(p => p.Filterable, true);
            parameters.Add(p => p.FilterOperators, true);
        });

        // Two operator dropdowns render (Name: text ops, Price: comparison ops).
        var opSelects = component.FindAll(".bit-dtg-filter-op");
        Assert.AreEqual(2, opSelects.Count);

        // Price > 2 → Banana (2.5), Cherry (10), Date (7).
        component.FindAll(".bit-dtg-filter-op")[1].Change("GreaterThan");
        component.Find("input.bit-dtg-filter-input[type=number]").Change("2");
        Assert.AreEqual(3, FirstCellTexts(component).Count);

        // Switching the operator re-applies the same filter text: Price < 2 → Apple (-5), Elderberry (1).
        component.FindAll(".bit-dtg-filter-op")[1].Change("LessThan");
        Assert.AreEqual(2, FirstCellTexts(component).Count);
    }

    [TestMethod]
    public void TextFilterOperatorStartsWithFilters()
    {
        var component = RenderGrid(configure: parameters =>
        {
            parameters.Add(p => p.Filterable, true);
            parameters.Add(p => p.FilterOperators, true);
        });

        component.FindAll(".bit-dtg-filter-op")[0].Change("StartsWith");
        component.Find("input.bit-dtg-filter-input:not([type=number])").Change("ba");

        var names = FirstCellTexts(component);
        Assert.AreEqual(1, names.Count);
        Assert.AreEqual("Banana", names[0]);
    }

    [TestMethod]
    public void FrozenEndColumnIsStickyWithEndOffset()
    {
        RenderFragment columns = builder =>
        {
            builder.OpenComponent<BitDataGridColumn<TestRow>>(0);
            builder.AddComponentParameter(1, "Field", "Name");
            builder.CloseComponent();
            builder.OpenComponent<BitDataGridColumn<TestRow>>(2);
            builder.AddComponentParameter(3, "Field", "Price");
            builder.AddComponentParameter(4, "FrozenEnd", true);
            builder.CloseComponent();
        };
        var component = RenderComponent<BitDataGrid<TestRow>>(parameters =>
        {
            parameters.Add(p => p.Items, CreateRows());
            parameters.Add(p => p.ChildContent, columns);
        });

        var headerCells = component.FindAll(".bit-dtg-header-row .bit-dtg-hcell");
        Assert.IsFalse(headerCells[0].ClassList.Contains("bit-dtg-sticky"));
        Assert.IsTrue(headerCells[1].ClassList.Contains("bit-dtg-sticky"));
        StringAssert.Contains(headerCells[1].GetAttribute("style") ?? "", "right:0px");

        var firstRowCells = component.FindAll(".bit-dtg-body > .bit-dtg-row")[0].QuerySelectorAll(".bit-dtg-cell");
        Assert.IsTrue(firstRowCells[1].ClassList.Contains("bit-dtg-sticky"));
    }

    [TestMethod]
    public void AggregateByComputesCustomFooterValue()
    {
        RenderFragment columns = builder =>
        {
            builder.OpenComponent<BitDataGridColumn<TestRow>>(0);
            builder.AddComponentParameter(1, "Field", "Name");
            builder.AddComponentParameter(2, "AggregateBy",
                (Func<IReadOnlyList<TestRow>, object?>)(rows => rows.Select(r => r.Name[0]).Distinct().Count()));
            // Render the aggregate's Type so the test can assert an AggregateBy result reaches the
            // FooterTemplate marked Custom (distinguishable from both built-ins and no aggregation).
            builder.AddComponentParameter(3, "FooterTemplate",
                (RenderFragment<BitDataGridAggregateResult>)(agg => b => b.AddContent(0, $"{agg.Type}:{agg.FormattedValue}")));
            builder.CloseComponent();
        };
        var component = RenderComponent<BitDataGrid<TestRow>>(parameters =>
        {
            parameters.Add(p => p.Items, CreateRows());
            parameters.Add(p => p.ShowFooter, true);
            parameters.Add(p => p.ChildContent, columns);
        });

        // Five products with five distinct first letters.
        var footer = component.Find(".bit-dtg-footer-row .bit-dtg-cell");
        StringAssert.Contains(footer.TextContent, "Custom:5");
    }

    [TestMethod]
    public void DeleteKeyDeletesTheFocusedRowAndKeepsFocusInGrid()
    {
        var items = CreateRows();
        var component = RenderGrid(items, parameters =>
        {
            parameters.Add(p => p.CellNavigation, true);
            parameters.Add(p => p.Editable, true);
            parameters.Add(p => p.OnRowDelete, (TestRow r) => { items.Remove(r); });
        });

        component.Find(".bit-dtg-body > .bit-dtg-row .bit-dtg-cell")
            .KeyDown(new KeyboardEventArgs { Key = "Delete" });

        var texts = FirstCellTexts(component);
        Assert.AreEqual(4, texts.Count);
        Assert.IsFalse(texts.Contains("Banana"), "the focused row must be deleted");
        // The row that took the deleted row's place now holds the roving tab stop.
        var newFirstCell = component.Find(".bit-dtg-body > .bit-dtg-row .bit-dtg-cell");
        Assert.AreEqual("0", newFirstCell.GetAttribute("tabindex"));
    }

    [TestMethod]
    public void DeleteKeyIsIgnoredWhenGridIsNotEditable()
    {
        var items = CreateRows();
        var component = RenderGrid(items, parameters =>
        {
            parameters.Add(p => p.CellNavigation, true);
            parameters.Add(p => p.OnRowDelete, (TestRow r) => { items.Remove(r); });
        });

        component.Find(".bit-dtg-body > .bit-dtg-row .bit-dtg-cell")
            .KeyDown(new KeyboardEventArgs { Key = "Delete" });

        Assert.AreEqual(5, FirstCellTexts(component).Count);
    }

    [TestMethod]
    public void ServerProvidedAggregatesShowInFooter()
    {
        RenderFragment columns = builder =>
        {
            builder.OpenComponent<BitDataGridColumn<TestRow>>(0);
            builder.AddComponentParameter(1, "Field", "Price");
            builder.AddComponentParameter(2, "Aggregate", BitDataGridAggregateType.Sum);
            builder.CloseComponent();
        };
        var component = RenderComponent<BitDataGrid<TestRow>>(parameters =>
        {
            parameters.Add(p => p.OnRead, (Func<BitDataGridReadRequest, Task<BitDataGridReadResult<TestRow>>>)(request =>
                Task.FromResult(new BitDataGridReadResult<TestRow>(CreateRows().Take(2).ToList(), 5)
                {
                    Aggregates = new List<BitDataGridAggregateResult>
                    {
                        new() { ColumnId = "Price", Type = BitDataGridAggregateType.Sum, Value = 15.5m, FormattedValue = "15.5 (all rows)" },
                    },
                })));
            parameters.Add(p => p.ShowFooter, true);
            parameters.Add(p => p.ChildContent, columns);
        });

        var footer = component.Find(".bit-dtg-footer-row .bit-dtg-cell");
        StringAssert.Contains(footer.TextContent, "15.5 (all rows)");
    }

    [TestMethod]
    public async Task AriaIndicesReflectDatasetPositions()
    {
        var component = RenderGrid(configure: parameters =>
        {
            parameters.Add(p => p.Pageable, true);
            parameters.Add(p => p.PageSize, 2);
        });

        // 5 data rows + 1 header row: aria-rowcount spans the whole grid, headers included.
        Assert.AreEqual("6", component.Find(".bit-dtg-table").GetAttribute("aria-rowcount"));
        Assert.AreEqual("2", component.Find(".bit-dtg-table").GetAttribute("aria-colcount"));
        Assert.AreEqual("1", component.Find(".bit-dtg-header-row").GetAttribute("aria-rowindex"));

        // Page 2 starts at dataset position 3 → aria-rowindex 4 (header row occupies index 1).
        await component.InvokeAsync(() => component.Instance.GoToPageAsync(2));

        var rows = component.FindAll(".bit-dtg-body > .bit-dtg-row:not(.bit-dtg-message-row)");
        Assert.AreEqual("4", rows[0].GetAttribute("aria-rowindex"));
        Assert.AreEqual("5", rows[1].GetAttribute("aria-rowindex"));
        Assert.AreEqual("1", rows[0].QuerySelectorAll(".bit-dtg-cell")[0].GetAttribute("aria-colindex"));
        Assert.AreEqual("2", rows[0].QuerySelectorAll(".bit-dtg-cell")[1].GetAttribute("aria-colindex"));
    }

    [TestMethod]
    public void AriaRowIndicesStaySequentialWithGroupHeaderAndFilterRows()
    {
        var component = RenderComponent<BitDataGrid<TestRow>>(parameters =>
        {
            parameters.Add(p => p.Items, CreateRows());
            parameters.Add(p => p.Filterable, true);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<BitDataGridColumn<TestRow>>(0);
                builder.AddComponentParameter(1, "Field", "Name");
                builder.AddComponentParameter(2, "Group", "Info");
                builder.CloseComponent();
                builder.OpenComponent<BitDataGridColumn<TestRow>>(3);
                builder.AddComponentParameter(4, "Field", "Price");
                builder.AddComponentParameter(5, "Group", "Info");
                builder.CloseComponent();
            }));
        });

        // 3 header rows (group header, header, filter) + 5 data rows, indexed as one sequence.
        Assert.AreEqual("8", component.Find(".bit-dtg-table").GetAttribute("aria-rowcount"));
        Assert.AreEqual("1", component.Find(".bit-dtg-group-header-row").GetAttribute("aria-rowindex"));
        Assert.AreEqual("2", component.Find(".bit-dtg-header-row").GetAttribute("aria-rowindex"));
        Assert.AreEqual("3", component.Find(".bit-dtg-filter-row").GetAttribute("aria-rowindex"));
        var rows = component.FindAll(".bit-dtg-body > .bit-dtg-row:not(.bit-dtg-message-row)");
        Assert.AreEqual("4", rows[0].GetAttribute("aria-rowindex"));
    }

    [TestMethod]
    public void SortAndPageChangesAreAnnounced()
    {
        var component = RenderGrid(configure: parameters =>
        {
            parameters.Add(p => p.Pageable, true);
            parameters.Add(p => p.PageSize, 2);
        });

        component.FindAll(".bit-dtg-header-row .bit-dtg-htext")[0].Click();
        StringAssert.Contains(component.Find("[aria-live]").TextContent, "Sorted by Name, ascending");

        component.FindAll(".bit-dtg-header-row .bit-dtg-htext")[0].Click();
        StringAssert.Contains(component.Find("[aria-live]").TextContent, "Sorted by Name, descending");

        component.Find("button[aria-label='Next page']").Click();
        StringAssert.Contains(component.Find("[aria-live]").TextContent, "Page 2 of 3");
    }

    [TestMethod]
    public async Task ExcelExportProducesValidWorkbookWithNativeValues()
    {
        var component = RenderGrid();

        byte[] bytes = null!;
        await component.InvokeAsync(async () => bytes = await component.Instance.ToExcelAsync());

        using var stream = new System.IO.MemoryStream(bytes);
        using var zip = new System.IO.Compression.ZipArchive(stream, System.IO.Compression.ZipArchiveMode.Read);
        Assert.IsNotNull(zip.GetEntry("[Content_Types].xml"));
        Assert.IsNotNull(zip.GetEntry("xl/workbook.xml"));
        Assert.IsNotNull(zip.GetEntry("xl/styles.xml"));
        var sheetEntry = zip.GetEntry("xl/worksheets/sheet1.xml");
        Assert.IsNotNull(sheetEntry);

        using var reader = new System.IO.StreamReader(sheetEntry!.Open());
        var sheet = reader.ReadToEnd();
        StringAssert.Contains(sheet, "<t xml:space=\"preserve\">Banana</t>");
        StringAssert.Contains(sheet, "<v>-5</v>", "numbers must be native cells, not text");
        StringAssert.Contains(sheet, "<c t=\"inlineStr\" s=\"1\">", "header cells must use the bold style");
        StringAssert.Contains(sheet, "<pane ySplit=\"1\" topLeftCell=\"A2\" activePane=\"bottomLeft\" state=\"frozen\"/>",
            "the header row must be frozen (the grid header is always sticky)");
        StringAssert.Contains(sheet, "<cols>", "grid column widths must carry over");
    }

    private static async Task<string> ExportSheetXmlAsync(IRenderedComponent<BitDataGrid<TestRow>> component)
    {
        byte[] bytes = null!;
        await component.InvokeAsync(async () => bytes = await component.Instance.ToExcelAsync());
        using var stream = new System.IO.MemoryStream(bytes);
        using var zip = new System.IO.Compression.ZipArchive(stream, System.IO.Compression.ZipArchiveMode.Read);
        using var reader = new System.IO.StreamReader(zip.GetEntry("xl/worksheets/sheet1.xml")!.Open());
        return reader.ReadToEnd();
    }

    [TestMethod]
    public async Task ExcelExportStyledBakesRenderedThemeIntoWorkbook()
    {
        // The grid samples its rendered theme via JS; simulate what getExportStyles returns.
        Context.JSInterop.Setup<BitDataGridExcelStyle?>("BitBlazorUI.DataGrid.getExportStyles", _ => true)
            .SetResult(new BitDataGridExcelStyle
            {
                HeaderBackground = "#112233",
                HeaderForeground = "#ffffff",
                HeaderBold = true,
                RowForeground = "#010203",
                RowItalic = true,
                StripeBackground = "#f4f4f4",
                BorderColor = "#dddddd",
            });
        var component = RenderGrid(configure: parameters =>
        {
            parameters.Add(p => p.ExcelExportStyled, true);
            parameters.Add(p => p.Striped, true);
            parameters.Add(p => p.Bordered, true);
        });

        byte[] bytes = null!;
        await component.InvokeAsync(async () => bytes = await component.Instance.ToExcelAsync());

        using var stream = new System.IO.MemoryStream(bytes);
        using var zip = new System.IO.Compression.ZipArchive(stream, System.IO.Compression.ZipArchiveMode.Read);
        using var stylesReader = new System.IO.StreamReader(zip.GetEntry("xl/styles.xml")!.Open());
        var styles = stylesReader.ReadToEnd();
        StringAssert.Contains(styles, "<fgColor rgb=\"FF112233\"/>", "header fill must carry the sampled background");
        StringAssert.Contains(styles, "<color rgb=\"FFFFFFFF\"/>", "header font must carry the sampled foreground");
        StringAssert.Contains(styles, "<i/>", "the sampled italic row font must be written");
        StringAssert.Contains(styles, "<fgColor rgb=\"FFF4F4F4\"/>", "the stripe fill must be written");
        StringAssert.Contains(styles, "<left style=\"thin\"><color rgb=\"FFDDDDDD\"/></left>",
            "Bordered mode must produce vertical borders in the sampled color");

        using var sheetReader = new System.IO.StreamReader(zip.GetEntry("xl/worksheets/sheet1.xml")!.Open());
        var sheet = sheetReader.ReadToEnd();
        // Data rows alternate between the default format (implicit) and the stripe format s="2",
        // matching the grid's nth-child(even) striping; Apple is the second data row.
        StringAssert.Contains(sheet, "<c t=\"inlineStr\" s=\"2\"><is><t xml:space=\"preserve\">Apple</t>");
        Assert.IsFalse(sheet.Contains("<c t=\"inlineStr\" s=\"2\"><is><t xml:space=\"preserve\">Banana</t>"),
            "odd data rows must keep the default (non-stripe) format");
    }

    [TestMethod]
    public async Task ExcelExportRepresentsColumnSpansAsMergedCells()
    {
        RenderFragment columns = builder =>
        {
            builder.OpenComponent<BitDataGridColumn<TestRow>>(0);
            builder.AddComponentParameter(1, "Field", "Name");
            builder.AddComponentParameter(2, "ColSpan", (Func<TestRow, int?>)(r => r.Id == 1 ? 2 : null));
            builder.CloseComponent();
            builder.OpenComponent<BitDataGridColumn<TestRow>>(3);
            builder.AddComponentParameter(4, "Field", "Price");
            builder.CloseComponent();
        };
        var component = RenderComponent<BitDataGrid<TestRow>>(parameters =>
        {
            parameters.Add(p => p.Items, CreateRows());
            parameters.Add(p => p.ChildContent, columns);
        });

        // The span is really active: Banana's row renders one field cell (Name covering Price).
        Assert.IsNotNull(component.Find(".bit-dtg-cell[style*='span 2']"));

        var sheet = await ExportSheetXmlAsync(component);

        // Banana is the first data row (sheet row 2): its Name cell merges over the Price column,
        // and the covered Price value is omitted (a merged region keeps only its top-left value).
        StringAssert.Contains(sheet, "<mergeCells count=\"1\"><mergeCell ref=\"A2:B2\"/></mergeCells>");
        Assert.IsFalse(sheet.Contains("<v>2.5</v>"), "the value covered by a merged cell must not be written");
        // Other rows keep their Price values (their spans are inactive).
        StringAssert.Contains(sheet, "<v>-5</v>");

        // CSV has no merge concept and stays flat, covered values included.
        string csv = null!;
        await component.InvokeAsync(async () => csv = await component.Instance.ToCsvAsync());
        StringAssert.Contains(csv, "2.5");
    }

    [TestMethod]
    public async Task ExportKeepsDeclaredColumnOrderWithFrozenColumns()
    {
        RenderFragment columns = builder =>
        {
            builder.OpenComponent<BitDataGridColumn<TestRow>>(0);
            builder.AddComponentParameter(1, "Field", "Id");
            builder.AddComponentParameter(2, "Frozen", true);
            builder.CloseComponent();
            builder.OpenComponent<BitDataGridColumn<TestRow>>(3);
            builder.AddComponentParameter(4, "Field", "Name");
            builder.CloseComponent();
            builder.OpenComponent<BitDataGridColumn<TestRow>>(5);
            builder.AddComponentParameter(6, "Field", "Price");
            builder.AddComponentParameter(7, "FrozenEnd", true);
            builder.CloseComponent();
        };
        var component = RenderComponent<BitDataGrid<TestRow>>(parameters =>
        {
            parameters.Add(p => p.Items, CreateRows());
            parameters.Add(p => p.ChildContent, columns);
        });

        // Pinning is really active on both edges.
        Assert.IsTrue(component.FindAll(".bit-dtg-header-row .bit-dtg-sticky").Count >= 2);

        string csv = null!;
        await component.InvokeAsync(async () => csv = await component.Instance.ToCsvAsync());
        StringAssert.StartsWith(csv, "Id,Name,Price", "frozen columns keep the declared order");

        var sheet = await ExportSheetXmlAsync(component);
        var headerRow = sheet[..sheet.IndexOf("</row>", StringComparison.Ordinal)];
        Assert.IsTrue(
            headerRow.IndexOf(">Id<", StringComparison.Ordinal) < headerRow.IndexOf(">Name<", StringComparison.Ordinal)
            && headerRow.IndexOf(">Name<", StringComparison.Ordinal) < headerRow.IndexOf(">Price<", StringComparison.Ordinal),
            "the Excel header row must keep the declared column order");

        // The leading Frozen column becomes an Excel freeze pane (together with the header row);
        // FrozenEnd has no workbook equivalent and must not affect the pane.
        StringAssert.Contains(sheet, "<pane xSplit=\"1\" ySplit=\"1\" topLeftCell=\"B2\" activePane=\"bottomRight\" state=\"frozen\"/>");
    }

    [TestMethod]
    public async Task ExcelExportCoversMasterRowsOnlyWithDetailTemplate()
    {
        RenderFragment columns = builder =>
        {
            builder.OpenComponent<BitDataGridColumn<TestRow>>(0);
            builder.AddComponentParameter(1, "Field", "Name");
            builder.CloseComponent();
            // A template-only column (no Field) has no exportable value and must be skipped.
            builder.OpenComponent<BitDataGridColumn<TestRow>>(2);
            builder.AddComponentParameter(3, "ColumnId", "Computed");
            builder.AddComponentParameter(4, "Title", "Computed");
            builder.AddComponentParameter(5, "Template", (RenderFragment<TestRow>)(r => b => b.AddContent(0, $"{r.Name}-{r.Price}")));
            builder.CloseComponent();
        };
        var component = RenderComponent<BitDataGrid<TestRow>>(parameters =>
        {
            parameters.Add(p => p.Items, CreateRows());
            parameters.Add(p => p.ChildContent, columns);
            parameters.Add(p => p.DetailTemplate, (RenderFragment<TestRow>)(r => b => b.AddContent(0, "DETAIL-CONTENT")));
        });

        // Expand a detail row so its content is actually rendered when the export runs.
        component.FindAll(".bit-dtg-cell-detail button")[0].Click();
        Assert.IsNotNull(component.Find(".bit-dtg-detail-row"));

        var sheet = await ExportSheetXmlAsync(component);

        Assert.AreEqual(6, sheet.Split("<row>").Length - 1, "export must cover the header plus one row per master item");
        Assert.IsFalse(sheet.Contains("DETAIL-CONTENT"), "detail template content must not be exported");
        Assert.IsFalse(sheet.Contains("Computed"), "template-only columns have no field and must be skipped");
    }

    [TestMethod]
    public async Task ExportCoversAllRowsInInfiniteScrollingMode()
    {
        var all = CreateRows();
        BitDataGridReadRequest? exportRequest = null;
        var component = RenderComponent<BitDataGrid<TestRow>>(parameters =>
        {
            parameters.Add(p => p.Height, "300px");
            parameters.Add(p => p.LoadMoreBatchSize, 2);
            parameters.Add(p => p.OnLoadMore, (Func<BitDataGridReadRequest, Task<BitDataGridReadResult<TestRow>>>)(req =>
            {
                IEnumerable<TestRow> rows = all.Skip(req.Skip);
                if (req.Take is { } take) rows = rows.Take(take);
                else exportRequest = req;
                return Task.FromResult(new BitDataGridReadResult<TestRow>(rows.ToList(), 0));
            }));
            parameters.Add(p => p.ChildContent, DefaultColumns());
        });

        // Only the first batch is loaded/rendered.
        component.WaitForAssertion(() => Assert.AreEqual(2, FirstCellTexts(component).Count));

        string csv = null!;
        await component.InvokeAsync(async () => csv = await component.Instance.ToCsvAsync());

        Assert.IsNotNull(exportRequest, "export must request all rows (Take = null) through OnLoadMore");
        Assert.AreEqual(0, exportRequest!.Skip);
        foreach (var row in all) StringAssert.Contains(csv, row.Name);
    }

    [TestMethod]
    public async Task ExportIncludesCollapsedTreeBranches()
    {
        RenderFragment columns = builder =>
        {
            builder.OpenComponent<BitDataGridColumn<TreeNode>>(0);
            builder.AddComponentParameter(1, "Field", "Name");
            builder.CloseComponent();
        };
        var roots = new List<TreeNode>
        {
            new()
            {
                Id = 1, Name = "root",
                Children = new() { new() { Id = 11, Name = "child-a" }, new() { Id = 12, Name = "child-b" } },
            },
        };

        var component = RenderComponent<BitDataGrid<TreeNode>>(parameters =>
        {
            parameters.Add(p => p.Items, roots);
            parameters.Add(p => p.KeyField, (Func<TreeNode, object>)(n => n.Id));
            parameters.Add(p => p.ChildrenSelector, (Func<TreeNode, IEnumerable<TreeNode>?>)(n => n.Children));
            parameters.Add(p => p.ChildContent, columns);
        });

        // The tree renders collapsed: only the root row is visible.
        Assert.AreEqual(1, component.FindAll(".bit-dtg-body > .bit-dtg-row").Count);

        string csv = null!;
        await component.InvokeAsync(async () => csv = await component.Instance.ToCsvAsync());

        StringAssert.Contains(csv, "root");
        StringAssert.Contains(csv, "child-a");
        StringAssert.Contains(csv, "child-b");

        // The synchronous ToCsv resolves the same rows without a provider round-trip.
        string syncCsv = null!;
        await component.InvokeAsync(() => syncCsv = component.Instance.ToCsv());
        Assert.AreEqual(csv, syncCsv);
    }

    [TestMethod]
    public async Task QueryableSourcePagesSortsAndFilters()
    {
        var component = RenderComponent<BitDataGrid<TestRow>>(parameters =>
        {
            parameters.Add(p => p.Items, CreateRows().AsQueryable());
            parameters.Add(p => p.Pageable, true);
            parameters.Add(p => p.PageSize, 2);
            parameters.Add(p => p.ChildContent, DefaultColumns());
        });

        Assert.AreEqual(2, FirstCellTexts(component).Count);
        Assert.AreEqual(5, component.Instance.TotalCount);
        Assert.AreEqual(3, component.Instance.TotalPages);

        await component.InvokeAsync(() => component.Instance.SortByAsync("Price", BitDataGridSortDirection.Descending));
        Assert.AreEqual("Cherry", FirstCellTexts(component)[0]);

        await component.InvokeAsync(() => component.Instance.ApplyFilterAsync("Price", BitDataGridFilterOperator.GreaterThan, 2.0));
        Assert.AreEqual(3, component.Instance.TotalCount); // Banana 2.5, Cherry 10, Date 7
        Assert.AreEqual("Cherry", FirstCellTexts(component)[0]);
    }

    public class DatedRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime Created { get; set; }
        public DateTimeOffset? Shipped { get; set; }
    }

    [TestMethod]
    public async Task QueryableDateEqualsFiltersCompareCalendarDays()
    {
        var rows = new List<DatedRow>
        {
            new() { Id = 1, Name = "Morning", Created = new DateTime(2024, 5, 1, 8, 30, 0), Shipped = new DateTimeOffset(2024, 5, 1, 23, 0, 0, TimeSpan.FromHours(2)) },
            new() { Id = 2, Name = "Evening", Created = new DateTime(2024, 5, 1, 22, 15, 0), Shipped = null },
            new() { Id = 3, Name = "NextDay", Created = new DateTime(2024, 5, 2, 0, 0, 0), Shipped = new DateTimeOffset(2024, 5, 2, 1, 0, 0, TimeSpan.Zero) },
        };

        RenderFragment columns = builder =>
        {
            builder.OpenComponent<BitDataGridColumn<DatedRow>>(0);
            builder.AddComponentParameter(1, "Field", "Name");
            builder.CloseComponent();
            builder.OpenComponent<BitDataGridColumn<DatedRow>>(2);
            builder.AddComponentParameter(3, "Field", "Created");
            builder.CloseComponent();
            builder.OpenComponent<BitDataGridColumn<DatedRow>>(4);
            builder.AddComponentParameter(5, "Field", "Shipped");
            builder.CloseComponent();
        };

        var component = RenderComponent<BitDataGrid<DatedRow>>(parameters =>
        {
            parameters.Add(p => p.Items, rows.AsQueryable());
            parameters.Add(p => p.ChildContent, columns);
        });

        // A midnight DateTime operand means "the whole calendar day" - matching the in-memory pipeline
        // - so both 2024-05-01 rows match regardless of their time-of-day.
        await component.InvokeAsync(() => component.Instance.ApplyFilterAsync("Created", BitDataGridFilterOperator.Equals, new DateTime(2024, 5, 1)));
        Assert.AreEqual(2, component.Instance.TotalCount);

        await component.InvokeAsync(() => component.Instance.ApplyFilterAsync("Created", BitDataGridFilterOperator.NotEquals, new DateTime(2024, 5, 1)));
        Assert.AreEqual(1, component.Instance.TotalCount);
        Assert.AreEqual("NextDay", FirstDatedCellTexts(component)[0]);

        // Nullable DateTimeOffset compares each row's own calendar date (23:00 +02:00 is still May 1);
        // a null value is never equal to a day and always not-equal, like the in-memory comparer.
        await component.InvokeAsync(() => component.Instance.ClearFiltersAsync());
        await component.InvokeAsync(() => component.Instance.ApplyFilterAsync("Shipped", BitDataGridFilterOperator.Equals, new DateTimeOffset(2024, 5, 1, 0, 0, 0, TimeSpan.Zero)));
        Assert.AreEqual(1, component.Instance.TotalCount);
        Assert.AreEqual("Morning", FirstDatedCellTexts(component)[0]);

        await component.InvokeAsync(() => component.Instance.ApplyFilterAsync("Shipped", BitDataGridFilterOperator.NotEquals, new DateTimeOffset(2024, 5, 1, 0, 0, 0, TimeSpan.Zero)));
        Assert.AreEqual(2, component.Instance.TotalCount);
    }

    private static IReadOnlyList<string> FirstDatedCellTexts(IRenderedComponent<BitDataGrid<DatedRow>> component)
        => component.FindAll(".bit-dtg-body > .bit-dtg-row:not(.bit-dtg-message-row):not(.bit-dtg-placeholder-row)")
            .Select(r => r.QuerySelector(".bit-dtg-cell")!.TextContent.Trim())
            .ToList();

    public class TreeNode
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsFolder { get; set; }
        public List<TreeNode>? Children { get; set; }
    }

    [TestMethod]
    public void LazyTreeLoadsChildrenOnceOnFirstExpand()
    {
        var providerCalls = 0;
        RenderFragment columns = builder =>
        {
            builder.OpenComponent<BitDataGridColumn<TreeNode>>(0);
            builder.AddComponentParameter(1, "Field", "Name");
            builder.CloseComponent();
        };
        var roots = new List<TreeNode> { new() { Id = 1, Name = "root", IsFolder = true } };

        var component = RenderComponent<BitDataGrid<TreeNode>>(parameters =>
        {
            parameters.Add(p => p.Items, roots);
            parameters.Add(p => p.KeyField, (Func<TreeNode, object>)(n => n.Id));
            parameters.Add(p => p.HasChildrenSelector, (Func<TreeNode, bool>)(n => n.IsFolder));
            parameters.Add(p => p.ChildrenProvider, (Func<TreeNode, Task<IEnumerable<TreeNode>?>>)(parent =>
            {
                providerCalls++;
                return Task.FromResult<IEnumerable<TreeNode>?>(new[]
                {
                    new TreeNode { Id = parent.Id * 10 + 1, Name = $"{parent.Name}-child" },
                });
            }));
            parameters.Add(p => p.ChildContent, columns);
        });

        Assert.AreEqual(1, component.FindAll(".bit-dtg-body > .bit-dtg-row").Count);

        // First expand fetches and renders the child.
        component.Find(".bit-dtg-tree-toggle").Click();
        component.WaitForAssertion(() => Assert.AreEqual(2, component.FindAll(".bit-dtg-body > .bit-dtg-row").Count));
        Assert.AreEqual(1, providerCalls);

        // Collapse and re-expand: served from the cache, no second fetch.
        component.Find(".bit-dtg-tree-toggle").Click();
        component.WaitForAssertion(() => Assert.AreEqual(1, component.FindAll(".bit-dtg-body > .bit-dtg-row").Count));
        component.Find(".bit-dtg-tree-toggle").Click();
        component.WaitForAssertion(() => Assert.AreEqual(2, component.FindAll(".bit-dtg-body > .bit-dtg-row").Count));
        Assert.AreEqual(1, providerCalls);
    }

    [TestMethod]
    public async Task PointerDropHandlersReorderRowsAndColumns()
    {
        // Rows: the JS touch bridge reports dataset indices; the handler drives the same pipeline.
        var items = CreateRows();
        var rowGrid = RenderGrid(items, parameters =>
        {
            parameters.Add(p => p.KeyField, (Func<TestRow, object>)(r => r.Id));
            parameters.Add(p => p.RowReorderable, true);
        });
        await rowGrid.InvokeAsync(() => rowGrid.Instance.OnPointerRowDropAsync(0, 2));
        Assert.AreEqual("Banana", items[2].Name);
        Assert.AreEqual("Apple", items[0].Name);

        // Columns: reported by column id.
        var colGrid = RenderGrid(configure: parameters => parameters.Add(p => p.Reorderable, true));
        await colGrid.InvokeAsync(() => colGrid.Instance.OnPointerColumnDropAsync("Name", "Price"));
        var headers = colGrid.FindAll(".bit-dtg-header-row .bit-dtg-htext").Select(h => h.TextContent.Trim()).ToList();
        Assert.AreEqual("Price", headers[0]);
        Assert.AreEqual("Name", headers[1]);
    }

    [TestMethod]
    public async Task ColumnVirtualizationRendersOnlyViewportWindow()
    {
        RenderFragment columns = builder =>
        {
            var seq = 0;
            for (var i = 0; i < 30; i++)
            {
                var index = i;
                builder.OpenComponent<BitDataGridColumn<TestRow>>(seq++);
                builder.AddComponentParameter(seq++, "ColumnId", $"c{index}");
                builder.AddComponentParameter(seq++, "Title", $"C{index}");
                builder.AddComponentParameter(seq++, "Width", "100px");
                builder.AddComponentParameter(seq++, "Template", (RenderFragment<TestRow>)(row => b => b.AddContent(0, $"{row.Id}-{index}")));
                builder.CloseComponent();
            }
        };
        var component = RenderComponent<BitDataGrid<TestRow>>(parameters =>
        {
            parameters.Add(p => p.Items, CreateRows());
            parameters.Add(p => p.VirtualizeColumns, true);
            parameters.Add(p => p.ChildContent, columns);
        });

        // Before the viewport is measured, everything renders (plain unvirtualized fallback).
        Assert.AreEqual(30, component.FindAll(".bit-dtg-header-row .bit-dtg-hcell:not([aria-hidden])").Count);

        // 400px viewport at scroll 0 (+200px overscan) → columns starting before x=600 → 7 columns.
        await component.InvokeAsync(() => component.Instance.OnHorizontalScrollAsync(0, 400));
        Assert.AreEqual(7, component.FindAll(".bit-dtg-header-row .bit-dtg-hcell:not([aria-hidden])").Count);
        Assert.AreEqual(1, component.FindAll(".bit-dtg-header-row .bit-dtg-hcell[aria-hidden]").Count, "one trailing spacer");
        // Data rows follow the same window: 7 cells + 1 spacer per row.
        var firstRow = component.FindAll(".bit-dtg-body > .bit-dtg-row")[0];
        Assert.AreEqual(7, firstRow.QuerySelectorAll(".bit-dtg-cell:not([aria-hidden])").Length);

        // Mid-scroll: window 1800..2600 → columns 16..25 (10) with spacers on both sides.
        await component.InvokeAsync(() => component.Instance.OnHorizontalScrollAsync(2000, 400));
        Assert.AreEqual(10, component.FindAll(".bit-dtg-header-row .bit-dtg-hcell:not([aria-hidden])").Count);
        Assert.AreEqual(2, component.FindAll(".bit-dtg-header-row .bit-dtg-hcell[aria-hidden]").Count, "leading and trailing spacers");
    }

    [TestMethod]
    public void ReplacedItemsPruneStaleSelection()
    {
        IReadOnlyList<TestRow>? selected = null;
        var items = CreateRows();
        var component = RenderGrid(items, parameters =>
        {
            parameters.Add(p => p.KeyField, (Func<TestRow, object>)(r => r.Id));
            parameters.Add(p => p.SelectionMode, BitDataGridSelectionMode.Multiple);
            parameters.Add(p => p.SelectedItemsChanged, EventCallback.Factory.Create<IReadOnlyList<TestRow>>(this, v => selected = v));
        });

        component.FindAll(".bit-dtg-cell-select input")[0].Change(true); // selects Id 1
        Assert.AreEqual(1, selected!.Count);

        // Replace the source with a list that no longer contains Id 1.
        var replacement = CreateRows().Where(r => r.Id != 1).ToList();
        component.Render(parameters => parameters.Add(p => p.Items, replacement));

        Assert.AreEqual(0, selected!.Count, "selection must not keep rows that left the data source");
    }

    [TestMethod]
    public async Task InPlaceItemRemovalPrunesStaleExpandedDetailsOnRefresh()
    {
        IReadOnlyList<TestRow>? expanded = null;
        var items = CreateRows();
        var component = RenderGrid(items, parameters =>
        {
            parameters.Add(p => p.KeyField, (Func<TestRow, object>)(r => r.Id));
            parameters.Add(p => p.DetailTemplate, (RenderFragment<TestRow>)(r => b => b.AddContent(0, $"DETAIL-{r.Id}")));
            parameters.Add(p => p.ExpandedDetailItemsChanged, EventCallback.Factory.Create<IReadOnlyList<TestRow>>(this, v => expanded = v));
        });

        await component.InvokeAsync(() => component.Instance.ExpandDetailAsync(items[0]));
        await component.InvokeAsync(() => component.Instance.ExpandDetailAsync(items[1]));
        CollectionAssert.AreEquivalent(new[] { 1, 2 }, expanded!.Select(r => r.Id).ToArray());

        // The row leaves through the same list instance, so the Items reference never changes and only
        // the explicit refresh can notice it.
        items.RemoveAt(0);
        await component.InvokeAsync(() => component.Instance.RefreshAsync());

        CollectionAssert.AreEquivalent(new[] { 2 }, expanded!.Select(r => r.Id).ToArray(),
            "a row removed in place must not stay expanded");
        Assert.IsFalse(component.Instance.IsDetailExpanded(new TestRow { Id = 1 }));
    }

    [TestMethod]
    public async Task PrunedRowsReportTheirCollapseThroughOnDetailToggle()
    {
        var toggles = new List<BitDataGridDetailEventArgs<TestRow>>();
        var items = CreateRows();
        var component = RenderGrid(items, parameters =>
        {
            parameters.Add(p => p.KeyField, (Func<TestRow, object>)(r => r.Id));
            parameters.Add(p => p.DetailTemplate, (RenderFragment<TestRow>)(r => b => b.AddContent(0, $"DETAIL-{r.Id}")));
            parameters.Add(p => p.OnDetailToggle, EventCallback.Factory.Create<BitDataGridDetailEventArgs<TestRow>>(this, toggles.Add));
        });

        await component.InvokeAsync(() => component.Instance.ExpandDetailAsync(items[0]));
        await component.InvokeAsync(() => component.Instance.ExpandDetailAsync(items[1]));
        toggles.Clear();

        // Path 1: a replaced materialized source prunes only the rows that left it.
        component.Render(parameters => parameters.Add(p => p.Items, CreateRows().Where(r => r.Id != 1).ToList()));

        CollectionAssert.AreEqual(new[] { 1 }, toggles.Select(t => t.Item.Id).ToArray(),
            "only the removed row collapsed");
        Assert.IsTrue(toggles.All(t => t.Expanded == false));

        // Path 2: a cleared source drops every row, so everything still expanded collapses.
        toggles.Clear();
        component.Render(parameters => parameters.Add(p => p.Items, (IEnumerable<TestRow>?)null));

        CollectionAssert.AreEqual(new[] { 2 }, toggles.Select(t => t.Item.Id).ToArray());
        Assert.IsTrue(toggles.All(t => t.Expanded == false));

        // Nothing is left expanded, so a further prune stays silent.
        toggles.Clear();
        component.Render(parameters => parameters.Add(p => p.Items, CreateRows()));
        Assert.AreEqual(0, toggles.Count);
    }

    [TestMethod]
    public async Task PropertyExpressionBindsColumnLikeField()
    {
        RenderFragment columns = builder =>
        {
            builder.OpenComponent<BitDataGridColumn<TestRow>>(0);
            builder.AddComponentParameter(1, "Property", (Expression<Func<TestRow, object?>>)(r => r.Name));
            builder.CloseComponent();
            builder.OpenComponent<BitDataGridColumn<TestRow>>(2);
            builder.AddComponentParameter(3, "Property", (Expression<Func<TestRow, object?>>)(r => r.Price));
            builder.CloseComponent();
        };
        var component = RenderComponent<BitDataGrid<TestRow>>(parameters =>
        {
            parameters.Add(p => p.Items, CreateRows());
            parameters.Add(p => p.ChildContent, columns);
        });

        // Titles humanize from the resolved path, values render through the same accessor as Field.
        var headers = component.FindAll(".bit-dtg-header-row .bit-dtg-htext").Select(h => h.TextContent.Trim()).ToList();
        CollectionAssert.AreEqual(new[] { "Name", "Price" }, headers);
        Assert.AreEqual("Banana", FirstCellTexts(component)[0]);

        // The column id falls back to the resolved path, so the programmatic state API keys still work.
        await component.InvokeAsync(() => component.Instance.SortByAsync("Price", BitDataGridSortDirection.Descending));
        Assert.AreEqual("Cherry", FirstCellTexts(component)[0]);
    }

    [TestMethod]
    public void PropertyExpressionTakesPrecedenceOverField()
    {
        RenderFragment columns = builder =>
        {
            builder.OpenComponent<BitDataGridColumn<TestRow>>(0);
            builder.AddComponentParameter(1, "Field", "Name");
            builder.AddComponentParameter(2, "Property", (Expression<Func<TestRow, object?>>)(r => r.Id));
            builder.CloseComponent();
        };
        var component = RenderComponent<BitDataGrid<TestRow>>(parameters =>
        {
            parameters.Add(p => p.Items, CreateRows());
            parameters.Add(p => p.ChildContent, columns);
        });

        Assert.AreEqual("Id", component.Find(".bit-dtg-header-row .bit-dtg-htext").TextContent.Trim());
        Assert.AreEqual("1", FirstCellTexts(component)[0]);
    }

    [TestMethod]
    public async Task DetailsStayExpandableWhileTheToggleColumnIsHidden()
    {
        var rows = CreateRows();
        var component = RenderGrid(rows, parameters =>
        {
            parameters.Add(p => p.DetailTemplate, (RenderFragment<TestRow>)(r => b => b.AddContent(0, $"DETAIL-{r.Id}")));
            parameters.Add(p => p.ShowDetailToggle, false);
        });

        Assert.AreEqual(0, component.FindAll(".bit-dtg-cell-detail").Count, "the toggle column must not render");

        await component.InvokeAsync(() => component.Instance.ExpandDetailAsync(rows[1]));

        Assert.AreEqual("DETAIL-2", component.Find(".bit-dtg-detail-content").TextContent.Trim());
        Assert.IsTrue(component.Instance.IsDetailExpanded(rows[1]));

        await component.InvokeAsync(() => component.Instance.CollapseDetailAsync(rows[1]));

        Assert.AreEqual(0, component.FindAll(".bit-dtg-detail-row").Count);
    }

    [TestMethod]
    public void RowClickTogglesDetailOnlyWhenEnabled()
    {
        var toggles = new List<BitDataGridDetailEventArgs<TestRow>>();
        var component = RenderGrid(configure: parameters =>
        {
            parameters.Add(p => p.DetailTemplate, (RenderFragment<TestRow>)(r => b => b.AddContent(0, $"DETAIL-{r.Id}")));
        });

        // Without ExpandDetailOnRowClick the row click is inert: only the toggle button expands.
        component.Find(".bit-dtg-body > .bit-dtg-row").Click();
        Assert.AreEqual(0, component.FindAll(".bit-dtg-detail-row").Count);

        component.Render(parameters =>
        {
            parameters.Add(p => p.ExpandDetailOnRowClick, true);
            parameters.Add(p => p.OnDetailToggle, EventCallback.Factory.Create<BitDataGridDetailEventArgs<TestRow>>(this, toggles.Add));
        });

        component.Find(".bit-dtg-body > .bit-dtg-row").Click();
        Assert.AreEqual("DETAIL-1", component.Find(".bit-dtg-detail-content").TextContent.Trim());

        component.Find(".bit-dtg-body > .bit-dtg-row").Click();
        Assert.AreEqual(0, component.FindAll(".bit-dtg-detail-row").Count);

        CollectionAssert.AreEqual(new[] { true, false }, toggles.Select(t => t.Expanded).ToArray());
        Assert.IsTrue(toggles.All(t => t.Item.Id == 1));
    }

    [TestMethod]
    [DataRow("Enter")]
    [DataRow(" ")]
    public void RowIsKeyboardOperableWhenItIsTheOnlyDetailToggle(string key)
    {
        // Hidden toggle column + no cell navigation leaves the row as the only detail affordance, so it
        // has to be focusable and activate on Enter/Space - otherwise the expansion is pointer-only.
        var component = RenderGrid(configure: parameters =>
        {
            parameters.Add(p => p.DetailTemplate, (RenderFragment<TestRow>)(r => b => b.AddContent(0, $"DETAIL-{r.Id}")));
            parameters.Add(p => p.ShowDetailToggle, false);
            parameters.Add(p => p.ExpandDetailOnRowClick, true);
        });

        var row = component.Find(".bit-dtg-body > .bit-dtg-row");
        Assert.AreEqual("0", row.GetAttribute("tabindex"), "the row must be reachable by Tab");
        Assert.AreEqual("false", row.GetAttribute("aria-expanded"));

        row.KeyDown(key);
        Assert.AreEqual("DETAIL-1", component.Find(".bit-dtg-detail-content").TextContent.Trim());
        Assert.AreEqual("true", component.Find(".bit-dtg-body > .bit-dtg-row").GetAttribute("aria-expanded"));

        component.Find(".bit-dtg-body > .bit-dtg-row").KeyDown(key);
        Assert.AreEqual(0, component.FindAll(".bit-dtg-detail-row").Count);
        Assert.AreEqual("false", component.Find(".bit-dtg-body > .bit-dtg-row").GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public void RowIsNotAKeyboardToggleWhenAnotherOneExists()
    {
        // The toggle column is a button of its own, so the row must not double as a tab stop.
        var withToggle = RenderGrid(configure: parameters =>
        {
            parameters.Add(p => p.DetailTemplate, (RenderFragment<TestRow>)(r => b => b.AddContent(0, $"DETAIL-{r.Id}")));
            parameters.Add(p => p.ExpandDetailOnRowClick, true);
        });
        Assert.IsNull(withToggle.Find(".bit-dtg-body > .bit-dtg-row").GetAttribute("tabindex"));

        // Cell navigation already routes Enter through the focused cell (HandleCellKeyDownAsync).
        var withCellNav = RenderGrid(configure: parameters =>
        {
            parameters.Add(p => p.DetailTemplate, (RenderFragment<TestRow>)(r => b => b.AddContent(0, $"DETAIL-{r.Id}")));
            parameters.Add(p => p.ShowDetailToggle, false);
            parameters.Add(p => p.ExpandDetailOnRowClick, true);
            parameters.Add(p => p.CellNavigation, true);
        });
        var navRow = withCellNav.Find(".bit-dtg-body > .bit-dtg-row");
        Assert.IsNull(navRow.GetAttribute("tabindex"));

        // ...and the row keydown stays inert there, so the cell's Enter can't toggle twice.
        navRow.KeyDown("Enter");
        Assert.AreEqual(0, withCellNav.FindAll(".bit-dtg-detail-row").Count);
    }

    [TestMethod]
    public void RowKeyboardToggleDoesNotFireWhileARowIsEdited()
    {
        var component = RenderGrid(configure: parameters =>
        {
            parameters.Add(p => p.Editable, true);
            parameters.Add(p => p.DetailTemplate, (RenderFragment<TestRow>)(r => b => b.AddContent(0, $"DETAIL-{r.Id}")));
            parameters.Add(p => p.ShowDetailToggle, false);
            parameters.Add(p => p.ExpandDetailOnRowClick, true);
        });

        component.FindAll(".bit-dtg-cell-command button")[0].Click();

        // The edited row must not advertise a toggle it refuses to perform.
        var editedRow = component.Find(".bit-dtg-body > .bit-dtg-row");
        Assert.IsNull(editedRow.GetAttribute("tabindex"));
        Assert.IsNull(editedRow.GetAttribute("aria-expanded"));
        Assert.IsFalse(editedRow.ClassList.Contains("bit-dtg-row-expandable"));

        editedRow.KeyDown("Enter");
        Assert.AreEqual(0, component.FindAll(".bit-dtg-detail-row").Count, "expanding would shift the editors mid-edit");
    }

    [TestMethod]
    public void RowClickDoesNotToggleDetailWhileARowIsEdited()
    {
        var component = RenderGrid(configure: parameters =>
        {
            parameters.Add(p => p.Editable, true);
            parameters.Add(p => p.ExpandDetailOnRowClick, true);
            parameters.Add(p => p.DetailTemplate, (RenderFragment<TestRow>)(r => b => b.AddContent(0, $"DETAIL-{r.Id}")));
        });

        // Entering edit mode and then clicking the row must leave the editors where they are.
        component.FindAll(".bit-dtg-cell-command button")[0].Click();
        component.Find(".bit-dtg-body > .bit-dtg-row").Click();

        Assert.AreEqual(0, component.FindAll(".bit-dtg-detail-row").Count);
    }

    [TestMethod]
    public async Task ExpandAllDetailsCoversTheWholeViewAndCollapseAllClearsIt()
    {
        var toggles = new List<BitDataGridDetailEventArgs<TestRow>>();
        var component = RenderGrid(configure: parameters =>
        {
            parameters.Add(p => p.DetailTemplate, (RenderFragment<TestRow>)(r => b => b.AddContent(0, $"DETAIL-{r.Id}")));
            parameters.Add(p => p.OnDetailToggle, EventCallback.Factory.Create<BitDataGridDetailEventArgs<TestRow>>(this, toggles.Add));
            parameters.Add(p => p.Pageable, true);
            parameters.Add(p => p.PageSize, 2);
        });

        await component.InvokeAsync(() => component.Instance.ExpandAllDetailsAsync());

        // Only the current page is rendered, but every row of the view is expanded - paging to the
        // next page shows its details already open.
        Assert.AreEqual(2, component.FindAll(".bit-dtg-detail-row").Count);
        await component.InvokeAsync(() => component.Instance.GoToPageAsync(2));
        Assert.AreEqual(2, component.FindAll(".bit-dtg-detail-row").Count);

        // The bulk operation reports each row it expanded, not just the rendered page.
        CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4, 5 }, toggles.Select(t => t.Item.Id).ToArray());
        Assert.IsTrue(toggles.All(t => t.Expanded));

        toggles.Clear();
        await component.InvokeAsync(() => component.Instance.CollapseAllDetailsAsync());
        Assert.AreEqual(0, component.FindAll(".bit-dtg-detail-row").Count);

        CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4, 5 }, toggles.Select(t => t.Item.Id).ToArray());
        Assert.IsTrue(toggles.All(t => t.Expanded == false));

        // Nothing left to change: a second bulk call is a no-op and raises no callback.
        toggles.Clear();
        await component.InvokeAsync(() => component.Instance.CollapseAllDetailsAsync());
        Assert.AreEqual(0, toggles.Count);
    }

    [TestMethod]
    public void ExpandedDetailItemsBindingDrivesTheDetailRows()
    {
        var rows = CreateRows();
        IReadOnlyList<TestRow> expanded = new List<TestRow>();
        var component = RenderGrid(rows, parameters =>
        {
            parameters.Add(p => p.DetailTemplate, (RenderFragment<TestRow>)(r => b => b.AddContent(0, $"DETAIL-{r.Id}")));
            parameters.Add(p => p.ExpandDetailOnRowClick, true);
            parameters.Add(p => p.ExpandedDetailItems, expanded);
            parameters.Add(p => p.ExpandedDetailItemsChanged, EventCallback.Factory.Create<IReadOnlyList<TestRow>>(this, v => expanded = v));
        });

        // Incoming state expands rows without any user interaction.
        component.Render(parameters => parameters.Add(p => p.ExpandedDetailItems, new List<TestRow> { rows[2] }));
        Assert.AreEqual("DETAIL-3", component.Find(".bit-dtg-detail-content").TextContent.Trim());

        // Outgoing changes are reported back through the binding.
        component.FindAll(".bit-dtg-body > .bit-dtg-row")[0].Click();
        CollectionAssert.AreEquivalent(new[] { 3, 1 }, expanded.Select(r => r.Id).ToArray());
    }

    [TestMethod]
    public void ColumnsParameterAliasesChildContent()
    {
        var component = RenderComponent<BitDataGrid<TestRow>>(parameters =>
        {
            parameters.Add(p => p.Items, CreateRows());
            parameters.Add(p => p.Columns, DefaultColumns());
        });

        var headers = component.FindAll(".bit-dtg-header-row .bit-dtg-htext").Select(h => h.TextContent.Trim()).ToList();
        CollectionAssert.AreEqual(new[] { "Name", "Price" }, headers);
        Assert.AreEqual(5, component.FindAll(".bit-dtg-body > .bit-dtg-row").Count);
    }

    // ------------------------------------------------------------ Quick search

    [TestMethod]
    public void SearchBoxFiltersAcrossEverySearchableColumn()
    {
        var component = RenderGrid(configure: parameters => parameters.Add(p => p.ShowSearchBox, true));

        // "err" only appears in names (Cherry, Elderberry), so the text column drives the match.
        component.Find(".bit-dtg-search-input").Change("err");
        CollectionAssert.AreEqual(new[] { "Cherry", "Elderberry" }, FirstCellTexts(component).ToArray());

        // A term that only the numeric Price column can satisfy still matches: search runs over each
        // column's rendered text, not just the string ones.
        component.Find(".bit-dtg-search-input").Change("10");
        CollectionAssert.AreEqual(new[] { "Cherry" }, FirstCellTexts(component).ToArray());

        component.Find(".bit-dtg-search-clear").Click();
        Assert.AreEqual(5, FirstCellTexts(component).Count);
        Assert.IsNull(component.Instance.ActiveSearch);
    }

    [TestMethod]
    public void SearchSkipsColumnsThatOptOut()
    {
        RenderFragment columns = builder =>
        {
            builder.OpenComponent<BitDataGridColumn<TestRow>>(0);
            builder.AddComponentParameter(1, "Field", "Name");
            builder.CloseComponent();
            builder.OpenComponent<BitDataGridColumn<TestRow>>(2);
            builder.AddComponentParameter(3, "Field", "Price");
            builder.AddComponentParameter(4, "Searchable", (bool?)false);
            builder.CloseComponent();
        };

        var component = RenderComponent<BitDataGrid<TestRow>>(parameters =>
        {
            parameters.Add(p => p.Items, CreateRows());
            parameters.Add(p => p.ChildContent, columns);
            parameters.Add(p => p.ShowSearchBox, true);
        });

        // "10" is only Cherry's price, and that column opted out - so nothing matches.
        component.Find(".bit-dtg-search-input").Change("10");
        Assert.AreEqual(0, FirstCellTexts(component).Count);
        component.Find(".bit-dtg-empty");
    }

    [TestMethod]
    public async Task SearchAsyncResetsToFirstPageAndReportsThroughBinding()
    {
        string? reported = null;
        var component = RenderGrid(configure: parameters =>
        {
            parameters.Add(p => p.Pageable, true);
            parameters.Add(p => p.PageSize, 2);
            parameters.Add(p => p.SearchTextChanged, (string? s) => reported = s);
        });

        await component.InvokeAsync(() => component.Instance.GoToPageAsync(2));
        Assert.AreEqual(2, component.Instance.CurrentPage);

        await component.InvokeAsync(() => component.Instance.SearchAsync("err"));

        Assert.AreEqual("err", reported);
        Assert.AreEqual("err", component.Instance.ActiveSearch);
        Assert.AreEqual(1, component.Instance.CurrentPage, "a new search must return to the first page");
        Assert.AreEqual(2, component.Instance.TotalCount);
    }

    [TestMethod]
    public void SearchTextParameterDrivesTheSearchWithoutClobberingUserInput()
    {
        var component = RenderGrid(configure: parameters =>
        {
            parameters.Add(p => p.ShowSearchBox, true);
            parameters.Add(p => p.SearchText, "Cherry");
        });
        CollectionAssert.AreEqual(new[] { "Cherry" }, FirstCellTexts(component).ToArray());

        // The parent never binds SearchText back, so a re-render with the same parameter value must not
        // reset the term the user just typed into the box.
        component.Find(".bit-dtg-search-input").Change("Date");
        component.Render(parameters => parameters.Add(p => p.Striped, false));
        CollectionAssert.AreEqual(new[] { "Date" }, FirstCellTexts(component).ToArray());
    }

    [TestMethod]
    public async Task QueryableSearchTranslatesToTheProvider()
    {
        var component = RenderComponent<BitDataGrid<TestRow>>(parameters =>
        {
            parameters.Add(p => p.Items, CreateRows().AsQueryable());
            parameters.Add(p => p.ChildContent, DefaultColumns());
        });

        // Only the string column is translatable, so the term matches on Name.
        await component.InvokeAsync(() => component.Instance.SearchAsync("an"));
        CollectionAssert.AreEqual(new[] { "Banana" }, FirstCellTexts(component).ToArray());
    }

    [TestMethod]
    public async Task ServerModeForwardsTheSearchTermInTheReadRequest()
    {
        string? seen = null;
        var component = RenderComponent<BitDataGrid<TestRow>>(parameters =>
        {
            parameters.Add(p => p.ChildContent, DefaultColumns());
            parameters.Add(p => p.OnRead, (Func<BitDataGridReadRequest, Task<BitDataGridReadResult<TestRow>>>)(request =>
            {
                seen = request.Search;
                var rows = CreateRows();
                if (request.Search is { Length: > 0 } term)
                    rows = rows.Where(r => r.Name.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
                return Task.FromResult(new BitDataGridReadResult<TestRow>(rows, rows.Count));
            }));
        });

        await component.InvokeAsync(() => component.Instance.SearchAsync("err"));

        Assert.AreEqual("err", seen);
        CollectionAssert.AreEqual(new[] { "Cherry", "Elderberry" }, FirstCellTexts(component).ToArray());
    }

    [TestMethod]
    public async Task StateSnapshotCarriesTheSearchTerm()
    {
        var component = RenderGrid(configure: parameters => parameters.Add(p => p.ShowSearchBox, true));

        await component.InvokeAsync(() => component.Instance.SearchAsync("err"));
        var state = component.Instance.GetState();
        Assert.AreEqual("err", state.Search);

        await component.InvokeAsync(() => component.Instance.SearchAsync(null));
        Assert.AreEqual(5, FirstCellTexts(component).Count);

        await component.InvokeAsync(() => component.Instance.ApplyStateAsync(state));
        CollectionAssert.AreEqual(new[] { "Cherry", "Elderberry" }, FirstCellTexts(component).ToArray());
    }

    // ------------------------------------------------------------------ Pager

    [TestMethod]
    public void PagerOffersThePageSizeActuallyInEffect()
    {
        // 8 is not one of the declared options, so without merging it in the select would show "10"
        // while the grid pages by 8.
        var component = RenderGrid(configure: parameters =>
        {
            parameters.Add(p => p.Pageable, true);
            parameters.Add(p => p.PageSize, 8);
        });

        var options = component.FindAll(".bit-dtg-page-size option");
        var selected = options.Single(o => o.HasAttribute("selected"));
        Assert.AreEqual("8", selected.GetAttribute("value"));
        CollectionAssert.AreEqual(new[] { "8", "10", "20", "50", "100" },
            options.Select(o => o.GetAttribute("value")).ToArray());
    }

    // ----------------------------------------------------------- Filter row

    [TestMethod]
    public void FilterRowIsNotRenderedWhenEveryColumnOptsOut()
    {
        RenderFragment columns = builder =>
        {
            builder.OpenComponent<BitDataGridColumn<TestRow>>(0);
            builder.AddComponentParameter(1, "Field", "Name");
            builder.AddComponentParameter(2, "Filterable", (bool?)false);
            builder.CloseComponent();
        };

        var component = RenderComponent<BitDataGrid<TestRow>>(parameters =>
        {
            parameters.Add(p => p.Items, CreateRows());
            parameters.Add(p => p.ChildContent, columns);
            parameters.Add(p => p.Filterable, true);
        });

        Assert.AreEqual(0, component.FindAll(".bit-dtg-filter-row").Count,
            "an empty filter row must not be reserved when no column can be filtered");
    }

    // -------------------------------------------------------- Column chooser

    [TestMethod]
    public void ColumnChooserRefusesToHideTheLastVisibleColumn()
    {
        var component = RenderGrid(configure: parameters =>
        {
            parameters.Add(p => p.ShowToolbar, true);
            parameters.Add(p => p.ShowColumnChooser, true);
        });

        component.Find(".bit-dtg-toolbar-end .bit-dtg-btn").Click();
        var boxes = component.FindAll(".bit-dtg-chooser-item input");
        Assert.AreEqual(2, boxes.Count);

        boxes[1].Change(false);
        Assert.AreEqual(1, component.FindAll(".bit-dtg-header-row .bit-dtg-htext").Count);

        // Only one column is left, so its checkbox is disabled and unchecking it is a no-op.
        var remaining = component.FindAll(".bit-dtg-chooser-item input")[0];
        Assert.IsTrue(remaining.HasAttribute("disabled"));
        remaining.Change(false);
        Assert.AreEqual(1, component.FindAll(".bit-dtg-header-row .bit-dtg-htext").Count,
            "the grid must never be left with no columns");
    }

    [TestMethod]
    public void EscapeClosesTheColumnChooser()
    {
        var component = RenderGrid(configure: parameters =>
        {
            parameters.Add(p => p.ShowToolbar, true);
            parameters.Add(p => p.ShowColumnChooser, true);
        });

        component.Find(".bit-dtg-toolbar-end .bit-dtg-btn").Click();
        Assert.AreEqual(1, component.FindAll(".bit-dtg-column-chooser").Count);

        component.Find(".bit-dtg-column-chooser").KeyDown("Escape");
        Assert.AreEqual(0, component.FindAll(".bit-dtg-column-chooser").Count);
        Assert.AreEqual("false", component.Find(".bit-dtg-toolbar-end .bit-dtg-btn").GetAttribute("aria-expanded"));
    }

    // ------------------------------------------------------- Row appearance

    [TestMethod]
    public void RowClassAndRowStyleDecorateTheirRows()
    {
        var component = RenderGrid(configure: parameters =>
        {
            parameters.Add(p => p.RowClass, (Func<TestRow, string?>)(r => r.Price < 0 ? "negative" : null));
            parameters.Add(p => p.RowStyle, (Func<TestRow, string?>)(r => r.Price < 0 ? "color:red" : null));
        });

        var rows = component.FindAll(".bit-dtg-body > .bit-dtg-row");
        // Apple is the only row with a negative price.
        Assert.IsTrue(rows[1].ClassList.Contains("negative"));
        StringAssert.Contains(rows[1].GetAttribute("style"), "color:red;");
        StringAssert.Contains(rows[1].GetAttribute("style"), "grid-template-columns:",
            "the caller's style must be appended to the layout style, not replace it");
        Assert.IsFalse(rows[0].ClassList.Contains("negative"));
    }

    [TestMethod]
    public void LoadingTemplateReplacesTheBuiltInSpinner()
    {
        var component = RenderGrid(configure: parameters =>
        {
            parameters.Add(p => p.Loading, true);
            parameters.Add(p => p.LoadingTemplate, (RenderFragment)(b => b.AddMarkupContent(0, "<span class=\"my-loader\">Fetching…</span>")));
        });

        component.Find(".bit-dtg-loading .my-loader");
        Assert.AreEqual(0, component.FindAll(".bit-dtg-spinner").Count);
        Assert.AreEqual("true", component.Find(".bit-dtg-table").GetAttribute("aria-busy"));
    }

    [TestMethod]
    public void CellTooltipsCarryTheFullValueAndRespectPerColumnOptOut()
    {
        RenderFragment columns = builder =>
        {
            builder.OpenComponent<BitDataGridColumn<TestRow>>(0);
            builder.AddComponentParameter(1, "Field", "Name");
            builder.CloseComponent();
            builder.OpenComponent<BitDataGridColumn<TestRow>>(2);
            builder.AddComponentParameter(3, "Field", "Price");
            builder.AddComponentParameter(4, "ShowTooltip", (bool?)false);
            builder.CloseComponent();
        };

        var component = RenderComponent<BitDataGrid<TestRow>>(parameters =>
        {
            parameters.Add(p => p.Items, CreateRows());
            parameters.Add(p => p.ChildContent, columns);
            parameters.Add(p => p.ShowCellTooltips, true);
        });

        var cells = component.FindAll(".bit-dtg-body > .bit-dtg-row")[0].QuerySelectorAll(".bit-dtg-cell");
        Assert.AreEqual("Banana", cells[0].GetAttribute("title"));
        Assert.IsNull(cells[1].GetAttribute("title"), "a column can opt out of the grid-level tooltips");
    }

    // ------------------------------------------------------------- Selection

    [TestMethod]
    public void ShiftClickSelectsTheRangeFromTheLastClickedRow()
    {
        IReadOnlyList<TestRow> selected = [];
        var component = RenderGrid(configure: parameters =>
        {
            parameters.Add(p => p.SelectionMode, BitDataGridSelectionMode.Multiple);
            parameters.Add(p => p.SelectedItemsChanged, (IReadOnlyList<TestRow> s) => selected = s);
        });

        var boxes = component.FindAll(".bit-dtg-cell-select input");
        boxes[0].Change(true);
        CollectionAssert.AreEqual(new[] { 1 }, selected.Select(r => r.Id).OrderBy(i => i).ToArray());

        // Shift is read on mousedown, which precedes the change event carrying the new value.
        var third = component.FindAll(".bit-dtg-cell-select input")[2];
        third.MouseDown(new MouseEventArgs { ShiftKey = true });
        third.Change(true);

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, selected.Select(r => r.Id).OrderBy(i => i).ToArray());
    }

    [TestMethod]
    public void ShiftFlagDoesNotLeakIntoTheNextPlainClick()
    {
        IReadOnlyList<TestRow> selected = [];
        var component = RenderGrid(configure: parameters =>
        {
            parameters.Add(p => p.SelectionMode, BitDataGridSelectionMode.Multiple);
            parameters.Add(p => p.SelectedItemsChanged, (IReadOnlyList<TestRow> s) => selected = s);
        });

        var boxes = component.FindAll(".bit-dtg-cell-select input");
        boxes[0].MouseDown(new MouseEventArgs { ShiftKey = true });
        boxes[0].Change(true);

        // No modifier this time: only the clicked row may be added.
        var fifth = component.FindAll(".bit-dtg-cell-select input")[4];
        fifth.MouseDown(new MouseEventArgs());
        fifth.Change(true);

        CollectionAssert.AreEqual(new[] { 1, 5 }, selected.Select(r => r.Id).OrderBy(i => i).ToArray());
    }

    [TestMethod]
    public void CtrlAAndSpaceDriveSelectionFromTheKeyboard()
    {
        IReadOnlyList<TestRow> selected = [];
        var component = RenderGrid(configure: parameters =>
        {
            parameters.Add(p => p.SelectionMode, BitDataGridSelectionMode.Multiple);
            parameters.Add(p => p.CellNavigation, true);
            parameters.Add(p => p.SelectedItemsChanged, (IReadOnlyList<TestRow> s) => selected = s);
        });

        var cell = component.Find(".bit-dtg-body > .bit-dtg-row .bit-dtg-cell:not(.bit-dtg-cell-select)");
        cell.KeyDown(new KeyboardEventArgs { Key = " " });
        CollectionAssert.AreEqual(new[] { 1 }, selected.Select(r => r.Id).ToArray());

        component.Find(".bit-dtg-body > .bit-dtg-row .bit-dtg-cell:not(.bit-dtg-cell-select)")
            .KeyDown(new KeyboardEventArgs { Key = "a", CtrlKey = true });
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, selected.Select(r => r.Id).OrderBy(i => i).ToArray());
    }

    [TestMethod]
    public async Task SelectAllAndClearSelectionCoverTheWholeView()
    {
        IReadOnlyList<TestRow> selected = [];
        var component = RenderGrid(configure: parameters =>
        {
            parameters.Add(p => p.SelectionMode, BitDataGridSelectionMode.Multiple);
            parameters.Add(p => p.Pageable, true);
            parameters.Add(p => p.PageSize, 2);
            parameters.Add(p => p.SelectedItemsChanged, (IReadOnlyList<TestRow> s) => selected = s);
        });

        // The header checkbox covers the page; SelectAllAsync covers every row of the view.
        await component.InvokeAsync(() => component.Instance.SelectAllAsync());
        Assert.AreEqual(5, selected.Count);

        await component.InvokeAsync(() => component.Instance.ClearSelectionAsync());
        Assert.AreEqual(0, selected.Count);
    }

    [TestMethod]
    public void MultipleSelectionIsAnnouncedThroughAriaMultiselectable()
    {
        var single = RenderGrid(configure: parameters => parameters.Add(p => p.SelectionMode, BitDataGridSelectionMode.Single));
        Assert.IsNull(single.Find(".bit-dtg-table").GetAttribute("aria-multiselectable"));

        var multiple = RenderGrid(configure: parameters => parameters.Add(p => p.SelectionMode, BitDataGridSelectionMode.Multiple));
        Assert.AreEqual("true", multiple.Find(".bit-dtg-table").GetAttribute("aria-multiselectable"));
    }

    // ------------------------------------------------------------ Clipboard

    [TestMethod]
    public async Task ClipboardCopyWritesTheSelectedRowsAsTabSeparatedText()
    {
        Context.JSInterop.Setup<bool>("BitBlazorUI.DataGrid.copyToClipboard", _ => true).SetResult(true);

        var items = CreateRows();
        var component = RenderGrid(items, parameters =>
        {
            parameters.Add(p => p.SelectionMode, BitDataGridSelectionMode.Multiple);
            parameters.Add(p => p.ClipboardCopy, true);
            parameters.Add(p => p.SelectedItems, new List<TestRow> { items[0], items[2] });
        });

        var copied = 0;
        await component.InvokeAsync(async () => copied = await component.Instance.CopyToClipboardAsync());

        Assert.AreEqual(2, copied);
        var payload = (string)Context.JSInterop.Invocations["BitBlazorUI.DataGrid.copyToClipboard"].Single().Arguments[0]!;
        Assert.AreEqual("Name\tPrice\r\nBanana\t2.5\r\nCherry\t10\r\n", payload);
    }

    [TestMethod]
    public async Task ClipboardCopyFallsBackToTheFocusedRowAndReportsFailure()
    {
        Context.JSInterop.Setup<bool>("BitBlazorUI.DataGrid.copyToClipboard", _ => true).SetResult(false);

        var items = CreateRows();
        var component = RenderGrid(items, parameters => parameters.Add(p => p.ClipboardCopy, true));

        var copied = -1;
        await component.InvokeAsync(async () => copied = await component.Instance.CopyToClipboardAsync(items[1]));

        Assert.AreEqual(0, copied, "a clipboard the browser refused must be reported as nothing copied");
        var payload = (string)Context.JSInterop.Invocations["BitBlazorUI.DataGrid.copyToClipboard"].Single().Arguments[0]!;
        StringAssert.Contains(payload, "Apple\t-5");
    }

    // --------------------------------------------------------------- Export

    [TestMethod]
    public void ExportableAndExportValueDecideWhatTheCsvCarries()
    {
        RenderFragment columns = builder =>
        {
            builder.OpenComponent<BitDataGridColumn<TestRow>>(0);
            builder.AddComponentParameter(1, "Field", "Name");
            builder.CloseComponent();
            builder.OpenComponent<BitDataGridColumn<TestRow>>(2);
            builder.AddComponentParameter(3, "Field", "Price");
            builder.AddComponentParameter(4, "Exportable", (bool?)false);
            builder.CloseComponent();
            // A template-only column has nothing to export until ExportValue supplies it.
            builder.OpenComponent<BitDataGridColumn<TestRow>>(5);
            builder.AddComponentParameter(6, "ColumnId", "double");
            builder.AddComponentParameter(7, "Title", "Double");
            builder.AddComponentParameter(8, "ExportValue", (Func<TestRow, object?>)(r => r.Price * 2));
            builder.CloseComponent();
        };

        var component = RenderComponent<BitDataGrid<TestRow>>(parameters =>
        {
            parameters.Add(p => p.Items, CreateRows());
            parameters.Add(p => p.ChildContent, columns);
        });

        var csv = component.Instance.ToCsv();
        StringAssert.StartsWith(csv, "Name,Double\r\n");
        StringAssert.Contains(csv, "Banana,5\r\n");
        Assert.IsFalse(csv.Contains("Price"), "an Exportable=false column must stay out of the file");
    }

    [TestMethod]
    public void CsvRowsEndWithCrLf()
    {
        var csv = RenderGrid().Instance.ToCsv();
        StringAssert.Contains(csv, "\r\n");
        Assert.AreEqual(6, csv.Split("\r\n", StringSplitOptions.RemoveEmptyEntries).Length,
            "a header line plus one line per row");
    }

    [TestMethod]
    public async Task ExportValueLandsInExcelAsANativeNumber()
    {
        RenderFragment columns = builder =>
        {
            builder.OpenComponent<BitDataGridColumn<TestRow>>(0);
            builder.AddComponentParameter(1, "ColumnId", "double");
            builder.AddComponentParameter(2, "Title", "Double");
            builder.AddComponentParameter(3, "ExportValue", (Func<TestRow, object?>)(r => r.Id * 2));
            builder.CloseComponent();
        };

        var component = RenderComponent<BitDataGrid<TestRow>>(parameters =>
        {
            parameters.Add(p => p.Items, CreateRows());
            parameters.Add(p => p.ChildContent, columns);
        });

        byte[] bytes = null!;
        await component.InvokeAsync(async () => bytes = await component.Instance.ToExcelAsync());
        using var stream = new System.IO.MemoryStream(bytes);
        using var zip = new System.IO.Compression.ZipArchive(stream, System.IO.Compression.ZipArchiveMode.Read);
        using var reader = new System.IO.StreamReader(zip.GetEntry("xl/worksheets/sheet1.xml")!.Open());
        var sheet = reader.ReadToEnd();

        StringAssert.Contains(sheet, "<c><v>2</v></c>", "the computed value must be a native numeric cell");
        StringAssert.Contains(sheet, "<t xml:space=\"preserve\">Double</t>");
    }

    [TestMethod]
    public async Task ExportFileNameNamesTheDownloadedFiles()
    {
        var component = RenderGrid(configure: parameters => parameters.Add(p => p.ExportFileName, "orders"));

        await component.InvokeAsync(() => component.Instance.ExportCsvAsync());

        var args = Context.JSInterop.Invocations["BitBlazorUI.DataGrid.download"].Single().Arguments;
        Assert.AreEqual("orders.csv", args[0]);
        // Excel decodes a BOM-less CSV with the system code page, so the download carries one.
        StringAssert.StartsWith((string)args[1]!, "﻿");
    }

    // -------------------------------------------------------------- Sorting

    [TestMethod]
    public async Task ColumnComparerOverridesTheDefaultOrdering()
    {
        RenderFragment columns = builder =>
        {
            builder.OpenComponent<BitDataGridColumn<TestRow>>(0);
            builder.AddComponentParameter(1, "Field", "Name");
            builder.CloseComponent();
            builder.OpenComponent<BitDataGridColumn<TestRow>>(2);
            builder.AddComponentParameter(3, "Field", "Price");
            // Orders by absolute value, which the default value comparer cannot express.
            builder.AddComponentParameter(4, "Comparer", (IComparer<object?>)new AbsComparer());
            builder.CloseComponent();
        };

        var component = RenderComponent<BitDataGrid<TestRow>>(parameters =>
        {
            parameters.Add(p => p.Items, CreateRows());
            parameters.Add(p => p.ChildContent, columns);
        });

        await component.InvokeAsync(() => component.Instance.SortByAsync("Price", BitDataGridSortDirection.Ascending));

        // |1| < |2.5| < |-5| < |7| < |10|
        CollectionAssert.AreEqual(new[] { "Elderberry", "Banana", "Apple", "Date", "Cherry" },
            FirstCellTexts(component).ToArray());
    }

    private sealed class AbsComparer : IComparer<object?>
    {
        public int Compare(object? x, object? y) => Math.Abs((double)(x ?? 0d)).CompareTo(Math.Abs((double)(y ?? 0d)));
    }

    // ------------------------------------------------------------- Grouping

    [TestMethod]
    public async Task GroupsInitiallyCollapsedAndExpandCollapseAllFlipEveryLevel()
    {
        var component = RenderGrid(configure: parameters =>
        {
            parameters.Add(p => p.Groupable, true);
            parameters.Add(p => p.GroupsInitiallyCollapsed, true);
        });

        await component.InvokeAsync(() => component.Instance.GroupByAsync("Name"));

        // Every row is its own group here; collapsed groups render headers but no rows.
        Assert.AreEqual(5, component.FindAll(".bit-dtg-group-row").Count);
        Assert.AreEqual(0, component.FindAll(".bit-dtg-body > .bit-dtg-row:not(.bit-dtg-group-row)").Count);

        await component.InvokeAsync(() => component.Instance.ExpandAllGroupsAsync());
        Assert.AreEqual(5, component.FindAll(".bit-dtg-body > .bit-dtg-row:not(.bit-dtg-group-row)").Count);

        await component.InvokeAsync(() => component.Instance.CollapseAllGroupsAsync());
        Assert.AreEqual(0, component.FindAll(".bit-dtg-body > .bit-dtg-row:not(.bit-dtg-group-row)").Count);
    }

    [TestMethod]
    public async Task ExpandAllGroupsSurvivesARegrouping()
    {
        var component = RenderGrid(configure: parameters =>
        {
            parameters.Add(p => p.Groupable, true);
            parameters.Add(p => p.GroupsInitiallyCollapsed, true);
        });

        await component.InvokeAsync(() => component.Instance.GroupByAsync("Name"));
        await component.InvokeAsync(() => component.Instance.ExpandAllGroupsAsync());
        await component.InvokeAsync(() => component.Instance.UngroupAsync("Name"));
        await component.InvokeAsync(() => component.Instance.GroupByAsync("Price"));

        Assert.AreEqual(5, component.FindAll(".bit-dtg-body > .bit-dtg-row:not(.bit-dtg-group-row)").Count,
            "expand-all flips the default every group follows, including groups built later");
    }

    // --------------------------------------------------------- View events

    [TestMethod]
    public async Task ViewChangesAreReportedThroughTheirCallbacks()
    {
        IReadOnlyList<BitDataGridSortDescriptor>? sorts = null;
        IReadOnlyList<BitDataGridFilterDescriptor>? filters = null;
        IReadOnlyList<BitDataGridGroupDescriptor>? groups = null;
        var page = 0;

        var component = RenderGrid(configure: parameters =>
        {
            parameters.Add(p => p.Pageable, true);
            parameters.Add(p => p.PageSize, 2);
            parameters.Add(p => p.Groupable, true);
            parameters.Add(p => p.OnSortChange, (IReadOnlyList<BitDataGridSortDescriptor> s) => sorts = s);
            parameters.Add(p => p.OnFilterChange, (IReadOnlyList<BitDataGridFilterDescriptor> f) => filters = f);
            parameters.Add(p => p.OnGroupChange, (IReadOnlyList<BitDataGridGroupDescriptor> g) => groups = g);
            parameters.Add(p => p.OnPageChange, (int p2) => page = p2);
        });

        component.FindAll(".bit-dtg-header-row .bit-dtg-htext")[0].Click();
        Assert.AreEqual(1, sorts!.Count);
        Assert.AreEqual("Name", sorts![0].ColumnId);

        await component.InvokeAsync(() => component.Instance.ApplyFilterAsync("Name", BitDataGridFilterOperator.Contains, "e"));
        Assert.AreEqual(1, filters!.Count);

        await component.InvokeAsync(() => component.Instance.GroupByAsync("Name"));
        Assert.AreEqual(1, groups!.Count);
        await component.InvokeAsync(() => component.Instance.ClearGroupsAsync());
        Assert.AreEqual(0, groups!.Count);

        await component.InvokeAsync(() => component.Instance.GoToPageAsync(2));
        Assert.AreEqual(2, page);

        await component.InvokeAsync(() => component.Instance.ClearFiltersAsync());
        Assert.AreEqual(0, filters!.Count);
    }

    // ------------------------------------------------------- Column auto-fit

    [TestMethod]
    public async Task AutoFitSizesTheColumnToItsMeasuredContent()
    {
        Context.JSInterop.Setup<double>("BitBlazorUI.DataGrid.measureColumnContentWidth", _ => true).SetResult(240d);

        var component = RenderGrid(configure: parameters => parameters.Add(p => p.Resizable, true));

        await component.InvokeAsync(() => component.Instance.AutoFitColumnAsync("Name"));

        var template = component.Find(".bit-dtg-table").GetAttribute("style");
        StringAssert.Contains(template, "240px", "the measured width must become the column's track");
    }

    [TestMethod]
    public async Task AutoFitStaysWithinTheColumnsWidthBounds()
    {
        Context.JSInterop.Setup<double>("BitBlazorUI.DataGrid.measureColumnContentWidth", _ => true).SetResult(1000d);

        RenderFragment columns = builder =>
        {
            builder.OpenComponent<BitDataGridColumn<TestRow>>(0);
            builder.AddComponentParameter(1, "Field", "Name");
            builder.AddComponentParameter(2, "MaxWidth", (int?)180);
            builder.CloseComponent();
        };

        var component = RenderComponent<BitDataGrid<TestRow>>(parameters =>
        {
            parameters.Add(p => p.Items, CreateRows());
            parameters.Add(p => p.ChildContent, columns);
            parameters.Add(p => p.Resizable, true);
        });

        await component.InvokeAsync(() => component.Instance.AutoFitColumnAsync("Name"));

        StringAssert.Contains(component.Find(".bit-dtg-table").GetAttribute("style"), "180px");
    }

    // --------------------------------------------------- Blank filter operators

    [TestMethod]
    public void BlankOperatorsFilterWithoutAValueAndHideTheEditor()
    {
        var rows = CreateRows();
        rows[2].Name = string.Empty;

        var component = RenderGrid(rows, parameters =>
        {
            parameters.Add(p => p.Filterable, true);
            parameters.Add(p => p.FilterOperators, true);
        });

        var operators = component.FindAll(".bit-dtg-filter-row .bit-dtg-filter-op")[0]
            .QuerySelectorAll("option").Select(o => o.GetAttribute("value")).ToList();
        CollectionAssert.Contains(operators, "IsEmpty");
        CollectionAssert.Contains(operators, "IsNotEmpty");

        // Choosing a value-less operator applies immediately - there is no value left to enter.
        component.FindAll(".bit-dtg-filter-row .bit-dtg-filter-op")[0].Change("IsEmpty");

        CollectionAssert.AreEqual(new[] { "" }, FirstCellTexts(component).ToArray());
        // The Price column keeps its own editor; only the column that took the blank operator loses one.
        var nameCell = component.FindAll(".bit-dtg-filter-row .bit-dtg-hcell")[0];
        Assert.AreEqual(0, nameCell.QuerySelectorAll(".bit-dtg-filter-input").Length,
            "a value-less operator must not leave an editor that collects nothing");

        component.FindAll(".bit-dtg-filter-row .bit-dtg-filter-op")[0].Change("IsNotEmpty");
        Assert.AreEqual(4, FirstCellTexts(component).Count);
    }

    [TestMethod]
    public void SwitchingAwayFromABlankOperatorClearsTheFilter()
    {
        var rows = CreateRows();
        rows[2].Name = string.Empty;

        var component = RenderGrid(rows, parameters =>
        {
            parameters.Add(p => p.Filterable, true);
            parameters.Add(p => p.FilterOperators, true);
        });

        component.FindAll(".bit-dtg-filter-row .bit-dtg-filter-op")[0].Change("IsEmpty");
        Assert.AreEqual(1, component.Instance.ActiveFilters.Count);

        // No text was ever typed, so the descriptor has nothing to re-apply under the new operator.
        component.FindAll(".bit-dtg-filter-row .bit-dtg-filter-op")[0].Change("Contains");
        Assert.AreEqual(0, component.Instance.ActiveFilters.Count);
        Assert.AreEqual(5, FirstCellTexts(component).Count);
    }

    [TestMethod]
    public async Task ClearingFiltersAlsoResetsTheChosenOperators()
    {
        var rows = CreateRows();
        rows[2].Name = string.Empty;

        var component = RenderGrid(rows, parameters =>
        {
            parameters.Add(p => p.Filterable, true);
            parameters.Add(p => p.FilterOperators, true);
        });

        component.FindAll(".bit-dtg-filter-row .bit-dtg-filter-op")[0].Change("IsEmpty");
        Assert.AreEqual(1, FirstCellTexts(component).Count);

        await component.InvokeAsync(() => component.Instance.ClearFiltersAsync());

        // A value-less operator left selected would show a criterion that is no longer applied, with no
        // editor to type into - so clearing the filters resets the operator to the column's default too.
        Assert.AreEqual("Contains", component.FindAll(".bit-dtg-filter-row .bit-dtg-filter-op")[0].GetAttribute("value"));
        var nameCell = component.FindAll(".bit-dtg-filter-row .bit-dtg-hcell")[0];
        Assert.AreEqual(1, nameCell.QuerySelectorAll(".bit-dtg-filter-input").Length);
        Assert.AreEqual(5, FirstCellTexts(component).Count);
    }

    // ------------------------------------------------------------- Tree ARIA

    [TestMethod]
    public void TreeModeReportsItselfAsATreegridWithLevelledRows()
    {
        var child = new TestRow { Id = 10, Name = "Child", Price = 1 };
        var root = new TestRow { Id = 1, Name = "Root", Price = 2 };
        var children = new Dictionary<int, List<TestRow>> { [1] = [child] };

        var component = RenderComponent<BitDataGrid<TestRow>>(parameters =>
        {
            parameters.Add(p => p.Items, new List<TestRow> { root });
            parameters.Add(p => p.ChildContent, DefaultColumns());
            parameters.Add(p => p.KeyField, (Func<TestRow, object>)(r => r.Id));
            parameters.Add(p => p.ChildrenSelector, (Func<TestRow, IEnumerable<TestRow>?>)(r =>
                children.TryGetValue(r.Id, out var c) ? c : null));
        });

        Assert.AreEqual("treegrid", component.Find(".bit-dtg-table").GetAttribute("role"));

        var rootRow = component.Find(".bit-dtg-body > .bit-dtg-row");
        Assert.AreEqual("1", rootRow.GetAttribute("aria-level"));
        Assert.AreEqual("false", rootRow.GetAttribute("aria-expanded"),
            "a collapsed node must report its expandable state on the row itself");

        component.Find(".bit-dtg-tree-toggle").Click();

        var rows = component.FindAll(".bit-dtg-body > .bit-dtg-row");
        Assert.AreEqual("true", rows[0].GetAttribute("aria-expanded"));
        Assert.AreEqual("2", rows[1].GetAttribute("aria-level"));
        Assert.IsNull(rows[1].GetAttribute("aria-expanded"), "a leaf must not advertise an expandable state");
    }

    [TestMethod]
    public void FlatGridStaysAGridWithNoRowLevels()
    {
        var component = RenderGrid();

        Assert.AreEqual("grid", component.Find(".bit-dtg-table").GetAttribute("role"));
        Assert.IsNull(component.Find(".bit-dtg-body > .bit-dtg-row").GetAttribute("aria-level"));
    }

    // -------------------------------------------------------- Multi-sorting

    [TestMethod]
    public void ShiftClickAddsToTheSortLikeCtrlClick()
    {
        var component = RenderGrid();

        component.FindAll(".bit-dtg-header-row .bit-dtg-htext")[0].Click();
        component.FindAll(".bit-dtg-header-row .bit-dtg-htext")[1]
            .Click(new MouseEventArgs { ShiftKey = true });

        var sorts = component.Instance.ActiveSorts;
        Assert.AreEqual(2, sorts.Count, "Shift+click must extend the sort rather than replace it");
        CollectionAssert.AreEqual(new[] { "Name", "Price" }, sorts.Select(s => s.ColumnId).ToArray());
    }

    // ------------------------------------------------------- Range filtering

    [TestMethod]
    public async Task ApplyRangeFilterKeepsBothBoundsAsOrdinaryDescriptors()
    {
        var component = RenderGrid();

        await component.InvokeAsync(() => component.Instance.ApplyRangeFilterAsync("Price", 1d, 7d));

        // Half-open: 1 and 2.5 are in, 7 and 10 are out, -5 is below the lower bound.
        CollectionAssert.AreEqual(new[] { "Banana", "Elderberry" }, FirstCellTexts(component).OrderBy(t => t).ToArray());

        var filters = component.Instance.ActiveFilters;
        Assert.AreEqual(2, filters.Count, "a range must travel as two ordinary comparisons");
        CollectionAssert.AreEqual(
            new[] { BitDataGridFilterOperator.GreaterThanOrEqual, BitDataGridFilterOperator.LessThan },
            filters.Select(f => f.Operator).ToArray());

        // A missing bound clears the column instead of applying half a range.
        await component.InvokeAsync(() => component.Instance.ApplyRangeFilterAsync("Price", null, null));
        Assert.AreEqual(0, component.Instance.ActiveFilters.Count);
        Assert.AreEqual(5, FirstCellTexts(component).Count);
    }

    [TestMethod]
    public async Task RestoringStateDoesNotEchoBackThroughTheChangeCallbacks()
    {
        var sortReports = 0;
        var component = RenderGrid(configure: parameters =>
            parameters.Add(p => p.OnSortChange, (IReadOnlyList<BitDataGridSortDescriptor> _) => sortReports++));

        await component.InvokeAsync(() => component.Instance.SortByAsync("Price", BitDataGridSortDirection.Descending));
        Assert.AreEqual(1, sortReports);

        var state = component.Instance.GetState();
        await component.InvokeAsync(() => component.Instance.ClearSortsAsync());
        Assert.AreEqual(2, sortReports);

        await component.InvokeAsync(() => component.Instance.ApplyStateAsync(state));
        Assert.AreEqual(2, sortReports, "a restore must not feed a persistence handler the state it just supplied");
        Assert.AreEqual(1, component.Instance.ActiveSorts.Count);
    }
}
