using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.AttachmentChip;

[TestClass]
public class BitAiAttachmentChipTests : BitAiTestBase
{
    private static BitAiAttachment Attachment(BitAiAttachmentKind kind = BitAiAttachmentKind.File, string name = "report.pdf") =>
        new("att-1", name) { Kind = kind, Size = 2048 };

    [TestMethod]
    public void BitAiAttachmentChipShouldRenderItsRootClassClassStyleAndDir()
    {
        var chip = RenderComponent<BitAiAttachmentChip>(parameters =>
        {
            parameters.Add(p => p.Attachment, Attachment());
            parameters.Add(p => p.Class, "custom-class");
            parameters.Add(p => p.Style, "margin: 1px");
            parameters.Add(p => p.Dir, BitDir.Rtl);
            parameters.Add(p => p.Classes, new BitAiAttachmentChipClassStyles { Root = "root-class", Thumbnail = "thumb-class" });
        });

        var root = chip.Find(".bit-aiach");

        Assert.IsTrue(root.ClassList.Contains("custom-class"));
        Assert.IsTrue(root.ClassList.Contains("root-class"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        StringAssert.Contains(root.GetAttribute("style"), "margin: 1px");
        Assert.IsNotNull(chip.Find(".bit-aiach > .bit-tag.bit-aiach-tag"));
        Assert.IsTrue(chip.Find(".bit-aiach-thm").ClassList.Contains("thumb-class"));
    }

    [TestMethod]
    public void TheChipShowsTheNameAndTheSize()
    {
        var chip = RenderComponent<BitAiAttachmentChip>(p => p.Add(c => c.Attachment, Attachment()));

        Assert.AreEqual("report.pdf", chip.Find(".bit-tag-tex").TextContent);
        Assert.AreEqual("2 KB", chip.Find(".bit-tag-stx").TextContent);
        Assert.AreEqual("report.pdf", chip.Find(".bit-tag").GetAttribute("title"));
    }

    [TestMethod]
    public void AnImageIsShownByItsThumbnailAndAnythingElseByItsGlyph()
    {
        var image = new BitAiAttachment("att-2", "shot.png") { Kind = BitAiAttachmentKind.Image, ThumbnailUrl = "blob:thumb", Url = "data:image/png;base64,AA==" };

        var chip = RenderComponent<BitAiAttachmentChip>(p => p.Add(c => c.Attachment, image));

        var thumbnail = chip.Find("img.bit-aiach-thm");
        Assert.AreEqual("blob:thumb", thumbnail.GetAttribute("src"));
        Assert.AreEqual(string.Empty, thumbnail.GetAttribute("alt"));

        chip.Render(p => p.Add(c => c.Attachment, Attachment(BitAiAttachmentKind.Audio, "note.mp3")));

        Assert.IsEmpty(chip.FindAll("img"));
        Assert.AreEqual("true", chip.Find("span.bit-aiach-thm").GetAttribute("aria-hidden"));
        Assert.IsTrue(chip.Find(".bit-aiach-ico").ClassList.Contains("bit-icon-ai--Microphone"));
    }

    [TestMethod]
    public void AnUploadShowsItsProgressAndAFailureItsReason()
    {
        var uploading = new BitAiAttachment("att-3", "big.zip") { Status = BitAiAttachmentStatus.Uploading, Progress = 0.42 };

        var chip = RenderComponent<BitAiAttachmentChip>(p => p.Add(c => c.Attachment, uploading));

        Assert.AreEqual("42%", chip.Find(".bit-tag-stx").TextContent);
        Assert.IsFalse(chip.Find(".bit-tag").ClassList.Contains("bit-tag-err"));
        // A neutral chip through the host-default class, never a Color, so the app's --bit-Tag-* variables reach it.
        Assert.IsTrue(chip.Find(".bit-tag").ClassList.Contains("bit-tag-dft-ter"));
        Assert.IsNull(chip.FindComponent<BitTag>().Instance.Color);

        chip.Render(p => p.Add(c => c.Attachment, uploading with { Status = BitAiAttachmentStatus.Failed, Error = null }));

        Assert.AreEqual("Upload failed", chip.Find(".bit-tag-stx").TextContent);
        Assert.IsTrue(chip.Find(".bit-tag").ClassList.Contains("bit-tag-err"));
    }

    [TestMethod]
    public void TheRemoveButtonIsThereOnlyWhileSomethingListens()
    {
        var removed = new List<BitAiAttachment>();
        var attachment = Attachment();

        var chip = RenderComponent<BitAiAttachmentChip>(p => p.Add(c => c.Attachment, attachment));

        Assert.IsEmpty(chip.FindAll("button"));

        chip.Render(p => p.Add(c => c.OnRemove, a => removed.Add(a)));

        var button = chip.Find("button");
        Assert.AreEqual("Remove report.pdf", button.GetAttribute("aria-label"));

        button.Click();

        Assert.AreSame(attachment, Assert.ContainsSingle(removed));
    }

    // The remove button that was pressed goes with its chip, so a row of chips hands the focus on to the next one with it.
    [TestMethod]
    public void FocusAsyncFocusesTheRemoveButtonOfAChipThatHasOne()
    {
        var removable = RenderComponent<BitAiAttachmentChip>(p => p
            .Add(c => c.Attachment, Attachment())
            .Add(c => c.OnRemove, _ => { }));

        removable.InvokeAsync(() => removable.Instance.FocusAsync().AsTask()).Wait();

        Context.JSInterop.VerifyFocusAsyncInvoke();

        var plain = RenderComponent<BitAiAttachmentChip>(p => p.Add(c => c.Attachment, Attachment()));

        plain.InvokeAsync(() => plain.Instance.FocusAsync().AsTask()).Wait();

        Assert.HasCount(1, Context.JSInterop.Invocations.Where(i => i.Identifier.EndsWith(".focus", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void ADisabledChipCannotBeRemoved()
    {
        var removed = 0;

        var chip = RenderComponent<BitAiAttachmentChip>(p => p
            .Add(c => c.Attachment, Attachment())
            .Add(c => c.Disabled, true)
            .Add(c => c.OnRemove, _ => removed++));

        Assert.IsTrue(chip.Find(".bit-aiach").ClassList.Contains("bit-dis"));

        chip.Find("button").Click();

        Assert.AreEqual(0, removed);
    }
}
