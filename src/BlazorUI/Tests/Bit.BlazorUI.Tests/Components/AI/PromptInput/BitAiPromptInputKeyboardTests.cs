using System.Collections.Generic;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.PromptInput;

[TestClass]
public class BitAiPromptInputKeyboardTests : BitAiTestBase
{
    private static KeyboardEventArgs Key(string key, bool shift = false, bool ctrl = false, bool meta = false) =>
        new() { Key = key, ShiftKey = shift, CtrlKey = ctrl, MetaKey = meta };

    [TestMethod]
    public void EnterDuringImeCompositionDoesNotSubmit()
    {
        var submissions = new List<BitAiPromptSubmission>();
        var composer = RenderComponent<BitAiPromptInput>(p => p
            .Add(c => c.Value, "こんにちは")
            .Add(c => c.OnSubmit, s => submissions.Add(s)));

        composer.Find("textarea").KeyDown(new KeyboardEventArgs { Key = "Enter", IsComposing = true });
        composer.Find("textarea").KeyDown(Key("Process"));
        Assert.IsEmpty(submissions);

        composer.Find("textarea").KeyDown(Key("Enter"));
        Assert.AreEqual("こんにちは", Assert.ContainsSingle(submissions).Text);
    }

    [TestMethod]
    public void ShiftEnterInsertsANewlineRatherThanSubmitting()
    {
        var submissions = new List<BitAiPromptSubmission>();
        var composer = RenderComponent<BitAiPromptInput>(p => p
            .Add(c => c.Value, "hello")
            .Add(c => c.OnSubmit, s => submissions.Add(s)));

        composer.Find("textarea").KeyDown(Key("Enter", shift: true));

        Assert.IsEmpty(submissions);
    }

    [TestMethod]
    public void EnterDuringCompositionDoesNotChooseAMenuItem()
    {
        var value = string.Empty;
        var composer = RenderComponent<BitAiPromptInput>(p => p
            .Add(c => c.Commands, [new BitAiSlashCommand("summarize") { Template = "Summarize: " }])
            .Add(c => c.ValueChanged, v => value = v));

        composer.Find("textarea").Input("/");
        composer.Find("textarea").KeyDown(new KeyboardEventArgs { Key = "Enter", IsComposing = true });

        Assert.AreEqual("/", value);
        Assert.IsNotEmpty(composer.FindAll("[role=option]"));
    }

    [TestMethod]
    public void TabAcceptsTheActiveMenuItem()
    {
        var value = string.Empty;
        var composer = RenderComponent<BitAiPromptInput>(p => p
            .Add(c => c.Commands, [new BitAiSlashCommand("summarize") { Template = "Summarize: " }, new BitAiSlashCommand("translate")])
            .Add(c => c.ValueChanged, v => value = v));

        composer.Find("textarea").Input("/");
        composer.Find("textarea").KeyDown(Key("ArrowDown"));
        composer.Find("textarea").KeyDown(Key("Tab"));

        Assert.AreEqual("/translate ", value);
        Assert.IsEmpty(composer.FindAll("[role=option]"));
    }

    [TestMethod]
    public void EscapeClosesTheMenuFirstAndStopsGenerationSecond()
    {
        var stops = 0;
        var composer = RenderComponent<BitAiPromptInput>(p => p
            .Add(c => c.Generating, true)
            .Add(c => c.Commands, [new BitAiSlashCommand("summarize")])
            .Add(c => c.OnStop, () => stops++));

        composer.Find("textarea").Input("/");
        Assert.IsNotEmpty(composer.FindAll("[role=option]"));

        composer.Find("textarea").KeyDown(Key("Escape"));
        Assert.IsEmpty(composer.FindAll("[role=option]"));
        Assert.AreEqual(0, stops);

        composer.Find("textarea").KeyDown(Key("Escape"));
        Assert.AreEqual(1, stops);
    }

    [TestMethod]
    public void AnEscapeWithAModifierIsLeftToTheSurfaceAround()
    {
        var stops = 0;
        var composer = RenderComponent<BitAiPromptInput>(p => p
            .Add(c => c.Generating, true)
            .Add(c => c.OnStop, () => stops++));

        composer.Find("textarea").KeyDown(Key("Escape", shift: true));

        Assert.AreEqual(0, stops);
    }

