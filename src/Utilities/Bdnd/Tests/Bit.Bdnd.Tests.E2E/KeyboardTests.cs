using Bit.Bdnd.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bdnd.Tests.E2E;

/// <summary>
/// Keyboard drags in the browser: Space picks a focused source up, the arrow keys move it between the targets that
/// can take it, Space drops and Escape cancels; the focus stays on the dragged item - also while a sortable list
/// reorders it under the drag - and a live region tells screen readers what happens.
/// </summary>
[TestClass]
public sealed class KeyboardTests : BdndTest
{
    private const string DarkGreen = "rgb(0, 100, 0)";

    private static ILocator LiveRegion(IPage page) => page.Locator("#bit-bdnd-live-region");

    /// <summary>The draggable host around the element <paramref name="content"/> finds: the element the keyboard focuses.</summary>
    private static ILocator SourceOf(IPage page, ILocator content) =>
        page.Locator("[draggable]", new() { Has = content });

    private static Task<bool> IsFocusedAsync(ILocator locator) => locator.EvaluateAsync<bool>("el => document.activeElement === el");

    private static async Task FocusAsync(IPage page, ILocator source)
    {
        await WaitForBdndAsync(page.MainFrame);
        await source.FocusAsync();
        Assert.IsTrue(await IsFocusedAsync(source), "the source takes the focus");
    }

    [TestMethod]
    public async Task Space_picks_a_box_up_the_arrows_move_it_onto_the_bin_and_space_drops_it()
    {
        var page = await OpenAsync("/dustbin/single");
        var bin = page.Locator(".dustbin");
        var glass = page.Locator(".box", new() { HasText = "Glass" });
        var source = SourceOf(page, glass);
        await FocusAsync(page, source);
        var scrollBefore = await page.EvaluateAsync<double>("() => window.scrollY");

        await page.Keyboard.PressAsync("Space");
        await Expect(glass).ToHaveCSSAsync("opacity", "0.4");
        await Expect(LiveRegion(page)).ToHaveTextAsync("Picked up Glass. 1 drop target is available.");
        Assert.AreEqual(scrollBefore, await page.EvaluateAsync<double>("() => window.scrollY"), "Space did not scroll the page");

        await page.Keyboard.PressAsync("ArrowUp");
        await Expect(bin).ToHaveTextAsync("Release to drop");
        await Expect(bin).ToHaveCSSAsync("background-color", DarkGreen);
        await Expect(LiveRegion(page)).ToHaveTextAsync("Glass is over drop target 1 of 1.");

        await page.Keyboard.PressAsync("Space");
        await Expect(bin).ToHaveTextAsync("Last dropped: Glass (total 1)");
        await Expect(glass).ToHaveCSSAsync("opacity", "1");
        await Expect(LiveRegion(page)).ToHaveTextAsync("Glass was dropped on drop target 1 of 1.");
        Assert.IsTrue(await IsFocusedAsync(source), "the focus stays on the source");
        Assert.AreEqual(0, (await DropsAsync(page.MainFrame)).Count, "no native drop event");

        // and once more, with Enter
        await page.Keyboard.PressAsync("Enter");
        await page.Keyboard.PressAsync("ArrowUp");
        await Expect(bin).ToHaveTextAsync("Release to drop");
        await page.Keyboard.PressAsync("Enter");
        await Expect(bin).ToHaveTextAsync("Last dropped: Glass (total 2)");
    }

    [TestMethod]
    public async Task Escape_cancels_a_keyboard_drag_and_keeps_the_focus()
    {
        var page = await OpenAsync("/dustbin/single");
        var bin = page.Locator(".dustbin");
        var glass = page.Locator(".box", new() { HasText = "Glass" });
        var source = SourceOf(page, glass);
        await FocusAsync(page, source);

        await page.Keyboard.PressAsync("Space");
        await page.Keyboard.PressAsync("ArrowUp");
        await Expect(bin).ToHaveTextAsync("Release to drop");
        await page.Keyboard.PressAsync("Escape");

        await Expect(bin).ToHaveTextAsync("Drag a box here");
        await Expect(glass).ToHaveCSSAsync("opacity", "1");
        await Expect(LiveRegion(page)).ToHaveTextAsync("Dragging Glass was cancelled.");
        Assert.IsTrue(await IsFocusedAsync(source));
    }

    [TestMethod]
    public async Task Tabbing_away_cancels_a_keyboard_drag()
    {
        var page = await OpenAsync("/dustbin/single");
        var bin = page.Locator(".dustbin");
        var glass = page.Locator(".box", new() { HasText = "Glass" });
        await FocusAsync(page, SourceOf(page, glass));

        await page.Keyboard.PressAsync("Space");
        await page.Keyboard.PressAsync("ArrowUp");
        await Expect(bin).ToHaveTextAsync("Release to drop");
        await page.Keyboard.PressAsync("Tab");

        await Expect(bin).ToHaveTextAsync("Drag a box here");
        await Expect(LiveRegion(page)).ToHaveTextAsync("Dragging Glass was cancelled.");
        Assert.IsTrue(await IsFocusedAsync(SourceOf(page, page.Locator(".box", new() { HasText = "Banana" }))),
            "Tab moved the focus on to the next box");
    }

