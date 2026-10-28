using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.AI;

/// <summary>
/// Drives BitAiAttachmentChip at /e2e/aiattachmentchip in a real browser: an image drawn from its thumbnail or its
/// content, the glyph of every other kind from the AI font, the size and the progress or failure of an upload, the
/// remove button (named, by the pointer and the keyboard, absent without OnRemove), a long name cut short with its full
/// name as the tooltip, Disabled, the theme tokens and the public variables, Styles/Classes, and right-to-left.
/// </summary>
[TestClass]
public class BitAiAttachmentChipE2ETests : E2ETestBase
{
    private const string Route = "/e2e/aiattachmentchip";

    private ILocator Root(string id) => Page.Locator($"#{id}");

    private ILocator Tag(string id) => Page.Locator($"#{id} .bit-tag");

    private ILocator Thumbnail(string id) => Page.Locator($"#{id} .bit-aiach-thm");

    private ILocator Glyph(string id) => Page.Locator($"#{id} i.bit-aiach-ico");

    private ILocator Name(string id) => Page.Locator($"#{id} .bit-tag-tex");

    private ILocator Details(string id) => Page.Locator($"#{id} .bit-tag-stx");

    private ILocator Remove(string id) => Page.Locator($"#{id} button.bit-tag-cls");

    [TestMethod]
    public async Task AnImage_IsDrawnFromItsThumbnail_OrFromItsContent()
    {
        await OpenAsync(Route);

        var thumbnail = Thumbnail("ach-image");
        await Expect(thumbnail).ToHaveJSPropertyAsync("tagName", "IMG");
        await Expect(thumbnail).ToHaveAttributeAsync("src", "samples/image-square.svg");
        await Expect(thumbnail).ToHaveAttributeAsync("alt", "");
        await Expect(thumbnail).ToHaveCSSAsync("object-fit", "cover");
        await Page.WaitForFunctionAsync("() => { const i = document.querySelector('#ach-image img.bit-aiach-thm'); return i.complete && i.naturalWidth > 0; }");

        // An image with no thumbnail is drawn from its own content.
        await Expect(Thumbnail("ach-image-url")).ToHaveAttributeAsync("src", new Regex("^data:image/png;base64,"));
        await Page.WaitForFunctionAsync("() => { const i = document.querySelector('#ach-image-url img.bit-aiach-thm'); return i.complete && i.naturalWidth === 1; }");

        // A thumbnail and a glyph sit in boxes of the same size, so a row of chips lines up.
        var image = (await Thumbnail("ach-image").BoundingBoxAsync())!;
        var file = (await Thumbnail("ach-file").BoundingBoxAsync())!;
        Assert.AreEqual(file.Width, image.Width, 0.5);
        Assert.AreEqual(file.Height, image.Height, 0.5);
        Assert.AreEqual(image.Width, image.Height, 0.5, "The thumbnail is square.");
    }

    [TestMethod]
    public async Task EveryOtherKind_ShowsTheGlyphOfItsKind_FromTheAiFont()
    {
        await OpenAsync(Route);

        await Glyph("ach-file").AssertAiGlyphAsync("Page");
        await Glyph("ach-audio").AssertAiGlyphAsync("Microphone");
        await Glyph("ach-video").AssertAiGlyphAsync("Video");
        await Expect(Thumbnail("ach-file")).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(Glyph("ach-image")).ToHaveCountAsync(0);

        Assert.IsTrue(await Page.AiGlyphFontLoadsAsync(""), "The AI font is fetched and draws the Page glyph.");
    }

