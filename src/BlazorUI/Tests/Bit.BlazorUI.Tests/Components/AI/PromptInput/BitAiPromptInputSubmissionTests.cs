using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Bit.BlazorUI.Tests.Components.AI.Core;
using Bunit;
using Microsoft.JSInterop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.PromptInput;

/// <summary>
/// Covers what happens to the prompt once it is sent: put back when it could not be, its files uploaded as they are
/// staged, and sent in place of a turn in flight when the session takes it so.
/// </summary>
[TestClass]
public class BitAiPromptInputSubmissionTests : BitAiTestBase
{
    private sealed class FakeStreamReference(byte[] bytes) : IJSStreamReference
    {
        public bool Disposed { get; private set; }

        public long Length => bytes.Length;

        public ValueTask<Stream> OpenReadStreamAsync(long maxAllowedSize = 512000, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<Stream>(new MemoryStream(bytes));

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => throw new NotSupportedException();

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            throw new NotSupportedException();

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }

    private static BitAiComposerFile File(string name, byte[] bytes) =>
        new(name, "application/octet-stream", bytes.Length) { Content = new FakeStreamReference(bytes) };

    [TestMethod]
    public async Task ASubmissionTheHandlerRejectsIsPutBack()
    {
        var composer = RenderComponent<BitAiPromptInput>(p => p
            .Add(c => c.Value, "  hello  ")
            .Add(c => c.OnSubmit, s => s.Reject()));

        await composer.InvokeAsync(() => composer.Instance._OnDroppedFile(File("notes.bin", [1, 2])));
        Assert.ContainsSingle(composer.Instance.Attachments);

        composer.Find(".bit-aipri-snd").Click();

        composer.WaitForAssertion(() => Assert.AreEqual("  hello  ", composer.Find("textarea").GetAttribute("value")));
        Assert.AreEqual("notes.bin", Assert.ContainsSingle(composer.Instance.Attachments).Name);
        Assert.ContainsSingle(composer.FindComponents<BitAiAttachmentChip>());
    }

    [TestMethod]
    public void ASubmissionTheHandlerTakesIsNotPutBack()
    {
        BitAiPromptSubmission? taken = null;
        var composer = RenderComponent<BitAiPromptInput>(p => p
            .Add(c => c.Value, "hello")
            .Add(c => c.OnSubmit, s => taken = s));

        composer.Find(".bit-aipri-snd").Click();

        Assert.IsFalse(taken!.IsRejected);
        Assert.AreEqual(string.Empty, composer.Find("textarea").GetAttribute("value"));
    }

    [TestMethod]
    public async Task APromptTheSessionDoesNotTakeIsPutBack()
    {
        var transport = new HangingThenAnswering();
        await using var session = new BitAiChatSession(transport, null, new() { Pacing = null });
        var turn = session.SubmitAsync("first", cancellationToken: TestContext.CancellationToken);
        await WaitForAsync(() => session.IsGenerating);

        // Rendered with no session, so it offers to send while the session is busy - as a composer handed a stale state
        // would, a moment before the turn it does not know of yet.
        var composer = RenderComponent<BitAiPromptInput>(p => p
            .Add(c => c.Value, "second")
            .Add(c => c.Generating, false)
            .Add(c => c.Session, session));

        composer.Find(".bit-aipri-snd").Click();

        composer.WaitForAssertion(() => Assert.AreEqual("second", composer.Find("textarea").GetAttribute("value")));
        Assert.HasCount(2, session.Conversation.Messages);

        session.Stop();
        await turn;
    }

