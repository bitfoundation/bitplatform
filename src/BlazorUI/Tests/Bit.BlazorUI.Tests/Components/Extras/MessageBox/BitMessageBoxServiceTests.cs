using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.MessageBox;

/// <summary>
/// The service half of the message box: what it renders inside the modal, how the dialog is named and
/// described, and what the answer it hands back is for each way the box can end.
/// </summary>
[TestClass]
public class BitMessageBoxServiceTests : BunitTestContext
{
    private BitModalService ModalService => Services.GetRequiredService<BitModalService>();

    private BitMessageBoxService MessageBoxService => Services.GetRequiredService<BitMessageBoxService>();

    [TestInitialize]
    public void SetupServices()
    {
        Services.AddSingleton<BitModalService>();
        Services.AddSingleton<BitMessageBoxService>();
    }



    [TestMethod]
    public async Task BitMessageBoxServiceShouldShowAMessageBoxInsideAModal()
    {
        var container = RenderComponent<BitModalContainer>();

        var showing = MessageBoxService.Show("The title", "The body");

        container.WaitForAssertion(() =>
        {
            Assert.ContainsSingle(container.FindAll(".bit-mdl .bit-msb"));
            Assert.Contains("The title", container.Markup);
            Assert.Contains("The body", container.Markup);
        });

        container.Find(".bit-msb-ftr .bit-btn").Click();

        Assert.AreEqual(BitMessageBoxResult.Ok, await showing);
    }

    [TestMethod]
    public async Task BitMessageBoxServiceShouldNameAndDescribeTheDialogWithTheTitleAndTheBody()
    {
        var container = RenderComponent<BitModalContainer>();

        var showing = MessageBoxService.Show("The title", "The body");

        container.WaitForAssertion(() => Assert.ContainsSingle(container.FindAll(".bit-msb")));

        var dialog = container.Find("[role='dialog']");
        var titleId = container.Find(".bit-msb-ttl").Id;
        var bodyId = container.Find(".bit-msb-bdy").Id;

        Assert.AreEqual(titleId, dialog.GetAttribute("aria-labelledby"));
        Assert.AreEqual(bodyId, dialog.GetAttribute("aria-describedby"));

        container.Find(".bit-msb-ftr .bit-btn").Click();

        await showing;
    }

    [TestMethod]
    public async Task BitMessageBoxServiceShouldNameTheDialogWithTheBodyWhenThereIsNoTitle()
    {
        var container = RenderComponent<BitModalContainer>();

        var showing = MessageBoxService.Show(new BitMessageBoxParameters { Body = "Only a body." }, TestContext.CancellationToken);

        container.WaitForAssertion(() => Assert.ContainsSingle(container.FindAll(".bit-msb")));

        var dialog = container.Find("[role='dialog']");

        Assert.AreEqual("Only a body.", dialog.GetAttribute("aria-label"));
        Assert.IsNull(dialog.GetAttribute("aria-labelledby"));

        container.Find(".bit-msb-ftr .bit-btn").Click();

        await showing;
    }

    [TestMethod]
    public async Task BitMessageBoxServiceShouldNameTheDialogWithTheWordsWhenAHeaderTemplateTakesTheTitleAway()
    {
        var container = RenderComponent<BitModalContainer>();

        var showing = MessageBoxService.Show(new BitMessageBoxParameters
        {
            Title = "The title",
            Body = "The body",
            HeaderTemplate = builder => builder.AddMarkupContent(0, "<div class=\"custom-header\"></div>")
        }, TestContext.CancellationToken);

        container.WaitForAssertion(() => Assert.ContainsSingle(container.FindAll(".custom-header")));

        var dialog = container.Find("[role=dialog]");

        // The title element was never rendered, so pointing aria-labelledby at it would leave the dialog nameless.
        Assert.IsNull(dialog.GetAttribute("aria-labelledby"));
        Assert.AreEqual("The title", dialog.GetAttribute("aria-label"));

        container.Find(".bit-msb-ftr .bit-btn").Click();

        await showing;
    }

