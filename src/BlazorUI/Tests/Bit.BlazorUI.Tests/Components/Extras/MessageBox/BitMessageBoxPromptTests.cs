using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.MessageBox;

/// <summary>
/// The prompt of the message box service: the field it asks in, what it hands back for each way it ends, how it checks the
/// value, and how the field is named - plus the cascaded message box a custom footer answers through.
/// </summary>
[TestClass]
public class BitMessageBoxPromptTests : BunitTestContext
{
    private BitMessageBoxService MessageBoxService => Services.GetRequiredService<BitMessageBoxService>();

    [TestInitialize]
    public void SetupServices()
    {
        Services.AddSingleton<BitModalService>();
        Services.AddSingleton<BitMessageBoxService>();
    }



    [TestMethod]
    public async Task PromptShouldReturnTheTypedValueWhenAnsweredOk()
    {
        var container = RenderComponent<BitModalContainer>();

        var prompting = MessageBoxService.Prompt("Rename", "The new name of the file:", "report.txt");

        container.WaitForAssertion(() => Assert.AreEqual("report.txt", container.Find(".bit-msb .bit-tfl-inp").GetAttribute("value")));

        container.Find(".bit-msb .bit-tfl-inp").Input("summary.txt");
        container.FindAll(".bit-msb-ftr .bit-btn")[0].Click();

        Assert.AreEqual("summary.txt", await prompting);
    }

    [TestMethod]
    public async Task PromptShouldReturnNullWhenCancelled()
    {
        var container = RenderComponent<BitModalContainer>();

        var prompting = MessageBoxService.Prompt("Rename", "The new name of the file:", "report.txt");

        container.WaitForAssertion(() => Assert.AreEqual(2, container.FindAll(".bit-msb-ftr .bit-btn").Count));

        container.FindAll(".bit-msb-ftr .bit-btn")[1].Click();

        Assert.IsNull(await prompting);
    }

    [TestMethod]
    public async Task PromptShouldReturnAnEmptyStringForAnEmptyFieldAnsweredOk()
    {
        var container = RenderComponent<BitModalContainer>();

        var prompting = MessageBoxService.Prompt("Note", "Anything to add?");

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-msb .bit-tfl-inp").Count));

        container.FindAll(".bit-msb-ftr .bit-btn")[0].Click();

        Assert.AreEqual(string.Empty, await prompting);
    }

    [TestMethod]
    public async Task PromptShouldFocusTheFieldRatherThanAButton()
    {
        var container = RenderComponent<BitModalContainer>();

        var prompting = MessageBoxService.Prompt("Rename", "The new name of the file:");

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-msb .bit-tfl-inp").Count));

        Assert.IsTrue(container.Find(".bit-msb .bit-tfl-inp").HasAttribute("autofocus"));
        Assert.AreEqual(0, container.FindAll(".bit-msb-ftr .bit-btn[autofocus]").Count);

        container.FindAll(".bit-msb-ftr .bit-btn")[1].Click();

        await prompting;
    }

    [TestMethod]
    public async Task PromptShouldRefuseAnEmptyRequiredValueAndKeepTheBoxOpen()
    {
        var container = RenderComponent<BitModalContainer>();

        var prompting = MessageBoxService.Prompt(new BitMessageBoxPromptParameters
        {
            Title = "Rename",
            Body = "The new name of the file:",
            Required = true,
            RequiredMessage = "Enter a name."
        });

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-msb .bit-tfl-inp").Count));

        Assert.IsTrue(container.Find(".bit-msb .bit-tfl-inp").HasAttribute("required"));

        container.FindAll(".bit-msb-ftr .bit-btn")[0].Click();

        container.WaitForAssertion(() =>
        {
            Assert.IsTrue(container.Find(".bit-msb .bit-tfl-erm").TextContent.Contains("Enter a name."));
            Assert.AreEqual("true", container.Find(".bit-msb .bit-tfl-inp").GetAttribute("aria-invalid"));
        });

        Assert.IsFalse(prompting.IsCompleted);

        // Fixing the value takes the message away at once, before the box is answered again.
        container.Find(".bit-msb .bit-tfl-inp").Input("notes.txt");

        container.WaitForAssertion(() => Assert.AreEqual(0, container.FindAll(".bit-msb .bit-tfl-erm").Count));

        container.FindAll(".bit-msb-ftr .bit-btn")[0].Click();

        Assert.AreEqual("notes.txt", await prompting);
    }

