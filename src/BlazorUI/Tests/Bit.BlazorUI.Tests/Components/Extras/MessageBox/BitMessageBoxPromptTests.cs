using System;
using System.Collections.Generic;
using System.Threading;
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
            Validator = v => v.Contains('/') ? "A name cannot hold a slash." : null
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
            Validator = v => v.Contains('/') ? "A name cannot hold a slash." : null
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

#if NET9_0_OR_GREATER
        // A key that composes a character with an IME is not an answer. Only net9.0 on carries the composition state
        // on the event; on net8.0 the field's own composition guard stops that keydown before Blazor sees it.
        container.Find(".bit-msb .bit-tfl-inp").KeyDown(new KeyboardEventArgs { Key = "Enter", IsComposing = true });
        Assert.IsFalse(prompting.IsCompleted);
#endif

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
    public async Task PromptShouldPassTheNewFieldOptionsDown()
    {
        var container = RenderComponent<BitModalContainer>();

        var prompting = MessageBoxService.Prompt(new BitMessageBoxPromptParameters
        {
            Title = "Delete the account?",
            Body = "Enter your password to confirm.",
            Label = "Password",
            Description = "The one you sign in with.",
            InputType = BitInputType.Password,
            AutoComplete = "current-password",
            CanRevealPassword = true
        });

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-msb .bit-tfl-inp").Count));

        var input = container.Find(".bit-msb .bit-tfl-inp");
        Assert.AreEqual("current-password", input.GetAttribute("autocomplete"));
        Assert.AreEqual("done", input.GetAttribute("enterkeyhint"));
        Assert.IsTrue(container.Find(".bit-msb .bit-tfl-des").TextContent.Contains("The one you sign in with."));
        Assert.AreEqual(1, container.FindAll(".bit-msb .bit-tfl-rpb").Count);

        container.FindAll(".bit-msb-ftr .bit-btn")[1].Click();

        await prompting;
    }

    [TestMethod]
    public async Task PromptShouldLeaveTheReturnKeyOfAMultilineFieldAlone()
    {
        var container = RenderComponent<BitModalContainer>();

        var prompting = MessageBoxService.Prompt(new BitMessageBoxPromptParameters { Title = "Feedback", Body = "Anything?", Multiline = true });

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-msb textarea.bit-tfl-inp").Count));

        Assert.IsNull(container.Find(".bit-msb .bit-tfl-inp").GetAttribute("enterkeyhint"));

        container.FindAll(".bit-msb-ftr .bit-btn")[1].Click();

        await prompting;
    }

    [TestMethod]
    public async Task PromptShouldRefuseAValueTheAsyncValidatorRefusesAndClearItsMessageOnEdit()
    {
        var container = RenderComponent<BitModalContainer>();
        var check = new TaskCompletionSource<string?>();
        var checkedValues = new List<string?>();

        var prompting = MessageBoxService.Prompt(new BitMessageBoxPromptParameters
        {
            Title = "New folder",
            Body = "The name of the folder:",
            Value = "Projects",
            AsyncValidator = (v, _) => { checkedValues.Add(v); return check.Task; }
        });

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-msb .bit-tfl-inp").Count));

        container.FindAll(".bit-msb-ftr .bit-btn")[0].Click();

        // The field says it is busy while the check runs.
        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-msb .bit-tfl-lod").Count));
        Assert.IsFalse(prompting.IsCompleted);

        check.SetResult("A folder with this name already exists.");

        container.WaitForAssertion(() =>
        {
            Assert.AreEqual(0, container.FindAll(".bit-msb .bit-tfl-lod").Count);
            Assert.IsTrue(container.Find(".bit-msb .bit-tfl-erm").TextContent.Contains("already exists"));
        });
        Assert.IsFalse(prompting.IsCompleted);

        // The message is about a value that is no longer there once the field is edited.
        container.Find(".bit-msb .bit-tfl-inp").Input("Projects 2");
        container.WaitForAssertion(() => Assert.AreEqual(0, container.FindAll(".bit-msb .bit-tfl-erm").Count));

        check = new TaskCompletionSource<string?>();
        check.SetResult(null);

        container.FindAll(".bit-msb-ftr .bit-btn")[0].Click();

        Assert.AreEqual("Projects 2", await prompting);
        CollectionAssert.AreEqual(new[] { "Projects", "Projects 2" }, checkedValues);
    }

    [TestMethod]
    public async Task PromptShouldNotRunTheAsyncValidatorOnAValueTheSyncChecksRefuse()
    {
        var container = RenderComponent<BitModalContainer>();
        var asyncRuns = 0;

        var prompting = MessageBoxService.Prompt(new BitMessageBoxPromptParameters
        {
            Title = "New folder",
            Body = "The name of the folder:",
            Required = true,
            AsyncValidator = (_, _) => { asyncRuns++; return Task.FromResult<string?>(null); }
        });

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-msb .bit-tfl-inp").Count));

        container.FindAll(".bit-msb-ftr .bit-btn")[0].Click();
        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-msb .bit-tfl-erm").Count));

        Assert.AreEqual(0, asyncRuns);

        // Dismissing is never checked.
        container.FindAll(".bit-msb-ftr .bit-btn")[1].Click();

        Assert.IsNull(await prompting);
        Assert.AreEqual(0, asyncRuns);
    }

    [TestMethod]
    public async Task PromptShouldNotShowAnAsyncRefusalOfAValueEditedWhileItWasChecked()
    {
        var container = RenderComponent<BitModalContainer>();
        var check = new TaskCompletionSource<string?>();

        var prompting = MessageBoxService.Prompt(new BitMessageBoxPromptParameters
        {
            Title = "New folder",
            Body = "The name of the folder:",
            Value = "Projects",
            AsyncValidator = (_, _) => check.Task
        });

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-msb .bit-tfl-inp").Count));

        container.FindAll(".bit-msb-ftr .bit-btn")[0].Click();
        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-msb .bit-tfl-lod").Count));

        container.Find(".bit-msb .bit-tfl-inp").Input("Projects 2");

        check.SetResult("A folder with this name already exists.");

        // Refused, but the message about "Projects" is not shown under "Projects 2".
        container.WaitForAssertion(() => Assert.AreEqual(0, container.FindAll(".bit-msb .bit-tfl-lod").Count));
        Assert.AreEqual(0, container.FindAll(".bit-msb .bit-tfl-erm").Count);
        Assert.IsFalse(prompting.IsCompleted);

        container.FindAll(".bit-msb-ftr .bit-btn")[1].Click();

        Assert.IsNull(await prompting);
    }

    [TestMethod]
    public async Task PromptShouldReturnTheValueTheAsyncValidatorAccepted()
    {
        var container = RenderComponent<BitModalContainer>();
        var check = new TaskCompletionSource<string?>();

        var prompting = MessageBoxService.Prompt(new BitMessageBoxPromptParameters
        {
            Title = "New folder",
            Body = "The name of the folder:",
            Value = "Reports",
            AsyncValidator = (_, _) => check.Task
        });

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-msb .bit-tfl-inp").Count));

        container.FindAll(".bit-msb-ftr .bit-btn")[0].Click();
        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-msb .bit-tfl-lod").Count));

        // An edit made while the check runs is not what the check said yes to.
        container.Find(".bit-msb .bit-tfl-inp").Input("Reports/2026");

        check.SetResult(null);

        Assert.AreEqual("Reports", await prompting);
    }

    [TestMethod]
    public async Task PromptShouldReturnNullWhenTheTokenIsCancelled()
    {
        var container = RenderComponent<BitModalContainer>();
        using var cts = new CancellationTokenSource();

        var prompting = MessageBoxService.Prompt(new BitMessageBoxPromptParameters { Title = "Rename", Body = "The new name:", Value = "a" }, cts.Token);

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-msb").Count));

        await cts.CancelAsync();

        Assert.IsNull(await prompting);
        container.WaitForAssertion(() => Assert.AreEqual(0, container.FindAll(".bit-msb").Count));
    }

    [TestMethod]
    public async Task PromptShouldCloseAndRethrowWhatTheAsyncValidatorThrew()
    {
        var container = RenderComponent<BitModalContainer>();

        var prompting = MessageBoxService.Prompt(new BitMessageBoxPromptParameters
        {
            Title = "New folder",
            Body = "The name of the folder:",
            Value = "Reports",
            AsyncValidator = (_, _) => Task.FromException<string?>(new InvalidOperationException("The server is down."))
        });

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-msb .bit-tfl-inp").Count));

        // The click that ran the check is not where the failure goes: it reaches the code waiting on the prompt.
        await container.FindAll(".bit-msb-ftr .bit-btn")[0].ClickAsync(new MouseEventArgs());

        var error = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => prompting);
        Assert.AreEqual("The server is down.", error.Message);

        container.WaitForAssertion(() => Assert.AreEqual(0, container.FindAll(".bit-msb").Count));
    }

    [TestMethod]
    public async Task PromptShouldCloseAndRethrowWhatTheValidatorThrew()
    {
        var container = RenderComponent<BitModalContainer>();

        var prompting = MessageBoxService.Prompt(new BitMessageBoxPromptParameters
        {
            Title = "Rename",
            Body = "The new name of the file:",
            Validator = _ => throw new InvalidOperationException("Broken check.")
        });

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-msb .bit-tfl-inp").Count));

        await container.Find(".bit-msb .bit-tfl-inp").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => prompting);

        container.WaitForAssertion(() => Assert.AreEqual(0, container.FindAll(".bit-msb").Count));
    }

    [TestMethod]
    public async Task PromptShouldBeCancellableWhileTheAsyncValidatorRuns()
    {
        var container = RenderComponent<BitModalContainer>();
        var check = new TaskCompletionSource<string?>();
        var token = CancellationToken.None;

        var prompting = MessageBoxService.Prompt(new BitMessageBoxPromptParameters
        {
            Title = "New folder",
            Body = "The name of the folder:",
            Value = "Reports",
            AsyncValidator = (_, ct) => { token = ct; return check.Task; }
        });

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-msb .bit-tfl-inp").Count));

        container.FindAll(".bit-msb-ftr .bit-btn")[0].Click();
        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-msb .bit-tfl-lod").Count));

        // The check never answers, and Cancel is not kept waiting on it: it ends the prompt and gives the check up.
        container.FindAll(".bit-msb-ftr .bit-btn")[1].Click();

        Assert.IsNull(await prompting);
        Assert.IsTrue(token.IsCancellationRequested);
    }

    [TestMethod]
    public async Task PromptShouldGiveTheAsyncValidatorUpWhenTheShowingIsCancelled()
    {
        var container = RenderComponent<BitModalContainer>();
        using var cts = new CancellationTokenSource();
        var token = CancellationToken.None;

        var prompting = MessageBoxService.Prompt(new BitMessageBoxPromptParameters
        {
            Title = "New folder",
            Body = "The name of the folder:",
            Value = "Reports",
            AsyncValidator = (_, ct) => { token = ct; return new TaskCompletionSource<string?>().Task; }
        }, cts.Token);

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-msb .bit-tfl-inp").Count));

        container.FindAll(".bit-msb-ftr .bit-btn")[0].Click();
        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-msb .bit-tfl-lod").Count));

        await cts.CancelAsync();

        Assert.IsNull(await prompting);
        Assert.IsTrue(token.IsCancellationRequested);
    }

    [TestMethod]
    public async Task PromptShouldCheckAnEmptyFieldAsAnEmptyString()
    {
        var container = RenderComponent<BitModalContainer>();
        var seen = new List<string?>();

        var prompting = MessageBoxService.Prompt(new BitMessageBoxPromptParameters
        {
            Title = "Note",
            Body = "Anything to add?",
            Validator = v => { seen.Add(v); return null; },
            AsyncValidator = (v, _) => { seen.Add(v); return Task.FromResult<string?>(null); }
        });

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-msb .bit-tfl-inp").Count));

        container.FindAll(".bit-msb-ftr .bit-btn")[0].Click();

        // What is checked is what is handed back.
        Assert.AreEqual(string.Empty, await prompting);
        CollectionAssert.AreEqual(new[] { string.Empty, string.Empty }, seen);
    }

    [TestMethod]
    public async Task PromptShouldStayAPlainDialogWhateverItsColor()
    {
        var container = RenderComponent<BitModalContainer>();

        var prompting = MessageBoxService.Prompt(new BitMessageBoxPromptParameters
        {
            Title = "Delete the account?",
            Body = "Enter your password to confirm.",
            Color = BitColor.Error
        });

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-msb .bit-tfl-inp").Count));

        Assert.AreEqual(1, container.FindAll("[role='dialog']").Count);
        Assert.AreEqual(0, container.FindAll("[role='alertdialog']").Count);

        container.FindAll(".bit-msb-ftr .bit-btn")[1].Click();

        await prompting;
    }

    [TestMethod]
    public async Task PromptShouldSizeTheFieldLikeTheBoxACascadeSized()
    {
        var host = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitMessageBoxParams { Size = BitSize.Small } });
            parameters.AddChildContent<BitModalContainer>();
        });

        var prompting = MessageBoxService.Prompt("Rename", "The new name of the file:");

        host.WaitForAssertion(() => Assert.AreEqual(1, host.FindAll(".bit-msb .bit-tfl-inp").Count));

        Assert.IsTrue(host.Find(".bit-msb").ClassList.Contains("bit-msb-sm"));
        Assert.IsTrue(host.Find(".bit-msb .bit-tfl").ClassList.Contains("bit-tfl-sm"));

        host.FindAll(".bit-msb-ftr .bit-btn")[1].Click();

        await prompting;
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