    [TestMethod]
    public async Task BitMessageBoxServiceShouldPaintTheAffirmativeButtonOfADestructiveQuestion()
    {
        var container = RenderComponent<BitModalContainer>();

        var confirming = MessageBoxService.Confirm(new BitMessageBoxParameters
        {
            Title = "Delete",
            Body = "Delete this file?",
            Buttons = BitMessageBoxButtons.YesNo,
            PrimaryButtonColor = BitColor.Error
        }, TestContext.CancellationToken);

        container.WaitForAssertion(() => Assert.HasCount(2, container.FindAll(".bit-msb-ftr .bit-btn")));

        var buttons = container.FindAll(".bit-msb-ftr .bit-btn");
        Assert.IsTrue(buttons[0].ClassList.Contains("bit-btn-err"));
        Assert.IsTrue(buttons[1].ClassList.Contains("bit-btn-dft-ter"));

        container.FindAll(".bit-msb-ftr .bit-btn")[1].Click();

        Assert.IsFalse(await confirming);
    }

    [TestMethod]
    public async Task BitMessageBoxServiceShouldAnnounceAnUrgentMessageBoxAsAnAlert()
    {
        var container = RenderComponent<BitModalContainer>();

        var showing = MessageBoxService.ShowError("Error", "It went wrong.");

        container.WaitForAssertion(() => Assert.ContainsSingle(container.FindAll("[role='alertdialog']")));

        container.Find(".bit-msb-ftr .bit-btn").Click();

        await showing;
    }

    [TestMethod]
    public async Task BitMessageBoxServiceShouldLeaveANonUrgentMessageBoxAsAPlainDialog()
    {
        var container = RenderComponent<BitModalContainer>();

        var showing = MessageBoxService.ShowInfo("Info", "Worth knowing.");

        container.WaitForAssertion(() => Assert.ContainsSingle(container.FindAll("[role='dialog']")));

        Assert.IsEmpty(container.FindAll("[role='alertdialog']"));
        Assert.ContainsSingle(container.FindAll(".bit-msb-ico.bit-icon--Info"));

        container.Find(".bit-msb-ftr .bit-btn").Click();

        await showing;
    }

    [TestMethod]
    public async Task BitMessageBoxServiceShouldGiveEverySeverityHelperItsOwnGlyph()
    {
        var container = RenderComponent<BitModalContainer>();

        var showing = MessageBoxService.ShowSuccess("Success", "It worked.");

        container.WaitForAssertion(() => Assert.ContainsSingle(container.FindAll(".bit-msb-ico.bit-icon--Completed")));

        container.Find(".bit-msb-ftr .bit-btn").Click();
        await showing;

        showing = MessageBoxService.ShowWarning("Warning", "Careful.");

        container.WaitForAssertion(() => Assert.ContainsSingle(container.FindAll(".bit-msb-ico.bit-icon--Warning")));

        container.Find(".bit-msb-ftr .bit-btn").Click();
        await showing;
    }

    [TestMethod]
    public async Task BitMessageBoxServiceShouldReportTheButtonThatEndedTheShowing()
    {
        var container = RenderComponent<BitModalContainer>();

        var showing = MessageBoxService.Show("Title", "Body", BitMessageBoxButtons.YesNoCancel);

        container.WaitForAssertion(() => Assert.HasCount(3, container.FindAll(".bit-msb-ftr .bit-btn")));

        container.FindAll(".bit-msb-ftr .bit-btn")[1].Click();

        Assert.AreEqual(BitMessageBoxResult.No, await showing);
    }

    [TestMethod]
    public async Task BitMessageBoxServiceShouldReportNoAnswerFromTheCloseButton()
    {
        var container = RenderComponent<BitModalContainer>();

        var showing = MessageBoxService.Show("Title", "Body", BitMessageBoxButtons.OkCancel);

        container.WaitForAssertion(() => Assert.ContainsSingle(container.FindAll(".bit-msb")));

        container.Find(".bit-msb-hdr .bit-btn").Click();

        Assert.AreEqual(BitMessageBoxResult.None, await showing);
    }

