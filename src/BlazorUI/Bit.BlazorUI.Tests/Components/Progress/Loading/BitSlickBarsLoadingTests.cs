using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Progress.Loading;

[TestClass]
public class BitSlickBarsLoadingTests : BunitTestContext
{
    [TestMethod]
    public void BitSlickBarsLoadingShouldRenderStructure()
    {
        var component = RenderComponent<BitSlickBarsLoading>();

        var root = component.Find(".bit-ldn");
        Assert.IsTrue(root.ClassList.Contains("bit-ldn-sbr"));
        Assert.IsTrue(root.ClassList.Contains("bit-ldn-ltp"));

        var container = component.Find(".bit-ldn-sbr-ccn");
        Assert.AreEqual(6, container.GetElementsByClassName("bit-ldn-sbr-chl").Length);
    }

    [TestMethod]
    public void BitSlickBarsLoadingShouldRenderLabel()
    {
        var component = RenderComponent<BitSlickBarsLoading>(parameters =>
        {
            parameters.Add(p => p.Label, "Loading...");
        });

        var label = component.Find(".bit-ldn-lbl");
        Assert.AreEqual("Loading...", label.TextContent);
    }

    [TestMethod]
    public void BitSlickBarsLoadingShouldRenderLabelTemplate()
    {
        var component = RenderComponent<BitSlickBarsLoading>(parameters =>
        {
            parameters.Add(p => p.LabelTemplate, (RenderFragment)(b => b.AddMarkupContent(0, "<span class=\"tmpl\">tmpl</span>")));
        });

        var template = component.Find(".tmpl");
        Assert.AreEqual("tmpl", template.TextContent);
    }

    [TestMethod]
    public void BitSlickBarsLoadingShouldRespectLabelPosition()
    {
        var component = RenderComponent<BitSlickBarsLoading>(parameters =>
        {
            parameters.Add(p => p.LabelPosition, BitLabelPosition.End);
        });

        var root = component.Find(".bit-ldn");
        Assert.IsTrue(root.ClassList.Contains("bit-ldn-led"));
    }

    [TestMethod]
    public void BitSlickBarsLoadingShouldHonorColorAndSize()
    {
        var component = RenderComponent<BitSlickBarsLoading>(parameters =>
        {
            parameters.Add(p => p.Color, BitColor.Warning);
            parameters.Add(p => p.Size, BitSize.Small);
        });

        var style = component.Find(".bit-ldn").GetAttribute("style") ?? string.Empty;

        StringAssert.Contains(style, "--bit-ldn-color: var(--bit-clr-wrn)");
        StringAssert.Contains(style, "--bit-ldn-size:40px");
        StringAssert.Contains(style, "--bit-ldn-font-size:10px");
    }

    [TestMethod]
    public void BitSlickBarsLoadingShouldRespectRootStyleAndClass()
    {
        var component = RenderComponent<BitSlickBarsLoading>(parameters =>
        {
            parameters.Add(p => p.Class, "custom-root");
            parameters.Add(p => p.Style, "margin:4px;");
        });

        var root = component.Find(".bit-ldn");
        Assert.IsTrue(root.ClassList.Contains("custom-root"));
        StringAssert.Contains(root.GetAttribute("style") ?? string.Empty, "margin:4px");
    }
}