    // The Escape the composer acts on is claimed in its markup, exactly while a press does something: closing the menu,
    // or else stopping the turn in flight. Otherwise it is left to the surface around the composer, a dialog say.
    [TestMethod]
    public void TheEscapeIsClaimedExactlyWhileThePressDoesSomething()
    {
        var composer = RenderComponent<BitAiPromptInput>(p => p
            .Add(c => c.Commands, [new BitAiSlashCommand("summarize")]));

        Assert.IsFalse(composer.Find("textarea").HasAttribute("data-bit-esc"));

        composer.Find("textarea").Input("/");
        Assert.AreEqual("claim", composer.Find("textarea").GetAttribute("data-bit-esc"));

        composer.Find("textarea").KeyDown(Key("Escape"));
        Assert.IsFalse(composer.Find("textarea").HasAttribute("data-bit-esc"));

        composer.Render(p => p.Add(c => c.Generating, true));
        Assert.AreEqual("claim", composer.Find("textarea").GetAttribute("data-bit-esc"));

        composer.Render(p => p.Add(c => c.Disabled, true));
        Assert.IsFalse(composer.Find("textarea").HasAttribute("data-bit-esc"));
    }

    [TestMethod]
    public void TheTextareaIsAComboboxWhoseStateFollowsTheMenu()
    {
        var composer = RenderComponent<BitAiPromptInput>(p => p
            .Add(c => c.Commands, [new BitAiSlashCommand("summarize")])
            .Add(c => c.Mentions, [new BitAiMentionItem("u1", "John")]));

        var textarea = composer.Find("textarea");
        Assert.AreEqual("combobox", textarea.GetAttribute("role"));
        Assert.AreEqual("listbox", textarea.GetAttribute("aria-haspopup"));
        Assert.AreEqual("list", textarea.GetAttribute("aria-autocomplete"));
        Assert.AreEqual("false", textarea.GetAttribute("aria-expanded"));
        Assert.IsFalse(textarea.HasAttribute("aria-controls"));

        composer.Find("textarea").Input("/");
        textarea = composer.Find("textarea");
        var listbox = composer.Find("[role=listbox]");
        Assert.AreEqual("true", textarea.GetAttribute("aria-expanded"));
        Assert.AreEqual(listbox.Id, textarea.GetAttribute("aria-controls"));
        Assert.AreEqual("Commands", listbox.GetAttribute("aria-label"));
        Assert.AreEqual("true", composer.Find("[role=option]").GetAttribute("aria-selected"));
        Assert.IsTrue(composer.Find("[role=option]").ClassList.Contains("bit-aipri-mia"));

        composer.Find("textarea").Input("@Jo");
        Assert.AreEqual("Mentions", composer.Find("[role=listbox]").GetAttribute("aria-label"));
    }

