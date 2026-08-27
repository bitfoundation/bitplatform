using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.ProModal;

[TestClass]
public class BitProModalTests : BunitTestContext
{
    [TestMethod]
    public void BitProModalModelessShouldNotRenderOverlayAndShouldSetAriaModalFalse()
    {
        var com = RenderComponent<BitProModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Modeless, true);
        });

        Assert.AreEqual("false", com.Find(".bit-mdl-ctn").Attributes["aria-modal"]?.Value);
        Assert.AreEqual(0, com.FindAll(".bit-mdl-ovl").Count);
    }

    [TestMethod]
    public void BitProModalBlockingShouldPreventOverlayDismissButStillInvokeOverlayCallback()
    {
        var dismissed = 0;
        var overlayClicked = 0;
        var isOpen = true;

        var com = RenderComponent<BitProModal>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, value => isOpen = value);
            parameters.Add(p => p.Blocking, true);
            parameters.Add(p => p.OnDismiss, EventCallback.Factory.Create<MouseEventArgs>(this, () => dismissed++));
            parameters.Add(p => p.OnOverlayClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => overlayClicked++));
        });

        com.Find(".bit-mdl-ovl").Click();

        com.WaitForAssertion(() =>
        {
            Assert.IsTrue(isOpen);
            Assert.AreEqual(0, dismissed);
            Assert.AreEqual(1, overlayClicked);
            Assert.AreEqual("alertdialog", com.Find(".bit-mdl-ctn").Attributes["role"]?.Value);
        });
    }

    [TestMethod]
    public void BitProModalShouldInvokeOnDismissWhenClosedByParent()
    {
        var dismissed = 0;

        var com = RenderComponent<BitProModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.OnDismiss, EventCallback.Factory.Create<MouseEventArgs>(this, () => dismissed++));
        });

        com.Render(parameters => parameters.Add(p => p.IsOpen, false));

        com.WaitForAssertion(() =>
        {
            Assert.AreEqual(1, dismissed);
            Assert.AreEqual(0, com.FindAll(".bit-mdl").Count);
        });
    }

    [TestMethod]
    public void BitProModalShouldForwardAriaIds()
    {
        var com = RenderComponent<BitProModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.TitleAriaId, "title-id");
            parameters.Add(p => p.SubtitleAriaId, "subtitle-id");
        });

        // The dialog is the content box, not the layer that also holds the overlay, so that is where the
        // name and the description of it belong.
        var content = com.Find(".bit-mdl-ctn");

        Assert.AreEqual("title-id", content.Attributes["aria-labelledby"]?.Value);
        Assert.AreEqual("subtitle-id", content.Attributes["aria-describedby"]?.Value);
    }

    [TestMethod,
        DataRow(BitPosition.TopLeft, "bit-pmd-tlf"),
        DataRow(BitPosition.TopCenter, "bit-pmd-tcr"),
        DataRow(BitPosition.TopRight, "bit-pmd-trg"),
        DataRow(BitPosition.CenterLeft, "bit-pmd-clf"),
        DataRow(BitPosition.Center, "bit-pmd-ctr"),
        DataRow(BitPosition.CenterRight, "bit-pmd-crg"),
        DataRow(BitPosition.BottomLeft, "bit-pmd-blf"),
        DataRow(BitPosition.BottomCenter, "bit-pmd-bcr"),
        DataRow(BitPosition.BottomRight, "bit-pmd-brg")
    ]
    public void BitProModalPositionShouldApplyCssClass(BitPosition position, string positionClass)
    {
        var com = RenderComponent<BitProModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Position, position);
        });

        var root = com.Find(".bit-mdl");

        Assert.IsTrue(root.ClassList.Contains(positionClass));
    }

    [TestMethod]
    public void BitProModalCloseButtonClickShouldCloseAndInvokeOnDismiss()
    {
        var dismissed = 0;
        var isOpen = true;

        var com = RenderComponent<BitProModal>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, value => isOpen = value);
            parameters.Add(p => p.ShowCloseButton, true);
            parameters.Add(p => p.OnDismiss, EventCallback.Factory.Create<MouseEventArgs>(this, () => dismissed++));
        });

        com.Find(".bit-pmd-cls").Click();

        com.WaitForAssertion(() =>
        {
            Assert.IsFalse(isOpen);
            Assert.AreEqual(1, dismissed);
            Assert.AreEqual(0, com.FindAll(".bit-mdl").Count);
        });
    }

    [TestMethod]
    public void BitProModalFullSizeShouldApplyFullWidthAndFullHeightClasses()
    {
        var com = RenderComponent<BitProModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.FullSize, true);
        });

        var root = com.Find(".bit-mdl");

        Assert.IsTrue(root.ClassList.Contains("bit-mdl-fwi"));
        Assert.IsTrue(root.ClassList.Contains("bit-mdl-fhe"));
    }

    [TestMethod]
    public void BitProModalFullWidthShouldApplyOnlyFullWidthClass()
    {
        var com = RenderComponent<BitProModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.FullWidth, true);
        });

        var root = com.Find(".bit-mdl");

        Assert.IsTrue(root.ClassList.Contains("bit-mdl-fwi"));
        Assert.IsFalse(root.ClassList.Contains("bit-mdl-fhe"));
    }

    [TestMethod]
    public void BitProModalFullHeightShouldApplyOnlyFullHeightClass()
    {
        var com = RenderComponent<BitProModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.FullHeight, true);
        });

        var root = com.Find(".bit-mdl");

        Assert.IsTrue(root.ClassList.Contains("bit-mdl-fhe"));
        Assert.IsFalse(root.ClassList.Contains("bit-mdl-fwi"));
    }

    [TestMethod]
    public void BitProModalModeFullShouldApplyCssClass()
    {
        var com = RenderComponent<BitProModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ModeFull, true);
        });

        Assert.IsTrue(com.Find(".bit-mdl").ClassList.Contains("bit-pmd-mfl"));
    }

    [TestMethod]
    public void BitProModalNoBorderShouldRemoveTopBorderClass()
    {
        var withBorder = RenderComponent<BitProModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
        });

        Assert.IsTrue(withBorder.Find(".bit-mdl").ClassList.Contains("bit-pmd-nbr"));

        var noBorder = RenderComponent<BitProModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.NoBorder, true);
        });

        Assert.IsFalse(noBorder.Find(".bit-mdl").ClassList.Contains("bit-pmd-nbr"));
    }

    [TestMethod]
    public void BitProModalShouldRenderHeaderFooterAndBodyContent()
    {
        var com = RenderComponent<BitProModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.HeaderText, "header-text");
            parameters.Add(p => p.FooterText, "footer-text");
            parameters.Add(p => p.ShowCloseButton, true);
            parameters.Add(p => p.ChildContent, "body-text");
        });

        var header = com.Find(".bit-pmd-hdr");
        var footer = com.Find(".bit-pmd-fcn");
        var body = com.Find(".bit-pmd-bdy");

        Assert.AreEqual("header-text", header.TextContent);
        Assert.AreEqual("footer-text", footer.TextContent);
        Assert.AreEqual("body-text", body.TextContent);
        Assert.AreEqual(1, com.FindAll(".bit-pmd-cls").Count);
    }

    [TestMethod]
    public void BitProModalShouldNotRenderHeaderContainerWhenNoHeaderOrCloseButton()
    {
        var com = RenderComponent<BitProModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
        });

        Assert.AreEqual(0, com.FindAll(".bit-pmd-hcn").Count);
        Assert.AreEqual(0, com.FindAll(".bit-pmd-fcn").Count);
    }

    [TestMethod]
    public void BitProModalShouldBeDismissedByTheEscapeKey()
    {
        // The dialog behaviors belong to the BitModal underneath, so a ProModal has them too.
        var isOpen = true;
        var com = RenderComponent<BitProModal>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, value => isOpen = value);
        });

        com.Find(".bit-mdl").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Escape" });

        Assert.IsFalse(isOpen);
    }

    [TestMethod]
    public void BitProModalShouldPassTheDialogBehaviorOptOutsThrough()
    {
        var isOpen = true;
        var com = RenderComponent<BitProModal>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, value => isOpen = value);
            parameters.Add(p => p.NoDismissOnEscape, true);
            parameters.Add(p => p.NoAutoFocus, true);
            parameters.Add(p => p.NoFocusTrap, true);
        });

        com.Find(".bit-mdl").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Escape" });

        Assert.IsTrue(isOpen);
        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.focusFirstElement"].Count);
        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.setupFocusTrap"].Count);
    }

    [TestMethod]
    public void BitProModalShouldNotTrapTheFocusWhileItIsModeless()
    {
        var com = RenderComponent<BitProModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Modeless, true);
        });

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.focusFirstElement"].Count));

        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.setupFocusTrap"].Count);
    }


    [TestMethod]
    public void BitProModalShouldForwardTheEscapeCallbackAndTheKeptMountedFlag()
    {
        var escapes = 0;
        var isOpen = true;

        var com = RenderComponent<BitProModal>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, value => isOpen = value);
            parameters.Add(p => p.KeepMounted, true);
            parameters.Add(p => p.NoDismissOnEscape, true);
            parameters.Add(p => p.OnEscapeKeyDown, EventCallback.Factory.Create<KeyboardEventArgs>(this, () => escapes++));
        });

        com.Find(".bit-mdl").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        com.WaitForAssertion(() => Assert.AreEqual(1, escapes));
        Assert.IsTrue(isOpen);

        com.Render(parameters =>
        {
            parameters.Add(p => p.IsOpen, false);
            parameters.Add(p => p.KeepMounted, true);
        });

        // Kept in the page, but out of the way of it.
        Assert.IsTrue(com.Find(".bit-mdl").ClassList.Contains("bit-mdl-hid"));
    }

    [TestMethod]
    public void BitProModalShouldStandDownTheModalHoldOnThePageWhileItTogglesTheScrollItself()
    {
        // AutoToggleScroll is the ProModal holding its own scroller, and two holds on the same page would
        // only get in each other's way.
        var com = RenderComponent<BitProModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.AutoToggleScroll, true);
        });

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.toggleOverflow"].Count));

        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"].Count);
    }

    [TestMethod]
    public void BitProModalShouldHoldThePageThroughTheModalWhenItDoesNotToggleTheScrollItself()
    {
        var com = RenderComponent<BitProModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
        });

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"].Count));

        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.toggleOverflow"].Count);
    }

    [TestMethod]
    public void BitProModalShouldNotHoldThePageWhileItIsModeless()
    {
        var com = RenderComponent<BitProModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Modeless, true);
        });

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.focusFirstElement"].Count));

        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"].Count);
    }
}
