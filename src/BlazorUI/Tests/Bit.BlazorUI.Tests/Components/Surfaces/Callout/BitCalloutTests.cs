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

        Assert.AreEqual("true", anchor.GetAttribute("aria-haspopup"));
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

        // A callout the consumer caps by hand is no longer the positioning code's to cap.
        Assert.IsFalse(callout.ClassList.Contains("bit-clo-fit"));
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

        // The preferred side is the last argument of the positioning call.
        Assert.AreEqual(expected, toggle[^1].Arguments[^1]);
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

    [DataTestMethod]
    [DataRow(BitVisibility.Visible, "")]
    [DataRow(BitVisibility.Hidden, "visibility:hidden")]
    [DataRow(BitVisibility.Collapsed, "display:none")]
    public void BitCalloutShouldRespectVisibility(BitVisibility visibility, string expectedStyle)
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.Visibility, visibility);
        });

        var style = component.Find(".bit-clo").GetAttribute("style") ?? string.Empty;

        Assert.IsTrue(style.Contains(expectedStyle));
    }
}
