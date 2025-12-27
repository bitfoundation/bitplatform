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
    [DataRow(BitColor.Info, "bit-bdg-inf")]
    [DataRow(BitColor.Success, "bit-bdg-suc")]
    [DataRow(BitColor.Warning, "bit-bdg-wrn")]
    [DataRow(BitColor.SevereWarning, "bit-bdg-swr")]
    [DataRow(BitColor.Error, "bit-bdg-err")]
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
    [DataRow(BitPosition.TopLeft, "bit-bdg-tlf")]
    [DataRow(BitPosition.Center, "bit-bdg-ctr")]
    [DataRow(BitPosition.BottomRight, "bit-bdg-brg")]
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
}
