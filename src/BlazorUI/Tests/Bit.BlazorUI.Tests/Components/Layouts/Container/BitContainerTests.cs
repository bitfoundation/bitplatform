using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Layouts.Container;

[TestClass]
public class BitContainerTests : BunitTestContext
{
    [TestMethod]
    public void BitContainerShouldRenderExpectedElement()
    {
        var component = RenderComponent<BitContainer>(parameters =>
        {
            parameters.AddChildContent("<p>Content</p>");
        });

        component.MarkupMatches(@"<div class=""bit-cnt"" id:ignore><p>Content</p></div>");
    }

    [TestMethod]
    public void BitContainerShouldRenderADivByDefault()
    {
        var component = RenderComponent<BitContainer>();

        Assert.AreEqual("DIV", component.Find(".bit-cnt").TagName);
    }

    [TestMethod]
    [DataRow("main")]
    [DataRow("section")]
    [DataRow("article")]
    public void BitContainerShouldRespectElement(string element)
    {
        var component = RenderComponent<BitContainer>(parameters =>
        {
            parameters.Add(p => p.Element, element);
            parameters.Add(p => p.MaxWidth, "40rem");
            parameters.AddChildContent("Content");
        });

        var root = component.Find(".bit-cnt");

        // Only the tag changes: the content and everything the container publishes have to survive the swap.
        Assert.AreEqual(element.ToUpperInvariant(), root.TagName);
        Assert.AreEqual("--bit-cnt-max-width:40rem", root.GetAttribute("style"));
        Assert.AreEqual("Content", root.TextContent);
    }

    [TestMethod]
    [DataRow("1200px")]
    [DataRow("75rem")]
    [DataRow("none")]
    public void BitContainerShouldPublishMaxWidthAsItsPrivateVariable(string maxWidth)
    {
        var component = RenderComponent<BitContainer>(parameters =>
        {
            parameters.Add(p => p.MaxWidth, maxWidth);
        });

        // The stylesheet reads the private variable ahead of --bit-Container-max-width, which is how the parameter wins.
        Assert.AreEqual($"--bit-cnt-max-width:{maxWidth}", component.Find(".bit-cnt").GetAttribute("style"));
    }

    [TestMethod]
    public void BitContainerShouldPublishNothingWhileMaxWidthIsNotSet()
    {
        var component = RenderComponent<BitContainer>();

        // An unset MaxWidth leaves the default to --bit-Container-max-width and the stylesheet's fallback.
        Assert.IsFalse(component.Find(".bit-cnt").HasAttribute("style"));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitContainerShouldRespectNoGutter(bool noGutter)
    {
        var component = RenderComponent<BitContainer>(parameters =>
        {
            parameters.Add(p => p.NoGutter, noGutter);
        });

        Assert.AreEqual(noGutter, component.Find(".bit-cnt").ClassList.Contains("bit-cnt-ngt"));
    }

    [TestMethod]
    public void BitContainerShouldUpdateWhenParametersChange()
    {
        var component = RenderComponent<BitContainer>();

        var root = component.Find(".bit-cnt");
        Assert.IsFalse(root.HasAttribute("style"));
        Assert.IsFalse(root.ClassList.Contains("bit-cnt-ngt"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.MaxWidth, "30rem");
            parameters.Add(p => p.NoGutter, true);
        });

        root = component.Find(".bit-cnt");
        Assert.AreEqual("--bit-cnt-max-width:30rem", root.GetAttribute("style"));
        Assert.IsTrue(root.ClassList.Contains("bit-cnt-ngt"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.MaxWidth, null);
            parameters.Add(p => p.NoGutter, false);
        });

