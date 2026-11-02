using Bit.Bdnd.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bdnd.Tests.E2E;

/// <summary>
/// Insertion edges in the browser: a drop target with <c>Edges</c> reports the edge the pointer is closest to while a
/// card is over it (the drop indicator), the drop lands the card before or after it - within a list or across two -
/// and the keyboard steps through the places between the cards.
/// </summary>
[TestClass]
public sealed class IndicatorTests : BdndTest
{
    private const string Start = "Backlog: Alpha, Bravo, Charlie, Delta | Sprint: Echo, Foxtrot";

    private static ILocator Card(IPage page, string title) =>
        page.Locator(".indicator-card:not([data-bit-bdnd-preview] *):not([data-bit-bdnd-preview])", new() { HasText = title });

    private static ILocator Slot(IPage page, string title) =>
        page.Locator(".indicator-slot", new() { Has = page.Locator(".indicator-card", new() { HasText = title }) });

    private static ILocator Order(IPage page) => page.Locator(".indicator-order");

    [TestMethod]
    public async Task A_card_dropped_on_the_lower_half_of_another_lands_after_it()
    {
        var page = await OpenAsync("/sortable/indicators");
        var charlie = await BoxAsync(Card(page, "Charlie"));
        var below = new Point(charlie.Centre.X, charlie.Y + charlie.Height * 0.8);

        await StartDragAsync(page, Card(page, "Alpha"));
        await DragToAsync(page, below, () => Expect(Slot(page, "Charlie")).ToHaveAttributeAsync("data-bit-bdnd-edge", "bottom"));

        await Expect(Order(page)).ToHaveTextAsync("Backlog: Bravo, Charlie, Alpha, Delta | Sprint: Echo, Foxtrot");
        await Expect(page.Locator("[data-bit-bdnd-edge]")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task A_card_dropped_on_the_upper_half_of_a_card_of_the_other_list_lands_before_it()
    {
        var page = await OpenAsync("/sortable/indicators");
        var echo = await BoxAsync(Card(page, "Echo"));
        var above = new Point(echo.Centre.X, echo.Y + echo.Height * 0.2);

        await StartDragAsync(page, Card(page, "Bravo"));
        await DragToAsync(page, above, () => Expect(Slot(page, "Echo")).ToHaveAttributeAsync("data-bit-bdnd-edge", "top"));

        await Expect(Order(page)).ToHaveTextAsync("Backlog: Alpha, Charlie, Delta | Sprint: Bravo, Echo, Foxtrot");
    }

    [TestMethod]
    public async Task The_indicator_follows_the_pointer_from_one_edge_to_the_other()
    {
        var page = await OpenAsync("/sortable/indicators");
        var slot = Slot(page, "Charlie");
        var charlie = await BoxAsync(Card(page, "Charlie"));

        await StartDragAsync(page, Card(page, "Alpha"));
        await MoveAsync(page, new Point(charlie.Centre.X, charlie.Y + charlie.Height * 0.2));
        await Expect(slot).ToHaveAttributeAsync("data-bit-bdnd-edge", "top");
        await MoveAsync(page, new Point(charlie.Centre.X, charlie.Y + charlie.Height * 0.85));
        await Expect(slot).ToHaveAttributeAsync("data-bit-bdnd-edge", "bottom");
        await MoveAsync(page, (await BoxAsync(Card(page, "Echo"))).Centre);
        await Expect(slot).Not.ToHaveAttributeAsync("data-bit-bdnd-edge", new System.Text.RegularExpressions.Regex(".*"));
        await page.Mouse.UpAsync();
    }

    [TestMethod]
    public async Task Only_the_target_under_the_pointer_shows_an_edge()
    {
        // The drag starts over its own label, whose line appears at once. Firefox rebuilds the label's box for the line
        // (a pseudo-element) and then raises no dragleave as the drag moves on: every label the drag crosses would keep
        // its line and its hover unless the next drag event tells the module the drag has left them.
        var page = await OpenAsync("/sortable/indicators");
        var ui = await BoxAsync(page.Locator(".indicator-chip", new() { HasText = "ui" }));
        var uiSlot = page.Locator(".indicator-chip-slot", new() { Has = page.Locator(".indicator-chip", new() { HasText = "ui" }) });

        await StartDragAsync(page, page.Locator(".indicator-chip", new() { HasText = "bug" }));
        await MoveAsync(page, new Point(ui.X + ui.Width * 0.8, ui.Centre.Y));
        await Expect(uiSlot).ToHaveAttributeAsync("data-bit-bdnd-edge", "right");
        await WaitForQuietAsync(page);

        await Expect(page.Locator("[data-bit-bdnd-edge]")).ToHaveCountAsync(1);
        await Expect(page.Locator("[data-bit-bdnd-over]")).ToHaveCountAsync(1);
        await page.Mouse.UpAsync();
    }

    [TestMethod]
    public async Task The_keyboard_steps_through_the_places_between_the_cards()
    {
        var page = await OpenAsync("/sortable/indicators");
        var live = page.Locator("#bit-bdnd-live-region");
        await WaitForBdndAsync(page.MainFrame);
        await Card(page, "Alpha").FocusAsync();

        await page.Keyboard.PressAsync("Space");
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(live).ToHaveTextAsync("Alpha is after Bravo.");
        await Expect(Slot(page, "Bravo")).ToHaveAttributeAsync("data-bit-bdnd-edge", "bottom");
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(live).ToHaveTextAsync("Alpha is after Charlie.");
        await page.Keyboard.PressAsync("Space");

        await Expect(Order(page)).ToHaveTextAsync("Backlog: Bravo, Charlie, Alpha, Delta | Sprint: Echo, Foxtrot");
        await Expect(live).ToHaveTextAsync("Alpha was dropped after Charlie.");
        await EventuallyAsync(() => Card(page, "Alpha").EvaluateAsync<bool>("el => document.activeElement === el"), "the moved card keeps the focus");

        // into the other list, before its first card
        await page.Keyboard.PressAsync("Space");
        await page.Keyboard.PressAsync("ArrowDown"); // after Delta
        await page.Keyboard.PressAsync("ArrowDown"); // the end of Backlog, after its cards
        await Expect(live).ToHaveTextAsync("Alpha is over the end of Backlog.");
        await page.Keyboard.PressAsync("ArrowDown"); // before Echo
        await Expect(live).ToHaveTextAsync("Alpha is before Echo.");
        await page.Keyboard.PressAsync("Enter");
        await Expect(Order(page)).ToHaveTextAsync("Backlog: Bravo, Charlie, Delta | Sprint: Alpha, Echo, Foxtrot");
    }

    [TestMethod]
    public async Task The_keyboard_never_offers_the_place_the_item_is_in_and_stops_at_the_ends()
    {
        var page = await OpenAsync("/sortable/indicators");
        var live = page.Locator("#bit-bdnd-live-region");
        var labels = page.Locator(".indicator-labels");
        var bug = page.Locator(".indicator-chip:not([data-bit-bdnd-preview])", new() { HasText = "bug" });
        await WaitForBdndAsync(page.MainFrame);
        await bug.FocusAsync();

        await page.Keyboard.PressAsync("Space");
        // the first label: nothing before it, and the arrows do not wrap around to the end of the page
        await page.Keyboard.PressAsync("ArrowLeft");
        await Expect(live).ToHaveTextAsync("There is no drop target in that direction.");
        // "before urgent" is where bug is: the first place to the right is after urgent
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(live).ToHaveTextAsync("bug is after urgent.");
        await Expect(page.Locator(".indicator-chip-slot", new() { Has = page.Locator(".indicator-chip", new() { HasText = "urgent" }) }))
            .ToHaveAttributeAsync("data-bit-bdnd-edge", "right");
        await page.Keyboard.PressAsync("End");
        await Expect(live).ToHaveTextAsync("bug is after ui.");
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(live).ToHaveTextAsync("There is nothing further in that direction: bug stays after ui.");
        await page.Keyboard.PressAsync("Space");

        await Expect(labels).ToHaveTextAsync("Labels: urgent, docs, ui, bug");
    }

    [TestMethod]
    public async Task A_label_dropped_on_the_left_half_of_another_lands_before_it()
    {
        var page = await OpenAsync("/sortable/indicators");
        var docs = await BoxAsync(page.Locator(".indicator-chip", new() { HasText = "docs" }));

        await StartDragAsync(page, page.Locator(".indicator-chip", new() { HasText = "ui" }));
        await DragToAsync(page, new Point(docs.X + docs.Width * 0.2, docs.Centre.Y),
            () => Expect(page.Locator(".indicator-chip-slot", new() { Has = page.Locator(".indicator-chip", new() { HasText = "docs" }) }))
                .ToHaveAttributeAsync("data-bit-bdnd-edge", "left"));

        await Expect(page.Locator(".indicator-labels")).ToHaveTextAsync("Labels: bug, urgent, ui, docs");
    }

    [TestMethod]
    public async Task A_card_dropped_below_the_cards_goes_to_the_end_of_the_list()
    {
        var page = await OpenAsync("/sortable/indicators");
        var sprint = page.Locator(".indicator-list", new() { Has = page.Locator("h2", new() { HasText = "Sprint" }) });
        var box = await BoxAsync(sprint);

        await StartDragAsync(page, Card(page, "Alpha"));
        await DragToAsync(page, new Point(box.Centre.X, box.Bottom - 10),
            () => Expect(sprint).ToHaveAttributeAsync("data-bit-bdnd-over-shallow", ""));

        await Expect(Order(page)).ToHaveTextAsync("Backlog: Bravo, Charlie, Delta | Sprint: Echo, Foxtrot, Alpha");
        Assert.AreNotEqual(Start, await Order(page).TextContentAsync());
    }

    [TestMethod]
    public async Task The_insertion_line_stays_visible_in_forced_colors()
    {
        var page = await OpenAsync("/sortable/indicators");
        // Windows high contrast: the page's colours are replaced by the system's - the demo draws the line in Highlight
        await page.EmulateMediaAsync(new() { ForcedColors = ForcedColors.Active });
        await page.Locator("[draggable]", new() { HasText = "Alpha" }).FocusAsync();

        await page.Keyboard.PressAsync("Space");
        await page.Keyboard.PressAsync("ArrowDown");
        var slot = page.Locator(".indicator-slot[data-bit-bdnd-edge]");
        await Expect(slot).ToHaveCountAsync(1);

        // drawn in the system's Highlight, which an element given that colour shows - against the page's Canvas
        var line = await slot.EvaluateAsync<string[]>(
            """
            el => {
              const a = getComputedStyle(el, '::after');
              const probe = document.createElement('div');
              probe.style.backgroundColor = 'Highlight';
              document.body.appendChild(probe);
              const highlight = getComputedStyle(probe).backgroundColor;
              probe.remove();
              return [a.content, a.height, a.backgroundColor, highlight, getComputedStyle(document.body).backgroundColor];
            }
            """);
        Assert.AreEqual("\"\"", line[0], "the line is drawn");
        Assert.AreEqual("3px", line[1]);
        Assert.AreEqual(line[3], line[2], "the line is drawn in Highlight");
        Assert.AreNotEqual(line[4], line[2], "the line shows against the page");
        await page.Keyboard.PressAsync("Escape");
    }

    [TestMethod]
    public async Task Right_to_left_a_label_dropped_on_the_left_edge_of_another_goes_after_it()
    {
        var page = await OpenAsync("/sortable/indicators");
        await page.GetByLabel("Right to left").CheckAsync();
        var urgent = page.Locator(".indicator-chip:not([data-bit-bdnd-preview])", new() { HasText = "urgent" });
        var urgentSlot = page.Locator(".indicator-chip-slot", new() { Has = urgent });
        var box = await BoxAsync(urgent);

        await StartDragAsync(page, page.Locator(".indicator-chip:not([data-bit-bdnd-preview])", new() { HasText = "bug" }));
        await DragToAsync(page, new Point(box.X + 4, box.Centre.Y), () => Expect(urgentSlot).ToHaveAttributeAsync("data-bit-bdnd-edge", "left"));

        // the list runs right to left: the left edge of "urgent" is after it
        await Expect(page.Locator(".indicator-labels")).ToHaveTextAsync("Labels: urgent, bug, docs, ui");
    }
}