    [TestMethod]
    public async Task AFileIsUploadedAsItIsStagedAndTheUploadedOneIsSent()
    {
        var release = new TaskCompletionSource();
        IProgress<double>? reporter = null;
        BitAiPromptSubmission? sent = null;

        var composer = RenderComponent<BitAiPromptInput>(p => p
            .Add(c => c.Value, "look")
            .Add(c => c.OnSubmit, s => sent = s)
            .Add(c => c.AttachmentUploader, async (attachment, progress, ct) =>
            {
                reporter = progress;
                await release.Task.WaitAsync(ct);
                return attachment with { Url = "https://files.example/notes.bin", Data = null };
            }));

        await composer.InvokeAsync(() => composer.Instance._OnDroppedFile(File("notes.bin", [1, 2])));

        var staged = Assert.ContainsSingle(composer.Instance.Attachments);
        Assert.AreEqual(BitAiAttachmentStatus.Uploading, staged.Status);
        Assert.AreEqual("true", composer.Find(".bit-aipri-snd").GetAttribute("aria-disabled"));

        reporter!.Report(0.5);
        composer.WaitForAssertion(() => Assert.AreEqual(0.5, composer.Instance.Attachments[0].Progress));
        composer.WaitForAssertion(() => Assert.Contains("50%", composer.Find(".bit-aiach").TextContent));

        release.SetResult();
        composer.WaitForAssertion(() => Assert.AreEqual(BitAiAttachmentStatus.Ready, composer.Instance.Attachments[0].Status));

        composer.Find(".bit-aipri-snd").Click();

        var attachment = Assert.ContainsSingle(sent!.Attachments);
        Assert.AreEqual("https://files.example/notes.bin", attachment.Url);
        Assert.IsNull(attachment.Data);
        Assert.AreEqual(staged.AttachmentId, attachment.AttachmentId);
    }

    [TestMethod]
    public async Task AFailedUploadIsShownAndHoldsTheSendBackUntilTheFileIsTakenOff()
    {
        var sent = 0;
        var composer = RenderComponent<BitAiPromptInput>(p => p
            .Add(c => c.Value, "look")
            .Add(c => c.OnSubmit, _ => sent++)
            .Add(c => c.AttachmentUploader, (_, _, _) => Task.FromException<BitAiAttachment>(new IOException("disk full on 10.0.0.7"))));

        await composer.InvokeAsync(() => composer.Instance._OnDroppedFile(File("notes.bin", [1])));

        composer.WaitForAssertion(() => Assert.AreEqual(BitAiAttachmentStatus.Failed, composer.Instance.Attachments[0].Status));
        var chip = composer.Find(".bit-aiach");
        Assert.Contains(BitAiStrings.Default.UploadFailed, chip.TextContent);
        Assert.DoesNotContain("10.0.0.7", composer.Markup);
        Assert.AreEqual("true", composer.Find(".bit-aipri-snd").GetAttribute("aria-disabled"));

        composer.Find(".bit-aiach button").Click();

        composer.WaitForAssertion(() => Assert.IsEmpty(composer.Instance.Attachments));
        composer.Find(".bit-aipri-snd").Click();
        Assert.AreEqual(1, sent);
    }

    [TestMethod]
    public async Task TakingAFileOffCancelsItsUpload()
    {
        var cancelled = new TaskCompletionSource();
        var composer = RenderComponent<BitAiPromptInput>(p => p
            .Add(c => c.AttachmentUploader, async (attachment, _, ct) =>
            {
                using var registration = ct.Register(() => cancelled.TrySetResult());
                await Task.Delay(Timeout.Infinite, ct);
                return attachment;
            }));

        await composer.InvokeAsync(() => composer.Instance._OnDroppedFile(File("notes.bin", [1])));
        composer.Find(".bit-aiach button").Click();

        await cancelled.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);
        Assert.IsEmpty(composer.Instance.Attachments);
    }

