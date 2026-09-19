using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.Chart;

[TestClass]
public class BitChartTests : BunitTestContext
{
    private static BitChartConfig CreateConfig(BitPlacement legendPosition) => new(
        BitChartType.Bar,
        new BitChartData
        {
            Labels = ["A", "B"],
            Datasets = [new BitChartDataset { Label = "Series", Data = [1, 2] }]
        },
        new BitChartOptions { Plugins = { Legend = { Placement = legendPosition } } });

    private IRenderedComponent<BitChart> RenderChart(BitPlacement legendPosition)
        => RenderComponent<BitChart>(parameters => parameters.Add(p => p.Config, CreateConfig(legendPosition)));

    [TestMethod]
    [DataRow(BitPlacement.Top, "bc-root")]
    [DataRow(BitPlacement.Bottom, "bc-root")]
    [DataRow(BitPlacement.Left, "bc-mid")]
    [DataRow(BitPlacement.Right, "bc-mid")]
    // A chart is laid out physically, so the logical and combined sides have no edge of their own
    // here - they leave the legend at the top instead of dropping it.
    [DataRow(BitPlacement.Start, "bc-root")]
    [DataRow(BitPlacement.End, "bc-root")]
    [DataRow(BitPlacement.TopAndBottom, "bc-root")]
    [DataRow(BitPlacement.StartAndEnd, "bc-root")]
    public void LegendPositionFallsBackToTheTop(BitPlacement position, string expectedParentClass)
    {
        var component = RenderChart(position);

        var legends = component.FindAll(".bc-legend");

        Assert.AreEqual(1, legends.Count);
        Assert.IsTrue(legends[0].ParentElement!.ClassList.Contains(expectedParentClass));
    }

    [TestMethod]
    public void LegendPositionDecidesTheLegendOrientation()
    {
        // Only the two physical inline sides stack the items vertically; every other side is a row.
        foreach (var position in new[] { BitPlacement.Left, BitPlacement.Right })
        {
            Assert.IsTrue(RenderChart(position).Find(".bc-legend").ClassList.Contains("bc-legend-v"));
        }

        foreach (var position in new[] { BitPlacement.Top, BitPlacement.Bottom, BitPlacement.Start, BitPlacement.End, BitPlacement.TopAndBottom, BitPlacement.StartAndEnd })
        {
            Assert.IsTrue(RenderChart(position).Find(".bc-legend").ClassList.Contains("bc-legend-h"));
        }
    }

    [TestMethod]
    [DataRow(BitPlacement.Start, "bc-align-start", "start")]
    [DataRow(BitPlacement.Left, "bc-align-left", "left")]
    [DataRow(BitPlacement.End, "bc-align-end", "end")]
    [DataRow(BitPlacement.Right, "bc-align-right", "right")]
    [DataRow(BitPlacement.Center, "bc-align-center", "center")]
    [DataRow(BitPlacement.Top, "bc-align-center", "center")]
    // Start and End follow the reading direction and Left and Right do not, so each is written as the class that
    // says so rather than the four collapsing onto two physical edges.
    public void TitleAndLegendAlignAlongTheirEdge(BitPlacement align, string alignClass, string textAlign)
    {
        var config = new BitChartConfig(
            BitChartType.Bar,
            new BitChartData
            {
                Labels = ["A", "B"],
                Datasets = [new BitChartDataset { Label = "Series", Data = [1, 2] }]
            },
            new BitChartOptions
            {
                Plugins =
                {
                    Title = { Display = true, Text = "Title", Align = align },
                    Legend = { Align = align }
                }
            });

        var component = RenderComponent<BitChart>(parameters => parameters.Add(p => p.Config, config));

        var title = component.Find(".bc-title");

        Assert.IsTrue(title.ClassList.Contains(alignClass));
        Assert.Contains($"text-align:{textAlign}", title.FirstElementChild!.GetAttribute("style")!);
        Assert.IsTrue(component.Find(".bc-legend").ClassList.Contains(alignClass));
    }

    // The physical justify-content keywords are the newer ones in a flex box, so each physical class names the
    // logical keyword for the same edge in a left-to-right layout first, for a browser that drops the physical one.
    [TestMethod]
    public void PhysicalAlignmentClassesCarryALogicalFallback()
    {
        var markup = RenderChart(BitPlacement.Top).Markup;

        Assert.Contains(".bc-align-left { justify-content: flex-start; justify-content: left; }", markup);
        Assert.Contains(".bc-align-right { justify-content: flex-end; justify-content: right; }", markup);
    }
}
