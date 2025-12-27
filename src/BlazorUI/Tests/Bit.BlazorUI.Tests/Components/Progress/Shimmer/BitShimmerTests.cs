using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Progress.Shimmer;

[TestClass]
public class BitShimmerTests : BunitTestContext
{
    [TestMethod]
    public void BitShimmerShouldRenderRootElement()
    {
        var component = RenderComponent<BitShimmer>();

        var root = component.Find(".bit-smr");

        Assert.IsNotNull(root);
    }

    [TestMethod]
    public void BitShimmerShouldRespectCircleParameter()
    {
        var compLinear = RenderComponent<BitShimmer>();
        var compCircle = RenderComponent<BitShimmer>(parameters => parameters.Add(p => p.Circle, true));

        Assert.IsTrue(compLinear.Find(".bit-smr").ClassList.Contains("bit-smr-lin"));
        Assert.IsTrue(compCircle.Find(".bit-smr").ClassList.Contains("bit-smr-crl"));
    }

    [DataTestMethod]
    [DataRow("5rem", null)]
    [DataRow(null, "10rem")]
    [DataRow("3rem", "8rem")]
    public void BitShimmerShouldRespectSize(string height, string width)
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            if (height is not null) parameters.Add(p => p.Height, height);
            if (width is not null) parameters.Add(p => p.Width, width);
        });

        var root = component.Find(".bit-smr");
        var style = root.GetAttribute("style");

        if (height is not null)
        {
            Assert.IsTrue(style.Contains($"height:{height}"));
        }

        if (width is not null)
        {
            Assert.IsTrue(style.Contains($"width:{width}"));
        }
    }

    [DataTestMethod]
    [DataRow(BitColor.Primary, "bit-smr-pri")]
    [DataRow(BitColor.Secondary, "bit-smr-sec")]
    [DataRow(null, "bit-smr-tbg")]
    public void BitShimmerShouldRespectColor(BitColor? color, string expectedClass)
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            if (color.HasValue) parameters.Add(p => p.Color, color.Value);
        });

        var root = component.Find(".bit-smr");

        Assert.IsTrue(root.ClassList.Contains(expectedClass));
    }

    [TestMethod]
    public void BitShimmerShouldRespectBackground()
    {
        var component = RenderComponent<BitShimmer>(parameters => parameters.Add(p => p.Background, BitColor.Primary));

        // Background class is applied to the wrapper; just assert rendered markup contains the background class
        Assert.IsTrue(component.Markup.Contains("bit-smr-bpri"));
    }

    [TestMethod]
    public void BitShimmerShouldShowChildContentWhenLoaded()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Loaded, true);
            parameters.AddChildContent("Loaded content");
        });

        Assert.IsTrue(component.Markup.Contains("Loaded content"));
    }

    [TestMethod]
    public void BitShimmerShouldRespectClassesAndStyles()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Classes, new BitShimmerClassStyles { Root = "custom-root", Shimmer = "custom-shimmer", ShimmerWrapper = "custom-wrapper" });
            parameters.Add(p => p.Styles, new BitShimmerClassStyles { Root = "background:tomato;", ShimmerWrapper = "background-color: darkgoldenrod;" });
            parameters.AddChildContent("Content");
        });

        var markup = component.Markup;

        Assert.IsTrue(markup.Contains("custom-root"));
        Assert.IsTrue(markup.Contains("custom-shimmer"));
        Assert.IsTrue(markup.Contains("custom-wrapper"));
        Assert.IsTrue(markup.Contains("background:tomato"));
        Assert.IsTrue(markup.Contains("darkgoldenrod"));
    }

    [TestMethod]
    public void BitShimmerShouldRespectHtmlAttributes()
    {
        var component = RenderComponent<BitShimmerHtmlAttributesTest>();

        component.MarkupMatches(@"
<div data-val-test=""bit"" class=""bit-smr bit-smr-lin bit-smr-tbg"" >
    <div class=""bit-smr-wrp bit-smr-bsbg "">
        <div style="" "" class=""bit-smr-anm bit-smr-wav ""></div>
    </div>
</div>");
    }
}
