using System.Collections.Generic;
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
        // A tag lives inside a sentence, so its root has to be phrasing content rather than a block element.
        Assert.AreEqual("SPAN", root.TagName);
    }

    [TestMethod]
    public void BitTagShouldRenderContentElementAsPlainSpanWhenNotInteractive()
    {
        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.Text, "Label");
        });

        var content = component.Find(".bit-tag-cnt");

        Assert.AreEqual("SPAN", content.TagName);
        Assert.IsFalse(content.ClassList.Contains("bit-tag-int"));
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
    public void BitTagIconShouldBeHiddenFromAssistiveTechnologies()
    {
        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.IconName, "Calendar");
            parameters.Add(p => p.Text, "Calendar");
        });

        var icon = component.Find(".bit-tag-icn");
        Assert.AreEqual("true", icon.GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitTagShouldRenderIconWhenIconCssProvided()
    {
        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.Icon, BitIconInfo.Css("fa-solid fa-house"));
        });

        var icon = component.Find(".bit-tag-icn");
        Assert.IsNotNull(icon);
        Assert.IsTrue(icon.ClassList.Contains("fa-solid"));
        Assert.IsTrue(icon.ClassList.Contains("fa-house"));
    }

    [TestMethod]
    public void BitTagShouldRenderIconWhenIconFaProvided()
    {
        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.Icon, BitIconInfo.Fa("solid rocket"));
        });

        var icon = component.Find(".bit-tag-icn");
        Assert.IsNotNull(icon);
        Assert.IsTrue(icon.ClassList.Contains("fa-solid"));
        Assert.IsTrue(icon.ClassList.Contains("fa-rocket"));
    }

    [TestMethod]
    public void BitTagShouldRenderIconWhenIconBiProvided()
    {
        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.Icon, BitIconInfo.Bi("github"));
        });

        var icon = component.Find(".bit-tag-icn");
        Assert.IsNotNull(icon);
        Assert.IsTrue(icon.ClassList.Contains("bi"));
        Assert.IsTrue(icon.ClassList.Contains("bi-github"));
    }

    [TestMethod]
    public void BitTagIconShouldTakePrecedenceOverIconName()
    {
        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.Icon, BitIconInfo.Css("fa-solid fa-house"));
            parameters.Add(p => p.IconName, "Calendar");
        });

        var icon = component.Find(".bit-tag-icn");
        Assert.IsNotNull(icon);
        Assert.IsTrue(icon.ClassList.Contains("fa-solid"));
        Assert.IsTrue(icon.ClassList.Contains("fa-house"));
        Assert.IsFalse(icon.ClassList.Contains("bit-icon--Calendar"));
    }

    [TestMethod]
    public void BitTagShouldAcceptImplicitStringAsIcon()
    {
        // BitIconInfo has an implicit operator from string; the string becomes the raw CSS class
        BitIconInfo? iconInfo = "fa-solid fa-star";
        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.Icon, iconInfo);
        });

        var icon = component.Find(".bit-tag-icn");
        Assert.IsNotNull(icon);
        Assert.IsTrue(icon.ClassList.Contains("fa-solid"));
        Assert.IsTrue(icon.ClassList.Contains("fa-star"));
    }

    [TestMethod]
    public void BitTagShouldRenderImageWhenIconUrlProvided()
    {
        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.IconUrl, "/images/avatar.png");
        });

        var image = component.Find(".bit-tag-img");
        Assert.AreEqual("/images/avatar.png", image.GetAttribute("src"));
        // decorative next to the label, so it carries an empty alt rather than none at all
        Assert.AreEqual(string.Empty, image.GetAttribute("alt"));
    }

    [TestMethod]
    public void BitTagShouldRenderImageAltWhenProvided()
    {
        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.IconUrl, "/images/avatar.png");
            parameters.Add(p => p.IconAlt, "Annie");
        });

        Assert.AreEqual("Annie", component.Find(".bit-tag-img").GetAttribute("alt"));
    }

    [TestMethod]
    public void BitTagIconShouldTakePrecedenceOverIconUrl()
    {
        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.IconName, "Calendar");
            parameters.Add(p => p.IconUrl, "/images/avatar.png");
        });

        Assert.IsNotNull(component.Find(".bit-tag-icn"));
        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-tag-img"));
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
    public void BitTagShouldNotRenderLabelWhenTextIsEmpty()
    {
        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.Text, string.Empty);
        });

        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-tag-lbl"));
        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-tag-tex"));
    }

    [TestMethod]
    public void BitTagShouldRenderSecondaryTextWhenProvided()
    {
        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.Text, "Alex");
            parameters.Add(p => p.SecondaryText, "Designer");
        });

        Assert.AreEqual("Alex", component.Find(".bit-tag-tex").TextContent);
        Assert.AreEqual("Designer", component.Find(".bit-tag-stx").TextContent);
    }

    [TestMethod]
    public void BitTagShouldRenderSecondaryTextWithoutText()
    {
        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.SecondaryText, "Designer");
        });

        Assert.AreEqual("Designer", component.Find(".bit-tag-stx").TextContent);
        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-tag-tex"));
    }

    [TestMethod]
    public void BitTagShouldRenderChildContentOverText()
    {
        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.Text, "ShouldNotRender");
            parameters.Add(p => p.SecondaryText, "ShouldNotRenderEither");
            parameters.AddChildContent("<span class=\"custom\">Custom</span>");
        });

        Assert.IsNotNull(component.Find(".custom"));

        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-tag-lbl"));
        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-tag-tex"));
        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-tag-stx"));
    }

    [TestMethod]
    public void BitTagShouldKeepIconNextToChildContent()
    {
        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.IconName, "Calendar");
            parameters.AddChildContent("<span class=\"custom\">Custom</span>");
        });

        // a template replaces the label, not the glyph in front of it
        Assert.IsNotNull(component.Find(".bit-tag-icn"));
        Assert.IsNotNull(component.Find(".custom"));
    }

    [TestMethod]
    [DataRow(BitColor.Primary, "bit-tag-pri")]
    [DataRow(BitColor.Secondary, "bit-tag-sec")]
    [DataRow(BitColor.Tertiary, "bit-tag-ter")]
    [DataRow(BitColor.Info, "bit-tag-inf")]
    [DataRow(BitColor.Success, "bit-tag-suc")]
    [DataRow(BitColor.Warning, "bit-tag-wrn")]
    [DataRow(BitColor.SevereWarning, "bit-tag-swr")]
    [DataRow(BitColor.Error, "bit-tag-err")]
    [DataRow(BitColor.PrimaryBackground, "bit-tag-pbg")]
    [DataRow(BitColor.SecondaryBackground, "bit-tag-sbg")]
    [DataRow(BitColor.TertiaryBackground, "bit-tag-tbg")]
    [DataRow(BitColor.PrimaryForeground, "bit-tag-pfg")]
    [DataRow(BitColor.SecondaryForeground, "bit-tag-sfg")]
    [DataRow(BitColor.TertiaryForeground, "bit-tag-tfg")]
    [DataRow(BitColor.PrimaryBorder, "bit-tag-pbr")]
    [DataRow(BitColor.SecondaryBorder, "bit-tag-sbr")]
    [DataRow(BitColor.TertiaryBorder, "bit-tag-tbr")]
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
    [DataRow(BitTagShape.Rounded, "bit-tag-rnd")]
    [DataRow(BitTagShape.Circular, "bit-tag-cir")]
    [DataRow(BitTagShape.Square, "bit-tag-sqr")]
    [DataRow(null, "bit-tag-rnd")]
    public void BitTagShouldRespectShape(BitTagShape? shape, string expectedClass)
    {
        var component = RenderComponent<BitTag>(parameters =>
        {
            if (shape.HasValue) parameters.Add(p => p.Shape, shape.Value);
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
    public void BitTagShouldRespectNoWrap(bool noWrap)
    {
        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.NoWrap, noWrap);
        });

        var root = component.Find(".bit-tag");
        Assert.AreEqual(noWrap, root.ClassList.Contains("bit-tag-nwr"));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitTagShouldRespectFullWidth(bool fullWidth)
    {
        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.FullWidth, fullWidth);
        });

        var root = component.Find(".bit-tag");
        Assert.AreEqual(fullWidth, root.ClassList.Contains("bit-tag-flw"));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitTagShouldRespectIsEnabled(bool isEnabled)
    {
        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var root = component.Find(".bit-tag");
        Assert.AreEqual(isEnabled is false, root.ClassList.Contains("bit-dis"));
    }

    [TestMethod]
    [DataRow(BitVisibility.Visible, "")]
    [DataRow(BitVisibility.Hidden, "visibility:hidden")]
    [DataRow(BitVisibility.Collapsed, "display:none")]
    public void BitTagShouldRespectVisibility(BitVisibility visibility, string expectedStyle)
    {
        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.Visibility, visibility);
        });

        var root = component.Find(".bit-tag");
        Assert.AreEqual(expectedStyle, root.GetAttribute("style") ?? string.Empty);
    }

    [TestMethod]
    public void BitTagShouldRespectDir()
    {
        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Rtl);
        });

        var root = component.Find(".bit-tag");
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
    }

    [TestMethod]
    public void BitTagShouldRespectTitle()
    {
        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.Title, "The whole label");
        });

        Assert.AreEqual("The whole label", component.Find(".bit-tag").GetAttribute("title"));
    }

    [TestMethod]
    [DataRow("data-role", "chip")]
    [DataRow("aria-describedby", "hint-1")]
    public void BitTagShouldRespectArbitraryHtmlAttributes(string name, string value)
    {
        // Arbitrary HTML attributes are captured by BitComponentBase from unmatched parameters, so
        // supply them as raw component attributes (as real markup would) rather than via the builder,
        // which rejects unmatched params on components without [Parameter(CaptureUnmatchedValues)].
        var component = Context.Render(builder =>
        {
            builder.OpenComponent<BitTag>(0);
            builder.AddAttribute(1, name, value);
            builder.CloseComponent();
        });

        Assert.AreEqual(value, component.Find(".bit-tag").GetAttribute(name));
    }


    [TestMethod]
    public void BitTagShouldBecomeAButtonWhenOnClickIsSet()
    {
        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, (MouseEventArgs e) => { }));
        });

        var content = component.Find(".bit-tag-cnt");

        Assert.AreEqual("BUTTON", content.TagName);
        Assert.AreEqual("button", content.GetAttribute("type"));
        Assert.IsTrue(content.ClassList.Contains("bit-tag-int"));
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

        var content = component.Find(".bit-tag-cnt");
        content.Click();

        Assert.AreEqual(isEnabled, clicked);
    }

    [TestMethod]
    public void BitTagButtonShouldBeDisabledWhenTagIsDisabled()
    {
        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, (MouseEventArgs e) => { }));
        });

        Assert.IsTrue(component.Find(".bit-tag-cnt").HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitTagShouldRespectTabIndex()
    {
        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.TabIndex, "3");
            parameters.Add(p => p.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, (MouseEventArgs e) => { }));
        });

        Assert.AreEqual("3", component.Find(".bit-tag-cnt").GetAttribute("tabindex"));
    }


    [TestMethod]
    public void BitTagShouldBecomeAnAnchorWhenHrefIsSet()
    {
        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.Href, "/somewhere");
        });

        var content = component.Find(".bit-tag-cnt");

        Assert.AreEqual("A", content.TagName);
        Assert.AreEqual("/somewhere", content.GetAttribute("href"));
        Assert.IsTrue(content.ClassList.Contains("bit-tag-int"));
    }

    [TestMethod]
    public void BitTagLinkShouldRespectTarget()
    {
        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.Href, "https://bitplatform.dev");
            parameters.Add(p => p.Target, "_blank");
        });

        var content = component.Find(".bit-tag-cnt");

        Assert.AreEqual("_blank", content.GetAttribute("target"));
        // protects against reverse-tabnabbing when no rel of its own was given
        Assert.AreEqual("noopener", content.GetAttribute("rel"));
    }

    [TestMethod]
    public void BitTagLinkShouldRespectExplicitRel()
    {
        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.Href, "https://bitplatform.dev");
            parameters.Add(p => p.Target, "_blank");
            parameters.Add(p => p.Rel, BitLinkRels.NoFollow | BitLinkRels.NoReferrer);
        });

        var rel = component.Find(".bit-tag-cnt").GetAttribute("rel");

        Assert.IsNotNull(rel);
        Assert.IsTrue(rel.Contains("nofollow"));
        Assert.IsTrue(rel.Contains("noreferrer"));
    }

    [TestMethod]
    public void BitTagLinkShouldNotGetRelForAFragmentHref()
    {
        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.Href, "#example1");
            parameters.Add(p => p.Target, "_blank");
        });

        Assert.IsNull(component.Find(".bit-tag-cnt").GetAttribute("rel"));
    }

    [TestMethod]
    public void BitTagDisabledLinkShouldDropHrefAndLeaveTheTabOrder()
    {
        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.Href, "https://bitplatform.dev");
            parameters.Add(p => p.IsEnabled, false);
        });

        var content = component.Find(".bit-tag-cnt");

        Assert.IsNull(content.GetAttribute("href"));
        Assert.AreEqual("-1", content.GetAttribute("tabindex"));
        Assert.AreEqual("true", content.GetAttribute("aria-disabled"));
    }

    [TestMethod]
    public void BitTagHrefShouldTakePrecedenceOverOnClickForTheRenderedElement()
    {
        var clicked = false;

        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.Href, "/somewhere");
            parameters.Add(p => p.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, (MouseEventArgs e) => clicked = true));
        });

        var content = component.Find(".bit-tag-cnt");
        Assert.AreEqual("A", content.TagName);

        // the handler still runs, and the navigation still happens
        content.Click();
        Assert.IsTrue(clicked);
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
        Assert.AreEqual("BUTTON", dismissBtn.TagName);

        dismissBtn.Click();

        Assert.IsTrue(dismissed);
    }

    [TestMethod]
    public void BitTagShouldNotRenderDismissButtonWithoutOnDismiss()
    {
        var component = RenderComponent<BitTag>();

        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-tag-cls"));
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
        Assert.IsTrue(dismissBtn.HasAttribute("disabled"));

        dismissBtn.Click();

        Assert.IsFalse(dismissed);
    }

    [TestMethod]
    public void BitTagDismissShouldNotTriggerTheClickOfTheTag()
    {
        var clicked = false;
        var dismissed = false;

        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, (MouseEventArgs e) => clicked = true));
            parameters.Add(p => p.OnDismiss, EventCallback.Factory.Create<MouseEventArgs>(this, (MouseEventArgs e) => dismissed = true));
        });

        component.Find(".bit-tag-cls").Click();

        Assert.IsTrue(dismissed);
        Assert.IsFalse(clicked);
    }

    [TestMethod]
    public void BitTagDismissIconShouldDefaultToCancel()
    {
        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.OnDismiss, EventCallback.Factory.Create<MouseEventArgs>(this, (MouseEventArgs e) => { }));
        });

        var dismissIcon = component.Find(".bit-tag-dsi");
        Assert.IsNotNull(dismissIcon);
        Assert.IsTrue(dismissIcon.ClassList.Contains("bit-icon"));
        Assert.IsTrue(dismissIcon.ClassList.Contains("bit-icon--Cancel"));
        Assert.AreEqual("true", dismissIcon.GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitTagShouldRenderCustomDismissIconNameWhenProvided()
    {
        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.OnDismiss, EventCallback.Factory.Create<MouseEventArgs>(this, (MouseEventArgs e) => { }));
            parameters.Add(p => p.DismissIconName, "ChromeClose");
        });

        var dismissIcon = component.Find(".bit-tag-dsi");
        Assert.IsNotNull(dismissIcon);
        Assert.IsTrue(dismissIcon.ClassList.Contains("bit-icon--ChromeClose"));
        Assert.IsFalse(dismissIcon.ClassList.Contains("bit-icon--Cancel"));
    }

    [TestMethod]
    public void BitTagShouldRenderCustomDismissIconWhenProvided()
    {
        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.OnDismiss, EventCallback.Factory.Create<MouseEventArgs>(this, (MouseEventArgs e) => { }));
            parameters.Add(p => p.DismissIcon, BitIconInfo.Css("fa-solid fa-xmark"));
        });

        var dismissIcon = component.Find(".bit-tag-dsi");
        Assert.IsNotNull(dismissIcon);
        Assert.IsTrue(dismissIcon.ClassList.Contains("fa-solid"));
        Assert.IsTrue(dismissIcon.ClassList.Contains("fa-xmark"));
        Assert.IsFalse(dismissIcon.ClassList.Contains("bit-icon--Cancel"));
    }

    [TestMethod]
    public void BitTagDismissIconShouldTakePrecedenceOverDismissIconName()
    {
        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.OnDismiss, EventCallback.Factory.Create<MouseEventArgs>(this, (MouseEventArgs e) => { }));
            parameters.Add(p => p.DismissIcon, BitIconInfo.Css("fa-solid fa-xmark"));
            parameters.Add(p => p.DismissIconName, "ChromeClose");
        });

        var dismissIcon = component.Find(".bit-tag-dsi");
        Assert.IsNotNull(dismissIcon);
        Assert.IsTrue(dismissIcon.ClassList.Contains("fa-solid"));
        Assert.IsTrue(dismissIcon.ClassList.Contains("fa-xmark"));
        Assert.IsFalse(dismissIcon.ClassList.Contains("bit-icon--ChromeClose"));
    }

    [TestMethod]
    public void BitTagDismissButtonShouldFallBackToADefaultName()
    {
        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.OnDismiss, EventCallback.Factory.Create<MouseEventArgs>(this, (MouseEventArgs e) => { }));
        });

        var dismissBtn = component.Find(".bit-tag-cls");
        Assert.AreEqual("Dismiss", dismissBtn.GetAttribute("aria-label"));
        Assert.AreEqual("Dismiss", dismissBtn.GetAttribute("title"));
    }

    [TestMethod]
    public void BitTagShouldRespectDismissLabel()
    {
        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.OnDismiss, EventCallback.Factory.Create<MouseEventArgs>(this, (MouseEventArgs e) => { }));
            parameters.Add(p => p.DismissLabel, "Remove the Design tag");
        });

        var dismissBtn = component.Find(".bit-tag-cls");
        Assert.AreEqual("Remove the Design tag", dismissBtn.GetAttribute("aria-label"));
        Assert.AreEqual("Remove the Design tag", dismissBtn.GetAttribute("title"));
    }


    [TestMethod]
    [DataRow("Delete")]
    [DataRow("Backspace")]
    public void BitTagShouldDismissFromTheKeyboard(string key)
    {
        var dismissed = false;

        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.OnDismiss, EventCallback.Factory.Create<MouseEventArgs>(this, (MouseEventArgs e) => dismissed = true));
        });

        component.Find(".bit-tag-cls").KeyDown(new KeyboardEventArgs { Key = key });

        Assert.IsTrue(dismissed);
    }

    [TestMethod]
    public void BitTagShouldDismissFromTheKeyboardWhileFocusIsOnTheTagItself()
    {
        var dismissed = false;

        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, (MouseEventArgs e) => { }));
            parameters.Add(p => p.OnDismiss, EventCallback.Factory.Create<MouseEventArgs>(this, (MouseEventArgs e) => dismissed = true));
        });

        component.Find(".bit-tag-cnt").KeyDown(new KeyboardEventArgs { Key = "Delete" });

        Assert.IsTrue(dismissed);
    }

    [TestMethod]
    [DataRow("Enter")]
    [DataRow(" ")]
    [DataRow("a")]
    public void BitTagShouldIgnoreOtherKeysForDismissal(string key)
    {
        var dismissed = false;

        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.OnDismiss, EventCallback.Factory.Create<MouseEventArgs>(this, (MouseEventArgs e) => dismissed = true));
        });

        component.Find(".bit-tag-cls").KeyDown(new KeyboardEventArgs { Key = key });

        Assert.IsFalse(dismissed);
    }

    [TestMethod]
    public void BitTagShouldNotDismissFromTheKeyboardWhenDisabled()
    {
        var dismissed = false;

        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.OnDismiss, EventCallback.Factory.Create<MouseEventArgs>(this, (MouseEventArgs e) => dismissed = true));
        });

        component.Find(".bit-tag-cls").KeyDown(new KeyboardEventArgs { Key = "Delete" });

        Assert.IsFalse(dismissed);
    }


    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitTagShouldRespectSelected(bool selected)
    {
        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.Selected, selected);
        });

        var root = component.Find(".bit-tag");
        Assert.AreEqual(selected, root.ClassList.Contains("bit-tag-sel"));
    }

    [TestMethod]
    public void BitTagSelectedShouldRenderTheCheckmark()
    {
        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.Selected, true);
        });

        var checkmark = component.Find(".bit-tag-sic");
        Assert.IsTrue(checkmark.ClassList.Contains("bit-icon--Accept"));
        Assert.AreEqual("true", checkmark.GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitTagUnselectedShouldNotRenderTheCheckmark()
    {
        var component = RenderComponent<BitTag>();

        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-tag-sic"));
    }

    [TestMethod]
    public void BitTagShouldRespectHideSelectedIcon()
    {
        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.Selected, true);
            parameters.Add(p => p.HideSelectedIcon, true);
        });

        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-tag-sic"));
    }

    [TestMethod]
    public void BitTagShouldRespectSelectedIconName()
    {
        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.Selected, true);
            parameters.Add(p => p.SelectedIconName, "FavoriteStarFill");
        });

        var checkmark = component.Find(".bit-tag-sic");
        Assert.IsTrue(checkmark.ClassList.Contains("bit-icon--FavoriteStarFill"));
        Assert.IsFalse(checkmark.ClassList.Contains("bit-icon--Accept"));
    }

    [TestMethod]
    public void BitTagSelectedIconShouldTakePrecedenceOverSelectedIconName()
    {
        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.Selected, true);
            parameters.Add(p => p.SelectedIcon, BitIconInfo.Css("fa-solid fa-check"));
            parameters.Add(p => p.SelectedIconName, "FavoriteStarFill");
        });

        var checkmark = component.Find(".bit-tag-sic");
        Assert.IsTrue(checkmark.ClassList.Contains("fa-check"));
        Assert.IsFalse(checkmark.ClassList.Contains("bit-icon--FavoriteStarFill"));
    }

    [TestMethod]
    public void BitTagSelectedAloneShouldNotMakeTheTagAControl()
    {
        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.Selected, true);
        });

        var content = component.Find(".bit-tag-cnt");

        Assert.AreEqual("SPAN", content.TagName);
        Assert.IsNull(content.GetAttribute("aria-pressed"));
    }

    [TestMethod]
    public void BitTagShouldBecomeAToggleWhenSelectedIsBound()
    {
        var selected = false;

        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.Selected, selected);
            parameters.Add(p => p.SelectedChanged, EventCallback.Factory.Create<bool>(this, (bool v) => selected = v));
        });

        var content = component.Find(".bit-tag-cnt");

        Assert.AreEqual("BUTTON", content.TagName);
        Assert.AreEqual("false", content.GetAttribute("aria-pressed"));

        content.Click();

        Assert.IsTrue(selected);
        Assert.AreEqual("true", component.Find(".bit-tag-cnt").GetAttribute("aria-pressed"));
        Assert.IsTrue(component.Find(".bit-tag").ClassList.Contains("bit-tag-sel"));
    }

    [TestMethod]
    public void BitTagToggleShouldInvokeOnChange()
    {
        bool? changed = null;

        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.OnChange, EventCallback.Factory.Create<bool>(this, (bool v) => changed = v));
        });

        component.Find(".bit-tag-cnt").Click();

        Assert.AreEqual(true, changed);
    }

    [TestMethod]
    public void BitTagToggleShouldNotChangeWhenDisabled()
    {
        var selected = false;

        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.Selected, selected);
            parameters.Add(p => p.SelectedChanged, EventCallback.Factory.Create<bool>(this, (bool v) => selected = v));
        });

        component.Find(".bit-tag-cnt").Click();

        Assert.IsFalse(selected);
    }

    [TestMethod]
    public void BitTagToggleShouldAlsoInvokeOnClick()
    {
        var clicked = false;
        var selected = false;

        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, (MouseEventArgs e) => clicked = true));
            parameters.Add(p => p.Selected, selected);
            parameters.Add(p => p.SelectedChanged, EventCallback.Factory.Create<bool>(this, (bool v) => selected = v));
        });

        component.Find(".bit-tag-cnt").Click();

        Assert.IsTrue(clicked);
        Assert.IsTrue(selected);
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

    [TestMethod]
    public void BitTagAriaLabelShouldMoveOntoTheControlWhenTheTagIsOne()
    {
        var aria = "Show the filters";

        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, aria);
            parameters.Add(p => p.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, (MouseEventArgs e) => { }));
        });

        // a control is what a screen reader lands on, so the name belongs on it rather than on the wrapper
        Assert.IsNull(component.Find(".bit-tag").GetAttribute("aria-label"));
        Assert.AreEqual(aria, component.Find(".bit-tag-cnt").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitTagAriaLabelShouldMoveOntoTheAnchorWhenTheTagIsALink()
    {
        var aria = "Open the docs";

        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, aria);
            parameters.Add(p => p.Href, "https://bitplatform.dev");
        });

        Assert.IsNull(component.Find(".bit-tag").GetAttribute("aria-label"));
        Assert.AreEqual(aria, component.Find(".bit-tag-cnt").GetAttribute("aria-label"));
    }


    [TestMethod]
    public void BitTagShouldRespectStyleAndClass()
    {
        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.Style, "color: red");
            parameters.Add(p => p.Class, "custom-class");
        });

        var root = component.Find(".bit-tag");

        Assert.AreEqual("color: red", root.GetAttribute("style"));
        Assert.IsTrue(root.ClassList.Contains("custom-class"));
    }

    [TestMethod]
    public void BitTagShouldRespectClassesAndStyles()
    {
        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.Text, "Alex");
            parameters.Add(p => p.SecondaryText, "Designer");
            parameters.Add(p => p.IconName, "Contact");
            parameters.Add(p => p.OnDismiss, EventCallback.Factory.Create<MouseEventArgs>(this, (MouseEventArgs e) => { }));
            parameters.Add(p => p.Classes, new BitTagClassStyles
            {
                Root = "custom-root",
                Content = "custom-content",
                Label = "custom-label",
                Text = "custom-text",
                SecondaryText = "custom-secondary",
                Icon = "custom-icon",
                DismissButton = "custom-dismiss",
                DismissIcon = "custom-dismiss-icon"
            });
            parameters.Add(p => p.Styles, new BitTagClassStyles
            {
                Root = "color: red",
                Content = "color: green",
                Label = "color: blue",
                Text = "color: tomato",
                SecondaryText = "color: gray",
                Icon = "color: purple",
                DismissButton = "color: brown",
                DismissIcon = "color: black"
            });
        });

        Assert.IsTrue(component.Find(".bit-tag").ClassList.Contains("custom-root"));
        Assert.IsTrue(component.Find(".bit-tag-cnt").ClassList.Contains("custom-content"));
        Assert.IsTrue(component.Find(".bit-tag-lbl").ClassList.Contains("custom-label"));
        Assert.IsTrue(component.Find(".bit-tag-tex").ClassList.Contains("custom-text"));
        Assert.IsTrue(component.Find(".bit-tag-stx").ClassList.Contains("custom-secondary"));
        Assert.IsTrue(component.Find(".bit-tag-icn").ClassList.Contains("custom-icon"));
        Assert.IsTrue(component.Find(".bit-tag-cls").ClassList.Contains("custom-dismiss"));
        Assert.IsTrue(component.Find(".bit-tag-dsi").ClassList.Contains("custom-dismiss-icon"));

        Assert.AreEqual("color: red", component.Find(".bit-tag").GetAttribute("style"));
        Assert.AreEqual("color: green", component.Find(".bit-tag-cnt").GetAttribute("style"));
        Assert.AreEqual("color: blue", component.Find(".bit-tag-lbl").GetAttribute("style"));
        Assert.AreEqual("color: tomato", component.Find(".bit-tag-tex").GetAttribute("style"));
        Assert.AreEqual("color: gray", component.Find(".bit-tag-stx").GetAttribute("style"));
        Assert.AreEqual("color: purple", component.Find(".bit-tag-icn").GetAttribute("style"));
        Assert.AreEqual("color: brown", component.Find(".bit-tag-cls").GetAttribute("style"));
        Assert.AreEqual("color: black", component.Find(".bit-tag-dsi").GetAttribute("style"));
    }

    [TestMethod]
    public void BitTagShouldRespectSelectedClassesAndStyles()
    {
        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.Selected, true);
            parameters.Add(p => p.Classes, new BitTagClassStyles { Selected = "custom-selected", SelectedIcon = "custom-selected-icon" });
            parameters.Add(p => p.Styles, new BitTagClassStyles { Selected = "color: deeppink", SelectedIcon = "color: gold" });
        });

        var root = component.Find(".bit-tag");
        Assert.IsTrue(root.ClassList.Contains("custom-selected"));
        Assert.AreEqual("color: deeppink", root.GetAttribute("style"));

        var checkmark = component.Find(".bit-tag-sic");
        Assert.IsTrue(checkmark.ClassList.Contains("custom-selected-icon"));
        Assert.AreEqual("color: gold", checkmark.GetAttribute("style"));
    }

    [TestMethod]
    public void BitTagShouldRespectImageClassesAndStyles()
    {
        var component = RenderComponent<BitTag>(parameters =>
        {
            parameters.Add(p => p.IconUrl, "/images/avatar.png");
            parameters.Add(p => p.Classes, new BitTagClassStyles { Image = "custom-image" });
            parameters.Add(p => p.Styles, new BitTagClassStyles { Image = "opacity: 0.5" });
        });

        var image = component.Find(".bit-tag-img");
        Assert.IsTrue(image.ClassList.Contains("custom-image"));
        Assert.AreEqual("opacity: 0.5", image.GetAttribute("style"));
    }


    [TestMethod]
    public void BitTagParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitTag)}", BitTagParams.ParamName);
    }

    [TestMethod]
    public void BitTagParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitTagParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitTagParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitTagShouldApplyCascadingParametersFromBitParams()
    {
        var paramsList = new List<IBitComponentParams>
        {
            new BitTagParams
            {
                Color = BitColor.Success,
                Size = BitSize.Large,
                Shape = BitTagShape.Circular,
                Variant = BitVariant.Outline,
                IconName = "Add",
                Text = "Cascaded",
                NoWrap = true,
                FullWidth = true,
                Reversed = true,
                DismissIconName = "ChromeClose",
                DismissLabel = "Cascaded dismiss"
            }
        };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, paramsList);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitTag>(0);
                builder.AddAttribute(1, nameof(BitTag.OnDismiss), EventCallback.Factory.Create<MouseEventArgs>(this, (MouseEventArgs e) => { }));
                builder.CloseComponent();
            });
        });

        var root = component.Find(".bit-tag");

        Assert.IsTrue(root.ClassList.Contains("bit-tag-suc"));
        Assert.IsTrue(root.ClassList.Contains("bit-tag-lg"));
        Assert.IsTrue(root.ClassList.Contains("bit-tag-cir"));
        Assert.IsTrue(root.ClassList.Contains("bit-tag-otl"));
        Assert.IsTrue(root.ClassList.Contains("bit-tag-nwr"));
        Assert.IsTrue(root.ClassList.Contains("bit-tag-flw"));
        Assert.IsTrue(root.ClassList.Contains("bit-tag-rvs"));

        Assert.AreEqual("Cascaded", component.Find(".bit-tag-tex").TextContent);
        Assert.IsTrue(component.Find(".bit-tag-icn").ClassList.Contains("bit-icon--Add"));
        Assert.IsTrue(component.Find(".bit-tag-dsi").ClassList.Contains("bit-icon--ChromeClose"));
        Assert.AreEqual("Cascaded dismiss", component.Find(".bit-tag-cls").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitTagOwnParametersShouldWinOverTheCascadedOnes()
    {
        var paramsList = new List<IBitComponentParams>
        {
            new BitTagParams { Color = BitColor.Success, Text = "Cascaded" }
        };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, paramsList);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitTag>(0);
                builder.AddAttribute(1, nameof(BitTag.Color), BitColor.Error);
                builder.AddAttribute(2, nameof(BitTag.Text), "Own");
                builder.CloseComponent();
            });
        });

        var root = component.Find(".bit-tag");

        Assert.IsTrue(root.ClassList.Contains("bit-tag-err"));
        Assert.IsFalse(root.ClassList.Contains("bit-tag-suc"));
        Assert.AreEqual("Own", component.Find(".bit-tag-tex").TextContent);
    }
}
