using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
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
        Assert.AreEqual("Dictation failed: microphone access was denied", voice.Find("[role=status]").TextContent.Trim());
        Assert.AreEqual("Dictate", voice.Find("button").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void ListeningThroughTheSpeechApiGivesTheRootItsListeningLookButNotTheRecordingClass()
    {
        SetUpDictation(handle: 3);

        var voice = RenderComponent<BitAiVoiceInput>(p =>
        {
            p.Add(c => c.Classes, new BitAiVoiceInputClassStyles { Root = "root-class", Listening = "listening-class" });
            p.Add(c => c.Styles, new BitAiVoiceInputClassStyles { Root = "margin: 1px", Listening = "outline: 2px solid red" });
        });
        var root = voice.WaitForElement(".bit-aivci");

        Assert.IsFalse(root.ClassList.Contains("bit-aivci-lsn"));
        Assert.IsFalse(root.ClassList.Contains("listening-class"));
        Assert.IsFalse(root.GetAttribute("style")!.Contains("outline", System.StringComparison.Ordinal));

        voice.Find("button").Click();

        root = voice.Find(".bit-aivci");
        Assert.IsTrue(root.ClassList.Contains("bit-aivci-lsn"));

        // bit-aivci-rec keeps meaning a recording for the transcriber, which the speech API of the browser makes none of.
        Assert.IsFalse(root.ClassList.Contains("bit-aivci-rec"));
        Assert.IsTrue(root.ClassList.Contains("listening-class"));
        Assert.IsTrue(root.ClassList.Contains("root-class"));
        StringAssert.Contains(root.GetAttribute("style"), "outline: 2px solid red");
        StringAssert.Contains(root.GetAttribute("style"), "margin: 1px");

        voice.Find("button").Click();

        root = voice.Find(".bit-aivci");
        Assert.IsFalse(root.ClassList.Contains("bit-aivci-lsn"));
        Assert.IsFalse(root.ClassList.Contains("listening-class"));
        Assert.IsTrue(root.ClassList.Contains("root-class"));
        Assert.IsFalse(root.GetAttribute("style")!.Contains("outline", System.StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task AFailureIsShownBesideTheButtonUntilTheNextDictation()
    {
        SetUpDictation(handle: 3);

        var voice = RenderComponent<BitAiVoiceInput>(p =>
        {
            p.Add(c => c.Classes, new BitAiVoiceInputClassStyles { Error = "error-class" });
            p.Add(c => c.Styles, new BitAiVoiceInputClassStyles { Error = "color: red" });
        });
        voice.WaitForElement("button").Click();

        Assert.AreEqual(0, voice.FindAll(".bit-aivci-err").Count);

        await voice.InvokeAsync(() => voice.Instance._OnDictationError("audio-capture"));

        var error = voice.Find(".bit-aivci > .bit-aivci-err");
        Assert.AreEqual("Dictation failed: no microphone was found", error.TextContent.Trim());
        Assert.AreEqual("Dictation failed: no microphone was found", error.GetAttribute("title"));

        // The live region announces the same text, so the visible copy is not read a second time.
        Assert.AreEqual("true", error.GetAttribute("aria-hidden"));
        Assert.AreEqual(error.TextContent.Trim(), voice.Find("[role=status]").TextContent.Trim());
        Assert.IsTrue(error.ClassList.Contains("error-class"));
        Assert.AreEqual("color: red", error.GetAttribute("style"));
        Assert.IsFalse(voice.Find(".bit-aivci").ClassList.Contains("bit-aivci-lsn"));

        voice.Find("button").Click();

        Assert.IsTrue(voice.Instance.IsListening);
        Assert.AreEqual(0, voice.FindAll(".bit-aivci-err").Count);
    }

    [TestMethod]
    public void AnUnknownErrorIsShownByItsName()
    {
        Assert.AreEqual("Dictation failed: something-new", new BitAiStrings().DictationFailed("something-new"));
        Assert.IsNull(new BitAiStrings().DictationErrorReason("something-new"));
    }

    [TestMethod]
    [DataRow("not-allowed")]
    [DataRow("service-not-allowed")]
    [DataRow("audio-capture")]
    [DataRow("no-speech")]
    [DataRow("aborted")]
    [DataRow("network")]
    [DataRow("language-not-supported")]
    [DataRow("bad-grammar")]
    [DataRow("unknown")]
    [DataRow("recording-failed")]
    [DataRow("too-large")]
    [DataRow("transcription-failed")]
    public void EveryErrorTheBrowserOrARecordingReportsHasAReasonInWords(string error)
    {
        var strings = new BitAiStrings();
        var reason = strings.DictationErrorReason(error);

        Assert.IsFalse(string.IsNullOrWhiteSpace(reason));
        Assert.AreEqual($"Dictation failed: {reason}", strings.DictationFailed(error));
        Assert.IsFalse(strings.DictationFailed(error).Contains(error, System.StringComparison.Ordinal));
    }

    // A translation of the reasons alone reaches the sentence the default DictationFailed builds around them.
    [TestMethod]
    public void TheReasonsCanBeTranslatedOnTheirOwn()
    {
        Assert.AreEqual("Dictation failed: micro refusé", new TranslatedReasons().DictationFailed("not-allowed"));
        Assert.AreEqual("Dictation failed: network", new TranslatedReasons().DictationFailed("network"));
    }

    private sealed class TranslatedReasons : BitAiStrings
    {
        public override string? DictationErrorReason(string error) => error is "not-allowed" ? "micro refusé" : null;
    }

    // A microphone refused twice fails twice in the same words, with nothing in between (the recording never starts). The
    // live region's text and the reason are keyed by the announcement, so each failure renders them as new elements - an
    // addition to the region a screen reader announces, where text set to what it already was changes nothing - and the
    // reason's entrance plays again for the eye.
    [TestMethod]
    public void TheSameFailureTwiceIsAnnouncedAndShownAgain()
    {
        Context.JSInterop.Setup<bool>("BitBlazorUI.AiVoiceRecorder.isSupported", _ => true).SetResult(true);
        Context.JSInterop.Setup<int>("BitBlazorUI.AiVoiceRecorder.start", _ => true).SetResult(-1);

        var voice = RenderComponent<BitAiVoiceInput>(p => p.Add(c => c.Transcriber, (_, _, _) => Task.FromResult<string?>(null)));
        voice.WaitForElement("button").Click();

        var (status, error) = (Version(voice.Instance, "_statusVersion"), Version(voice.Instance, "_errorVersion"));

        voice.Find("button").Click();

        Assert.AreEqual("Dictation failed: microphone access was denied", voice.Find("[role=status] > span").TextContent);
        Assert.AreEqual("Dictation failed: microphone access was denied", voice.Find(".bit-aivci-err").TextContent.Trim());
        Assert.IsGreaterThan(status, Version(voice.Instance, "_statusVersion"));
        Assert.IsGreaterThan(error, Version(voice.Instance, "_errorVersion"));

        static int Version(BitAiVoiceInput voice, string field) =>
            (int)typeof(BitAiVoiceInput).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(voice)!;
    }

    [TestMethod]
    public async Task TheReasonOfAFailureGoesOnceTheDictationIsStoppedOrDisabled()
    {
        SetUpDictation(handle: 3);

        var voice = RenderComponent<BitAiVoiceInput>();
        voice.WaitForElement("button").Click();
        await voice.InvokeAsync(() => voice.Instance._OnDictationError("audio-capture"));
        Assert.AreEqual(1, voice.FindAll(".bit-aivci-err").Count);

        // An end reported late, after the failure, is not a new state of the button: the reason stays.
        await voice.InvokeAsync(() => voice.Instance._OnDictationEnded());
        Assert.AreEqual(1, voice.FindAll(".bit-aivci-err").Count);

        // Started and stopped again: the reason was about the dictation before.
        voice.Find("button").Click();
        voice.Find("button").Click();
        Assert.IsFalse(voice.Instance.IsListening);
        Assert.AreEqual(0, voice.FindAll(".bit-aivci-err").Count);

        voice.Find("button").Click();
        await voice.InvokeAsync(() => voice.Instance._OnDictationError("audio-capture"));
        Assert.AreEqual(1, voice.FindAll(".bit-aivci-err").Count);

        voice.Render(p => p.Add(c => c.Disabled, true));
        Assert.AreEqual(0, voice.FindAll(".bit-aivci-err").Count);
    }

    // A parent removing the voice input from inside the callbacks a failure raises leaves no timer of the reason behind
    // to render it after it went away.
    [TestMethod]
    public async Task AVoiceInputRemovedWhileAFailureIsReportedLeavesNothingRunning()
    {
        SetUpDictation(handle: 3);
        var errors = new List<string>();

        var host = RenderComponent<VoiceInputHost>(p => p.Add(c => c.OnError, EventCallback.Factory.Create<string>(this, e => errors.Add(e))));
        host.WaitForElement("button").Click();

        var voice = host.FindComponent<BitAiVoiceInput>().Instance;
        await host.InvokeAsync(() => voice._OnDictationError("not-allowed"));

        Assert.AreEqual(0, host.FindAll(".bit-aivci").Count);
        CollectionAssert.AreEqual(new[] { "not-allowed" }, errors);

        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        var timer = typeof(BitAiVoiceInput).GetField("_errorTimer", Private)!.GetValue(voice)!;
        Assert.IsNull(timer.GetType().GetField("_cts", Private)!.GetValue(timer));
    }

    private sealed class VoiceInputHost : ComponentBase
    {
        private bool _shown = true;

        [Parameter] public EventCallback<string> OnError { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (_shown is false) return;

            builder.OpenComponent<BitAiVoiceInput>(0);
            builder.AddAttribute(1, nameof(BitAiVoiceInput.OnListeningChanged), EventCallback.Factory.Create<bool>(this, listening =>
            {
                if (listening is false) _shown = false;
            }));
            builder.AddAttribute(2, nameof(BitAiVoiceInput.OnError), OnError);
            builder.CloseComponent();
        }
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
