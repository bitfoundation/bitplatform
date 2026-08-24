using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;

namespace Bit.BlazorUI.Tests.Components.Notifications.Badge;

[TestClass]
public class BitBadgeTests : BunitTestContext
{
    [TestMethod]
    public void BitBadgeShouldRenderExpectedElement()
    {
        var component = RenderComponent<BitBadge>();

        var root = component.Find(".bit-bdg");

        Assert.IsNotNull(root);

        // badge container should exist
        var badgeCtn = component.Find(".bit-bdg-ctn");
        Assert.IsNotNull(badgeCtn);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitBadgeShouldRespectHidden(bool hidden)
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Hidden, hidden);
        });

        var badgeContainers = component.FindAll(".bit-bdg-ctn");

        Assert.AreEqual(hidden ? 0 : 1, badgeContainers.Count);
    }

    [TestMethod]
    public void BitBadgeShouldToggleHiddenAtRuntime()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Content, 5);
        });

        Assert.AreEqual(1, component.FindAll(".bit-bdg-ctn").Count);

        component.Render(parameters => parameters.Add(p => p.Hidden, true));

        Assert.AreEqual(0, component.FindAll(".bit-bdg-ctn").Count);

        component.Render(parameters => parameters.Add(p => p.Hidden, false));

        Assert.AreEqual(1, component.FindAll(".bit-bdg-ctn").Count);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitBadgeShouldRespectDot(bool dot)
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Dot, dot);
            parameters.Add(p => p.Content, "99");
        });

        var root = component.Find(".bit-bdg");

        Assert.AreEqual(dot, root.ClassList.Contains("bit-bdg-dot"));

        var badgeCtn = component.Find(".bit-bdg-ctn");

        if (dot)
        {
            // when dot is true the badge shouldn't render icon or content
            Assert.AreEqual(string.Empty, badgeCtn.TextContent.Trim());
            Assert.AreEqual(0, badgeCtn.GetElementsByTagName("i").Length);
        }
        else
        {
            Assert.IsTrue(badgeCtn.TextContent.Trim().Length > 0);
        }
    }

    [TestMethod]
    public void BitBadgeDotShouldStillRenderItsDescription()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Dot, true);
            parameters.Add(p => p.Content, 99);
            parameters.Add(p => p.Description, "3 unread messages");
        });

        var description = component.Find(".bit-bdg-vhd");

        Assert.AreEqual("3 unread messages", description.TextContent);
        Assert.AreEqual(0, component.FindAll(".bit-bdg-con").Count);
    }

    [TestMethod]
    public void BitBadgeShouldRenderIconWhenIconNameProvided()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.IconName, "TestIcon");
            parameters.Add(p => p.Dot, false);
        });

        var icon = component.Find(".bit-bdg-ctn > i");
        Assert.IsNotNull(icon);
        Assert.IsTrue(icon.ClassList.Contains("bit-icon--TestIcon"));
        Assert.IsTrue(icon.ClassList.Contains("bit-bdg-ico"));
        Assert.AreEqual("true", icon.GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitBadgeShouldRenderIconWhenIconCssProvided()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Icon, BitIconInfo.Css("fa-solid fa-house"));
        });

        var icon = component.Find(".bit-bdg-ico");
        Assert.IsTrue(icon.ClassList.Contains("fa-solid"));
        Assert.IsTrue(icon.ClassList.Contains("fa-house"));
    }

    [TestMethod]
    public void BitBadgeIconShouldTakePrecedenceOverIconName()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.IconName, "TestIcon");
            parameters.Add(p => p.Icon, BitIconInfo.Bi("gear-fill"));
        });

        var icon = component.Find(".bit-bdg-ico");
        Assert.IsTrue(icon.ClassList.Contains("bi"));
        Assert.IsTrue(icon.ClassList.Contains("bi-gear-fill"));
        Assert.IsFalse(icon.ClassList.Contains("bit-icon--TestIcon"));
    }

    [TestMethod]
    public void BitBadgeShouldRenderStringContent()
    {
        var content = "Hello";
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Content, content);
            parameters.Add(p => p.Dot, false);
        });

        var badgeCtn = component.Find(".bit-bdg-ctn");
        Assert.IsTrue(badgeCtn.TextContent.Contains(content));

        var contentElement = component.Find(".bit-bdg-con");
        Assert.AreEqual(content, contentElement.TextContent);
    }

    [TestMethod]
    public void BitBadgeShouldRespectNumericContentAndMax()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Content, 150);
            parameters.Add(p => p.Max, 99);
            parameters.Add(p => p.Dot, false);
        });

        var badgeCtn = component.Find(".bit-bdg-ctn");
        Assert.IsTrue(badgeCtn.TextContent.Contains("99+"));
    }

    [TestMethod]
    public void BitBadgeShouldNotCapAContentBelowTheMax()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Content, 99);
            parameters.Add(p => p.Max, 99);
        });

        Assert.AreEqual("99", component.Find(".bit-bdg-con").TextContent);
    }

    [TestMethod]
    public void BitBadgeShouldRecalculateTheContentWhenMaxChanges()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Content, 150);
        });

        Assert.AreEqual("150", component.Find(".bit-bdg-con").TextContent);

        component.Render(parameters => parameters.Add(p => p.Max, 99));

        Assert.AreEqual("99+", component.Find(".bit-bdg-con").TextContent);

        component.Render(parameters => parameters.Add(p => p.Max, 200));

        Assert.AreEqual("150", component.Find(".bit-bdg-con").TextContent);
    }

    [TestMethod]
    public void BitBadgeShouldRecalculateTheContentWhenContentChanges()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Content, 1);
            parameters.Add(p => p.Max, 9);
        });

        Assert.AreEqual("1", component.Find(".bit-bdg-con").TextContent);

        component.Render(parameters => parameters.Add(p => p.Content, 42));

        Assert.AreEqual("9+", component.Find(".bit-bdg-con").TextContent);
    }

    [TestMethod]
    public void BitBadgeShouldCapEveryIntegralContentType()
    {
        AssertContent(150L, 99, "99+");
        AssertContent((short)150, 99, "99+");
        AssertContent((byte)150, 99, "99+");
        AssertContent((sbyte)100, 99, "99+");
        AssertContent((ushort)150, 99, "99+");
        AssertContent(150u, 99, "99+");
        AssertContent(150ul, 99, "99+");

        void AssertContent(object content, int max, string expected)
        {
            var component = RenderComponent<BitBadge>(parameters =>
            {
                parameters.Add(p => p.Content, content);
                parameters.Add(p => p.Max, max);
            });

            Assert.AreEqual(expected, component.Find(".bit-bdg-con").TextContent, $"for a content of type {content.GetType().Name}");
        }
    }

    [TestMethod]
    public void BitBadgeShouldRenderANonIntegralContentThroughToString()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Content, BitColor.Success);
        });

        Assert.AreEqual("Success", component.Find(".bit-bdg-con").TextContent);
    }

    [TestMethod]
    public void BitBadgeShouldRenderNoContentElementWhenContentIsNull()
    {
        var component = RenderComponent<BitBadge>();

        Assert.AreEqual(0, component.FindAll(".bit-bdg-con").Count);
    }

    [TestMethod]
    [DataRow(0, true, 1)]
    [DataRow(0, false, 0)]
    [DataRow(1, false, 1)]
    public void BitBadgeShouldRespectShowZero(int content, bool showZero, int expectedCount)
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Content, content);
            parameters.Add(p => p.ShowZero, showZero);
        });

        Assert.AreEqual(expectedCount, component.FindAll(".bit-bdg-ctn").Count);
    }

    [TestMethod]
    public void BitBadgeShowZeroShouldNotHideANonNumericContent()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Content, "0");
            parameters.Add(p => p.ShowZero, false);
        });

        Assert.AreEqual(1, component.FindAll(".bit-bdg-ctn").Count);
    }

    [TestMethod]
    public void BitBadgeShowZeroShouldHideADotToo()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Dot, true);
            parameters.Add(p => p.Content, 0);
            parameters.Add(p => p.ShowZero, false);
        });

        Assert.AreEqual(0, component.FindAll(".bit-bdg-ctn").Count);
    }

    [TestMethod]
    public void BitBadgeShowZeroShouldKeepAContentlessDot()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Dot, true);
            parameters.Add(p => p.ShowZero, false);
        });

        Assert.AreEqual(1, component.FindAll(".bit-bdg-ctn").Count);
    }

    [TestMethod]
    public void BitBadgeShouldReappearWhenTheCountLeavesZero()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Content, 0);
            parameters.Add(p => p.ShowZero, false);
        });

        Assert.AreEqual(0, component.FindAll(".bit-bdg-ctn").Count);

        component.Render(parameters => parameters.Add(p => p.Content, 1));

        Assert.AreEqual(1, component.FindAll(".bit-bdg-ctn").Count);
        Assert.AreEqual("1", component.Find(".bit-bdg-con").TextContent);
    }

    [TestMethod]
    public void BitBadgeShouldRenderContentTemplateInsteadOfContent()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Content, 5);
            parameters.Add(p => p.ContentTemplate, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "b");
                builder.AddContent(1, "template");
                builder.CloseElement();
            }));
        });

        var content = component.Find(".bit-bdg-con");
        Assert.AreEqual("template", content.TextContent);
        Assert.IsNotNull(content.QuerySelector("b"));
    }

    [TestMethod]
    public void BitBadgeShouldNotRenderContentTemplateWhenDot()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Dot, true);
            parameters.Add(p => p.ContentTemplate, (RenderFragment)(builder => builder.AddContent(0, "template")));
        });

        Assert.AreEqual(0, component.FindAll(".bit-bdg-con").Count);
    }

    [TestMethod]
    [DataRow(BitColor.Primary, "bit-bdg-pri")]
    [DataRow(BitColor.Secondary, "bit-bdg-sec")]
    [DataRow(BitColor.Tertiary, "bit-bdg-ter")]
    [DataRow(BitColor.Info, "bit-bdg-inf")]
    [DataRow(BitColor.Success, "bit-bdg-suc")]
    [DataRow(BitColor.Warning, "bit-bdg-wrn")]
    [DataRow(BitColor.SevereWarning, "bit-bdg-swr")]
    [DataRow(BitColor.Error, "bit-bdg-err")]
    [DataRow(BitColor.PrimaryBackground, "bit-bdg-pbg")]
    [DataRow(BitColor.SecondaryBackground, "bit-bdg-sbg")]
    [DataRow(BitColor.TertiaryBackground, "bit-bdg-tbg")]
    [DataRow(BitColor.PrimaryForeground, "bit-bdg-pfg")]
    [DataRow(BitColor.SecondaryForeground, "bit-bdg-sfg")]
    [DataRow(BitColor.TertiaryForeground, "bit-bdg-tfg")]
    [DataRow(BitColor.PrimaryBorder, "bit-bdg-pbr")]
    [DataRow(BitColor.SecondaryBorder, "bit-bdg-sbr")]
    [DataRow(BitColor.TertiaryBorder, "bit-bdg-tbr")]
    [DataRow(null, "bit-bdg-pri")]
    public void BitBadgeShouldRespectColor(BitColor? color, string expectedClass)
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            if (color.HasValue) parameters.Add(p => p.Color, color.Value);
        });

        var root = component.Find(".bit-bdg");
        Assert.IsTrue(root.ClassList.Contains(expectedClass));
    }

    [TestMethod]
    public void BitBadgeShouldRespectColorChangingAtRuntime()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Color, BitColor.Info);
        });

        Assert.IsTrue(component.Find(".bit-bdg").ClassList.Contains("bit-bdg-inf"));

        component.Render(parameters => parameters.Add(p => p.Color, BitColor.Error));

        var root = component.Find(".bit-bdg");
        Assert.IsTrue(root.ClassList.Contains("bit-bdg-err"));
        Assert.IsFalse(root.ClassList.Contains("bit-bdg-inf"));
    }

    [TestMethod]
    [DataRow(BitSize.Small, "bit-bdg-sm")]
    [DataRow(BitSize.Medium, "bit-bdg-md")]
    [DataRow(BitSize.Large, "bit-bdg-lg")]
    [DataRow(null, "bit-bdg-md")]
    public void BitBadgeShouldRespectSize(BitSize? size, string expectedClass)
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            if (size.HasValue) parameters.Add(p => p.Size, size.Value);
        });

        var root = component.Find(".bit-bdg");
        Assert.IsTrue(root.ClassList.Contains(expectedClass));
    }

    [TestMethod]
    [DataRow(BitVariant.Fill, "bit-bdg-fil")]
    [DataRow(BitVariant.Outline, "bit-bdg-otl")]
    [DataRow(BitVariant.Text, "bit-bdg-txt")]
    [DataRow(null, "bit-bdg-fil")]
    public void BitBadgeShouldRespectVariant(BitVariant? variant, string expectedClass)
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            if (variant.HasValue) parameters.Add(p => p.Variant, variant.Value);
        });

        var root = component.Find(".bit-bdg");
        Assert.IsTrue(root.ClassList.Contains(expectedClass));
    }

    [TestMethod]
    [DataRow(BitBadgeShape.Circular, "bit-bdg-cir")]
    [DataRow(BitBadgeShape.Rounded, "bit-bdg-rnd")]
    [DataRow(BitBadgeShape.Square, "bit-bdg-sqr")]
    [DataRow(null, "bit-bdg-cir")]
    public void BitBadgeShouldRespectShape(BitBadgeShape? shape, string expectedClass)
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            if (shape.HasValue) parameters.Add(p => p.Shape, shape.Value);
        });

        var root = component.Find(".bit-bdg");
        Assert.IsTrue(root.ClassList.Contains(expectedClass));
    }

    [TestMethod]
    [DataRow(BitPosition.TopLeft, "bit-bdg-tlf")]
    [DataRow(BitPosition.TopCenter, "bit-bdg-tcr")]
    [DataRow(BitPosition.TopRight, "bit-bdg-trg")]
    [DataRow(BitPosition.TopStart, "bit-bdg-tst")]
    [DataRow(BitPosition.TopEnd, "bit-bdg-ten")]
    [DataRow(BitPosition.CenterLeft, "bit-bdg-clf")]
    [DataRow(BitPosition.Center, "bit-bdg-ctr")]
    [DataRow(BitPosition.CenterRight, "bit-bdg-crg")]
    [DataRow(BitPosition.CenterStart, "bit-bdg-cst")]
    [DataRow(BitPosition.CenterEnd, "bit-bdg-cen")]
    [DataRow(BitPosition.BottomLeft, "bit-bdg-blf")]
    [DataRow(BitPosition.BottomCenter, "bit-bdg-bcr")]
    [DataRow(BitPosition.BottomRight, "bit-bdg-brg")]
    [DataRow(BitPosition.BottomStart, "bit-bdg-bst")]
    [DataRow(BitPosition.BottomEnd, "bit-bdg-ben")]
    [DataRow(null, "bit-bdg-trg")]
    public void BitBadgeShouldRespectPosition(BitPosition? position, string expectedClass)
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            if (position.HasValue) parameters.Add(p => p.Position, position.Value);
        });

        var root = component.Find(".bit-bdg");
        Assert.IsTrue(root.ClassList.Contains(expectedClass));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitBadgeShouldRespectOverlap(bool overlap)
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Overlap, overlap);
        });

        var root = component.Find(".bit-bdg");
        Assert.AreEqual(overlap, root.ClassList.Contains("bit-bdg-orp"));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitBadgeShouldRespectBordered(bool bordered)
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Bordered, bordered);
        });

        var root = component.Find(".bit-bdg");
        Assert.AreEqual(bordered, root.ClassList.Contains("bit-bdg-brd"));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitBadgeShouldRespectPulse(bool pulse)
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Pulse, pulse);
        });

        var root = component.Find(".bit-bdg");
        Assert.AreEqual(pulse, root.ClassList.Contains("bit-bdg-pls"));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitBadgeShouldRespectReversed(bool reversed)
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Reversed, reversed);
        });

        var root = component.Find(".bit-bdg");
        Assert.AreEqual(reversed, root.ClassList.Contains("bit-bdg-rvs"));
    }

    [TestMethod]
    public void BitBadgeShouldRespectOffsets()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.OffsetX, "-0.5rem");
            parameters.Add(p => p.OffsetY, "4px");
        });

        var style = component.Find(".bit-bdg").GetAttribute("style");

        Assert.IsTrue(style!.Contains("--bit-bdg-ofs-x:-0.5rem"));
        Assert.IsTrue(style.Contains("--bit-bdg-ofs-y:4px"));
    }

    [TestMethod]
    public void BitBadgeShouldRespectOffsetsChangingAtRuntime()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.OffsetX, "1px");
        });

        Assert.IsTrue(component.Find(".bit-bdg").GetAttribute("style")!.Contains("--bit-bdg-ofs-x:1px"));

        component.Render(parameters => parameters.Add(p => p.OffsetX, "2px"));

        var style = component.Find(".bit-bdg").GetAttribute("style");
        Assert.IsTrue(style!.Contains("--bit-bdg-ofs-x:2px"));
        Assert.IsFalse(style.Contains("--bit-bdg-ofs-x:1px"));
    }

    [TestMethod]
    public void BitBadgeShouldNotRenderTheOffsetVariablesWhenTheyAreNotSet()
    {
        var component = RenderComponent<BitBadge>();

        var style = component.Find(".bit-bdg").GetAttribute("style");

        Assert.IsTrue(style is null || style.Contains("--bit-bdg-ofs") is false);
    }

    [TestMethod]
    public void BitBadgeShouldRenderStandaloneWithoutChildContent()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Content, 5);
        });

        var wrapper = component.Find(".bit-bdg-wrp");

        Assert.IsTrue(wrapper.ClassList.Contains("bit-bdg-stl"));
    }

    [TestMethod]
    public void BitBadgeShouldNotRenderStandaloneWithChildContent()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Content, 5);
            parameters.AddChildContent("<span>anchor</span>");
        });

        var wrapper = component.Find(".bit-bdg-wrp");

        Assert.IsFalse(wrapper.ClassList.Contains("bit-bdg-stl"));
        Assert.IsNotNull(component.Find(".bit-bdg > span"));
    }

    [TestMethod]
    public void BitBadgeShouldRenderTheChildContent()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.AddChildContent("<span id=\"anchor\">anchor</span>");
        });

        Assert.IsNotNull(component.Find("#anchor"));
    }

    [TestMethod]
    public void BitBadgeShouldRenderASpanWhenItHasNoClickHandler()
    {
        var component = RenderComponent<BitBadge>();

        var badge = component.Find(".bit-bdg-ctn");

        Assert.AreEqual("SPAN", badge.TagName);
        Assert.IsFalse(badge.ClassList.Contains("bit-bdg-clk"));
        Assert.IsNull(badge.GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitBadgeShouldRenderAButtonWhenItHasAClickHandler()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => { }));
        });

        var badge = component.Find(".bit-bdg-ctn");

        Assert.AreEqual("BUTTON", badge.TagName);
        Assert.IsTrue(badge.ClassList.Contains("bit-bdg-clk"));
        Assert.AreEqual("button", badge.GetAttribute("type"));
        Assert.IsFalse(badge.HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitBadgeButtonShouldBeDisabledWhenTheBadgeIsNotEnabled()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => { }));
        });

        var badge = component.Find(".bit-bdg-ctn");

        Assert.IsTrue(badge.HasAttribute("disabled"));
        Assert.IsTrue(component.Find(".bit-bdg").ClassList.Contains("bit-dis"));
    }

    [TestMethod]
    public void BitBadgeButtonShouldRespectTabIndex()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.TabIndex, "3");
            parameters.Add(p => p.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => { }));
        });

        Assert.AreEqual("3", component.Find(".bit-bdg-ctn").GetAttribute("tabindex"));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitBadgeOnClickBehaviorDependsOnIsEnabled(bool isEnabled)
    {
        var clicked = false;

        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, (MouseEventArgs e) => clicked = true));
        });

        var badgeCtn = component.Find(".bit-bdg-ctn");
        badgeCtn.Click();

        Assert.AreEqual(isEnabled, clicked);
    }

    [TestMethod]
    public void BitBadgeShouldRenderTheAriaLabelOnItsRoot()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, "Inbox");
        });

        Assert.AreEqual("Inbox", component.Find(".bit-bdg").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitBadgeShouldRenderTheDescriptionAndHideTheVisibleContentFromIt()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Content, 5);
            parameters.Add(p => p.IconName, "TestIcon");
            parameters.Add(p => p.Description, "5 unread messages");
        });

        var description = component.Find(".bit-bdg-vhd");
        Assert.AreEqual("5 unread messages", description.TextContent);

        Assert.AreEqual("true", component.Find(".bit-bdg-con").GetAttribute("aria-hidden"));
        Assert.AreEqual("true", component.Find(".bit-bdg-ico").GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitBadgeShouldNotHideItsContentWithoutADescription()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Content, 5);
        });

        Assert.AreEqual(0, component.FindAll(".bit-bdg-vhd").Count);
        Assert.IsNull(component.Find(".bit-bdg-con").GetAttribute("aria-hidden"));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitBadgeShouldRespectLive(bool live)
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Content, 5);
            parameters.Add(p => p.Live, live);
        });

        var badge = component.Find(".bit-bdg-ctn");

        Assert.AreEqual(live ? "status" : null, badge.GetAttribute("role"));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitBadgeButtonShouldRespectLive(bool live)
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Content, 5);
            parameters.Add(p => p.Live, live);
            parameters.Add(p => p.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => { }));
        });

        var badge = component.Find(".bit-bdg-ctn");

        Assert.AreEqual(live ? "polite" : null, badge.GetAttribute("aria-live"));
        Assert.AreEqual(live ? "true" : null, badge.GetAttribute("aria-atomic"));
    }

    [TestMethod]
    [DataRow(BitDir.Rtl, "bit-rtl")]
    [DataRow(BitDir.Ltr, null)]
    public void BitBadgeShouldRespectDir(BitDir dir, string? expectedClass)
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Dir, dir);
        });

        var root = component.Find(".bit-bdg");

        Assert.AreEqual(dir.ToString().ToLower(), root.GetAttribute("dir"));

        if (expectedClass is not null)
        {
            Assert.IsTrue(root.ClassList.Contains(expectedClass));
        }
        else
        {
            Assert.IsFalse(root.ClassList.Contains("bit-rtl"));
        }
    }

    [TestMethod]
    public void BitBadgeShouldRespectClassesAndStyles()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Content, 5);
            parameters.Add(p => p.IconName, "TestIcon");
            parameters.Add(p => p.Description, "5 unread messages");
            parameters.Add(p => p.Classes, new BitBadgeClassStyles
            {
                Root = "custom-root",
                BadgeWrapper = "custom-wrapper",
                Badge = "custom-badge",
                Icon = "custom-icon",
                Content = "custom-content",
                Description = "custom-description"
            });
            parameters.Add(p => p.Styles, new BitBadgeClassStyles
            {
                Root = "color: red;",
                BadgeWrapper = "color: green;",
                Badge = "color: blue;",
                Icon = "color: yellow;",
                Content = "color: purple;",
                Description = "color: pink;"
            });
        });

        Assert.IsTrue(component.Find(".bit-bdg").ClassList.Contains("custom-root"));
        Assert.IsTrue(component.Find(".bit-bdg").GetAttribute("style")!.Contains("color: red;"));

        Assert.IsTrue(component.Find(".bit-bdg-wrp").ClassList.Contains("custom-wrapper"));
        Assert.AreEqual("color: green;", component.Find(".bit-bdg-wrp").GetAttribute("style"));

        Assert.IsTrue(component.Find(".bit-bdg-ctn").ClassList.Contains("custom-badge"));
        Assert.AreEqual("color: blue;", component.Find(".bit-bdg-ctn").GetAttribute("style"));

        Assert.IsTrue(component.Find(".bit-bdg-ico").ClassList.Contains("custom-icon"));
        Assert.AreEqual("color: yellow;", component.Find(".bit-bdg-ico").GetAttribute("style"));

        Assert.IsTrue(component.Find(".bit-bdg-con").ClassList.Contains("custom-content"));
        Assert.AreEqual("color: purple;", component.Find(".bit-bdg-con").GetAttribute("style"));

        Assert.IsTrue(component.Find(".bit-bdg-vhd").ClassList.Contains("custom-description"));
        Assert.AreEqual("color: pink;", component.Find(".bit-bdg-vhd").GetAttribute("style"));
    }

    [TestMethod]
    public void BitBadgeShouldRespectClassAndStyle()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Class, "custom-class");
            parameters.Add(p => p.Style, "color: tomato;");
        });

        var root = component.Find(".bit-bdg");

        Assert.IsTrue(root.ClassList.Contains("custom-class"));
        Assert.IsTrue(root.GetAttribute("style")!.Contains("color: tomato;"));
    }

    [TestMethod]
    [DataRow(BitVisibility.Visible, "")]
    [DataRow(BitVisibility.Hidden, "visibility:hidden")]
    [DataRow(BitVisibility.Collapsed, "display:none")]
    public void BitBadgeShouldRespectVisibility(BitVisibility visibility, string expectedStyle)
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Visibility, visibility);
        });

        var style = component.Find(".bit-bdg").GetAttribute("style");

        if (expectedStyle.Length == 0)
        {
            Assert.IsTrue(style is null || style.Contains("visibility:hidden") is false);
        }
        else
        {
            Assert.IsTrue(style!.Contains(expectedStyle));
        }
    }

    [TestMethod]
    public void BitBadgeShouldRespectId()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Id, "the-badge");
        });

        Assert.AreEqual("the-badge", component.Find(".bit-bdg").GetAttribute("id"));
    }
}
