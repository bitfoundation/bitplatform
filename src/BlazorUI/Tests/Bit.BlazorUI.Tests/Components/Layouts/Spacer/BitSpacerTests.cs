using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Layouts.Spacer;

[TestClass]
public class BitSpacerTests : BunitTestContext
{
    [TestMethod]
    public void BitSpacerShouldRenderExpectedElement()
    {
        var component = RenderComponent<BitSpacer>();

        component.MarkupMatches(@"<div aria-hidden=""true"" style=""flex-grow:1"" class=""bit-spc"" id:ignore></div>");
    }

    [TestMethod]
    public void BitSpacerShouldRenderRootElement()
    {
        var component = RenderComponent<BitSpacer>();

        var root = component.Find(".bit-spc");

        Assert.IsNotNull(root);
    }

    [TestMethod]
    public void BitSpacerShouldRenderADivByDefault()
    {
        var component = RenderComponent<BitSpacer>();

        Assert.AreEqual("DIV", component.Find(".bit-spc").TagName);
    }

    [TestMethod]
    [DataRow("li")]
    [DataRow("span")]
    public void BitSpacerShouldRespectElement(string element)
    {
        var component = RenderComponent<BitSpacer>(parameters =>
        {
            parameters.Add(p => p.Element, element);
            parameters.Add(p => p.Gap, "1rem");
        });

        var root = component.Find(".bit-spc");

        // Only the tag changes: everything the spacer generates has to survive the swap.
        Assert.AreEqual(element.ToUpperInvariant(), root.TagName);
        Assert.AreEqual("true", root.GetAttribute("aria-hidden"));
        StringAssert.Contains(root.GetAttribute("style"), "margin-inline-start:1rem");
    }

    [TestMethod]
    public void BitSpacerShouldGrowWhenNoFixedSizeIsSet()
    {
        var component = RenderComponent<BitSpacer>();

        var root = component.Find(".bit-spc");

        var style = root.GetAttribute("style");

        Assert.IsTrue(style.Contains("flex-grow:1"));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(10)]
    [DataRow(64)]
    [DataRow(-8)]
    public void BitSpacerShouldRespectWidth(int width)
    {
        var component = RenderComponent<BitSpacer>(parameters =>
        {
            parameters.Add(p => p.Width, width);
        });

        var style = component.Find(".bit-spc").GetAttribute("style");

        StringAssert.Contains(style, $"margin-inline-start:{width}px");

        // A fixed spacer must not also absorb the leftover space of its container.
        Assert.IsFalse(style.Contains("flex-grow"));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(8)]
    [DataRow(32)]
    public void BitSpacerShouldRespectHeight(int height)
    {
        var component = RenderComponent<BitSpacer>(parameters =>
        {
            parameters.Add(p => p.Height, height);
        });

        var style = component.Find(".bit-spc").GetAttribute("style");

        StringAssert.Contains(style, $"margin-block-start:{height}px");
        Assert.IsFalse(style.Contains("margin-inline-start"));
        Assert.IsFalse(style.Contains("flex-grow"));
    }

    [TestMethod]
    public void BitSpacerShouldRespectWidthAndHeightTogether()
    {
        var component = RenderComponent<BitSpacer>(parameters =>
        {
            parameters.Add(p => p.Width, 16);
            parameters.Add(p => p.Height, 24);
        });

        var style = component.Find(".bit-spc").GetAttribute("style");

        StringAssert.Contains(style, "margin-inline-start:16px");
        StringAssert.Contains(style, "margin-block-start:24px");
        Assert.IsFalse(style.Contains("flex-grow"));
    }

    [TestMethod]
    [DataRow("1rem")]
    [DataRow("5%")]
    [DataRow("var(--my-gap)")]
    public void BitSpacerShouldRespectGap(string gap)
    {
        var component = RenderComponent<BitSpacer>(parameters =>
        {
            parameters.Add(p => p.Gap, gap);
        });

        var style = component.Find(".bit-spc").GetAttribute("style");

        StringAssert.Contains(style, $"margin-inline-start:{gap}");
        Assert.IsFalse(style.Contains("margin-block-start"));
        Assert.IsFalse(style.Contains("flex-grow"));
    }

    [TestMethod]
    public void BitSpacerShouldRespectVerticalGap()
    {
        var component = RenderComponent<BitSpacer>(parameters =>
        {
            parameters.Add(p => p.Vertical, true);
            parameters.Add(p => p.Gap, "3rem");
        });

        var style = component.Find(".bit-spc").GetAttribute("style");

        StringAssert.Contains(style, "margin-block-start:3rem");
        Assert.IsFalse(style.Contains("margin-inline-start"));
    }

