using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Notifications.Tag;

[TestClass]
public class BitTagTests : BunitTestContext
{
    [TestMethod]
    public void BitTagShouldRenderExpectedElement()
    {
        var component = RenderComponent<BitTag>();

        var root = component.Find(".bit-tag");

        Assert.IsNotNull(root);
    }

    [TestMethod]
    public void BitTagShouldRenderIconWhenIconNameProvided()
    {
        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.IconName, "TestIcon");
        });

        var icon = component.Find(".bit-tag-icn");
        Assert.IsNotNull(icon);
        Assert.IsTrue(icon.ClassList.Contains("bit-icon--TestIcon"));
    }

    [TestMethod]
    public void BitTagShouldRenderTextWhenTextProvided()
    {
        var text = "Sample";
        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.Text, text);
        });

        var textEl = component.Find(".bit-tag-tex");
        Assert.IsNotNull(textEl);
        Assert.AreEqual(text, textEl.TextContent);
    }

    [TestMethod]
    public void BitTagShouldRenderChildContentOverDefault()
    {
        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.IconName, "ShouldNotRender");
            parameters.AddChildContent("<span class=\"custom\">Custom</span>");
        });

        var custom = component.Find(".custom");
        Assert.IsNotNull(custom);

        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-tag-icn"));
        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-tag-tex"));
    }

    [TestMethod]
    [DataRow(BitColor.Info, "bit-tag-inf")]
    [DataRow(BitColor.Success, "bit-tag-suc")]
    [DataRow(BitColor.Warning, "bit-tag-wrn")]
    [DataRow(BitColor.SevereWarning, "bit-tag-swr")]
    [DataRow(BitColor.Error, "bit-tag-err")]
    [DataRow(null, "bit-tag-pri")]
    public void BitTagShouldRespectColor(BitColor? color, string expectedClass)
    {
        var component = RenderComponent<BitTag>(parameters =>
        {
            if (color.HasValue) parameters.Add(p => p.Color, color.Value);
        });

        var root = component.Find(".bit-tag");
        Assert.IsTrue(root.ClassList.Contains(expectedClass));
    }

    [TestMethod]
    [DataRow(BitSize.Small, "bit-tag-sm")]
    [DataRow(BitSize.Medium, "bit-tag-md")]
    [DataRow(BitSize.Large, "bit-tag-lg")]
    [DataRow(null, "bit-tag-md")]
    public void BitTagShouldRespectSize(BitSize? size, string expectedClass)
    {
        var component = RenderComponent<BitTag>(parameters =>
        {
            if (size.HasValue) parameters.Add(p => p.Size, size.Value);
        });

        var root = component.Find(".bit-tag");
        Assert.IsTrue(root.ClassList.Contains(expectedClass));
    }

    [TestMethod]
    [DataRow(BitVariant.Fill, "bit-tag-fil")]
    [DataRow(BitVariant.Outline, "bit-tag-otl")]
    [DataRow(BitVariant.Text, "bit-tag-txt")]
    [DataRow(null, "bit-tag-fil")]
    public void BitTagShouldRespectVariant(BitVariant? variant, string expectedClass)
    {
        var component = RenderComponent<BitTag>(parameters =>
        {
            if (variant.HasValue) parameters.Add(p => p.Variant, variant.Value);
        });

        var root = component.Find(".bit-tag");
        Assert.IsTrue(root.ClassList.Contains(expectedClass));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitTagShouldRespectReversed(bool reversed)
    {
        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.Reversed, reversed);
        });

        var root = component.Find(".bit-tag");
        Assert.AreEqual(reversed, root.ClassList.Contains("bit-tag-rvs"));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitTagOnClickBehaviorDependsOnIsEnabled(bool isEnabled)
    {
        var clicked = false;

        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, (MouseEventArgs e) => clicked = true));
        });

        var root = component.Find(".bit-tag");
        root.Click();

        Assert.AreEqual(isEnabled, clicked);
    }

    [TestMethod]
    public void BitTagShouldRenderDismissButtonWhenOnDismissSetAndTriggerIt()
    {
        var dismissed = false;

        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.OnDismiss, EventCallback.Factory.Create<MouseEventArgs>(this, (MouseEventArgs e) => dismissed = true));
            parameters.Add(p => p.IsEnabled, true);
        });

        var dismissBtn = component.Find(".bit-tag-cls");
        Assert.IsNotNull(dismissBtn);

        dismissBtn.Click();

        Assert.IsTrue(dismissed);
    }

    [TestMethod]
    public void BitTagDismissDoesNotTriggerWhenDisabled()
    {
        var dismissed = false;

        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.OnDismiss, EventCallback.Factory.Create<MouseEventArgs>(this, (MouseEventArgs e) => dismissed = true));
            parameters.Add(p => p.IsEnabled, false);
        });

        var dismissBtn = component.Find(".bit-tag-cls");
        Assert.IsNotNull(dismissBtn);

        dismissBtn.Click();

        Assert.IsFalse(dismissed);
    }

    [TestMethod]
    public void BitTagShouldRespectAriaLabel()
    {
        var aria = "my-aria";
        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, aria);
        });

        var root = component.Find(".bit-tag");
        Assert.AreEqual(aria, root.GetAttribute("aria-label"));
    }
}