    [TestMethod]
    public async Task A_pointer_press_cancels_a_keyboard_drag_and_a_mouse_drag_works_right_after()
    {
        var page = await OpenAsync("/dustbin/single");
        var bin = page.Locator(".dustbin");
        var glass = page.Locator(".box", new() { HasText = "Glass" });
        await FocusAsync(page, SourceOf(page, glass));
        await page.Keyboard.PressAsync("Space");
        await page.Keyboard.PressAsync("ArrowUp");
        await Expect(bin).ToHaveTextAsync("Release to drop");

        await DragAsync(page, page.Locator(".box", new() { HasText = "Banana" }), bin,
            () => Expect(bin).ToHaveTextAsync("Release to drop"));

        await Expect(bin).ToHaveTextAsync("Last dropped: Banana (total 1)");
        await Expect(glass).ToHaveCSSAsync("opacity", "1");
    }

    [TestMethod]
    public async Task The_arrows_reorder_a_sortable_list_one_place_a_key_and_the_focus_follows_the_card()
    {
        var page = await OpenAsync("/sortable/simple");
        var order = page.Locator(".sortable-order");
        var source = SourceOf(page, page.Locator(".sortable-card", new() { HasText = "Write the docs" }));
        await FocusAsync(page, source);

        await page.Keyboard.PressAsync("Space");
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(order).ToHaveTextAsync("Order: Review the PR, Write the docs, Ship the release, Celebrate, Sleep");
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(order).ToHaveTextAsync("Order: Review the PR, Ship the release, Write the docs, Celebrate, Sleep");
        await page.Keyboard.PressAsync("ArrowUp");
        await Expect(order).ToHaveTextAsync("Order: Review the PR, Write the docs, Ship the release, Celebrate, Sleep");
        await EventuallyAsync(() => IsFocusedAsync(source), "the moved card keeps the focus");

        await page.Keyboard.PressAsync("Space");
        await Expect(LiveRegion(page)).ToContainTextAsync("was dropped on");
        await Expect(order).ToHaveTextAsync("Order: Review the PR, Write the docs, Ship the release, Celebrate, Sleep");
        Assert.IsTrue(await IsFocusedAsync(source));
    }

    [TestMethod]
    public async Task A_kanban_card_moves_across_columns_by_keyboard_and_the_moved_card_keeps_the_focus()
    {
        var page = await OpenAsync("/accessibility/keyboard");
        var status = page.Locator(".kanban-status");
        var doing = page.Locator(".kanban-column", new() { Has = page.Locator("h2", new() { HasText = "Doing" }) });
        var done = page.Locator(".kanban-column", new() { Has = page.Locator("h2", new() { HasText = "Done" }) });
        await FocusAsync(page, Card(page, "Write the docs"));

        await page.Keyboard.PressAsync("Space");
        await Expect(LiveRegion(page)).ToHaveTextAsync("Picked up Write the docs. 2 drop targets are available.");
        // the columns that can take the card show it, from the data-bit-bdnd-can-drop attribute alone
        await Expect(page.Locator(".kanban-column[data-bit-bdnd-can-drop]")).ToHaveCountAsync(3);
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(LiveRegion(page)).ToHaveTextAsync("Write the docs is over Doing.");
        await Expect(doing).ToHaveAttributeAsync("data-bit-bdnd-over", "");
        await page.Keyboard.PressAsync("Space");

        await Expect(status).ToHaveTextAsync("Moved Write the docs from To do to Doing.");
        await Expect(doing.Locator(".kanban-card", new() { HasText = "Write the docs" })).ToHaveCountAsync(1);
        await Expect(LiveRegion(page)).ToHaveTextAsync("Write the docs was dropped on Doing.");
        // the card is a new element in the other column, and it has the focus
        await EventuallyAsync(() => IsFocusedAsync(Card(page, "Write the docs")), "the moved card takes the focus over");

        // "Doing" is full now: a card from "To do" can only go to "Done", and the arrows skip "Doing" (and the
        // card's own column, where it already is)
        await Card(page, "Review the PR").FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await Expect(LiveRegion(page)).ToHaveTextAsync("Picked up Review the PR. 1 drop target is available.");
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(LiveRegion(page)).ToHaveTextAsync("Review the PR is over Done.");
        await page.Keyboard.PressAsync("Enter");
        await Expect(done.Locator(".kanban-card", new() { HasText = "Review the PR" })).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task The_move_buttons_are_a_non_drag_alternative()
    {
        var page = await OpenAsync("/accessibility/keyboard");
        var doing = page.Locator(".kanban-column", new() { Has = page.Locator("h2", new() { HasText = "Doing" }) });

        await page.GetByRole(AriaRole.Button, new() { Name = "Move Write the docs to Doing" }).ClickAsync();

        await Expect(page.Locator(".kanban-status")).ToHaveTextAsync("Moved Write the docs from To do to Doing.");
        await Expect(doing.Locator(".kanban-card", new() { HasText = "Write the docs" })).ToHaveCountAsync(1);
        // a click on a button inside a draggable starts no drag
        Assert.IsEmpty(await DragStartsAsync(page.MainFrame));
    }

    private static ILocator Card(IPage page, string title) => page.Locator(".kanban-card", new() { HasText = title });

    [TestMethod]
    public async Task Escape_on_a_sortable_list_dropped_outside_reverts_the_order()
    {
        var page = await OpenAsync("/sortable/cancel-on-drop-outside");
        var order = page.Locator(".sortable-order");
        var before = await order.TextContentAsync();
        var source = SourceOf(page, page.Locator(".sortable-card", new() { HasText = "Alpha" }));
        await FocusAsync(page, source);

        await page.Keyboard.PressAsync("Space");
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(order).Not.ToHaveTextAsync(before!);
        await page.Keyboard.PressAsync("Escape");

        await Expect(order).ToHaveTextAsync(before!);
        // the revert re-rendered the card back into its place, and it still has the focus
        await EventuallyAsync(() => IsFocusedAsync(source), "the reverted card keeps the focus");
    }
}
