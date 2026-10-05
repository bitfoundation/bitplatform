using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.DataGrid;

[TestClass]
public class BitDataGridPrerenderTests : BunitTestContext
{
    public class TestRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    // The window a virtualized grid renders before anything has been measured, at the default 36px RowHeight:
    // ceil(600 / 36) rows for a 600px viewport, three overscan rows on each side, and one more.
    private const int EstimatedWindow = 24;

    private static List<TestRow> CreateRows(int count)
        => Enumerable.Range(0, count).Select(i => new TestRow { Id = i, Name = $"row-{i};" }).ToList();

    private static RenderFragment Columns() => builder =>
    {
        builder.OpenComponent<BitDataGridColumn<TestRow>>(0);
        builder.AddComponentParameter(1, "Field", "Name");
        builder.CloseComponent();
    };

    private static Func<BitDataGridReadRequest, Task<BitDataGridReadResult<TestRow>>> Reader(List<TestRow> all, List<BitDataGridReadRequest> requests)
        => request =>
        {
            requests.Add(request);
            IEnumerable<TestRow> rows = all.Skip(request.Skip);
            if (request.Take is { } take) rows = rows.Take(take);
            return Task.FromResult(new BitDataGridReadResult<TestRow>(rows.ToList(), all.Count));
        };

    private static List<string> RenderedRows(string html) => Regex.Matches(html, "row-\\d+;").Select(m => m.Value).ToList();

    private static int RenderedRowCount(string html) => RenderedRows(html).Count;

    [TestMethod]
    public async Task BitDataGridShouldPrerenderTheFirstWindowOfVirtualizedRows()
    {
        var html = await Prerenderer.RenderAsync<BitDataGrid<TestRow>>(new Dictionary<string, object?>
        {
            [nameof(BitDataGrid<TestRow>.Items)] = CreateRows(1000),
            [nameof(BitDataGrid<TestRow>.Virtualize)] = true,
            [nameof(BitDataGrid<TestRow>.ChildContent)] = Columns(),
        });

        StringAssert.Contains(html, "row-0;");
        StringAssert.Contains(html, $"row-{EstimatedWindow - 1};");
        Assert.AreEqual(EstimatedWindow, RenderedRowCount(html), "only the first window is rendered, not the whole set");

        // The first data row comes right after the single header row.
        StringAssert.Contains(html, "aria-rowindex=\"2\"");

        // The rows past the window are reserved by a spacer, so the scroll height is already the real one.
        StringAssert.Contains(html, $"height: {(1000 - EstimatedWindow) * 36}px");
    }

    [TestMethod]
    public async Task BitDataGridShouldPrerenderAllRowsOfAShortVirtualizedSet()
    {
        var html = await Prerenderer.RenderAsync<BitDataGrid<TestRow>>(new Dictionary<string, object?>
        {
            [nameof(BitDataGrid<TestRow>.Items)] = CreateRows(5),
            [nameof(BitDataGrid<TestRow>.Virtualize)] = true,
            [nameof(BitDataGrid<TestRow>.ChildContent)] = Columns(),
        });

        Assert.AreEqual(5, RenderedRowCount(html));
    }

    [TestMethod]
    public async Task BitDataGridShouldPrerenderVirtualizedInfiniteScrollingRows()
    {
        var all = CreateRows(1000);
        var html = await Prerenderer.RenderAsync<BitDataGrid<TestRow>>(new Dictionary<string, object?>
        {
            [nameof(BitDataGrid<TestRow>.Virtualize)] = true,
            [nameof(BitDataGrid<TestRow>.LoadMoreBatchSize)] = 50,
            [nameof(BitDataGrid<TestRow>.OnLoadMore)] = Reader(all, []),
            [nameof(BitDataGrid<TestRow>.ChildContent)] = Columns(),
        });

        StringAssert.Contains(html, "row-0;");
        StringAssert.Contains(html, $"row-{EstimatedWindow - 1};");
        Assert.AreEqual(EstimatedWindow, RenderedRowCount(html));
        StringAssert.Contains(html, $"height: {(50 - EstimatedWindow) * 36}px");
    }

