using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.ReadAloud;

/// <summary>
/// Covers reading replies aloud through the app's text-to-speech: BitAiReadAloud itself (synthesizing, playing, stopping,
/// keep reading, failures), the action BitAiMessageActions offers for it, and the reader BitAiChat makes and cascades.
/// </summary>
[TestClass]
public class BitAiReadAloudTests : BitAiTestBase
{
    private const string Prime = "BitBlazorUI.AiReadAloud.prime";
    private const string Play = "BitBlazorUI.AiReadAloud.play";
    private const string Stop = "BitBlazorUI.AiReadAloud.stop";

    private sealed class FakeTextToSpeech : IBitAiTextToSpeech
    {
        public List<(string Text, string? Language)> Calls { get; } = [];

        public Task<BitAiSpeechAudio?> SynthesizeAsync(string text, string? language, CancellationToken cancellationToken)
        {
            Calls.Add((text, language));
            return Task.FromResult<BitAiSpeechAudio?>(BitAiSpeechAudio.FromUrl("/speech.mp3"));
        }
    }

    private sealed class Synthesizer
    {
        public List<string> Texts { get; } = [];

        public Func<string, BitAiSpeechAudio?> Audio { get; set; } = _ => BitAiSpeechAudio.FromBytes([1, 2, 3], "audio/mpeg");

        public Task<BitAiSpeechAudio?> SynthesizeAsync(BitAiMessage reply, CancellationToken cancellationToken)
        {
            var text = reply.GetText();
            Texts.Add(text);
            return Task.FromResult(Audio(text));
        }
    }

    private IJSRuntime Js => Services.GetRequiredService<IJSRuntime>();

    private void SetUpPlayback(int handle = 3)
    {
        Context.JSInterop.Setup<int>(Play, _ => true).SetResult(handle);
    }

    private static BitAiMessage Reply(string text) => Assistant(new BitAiTextPart(text));

    [TestMethod]
    public async Task AReplyIsSynthesizedAndItsBytesPlayed()
    {
        SetUpPlayback(handle: 3);
        var synthesizer = new Synthesizer();
        var reader = new BitAiReadAloud(Js, synthesizer.SynthesizeAsync) { PlaybackRate = 1.25 };
        var changes = 0;
        reader.Changed += () => changes++;
        var reply = Reply("Hello there.");

        await reader.ReadAsync(reply);

        CollectionAssert.AreEqual(new[] { "Hello there." }, synthesizer.Texts);
        Context.JSInterop.VerifyInvoke(Prime);

        var play = Context.JSInterop.VerifyInvoke(Play);
        Assert.IsNull(play.Arguments[1]);
        Assert.IsInstanceOfType<DotNetStreamReference>(play.Arguments[2]);
        Assert.AreEqual("audio/mpeg", play.Arguments[3]);
        Assert.AreEqual(1.25, (double)play.Arguments[4]!);

        Assert.IsTrue(reader.IsPlaying);
        Assert.IsFalse(reader.IsLoading);
        Assert.IsTrue(reader.IsReading(reply));
        Assert.AreSame(reply, reader.Message);
        Assert.IsTrue(changes > 0);
    }

    [TestMethod]
    public async Task AnAddressIsPlayedAsTheServiceGaveIt()
    {
        SetUpPlayback();
        var service = new FakeTextToSpeech();
        var reader = new BitAiReadAloud(Js, service) { Language = "fr-FR" };

        await reader.ReadAsync(Reply("Bonjour."));

        Assert.AreEqual(("Bonjour.", "fr-FR"), service.Calls.Single());

        var play = Context.JSInterop.VerifyInvoke(Play);
        Assert.AreEqual("/speech.mp3", play.Arguments[1]);
        Assert.IsNull(play.Arguments[2]);
    }

    [TestMethod]
    public async Task ToggleStopsTheReplyBeingReadAndReadsAnyOther()
    {
        SetUpPlayback(handle: 4);
        var reader = new BitAiReadAloud(Js, new Synthesizer().SynthesizeAsync);
        var first = Reply("First.");
        var second = Reply("Second.");

        await reader.ToggleAsync(first);
        Assert.IsTrue(reader.IsReading(first));

        await reader.ToggleAsync(second);
        Assert.IsFalse(reader.IsReading(first));
        Assert.IsTrue(reader.IsReading(second));
        Assert.AreEqual(4, (int)Context.JSInterop.VerifyInvoke(Stop).Arguments[0]!);

        await reader.ToggleAsync(second);
        Assert.IsNull(reader.Message);
        Assert.IsFalse(reader.IsPlaying);
        Assert.AreEqual(2, Context.JSInterop.Invocations[Stop].Count);
    }

