using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.PromptInput;

[TestClass]
public class BitAiPromptInputAttachmentTests : BitAiTestBase
{
    private sealed class FakeStreamReference(byte[] bytes) : IJSStreamReference
    {
        public bool Disposed { get; private set; }

        public int Opens { get; private set; }

        public long Length => bytes.Length;

        public ValueTask<Stream> OpenReadStreamAsync(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
        {
            Opens++;
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

    private static BitAiComposerFile File(string name, string type, byte[] bytes, string? preview = null) =>
        new(name, type, bytes.Length) { Content = new FakeStreamReference(bytes), PreviewUrl = preview };

    [TestMethod]
    public async Task AFileBeyondTheCountLimitIsRejectedAsTooMany()
    {
        var rejections = new List<BitAiAttachmentRejection>();
        var composer = RenderComponent<BitAiPromptInput>(p => p
            .Add(c => c.MaxAttachments, 1)
            .Add(c => c.OnAttachmentRejected, r => rejections.Add(r)));

        await composer.InvokeAsync(() => composer.Instance._OnDroppedFile(File("one.bin", "application/octet-stream", [1])));
        await composer.InvokeAsync(() => composer.Instance._OnDroppedFile(File("two.bin", "application/octet-stream", [2])));

        Assert.HasCount(1, composer.Instance.Attachments);
        var rejection = Assert.ContainsSingle(rejections);
        Assert.AreEqual(BitAiAttachmentRejectionReason.TooMany, rejection.Reason);
        Assert.AreEqual("two.bin", rejection.FileName);
        StringAssert.Contains(composer.Find(".bit-aipri-ntc").TextContent, "Up to 1 files can be attached");
    }

    [TestMethod]
    public void PickingMoreFilesThanAllowedRejectsTheExcessWithoutLosingTheRest()
    {
        var rejections = new List<BitAiAttachmentRejection>();
        var composer = RenderComponent<BitAiPromptInput>(p => p
            .Add(c => c.MaxAttachments, 2)
            .Add(c => c.OnAttachmentRejected, r => rejections.Add(r)));

        composer.FindComponent<InputFile>().UploadFiles(
            InputFileContent.CreateFromBinary([1], "a.bin"),
            InputFileContent.CreateFromBinary([2], "b.bin"),
            InputFileContent.CreateFromBinary([3], "c.bin"));

        composer.WaitForAssertion(() => Assert.HasCount(2, composer.Instance.Attachments));
        Assert.AreEqual(BitAiAttachmentRejectionReason.TooMany, Assert.ContainsSingle(rejections).Reason);
        Assert.AreEqual("c.bin", rejections[0].FileName);
    }

    [TestMethod]
    public async Task ALargeFileStaysInTheBrowserUntilSomethingOpensIt()
    {
        var content = new FakeStreamReference([1, 2, 3, 4]);
        var composer = RenderComponent<BitAiPromptInput>(p => p.Add(c => c.MaxInlineSize, 2));

        await composer.InvokeAsync(() => composer.Instance._OnDroppedFile(
            new BitAiComposerFile("big.bin", "application/octet-stream", 4) { Content = content }));

        var attachment = Assert.ContainsSingle(composer.Instance.Attachments);
        Assert.IsFalse(attachment.HasInlineData);
        Assert.IsNull(attachment.Url);
        Assert.IsNotNull(attachment.ContentFactory);
        Assert.AreEqual(4L, attachment.Size);
        Assert.AreEqual(0, content.Opens);
        Assert.IsFalse(content.Disposed);

        await using var stream = await attachment.OpenReadAsync();
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4 }, buffer.ToArray());
        Assert.AreEqual(1, content.Opens);
    }

    [TestMethod]
    public async Task RemovingALazyAttachmentReleasesItsBrowserStreamAndPreview()
    {
        var content = new FakeStreamReference([1, 2, 3, 4]);
        var composer = RenderComponent<BitAiPromptInput>(p => p.Add(c => c.MaxInlineSize, 2));

        await composer.InvokeAsync(() => composer.Instance._OnDroppedFile(
            new BitAiComposerFile("shot.png", "image/png", 4) { Content = content, PreviewUrl = "blob:preview" }));

        Assert.AreEqual("blob:preview", Assert.ContainsSingle(composer.Instance.Attachments).ThumbnailUrl);
        Assert.AreEqual("blob:preview", composer.Find("img.bit-aiach-thm").GetAttribute("src"));

        composer.Find(".bit-aiach button[aria-label='Remove shot.png']").Click();

        Assert.IsEmpty(composer.Instance.Attachments);
        Assert.IsTrue(content.Disposed);
        Assert.AreEqual("blob:preview", Context.JSInterop.VerifyInvoke("BitBlazorUI.AiPromptInput.revokePreview").Arguments[0]);
    }

