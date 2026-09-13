using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Callout;

[TestClass]
public class BitCalloutTests : BunitTestContext
{
    private static RenderFragment Markup(string html) => builder => builder.AddMarkupContent(0, html);



    [TestMethod]
    public void BitCalloutShouldRenderRootAndParts()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.AddChildContent("<div class=\"content\">Hello</div>");
        });

        Assert.IsNotNull(component.Find(".bit-clo"));
        Assert.IsNotNull(component.Find(".bit-clo-ovl"));
        Assert.IsNotNull(component.Find(".bit-clo-cal"));

        // The arrow is opt-in, so a callout that was not asked for one renders none.
        Assert.AreEqual(0, component.FindAll(".bit-clo-arw").Count);
    }

    [TestMethod]
    public void BitCalloutShouldRenderAnchorWhenProvided()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.Anchor, Markup("<button class=\"anchor-btn\">Anchor</button>"));
        });

        var anchor = component.Find(".bit-clo-acn");

        Assert.IsNotNull(anchor);
        Assert.IsTrue(anchor.OuterHtml.Contains("Anchor"));
    }

    [TestMethod]
    public void BitCalloutShouldNotRenderAnchorWhenNotProvided()
    {
        var component = RenderComponent<BitCallout>();

        Assert.AreEqual(0, component.FindAll(".bit-clo-acn").Count);
    }

    [TestMethod]
    public void BitCalloutAnchorShouldCarryThePopupRelationship()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        var anchor = component.Find(".bit-clo-acn");
        var callout = component.Find(".bit-clo-cal");

        // A callout of plain content is not a menu, and `true` is what every screen reader reads
        // aria-haspopup as, so a callout that is none of the kinds the property can name carries nothing:
        // aria-expanded is what tells the user there is something to open.
        Assert.IsNull(anchor.GetAttribute("aria-haspopup"));
        Assert.AreEqual("false", anchor.GetAttribute("aria-expanded"));
        Assert.AreEqual(callout.Id, anchor.GetAttribute("aria-controls"));

        // The anchor is a container for the consumer's own trigger, so it must not become a second tab
        // stop or an interactive element wrapping an interactive element.
        Assert.IsNull(anchor.GetAttribute("role"));
        Assert.IsNull(anchor.GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitCalloutAnchorShouldReportTheOpenStateAndTheDialogPopup()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.TrapFocus, true);
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        var anchor = component.Find(".bit-clo-acn");

        Assert.AreEqual("true", anchor.GetAttribute("aria-expanded"));
        Assert.AreEqual("dialog", anchor.GetAttribute("aria-haspopup"));
    }

    [DataTestMethod]
    [DataRow("menu", "menu")]
    [DataRow("listbox", "listbox")]
    [DataRow("tree", "tree")]
    [DataRow("grid", "grid")]
    [DataRow("dialog", "dialog")]
    // The role of the element that holds the popup has to match what aria-haspopup names it, so a role
    // the property cannot name leaves the anchor carrying nothing rather than naming it wrongly.
    [DataRow("tooltip", null)]
    [DataRow("region", null)]
    public void BitCalloutAnchorShouldNameTheKindOfPopupItHolds(string role, string? expected)
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.Role, role);
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        Assert.AreEqual(expected, component.Find(".bit-clo-acn").GetAttribute("aria-haspopup"));
    }

    [TestMethod]
    public void BitCalloutAnchorShouldNotNameAGroupAsAPopup()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            // A named callout without a role of its own is reported as a group, which is not a kind of
            // popup aria-haspopup can name.
            parameters.Add(p => p.AriaLabel, "Details");
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        Assert.AreEqual("group", component.Find(".bit-clo-cal").GetAttribute("role"));
        Assert.IsNull(component.Find(".bit-clo-acn").GetAttribute("aria-haspopup"));
    }

    [TestMethod]
    public void BitCalloutShouldMirrorThePopupRelationshipOntoTheTriggerInTheAnchor()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.TrapFocus, true);
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        var sync = Context.JSInterop.Invocations["BitBlazorUI.Utils.syncAriaPopup"];

        Assert.AreEqual(component.Find(".bit-clo-acn").Id, sync[^1].Arguments[0]);
        Assert.AreEqual(component.Find(".bit-clo-cal").Id, sync[^1].Arguments[1]);
        Assert.AreEqual(false, sync[^1].Arguments[2]);
        Assert.AreEqual("dialog", sync[^1].Arguments[3]);

        component.Find(".bit-clo-acn").Click();

        Assert.AreEqual(true, Context.JSInterop.Invocations["BitBlazorUI.Utils.syncAriaPopup"][^1].Arguments[2]);
    }

    [TestMethod]
    public void BitCalloutShouldNotMirrorThePopupRelationshipWithoutAnAnchorOfItsOwn()
    {
        RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.AnchorId, "external");
        });

        // An external anchor belongs to the consumer, who declares the relationship on it themselves.
        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.syncAriaPopup"].Count);
    }

    [TestMethod]
    public void BitCalloutShouldNameItselfByTheHeaderItRenders()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.TrapFocus, true);
            parameters.Add(p => p.Header, Markup("<span>Filters</span>"));
            parameters.AddChildContent("<div>Body</div>");
        });

        var callout = component.Find(".bit-clo-cal");

        Assert.AreEqual("dialog", callout.GetAttribute("role"));
        Assert.AreEqual(component.Find(".bit-clo-hdr").Id, callout.GetAttribute("aria-labelledby"));
    }

    [TestMethod]
    public void BitCalloutShouldLetTheAriaLabelWinOverTheHeaderForItsName()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, "Filter panel");
            parameters.Add(p => p.Header, Markup("<span>Filters</span>"));
            parameters.AddChildContent("<div>Body</div>");
        });

        var callout = component.Find(".bit-clo-cal");

        Assert.AreEqual("Filter panel", callout.GetAttribute("aria-label"));
        Assert.IsNull(callout.GetAttribute("aria-labelledby"));
    }

    [TestMethod]
    public void BitCalloutShouldNotNameItselfByAHeaderItDoesNotRender()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.AddChildContent("<div>Body</div>");
        });

        Assert.IsNull(component.Find(".bit-clo-cal").GetAttribute("aria-labelledby"));
    }

    [TestMethod]
    public void BitCalloutShouldShowOverlayWhenIsOpenTrue()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.AddChildContent("<div class=\"content\">Hello</div>");
        });

        var overlay = component.Find(".bit-clo-ovl");

        Assert.IsTrue(overlay.GetAttribute("style").Contains("display:block"));

        var content = component.Find(".bit-clo-cal");

        Assert.IsTrue(content.OuterHtml.Contains("Hello"));
        Assert.IsTrue(content.ClassList.Contains("bit-clo-ocl"));
    }

    [TestMethod]
    public void BitCalloutShouldHideOverlayWhenIsOpenFalse()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.IsOpen, false);
            parameters.AddChildContent("<div class=\"content\">Hello</div>");
        });

        var overlay = component.Find(".bit-clo-ovl");

        Assert.IsTrue(overlay.GetAttribute("style").Contains("display:none"));
        Assert.IsFalse(component.Find(".bit-clo-cal").ClassList.Contains("bit-clo-ocl"));
    }

    [TestMethod]
    public void BitCalloutShouldPreferContentOverChildContent()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.Content, Markup("<div class=\"content\">ContentFragment</div>"));
            parameters.AddChildContent("<div class=\"child\">ChildContent</div>");
        });

        var content = component.Find(".bit-clo-cal");

        Assert.IsTrue(content.OuterHtml.Contains("ContentFragment"));
        Assert.IsFalse(content.OuterHtml.Contains("ChildContent"));
    }

    [TestMethod]
    public void BitCalloutShouldRespectIsEnabled()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
        });

        Assert.IsTrue(component.Find(".bit-clo").ClassList.Contains("bit-dis"));
    }

    [TestMethod]
    public void BitCalloutShouldNotOpenWhenDisabled()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        component.Find(".bit-clo-acn").Click();

        Assert.IsFalse(component.Instance.IsOpen);
    }

    [TestMethod]
    public void BitCalloutShouldCloseWhenItIsDisabledWhileOpen()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        component.Find(".bit-clo-acn").Click();
        Assert.IsTrue(component.Instance.IsOpen);

        component.Render(parameters => parameters.Add(p => p.IsEnabled, false));

        // A callout left hanging over the page with a disabled anchor under it could never be closed again.
        Assert.IsFalse(component.Instance.IsOpen);
    }



    [TestMethod]
    public void BitCalloutShouldToggleOnAnchorClick()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        var anchor = component.Find(".bit-clo-acn");

        anchor.Click();
        Assert.IsTrue(component.Instance.IsOpen);

        component.Find(".bit-clo-acn").Click();
        Assert.IsFalse(component.Instance.IsOpen);
    }

    [TestMethod]
    public void BitCalloutShouldCloseOnOverlayClick()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        component.Find(".bit-clo-acn").Click();
        Assert.IsTrue(component.Instance.IsOpen);

        component.Find(".bit-clo-ovl").Click();
        Assert.IsFalse(component.Instance.IsOpen);
    }

    [TestMethod]
    public void BitCalloutShouldNotCloseOnOverlayClickWhenNoDismissOnOutsideClick()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.NoDismissOnOutsideClick, true);
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        component.Find(".bit-clo-acn").Click();
        component.Find(".bit-clo-ovl").Click();

        Assert.IsTrue(component.Instance.IsOpen);
    }

    [TestMethod]
    public void BitCalloutShouldCloseOnEscape()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        component.Find(".bit-clo-acn").Click();
        Assert.IsTrue(component.Instance.IsOpen);

        component.Find(".bit-clo-cal").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.IsFalse(component.Instance.IsOpen);
    }

    [TestMethod]
    public void BitCalloutShouldNotCloseOnEscapeWhenNoDismissOnEscape()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.NoDismissOnEscape, true);
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        component.Find(".bit-clo-acn").Click();
        component.Find(".bit-clo-cal").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.IsTrue(component.Instance.IsOpen);
    }

    [TestMethod]
    public void BitCalloutShouldCloseOnEscapeFromTheAnchor()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        component.Find(".bit-clo-acn").Click();
        Assert.IsTrue(component.Instance.IsOpen);

        // The focus stays on the trigger unless the callout was asked to take it, so Escape has to reach
        // the callout from the anchor as well as from inside the callout.
        component.Find(".bit-clo").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.IsFalse(component.Instance.IsOpen);
    }

    [TestMethod]
    public void BitCalloutShouldNotCloseOnEscapeFromTheAnchorWhenNoDismissOnEscape()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.NoDismissOnEscape, true);
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        component.Find(".bit-clo-acn").Click();
        component.Find(".bit-clo").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.IsTrue(component.Instance.IsOpen);
    }

    [TestMethod]
    public void BitCalloutShouldIgnoreEscapeFromTheAnchorWhileItIsClosed()
    {
        var toggled = new List<bool>();

        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.OnToggle, v => toggled.Add(v));
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        component.Find(".bit-clo").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.IsFalse(component.Instance.IsOpen);
        Assert.AreEqual(0, toggled.Count);
    }

    [TestMethod]
    public void BitCalloutShouldNotLetANestedEscapeDismissTheCalloutItIsNestedIn()
    {
        var component = RenderComponent<BitCalloutNestedTest>();

        component.Find(".outer-callout .bit-clo-acn").Click();
        component.Find(".inner-callout .bit-clo-acn").Click();

        Assert.IsTrue(component.Instance.Outer.IsOpen);
        Assert.IsTrue(component.Instance.Inner.IsOpen);

        // The anchor of the nested callout sits inside the content of the one it is nested in, so an
        // Escape left to carry on up would dismiss both at once.
        component.Find(".inner-callout").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.IsFalse(component.Instance.Inner.IsOpen);
        Assert.IsTrue(component.Instance.Outer.IsOpen);
    }

    [TestMethod]
    public void BitCalloutShouldNotLetANestedAnchorClickAutoCloseTheCalloutItIsNestedIn()
    {
        var component = RenderComponent<BitCalloutNestedTest>(parameters =>
        {
            parameters.Add(p => p.AutoCloseOuter, true);
        });

        component.Find(".outer-callout .bit-clo-acn").Click();
        component.Find(".inner-callout .bit-clo-acn").Click();

        // Opening the nested callout is not the interaction an AutoClose callout is waiting to complete,
        // and closing the outer one would take the callout just opened from it along with it.
        Assert.IsTrue(component.Instance.Inner.IsOpen);
        Assert.IsTrue(component.Instance.Outer.IsOpen);
    }

    [TestMethod]
    public void BitCalloutShouldIgnoreOtherKeys()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        component.Find(".bit-clo-acn").Click();
        component.Find(".bit-clo-cal").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.IsTrue(component.Instance.IsOpen);
    }

    [TestMethod]
    public void BitCalloutShouldCloseOnContentClickWhenAutoClose()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.AutoClose, true);
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
            parameters.AddChildContent("<button class=\"item\">Item</button>");
        });

        component.Find(".bit-clo-acn").Click();
        Assert.IsTrue(component.Instance.IsOpen);

        component.Find(".bit-clo-cal").Click();
        Assert.IsFalse(component.Instance.IsOpen);
    }

    [TestMethod]
    public void BitCalloutShouldStayOpenOnContentClickWithoutAutoClose()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
            parameters.AddChildContent("<button class=\"item\">Item</button>");
        });

        component.Find(".bit-clo-acn").Click();
        component.Find(".bit-clo-cal").Click();

        Assert.IsTrue(component.Instance.IsOpen);
    }



    [TestMethod]
    public async Task BitCalloutShouldOpenAndCloseProgrammatically()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        await component.InvokeAsync(() => component.Instance.Open());
        Assert.IsTrue(component.Instance.IsOpen);

        // Opening an already open callout is a no-op rather than a re-entry.
        await component.InvokeAsync(() => component.Instance.Open());
        Assert.IsTrue(component.Instance.IsOpen);

        await component.InvokeAsync(() => component.Instance.Close());
        Assert.IsFalse(component.Instance.IsOpen);

        await component.InvokeAsync(() => component.Instance.Toggle());
        Assert.IsTrue(component.Instance.IsOpen);

        await component.InvokeAsync(() => component.Instance.Toggle());
        Assert.IsFalse(component.Instance.IsOpen);
    }

    [TestMethod]
    public async Task BitCalloutShouldOpenAtAPointOnTheScreen()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        // The point is only rendered for a callout that was opened at one.
        Assert.AreEqual(0, component.FindAll(".bit-clo-pnt").Count);

        await component.InvokeAsync(() => component.Instance.OpenAt(120, 240));

        Assert.IsTrue(component.Instance.IsOpen);

        var point = component.Find(".bit-clo-pnt");
        var style = point.GetAttribute("style");

        Assert.IsTrue(style!.Contains("left:120px"));
        Assert.IsTrue(style.Contains("top:240px"));
        Assert.AreEqual("true", point.GetAttribute("aria-hidden"));

        // The placement is measured against that point rather than against the anchor.
        component.WaitForAssertion(() => Assert.AreNotEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Callouts.toggle"].Count));

        var arguments = Context.JSInterop.Invocations["BitBlazorUI.Callouts.toggle"][^1].Arguments;

        Assert.AreEqual(point.Id, arguments[1]);
    }

    [TestMethod]
    public async Task BitCalloutShouldMoveAnOpenCalloutToANewPointWithoutReopeningIt()
    {
        var opened = 0;
        var toggled = new List<bool>();

        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.OnOpen, () => opened++);
            parameters.Add(p => p.OnToggle, v => toggled.Add(v));
        });

        await component.InvokeAsync(() => component.Instance.OpenAt(10, 20));
        await component.InvokeAsync(() => component.Instance.OpenAt(30, 40));

        var style = component.Find(".bit-clo-pnt").GetAttribute("style");

        Assert.IsTrue(style!.Contains("left:30px"));
        Assert.IsTrue(style.Contains("top:40px"));

        // Nothing about the open state changed for the second call, so nothing is reported for it.
        Assert.IsTrue(component.Instance.IsOpen);
        Assert.AreEqual(1, opened);
        CollectionAssert.AreEqual(new[] { true }, toggled);

        // The callout never went anywhere, so it is laid out again rather than toggled again: going back
        // through the toggle would replay the entry animation of a menu that has only moved.
        Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Callouts.toggle"].Count);
        Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Callouts.reposition"].Count);
    }

    [TestMethod]
    public async Task BitCalloutShouldLayTheOpenCalloutOutAgainOnDemand()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        // A closed callout has nothing to lay out, and reaching the JS side would place one that is hidden.
        await component.InvokeAsync(() => component.Instance.Reposition());

        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Callouts.reposition"].Count);

        component.Find(".bit-clo-acn").Click();

        await component.InvokeAsync(() => component.Instance.Reposition());

        Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Callouts.reposition"].Count);

        // Laying it out again is not opening it again: the open state, and what the consumer hears about
        // it, are left exactly as they were.
        Assert.IsTrue(component.Instance.IsOpen);
        Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Callouts.toggle"].Count);
    }

    [TestMethod]
    public async Task BitCalloutShouldGiveThePlacementBackToTheAnchorWhenItIsOpenedNormally()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        await component.InvokeAsync(() => component.Instance.OpenAt(10, 20));
        await component.InvokeAsync(() => component.Instance.Close());

        component.Find(".bit-clo-acn").Click();

        Assert.AreEqual(0, component.FindAll(".bit-clo-pnt").Count);

        var arguments = Context.JSInterop.Invocations["BitBlazorUI.Callouts.toggle"][^1].Arguments;

        Assert.AreEqual(component.Find(".bit-clo-acn").Id, arguments[1]);
    }

    [TestMethod]
    public async Task BitCalloutShouldNotOpenAtAPointWhenDisabled()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
        });

        await component.InvokeAsync(() => component.Instance.OpenAt(10, 20));

        Assert.IsFalse(component.Instance.IsOpen);
        Assert.AreEqual(0, component.FindAll(".bit-clo-pnt").Count);
    }

    [TestMethod]
    public async Task BitCalloutShouldNotOpenProgrammaticallyWhenDisabled()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
        });

        await component.InvokeAsync(() => component.Instance.Open());
        Assert.IsFalse(component.Instance.IsOpen);

        await component.InvokeAsync(() => component.Instance.Toggle());
        Assert.IsFalse(component.Instance.IsOpen);
    }

    [TestMethod]
    public async Task BitCalloutShouldCloseFromTheJsSideWhenAnotherCalloutTakesOver()
    {
        var dismissed = 0;
        var toggled = new List<bool>();

        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.OnDismiss, () => dismissed++);
            parameters.Add(p => p.OnToggle, v => toggled.Add(v));
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        component.Find(".bit-clo-acn").Click();

        await component.InvokeAsync(() => component.Instance.CloseCalloutBeforeAnotherCalloutIsOpened());

        Assert.IsFalse(component.Instance.IsOpen);
        Assert.AreEqual(1, dismissed);
        CollectionAssert.AreEqual(new[] { true, false }, toggled);
    }

    [TestMethod]
    public async Task BitCalloutShouldNotReportADismissalThatNeverHappened()
    {
        var dismissed = 0;

        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.OnDismiss, () => dismissed++);
        });

        await component.InvokeAsync(() => component.Instance.CloseCalloutBeforeAnotherCalloutIsOpened());

        Assert.AreEqual(0, dismissed);
    }



    [TestMethod]
    public void BitCalloutShouldFireTheOpenAndToggleCallbacks()
    {
        var opened = 0;
        var dismissed = 0;
        var toggled = new List<bool>();

        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.OnOpen, () => opened++);
            parameters.Add(p => p.OnDismiss, () => dismissed++);
            parameters.Add(p => p.OnToggle, v => toggled.Add(v));
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        component.Find(".bit-clo-acn").Click();
        component.Find(".bit-clo-ovl").Click();

        Assert.AreEqual(1, opened);
        Assert.AreEqual(1, dismissed);
        CollectionAssert.AreEqual(new[] { true, false }, toggled);
    }

    [TestMethod]
    public void BitCalloutShouldFireTheCallbacksForAnIsOpenSetFromOutside()
    {
        var opened = 0;
        var dismissed = 0;

        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.OnOpen, () => opened++);
            parameters.Add(p => p.OnDismiss, () => dismissed++);
            parameters.Add(p => p.IsOpen, false);
        });

        component.Render(parameters => parameters.Add(p => p.IsOpen, true));
        component.WaitForAssertion(() => Assert.AreEqual(1, opened));

        component.Render(parameters => parameters.Add(p => p.IsOpen, false));
        component.WaitForAssertion(() => Assert.AreEqual(1, dismissed));
    }

    [TestMethod]
    public void BitCalloutShouldSupportTwoWayBindingOfIsOpen()
    {
        var isOpen = false;

        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        component.Find(".bit-clo-acn").Click();
        Assert.IsTrue(isOpen);

        component.Find(".bit-clo-ovl").Click();
        Assert.IsFalse(isOpen);
    }

    [TestMethod]
    public void BitCalloutShouldNotChangeAnIsOpenTheParentHoldsWithoutACallback()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        component.Find(".bit-clo-ovl").Click();

        // The state is the parent's to own: without a change callback the callout stays as it was told.
        Assert.IsTrue(component.Instance.IsOpen);
    }

    [TestMethod]
    public void BitCalloutShouldTakeTheDefaultIsOpenWhenIsOpenIsNotSet()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.DefaultIsOpen, true);
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        Assert.IsTrue(component.Instance.IsOpen);
        Assert.IsTrue(component.Find(".bit-clo-cal").ClassList.Contains("bit-clo-ocl"));

        // It is only the starting state: the callout manages it itself from there.
        component.Find(".bit-clo-ovl").Click();
        Assert.IsFalse(component.Instance.IsOpen);
    }

    [TestMethod]
    public void BitCalloutShouldIgnoreTheDefaultIsOpenWhenIsOpenIsSet()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.DefaultIsOpen, true);
            parameters.Add(p => p.IsOpen, false);
        });

        Assert.IsFalse(component.Instance.IsOpen);
    }



    [TestMethod]
    public void BitCalloutShouldRenderTheArrowWhenAsked()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.ShowArrow, true);
        });

        var arrow = component.Find(".bit-clo-arw");

        Assert.AreEqual("true", arrow.GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitCalloutShouldRenderTheArrowSizeCustomProperty()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.ShowArrow, true);
            parameters.Add(p => p.ArrowSize, 20);
            parameters.Add(p => p.Styles, new BitCalloutClassStyles { Arrow = "opacity:0.5;" });
        });

        var style = component.Find(".bit-clo-arw").GetAttribute("style");

        Assert.IsTrue(style!.Contains("--bit-clo-arw-siz:20px"));

        // The consumer's own styles are still applied, and after the size so they win over it.
        Assert.IsTrue(style.Contains("opacity:0.5"));
    }

    [DataTestMethod]
    [DataRow(null)]
    [DataRow(0)]
    [DataRow(-4)]
    public void BitCalloutShouldRenderNoArrowStyleWhenNoUsableSizeIsAsked(int? size)
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.ShowArrow, true);
            parameters.Add(p => p.ArrowSize, size);
        });

        // A size of zero or less would leave no beak at all, so the theme's own size is kept instead.
        Assert.IsNull(component.Find(".bit-clo-arw").GetAttribute("style"));
    }

    [TestMethod]
    public void BitCalloutArrowShouldTakeTheSurfaceOfTheCallout()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.ShowArrow, true);
            parameters.Add(p => p.Background, BitColorKind.Secondary);
            parameters.Add(p => p.Border, BitColorKind.Tertiary);
        });

        var arrow = component.Find(".bit-clo-arw");
        var callout = component.Find(".bit-clo-cal");

        foreach (var cssClass in new[] { "bit-clo-bsg", "bit-clo-brd", "bit-clo-btr" })
        {
            Assert.IsTrue(arrow.ClassList.Contains(cssClass), $"arrow is missing {cssClass}");
            Assert.IsTrue(callout.ClassList.Contains(cssClass), $"callout is missing {cssClass}");
        }
    }

    [DataTestMethod]
    [DataRow(BitColorKind.Primary, "bit-clo-bpg")]
    [DataRow(BitColorKind.Secondary, "bit-clo-bsg")]
    [DataRow(BitColorKind.Tertiary, "bit-clo-btg")]
    [DataRow(BitColorKind.Transparent, "bit-clo-brg")]
    public void BitCalloutShouldRenderTheBackgroundKind(BitColorKind kind, string expectedClass)
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.Background, kind);
        });

        Assert.IsTrue(component.Find(".bit-clo-cal").ClassList.Contains(expectedClass));
    }

    [DataTestMethod]
    [DataRow(BitColorKind.Primary, "bit-clo-bpr")]
    [DataRow(BitColorKind.Secondary, "bit-clo-bsr")]
    [DataRow(BitColorKind.Tertiary, "bit-clo-btr")]
    [DataRow(BitColorKind.Transparent, "bit-clo-brr")]
    public void BitCalloutShouldRenderTheBorderKind(BitColorKind kind, string expectedClass)
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.Border, kind);
        });

        var callout = component.Find(".bit-clo-cal");

        Assert.IsTrue(callout.ClassList.Contains("bit-clo-brd"));
        Assert.IsTrue(callout.ClassList.Contains(expectedClass));
    }

    [TestMethod]
    public void BitCalloutShouldRenderTheNoShadowClass()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.NoShadow, true);
        });

        Assert.IsTrue(component.Find(".bit-clo-cal").ClassList.Contains("bit-clo-nsh"));
    }

    [TestMethod]
    public void BitCalloutShouldDimTheOverlayWhenModal()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.Modal, true);
        });

        Assert.IsTrue(component.Find(".bit-clo-ovl").ClassList.Contains("bit-clo-ovm"));
    }

    [TestMethod]
    public void BitCalloutShouldNotDimTheOverlayByDefault()
    {
        var component = RenderComponent<BitCallout>();

        Assert.IsFalse(component.Find(".bit-clo-ovl").ClassList.Contains("bit-clo-ovm"));
    }

    [TestMethod]
    public void BitCalloutShouldRenderNoOverlayWhenThePageKeepsItsOwnClicks()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.NoOverlay, true);
        });

        Assert.AreEqual(0, component.FindAll(".bit-clo-ovl").Count);

        // Everything else about the callout is unchanged: only the element between it and the page is gone.
        Assert.IsNotNull(component.Find(".bit-clo-cal"));
    }

    [TestMethod]
    public void BitCalloutShouldKeepTheOverlayOfAModalCalloutAskedForNone()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.Modal, true);
            parameters.Add(p => p.NoOverlay, true);
        });

        // The overlay is what dims the page and holds it still, so a modal callout keeps it.
        Assert.IsTrue(component.Find(".bit-clo-ovl").ClassList.Contains("bit-clo-ovm"));
    }

    [TestMethod]
    public void BitCalloutShouldPassNoOverlayIdToThePositioningWhenItRendersNone()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.NoOverlay, true);
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        component.Find(".bit-clo-acn").Click();

        var arguments = Context.JSInterop.Invocations["BitBlazorUI.Callouts.toggle"][^1].Arguments;

        // An empty id is how the JS side is told that there is no overlay to take the outside clicks for
        // this callout, and that the page-level handler has to dismiss it instead.
        Assert.AreEqual(string.Empty, arguments[5]);
    }

    [TestMethod]
    public void BitCalloutShouldPassTheOverlayIdToThePositioningWhenItRendersOne()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        component.Find(".bit-clo-acn").Click();

        var arguments = Context.JSInterop.Invocations["BitBlazorUI.Callouts.toggle"][^1].Arguments;

        Assert.AreEqual(component.Find(".bit-clo-ovl").Id, arguments[5]);
    }

    [TestMethod]
    public void BitCalloutShouldHoldThePageStillWhileAModalOneIsOpen()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.Modal, true);
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        component.Find(".bit-clo-acn").Click();

        var opened = Context.JSInterop.Invocations["BitBlazorUI.Utils.toggleOverflow"][^1];

        // The first argument is the key the hold is counted under; the scroller is the second.
        Assert.AreEqual("body", opened.Arguments[1]);
        Assert.AreEqual(true, opened.Arguments[2]);

        component.Find(".bit-clo-ovl").Click();

        // The page is handed back its scrolling when the callout that took it away goes.
        var closed = Context.JSInterop.Invocations["BitBlazorUI.Utils.toggleOverflow"][^1];

        Assert.AreEqual("body", closed.Arguments[1]);
        Assert.AreEqual(false, closed.Arguments[2]);
    }

    [TestMethod]
    public void BitCalloutShouldNotHoldThePageStillWhenItIsNotModal()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        component.Find(".bit-clo-acn").Click();

        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.toggleOverflow"].Count);
    }

    [TestMethod]
    public void BitCalloutShouldReleaseThePageWhenModalIsTurnedOffWhileItIsOpen()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.Modal, true);
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        component.Find(".bit-clo-acn").Click();

        component.Render(parameters =>
        {
            parameters.Add(p => p.Modal, false);
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        var released = Context.JSInterop.Invocations["BitBlazorUI.Utils.toggleOverflow"][^1];

        Assert.AreEqual("body", released.Arguments[1]);
        Assert.AreEqual(false, released.Arguments[2]);
    }



    [TestMethod]
    public void BitCalloutShouldRenderTheSizingCustomProperties()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.Width, "20rem");
            parameters.Add(p => p.MinWidth, "10rem");
            parameters.Add(p => p.MaxWidth, "30rem");
            parameters.Add(p => p.MaxHeight, "12rem");
        });

        var callout = component.Find(".bit-clo-cal");
        var style = callout.GetAttribute("style");

        Assert.IsTrue(style!.Contains("--bit-clo-wid:20rem"));
        Assert.IsTrue(style.Contains("--bit-clo-mnw:10rem"));
        Assert.IsTrue(style.Contains("--bit-clo-mxw:30rem"));
        Assert.IsTrue(style.Contains("--bit-clo-mxh:12rem"));

        Assert.IsTrue(callout.ClassList.Contains("bit-clo-mxh"));
        Assert.IsTrue(callout.ClassList.Contains("bit-clo-mxw"));
        Assert.IsTrue(callout.ClassList.Contains("bit-clo-wid"));

        // A callout the consumer caps by hand is no longer the positioning code's to cap.
        Assert.IsFalse(callout.ClassList.Contains("bit-clo-fit"));
    }

    [TestMethod]
    public void BitCalloutShouldDropTheContentWidthFloorForAWidthOfItsOwn()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.Width, "20rem");
        });

        // A callout is as wide as its content asks to be, which is a min-width of max-content; that floor
        // would win over the width the consumer asked for, so a callout given one drops it and lets the
        // content wrap inside the width instead of stretching the callout past it.
        Assert.IsTrue(component.Find(".bit-clo-cal").ClassList.Contains("bit-clo-wid"));
    }

    [TestMethod]
    public void BitCalloutShouldKeepTheContentWidthFloorWhenNoWidthIsAsked()
    {
        var component = RenderComponent<BitCallout>();

        var callout = component.Find(".bit-clo-cal");

        Assert.IsFalse(callout.ClassList.Contains("bit-clo-wid"));
        Assert.IsFalse(callout.ClassList.Contains("bit-clo-mxw"));
    }

    [TestMethod]
    public void BitCalloutShouldCapItselfToTheViewportWhenNothingElseDoes()
    {
        var component = RenderComponent<BitCallout>();

        Assert.IsTrue(component.Find(".bit-clo-cal").ClassList.Contains("bit-clo-fit"));
    }

    [TestMethod]
    public void BitCalloutShouldLeaveTheCappingToTheNamedScrollContainer()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.ScrollContainerId, "scroller");
        });

        Assert.IsFalse(component.Find(".bit-clo-cal").ClassList.Contains("bit-clo-fit"));
    }

    [TestMethod]
    public void BitCalloutShouldRenderNoSizingStyleWhenNoneIsAsked()
    {
        var component = RenderComponent<BitCallout>();

        Assert.IsNull(component.Find(".bit-clo-cal").GetAttribute("style"));
    }



    [TestMethod]
    public void BitCalloutShouldRenderTheContentUpFrontByDefault()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.AddChildContent("<div class=\"content\">Hello</div>");
        });

        Assert.IsTrue(component.Find(".bit-clo-cal").OuterHtml.Contains("Hello"));
    }

    [TestMethod]
    public void BitCalloutShouldKeepALazyContentOutOfThePageUntilItIsOpened()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.LazyRender, true);
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
            parameters.AddChildContent("<div class=\"content\">Hello</div>");
        });

        // The callout itself is always rendered: it is the element the positioning code shows and measures.
        Assert.IsNotNull(component.Find(".bit-clo-cal"));
        Assert.AreEqual(0, component.FindAll(".content").Count);

        component.Find(".bit-clo-acn").Click();

        Assert.IsTrue(component.Instance.IsOpen);
        component.WaitForAssertion(() => Assert.AreEqual(1, component.FindAll(".content").Count));

        // Once rendered the content stays, so whatever state it holds survives the callout closing.
        component.Find(".bit-clo-ovl").Click();

        Assert.IsFalse(component.Instance.IsOpen);
        Assert.AreEqual(1, component.FindAll(".content").Count);
    }

    [TestMethod]
    public void BitCalloutShouldPlaceALazyContentOnlyOnceItIsInThePage()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.LazyRender, true);
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
            parameters.AddChildContent("<div class=\"content\">Hello</div>");
        });

        component.Find(".bit-clo-acn").Click();

        // The placement is measured against what is in the callout, so the opening waits for the render
        // that puts the content there - but it still gets there.
        component.WaitForAssertion(() => Assert.AreNotEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Callouts.toggle"].Count));

        var arguments = Context.JSInterop.Invocations["BitBlazorUI.Callouts.toggle"][^1].Arguments;

        Assert.AreEqual(true, arguments[6]);
    }

    [TestMethod]
    public void BitCalloutShouldRenderALazyContentThatStartsOutOpen()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.LazyRender, true);
            parameters.Add(p => p.DefaultIsOpen, true);
            parameters.AddChildContent("<div class=\"content\">Hello</div>");
        });

        Assert.AreEqual(1, component.FindAll(".content").Count);
    }

    [TestMethod]
    public void BitCalloutShouldRenderALazyContentForAnIsOpenSetFromOutside()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.LazyRender, true);
            parameters.Add(p => p.IsOpen, false);
            parameters.AddChildContent("<div class=\"content\">Hello</div>");
        });

        Assert.AreEqual(0, component.FindAll(".content").Count);

        component.Render(parameters =>
        {
            parameters.Add(p => p.LazyRender, true);
            parameters.Add(p => p.IsOpen, true);
            parameters.AddChildContent("<div class=\"content\">Hello</div>");
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.FindAll(".content").Count));
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void BitCalloutShouldPassTheForcedSideToThePositioning(bool noFlip)
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.Side, BitCalloutSide.Top);
            parameters.Add(p => p.NoFlip, noFlip);
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        component.Find(".bit-clo-acn").Click();

        var arguments = Context.JSInterop.Invocations["BitBlazorUI.Callouts.toggle"][^1].Arguments;

        Assert.AreEqual(noFlip, arguments[23]);
    }

    [TestMethod]
    public void BitCalloutShouldPassTheCollisionPaddingToThePositioning()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.CollisionPadding, 24);
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        component.Find(".bit-clo-acn").Click();

        var arguments = Context.JSInterop.Invocations["BitBlazorUI.Callouts.toggle"][^1].Arguments;

        Assert.AreEqual(24, arguments[24]);
    }

    [TestMethod]
    public void BitCalloutShouldPassNoCollisionPaddingByDefault()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        component.Find(".bit-clo-acn").Click();

        var arguments = Context.JSInterop.Invocations["BitBlazorUI.Callouts.toggle"][^1].Arguments;

        Assert.AreEqual(0, arguments[24]);
    }

    [TestMethod]
    public async Task BitCalloutShouldStayClosedWhenALazyOpeningIsClosedAgainImmediately()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.LazyRender, true);
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
            parameters.AddChildContent("<div class=\"content\">Hello</div>");
        });

        // The first opening of a lazy callout waits for the render that puts its content in the page, so a
        // close that lands around that render must not leave the deferred opening to go through afterwards.
        await component.InvokeAsync(() => component.Instance.Open());
        await component.InvokeAsync(() => component.Instance.Close());

        component.Render();

        Assert.IsFalse(component.Instance.IsOpen);
        Assert.IsFalse(component.Find(".bit-clo-cal").ClassList.Contains("bit-clo-ocl"));
    }



    [TestMethod]
    public void BitCalloutShouldRenderNoSectionsByDefault()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.AddChildContent("<div class=\"content\">Hello</div>");
        });

        Assert.AreEqual(0, component.FindAll(".bit-clo-hdr").Count);
        Assert.AreEqual(0, component.FindAll(".bit-clo-bdy").Count);
        Assert.AreEqual(0, component.FindAll(".bit-clo-ftr").Count);
        Assert.IsFalse(component.Find(".bit-clo-cal").ClassList.Contains("bit-clo-sec"));
    }

    [TestMethod]
    public void BitCalloutShouldRenderTheHeaderTheBodyAndTheFooter()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.Header, Markup("<span>Title</span>"));
            parameters.Add(p => p.Footer, Markup("<span>Actions</span>"));
            parameters.AddChildContent("<div class=\"content\">Hello</div>");
        });

        var callout = component.Find(".bit-clo-cal");

        Assert.IsTrue(callout.ClassList.Contains("bit-clo-sec"));
        Assert.IsTrue(component.Find(".bit-clo-hdr").OuterHtml.Contains("Title"));
        Assert.IsTrue(component.Find(".bit-clo-bdy").OuterHtml.Contains("Hello"));
        Assert.IsTrue(component.Find(".bit-clo-ftr").OuterHtml.Contains("Actions"));

        // The body is the scroller between them, so the callout is no longer the one being capped.
        Assert.IsFalse(callout.ClassList.Contains("bit-clo-fit"));
    }

    [TestMethod]
    public void BitCalloutShouldRenderTheBodyForAFooterAlone()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.Footer, Markup("<span>Actions</span>"));
            parameters.AddChildContent("<div class=\"content\">Hello</div>");
        });

        Assert.AreEqual(0, component.FindAll(".bit-clo-hdr").Count);
        Assert.IsTrue(component.Find(".bit-clo-bdy").OuterHtml.Contains("Hello"));
        Assert.IsTrue(component.Find(".bit-clo-ftr").OuterHtml.Contains("Actions"));
    }

    [TestMethod]
    public void BitCalloutShouldWireTheSectionsUpInThePositioning()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.Header, Markup("<span>Title</span>"));
            parameters.Add(p => p.Footer, Markup("<span>Actions</span>"));
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        component.Find(".bit-clo-acn").Click();

        var arguments = Context.JSInterop.Invocations["BitBlazorUI.Callouts.toggle"][^1].Arguments;

        Assert.AreEqual(component.Find(".bit-clo-bdy").Id, arguments[10]);
        Assert.AreEqual(component.Find(".bit-clo-hdr").Id, arguments[12]);
        Assert.AreEqual(component.Find(".bit-clo-ftr").Id, arguments[13]);
    }

    [TestMethod]
    public void BitCalloutShouldLetTheNamedElementsWinOverTheSectionsItRenders()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.Header, Markup("<span>Title</span>"));
            parameters.Add(p => p.Footer, Markup("<span>Actions</span>"));
            parameters.Add(p => p.ScrollContainerId, "scroller");
            parameters.Add(p => p.HeaderId, "my-header");
            parameters.Add(p => p.FooterId, "my-footer");
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        component.Find(".bit-clo-acn").Click();

        var arguments = Context.JSInterop.Invocations["BitBlazorUI.Callouts.toggle"][^1].Arguments;

        Assert.AreEqual("scroller", arguments[10]);
        Assert.AreEqual("my-header", arguments[12]);
        Assert.AreEqual("my-footer", arguments[13]);
    }

    [TestMethod]
    public void BitCalloutShouldApplyTheCustomClassesAndStylesToTheSections()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.Header, Markup("<span>Title</span>"));
            parameters.Add(p => p.Footer, Markup("<span>Actions</span>"));
            parameters.Add(p => p.Classes, new BitCalloutClassStyles
            {
                Header = "custom-header",
                Body = "custom-body",
                Footer = "custom-footer"
            });
            parameters.Add(p => p.Styles, new BitCalloutClassStyles
            {
                Header = "color:red;",
                Body = "color:green;",
                Footer = "color:blue;"
            });
        });

        Assert.IsTrue(component.Find(".bit-clo-hdr").ClassList.Contains("custom-header"));
        Assert.IsTrue(component.Find(".bit-clo-bdy").ClassList.Contains("custom-body"));
        Assert.IsTrue(component.Find(".bit-clo-ftr").ClassList.Contains("custom-footer"));

        Assert.IsTrue(component.Find(".bit-clo-hdr").GetAttribute("style")!.Contains("color:red"));
        Assert.IsTrue(component.Find(".bit-clo-bdy").GetAttribute("style")!.Contains("color:green"));
        Assert.IsTrue(component.Find(".bit-clo-ftr").GetAttribute("style")!.Contains("color:blue"));
    }



    [DataTestMethod]
    [DataRow(BitResponsiveMode.Panel, null, "bit-clo-end")]
    [DataRow(BitResponsiveMode.Panel, BitPanelPosition.Start, "bit-clo-sta")]
    [DataRow(BitResponsiveMode.Panel, BitPanelPosition.End, "bit-clo-end")]
    [DataRow(BitResponsiveMode.Top, null, "bit-clo-top")]
    [DataRow(BitResponsiveMode.Bottom, null, "bit-clo-btm")]
    public void BitCalloutShouldRenderTheResponsivePanelClasses(BitResponsiveMode mode, BitPanelPosition? position, string expectedClass)
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.ResponsiveMode, mode);
            parameters.Add(p => p.PanelPosition, position);
        });

        var callout = component.Find(".bit-clo-cal");

        Assert.IsTrue(callout.ClassList.Contains("bit-clo-res"));
        Assert.IsTrue(callout.ClassList.Contains(expectedClass));
    }

    [TestMethod]
    public void BitCalloutShouldNotRenderTheResponsiveClassesWhenTheModeIsNone()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.ResponsiveMode, BitResponsiveMode.None);
        });

        Assert.IsFalse(component.Find(".bit-clo-cal").ClassList.Contains("bit-clo-res"));
    }



    [TestMethod]
    public void BitCalloutShouldReportItselfAsADialogWhenItTrapsTheFocus()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.TrapFocus, true);
        });

        var callout = component.Find(".bit-clo-cal");

        Assert.AreEqual("dialog", callout.GetAttribute("role"));
        Assert.AreEqual("true", callout.GetAttribute("aria-modal"));
    }

    [TestMethod]
    public void BitCalloutShouldRenderNoRoleByDefault()
    {
        var component = RenderComponent<BitCallout>();

        var callout = component.Find(".bit-clo-cal");

        Assert.IsNull(callout.GetAttribute("role"));
        Assert.IsNull(callout.GetAttribute("aria-modal"));

        // The callout takes the focus itself when its content holds nothing focusable of its own.
        Assert.AreEqual("-1", callout.GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitCalloutShouldRenderTheGivenRoleAndAriaLabel()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.Role, "status");
            parameters.Add(p => p.AriaLabel, "Sync status");
        });

        var callout = component.Find(".bit-clo-cal");

        Assert.AreEqual("status", callout.GetAttribute("role"));
        Assert.AreEqual("Sync status", callout.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitCalloutShouldReportItselfAsAGroupWhenItIsGivenANameAndNoRole()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, "Filters");
        });

        var callout = component.Find(".bit-clo-cal");

        // A name on a plain container is one no screen reader announces.
        Assert.AreEqual("group", callout.GetAttribute("role"));
        Assert.AreEqual("Filters", callout.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitCalloutShouldStillReportItselfAsADialogWhenItIsNamedAndTrapsTheFocus()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.TrapFocus, true);
            parameters.Add(p => p.AriaLabel, "Filters");
        });

        Assert.AreEqual("dialog", component.Find(".bit-clo-cal").GetAttribute("role"));
    }

    [TestMethod]
    public void BitCalloutShouldLetTheGivenRoleWinOverTheDialogOne()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.TrapFocus, true);
            parameters.Add(p => p.Role, "alertdialog");
        });

        Assert.AreEqual("alertdialog", component.Find(".bit-clo-cal").GetAttribute("role"));
    }



    [TestMethod]
    public void BitCalloutShouldCarryTheDirectionOverToTheCallout()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Rtl);
        });

        var callout = component.Find(".bit-clo-cal");

        // The callout is relocated to the body while it is open, so it cannot inherit either of these.
        Assert.AreEqual("rtl", callout.GetAttribute("dir"));
        Assert.IsTrue(callout.ClassList.Contains("bit-rtl"));
    }

    [TestMethod]
    public void BitCalloutShouldCarryForceAnimationOverToTheCallout()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.ForceAnimation, true);
        });

        Assert.IsTrue(component.Find(".bit-clo-cal").ClassList.Contains("bit-fam"));
        Assert.IsTrue(component.Find(".bit-clo").ClassList.Contains("bit-fam"));
    }

    [TestMethod]
    public void BitCalloutShouldMarkTheRootWhileItIsOpen()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
        });

        Assert.IsTrue(component.Find(".bit-clo").ClassList.Contains("bit-clo-opn"));
    }



    [TestMethod]
    public void BitCalloutShouldApplyTheCustomClasses()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.ShowArrow, true);
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
            parameters.Add(p => p.Classes, new BitCalloutClassStyles
            {
                Root = "custom-root",
                Opened = "custom-opened",
                AnchorContainer = "custom-anchor",
                Arrow = "custom-arrow",
                Content = "custom-content",
                Overlay = "custom-overlay"
            });
        });

        Assert.IsTrue(component.Find(".bit-clo").ClassList.Contains("custom-root"));
        Assert.IsTrue(component.Find(".bit-clo").ClassList.Contains("custom-opened"));
        Assert.IsTrue(component.Find(".bit-clo-acn").ClassList.Contains("custom-anchor"));
        Assert.IsTrue(component.Find(".bit-clo-arw").ClassList.Contains("custom-arrow"));
        Assert.IsTrue(component.Find(".bit-clo-cal").ClassList.Contains("custom-content"));
        Assert.IsTrue(component.Find(".bit-clo-ovl").ClassList.Contains("custom-overlay"));
    }

    [TestMethod]
    public void BitCalloutShouldApplyTheCustomStyles()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.ShowArrow, true);
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
            parameters.Add(p => p.Styles, new BitCalloutClassStyles
            {
                Root = "color:red;",
                Opened = "background:blue;",
                AnchorContainer = "padding:4px;",
                Arrow = "opacity:0.5;",
                Content = "margin:2px;",
                Overlay = "cursor:pointer;"
            });
        });

        Assert.IsTrue(component.Find(".bit-clo").GetAttribute("style")!.Contains("color:red"));
        Assert.IsTrue(component.Find(".bit-clo").GetAttribute("style")!.Contains("background:blue"));
        Assert.IsTrue(component.Find(".bit-clo-acn").GetAttribute("style")!.Contains("padding:4px"));
        Assert.IsTrue(component.Find(".bit-clo-arw").GetAttribute("style")!.Contains("opacity:0.5"));
        Assert.IsTrue(component.Find(".bit-clo-cal").GetAttribute("style")!.Contains("margin:2px"));
        Assert.IsTrue(component.Find(".bit-clo-ovl").GetAttribute("style")!.Contains("cursor:pointer"));
    }

    [TestMethod]
    public void BitCalloutShouldNotApplyTheOpenedClassAndStyleWhileClosed()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.Classes, new BitCalloutClassStyles { Opened = "custom-opened" });
            parameters.Add(p => p.Styles, new BitCalloutClassStyles { Opened = "background:blue;" });
        });

        var root = component.Find(".bit-clo");

        Assert.IsFalse(root.ClassList.Contains("custom-opened"));
        Assert.IsFalse(root.GetAttribute("style")?.Contains("background:blue") ?? false);
    }

    [TestMethod]
    public void BitCalloutShouldRenderTheIdClassAndStyleOnTheRoot()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.Id, "my-callout");
            parameters.Add(p => p.Class, "my-class");
            parameters.Add(p => p.Style, "color:green;");
        });

        var root = component.Find(".bit-clo");

        Assert.AreEqual("my-callout", root.Id);
        Assert.IsTrue(root.ClassList.Contains("my-class"));
        Assert.IsTrue(root.GetAttribute("style")!.Contains("color:green"));
    }

    [TestMethod]
    public void BitCalloutShouldRenderTheUnmatchedHtmlAttributesOnTheRoot()
    {
        var component = RenderComponent<BitCalloutHtmlAttributesTest>();

        var root = component.Find(".bit-clo");

        Assert.AreEqual("bit", root.GetAttribute("data-val-test"));
    }

    [DataTestMethod]
    [DataRow(null, "")]
    [DataRow(BitCalloutSide.Top, "top")]
    [DataRow(BitCalloutSide.Bottom, "bottom")]
    [DataRow(BitCalloutSide.Start, "start")]
    [DataRow(BitCalloutSide.End, "end")]
    public void BitCalloutShouldPassThePreferredSideToThePositioning(BitCalloutSide? side, string expected)
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.Side, side);
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        component.Find(".bit-clo-acn").Click();

        var toggle = Context.JSInterop.Invocations["BitBlazorUI.Callouts.toggle"];

        // The preferred side, the alignment and the forced side are the last three arguments.
        Assert.AreEqual(expected, toggle[^1].Arguments[21]);
    }

    [DataTestMethod]
    [DataRow(null, "")]
    [DataRow(BitCalloutAlignment.Start, "")]
    [DataRow(BitCalloutAlignment.Center, "center")]
    [DataRow(BitCalloutAlignment.End, "end")]
    public void BitCalloutShouldPassTheAlignmentToThePositioning(BitCalloutAlignment? alignment, string expected)
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.Alignment, alignment);
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        component.Find(".bit-clo-acn").Click();

        var toggle = Context.JSInterop.Invocations["BitBlazorUI.Callouts.toggle"];

        // The start-edge default travels as an empty string, which is what every component without the
        // choice passes.
        Assert.AreEqual(expected, toggle[^1].Arguments[22]);
    }

    [TestMethod]
    public void BitCalloutShouldPassTheArrowTheGapAndTheDismissalOptOutToThePositioning()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.ShowArrow, true);
            parameters.Add(p => p.Gap, 12);
            parameters.Add(p => p.NoDismissOnOutsideClick, true);
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        component.Find(".bit-clo-acn").Click();

        var arguments = Context.JSInterop.Invocations["BitBlazorUI.Callouts.toggle"][^1].Arguments;

        Assert.AreEqual(component.Find(".bit-clo-arw").Id, arguments[18]);
        Assert.AreEqual(12, arguments[19]);
        Assert.AreEqual(true, arguments[20]);
    }

    [TestMethod]
    public void BitCalloutShouldPassTheAlignmentOffsetAndTheArrowPaddingToThePositioning()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.AlignmentOffset, 24);
            parameters.Add(p => p.ArrowPadding, 32);
            parameters.Add(p => p.ShowArrow, true);
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        component.Find(".bit-clo-acn").Click();

        var arguments = Context.JSInterop.Invocations["BitBlazorUI.Callouts.toggle"][^1].Arguments;

        Assert.AreEqual(24, arguments[25]);
        Assert.AreEqual(32, arguments[26]);
    }

    [TestMethod]
    public void BitCalloutShouldPassNoAlignmentOffsetAndNoArrowPaddingByDefault()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        component.Find(".bit-clo-acn").Click();

        var arguments = Context.JSInterop.Invocations["BitBlazorUI.Callouts.toggle"][^1].Arguments;

        // Zero is what leaves the callout on the edge the alignment lined it up with, and what leaves the
        // arrow the distance from the corners the placement keeps on its own.
        Assert.AreEqual(0, arguments[25]);
        Assert.AreEqual(0, arguments[26]);
    }

    [TestMethod]
    public void BitCalloutShouldNotPassAnArrowIdWhenNoArrowIsShown()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        component.Find(".bit-clo-acn").Click();

        var arguments = Context.JSInterop.Invocations["BitBlazorUI.Callouts.toggle"][^1].Arguments;

        Assert.AreEqual("", arguments[18]);
        Assert.AreEqual(false, arguments[20]);
    }

    [TestMethod]
    public void BitCalloutShouldTakeOverTheCappingOfItsOwnContentWhenNothingElseDoesIt()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        component.Find(".bit-clo-acn").Click();

        var arguments = Context.JSInterop.Invocations["BitBlazorUI.Callouts.toggle"][^1].Arguments;

        // With no scroll container named, the callout itself is what the positioning code caps to the
        // room the viewport leaves, so a content taller than the screen scrolls inside it.
        Assert.AreEqual(component.Find(".bit-clo-cal").Id, arguments[10]);
    }

    [TestMethod]
    public void BitCalloutShouldLeaveTheCappingToTheNamedScrollContainerInThePositioning()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.ScrollContainerId, "scroller");
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        component.Find(".bit-clo-acn").Click();

        var arguments = Context.JSInterop.Invocations["BitBlazorUI.Callouts.toggle"][^1].Arguments;

        Assert.AreEqual("scroller", arguments[10]);
    }

    [TestMethod]
    public void BitCalloutShouldPassTheRemainingPositioningOptionsToThePositioning()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Rtl);
            parameters.Add(p => p.Direction, BitDropDirection.All);
            parameters.Add(p => p.ResponsiveMode, BitResponsiveMode.Panel);
            parameters.Add(p => p.ScrollOffset, 18);
            parameters.Add(p => p.SetCalloutWidth, true);
            parameters.Add(p => p.FixedCalloutWidth, true);
            parameters.Add(p => p.MaxWindowWidth, 640);
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        component.Find(".bit-clo-acn").Click();

        var arguments = Context.JSInterop.Invocations["BitBlazorUI.Callouts.toggle"][^1].Arguments;

        Assert.AreEqual(BitResponsiveMode.Panel, arguments[7]);
        Assert.AreEqual(BitDropDirection.All, arguments[8]);
        Assert.AreEqual(true, arguments[9]);
        Assert.AreEqual(18, arguments[11]);
        Assert.AreEqual(true, arguments[14]);
        Assert.AreEqual(true, arguments[15]);
        Assert.AreEqual(640, arguments[16]);
    }

    [TestMethod]
    public void BitCalloutShouldPassTheDefaultPositioningOptions()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        component.Find(".bit-clo-acn").Click();

        var arguments = Context.JSInterop.Invocations["BitBlazorUI.Callouts.toggle"][^1].Arguments;

        // A callout that asks for nothing drops above or below its anchor, is laid out left to right,
        // takes the width of its content and is kept within the screen at every window width.
        Assert.AreEqual(BitResponsiveMode.None, arguments[7]);
        Assert.AreEqual(BitDropDirection.TopAndBottom, arguments[8]);
        Assert.AreEqual(false, arguments[9]);
        Assert.AreEqual(0, arguments[11]);
        Assert.AreEqual(false, arguments[14]);
        Assert.AreEqual(false, arguments[15]);
        Assert.AreEqual(0, arguments[16]);

        // The cap on the scrollable content is the callout's own stylesheet business, so the positioning
        // is always left to decide it against the room the viewport leaves.
        Assert.AreEqual(0, arguments[17]);
    }

    [TestMethod]
    public void BitCalloutShouldMoveTheFocusIntoItWhenItOpensWithAutoFocus()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.AutoFocus, true);
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
            parameters.AddChildContent("<button>Inside</button>");
        });

        component.Find(".bit-clo-acn").Click();

        var focus = Context.JSInterop.Invocations["BitBlazorUI.Utils.focusFirstElement"];

        Assert.AreEqual(1, focus.Count);
        Assert.AreEqual(component.Find(".bit-clo-cal").Id, focus[^1].Arguments[0]);
    }

    [TestMethod]
    public void BitCalloutShouldLeaveTheFocusOnTheTriggerWithoutAutoFocus()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
            parameters.AddChildContent("<button>Inside</button>");
        });

        component.Find(".bit-clo-acn").Click();

        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.focusFirstElement"].Count);
    }

    [TestMethod]
    public void BitCalloutShouldTakeTheFocusForItselfWhenItTrapsIt()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            // A trapped callout has to hold the focus to trap it: left on the trigger, the very first Tab
            // would run out of the callout, which only ever sees the keys pressed inside of it.
            parameters.Add(p => p.TrapFocus, true);
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
            parameters.AddChildContent("<button>Inside</button>");
        });

        component.Find(".bit-clo-acn").Click();

        var calloutId = component.Find(".bit-clo-cal").Id;

        Assert.AreEqual(calloutId, Context.JSInterop.Invocations["BitBlazorUI.Utils.focusFirstElement"][^1].Arguments[0]);
        Assert.AreEqual(calloutId, Context.JSInterop.Invocations["BitBlazorUI.Utils.setupFocusTrap"][^1].Arguments[0]);
    }

    [TestMethod]
    public void BitCalloutShouldLetTheKeyboardBackOutWhenItCloses()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.TrapFocus, true);
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
            parameters.AddChildContent("<button>Inside</button>");
        });

        component.Find(".bit-clo-acn").Click();
        component.Find(".bit-clo-ovl").Click();

        Assert.AreEqual(component.Find(".bit-clo-cal").Id,
                        Context.JSInterop.Invocations["BitBlazorUI.Utils.disposeFocusTrap"][^1].Arguments[0]);
    }

    [TestMethod]
    public void BitCalloutShouldTrapTheFocusWhenItIsTurnedOnWhileItIsOpen()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.setupFocusTrap"].Count);

        component.Render(parameters => parameters.Add(p => p.TrapFocus, true));

        Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.setupFocusTrap"].Count);

        component.Render(parameters => parameters.Add(p => p.TrapFocus, false));

        Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.disposeFocusTrap"].Count);
    }

    [TestMethod]
    public void BitCalloutShouldHandTheFocusBackToTheAnchorWhenItWasItsToHandBack()
    {
        Context.JSInterop.Setup<bool>("BitBlazorUI.Utils.containsActiveElement", _ => true).SetResult(true);

        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
            parameters.AddChildContent("<button>Inside</button>");
        });

        component.Find(".bit-clo-acn").Click();
        component.Find(".bit-clo-ovl").Click();

        // The element the focus was on goes with the callout, which would leave the keyboard back at the
        // top of the page, so it goes to the anchor it came from.
        Assert.AreEqual(component.Find(".bit-clo-acn").Id,
                        Context.JSInterop.Invocations["BitBlazorUI.Utils.focusFirstElement"][^1].Arguments[0]);
    }

    [TestMethod]
    public void BitCalloutShouldLeaveTheFocusWhereItIsWhenItWasNeverItsToHandBack()
    {
        Context.JSInterop.Setup<bool>("BitBlazorUI.Utils.containsActiveElement", _ => true).SetResult(false);

        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
            parameters.AddChildContent("<button>Inside</button>");
        });

        component.Find(".bit-clo-acn").Click();
        component.Find(".bit-clo-ovl").Click();

        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.focusFirstElement"].Count);
    }

    [TestMethod]
    public void BitCalloutShouldOpenOnHoverOnADeviceThatCanHover()
    {
        Context.JSInterop.Setup<bool>("BitBlazorUI.Utils.isHoverDevice").SetResult(true);

        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.OpenOnHover, true);
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        component.Find(".bit-clo").MouseEnter();

        Assert.IsTrue(component.Instance.IsOpen);

        component.Find(".bit-clo").MouseLeave();

        component.WaitForAssertion(() => Assert.IsFalse(component.Instance.IsOpen));
    }

    [TestMethod]
    public void BitCalloutShouldStayOpenWhileThePointerIsInTheCallout()
    {
        Context.JSInterop.Setup<bool>("BitBlazorUI.Utils.isHoverDevice").SetResult(true);

        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.OpenOnHover, true);
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        component.Find(".bit-clo").MouseEnter();
        component.Find(".bit-clo").MouseLeave();

        // The close is what bridges the gap between the anchor and the callout, so arriving in the callout
        // before the delay is up calls it off: the pointer is on its way to what it is about to read.
        component.Find(".bit-clo-cal").MouseEnter();

        Assert.IsTrue(component.Instance.IsOpen);
    }

    [TestMethod]
    public void BitCalloutShouldNotOpenOnHoverOnADeviceThatCannotHover()
    {
        // A tap on a touch screen reports a mouseover of its own, which would fight the click that is also
        // meant to toggle the callout, so the mode turns itself off there.
        Context.JSInterop.Setup<bool>("BitBlazorUI.Utils.isHoverDevice").SetResult(false);

        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.OpenOnHover, true);
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        component.Find(".bit-clo").MouseEnter();

        Assert.IsFalse(component.Instance.IsOpen);

        // The click is what is left to reach it with.
        component.Find(".bit-clo-acn").Click();

        Assert.IsTrue(component.Instance.IsOpen);
    }

    [TestMethod]
    public void BitCalloutShouldLetTheOverlayPassThePointerThroughInTheHoverMode()
    {
        Context.JSInterop.Setup<bool>("BitBlazorUI.Utils.isHoverDevice").SetResult(true);

        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.OpenOnHover, true);
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        // The overlay covers the whole page while the callout is open, so it would otherwise swallow the
        // very mouseover events the mode is driven by.
        Assert.IsTrue(component.Find(".bit-clo-ovl").ClassList.Contains("bit-clo-ovh"));
    }

    [TestMethod]
    public void BitCalloutShouldNotLetTheOverlayPassThePointerThroughWithoutTheHoverMode()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        Assert.IsFalse(component.Find(".bit-clo-ovl").ClassList.Contains("bit-clo-ovh"));
    }

    [TestMethod]
    public void BitCalloutShouldRegisterTheSwipeGesturesForAResponsivePanel()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.ResponsiveMode, BitResponsiveMode.Bottom);
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        var setup = Context.JSInterop.Invocations["BitBlazorUI.Swipes.setup"];

        Assert.AreEqual(1, setup.Count);
        Assert.AreEqual(component.Find(".bit-clo-cal").Id, setup[^1].Arguments[0]);
        // A sheet is swiped away along the axis it slid in on, and the lock is what takes that axis from
        // the page underneath it.
        Assert.AreEqual(BitPanelPosition.Bottom, setup[^1].Arguments[2]);
        Assert.AreEqual(BitSwipeOrientation.Vertical, setup[^1].Arguments[4]);
    }

    [TestMethod]
    public void BitCalloutShouldNotRegisterTheSwipeGesturesWithoutAResponsiveMode()
    {
        RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Swipes.setup"].Count);
    }

    [TestMethod]
    public void BitCalloutShouldRegisterTheSwipeGesturesAgainWhenTheirGeometryChanges()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.ResponsiveMode, BitResponsiveMode.Panel);
            parameters.Add(p => p.Anchor, Markup("<button>Anchor</button>"));
        });

        // Every input of that geometry is a parameter that can change at runtime - the responsive mode
        // itself can be bound to a media query - so the gestures follow it rather than the first render.
        component.Render(parameters => parameters.Add(p => p.PanelPosition, BitPanelPosition.Start));

        Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Swipes.dispose"].Count);
        Assert.AreEqual(2, Context.JSInterop.Invocations["BitBlazorUI.Swipes.setup"].Count);
        Assert.AreEqual(BitPanelPosition.Start, Context.JSInterop.Invocations["BitBlazorUI.Swipes.setup"][^1].Arguments[2]);
    }

    [DataTestMethod]
    [DataRow(BitVisibility.Visible, null)]
    [DataRow(BitVisibility.Hidden, "visibility:hidden")]
    [DataRow(BitVisibility.Collapsed, "display:none")]
    public void BitCalloutShouldRespectVisibility(BitVisibility visibility, string? expectedStyle)
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.Visibility, visibility);
        });

        var style = component.Find(".bit-clo").GetAttribute("style") ?? string.Empty;

        if (expectedStyle is null)
        {
            // A visible callout is the absence of both of the other two, so it is the one case an
            // expected substring cannot state: it is asserted by neither of them being there.
            Assert.IsFalse(style.Contains("visibility:hidden"));
            Assert.IsFalse(style.Contains("display:none"));
        }
        else
        {
            Assert.IsTrue(style.Contains(expectedStyle));
        }
    }
}