    [TestMethod]
    public async Task AReadingThatPlaysOutIsOverUnlessItKeepsReading()
    {
        SetUpPlayback(handle: 5);
        var reader = new BitAiReadAloud(Js, new Synthesizer().SynthesizeAsync);
        var reply = Reply("Done.");

        await reader.ReadAsync(reply);
        await reader._OnPlaybackEnded(5);

        Assert.IsNull(reader.Message);
        Assert.IsFalse(reader.IsPlaying);

        // Another reply finishing is not read: no reading follows the conversation.
        await reader.ReadNextAsync(Reply("Next."));
        Assert.AreEqual(1, Context.JSInterop.Invocations[Play].Count);
    }

    [TestMethod]
    public async Task KeepReadingReadsEveryReplyThatFinishesAfterIt()
    {
        SetUpPlayback(handle: 6);
        var synthesizer = new Synthesizer();
        var reader = new BitAiReadAloud(Js, synthesizer.SynthesizeAsync) { KeepReading = true };
        var first = Reply("First.");
        var next = Reply("Next.");

        await reader.ReadAsync(first);
        await reader._OnPlaybackEnded(6);

        Assert.IsTrue(reader.IsFollowing);
        Assert.IsTrue(reader.IsReading(first));

        await reader.ReadNextAsync(first); // the same reply again is not read twice
        await reader.ReadNextAsync(next);

        CollectionAssert.AreEqual(new[] { "First.", "Next." }, synthesizer.Texts);
        Assert.IsTrue(reader.IsReading(next));

        await reader.StopAsync();
        Assert.IsFalse(reader.IsFollowing);

        await reader.ReadNextAsync(Reply("After the stop."));
        Assert.HasCount(2, synthesizer.Texts);
    }

    [TestMethod]
    public async Task AReplyStillArrivingIsReadOnceItHasFinished()
    {
        SetUpPlayback();
        var synthesizer = new Synthesizer();
        var reader = new BitAiReadAloud(Js, synthesizer.SynthesizeAsync);
        var reply = Reply("Streaming...");
        reply.SetStatus(BitAiMessageStatus.Streaming);

        await reader.ReadAsync(reply);

        Assert.IsEmpty(synthesizer.Texts);
        Assert.IsTrue(reader.IsReading(reply));

        reply.SetStatus(BitAiMessageStatus.Complete);
        await reader.ReadNextAsync(reply);

        CollectionAssert.AreEqual(new[] { "Streaming..." }, synthesizer.Texts);
        Assert.IsTrue(reader.IsPlaying);
    }

    [TestMethod]
    public async Task NothingPlaysWhileTheMicrophoneCaptures()
    {
        // The player answers 0 when a dictation is recording: nothing is said into the microphone.
        SetUpPlayback(handle: 0);
        var reader = new BitAiReadAloud(Js, new Synthesizer().SynthesizeAsync);

        await reader.ReadAsync(Reply("Quiet, please."));

        Assert.IsFalse(reader.IsPlaying);
        Assert.IsNull(reader.Message);
    }

    [TestMethod]
    public async Task FailuresStopTheReadingAndAreReported()
    {
        var failures = new List<string>();

        var throwing = new BitAiReadAloud(Js, (_, _) => throw new InvalidOperationException("down")) { KeepReading = true };
        throwing.Failed += failures.Add;
        await throwing.ReadAsync(Reply("Text."));

        Assert.IsNull(throwing.Message);
        Assert.IsFalse(throwing.IsFollowing);
        Assert.IsFalse(throwing.IsLoading);

        SetUpPlayback(handle: -1);
        var refused = new BitAiReadAloud(Js, new Synthesizer().SynthesizeAsync);
        refused.Failed += failures.Add;
        await refused.ReadAsync(Reply("Text."));

        CollectionAssert.AreEqual(new[] { "synthesis-failed", "playback-failed" }, failures);
        Assert.IsFalse(refused.IsPlaying);
    }