    [TestMethod]
    public void BitSpacerGapShouldOverrideWidth()
    {
        var component = RenderComponent<BitSpacer>(parameters =>
        {
            parameters.Add(p => p.Width, 64);
            parameters.Add(p => p.Gap, "2rem");
        });

        var style = component.Find(".bit-spc").GetAttribute("style");

        StringAssert.Contains(style, "margin-inline-start:2rem");
        Assert.IsFalse(style.Contains("64px"));
    }

    [TestMethod]
    public void BitSpacerVerticalGapShouldOverrideHeightAndLeaveWidthAlone()
    {
        var component = RenderComponent<BitSpacer>(parameters =>
        {
            parameters.Add(p => p.Vertical, true);
            parameters.Add(p => p.Gap, "2rem");
            parameters.Add(p => p.Width, 16);
            parameters.Add(p => p.Height, 64);
        });

        var style = component.Find(".bit-spc").GetAttribute("style");

        StringAssert.Contains(style, "margin-block-start:2rem");
        StringAssert.Contains(style, "margin-inline-start:16px");
        Assert.IsFalse(style.Contains("64px"));
    }

    [TestMethod]
    [DataRow("2")]
    [DataRow("0.5")]
    public void BitSpacerShouldRespectGrow(string grow)
    {
        var component = RenderComponent<BitSpacer>(parameters =>
        {
            parameters.Add(p => p.Grow, grow);
        });

        var style = component.Find(".bit-spc").GetAttribute("style");

        StringAssert.Contains(style, $"flex-grow:{grow}");
    }

    [TestMethod]
    public void BitSpacerShouldIgnoreGrowWhenAFixedSizeIsSet()
    {
        var component = RenderComponent<BitSpacer>(parameters =>
        {
            parameters.Add(p => p.Grow, "3");
            parameters.Add(p => p.Gap, "1rem");
        });

        var style = component.Find(".bit-spc").GetAttribute("style");

        Assert.IsFalse(style.Contains("flex-grow"));
    }

    [TestMethod]
    [DataRow(BitSize.Small, "bit-spc-sm")]
    [DataRow(BitSize.Medium, "bit-spc-md")]
    [DataRow(BitSize.Large, "bit-spc-lg")]
    public void BitSpacerShouldRespectSize(BitSize size, string expectedClass)
    {
        var component = RenderComponent<BitSpacer>(parameters =>
        {
            parameters.Add(p => p.Size, size);
        });

        var root = component.Find(".bit-spc");

        // The length itself comes from the spacing scale of the theme through the class of the size.
        Assert.IsTrue(root.ClassList.Contains(expectedClass));
        StringAssert.Contains(root.GetAttribute("style"), "margin-inline-start:var(--bit-spc-size)");
        Assert.IsFalse(root.GetAttribute("style")!.Contains("flex-grow"));
    }

    [TestMethod]
    public void BitSpacerShouldRespectVerticalSize()
    {
        var component = RenderComponent<BitSpacer>(parameters =>
        {
            parameters.Add(p => p.Vertical, true);
            parameters.Add(p => p.Size, BitSize.Large);
        });

        var root = component.Find(".bit-spc");

        Assert.IsTrue(root.ClassList.Contains("bit-spc-lg"));
        StringAssert.Contains(root.GetAttribute("style"), "margin-block-start:var(--bit-spc-size)");
        Assert.IsFalse(root.GetAttribute("style")!.Contains("margin-inline-start"));
    }

    [TestMethod]
    public void BitSpacerShouldRenderNoSizeClassWhenSizeIsNotSet()
    {
        var component = RenderComponent<BitSpacer>();

        var cssClass = component.Find(".bit-spc").GetAttribute("class");

        Assert.AreEqual("bit-spc", cssClass);
    }

    [TestMethod]
    public void BitSpacerGapAndWidthShouldOverrideSize()
    {
        var component = RenderComponent<BitSpacer>(parameters =>
        {
            parameters.Add(p => p.Size, BitSize.Medium);
            parameters.Add(p => p.Gap, "2rem");
        });

        // The class still carries the token, but the more specific Gap is what the margin resolves to.
        StringAssert.Contains(component.Find(".bit-spc").GetAttribute("style"), "margin-inline-start:2rem");
        Assert.IsFalse(component.Find(".bit-spc").GetAttribute("style")!.Contains("--bit-spc-size"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Size, BitSize.Medium);
            parameters.Add(p => p.Gap, null);
            parameters.Add(p => p.Width, 24);
        });

