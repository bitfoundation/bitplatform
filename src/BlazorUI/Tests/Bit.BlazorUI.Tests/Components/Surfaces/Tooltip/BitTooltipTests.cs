using System;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Tooltip;

[TestClass]
public class BitTooltipTests : BunitTestContext
{
    private static RenderFragment Markup(string html) => builder => builder.AddMarkupContent(0, html);

    private static PointerEventArgs Mouse() => new() { PointerType = "mouse" };

    private static PointerEventArgs Touch() => new() { PointerType = "touch" };



    [TestMethod]
    public void BitTooltipShouldRenderTextAndRespectDefaultIsShown()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "This is a tooltip");
        });

        var wrapper = component.Find(".bit-ttp-wrp");

        // By default tooltip should not be visible
        Assert.IsFalse(wrapper.ClassList.Contains("bit-ttp-vis"));
        Assert.IsTrue(component.Find(".bit-ttp-ctn").TextContent.Contains("This is a tooltip"));

        var component2 = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Shown tooltip");
            parameters.Add(p => p.DefaultIsShown, true);
        });

        var wrapper2 = component2.Find(".bit-ttp-wrp");

        Assert.IsTrue(wrapper2.ClassList.Contains("bit-ttp-vis"));
    }

    [TestMethod]
    public void BitTooltipShouldRenderTemplateContent()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Template, Markup("<span class=\"tpl\">TemplateContent</span>"));
        });

        var tooltip = component.Find(".bit-ttp-ctn");

        Assert.IsTrue(tooltip.OuterHtml.Contains("TemplateContent"));
    }

    [TestMethod]
    public void BitTooltipTemplateShouldWinOverText()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "PlainText");
            parameters.Add(p => p.Template, Markup("<span>TemplateContent</span>"));
        });

        var tooltip = component.Find(".bit-ttp-ctn");

        Assert.IsTrue(tooltip.TextContent.Contains("TemplateContent"));
        Assert.IsFalse(tooltip.TextContent.Contains("PlainText"));
    }

    [TestMethod]
    public void BitTooltipShouldRenderAnchorWhenProvided()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Anchor, Markup("<button class=\"anchor-btn\">Anchor</button>"));
        });

        var anchor = component.Find(".anchor-btn");

        Assert.IsNotNull(anchor);
        Assert.IsTrue(anchor.OuterHtml.Contains("Anchor"));
    }

    [TestMethod]
    public void BitTooltipAnchorShouldWinOverChildContent()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.AddChildContent("<button class=\"child\">Child</button>");
            parameters.Add(p => p.Anchor, Markup("<button class=\"anchor-btn\">Anchor</button>"));
        });

        Assert.AreEqual(1, component.FindAll(".anchor-btn").Count);
        Assert.AreEqual(0, component.FindAll(".child").Count);
    }



    [DataTestMethod]
    [DataRow(BitTooltipPosition.Top, "bit-ttp-top")]
    [DataRow(BitTooltipPosition.TopLeft, "bit-ttp-tlf")]
    [DataRow(BitTooltipPosition.TopRight, "bit-ttp-trg")]
    [DataRow(BitTooltipPosition.RightTop, "bit-ttp-rtp")]
    [DataRow(BitTooltipPosition.Right, "bit-ttp-rgt")]
    [DataRow(BitTooltipPosition.RightBottom, "bit-ttp-rbm")]
    [DataRow(BitTooltipPosition.BottomRight, "bit-ttp-brg")]
    [DataRow(BitTooltipPosition.Bottom, "bit-ttp-btm")]
    [DataRow(BitTooltipPosition.BottomLeft, "bit-ttp-blf")]
    [DataRow(BitTooltipPosition.LeftBottom, "bit-ttp-lbm")]
    [DataRow(BitTooltipPosition.Left, "bit-ttp-lft")]
    [DataRow(BitTooltipPosition.LeftTop, "bit-ttp-ltp")]
    public void BitTooltipShouldRespectPosition(BitTooltipPosition position, string expectedClass)
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.Position, position);
        });

        Assert.IsTrue(component.Find(".bit-ttp-wrp").ClassList.Contains(expectedClass));
    }

    [TestMethod]
    public void BitTooltipShouldChangePositionClassWhenPositionChanges()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
        });

        Assert.IsTrue(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-top"));

        component.Render(parameters => parameters.Add(p => p.Position, BitTooltipPosition.Bottom));

        Assert.IsTrue(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-btm"));
    }



    [TestMethod]
    public void BitTooltipShouldRenderArrowUnlessHidden()
    {
        var component = RenderComponent<BitTooltip>(parameters => parameters.Add(p => p.Text, "Tip"));

        Assert.AreEqual(1, component.FindAll(".bit-ttp-arw").Count);

        component.Render(parameters => parameters.Add(p => p.HideArrow, true));

        Assert.AreEqual(0, component.FindAll(".bit-ttp-arw").Count);
    }



    [DataTestMethod]
    [DataRow(BitColor.Primary, "bit-ttp-pri")]
    [DataRow(BitColor.Secondary, "bit-ttp-sec")]
    [DataRow(BitColor.Tertiary, "bit-ttp-ter")]
    [DataRow(BitColor.Info, "bit-ttp-inf")]
    [DataRow(BitColor.Success, "bit-ttp-suc")]
    [DataRow(BitColor.Warning, "bit-ttp-wrn")]
    [DataRow(BitColor.SevereWarning, "bit-ttp-swr")]
    [DataRow(BitColor.Error, "bit-ttp-err")]
    [DataRow(BitColor.PrimaryBackground, "bit-ttp-pbg")]
    [DataRow(BitColor.SecondaryBackground, "bit-ttp-sbg")]
    [DataRow(BitColor.TertiaryBackground, "bit-ttp-tbg")]
    [DataRow(BitColor.PrimaryForeground, "bit-ttp-pfg")]
    [DataRow(BitColor.SecondaryForeground, "bit-ttp-sfg")]
    [DataRow(BitColor.TertiaryForeground, "bit-ttp-tfg")]
    [DataRow(BitColor.PrimaryBorder, "bit-ttp-pbr")]
    [DataRow(BitColor.SecondaryBorder, "bit-ttp-sbr")]
    [DataRow(BitColor.TertiaryBorder, "bit-ttp-tbr")]
    public void BitTooltipShouldRespectColor(BitColor color, string expectedClass)
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.Color, color);
        });

        Assert.IsTrue(component.Find(".bit-ttp").ClassList.Contains(expectedClass));
    }

    [DataTestMethod]
    [DataRow(BitSize.Small, "bit-ttp-sm")]
    [DataRow(BitSize.Medium, "bit-ttp-md")]
    [DataRow(BitSize.Large, "bit-ttp-lg")]
    public void BitTooltipShouldRespectSize(BitSize size, string expectedClass)
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.Size, size);
        });

        Assert.IsTrue(component.Find(".bit-ttp").ClassList.Contains(expectedClass));
    }

    [TestMethod]
    public void BitTooltipShouldRenderNoColorOrSizeClassByDefault()
    {
        var component = RenderComponent<BitTooltip>(parameters => parameters.Add(p => p.Text, "Tip"));

        var css = component.Find(".bit-ttp").ClassList;

        Assert.IsFalse(css.Contains("bit-ttp-pri"));
        Assert.IsFalse(css.Contains("bit-ttp-sm"));
        Assert.IsFalse(css.Contains("bit-ttp-md"));
        Assert.IsFalse(css.Contains("bit-ttp-lg"));
    }

    [TestMethod]
    public void BitTooltipShouldRespectInteractiveAndNoAnimation()
    {
        var component = RenderComponent<BitTooltip>(parameters => parameters.Add(p => p.Text, "Tip"));

        Assert.IsFalse(component.Find(".bit-ttp").ClassList.Contains("bit-ttp-itr"));
        Assert.IsFalse(component.Find(".bit-ttp").ClassList.Contains("bit-ttp-nan"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Interactive, true);
            parameters.Add(p => p.NoAnimation, true);
        });

        Assert.IsTrue(component.Find(".bit-ttp").ClassList.Contains("bit-ttp-itr"));
        Assert.IsTrue(component.Find(".bit-ttp").ClassList.Contains("bit-ttp-nan"));
    }

    [TestMethod]
    public void BitTooltipShouldRenderTheMeasurementsAsCustomProperties()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.Offset, 24);
            parameters.Add(p => p.ArrowSize, 18);
            parameters.Add(p => p.MaxWidth, "10rem");
        });

        var style = component.Find(".bit-ttp").GetAttribute("style");

        StringAssert.Contains(style, "--bit-ttp-offset:24px");
        StringAssert.Contains(style, "--bit-ttp-arrow-size:18px");
        StringAssert.Contains(style, "--bit-ttp-max-width:10rem");
    }

    [TestMethod]
    public void BitTooltipShouldRebuildTheMeasurementsWhenTheyChange()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.MaxWidth, "10rem");
        });

        StringAssert.Contains(component.Find(".bit-ttp").GetAttribute("style"), "--bit-ttp-max-width:10rem");

        component.Render(parameters => parameters.Add(p => p.MaxWidth, "none"));

        StringAssert.Contains(component.Find(".bit-ttp").GetAttribute("style"), "--bit-ttp-max-width:none");
    }

    [TestMethod]
    public void BitTooltipShouldNotRenderTheMeasurementsWhenNotAsked()
    {
        var component = RenderComponent<BitTooltip>(parameters => parameters.Add(p => p.Text, "Tip"));

        var style = component.Find(".bit-ttp").GetAttribute("style") ?? string.Empty;

        Assert.IsFalse(style.Contains("--bit-ttp-offset"));
        Assert.IsFalse(style.Contains("--bit-ttp-arrow-size"));
        Assert.IsFalse(style.Contains("--bit-ttp-max-width"));
    }



    [TestMethod]
    public void BitTooltipShouldCarryTheTooltipRoleAndDescribeItsAnchor()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
        });

        var root = component.Find(".bit-ttp");
        var tooltip = component.Find(".bit-ttp-ctn");

        Assert.AreEqual("tooltip", tooltip.GetAttribute("role"));
        Assert.IsFalse(string.IsNullOrEmpty(tooltip.Id));

        // The description is there before the tooltip is, so a screen reader has the text the moment the
        // anchor is reached rather than racing the render that shows it.
        Assert.AreEqual(tooltip.Id, root.GetAttribute("aria-describedby"));

        component.Find(".bit-ttp").TriggerEvent("onpointerenter", Mouse());

        Assert.AreEqual(component.Find(".bit-ttp-ctn").Id, component.Find(".bit-ttp").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitTooltipWithoutContentShouldDescribeNothing()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.DefaultIsShown, true);
        });

        Assert.IsNull(component.Find(".bit-ttp").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitTooltipShouldRespectId()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Id, "the-tip");
            parameters.Add(p => p.Text, "Tip");
        });

        Assert.AreEqual("the-tip-ttp", component.Find(".bit-ttp-ctn").Id);
    }

    [TestMethod]
    public void BitTooltipShouldRespectTabIndex()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.TabIndex, "0");
        });

        Assert.AreEqual("0", component.Find(".bit-ttp").GetAttribute("tabindex"));
    }



    [TestMethod]
    public void BitTooltipShouldShowOnPointerEnterAndHideOnPointerLeave()
    {
        var component = RenderComponent<BitTooltip>(parameters => parameters.Add(p => p.Text, "Tip"));

        var root = component.Find(".bit-ttp");

        root.TriggerEvent("onpointerenter", Mouse());
        Assert.IsTrue(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));

        component.Find(".bit-ttp").TriggerEvent("onpointerleave", Mouse());
        Assert.IsFalse(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
    }

    [TestMethod]
    public void BitTooltipShowOnHoverFalseShouldIgnoreThePointer()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.ShowOnHover, false);
        });

        component.Find(".bit-ttp").TriggerEvent("onpointerenter", Mouse());

        Assert.IsFalse(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
    }

    [TestMethod]
    public void BitTooltipShouldShowOnFocusByDefault()
    {
        var component = RenderComponent<BitTooltip>(parameters => parameters.Add(p => p.Text, "Tip"));

        component.Find(".bit-ttp").TriggerEvent("onfocusin", new FocusEventArgs());
        Assert.IsTrue(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));

        component.Find(".bit-ttp").TriggerEvent("onfocusout", new FocusEventArgs());
        Assert.IsFalse(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
    }

    [TestMethod]
    public void BitTooltipShowOnFocusFalseShouldIgnoreTheKeyboard()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.ShowOnFocus, false);
        });

        component.Find(".bit-ttp").TriggerEvent("onfocusin", new FocusEventArgs());

        Assert.IsFalse(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
    }

    [TestMethod]
    public void BitTooltipShowOnClickShouldToggleVisibilityOnPointerUp()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Click tooltip");
            parameters.Add(p => p.ShowOnClick, true);
        });

        var root = component.Find(".bit-ttp");
        var wrapper = component.Find(".bit-ttp-wrp");

        // initial hidden
        Assert.IsFalse(wrapper.ClassList.Contains("bit-ttp-vis"));

        // trigger pointer up (click)
        root.TriggerEvent("onpointerup", new PointerEventArgs());

        // after click it should be visible
        wrapper = component.Find(".bit-ttp-wrp");
        Assert.IsTrue(wrapper.ClassList.Contains("bit-ttp-vis"));

        // trigger again to hide
        component.Find(".bit-ttp").TriggerEvent("onpointerup", new PointerEventArgs());

        wrapper = component.Find(".bit-ttp-wrp");
        Assert.IsFalse(wrapper.ClassList.Contains("bit-ttp-vis"));
    }

    [TestMethod]
    public void BitTooltipShouldIgnoreTheSecondaryButton()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.ShowOnClick, true);
        });

        component.Find(".bit-ttp").TriggerEvent("onpointerup", new PointerEventArgs { Button = 2 });

        Assert.IsFalse(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
    }

    [TestMethod]
    public void BitTooltipShowOnClickFalseShouldIgnoreThePointerUp()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.ShowOnHover, false);
            parameters.Add(p => p.ShowOnFocus, false);
        });

        component.Find(".bit-ttp").TriggerEvent("onpointerup", new PointerEventArgs());

        Assert.IsFalse(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
    }



    [TestMethod]
    public void BitTooltipShownByATouchShouldSurviveTheLeaveThatFollowsIt()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            // The timer that hides a tooltip shown by a touch is turned off, so this test is about
            // the leave alone.
            parameters.Add(p => p.TouchHideDelay, 0);
        });

        component.Find(".bit-ttp").TriggerEvent("onpointerenter", Touch());
        Assert.IsTrue(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));

        component.Find(".bit-ttp").TriggerEvent("onpointerleave", Touch());
        Assert.IsTrue(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
    }

    [TestMethod]
    public void BitTooltipShownByATouchShouldHideItselfAfterTheTouchHideDelay()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.TouchHideDelay, 50);
        });

        component.Find(".bit-ttp").TriggerEvent("onpointerenter", Touch());

        component.WaitForAssertion(() =>
            Assert.IsFalse(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis")));
    }



    [TestMethod]
    public void BitTooltipShouldWaitOutTheShowDelay()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.ShowDelay, 50);
        });

        component.Find(".bit-ttp").TriggerEvent("onpointerenter", Mouse());

        Assert.IsFalse(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));

        component.WaitForAssertion(() =>
            Assert.IsTrue(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis")));
    }

    [TestMethod]
    public async Task BitTooltipShouldCancelAPendingShowWhenThePointerLeavesFirst()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.ShowDelay, 100);
        });

        component.Find(".bit-ttp").TriggerEvent("onpointerenter", Mouse());
        component.Find(".bit-ttp").TriggerEvent("onpointerleave", Mouse());

        await Task.Delay(250);

        Assert.IsFalse(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
    }

    [TestMethod]
    public async Task BitTooltipShouldCancelAPendingHideWhenThePointerComesBack()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.HideDelay, 100);
        });

        component.Find(".bit-ttp").TriggerEvent("onpointerenter", Mouse());
        component.Find(".bit-ttp").TriggerEvent("onpointerleave", Mouse());
        component.Find(".bit-ttp").TriggerEvent("onpointerenter", Mouse());

        await Task.Delay(250);

        Assert.IsTrue(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
    }

    [TestMethod]
    public void BitTooltipShouldNotDelayAShowThatTheKeyboardAsksFor()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.ShowDelay, 5000);
        });

        component.Find(".bit-ttp").TriggerEvent("onfocusin", new FocusEventArgs());

        Assert.IsTrue(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
    }



    [TestMethod]
    public void BitTooltipShouldBeDismissedByEscape()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.DefaultIsShown, true);
        });

        component.Find(".bit-ttp").TriggerEvent("onkeydown", new KeyboardEventArgs { Key = "Escape" });

        Assert.IsFalse(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
    }

    [TestMethod]
    public void BitTooltipShouldIgnoreOtherKeys()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.DefaultIsShown, true);
        });

        component.Find(".bit-ttp").TriggerEvent("onkeydown", new KeyboardEventArgs { Key = "Enter" });

        Assert.IsTrue(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
    }

    [TestMethod]
    public void BitTooltipNoDismissOnEscapeShouldKeepTheTooltip()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.DefaultIsShown, true);
            parameters.Add(p => p.NoDismissOnEscape, true);
        });

        component.Find(".bit-ttp").TriggerEvent("onkeydown", new KeyboardEventArgs { Key = "Escape" });

        Assert.IsTrue(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
    }



    [TestMethod]
    public void BitTooltipDisabledShouldNotShow()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.IsEnabled, false);
        });

        Assert.IsTrue(component.Find(".bit-ttp").ClassList.Contains("bit-dis"));

        component.Find(".bit-ttp").TriggerEvent("onpointerenter", Mouse());
        Assert.IsFalse(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));

        component.Find(".bit-ttp").TriggerEvent("onfocusin", new FocusEventArgs());
        Assert.IsFalse(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
    }

    [TestMethod]
    public void BitTooltipDisabledWhileShownShouldHide()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.DefaultIsShown, true);
        });

        Assert.IsTrue(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));

        component.Render(parameters => parameters.Add(p => p.IsEnabled, false));

        Assert.IsFalse(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
    }

    [TestMethod]
    public void BitTooltipDisabledShouldOverrideDefaultIsShown()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.DefaultIsShown, true);
            parameters.Add(p => p.IsEnabled, false);
        });

        Assert.IsFalse(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
    }

    [TestMethod]
    public async Task BitTooltipDisabledShouldRefuseTheShowMethod()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.IsEnabled, false);
        });

        await component.InvokeAsync(() => component.Instance.Show());

        Assert.IsFalse(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
    }



    [TestMethod]
    public void BitTooltipShouldReportTheStateBack()
    {
        var isShown = false;

        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.IsShown, isShown);
            parameters.Add(p => p.IsShownChanged, EventCallback.Factory.Create<bool>(this, v => isShown = v));
        });

        component.Find(".bit-ttp").TriggerEvent("onpointerenter", Mouse());

        Assert.IsTrue(isShown);

        component.Find(".bit-ttp").TriggerEvent("onpointerleave", Mouse());

        Assert.IsFalse(isShown);
    }

    [TestMethod]
    public void BitTooltipBoundOneWayShouldLeaveTheTriggersAlone()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.IsShown, false);
        });

        component.Find(".bit-ttp").TriggerEvent("onpointerenter", Mouse());
        Assert.IsFalse(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));

        component.Render(parameters => parameters.Add(p => p.IsShown, true));
        Assert.IsTrue(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));

        component.Find(".bit-ttp").TriggerEvent("onpointerleave", Mouse());
        Assert.IsTrue(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
    }

    [TestMethod]
    public void BitTooltipDefaultIsShownShouldBeIgnoredWhenIsShownIsGiven()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.DefaultIsShown, true);
            parameters.Add(p => p.IsShown, false);
        });

        Assert.IsFalse(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
    }



    [TestMethod]
    public async Task BitTooltipShouldExposeShowHideAndToggle()
    {
        var component = RenderComponent<BitTooltip>(parameters => parameters.Add(p => p.Text, "Tip"));

        await component.InvokeAsync(() => component.Instance.Show());
        Assert.IsTrue(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));

        await component.InvokeAsync(() => component.Instance.Hide());
        Assert.IsFalse(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));

        await component.InvokeAsync(() => component.Instance.Toggle());
        Assert.IsTrue(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));

        await component.InvokeAsync(() => component.Instance.Toggle());
        Assert.IsFalse(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
    }

    [TestMethod]
    public async Task BitTooltipTheShowMethodShouldNotWaitOutTheShowDelay()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.ShowDelay, 5000);
        });

        await component.InvokeAsync(() => component.Instance.Show());

        Assert.IsTrue(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
    }



    [TestMethod]
    public void BitTooltipShouldRaiseItsEvents()
    {
        var shown = 0;
        var hidden = 0;
        bool? toggled = null;

        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.OnShow, EventCallback.Factory.Create(this, () => shown++));
            parameters.Add(p => p.OnHide, EventCallback.Factory.Create(this, () => hidden++));
            parameters.Add(p => p.OnToggle, EventCallback.Factory.Create<bool>(this, v => toggled = v));
        });

        component.Find(".bit-ttp").TriggerEvent("onpointerenter", Mouse());

        Assert.AreEqual(1, shown);
        Assert.AreEqual(0, hidden);
        Assert.AreEqual(true, toggled);

        component.Find(".bit-ttp").TriggerEvent("onpointerleave", Mouse());

        Assert.AreEqual(1, shown);
        Assert.AreEqual(1, hidden);
        Assert.AreEqual(false, toggled);
    }

    [TestMethod]
    public void BitTooltipShouldNotRaiseItsEventsForAStateThatDidNotChange()
    {
        var shown = 0;

        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.DefaultIsShown, true);
            parameters.Add(p => p.OnShow, EventCallback.Factory.Create(this, () => shown++));
        });

        component.Find(".bit-ttp").TriggerEvent("onpointerenter", Mouse());

        Assert.AreEqual(0, shown);
    }



    [TestMethod]
    public void BitTooltipLazyRenderShouldHoldTheContentBackUntilItIsShown()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.LazyRender, true);
            parameters.Add(p => p.Template, Markup("<span class=\"tpl\">TemplateContent</span>"));
        });

        Assert.AreEqual(0, component.FindAll(".tpl").Count);

        component.Find(".bit-ttp").TriggerEvent("onpointerenter", Mouse());
        Assert.AreEqual(1, component.FindAll(".tpl").Count);

        // What has been rendered once stays rendered.
        component.Find(".bit-ttp").TriggerEvent("onpointerleave", Mouse());
        Assert.AreEqual(1, component.FindAll(".tpl").Count);
    }

    [TestMethod]
    public void BitTooltipLazyRenderShouldRenderAContentThatStartsShown()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.LazyRender, true);
            parameters.Add(p => p.DefaultIsShown, true);
            parameters.Add(p => p.Template, Markup("<span class=\"tpl\">TemplateContent</span>"));
        });

        Assert.AreEqual(1, component.FindAll(".tpl").Count);
    }



    [TestMethod]
    public void BitTooltipShouldRespectStylesAndClasses()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.Classes, new BitTooltipClassStyles
            {
                Root = "custom-root",
                TooltipWrapper = "custom-wrapper",
                Tooltip = "custom-tooltip",
                Arrow = "custom-arrow"
            });
            parameters.Add(p => p.Styles, new BitTooltipClassStyles
            {
                Root = "color:red",
                TooltipWrapper = "color:green",
                Tooltip = "color:blue",
                Arrow = "color:yellow"
            });
        });

        Assert.IsTrue(component.Find(".bit-ttp").ClassList.Contains("custom-root"));
        Assert.IsTrue(component.Find(".bit-ttp-wrp").ClassList.Contains("custom-wrapper"));
        Assert.IsTrue(component.Find(".bit-ttp-ctn").ClassList.Contains("custom-tooltip"));
        Assert.IsTrue(component.Find(".bit-ttp-arw").ClassList.Contains("custom-arrow"));

        StringAssert.Contains(component.Find(".bit-ttp").GetAttribute("style"), "color:red");
        StringAssert.Contains(component.Find(".bit-ttp-wrp").GetAttribute("style"), "color:green");
        StringAssert.Contains(component.Find(".bit-ttp-ctn").GetAttribute("style"), "color:blue");
        StringAssert.Contains(component.Find(".bit-ttp-arw").GetAttribute("style"), "color:yellow");
    }

    [TestMethod]
    public void BitTooltipShouldRespectStyleAndClass()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.Style, "padding:1rem");
            parameters.Add(p => p.Class, "the-class");
        });

        var root = component.Find(".bit-ttp");

        Assert.IsTrue(root.ClassList.Contains("the-class"));
        StringAssert.Contains(root.GetAttribute("style"), "padding:1rem");
    }

    [TestMethod]
    public void BitTooltipShouldRespectDirAndVisibility()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.Dir, BitDir.Rtl);
            parameters.Add(p => p.Visibility, BitVisibility.Collapsed);
        });

        var root = component.Find(".bit-ttp");

        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        StringAssert.Contains(root.GetAttribute("style"), "display:none");
    }

    [TestMethod]
    public void BitTooltipShouldRespectHtmlAttributes()
    {
        // The splat only reaches the tooltip through the render tree: HtmlAttributes is a plain parameter
        // on BitComponentBase rather than a CaptureUnmatchedValues one, so bUnit's AddUnmatched cannot
        // feed it.
        var component = RenderComponent<BitTooltipHtmlAttributesTest>();

        Assert.AreEqual("bit", component.Find(".bit-ttp").GetAttribute("data-val-test"));
    }



    [TestMethod]
    public async Task BitTooltipDisposeShouldNotThrow()
    {
        var component = RenderComponent<BitTooltip>(p =>
        {
            p.Add(x => x.Text, "Tip");
            p.Add(x => x.ShowDelay, 500);
            p.Add(x => x.HideDelay, 500);
        });

        await component.Instance.DisposeAsync();
    }

    [TestMethod]
    public async Task BitTooltipDisposeDuringShowDelayShouldNotThrow()
    {
        var component = RenderComponent<BitTooltip>(p =>
        {
            p.Add(x => x.Text, "Tip");
            p.Add(x => x.ShowDelay, 5000);
            p.Add(x => x.ShowOnHover, true);
            p.AddChildContent("<button>Hover me</button>");
        });

        var root = component.Find(".bit-ttp");
        await component.InvokeAsync(() => root.TriggerEvent("onpointerenter", Mouse()));

        await component.Instance.DisposeAsync();
    }

    [TestMethod]
    public async Task BitTooltipDisposeDuringHideDelayShouldNotThrow()
    {
        var component = RenderComponent<BitTooltip>(p =>
        {
            p.Add(x => x.Text, "Tip");
            p.Add(x => x.DefaultIsShown, true);
            p.Add(x => x.HideDelay, 5000);
            p.Add(x => x.ShowOnHover, true);
            p.AddChildContent("<button>Hover me</button>");
        });

        var root = component.Find(".bit-ttp");
        await component.InvokeAsync(() => root.TriggerEvent("onpointerleave", Mouse()));

        await component.Instance.DisposeAsync();
    }

    [TestMethod]
    public async Task BitTooltipRepeatedTriggersDuringADelayShouldNotThrow()
    {
        var component = RenderComponent<BitTooltip>(p =>
        {
            p.Add(x => x.Text, "Tip");
            p.Add(x => x.ShowDelay, 30);
            p.Add(x => x.HideDelay, 30);
        });

        for (var i = 0; i < 20; i++)
        {
            component.Find(".bit-ttp").TriggerEvent("onpointerenter", Mouse());
            component.Find(".bit-ttp").TriggerEvent("onpointerleave", Mouse());
        }

        component.Find(".bit-ttp").TriggerEvent("onpointerenter", Mouse());

        component.WaitForAssertion(() =>
            Assert.IsTrue(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis")));

        await Task.CompletedTask;
    }
}
