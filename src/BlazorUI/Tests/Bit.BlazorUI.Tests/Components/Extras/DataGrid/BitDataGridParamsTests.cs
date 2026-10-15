using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.DataGrid;

/// <summary>
/// Covers the BitParams cascade of the grid: what a BitDataGridParams fills in, what stays on the instance because it
/// belongs to one data set rather than to the app, that every property the params object carries reaches the grid,
/// and that a parameter the grid's own markup sets wins over the cascaded value.
/// </summary>
[TestClass]
public class BitDataGridParamsTests : BunitTestContext
{
    public class Row
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Price { get; set; }
        public List<Row>? Children { get; set; }
    }

    private static List<Row> Rows() =>
    [
        new() { Id = 1, Name = "Banana", Price = 2.5 },
        new() { Id = 2, Name = "Apple", Price = 5 },
        new() { Id = 3, Name = "Cherry", Price = 10 },
    ];

    private static RenderFragment Columns(BitDataGridAggregateType priceAggregate = BitDataGridAggregateType.None) => builder =>
    {
        builder.OpenComponent<BitDataGridColumn<Row>>(0);
        builder.AddComponentParameter(1, nameof(BitDataGridColumn<Row>.Field), "Name");
        builder.CloseComponent();
        builder.OpenComponent<BitDataGridColumn<Row>>(2);
        builder.AddComponentParameter(3, nameof(BitDataGridColumn<Row>.Field), "Price");
        builder.AddComponentParameter(4, nameof(BitDataGridColumn<Row>.Aggregate), priceAggregate);
        builder.CloseComponent();
    };

    // What belongs to one grid rather than to the app, which is why it is not on the params object: its data and the
    // selectors over it, its selection, the templates typed over TItem, the event callbacks, and the choices that go
    // with one data set rather than with every grid - editing, row reordering, virtualization, the export file name.
    private static readonly string[] _notCascaded =
    [
        nameof(BitDataGrid<Row>.AriaDescribedBy),
        nameof(BitDataGrid<Row>.AriaLabelledBy),
        nameof(BitDataGrid<Row>.CascadingParameters),
        nameof(BitDataGrid<Row>.ChildContent),
        nameof(BitDataGrid<Row>.ChildrenProvider),
        nameof(BitDataGrid<Row>.ChildrenSelector),
        nameof(BitDataGrid<Row>.Columns),
        nameof(BitDataGrid<Row>.DetailTemplate),
        nameof(BitDataGrid<Row>.Editable),
        nameof(BitDataGrid<Row>.EditMode),
        nameof(BitDataGrid<Row>.ExpandedDetailItems),
        nameof(BitDataGrid<Row>.ExpandedDetailItemsChanged),
        nameof(BitDataGrid<Row>.ExportFileName),
        nameof(BitDataGrid<Row>.HasChildrenSelector),
        nameof(BitDataGrid<Row>.IsRowSelectionDisabled),
        nameof(BitDataGrid<Row>.Items),
        nameof(BitDataGrid<Row>.KeyField),
        nameof(BitDataGrid<Row>.Loading),
        nameof(BitDataGrid<Row>.NewItemFactory),
        nameof(BitDataGrid<Row>.OnCellClick),
        nameof(BitDataGrid<Row>.OnCellContextMenu),
        nameof(BitDataGrid<Row>.OnCellDoubleClick),
        nameof(BitDataGrid<Row>.OnDetailToggle),
        nameof(BitDataGrid<Row>.OnFilterChange),
        nameof(BitDataGrid<Row>.OnGroupChange),
        nameof(BitDataGrid<Row>.OnLoadMore),
        nameof(BitDataGrid<Row>.OnPageChange),
        nameof(BitDataGrid<Row>.OnRead),
        nameof(BitDataGrid<Row>.OnRowCancel),
        nameof(BitDataGrid<Row>.OnRowClick),
        nameof(BitDataGrid<Row>.OnRowCreate),
        nameof(BitDataGrid<Row>.OnRowDelete),
        nameof(BitDataGrid<Row>.OnRowDoubleClick),
        nameof(BitDataGrid<Row>.OnRowReorder),
        nameof(BitDataGrid<Row>.OnRowSave),
        nameof(BitDataGrid<Row>.OnSortChange),
        nameof(BitDataGrid<Row>.OnStateChange),
        nameof(BitDataGrid<Row>.RowClass),
        nameof(BitDataGrid<Row>.RowHeightSelector),
        nameof(BitDataGrid<Row>.RowReorderable),
        nameof(BitDataGrid<Row>.RowStyle),
        nameof(BitDataGrid<Row>.SearchText),
        nameof(BitDataGrid<Row>.SearchTextChanged),
        nameof(BitDataGrid<Row>.SelectedItems),
        nameof(BitDataGrid<Row>.SelectedItemsChanged),
        nameof(BitDataGrid<Row>.SelectionMode),
        nameof(BitDataGrid<Row>.ToolbarTemplate),
        nameof(BitDataGrid<Row>.Virtualize),
        nameof(BitDataGrid<Row>.VirtualizeColumns),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(
        BitDataGridParams gridParams,
        Action<RenderTreeBuilder>? extraAttributes = null,
        List<Row>? items = null,
        RenderFragment? columns = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { gridParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitDataGrid<Row>>(0);
                builder.AddComponentParameter(1, nameof(BitDataGrid<Row>.Items), items ?? Rows());
                builder.AddComponentParameter(2, nameof(BitDataGrid<Row>.ChildContent), columns ?? Columns());
                extraAttributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    private static BitDataGrid<Row> Grid(IRenderedComponent<BitParams> component) => component.FindComponent<BitDataGrid<Row>>().Instance;

    private static IReadOnlyList<IElement> BodyRows(IRenderedComponent<BitParams> component)
        => component.FindAll(".bit-dtg-body > .bit-dtg-row:not(.bit-dtg-message-row)");

    private static string[] FirstCellTexts(IRenderedComponent<BitParams> component)
        => BodyRows(component).Select(r => r.QuerySelector(".bit-dtg-cell")!.TextContent.Trim()).ToArray();

    [TestMethod]
    public void BitDataGridParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitDataGrid", BitDataGridParams.ParamName);
    }

    [TestMethod]
    public void BitDataGridParamsShouldImplementIBitComponentParams()
    {
        var gridParams = new BitDataGridParams();

        Assert.IsInstanceOfType<IBitComponentParams>(gridParams);
        Assert.AreEqual(BitDataGridParams.ParamName, gridParams.Name);
    }

    [TestMethod]
    public void BitDataGridParamsShouldCarryEveryParameterThatIsNotPerGrid()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitDataGrid<Row>).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                 .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                                 .Select(p => p.Name)
                                                 .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitDataGridParams).GetProperty(name), $"BitDataGridParams has no {name}.");
        }
    }

    // Every property of the params object is one the grid takes from the cascade, so a property added to one and not
    // wired to the other - or wired to the wrong parameter - fails here rather than going silently unapplied.
    [TestMethod]
    public void BitDataGridParamsShouldApplyEveryOneOfItsProperties()
    {
        var defaults = new BitDataGrid<Row>();
        var gridParams = new BitDataGridParams();
        var supplied = new Dictionary<string, object>();

        foreach (var property in typeof(BitDataGridParams).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(p => p.CanWrite))
        {
            var parameter = typeof(BitDataGrid<Row>).GetProperty(property.Name);

            Assert.IsNotNull(parameter, $"{property.Name} is not a property of BitDataGrid.");
            Assert.IsTrue(parameter.IsDefined(typeof(ParameterAttribute), true), $"{property.Name} is not a parameter of BitDataGrid.");

            // Each value differs from the grid's default, so a property left unapplied cannot pass for an applied one.
            var value = SampleValue(property.PropertyType, parameter.GetValue(defaults));

            property.SetValue(gridParams, value);
            supplied.Add(property.Name, value);
        }

        var grid = Grid(RenderWithParams(gridParams));

        foreach (var (name, value) in supplied)
        {
            Assert.AreEqual(value, typeof(BitDataGrid<Row>).GetProperty(name)!.GetValue(grid), $"{name} is not applied by the cascade.");
        }
    }

    [TestMethod]
    public void BitDataGridShouldApplyTheCascadedAppearance()
    {
        var component = RenderWithParams(new BitDataGridParams
        {
            Striped = false,
            Hoverable = false,
            Bordered = false,
            Class = "cascaded-root",
            Dir = BitDir.Rtl,
            Classes = new() { HeaderRow = "cascaded-header" },
            Styles = new() { Root = "margin: 1px;" },
        });

        var root = component.Find(".bit-dtg");

        Assert.IsFalse(root.ClassList.Contains("bit-dtg-striped"));
        Assert.IsFalse(root.ClassList.Contains("bit-dtg-hoverable"));
        Assert.IsFalse(root.ClassList.Contains("bit-dtg-bordered"));
        Assert.IsTrue(root.ClassList.Contains("cascaded-root"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        StringAssert.Contains(root.GetAttribute("style"), "margin: 1px;");
        Assert.IsNotNull(root.QuerySelector(".bit-dtg-header-row.cascaded-header"));
    }

    [TestMethod]
    public void BitDataGridShouldApplyTheCascadedChrome()
    {
        var component = RenderWithParams(new BitDataGridParams
        {
            ShowToolbar = true,
            ShowSearchBox = true,
            ShowColumnChooser = true,
            ShowCsvExport = true,
            ShowExcelExport = true,
            ShowRowNumbers = true,
            ShowFooter = true,
            ShowCellTooltips = true,
            WrapCellText = true,
            Strings = new BitDataGridStrings { GridLabel = "Cascaded label", ExportCsvText = "CSV please" },
        }, columns: Columns(BitDataGridAggregateType.Sum));

        Assert.IsNotNull(component.Find(".bit-dtg-toolbar"));
        Assert.IsNotNull(component.Find(".bit-dtg-search-input"));
        Assert.IsNotNull(component.Find(".bit-dtg-toolbar button[aria-controls]"));

        var toolbarTexts = component.FindAll(".bit-dtg-toolbar button").Select(b => b.TextContent.Trim()).ToList();
        CollectionAssert.Contains(toolbarTexts, "CSV please");
        CollectionAssert.Contains(toolbarTexts, "Export Excel");

        Assert.IsNotNull(component.Find(".bit-dtg-hcell-rownumber"));
        Assert.IsNotNull(component.Find(".bit-dtg-footer-row"));
        Assert.AreEqual("Cascaded label", component.Find("[role=grid]").GetAttribute("aria-label"));

        var nameCell = BodyRows(component)[0].QuerySelector(".bit-dtg-cell:not(.bit-dtg-cell-rownumber)")!;
        Assert.AreEqual("Banana", nameCell.GetAttribute("title"));
        Assert.IsTrue(nameCell.ClassList.Contains("bit-dtg-cell-wrap"));
    }

    [TestMethod]
    public void BitDataGridShouldDropTheHeaderFromTheCascade()
    {
        var component = RenderWithParams(new BitDataGridParams { ShowHeader = false });

        Assert.AreEqual(0, component.FindAll(".bit-dtg-header-row").Count);
        Assert.AreEqual(3, BodyRows(component).Count);
    }

    [TestMethod]
    public void BitDataGridShouldApplyTheCascadedSortingChoices()
    {
        var unsortable = RenderWithParams(new BitDataGridParams { Sortable = false });
        Assert.AreEqual(0, unsortable.FindAll("button.bit-dtg-htext").Count);

        var component = RenderWithParams(new BitDataGridParams { MultiSort = false, AllowUnsorted = false });
        var grid = Grid(component);
        IElement Header(int index) => component.FindAll("button.bit-dtg-htext")[index];

        // Ctrl+click replaces the sort rather than adding to it.
        Header(0).Click();
        Header(1).Click(new MouseEventArgs { CtrlKey = true });
        Assert.AreEqual(1, grid.ActiveSorts.Count);
        Assert.AreEqual("Price", grid.ActiveSorts[0].ColumnId);

        // The third click restarts the cycle instead of dropping the sort.
        Header(1).Click();
        Header(1).Click();
        Assert.AreEqual(1, grid.ActiveSorts.Count);
        Assert.AreEqual(BitDataGridSortDirection.Ascending, grid.ActiveSorts[0].Direction);
    }

    [TestMethod]
    public void BitDataGridShouldApplyTheCascadedFiltering()
    {
        var component = RenderWithParams(new BitDataGridParams { Filterable = true, FilterOperators = true, FilterDebounce = 0 });

        Assert.IsNotNull(component.Find(".bit-dtg-filter-row"));
        Assert.AreEqual(2, component.FindAll(".bit-dtg-filter-op").Count);

        // A zero debounce applies every keystroke at once.
        component.Find(".bit-dtg-filter-input:not([inputmode=decimal])").Input("err");
        CollectionAssert.AreEqual(new[] { "Cherry" }, FirstCellTexts(component));
    }

    [TestMethod]
    public void BitDataGridShouldApplyTheCascadedSearch()
    {
        var component = RenderWithParams(new BitDataGridParams { ShowSearchBox = true, SearchDebounce = 0 });

        component.Find(".bit-dtg-search-input").Input("app");

        CollectionAssert.AreEqual(new[] { "Apple" }, FirstCellTexts(component));
    }

    [TestMethod]
    public void BitDataGridShouldApplyTheCascadedPaging()
    {
        var component = RenderWithParams(new BitDataGridParams
        {
            Pageable = true,
            PageSize = 2,
            PageSizeOptions = [2, 4],
            PagerPlacement = BitPlacement.Top,
        });

        Assert.AreEqual(2, BodyRows(component).Count);
        Assert.AreEqual(2, Grid(component).TotalPages);
        CollectionAssert.AreEqual(new[] { "2", "4" }, component.FindAll(".bit-dtg-page-size option").Select(o => o.GetAttribute("value")).ToArray());

        // The pager sits above the rows.
        var parts = component.FindAll(".bit-dtg-pager, .bit-dtg-viewport").Select(e => e.ClassList.Contains("bit-dtg-pager") ? "pager" : "viewport").ToArray();
        CollectionAssert.AreEqual(new[] { "pager", "viewport" }, parts);
    }

    [TestMethod]
    public async Task BitDataGridShouldApplyTheCascadedGrouping()
    {
        var component = RenderWithParams(new BitDataGridParams { Groupable = true, GroupsInitiallyCollapsed = true });

        Assert.IsTrue(component.FindAll(".bit-dtg-group-btn").Count > 0);

        await component.InvokeAsync(() => Grid(component).GroupByAsync("Name"));

        Assert.AreEqual(3, component.FindAll(".bit-dtg-group-row").Count);
        Assert.AreEqual(0, component.FindAll(".bit-dtg-body > .bit-dtg-row:not(.bit-dtg-group-row)").Count);
    }

    [TestMethod]
    public void BitDataGridShouldApplyTheCascadedColumnInteractions()
    {
        var component = RenderWithParams(new BitDataGridParams { Resizable = true, Reorderable = true, CellNavigation = true });

        Assert.IsTrue(component.FindAll(".bit-dtg-resizer").Count > 0);
        Assert.IsTrue(component.FindAll(".bit-dtg-header-row .bit-dtg-hcell").All(h => h.GetAttribute("draggable") == "true"));
        Assert.IsTrue(component.FindAll(".bit-dtg-body .bit-dtg-cell[tabindex]").Count > 0);
    }

    [TestMethod]
    public void BitDataGridShouldApplyTheCascadedTemplates()
    {
        var empty = RenderWithParams(new BitDataGridParams
        {
            EmptyTemplate = b => b.AddMarkupContent(0, "<span class=\"cascaded-empty\">Nothing</span>"),
        }, items: []);

        Assert.IsNotNull(empty.Find(".bit-dtg-empty .cascaded-empty"));

        var loading = RenderWithParams(new BitDataGridParams
        {
            LoadingTemplate = b => b.AddMarkupContent(0, "<span class=\"cascaded-loading\">Fetching</span>"),
        }, builder => builder.AddComponentParameter(10, nameof(BitDataGrid<Row>.Loading), true));

        Assert.IsNotNull(loading.Find(".bit-dtg-loading .cascaded-loading"));
    }

    [TestMethod]
    public void BitDataGridShouldApplyTheCascadedLayout()
    {
        var component = RenderWithParams(new BitDataGridParams { Height = "480px", RowHeight = 48 });

        StringAssert.Contains(component.Find(".bit-dtg-viewport").GetAttribute("style"), "height:480px;");
        StringAssert.Contains(BodyRows(component)[0].GetAttribute("style"), "min-height:48px;");
    }

    [TestMethod]
    public void BitDataGridShouldApplyTheCascadedDetailChoices()
    {
        var component = RenderWithParams(new BitDataGridParams { ShowDetailToggle = false, ExpandDetailOnRowClick = true },
            builder => builder.AddComponentParameter(10, nameof(BitDataGrid<Row>.DetailTemplate), (RenderFragment<Row>)(r => b => b.AddContent(0, $"DETAIL-{r.Id}"))));

        Assert.AreEqual(0, component.FindAll(".bit-dtg-cell-detail").Count);

        BodyRows(component)[0].Click();

        Assert.AreEqual("DETAIL-1", component.Find(".bit-dtg-detail-content").TextContent.Trim());
    }

    [TestMethod]
    public void BitDataGridShouldApplyTheCascadedTreeExpansion()
    {
        var roots = new List<Row>
        {
            new() { Id = 1, Name = "Fruit", Children = [new() { Id = 2, Name = "Apple" }, new() { Id = 3, Name = "Banana" }] },
        };

        var component = RenderWithParams(new BitDataGridParams { TreeInitiallyExpanded = true }, builder =>
        {
            builder.AddComponentParameter(10, nameof(BitDataGrid<Row>.KeyField), (Func<Row, object>)(r => r.Id));
            builder.AddComponentParameter(11, nameof(BitDataGrid<Row>.ChildrenSelector), (Func<Row, IEnumerable<Row>?>)(r => r.Children));
        }, items: roots);

        CollectionAssert.AreEqual(new[] { "Fruit", "Apple", "Banana" },
            BodyRows(component).Select(r => r.QuerySelector(".bit-dtg-cell-text")!.TextContent.Trim()).ToArray());
    }

    [TestMethod]
    public void BitDataGridShouldApplyTheCascadedClipboardCopy()
    {
        Context.JSInterop.Setup<bool>("BitBlazorUI.DataGrid.copyToClipboard", _ => true).SetResult(true);

        var component = RenderWithParams(new BitDataGridParams { ClipboardCopy = true, CellNavigation = true });

        component.Find(".bit-dtg-body > .bit-dtg-row .bit-dtg-cell").KeyDown(new KeyboardEventArgs { Key = "c", CtrlKey = true });

        component.WaitForAssertion(() =>
        {
            var payload = (string)Context.JSInterop.Invocations["BitBlazorUI.DataGrid.copyToClipboard"].Single().Arguments[0]!;
            StringAssert.Contains(payload, "Banana\t2.5");
        });
    }

    [TestMethod]
    public async Task BitDataGridShouldApplyTheCascadedExcelStyling()
    {
        var component = RenderWithParams(new BitDataGridParams { ExcelExportStyled = true });

        await component.InvokeAsync(async () => _ = await Grid(component).ToExcelAsync());

        Context.JSInterop.VerifyInvoke("BitBlazorUI.DataGrid.getExportStyles");
    }

    [TestMethod]
    public void BitDataGridShouldApplyTheCascadedLoadMoreBatchSize()
    {
        var all = Rows();
        var requests = new List<BitDataGridReadRequest>();

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitDataGridParams { LoadMoreBatchSize = 2, Height = "300px" } });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitDataGrid<Row>>(0);
                builder.AddComponentParameter(1, nameof(BitDataGrid<Row>.ChildContent), Columns());
                builder.AddComponentParameter(2, nameof(BitDataGrid<Row>.OnLoadMore), (Func<BitDataGridReadRequest, Task<BitDataGridReadResult<Row>>>)(request =>
                {
                    requests.Add(request);
                    IEnumerable<Row> rows = all.Skip(request.Skip);
                    if (request.Take is { } take) rows = rows.Take(take);
                    return Task.FromResult(new BitDataGridReadResult<Row>(rows.ToList(), all.Count));
                }));
                builder.CloseComponent();
            });
        });

        component.WaitForAssertion(() =>
        {
            Assert.IsTrue(requests.Count > 0);
            Assert.AreEqual(2, requests[0].Take);
            Assert.AreEqual(2, BodyRows(component).Count);
        });
    }

    [TestMethod]
    public void BitDataGridShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitDataGridParams
        {
            Striped = false,
            ShowHeader = false,
            Pageable = true,
            PageSize = 2,
            Height = "480px",
            Strings = new BitDataGridStrings { GridLabel = "Cascaded label" },
        }, builder =>
        {
            builder.AddComponentParameter(10, nameof(BitDataGrid<Row>.Striped), true);
            builder.AddComponentParameter(11, nameof(BitDataGrid<Row>.ShowHeader), true);
            builder.AddComponentParameter(12, nameof(BitDataGrid<Row>.PageSize), 3);
            builder.AddComponentParameter(13, nameof(BitDataGrid<Row>.Height), "200px");
            builder.AddComponentParameter(14, nameof(BitDataGrid<Row>.Strings), new BitDataGridStrings { GridLabel = "Own label" });
        });

        var root = component.Find(".bit-dtg");

        Assert.IsTrue(root.ClassList.Contains("bit-dtg-striped"));
        Assert.AreEqual(1, component.FindAll(".bit-dtg-header-row").Count);
        Assert.AreEqual(3, BodyRows(component).Count, "the grid's own page size holds while the cascaded Pageable still applies");
        Assert.AreEqual(1, Grid(component).TotalPages);

        var viewportStyle = component.Find(".bit-dtg-viewport").GetAttribute("style")!;
        StringAssert.Contains(viewportStyle, "height:200px;");
        Assert.IsFalse(viewportStyle.Contains("480px"));

        Assert.AreEqual("Own label", component.Find("[role=grid]").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitDataGridShouldNotBeAffectedByAnEmptyParamsObject()
    {
        var component = RenderWithParams(new BitDataGridParams());

        var root = component.Find(".bit-dtg");

        foreach (var cls in new[] { "bit-dtg-striped", "bit-dtg-hoverable", "bit-dtg-bordered" })
        {
            Assert.IsTrue(root.ClassList.Contains(cls), cls);
        }

        Assert.AreEqual(1, component.FindAll(".bit-dtg-header-row").Count);
        Assert.AreEqual(3, BodyRows(component).Count);
        Assert.AreEqual(0, component.FindAll(".bit-dtg-toolbar, .bit-dtg-pager, .bit-dtg-filter-row, .bit-dtg-footer-row, .bit-dtg-resizer, .bit-dtg-group-btn, .bit-dtg-hcell-rownumber").Count);
        Assert.IsTrue(component.FindAll(".bit-dtg-header-row .bit-dtg-hcell").All(h => h.GetAttribute("draggable") == "false"));
        Assert.AreEqual(0, component.FindAll(".bit-dtg-body .bit-dtg-cell[tabindex]").Count);
        Assert.AreEqual("Data grid", component.Find("[role=grid]").GetAttribute("aria-label"));
    }

    private static object SampleValue(Type type, object? defaultValue)
    {
        var valueType = Nullable.GetUnderlyingType(type) ?? type;

        if (valueType == typeof(bool)) return (bool)defaultValue! is false;
        if (valueType == typeof(int)) return (int)defaultValue! + 3;
        if (valueType == typeof(float)) return (float)defaultValue! + 4f;
        if (valueType == typeof(string)) return "480px";
        if (valueType == typeof(BitPlacement)) return BitPlacement.Top;
        if (valueType == typeof(int[])) return new[] { 2, 4 };
        if (valueType == typeof(RenderFragment)) return (RenderFragment)(_ => { });
        if (valueType == typeof(BitDataGridClassStyles)) return new BitDataGridClassStyles();
        if (valueType == typeof(BitDataGridStrings)) return new BitDataGridStrings();

        throw new NotSupportedException($"No sample value for {type}.");
    }
}
