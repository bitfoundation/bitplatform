using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.JSInterop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.PromptInput;

/// <summary>
/// Covers a draft key that changes while the composer is on the page: what the composer holds stays with the key it was
/// written under, unless the change is the same conversation getting its key.
/// </summary>
[TestClass]
public class BitAiPromptInputDraftKeyTests : BitAiTestBase
{
    private const string ReadDraft = "BitBlazorUI.AiPromptInput.readDraft";
    private const string WriteDraft = "BitBlazorUI.AiPromptInput.writeDraft";
    private const string ClearDraft = "BitBlazorUI.AiPromptInput.clearDraft";

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

    private static BitAiComposerFile File(string name, FakeStreamReference content) =>
        new(name, "application/octet-stream", content.Length) { Content = content };

    private string?[] Writes(string key) =>
        Context.JSInterop.Invocations[WriteDraft].Where(i => Equals(i.Arguments[0], key)).Select(i => (string?)i.Arguments[1]).ToArray();

    [TestMethod]
    public async Task AnotherKeyKeepsTheTextAndTheFilesOfTheOldOneAndBringsThemBack()
    {
        Context.JSInterop.Setup<string?>(ReadDraft, i => Equals(i.Arguments[0], "chat-1")).SetResult(null);
        Context.JSInterop.Setup<string?>(ReadDraft, i => Equals(i.Arguments[0], "chat-2")).SetResult("draft of two");
        var content = new FakeStreamReference([1, 2]);

        // Kept in the browser rather than buffered, so its stream is held until the file is sent or released.
        var composer = RenderComponent<BitAiPromptInput>(p => p.Add(c => c.DraftKey, "chat-1").Add(c => c.MaxInlineSize, 0L));
        var instance = composer.Instance;

        composer.Find("textarea").Input("typed for one");
        await composer.InvokeAsync(() => composer.Instance._OnDroppedFile(File("notes.bin", content)));
        Assert.HasCount(1, composer.Instance.Attachments);

        composer.Render(p => p.Add(c => c.DraftKey, "chat-2"));

        composer.WaitForAssertion(() => Assert.AreEqual("draft of two", composer.Find("textarea").GetAttribute("value")));
        Assert.AreSame(instance, composer.Instance);
        Assert.IsEmpty(composer.Instance.Attachments);
        Assert.IsEmpty(composer.FindComponents<BitAiAttachmentChip>());
        Assert.AreEqual("typed for one", Writes("chat-1").Last());
        Assert.IsFalse(Writes("chat-2").Contains("typed for one"));
        // The file is parked, not released.
        Assert.IsFalse(content.Disposed);

        composer.Find("textarea").Input("draft of two, edited");

        composer.Render(p => p.Add(c => c.DraftKey, "chat-1"));

        composer.WaitForAssertion(() => Assert.AreEqual("typed for one", composer.Find("textarea").GetAttribute("value")));
        Assert.AreEqual("notes.bin", Assert.ContainsSingle(composer.Instance.Attachments).Name);
        Assert.AreEqual("draft of two, edited", Writes("chat-2").Last());
        // What the composer kept for the key wins over the storage, which it is not read from again.
        Assert.HasCount(1, Context.JSInterop.Invocations[ReadDraft].Where(i => Equals(i.Arguments[0], "chat-1")));
        Assert.IsFalse(content.Disposed);
    }

    [TestMethod]
    public async Task AFileParkedUnderAnotherKeyKeepsUploading()
    {
        var release = new TaskCompletionSource();
        var cancelled = false;

        var composer = RenderComponent<BitAiPromptInput>(p => p
            .Add(c => c.DraftKey, "chat-1")
            .Add(c => c.AttachmentUploader, async (attachment, _, ct) =>
            {
                using var registration = ct.Register(() => cancelled = true);
                await release.Task;
                return attachment with { Url = "https://files.example/notes.bin", Data = null };
            }));

        await composer.InvokeAsync(() => composer.Instance._OnDroppedFile(File("notes.bin", new FakeStreamReference([1]))));
        Assert.AreEqual(BitAiAttachmentStatus.Uploading, composer.Instance.Attachments[0].Status);

        composer.Render(p => p.Add(c => c.DraftKey, "chat-2"));
        Assert.IsEmpty(composer.Instance.Attachments);

        release.SetResult();
        await Task.Yield();

        composer.Render(p => p.Add(c => c.DraftKey, "chat-1"));

        composer.WaitForAssertion(() =>
        {
            var attachment = Assert.ContainsSingle(composer.Instance.Attachments);
            Assert.AreEqual(BitAiAttachmentStatus.Ready, attachment.Status);
            Assert.AreEqual("https://files.example/notes.bin", attachment.Url);
        });
        Assert.IsFalse(cancelled);
    }

    [TestMethod]
    public async Task AFileParkedUnderAnotherKeyIsReleasedWithTheComposer()
    {
        var content = new FakeStreamReference([1, 2]);
        var composer = RenderComponent<BitAiPromptInput>(p => p.Add(c => c.DraftKey, "chat-1").Add(c => c.MaxInlineSize, 0L));

        await composer.InvokeAsync(() => composer.Instance._OnDroppedFile(File("notes.bin", content)));
        composer.Render(p => p.Add(c => c.DraftKey, "chat-2"));

        await composer.InvokeAsync(() => composer.Instance.DisposeAsync().AsTask());

        Assert.IsTrue(content.Disposed);
    }

