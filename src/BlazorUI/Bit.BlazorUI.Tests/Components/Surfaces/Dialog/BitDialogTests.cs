using System.Threading.Tasks;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Dialog;

[TestClass]
public class BitDialogTests : BunitTestContext
{
    [TestMethod]
    public void BitDialogShouldRenderTitleMessageAndButtonsWhenOpen()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

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

        Assert.IsTrue(okBtn.Count == 1);
        Assert.IsTrue(cancelBtn.Count == 1);

        var overlay = component.FindAll(".bit-dlg-ovl");
        Assert.AreEqual(1, overlay.Count);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitDialogOverlayClickRespectsIsBlocking(bool isBlocking)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

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
        Assert.AreEqual(isBlocking ? 1 : 1, overlays.Count);

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
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var isOpen = true;
        var closedCount = 0;

        var component = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.OnClose, () => closedCount++);
        });

        var closeButton = component.Find(".bit-dlg-cls");
        closeButton.Click();

        Assert.IsFalse(isOpen);
        Assert.AreEqual(1, closedCount);
    }

    [TestMethod]
    public async Task BitDialogOkAndCancelShouldReturnProperResultWhenUsingShow()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var componentOk = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.OkText, "OK");
            parameters.Add(p => p.CancelText, "Cancel");
        });

        var showTask = componentOk.Instance.Show();

        var okBtn = componentOk.Find(".bit-dlg-okb");
        okBtn.Click();

        var result = await showTask;
        Assert.AreEqual(BitDialogResult.Ok, result);

        var componentCancel = RenderComponent<BitDialog>(parameters =>
        {
            parameters.Add(p => p.OkText, "OK");
            parameters.Add(p => p.CancelText, "Cancel");
        });

        var showTask2 = componentCancel.Instance.Show();

        var cancelBtn = componentCancel.Find(".bit-dlg-cnb");
        cancelBtn.Click();

        var result2 = await showTask2;
        Assert.AreEqual(BitDialogResult.Cancel, result2);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow(true)]
    [DataRow(false)]
    public void BitDialogRoleRespectsIsAlertAndBlocking(bool? isAlert)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

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

        var roleDiv = component.Find("div[role]");

        var expected = (isAlert.HasValue ? isAlert.Value : true) ? "alertdialog" : "dialog";

        Assert.AreEqual(expected, roleDiv.GetAttribute("role"));
    }
}
