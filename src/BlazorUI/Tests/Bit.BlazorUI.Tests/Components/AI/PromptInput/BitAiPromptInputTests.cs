using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.PromptInput;

[TestClass]
public class BitAiPromptInputTests : BitAiTestBase
{
    /// <summary>A browser stream over fixed bytes, or one whose read fails.</summary>
    private sealed class FakeStreamReference(byte[] bytes, bool fail = false) : IJSStreamReference
    {
        public bool Disposed { get; private set; }

        public long Length => bytes.Length;

        public ValueTask<Stream> OpenReadStreamAsync(long maxAllowedSize = 512000, CancellationToken cancellationToken = default) =>
            fail
                ? ValueTask.FromException<Stream>(new IOException("The transfer was interrupted."))
                : ValueTask.FromResult<Stream>(new MemoryStream(bytes));

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            throw new NotSupportedException();

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            throw new NotSupportedException();

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }



    [TestMethod]
    public void BitAiPromptInputShouldRenderItsRootClass()
    {
        var component = RenderComponent<BitAiPromptInput>();

        var root = component.Find(".bit-aipri");

        Assert.AreEqual("DIV", root.TagName);
        Assert.IsNotNull(component.Find(".bit-aipri > .bit-aipri-bdy > textarea.bit-aipri-inp"));
    }