    [TestMethod]
    public async Task AKeyRenamedWhileASubmissionIsInFlightKeepsWhatTheComposerHolds()
    {
        var gate = new TaskCompletionSource();
        var content = new FakeStreamReference([1]);

        var composer = RenderComponent<BitAiPromptInput>(p => p
            .Add(c => c.DraftKey, "chat-new")
            .Add(c => c.MaxInlineSize, 0L)
            .Add(c => c.OnSubmit, async _ => await gate.Task));
        var instance = composer.Instance;

        composer.Find("textarea").Input("first");
        composer.Find("textarea").KeyDown(Key.Enter);
        Assert.AreEqual("chat-new", Context.JSInterop.VerifyInvoke(ClearDraft).Arguments[0]);

        // Typed while the reply streams, and the conversation named meanwhile.
        composer.Find("textarea").Input("second");
        await composer.InvokeAsync(() => composer.Instance._OnDroppedFile(File("notes.bin", content)));

        composer.Render(p => p.Add(c => c.DraftKey, "chat-42"));

        Assert.AreSame(instance, composer.Instance);
        Assert.AreEqual("second", composer.Find("textarea").GetAttribute("value"));
        Assert.AreEqual("notes.bin", Assert.ContainsSingle(composer.Instance.Attachments).Name);
        Assert.AreEqual("second", Writes("chat-42").Last());
        Assert.AreEqual("chat-new", Context.JSInterop.Invocations[ClearDraft].Last().Arguments[0]);
        Assert.IsEmpty(Context.JSInterop.Invocations[ReadDraft].Where(i => Equals(i.Arguments[0], "chat-42")));

        gate.SetResult();
        composer.WaitForAssertion(() => Assert.AreEqual("second", composer.Find("textarea").GetAttribute("value")));
        Assert.IsFalse(content.Disposed);
    }

    [TestMethod]
    public void ASubmissionTurnedDownAfterItsKeyWasRenamedIsPutBackUnderTheNewKey()
    {
        var gate = new TaskCompletionSource();

        var composer = RenderComponent<BitAiPromptInput>(p => p
            .Add(c => c.DraftKey, "chat-new")
            .Add(c => c.OnSubmit, async s =>
            {
                await gate.Task;
                s.Reject();
            }));

        composer.Find("textarea").Input("first");
        composer.Find("textarea").KeyDown(Key.Enter);

        composer.Render(p => p.Add(c => c.DraftKey, "chat-42"));
        gate.SetResult();

        composer.WaitForAssertion(() => Assert.AreEqual("first", composer.Find("textarea").GetAttribute("value")));
        Assert.AreEqual("first", Writes("chat-42").Last());
    }

    [TestMethod]
    public async Task ASubmissionTurnedDownOnceTheComposerIsGoneIsPutBackIntoTheStorage()
    {
        var gate = new TaskCompletionSource();
        Context.JSInterop.Setup<string?>(ReadDraft, _ => true).SetResult(null);

        var composer = RenderComponent<BitAiPromptInput>(p => p
            .Add(c => c.DraftKey, "chat-1")
            .Add(c => c.OnSubmit, async s =>
            {
                await gate.Task;
                s.Reject();
            }));

        composer.Find("textarea").Input("first");
        composer.Find("textarea").KeyDown(Key.Enter);

        await composer.InvokeAsync(() => composer.Instance.DisposeAsync().AsTask());
        gate.SetResult();

        composer.WaitForAssertion(() => Assert.AreEqual("first", Writes("chat-1").Last()));
    }

    [TestMethod]
    public void AKeySetWhereThereWasNoneKeepsTheTextTypedWithoutOne()
    {
        Context.JSInterop.Setup<string?>(ReadDraft, _ => true).SetResult("stale");

        var composer = RenderComponent<BitAiPromptInput>();

        composer.Find("textarea").Input("typed before saving");
        composer.Render(p => p.Add(c => c.DraftKey, "chat-1"));

        Assert.AreEqual("typed before saving", composer.Find("textarea").GetAttribute("value"));
        Assert.AreEqual("typed before saving", Writes("chat-1").Last());
        Assert.IsEmpty(Context.JSInterop.Invocations[ReadDraft]);
    }

    [TestMethod]
    public void AKeySetOnAnEmptyComposerRestoresItsDraft()
    {
        Context.JSInterop.Setup<string?>(ReadDraft, i => Equals(i.Arguments[0], "chat-1")).SetResult("saved text");

        var composer = RenderComponent<BitAiPromptInput>();
        composer.Render(p => p.Add(c => c.DraftKey, "chat-1"));

        composer.WaitForAssertion(() => Assert.AreEqual("saved text", composer.Find("textarea").GetAttribute("value")));
        // Nothing was written over the draft before it was read.
        Assert.IsEmpty(Writes("chat-1").Where(w => string.IsNullOrEmpty(w)));
        Assert.IsEmpty(Context.JSInterop.Invocations[ClearDraft]);
    }

    [TestMethod]
    public void AKeyTakenAwayKeepsTheText()
    {
        var composer = RenderComponent<BitAiPromptInput>(p => p.Add(c => c.DraftKey, "chat-1"));

        composer.Find("textarea").Input("typed");
        composer.Render(p => p.Add(c => c.DraftKey, (string?)null));

        Assert.AreEqual("typed", composer.Find("textarea").GetAttribute("value"));
        Assert.AreEqual("chat-1", Context.JSInterop.Invocations[ClearDraft].Last().Arguments[0]);

        var writes = Context.JSInterop.Invocations[WriteDraft].Count;
        composer.Find("textarea").Input("typed more");
        Assert.HasCount(writes, Context.JSInterop.Invocations[WriteDraft]);
    }
}