    [TestMethod]
    public async Task BitMessageBoxServiceShouldCloseTheModalOnceTheMessageBoxIsAnswered()
    {
        var container = RenderComponent<BitModalContainer>();

        var showing = MessageBoxService.Show("Title", "Body");

        container.WaitForAssertion(() => Assert.ContainsSingle(container.FindAll(".bit-msb")));

        container.Find(".bit-msb-ftr .bit-btn").Click();

        await showing;

        container.WaitForAssertion(() => Assert.IsEmpty(container.FindAll(".bit-msb")));
        Assert.IsEmpty(ModalService.OpenModals);
    }

    [TestMethod]
    public async Task BitMessageBoxServiceShouldConfirmOnlyOnAnAffirmativeAnswer()
    {
        var container = RenderComponent<BitModalContainer>();

        var confirming = MessageBoxService.Confirm("Title", "Body");

        container.WaitForAssertion(() => Assert.HasCount(2, container.FindAll(".bit-msb-ftr .bit-btn")));

        container.FindAll(".bit-msb-ftr .bit-btn")[0].Click();

        Assert.IsTrue(await confirming);
    }

    [TestMethod]
    public async Task BitMessageBoxServiceShouldNotConfirmOnARefusal()
    {
        var container = RenderComponent<BitModalContainer>();

        var confirming = MessageBoxService.Confirm("Title", "Body");

        container.WaitForAssertion(() => Assert.HasCount(2, container.FindAll(".bit-msb-ftr .bit-btn")));

        container.FindAll(".bit-msb-ftr .bit-btn")[1].Click();

        Assert.IsFalse(await confirming);
    }

    [TestMethod]
    public async Task BitMessageBoxServiceShouldNotConfirmOnADismissal()
    {
        var container = RenderComponent<BitModalContainer>();

        var confirming = MessageBoxService.Confirm("Title", "Body");

        container.WaitForAssertion(() => Assert.ContainsSingle(container.FindAll(".bit-msb")));

        container.Find(".bit-msb-hdr .bit-btn").Click();

        Assert.IsFalse(await confirming);
    }

    [TestMethod]
    public async Task BitMessageBoxServiceShouldConfirmWithTheYesNoSetToo()
    {
        var container = RenderComponent<BitModalContainer>();

        var confirming = MessageBoxService.Confirm("Title", "Body", BitMessageBoxButtons.YesNo);

        container.WaitForAssertion(() =>
        {
            var texts = container.FindAll(".bit-msb-ftr .bit-btn-prt").Select(b => b.TextContent).ToArray();
            Assert.AreSequenceEqual(new[] { "Yes", "No" }, texts);
        });

        container.FindAll(".bit-msb-ftr .bit-btn")[0].Click();

        Assert.IsTrue(await confirming);
    }

    [TestMethod]
    public async Task BitMessageBoxServiceShouldPassTheParametersOnToTheMessageBox()
    {
        var container = RenderComponent<BitModalContainer>();

        var showing = MessageBoxService.Show(new BitMessageBoxParameters
        {
            Id = "the-box",
            Title = "Title",
            Body = "Body",
            Color = BitColor.Info,
            Size = BitSize.Large,
            Buttons = BitMessageBoxButtons.YesNo,
            YesText = "Sure",
            NoText = "Nope",
            CloseButtonTitle = "Dismiss",
            Dir = BitDir.Rtl
        }, TestContext.CancellationToken);

        container.WaitForAssertion(() => Assert.ContainsSingle(container.FindAll(".bit-msb")));

        var root = container.Find(".bit-msb");

        Assert.AreEqual("the-box", root.Id);
        Assert.IsTrue(root.ClassList.Contains("bit-msb-inf"));
        Assert.IsTrue(root.ClassList.Contains("bit-msb-lg"));
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.AreEqual("Dismiss", container.Find(".bit-msb-hdr .bit-btn").GetAttribute("title"));

        var texts = container.FindAll(".bit-msb-ftr .bit-btn-prt").Select(b => b.TextContent).ToArray();
        Assert.AreSequenceEqual(new[] { "Sure", "Nope" }, texts);

        container.FindAll(".bit-msb-ftr .bit-btn")[0].Click();

        Assert.AreEqual(BitMessageBoxResult.Yes, await showing);
    }