    // A popup that can never open is nothing to announce, so a composer with no commands and no mentions is a text box.
    [TestMethod]
    public void AComposerWithNothingToOfferIsAPlainTextBox()
    {
        var composer = RenderComponent<BitAiPromptInput>();

        var textarea = composer.Find("textarea");
        Assert.IsFalse(textarea.HasAttribute("role"));
        Assert.IsFalse(textarea.HasAttribute("aria-haspopup"));
        Assert.IsFalse(textarea.HasAttribute("aria-autocomplete"));
        Assert.IsFalse(textarea.HasAttribute("aria-expanded"));

        composer.Render(p => p.Add(c => c.Mentions, [new BitAiMentionItem("u1", "John")]));

        Assert.AreEqual("combobox", composer.Find("textarea").GetAttribute("role"));
        Assert.AreEqual("false", composer.Find("textarea").GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public void TheMenuClosesAsTheFocusLeavesTheText()
    {
        var composer = RenderComponent<BitAiPromptInput>(p => p
            .Add(c => c.Commands, [new BitAiSlashCommand("summarize")]));

        composer.Find("textarea").Input("/");
        Assert.IsNotEmpty(composer.FindAll("[role=option]"));

        composer.Find("textarea").Blur();

        Assert.IsEmpty(composer.FindAll("[role=option]"));
        Assert.AreEqual("false", composer.Find("textarea").GetAttribute("aria-expanded"));
        Assert.IsFalse(composer.Find("textarea").HasAttribute("data-bit-aipri-mnu"));
    }

    // With a modifier Enter and Tab keep their own meaning while the menu is open: Shift+Enter breaks the line, Shift+Tab
    // moves the focus back, and Ctrl or Cmd with Enter sends.
    [TestMethod]
    public void OnlyAPlainEnterOrTabTakesTheOption()
    {
        var value = string.Empty;
        var submissions = new List<BitAiPromptSubmission>();
        var composer = RenderComponent<BitAiPromptInput>(p => p
            .Add(c => c.Mentions, [new BitAiMentionItem("u1", "John")])
            .Add(c => c.ValueChanged, v => value = v)
            .Add(c => c.OnSubmit, s => submissions.Add(s)));

        composer.Find("textarea").Input("hi @Jo");

        composer.Find("textarea").KeyDown(Key("Enter", shift: true));
        composer.Find("textarea").KeyDown(Key("Tab", shift: true));

        Assert.AreEqual("hi @Jo", value);
        Assert.IsEmpty(submissions);
        Assert.IsNotEmpty(composer.FindAll("[role=option]"));

        composer.Find("textarea").KeyDown(Key("Enter", ctrl: true));

        Assert.AreEqual("hi @Jo", Assert.ContainsSingle(submissions).Text);
        Assert.IsEmpty(composer.FindAll("[role=option]"));
    }

    [TestMethod]
    public void TheSlashMenuIsExposedToAssistiveTechnologyAndToTheKeyGuard()
    {
        var component = RenderComponent<BitAiPromptInput>(p => p
            .Add(c => c.Commands, [new BitAiSlashCommand("summarize"), new BitAiSlashCommand("translate")]));

        component.Find("textarea").Input("/");

        var textarea = component.Find("textarea");
        var listbox = component.Find("[role=listbox]");
        Assert.AreEqual(listbox.Id, textarea.GetAttribute("aria-controls"));
        Assert.AreEqual(component.FindAll("[role=option]")[0].Id, textarea.GetAttribute("aria-activedescendant"));
        Assert.IsTrue(textarea.HasAttribute("data-bit-aipri-mnu"));

        textarea.KeyDown(Key("ArrowDown"));
        Assert.AreEqual(component.FindAll("[role=option]")[1].Id, component.Find("textarea").GetAttribute("aria-activedescendant"));

        component.Find("textarea").KeyDown(Key("Escape"));
        Assert.IsFalse(component.Find("textarea").HasAttribute("data-bit-aipri-mnu"));
    }

    [TestMethod]
    public void TheSendButtonBecomesTheStopButtonWithoutLeaving()
    {
        var composer = RenderComponent<BitAiPromptInput>(p => p.Add(c => c.Value, "hi"));

        Assert.AreEqual("Send", composer.Find(".bit-aipri-snd").GetAttribute("aria-label"));
        Assert.IsFalse(composer.Find(".bit-aipri-snd").HasAttribute("aria-disabled"));

        composer.Render(p => p.Add(c => c.Generating, true));

        var button = Assert.ContainsSingle(composer.FindAll(".bit-aipri-snd"));
        Assert.AreEqual("Stop", button.GetAttribute("aria-label"));
        Assert.IsFalse(button.HasAttribute("aria-disabled"));
    }

    [TestMethod]
    public void TheSendButtonIsUnavailableWithNothingToSend()
    {
        var composer = RenderComponent<BitAiPromptInput>();

        Assert.AreEqual("true", composer.Find(".bit-aipri-snd").GetAttribute("aria-disabled"));

        composer.Find("textarea").Input("hello");

        Assert.IsFalse(composer.Find(".bit-aipri-snd").HasAttribute("aria-disabled"));
    }

    [TestMethod]
    public void TheCharacterCounterIsAnnouncedOnlyNearTheLimit()
    {
        var composer = RenderComponent<BitAiPromptInput>(p => p.Add(c => c.MaxLength, 20).Add(c => c.Value, "hi"));

        Assert.AreEqual("20", composer.Find("textarea").GetAttribute("maxlength"));
        Assert.AreEqual(composer.Find(".bit-aipri-ctr").Id, composer.Find("textarea").GetAttribute("aria-describedby"));
        Assert.AreEqual("2 / 20", composer.Find(".bit-aipri-ctr").TextContent.Trim());
        Assert.AreEqual(string.Empty, composer.Find("[role=status]").TextContent.Trim());

        composer.Find("textarea").Input(new string('x', 18));

        Assert.AreEqual("18 / 20", composer.Find(".bit-aipri-ctr").TextContent.Trim());
        Assert.IsTrue(composer.Find(".bit-aipri-ctr").ClassList.Contains("bit-aipri-ctn"));
        Assert.AreEqual("2 characters remaining", composer.Find("[role=status]").TextContent.Trim());
    }

    [TestMethod]
    public void TheSubmitKeyGuardFollowsTheOption()
    {
        var provider = RenderComponent<BitAiOptionsProvider>(p => p
            .Add(c => c.UiOptions, BitAiUiOptions.Default with { SubmitOnEnter = true })
            .AddChildContent<BitAiPromptInput>());

        provider.WaitForAssertion(() =>
            Assert.IsTrue((bool)Context.JSInterop.VerifyInvoke("BitBlazorUI.AiPromptInput.registerSubmitKey").Arguments[2]!));

        provider.Render(p => p.Add(c => c.UiOptions, BitAiUiOptions.Default with { SubmitOnEnter = false }));

        provider.WaitForAssertion(() =>
        {
            var calls = Context.JSInterop.VerifyInvoke("BitBlazorUI.AiPromptInput.registerSubmitKey", calledTimes: 2);
            Assert.IsFalse((bool)calls[1].Arguments[2]!);
        });
    }

    [TestMethod]
    public void ADraftIsRestoredSavedAndCleared()
    {
        Context.JSInterop.Setup<string?>("BitBlazorUI.AiPromptInput.readDraft", _ => true).SetResult("saved text");
        var value = string.Empty;

        var composer = RenderComponent<BitAiPromptInput>(p => p
            .Add(c => c.DraftKey, "conversation-1")
            .Add(c => c.ValueChanged, v => value = v));

        composer.WaitForAssertion(() => Assert.AreEqual("saved text", value));
        Assert.AreEqual("conversation-1", Context.JSInterop.VerifyInvoke("BitBlazorUI.AiPromptInput.readDraft").Arguments[0]);

        composer.Find("textarea").Input("saved text and more");
        var saved = Context.JSInterop.VerifyInvoke("BitBlazorUI.AiPromptInput.writeDraft");
        Assert.AreEqual("conversation-1", saved.Arguments[0]);
        Assert.AreEqual("saved text and more", saved.Arguments[1]);

        composer.Find("textarea").KeyDown(Key("Enter"));
        Assert.AreEqual("conversation-1", Context.JSInterop.VerifyInvoke("BitBlazorUI.AiPromptInput.clearDraft").Arguments[0]);
        Assert.AreEqual(string.Empty, value);
    }

    [TestMethod]
    public async Task DisposingTheComposerReleasesItsBrowserRegistrations()
    {
        var composer = RenderComponent<BitAiPromptInput>();
        composer.WaitForAssertion(() => Context.JSInterop.VerifyInvoke("BitBlazorUI.AiPromptInput.registerDrop"));

        await Context.DisposeComponentsAsync();

        Context.JSInterop.VerifyInvoke("BitBlazorUI.AiPromptInput.unregisterPaste");
        Context.JSInterop.VerifyInvoke("BitBlazorUI.AiPromptInput.unregisterDrop");
        Context.JSInterop.VerifyInvoke("BitBlazorUI.AiPromptInput.unregisterSubmitKey");
    }
}

[TestClass]
public class BitAiPromptInputSubmitKeyOptionTests : BitAiTestBase
{
    [TestInitialize]
    public void UseCtrlEnterToSubmit()
    {
        Context.Services.AddSingleton(BitAiUiOptions.Default with { SubmitOnEnter = false });
    }

    [TestMethod]
    public void CtrlOrCmdEnterSubmitsWhenEnterAloneDoesNot()
    {
        var submissions = new List<BitAiPromptSubmission>();
        var composer = RenderComponent<BitAiPromptInput>(p => p
            .Add(c => c.Value, "hello")
            .Add(c => c.OnSubmit, s => submissions.Add(s)));

        composer.Find("textarea").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        Assert.IsEmpty(submissions);

        composer.Find("textarea").KeyDown(new KeyboardEventArgs { Key = "Enter", CtrlKey = true });
        Assert.HasCount(1, submissions);

        composer.Render(p => p.Add(c => c.Value, "again"));
        composer.Find("textarea").KeyDown(new KeyboardEventArgs { Key = "Enter", MetaKey = true });
        Assert.HasCount(2, submissions);
    }
}
