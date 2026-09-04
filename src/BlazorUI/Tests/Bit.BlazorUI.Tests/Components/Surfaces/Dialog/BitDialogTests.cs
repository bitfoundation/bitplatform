using System;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Dialog;

[TestClass]
public class BitDialogTests : BunitTestContext
{
    #region rendering

    [TestMethod]
    public void BitDialogShouldRenderTitleMessageAndButtonsWhenOpen()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Title, "Test Title");
            parameters.Add(p => p.Message, "Test Message");
        });

        var root = component.Find(".bit-dlg");
        Assert.IsNotNull(root);

        var title = component.Find(".bit-dlg-ttl");
        Assert.AreEqual("Test Title", title.TextContent);

        var message = component.Find(".bit-dlg-msg");
        Assert.AreEqual("Test Message", message.TextContent);

        var okBtn = component.FindAll(".bit-dlg-okb");
        var cancelBtn = component.FindAll(".bit-dlg-cnb");

        Assert.HasCount(1, okBtn);
        Assert.HasCount(1, cancelBtn);

        var overlay = component.FindAll(".bit-dlg-ovl");
        Assert.HasCount(1, overlay);
    }

    [TestMethod]
    public void BitDialogShouldRenderNothingWhenClosed()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, false);
            parameters.Add(p => p.Title, "Test Title");
        });

        Assert.IsEmpty(component.FindAll(".bit-dlg"));
    }

    [TestMethod]
    public void BitDialogShouldNotRenderHeaderWithoutTitleSubtitleOrCloseButton()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowCloseButton, false);
        });

        Assert.IsEmpty(component.FindAll(".bit-dlg-hdr"));
    }

    [TestMethod]
    public void BitDialogShouldRenderSubtitle()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Title, "Title");
            parameters.Add(p => p.Subtitle, "Subtitle");
        });

        Assert.AreEqual("Subtitle", component.Find(".bit-dlg-sub").TextContent);
    }

    [TestMethod]
    public void BitDialogHeaderTemplateShouldReplaceTitleAndSubtitle()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Title, "Title");
            parameters.Add(p => p.Subtitle, "Subtitle");
            parameters.Add(p => p.HeaderTemplate, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "custom-header");
                builder.AddContent(2, "Custom header");
                builder.CloseElement();
            }));
        });

        Assert.HasCount(1, component.FindAll(".custom-header"));
        Assert.IsEmpty(component.FindAll(".bit-dlg-ttl"));
        Assert.IsEmpty(component.FindAll(".bit-dlg-sub"));
        // The close button still sits beside a custom header.
        Assert.HasCount(1, component.FindAll(".bit-dlg-cls"));
    }

    [TestMethod]
    public void BitDialogFooterTemplateShouldRenderInsideFooterElement()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.FooterTemplate, (RenderFragment)(builder => builder.AddContent(0, "Footer content")));
        });

        var footer = component.Find(".bit-dlg-ftr");
        Assert.AreEqual("Footer content", footer.TextContent.Trim());
    }

    [TestMethod]
    public void BitDialogBodyShouldBeAnAliasOfChildContent()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Body, (RenderFragment)(builder => builder.AddContent(0, "From Body")));
            parameters.AddChildContent("From ChildContent");
        });

        // Body wins when both are set.
        StringAssert.Contains(component.Find(".bit-dlg-scr-cnt").TextContent, "From Body");
        Assert.IsFalse(component.Find(".bit-dlg-scr-cnt").TextContent.Contains("From ChildContent"));
    }

    [TestMethod]
    [DataRow(true, true, true)]
    [DataRow(false, true, true)]
    [DataRow(true, false, true)]
    [DataRow(true, true, false)]
    [DataRow(false, false, false)]
    public void BitDialogShouldHonorButtonVisibilityFlags(bool showOk, bool showCancel, bool showClose)
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Title, "Title");
            parameters.Add(p => p.ShowOkButton, showOk);
            parameters.Add(p => p.ShowCancelButton, showCancel);
            parameters.Add(p => p.ShowCloseButton, showClose);
        });

        Assert.HasCount(showOk ? 1 : 0, component.FindAll(".bit-dlg-okb"));
        Assert.HasCount(showCancel ? 1 : 0, component.FindAll(".bit-dlg-cnb"));
        Assert.HasCount(showClose ? 1 : 0, component.FindAll(".bit-dlg-cls"));
        // The buttons container only exists while one of the two action buttons does.
        Assert.HasCount((showOk || showCancel) ? 1 : 0, component.FindAll(".bit-dlg-bct"));
    }

    [TestMethod]
    public void BitDialogShouldRenderCustomButtonTexts()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.OkText, "Move to trash");
            parameters.Add(p => p.CancelText, "Keep it");
        });

        Assert.AreEqual("Move to trash", component.Find(".bit-dlg-okb").TextContent.Trim());
        Assert.AreEqual("Keep it", component.Find(".bit-dlg-cnb").TextContent.Trim());
    }

    [TestMethod]
    public void BitDialogButtonsShouldBeOfTypeButtonSoTheyNeverSubmitAForm()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Title, "Title");
        });

        Assert.AreEqual("button", component.Find(".bit-dlg-okb").GetAttribute("type"));
        Assert.AreEqual("button", component.Find(".bit-dlg-cnb").GetAttribute("type"));
        Assert.AreEqual("button", component.Find(".bit-dlg-cls").GetAttribute("type"));
    }

    [TestMethod]
    public void BitDialogModelessShouldNotRenderOverlay()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.IsModeless, true);
        });

        Assert.IsEmpty(component.FindAll(".bit-dlg-ovl"));
        Assert.IsTrue(component.Find(".bit-dlg").ClassList.Contains("bit-dlg-mls"));
    }

    [TestMethod]
    public void BitDialogKeepMountedShouldRenderNothingUntilTheFirstOpening()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, false);
            parameters.Add(p => p.KeepMounted, true);
            parameters.Add(p => p.Title, "Test Title");
        });

        // A Dialog that has never opened holds no state worth keeping, so it costs the page nothing.
        Assert.IsEmpty(component.FindAll(".bit-dlg"));
    }

    [TestMethod]
    public void BitDialogKeepMountedShouldRenderTheClosedDialogHiddenOnceItHasBeenOpened()
    {
        var isOpen = true;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.KeepMounted, true);
            parameters.Add(p => p.Title, "Test Title");
        });

        Assert.IsFalse(component.Find(".bit-dlg").ClassList.Contains("bit-dlg-hdn"));

        component.Find(".bit-dlg-cls").Click();

        component.WaitForAssertion(() =>
        {
            var root = component.Find(".bit-dlg");
            Assert.IsTrue(root.ClassList.Contains("bit-dlg-hdn"));
            // Hidden rather than gone: the content, and whatever state it holds, is still there.
            Assert.AreEqual("Test Title", component.Find(".bit-dlg-ttl").TextContent);
            // And out of the tab sequence and the reading order for as long as it is closed.
            Assert.IsTrue(root.HasAttribute("inert"));
            Assert.AreEqual("true", root.GetAttribute("aria-hidden"));
        }, TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public void BitDialogKeepMountedShouldDropTheHiddenClassWhenItOpensAgain()
    {
        var isOpen = true;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.KeepMounted, true);
        });

        component.Find(".bit-dlg-cls").Click();

        component.WaitForAssertion(
            () => Assert.IsTrue(component.Find(".bit-dlg").ClassList.Contains("bit-dlg-hdn")),
            TimeSpan.FromSeconds(5));

        component.Render(parameters => parameters.Add(p => p.IsOpen, true));

        var root = component.Find(".bit-dlg");
        Assert.IsFalse(root.ClassList.Contains("bit-dlg-hdn"));
        Assert.IsFalse(root.HasAttribute("inert"));
        Assert.IsFalse(root.HasAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitDialogWithoutKeepMountedShouldStillRenderNothingWhenClosed()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, false);
            parameters.Add(p => p.Title, "Test Title");
        });

        Assert.IsEmpty(component.FindAll(".bit-dlg"));
    }

    #endregion

    #region classes and styles

    [TestMethod]
    [DataRow(BitPosition.Center, "bit-dlg-ctr")]
    [DataRow(BitPosition.TopLeft, "bit-dlg-tl")]
    [DataRow(BitPosition.TopCenter, "bit-dlg-tc")]
    [DataRow(BitPosition.TopRight, "bit-dlg-tr")]
    [DataRow(BitPosition.CenterLeft, "bit-dlg-cl")]
    [DataRow(BitPosition.CenterRight, "bit-dlg-cr")]
    [DataRow(BitPosition.BottomLeft, "bit-dlg-bl")]
    [DataRow(BitPosition.BottomCenter, "bit-dlg-bc")]
    [DataRow(BitPosition.BottomRight, "bit-dlg-br")]
    [DataRow(BitPosition.TopStart, "bit-dlg-ts")]
    [DataRow(BitPosition.TopEnd, "bit-dlg-te")]
    [DataRow(BitPosition.CenterStart, "bit-dlg-cs")]
    [DataRow(BitPosition.CenterEnd, "bit-dlg-ce")]
    [DataRow(BitPosition.BottomStart, "bit-dlg-bs")]
    [DataRow(BitPosition.BottomEnd, "bit-dlg-be")]
    public void BitDialogPositionShouldRenderItsClass(BitPosition position, string expectedClass)
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Position, position);
        });

        Assert.IsTrue(component.Find(".bit-dlg-doc").ClassList.Contains(expectedClass));
    }

    // Position is one of the few parameters that is not nullable, so its default is the value the property is
    // initialised with rather than a fallback in the class builder. It is asserted on its own because nothing
    // else would catch it drifting to whichever member of the shared enum happens to be declared first.
    [TestMethod]
    public void BitDialogShouldBeCenteredByDefault()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
        });

        Assert.IsTrue(component.Find(".bit-dlg-doc").ClassList.Contains("bit-dlg-ctr"));
    }

    [TestMethod]
    public void BitDialogPositionShouldBeUpdatedOnRerender()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Position, BitPosition.TopLeft);
        });

        Assert.IsTrue(component.Find(".bit-dlg-doc").ClassList.Contains("bit-dlg-tl"));

        component.Render(parameters => parameters.Add(p => p.Position, BitPosition.BottomEnd));

        Assert.IsTrue(component.Find(".bit-dlg-doc").ClassList.Contains("bit-dlg-be"));
        Assert.IsFalse(component.Find(".bit-dlg-doc").ClassList.Contains("bit-dlg-tl"));
    }

    [TestMethod]
    public void BitDialogAbsolutePositionShouldRenderItsClass()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.AbsolutePosition, true);
        });

        Assert.IsTrue(component.Find(".bit-dlg").ClassList.Contains("bit-dlg-abs"));
    }

    [TestMethod]
    [DataRow(true, false, false, true, false)]
    [DataRow(false, true, false, false, true)]
    [DataRow(false, false, true, true, true)]
    [DataRow(false, false, false, false, false)]
    public void BitDialogFullSizeFlagsShouldRenderTheirClasses(bool fullWidth, bool fullHeight, bool fullSize, bool expectWidth, bool expectHeight)
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.FullWidth, fullWidth);
            parameters.Add(p => p.FullHeight, fullHeight);
            parameters.Add(p => p.FullSize, fullSize);
        });

        var root = component.Find(".bit-dlg");

        Assert.AreEqual(expectWidth, root.ClassList.Contains("bit-dlg-fwi"));
        Assert.AreEqual(expectHeight, root.ClassList.Contains("bit-dlg-fhe"));
    }

    [TestMethod]
    public void BitDialogClassesShouldApplyToEachPart()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Title, "Title");
            parameters.Add(p => p.Subtitle, "Subtitle");
            parameters.Add(p => p.Message, "Message");
            parameters.Add(p => p.FooterTemplate, (RenderFragment)(builder => builder.AddContent(0, "Footer")));
            parameters.Add(p => p.Classes, new BitDialogClassStyles
            {
                Root = "cls-root",
                Document = "cls-document",
                Overlay = "cls-overlay",
                Container = "cls-container",
                Header = "cls-header",
                Title = "cls-title",
                Subtitle = "cls-subtitle",
                CloseButton = "cls-close-button",
                CloseIcon = "cls-close-icon",
                Body = "cls-body",
                Message = "cls-message",
                ButtonsContainer = "cls-buttons",
                OkButton = "cls-ok",
                CancelButton = "cls-cancel",
                Footer = "cls-footer"
            });
        });

        Assert.IsTrue(component.Find(".bit-dlg").ClassList.Contains("cls-root"));
        Assert.IsTrue(component.Find(".bit-dlg-doc").ClassList.Contains("cls-document"));
        Assert.IsTrue(component.Find(".bit-dlg-ovl").ClassList.Contains("cls-overlay"));
        Assert.IsTrue(component.Find(".bit-dlg-ctn").ClassList.Contains("cls-container"));
        Assert.IsTrue(component.Find(".bit-dlg-hdr").ClassList.Contains("cls-header"));
        Assert.IsTrue(component.Find(".bit-dlg-ttl").ClassList.Contains("cls-title"));
        Assert.IsTrue(component.Find(".bit-dlg-sub").ClassList.Contains("cls-subtitle"));
        Assert.IsTrue(component.Find(".bit-dlg-cls").ClassList.Contains("cls-close-button"));
        Assert.IsTrue(component.Find(".bit-dlg-cli").ClassList.Contains("cls-close-icon"));
        Assert.IsTrue(component.Find(".bit-dlg-scr-cnt").ClassList.Contains("cls-body"));
        Assert.IsTrue(component.Find(".bit-dlg-msg").ClassList.Contains("cls-message"));
        Assert.IsTrue(component.Find(".bit-dlg-bct").ClassList.Contains("cls-buttons"));
        Assert.IsTrue(component.Find(".bit-dlg-okb").ClassList.Contains("cls-ok"));
        Assert.IsTrue(component.Find(".bit-dlg-cnb").ClassList.Contains("cls-cancel"));
        Assert.IsTrue(component.Find(".bit-dlg-ftr").ClassList.Contains("cls-footer"));
    }

    [TestMethod]
    public void BitDialogStylesShouldApplyToEachPart()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Title, "Title");
            parameters.Add(p => p.Subtitle, "Subtitle");
            parameters.Add(p => p.Message, "Message");
            parameters.Add(p => p.FooterTemplate, (RenderFragment)(builder => builder.AddContent(0, "Footer")));
            parameters.Add(p => p.Styles, new BitDialogClassStyles
            {
                Root = "z-index:1",
                Document = "opacity:0.9",
                Overlay = "background-color:red",
                Container = "width:10rem",
                Header = "padding:1rem",
                Title = "color:blue",
                Subtitle = "color:green",
                CloseButton = "color:pink",
                CloseIcon = "font-size:2rem",
                Body = "gap:1rem",
                Message = "color:gray",
                ButtonsContainer = "gap:2rem",
                OkButton = "color:white",
                CancelButton = "color:black",
                Footer = "padding:2rem"
            });
        });

        StringAssert.Contains(component.Find(".bit-dlg").GetAttribute("style"), "z-index:1");
        StringAssert.Contains(component.Find(".bit-dlg-doc").GetAttribute("style"), "opacity:0.9");
        StringAssert.Contains(component.Find(".bit-dlg-ovl").GetAttribute("style"), "background-color:red");
        StringAssert.Contains(component.Find(".bit-dlg-ctn").GetAttribute("style"), "width:10rem");
        StringAssert.Contains(component.Find(".bit-dlg-hdr").GetAttribute("style"), "padding:1rem");
        StringAssert.Contains(component.Find(".bit-dlg-ttl").GetAttribute("style"), "color:blue");
        StringAssert.Contains(component.Find(".bit-dlg-sub").GetAttribute("style"), "color:green");
        StringAssert.Contains(component.Find(".bit-dlg-cls").GetAttribute("style"), "color:pink");
        StringAssert.Contains(component.Find(".bit-dlg-cli").GetAttribute("style"), "font-size:2rem");
        StringAssert.Contains(component.Find(".bit-dlg-scr-cnt").GetAttribute("style"), "gap:1rem");
        StringAssert.Contains(component.Find(".bit-dlg-msg").GetAttribute("style"), "color:gray");
        StringAssert.Contains(component.Find(".bit-dlg-bct").GetAttribute("style"), "gap:2rem");
        StringAssert.Contains(component.Find(".bit-dlg-okb").GetAttribute("style"), "color:white");
        StringAssert.Contains(component.Find(".bit-dlg-cnb").GetAttribute("style"), "color:black");
        StringAssert.Contains(component.Find(".bit-dlg-ftr").GetAttribute("style"), "padding:2rem");
    }

    [TestMethod]
    public void BitDialogRtlShouldRenderDirAndClass()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Dir, BitDir.Rtl);
        });

        var root = component.Find(".bit-dlg");

        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
    }

    #endregion

    #region accessibility

    [TestMethod]
    [DataRow(null)]
    [DataRow(true)]
    [DataRow(false)]
    public void BitDialogRoleRespectsIsAlertAndBlocking(bool? isAlert)
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.IsBlocking, true);
            parameters.Add(p => p.IsModeless, false);

            if (isAlert.HasValue)
            {
                parameters.Add(p => p.IsAlert, isAlert);
            }
        });

        var roleDiv = component.Find(".bit-dlg-ctn");

        var expected = (isAlert ?? true) ? "alertdialog" : "dialog";

        Assert.AreEqual(expected, roleDiv.GetAttribute("role"));
    }

    [TestMethod]
    public void BitDialogNonBlockingShouldBeAPlainDialogRole()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
        });

        Assert.AreEqual("dialog", component.Find(".bit-dlg-ctn").GetAttribute("role"));
    }

    [TestMethod]
    [DataRow(false, "true")]
    [DataRow(true, "false")]
    public void BitDialogAriaModalShouldBeALowercaseBoolean(bool isModeless, string expected)
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.IsModeless, isModeless);
        });

        Assert.AreEqual(expected, component.Find(".bit-dlg-ctn").GetAttribute("aria-modal"));
    }

    [TestMethod]
    public void BitDialogShouldBeProgrammaticallyFocusable()
    {
        var component = RenderComponent<BitDialog>(parameters => parameters.Add(p => p.IsOpen, true));

        Assert.AreEqual("-1", component.Find(".bit-dlg-ctn").GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitDialogShouldNameItselfWithItsOwnTitle()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Title, "Missing subject");
        });

        var titleId = component.Find(".bit-dlg-ttl").GetAttribute("id");

        Assert.IsFalse(string.IsNullOrEmpty(titleId));
        Assert.AreEqual(titleId, component.Find(".bit-dlg-ctn").GetAttribute("aria-labelledby"));
    }

    [TestMethod]
    public void BitDialogTitleShouldCarryHeadingSemantics()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Title, "Title");
        });

        var title = component.Find(".bit-dlg-ttl");

        Assert.AreEqual("heading", title.GetAttribute("role"));
        Assert.AreEqual("2", title.GetAttribute("aria-level"));
    }

    [TestMethod]
    public void BitDialogTitleAriaIdShouldOverrideTheGeneratedOne()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Title, "Missing subject");
            parameters.Add(p => p.TitleAriaId, "my-heading");
        });

        Assert.AreEqual("my-heading", component.Find(".bit-dlg-ctn").GetAttribute("aria-labelledby"));
    }

    [TestMethod]
    public void BitDialogWithoutATitleShouldFallBackToAriaLabel()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.AriaLabel, "Confirm deletion");
        });

        var container = component.Find(".bit-dlg-ctn");

        Assert.AreEqual("Confirm deletion", container.GetAttribute("aria-label"));
        Assert.IsFalse(container.HasAttribute("aria-labelledby"));
    }

    [TestMethod]
    public void BitDialogShouldDescribeItselfWithItsMessage()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Message, "This cannot be undone.");
        });

        var messageId = component.Find(".bit-dlg-msg").GetAttribute("id");

        Assert.IsFalse(string.IsNullOrEmpty(messageId));
        Assert.AreEqual(messageId, component.Find(".bit-dlg-ctn").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitDialogShouldDescribeItselfWithBothTheSubtitleAndTheMessage()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Title, "Title");
            parameters.Add(p => p.Subtitle, "Subtitle");
            parameters.Add(p => p.Message, "Message");
        });

        var subtitleId = component.Find(".bit-dlg-sub").GetAttribute("id");
        var messageId = component.Find(".bit-dlg-msg").GetAttribute("id");

        // aria-describedby takes a list, so neither of the two descriptive lines is dropped.
        Assert.AreEqual($"{subtitleId} {messageId}", component.Find(".bit-dlg-ctn").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitDialogSubtitleAloneShouldBeTheDescription()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Title, "Title");
            parameters.Add(p => p.Subtitle, "Subtitle");
        });

        var subtitleId = component.Find(".bit-dlg-sub").GetAttribute("id");

        Assert.AreEqual(subtitleId, component.Find(".bit-dlg-ctn").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitDialogSubtitleAriaIdShouldOverrideTheGeneratedOne()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Message, "Message");
            parameters.Add(p => p.SubtitleAriaId, "my-description");
        });

        Assert.AreEqual("my-description", component.Find(".bit-dlg-ctn").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitDialogCloseButtonShouldCarryItsDefaultTitleAndAriaLabel()
    {
        var component = RenderComponent<BitDialog>(parameters => parameters.Add(p => p.IsOpen, true));

        var closeButton = component.Find(".bit-dlg-cls");

        Assert.AreEqual("Close", closeButton.GetAttribute("title"));
        Assert.AreEqual("Close", closeButton.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitDialogCloseButtonTitleShouldBeCustomizable()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.CloseButtonTitle, "بستن");
        });

        var closeButton = component.Find(".bit-dlg-cls");

        Assert.AreEqual("بستن", closeButton.GetAttribute("title"));
        Assert.AreEqual("بستن", closeButton.GetAttribute("aria-label"));
    }

    #endregion

    #region dismissing

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitDialogOverlayClickRespectsIsBlocking(bool isBlocking)
    {
        var isOpen = true;
        var dismissedCount = 0;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.IsBlocking, isBlocking);
            parameters.Add(p => p.OnDismiss, () => dismissedCount++);
        });

        var overlays = component.FindAll(".bit-dlg-ovl");

        // overlay exists regardless, click handling differs
        Assert.HasCount(1, overlays);

        overlays[0].Click();

        if (isBlocking)
        {
            Assert.IsTrue(isOpen);
            Assert.AreEqual(0, dismissedCount);
        }
        else
        {
            Assert.IsFalse(isOpen);
            Assert.AreEqual(1, dismissedCount);
        }
    }

    [TestMethod]
    public void BitDialogClickCloseButtonInvokesOnCloseAndCloses()
    {
        var isOpen = true;
        var closedCount = 0;
        var dismissedCount = 0;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.OnClose, () => closedCount++);
            parameters.Add(p => p.OnDismiss, () => dismissedCount++);
        });

        component.Find(".bit-dlg-cls").Click();

        Assert.IsFalse(isOpen);
        Assert.AreEqual(1, closedCount);
        Assert.AreEqual(1, dismissedCount);
    }

    [TestMethod]
    public void BitDialogCancelButtonInvokesOnCancelAndCloses()
    {
        var isOpen = true;
        var cancelledCount = 0;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.OnCancel, () => cancelledCount++);
        });

        component.Find(".bit-dlg-cnb").Click();

        Assert.IsFalse(isOpen);
        Assert.AreEqual(1, cancelledCount);
        Assert.AreEqual(BitDialogResult.Cancel, component.Instance.Result);
    }

    [TestMethod]
    public void BitDialogOkButtonInvokesOnOkAndCloses()
    {
        var isOpen = true;
        var okCount = 0;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.OnOk, () => okCount++);
        });

        component.Find(".bit-dlg-okb").Click();

        Assert.IsFalse(isOpen);
        Assert.AreEqual(1, okCount);
        Assert.AreEqual(BitDialogResult.Ok, component.Instance.Result);
    }

    [TestMethod]
    public void BitDialogEscapeShouldDismissByDefault()
    {
        var isOpen = true;
        var dismissedCount = 0;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.OnDismiss, () => dismissedCount++);
        });

        component.Find(".bit-dlg-ctn").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.IsFalse(isOpen);
        Assert.AreEqual(1, dismissedCount);
    }

    [TestMethod]
    public void BitDialogEscapeShouldBeIgnoredWhenCloseOnEscapeIsOff()
    {
        var isOpen = true;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.CloseOnEscape, false);
        });

        component.Find(".bit-dlg-ctn").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.IsTrue(isOpen);
    }

    [TestMethod]
    public void BitDialogEscapeShouldBeIgnoredWhenBlocking()
    {
        var isOpen = true;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.IsBlocking, true);
        });

        component.Find(".bit-dlg-ctn").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.IsTrue(isOpen);
    }

    [TestMethod]
    public void BitDialogOtherKeysShouldNotDismiss()
    {
        var isOpen = true;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        component.Find(".bit-dlg-ctn").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.IsTrue(isOpen);
    }

    [TestMethod]
    public void BitDialogOverlayClickShouldInvokeOnOverlayClickBeforeDismissing()
    {
        var isOpen = true;
        var overlayClicks = 0;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.OnOverlayClick, () => overlayClicks++);
        });

        component.Find(".bit-dlg-ovl").Click();

        Assert.AreEqual(1, overlayClicks);
        Assert.IsFalse(isOpen);
    }

    [TestMethod]
    public void BitDialogBlockingOverlayClickShouldStillInvokeOnOverlayClick()
    {
        var isOpen = true;
        var overlayClicks = 0;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.IsBlocking, true);
            parameters.Add(p => p.OnOverlayClick, () => overlayClicks++);
        });

        component.Find(".bit-dlg-ovl").Click();

        Assert.AreEqual(1, overlayClicks);
        Assert.IsTrue(isOpen);
    }

    [TestMethod]
    [DataRow(true, true)]
    [DataRow(false, false)]
    public void BitDialogRefusedDismissShouldBePlayedBackAndReported(bool isBlocking, bool closeOnEscape)
    {
        var isOpen = true;
        BitDialogDismissReason? prevented = null;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.IsBlocking, isBlocking);
            parameters.Add(p => p.CloseOnEscape, closeOnEscape);
            parameters.Add(p => p.OnDismissPrevented, (BitDialogDismissReason r) => prevented = r);
        });

        component.Find(".bit-dlg-ctn").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.IsTrue(isOpen);
        Assert.AreEqual(BitDialogDismissReason.Escape, prevented);
        // The class is taken off again once the animation has had time to run.
        component.WaitForAssertion(
            () => Assert.IsFalse(component.Find(".bit-dlg-ctn").ClassList.Contains("bit-dlg-prv")),
            TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public void BitDialogSecondRefusalShouldReplayTheMovementRatherThanBeSwallowed()
    {
        var isOpen = true;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.IsBlocking, true);
        });

        component.Find(".bit-dlg-ctn").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        var first = component.Find(".bit-dlg-ctn").ClassList;
        Assert.IsTrue(first.Contains("bit-dlg-prv"));
        Assert.IsTrue(first.Contains("bit-dlg-pva"));

        component.Find(".bit-dlg-ctn").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        // An animation restarts only when the name it resolves to changes, so the second refusal is
        // answered by the other of the two classes rather than by re-applying the one already there.
        var second = component.Find(".bit-dlg-ctn").ClassList;
        Assert.IsTrue(second.Contains("bit-dlg-prv"));
        Assert.IsTrue(second.Contains("bit-dlg-pvb"));
        Assert.IsFalse(second.Contains("bit-dlg-pva"));

        component.WaitForAssertion(
            () => Assert.IsFalse(component.Find(".bit-dlg-ctn").ClassList.Contains("bit-dlg-prv")),
            TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public void BitDialogPlayedBackRefusalShouldNotResolveBackToTheEntranceAnimation()
    {
        var isOpen = true;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.IsBlocking, true);
        });

        component.Find(".bit-dlg-ctn").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        // An animation starts when the animation-name it resolves to changes, so a surface handed none of the
        // refusal classes back resolves to the entrance animation and grows back into place. The played-back
        // refusal is a class of its own, which pins the name off until the Dialog closes.
        component.WaitForAssertion(
            () => Assert.IsTrue(component.Find(".bit-dlg-ctn").ClassList.Contains("bit-dlg-pvn")),
            TimeSpan.FromSeconds(5));

        var settled = component.Find(".bit-dlg-ctn").ClassList;
        Assert.IsFalse(settled.Contains("bit-dlg-prv"));
        Assert.IsFalse(settled.Contains("bit-dlg-pva"));
        Assert.IsFalse(settled.Contains("bit-dlg-pvb"));
    }

    [TestMethod]
    public void BitDialogRefusalAfterAPlayedBackOneShouldStillReplayTheMovement()
    {
        var isOpen = true;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.IsBlocking, true);
        });

        component.Find(".bit-dlg-ctn").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        component.WaitForAssertion(
            () => Assert.IsTrue(component.Find(".bit-dlg-ctn").ClassList.Contains("bit-dlg-pvn")),
            TimeSpan.FromSeconds(5));

        component.Find(".bit-dlg-ctn").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        // The name has to change for the movement to run at all, and the settled class is a name of its own,
        // so the refusal that follows one is answered with the first of the pair rather than with nothing.
        component.WaitForAssertion(() =>
        {
            var again = component.Find(".bit-dlg-ctn").ClassList;
            Assert.IsTrue(again.Contains("bit-dlg-prv"));
            Assert.IsTrue(again.Contains("bit-dlg-pva"));
            Assert.IsFalse(again.Contains("bit-dlg-pvn"));
        }, TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public void BitDialogClosingShouldTakeThePlayedBackRefusalOffSoTheEntrancePlaysAgain()
    {
        var isOpen = true;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.IsBlocking, true);
            parameters.Add(p => p.KeepMounted, true);
        });

        component.Find(".bit-dlg-ctn").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        component.WaitForAssertion(
            () => Assert.IsTrue(component.Find(".bit-dlg-ctn").ClassList.Contains("bit-dlg-pvn")),
            TimeSpan.FromSeconds(5));

        // A kept-mounted Dialog stays in the DOM while it is closed, so the class that pins the entrance
        // animation off has to be given up with the closing - the opening that follows is where the entrance
        // is meant to play from, and it only plays where the name it resolves to has changed.
        component.Render(parameters =>
        {
            parameters.Add(p => p.IsOpen, false);
            parameters.Add(p => p.IsBlocking, true);
            parameters.Add(p => p.KeepMounted, true);
        });

        component.Render(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.IsBlocking, true);
            parameters.Add(p => p.KeepMounted, true);
        });

        var reopened = component.Find(".bit-dlg-ctn").ClassList;
        Assert.IsFalse(reopened.Contains("bit-dlg-pvn"));
        Assert.IsFalse(reopened.Contains("bit-dlg-prv"));
    }

    [TestMethod]
    public void BitDialogBlockingOverlayClickShouldReportTheRefusedDismiss()
    {
        var isOpen = true;
        BitDialogDismissReason? prevented = null;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.IsBlocking, true);
            parameters.Add(p => p.OnDismissPrevented, (BitDialogDismissReason r) => prevented = r);
        });

        component.Find(".bit-dlg-ovl").Click();

        Assert.IsTrue(isOpen);
        Assert.AreEqual(BitDialogDismissReason.OverlayClick, prevented);
    }

    [TestMethod]
    public void BitDialogAcceptedDismissShouldNotBeReportedAsRefused()
    {
        var isOpen = true;
        var preventedCount = 0;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.OnDismissPrevented, () => preventedCount++);
        });

        component.Find(".bit-dlg-ctn").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.IsFalse(isOpen);
        Assert.AreEqual(0, preventedCount);
    }

    #endregion

    #region dismiss reason

    [TestMethod]
    public void BitDialogDismissReasonShouldBeNullWhileTheDialogIsOpen()
    {
        var component = RenderComponent<BitDialog>(parameters => parameters.Add(p => p.IsOpen, true));

        Assert.IsNull(component.Instance.DismissReason);
    }

    [TestMethod]
    public void BitDialogCloseButtonShouldReportItsDismissReason()
    {
        var isOpen = true;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        component.Find(".bit-dlg-cls").Click();

        Assert.AreEqual(BitDialogDismissReason.CloseButton, component.Instance.DismissReason);
    }

    [TestMethod]
    public void BitDialogOverlayClickShouldReportItsDismissReason()
    {
        var isOpen = true;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        component.Find(".bit-dlg-ovl").Click();

        Assert.AreEqual(BitDialogDismissReason.OverlayClick, component.Instance.DismissReason);
    }

    [TestMethod]
    public void BitDialogEscapeShouldReportItsDismissReason()
    {
        var isOpen = true;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        component.Find(".bit-dlg-ctn").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.AreEqual(BitDialogDismissReason.Escape, component.Instance.DismissReason);
    }

    [TestMethod]
    public void BitDialogOkAndCancelShouldReportTheirDismissReasons()
    {
        var isOpen = true;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        component.Find(".bit-dlg-okb").Click();
        Assert.AreEqual(BitDialogDismissReason.OkButton, component.Instance.DismissReason);

        isOpen = true;
        component.Render(parameters => parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v));

        component.Find(".bit-dlg-cnb").Click();
        Assert.AreEqual(BitDialogDismissReason.CancelButton, component.Instance.DismissReason);
    }

    [TestMethod]
    public async Task BitDialogProgrammaticCloseShouldReportItsDismissReason()
    {
        var isOpen = true;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        await component.InvokeAsync(() => component.Instance.Close());

        Assert.IsFalse(isOpen);

        Assert.AreEqual(BitDialogDismissReason.Programmatic, component.Instance.DismissReason);
    }

    [TestMethod]
    public void BitDialogDismissReasonShouldBeReadableFromOnDismiss()
    {
        var isOpen = true;
        BitDialogDismissReason? seen = null;
        BitDialog? dialog = null;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.OnDismiss, () => seen = dialog!.DismissReason);
        });

        dialog = component.Instance;

        component.Find(".bit-dlg-cls").Click();

        Assert.AreEqual(BitDialogDismissReason.CloseButton, seen);
    }

    [TestMethod]
    public void BitDialogDismissReasonShouldResetOnReopen()
    {
        var isOpen = true;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        component.Find(".bit-dlg-cls").Click();
        Assert.AreEqual(BitDialogDismissReason.CloseButton, component.Instance.DismissReason);

        isOpen = true;
        component.Render(parameters => parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v));

        Assert.IsNull(component.Instance.DismissReason);
    }

    #endregion

    #region disabled

    [TestMethod]
    public void BitDialogDisabledShouldDisableItsButtons()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Title, "Title");
            parameters.Add(p => p.IsEnabled, false);
        });

        Assert.IsTrue(component.Find(".bit-dlg-okb").HasAttribute("disabled"));
        Assert.IsTrue(component.Find(".bit-dlg-cnb").HasAttribute("disabled"));
        Assert.IsTrue(component.Find(".bit-dlg-cls").HasAttribute("disabled"));
        Assert.IsTrue(component.Find(".bit-dlg").ClassList.Contains("bit-dis"));
    }

    [TestMethod]
    public void BitDialogDisabledShouldNotInvokeItsCallbacks()
    {
        var isOpen = true;
        var okCount = 0;
        var cancelCount = 0;
        var closeCount = 0;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.OnOk, () => okCount++);
            parameters.Add(p => p.OnCancel, () => cancelCount++);
            parameters.Add(p => p.OnClose, () => closeCount++);
        });

        component.Find(".bit-dlg-okb").Click();
        component.Find(".bit-dlg-cnb").Click();
        component.Find(".bit-dlg-cls").Click();
        component.Find(".bit-dlg-ovl").Click();
        component.Find(".bit-dlg-ctn").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.IsTrue(isOpen);
        Assert.AreEqual(0, okCount);
        Assert.AreEqual(0, cancelCount);
        Assert.AreEqual(0, closeCount);
        Assert.IsNull(component.Instance.Result);
    }

    [TestMethod]
    public void BitDialogGatedOkButtonShouldBeDisabled()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.IsOkButtonEnabled, false);
        });

        Assert.IsTrue(component.Find(".bit-dlg-okb").HasAttribute("disabled"));

        // The Ok button on its own: everything else is still a way out of the Dialog.
        Assert.IsFalse(component.Find(".bit-dlg-cnb").HasAttribute("disabled"));
        Assert.IsFalse(component.Find(".bit-dlg-cls").HasAttribute("disabled"));
        Assert.IsFalse(component.Find(".bit-dlg").ClassList.Contains("bit-dis"));
    }

    [TestMethod]
    public void BitDialogGatedCancelButtonShouldBeDisabled()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.IsCancelButtonEnabled, false);
        });

        Assert.IsTrue(component.Find(".bit-dlg-cnb").HasAttribute("disabled"));
        Assert.IsFalse(component.Find(".bit-dlg-okb").HasAttribute("disabled"));
        Assert.IsFalse(component.Find(".bit-dlg-cls").HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitDialogGatedOkButtonShouldNeitherAnswerNorClose()
    {
        var isOpen = true;
        var okCount = 0;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.IsOkButtonEnabled, false);
            parameters.Add(p => p.OnOk, () => okCount++);
        });

        // bUnit dispatches to the handler whatever the disabled attribute says, so the guard has to hold
        // in the handler as well as in the markup.
        component.Find(".bit-dlg-okb").Click();

        Assert.IsTrue(isOpen);
        Assert.AreEqual(0, okCount);
        Assert.IsNull(component.Instance.Result);
        Assert.IsNull(component.Instance.DismissReason);
    }

    [TestMethod]
    public void BitDialogGatedCancelButtonShouldNeitherAnswerNorClose()
    {
        var isOpen = true;
        var cancelCount = 0;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.IsCancelButtonEnabled, false);
            parameters.Add(p => p.OnCancel, () => cancelCount++);
        });

        component.Find(".bit-dlg-cnb").Click();

        Assert.IsTrue(isOpen);
        Assert.AreEqual(0, cancelCount);
        Assert.IsNull(component.Instance.Result);
        Assert.IsNull(component.Instance.DismissReason);
    }

    [TestMethod]
    public void BitDialogGatedOkButtonShouldLeaveTheOtherWaysOutOpen()
    {
        var isOpen = true;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.IsOkButtonEnabled, false);
        });

        component.Find(".bit-dlg-ctn").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        component.WaitForAssertion(() => Assert.IsFalse(isOpen), TimeSpan.FromSeconds(5));
        Assert.AreEqual(BitDialogDismissReason.Escape, component.Instance.DismissReason);
    }

    [TestMethod]
    public void BitDialogGatedOkButtonShouldBecomePressableAgainWhenItIsEnabled()
    {
        var isOpen = true;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.IsOkButtonEnabled, false);
        });

        component.Render(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.IsOkButtonEnabled, true);
        });

        Assert.IsFalse(component.Find(".bit-dlg-okb").HasAttribute("disabled"));

        component.Find(".bit-dlg-okb").Click();

        component.WaitForAssertion(() => Assert.IsFalse(isOpen), TimeSpan.FromSeconds(5));
        Assert.AreEqual(BitDialogResult.Ok, component.Instance.Result);
    }

    #endregion

    #region result and Show

    [TestMethod]
    public async Task BitDialogOkAndCancelShouldReturnProperResultWhenUsingShow()
    {
        var componentOk = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.OkText, "OK");
            parameters.Add(p => p.CancelText, "Cancel");
        });

        var showTask = componentOk.Instance.Show();

        componentOk.WaitForState(() => componentOk.FindAll(".bit-dlg-okb").Count == 1);
        componentOk.Find(".bit-dlg-okb").Click();

        var result = await showTask;
        Assert.AreEqual(BitDialogResult.Ok, result);

        var componentCancel = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.OkText, "OK");
            parameters.Add(p => p.CancelText, "Cancel");
        });

        var showTask2 = componentCancel.Instance.Show();

        componentCancel.WaitForState(() => componentCancel.FindAll(".bit-dlg-cnb").Count == 1);
        componentCancel.Find(".bit-dlg-cnb").Click();

        var result2 = await showTask2;
        Assert.AreEqual(BitDialogResult.Cancel, result2);
    }

    [TestMethod]
    public async Task BitDialogShowShouldCompleteWithNullWhenDismissedByTheCloseButton()
    {
        var component = RenderComponent<BitDialog>();

        var showTask = component.Instance.Show();

        component.WaitForState(() => component.FindAll(".bit-dlg-cls").Count == 1);
        component.Find(".bit-dlg-cls").Click();

        var result = await showTask.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task BitDialogShowShouldCompleteWithNullWhenDismissedByTheOverlay()
    {
        var component = RenderComponent<BitDialog>();

        var showTask = component.Instance.Show();

        component.WaitForState(() => component.FindAll(".bit-dlg-ovl").Count == 1);
        component.Find(".bit-dlg-ovl").Click();

        var result = await showTask.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task BitDialogShowShouldCompleteWithNullWhenDismissedByEscape()
    {
        var component = RenderComponent<BitDialog>();

        var showTask = component.Instance.Show();

        component.WaitForState(() => component.FindAll(".bit-dlg-ctn").Count == 1);
        component.Find(".bit-dlg-ctn").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        var result = await showTask.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task BitDialogShowShouldCompleteWhenTheDialogIsClosedProgrammatically()
    {
        var component = RenderComponent<BitDialog>();

        var showTask = component.Instance.Show();

        component.WaitForState(() => component.FindAll(".bit-dlg").Count == 1);

        await component.InvokeAsync(() => component.Instance.Close());

        var result = await showTask.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task BitDialogSecondShowShouldReleaseTheFirstOne()
    {
        var component = RenderComponent<BitDialog>();

        var firstShow = component.Instance.Show();

        component.WaitForState(() => component.FindAll(".bit-dlg").Count == 1);

        var secondShow = component.Instance.Show();

        var firstResult = await firstShow.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.IsNull(firstResult);

        component.WaitForState(() => component.FindAll(".bit-dlg-okb").Count == 1);
        component.Find(".bit-dlg-okb").Click();

        Assert.AreEqual(BitDialogResult.Ok, await secondShow.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [TestMethod]
    public void BitDialogResultShouldResetOnReopen()
    {
        var isOpen = true;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        component.Find(".bit-dlg-cnb").Click();

        Assert.AreEqual(BitDialogResult.Cancel, component.Instance.Result);

        component.Render(parameters => parameters.Add(p => p.IsOpen, true));

        Assert.IsNull(component.Instance.Result);
    }

    #endregion

    #region public methods

    [TestMethod]
    public async Task BitDialogOpenCloseAndToggleShouldControlIsOpen()
    {
        var component = RenderComponent<BitDialog>();

        Assert.IsEmpty(component.FindAll(".bit-dlg"));

        await component.InvokeAsync(() => component.Instance.Open());
        Assert.HasCount(1, component.FindAll(".bit-dlg"));

        await component.InvokeAsync(() => component.Instance.Close());
        Assert.IsEmpty(component.FindAll(".bit-dlg"));

        await component.InvokeAsync(() => component.Instance.Toggle());
        Assert.HasCount(1, component.FindAll(".bit-dlg"));

        await component.InvokeAsync(() => component.Instance.Toggle());
        Assert.IsEmpty(component.FindAll(".bit-dlg"));
    }

    [TestMethod]
    public async Task BitDialogShouldInvokeOnOpenWhenItOpens()
    {
        var openedCount = 0;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.OnOpen, () => openedCount++);
        });

        await component.InvokeAsync(() => component.Instance.Open());

        component.WaitForAssertion(() => Assert.AreEqual(1, openedCount));
    }

    [TestMethod]
    public void BitDialogDefaultIsOpenShouldOpenTheUncontrolledDialog()
    {
        var component = RenderComponent<BitDialog>(parameters => parameters.Add(p => p.DefaultIsOpen, true));

        Assert.HasCount(1, component.FindAll(".bit-dlg"));
    }

    [TestMethod]
    public void BitDialogDefaultIsOpenShouldNotReopenTheDialogThatWasClosed()
    {
        var component = RenderComponent<BitDialog>(parameters => parameters.Add(p => p.DefaultIsOpen, true));

        component.Find(".bit-dlg-cls").Click();

        // Read once, at initialization: a render of the page around the Dialog does not put it back up.
        component.Render(parameters => parameters.Add(p => p.Title, "Test Title"));

        Assert.IsEmpty(component.FindAll(".bit-dlg"));
    }

    [TestMethod]
    public void BitDialogDefaultIsOpenShouldBeIgnoredWhenIsOpenIsSet()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.DefaultIsOpen, true);
            parameters.Add(p => p.IsOpen, false);
        });

        Assert.IsEmpty(component.FindAll(".bit-dlg"));
    }

    #endregion

    #region ok button loading

    [TestMethod]
    public void BitDialogOkButtonShouldShowASpinnerWhileOnOkIsRunning()
    {
        var gate = new TaskCompletionSource();
        var isOpen = true;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.OnOk, async () => await gate.Task);
        });

        Assert.IsEmpty(component.FindAll(".bit-dlg-spn"));

        component.Find(".bit-dlg-okb").Click();

        component.WaitForAssertion(() => Assert.HasCount(1, component.FindAll(".bit-dlg-spn")), TimeSpan.FromSeconds(5));
        Assert.IsTrue(component.Find(".bit-dlg-okb").HasAttribute("disabled"));
        Assert.AreEqual("true", component.Find(".bit-dlg-okb").GetAttribute("aria-busy"));
        // The spinner replaced the label, so the button keeps its name through the aria-label instead.
        Assert.AreEqual("Ok", component.Find(".bit-dlg-okb").GetAttribute("aria-label"));
        Assert.IsTrue(isOpen);

        gate.SetResult();

        component.WaitForAssertion(() => Assert.IsFalse(isOpen), TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public void BitDialogOkButtonShouldAnswerOnlyOncePerShowing()
    {
        var gate = new TaskCompletionSource();
        var okCount = 0;
        var isOpen = true;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.OnOk, async () =>
            {
                okCount++;
                await gate.Task;
            });
        });

        component.Find(".bit-dlg-okb").Click();
        component.WaitForAssertion(() => Assert.HasCount(1, component.FindAll(".bit-dlg-spn")), TimeSpan.FromSeconds(5));

        component.Find(".bit-dlg-okb").Click();

        gate.SetResult();

        component.WaitForAssertion(() => Assert.IsFalse(isOpen), TimeSpan.FromSeconds(5));
        Assert.AreEqual(1, okCount);
    }

    [TestMethod]
    public void BitDialogShouldHoldEveryOtherWayOutShutWhileOnOkIsRunning()
    {
        var gate = new TaskCompletionSource();
        var isOpen = true;
        var cancelCount = 0;
        var closeCount = 0;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.OnOk, async () => await gate.Task);
            parameters.Add(p => p.OnCancel, () => cancelCount++);
            parameters.Add(p => p.OnClose, () => closeCount++);
        });

        component.Find(".bit-dlg-okb").Click();
        component.WaitForAssertion(() => Assert.HasCount(1, component.FindAll(".bit-dlg-spn")), TimeSpan.FromSeconds(5));

        // The showing has already been answered, so a second answer must not be able to get in.
        Assert.IsTrue(component.Find(".bit-dlg-cnb").HasAttribute("disabled"));
        Assert.IsTrue(component.Find(".bit-dlg-cls").HasAttribute("disabled"));

        component.Find(".bit-dlg-ovl").Click();
        component.Find(".bit-dlg-ctn").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.IsTrue(isOpen);
        Assert.AreEqual(0, cancelCount);
        Assert.AreEqual(0, closeCount);

        gate.SetResult();

        component.WaitForAssertion(() => Assert.IsFalse(isOpen), TimeSpan.FromSeconds(5));
        Assert.AreEqual(BitDialogResult.Ok, component.Instance.Result);
        Assert.AreEqual(BitDialogDismissReason.OkButton, component.Instance.DismissReason);
    }

    [TestMethod]
    public void BitDialogShouldStayOpenWhenOnOkThrows()
    {
        var isOpen = true;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.OnOk, () => throw new InvalidOperationException("nope"));
        });

        Assert.ThrowsExactly<InvalidOperationException>(() => component.Find(".bit-dlg-okb").Click());

        Assert.IsTrue(isOpen);
    }

    [TestMethod]
    public void BitDialogOkThatThrowsShouldLeaveNeitherASpinnerNorAnAnswerBehind()
    {
        var isOpen = true;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.OnOk, () => throw new InvalidOperationException("nope"));
        });

        Assert.ThrowsExactly<InvalidOperationException>(() => component.Find(".bit-dlg-okb").Click());

        Assert.IsTrue(isOpen);
        // The Dialog is open and unanswered, so a later dismissal must not report the Ok that never happened.
        Assert.IsNull(component.Instance.Result);
        Assert.IsEmpty(component.FindAll(".bit-dlg-spn"));
        Assert.IsFalse(component.Find(".bit-dlg-okb").HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitDialogCancelThatThrowsShouldLeaveNoAnswerBehind()
    {
        var isOpen = true;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.OnCancel, () => throw new InvalidOperationException("nope"));
        });

        Assert.ThrowsExactly<InvalidOperationException>(() => component.Find(".bit-dlg-cnb").Click());

        Assert.IsTrue(isOpen);
        Assert.IsNull(component.Instance.Result);
    }

    [TestMethod]
    public async Task BitDialogDismissedAfterAFailedOkShouldCompleteShowWithNull()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.OnOk, () => throw new InvalidOperationException("nope"));
        });

        var showTask = component.Instance.Show();

        component.WaitForState(() => component.FindAll(".bit-dlg-okb").Count == 1);

        Assert.ThrowsExactly<InvalidOperationException>(() => component.Find(".bit-dlg-okb").Click());

        component.Find(".bit-dlg-cls").Click();

        Assert.IsNull(await showTask);
    }

    [TestMethod]
    public void BitDialogOkThatClosesTheDialogItselfShouldStillReportOk()
    {
        var isOpen = true;
        BitDialog? dialog = null;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.OnOk, async () => await dialog!.Close());
        });

        dialog = component.Instance;

        component.Find(".bit-dlg-okb").Click();

        Assert.IsFalse(isOpen);
        Assert.AreEqual(BitDialogResult.Ok, component.Instance.Result);
    }

    #endregion

    #region focus, scroll and drag interop

    [TestMethod]
    public void BitDialogShouldTrapTheFocusAndTakeItWhenItOpens()
    {
        RenderComponent<BitDialog>(parameters => parameters.Add(p => p.IsOpen, true));

        Context.JSInterop.VerifyInvoke("BitBlazorUI.Utils.setupFocusTrap");
        Context.JSInterop.VerifyInvoke("BitBlazorUI.Utils.focusFirstElement");
        Context.JSInterop.VerifyInvoke("BitBlazorUI.Utils.storeFocus");
    }

    [TestMethod]
    public void BitDialogModelessShouldNotTrapTheFocus()
    {
        RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.IsModeless, true);
        });

        Assert.IsEmpty(Context.JSInterop.Invocations["BitBlazorUI.Utils.setupFocusTrap"]);
    }

    [TestMethod]
    public void BitDialogTrapFocusShouldOverrideTheModelessDefault()
    {
        RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.IsModeless, true);
            parameters.Add(p => p.TrapFocus, true);
        });

        Context.JSInterop.VerifyInvoke("BitBlazorUI.Utils.setupFocusTrap");
    }

    [TestMethod]
    public void BitDialogAutoFocusOffShouldLeaveTheFocusAlone()
    {
        RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.AutoFocus, false);
        });

        Assert.IsEmpty(Context.JSInterop.Invocations["BitBlazorUI.Utils.focusFirstElement"]);
    }

    [TestMethod]
    public void BitDialogAutoFocusButtonShouldFocusThatButtonInsteadOfTheFirstElement()
    {
        RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.AutoFocusButton, BitDialogButton.Cancel);
        });

        Assert.IsEmpty(Context.JSInterop.Invocations["BitBlazorUI.Utils.focusFirstElement"]);
    }

    [TestMethod]
    [DataRow(BitDialogButton.Ok)]
    [DataRow(BitDialogButton.Cancel)]
    public void BitDialogAutoFocusButtonShouldFallBackWhenThatButtonCannotBePressed(BitDialogButton button)
    {
        // The browser refuses the focus to a disabled button and leaves it where it was - which for a Dialog
        // that has just opened is the page behind it - so a button that cannot be pressed falls back the
        // same way a button that is not being shown does.
        RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.IsOkButtonEnabled, false);
            parameters.Add(p => p.IsCancelButtonEnabled, false);
            parameters.Add(p => p.AutoFocusButton, button);
        });

        Context.JSInterop.VerifyInvoke("BitBlazorUI.Utils.focusFirstElement");
    }

    [TestMethod]
    [DataRow(BitDialogButton.Ok)]
    [DataRow(BitDialogButton.Cancel)]
    [DataRow(BitDialogButton.Close)]
    public void BitDialogAutoFocusButtonShouldFallBackWhenTheWholeDialogIsDisabled(BitDialogButton button)
    {
        // A disabled Dialog disables all three of its buttons, so naming one of them is naming somewhere the
        // focus cannot land - and the fall back has to be taken for the same reason it is taken for a button
        // that is disabled on its own.
        RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.AutoFocusButton, button);
        });

        Context.JSInterop.VerifyInvoke("BitBlazorUI.Utils.focusFirstElement");
    }

    [TestMethod]
    public void BitDialogAutoFocusButtonShouldStillLandOnAButtonThatIsOnlyDisabledOnTheOtherSide()
    {
        // Gating one button says nothing about the other one.
        RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.IsOkButtonEnabled, false);
            parameters.Add(p => p.AutoFocusButton, BitDialogButton.Cancel);
        });

        Assert.IsEmpty(Context.JSInterop.Invocations["BitBlazorUI.Utils.focusFirstElement"]);
    }

    [TestMethod]
    public void BitDialogAutoFocusButtonShouldFallBackWhenThatButtonIsHidden()
    {
        RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowCancelButton, false);
            parameters.Add(p => p.AutoFocusButton, BitDialogButton.Cancel);
        });

        Context.JSInterop.VerifyInvoke("BitBlazorUI.Utils.focusFirstElement");
    }

    [TestMethod]
    public void BitDialogOverlayShouldRefuseTheDefaultOfThePressSoTheFocusNeverLeaves()
    {
        var component = RenderComponent<BitDialog>(parameters => parameters.Add(p => p.IsOpen, true));

        // Pressing the overlay is what would otherwise leave the body holding the focus, outside the trap.
        // The default of the press is refused, so the focus stays where the Dialog put it.
        StringAssert.Contains(component.Markup, "onmousedown:preventDefault");
    }

    [TestMethod]
    public void BitDialogOverlayClickShouldNotChaseTheFocusAcrossTheInterop()
    {
        var isOpen = true;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.IsBlocking, true);
        });

        component.Find(".bit-dlg-ovl").Click();

        // The focus was never taken off the Dialog, so there is nothing to ask the browser about and
        // nothing to put back - the round trip the Dialog used to pay for every overlay click is gone.
        Assert.IsEmpty(Context.JSInterop.Invocations["BitBlazorUI.Utils.containsActiveElement"]);
    }

    [TestMethod]
    public void BitDialogAutoFocusSelectorShouldBeHandedToTheFocusCall()
    {
        RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.AutoFocusSelector, ".my-field");
        });

        var invocation = Context.JSInterop.Invocations["BitBlazorUI.Utils.focusFirstElement"][0];
        Assert.AreEqual(".my-field", invocation.Arguments[1]);
    }

    [TestMethod]
    public void BitDialogWithoutAnAutoFocusSelectorShouldAskForTheFirstFocusableElement()
    {
        RenderComponent<BitDialog>(parameters => parameters.Add(p => p.IsOpen, true));

        var invocation = Context.JSInterop.Invocations["BitBlazorUI.Utils.focusFirstElement"][0];
        Assert.IsNull(invocation.Arguments[1]);
    }

    [TestMethod]
    public void BitDialogAutoFocusButtonShouldWinOverTheAutoFocusSelector()
    {
        RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.AutoFocusButton, BitDialogButton.Cancel);
            parameters.Add(p => p.AutoFocusSelector, ".my-field");
        });

        Assert.IsEmpty(Context.JSInterop.Invocations["BitBlazorUI.Utils.focusFirstElement"]);
    }

    [TestMethod]
    public void BitDialogRestoreFocusOffShouldNotRememberTheFocus()
    {
        RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.RestoreFocus, false);
        });

        Assert.IsEmpty(Context.JSInterop.Invocations["BitBlazorUI.Utils.storeFocus"]);
    }

    [TestMethod]
    public void BitDialogShouldReleaseTheFocusTrapAndTheFocusWhenItCloses()
    {
        var isOpen = true;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        component.Find(".bit-dlg-cls").Click();

        component.WaitForAssertion(() =>
        {
            Context.JSInterop.VerifyInvoke("BitBlazorUI.Utils.disposeFocusTrap");
            Context.JSInterop.VerifyInvoke("BitBlazorUI.Utils.restoreFocus");
        }, TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public async Task BitDialogReopeningWhileTheCloseIsStillWaitingShouldNotTearTheNewOpenDown()
    {
        // On a circuit every step of a closing waits on the browser, so an opening that lands while one is
        // still waiting would otherwise be undone by the rest of that closing: the trap, the hold on the
        // scroller and the focus all handed back on a Dialog that is on the screen, with nothing to take them
        // again until it has been closed and opened a second time.
        var parked = Context.JSInterop.SetupVoid("BitBlazorUI.Utils.disposeFocusTrap", _ => true);

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.IsDraggable, true);
            parameters.Add(p => p.AutoToggleScroll, true);
        });

        // The closing starts and parks on the first thing it asks the browser for.
        component.Render(parameters =>
        {
            parameters.Add(p => p.IsOpen, false);
            parameters.Add(p => p.IsDraggable, true);
            parameters.Add(p => p.AutoToggleScroll, true);
        });

        // The opening lands while it is still parked, and registers everything again.
        component.Render(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.IsDraggable, true);
            parameters.Add(p => p.AutoToggleScroll, true);
        });

        // Only now does the closing get to carry on. The rest of it is waiting on nothing, so a turn of the
        // renderer's own queue taken after it is enough to see it through to the end - what it would go on
        // to do, it has done by the time this returns.
        parked.SetVoidResult();
        await component.InvokeAsync(() => { });

        Assert.HasCount(2, Context.JSInterop.Invocations["BitBlazorUI.Utils.setupFocusTrap"]);
        Assert.HasCount(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.disposeFocusTrap"]);
        Assert.IsEmpty(Context.JSInterop.Invocations["BitBlazorUI.Utils.restoreFocus"]);
        Assert.IsEmpty(Context.JSInterop.Invocations["BitBlazorUI.DragDrop.remove"]);
        // The third argument is the one that says whether the overflow is being taken or handed back.
        Assert.IsFalse(Context.JSInterop.Invocations["BitBlazorUI.Utils.toggleOverflow"]
                              .Any(i => i.Arguments[2] is false));
    }

    [TestMethod]
    public void BitDialogAutoToggleScrollShouldLockTheScrollerOnOpenAndUnlockItOnClose()
    {
        var isOpen = true;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.AutoToggleScroll, true);
            parameters.Add(p => p.ScrollerSelector, ".my-scroller");
        });

        var invocation = Context.JSInterop.Invocations["BitBlazorUI.Utils.toggleOverflow"][0];
        Assert.AreEqual(".my-scroller", invocation.Arguments[1]);
        Assert.AreEqual(true, invocation.Arguments[2]);

        component.Find(".bit-dlg-cls").Click();

        component.WaitForAssertion(() =>
        {
            var invocations = Context.JSInterop.Invocations["BitBlazorUI.Utils.toggleOverflow"];
            Assert.HasCount(2, invocations);
            Assert.AreEqual(".my-scroller", invocations[1].Arguments[1]);
            Assert.AreEqual(false, invocations[1].Arguments[2]);
        }, TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public void BitDialogAbsolutePositionShouldCarryTheScrollTopOffsetOfTheLockedScroller()
    {
        Context.JSInterop.Setup<float>("BitBlazorUI.Utils.toggleOverflow", _ => true).SetResult(120);

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.AbsolutePosition, true);
            parameters.Add(p => p.AutoToggleScroll, true);
        });

        component.WaitForAssertion(
            () => StringAssert.Contains(component.Find(".bit-dlg").GetAttribute("style"), "top:120px"),
            TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public void BitDialogFixedDialogShouldNeverCarryTheScrollTopOffset()
    {
        // toggleOverflow reports the scroller's scrollTop, which only an absolutely positioned Dialog uses
        // to re-align itself; on a fixed one the same declaration would push it off the bottom of the screen.
        Context.JSInterop.Setup<float>("BitBlazorUI.Utils.toggleOverflow", _ => true).SetResult(120);

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.AutoToggleScroll, true);
            parameters.Add(p => p.Style, "color:red");
        });

        // Force the style builder to recompute so a stale offset would have a chance to land.
        component.Render(parameters => parameters.Add(p => p.Style, "color:blue"));

        var style = component.Find(".bit-dlg").GetAttribute("style") ?? string.Empty;

        Assert.IsFalse(style.Contains("top:"), $"A fixed Dialog must not carry a top offset, got '{style}'.");
    }

    [TestMethod]
    public void BitDialogClosedWhileOpeningShouldNotRegisterWhatTheClosingHasAlreadyStoodDown()
    {
        // The opening reaches the browser several times over, and on a circuit every one of those waits. A
        // closing that lands while the opening is still waiting must not let the opening carry on and put
        // the focus trap and the hold on the scroller back on a Dialog that is no longer on the screen.
        var storeFocus = Context.JSInterop.SetupVoid("BitBlazorUI.Utils.storeFocus", _ => true);

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.AutoToggleScroll, true);
        });

        // The opening is now suspended on the very first thing it does.
        Assert.IsEmpty(Context.JSInterop.Invocations["BitBlazorUI.Utils.setupFocusTrap"]);

        component.Render(parameters => parameters.Add(p => p.IsOpen, false));

        storeFocus.SetVoidResult();

        component.WaitForAssertion(() =>
        {
            Assert.IsEmpty(Context.JSInterop.Invocations["BitBlazorUI.Utils.setupFocusTrap"]);
            Assert.IsEmpty(Context.JSInterop.Invocations["BitBlazorUI.Utils.focusFirstElement"]);
            Assert.IsEmpty(Context.JSInterop.Invocations["BitBlazorUI.Utils.toggleOverflow"]);
        }, TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public void BitDialogShouldHoldTheScrollerOfTheApplicationShellItIsInside()
    {
        // An application shell scrolls a region of its own, so the body of such an app never scrolls and a
        // hold taken on it would hold nothing. BitAppShell cascades its scroller under this name, and a
        // Dialog that has not been pointed at a scroller of its own holds that one instead of the page.
        var shell = new ElementReference("shell-container");

        RenderComponent<BitDialog>(parameters =>
        {
            parameters.AddCascadingValue("BitAppShell.Container", (ElementReference?)shell);
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.AutoToggleScroll, true);
        });

        var invocation = Context.JSInterop.Invocations["BitBlazorUI.Utils.toggleOverflow"][0];
        Assert.AreEqual(shell, invocation.Arguments[1]);
    }

    [TestMethod]
    public void BitDialogShouldPreferTheScrollerItWasPointedAtOverTheOneOfTheShell()
    {
        // The shell is the fallback, not the answer: a Dialog told which scroller to hold holds that one.
        var shell = new ElementReference("shell-container");

        RenderComponent<BitDialog>(parameters =>
        {
            parameters.AddCascadingValue("BitAppShell.Container", (ElementReference?)shell);
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.AutoToggleScroll, true);
            parameters.Add(p => p.ScrollerSelector, "#own-scroller");
        });

        var invocation = Context.JSInterop.Invocations["BitBlazorUI.Utils.toggleOverflow"][0];
        Assert.AreEqual("#own-scroller", invocation.Arguments[1]);
    }

    [TestMethod]
    public void BitDialogShouldHoldTheBodyWhenThereIsNoShellAndNoScrollerOfItsOwn()
    {
        RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.AutoToggleScroll, true);
        });

        var invocation = Context.JSInterop.Invocations["BitBlazorUI.Utils.toggleOverflow"][0];
        Assert.AreEqual("body", invocation.Arguments[1]);
    }

    [TestMethod]
    public void BitDialogShouldNotTouchTheScrollerWhenAutoToggleScrollIsOff()
    {
        RenderComponent<BitDialog>(parameters => parameters.Add(p => p.IsOpen, true));

        Assert.IsEmpty(Context.JSInterop.Invocations["BitBlazorUI.Utils.toggleOverflow"]);
    }

    [TestMethod]
    public void BitDialogShouldUnlockTheScrollerItLockedEvenAfterTheSelectorChanges()
    {
        var isOpen = true;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.AutoToggleScroll, true);
            parameters.Add(p => p.ScrollerSelector, ".first-scroller");
        });

        component.Render(parameters => parameters.Add(p => p.ScrollerSelector, ".second-scroller"));

        component.Find(".bit-dlg-cls").Click();

        component.WaitForAssertion(() =>
        {
            var invocations = Context.JSInterop.Invocations["BitBlazorUI.Utils.toggleOverflow"];
            Assert.HasCount(2, invocations);
            // The scroller that was locked is the one that gets unlocked, not the one named now.
            Assert.AreEqual(".first-scroller", invocations[1].Arguments[1]);
            Assert.AreEqual(false, invocations[1].Arguments[2]);
        }, TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public async Task BitDialogShouldUnlockTheScrollerWhenItIsDisposedWhileOpen()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.AutoToggleScroll, true);
        });

        Assert.HasCount(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.toggleOverflow"]);

        await component.Instance.DisposeAsync();

        var invocations = Context.JSInterop.Invocations["BitBlazorUI.Utils.toggleOverflow"];
        Assert.HasCount(2, invocations);
        Assert.AreEqual(false, invocations[1].Arguments[2]);
    }

    [TestMethod]
    public void BitDialogDraggableShouldRegisterTheDragHandlers()
    {
        RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.IsDraggable, true);
        });

        Context.JSInterop.VerifyInvoke("BitBlazorUI.DragDrop.setup");
    }

    [TestMethod]
    public void BitDialogShouldNotRegisterTheDragHandlersWhenNotDraggable()
    {
        RenderComponent<BitDialog>(parameters => parameters.Add(p => p.IsOpen, true));

        Assert.IsEmpty(Context.JSInterop.Invocations["BitBlazorUI.DragDrop.setup"]);
    }

    [TestMethod]
    public void BitDialogShouldNotTearDownDragHandlersItNeverRegistered()
    {
        var isOpen = true;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        component.Find(".bit-dlg-cls").Click();

        component.WaitForAssertion(
            () => Assert.IsFalse(isOpen),
            TimeSpan.FromSeconds(5));

        Assert.IsEmpty(Context.JSInterop.Invocations["BitBlazorUI.DragDrop.remove"]);
    }

    [TestMethod]
    public void BitDialogTurningDragOnWhileItIsOpenShouldRegisterTheHandlers()
    {
        var component = RenderComponent<BitDialog>(parameters => parameters.Add(p => p.IsOpen, true));

        Assert.IsEmpty(Context.JSInterop.Invocations["BitBlazorUI.DragDrop.setup"]);

        component.Render(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.IsDraggable, true);
        });

        Context.JSInterop.VerifyInvoke("BitBlazorUI.DragDrop.setup");
    }

    [TestMethod]
    public void BitDialogTurningDragOffWhileItIsOpenShouldTearTheHandlersDown()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.IsDraggable, true);
        });

        component.Render(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.IsDraggable, false);
        });

        Context.JSInterop.VerifyInvoke("BitBlazorUI.DragDrop.remove");
        Assert.HasCount(1, Context.JSInterop.Invocations["BitBlazorUI.DragDrop.setup"]);
    }

    [TestMethod]
    public void BitDialogMovingTheDragHandleWhileItIsOpenShouldRebindItToTheNewOne()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.IsDraggable, true);
            parameters.Add(p => p.DragElementSelector, ".first-handle");
        });

        component.Render(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.IsDraggable, true);
            parameters.Add(p => p.DragElementSelector, ".second-handle");
        });

        var removals = Context.JSInterop.Invocations["BitBlazorUI.DragDrop.remove"];
        var setups = Context.JSInterop.Invocations["BitBlazorUI.DragDrop.setup"];

        // Torn down from the handle it was actually put on, and put on the one named now.
        Assert.HasCount(1, removals);
        Assert.AreEqual(".first-handle", removals[0].Arguments[1]);
        Assert.HasCount(2, setups);
        Assert.AreEqual(".second-handle", setups[1].Arguments[2]);
    }

    [TestMethod]
    public void BitDialogRenderingAgainWithoutChangingTheHandleShouldNotRebindIt()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.IsDraggable, true);
            parameters.Add(p => p.Title, "Draggable");
        });

        component.Render(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.IsDraggable, true);
            parameters.Add(p => p.Title, "Draggable");
            parameters.Add(p => p.Message, "A render that changed nothing the handle depends on.");
        });

        Assert.HasCount(1, Context.JSInterop.Invocations["BitBlazorUI.DragDrop.setup"]);
        Assert.IsEmpty(Context.JSInterop.Invocations["BitBlazorUI.DragDrop.remove"]);
    }

    [TestMethod]
    public void BitDialogGainingAHeaderWhileItIsOpenShouldMoveTheDragHandleOntoIt()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.IsDraggable, true);
            parameters.Add(p => p.ShowCloseButton, false);
        });

        var containerId = component.Find(".bit-dlg-ctn").GetAttribute("id");
        Assert.AreEqual($"[id=\"{containerId}\"]", Context.JSInterop.Invocations["BitBlazorUI.DragDrop.setup"][0].Arguments[2]);

        component.Render(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.IsDraggable, true);
            parameters.Add(p => p.ShowCloseButton, false);
            parameters.Add(p => p.Title, "Now it has a title bar");
        });

        var setups = Context.JSInterop.Invocations["BitBlazorUI.DragDrop.setup"];
        Assert.HasCount(2, setups);
        Assert.AreEqual($"[id=\"{containerId}\"] > .bit-dlg-hdr", setups[1].Arguments[2]);
    }

    [TestMethod]
    public void BitDialogTurningTheFocusTrapOffWhileItIsOpenShouldReleaseTheKeyboard()
    {
        var component = RenderComponent<BitDialog>(parameters => parameters.Add(p => p.IsOpen, true));

        Context.JSInterop.VerifyInvoke("BitBlazorUI.Utils.setupFocusTrap");
        Assert.IsEmpty(Context.JSInterop.Invocations["BitBlazorUI.Utils.disposeFocusTrap"]);

        component.Render(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.TrapFocus, false);
        });

        Context.JSInterop.VerifyInvoke("BitBlazorUI.Utils.disposeFocusTrap");
    }

    [TestMethod]
    public void BitDialogTurningTheFocusTrapOnWhileItIsOpenShouldTakeTheKeyboard()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.TrapFocus, false);
        });

        Assert.IsEmpty(Context.JSInterop.Invocations["BitBlazorUI.Utils.setupFocusTrap"]);

        component.Render(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.TrapFocus, true);
        });

        Context.JSInterop.VerifyInvoke("BitBlazorUI.Utils.setupFocusTrap");
    }

    [TestMethod]
    public void BitDialogRenderingAgainWithTheTrapUnchangedShouldNotRegisterItTwice()
    {
        var component = RenderComponent<BitDialog>(parameters => parameters.Add(p => p.IsOpen, true));

        component.Render(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Message, "A render that changed nothing the trap depends on.");
        });

        Assert.HasCount(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.setupFocusTrap"]);
    }

    [TestMethod]
    public void BitDialogDefaultDragHandleShouldBeTheHeader()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.IsDraggable, true);
            parameters.Add(p => p.Title, "Draggable");
        });

        var containerId = component.Find(".bit-dlg-ctn").GetAttribute("id");
        var selector = Context.JSInterop.Invocations["BitBlazorUI.DragDrop.setup"][0].Arguments[2];

        // A window is dragged by its title bar, so the content underneath it stays usable.
        Assert.AreEqual($"[id=\"{containerId}\"] > .bit-dlg-hdr", selector);
    }

    [TestMethod]
    public void BitDialogWithoutAHeaderShouldFallBackToTheWholeContainerAsTheDragHandle()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.IsDraggable, true);
            parameters.Add(p => p.ShowCloseButton, false);
        });

        var containerId = component.Find(".bit-dlg-ctn").GetAttribute("id");
        var selector = Context.JSInterop.Invocations["BitBlazorUI.DragDrop.setup"][0].Arguments[2];

        Assert.IsEmpty(component.FindAll(".bit-dlg-hdr"));
        Assert.AreEqual($"[id=\"{containerId}\"]", selector);
    }

    [TestMethod]
    public void BitDialogDragElementSelectorShouldBePassedThroughAndReusedOnTeardown()
    {
        var isOpen = true;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.IsDraggable, true);
            parameters.Add(p => p.DragElementSelector, ".my-handle");
        });

        Assert.AreEqual(".my-handle", Context.JSInterop.Invocations["BitBlazorUI.DragDrop.setup"][0].Arguments[2]);

        component.Find(".bit-dlg-cls").Click();

        component.WaitForAssertion(() =>
        {
            var invocations = Context.JSInterop.Invocations["BitBlazorUI.DragDrop.remove"];
            Assert.IsNotEmpty(invocations);
            // Teardown targets the selector the handlers were registered with.
            Assert.AreEqual(".my-handle", invocations[^1].Arguments[1]);
        }, TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public void BitDialogClosingAfterTheHandleMovedShouldTearDownTheHandleItIsOnNow()
    {
        var isOpen = true;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.IsDraggable, true);
            parameters.Add(p => p.DragElementSelector, ".my-handle");
        });

        component.Render(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.IsDraggable, true);
            parameters.Add(p => p.DragElementSelector, ".another-handle");
        });

        component.Find(".bit-dlg-cls").Click();

        component.WaitForAssertion(() =>
        {
            var invocations = Context.JSInterop.Invocations["BitBlazorUI.DragDrop.remove"];
            // One for the rebind the change asked for, one for the closing - each naming the handle the
            // handlers were actually on at the time.
            Assert.HasCount(2, invocations);
            Assert.AreEqual(".my-handle", invocations[0].Arguments[1]);
            Assert.AreEqual(".another-handle", invocations[1].Arguments[1]);
        }, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region close icon

    [TestMethod]
    public void BitDialogDefaultCloseIconShouldRenderCancelIcon()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowCloseButton, true);
        });

        var icon = component.Find(".bit-dlg-cli");

        Assert.IsTrue(icon.ClassList.Contains("bit-icon"));
        Assert.IsTrue(icon.ClassList.Contains("bit-icon--Cancel"));
    }

    [TestMethod,
        DataRow("ChromeClose"),
        DataRow("Cancel")
    ]
    public void BitDialogCloseIconNameTest(string iconName)
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowCloseButton, true);
            parameters.Add(p => p.CloseIconName, iconName);
        });

        var icon = component.Find(".bit-dlg-cli");

        Assert.IsTrue(icon.ClassList.Contains("bit-icon"));
        Assert.IsTrue(icon.ClassList.Contains($"bit-icon--{iconName}"));
    }

    [TestMethod,
        DataRow("fa-solid fa-xmark"),
        DataRow("bi bi-x-lg")
    ]
    public void BitDialogCloseIconWithCssClassesTest(string cssClasses)
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowCloseButton, true);
            parameters.Add(p => p.CloseIcon, (BitIconInfo)cssClasses!);
        });

        var icon = component.Find(".bit-dlg-cli");

        var classes = cssClasses.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var cls in classes)
        {
            Assert.IsTrue(icon.ClassList.Contains(cls), $"Icon should contain class '{cls}'");
        }
    }

    [TestMethod]
    public void BitDialogCloseIconInfoCssHelperTest()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowCloseButton, true);
            parameters.Add(p => p.CloseIcon, BitIconInfo.Css("fa-solid fa-circle-xmark"));
        });

        var icon = component.Find(".bit-dlg-cli");

        Assert.IsTrue(icon.ClassList.Contains("fa-solid"));
        Assert.IsTrue(icon.ClassList.Contains("fa-circle-xmark"));
    }

    [TestMethod]
    public void BitDialogCloseIconInfoFaHelperTest()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowCloseButton, true);
            parameters.Add(p => p.CloseIcon, BitIconInfo.Fa("solid xmark"));
        });

        var icon = component.Find(".bit-dlg-cli");

        Assert.IsTrue(icon.ClassList.Contains("fa-solid"));
        Assert.IsTrue(icon.ClassList.Contains("fa-xmark"));
    }

    [TestMethod]
    public void BitDialogCloseIconInfoBiHelperTest()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowCloseButton, true);
            parameters.Add(p => p.CloseIcon, BitIconInfo.Bi("x-lg"));
        });

        var icon = component.Find(".bit-dlg-cli");

        Assert.IsTrue(icon.ClassList.Contains("bi"));
        Assert.IsTrue(icon.ClassList.Contains("bi-x-lg"));
    }

    [TestMethod]
    public void BitDialogCloseIconTakesPrecedenceOverCloseIconNameTest()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowCloseButton, true);
            parameters.Add(p => p.CloseIcon, BitIconInfo.Fa("solid xmark"));
            parameters.Add(p => p.CloseIconName, "Cancel");
        });

        var icon = component.Find(".bit-dlg-cli");

        // CloseIcon parameter should take precedence
        Assert.IsTrue(icon.ClassList.Contains("fa-solid"));
        Assert.IsTrue(icon.ClassList.Contains("fa-xmark"));

        // Should not contain CloseIconName classes
        Assert.IsFalse(icon.ClassList.Contains("bit-icon"));
        Assert.IsFalse(icon.ClassList.Contains("bit-icon--Cancel"));
    }

    #endregion

    #region guarded close

    [TestMethod]
    public void BitDialogOnDismissingShouldRunBeforeTheCloseButtonCloses()
    {
        var isOpen = true;
        BitDialogDismissReason? seen = null;
        var openWhenAsked = false;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.OnDismissing, (BitDialogDismissArgs args) =>
            {
                seen = args.Reason;
                openWhenAsked = isOpen;
            });
        });

        component.Find(".bit-dlg-cls").Click();

        Assert.AreEqual(BitDialogDismissReason.CloseButton, seen);
        Assert.IsTrue(openWhenAsked);
        Assert.IsFalse(isOpen);
    }

    [TestMethod]
    public void BitDialogOnDismissingShouldSeeTheReasonOfTheOkButton()
    {
        Assert.AreEqual(BitDialogDismissReason.OkButton, DismissingReasonOf(c => c.Find(".bit-dlg-okb").Click()));
    }

    [TestMethod]
    public void BitDialogOnDismissingShouldSeeTheReasonOfTheCancelButton()
    {
        Assert.AreEqual(BitDialogDismissReason.CancelButton, DismissingReasonOf(c => c.Find(".bit-dlg-cnb").Click()));
    }

    [TestMethod]
    public void BitDialogOnDismissingShouldSeeTheReasonOfTheOverlay()
    {
        Assert.AreEqual(BitDialogDismissReason.OverlayClick, DismissingReasonOf(c => c.Find(".bit-dlg-ovl").Click()));
    }

    [TestMethod]
    public void BitDialogOnDismissingShouldSeeTheReasonOfTheEscapeKey()
    {
        Assert.AreEqual(BitDialogDismissReason.Escape,
                        DismissingReasonOf(c => c.Find(".bit-dlg-ctn").KeyDown(new KeyboardEventArgs { Key = "Escape" })));
    }

    [TestMethod]
    public void BitDialogOnDismissingShouldSeeTheReasonOfAProgrammaticClose()
    {
        Assert.AreEqual(BitDialogDismissReason.Programmatic,
                        DismissingReasonOf(c => c.InvokeAsync(() => c.Instance.Close()).GetAwaiter().GetResult()));
    }

    // Opens a Dialog, makes the given gesture on it, and reports the reason OnDismissing was handed.
    private BitDialogDismissReason? DismissingReasonOf(Action<IRenderedComponent<BitDialog>> gesture)
    {
        BitDialogDismissReason? seen = null;
        var isOpen = true;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.OnDismissing, (BitDialogDismissArgs args) => seen = args.Reason);
        });

        gesture(component);

        return seen;
    }

    [TestMethod]
    public void BitDialogCancelledDismissingShouldKeepTheDialogOpenAndUnanswered()
    {
        var isOpen = true;
        var dismissCount = 0;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.OnDismissing, (BitDialogDismissArgs args) => args.Cancel = true);
            parameters.Add(p => p.OnDismiss, () => dismissCount++);
        });

        component.Find(".bit-dlg-okb").Click();

        Assert.IsTrue(isOpen);
        Assert.AreEqual(0, dismissCount);
        Assert.IsNull(component.Instance.Result);
        Assert.IsNull(component.Instance.DismissReason);
    }

    [TestMethod]
    public void BitDialogCancelledDismissingShouldTakeBackTheCancelAnswerToo()
    {
        var isOpen = true;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.OnDismissing, (BitDialogDismissArgs args) => args.Cancel = true);
        });

        component.Find(".bit-dlg-cnb").Click();

        Assert.IsTrue(isOpen);
        Assert.IsNull(component.Instance.Result);
    }

    [TestMethod]
    public void BitDialogCancelledDismissingShouldBePlayedBackAndReported()
    {
        var isOpen = true;
        BitDialogDismissReason? prevented = null;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.OnDismissing, (BitDialogDismissArgs args) => args.Cancel = true);
            parameters.Add(p => p.OnDismissPrevented, (BitDialogDismissReason r) => prevented = r);
        });

        component.Find(".bit-dlg-ctn").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.IsTrue(isOpen);
        Assert.AreEqual(BitDialogDismissReason.Escape, prevented);
        component.WaitForAssertion(
            () => Assert.IsFalse(component.Find(".bit-dlg-ctn").ClassList.Contains("bit-dlg-prv")),
            TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public async Task BitDialogCancelledDismissingShouldRefuseAProgrammaticCloseToo()
    {
        var isOpen = true;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.OnDismissing, (BitDialogDismissArgs args) => args.Cancel = true);
        });

        await component.InvokeAsync(() => component.Instance.Close());

        Assert.IsTrue(isOpen);
        Assert.IsNull(component.Instance.DismissReason);
    }

    [TestMethod]
    public async Task BitDialogProgrammaticCloseShouldInvokeOnDismiss()
    {
        var isOpen = true;
        var dismissCount = 0;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.OnDismiss, () => dismissCount++);
        });

        await component.InvokeAsync(() => component.Instance.Close());

        Assert.IsFalse(isOpen);
        Assert.AreEqual(1, dismissCount);
    }

    [TestMethod]
    public async Task BitDialogCloseOnAnAlreadyClosedDialogShouldDoNothing()
    {
        var isOpen = false;
        var dismissingCount = 0;
        var dismissCount = 0;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.OnDismissing, (BitDialogDismissArgs _) => dismissingCount++);
            parameters.Add(p => p.OnDismiss, () => dismissCount++);
        });

        await component.InvokeAsync(() => component.Instance.Close());

        Assert.AreEqual(0, dismissingCount);
        Assert.AreEqual(0, dismissCount);
    }

    [TestMethod]
    public async Task BitDialogToggleShouldCloseThroughTheSameDismissalPath()
    {
        var isOpen = true;
        BitDialogDismissReason? seen = null;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.OnDismissing, (BitDialogDismissArgs args) => seen = args.Reason);
        });

        await component.InvokeAsync(() => component.Instance.Toggle());

        Assert.IsFalse(isOpen);
        Assert.AreEqual(BitDialogDismissReason.Programmatic, seen);
    }

    [TestMethod]
    public async Task BitDialogCancelledDismissingShouldLeaveShowStillWaiting()
    {
        var isOpen = false;
        var refuse = true;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.OnDismissing, (BitDialogDismissArgs args) => args.Cancel = refuse);
        });

        var showing = component.InvokeAsync(() => component.Instance.Show());

        component.Find(".bit-dlg-cnb").Click();

        Assert.IsTrue(isOpen);
        Assert.IsFalse(showing.IsCompleted);

        refuse = false;
        component.Find(".bit-dlg-cnb").Click();

        Assert.AreEqual(BitDialogResult.Cancel, await showing);
    }

    [TestMethod]
    public void BitDialogNoDismissPreventedAnimationShouldStillReportTheRefusal()
    {
        var isOpen = true;
        BitDialogDismissReason? prevented = null;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.IsBlocking, true);
            parameters.Add(p => p.NoDismissPreventedAnimation, true);
            parameters.Add(p => p.OnDismissPrevented, (BitDialogDismissReason r) => prevented = r);
        });

        component.Find(".bit-dlg-ctn").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.IsTrue(isOpen);
        Assert.AreEqual(BitDialogDismissReason.Escape, prevented);
        Assert.IsFalse(component.Find(".bit-dlg-ctn").ClassList.Contains("bit-dlg-prv"));
    }

    [TestMethod]
    public void BitDialogShouldHoldEveryWayOutShutWhileOnDismissingIsStillDeciding()
    {
        var gate = new TaskCompletionSource();
        var isOpen = true;
        var okCount = 0;
        var dismissingCount = 0;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.OnOk, () => okCount++);
            parameters.Add(p => p.OnDismissing, async (BitDialogDismissArgs _) =>
            {
                dismissingCount++;
                await gate.Task;
            });
        });

        component.Find(".bit-dlg-okb").Click();

        // The first press is still waiting on OnDismissing, so nothing else may answer the showing over it.
        component.Find(".bit-dlg-okb").Click();
        component.Find(".bit-dlg-cnb").Click();
        component.Find(".bit-dlg-cls").Click();
        component.Find(".bit-dlg-ovl").Click();
        component.Find(".bit-dlg-ctn").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.AreEqual(1, okCount);
        Assert.AreEqual(1, dismissingCount);
        Assert.IsTrue(isOpen);

        gate.SetResult();

        component.WaitForAssertion(() => Assert.IsFalse(isOpen), TimeSpan.FromSeconds(5));
        Assert.AreEqual(BitDialogResult.Ok, component.Instance.Result);
    }

    [TestMethod]
    public void BitDialogRefusedOverlayClickShouldLeaveTheFocusWhereItWas()
    {
        var isOpen = true;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.OnDismissing, (BitDialogDismissArgs args) => args.Cancel = true);
        });

        component.Find(".bit-dlg-ovl").Click();

        // The press on the overlay never took the focus off the Dialog in the first place, so a refusal -
        // which leaves the Dialog standing for as long as the movement lasts and beyond - has no focus to
        // go chasing after.
        Assert.IsEmpty(Context.JSInterop.Invocations["BitBlazorUI.Utils.containsActiveElement"]);
        Assert.IsTrue(isOpen);
    }

    [TestMethod]
    public void BitDialogBlockingShouldRefuseWithoutAskingOnDismissing()
    {
        var isOpen = true;
        var dismissingCount = 0;
        BitDialogDismissReason? prevented = null;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.IsBlocking, true);
            parameters.Add(p => p.OnDismissing, (BitDialogDismissArgs _) => dismissingCount++);
            parameters.Add(p => p.OnDismissPrevented, (BitDialogDismissReason r) => prevented = r);
        });

        component.Find(".bit-dlg-ctn").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        component.Find(".bit-dlg-ovl").Click();

        // A blocking Dialog is refusing on its own terms, so there is nothing for OnDismissing to decide.
        Assert.AreEqual(0, dismissingCount);
        Assert.AreEqual(BitDialogDismissReason.OverlayClick, prevented);
        Assert.IsTrue(isOpen);
    }

    [TestMethod]
    public void BitDialogHeaderTemplateShouldLeaveTheDialogToNameItself()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Title, "Title");
            parameters.Add(p => p.AriaLabel, "Named by hand");
            parameters.Add(p => p.HeaderTemplate, (RenderFragment)(builder => builder.AddContent(0, "Custom header")));
        });

        var container = component.Find(".bit-dlg-ctn");

        // The Title is not rendered under a HeaderTemplate, so pointing aria-labelledby at it would name the
        // Dialog after an element that is not there.
        Assert.IsFalse(container.HasAttribute("aria-labelledby"));
        Assert.AreEqual("Named by hand", container.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitDialogShouldCarryTheConsumerIdOnItsRoot()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Id, "my-dialog");
        });

        Assert.AreEqual("my-dialog", component.Find(".bit-dlg").GetAttribute("id"));

        // The parts the Dialog names for ARIA are keyed off its own unique id rather than off this one, so
        // an id a consumer picks can never collide with them.
        Assert.AreNotEqual("my-dialog", component.Find(".bit-dlg-ctn").GetAttribute("id"));
    }

    #endregion

    #region color

    [TestMethod]
    [DataRow(BitColor.Primary, "bit-dlg-pri")]
    [DataRow(BitColor.Secondary, "bit-dlg-sec")]
    [DataRow(BitColor.Tertiary, "bit-dlg-ter")]
    [DataRow(BitColor.Info, "bit-dlg-inf")]
    [DataRow(BitColor.Success, "bit-dlg-suc")]
    [DataRow(BitColor.Warning, "bit-dlg-wrn")]
    [DataRow(BitColor.SevereWarning, "bit-dlg-swr")]
    [DataRow(BitColor.Error, "bit-dlg-err")]
    [DataRow(BitColor.PrimaryBackground, "bit-dlg-pbg")]
    [DataRow(BitColor.SecondaryBackground, "bit-dlg-sbg")]
    [DataRow(BitColor.TertiaryBackground, "bit-dlg-tbg")]
    [DataRow(BitColor.PrimaryForeground, "bit-dlg-pfg")]
    [DataRow(BitColor.SecondaryForeground, "bit-dlg-sfg")]
    [DataRow(BitColor.TertiaryForeground, "bit-dlg-tfg")]
    [DataRow(BitColor.PrimaryBorder, "bit-dlg-pbr")]
    [DataRow(BitColor.SecondaryBorder, "bit-dlg-sbr")]
    [DataRow(BitColor.TertiaryBorder, "bit-dlg-tbr")]
    public void BitDialogColorShouldRenderItsClass(BitColor color, string expectedClass)
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Color, color);
        });

        Assert.IsTrue(component.Find(".bit-dlg").ClassList.Contains(expectedClass));
    }

    [TestMethod]
    public void BitDialogWithoutAColorShouldCarryNoColorClass()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
        });

        var classes = component.Find(".bit-dlg").ClassList;

        Assert.IsFalse(classes.Contains("bit-dlg-pri"));
        Assert.IsFalse(classes.Contains("bit-dlg-err"));
    }

    [TestMethod]
    public void BitDialogColorShouldBeUpdatedOnRerender()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Color, BitColor.Info);
        });

        Assert.IsTrue(component.Find(".bit-dlg").ClassList.Contains("bit-dlg-inf"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Color, BitColor.Error);
        });

        var classes = component.Find(".bit-dlg").ClassList;

        Assert.IsTrue(classes.Contains("bit-dlg-err"));
        Assert.IsFalse(classes.Contains("bit-dlg-inf"));
    }

    #endregion

    #region size

    [TestMethod]
    public void BitDialogSizeParametersShouldReachTheContainerAsCustomProperties()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Width, "30rem");
            parameters.Add(p => p.MinWidth, "20rem");
            parameters.Add(p => p.MaxWidth, "min(100%, 40rem)");
            parameters.Add(p => p.Height, "24rem");
            parameters.Add(p => p.MinHeight, "10rem");
            parameters.Add(p => p.MaxHeight, "80%");
        });

        var style = component.Find(".bit-dlg-ctn").GetAttribute("style");

        StringAssert.Contains(style, "--bit-dlg-wid:30rem;");
        StringAssert.Contains(style, "--bit-dlg-mnw:20rem;");
        StringAssert.Contains(style, "--bit-dlg-mxw:min(100%, 40rem);");
        StringAssert.Contains(style, "--bit-dlg-hei:24rem;");
        StringAssert.Contains(style, "--bit-dlg-mnh:10rem;");
        StringAssert.Contains(style, "--bit-dlg-mxh:80%;");
    }

    [TestMethod]
    public void BitDialogSizeParametersShouldKeepTheConsumerContainerStyleAfterThem()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Width, "30rem");
            parameters.Add(p => p.Styles, new BitDialogClassStyles { Container = "border:1px solid red" });
        });

        var style = component.Find(".bit-dlg-ctn").GetAttribute("style");

        StringAssert.Contains(style, "--bit-dlg-wid:30rem;");
        StringAssert.Contains(style, "border:1px solid red");
        Assert.IsTrue(style!.IndexOf("--bit-dlg-wid", StringComparison.Ordinal) < style.IndexOf("border:1px", StringComparison.Ordinal));
    }

    [TestMethod]
    public void BitDialogWithoutSizeParametersShouldStopWideningAtTheThemesDialogWidth()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
        });

        var style = component.Find(".bit-dlg-ctn").GetAttribute("style");

        // A Dialog is as wide as its content, so without a ceiling a two-sentence confirmation spans the
        // screen. The ceiling is the design system's, and it is capped at the area the Dialog is in as well.
        Assert.AreEqual($"--bit-dlg-mxw:min(100%,var({BitCss.Var.Size.DialogMaxWidth}));", style);
    }

    [TestMethod]
    public void BitDialogMaxWidthShouldReplaceTheDefaultCeilingRatherThanJoinIt()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.MaxWidth, "40rem");
        });

        Assert.AreEqual("--bit-dlg-mxw:40rem;", component.Find(".bit-dlg-ctn").GetAttribute("style"));
    }

    [TestMethod]
    public void BitDialogWidthShouldLeaveTheDefaultCeilingOut()
    {
        // A width of its own is a decision about how wide the Dialog is, so the default ceiling has nothing
        // left to decide - and the stylesheet's own 100% cap still keeps it inside the area it sits in.
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Width, "60rem");
        });

        Assert.AreEqual("--bit-dlg-wid:60rem;", component.Find(".bit-dlg-ctn").GetAttribute("style"));
    }

    [TestMethod]
    [DataRow(true, false)]
    [DataRow(false, true)]
    public void BitDialogFullWidthShouldLeaveTheDefaultCeilingOut(bool fullWidth, bool fullSize)
    {
        // A full-width Dialog is asking for the whole of the area by name; capping it at the theme's dialog
        // width would answer with something narrower.
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.FullWidth, fullWidth);
            parameters.Add(p => p.FullSize, fullSize);
        });

        var style = component.Find(".bit-dlg-ctn").GetAttribute("style") ?? string.Empty;

        Assert.IsFalse(style.Contains("--bit-dlg-mxw", StringComparison.Ordinal),
            $"A full-width Dialog must not carry the default width ceiling, got '{style}'.");
    }

    [TestMethod]
    public void BitDialogFullHeightShouldStillCarryTheDefaultWidthCeiling()
    {
        // FullHeight says nothing about how wide the Dialog is, so the ceiling still applies.
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.FullHeight, true);
        });

        StringAssert.Contains(component.Find(".bit-dlg-ctn").GetAttribute("style"), "--bit-dlg-mxw:min(100%,var(");
    }

    [TestMethod]
    public void BitDialogSizeParametersShouldBeUpdatedOnRerender()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.MaxWidth, "20rem");
        });

        StringAssert.Contains(component.Find(".bit-dlg-ctn").GetAttribute("style"), "--bit-dlg-mxw:20rem;");

        component.Render(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.MaxWidth, "40rem");
        });

        StringAssert.Contains(component.Find(".bit-dlg-ctn").GetAttribute("style"), "--bit-dlg-mxw:40rem;");
    }

    #endregion
}
