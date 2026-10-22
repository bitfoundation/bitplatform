using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.PromptInput;

/// <summary>
/// What the composer does when the browser and .NET are not in step: keystrokes arriving while the answer to an earlier
/// one is still on its way, which a Blazor Server circuit makes the common case rather than the exception.
/// </summary>
[TestClass]
public class BitAiPromptInputRaceTests : BitAiTestBase
{
    private const string GetCaret = "BitBlazorUI.AiPromptInput.getCaret";

    private static readonly BitAiSlashCommand[] Commands = [new("extract"), new("export"), new("edit")];

    private static KeyboardEventArgs Key(string key) => new() { Key = key };

    [TestMethod]
    public void TheCaretOfAnEarlierKeystrokeAnsweredLateAddsNothingToTheMenu()
    {
        var phase = 1;
        var stale = Context.JSInterop.Setup<int?>(GetCaret, _ => phase == 1);
        var fresh = Context.JSInterop.Setup<int?>(GetCaret, _ => phase == 2);
        var value = string.Empty;

        var composer = RenderComponent<BitAiPromptInput>(p => p
            .Add(c => c.Commands, Commands)
            .Add(c => c.ValueChanged, v => value = v));

        composer.Find("textarea").Input("/ex");
        phase = 2;
        composer.Find("textarea").Input("/exp");

        // The newer read is answered first, the earlier one last.
        fresh.SetResult("/exp".Length);
        stale.SetResult("/ex".Length);

        composer.WaitForAssertion(() => Assert.HasCount(1, composer.FindAll("[role=option]")));
        StringAssert.Contains(composer.Find("[role=option]").TextContent, "/export");

        // And what accepting it replaces is the token there is now, with nothing of it left behind.
        composer.Find("textarea").KeyDown(Key("Enter"));

        Assert.AreEqual("/export ", value);
        Assert.IsEmpty(composer.FindAll("[role=option]"));
    }

    [TestMethod]
    public void AnOpenMenuFollowsTheTypingBeforeTheNewCaretIsKnown()
    {
        var phase = 1;
        Context.JSInterop.Setup<int?>(GetCaret, _ => phase == 1).SetResult("/ex".Length);
        Context.JSInterop.Setup<int?>(GetCaret, _ => phase == 2);
        var value = string.Empty;
        var submissions = new List<BitAiPromptSubmission>();

        var composer = RenderComponent<BitAiPromptInput>(p => p
            .Add(c => c.Commands, Commands)
            .Add(c => c.ValueChanged, v => value = v)
            .Add(c => c.OnSubmit, s => submissions.Add(s)));

        composer.Find("textarea").Input("/ex");
        Assert.HasCount(2, composer.FindAll("[role=option]"));

        // The caret of the next keystroke is never answered: the menu is worked out from the text alone meanwhile.
        phase = 2;
        composer.Find("textarea").Input("/exp");

        var option = Assert.ContainsSingle(composer.FindAll("[role=option]"));
        StringAssert.Contains(option.TextContent, "/export");

        composer.Find("textarea").KeyDown(Key("Enter"));

        Assert.AreEqual("/export ", value);
        Assert.IsEmpty(submissions);
    }

    [TestMethod]
    public void AnOptionPressedInAMenuOfAnOlderTextReplacesTheTokenThereIsNow()
    {
        Context.JSInterop.Setup<int?>(GetCaret, _ => true).SetResult("@Jo".Length);
        var value = string.Empty;

        var composer = RenderComponent<BitAiPromptInput>(p => p
            .Add(c => c.Mentions, [new BitAiMentionItem("u1", "John"), new BitAiMentionItem("u2", "Joanna")])
            .Add(c => c.ValueChanged, v => value = v));

        composer.Find("textarea").Input("@Jo");
        Assert.HasCount(2, composer.FindAll("[role=option]"));

        // The text moves on under the open menu without a keystroke of its own - the page hands the composer another.
        composer.Render(p => p.Add(c => c.Value, "@Joh"));

        composer.FindAll("[role=option]").Single(o => o.TextContent.Contains("John")).Click();

        Assert.AreEqual("@John ", value);
    }

    [TestMethod]
    public void ADismissedMenuIsNotBroughtBackByACaretAnsweredLate()
    {
        var phase = 1;
        Context.JSInterop.Setup<int?>(GetCaret, _ => phase == 1).SetResult("/ex".Length);
        var late = Context.JSInterop.Setup<int?>(GetCaret, _ => phase == 2);

        var composer = RenderComponent<BitAiPromptInput>(p => p.Add(c => c.Commands, Commands));

        composer.Find("textarea").Input("/ex");
        phase = 2;
        composer.Find("textarea").Input("/exp");
        Assert.IsNotEmpty(composer.FindAll("[role=option]"));

        composer.Find("textarea").KeyDown(Key("Escape"));
        Assert.IsEmpty(composer.FindAll("[role=option]"));

        late.SetResult("/exp".Length);

        composer.Render(p => p.Add(c => c.Commands, Commands));
        Assert.IsEmpty(composer.FindAll("[role=option]"));
    }

    // Blazor takes what the textarea held when a keystroke was sent as what it last rendered only for a binding - the
    // render tree marks its handler as one that updates the value - so a render arriving while later keystrokes are on
    // their way compares against those and writes nothing back over them. A value and an oninput handler of their own
    // would write the older text back, and the keystrokes typed since would be lost ("hello" arriving as "hlo").
    [TestMethod]
    public void TheTextIsABindingBlazorKeepsInStepWithTheBrowser()
    {
        var composer = RenderComponent<BitAiPromptInput>(p => p.Add(c => c.Value, "hi"));

        var frames = CurrentFrames(composer.ComponentId);
        var textarea = Enumerable.Range(0, frames.Count).Single(i => frames.Array[i].FrameType == RenderTreeFrameType.Element
                                                                     && frames.Array[i].ElementName == "textarea");
        var attributes = Enumerable.Range(textarea + 1, frames.Count - textarea - 1)
                                   .TakeWhile(i => frames.Array[i].FrameType == RenderTreeFrameType.Attribute)
                                   .Select(i => frames.Array[i])
                                   .ToArray();

        Assert.AreEqual("hi", attributes.Single(a => a.AttributeName == "value").AttributeValue);
        Assert.AreEqual("value", attributes.Single(a => a.AttributeName == "oninput").AttributeEventUpdatesAttributeName);

        // The binding still hands the typed text on, as the handler did.
        var value = string.Empty;
        composer.Render(p => p.Add(c => c.ValueChanged, v => value = v));
        composer.Find("textarea").Input("hello");

        Assert.AreEqual("hello", value);
    }

    private ArrayRange<RenderTreeFrame> CurrentFrames(int componentId)
    {
        var method = typeof(Renderer).GetMethod("GetCurrentRenderTreeFrames",
                                                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                                                [typeof(int)])!;

        return (ArrayRange<RenderTreeFrame>)method.Invoke(Renderer, [componentId])!;
    }
}