    [TestMethod]
    public void BitAiPromptInputShouldRespectClassStyleIdAndDir()
    {
        var component = RenderComponent<BitAiPromptInput>(parameters =>
        {
            parameters.Add(p => p.Class, "custom-class");
            parameters.Add(p => p.Style, "margin: 1px");
            parameters.Add(p => p.Id, "composer");
            parameters.Add(p => p.Dir, BitDir.Rtl);
            parameters.Add(p => p.Classes, new BitAiPromptInputClassStyles { Root = "root-class", Input = "input-class" });
            parameters.Add(p => p.Styles, new BitAiPromptInputClassStyles { Root = "padding: 2px", Input = "color: red" });
        });

        var root = component.Find(".bit-aipri");

        Assert.IsTrue(root.ClassList.Contains("custom-class"));
        Assert.IsTrue(root.ClassList.Contains("root-class"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        Assert.AreEqual("composer", root.Id);
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        StringAssert.Contains(root.GetAttribute("style"), "margin: 1px");
        StringAssert.Contains(root.GetAttribute("style"), "padding: 2px");

        var input = component.Find("textarea");

        Assert.IsTrue(input.ClassList.Contains("input-class"));
        Assert.AreEqual("color: red", input.GetAttribute("style"));
    }

    [TestMethod,
        DataRow(BitVisibility.Visible),
        DataRow(BitVisibility.Collapsed),
        DataRow(BitVisibility.Hidden)]
    public void BitAiPromptInputShouldRespectVisibility(BitVisibility visibility)
    {
        var component = RenderComponent<BitAiPromptInput>(parameters =>
        {
            parameters.Add(p => p.Visibility, visibility);
        });

        var style = component.Find(".bit-aipri").GetAttribute("style") ?? string.Empty;

        switch (visibility)
        {
            case BitVisibility.Visible:
                Assert.IsFalse(style.Contains("visibility") || style.Contains("display"));
                break;
            case BitVisibility.Collapsed:
                StringAssert.Contains(style, "display:none");
                break;
            case BitVisibility.Hidden:
                StringAssert.Contains(style, "visibility:hidden");
                break;
        }
    }

    [TestMethod]
    public void BitAiPromptInputShouldDisableTheWholeComposer()
    {
        var component = RenderComponent<BitAiPromptInput>(parameters =>
        {
            parameters.Add(p => p.Disabled, true);
            parameters.Add(p => p.Value, "hello");
        });

        Assert.IsTrue(component.Find(".bit-aipri").ClassList.Contains("bit-dis"));

        var textarea = component.Find("textarea");

        Assert.IsTrue(textarea.HasAttribute("disabled"));
        Assert.AreEqual("true", textarea.GetAttribute("aria-disabled"));
        Assert.AreEqual("true", component.Find(".bit-aipri-atb").GetAttribute("aria-disabled"));
        Assert.AreEqual("true", component.Find(".bit-aipri-snd").GetAttribute("aria-disabled"));

        component.Render(parameters => parameters.Add(p => p.Disabled, false));

        Assert.IsFalse(component.Find(".bit-aipri").ClassList.Contains("bit-dis"));
        Assert.IsFalse(component.Find("textarea").HasAttribute("disabled"));
        Assert.IsFalse(component.Find("textarea").HasAttribute("aria-disabled"));
    }

    [TestMethod]
    public void ThePlaceholderNamesTheTextUnlessAnAriaLabelDoes()
    {
        var component = RenderComponent<BitAiPromptInput>(parameters =>
        {
            parameters.Add(p => p.Placeholder, "Ask anything");
        });

        Assert.AreEqual("Ask anything", component.Find("textarea").GetAttribute("placeholder"));
        Assert.AreEqual("Ask anything", component.Find("textarea").GetAttribute("aria-label"));

        component.Render(parameters => parameters.Add(p => p.AriaLabel, "Message"));

        Assert.AreEqual("Message", component.Find("textarea").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void FocusAsyncFocusesTheText()
    {
        var component = RenderComponent<BitAiPromptInput>();

        component.InvokeAsync(() => component.Instance.FocusAsync().AsTask()).GetAwaiter().GetResult();

        Context.JSInterop.VerifyInvoke("BitBlazorUI.AiPromptInput.focus");
    }

    [TestMethod]
    public async Task AFileOverTheLimitIsRejectedAndTheUserIsTold()
    {
        var rejections = new List<BitAiAttachmentRejection>();
        var composer = RenderComponent<BitAiPromptInput>(p => p
            .Add(c => c.MaxAttachmentSize, 1024)
            .Add(c => c.OnAttachmentRejected, r => rejections.Add(r)));

        // The browser withholds the content of a file it has already found too large.
        await composer.InvokeAsync(() => composer.Instance._OnDroppedFile(new BitAiComposerFile("video.mp4", "video/mp4", 5_000_000)));

        var rejection = Assert.ContainsSingle(rejections);
        Assert.AreEqual(BitAiAttachmentRejectionReason.TooLarge, rejection.Reason);
        Assert.AreEqual("video.mp4", rejection.FileName);
        Assert.IsEmpty(composer.Instance.Attachments);

        var notice = composer.Find(".bit-aipri-ntc [role=alert]");
        StringAssert.Contains(notice.TextContent, "video.mp4 is too large. Files can be up to 1 KB.");
    }

    [TestMethod]
    public async Task TheRejectionNoticeCanBeDismissed()
    {
        var composer = RenderComponent<BitAiPromptInput>(p => p.Add(c => c.MaxAttachmentSize, 10));
        await composer.InvokeAsync(() => composer.Instance._OnPastedFile(new BitAiComposerFile("big.png", "image/png", 11)));

        composer.Find(".bit-aipri-ntc button[aria-label='Dismiss']").Click();

        Assert.IsEmpty(composer.FindAll(".bit-aipri-ntc"));
    }

    [TestMethod]
    public async Task APastedFileOfATypeNotAcceptedIsRejectedWithoutBeingRead()
    {
        var rejections = new List<BitAiAttachmentRejection>();
        var content = new FakeStreamReference([1, 2, 3]);
        var composer = RenderComponent<BitAiPromptInput>(p => p
            .Add(c => c.Accept, "image/*, .pdf")
            .Add(c => c.OnAttachmentRejected, r => rejections.Add(r)));

        await composer.InvokeAsync(() => composer.Instance._OnPastedFile(
            new BitAiComposerFile("notes.txt", "text/plain", 3) { Content = content }));

        Assert.AreEqual(BitAiAttachmentRejectionReason.TypeNotAllowed, Assert.ContainsSingle(rejections).Reason);
        Assert.IsTrue(content.Disposed);
        StringAssert.Contains(composer.Find(".bit-aipri-ntc").TextContent, "notes.txt is not a supported file type.");
    }

    [TestMethod,
        DataRow("report.PDF", "application/octet-stream"),
        DataRow("shot", "image/png")]
    public async Task AcceptMatchesExtensionsAndWildcardTypes(string name, string contentType)
    {
        var composer = RenderComponent<BitAiPromptInput>(p => p.Add(c => c.Accept, "image/*, .pdf"));

        await composer.InvokeAsync(() => composer.Instance._OnPastedFile(
            new BitAiComposerFile(name, contentType, 3) { Content = new FakeStreamReference([1, 2, 3]) }));

        Assert.HasCount(1, composer.Instance.Attachments);
    }

    [TestMethod]
    public async Task AFileWithinTheLimitIsStreamedIntoAnAttachment()
    {
        var content = new FakeStreamReference([1, 2, 3, 4]);
        var composer = RenderComponent<BitAiPromptInput>();

        await composer.InvokeAsync(() => composer.Instance._OnDroppedFile(
            new BitAiComposerFile("data.bin", "application/octet-stream", 4) { Content = content }));

        var attachment = Assert.ContainsSingle(composer.Instance.Attachments);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4 }, attachment.Data!.Value.ToArray());
        Assert.AreEqual(4L, attachment.Size);
        Assert.AreEqual("data:application/octet-stream;base64,AQIDBA==", attachment.Url);
        Assert.IsTrue(content.Disposed);
        Assert.IsEmpty(composer.FindAll(".bit-aipri-ntc"));
        Assert.HasCount(1, composer.FindAll(".bit-aipri-ats .bit-aiach"));
    }

    [TestMethod]
    public async Task AFileThatCannotBeReadIsReportedRatherThanLost()
    {
        var rejections = new List<BitAiAttachmentRejection>();
        var composer = RenderComponent<BitAiPromptInput>(p => p.Add(c => c.OnAttachmentRejected, r => rejections.Add(r)));

        await composer.InvokeAsync(() => composer.Instance._OnPastedFile(
            new BitAiComposerFile("photo.jpg", "image/jpeg", 4) { Content = new FakeStreamReference([1, 2, 3, 4], fail: true) }));

        Assert.AreEqual(BitAiAttachmentRejectionReason.ReadFailed, Assert.ContainsSingle(rejections).Reason);
        Assert.IsEmpty(composer.Instance.Attachments);
        StringAssert.Contains(composer.Find(".bit-aipri-ntc").TextContent, "photo.jpg could not be read.");
    }

    [TestMethod]
    public void AnInlineFileStillConvertsSynchronously()
    {
        var attachment = new BitAiComposerFile("a.txt", "text/plain", 2, "aGk=").ToAttachment();

        CollectionAssert.AreEqual("hi"u8.ToArray(), attachment.Data!.Value.ToArray());
    }

    [TestMethod]
    public void APickedFileOverTheLimitIsRejectedToo()
    {
        var rejections = new List<BitAiAttachmentRejection>();
        var composer = RenderComponent<BitAiPromptInput>(p => p
            .Add(c => c.MaxAttachmentSize, 4)
            .Add(c => c.OnAttachmentRejected, r => rejections.Add(r)));

        composer.FindComponent<InputFile>().UploadFiles(
            InputFileContent.CreateFromBinary([1, 2], "small.bin"),
            InputFileContent.CreateFromBinary([1, 2, 3, 4, 5, 6], "large.bin"));

        composer.WaitForAssertion(() => Assert.HasCount(1, composer.Instance.Attachments));
        Assert.AreEqual("small.bin", composer.Instance.Attachments[0].Name);
        Assert.AreEqual("large.bin", Assert.ContainsSingle(rejections).FileName);
        StringAssert.Contains(composer.Find(".bit-aipri-ntc").TextContent, "large.bin is too large.");
    }

    [TestMethod]
    public void AMentionInsertedMidSentenceKeepsTheTextAfterTheCaret()
    {
        Context.JSInterop.Setup<int?>("BitBlazorUI.AiPromptInput.getCaret", _ => true).SetResult("hi @Jo".Length);
        var value = string.Empty;

        var composer = RenderComponent<BitAiPromptInput>(p => p
            .Add(c => c.Mentions, [new BitAiMentionItem("u1", "John")])
            .Add(c => c.ValueChanged, v => value = v));

        composer.Find("textarea").Input("hi @Jo there");
        composer.Find("[role=option]").Click();

        Assert.AreEqual("hi @John there", value);
        composer.WaitForAssertion(() =>
            Assert.AreEqual("hi @John ".Length, (int)Context.JSInterop.VerifyInvoke("BitBlazorUI.AiPromptInput.setCaret").Arguments[1]!));
    }

    [TestMethod]
    public void ACommandTemplateKeepsTheTextAfterTheCaret()
    {
        Context.JSInterop.Setup<int?>("BitBlazorUI.AiPromptInput.getCaret", _ => true).SetResult("/sum".Length);
        var value = string.Empty;

        var composer = RenderComponent<BitAiPromptInput>(p => p
            .Add(c => c.Commands, [new BitAiSlashCommand("summarize") { Template = "Summarize: " }])
            .Add(c => c.ValueChanged, v => value = v));

        composer.Find("textarea").Input("/sum the report");
        composer.Find("[role=option]").Click();

        Assert.AreEqual("Summarize: the report", value);
        composer.WaitForAssertion(() =>
            Assert.AreEqual("Summarize: ".Length, (int)Context.JSInterop.VerifyInvoke("BitBlazorUI.AiPromptInput.setCaret").Arguments[1]!));
    }

    [TestMethod]
    public void ACommandWithAHandlerRunsItAndTakesItsTokenOut()
    {
        var ran = 0;
        var value = "unchanged";

        var composer = RenderComponent<BitAiPromptInput>(p => p
            .Add(c => c.Commands, [new BitAiSlashCommand("clear", "Clear the conversation", "Delete") { Handler = () => { ran++; return Task.CompletedTask; } }])
            .Add(c => c.ValueChanged, v => value = v));

        composer.Find("textarea").Input("/cl");

        var option = composer.Find("[role=option]");
        Assert.IsNotNull(option.QuerySelector(".bit-aipri-mic"));
        StringAssert.Contains(option.QuerySelector(".bit-aipri-mlb")!.TextContent, "/clear");
        StringAssert.Contains(option.QuerySelector(".bit-aipri-mhn")!.TextContent, "Clear the conversation");

        option.Click();

        Assert.AreEqual(1, ran);
        Assert.AreEqual(string.Empty, value);
        Assert.IsEmpty(composer.FindAll("[role=option]"));
    }

    [TestMethod]
    public void TheSendButtonSubmitsTheTrimmedTextAndEmptiesTheComposer()
    {
        var submissions = new List<BitAiPromptSubmission>();
        var composer = RenderComponent<BitAiPromptInput>(p => p
            .Add(c => c.Value, "  hello  ")
            .Add(c => c.OnSubmit, s => submissions.Add(s)));

        composer.Find(".bit-aipri-snd").Click();

        var submission = Assert.ContainsSingle(submissions);
        Assert.AreEqual("hello", submission.Text);
        Assert.IsEmpty(submission.Attachments);
        Assert.AreEqual(string.Empty, composer.Find("textarea").GetAttribute("value"));
    }

    [TestMethod]
    public async Task DraggingFilesOverTheComposerShowsTheDropOverlay()
    {
        var composer = RenderComponent<BitAiPromptInput>();

        Assert.IsEmpty(composer.FindAll(".bit-aipri-dro"));

        await composer.InvokeAsync(() => composer.Instance._OnDragStateChanged(true));

        Assert.IsTrue(composer.Find(".bit-aipri").ClassList.Contains("bit-aipri-dnd"));
        Assert.AreEqual("Drop files to attach", composer.Find(".bit-aipri-dro").TextContent);

        await composer.InvokeAsync(() => composer.Instance._OnDragStateChanged(false));

        Assert.IsFalse(composer.Find(".bit-aipri").ClassList.Contains("bit-aipri-dnd"));
        Assert.IsEmpty(composer.FindAll(".bit-aipri-dro"));
    }

    [TestMethod]
    public void TheButtonsDrawTheirGlyphsFromTheIconFontOfThePackageBesideTheClassesOfTheApp()
    {
        var composer = RenderComponent<BitAiPromptInput>(p => p
            .Add(c => c.Value, "hi")
            .Add(c => c.Classes, new BitAiPromptInputClassStyles { SendButton = new() { Icon = "app-icon", Root = "app-root" } }));

        Assert.IsTrue(composer.Find(".bit-aipri-atb .bit-btn-icn").ClassList.Contains("bit-icon-ai--Attach"));

        var send = composer.Find(".bit-aipri-snd .bit-btn-icn");
        Assert.IsTrue(send.ClassList.Contains("bit-icon-ai"));
        Assert.IsTrue(send.ClassList.Contains("bit-icon-ai--Send"));
        Assert.IsTrue(send.ClassList.Contains("app-icon"));
        Assert.IsTrue(composer.Find(".bit-aipri-snd").ClassList.Contains("app-root"));

        composer.Render(p => p.Add(c => c.Generating, true));

        Assert.IsTrue(composer.Find(".bit-aipri-snd .bit-btn-icn").ClassList.Contains("bit-icon-ai--StopSolid"));
        Assert.IsTrue(composer.Find(".bit-aipri-snd .bit-btn-icn").ClassList.Contains("app-icon"));
    }

    [TestMethod]
    public void ThePrefixSuffixAndHintTemplatesAreRendered()
    {
        var composer = RenderComponent<BitAiPromptInput>(p => p
            .Add(c => c.PrefixTemplate, b => b.AddMarkupContent(0, "<span class=\"lead\">L</span>"))
            .Add(c => c.SuffixTemplate, b => b.AddMarkupContent(0, "<span class=\"trail\">T</span>"))
            .Add(c => c.HintTemplate, b => b.AddMarkupContent(0, "<span class=\"hint\">H</span>")));

        Assert.IsNotNull(composer.Find(".bit-aipri-pre > .lead"));
        Assert.IsNotNull(composer.Find(".bit-aipri-tls > .trail"));
        Assert.IsNotNull(composer.Find(".bit-aipri-ftr > .bit-aipri-hnt > .hint"));
    }
}
