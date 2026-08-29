using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Modal;

/// <summary>
/// The half of the Modal that was the BitProModal of the Extras package before the two were merged: the
/// header / body / footer chrome, the positioning, and the scroll and drag handling the Modal does itself.
/// </summary>
[TestClass]
public class BitModalChromeTests : BunitTestContext
{
    [TestMethod]
    public void BitModalModelessShouldNotRenderOverlayAndShouldSetAriaModalFalse()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Modeless, true);
        });

        Assert.AreEqual("false", com.Find(".bit-mdl-ctn").Attributes["aria-modal"]?.Value);
        Assert.AreEqual(0, com.FindAll(".bit-mdl-ovl").Count);
    }

    [TestMethod]
    public void BitModalShouldNotTrapTheFocusWhileItIsModeless()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Modeless, true);
        });

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.focusFirstElement"].Count));

        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.setupFocusTrap"].Count);
    }

    [TestMethod]
    public void BitModalShouldNotHoldThePageWhileItIsModeless()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Modeless, true);
        });

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.focusFirstElement"].Count));

        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"].Count);
    }

    [TestMethod]
    public void BitModalBlockingShouldAnnounceItselfAsAnAlertDialog()
    {
        // A surface that refuses to be dismissed by a click outside of it is one waiting to be answered,
        // which is what the alertdialog role says - unless IsAlert was asked for explicitly.
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Blocking, true);
        });

        Assert.AreEqual("alertdialog", com.Find(".bit-mdl-ctn").Attributes["role"]?.Value);

        var modeless = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Blocking, true);
            parameters.Add(p => p.Modeless, true);
        });

        Assert.AreEqual("dialog", modeless.Find(".bit-mdl-ctn").Attributes["role"]?.Value);

        var explicitlyNot = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Blocking, true);
            parameters.Add(p => p.IsAlert, false);
        });

        Assert.AreEqual("dialog", explicitlyNot.Find(".bit-mdl-ctn").Attributes["role"]?.Value);
    }

    [TestMethod,
        DataRow(BitPosition.TopLeft, "bit-mdl-tlf"),
        DataRow(BitPosition.TopCenter, "bit-mdl-tcr"),
        DataRow(BitPosition.TopRight, "bit-mdl-trg"),
        DataRow(BitPosition.TopStart, "bit-mdl-tst"),
        DataRow(BitPosition.TopEnd, "bit-mdl-ten"),
        DataRow(BitPosition.CenterLeft, "bit-mdl-clf"),
        DataRow(BitPosition.Center, "bit-mdl-ctr"),
        DataRow(BitPosition.CenterRight, "bit-mdl-crg"),
        DataRow(BitPosition.CenterStart, "bit-mdl-cst"),
        DataRow(BitPosition.CenterEnd, "bit-mdl-cen"),
        DataRow(BitPosition.BottomLeft, "bit-mdl-blf"),
        DataRow(BitPosition.BottomCenter, "bit-mdl-bcr"),
        DataRow(BitPosition.BottomRight, "bit-mdl-brg"),
        DataRow(BitPosition.BottomStart, "bit-mdl-bst"),
        DataRow(BitPosition.BottomEnd, "bit-mdl-ben")
    ]
    public void BitModalPositionShouldApplyCssClass(BitPosition position, string positionClass)
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Position, position);
        });

        Assert.IsTrue(com.Find(".bit-mdl").ClassList.Contains(positionClass));
    }

    [TestMethod]
    public void BitModalAbsolutePositionShouldApplyCssClass()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.AbsolutePosition, true);
        });

        Assert.IsTrue(com.Find(".bit-mdl").ClassList.Contains("bit-mdl-abs"));
    }

    [TestMethod]
    public void BitModalCloseButtonClickShouldCloseAndInvokeOnDismiss()
    {
        var dismissed = 0;
        var isOpen = true;

        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, value => isOpen = value);
            parameters.Add(p => p.ShowCloseButton, true);
            parameters.Add(p => p.OnDismiss, EventCallback.Factory.Create<MouseEventArgs>(this, () => dismissed++));
        });

        com.Find(".bit-mdl-cls").Click();

        com.WaitForAssertion(() =>
        {
            Assert.IsFalse(isOpen);
            Assert.AreEqual(1, dismissed);
            Assert.AreEqual(0, com.FindAll(".bit-mdl").Count);
        });
    }

    [TestMethod]
    public void BitModalCloseButtonShouldCarryItsTitleAndIcon()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowCloseButton, true);
            parameters.Add(p => p.CloseButtonTitle, "Dismiss");
            parameters.Add(p => p.CloseIconName, "ChromeClose");
        });

        var button = com.Find(".bit-mdl-cls");

        Assert.AreEqual("Dismiss", button.GetAttribute("title"));
        Assert.AreEqual("Dismiss", button.GetAttribute("aria-label"));
        Assert.IsTrue(com.Find(".bit-mdl-cls i").ClassList.Contains("bit-icon--ChromeClose"));
    }

    [TestMethod]
    public void BitModalFullSizeShouldApplyFullWidthAndFullHeightClasses()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.FullSize, true);
        });

        var root = com.Find(".bit-mdl");

        Assert.IsTrue(root.ClassList.Contains("bit-mdl-fwi"));
        Assert.IsTrue(root.ClassList.Contains("bit-mdl-fhe"));
    }

    [TestMethod]
    public void BitModalModeFullShouldApplyCssClass()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ModeFull, true);
        });

        Assert.IsTrue(com.Find(".bit-mdl").ClassList.Contains("bit-mdl-mfl"));
    }

    [TestMethod]
    public void BitModalNoBorderShouldRemoveTopBorderClass()
    {
        var withBorder = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
        });

        Assert.IsTrue(withBorder.Find(".bit-mdl").ClassList.Contains("bit-mdl-bdr"));

        var noBorder = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.NoBorder, true);
        });

        Assert.IsFalse(noBorder.Find(".bit-mdl").ClassList.Contains("bit-mdl-bdr"));
    }

    [TestMethod]
    public void BitModalShouldRenderHeaderFooterAndBodyContent()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.HeaderText, "header-text");
            parameters.Add(p => p.FooterText, "footer-text");
            parameters.Add(p => p.ShowCloseButton, true);
            parameters.Add(p => p.ChildContent, "body-text");
        });

        Assert.AreEqual("header-text", com.Find(".bit-mdl-hdr").TextContent);
        Assert.AreEqual("footer-text", com.Find(".bit-mdl-fcn").TextContent);
        Assert.AreEqual("body-text", com.Find(".bit-mdl-bdy").TextContent);
        Assert.AreEqual(1, com.FindAll(".bit-mdl-cls").Count);
        Assert.IsTrue(com.Find(".bit-mdl-ctn").ClassList.Contains("bit-mdl-chr"));
    }

    [TestMethod]
    public void BitModalBodyShouldTakePrecedenceOverChildContent()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Body, "body-fragment");
            parameters.Add(p => p.ChildContent, "child-content");
        });

        Assert.AreEqual("body-fragment", com.Find(".bit-mdl-bdy").TextContent);
    }

    [TestMethod]
    public void BitModalShouldRenderTheContentBareWhenNoChromeIsAskedFor()
    {
        // The chrome is opt-in: a Modal given nothing but its content is the bare surface it has always
        // been, so the markup around that content never changes under an existing consumer.
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ChildContent, "body-text");
        });

        Assert.AreEqual(0, com.FindAll(".bit-mdl-hcn").Count);
        Assert.AreEqual(0, com.FindAll(".bit-mdl-bdy").Count);
        Assert.AreEqual(0, com.FindAll(".bit-mdl-fcn").Count);
        Assert.IsFalse(com.Find(".bit-mdl-ctn").ClassList.Contains("bit-mdl-chr"));
        Assert.AreEqual("body-text", com.Find(".bit-mdl-ctn").TextContent);
    }

    [TestMethod]
    public void BitModalShouldRenderTheBodyWrapperForAFooterOnlyChrome()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.FooterText, "footer-text");
            parameters.Add(p => p.ChildContent, "body-text");
        });

        Assert.AreEqual(0, com.FindAll(".bit-mdl-hcn").Count);
        Assert.AreEqual("body-text", com.Find(".bit-mdl-bdy").TextContent);
        Assert.AreEqual("footer-text", com.Find(".bit-mdl-fcn").TextContent);
    }

    [TestMethod]
    public void BitModalShouldStandDownTheHoldOnThePageWhileItTogglesTheScrollItself()
    {
        // AutoToggleScroll is the Modal holding its own scroller, and two holds on the same page would
        // only get in each other's way.
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.AutoToggleScroll, true);
        });

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.toggleOverflow"].Count));

        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"].Count);
    }

    [TestMethod]
    public void BitModalShouldHoldThePageWhenItDoesNotToggleTheScrollItself()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
        });

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"].Count));

        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.toggleOverflow"].Count);
    }

    [TestMethod]
    public void BitModalShouldHandTheOverflowBackToTheScrollerWhenItCloses()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.AutoToggleScroll, true);
        });

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.toggleOverflow"].Count));

        com.Render(parameters =>
        {
            parameters.Add(p => p.IsOpen, false);
            parameters.Add(p => p.AutoToggleScroll, true);
        });

        com.WaitForAssertion(() => Assert.AreEqual(2, Context.JSInterop.Invocations["BitBlazorUI.Utils.toggleOverflow"].Count));
    }

    [TestMethod]
    public void BitModalDraggableShouldRegisterAndUnregisterTheDragHandlers()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Draggable, true);
        });

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.DragDrop.setup"].Count));

        com.Render(parameters =>
        {
            parameters.Add(p => p.IsOpen, false);
            parameters.Add(p => p.Draggable, true);
        });

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.DragDrop.remove"].Count));
    }

    [TestMethod]
    public void BitModalShouldMoveTheDragHandlersWhileItIsOpen()
    {
        // Dragging is turned on - and pointed at another handle - by the same parameters as everything else,
        // and a parameter of an open Modal says something the moment it is set: a Modal that only read them
        // as it opened would say nothing at all until it had been closed and opened again.
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
        });

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.focusFirstElement"].Count));
        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.DragDrop.setup"].Count);

        var containerId = com.Find(".bit-mdl-ctn").Id;

        com.Render(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Draggable, true);
        });

        // The whole content box is the handle of a Modal that was not pointed at one.
        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.DragDrop.setup"].Count));
        Assert.AreEqual($"#{containerId}", Context.JSInterop.Invocations["BitBlazorUI.DragDrop.setup"][0].Arguments[2]);

        com.Render(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Draggable, true);
            parameters.Add(p => p.DragElementSelector, "#the-handle");
        });

        // The handlers are registered against the element the selector named, so a Modal pointed somewhere
        // else takes them back off the old handle before it puts them on the new one.
        com.WaitForAssertion(() => Assert.AreEqual(2, Context.JSInterop.Invocations["BitBlazorUI.DragDrop.setup"].Count));

        Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.DragDrop.remove"].Count);
        Assert.AreEqual($"#{containerId}", Context.JSInterop.Invocations["BitBlazorUI.DragDrop.remove"][0].Arguments[1]);
        Assert.AreEqual("#the-handle", Context.JSInterop.Invocations["BitBlazorUI.DragDrop.setup"][1].Arguments[2]);
    }

    [TestMethod]
    public void BitModalShouldTakeTheOverflowOffItsScrollerWhenItStartsTogglingItWhileItIsOpen()
    {
        // Holding the page and taking the overflow off a scroller are two ways of doing the one job, and a
        // Modal switching between them while it is open has to end up doing one of them: a Modal left doing
        // neither leaves the page scrolling behind a surface that is meant to be holding it still.
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
        });

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"].Count));

        com.Render(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.AutoToggleScroll, true);
        });

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.toggleOverflow"].Count));

        // The hold on the page is given up, and the overflow of the scroller is what holds it instead.
        Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.unlockScroll"].Count);

        // The key is the first argument and the scroller the second; the third is whether the overflow is
        // being taken away.
        Assert.AreEqual(true, Context.JSInterop.Invocations["BitBlazorUI.Utils.toggleOverflow"][0].Arguments[2]);
    }

    [TestMethod]
    public void BitModalShouldNotRegisterTheDragHandlersWhenItIsNotDraggable()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
        });

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.focusFirstElement"].Count));

        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.DragDrop.setup"].Count);
    }

    [TestMethod]
    public void BitModalShouldRenderTheChromeFromTheCascadedParameters()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.AddCascadingValue(new BitModalParameters
            {
                HeaderText = "cascaded header",
                FooterText = "cascaded footer",
                ShowCloseButton = true,
                Position = BitPosition.TopRight,
                ModeFull = true
            });
        });

        Assert.AreEqual("cascaded header", com.Find(".bit-mdl-hdr").TextContent);
        Assert.AreEqual("cascaded footer", com.Find(".bit-mdl-fcn").TextContent);
        Assert.AreEqual(1, com.FindAll(".bit-mdl-cls").Count);
        Assert.IsTrue(com.Find(".bit-mdl").ClassList.Contains("bit-mdl-trg"));
        Assert.IsTrue(com.Find(".bit-mdl").ClassList.Contains("bit-mdl-mfl"));
    }

    [TestMethod]
    public void BitModalShouldApplyTheClassesAndStylesOfEveryChromePart()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.HeaderText, "header");
            parameters.Add(p => p.FooterText, "footer");
            parameters.Add(p => p.ShowCloseButton, true);
            parameters.Add(p => p.Classes, new BitModalClassStyles
            {
                HeaderContainer = "custom-hcn",
                Header = "custom-hdr",
                CloseButton = "custom-cls",
                CloseIcon = "custom-cic",
                Body = "custom-bdy",
                Footer = "custom-fcn"
            });
            parameters.Add(p => p.Styles, new BitModalClassStyles
            {
                HeaderContainer = "color:red",
                Header = "color:green",
                CloseButton = "color:blue",
                CloseIcon = "color:brown",
                Body = "color:gold",
                Footer = "color:gray"
            });
        });

        Assert.IsTrue(com.Find(".bit-mdl-hcn").ClassList.Contains("custom-hcn"));
        Assert.IsTrue(com.Find(".bit-mdl-hdr").ClassList.Contains("custom-hdr"));
        Assert.IsTrue(com.Find(".bit-mdl-cls").ClassList.Contains("custom-cls"));
        Assert.IsTrue(com.Find(".bit-mdl-cls i").ClassList.Contains("custom-cic"));
        Assert.IsTrue(com.Find(".bit-mdl-bdy").ClassList.Contains("custom-bdy"));
        Assert.IsTrue(com.Find(".bit-mdl-fcn").ClassList.Contains("custom-fcn"));

        Assert.AreEqual("color:red", com.Find(".bit-mdl-hcn").GetAttribute("style"));
        Assert.AreEqual("color:green", com.Find(".bit-mdl-hdr").GetAttribute("style"));
        Assert.AreEqual("color:blue", com.Find(".bit-mdl-cls").GetAttribute("style"));
        Assert.AreEqual("color:brown", com.Find(".bit-mdl-cls i").GetAttribute("style"));
        Assert.AreEqual("color:gold", com.Find(".bit-mdl-bdy").GetAttribute("style"));
        Assert.AreEqual("color:gray", com.Find(".bit-mdl-fcn").GetAttribute("style"));
    }
}