    [TestMethod]
    public async Task PromptShouldShowTheMessageOfTheValidator()
    {
        var container = RenderComponent<BitModalContainer>();

        var prompting = MessageBoxService.Prompt(new BitMessageBoxPromptParameters
        {
            Title = "Rename",
            Body = "The new name of the file:",
            Value = "a/b",
            Validator = v => v?.Contains('/') is true ? "A name cannot hold a slash." : null
        });

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-msb .bit-tfl-inp").Count));

        container.FindAll(".bit-msb-ftr .bit-btn")[0].Click();

        container.WaitForAssertion(() => Assert.IsTrue(container.Find(".bit-msb .bit-tfl-erm").TextContent.Contains("A name cannot hold a slash.")));

        Assert.IsFalse(prompting.IsCompleted);

        // A dismissal is never refused.
        container.FindAll(".bit-msb-ftr .bit-btn")[1].Click();

        Assert.IsNull(await prompting);
    }

    [TestMethod]
    public async Task PromptShouldKeepCheckingEveryEditAfterTheFirstRefusal()
    {
        var container = RenderComponent<BitModalContainer>();

        var prompting = MessageBoxService.Prompt(new BitMessageBoxPromptParameters
        {
            Title = "Rename",
            Body = "The new name of the file:",
            Value = "a/b",
            Validator = v => v?.Contains('/') is true ? "A name cannot hold a slash." : null
        });

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-msb .bit-tfl-inp").Count));

        // Nothing is said while the field is being typed into for the first time.
        container.Find(".bit-msb .bit-tfl-inp").Input("a/bc");
        Assert.AreEqual(0, container.FindAll(".bit-msb .bit-tfl-erm").Count);

        container.FindAll(".bit-msb-ftr .bit-btn")[0].Click();
        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-msb .bit-tfl-erm").Count));

        container.Find(".bit-msb .bit-tfl-inp").Input("abc");
        container.WaitForAssertion(() => Assert.AreEqual(0, container.FindAll(".bit-msb .bit-tfl-erm").Count));

        // A value that turns invalid again is flagged again, without another press of the button.
        container.Find(".bit-msb .bit-tfl-inp").Input("abc/");
        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-msb .bit-tfl-erm").Count));

        container.FindAll(".bit-msb-ftr .bit-btn")[1].Click();

        Assert.IsNull(await prompting);
    }

    [TestMethod]
    public async Task PromptShouldAskTheCallersGuardOnlyOnceTheValueIsAccepted()
    {
        var container = RenderComponent<BitModalContainer>();
        var asked = 0;

        var prompting = MessageBoxService.Prompt(new BitMessageBoxPromptParameters
        {
            Title = "Rename",
            Body = "The new name of the file:",
            Required = true,
            OnBeforeResult = EventCallback.Factory.Create<BitMessageBoxBeforeResultArgs>(this, _ => asked++)
        });

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-msb .bit-tfl-inp").Count));

        container.FindAll(".bit-msb-ftr .bit-btn")[0].Click();
        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-msb .bit-tfl-erm").Count));

        Assert.AreEqual(0, asked);

        container.Find(".bit-msb .bit-tfl-inp").Input("x");
        container.FindAll(".bit-msb-ftr .bit-btn")[0].Click();

        Assert.AreEqual("x", await prompting);
        Assert.AreEqual(1, asked);
    }

    [TestMethod]
    public async Task PromptShouldBeAnsweredByEnterInTheField()
    {
        var container = RenderComponent<BitModalContainer>();

        var prompting = MessageBoxService.Prompt("Rename", "The new name of the file:");

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-msb .bit-tfl-inp").Count));

        container.Find(".bit-msb .bit-tfl-inp").Input("final.txt");

        // A key that composes a character with an IME is not an answer.
        container.Find(".bit-msb .bit-tfl-inp").KeyDown(new KeyboardEventArgs { Key = "Enter", IsComposing = true });
        Assert.IsFalse(prompting.IsCompleted);

        container.Find(".bit-msb .bit-tfl-inp").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.AreEqual("final.txt", await prompting);
    }

    [TestMethod]
    public async Task PromptShouldKeepEnterForNewLinesInAMultilineField()
    {
        var container = RenderComponent<BitModalContainer>();

        var prompting = MessageBoxService.Prompt(new BitMessageBoxPromptParameters
        {
            Title = "Feedback",
            Body = "What could be better?",
            Multiline = true
        });

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-msb textarea.bit-tfl-inp").Count));

        container.Find(".bit-msb .bit-tfl-inp").Input("More examples");
        container.Find(".bit-msb .bit-tfl-inp").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.IsFalse(prompting.IsCompleted);

        container.Find(".bit-msb .bit-tfl-inp").KeyDown(new KeyboardEventArgs { Key = "Enter", CtrlKey = true });

        Assert.AreEqual("More examples", await prompting);
    }

