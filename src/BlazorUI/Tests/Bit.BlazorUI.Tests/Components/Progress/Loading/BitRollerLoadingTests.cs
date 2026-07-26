using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Progress.Loading;

[TestClass]
public class BitRollerLoadingTests : BunitTestContext
{
    [TestMethod]
    public void BitRollerLoadingShouldRenderStructure()
    {
        var component = RenderComponent<BitRollerLoading>();

        var root = component.Find(".bit-ldn");
        Assert.IsTrue(root.ClassList.Contains("bit-ldn-rol"));
        Assert.IsTrue(root.ClassList.Contains("bit-ldn-ltp"));

        var container = component.Find(".bit-ldn-rol-ccn");
        Assert.AreEqual(8, container.GetElementsByClassName("bit-ldn-rol-chl").Length);
    }

    [TestMethod]
    public void BitRollerLoadingShouldRenderLabel()
    {
        var component = RenderComponent<BitRollerLoading>(parameters =>
        {
            parameters.Add(p => p.Label, "Loading...");
        });

        var label = component.Find(".bit-ldn-lbl");
        Assert.AreEqual("Loading...", label.TextContent);
    }

    [TestMethod]
    public void BitRollerLoadingShouldRenderLabelTemplate()
    {
        var component = RenderComponent<BitRollerLoading>(parameters =>
        {
            parameters.Add(p => p.LabelTemplate, (RenderFragment)(b => b.AddMarkupContent(0, "<span class=\"tmpl\">tmpl</span>")));
        });

        var template = component.Find(".tmpl");
        Assert.AreEqual("tmpl", template.TextContent);
    }

    [TestMethod]
    public void BitRollerLoadingShouldRespectLabelPosition()
    {
        var component = RenderComponent<BitRollerLoading>(parameters =>
        {
            parameters.Add(p => p.LabelPosition, BitLabelPosition.End);
        });

        var root = component.Find(".bit-ldn");
        Assert.IsTrue(root.ClassList.Contains("bit-ldn-led"));
    }

    [TestMethod]
    public void BitRollerLoadingShouldHonorColorAndSize()
    {
        var component = RenderComponent<BitRollerLoading>(parameters =>
        {
            parameters.Add(p => p.Color, BitColor.Warning);
            parameters.Add(p => p.Size, BitSize.Small);
        });

        var style = component.Find(".bit-ldn").GetAttribute("style") ?? string.Empty;

        StringAssert.Contains(style, "--bit-ldn-color: var(--bit-clr-wrn)");
        StringAssert.Contains(style, "--bit-ldn-size:40px");
        StringAssert.Contains(style, "--bit-ldn-font-size:10px");
    }

    [TestMethod,
        DataRow(BitColor.Primary, "var(--bit-clr-pri)"),
        DataRow(BitColor.Secondary, "var(--bit-clr-sec)"),
        DataRow(BitColor.Tertiary, "var(--bit-clr-ter)"),
        DataRow(BitColor.Info, "var(--bit-clr-inf)"),
        DataRow(BitColor.Success, "var(--bit-clr-suc)"),
        DataRow(BitColor.Warning, "var(--bit-clr-wrn)"),
        DataRow(BitColor.SevereWarning, "var(--bit-clr-swr)"),
        DataRow(BitColor.Error, "var(--bit-clr-err)"),
        DataRow(BitColor.PrimaryBackground, "var(--bit-clr-bg-pri)"),
        DataRow(BitColor.SecondaryBackground, "var(--bit-clr-bg-sec)"),
        DataRow(BitColor.TertiaryBackground, "var(--bit-clr-bg-ter)"),
        DataRow(BitColor.PrimaryForeground, "var(--bit-clr-fg-pri)"),
        DataRow(BitColor.SecondaryForeground, "var(--bit-clr-fg-sec)"),
        DataRow(BitColor.TertiaryForeground, "var(--bit-clr-fg-ter)"),
        DataRow(BitColor.PrimaryBorder, "var(--bit-clr-brd-pri)"),
        DataRow(BitColor.SecondaryBorder, "var(--bit-clr-brd-sec)"),
        DataRow(BitColor.TertiaryBorder, "var(--bit-clr-brd-ter)"),
        DataRow(null, "var(--bit-clr-pri)")
        ]
    public void BitRollerLoadingColorTest(BitColor? color, string expectedColor)
    {
        var component = RenderComponent<BitRollerLoading>(parameters =>
        {
            parameters.Add(p => p.Color, color);
        });

        var style = component.Find(".bit-ldn").GetAttribute("style") ?? string.Empty;

        StringAssert.Contains(style, $"--bit-ldn-color: {expectedColor}");
    }

    [TestMethod]
    public void BitRollerLoadingShouldHonorCustomColorWhenColorIsNotSet()
    {
        var component = RenderComponent<BitRollerLoading>(parameters =>
        {
            parameters.Add(p => p.CustomColor, "hotpink");
        });

        var style = component.Find(".bit-ldn").GetAttribute("style") ?? string.Empty;

        StringAssert.Contains(style, "--bit-ldn-color: hotpink");
    }

    [TestMethod]
    public void BitRollerLoadingShouldRespectRootStyleAndClass()
    {
        var component = RenderComponent<BitRollerLoading>(parameters =>
        {
            parameters.Add(p => p.Class, "custom-root");
            parameters.Add(p => p.Style, "margin:4px;");
        });

        var root = component.Find(".bit-ldn");
        Assert.IsTrue(root.ClassList.Contains("custom-root"));
        StringAssert.Contains(root.GetAttribute("style") ?? string.Empty, "margin:4px");
    }
}