    [TestMethod]
    public async Task BitMessageBoxServiceShouldLetTheCallerOverrideWhatItWorkedOutForTheModal()
    {
        var container = RenderComponent<BitModalContainer>();

        var showing = MessageBoxService.Show(new BitMessageBoxParameters
        {
            Title = "Title",
            Body = "Body",
            Modal = new BitModalParameters { IsAlert = true }
        }, TestContext.CancellationToken);

        container.WaitForAssertion(() => Assert.ContainsSingle(container.FindAll("[role='alertdialog']")));

        container.Find(".bit-msb-ftr .bit-btn").Click();

        await showing;
    }

    [TestMethod]
    public async Task BitMessageBoxServiceShouldAnswerWithNoAnswerWhileNoContainerIsMounted()
    {
        Assert.IsFalse(ModalService.IsContainerAvailable);

        Assert.AreEqual(BitMessageBoxResult.None, await MessageBoxService.Show("Title", "Body"));
        Assert.IsFalse(await MessageBoxService.Confirm("Title", "Body"));
    }

    [TestMethod]
    public async Task BitMessageBoxServiceShouldAnnounceASevereWarningAsAnAlertWithItsOwnGlyph()
    {
        var container = RenderComponent<BitModalContainer>();

        var showing = MessageBoxService.ShowSevereWarning("Severe warning", "Out of space.");

        container.WaitForAssertion(() =>
        {
            Assert.ContainsSingle(container.FindAll("[role='alertdialog']"));
            Assert.ContainsSingle(container.FindAll(".bit-msb-ico.bit-icon--WarningSolid"));
        });

        container.Find(".bit-msb-ftr .bit-btn").Click();

        Assert.AreEqual(BitMessageBoxResult.Ok, await showing);
    }

    [TestMethod]
    public async Task BitMessageBoxServiceShouldShowAMessageBoxWithNoActionButtons()
    {
        var container = RenderComponent<BitModalContainer>();

        var showing = MessageBoxService.Show(new BitMessageBoxParameters
        {
            Title = "Title",
            Body = "Body",
            Buttons = BitMessageBoxButtons.None
        }, TestContext.CancellationToken);

        container.WaitForAssertion(() =>
        {
            Assert.ContainsSingle(container.FindAll(".bit-msb"));
            Assert.IsEmpty(container.FindAll(".bit-msb-ftr"));
        });

        // Dismissed rather than answered, which is what the None set is for.
        container.Find(".bit-msb-hdr .bit-btn").Click();

        Assert.AreEqual(BitMessageBoxResult.None, await showing);
    }

    [TestMethod]
    public async Task BitMessageBoxServiceShouldKeepTheModalOpenWhileTheGuardRefusesTheAnswer()
    {
        var container = RenderComponent<BitModalContainer>();

        var refuse = true;

        var showing = MessageBoxService.Show(new BitMessageBoxParameters
        {
            Title = "Title",
            Body = "Body",
            OnBeforeResult = EventCallback.Factory.Create<BitMessageBoxBeforeResultArgs>(
                                this, args => args.Cancel = refuse)
        }, TestContext.CancellationToken);

        container.WaitForAssertion(() => Assert.ContainsSingle(container.FindAll(".bit-msb")));

        container.Find(".bit-msb-ftr .bit-btn").Click();

        // Refused, so the box is still on the screen and the caller is still waiting for its answer.
        Assert.ContainsSingle(container.FindAll(".bit-msb"));
        Assert.IsFalse(showing.IsCompleted);

        refuse = false;

        container.Find(".bit-msb-ftr .bit-btn").Click();

        Assert.AreEqual(BitMessageBoxResult.Ok, await showing);
    }

