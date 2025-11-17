using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Splitter;

[TestClass]
public class BitSplitterTests : BunitTestContext
{
    [TestMethod]
    public void BitSplitterShouldRenderExpectedElement()
    {
        var component = RenderComponent<BitSplitter>();

        component.MarkupMatches(@"
<div class=""bit-spl"" id:ignore>
    <div class=""bit-spl-pnl bit-spl-fpn"">
    </div>
    <div class=""bit-spl-gtr"">
        <div class=""bit-spl-gti"">
        </div>
    </div>
    <div class=""bit-spl-pnl bit-spl-spn"">
    </div>
</div>");
    }

    [TestMethod]
    public void BitSplitterShouldRespectGutterSizeStyle()
    {
        var gutter = 20;
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.GutterSize, gutter);
        });

        var root = component.Find(".bit-spl");
        var style = root.GetAttribute("style");

        Assert.IsNotNull(style);
        Assert.IsTrue(style.Contains($"--gutter-size:{gutter}px"));
    }

    [TestMethod]
    public void BitSplitterShouldRenderGutterIconWhenProvided()
    {
        var iconName = "GripperDotsVertical";
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.GutterIcon, iconName);
        });

        var icon = component.Find(".bit-spl .bit-icon");

        Assert.IsTrue(icon.ClassList.Contains($"bit-icon--{iconName}"));
    }

    [TestMethod]
    public void BitSplitterShouldRespectVerticalClass()
    {
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.Vertical, true);
        });

        var root = component.Find(".bit-spl");
        Assert.IsTrue(root.ClassList.Contains("bit-spl-vrt"));
    }

    [TestMethod]
    public void BitSplitterShouldRenderPanelSizesAsCssVariables()
    {
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.FirstPanelSize, 128);
            parameters.Add(p => p.FirstPanelMaxSize, 256);
            parameters.Add(p => p.FirstPanelMinSize, 64);

            parameters.Add(p => p.SecondPanelSize, 200);
            parameters.Add(p => p.SecondPanelMaxSize, 300);
            parameters.Add(p => p.SecondPanelMinSize, 100);
        });

        var root = component.Find(".bit-spl");
        var style = root.GetAttribute("style");

        Assert.IsNotNull(style);
        Assert.IsTrue(style.Contains("--first-panel:128px"));
        Assert.IsTrue(style.Contains("--first-panel-max:256px"));
        Assert.IsTrue(style.Contains("--first-panel-min:64px"));

        Assert.IsTrue(style.Contains("--second-panel:200px"));
        Assert.IsTrue(style.Contains("--second-panel-max:300px"));
        Assert.IsTrue(style.Contains("--second-panel-min:100px"));
    }

    [TestMethod]
    public void BitSplitterShouldRenderChildContentInPanels()
    {
        RenderFragment first = builder => builder.AddContent(0, "First Panel Content");
        RenderFragment second = builder => builder.AddContent(0, "Second Panel Content");

        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.FirstPanel, first);
            parameters.Add(p => p.SecondPanel, second);
        });

        var firstPanel = component.Find(".bit-spl-fpn");
        var secondPanel = component.Find(".bit-spl-spn");

        Assert.IsTrue(firstPanel.TextContent.Contains("First Panel Content"));
        Assert.IsTrue(secondPanel.TextContent.Contains("Second Panel Content"));
    }
}
