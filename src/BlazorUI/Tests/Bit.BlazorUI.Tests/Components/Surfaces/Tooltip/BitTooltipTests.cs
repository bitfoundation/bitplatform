using System;
using System.Linq;
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
            // Long enough that the assertion below is not racing the delay it is there to prove: a
            // short one could elapse between the trigger and the read on a loaded machine.
            parameters.Add(p => p.ShowDelay, 1000);
        });

        component.Find(".bit-ttp").TriggerEvent("onpointerenter", Mouse());

        Assert.IsFalse(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));

        component.WaitForAssertion(
            () => Assert.IsTrue(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis")),
            TimeSpan.FromSeconds(5));
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

        // The pointer has to have arrived for the leave to be the one that starts the pending hide this
        // test is about.
        component.Find(".bit-ttp").TriggerEvent("onpointerenter", Mouse());

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



    [TestMethod]
    public void BitTooltipWithoutContentShouldRenderNoSurfaceAtAll()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.DefaultIsShown, true);
            parameters.AddChildContent("<button>Anchor</button>");
        });

        Assert.AreEqual(0, component.FindAll(".bit-ttp-wrp").Count);
        Assert.AreEqual(0, component.FindAll(".bit-ttp-ctn").Count);
        Assert.AreEqual(0, component.FindAll(".bit-ttp-arw").Count);

        component.Render(parameters => parameters.Add(p => p.Text, "Tip"));

        Assert.AreEqual(1, component.FindAll(".bit-ttp-wrp").Count);
    }

    [TestMethod]
    public void BitTooltipShouldRenderItsIdAndAriaLabelOnTheRoot()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Id, "the-tip");
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.AriaLabel, "The label");
        });

        var root = component.Find(".bit-ttp");

        Assert.AreEqual("the-tip", root.Id);
        Assert.AreEqual("The label", root.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitTooltipShouldExposeTheIdOfTheElementItsTextLandsIn()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Id, "the-tip");
            parameters.Add(p => p.Text, "Tip");
        });

        Assert.AreEqual("the-tip-ttp", component.Instance.TooltipId);
        Assert.AreEqual(component.Instance.TooltipId, component.Find(".bit-ttp-ctn").Id);
    }



    [TestMethod]
    public void BitTooltipShouldDescribeItsAnchorByDefault()
    {
        var component = RenderComponent<BitTooltip>(parameters => parameters.Add(p => p.Text, "Tip"));

        var root = component.Find(".bit-ttp");

        Assert.AreEqual(component.Find(".bit-ttp-ctn").Id, root.GetAttribute("aria-describedby"));
        Assert.IsNull(root.GetAttribute("aria-labelledby"));
    }

    [TestMethod]
    public void BitTooltipRelationshipLabelShouldNameItsAnchorInstead()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Save");
            parameters.Add(p => p.Relationship, BitTooltipRelationship.Label);
        });

        var root = component.Find(".bit-ttp");

        Assert.AreEqual(component.Find(".bit-ttp-ctn").Id, root.GetAttribute("aria-labelledby"));
        Assert.IsNull(root.GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitTooltipRelationshipNoneShouldDeclareNothing()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.Relationship, BitTooltipRelationship.None);
        });

        var root = component.Find(".bit-ttp");

        Assert.IsNull(root.GetAttribute("aria-describedby"));
        Assert.IsNull(root.GetAttribute("aria-labelledby"));

        // The surface is still there: the relationship is about the accessibility tree, not about what
        // is drawn on the screen.
        Assert.AreEqual(1, component.FindAll(".bit-ttp-ctn").Count);
    }

    [TestMethod]
    public void BitTooltipDisabledShouldDescribeNothing()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.IsEnabled, false);
        });

        Assert.IsNull(component.Find(".bit-ttp").GetAttribute("aria-describedby"));
    }



    [TestMethod]
    public void BitTooltipShouldRenderTheZIndexAsACustomProperty()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.ZIndex, 9999);
        });

        StringAssert.Contains(component.Find(".bit-ttp").GetAttribute("style"), "--bit-ttp-zindex:9999");

        component.Render(parameters => parameters.Add(p => p.ZIndex, (int?)null));

        var style = component.Find(".bit-ttp").GetAttribute("style") ?? string.Empty;

        Assert.IsFalse(style.Contains("--bit-ttp-zindex"));
    }



    [TestMethod]
    public void BitTooltipShouldStayWhileTheKeyboardIsStillOnTheAnchor()
    {
        var component = RenderComponent<BitTooltip>(parameters => parameters.Add(p => p.Text, "Tip"));

        component.Find(".bit-ttp").TriggerEvent("onfocusin", new FocusEventArgs());
        component.Find(".bit-ttp").TriggerEvent("onpointerenter", Mouse());

        Assert.IsTrue(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));

        // The pointer leaving does not take away the tooltip the keyboard is still asking for.
        component.Find(".bit-ttp").TriggerEvent("onpointerleave", Mouse());
        Assert.IsTrue(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));

        component.Find(".bit-ttp").TriggerEvent("onfocusout", new FocusEventArgs());
        Assert.IsFalse(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
    }

    [TestMethod]
    public void BitTooltipShouldStayWhileThePointerIsStillOnTheAnchor()
    {
        var component = RenderComponent<BitTooltip>(parameters => parameters.Add(p => p.Text, "Tip"));

        component.Find(".bit-ttp").TriggerEvent("onpointerenter", Mouse());
        component.Find(".bit-ttp").TriggerEvent("onfocusin", new FocusEventArgs());

        Assert.IsTrue(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));

        // The focus leaving does not take away the tooltip the pointer is still hovering.
        component.Find(".bit-ttp").TriggerEvent("onfocusout", new FocusEventArgs());
        Assert.IsTrue(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));

        component.Find(".bit-ttp").TriggerEvent("onpointerleave", Mouse());
        Assert.IsFalse(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
    }

    [TestMethod]
    public void BitTooltipShouldNotAnswerTheFocusThatFollowsAPress()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.ShowOnHover, false);
        });

        component.Find(".bit-ttp").TriggerEvent("onpointerdown", new PointerEventArgs());
        component.Find(".bit-ttp").TriggerEvent("onfocusin", new FocusEventArgs());

        Assert.IsFalse(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));

        // What the press left behind is given up once the pointer has gone, so the keyboard reaching the
        // same anchor afterwards is answered as the keyboard.
        component.Find(".bit-ttp").TriggerEvent("onfocusout", new FocusEventArgs());
        component.Find(".bit-ttp").TriggerEvent("onfocusin", new FocusEventArgs());

        Assert.IsTrue(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
    }



    [TestMethod]
    public void BitTooltipHideOnClickShouldTakeAHoverTooltipAway()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.HideOnClick, true);
        });

        component.Find(".bit-ttp").TriggerEvent("onpointerenter", Mouse());
        Assert.IsTrue(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));

        component.Find(".bit-ttp").TriggerEvent("onpointerup", new PointerEventArgs());
        Assert.IsFalse(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
    }

    [TestMethod]
    public void BitTooltipHideOnClickShouldLeaveTheSecondaryButtonAlone()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.HideOnClick, true);
        });

        component.Find(".bit-ttp").TriggerEvent("onpointerenter", Mouse());

        component.Find(".bit-ttp").TriggerEvent("onpointerup", new PointerEventArgs { Button = 2 });

        Assert.IsTrue(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
    }

    [TestMethod]
    public void BitTooltipShowOnClickShouldWinOverHideOnClick()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.ShowOnClick, true);
            parameters.Add(p => p.HideOnClick, true);
            parameters.Add(p => p.ShowOnHover, false);
            parameters.Add(p => p.ShowOnFocus, false);
        });

        component.Find(".bit-ttp").TriggerEvent("onpointerup", new PointerEventArgs());

        Assert.IsTrue(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
    }

    [TestMethod]
    public void BitTooltipAPointerUpInsideTheTooltipShouldNotToggleIt()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.Interactive, true);
            parameters.Add(p => p.ShowOnClick, true);
            parameters.Add(p => p.DefaultIsShown, true);
        });

        component.Find(".bit-ttp-ctn").TriggerEvent("onpointerup", new PointerEventArgs());

        Assert.IsTrue(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
    }



    [TestMethod]
    public void BitTooltipNoTouchShouldIgnoreATap()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.NoTouch, true);
        });

        component.Find(".bit-ttp").TriggerEvent("onpointerenter", Touch());

        Assert.IsFalse(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
    }

    [TestMethod]
    public void BitTooltipNoTouchShouldStillAnswerThePointer()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.NoTouch, true);
        });

        component.Find(".bit-ttp").TriggerEvent("onpointerenter", Mouse());

        Assert.IsTrue(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
    }



    [TestMethod]
    public void BitTooltipEscapeShouldOutliveThePointerThatIsStillOnTheAnchor()
    {
        var component = RenderComponent<BitTooltip>(parameters => parameters.Add(p => p.Text, "Tip"));

        component.Find(".bit-ttp").TriggerEvent("onpointerenter", Mouse());
        component.Find(".bit-ttp").TriggerEvent("onfocusin", new FocusEventArgs());

        component.Find(".bit-ttp").TriggerEvent("onkeydown", new KeyboardEventArgs { Key = "Escape" });
        Assert.IsFalse(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));

        // Neither the pointer nor the keyboard leaving brings back what was dismissed.
        component.Find(".bit-ttp").TriggerEvent("onpointerleave", Mouse());
        component.Find(".bit-ttp").TriggerEvent("onfocusout", new FocusEventArgs());

        Assert.IsFalse(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
    }

    [TestMethod]
    public void BitTooltipDisabledShouldForgetTheTriggersItWasHeldBy()
    {
        var component = RenderComponent<BitTooltip>(parameters => parameters.Add(p => p.Text, "Tip"));

        component.Find(".bit-ttp").TriggerEvent("onpointerenter", Mouse());
        Assert.IsTrue(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));

        component.Render(parameters => parameters.Add(p => p.IsEnabled, false));
        Assert.IsFalse(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));

        component.Render(parameters => parameters.Add(p => p.IsEnabled, true));
        Assert.IsFalse(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));

        // The pointer that was over the anchor is no longer counted, so the leave that follows it hides
        // nothing and the next enter shows the tooltip again from a clean state.
        component.Find(".bit-ttp").TriggerEvent("onpointerleave", Mouse());
        component.Find(".bit-ttp").TriggerEvent("onpointerenter", Mouse());

        Assert.IsTrue(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
    }



    [TestMethod]
    public void BitTooltipGroupShouldHandItsDelaysToTheTooltipsThatHaveNone()
    {
        var component = RenderComponent<BitTooltipGroupTest>(parameters =>
        {
            parameters.Add(p => p.ShowDelay, 60);
            parameters.Add(p => p.HideDelay, 0);
        });

        component.Find("#first").TriggerEvent("onpointerenter", Mouse());

        Assert.IsFalse(component.Find("#first .bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));

        component.WaitForAssertion(() =>
            Assert.IsTrue(component.Find("#first .bit-ttp-wrp").ClassList.Contains("bit-ttp-vis")));
    }

    [TestMethod]
    public void BitTooltipGroupShouldLeaveADelayOfItsOwnAlone()
    {
        var component = RenderComponent<BitTooltipGroupTest>(parameters =>
        {
            parameters.Add(p => p.ShowDelay, 5000);
            parameters.Add(p => p.HideDelay, 0);
        });

        // The third tooltip names a show delay of zero, which the group does not fill in over.
        component.Find("#third").TriggerEvent("onpointerenter", Mouse());

        Assert.IsTrue(component.Find("#third .bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
    }

    [TestMethod]
    public void BitTooltipGroupShouldSkipTheShowDelayWhileTheLastTooltipIsFresh()
    {
        var component = RenderComponent<BitTooltipGroupTest>(parameters =>
        {
            parameters.Add(p => p.ShowDelay, 5000);
            parameters.Add(p => p.HideDelay, 0);
            parameters.Add(p => p.SkipDelay, 5000);
        });

        // The first of a row waits, so it is shown by hand rather than waited out.
        component.Find("#third").TriggerEvent("onpointerenter", Mouse());
        component.Find("#third").TriggerEvent("onpointerleave", Mouse());

        Assert.IsFalse(component.Find("#third .bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));

        component.Find("#first").TriggerEvent("onpointerenter", Mouse());

        Assert.IsTrue(component.Find("#first .bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
    }

    [TestMethod]
    public void BitTooltipGroupWithoutASkipWindowShouldMakeEveryTooltipWait()
    {
        var component = RenderComponent<BitTooltipGroupTest>(parameters =>
        {
            parameters.Add(p => p.ShowDelay, 5000);
            parameters.Add(p => p.HideDelay, 0);
            parameters.Add(p => p.SkipDelay, 0);
        });

        component.Find("#third").TriggerEvent("onpointerenter", Mouse());
        component.Find("#third").TriggerEvent("onpointerleave", Mouse());

        component.Find("#first").TriggerEvent("onpointerenter", Mouse());

        Assert.IsFalse(component.Find("#first .bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
    }

    [TestMethod]
    public void BitTooltipGroupShouldShowOneTooltipAtATime()
    {
        var component = RenderComponent<BitTooltipGroupTest>(parameters =>
        {
            parameters.Add(p => p.ShowDelay, 0);
            parameters.Add(p => p.HideDelay, 0);
        });

        component.Find("#first").TriggerEvent("onpointerenter", Mouse());
        Assert.IsTrue(component.Find("#first .bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));

        component.Find("#second").TriggerEvent("onpointerenter", Mouse());

        Assert.IsTrue(component.Find("#second .bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
        Assert.IsFalse(component.Find("#first .bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
    }

    [TestMethod]
    public void BitTooltipShouldRespectFullWidth()
    {
        var component = RenderComponent<BitTooltip>(parameters => parameters.Add(p => p.Text, "Tip"));

        Assert.IsFalse(component.Find(".bit-ttp").ClassList.Contains("bit-ttp-flw"));

        component.Render(parameters => parameters.Add(p => p.FullWidth, true));

        Assert.IsTrue(component.Find(".bit-ttp").ClassList.Contains("bit-ttp-flw"));
    }

    [TestMethod]
    public void BitTooltipShouldTreatAWhitespaceTextAsNoContent()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "   ");
            parameters.Add(p => p.DefaultIsShown, true);
        });

        Assert.AreEqual(0, component.FindAll(".bit-ttp-wrp").Count);
        Assert.IsNull(component.Find(".bit-ttp").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitTooltipShownByATapShouldNotBeClosedByTheSameTap()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.TouchHideDelay, 0);
            parameters.Add(p => p.ShowOnClick, true);
        });

        // The enter, the down and the up of one tap arrive one after another; only the enter acts.
        component.Find(".bit-ttp").TriggerEvent("onpointerenter", Touch());
        component.Find(".bit-ttp").TriggerEvent("onpointerup", Touch());

        Assert.IsTrue(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));

        // A second tap, whose enter did nothing because the pointer never left, does toggle it.
        component.Find(".bit-ttp").TriggerEvent("onpointerup", Touch());

        Assert.IsFalse(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
    }

    [TestMethod]
    public void BitTooltipHideOnClickShouldNotUndoTheTapThatShowedIt()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.TouchHideDelay, 0);
            parameters.Add(p => p.HideOnClick, true);
        });

        component.Find(".bit-ttp").TriggerEvent("onpointerenter", Touch());
        component.Find(".bit-ttp").TriggerEvent("onpointerup", Touch());

        Assert.IsTrue(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
    }



    [DataTestMethod]
    [DataRow(BitTooltipPosition.Top, "bit-ttp-top")]
    [DataRow(BitTooltipPosition.TopLeft, "bit-ttp-trg")]
    [DataRow(BitTooltipPosition.TopRight, "bit-ttp-tlf")]
    [DataRow(BitTooltipPosition.RightTop, "bit-ttp-ltp")]
    [DataRow(BitTooltipPosition.Right, "bit-ttp-lft")]
    [DataRow(BitTooltipPosition.RightBottom, "bit-ttp-lbm")]
    [DataRow(BitTooltipPosition.BottomRight, "bit-ttp-blf")]
    [DataRow(BitTooltipPosition.Bottom, "bit-ttp-btm")]
    [DataRow(BitTooltipPosition.BottomLeft, "bit-ttp-brg")]
    [DataRow(BitTooltipPosition.LeftBottom, "bit-ttp-rbm")]
    [DataRow(BitTooltipPosition.Left, "bit-ttp-rgt")]
    [DataRow(BitTooltipPosition.LeftTop, "bit-ttp-rtp")]
    public void BitTooltipMirrorInRtlShouldSwapTheTwoSides(BitTooltipPosition position, string expectedClass)
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.Position, position);
            parameters.Add(p => p.Dir, BitDir.Rtl);
            parameters.Add(p => p.MirrorInRtl, true);
        });

        Assert.IsTrue(component.Find(".bit-ttp-wrp").ClassList.Contains(expectedClass));
    }

    [TestMethod]
    public void BitTooltipMirrorInRtlShouldLeaveALeftToRightTooltipAlone()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.Position, BitTooltipPosition.Left);
            parameters.Add(p => p.MirrorInRtl, true);
        });

        Assert.IsTrue(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-lft"));
    }

    [TestMethod]
    public void BitTooltipInRtlShouldKeepItsPositionWithoutMirrorInRtl()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.Position, BitTooltipPosition.Left);
            parameters.Add(p => p.Dir, BitDir.Rtl);
        });

        Assert.IsTrue(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-lft"));
    }



    [TestMethod]
    public void BitTooltipGroupAllowMultipleShouldLetThemStandTogether()
    {
        var component = RenderComponent<BitTooltipGroupTest>(parameters =>
        {
            parameters.Add(p => p.ShowDelay, 0);
            parameters.Add(p => p.HideDelay, 0);
            parameters.Add(p => p.AllowMultiple, true);
        });

        component.Find("#first").TriggerEvent("onpointerenter", Mouse());
        component.Find("#second").TriggerEvent("onpointerenter", Mouse());

        Assert.IsTrue(component.Find("#first .bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
        Assert.IsTrue(component.Find("#second .bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
    }

    [TestMethod]
    public void BitTooltipGroupShouldLetGoOfATooltipThatLeavesThePage()
    {
        var component = RenderComponent<BitTooltipGroupTest>(parameters =>
        {
            parameters.Add(p => p.ShowDelay, 0);
            parameters.Add(p => p.HideDelay, 0);
        });

        component.Find("#third").TriggerEvent("onpointerenter", Mouse());
        Assert.IsTrue(component.Find("#third .bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));

        // The tooltip that was on the screen is taken off the page while it is still shown; the group
        // must not go on holding it, or showing another one would reach a component that is gone.
        component.Render(parameters => parameters.Add(p => p.RenderThird, false));

        component.Find("#first").TriggerEvent("onpointerenter", Mouse());

        Assert.IsTrue(component.Find("#first .bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
        Assert.AreEqual(0, component.FindAll("#third").Count);
    }

    [TestMethod]
    public void BitTooltipShowOnClickShouldBeToggledByTheKeyboardAsWell()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.ShowOnClick, true);
            parameters.Add(p => p.ShowOnHover, false);
            parameters.Add(p => p.ShowOnFocus, false);
        });

        // Enter and Space are how a keyboard presses the anchor, so a tooltip only the click shows is one
        // the keyboard can open as well - without it there would be no way to reach it at all.
        component.Find(".bit-ttp").TriggerEvent("onkeydown", new KeyboardEventArgs { Key = "Enter" });

        Assert.IsTrue(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));

        component.Find(".bit-ttp").TriggerEvent("onkeydown", new KeyboardEventArgs { Key = " " });

        Assert.IsFalse(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
    }

    [TestMethod]
    public void BitTooltipHideOnClickShouldAnswerTheKeyboardPressToo()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.HideOnClick, true);
        });

        component.Find(".bit-ttp").TriggerEvent("onfocusin", new FocusEventArgs());

        Assert.IsTrue(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));

        component.Find(".bit-ttp").TriggerEvent("onkeydown", new KeyboardEventArgs { Key = " " });

        Assert.IsFalse(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
    }

    [TestMethod]
    public void BitTooltipShouldLeaveTheKeyboardPressAloneWithoutAClickTrigger()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
        });

        component.Find(".bit-ttp").TriggerEvent("onfocusin", new FocusEventArgs());
        component.Find(".bit-ttp").TriggerEvent("onkeydown", new KeyboardEventArgs { Key = "Enter" });

        // Neither trigger asked for the press to mean anything, so the tooltip the focus is holding stays.
        Assert.IsTrue(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
    }

    [TestMethod]
    public void BitTooltipOpenedByAClickShouldBeDismissedByTheFocusLeavingTheAnchor()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.ShowOnClick, true);
            parameters.Add(p => p.ShowOnHover, false);
            parameters.Add(p => p.ShowOnFocus, false);
        });

        component.Find(".bit-ttp").TriggerEvent("onpointerup", Mouse());

        Assert.IsTrue(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));

        // A click elsewhere on the page and a Tab away from the anchor both come down to the focus
        // leaving it, which is what dismisses a tooltip nothing else is holding.
        component.Find(".bit-ttp").TriggerEvent("onfocusout", new FocusEventArgs());

        Assert.IsFalse(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
    }

    [TestMethod]
    public void BitTooltipOpenedByAClickShouldStayWhileThePointerIsStillOnTheAnchor()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.ShowOnClick, true);
            parameters.Add(p => p.ShowOnFocus, false);
        });

        component.Find(".bit-ttp").TriggerEvent("onpointerenter", Mouse());
        component.Find(".bit-ttp").TriggerEvent("onfocusout", new FocusEventArgs());

        // The pointer resting on the anchor is asking for the tooltip in its own right, so the focus
        // leaving takes nothing away.
        Assert.IsTrue(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
    }

    [TestMethod]
    public void BitTooltipEscapeShouldNotBeUndoneByTheFocusLeavingAfterAClick()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.ShowOnClick, true);
            parameters.Add(p => p.ShowOnHover, false);
            parameters.Add(p => p.ShowOnFocus, false);
        });

        component.Find(".bit-ttp").TriggerEvent("onpointerup", Mouse());
        component.Find(".bit-ttp").TriggerEvent("onkeydown", new KeyboardEventArgs { Key = "Escape" });

        Assert.IsFalse(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));

        component.Find(".bit-ttp").TriggerEvent("onfocusout", new FocusEventArgs());

        Assert.IsFalse(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
    }



    [TestMethod]
    public void BitTooltipTouchShowDelayShouldLeaveAQuickTapAlone()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.TouchShowDelay, 5000);
        });

        // The enter, the up and the leave of one tap arrive one after another, all of them long before a
        // press that lasts would have asked for the tooltip.
        component.Find(".bit-ttp").TriggerEvent("onpointerenter", Touch());
        component.Find(".bit-ttp").TriggerEvent("onpointerup", Touch());
        component.Find(".bit-ttp").TriggerEvent("onpointerleave", Touch());

        Assert.IsFalse(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
    }

    [TestMethod]
    public void BitTooltipTouchShowDelayShouldShowTheTooltipOnceThePressHasLasted()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.TouchShowDelay, 50);
            parameters.Add(p => p.TouchHideDelay, 0);
        });

        component.Find(".bit-ttp").TriggerEvent("onpointerenter", Touch());

        component.WaitForAssertion(() =>
            Assert.IsTrue(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis")));
    }

    [TestMethod]
    public void BitTooltipTouchWithoutAShowDelayShouldStillShowOnTheTap()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.TouchHideDelay, 0);
        });

        component.Find(".bit-ttp").TriggerEvent("onpointerenter", Touch());

        Assert.IsTrue(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
    }



    [TestMethod]
    public void BitTooltipShouldMirrorTheRelationshipOntoTheAnchor()
    {
        RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Id, "tip");
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.ChildContent, Markup("<button>Anchor</button>"));
        });

        // A describedby on the container the anchor is wrapped in is read by nothing, so the same one is
        // written onto the control the reader actually lands on.
        var invocation = Context.JSInterop.Invocations
                                .Single(i => i.Identifier == "BitBlazorUI.Utils.syncAriaDescription");

        Assert.AreEqual("tip", invocation.Arguments[0]);
        Assert.AreEqual("tip-ttp", invocation.Arguments[1]);
        Assert.AreEqual("aria-describedby", invocation.Arguments[2]);
    }

    [TestMethod]
    public void BitTooltipRelationshipLabelShouldMirrorTheLabelledByInstead()
    {
        RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.Relationship, BitTooltipRelationship.Label);
        });

        var invocation = Context.JSInterop.Invocations
                                .Single(i => i.Identifier == "BitBlazorUI.Utils.syncAriaDescription");

        Assert.AreEqual("aria-labelledby", invocation.Arguments[2]);
    }

    [TestMethod]
    public void BitTooltipRelationshipNoneShouldMirrorNothing()
    {
        RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.Relationship, BitTooltipRelationship.None);
        });

        // Nothing was ever written, so there is nothing to take away either: the round trip is not made.
        Assert.IsFalse(Context.JSInterop.Invocations
                              .Any(i => i.Identifier == "BitBlazorUI.Utils.syncAriaDescription"));
    }

    [TestMethod]
    public void BitTooltipShouldTakeTheMirroredRelationshipAwayWhenItIsGivenUp()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
        });

        component.Render(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.Relationship, BitTooltipRelationship.None);
        });

        var invocations = Context.JSInterop.Invocations
                                 .Where(i => i.Identifier == "BitBlazorUI.Utils.syncAriaDescription")
                                 .ToArray();

        Assert.AreEqual(2, invocations.Length);
        Assert.AreEqual("aria-describedby", invocations[0].Arguments[2]);
        Assert.AreEqual(string.Empty, invocations[1].Arguments[2]);
    }

    [TestMethod]
    public void BitTooltipShouldMirrorTheRelationshipOnlyWhenItChanges()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
        });

        component.Render(parameters => parameters.Add(p => p.Text, "Another tip"));

        // The attribute is written from JavaScript, so a call per render would be a round trip per render
        // for something that changes with the relationship alone.
        Assert.AreEqual(1, Context.JSInterop.Invocations
                                 .Count(i => i.Identifier == "BitBlazorUI.Utils.syncAriaDescription"));
    }

    [TestMethod]
    public void BitTooltipAPointerDownInsideTheTooltipShouldNotCountAsTheOneThatFocusesTheAnchor()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.Interactive, true);
        });

        // Selecting the text of an interactive tooltip is a press inside the tooltip, not the press that
        // focuses the anchor, so the keyboard arriving afterwards is still answered as the keyboard.
        component.Find(".bit-ttp-wrp").TriggerEvent("onpointerdown", Mouse());
        component.Find(".bit-ttp").TriggerEvent("onfocusin", new FocusEventArgs());

        Assert.IsTrue(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
    }

    [TestMethod]
    public void BitTooltipBoundOneWayShouldLeaveTheKeyboardPressAlone()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.IsShown, false);
            parameters.Add(p => p.ShowOnClick, true);
        });

        component.Find(".bit-ttp").TriggerEvent("onkeydown", new KeyboardEventArgs { Key = "Enter" });

        // The page owns the state, so nothing that happens on the anchor changes it.
        Assert.IsFalse(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
    }

    [TestMethod]
    public void BitTooltipDisabledShouldRefuseTheKeyboardPress()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Tip");
            parameters.Add(p => p.ShowOnClick, true);
            parameters.Add(p => p.IsEnabled, false);
        });

        component.Find(".bit-ttp").TriggerEvent("onkeydown", new KeyboardEventArgs { Key = "Enter" });

        Assert.IsFalse(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
    }
}
