using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.VoiceInput;

/// <summary>
/// Covers the dictation of BitAiVoiceInput through the app's own transcriber: the microphone recorded in the browser
/// (AiVoiceRecorder), the take handed to a Transcriber delegate or an IBitAiSpeechToText service, and the countdown,
/// the announcements and the failures around it.
/// </summary>
[TestClass]
public class BitAiVoiceInputRecordingTests : BitAiTestBase
{
    private const string IsSpeechSupported = "BitBlazorUI.AiVoiceInput.isDictationSupported";
    private const string StartDictation = "BitBlazorUI.AiVoiceInput.startDictation";
    private const string IsRecordingSupported = "BitBlazorUI.AiVoiceRecorder.isSupported";
    private const string StartRecording = "BitBlazorUI.AiVoiceRecorder.start";
    private const string StopRecording = "BitBlazorUI.AiVoiceRecorder.stop";

    private sealed class FakeStreamReference(byte[] bytes) : IJSStreamReference
    {
        public bool Disposed { get; private set; }

        public long Length => bytes.Length;

        public ValueTask<Stream> OpenReadStreamAsync(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult<Stream>(new MemoryStream(bytes));
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => throw new NotSupportedException();

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            throw new NotSupportedException();

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FakeSpeechToText : IBitAiSpeechToText
    {
        public string? Language { get; private set; }

        public string? ContentType { get; private set; }

        public Task<string?> TranscribeAsync(Stream audio, string contentType, string? language, CancellationToken cancellationToken)
        {
            Language = language;
            ContentType = contentType;
            return Task.FromResult<string?>("from the service");
        }
    }

    private sealed class Recorded
    {
        public byte[]? Bytes { get; set; }

        public string? ContentType { get; set; }
    }

    private void SetUpRecording(bool recordingSupported = true, int handle = 5, bool speechSupported = true)
    {
        Context.JSInterop.Setup<bool>(IsRecordingSupported, _ => true).SetResult(recordingSupported);
        Context.JSInterop.Setup<int>(StartRecording, _ => true).SetResult(handle);
        Context.JSInterop.Setup<bool>(IsSpeechSupported, _ => true).SetResult(speechSupported);
        Context.JSInterop.Setup<int>(StartDictation, _ => true).SetResult(9);
    }

    private static Func<Stream, string, CancellationToken, Task<string?>> Transcriber(Recorded recorded, string? text = "hello there")
    {
        return (audio, contentType, _) =>
        {
            using var copy = new MemoryStream();
            audio.CopyTo(copy);
            recorded.Bytes = copy.ToArray();
            recorded.ContentType = contentType;
            return Task.FromResult(text);
        };
    }

    [TestMethod]
    public void ATranscriberRecordsInsteadOfUsingTheSpeechApiOfTheBrowser()
    {
        SetUpRecording();

        var voice = RenderComponent<BitAiVoiceInput>(p => p.Add(c => c.Transcriber, Transcriber(new())));
        voice.WaitForElement("button").Click();

        Context.JSInterop.VerifyInvoke(IsRecordingSupported);
        Context.JSInterop.VerifyNotInvoke(IsSpeechSupported);
        Context.JSInterop.VerifyNotInvoke(StartDictation);

        var start = Context.JSInterop.VerifyInvoke(StartRecording);
        Assert.AreEqual(60000d, (double)start.Arguments[1]!);
        Assert.AreEqual(0d, (double)start.Arguments[2]!);

        Assert.IsTrue(voice.Instance.IsListening);
        Assert.IsTrue(voice.Find(".bit-aivci").ClassList.Contains("bit-aivci-rec"));
        Assert.AreEqual("Stop dictation", voice.Find("button").GetAttribute("aria-label"));
        Assert.AreEqual("Recording, 1 minute left", voice.Find("[role=status]").TextContent.Trim());
    }

    [TestMethod]
    public void TheTimeLeftIsShownBesideTheButtonForTheEyeAlone()
    {
        SetUpRecording();

        var voice = RenderComponent<BitAiVoiceInput>(p =>
        {
            p.Add(c => c.Transcriber, Transcriber(new()));
            p.Add(c => c.MaxRecordingDuration, TimeSpan.FromSeconds(90));
            p.Add(c => c.Classes, new BitAiVoiceInputClassStyles { Countdown = "countdown-class" });
        });

        Assert.IsEmpty(voice.FindAll(".bit-aivci-cdn"));

        voice.WaitForElement("button").Click();

        var countdown = voice.Find(".bit-aivci-cdn");
        Assert.AreEqual("1:30", countdown.TextContent.Trim());
        Assert.AreEqual("true", countdown.GetAttribute("aria-hidden"));
        Assert.IsTrue(countdown.ClassList.Contains("countdown-class"));
        Assert.AreEqual("Recording, 1 minute 30 seconds left", voice.Find("[role=status]").TextContent.Trim());
        Assert.AreEqual(90000d, (double)Context.JSInterop.VerifyInvoke(StartRecording).Arguments[1]!);
    }

    [TestMethod]
    public void TheTimeLeftCountsDownAndIsAnnouncedOnceAsTheEndNears()
    {
        SetUpRecording();

        var voice = RenderComponent<BitAiVoiceInput>(p =>
        {
            p.Add(c => c.Transcriber, Transcriber(new()));
            p.Add(c => c.MaxRecordingDuration, TimeSpan.FromSeconds(10.5));
        });
        voice.WaitForElement("button").Click();

        Assert.AreEqual("0:11", voice.Find(".bit-aivci-cdn").TextContent.Trim());

        voice.WaitForAssertion(() =>
        {
            Assert.AreEqual("0:10", voice.Find(".bit-aivci-cdn").TextContent.Trim());
            Assert.AreEqual("10 seconds left", voice.Find("[role=status]").TextContent.Trim());
        }, TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public void TheSilenceTimeoutIsHandedToTheRecorder()
    {
        SetUpRecording();

        var voice = RenderComponent<BitAiVoiceInput>(p =>
        {
            p.Add(c => c.Transcriber, Transcriber(new()));
            p.Add(c => c.SilenceTimeout, TimeSpan.FromSeconds(2));
        });
        voice.WaitForElement("button").Click();

        Assert.AreEqual(2000d, (double)Context.JSInterop.VerifyInvoke(StartRecording).Arguments[2]!);
    }

    [TestMethod]
    public async Task StoppingKeepsTheTakeAndItsTranscriptIsRaised()
    {
        SetUpRecording(handle: 5);
        var recorded = new Recorded();
        var transcripts = new List<string>();
        var listening = new List<bool>();

        var voice = RenderComponent<BitAiVoiceInput>(p =>
        {
            p.Add(c => c.Transcriber, Transcriber(recorded, "  hello there  "));
            p.Add(c => c.OnTranscript, t => transcripts.Add(t));
            p.Add(c => c.OnListeningChanged, l => listening.Add(l));
        });
        voice.WaitForElement("button").Click();
        voice.Find("button").Click();

        var stop = Context.JSInterop.VerifyInvoke(StopRecording);
        Assert.AreEqual(5, (int)stop.Arguments[0]!);
        Assert.IsTrue((bool)stop.Arguments[1]!);
        Assert.IsTrue(voice.Instance.IsTranscribing);
        Assert.IsFalse(voice.Instance.IsListening);
        Assert.IsEmpty(voice.FindAll(".bit-aivci-cdn"));
        Assert.AreEqual("Transcribing", voice.Find("[role=status]").TextContent.Trim());

        var audio = new FakeStreamReference([1, 2, 3, 4]);
        await voice.InvokeAsync(() => voice.Instance._OnRecordingCompleted(audio, "audio/webm;codecs=opus", 4));

        CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4 }, recorded.Bytes);
        Assert.AreEqual("audio/webm", recorded.ContentType);
        CollectionAssert.AreEqual(new[] { "hello there" }, transcripts);
        CollectionAssert.AreEqual(new[] { true, false }, listening);
        Assert.IsFalse(voice.Instance.IsTranscribing);
        Assert.IsTrue(audio.Disposed);
        Assert.AreEqual("Dictation stopped", voice.Find("[role=status]").TextContent.Trim());
    }

    [TestMethod]
    public async Task ATakeTheRecorderEndsOnItsOwnIsTranscribedAllTheSame()
    {
        SetUpRecording();
        var transcripts = new List<string>();

        var voice = RenderComponent<BitAiVoiceInput>(p =>
        {
            p.Add(c => c.Transcriber, Transcriber(new(), "said"));
            p.Add(c => c.OnTranscript, t => transcripts.Add(t));
        });
        voice.WaitForElement("button").Click();

        // The maximum length, a silence or another capture of the microphone ended it, with no press.
        await voice.InvokeAsync(() => voice.Instance._OnRecordingCompleted(new FakeStreamReference([7]), "audio/mp4", 1));

        CollectionAssert.AreEqual(new[] { "said" }, transcripts);
        Assert.IsFalse(voice.Instance.IsListening);
        Assert.AreEqual("Dictate", voice.Find("button").GetAttribute("aria-label"));
    }

    [TestMethod]
    public async Task AnEmptyTakeOrTranscriptRaisesNothing()
    {
        SetUpRecording();
        var transcripts = new List<string>();

        var voice = RenderComponent<BitAiVoiceInput>(p =>
        {
            p.Add(c => c.Transcriber, Transcriber(new(), "   "));
            p.Add(c => c.OnTranscript, t => transcripts.Add(t));
        });

        voice.WaitForElement("button").Click();
        await voice.InvokeAsync(() => voice.Instance._OnRecordingCompleted(null, "audio/webm", 0));

        voice.Find("button").Click();
        await voice.InvokeAsync(() => voice.Instance._OnRecordingCompleted(new FakeStreamReference([1]), "audio/webm", 1));

        Assert.IsEmpty(transcripts);
        Assert.AreEqual("Dictation stopped", voice.Find("[role=status]").TextContent.Trim());
    }

    [TestMethod]
    public void ARefusedOrMissingMicrophoneIsReportedThroughOnError()
    {
        foreach (var (code, name) in new[] { (-1, "not-allowed"), (-2, "audio-capture"), (-3, "recording-failed") })
        {
            Context.JSInterop.Setup<bool>(IsRecordingSupported, _ => true).SetResult(true);
            Context.JSInterop.Setup<int>(StartRecording, _ => true).SetResult(code);
            string? error = null;

            var voice = RenderComponent<BitAiVoiceInput>(p =>
            {
                p.Add(c => c.Transcriber, Transcriber(new()));
                p.Add(c => c.OnError, e => error = e);
            });
            voice.WaitForElement("button").Click();

            Assert.AreEqual(name, error);
            Assert.IsFalse(voice.Instance.IsListening);
            Assert.AreEqual($"Dictation failed: {name}", voice.Find("[role=status]").TextContent.Trim());
            Assert.AreEqual("Dictate", voice.Find("button").GetAttribute("aria-label"));
        }
    }

    [TestMethod]
    public async Task ATranscriberThatThrowsFailsWithoutShowingItsMessage()
    {
        SetUpRecording();
        string? error = null;

        var voice = RenderComponent<BitAiVoiceInput>(p =>
        {
            p.Add(c => c.Transcriber, (_, _, _) => throw new InvalidOperationException("secret server detail"));
            p.Add(c => c.OnError, e => error = e);
        });
        voice.WaitForElement("button").Click();
        voice.Find("button").Click();

        await voice.InvokeAsync(() => voice.Instance._OnRecordingCompleted(new FakeStreamReference([1]), "audio/webm", 1));

        Assert.AreEqual("transcription-failed", error);
        Assert.IsFalse(voice.Instance.IsTranscribing);
        Assert.AreEqual("Dictation failed: transcription-failed", voice.Find("[role=status]").TextContent.Trim());
        Assert.IsFalse(voice.Markup.Contains("secret", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task ARecordingOverTheSizeLimitIsNotTranscribed()
    {
        SetUpRecording();
        var recorded = new Recorded();
        string? error = null;

        var voice = RenderComponent<BitAiVoiceInput>(p =>
        {
            p.Add(c => c.Transcriber, Transcriber(recorded));
            p.Add(c => c.MaxRecordingSize, 3);
            p.Add(c => c.OnError, e => error = e);
        });
        voice.WaitForElement("button").Click();

        var audio = new FakeStreamReference([1, 2, 3, 4]);
        await voice.InvokeAsync(() => voice.Instance._OnRecordingCompleted(audio, "audio/webm", 4));

        Assert.AreEqual("too-large", error);
        Assert.IsNull(recorded.Bytes);
        Assert.IsTrue(audio.Disposed);
    }

    [TestMethod]
    public async Task ARegisteredSpeechToTextServiceIsUsedWithTheLanguage()
    {
        var service = new FakeSpeechToText();
        Context.Services.AddBitBlazorUIAIServices(ai => ai.UseSpeechToText(service));
        SetUpRecording();
        var transcripts = new List<string>();

        var voice = RenderComponent<BitAiVoiceInput>(p =>
        {
            p.Add(c => c.Language, "de-DE");
            p.Add(c => c.OnTranscript, t => transcripts.Add(t));
        });
        voice.WaitForElement("button").Click();

        Context.JSInterop.VerifyInvoke(StartRecording);

        await voice.InvokeAsync(() => voice.Instance._OnRecordingCompleted(new FakeStreamReference([1]), "audio/ogg; codecs=opus", 1));

        Assert.AreEqual("de-DE", service.Language);
        Assert.AreEqual("audio/ogg", service.ContentType);
        CollectionAssert.AreEqual(new[] { "from the service" }, transcripts);
    }

    [TestMethod]
    public void TheBrowserModePrefersTheSpeechApiAndFallsBackToTheTranscriber()
    {
        SetUpRecording(speechSupported: true);

        var withSpeech = RenderComponent<BitAiVoiceInput>(p =>
        {
            p.Add(c => c.Transcriber, Transcriber(new()));
            p.Add(c => c.Mode, BitAiDictationMode.Browser);
        });
        withSpeech.WaitForElement("button").Click();

        Context.JSInterop.VerifyInvoke(StartDictation);
        Context.JSInterop.VerifyNotInvoke(StartRecording);
        Context.JSInterop.VerifyNotInvoke(IsRecordingSupported);
    }

    [TestMethod]
    public void TheBrowserModeFallsBackToTheTranscriberWhereTheBrowserHasNoSpeechApi()
    {
        SetUpRecording(speechSupported: false);

        var voice = RenderComponent<BitAiVoiceInput>(p =>
        {
            p.Add(c => c.Transcriber, Transcriber(new()));
            p.Add(c => c.Mode, BitAiDictationMode.Browser);
        });
        voice.WaitForElement("button").Click();

        Context.JSInterop.VerifyInvoke(StartRecording);
        Context.JSInterop.VerifyNotInvoke(StartDictation);
    }

    [TestMethod]
    public void TheAutoModeFallsBackToTheSpeechApiWhereTheBrowserCannotRecord()
    {
        SetUpRecording(recordingSupported: false, speechSupported: true);

        var voice = RenderComponent<BitAiVoiceInput>(p => p.Add(c => c.Transcriber, Transcriber(new())));
        voice.WaitForElement("button").Click();

        Context.JSInterop.VerifyInvoke(StartDictation);
        Context.JSInterop.VerifyNotInvoke(StartRecording);
    }

    [TestMethod]
    public void TheRecordingModeWithoutATranscriberRendersNothing()
    {
        SetUpRecording();

        var voice = RenderComponent<BitAiVoiceInput>(p => p.Add(c => c.Mode, BitAiDictationMode.Recording));

        Assert.IsEmpty(voice.FindAll(".bit-aivci"));
        Context.JSInterop.VerifyNotInvoke(IsSpeechSupported);
        Context.JSInterop.VerifyNotInvoke(IsRecordingSupported);
    }

    [TestMethod]
    public void NothingIsRenderedWhereTheBrowserCanDoNeither()
    {
        SetUpRecording(recordingSupported: false, speechSupported: false);

        var voice = RenderComponent<BitAiVoiceInput>(p => p.Add(c => c.Transcriber, Transcriber(new())));

        Assert.IsEmpty(voice.FindAll(".bit-aivci"));
    }

    [TestMethod]
    public void DisablingWhileRecordingStopsAndKeepsTheTake()
    {
        SetUpRecording(handle: 4);

        var voice = RenderComponent<BitAiVoiceInput>(p => p.Add(c => c.Transcriber, Transcriber(new())));
        voice.WaitForElement("button").Click();

        voice.Render(p => p.Add(c => c.Disabled, true));

        var stop = Context.JSInterop.VerifyInvoke(StopRecording);
        Assert.AreEqual(4, (int)stop.Arguments[0]!);
        Assert.IsTrue((bool)stop.Arguments[1]!);
        Assert.IsFalse(voice.Instance.IsListening);
    }

    [TestMethod]
    public async Task DisposingWhileRecordingThrowsTheTakeAway()
    {
        SetUpRecording(handle: 6);

        var voice = RenderComponent<BitAiVoiceInput>(p => p.Add(c => c.Transcriber, Transcriber(new())));
        voice.WaitForElement("button").Click();

        await voice.Instance.DisposeAsync();

        var stop = Context.JSInterop.VerifyInvoke(StopRecording);
        Assert.AreEqual(6, (int)stop.Arguments[0]!);
        Assert.IsFalse((bool)stop.Arguments[1]!);
    }

    [TestMethod]
    public void TheComposerAndTheChatHandTheirTranscriberToTheVoiceInput()
    {
        Interactive();
        SetUpRecording();
        var transcriber = Transcriber(new());

        var composer = RenderComponent<BitAiPromptInput>(p =>
        {
            p.Add(c => c.AllowDictation, true);
            p.Add(c => c.Transcriber, transcriber);
        });

        Assert.AreSame(transcriber, composer.FindComponent<BitAiVoiceInput>().Instance.Transcriber);

        var chat = RenderComponent<BitAiChat>(p =>
        {
            p.Add(c => c.Session, new BitAiChatSession(new SilentTransport()));
            p.Add(c => c.AllowDictation, true);
            p.Add(c => c.Transcriber, transcriber);
        });

        Assert.AreSame(transcriber, chat.FindComponent<BitAiVoiceInput>().Instance.Transcriber);
    }

    private sealed class SilentTransport : IBitAiTransport
    {
        public async IAsyncEnumerable<BitAiEvent> StreamAsync(BitAiRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }
    }
}
