using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.DataGrid;

/// <summary>
/// BitDataGridReadRequest.Aggregates: the aggregates the footer shows, which a server grid asks its OnRead for so the
/// data source can compute them over the whole filtered data. The first read goes out before the columns register,
/// so a source that computes aggregates is read once more for them - and only then, and only on the reads whose
/// aggregates the grid shows.
/// </summary>
[TestClass]
public class BitDataGridAggregateRequestTests : BunitTestContext
{
    public class Row
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public double Rating { get; set; }
    }

    private static List<Row> CreateRows(int count = 5)
        => Enumerable.Range(1, count).Select(i => new Row { Id = i, Name = $"row-{i}", Price = i * 10, Stock = i, Rating = i % 5 }).ToList();

    // One column of every kind: no aggregate; built-in ones, one of them hidden; an AggregateBy of its own, which wins
    // over its Aggregate; and an Aggregate on a column with no field, which the grid cannot aggregate either.
    private static RenderFragment Columns(BitDataGridAggregateType priceAggregate = BitDataGridAggregateType.Sum) => builder =>
    {
        builder.OpenComponent<BitDataGridColumn<Row>>(0);
        builder.AddComponentParameter(1, "Field", "Name");
        builder.CloseComponent();

        builder.OpenComponent<BitDataGridColumn<Row>>(10);
        builder.AddComponentParameter(11, "Field", "Price");
        builder.AddComponentParameter(12, "Aggregate", priceAggregate);
        builder.CloseComponent();

        builder.OpenComponent<BitDataGridColumn<Row>>(20);
        builder.AddComponentParameter(21, "Field", "Stock");
        builder.AddComponentParameter(22, "Aggregate", BitDataGridAggregateType.Max);
        builder.AddComponentParameter(23, "Visible", false);
        builder.CloseComponent();

        builder.OpenComponent<BitDataGridColumn<Row>>(30);
        builder.AddComponentParameter(31, "Field", "Rating");
        builder.AddComponentParameter(32, "Aggregate", BitDataGridAggregateType.Average);
        builder.AddComponentParameter(33, "AggregateBy", (Func<IReadOnlyList<Row>, object?>)(rows => rows.Select(r => r.Rating).Distinct().Count()));
        builder.CloseComponent();

        builder.OpenComponent<BitDataGridColumn<Row>>(40);
        builder.AddComponentParameter(41, "ColumnId", "Actions");
        builder.AddComponentParameter(42, "Aggregate", BitDataGridAggregateType.Count);
        builder.AddComponentParameter(43, "Template", (RenderFragment<Row>)(row => b => b.AddContent(0, row.Id)));
        builder.CloseComponent();
    };

    private static RenderFragment NameOnly() => builder =>
    {
        builder.OpenComponent<BitDataGridColumn<Row>>(0);
        builder.AddComponentParameter(1, "Field", "Name");
        builder.CloseComponent();
    };

    private static string[] Describe(BitDataGridReadRequest request)
        => request.Aggregates.Select(a => $"{a.ColumnId}:{a.Type}").ToArray();

    private static readonly string[] Declared = ["Price:Sum", "Stock:Max", "Rating:Custom"];

    private static BitDataGridAggregateResult ServerAggregate(string columnId, BitDataGridAggregateType type)
        => new() { ColumnId = columnId, Type = type, Value = 42, FormattedValue = $"{columnId}-from-server" };

    // How a data source answers for the aggregates.
    public enum Source
    {
        // It returns none: the footer aggregates the page itself.
        None,
        // It computes those the request names.
        Asked,
        // It computes those the request names, except the stock, which it cannot.
        AskedButStock,
        // It computes every one the grid can show, whatever the request names.
        Everything,
    }

    // A source answering every read at once with the rows the request asks for.
    private static Func<BitDataGridReadRequest, Task<BitDataGridReadResult<Row>>> Reader(List<BitDataGridReadRequest> reads, Source source)
        => request =>
        {
            reads.Add(request);
            var all = CreateRows();
            IEnumerable<Row> rows = all.Skip(request.Skip);
            if (request.Take is { } take) rows = rows.Take(take);
            var aggregates = source switch
            {
                Source.Asked => request.Aggregates.Select(a => ServerAggregate(a.ColumnId, a.Type)).ToList(),
                Source.AskedButStock => request.Aggregates.Where(a => a.ColumnId != "Stock").Select(a => ServerAggregate(a.ColumnId, a.Type)).ToList(),
                Source.Everything => Declared.Select(d => d.Split(':')).Select(d => ServerAggregate(d[0], Enum.Parse<BitDataGridAggregateType>(d[1]))).ToList(),
                _ => null,
            };
            return Task.FromResult(new BitDataGridReadResult<Row>(rows.ToList(), all.Count) { Aggregates = aggregates });
        };

    private IRenderedComponent<BitDataGrid<Row>> RenderPaged(List<BitDataGridReadRequest> reads, Source source, RenderFragment? columns = null)
        => RenderComponent<BitDataGrid<Row>>(parameters =>
        {
            parameters.Add(p => p.OnRead, Reader(reads, source));
            parameters.Add(p => p.Pageable, true);
            parameters.Add(p => p.PageSize, 2);
            parameters.Add(p => p.ShowFooter, true);
            parameters.Add(p => p.ChildContent, columns ?? Columns());
        });

    private static string Footer(IRenderedComponent<BitDataGrid<Row>> component) => component.Find(".bit-dtg-footer-row").TextContent;

    [TestMethod]
    public async Task BitDataGridShouldAskOnReadForTheAggregatesItsColumnsDeclare()
    {
        var reads = new List<BitDataGridReadRequest>();
        var component = RenderPaged(reads, Source.Asked);

        // The first read goes out before the columns register, so it names none. The source answered it with
        // aggregates, which miss the ones the columns declare: the grid reads once more, naming them, in column order.
        component.WaitForAssertion(() => Assert.HasCount(2, reads));
        Assert.IsEmpty(reads[0].Aggregates);
        CollectionAssert.AreEqual(Declared, Describe(reads[1]));
        StringAssert.Contains(Footer(component), "Price-from-server");
        StringAssert.Contains(Footer(component), "Rating-from-server");

        // Every read of a page names them from then on.
        await component.InvokeAsync(() => component.Instance.GoToPageAsync(2));
        Assert.HasCount(3, reads);
        CollectionAssert.AreEqual(Declared, Describe(reads[2]));
    }

    // Sources written before the request named the aggregates are read as before: one returning every aggregate the
    // footer shows is not read again, and one returning none leaves the footer to aggregate the page.
    [TestMethod]
    [DataRow(Source.Everything)]
    [DataRow(Source.None)]
    public void BitDataGridShouldNotReadAgainForAggregatesWhenTheSourceReturnsThemAllOrNone(Source source)
    {
        var reads = new List<BitDataGridReadRequest>();
        var component = RenderPaged(reads, source);
        component.Render();

        Assert.HasCount(1, reads);
        StringAssert.Contains(Footer(component), source == Source.Everything ? "Price-from-server" : "30");
    }

    // A source that cannot compute one of the aggregates is read once more for it, not once more per render.
    [TestMethod]
    public void BitDataGridShouldReadAgainOnlyOnceForAnAggregateTheSourceNeverReturns()
    {
        var reads = new List<BitDataGridReadRequest>();
        var component = RenderPaged(reads, Source.AskedButStock);
        component.WaitForAssertion(() => Assert.HasCount(2, reads));

        component.Render();
        component.Render();
        Assert.HasCount(2, reads);
    }

    [TestMethod]
    public void BitDataGridShouldReadAgainWhenAColumnChangesItsAggregate()
    {
        var reads = new List<BitDataGridReadRequest>();
        var component = RenderPaged(reads, Source.Asked);
        component.WaitForAssertion(() => Assert.HasCount(2, reads));

        component.Render(p => p.Add(x => x.ChildContent, Columns(BitDataGridAggregateType.Average)));

        component.WaitForAssertion(() => Assert.HasCount(3, reads));
        CollectionAssert.AreEqual(new[] { "Price:Average", "Stock:Max", "Rating:Custom" }, Describe(reads[2]));
    }

    [TestMethod]
    public void BitDataGridShouldAskAVirtualizedOnReadForTheAggregatesItsColumnsDeclare()
    {
        var reads = new List<BitDataGridReadRequest>();
        var component = RenderComponent<BitDataGrid<Row>>(parameters =>
        {
            parameters.Add(p => p.OnRead, Reader(reads, Source.Asked));
            parameters.Add(p => p.Virtualize, true);
            parameters.Add(p => p.Height, "300px");
            parameters.Add(p => p.ShowFooter, true);
            parameters.Add(p => p.ChildContent, Columns());
        });

        component.WaitForAssertion(() => StringAssert.Contains(Footer(component), "Price-from-server"));
        Assert.IsEmpty(reads[0].Aggregates, "the first window is read before the columns register");
        CollectionAssert.AreEqual(Declared, Describe(reads[^1]));
    }

    [TestMethod]
    public void BitDataGridShouldAskForNoAggregatesWhenNoColumnAggregates()
    {
        var reads = new List<BitDataGridReadRequest>();
        var component = RenderPaged(reads, Source.Asked, NameOnly());
        component.Render();

        Assert.HasCount(1, reads);
        Assert.IsEmpty(reads[0].Aggregates);
    }

    // The grid does not show the aggregates these reads return, so it does not have the data source compute them.
    [TestMethod]
    public async Task BitDataGridShouldAskForNoAggregatesOnTheReadsWhoseAggregatesItDoesNotShow()
    {
        var reads = new List<BitDataGridReadRequest>();
        var server = RenderPaged(reads, Source.Asked);
        server.WaitForAssertion(() => Assert.HasCount(2, reads));

        await server.InvokeAsync(() => server.Instance.ToCsvAsync());
        await server.InvokeAsync(() => server.Instance.GetViewItemsAsync());
        Assert.HasCount(4, reads);
        Assert.IsNull(reads[2].Take, "an export reads every row");
        Assert.IsEmpty(reads[2].Aggregates);
        Assert.IsNull(reads[3].Take, "GetViewItemsAsync reads every row");
        Assert.IsEmpty(reads[3].Aggregates);

        var batches = new List<BitDataGridReadRequest>();
        var infinite = RenderComponent<BitDataGrid<Row>>(parameters =>
        {
            parameters.Add(p => p.Height, "300px");
            parameters.Add(p => p.OnLoadMore, (Func<BitDataGridReadRequest, Task<BitDataGridReadResult<Row>>>)(request =>
            {
                batches.Add(request);
                return Task.FromResult(new BitDataGridReadResult<Row>(request.Skip == 0 ? CreateRows() : [], 0));
            }));
            parameters.Add(p => p.ChildContent, Columns());
        });
        infinite.Render();
        Assert.IsNotEmpty(batches);
        foreach (var batch in batches) Assert.IsEmpty(batch.Aggregates);
    }

    [TestMethod]
    public void BitDataGridReadRequestShouldHaveNoAggregatesUnlessGiven()
    {
        Assert.IsEmpty(new BitDataGridReadRequest().Aggregates);
    }
}