    [TestMethod]
    public async Task TheDetails_AreTheSize_ThenTheProgressOfAnUpload_OrWhyItFailed()
    {
        await OpenAsync(Route);

        await Expect(Name("ach-image")).ToHaveTextAsync("photo.png");
        await Expect(Details("ach-image")).ToHaveTextAsync("2 KB");
        await Expect(Details("ach-file")).ToHaveTextAsync("1.5 MB");
        await Expect(Details("ach-audio")).ToHaveTextAsync("512 B");
        await Expect(Details("ach-video")).ToHaveCountAsync(0);

        await Expect(Details("ach-uploading")).ToHaveTextAsync("40%");
        await Expect(Details("ach-failed")).ToHaveTextAsync("Network lost");
        await Expect(Details("ach-failed-default")).ToHaveTextAsync("Upload failed");

        // A failed upload takes the error color, which is the state of the file rather than a look of the chip.
        await Expect(Tag("ach-failed")).ToHaveClassAsync(HasClass("bit-tag-err"));
        await Expect(Tag("ach-file")).Not.ToHaveClassAsync(HasClass("bit-tag-err"));
        Assert.AreNotEqual(await ComputedStyleAsync("#ach-file .bit-tag", "border-top-color"),
                           await ComputedStyleAsync("#ach-failed .bit-tag", "border-top-color"),
                           "The failed chip is drawn in another color.");

        // The progress follows the attachment as it arrives, and the size takes its place once it has.
        await Expect(Details("ach-progress")).ToHaveTextAsync("0%");
        await Page.Locator("#ach-progress-advance").ClickAsync();
        await Expect(Details("ach-progress")).ToHaveTextAsync("50%");
        await Page.Locator("#ach-progress-advance").ClickAsync();
        await Expect(Details("ach-progress")).ToHaveTextAsync("3.0 MB");
    }

