using Bit.Bdnd.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bdnd.Tests.E2E;

/// <summary>
/// Separate contexts: every note has the same type and every bin accepts it, yet a provider's notes only light up and
/// drop on its own bins - providers side by side and nested ones alike - while two providers given one manager are one
/// context.
/// </summary>
[TestClass]
public sealed class ContextsTests : BdndTest
{
    private static ILocator Note(IPage page, string name) =>
        page.Locator(".contexts-note:not([data-bit-bdnd-preview])", new() { HasText = name });

    private static ILocator Bin(IPage page, string text) => page.Locator(".contexts-bin", new() { HasText = text });

    [TestMethod]
    public async Task A_note_lights_up_and_drops_on_the_bins_of_its_own_provider_only()
    {
        var page = await OpenAsync("/other/contexts");
        var (binA, binB, nestedBin) = (Bin(page, "Bin A"), Bin(page, "Bin B"), Bin(page, "Nested bin"));
        await nestedBin.ScrollIntoViewIfNeededAsync();

        await StartDragAsync(page, Note(page, "A1"));
        await Expect(binA).ToHaveAttributeAsync("data-bit-bdnd-can-drop", "");
        await Expect(binB).Not.ToHaveAttributeAsync("data-bit-bdnd-can-drop", "");
        await Expect(nestedBin).Not.ToHaveAttributeAsync("data-bit-bdnd-can-drop", "");
        // released over board B's bin: no drop, the note stays on board A
        await DragToAsync(page, (await BoxAsync(binB)).Centre);
        await Expect(binB).ToHaveTextAsync("Bin B: empty");
        await Expect(Note(page, "A1")).ToHaveCountAsync(1);

        await DragAsync(page, Note(page, "A1"), binA, () => Expect(binA).ToHaveAttributeAsync("data-bit-bdnd-over", ""));
        await Expect(binA).ToHaveTextAsync("Bin A: A1");
        await Expect(Note(page, "A1")).ToHaveCountAsync(0);

        // the nested provider's note is not board A's: board A's bin ignores it
        await StartDragAsync(page, Note(page, "Nested"));
        await Expect(nestedBin).ToHaveAttributeAsync("data-bit-bdnd-can-drop", "");
        await Expect(binA).Not.ToHaveAttributeAsync("data-bit-bdnd-can-drop", "");
        await DragToAsync(page, (await BoxAsync(binA)).Centre);
        await Expect(binA).ToHaveTextAsync("Bin A: A1");
        await Expect(Note(page, "Nested")).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task Two_providers_that_share_a_manager_are_one_context()
    {
        var page = await OpenAsync("/other/contexts");
        var shelf = page.Locator(".contexts-shelf", new() { Has = page.Locator("h3", new() { HasText = "Shelf" }) });
        var tray = page.Locator(".contexts-shelf", new() { Has = page.Locator("h3", new() { HasText = "Tray" }) });
        await tray.ScrollIntoViewIfNeededAsync();

        await DragAsync(page, Note(page, "S2"), tray, () => Expect(tray).ToHaveAttributeAsync("data-bit-bdnd-over", ""));

        await Expect(tray.Locator(".contexts-note")).ToHaveTextAsync(["S2"]);
        await Expect(shelf.Locator(".contexts-note")).ToHaveTextAsync(["S1", "S3"]);
    }
}