        root = component.Find(".bit-cnt");
        Assert.IsFalse(root.HasAttribute("style"));
        Assert.IsFalse(root.ClassList.Contains("bit-cnt-ngt"));
    }

    [TestMethod]
    public void BitContainerShouldRespectStyleAndClass()
    {
        var component = RenderComponent<BitContainer>(parameters =>
        {
            parameters.Add(p => p.MaxWidth, "40rem");
            parameters.Add(p => p.Style, "--bit-Container-padding-block:1rem");
            parameters.Add(p => p.Class, "custom-class");
        });

        var root = component.Find(".bit-cnt");

        Assert.IsTrue(root.ClassList.Contains("custom-class"));

        // The custom style is appended after the generated one so it wins over it.
        Assert.AreEqual("--bit-cnt-max-width:40rem;--bit-Container-padding-block:1rem", root.GetAttribute("style"));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitContainerShouldRespectDisabled(bool isEnabled)
    {
        var component = RenderComponent<BitContainer>(parameters =>
        {
            parameters.Add(p => p.Disabled, isEnabled is false);
        });

        Assert.AreEqual(isEnabled is false, component.Find(".bit-cnt").ClassList.Contains("bit-dis"));
    }

    [TestMethod]
    [DataRow(BitVisibility.Visible, null)]
    [DataRow(BitVisibility.Hidden, "visibility:hidden")]
    [DataRow(BitVisibility.Collapsed, "display:none")]
    public void BitContainerShouldRespectVisibility(BitVisibility visibility, string? expectedStyle)
    {
        var component = RenderComponent<BitContainer>(parameters =>
        {
            parameters.Add(p => p.Visibility, visibility);
        });

        var style = component.Find(".bit-cnt").GetAttribute("style");

        if (expectedStyle is null)
        {
            Assert.IsNull(style);
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
    public void BitContainerShouldRespectDir(BitDir? dir)
    {
        var component = RenderComponent<BitContainer>(parameters =>
        {
            parameters.Add(p => p.Dir, dir);
        });

        if (dir.HasValue)
        {
            var cssClass = dir is BitDir.Rtl ? " bit-rtl" : string.Empty;
            component.MarkupMatches($@"<div dir=""{dir.Value.ToString().ToLower()}"" class=""bit-cnt{cssClass}"" id:ignore></div>");
        }
        else
        {
            component.MarkupMatches(@"<div class=""bit-cnt"" id:ignore></div>");
        }
    }

    [TestMethod]
    public void BitContainerShouldRespectAriaLabelAndTabIndex()
    {
        var component = RenderComponent<BitContainer>(parameters =>
        {
            parameters.Add(p => p.Element, "section");
            parameters.Add(p => p.AriaLabel, "Latest news");
            parameters.Add(p => p.TabIndex, "0");
        });

        var root = component.Find(".bit-cnt");

        Assert.AreEqual("Latest news", root.GetAttribute("aria-label"));
        Assert.AreEqual("0", root.GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitContainerShouldKeepASplattedAriaLabelAndTabIndex()
    {
        // An unset AriaLabel or TabIndex must not wipe out the attribute of the same name splatted on the container.
        var component = RenderComponent<BitContainerSplattedAriaLabelTest>();

        var root = component.Find(".bit-cnt");

        Assert.AreEqual("Splatted", root.GetAttribute("aria-label"));
        Assert.AreEqual("0", root.GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitContainerShouldRespectId()
    {
        var component = RenderComponent<BitContainer>(parameters =>
        {
            parameters.Add(p => p.Id, "container-id");
        });

        Assert.AreEqual("container-id", component.Find(".bit-cnt").GetAttribute("id"));
    }

    [TestMethod]
    public void BitContainerShouldRespectHtmlAttributes()
    {
        var component = RenderComponent<BitContainerHtmlAttributesTest>();

        component.MarkupMatches(@"<div data-val-test=""bit"" class=""bit-cnt"" id:ignore>Content</div>");
    }

    [TestMethod]
    public void BitContainerShouldRespectCascadingParams()
    {
        var component = RenderComponent<BitContainerCascadingParamsTest>();

        var containers = component.FindAll(".bit-cnt");

        // The first container takes everything from the cascading parameters.
        Assert.AreEqual("SECTION", containers[0].TagName);
        Assert.IsTrue(containers[0].ClassList.Contains("cascaded"));
        Assert.IsTrue(containers[0].ClassList.Contains("bit-cnt-ngt"));
        Assert.AreEqual("--bit-cnt-max-width:40rem", containers[0].GetAttribute("style"));

        // The second one sets its own element, width and gutter, which the cascading parameters must not overwrite.
        Assert.AreEqual("MAIN", containers[1].TagName);
        Assert.IsTrue(containers[1].ClassList.Contains("cascaded"));
        Assert.IsFalse(containers[1].ClassList.Contains("bit-cnt-ngt"));
        Assert.AreEqual("--bit-cnt-max-width:30rem", containers[1].GetAttribute("style"));
    }
}
