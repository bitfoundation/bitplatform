using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Layouts.Grid;

[TestClass]
public class BitGridTests : BunitTestContext
{
    [TestMethod]
    public void BitGridShouldRenderChildContent()
    {
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.AddChildContent("<div class='child'>Hello</div>");

        });

        var root = component.Find(".bit-grd");
        Assert.IsNotNull(root);

        var child = component.Find(".child");
        Assert.IsNotNull(child);
        Assert.AreEqual("Hello", child.TextContent);
    }

    [TestMethod]
    public void BitGridShouldSetCssVariablesForSpanAndColumns()
    {
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.Span, 2);
            parameters.Add(p => p.Columns, 8);
        });

        var root = component.Find(".bit-grd");
        var style = root.GetAttribute("style");

        Assert.IsTrue(style.Contains("--span:2"));
        Assert.IsTrue(style.Contains("--columns:8"));
    }

    [TestMethod]
    [DataRow(BitAlignment.Start, "flex-start")]
    [DataRow(BitAlignment.End, "flex-end")]
    [DataRow(BitAlignment.Center, "center")]
    [DataRow(BitAlignment.SpaceBetween, "space-between")]
    [DataRow(BitAlignment.SpaceAround, "space-around")]
    [DataRow(BitAlignment.SpaceEvenly, "space-evenly")]
    [DataRow(BitAlignment.Baseline, "baseline")]
    [DataRow(BitAlignment.Stretch, "stretch")]
    public void BitGridShouldSetJustifyContentBasedOnAlignment(BitAlignment alignment, string expected)
    {
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.HorizontalAlign, alignment);
        });

        var root = component.Find(".bit-grd");
        var style = root.GetAttribute("style");

        Assert.IsTrue(style.Contains($"justify-content:{expected}"));
    }

    [TestMethod]
    public void BitGridShouldRespectSpacingParameters()
    {
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.HorizontalSpacing, "10px");
            parameters.Add(p => p.VerticalSpacing, "20px");
        });

        var root = component.Find(".bit-grd");
        var style = root.GetAttribute("style");

        Assert.IsTrue(style.Contains("column-gap:10px") || style.Contains("--spacing:10px"));
        Assert.IsTrue(style.Contains("row-gap:20px"));
    }
}