    [TestMethod]
    public async Task BitDataGridShouldPrerenderTheFirstWindowOfServerVirtualizedRows()
    {
        var all = CreateRows(1000);
        var requests = new List<BitDataGridReadRequest>();
        var html = await Prerenderer.RenderAsync<BitDataGrid<TestRow>>(new Dictionary<string, object?>
        {
            [nameof(BitDataGrid<TestRow>.Virtualize)] = true,
            [nameof(BitDataGrid<TestRow>.OnRead)] = Reader(all, requests),
            [nameof(BitDataGrid<TestRow>.ChildContent)] = Columns(),
        });

        // One read, for the first window only - the same read the non-virtualized server mode already makes
        // before the grid is interactive.
        Assert.AreEqual(1, requests.Count);
        Assert.AreEqual(0, requests[0].Skip);
        Assert.AreEqual(EstimatedWindow, requests[0].Take);

        StringAssert.Contains(html, "row-0;");
        StringAssert.Contains(html, $"row-{EstimatedWindow - 1};");
        Assert.AreEqual(EstimatedWindow, RenderedRowCount(html));
        StringAssert.Contains(html, "aria-rowindex=\"2\"");
        StringAssert.Contains(html, $"height: {(1000 - EstimatedWindow) * 36}px");
    }

    [TestMethod]
    public void BitDataGridShouldHandVirtualizeTheServerRowsItAlreadyRead()
    {
        // Fewer rows than the window read ahead at this RowHeight, so that window already holds every row Virtualize
        // can ask for: how many rows Virtualize asks bUnit's stand-in for its JS for differs between the TFMs, and
        // net8.0's asks for the whole set.
        var all = CreateRows(150);
        var requests = new List<BitDataGridReadRequest>();
        var component = RenderComponent<BitDataGrid<TestRow>>(parameters =>
        {
            // Short rows make the window read ahead larger than the one bUnit's stand-in for Virtualize's JS asks for.
            parameters.Add(p => p.RowHeight, 4f);
            parameters.Add(p => p.Virtualize, true);
            parameters.Add(p => p.OnRead, Reader(all, requests));
            parameters.Add(p => p.ChildContent, Columns());
        });

        component.WaitForAssertion(() =>
        {
            // Virtualize's first window is answered from the rows already read, not read a second time...
            Assert.AreEqual(1, requests.Count);
            // ...and once Virtualize renders them, the rows rendered in its place are gone rather than doubled.
            var rows = RenderedRows(component.Markup);
            Assert.IsTrue(rows.Count > 0);
            Assert.AreEqual(rows.Count, rows.Distinct().Count());
            Assert.AreEqual(0, component.FindAll(".bit-dtg-placeholder-row").Count);
        });
    }

    [TestMethod]
    public void BitDataGridShouldReadVirtualizeAWindowLargerThanTheOneAlreadyRead()
    {
        var all = CreateRows(1000);
        var requests = new List<BitDataGridReadRequest>();
        var component = RenderComponent<BitDataGrid<TestRow>>(parameters =>
        {
            parameters.Add(p => p.Virtualize, true);
            parameters.Add(p => p.OnRead, Reader(all, requests));
            parameters.Add(p => p.ChildContent, Columns());
        });

        component.WaitForAssertion(() =>
        {
            // Only the rows past the window already read are read again: the two reads never overlap.
            Assert.AreEqual(2, requests.Count);
            Assert.AreEqual(EstimatedWindow, requests[0].Take);
            Assert.AreEqual(EstimatedWindow, requests[1].Skip);
            Assert.IsTrue(requests[1].Take > 0);

            var rows = RenderedRows(component.Markup);
            Assert.AreEqual(EstimatedWindow + requests[1].Take, rows.Count);
            Assert.AreEqual(rows.Count, rows.Distinct().Count());
        });
    }
}
