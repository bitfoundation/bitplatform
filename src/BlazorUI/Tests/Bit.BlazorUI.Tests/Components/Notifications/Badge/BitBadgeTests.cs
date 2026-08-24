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
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Content, 5);
        });

        var root = component.Find(".bit-bdg");

        Assert.IsNotNull(root);

        // badge container should exist
        var badgeCtn = component.Find(".bit-bdg-ctn");
        Assert.IsNotNull(badgeCtn);
    }

    [TestMethod]
    public void BitBadgeShouldNotRenderABadgeWithNothingToShow()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.AddChildContent("<span class=\"child\">child</span>");
        });

        // A badge with no content, no icon, no template, no description and no dot has nothing to report, so
        // it is not rendered as an empty mark on top of its child - which keeps rendering either way.
        Assert.AreEqual(0, component.FindAll(".bit-bdg-ctn").Count);
        Assert.AreEqual(1, component.FindAll(".child").Count);
        Assert.IsNotNull(component.Find(".bit-bdg"));
    }

    [TestMethod]
    public void BitBadgeShouldNotRenderAnEmptyStringContent()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Content, string.Empty);
        });

        Assert.AreEqual(0, component.FindAll(".bit-bdg-ctn").Count);
    }

    [TestMethod]
    public void BitBadgeShouldRenderForADotOrAnIconOrATemplateOrADescriptionAlone()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Dot, true);
        });

        Assert.AreEqual(1, component.FindAll(".bit-bdg-ctn").Count);

        component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.IconName, "Emoji");
        });

        Assert.AreEqual(1, component.FindAll(".bit-bdg-ctn").Count);

        component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.ContentTemplate, (RenderFragment)(builder => builder.AddMarkupContent(0, "<span class=\"tpl\">new</span>")));
        });

        Assert.AreEqual(1, component.FindAll(".bit-bdg-ctn").Count);

        component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Description, "Online");
        });

        // A description is content of its own for assistive technologies, so dropping the badge would drop
        // the only thing it had to say.
        Assert.AreEqual(1, component.FindAll(".bit-bdg-ctn").Count);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitBadgeShouldRespectHidden(bool hidden)
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Hidden, hidden);
            parameters.Add(p => p.Content, 5);
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
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Content, 5);
        });

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
            parameters.Add(p => p.Content, 5);
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
            parameters.Add(p => p.Content, 5);
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
            parameters.Add(p => p.Content, 5);
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
            parameters.Add(p => p.Content, 5);
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
    public void BitBadgeShouldMoveTheAriaLabelOntoItsButton()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.IconName, "TestIcon");
            parameters.Add(p => p.AriaLabel, "Inbox");
            parameters.Add(p => p.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => { }));
        });

        Assert.AreEqual("Inbox", component.Find(".bit-bdg-ctn").GetAttribute("aria-label"));
        Assert.IsNull(component.Find(".bit-bdg").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitBadgeShouldKeepTheAriaLabelOnItsRootWhenTheButtonHasADescription()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.IconName, "TestIcon");
            parameters.Add(p => p.AriaLabel, "Inbox");
            parameters.Add(p => p.Description, "5 unread messages");
            parameters.Add(p => p.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => { }));
        });

        Assert.AreEqual("Inbox", component.Find(".bit-bdg").GetAttribute("aria-label"));
        Assert.IsNull(component.Find(".bit-bdg-ctn").GetAttribute("aria-label"));
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

        Assert.AreEqual(live ? 1 : 0, component.FindAll(".bit-bdg-lvr").Count);

        // The visual badge is hidden from assistive technologies while the live region carries its text,
        // so the count is never announced twice.
        Assert.AreEqual(live ? "true" : null, component.Find(".bit-bdg-wrp").GetAttribute("aria-hidden"));

        if (live is false) return;

        var region = component.Find(".bit-bdg-lvr");

        Assert.AreEqual("status", region.GetAttribute("role"));
        Assert.AreEqual("polite", region.GetAttribute("aria-live"));
        Assert.AreEqual("true", region.GetAttribute("aria-atomic"));
        Assert.AreEqual("5", region.TextContent);
    }

    [TestMethod]
    public void BitBadgeLiveRegionShouldPreferTheDescription()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Live, true);
            parameters.Add(p => p.Content, 5);
            parameters.Add(p => p.Description, "5 unread messages");
        });

        Assert.AreEqual("5 unread messages", component.Find(".bit-bdg-lvr").TextContent);
    }

    [TestMethod]
    public void BitBadgeLiveRegionShouldCarryNoTextForAContentlessDot()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Dot, true);
            parameters.Add(p => p.Live, true);
            parameters.Add(p => p.Content, 5);
        });

        // A dot shows nothing, so there is nothing to read out of it either; only a description gives it a voice.
        Assert.AreEqual(string.Empty, component.Find(".bit-bdg-lvr").TextContent);
    }

    [TestMethod]
    public void BitBadgeLiveRegionShouldOutliveTheBadgeItself()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Live, true);
            parameters.Add(p => p.Content, 0);
            parameters.Add(p => p.ShowZero, false);
        });

        // The badge is gone, but the region a screen reader listens to has to already be on the page when
        // the count comes back - otherwise the change it is meant to announce is what creates it.
        Assert.AreEqual(0, component.FindAll(".bit-bdg-ctn").Count);
        Assert.AreEqual(string.Empty, component.Find(".bit-bdg-lvr").TextContent);

        component.Render(parameters => parameters.Add(p => p.Content, 3));

        Assert.AreEqual(1, component.FindAll(".bit-bdg-ctn").Count);
        Assert.AreEqual("3", component.Find(".bit-bdg-lvr").TextContent);
    }

    [TestMethod]
    public void BitBadgeLiveRegionShouldFollowTheContent()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Live, true);
            parameters.Add(p => p.Content, 5);
            parameters.Add(p => p.Max, 9);
        });

        Assert.AreEqual("5", component.Find(".bit-bdg-lvr").TextContent);

        component.Render(parameters => parameters.Add(p => p.Content, 50));

        Assert.AreEqual("9+", component.Find(".bit-bdg-lvr").TextContent);
    }

    [TestMethod]
    public void BitBadgeButtonShouldKeepItsLiveRegionInsideItself()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Content, 5);
            parameters.Add(p => p.Live, true);
            parameters.Add(p => p.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => { }));
        });

        // A focusable element cannot be hidden from a screen reader, so a clickable badge announces itself
        // rather than being spoken for by a region next to it.
        Assert.AreEqual(0, component.FindAll(".bit-bdg-lvr").Count);
        Assert.IsNull(component.Find(".bit-bdg-wrp").GetAttribute("aria-hidden"));
        Assert.AreEqual("polite", component.Find(".bit-bdg-ctn").GetAttribute("aria-live"));
    }

    [TestMethod]
    public void BitBadgeShouldNotRenderALiveRegionWithoutLive()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Content, 5);
        });

        Assert.AreEqual(0, component.FindAll(".bit-bdg-lvr").Count);
        Assert.IsNull(component.Find(".bit-bdg-wrp").GetAttribute("aria-hidden"));
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

    [TestMethod]
    public void BitBadgeShouldRespectLiveRegionClassAndStyle()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Live, true);
            parameters.Add(p => p.Content, 5);
            parameters.Add(p => p.Classes, new BitBadgeClassStyles { LiveRegion = "custom-live" });
            parameters.Add(p => p.Styles, new BitBadgeClassStyles { LiveRegion = "color: red;" });
        });

        var region = component.Find(".bit-bdg-lvr");

        Assert.IsTrue(region.ClassList.Contains("custom-live"));
        Assert.AreEqual("color: red;", region.GetAttribute("style"));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitBadgeShouldRespectInline(bool inline)
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Inline, inline);
            parameters.Add(p => p.Content, 5);
            parameters.AddChildContent("<span class=\"child\">child</span>");
        });

        Assert.AreEqual(inline, component.Find(".bit-bdg").ClassList.Contains("bit-bdg-inl"));

        // An inline badge is laid out in the flow next to its child, which is the same layer a standalone
        // badge lands in - so it drops out of the overlay wrapper exactly the way that one does.
        Assert.AreEqual(inline, component.Find(".bit-bdg-wrp").ClassList.Contains("bit-bdg-stl"));
    }

    [TestMethod]
    public void BitBadgeInlineShouldKeepRenderingItsChildContent()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Inline, true);
            parameters.Add(p => p.Content, 5);
            parameters.AddChildContent("<span class=\"child\">child</span>");
        });

        Assert.AreEqual(1, component.FindAll(".child").Count);
        Assert.AreEqual("5", component.Find(".bit-bdg-con").TextContent);
    }

    [TestMethod]
    public void BitBadgeInlineShouldToggleAtRuntime()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Content, 5);
            parameters.AddChildContent("<span class=\"child\">child</span>");
        });

        Assert.IsFalse(component.Find(".bit-bdg").ClassList.Contains("bit-bdg-inl"));

        component.Render(parameters => parameters.Add(p => p.Inline, true));

        Assert.IsTrue(component.Find(".bit-bdg").ClassList.Contains("bit-bdg-inl"));
        Assert.IsTrue(component.Find(".bit-bdg-wrp").ClassList.Contains("bit-bdg-stl"));
    }

    [TestMethod]
    public void BitBadgeInlineShouldDefaultToTheEndOfTheWritingDirection()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Inline, true);
            parameters.Add(p => p.Content, 5);
        });

        // An overlaid badge defaults to the physical top right, but an inline one reads only the side, and a
        // physical right would pin it to the leading edge of a right-to-left line.
        Assert.IsTrue(component.Find(".bit-bdg").ClassList.Contains("bit-bdg-ten"));
        Assert.IsFalse(component.Find(".bit-bdg").ClassList.Contains("bit-bdg-trg"));

        component.Render(parameters => parameters.Add(p => p.Inline, false));

        Assert.IsTrue(component.Find(".bit-bdg").ClassList.Contains("bit-bdg-trg"));
    }

    [TestMethod]
    public void BitBadgeInlineShouldStillRespectAnExplicitPosition()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Inline, true);
            parameters.Add(p => p.Content, 5);
            parameters.Add(p => p.Position, BitPosition.CenterStart);
        });

        Assert.IsTrue(component.Find(".bit-bdg").ClassList.Contains("bit-bdg-cst"));
    }

    [TestMethod]
    public void BitBadgeStandaloneShouldStayStandaloneWhateverInlineSays()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Inline, false);
            parameters.Add(p => p.Content, 5);
        });

        Assert.IsTrue(component.Find(".bit-bdg-wrp").ClassList.Contains("bit-bdg-stl"));
    }

    [TestMethod]
    public void BitBadgeShowZeroShouldNotHideAContentTemplate()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Content, 0);
            parameters.Add(p => p.ShowZero, false);
            parameters.Add(p => p.ContentTemplate, (RenderFragment)(builder => builder.AddMarkupContent(0, "<span class=\"tpl\">new</span>")));
        });

        // The template is what the badge is showing, so an unrelated numeric Content of zero next to it is
        // not an empty counter and must not take the badge off the page.
        Assert.AreEqual(1, component.FindAll(".bit-bdg-ctn").Count);
        Assert.AreEqual(1, component.FindAll(".tpl").Count);
    }

    [TestMethod]
    public void BitBadgeHiddenShouldStillWinOverAContentTemplate()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Hidden, true);
            parameters.Add(p => p.ContentTemplate, (RenderFragment)(builder => builder.AddMarkupContent(0, "<span class=\"tpl\">new</span>")));
        });

        Assert.AreEqual(0, component.FindAll(".bit-bdg-ctn").Count);
    }

    [TestMethod]
    public void BitBadgeShouldCapTheNativeIntegerContentTypes()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Max, 99);
            parameters.Add(p => p.Content, (nint)1000);
        });

        Assert.AreEqual("99+", component.Find(".bit-bdg-con").TextContent);

        component.Render(parameters => parameters.Add(p => p.Content, (nuint)1000));

        Assert.AreEqual("99+", component.Find(".bit-bdg-con").TextContent);
    }

    [TestMethod]
    public void BitBadgeShowZeroShouldHideANativeIntegerZero()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.ShowZero, false);
            parameters.Add(p => p.Content, (nint)0);
        });

        Assert.AreEqual(0, component.FindAll(".bit-bdg-ctn").Count);
    }

    [TestMethod]
    public void BitBadgeClickShouldNotReachWhatTheBadgeSitsOn()
    {
        var badgeClicked = false;
        var hostClicked = false;

        var component = Context.Render(builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "host");
            builder.AddAttribute(2, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => hostClicked = true));
            builder.OpenComponent<BitBadge>(3);
            builder.AddAttribute(4, nameof(BitBadge.Content), 5);
            builder.AddAttribute(5, nameof(BitBadge.OnClick), EventCallback.Factory.Create<MouseEventArgs>(this, () => badgeClicked = true));
            builder.CloseComponent();
            builder.CloseElement();
        });

        component.Find(".bit-bdg-ctn").Click();

        // A badge that does something of its own is its own target: the click stops there rather than also
        // firing whatever it is sitting on.
        Assert.IsTrue(badgeClicked);
        Assert.IsFalse(hostClicked);

        // ... and the assertion above is only worth anything because a click anywhere else inside the badge
        // does reach the host.
        component.Find(".bit-bdg").Click();

        Assert.IsTrue(hostClicked);
    }

    [TestMethod]
    public void BitBadgeShouldKeepAnIconWhenTheEmptiedCountIsTakenOffIt()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Content, 0);
            parameters.Add(p => p.ShowZero, false);
            parameters.Add(p => p.IconName, "Emoji");
        });

        // An icon is content of its own rather than a number, so ShowZero takes the emptied count off the
        // badge and leaves the glyph - and the badge - where they are.
        Assert.AreEqual(1, component.FindAll(".bit-bdg-ctn").Count);
        Assert.AreEqual(1, component.FindAll(".bit-bdg-ico").Count);
        Assert.AreEqual(0, component.FindAll(".bit-bdg-con").Count);
    }

    [TestMethod]
    public void BitBadgeShouldKeepATemplateWhenTheEmptiedCountIsTakenOffIt()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Content, 0);
            parameters.Add(p => p.ShowZero, false);
            parameters.Add(p => p.ContentTemplate, (RenderFragment)(builder => builder.AddMarkupContent(0, "<span class=\"tpl\">new</span>")));
        });

        Assert.AreEqual(1, component.FindAll(".bit-bdg-ctn").Count);
        Assert.AreEqual(1, component.FindAll(".tpl").Count);
    }

    [TestMethod]
    public void BitBadgeShouldNotAnnounceAnEmptiedCountItIsNoLongerShowing()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Live, true);
            parameters.Add(p => p.Content, 0);
            parameters.Add(p => p.ShowZero, false);
            parameters.Add(p => p.IconName, "Emoji");
        });

        // The badge is still on the page for its icon, but the zero it stopped showing is not what the live
        // region reports.
        Assert.AreEqual(string.Empty, component.Find(".bit-bdg-lvr").TextContent);
    }

    [TestMethod]
    public void BitBadgeShouldCapAFractionalContent()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Max, 99);
            parameters.Add(p => p.Content, 123.45);
        });

        Assert.AreEqual("99+", component.Find(".bit-bdg-con").TextContent);

        component.Render(parameters => parameters.Add(p => p.Content, 123.45m));

        Assert.AreEqual("99+", component.Find(".bit-bdg-con").TextContent);

        component.Render(parameters => parameters.Add(p => p.Content, 123.45f));

        Assert.AreEqual("99+", component.Find(".bit-bdg-con").TextContent);
    }

    [TestMethod]
    public void BitBadgeShouldRenderAnUncappedFractionalContentAsItPrints()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Max, 99);
            parameters.Add(p => p.Content, 12.5);
        });

        // A fraction below the max is not a capped count, so it keeps the separator of the current culture
        // rather than being rounded into an integer.
        Assert.AreEqual(12.5.ToString(), component.Find(".bit-bdg-con").TextContent);
    }

    [TestMethod]
    public void BitBadgeShouldRespectShowZeroForAFractionalZero()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.ShowZero, false);
            parameters.Add(p => p.Content, 0.0);
        });

        Assert.AreEqual(0, component.FindAll(".bit-bdg-ctn").Count);

        component.Render(parameters => parameters.Add(p => p.Content, 0m));

        Assert.AreEqual(0, component.FindAll(".bit-bdg-ctn").Count);

        component.Render(parameters => parameters.Add(p => p.Content, 0.5));

        Assert.AreEqual(1, component.FindAll(".bit-bdg-ctn").Count);
    }

    [TestMethod]
    public void BitBadgeShouldNotCountANonFiniteNumber()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Max, 99);
            parameters.Add(p => p.Content, double.NaN);
        });

        // A NaN is no count at all: it is neither capped nor read as a zero, and it is printed as it is.
        Assert.AreEqual(double.NaN.ToString(), component.Find(".bit-bdg-con").TextContent);

        component.Render(parameters => parameters.Add(p => p.Content, double.PositiveInfinity));

        Assert.AreEqual(double.PositiveInfinity.ToString(), component.Find(".bit-bdg-con").TextContent);
    }

    [TestMethod]
    public void BitBadgeShouldCapAnUnsignedContentBeyondTheRangeOfALong()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Max, 99);
            parameters.Add(p => p.Content, ulong.MaxValue);
        });

        Assert.AreEqual("99+", component.Find(".bit-bdg-con").TextContent);
    }

    [TestMethod]
    public void BitBadgeShouldRenderAnAnchorWhenItHasAnHref()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Content, 5);
            parameters.Add(p => p.Href, "/inbox");
        });

        var badge = component.Find(".bit-bdg-ctn");

        Assert.AreEqual("A", badge.TagName);
        Assert.IsTrue(badge.ClassList.Contains("bit-bdg-clk"));
        Assert.AreEqual("/inbox", badge.GetAttribute("href"));
        Assert.IsNull(badge.GetAttribute("aria-disabled"));
    }

    [TestMethod]
    public void BitBadgeHrefShouldWinOverTheButton()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Content, 5);
            parameters.Add(p => p.Href, "/inbox");
            parameters.Add(p => p.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => { }));
        });

        // A badge that leads somewhere is a link even when it also does something on the way there: the
        // navigation is what cannot be reproduced by a handler on a button.
        Assert.AreEqual("A", component.Find(".bit-bdg-ctn").TagName);
        Assert.AreEqual(0, component.FindAll("button").Count);
    }

    [TestMethod]
    public void BitBadgeAnchorShouldRespectTargetAndRel()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Content, 5);
            parameters.Add(p => p.Href, "https://bitplatform.dev");
            parameters.Add(p => p.Target, "_blank");
        });

        var badge = component.Find(".bit-bdg-ctn");

        Assert.AreEqual("_blank", badge.GetAttribute("target"));

        // a link opening in a new browsing context protects itself from reverse-tabnabbing on its own
        Assert.AreEqual("noopener", badge.GetAttribute("rel"));

        component.Render(parameters => parameters.Add(p => p.Rel, BitLinkRels.NoFollow | BitLinkRels.NoReferrer));

        Assert.AreEqual("nofollow noreferrer", component.Find(".bit-bdg-ctn").GetAttribute("rel"));
    }

    [TestMethod]
    public void BitBadgeAnchorShouldNotAddARelToAnAnchorOnlyHref()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Content, 5);
            parameters.Add(p => p.Href, "#section");
            parameters.Add(p => p.Target, "_blank");
        });

        Assert.IsNull(component.Find(".bit-bdg-ctn").GetAttribute("rel"));
    }

    [TestMethod]
    public void BitBadgeAnchorShouldRespectTabIndex()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Content, 5);
            parameters.Add(p => p.Href, "/inbox");
            parameters.Add(p => p.TabIndex, "3");
        });

        Assert.AreEqual("3", component.Find(".bit-bdg-ctn").GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitBadgeAnchorShouldDropItsHrefWhenTheBadgeIsNotEnabled()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Content, 5);
            parameters.Add(p => p.Href, "/inbox");
            parameters.Add(p => p.IsEnabled, false);
        });

        var badge = component.Find(".bit-bdg-ctn");

        // An anchor has no disabled state of its own, so a disabled link badge loses the href it would
        // follow, leaves the tab order and reports itself as disabled instead.
        Assert.IsFalse(badge.HasAttribute("href"));
        Assert.AreEqual("-1", badge.GetAttribute("tabindex"));
        Assert.AreEqual("true", badge.GetAttribute("aria-disabled"));
        Assert.IsTrue(component.Find(".bit-bdg").ClassList.Contains("bit-dis"));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitBadgeAnchorOnClickBehaviorDependsOnIsEnabled(bool isEnabled)
    {
        var clicked = false;

        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Content, 5);
            parameters.Add(p => p.Href, "/inbox");
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, (MouseEventArgs e) => clicked = true));
        });

        component.Find(".bit-bdg-ctn").Click();

        Assert.AreEqual(isEnabled, clicked);
    }

    [TestMethod]
    public void BitBadgeShouldMoveTheAriaLabelOntoItsAnchor()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Content, 5);
            parameters.Add(p => p.Href, "/inbox");
            parameters.Add(p => p.AriaLabel, "5 unread messages");
        });

        // The link is what a screen reader lands on, so it is the link that carries the name.
        Assert.AreEqual("5 unread messages", component.Find(".bit-bdg-ctn").GetAttribute("aria-label"));
        Assert.IsNull(component.Find(".bit-bdg").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitBadgeAnchorShouldKeepItsLiveRegionInsideItself()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Live, true);
            parameters.Add(p => p.Content, 5);
            parameters.Add(p => p.Href, "/inbox");
        });

        // A link is focusable, so it cannot be hidden from assistive technologies the way the rest of the
        // badge is: the region stays on the link itself rather than moving out to the root.
        Assert.AreEqual(0, component.FindAll(".bit-bdg-lvr").Count);

        var badge = component.Find(".bit-bdg-ctn");

        Assert.AreEqual("polite", badge.GetAttribute("aria-live"));
        Assert.AreEqual("true", badge.GetAttribute("aria-atomic"));
        Assert.IsNull(component.Find(".bit-bdg-wrp").GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitBadgeHiddenShouldWinOverADescriptionAlone()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Hidden, true);
            parameters.Add(p => p.Description, "Online");
        });

        Assert.AreEqual(0, component.FindAll(".bit-bdg-ctn").Count);
        Assert.AreEqual(0, component.FindAll(".bit-bdg-vhd").Count);
    }

    [TestMethod]
    public void BitBadgeAnchorShouldCarryItsDescriptionAndHideTheVisibleContentFromIt()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Content, 5);
            parameters.Add(p => p.Href, "/inbox");
            parameters.Add(p => p.Description, "5 unread messages");
        });

        Assert.AreEqual("5 unread messages", component.Find(".bit-bdg-vhd").TextContent);
        Assert.AreEqual("true", component.Find(".bit-bdg-con").GetAttribute("aria-hidden"));

        // A description names the link on its own, so the label stays where it was rather than replacing it.
        Assert.IsNull(component.Find(".bit-bdg-ctn").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitBadgeShouldNotBumpOnItsFirstRender()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Max, 99);
            parameters.Add(p => p.Content, 5);
        });

        var badge = component.Find(".bit-bdg-ctn");

        // A badge arriving on the page reports itself with its entry animation; the bump is for a count that
        // changes once the badge is already there.
        Assert.IsFalse(badge.ClassList.Contains("bit-bdg-bm1"));
        Assert.IsFalse(badge.ClassList.Contains("bit-bdg-bm2"));
    }

    [TestMethod]
    public void BitBadgeShouldBumpOnEveryChangeOfTheContentItShows()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Content, 5);
        });

        component.Render(parameters => parameters.Add(p => p.Content, 6));

        // An animation restarts only when the class carrying it changes, so the two classes alternate: the
        // badge has to land on the other one at every change for the bump to play again.
        Assert.IsTrue(component.Find(".bit-bdg-ctn").ClassList.Contains("bit-bdg-bm1"));

        component.Render(parameters => parameters.Add(p => p.Content, 7));

        Assert.IsTrue(component.Find(".bit-bdg-ctn").ClassList.Contains("bit-bdg-bm2"));
        Assert.IsFalse(component.Find(".bit-bdg-ctn").ClassList.Contains("bit-bdg-bm1"));

        component.Render(parameters => parameters.Add(p => p.Content, 8));

        Assert.IsTrue(component.Find(".bit-bdg-ctn").ClassList.Contains("bit-bdg-bm1"));
    }

    [TestMethod]
    public void BitBadgeShouldNotBumpWhenWhatItShowsStaysTheSame()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Max, 99);
            parameters.Add(p => p.Content, 100);
        });

        component.Render(parameters => parameters.Add(p => p.Content, 200));

        // Both counts are capped to the same 99+, so nothing the reader can see has changed.
        Assert.IsFalse(component.Find(".bit-bdg-ctn").ClassList.Contains("bit-bdg-bm1"));
        Assert.IsFalse(component.Find(".bit-bdg-ctn").ClassList.Contains("bit-bdg-bm2"));

        component.Render(parameters => parameters.Add(p => p.Max, 999));

        // ... and lifting the max above the count does change it, so that is a bump.
        Assert.IsTrue(component.Find(".bit-bdg-ctn").ClassList.Contains("bit-bdg-bm1"));
    }

    [TestMethod]
    public void BitBadgeShouldRenderTheTitleOnTheBadgeItself()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Max, 99);
            parameters.Add(p => p.Content, 12345);
            parameters.Add(p => p.Title, "12345 unread messages");
        });

        // The tooltip belongs to the badge rather than to the child content underneath it, which is what lets
        // it spell out the count the cap shortened.
        Assert.AreEqual("12345 unread messages", component.Find(".bit-bdg-ctn").GetAttribute("title"));
        Assert.IsNull(component.Find(".bit-bdg").GetAttribute("title"));
    }

    [TestMethod]
    public void BitBadgeButtonAndAnchorShouldRenderTheTitleToo()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Content, 5);
            parameters.Add(p => p.Title, "five");
            parameters.Add(p => p.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => { }));
        });

        Assert.AreEqual("five", component.Find(".bit-bdg-ctn").GetAttribute("title"));

        component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Content, 5);
            parameters.Add(p => p.Title, "five");
            parameters.Add(p => p.Href, "/inbox");
        });

        Assert.AreEqual("five", component.Find(".bit-bdg-ctn").GetAttribute("title"));
    }

    [TestMethod]
    public void BitBadgeShouldNotRenderATitleAttributeWithoutATitle()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Content, 5);
        });

        Assert.IsFalse(component.Find(".bit-bdg-ctn").HasAttribute("title"));
    }

    [TestMethod]
    public void BitBadgeAnchorClickShouldNotReachWhatTheBadgeSitsOn()
    {
        var hostClicked = false;

        var component = Context.Render(builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "host");
            builder.AddAttribute(2, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => hostClicked = true));
            builder.OpenComponent<BitBadge>(3);
            builder.AddAttribute(4, nameof(BitBadge.Content), 5);
            builder.AddAttribute(5, nameof(BitBadge.Href), "/inbox");
            builder.CloseComponent();
            builder.CloseElement();
        });

        component.Find(".bit-bdg-ctn").Click();

        Assert.IsFalse(hostClicked);
    }

    [TestMethod]
    public void BitBadgeShouldClearItsBumpWhileItIsShowingNoText()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Content, 1);
            parameters.Add(p => p.ShowZero, false);
        });

        component.Render(parameters => parameters.Add(p => p.Content, 2));

        Assert.IsTrue(component.Find(".bit-bdg-ctn").ClassList.Contains("bit-bdg-bm1"));

        component.Render(parameters => parameters.Add(p => p.Content, 0));

        Assert.AreEqual(0, component.FindAll(".bit-bdg-ctn").Count);

        component.Render(parameters => parameters.Add(p => p.Content, 1));

        // The badge is landing on the page again rather than ticking over on it, so the arrival is the entry
        // animation's to report. The bump is declared on the same element and would otherwise win the cascade
        // for the rest of the badge's life, which would leave it with no entry animation ever again.
        var badge = component.Find(".bit-bdg-ctn");
        Assert.IsFalse(badge.ClassList.Contains("bit-bdg-bm1"));
        Assert.IsFalse(badge.ClassList.Contains("bit-bdg-bm2"));
    }

    [TestMethod]
    public void BitBadgeShouldNotBumpWhileItIsHidden()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Content, 1);
            parameters.Add(p => p.Hidden, true);
        });

        component.Render(parameters => parameters.Add(p => p.Content, 2));
        component.Render(parameters => parameters.Add(p => p.Hidden, false));

        // Nothing the reader could see has changed while the badge was off the page, so what it does now is
        // arrive rather than tick over.
        var badge = component.Find(".bit-bdg-ctn");
        Assert.IsFalse(badge.ClassList.Contains("bit-bdg-bm1"));
        Assert.IsFalse(badge.ClassList.Contains("bit-bdg-bm2"));
    }

    [TestMethod]
    public void BitBadgeShouldNotBumpForAContentATemplateHasTakenOver()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Content, 5);
            parameters.Add(p => p.ContentTemplate, (RenderFragment)(builder => builder.AddContent(0, "template")));
        });

        component.Render(parameters => parameters.Add(p => p.Content, 6));

        // The template is what the badge is showing, so a count changing behind it changes nothing the
        // reader can see and is not worth a bump.
        var badge = component.Find(".bit-bdg-ctn");
        Assert.IsFalse(badge.ClassList.Contains("bit-bdg-bm1"));
        Assert.IsFalse(badge.ClassList.Contains("bit-bdg-bm2"));
    }

    [TestMethod]
    public void BitBadgeShouldNotBumpForAContentADotHasTakenOver()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Dot, true);
            parameters.Add(p => p.Content, 5);
        });

        component.Render(parameters => parameters.Add(p => p.Content, 6));

        var badge = component.Find(".bit-bdg-ctn");
        Assert.IsFalse(badge.ClassList.Contains("bit-bdg-bm1"));
        Assert.IsFalse(badge.ClassList.Contains("bit-bdg-bm2"));
    }

    [TestMethod]
    public void BitBadgeShouldStillBumpForAnEmptiedCountItKeepsShowing()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Content, 1);
        });

        component.Render(parameters => parameters.Add(p => p.Content, 0));

        // ShowZero is on by default, so the zero is what the badge is showing now - a change like any other.
        Assert.IsTrue(component.Find(".bit-bdg-ctn").ClassList.Contains("bit-bdg-bm1"));
    }

    [TestMethod]
    public void BitBadgeLiveRegionShouldCarryNoTextForATemplate()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Live, true);
            parameters.Add(p => p.Content, 5);
            parameters.Add(p => p.ContentTemplate, (RenderFragment)(builder => builder.AddContent(0, "template")));
        });

        // The template is what the badge shows and it is markup rather than words, so the count behind it is
        // not what a screen reader should be told the badge says.
        Assert.AreEqual(string.Empty, component.Find(".bit-bdg-lvr").TextContent);
    }

    [TestMethod]
    public void BitBadgeShouldNotMuteATemplateItsLiveRegionCannotRead()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Live, true);
            parameters.Add(p => p.ContentTemplate, (RenderFragment)(builder => builder.AddContent(0, "template")));
        });

        // Hiding the badge from assistive technologies is what keeps the count from being announced twice.
        // A region with nothing to say in its place has nothing to keep it from, so silencing the badge would
        // leave the template reaching a screen reader as nothing at all.
        Assert.IsNull(component.Find(".bit-bdg-wrp").GetAttribute("aria-hidden"));
        Assert.AreEqual("template", component.Find(".bit-bdg-con").TextContent);
    }

    [TestMethod]
    public void BitBadgeLiveRegionShouldSpeakForATemplateThatHasADescription()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Live, true);
            parameters.Add(p => p.Description, "99 percent complete");
            parameters.Add(p => p.ContentTemplate, (RenderFragment)(builder => builder.AddContent(0, "99%")));
        });

        // A description is words, so the region can carry it - and the badge is hidden behind it again.
        Assert.AreEqual("99 percent complete", component.Find(".bit-bdg-lvr").TextContent);
        Assert.AreEqual("true", component.Find(".bit-bdg-wrp").GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitBadgeShouldMuteAnIconOnlyBadgeItsLiveRegionSpeaksFor()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Live, true);
            parameters.Add(p => p.Content, 5);
            parameters.Add(p => p.IconName, "Mail");
        });

        // A glyph carries no text of its own, so the counter next to it is all the badge has to say and the
        // region says it in its place.
        Assert.AreEqual("5", component.Find(".bit-bdg-lvr").TextContent);
        Assert.AreEqual("true", component.Find(".bit-bdg-wrp").GetAttribute("aria-hidden"));
    }
    [TestMethod]
    public void BitBadgeShouldBumpWhenATextlessBadgeItWasAlreadyShowingGainsACount()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.IconName, "Mail");
        });

        component.Render(parameters => parameters.Add(p => p.Content, 3));

        // The badge never left the page, so nothing replays its entry animation: the count turning up inside
        // a glyph-only badge is a change of what it shows like any other.
        Assert.IsTrue(component.Find(".bit-bdg-ctn").ClassList.Contains("bit-bdg-bm1"));
    }

    [TestMethod]
    public void BitBadgeShouldBumpWhenACountItWasAlreadyShowingIsTakenOverByATemplate()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Content, 3);
        });

        component.Render(parameters => parameters.Add(p => p.ContentTemplate, (RenderFragment)(builder => builder.AddContent(0, "template"))));

        Assert.IsTrue(component.Find(".bit-bdg-ctn").ClassList.Contains("bit-bdg-bm1"));
    }
    [TestMethod]
    public void BitBadgeShouldSpellOutACappedCountInItsTooltip()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Max, 99);
            parameters.Add(p => p.Content, 12345);
        });

        // A "99+" is the one thing a reader cannot get the real figure out of, so the figure is what the
        // badge says on hover without being asked.
        var badge = component.Find(".bit-bdg-ctn");
        Assert.AreEqual("99+", badge.TextContent);
        Assert.AreEqual("12345", badge.GetAttribute("title"));
    }

    [TestMethod]
    public void BitBadgeShouldNotSpellOutACountItIsAlreadyShowingInFull()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Max, 99);
            parameters.Add(p => p.Content, 50);
        });

        // The badge is showing the whole count, so a tooltip repeating it would say nothing new.
        Assert.IsFalse(component.Find(".bit-bdg-ctn").HasAttribute("title"));
    }

    [TestMethod]
    public void BitBadgeTitleShouldWinOverTheCountItWouldHaveSpelledOut()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Max, 99);
            parameters.Add(p => p.Content, 12345);
            parameters.Add(p => p.Title, "12345 unread messages");
        });

        Assert.AreEqual("12345 unread messages", component.Find(".bit-bdg-ctn").GetAttribute("title"));
    }

    [TestMethod]
    public void BitBadgeShouldStopSpellingOutACountItStopsShowing()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Max, 99);
            parameters.Add(p => p.Content, 12345);
            parameters.Add(p => p.Dot, true);
        });

        // A dot shows no count at all, so there is nothing a cap has shortened for the tooltip to restore.
        Assert.IsFalse(component.Find(".bit-bdg-ctn").HasAttribute("title"));
    }

    [TestMethod]
    public void BitBadgeShouldStopSpellingOutACountATemplateHasTakenOver()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Max, 99);
            parameters.Add(p => p.Content, 12345);
            parameters.Add(p => p.ContentTemplate, (RenderFragment)(builder => builder.AddContent(0, "template")));
        });

        Assert.IsFalse(component.Find(".bit-bdg-ctn").HasAttribute("title"));
    }

    [TestMethod]
    public void BitBadgeShouldFollowTheCapWithItsTooltipAtRuntime()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Max, 99);
            parameters.Add(p => p.Content, 12345);
        });

        Assert.AreEqual("12345", component.Find(".bit-bdg-ctn").GetAttribute("title"));

        component.Render(parameters => parameters.Add(p => p.Content, 5));

        Assert.IsFalse(component.Find(".bit-bdg-ctn").HasAttribute("title"));
    }

    [TestMethod]
    public void BitBadgeButtonAndAnchorShouldSpellOutACappedCountToo()
    {
        var component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Max, 9);
            parameters.Add(p => p.Content, 42);
            parameters.Add(p => p.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => { }));
        });

        Assert.AreEqual("42", component.Find(".bit-bdg-ctn").GetAttribute("title"));

        component = RenderComponent<BitBadge>(parameters =>
        {
            parameters.Add(p => p.Max, 9);
            parameters.Add(p => p.Content, 42);
            parameters.Add(p => p.Href, "/inbox");
        });

        Assert.AreEqual("42", component.Find(".bit-bdg-ctn").GetAttribute("title"));
    }
}