        StringAssert.Contains(component.Find(".bit-spc").GetAttribute("style"), "margin-inline-start:24px");
        Assert.IsFalse(component.Find(".bit-spc").GetAttribute("style")!.Contains("--bit-spc-size"));
    }

    [TestMethod]
    public void BitSpacerVerticalSizeShouldLeaveTheInlineAxisAlone()
    {
        var component = RenderComponent<BitSpacer>(parameters =>
        {
            parameters.Add(p => p.Vertical, true);
            parameters.Add(p => p.Size, BitSize.Small);
            parameters.Add(p => p.Width, 16);
        });

        var style = component.Find(".bit-spc").GetAttribute("style");

        // Width owns the inline axis whichever way the spacer faces, while a vertical Size targets the block one.
        StringAssert.Contains(style, "margin-inline-start:16px");
        StringAssert.Contains(style, "margin-block-start:var(--bit-spc-size)");
    }

    [TestMethod]
    public void BitSpacerShouldIgnoreGrowWhenSizeIsSet()
    {
        var component = RenderComponent<BitSpacer>(parameters =>
        {
            parameters.Add(p => p.Grow, "3");
            parameters.Add(p => p.Size, BitSize.Small);
        });

        Assert.IsFalse(component.Find(".bit-spc").GetAttribute("style")!.Contains("flex-grow"));
    }

    [TestMethod]
    public void BitSpacerShouldRespectMinGap()
    {
        var component = RenderComponent<BitSpacer>(parameters =>
        {
            parameters.Add(p => p.MinGap, "2rem");
        });

        var style = component.Find(".bit-spc").GetAttribute("style");

        StringAssert.Contains(style, "min-inline-size:2rem");
        StringAssert.Contains(style, "flex-grow:1");
    }

    [TestMethod]
    public void BitSpacerShouldRespectVerticalMinGap()
    {
        var component = RenderComponent<BitSpacer>(parameters =>
        {
            parameters.Add(p => p.Vertical, true);
            parameters.Add(p => p.MinGap, "2rem");
        });

        var style = component.Find(".bit-spc").GetAttribute("style");

        StringAssert.Contains(style, "min-block-size:2rem");
        Assert.IsFalse(style.Contains("min-inline-size"));
    }

    [TestMethod]
    [DataRow("1rem", null, null)]
    [DataRow(null, 32, null)]
    [DataRow(null, null, BitSize.Large)]
    public void BitSpacerShouldIgnoreMinGapWhenAFixedSizeIsSet(string? gap, int? width, BitSize? size)
    {
        var component = RenderComponent<BitSpacer>(parameters =>
        {
            parameters.Add(p => p.MinGap, "2rem");
            parameters.Add(p => p.Gap, gap);
            parameters.Add(p => p.Width, width);
            parameters.Add(p => p.Size, size);
        });

        // A fixed spacer can never collapse, so the floor of a flexible one has nothing to do on it.
        Assert.IsFalse(component.Find(".bit-spc").GetAttribute("style")!.Contains("min-inline-size"));
    }

    [TestMethod]
    public void BitSpacerShouldUpdateStylesWhenParametersChange()
    {
        var component = RenderComponent<BitSpacer>();

        StringAssert.Contains(component.Find(".bit-spc").GetAttribute("style"), "flex-grow:1");

        component.Render(parameters => parameters.Add(p => p.Width, 32));

        StringAssert.Contains(component.Find(".bit-spc").GetAttribute("style"), "margin-inline-start:32px");

        component.Render(parameters => parameters.Add(p => p.Width, null));

        StringAssert.Contains(component.Find(".bit-spc").GetAttribute("style"), "flex-grow:1");
    }

    [TestMethod]
    public void BitSpacerShouldRespectStyleAndClass()
    {
        var component = RenderComponent<BitSpacer>(parameters =>
        {
            parameters.Add(p => p.Style, "background:tomato");
            parameters.Add(p => p.Class, "custom-class");
        });

        var root = component.Find(".bit-spc");

        Assert.IsTrue(root.ClassList.Contains("custom-class"));

        // The custom style is appended after the generated one so it wins over it.
        Assert.AreEqual("flex-grow:1;background:tomato", root.GetAttribute("style"));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitSpacerShouldRespectIsEnabled(bool isEnabled)
    {
        var component = RenderComponent<BitSpacer>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        Assert.AreEqual(isEnabled is false, component.Find(".bit-spc").ClassList.Contains("bit-dis"));
    }

    [TestMethod]
    [DataRow(BitVisibility.Visible, null)]
    [DataRow(BitVisibility.Hidden, "visibility:hidden")]
    [DataRow(BitVisibility.Collapsed, "display:none")]
    public void BitSpacerShouldRespectVisibility(BitVisibility visibility, string expectedStyle)
    {
        var component = RenderComponent<BitSpacer>(parameters =>
        {
            parameters.Add(p => p.Visibility, visibility);
        });

        var style = component.Find(".bit-spc").GetAttribute("style");

        if (expectedStyle is null)
        {
            Assert.AreEqual("flex-grow:1", style);
        }
        else
        {
            StringAssert.Contains(style, expectedStyle);
        }
    }

    [TestMethod]
    [DataRow(BitDir.Rtl)]
    [DataRow(BitDir.Ltr)]
    [DataRow(BitDir.Auto)]
    [DataRow(null)]
    public void BitSpacerShouldRespectDir(BitDir? dir)
    {
        var component = RenderComponent<BitSpacer>(parameters =>
        {
            parameters.Add(p => p.Dir, dir);
        });

        if (dir.HasValue)
        {
            var cssClass = dir is BitDir.Rtl ? " bit-rtl" : string.Empty;
            component.MarkupMatches($@"<div aria-hidden=""true"" dir=""{dir.Value.ToString().ToLower()}"" style=""flex-grow:1"" class=""bit-spc{cssClass}"" id:ignore></div>");
        }
        else
        {
            component.MarkupMatches(@"<div aria-hidden=""true"" style=""flex-grow:1"" class=""bit-spc"" id:ignore></div>");
        }
    }

    [TestMethod]
    public void BitSpacerShouldBeHiddenFromAssistiveTechnologiesByDefault()
    {
        var component = RenderComponent<BitSpacer>();

        var root = component.Find(".bit-spc");

        Assert.AreEqual("true", root.GetAttribute("aria-hidden"));
        Assert.IsFalse(root.HasAttribute("aria-label"));
    }

    [TestMethod]
    public void BitSpacerShouldRespectAriaLabel()
    {
        var component = RenderComponent<BitSpacer>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, "spacer");
        });

        var root = component.Find(".bit-spc");

        // A labelled spacer is meant to be announced, so the default aria-hidden steps aside.
        Assert.AreEqual("spacer", root.GetAttribute("aria-label"));
        Assert.IsFalse(root.HasAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitSpacerShouldLetHtmlAttributesOverrideAriaHidden()
    {
        var component = RenderComponent<BitSpacerAriaHiddenOverrideTest>();

        Assert.AreEqual("false", component.Find(".bit-spc").GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitSpacerShouldRespectId()
    {
        var component = RenderComponent<BitSpacer>(parameters =>
        {
            parameters.Add(p => p.Id, "spacer-id");
        });

        Assert.AreEqual("spacer-id", component.Find(".bit-spc").GetAttribute("id"));
    }

    [TestMethod]
    public void BitSpacerShouldRespectHtmlAttributes()
    {
        var component = RenderComponent<BitSpacerHtmlAttributesTest>();

        component.MarkupMatches(@"<div aria-hidden=""true"" data-val-test=""bit"" style=""flex-grow:1"" class=""bit-spc"" id:ignore></div>");
    }

    [TestMethod]
    public void BitSpacerShouldRespectCascadingParams()
    {
        var component = RenderComponent<BitSpacerCascadingParamsTest>();

        var spacers = component.FindAll(".bit-spc");

        // The first spacer takes everything from the cascading parameters, the cascaded Size included,
        // even though the cascaded Gap is the more specific of the two and wins the margin.
        Assert.IsTrue(spacers[0].ClassList.Contains("cascaded"));
        Assert.IsTrue(spacers[0].ClassList.Contains("bit-spc-lg"));
        StringAssert.Contains(spacers[0].GetAttribute("style")!, "margin-block-start:3rem");

        // The second one sets its own gap, which the cascading parameters must not overwrite.
        Assert.IsTrue(spacers[1].ClassList.Contains("cascaded"));
        StringAssert.Contains(spacers[1].GetAttribute("style")!, "margin-block-start:5rem");
        Assert.IsFalse(spacers[1].GetAttribute("style")!.Contains("3rem"));

        // The third one is flexible, so it takes the cascaded Grow and MinGap on the cascaded axis.
        Assert.IsTrue(spacers[2].ClassList.Contains("cascaded"));
        Assert.AreEqual("SPAN", spacers[2].TagName);
        StringAssert.Contains(spacers[2].GetAttribute("style")!, "flex-grow:2");
        StringAssert.Contains(spacers[2].GetAttribute("style")!, "min-block-size:1rem");
    }
}
