using System.Linq;
using System.Threading.Tasks;
using Bunit;
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
            Assert.AreEqual(1, container.FindAll(".bit-mdl .bit-msb").Count);
            Assert.IsTrue(container.Markup.Contains("The title"));
            Assert.IsTrue(container.Markup.Contains("The body"));
        });

        container.Find(".bit-msb-ftr .bit-btn").Click();

        Assert.AreEqual(BitMessageBoxResult.Ok, await showing);
    }

    [TestMethod]
    public async Task BitMessageBoxServiceShouldNameAndDescribeTheDialogWithTheTitleAndTheBody()
    {
        var container = RenderComponent<BitModalContainer>();

        var showing = MessageBoxService.Show("The title", "The body");

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-msb").Count));

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

        var showing = MessageBoxService.Show(new BitMessageBoxParameters { Body = "Only a body." });

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-msb").Count));

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
        });

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".custom-header").Count));

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
        });

        container.WaitForAssertion(() => Assert.AreEqual(2, container.FindAll(".bit-msb-ftr .bit-btn").Count));

        var buttons = container.FindAll(".bit-msb-ftr .bit-btn");
        Assert.IsTrue(buttons[0].ClassList.Contains("bit-btn-err"));
        Assert.IsTrue(buttons[1].ClassList.Contains("bit-btn-ter"));

        container.FindAll(".bit-msb-ftr .bit-btn")[1].Click();

        Assert.IsFalse(await confirming);
    }

    [TestMethod]
    public async Task BitMessageBoxServiceShouldAnnounceAnUrgentMessageBoxAsAnAlert()
    {
        var container = RenderComponent<BitModalContainer>();

        var showing = MessageBoxService.ShowError("Error", "It went wrong.");

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll("[role='alertdialog']").Count));

        container.Find(".bit-msb-ftr .bit-btn").Click();

        await showing;
    }

    [TestMethod]
    public async Task BitMessageBoxServiceShouldLeaveANonUrgentMessageBoxAsAPlainDialog()
    {
        var container = RenderComponent<BitModalContainer>();

        var showing = MessageBoxService.ShowInfo("Info", "Worth knowing.");

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll("[role='dialog']").Count));

        Assert.AreEqual(0, container.FindAll("[role='alertdialog']").Count);
        Assert.AreEqual(1, container.FindAll(".bit-msb-ico.bit-icon--Info").Count);

        container.Find(".bit-msb-ftr .bit-btn").Click();

        await showing;
    }

    [TestMethod]
    public async Task BitMessageBoxServiceShouldGiveEverySeverityHelperItsOwnGlyph()
    {
        var container = RenderComponent<BitModalContainer>();

        var showing = MessageBoxService.ShowSuccess("Success", "It worked.");

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-msb-ico.bit-icon--Completed").Count));

        container.Find(".bit-msb-ftr .bit-btn").Click();
        await showing;

        showing = MessageBoxService.ShowWarning("Warning", "Careful.");

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-msb-ico.bit-icon--Warning").Count));

        container.Find(".bit-msb-ftr .bit-btn").Click();
        await showing;
    }

    [TestMethod]
    public async Task BitMessageBoxServiceShouldReportTheButtonThatEndedTheShowing()
    {
        var container = RenderComponent<BitModalContainer>();

        var showing = MessageBoxService.Show("Title", "Body", BitMessageBoxButtons.YesNoCancel);

        container.WaitForAssertion(() => Assert.AreEqual(3, container.FindAll(".bit-msb-ftr .bit-btn").Count));

        container.FindAll(".bit-msb-ftr .bit-btn")[1].Click();

        Assert.AreEqual(BitMessageBoxResult.No, await showing);
    }

    [TestMethod]
    public async Task BitMessageBoxServiceShouldReportNoAnswerFromTheCloseButton()
    {
        var container = RenderComponent<BitModalContainer>();

        var showing = MessageBoxService.Show("Title", "Body", BitMessageBoxButtons.OkCancel);

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-msb").Count));

        container.Find(".bit-msb-hdr .bit-btn").Click();

        Assert.AreEqual(BitMessageBoxResult.None, await showing);
    }

    [TestMethod]
    public async Task BitMessageBoxServiceShouldCloseTheModalOnceTheMessageBoxIsAnswered()
    {
        var container = RenderComponent<BitModalContainer>();

        var showing = MessageBoxService.Show("Title", "Body");

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-msb").Count));

        container.Find(".bit-msb-ftr .bit-btn").Click();

        await showing;

        container.WaitForAssertion(() => Assert.AreEqual(0, container.FindAll(".bit-msb").Count));
        Assert.AreEqual(0, ModalService.OpenModals.Count);
    }

    [TestMethod]
    public async Task BitMessageBoxServiceShouldConfirmOnlyOnAnAffirmativeAnswer()
    {
        var container = RenderComponent<BitModalContainer>();

        var confirming = MessageBoxService.Confirm("Title", "Body");

        container.WaitForAssertion(() => Assert.AreEqual(2, container.FindAll(".bit-msb-ftr .bit-btn").Count));

        container.FindAll(".bit-msb-ftr .bit-btn")[0].Click();

        Assert.IsTrue(await confirming);
    }

    [TestMethod]
    public async Task BitMessageBoxServiceShouldNotConfirmOnARefusal()
    {
        var container = RenderComponent<BitModalContainer>();

        var confirming = MessageBoxService.Confirm("Title", "Body");

        container.WaitForAssertion(() => Assert.AreEqual(2, container.FindAll(".bit-msb-ftr .bit-btn").Count));

        container.FindAll(".bit-msb-ftr .bit-btn")[1].Click();

        Assert.IsFalse(await confirming);
    }

    [TestMethod]
    public async Task BitMessageBoxServiceShouldNotConfirmOnADismissal()
    {
        var container = RenderComponent<BitModalContainer>();

        var confirming = MessageBoxService.Confirm("Title", "Body");

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-msb").Count));

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
            CollectionAssert.AreEqual(new[] { "Yes", "No" }, texts);
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
        });

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-msb").Count));

        var root = container.Find(".bit-msb");

        Assert.AreEqual("the-box", root.Id);
        Assert.IsTrue(root.ClassList.Contains("bit-msb-inf"));
        Assert.IsTrue(root.ClassList.Contains("bit-msb-lg"));
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.AreEqual("Dismiss", container.Find(".bit-msb-hdr .bit-btn").GetAttribute("title"));

        var texts = container.FindAll(".bit-msb-ftr .bit-btn-prt").Select(b => b.TextContent).ToArray();
        CollectionAssert.AreEqual(new[] { "Sure", "Nope" }, texts);

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
        });

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll("[role='alertdialog']").Count));

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
}