    [TestMethod]
    public async Task BitMessageBoxServiceShouldAskTheGuardForTheCloseButtonToo()
    {
        var container = RenderComponent<BitModalContainer>();

        var seen = new List<BitMessageBoxResult>();

        var showing = MessageBoxService.Show(new BitMessageBoxParameters
        {
            Title = "Title",
            Body = "Body",
            OnBeforeResult = EventCallback.Factory.Create<BitMessageBoxBeforeResultArgs>(
                                this, args =>
                                {
                                    seen.Add(args.Result);
                                    args.Cancel = args.Result is BitMessageBoxResult.None;
                                })
        }, TestContext.CancellationToken);

        container.WaitForAssertion(() => Assert.ContainsSingle(container.FindAll(".bit-msb")));

        container.Find(".bit-msb-hdr .bit-btn").Click();

        Assert.ContainsSingle(container.FindAll(".bit-msb"));
        Assert.IsFalse(showing.IsCompleted);

        container.Find(".bit-msb-ftr .bit-btn").Click();

        Assert.AreEqual(BitMessageBoxResult.Ok, await showing);
        Assert.AreSequenceEqual(new[] { BitMessageBoxResult.None, BitMessageBoxResult.Ok }, seen);
    }

    [TestMethod]
    public async Task BitMessageBoxServiceShouldShowAQuestionAsAnAlertDialog()
    {
        var container = RenderComponent<BitModalContainer>();

        var confirming = MessageBoxService.Confirm("Publish", "Publish these changes?");

        container.WaitForAssertion(() => Assert.ContainsSingle(container.FindAll("[role='alertdialog']")));

        container.Find(".bit-msb-ftr .bit-btn").Click();

        Assert.IsTrue(await confirming);

        var asking = MessageBoxService.Show("Save", "Save the changes?", BitMessageBoxButtons.YesNo);

        container.WaitForAssertion(() => Assert.ContainsSingle(container.FindAll("[role='alertdialog']")));

        container.Find(".bit-msb-ftr .bit-btn").Click();

        Assert.AreEqual(BitMessageBoxResult.Yes, await asking);
    }

    [TestMethod]
    public async Task BitMessageBoxServiceShouldLeaveAQuestionAPlainDialogWhereTheCallerSaysSo()
    {
        var container = RenderComponent<BitModalContainer>();

        var confirming = MessageBoxService.Confirm(new BitMessageBoxParameters
        {
            Title = "Publish",
            Modal = new BitModalParameters { IsAlert = false }
        }, TestContext.CancellationToken);

        container.WaitForAssertion(() => Assert.ContainsSingle(container.FindAll("[role='dialog']")));

        Assert.IsEmpty(container.FindAll("[role='alertdialog']"));

        container.Find(".bit-msb-ftr .bit-btn").Click();

        Assert.IsTrue(await confirming);
    }

    [TestMethod]
    public async Task BitMessageBoxServiceShouldCloseTheMessageBoxWhenTheShowingIsCancelled()
    {
        var container = RenderComponent<BitModalContainer>();

        using var cts = new System.Threading.CancellationTokenSource();

        var showing = MessageBoxService.Show(new BitMessageBoxParameters { Title = "Title", Body = "Body" }, cts.Token);

        container.WaitForAssertion(() => Assert.ContainsSingle(container.FindAll(".bit-msb")));

        cts.Cancel();

        Assert.AreEqual(BitMessageBoxResult.None, await showing);

        container.WaitForAssertion(() => Assert.IsEmpty(container.FindAll(".bit-msb")));
    }

    [TestMethod]
    public async Task BitMessageBoxServiceShouldAnswerACancelledConfirmationWithARefusal()
    {
        var container = RenderComponent<BitModalContainer>();

        using var cts = new System.Threading.CancellationTokenSource();

        var confirming = MessageBoxService.Confirm(new BitMessageBoxParameters
        {
            Title = "Title",
            // A guard is the user's say over a dismissal; the page taking its question back is not one.
            Modal = new BitModalParameters { CanClose = () => Task.FromResult(false) }
        }, cts.Token);

        container.WaitForAssertion(() => Assert.ContainsSingle(container.FindAll(".bit-msb")));

        cts.Cancel();

        Assert.IsFalse(await confirming);

        container.WaitForAssertion(() => Assert.IsEmpty(container.FindAll(".bit-msb")));
    }

