using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Progress.Loading;

[TestClass]
public class BitHourglassLoadingTests : BunitTestContext
{
    [TestMethod]
    public void BitHourglassLoadingShouldRenderStructure()
    {
        var component = RenderComponent<BitHourglassLoading>();

        var root = component.Find(".bit-ldn");
        Assert.IsTrue(root.ClassList.Contains("bit-ldn-hgl"));
        Assert.IsTrue(root.ClassList.Contains("bit-ldn-ltp"));

        var container = component.Find(".bit-ldn-hgl-ccn");
        Assert.IsNotNull(container);
    }

    [TestMethod]
    public void BitHourglassLoadingShouldRenderLabel()
    {
        var component = RenderComponent<BitHourglassLoading>(parameters =>
        {
            parameters.Add(p => p.Label, "Loading...");
        });

        var label = component.Find(".bit-ldn-lbl");
        Assert.AreEqual("Loading...", label.TextContent);
    }

    [TestMethod]
    public void BitHourglassLoadingShouldRenderLabelTemplate()
    {
        var component = RenderComponent<BitHourglassLoading>(parameters =>
        {
            parameters.Add(p => p.LabelTemplate, (RenderFragment)(b => b.AddMarkupContent(0, "<span class=\"tmpl\">tmpl</span>")));
        });

        var template = component.Find(".tmpl");
        Assert.AreEqual("tmpl", template.TextContent);
    }

    [TestMethod]
    public void BitHourglassLoadingShouldRespectLabelPosition()
    {
        var component = RenderComponent<BitHourglassLoading>(parameters =>
        {
            parameters.Add(p => p.LabelPosition, BitLabelPosition.End);
        });

        var root = component.Find(".bit-ldn");
        Assert.IsTrue(root.ClassList.Contains("bit-ldn-led"));
    }

    [TestMethod]
    public void BitHourglassLoadingShouldHonorColorAndSize()
    {
        var component = RenderComponent<BitHourglassLoading>(parameters =>
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
    public void BitHourglassLoadingShouldRespectRootStyleAndClass()
    {
        var component = RenderComponent<BitHourglassLoading>(parameters =>
        {
            parameters.Add(p => p.Class, "custom-root");
            parameters.Add(p => p.Style, "margin:4px;");
        });

        var root = component.Find(".bit-ldn");
        Assert.IsTrue(root.ClassList.Contains("custom-root"));
        StringAssert.Contains(root.GetAttribute("style") ?? string.Empty, "margin:4px");
    }
}