    [TestMethod]
    public async Task WhileATurnIsInFlightTheButtonSendsInItsPlaceWhenTheSessionSaysSo()
    {
        var transport = new HangingThenAnswering();
        await using var session = new BitAiChatSession(transport, null, new() { Pacing = null, InterruptOnSubmit = true });
        var first = session.SubmitAsync("first", cancellationToken: TestContext.CancellationToken);
        await WaitForAsync(() => session.IsGenerating);

        var composer = RenderComponent<BitAiPromptInput>(p => p.Add(c => c.Session, session));

        // Nothing typed: the button stops the turn.
        Assert.IsTrue(composer.Find(".bit-aipri-snd .bit-btn-icn").ClassList.Contains("bit-icon-ai--StopSolid"));

        composer.Find("textarea").Input("second");

        Assert.IsTrue(composer.Find(".bit-aipri-snd .bit-btn-icn").ClassList.Contains("bit-icon-ai--Send"));

        composer.Find(".bit-aipri-snd").Click();

        await first;
        await WaitForAsync(() => session.Conversation.Messages.Count == 4 && session.IsGenerating is false);
        Assert.AreEqual(BitAiMessageStatus.Cancelled, session.Conversation.Messages[1].Status);
        Assert.AreEqual("answer to second", session.Conversation.Messages[3].GetText());
    }

    [TestMethod]
    public async Task WhileATurnIsInFlightTheButtonStopsByDefault()
    {
        var transport = new HangingThenAnswering();
        await using var session = new BitAiChatSession(transport, null, new() { Pacing = null });
        var first = session.SubmitAsync("first", cancellationToken: TestContext.CancellationToken);
        await WaitForAsync(() => session.IsGenerating);

        var composer = RenderComponent<BitAiPromptInput>(p => p.Add(c => c.Session, session));
        composer.Find("textarea").Input("second");

        Assert.IsTrue(composer.Find(".bit-aipri-snd .bit-btn-icn").ClassList.Contains("bit-icon-ai--StopSolid"));

        composer.Find(".bit-aipri-snd").Click();
        await first;

        Assert.AreEqual("second", composer.Find("textarea").GetAttribute("value"));
        Assert.HasCount(2, session.Conversation.Messages);
    }

    [TestMethod]
    public async Task WhileATurnIsInFlightTheButtonStaysAStopButtonWhileWhatIsTypedCannotGo()
    {
        var transport = new HangingThenAnswering();
        await using var session = new BitAiChatSession(transport, null, new() { Pacing = null, InterruptOnSubmit = true });
        var first = session.SubmitAsync("first", cancellationToken: TestContext.CancellationToken);
        await WaitForAsync(() => session.IsGenerating);

        var composer = RenderComponent<BitAiPromptInput>(p => p
            .Add(c => c.Session, session)
            .Add(c => c.MaxLength, 3));

        // Over MaxLength, the text cannot be sent - so the one button still stops the turn rather than sitting disabled.
        composer.Find("textarea").Input("too long");

        var button = composer.Find(".bit-aipri-snd");
        Assert.IsTrue(composer.Find(".bit-aipri-snd .bit-btn-icn").ClassList.Contains("bit-icon-ai--StopSolid"));
        Assert.AreNotEqual("true", button.GetAttribute("aria-disabled"));

        button.Click();
        await first;

        Assert.AreEqual(BitAiMessageStatus.Cancelled, session.Conversation.Messages[1].Status);
        Assert.HasCount(2, session.Conversation.Messages);
        Assert.AreEqual("too long", composer.Find("textarea").GetAttribute("value"));
    }

    private static async Task WaitForAsync(Func<bool> condition, int timeoutMs = 10_000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (!condition() && DateTime.UtcNow < deadline) await Task.Delay(5);
        Assert.IsTrue(condition(), "The expected condition was not reached in time.");
    }

    /// <summary>Streams the first request until it is stopped, and answers every later one at once.</summary>
    private sealed class HangingThenAnswering : IBitAiTransport
    {
        private int _started;

        public async IAsyncEnumerable<BitAiEvent> StreamAsync(BitAiRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref _started) == 1)
            {
                yield return new BitAiMessageStartedEvent();

                while (true)
                {
                    await Task.Delay(10, cancellationToken);
                    yield return new BitAiTextDeltaEvent(".");
                }
            }

            yield return new BitAiTextDeltaEvent($"answer to {request.Messages[^1].GetText()}");
            yield return new BitAiMessageCompletedEvent();
        }
    }

    public global::Microsoft.VisualStudio.TestTools.UnitTesting.TestContext TestContext { get; set; }
}