    [TestMethod]
    public async Task NothingIsPlayedForAReplyWithNothingToRead()
    {
        SetUpPlayback();
        var synthesizer = new Synthesizer { Audio = _ => null };
        var reader = new BitAiReadAloud(Js, synthesizer.SynthesizeAsync);

        await reader.ReadAsync(Reply("   "));
        Assert.IsEmpty(synthesizer.Texts);

        await reader.ReadAsync(Reply("Only code."));
        CollectionAssert.AreEqual(new[] { "Only code." }, synthesizer.Texts);

        Context.JSInterop.VerifyNotInvoke(Play);
        Assert.IsNull(reader.Message);
    }

    [TestMethod]
    public async Task DisposingStopsWhatPlays()
    {
        SetUpPlayback(handle: 8);
        var reader = new BitAiReadAloud(Js, new Synthesizer().SynthesizeAsync);
        await reader.ReadAsync(Reply("Text."));

        await reader.DisposeAsync();

        Assert.AreEqual(8, (int)Context.JSInterop.VerifyInvoke(Stop).Arguments[0]!);

        await reader.ReadAsync(Reply("After."));
        Assert.AreEqual(1, Context.JSInterop.Invocations[Play].Count);
    }

    [TestMethod]
    public void TheActionsOfferNoReadAloudWithoutAReader()
    {
        var actions = RenderComponent<BitAiMessageActions>(p => p.Add(c => c.Message, Reply("answer")));

        Assert.IsEmpty(actions.FindAll("[aria-label='Read aloud']"));
    }

    [TestMethod]
    public async Task TheActionReadsTheMessageAndThenStopsIt()
    {
        SetUpPlayback(handle: 2);
        var reader = new BitAiReadAloud(Js, new Synthesizer().SynthesizeAsync);
        var reply = Reply("answer");

        var actions = RenderComponent<BitAiMessageActions>(p =>
        {
            p.Add(c => c.Message, reply);
            p.Add(c => c.ReadAloud, reader);
            p.Add(c => c.Classes, new BitAiMessageActionsClassStyles { ReadAloudButton = new() { Root = "read-class" } });
        });

        var button = actions.Find("[aria-label='Read aloud']");
        Assert.AreEqual("Read aloud", button.GetAttribute("title"));
        Assert.IsFalse(button.HasAttribute("aria-pressed"));
        Assert.IsTrue(button.ClassList.Contains("read-class"));
        Assert.IsNotNull(button.QuerySelector(".bit-icon-ai--ReadOutLoud"));

        button.Click();

        actions.WaitForAssertion(() => Assert.IsNotNull(actions.Find("[aria-label='Stop reading aloud']")));
        Assert.IsTrue(reader.IsReading(reply));
        Assert.IsNotNull(actions.Find("[aria-label='Stop reading aloud']").QuerySelector(".bit-icon-ai--StopSolid"));

        // The reading ends in the browser: the button follows the reader.
        await actions.InvokeAsync(() => reader._OnPlaybackEnded(2));
        actions.WaitForAssertion(() => Assert.IsNotNull(actions.Find("[aria-label='Read aloud']")));

        actions.Find("[aria-label='Read aloud']").Click();
        actions.WaitForAssertion(() => Assert.IsNotNull(actions.Find("[aria-label='Stop reading aloud']")));
        actions.Find("[aria-label='Stop reading aloud']").Click();

        actions.WaitForAssertion(() => Assert.IsNotNull(actions.Find("[aria-label='Read aloud']")));
        Assert.IsNull(reader.Message);
    }

    [TestMethod]
    public void TheActionTakesTheCascadedReader()
    {
        var reader = new BitAiReadAloud(Js, new Synthesizer().SynthesizeAsync);

        var cascade = RenderComponent<CascadingValue<BitAiReadAloud>>(p =>
        {
            p.Add(c => c.Value, reader);
            p.AddChildContent<BitAiMessageActions>(a => a.Add(c => c.Message, Reply("answer")));
        });

        Assert.IsNotNull(cascade.Find("[aria-label='Read aloud']"));
    }

    [TestMethod]
    public void AReplyWithNoTextOffersNoReading()
    {
        var reader = new BitAiReadAloud(Js, new Synthesizer().SynthesizeAsync);

        var actions = RenderComponent<BitAiMessageActions>(p =>
        {
            p.Add(c => c.Message, Assistant());
            p.Add(c => c.ReadAloud, reader);
        });

        Assert.IsEmpty(actions.FindAll("[aria-label='Read aloud']"));
    }