    [TestMethod]
    public async Task DisposingTheComposerReleasesStagedFilesButNotSubmittedOnes()
    {
        var staged = new FakeStreamReference([1, 2, 3, 4]);
        var submitted = new FakeStreamReference([5, 6, 7, 8]);
        var submissions = new List<BitAiPromptSubmission>();
        var composer = RenderComponent<BitAiPromptInput>(p => p
            .Add(c => c.MaxInlineSize, 2)
            .Add(c => c.OnSubmit, s => submissions.Add(s)));

        await composer.InvokeAsync(() => composer.Instance._OnDroppedFile(
            new BitAiComposerFile("sent.bin", "application/octet-stream", 4) { Content = submitted }));
        composer.Find(".bit-aipri-snd").Click();
        Assert.HasCount(1, submissions);

        await composer.InvokeAsync(() => composer.Instance._OnDroppedFile(
            new BitAiComposerFile("left.bin", "application/octet-stream", 4) { Content = staged }));

        await Context.DisposeComponentsAsync();

        Assert.IsTrue(staged.Disposed);
        Assert.IsFalse(submitted.Disposed);
    }

    [TestMethod]
    public async Task ASmallFileIsBufferedAndItsStreamReleased()
    {
        var content = new FakeStreamReference([1, 2]);
        var composer = RenderComponent<BitAiPromptInput>(p => p.Add(c => c.MaxInlineSize, 2));

        await composer.InvokeAsync(() => composer.Instance._OnDroppedFile(
            new BitAiComposerFile("small.bin", "application/octet-stream", 2) { Content = content }));

        var attachment = Assert.ContainsSingle(composer.Instance.Attachments);
        Assert.IsTrue(attachment.HasInlineData);
        Assert.IsNull(attachment.ContentFactory);
        Assert.IsTrue(content.Disposed);
    }

    [TestMethod]
    public void TheAttachControlIsARealButtonThatOpensTheHiddenInput()
    {
        var composer = RenderComponent<BitAiPromptInput>();

        var button = composer.Find("button.bit-aipri-atb");
        Assert.AreEqual("Attach files", button.GetAttribute("aria-label"));
        Assert.IsTrue(button.ClassList.Contains("bit-btn-dft-ter"));

        var input = composer.Find("input[type=file]");
        Assert.AreEqual("-1", input.GetAttribute("tabindex"));
        Assert.AreEqual("true", input.GetAttribute("aria-hidden"));

        button.Click();

        Context.JSInterop.VerifyInvoke("BitBlazorUI.AiPromptInput.click");
    }

    [TestMethod]
    public void TheFileInputIsHiddenWithoutLeavingThePage()
    {
        var component = RenderComponent<BitAiPromptInput>();

        var input = component.Find("input[type=file]");

        Assert.IsFalse((input.GetAttribute("style") ?? string.Empty).Contains("display:none"));
        Assert.IsTrue(input.ClassList.Contains("bit-aipri-fil"));
    }

    [TestMethod]
    public void AttachmentsAreHiddenForAModelThatTakesOnlyText()
    {
        var textOnly = new BitAiModel("t", "Text") { Capabilities = BitAiModelCapabilities.Streaming | BitAiModelCapabilities.Tools };
        var composer = RenderComponent<BitAiPromptInput>(p => p.Add(c => c.Model, textOnly).Add(c => c.AllowDictation, true));

        Assert.IsEmpty(composer.FindAll("input[type=file]"));
        Assert.IsEmpty(composer.FindAll(".bit-aipri-atb"));
        Assert.IsEmpty(composer.FindComponents<BitAiVoiceInput>());
    }

    [TestMethod]
    public void TheAcceptedTypesFollowTheModelCapabilities()
    {
        var composer = RenderComponent<BitAiPromptInput>(p => p.Add(c => c.Capabilities, BitAiModelCapabilities.Vision));

        Assert.AreEqual("image/*", composer.Find("input[type=file]").GetAttribute("accept"));

        composer.Render(p => p.Add(c => c.Capabilities, BitAiModelCapabilities.Vision | BitAiModelCapabilities.Documents));
        StringAssert.Contains(composer.Find("input[type=file]").GetAttribute("accept"), "application/pdf");

        composer.Render(p => p.Add(c => c.Accept, ".csv"));
        Assert.AreEqual(".csv", composer.Find("input[type=file]").GetAttribute("accept"));
    }

    [TestMethod]
    public async Task ADroppedFileTheModelCannotTakeIsRejected()
    {
        var rejections = new List<BitAiAttachmentRejection>();
        var composer = RenderComponent<BitAiPromptInput>(p => p
            .Add(c => c.Capabilities, BitAiModelCapabilities.Vision)
            .Add(c => c.OnAttachmentRejected, r => rejections.Add(r)));

        await composer.InvokeAsync(() => composer.Instance._OnDroppedFile(File("notes.txt", "text/plain", [1])));
        await composer.InvokeAsync(() => composer.Instance._OnDroppedFile(File("shot.png", "image/png", [1])));

        Assert.AreEqual(BitAiAttachmentRejectionReason.TypeNotAllowed, Assert.ContainsSingle(rejections).Reason);
        Assert.AreEqual("shot.png", Assert.ContainsSingle(composer.Instance.Attachments).Name);
    }

