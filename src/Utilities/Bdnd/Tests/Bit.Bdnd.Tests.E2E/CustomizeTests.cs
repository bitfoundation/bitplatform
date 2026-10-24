using System.Text.RegularExpressions;
using Bit.Bdnd.Tests.E2E.Infrastructure;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bdnd.Tests.E2E;

/// <summary>
/// Customizing a source: a drag handle decides where a drag may start, which the browser decides from the
/// <c>draggable</c> attribute and the module from the handle under the pointer; and a source's drop effect is what
/// the module puts on the native drag at <c>dragstart</c>, tagged with the source's own id.
/// </summary>
[TestClass]
public sealed partial class CustomizeTests : BdndTest
{
    private const string DarkGreen = "rgb(0, 100, 0)";

    [TestMethod]
    public async Task A_box_without_a_handle_drags_from_anywhere_on_it()
    {
        var page = await OpenAsync("/customize/handles");
        var whole = page.Locator(".box", new() { HasText = "Drag me from anywhere" });
        var bin = page.Locator(".dustbin");
        var box = await BoxAsync(whole);

        // By its left edge rather than its middle.
        await StartDragAsync(page, new Point(box.X + 8, box.Centre.Y));
        await Expect(whole).ToHaveCSSAsync("opacity", "0.4");
        await DragToAsync(page, (await BoxAsync(bin)).Centre, () => Expect(bin).ToHaveCSSAsync("background-color", DarkGreen));

        await Expect(bin).ToHaveTextAsync("Dropped: whole");
    }

    [TestMethod]
    public async Task A_box_with_a_handle_drags_only_from_its_grip()
    {
        var page = await OpenAsync("/customize/handles");
        var handleBox = page.Locator(".box", new() { HasText = "Only the grip drags me" });
        var label = page.GetByText("Only the grip drags me");
        var bin = page.Locator(".dustbin");
        var binCentre = (await BoxAsync(bin)).Centre;

        // Pressed on its text, the box does not drag: the browser starts no drag at all, and the bin never reacts.
        await StartDragAsync(page, label);
        await MoveAsync(page, binCentre);
        await WaitForQuietAsync(page);
        await Expect(bin).ToHaveCSSAsync("background-color", "rgb(51, 51, 51)");
        await Expect(handleBox).ToHaveCSSAsync("opacity", "1");
        await page.Mouse.UpAsync();
        await WaitForQuietAsync(page);
        Assert.IsEmpty(await DragStartsAsync(page.MainFrame), "a press on the text of a box with a handle started a drag");
        await Expect(bin).ToHaveTextAsync("Drop a box here");

        // The press selected some text; a press on a selection would drag the text instead of the box.
        await page.EvaluateAsync("() => getSelection().removeAllRanges()");

        // Pressed on the grip, the whole box drags.
        await StartDragAsync(page, page.Locator(".handle"));
        await Expect(handleBox).ToHaveCSSAsync("opacity", "0.4");
        await DragToAsync(page, binCentre, () => Expect(bin).ToHaveCSSAsync("background-color", DarkGreen));

        await Expect(bin).ToHaveTextAsync("Dropped: handle");
        await Expect(handleBox).ToHaveCSSAsync("opacity", "1");
    }

    [TestMethod]
    public async Task A_press_in_a_text_field_inside_a_draggable_selects_text_and_drags_nothing()
    {
        var page = await OpenAsync("/customize/handles");
        var noteBox = page.Locator(".handles-note");
        var field = page.Locator(".handles-note__field");
        var bin = page.Locator(".dustbin");
        var box = await BoxAsync(field);

        // pressed at the start of the text and moved along it: the text is selected, the box does not move
        await page.Mouse.MoveAsync((float)(box.X + 4), (float)box.Centre.Y);
        await page.Mouse.DownAsync();
        await MoveAsync(page, new Point(box.Right - 4, box.Centre.Y), steps: 10);
        await page.Mouse.UpAsync();
        await WaitForQuietAsync(page);

        Assert.IsEmpty(await DragStartsAsync(page.MainFrame), "a press in the field started a drag");
        Assert.IsGreaterThan(5, await field.EvaluateAsync<int>("f => f.selectionEnd - f.selectionStart"), "the press selected the field's text");
        await Expect(bin).ToHaveTextAsync("Drop a box here");
        // the box still drags from the rest of it, once the press is over
        await Expect(noteBox.Locator("xpath=..")).ToHaveAttributeAsync("draggable", "true");
        await DragAsync(page, page.GetByText("Drag me, or edit my note"), bin);
        await Expect(bin).ToHaveTextAsync("Dropped: note");
    }

    [TestMethod]
    public async Task A_drag_is_tagged_with_its_source_and_carries_the_drop_effect_the_source_declares()
    {
        var page = await OpenAsync("/customize/drop-effects");
        var bin = page.Locator(".dustbin");
        var tags = new List<string>();

        foreach (var effect in new[] { "copy", "move" })
        {
            var source = page.Locator(".box", new() { HasText = $"(effect = {effect})" });
            var centre = (await BoxAsync(bin)).Centre;
            await StartDragAsync(page, source);
            await MoveAsync(page, centre);
            await Expect(bin).ToHaveCSSAsync("background-color", DarkGreen);

            // Over a target that takes the item, the module accepts each dragover and leaves the effect to the
            // browser, which picks it from what the source allows: the cursor shows a copy or a move. (A dragover
            // raised now, once .NET has told the module the bin can take the item.)
            var over = await DragOverAtAsync(page, bin, centre);
            Assert.IsTrue(over.Inside && over.Prevented, $"the dragover over the bin was not accepted ({effect}): {over}");
            // WebKit reads "none" while the dragover runs and picks the effect after it: there the drop below shows it
            if (Browser.BrowserType.Name != "webkit") Assert.AreEqual(effect, over.DropEffect, $"the drop effect over the bin: {over}");

            await DragToAsync(page, centre);
            await Expect(bin).ToHaveTextAsync($"Last drop used: {effect}");

            // At dragstart the module marked the drag as the library's, with the source's id as both the type's
            // suffix and its data, and set the effects the source allows.
            var start = (await DragStartsAsync(page.MainFrame)).Last();
            Assert.IsNotNull(start.Tag, "the dragstart carries no library tag: " + string.Join(", ", start.Types));
            StringAssert.Matches(start.Tag, TagPattern());
            Assert.AreEqual(start.Tag["application/x-bit-bdnd-".Length..], start.TagData, "the tag's data is not the source's id");
            Assert.AreEqual(effect, start.EffectAllowed, "effectAllowed at dragstart");
            tags.Add(start.Tag);
        }

        Assert.AreNotEqual(tags[0], tags[1], "two sources tagged their drags with the same id");

        // Every drop was kept from the browser, which would otherwise act on the dropped data.
        var (count, prevented) = await DropsAsync(page.MainFrame);
        Assert.AreEqual(2, count, "drops");
        Assert.AreEqual(2, prevented, "drops the module left to the browser");
    }

    [GeneratedRegex("^application/x-bit-bdnd-[0-9a-f]{32}$")]
    private static partial Regex TagPattern();
}