    [TestMethod]
    public async Task PromptShouldAnswerYesForAYesNoSet()
    {
        var container = RenderComponent<BitModalContainer>();

        var prompting = MessageBoxService.Prompt(new BitMessageBoxPromptParameters
        {
            Title = "Rename",
            Body = "The new name of the file:",
            Buttons = BitMessageBoxButtons.YesNo,
            Value = "a"
        });

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-msb .bit-tfl-inp").Count));

        container.Find(".bit-msb .bit-tfl-inp").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.AreEqual("a", await prompting);
    }

    [TestMethod]
    public async Task PromptShouldNameTheFieldWithTheQuestionAndKeepItAPlainDialog()
    {
        var container = RenderComponent<BitModalContainer>();

        var prompting = MessageBoxService.Prompt("Rename", "The new name of the file:");

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-msb .bit-tfl-inp").Count));

        Assert.AreEqual("The new name of the file:", container.Find(".bit-msb .bit-tfl-inp").GetAttribute("aria-label"));

        // The question names the field, so the dialog is not also described by it.
        var dialog = container.Find("[role='dialog']");
        Assert.IsNull(dialog.GetAttribute("aria-describedby"));
        Assert.AreEqual(container.Find(".bit-msb-ttl").Id, dialog.GetAttribute("aria-labelledby"));

        container.FindAll(".bit-msb-ftr .bit-btn")[1].Click();

        await prompting;
    }

    [TestMethod]
    public async Task PromptWithALabelShouldDescribeTheDialogWithTheQuestion()
    {
        var container = RenderComponent<BitModalContainer>();

        var prompting = MessageBoxService.Prompt(new BitMessageBoxPromptParameters
        {
            Title = "Rename",
            Body = "Pick something short.",
            Label = "Name"
        });

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-msb .bit-tfl-lbl").Count));

        Assert.AreEqual("Name", container.Find(".bit-msb .bit-tfl-lbl").TextContent.Trim());
        Assert.AreEqual(container.Find(".bit-msb-pmg").Id, container.Find("[role='dialog']").GetAttribute("aria-describedby"));

        container.FindAll(".bit-msb-ftr .bit-btn")[1].Click();

        await prompting;
    }

    [TestMethod]
    public async Task PromptShouldPassTheFieldOptionsDown()
    {
        var container = RenderComponent<BitModalContainer>();

        var prompting = MessageBoxService.Prompt(new BitMessageBoxPromptParameters
        {
            Title = "Unlock",
            Body = "The password of the archive:",
            InputType = BitInputType.Password,
            Placeholder = "Password",
            MaxLength = 20
        });

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-msb .bit-tfl-inp").Count));

        var input = container.Find(".bit-msb .bit-tfl-inp");
        Assert.AreEqual("password", input.GetAttribute("type"));
        Assert.AreEqual("Password", input.GetAttribute("placeholder"));
        Assert.AreEqual("20", input.GetAttribute("maxlength"));

        container.FindAll(".bit-msb-ftr .bit-btn")[1].Click();

        await prompting;
    }

    [TestMethod]
    public async Task PromptShouldReturnNullWhenTheTokenIsCancelled()
    {
        var container = RenderComponent<BitModalContainer>();
        using var cts = new System.Threading.CancellationTokenSource();

        var prompting = MessageBoxService.Prompt(new BitMessageBoxPromptParameters { Title = "Rename", Body = "The new name:", Value = "a" }, cts.Token);

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-msb").Count));

        await cts.CancelAsync();

        Assert.IsNull(await prompting);
        container.WaitForAssertion(() => Assert.AreEqual(0, container.FindAll(".bit-msb").Count));
    }

    [TestMethod]
    public async Task ACustomFooterShouldAnswerAServiceMessageBoxThroughTheCascadedMessageBox()
    {
        var container = RenderComponent<BitModalContainer>();

        var showing = MessageBoxService.Show(new BitMessageBoxParameters
        {
            Title = "Delete",
            Body = "Delete this file?",
            FooterTemplate = builder =>
            {
                builder.OpenComponent<AnswerYesFooter>(0);
                builder.CloseComponent();
            }
        });

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".answer-yes").Count));

        container.Find(".answer-yes").Click();

        Assert.AreEqual(BitMessageBoxResult.Yes, await showing);
    }



    private sealed class AnswerYesFooter : ComponentBase
    {
        [CascadingParameter] public BitMessageBox? MessageBox { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "class", "answer-yes");
            builder.AddAttribute(2, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => MessageBox!.AnswerAsync(BitMessageBoxResult.Yes)));
            builder.AddContent(3, "Yes");
            builder.CloseElement();
        }
    }
}
