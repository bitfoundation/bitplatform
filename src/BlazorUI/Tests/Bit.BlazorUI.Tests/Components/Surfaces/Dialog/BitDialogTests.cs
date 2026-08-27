using System;
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
    public void BitDialogKeepMountedShouldRenderTheClosedDialogHidden()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, false);
            parameters.Add(p => p.KeepMounted, true);
            parameters.Add(p => p.Title, "Test Title");
        });

        var root = component.Find(".bit-dlg");
        Assert.IsTrue(root.ClassList.Contains("bit-dlg-hdn"));
        Assert.AreEqual("Test Title", component.Find(".bit-dlg-ttl").TextContent);
    }

    [TestMethod]
    public void BitDialogKeepMountedShouldDropTheHiddenClassWhenItOpens()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, false);
            parameters.Add(p => p.KeepMounted, true);
        });

        Assert.IsTrue(component.Find(".bit-dlg").ClassList.Contains("bit-dlg-hdn"));

        component.Render(parameters => parameters.Add(p => p.IsOpen, true));

        Assert.IsFalse(component.Find(".bit-dlg").ClassList.Contains("bit-dlg-hdn"));
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
    [DataRow(BitDialogPosition.Center, "bit-dlg-ctr")]
    [DataRow(BitDialogPosition.TopLeft, "bit-dlg-tl")]
    [DataRow(BitDialogPosition.TopCenter, "bit-dlg-tc")]
    [DataRow(BitDialogPosition.TopRight, "bit-dlg-tr")]
    [DataRow(BitDialogPosition.CenterLeft, "bit-dlg-cl")]
    [DataRow(BitDialogPosition.CenterRight, "bit-dlg-cr")]
    [DataRow(BitDialogPosition.BottomLeft, "bit-dlg-bl")]
    [DataRow(BitDialogPosition.BottomCenter, "bit-dlg-bc")]
    [DataRow(BitDialogPosition.BottomRight, "bit-dlg-br")]
    [DataRow(BitDialogPosition.TopStart, "bit-dlg-ts")]
    [DataRow(BitDialogPosition.TopEnd, "bit-dlg-te")]
    [DataRow(BitDialogPosition.CenterStart, "bit-dlg-cs")]
    [DataRow(BitDialogPosition.CenterEnd, "bit-dlg-ce")]
    [DataRow(BitDialogPosition.BottomStart, "bit-dlg-bs")]
    [DataRow(BitDialogPosition.BottomEnd, "bit-dlg-be")]
    public void BitDialogPositionShouldRenderItsClass(BitDialogPosition position, string expectedClass)
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Position, position);
        });

        Assert.IsTrue(component.Find(".bit-dlg-doc").ClassList.Contains(expectedClass));
    }

    [TestMethod]
    public void BitDialogPositionShouldBeUpdatedOnRerender()
    {
        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Position, BitDialogPosition.TopLeft);
        });

        Assert.IsTrue(component.Find(".bit-dlg-doc").ClassList.Contains("bit-dlg-tl"));

        component.Render(parameters => parameters.Add(p => p.Position, BitDialogPosition.BottomEnd));

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
        Context.JSInterop.VerifyInvoke("BitBlazorUI.Utils.saveFocus");
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
    public void BitDialogBlockingOverlayClickShouldCheckTheFocusStillSitsInsideTheDialog()
    {
        var isOpen = true;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.IsBlocking, true);
        });

        component.Find(".bit-dlg-ovl").Click();

        // A click on the overlay leaves the body holding the focus, which is outside the trap - so the
        // Dialog asks where the focus is before deciding to take it back.
        component.WaitForAssertion(
            () => Context.JSInterop.VerifyInvoke("BitBlazorUI.Utils.containsActiveElement"),
            TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public void BitDialogModelessShouldNotReclaimTheFocusAfterAnOverlayClick()
    {
        var isOpen = true;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.IsBlocking, true);
            parameters.Add(p => p.TrapFocus, false);
        });

        component.Find(".bit-dlg-ovl").Click();

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

        Assert.IsEmpty(Context.JSInterop.Invocations["BitBlazorUI.Utils.saveFocus"]);
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
        Assert.AreEqual(".my-scroller", invocation.Arguments[0]);
        Assert.AreEqual(true, invocation.Arguments[1]);

        component.Find(".bit-dlg-cls").Click();

        component.WaitForAssertion(() =>
        {
            var invocations = Context.JSInterop.Invocations["BitBlazorUI.Utils.toggleOverflow"];
            Assert.HasCount(2, invocations);
            Assert.AreEqual(".my-scroller", invocations[1].Arguments[0]);
            Assert.AreEqual(false, invocations[1].Arguments[1]);
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
            Assert.AreEqual(".first-scroller", invocations[1].Arguments[0]);
            Assert.AreEqual(false, invocations[1].Arguments[1]);
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
        Assert.AreEqual(false, invocations[1].Arguments[1]);
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

        component.Render(parameters => parameters.Add(p => p.DragElementSelector, ".another-handle"));

        component.Find(".bit-dlg-cls").Click();

        component.WaitForAssertion(() =>
        {
            var invocations = Context.JSInterop.Invocations["BitBlazorUI.DragDrop.remove"];
            Assert.IsNotEmpty(invocations);
            // Teardown targets the selector the handlers were registered with, not the current one.
            Assert.AreEqual(".my-handle", invocations[^1].Arguments[1]);
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
}
