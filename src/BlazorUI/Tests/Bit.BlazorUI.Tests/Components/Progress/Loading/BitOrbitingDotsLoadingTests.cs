using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Progress.Loading;

[TestClass]
public class BitOrbitingDotsLoadingTests : BunitTestContext
{
    [TestMethod]
    public void BitOrbitingDotsLoadingShouldRenderStructure()
    {
        var component = RenderComponent<BitOrbitingDotsLoading>();

        var root = component.Find(".bit-ldn");
        Assert.IsTrue(root.ClassList.Contains("bit-ldn-ord"));
        Assert.IsTrue(root.ClassList.Contains("bit-ldn-ltp"));

        var container = component.Find(".bit-ldn-ord-ccn");
        Assert.AreEqual(2, container.GetElementsByClassName("bit-ldn-ord-chl").Length);
    }

    [TestMethod]
    public void BitOrbitingDotsLoadingShouldRenderLabel()
    {
        var component = RenderComponent<BitOrbitingDotsLoading>(parameters =>
        {
            parameters.Add(p => p.Label, "Loading...");
        });

        var label = component.Find(".bit-ldn-lbl");
        Assert.AreEqual("Loading...", label.TextContent);
    }

    [TestMethod]
    public void BitOrbitingDotsLoadingShouldRenderLabelTemplate()
    {
        var component = RenderComponent<BitOrbitingDotsLoading>(parameters =>
        {
            parameters.Add(p => p.LabelTemplate, (RenderFragment)(b => b.AddMarkupContent(0, "<span class=\"tmpl\">tmpl</span>")));
        });

        var template = component.Find(".tmpl");
        Assert.AreEqual("tmpl", template.TextContent);
    }

    [TestMethod]
    public void BitOrbitingDotsLoadingShouldRespectLabelPosition()
    {
        var component = RenderComponent<BitOrbitingDotsLoading>(parameters =>
        {
            parameters.Add(p => p.LabelPosition, BitLabelPosition.End);
        });

        var root = component.Find(".bit-ldn");
        Assert.IsTrue(root.ClassList.Contains("bit-ldn-led"));
    }

    [TestMethod]
    public void BitOrbitingDotsLoadingShouldHonorColorAndSize()
    {
        var component = RenderComponent<BitOrbitingDotsLoading>(parameters =>
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
    public void BitOrbitingDotsLoadingShouldRespectRootStyleAndClass()
    {
        var component = RenderComponent<BitOrbitingDotsLoading>(parameters =>
        {
            parameters.Add(p => p.Class, "custom-root");
            parameters.Add(p => p.Style, "margin:4px;");
        });

        var root = component.Find(".bit-ldn");
        Assert.IsTrue(root.ClassList.Contains("custom-root"));
        StringAssert.Contains(root.GetAttribute("style") ?? string.Empty, "margin:4px");
    }
}
