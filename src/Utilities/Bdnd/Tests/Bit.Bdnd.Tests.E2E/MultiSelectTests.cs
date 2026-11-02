using Bit.Bdnd.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bdnd.Tests.E2E;

/// <summary>
/// Dragging several items: a drag of a ticked card carries the whole selection - every ticked card shows as dragged
/// and the archive takes them all - while an unticked card drags alone; the keyboard drags the selection too.
/// </summary>
[TestClass]
public sealed class MultiSelectTests : BdndTest
{
    private static ILocator Card(IPage page, string name) =>
        page.Locator(".multi-card:not([data-bit-bdnd-preview])", new() { HasText = name });

    private static ILocator Check(IPage page, string name) => page.GetByRole(AriaRole.Checkbox, new() { Name = $"Select {name}" });

    private static ILocator Archive(IPage page) => page.Locator(".multi-archive");

    private static ILocator Archived(IPage page) => page.Locator(".multi-archived");

    [TestMethod]
    public async Task A_drag_of_a_ticked_card_carries_the_whole_selection()
    {
        var page = await OpenAsync("/sortable/multi-select");
        await Check(page, "Bravo").CheckAsync();
        await Check(page, "Delta").CheckAsync();
        await Expect(Card(page, "Delta")).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("multi-card--selected"));

        await DragAsync(page, Card(page, "Delta"), Archive(page), async () =>
        {
            await Expect(Archive(page)).ToHaveTextAsync("Release to archive 2 card(s)");
            // both ticked cards show as dragged, the others do not
            await Expect(Card(page, "Bravo")).ToHaveAttributeAsync("data-bit-bdnd-dragging", "");
            await Expect(Card(page, "Delta")).ToHaveAttributeAsync("data-bit-bdnd-dragging", "");
            await Expect(Card(page, "Alpha")).Not.ToHaveAttributeAsync("data-bit-bdnd-dragging", "");
        });

        await Expect(Archived(page)).ToHaveTextAsync("Archived: Bravo, Delta");
        await Expect(page.Locator(".multi-card:not([data-bit-bdnd-preview])")).ToHaveCountAsync(4);
    }

    [TestMethod]
    public async Task An_unticked_card_drags_alone()
    {
        var page = await OpenAsync("/sortable/multi-select");
        await Check(page, "Bravo").CheckAsync();

        await DragAsync(page, Card(page, "Echo"), Archive(page), () => Expect(Archive(page)).ToHaveTextAsync("Release to archive 1 card(s)"));

        await Expect(Archived(page)).ToHaveTextAsync("Archived: Echo");
        await Expect(Check(page, "Bravo")).ToBeCheckedAsync();
    }

    [TestMethod]
    public async Task The_keyboard_drags_the_selection()
    {
        var page = await OpenAsync("/sortable/multi-select");
        var live = page.Locator("#bit-bdnd-live-region");
        // the selection is made from the keyboard too: the checkboxes take Space
        await Check(page, "Alpha").FocusAsync();
        await page.Keyboard.PressAsync("Space");
        await Check(page, "Charlie").FocusAsync();
        await page.Keyboard.PressAsync("Space");
        await Expect(Check(page, "Charlie")).ToBeCheckedAsync();
        await WaitForBdndAsync(page.MainFrame);
        await Card(page, "Charlie").FocusAsync();

        await page.Keyboard.PressAsync("Space");
        await Expect(live).ToContainTextAsync("Picked up Charlie.");
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(live).ToHaveTextAsync("Charlie is over the archive.");
        await page.Keyboard.PressAsync("Enter");

        await Expect(Archived(page)).ToHaveTextAsync("Archived: Alpha, Charlie");
    }

    [TestMethod]
    public async Task A_drag_layer_says_how_many_cards_the_drag_carries_and_the_archive_gives_them_back()
    {
        var page = await OpenAsync("/sortable/multi-select");
        var badge = page.Locator(".multi-preview");
        await Check(page, "Alpha").CheckAsync();
        await Check(page, "Charlie").CheckAsync();
        await Check(page, "Echo").CheckAsync();
        await Expect(badge).ToHaveCountAsync(0);

        await DragAsync(page, Card(page, "Charlie"), Archive(page), () => Expect(badge).ToHaveTextAsync("3 cards"));

        await Expect(badge).ToHaveCountAsync(0);
        await Expect(Archived(page)).ToHaveTextAsync("Archived: Alpha, Charlie, Echo");

        await page.GetByRole(AriaRole.Button, new() { Name = "Put them back" }).ClickAsync();
        await Expect(Archived(page)).ToHaveTextAsync("Archived: nothing yet");
        await Expect(page.Locator(".multi-card:not([data-bit-bdnd-preview])")).ToHaveCountAsync(6);
    }
}
