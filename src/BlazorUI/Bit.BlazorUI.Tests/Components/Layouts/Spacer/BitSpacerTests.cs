using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Layouts.Spacer;

[TestClass]
public class BitSpacerTests : BunitTestContext
{
    [TestMethod]
    public void BitSpacerShouldRenderRootElement()
    {
        var component = RenderComponent<BitSpacer>();

        var root = component.Find(".bit-spc");

        Assert.IsNotNull(root);
    }

    [TestMethod]
    public void BitSpacerShouldGrowWhenWidthIsNull()
    {
        var component = RenderComponent<BitSpacer>();

        var root = component.Find(".bit-spc");

        var style = root.GetAttribute("style");

        Assert.IsTrue(style.Contains("flex-grow:1"));
    }

    [DataTestMethod]
    [DataRow(0)]
    [DataRow(10)]
    [DataRow(64)]
    public void BitSpacerShouldRespectWidth(int width)
    {
        var component = RenderComponent<BitSpacer>(parameters =>
        {
            parameters.Add(p => p.Width, width);
        });

        var root = component.Find(".bit-spc");

        var style = root.GetAttribute("style");

        Assert.IsTrue(style.Contains($"margin-inline-start:{width}px"));
    }

    [TestMethod]
    public void BitSpacerShouldRespectHtmlAttributes()
    {
        var component = RenderComponent<BitSpacerHtmlAttributesTest>();

        component.MarkupMatches(@"<div data-val-test=""bit"" style=""flex-grow:1"" class=""bit-spc"" id:ignore></div>");
    }
}