    [TestMethod]
    public async Task BitMessageBoxServiceShouldNotShowAShowingCancelledBeforehand()
    {
        var container = RenderComponent<BitModalContainer>();

        var result = await MessageBoxService.Show(new BitMessageBoxParameters { Title = "Title" }, new System.Threading.CancellationToken(canceled: true));

        Assert.AreEqual(BitMessageBoxResult.None, result);
        Assert.IsEmpty(container.FindAll(".bit-msb"));
    }

    [TestMethod]
    public async Task BitMessageBoxServiceShouldPassTheRestOfTheParametersOnToTheMessageBox()
    {
        var container = RenderComponent<BitModalContainer>();

        var showing = MessageBoxService.Show(new BitMessageBoxParameters
        {
            Title = "Title",
            Body = "Body",
            Buttons = BitMessageBoxButtons.YesNoCancel,
            Reversed = true,
            TitleElement = "h2",
            Color = BitColor.Warning,
            IconAriaLabel = "Warning",
            IconName = "Lightbulb",
            ButtonColor = BitColor.Secondary,
            CloseIconName = "Cancel",
            DefaultButton = BitMessageBoxResult.Cancel,
            OkText = "Go",
            CancelText = "Later",
            ShowCloseButton = false,
            Classes = new() { Root = "custom-root" },
            Styles = new() { Root = "margin:1px" },
        }, TestContext.CancellationToken);

        container.WaitForAssertion(() => Assert.ContainsSingle(container.FindAll(".bit-msb")));

        var root = container.Find(".bit-msb");

        Assert.IsTrue(root.ClassList.Contains("custom-root"));
        Assert.Contains("margin:1px", root.GetAttribute("style")!);
        Assert.AreEqual("H2", container.Find(".bit-msb-ttl").TagName);
        Assert.AreEqual("Warning", container.Find(".bit-msb-ict").GetAttribute("aria-label"));
        Assert.IsTrue(container.Find(".bit-msb-ico").ClassList.Contains("bit-icon--Lightbulb"));
        Assert.IsEmpty(container.FindAll(".bit-msb-hdr .bit-btn"));

        var buttons = container.FindAll(".bit-msb-ftr .bit-btn");
        var texts = buttons.Select(b => b.TextContent.Trim()).ToArray();

        Assert.AreSequenceEqual(new[] { "Later", "No", "Yes" }, texts);
        Assert.IsTrue(buttons.All(b => b.ClassList.Contains("bit-btn-sec")));
        // The focus lands on the button the showing named rather than on the affirmative one.
        Assert.IsTrue(buttons[0].HasAttribute("autofocus"));

        buttons[0].Click();

        Assert.AreEqual(BitMessageBoxResult.Cancel, await showing);
    }

    [TestMethod]
    public async Task BitMessageBoxServiceShouldPassAHiddenIconAndAFooterTemplateOn()
    {
        var container = RenderComponent<BitModalContainer>();

        var showing = MessageBoxService.Show(new BitMessageBoxParameters
        {
            Title = "Title",
            Color = BitColor.Error,
            HideIcon = true,
            FooterTemplate = builder => builder.AddMarkupContent(0, "<div class=\"custom-footer\"></div>"),
        }, TestContext.CancellationToken);

        container.WaitForAssertion(() => Assert.ContainsSingle(container.FindAll(".custom-footer")));

        Assert.IsEmpty(container.FindAll(".bit-msb-ict"));
        Assert.IsEmpty(container.FindAll(".bit-msb-ftr .bit-btn"));

        // The close button is the only way left to end it, and it is wired to close the modal too.
        container.Find(".bit-msb-hdr .bit-btn").Click();

        Assert.AreEqual(BitMessageBoxResult.None, await showing);
        container.WaitForAssertion(() => Assert.IsEmpty(container.FindAll(".bit-msb")));
    }

