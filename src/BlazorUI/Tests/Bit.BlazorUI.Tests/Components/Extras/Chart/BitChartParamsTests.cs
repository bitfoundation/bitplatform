using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.Chart;

[TestClass]
public class BitChartParamsTests : BunitTestContext
{
    [TestMethod]
    public void BitChartShouldRespectCascadingParams()
    {
        var component = RenderComponent<BitChartCascadingParamsTest>();

        var charts = component.FindAll(".bit-cht");

        Assert.AreEqual(4, charts.Count);

        // The first chart takes everything from the cascading parameters.
        var first = charts[0];
        Assert.IsTrue(first.ClassList.Contains("cascaded"));
        Assert.IsTrue(first.ClassList.Contains("cascaded-root"));
        StringAssert.Contains(first.GetAttribute("style"), "height:200px");
        StringAssert.Contains(first.GetAttribute("style"), "width:50%");
        StringAssert.Contains(first.GetAttribute("style"), "--bit-Chart-grid-color: red");
        Assert.AreEqual("Shared title", first.QuerySelector("svg")!.GetAttribute("aria-label"));
        Assert.IsNull(first.QuerySelector("table"), "GenerateTable=false is cascaded");
        Assert.IsNotNull(first.QuerySelector(".cascaded-legend"));
        Assert.AreEqual("Cascaded legend", first.QuerySelector(".bit-cht-lgd")!.GetAttribute("aria-label"));
        Assert.AreEqual(2, first.QuerySelectorAll(".bit-cht-data rect").Length, "the cascaded Bar type draws bars");

        // The second one sets its own type, height, table and classes, which the cascade must not overwrite.
        var second = charts[1];
        Assert.IsTrue(second.ClassList.Contains("second"));
        Assert.IsTrue(second.ClassList.Contains("own-root"));
        Assert.IsFalse(second.ClassList.Contains("cascaded"));
        Assert.IsFalse(second.ClassList.Contains("cascaded-root"));
        StringAssert.Contains(second.GetAttribute("style"), "height:120px");
        Assert.AreEqual("Own legend", second.QuerySelector(".bit-cht-lgd")!.GetAttribute("aria-label"));
        Assert.IsNotNull(second.QuerySelector("table"));
        Assert.AreEqual(0, second.QuerySelectorAll(".bit-cht-data rect").Length, "its own Line type draws no bars");

        // The third one has no data, so the cascaded empty-state text shows.
        StringAssert.Contains(charts[2].QuerySelector(".bit-cht-nodata")!.TextContent, "Nothing yet");

        // The fourth one is loading, and says so with the cascaded label.
        StringAssert.Contains(charts[3].QuerySelector(".bit-cht-ldg")!.TextContent, "Cascaded loading");
    }
}