    [TestMethod]
    public async Task Remove_TheButtonIsNamedForTheFile_AndRemovesItByPointerOrKeyboard()
    {
        await OpenAsync(Route);

        var removeOne = Page.GetByRole(AriaRole.Button, new() { Name = "Remove one.txt", Exact = true });
        await Expect(removeOne).ToHaveCountAsync(1);
        await Expect(removeOne).ToHaveAttributeAsync("title", "Remove one.txt");

        await removeOne.ClickAsync();
        await Expect(Root("ach-rm-one")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#ach-removed")).ToHaveTextAsync("one.txt");

        // Enter and Space on the focused button remove the file too.
        await Remove("ach-rm-two").FocusAsync();
        await Expect(Remove("ach-rm-two")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Root("ach-rm-two")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#ach-removed")).ToHaveTextAsync("one.txt,two.txt");

        await Remove("ach-rm-three").FocusAsync();
        await Page.Keyboard.PressAsync(" ");
        await Expect(Root("ach-rm-three")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#ach-removed")).ToHaveTextAsync("one.txt,two.txt,three.txt");
    }

    [TestMethod]
    public async Task WithoutOnRemove_ThereIsNoRemoveButton()
    {
        await OpenAsync(Route);

        foreach (var id in new[] { "ach-image", "ach-file", "ach-audio", "ach-video", "ach-failed", "ach-long" })
        {
            await Expect(Remove(id)).ToHaveCountAsync(0);
        }

        await Expect(Root("ach-file").GetByRole(AriaRole.Button)).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task ALongName_IsCutShortWithAnEllipsis_AndTheFullNameIsTheTooltip()
    {
        await OpenAsync(Route);

        const string full = "a-really-long-file-name-that-will-never-fit-inside-the-chip-of-an-attachment.pdf";

        var root = Root("ach-long");
        var maxWidth = AiE2EHelpers.ParsePx(await ComputedStyleAsync("#ach-long", "max-width"));
        Assert.IsTrue(maxWidth > 0, "The chip has a max width.");
        Assert.IsTrue((await root.BoundingBoxAsync())!.Width <= maxWidth + 0.5, "The chip stops at its max width.");

        await Expect(Tag("ach-long")).ToHaveAttributeAsync("title", full);
        await Expect(Name("ach-long")).ToHaveTextAsync(full);
        Assert.IsTrue(await Name("ach-long").EvaluateAsync<bool>("e => e.scrollWidth > e.clientWidth"), "The name is cut short rather than wrapped.");
        await Expect(Name("ach-long")).ToHaveCSSAsync("white-space", "nowrap");
        await Expect(Name("ach-long")).ToHaveCSSAsync("text-overflow", "ellipsis");

        // The details stay on the same line as the name.
        var name = (await Name("ach-long").BoundingBoxAsync())!;
        var details = (await Details("ach-long").BoundingBoxAsync())!;
        Assert.IsTrue(Math.Abs(name.Y - details.Y) < name.Height + details.Height, "The chip keeps to one row.");
    }

    [TestMethod]
    public async Task Disabled_TheRemoveButtonIsUnavailable_AndRemovesNothing()
    {
        await OpenAsync(Route);

        await Expect(Root("ach-dis")).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Remove("ach-dis")).ToBeDisabledAsync();

        await Remove("ach-dis").ClickAsync(new() { Force = true });
        await Remove("ach-dis").DispatchEventAsync("click");

        // A remove that works, after it, proves the presses were answered.
        await Remove("ach-rm-one").ClickAsync();
        await Expect(Page.Locator("#ach-removed")).ToHaveTextAsync("one.txt");
        await Expect(Page.Locator("#ach-dis-removed")).ToHaveTextAsync("0");
        await Expect(Root("ach-dis")).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task Style_TheThemeTokensPaintTheThumbnail()
    {
        await OpenAsync(Route);

        var thumbnail = Thumbnail("ach-file");
        await Expect(thumbnail).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("var(--bit-clr-bg-sec)"));
        await Expect(thumbnail).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-fg-sec)"));
        await Expect(Root("ach-file")).ToHaveCSSAsync("display", "inline-flex");

        // A neutral outlined chip.
        await Expect(Tag("ach-file")).ToHaveClassAsync(HasClass("bit-tag-otl"));
        await Expect(Tag("ach-file")).ToHaveClassAsync(HasClass("bit-tag-dft-ter"));
    }

    [TestMethod]
    public async Task Style_ThePublicVariables_RestyleTheChip_OnTheInstanceOrAnAncestor()
    {
        await OpenAsync(Route);

        await Expect(Root("ach-style")).ToHaveCSSAsync("max-width", "150px");
        Assert.IsTrue((await Root("ach-style").BoundingBoxAsync())!.Width <= 150.5);

        var thumbnail = Thumbnail("ach-style");
        await Expect(thumbnail).ToHaveCSSAsync("width", "24px");
        await Expect(thumbnail).ToHaveCSSAsync("height", "24px");
        await Expect(thumbnail).ToHaveCSSAsync("border-top-left-radius", "5px");
        await Expect(thumbnail).ToHaveCSSAsync("background-color", "rgb(1, 2, 3)");
        await Expect(thumbnail).ToHaveCSSAsync("color", "rgb(4, 5, 6)");

        await Expect(Thumbnail("ach-style-ancestor")).ToHaveCSSAsync("background-color", "rgb(7, 8, 9)");

        await Expect(Root("ach-style-parts")).ToHaveClassAsync(HasClass("ach-custom-root"));
        await Expect(Thumbnail("ach-style-parts")).ToHaveClassAsync(HasClass("ach-custom-thumb"));
        await Expect(Thumbnail("ach-style-parts")).ToHaveCSSAsync("outline-color", "rgb(10, 11, 12)");
        await Expect(Glyph("ach-style-parts")).ToHaveClassAsync(HasClass("ach-custom-icon"));
        await Expect(Glyph("ach-style-parts")).ToHaveCSSAsync("color", "rgb(13, 14, 15)");
        await Glyph("ach-style-parts").AssertAiGlyphAsync("Page");
    }

    [TestMethod]
    public async Task Rtl_TheThumbnailLeadsOnTheRight_AndTheRemoveButtonEndsOnTheLeft()
    {
        await OpenAsync(Route);

        await Expect(Root("ach-rtl")).ToHaveAttributeAsync("dir", "rtl");

        foreach (var id in new[] { "ach-rtl", "ach-rtl-inherited" })
        {
            var thumbnail = await Thumbnail(id).RectAsync();
            var name = await Name(id).RectAsync();
            var remove = await Remove(id).RectAsync();
            Assert.IsTrue(thumbnail.Left >= name.Right - 1, $"#{id}: the thumbnail is on the right of the name.");
            Assert.IsTrue(remove.Right <= name.Left + 1, $"#{id}: the remove button is on the left of the name.");
        }

        var ltrThumbnail = await Thumbnail("ach-ltr").RectAsync();
        var ltrName = await Name("ach-ltr").RectAsync();
        Assert.IsTrue(ltrThumbnail.Right <= ltrName.Left + 1, "Left to right, the thumbnail leads on the left.");
    }
}