    [TestMethod]
    public void APickedImageGetsABrowserSidePreview()
    {
        Context.JSInterop.Setup<string?>("BitBlazorUI.AiPromptInput.previewFromInput", _ => true).SetResult("blob:thumb");
        var composer = RenderComponent<BitAiPromptInput>();

        composer.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromBinary([1, 2, 3], "photo.png", contentType: "image/png"));

        composer.WaitForAssertion(() => Assert.AreEqual("blob:thumb", Assert.ContainsSingle(composer.Instance.Attachments).ThumbnailUrl));
        var call = Context.JSInterop.VerifyInvoke("BitBlazorUI.AiPromptInput.previewFromInput");
        Assert.AreEqual(0, (int)call.Arguments[1]!);
        Assert.AreEqual("photo.png", call.Arguments[2]);
    }

    [TestMethod]
    public void ThePasteAndDropListenersAreRegisteredWithTheSizeLimit()
    {
        RenderComponent<BitAiPromptInput>(p => p.Add(c => c.MaxAttachmentSize, 2048));

        Assert.AreEqual(2048L, (long)Context.JSInterop.VerifyInvoke("BitBlazorUI.AiPromptInput.registerPaste").Arguments[3]!);
        Assert.AreEqual(2048L, (long)Context.JSInterop.VerifyInvoke("BitBlazorUI.AiPromptInput.registerDrop").Arguments[3]!);
    }

    [TestMethod]
    public async Task ADisabledComposerTakesNoPastedOrDroppedFile()
    {
        var rejections = new List<BitAiAttachmentRejection>();
        var composer = RenderComponent<BitAiPromptInput>(p => p
            .Add(c => c.Disabled, true)
            .Add(c => c.OnAttachmentRejected, r => rejections.Add(r)));

        var pasted = File("shot.png", "image/png", [1, 2], "blob:pasted");
        var dropped = File("notes.txt", "text/plain", [3]);

        await composer.InvokeAsync(() => composer.Instance._OnPastedFile(pasted));
        await composer.InvokeAsync(() => composer.Instance._OnDroppedFile(dropped));

        Assert.IsEmpty(composer.Instance.Attachments);
        Assert.IsEmpty(composer.FindAll(".bit-aipri-ats"));

        // Turned away without a word - the composer is unavailable, not choosy - and with nothing of the files kept.
        Assert.IsEmpty(rejections);
        Assert.IsEmpty(composer.FindAll(".bit-aipri-ntc"));
        Assert.IsTrue(((FakeStreamReference)pasted.Content!).Disposed);
        Assert.IsTrue(((FakeStreamReference)dropped.Content!).Disposed);
        Assert.AreEqual("blob:pasted", Context.JSInterop.VerifyInvoke("BitBlazorUI.AiPromptInput.revokePreview").Arguments[0]);
    }

    [TestMethod]
    public async Task ADragOverADisabledComposerRaisesNoOverlay()
    {
        var composer = RenderComponent<BitAiPromptInput>(p => p.Add(c => c.Disabled, true));

        await composer.InvokeAsync(() => composer.Instance._OnDragStateChanged(true));

        Assert.IsFalse(composer.Find(".bit-aipri").ClassList.Contains("bit-aipri-dnd"));
        Assert.IsEmpty(composer.FindAll(".bit-aipri-dro"));
    }

    [TestMethod]
    public async Task DisablingTheComposerDuringADragTakesTheOverlayDown()
    {
        var composer = RenderComponent<BitAiPromptInput>();

        await composer.InvokeAsync(() => composer.Instance._OnDragStateChanged(true));
        Assert.IsNotNull(composer.Find(".bit-aipri-dro"));

        composer.Render(p => p.Add(c => c.Disabled, true));

        Assert.IsFalse(composer.Find(".bit-aipri").ClassList.Contains("bit-aipri-dnd"));
        Assert.IsEmpty(composer.FindAll(".bit-aipri-dro"));
    }

    [TestMethod]
    public void ThePasteAndDropListenersRefuseFilesForAsLongAsTheComposerIsDisabled()
    {
        var composer = RenderComponent<BitAiPromptInput>(p => p.Add(c => c.Disabled, true));

        Assert.IsTrue((bool)Context.JSInterop.VerifyInvoke("BitBlazorUI.AiPromptInput.registerPaste").Arguments[4]!);
        Assert.IsTrue((bool)Context.JSInterop.VerifyInvoke("BitBlazorUI.AiPromptInput.registerDrop").Arguments[4]!);

        composer.Render(p => p.Add(c => c.Disabled, false));

        composer.WaitForAssertion(() =>
        {
            Assert.IsFalse((bool)Context.JSInterop.VerifyInvoke("BitBlazorUI.AiPromptInput.registerPaste", calledTimes: 2)[1].Arguments[4]!);
            Assert.IsFalse((bool)Context.JSInterop.VerifyInvoke("BitBlazorUI.AiPromptInput.registerDrop", calledTimes: 2)[1].Arguments[4]!);
        });

        // A render that changes neither the limit nor the state registers nothing again.
        composer.Render(p => p.Add(c => c.Placeholder, "Ask"));

        Context.JSInterop.VerifyInvoke("BitBlazorUI.AiPromptInput.registerDrop", calledTimes: 2);
    }
}
