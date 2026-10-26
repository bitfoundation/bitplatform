using System.Collections.Generic;
using System.Threading.Tasks;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.VoiceInput;

[TestClass]
public class BitAiVoiceInputTests : BitAiTestBase
{
    private const string IsSupported = "BitBlazorUI.AiVoiceInput.isDictationSupported";
    private const string Start = "BitBlazorUI.AiVoiceInput.startDictation";
    private const string Stop = "BitBlazorUI.AiVoiceInput.stopDictation";

    private void SetUpDictation(bool supported = true, int handle = 7)
    {
        Context.JSInterop.Setup<bool>(IsSupported, _ => true).SetResult(supported);
        Context.JSInterop.Setup<int>(Start, _ => true).SetResult(handle);
    }

    [TestMethod]
    public void BitAiVoiceInputShouldRenderItsRootClassClassStyleAndDir()
    {
        SetUpDictation();

        var voice = RenderComponent<BitAiVoiceInput>(parameters =>
        {
            parameters.Add(p => p.Class, "custom-class");
            parameters.Add(p => p.Style, "margin: 1px");
            parameters.Add(p => p.Dir, BitDir.Rtl);
            parameters.Add(p => p.Classes, new BitAiVoiceInputClassStyles { Root = "root-class", LiveRegion = "live-class" });
        });

        var root = voice.WaitForElement(".bit-aivci");

        Assert.AreEqual("SPAN", root.TagName);
        Assert.IsTrue(root.ClassList.Contains("custom-class"));
        Assert.IsTrue(root.ClassList.Contains("root-class"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        StringAssert.Contains(root.GetAttribute("style"), "margin: 1px");
        Assert.IsTrue(voice.Find(".bit-aivci-sts").ClassList.Contains("live-class"));
        Assert.IsNotNull(voice.Find(".bit-aivci > button.bit-aivci-btn"));
        Assert.IsTrue(voice.Find(".bit-aivci-btn i").ClassList.Contains("bit-icon-ai--Microphone"));
    }

    [TestMethod]
    public void BitAiVoiceInputShouldRespectDisabled()
    {
        SetUpDictation();

        var voice = RenderComponent<BitAiVoiceInput>(parameters => parameters.Add(p => p.Disabled, true));

        var root = voice.WaitForElement(".bit-aivci");

        Assert.IsTrue(root.ClassList.Contains("bit-dis"));
        Assert.AreEqual("true", voice.Find("button").GetAttribute("aria-disabled"));

        voice.Find("button").Click();

        Assert.AreEqual(0, Context.JSInterop.Invocations[Start].Count);
    }

    [TestMethod]
    public void DictationStateIsAnnouncedThroughALiveRegion()
    {
        SetUpDictation(handle: 7);
        var listening = new List<bool>();

        var voice = RenderComponent<BitAiVoiceInput>(p => p.Add(c => c.OnListeningChanged, l => listening.Add(l)));
        voice.WaitForElement("button");

        var status = voice.Find("[role=status]");
        Assert.AreEqual("polite", status.GetAttribute("aria-live"));
        Assert.AreEqual(string.Empty, status.TextContent.Trim());
        Assert.AreEqual("Dictate", voice.Find("button").GetAttribute("aria-label"));

        // The name says what a press does next, and is the one thing announced: a pressed state on top of a name that
        // changes with it would read ambiguously ("Stop dictation, pressed").
        Assert.IsFalse(voice.Find("button").HasAttribute("aria-pressed"));

        voice.Find("button").Click();
        Assert.AreEqual("Listening", voice.Find("[role=status]").TextContent.Trim());
        Assert.AreEqual("Stop dictation", voice.Find("button").GetAttribute("aria-label"));
        Assert.IsFalse(voice.Find("button").HasAttribute("aria-pressed"));

        voice.Find("button").Click();
        Assert.AreEqual(7, (int)Context.JSInterop.VerifyInvoke(Stop).Arguments[0]!);
        Assert.AreEqual("Dictation stopped", voice.Find("[role=status]").TextContent.Trim());
        CollectionAssert.AreEqual(new[] { true, false }, listening);
    }

    [TestMethod]
    public async Task AFailureIsAnnouncedAndEndsListening()
    {
        SetUpDictation(handle: 3);
        string? error = null;

        var voice = RenderComponent<BitAiVoiceInput>(p => p.Add(c => c.OnError, e => error = e));
        voice.WaitForElement("button").Click();

        await voice.InvokeAsync(() => voice.Instance._OnDictationError("not-allowed"));

        Assert.AreEqual("not-allowed", error);
        Assert.IsFalse(voice.Instance.IsListening);
        Assert.AreEqual("Dictation failed: not-allowed", voice.Find("[role=status]").TextContent.Trim());
        Assert.AreEqual("Dictate", voice.Find("button").GetAttribute("aria-label"));
    }

    [TestMethod]
    public async Task DictationThatEndsOnItsOwnTurnsTheButtonOff()
    {
        SetUpDictation(handle: 3);

        var voice = RenderComponent<BitAiVoiceInput>();
        voice.WaitForElement("button").Click();
        Assert.IsTrue(voice.Instance.IsListening);

        await voice.InvokeAsync(() => voice.Instance._OnDictationEnded());

        Assert.IsFalse(voice.Instance.IsListening);
        Assert.AreEqual("Dictate", voice.Find("button").GetAttribute("aria-label"));
        Assert.AreEqual("Dictation stopped", voice.Find("[role=status]").TextContent.Trim());
    }

    [TestMethod]
    public void TheLanguageIsHandedToTheRecognizer()
    {
        SetUpDictation();

        var voice = RenderComponent<BitAiVoiceInput>(p => p.Add(c => c.Language, "fa-IR"));
        voice.WaitForElement("button").Click();

        Assert.AreEqual("fa-IR", Context.JSInterop.VerifyInvoke(Start).Arguments[1]);
    }

    [TestMethod]
    public async Task InterimTextIsForwardedAndClearedWhenAPhraseIsFinal()
    {
        SetUpDictation(handle: 3);
        var composer = RenderComponent<BitAiPromptInput>(p => p.Add(c => c.AllowDictation, true));
        var voice = composer.FindComponent<BitAiVoiceInput>();
        composer.WaitForElement("[aria-label='Dictate']").Click();

        await composer.InvokeAsync(() => voice.Instance._OnTranscriptReceived(string.Empty, "hel"));
        Assert.AreEqual("hel", composer.Find(".bit-aipri-itr").TextContent.Trim());

        await composer.InvokeAsync(() => voice.Instance._OnTranscriptReceived("hello", string.Empty));
        Assert.IsEmpty(composer.FindAll(".bit-aipri-itr"));
        Assert.AreEqual("hello", composer.Find("textarea").GetAttribute("value"));
    }

    // Provisional text the recognizer will never make final is cleared with the empty interim OnInterim promises, however
    // the dictation stops: the button, an error, or an end on its own (another instance taking over included).
    [TestMethod]
    public async Task ProvisionalTextIsClearedWhenTheDictationStops()
    {
        SetUpDictation(handle: 3);
        var interims = new List<string>();

        var voice = RenderComponent<BitAiVoiceInput>(p => p.Add(c => c.OnInterim, i => interims.Add(i)));
        voice.WaitForElement("button").Click();

        await voice.InvokeAsync(() => voice.Instance._OnTranscriptReceived(string.Empty, "hel"));
        voice.Find("button").Click();
        CollectionAssert.AreEqual(new[] { "hel", string.Empty }, interims);

        voice.Find("button").Click();
        await voice.InvokeAsync(() => voice.Instance._OnTranscriptReceived(string.Empty, "wor"));
        await voice.InvokeAsync(() => voice.Instance._OnDictationError("network"));
        CollectionAssert.AreEqual(new[] { "hel", string.Empty, "wor", string.Empty }, interims);

        voice.Find("button").Click();
        await voice.InvokeAsync(() => voice.Instance._OnTranscriptReceived(string.Empty, "ld"));
        await voice.InvokeAsync(() => voice.Instance._OnDictationEnded());
        CollectionAssert.AreEqual(new[] { "hel", string.Empty, "wor", string.Empty, "ld", string.Empty }, interims);
    }

    [TestMethod]
    public async Task StoppingWithNothingProvisionalRaisesNoInterim()
    {
        SetUpDictation(handle: 3);
        var interims = new List<string>();

        var voice = RenderComponent<BitAiVoiceInput>(p => p.Add(c => c.OnInterim, i => interims.Add(i)));
        voice.WaitForElement("button").Click();

        await voice.InvokeAsync(() => voice.Instance._OnTranscriptReceived("hello", string.Empty));
        voice.Find("button").Click();

        CollectionAssert.AreEqual(new[] { string.Empty }, interims);
    }

    // The recognizer finishes the phrase it was hearing after stop(), and that phrase is still raised.
    [TestMethod]
    public async Task APhraseFinishedAfterTheStopIsStillRaised()
    {
        SetUpDictation(handle: 3);
        var transcripts = new List<string>();

        var voice = RenderComponent<BitAiVoiceInput>(p => p.Add(c => c.OnTranscript, t => transcripts.Add(t)));
        voice.WaitForElement("button").Click();
        voice.Find("button").Click();
        Assert.IsFalse(voice.Instance.IsListening);

        await voice.InvokeAsync(() => voice.Instance._OnTranscriptReceived("the last words", string.Empty));

        CollectionAssert.AreEqual(new[] { "the last words" }, transcripts);
    }

    [TestMethod]
    public async Task DisposingWhileListeningStopsTheMicrophone()
    {
        SetUpDictation(handle: 5);
        var voice = RenderComponent<BitAiVoiceInput>();
        voice.WaitForElement("button").Click();

        await Context.DisposeComponentsAsync();

        Assert.AreEqual(5, (int)Context.JSInterop.VerifyInvoke(Stop).Arguments[0]!);
    }

    [TestMethod]
    public void DisablingWhileListeningStopsTheMicrophone()
    {
        SetUpDictation(handle: 9);
        var voice = RenderComponent<BitAiVoiceInput>();
        voice.WaitForElement("button").Click();

        voice.Render(p => p.Add(c => c.Disabled, true));

        Assert.AreEqual(9, (int)Context.JSInterop.VerifyInvoke(Stop).Arguments[0]!);
        Assert.IsFalse(voice.Instance.IsListening);
    }

    // One dictation runs at a time in a page, so the script stops the first when a second instance starts one, and tells
    // the first that its dictation is over - unless the one starting is the first itself. It tells them apart by the
    // owner each start names.
    [TestMethod]
    public void EveryStartNamesTheInstanceItIsFor()
    {
        SetUpDictation();

        var first = RenderComponent<BitAiVoiceInput>();
        var second = RenderComponent<BitAiVoiceInput>();

        first.WaitForElement("button").Click();
        second.WaitForElement("button").Click();

        var starts = Context.JSInterop.VerifyInvoke(Start, calledTimes: 2);
        Assert.AreEqual(first.Instance.UniqueId, starts[0].Arguments[2]);
        Assert.AreEqual(second.Instance.UniqueId, starts[1].Arguments[2]);
        Assert.AreNotEqual(starts[0].Arguments[2], starts[1].Arguments[2]);
    }

    [TestMethod]
    public async Task AnInstanceWhoseDictationAnotherOneStoppedTurnsItselfOff()
    {
        SetUpDictation(handle: 4);
        var listening = new List<bool>();

        var first = RenderComponent<BitAiVoiceInput>(p => p.Add(c => c.OnListeningChanged, l => listening.Add(l)));
        var second = RenderComponent<BitAiVoiceInput>();

        first.WaitForElement("button").Click();
        second.WaitForElement("button").Click();

        // What the script sends the first instance as the second one's start stops its recognizer.
        await first.InvokeAsync(() => first.Instance._OnDictationEnded());

        Assert.IsFalse(first.Instance.IsListening);
        Assert.AreEqual("Dictate", first.Find("button").GetAttribute("aria-label"));
        Assert.AreEqual("Dictation stopped", first.Find("[role=status]").TextContent.Trim());
        CollectionAssert.AreEqual(new[] { true, false }, listening);
        Assert.IsTrue(second.Instance.IsListening);
    }

    [TestMethod]
    public void TheButtonIsAbsentWhenTheBrowserCannotTranscribe()
    {
        SetUpDictation(supported: false);

        var voice = RenderComponent<BitAiVoiceInput>();

        Assert.IsEmpty(voice.FindAll(".bit-aivci"));
        Assert.IsEmpty(voice.FindAll("button"));
        Assert.IsEmpty(voice.FindAll("[role=status]"));
    }
}