    [TestMethod]
    public void TheChatReadsAloudOnlyWithSomethingToSynthesizeThrough()
    {
        Interactive();
        var session = SessionWithReply();

        var without = RenderComponent<BitAiChat>(p =>
        {
            p.Add(c => c.Session, session);
            p.Add(c => c.AllowReadAloud, true);
        });

        Assert.IsNull(without.Instance.ReadAloud);
        Assert.IsEmpty(without.FindAll("[aria-label='Read aloud']"));

        var off = RenderComponent<BitAiChat>(p =>
        {
            p.Add(c => c.Session, session);
            p.Add(c => c.Synthesizer, new Synthesizer().SynthesizeAsync);
        });

        Assert.IsNull(off.Instance.ReadAloud);
        Assert.IsEmpty(off.FindAll("[aria-label='Read aloud']"));

        var on = RenderComponent<BitAiChat>(p =>
        {
            p.Add(c => c.Session, session);
            p.Add(c => c.AllowReadAloud, true);
            p.Add(c => c.KeepReadingAloud, true);
            p.Add(c => c.Synthesizer, new Synthesizer().SynthesizeAsync);
        });

        Assert.IsNotNull(on.Instance.ReadAloud);
        Assert.IsTrue(on.Instance.ReadAloud!.KeepReading);
        Assert.IsNotNull(on.Find("[aria-label='Read aloud']"));
    }

    [TestMethod]
    public void TheChatReadsThroughARegisteredTextToSpeechService()
    {
        Context.Services.AddBitBlazorUIAIServices(ai => ai.UseTextToSpeech(new FakeTextToSpeech()));
        Interactive();

        var chat = RenderComponent<BitAiChat>(p =>
        {
            p.Add(c => c.Session, SessionWithReply());
            p.Add(c => c.AllowReadAloud, true);
        });

        Assert.IsNotNull(chat.Instance.ReadAloud);
        Assert.IsNotNull(chat.Find("[aria-label='Read aloud']"));
    }

    [TestMethod]
    public void TheChatKeepsItsReaderAcrossRendersOfAMethodGroup()
    {
        Interactive();
        var synthesizer = new Synthesizer();

        var chat = RenderComponent<BitAiChat>(p =>
        {
            p.Add(c => c.Session, SessionWithReply());
            p.Add(c => c.AllowReadAloud, true);
            p.Add(c => c.Synthesizer, synthesizer.SynthesizeAsync);
        });
        var reader = chat.Instance.ReadAloud;

        // A method group handed over in markup is a new delegate on every render of the host.
        chat.Render(p => p.Add(c => c.Synthesizer, synthesizer.SynthesizeAsync));

        Assert.AreSame(reader, chat.Instance.ReadAloud);
    }

    [TestMethod]
    public async Task KeepReadingAloudInTheChatReadsTheNextReplyAsItsTurnEnds()
    {
        SetUpPlayback(handle: 7);
        Interactive();
        var synthesizer = new Synthesizer();
        var session = new BitAiChatSession(new ReplyTransport("The next reply."));
        session.Conversation.AddMessage(BitAiRole.User, "Hi");
        var first = session.Conversation.AddMessage(BitAiRole.Assistant, "The first reply.");

        var chat = RenderComponent<BitAiChat>(p =>
        {
            p.Add(c => c.Session, session);
            p.Add(c => c.AllowReadAloud, true);
            p.Add(c => c.KeepReadingAloud, true);
            p.Add(c => c.Synthesizer, synthesizer.SynthesizeAsync);
        });

        await chat.InvokeAsync(() => chat.Instance.ReadAloud!.ReadAsync(first));
        await chat.InvokeAsync(() => chat.Instance.ReadAloud!._OnPlaybackEnded(7));

        await chat.InvokeAsync(() => session.SubmitAsync("And then?"));

        chat.WaitForAssertion(() => CollectionAssert.AreEqual(new[] { "The first reply.", "The next reply." }, synthesizer.Texts));
    }

    private static BitAiChatSession SessionWithReply()
    {
        var session = new BitAiChatSession(new ReplyTransport("unused"));
        session.Conversation.AddMessage(BitAiRole.User, "Hi");
        session.Conversation.AddMessage(BitAiRole.Assistant, "Hello!");
        return session;
    }

    private sealed class ReplyTransport(string reply) : IBitAiTransport
    {
        public async IAsyncEnumerable<BitAiEvent> StreamAsync(BitAiRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return new BitAiMessageStartedEvent();
            yield return new BitAiTextDeltaEvent(reply);
            yield return new BitAiMessageCompletedEvent();
            await Task.CompletedTask;
        }
    }
}
