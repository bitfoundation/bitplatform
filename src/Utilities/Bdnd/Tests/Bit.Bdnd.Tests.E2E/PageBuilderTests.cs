using Bit.Bdnd.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bdnd.Tests.E2E;

/// <summary>
/// The page builder: palette blocks are copied onto the page where the line shows and the palette keeps them; page
/// blocks move within the page or into the bin, which takes no palette block; the keyboard places a new block too.
/// </summary>
[TestClass]
public sealed class PageBuilderTests : BdndTest
{
    private static ILocator Tool(IPage page, string kind) => page.Locator(".builder-tool:not([data-bit-bdnd-preview])", new() { HasText = kind });

    private static ILocator Block(IPage page, string title) =>
        page.Locator(".builder-block:not([data-bit-bdnd-preview])", new() { HasText = title });

    private static ILocator Slot(IPage page, string title) => page.Locator(".builder-slot", new() { Has = Block(page, title) });

    private static ILocator Outline(IPage page) => page.Locator(".builder-outline");

    [TestMethod]
    public async Task A_palette_block_is_copied_onto_the_page_where_the_line_shows()
    {
        var page = await OpenAsync("/other/page-builder");
        var text = await BoxAsync(Block(page, "Text 1"));

        await StartDragAsync(page, Tool(page, "Image"));
        await DragToAsync(page, new Point(text.Centre.X, text.Y + text.Height * 0.2),
            () => Expect(Slot(page, "Text 1")).ToHaveAttributeAsync("data-bit-bdnd-edge", "top"));

        await Expect(Outline(page)).ToHaveTextAsync("Page: Heading 1, Image 1, Text 1");
        // a copy: the palette still has it
        await Expect(Tool(page, "Image")).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task A_palette_block_dropped_below_the_blocks_goes_to_the_end()
    {
        var page = await OpenAsync("/other/page-builder");
        var pageBox = await BoxAsync(page.Locator(".builder-page"));

        await StartDragAsync(page, Tool(page, "Button"));
        await DragToAsync(page, new Point(pageBox.Centre.X, pageBox.Bottom - 10),
            () => Expect(page.Locator(".builder-page")).ToHaveAttributeAsync("data-bit-bdnd-over-shallow", ""));

        await Expect(Outline(page)).ToHaveTextAsync("Page: Heading 1, Text 1, Button 1");
    }

    [TestMethod]
    public async Task A_page_block_moves_within_the_page_and_into_the_bin()
    {
        var page = await OpenAsync("/other/page-builder");
        var heading = await BoxAsync(Block(page, "Heading 1"));

        await StartDragAsync(page, Block(page, "Text 1"));
        await DragToAsync(page, new Point(heading.Centre.X, heading.Y + heading.Height * 0.2),
            () => Expect(Slot(page, "Heading 1")).ToHaveAttributeAsync("data-bit-bdnd-edge", "top"));
        await Expect(Outline(page)).ToHaveTextAsync("Page: Text 1, Heading 1");

        var bin = page.Locator(".builder-bin");
        await DragAsync(page, Block(page, "Heading 1"), bin, () => Expect(bin).ToHaveTextAsync("Release to remove"));
        await Expect(Outline(page)).ToHaveTextAsync("Page: Text 1");
    }

    [TestMethod]
    public async Task The_bin_takes_no_palette_block()
    {
        var page = await OpenAsync("/other/page-builder");
        var bin = page.Locator(".builder-bin");

        await StartDragAsync(page, Tool(page, "Text"));
        // the drag is on: the page can take the block, the bin cannot
        await Expect(page.Locator(".builder-page")).ToHaveAttributeAsync("data-bit-bdnd-can-drop", "");
        await MoveAsync(page, (await BoxAsync(bin)).Centre);
        await WaitForQuietAsync(page);
        await Expect(bin).Not.ToHaveAttributeAsync("data-bit-bdnd-can-drop", "");
        await Expect(bin).ToHaveTextAsync("Bin");
        await DragToAsync(page, (await BoxAsync(bin)).Centre.Offset(2, 2));

        // the bin refused the drag: the browser raised no drop there
        Assert.AreEqual(0, (await DropsAsync(page.MainFrame)).Count, "a drop was raised on the bin");
        await Expect(Outline(page)).ToHaveTextAsync("Page: Heading 1, Text 1");
        await Expect(Tool(page, "Text")).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task The_keyboard_places_a_new_block()
    {
        var page = await OpenAsync("/other/page-builder");
        var live = page.Locator("#bit-bdnd-live-region");
        await Tool(page, "Image").FocusAsync();

        await page.Keyboard.PressAsync("Space");
        await Expect(live).ToContainTextAsync("Picked up New image.");
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(live).ToHaveTextAsync("New image is before Heading 1.");
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(live).ToHaveTextAsync("New image is after Heading 1.");
        await page.Keyboard.PressAsync("Enter");

        await Expect(Outline(page)).ToHaveTextAsync("Page: Heading 1, Image 1, Text 1");
    }
}