    [TestMethod]
    public async Task BitMessageBoxServiceShouldFocusTheMessageBoxItShowsUnlessToldNotTo()
    {
        var container = RenderComponent<BitModalContainer>();

        var showing = MessageBoxService.Show("Title", "Body", BitMessageBoxButtons.OkCancel);

        container.WaitForAssertion(() => Assert.ContainsSingle(container.FindAll(".bit-msb")));

        // A dialog that interrupts puts the focus inside itself: on the affirmative button by default.
        Assert.IsTrue(container.FindAll(".bit-msb-ftr .bit-btn")[0].HasAttribute("autofocus"));

        container.FindAll(".bit-msb-ftr .bit-btn")[0].Click();
        await showing;

        showing = MessageBoxService.Show(new BitMessageBoxParameters { Title = "Title", Body = "Body", AutoFocus = false }, TestContext.CancellationToken);

        container.WaitForAssertion(() => Assert.ContainsSingle(container.FindAll(".bit-msb")));

        Assert.IsEmpty(container.FindAll(".bit-msb .bit-btn[autofocus]"));

        container.Find(".bit-msb-ftr .bit-btn").Click();
        await showing;
    }

    [TestMethod,
        DataRow(BitMessageBoxButtons.Ok, BitColor.Success, false),
        DataRow(BitMessageBoxButtons.None, BitColor.Info, false),
        DataRow(BitMessageBoxButtons.OkCancel, BitColor.Info, true),
        DataRow(BitMessageBoxButtons.None, BitColor.Warning, true),
        DataRow(BitMessageBoxButtons.Ok, BitColor.SevereWarning, true),
        DataRow(BitMessageBoxButtons.Ok, BitColor.Error, true)]
    public async Task BitMessageBoxServiceShouldAnnounceAQuestionOrAnUrgentColorAsAnAlert(BitMessageBoxButtons buttons, BitColor color, bool isAlert)
    {
        var container = RenderComponent<BitModalContainer>();

        var showing = MessageBoxService.Show(new BitMessageBoxParameters { Title = "Title", Body = "Body", Buttons = buttons, Color = color }, TestContext.CancellationToken);

        container.WaitForAssertion(() => Assert.ContainsSingle(container.FindAll(".bit-msb")));

        Assert.HasCount(isAlert ? 1 : 0, container.FindAll("[role='alertdialog']"));
        Assert.HasCount(isAlert ? 0 : 1, container.FindAll("[role='dialog']"));

        container.Find(".bit-msb .bit-btn").Click();

        await showing;
    }

    [TestMethod]
    public async Task BitMessageBoxServiceShouldShowAPersistentMessageBoxOnceAContainerMounts()
    {
        // No container yet: a persistent showing waits for one rather than answering with nothing.
        var showing = MessageBoxService.Show(new BitMessageBoxParameters { Title = "Title", Body = "Body", Persistent = true }, TestContext.CancellationToken);

        Assert.IsFalse(showing.IsCompleted);

        var container = RenderComponent<BitModalContainer>();

        container.WaitForAssertion(() => Assert.ContainsSingle(container.FindAll(".bit-msb")));

        container.Find(".bit-msb-ftr .bit-btn").Click();

        Assert.AreEqual(BitMessageBoxResult.Ok, await showing);
    }

    [TestMethod]
    public async Task BitMessageBoxServiceShouldDescribeTheDialogByNothingWithoutABody()
    {
        var container = RenderComponent<BitModalContainer>();

        var showing = MessageBoxService.Show(new BitMessageBoxParameters { Title = "Only a title." }, TestContext.CancellationToken);

        container.WaitForAssertion(() => Assert.ContainsSingle(container.FindAll(".bit-msb")));

        var dialog = container.Find("[role='dialog']");

        Assert.AreEqual(container.Find(".bit-msb-ttl").Id, dialog.GetAttribute("aria-labelledby"));
        Assert.IsNull(dialog.GetAttribute("aria-describedby"));

        container.Find(".bit-msb-ftr .bit-btn").Click();

        await showing;
    }

    public global::Microsoft.VisualStudio.TestTools.UnitTesting.TestContext TestContext { get; set; }
}
