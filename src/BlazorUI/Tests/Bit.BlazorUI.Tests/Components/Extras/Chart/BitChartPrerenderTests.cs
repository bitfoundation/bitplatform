using System.Collections.Generic;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.Chart;

/// <summary>
/// Covers what a BitChart sends before the page is interactive (a prerender, or a static SSR page): the plot is drawn
/// in C# into an SVG, so the whole chart - its drawing, its legend, its screen reader table and its hint - is in the
/// first response, with nothing waiting on the browser side (which only ever adds the resize and zoom listeners).
/// </summary>
[TestClass]
public class BitChartPrerenderTests
{
    private static BitChartData TwoSeries() => new()
    {
        Labels = { "Jan", "Feb", "Mar" },
        Datasets =
        {
            new BitChartDataset { Label = "Alpha", Data = { 1, 2, 3 } },
            new BitChartDataset { Label = "Beta", Data = { 3, 2, 1 } }
        }
    };

    private static async Task<IDocument> PrerenderAsync(Dictionary<string, object?> parameters)
    {
        var html = await Prerenderer.RenderAsync<BitChart>(parameters);

        return new HtmlParser().ParseDocument($"<!DOCTYPE html><html><body>{html}</body></html>");
    }

    [TestMethod]
    public async Task BitChartShouldPrerenderTheDrawingTheLegendAndTheTable()
    {
        var document = await PrerenderAsync(new Dictionary<string, object?>
        {
            [nameof(BitChart.Type)] = BitChartType.Bar,
            [nameof(BitChart.Data)] = TwoSeries(),
        });

        var root = document.QuerySelector(".bit-cht")!;

        Assert.IsNotNull(root.QuerySelector("svg"));
        Assert.AreEqual(6, root.QuerySelectorAll(".bit-cht-data rect").Length, "Three bars per series are drawn on the server.");
        Assert.AreEqual(2, root.QuerySelectorAll(".bit-cht-lgd-itm").Length);

        var table = root.QuerySelector("table")!;
        Assert.AreEqual(2, table.QuerySelectorAll("tbody tr").Length);
        Assert.Contains("Alpha", table.TextContent);
        Assert.Contains("Feb", table.TextContent);
    }

    [TestMethod]
    public async Task BitChartShouldPrerenderTheEmptyAndTheLoadingStates()
    {
        var empty = await PrerenderAsync(new Dictionary<string, object?>
        {
            [nameof(BitChart.NoDataText)] = "Nothing yet",
        });

        Assert.Contains("Nothing yet", empty.QuerySelector(".bit-cht-nodata")!.TextContent);

        var loading = await PrerenderAsync(new Dictionary<string, object?>
        {
            [nameof(BitChart.Data)] = TwoSeries(),
            [nameof(BitChart.IsLoading)] = true,
            [nameof(BitChart.LoadingLabel)] = "Fetching",
        });

        Assert.Contains("Fetching", loading.QuerySelector(".bit-cht-ldg")!.TextContent);
        Assert.IsNull(loading.QuerySelector(".bit-cht-nodata"), "Loading holds the empty state back.");
    }

    [TestMethod]
    public async Task BitChartShouldPrerenderItsAccessibleNameDescriptionAndHint()
    {
        var document = await PrerenderAsync(new Dictionary<string, object?>
        {
            [nameof(BitChart.Data)] = TwoSeries(),
            [nameof(BitChart.AriaLabel)] = "Quarterly revenue",
            [nameof(BitChart.Description)] = "Sales by month",
            [nameof(BitChart.NavigationHint)] = "Arrow keys walk the bars.",
        });

        var svg = document.QuerySelector("svg")!;

        Assert.AreEqual("Quarterly revenue", svg.GetAttribute("aria-label"));

        // The summary first, then how to walk the data - both visually hidden, both in the first response.
        var ids = svg.GetAttribute("aria-describedby")!.Split(' ');
        Assert.HasCount(2, ids);
        Assert.AreEqual("Sales by month", document.GetElementById(ids[0])!.TextContent);
        Assert.Contains("Arrow keys walk the bars.", document.GetElementById(ids[1])!.TextContent);
    }
}
