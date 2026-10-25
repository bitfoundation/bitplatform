using Bit.Bortable.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bortable.Tests.E2E;

/// <summary>
/// The rules page's board, with real drags: a column that refuses a card (<c>CanDrop</c>) is marked as refusing it and
/// a release there puts the card back (<c>RevertOnSpill</c>); a full column (<c>MaxItems</c>) does not take the drag at
/// all; a drop the server refuses (<c>ConfirmDrop</c>) slides back, and one it approves stays. And the accessibility
/// page's buttons, which move items without dragging them.
/// </summary>
[TestClass]
public sealed class RulesTests : BortableTest
{
    private static ILocator Card(IPage page, string list, string title) => Item(page, list, title);

    [TestMethod]
    public async Task A_column_that_refuses_the_card_shows_it_and_a_release_there_puts_the_card_back()
    {
        var page = await OpenAsync("/rules");

        await PressAsync(page, (await BoxAsync(Card(page, "rules-todo", "Write the spec"))).Centre);
        await Expect(page.Locator("#rules-done")).ToHaveAttributeAsync("data-bortable-accepts", "");
        await MoveAsync(page, (await BoxAsync(Card(page, "rules-done", "Set up CI"))).At(0.5, 0.7));

        await Expect(page.Locator("#rules-done")).ToHaveAttributeAsync("data-bortable-refused", "");
        await Expect(Placeholder(page, "rules-done")).ToHaveCountAsync(0);
        await ReleaseAsync(page);

        await AssertOrderAsync(page, "rules-todo", "rules-todo-order", "Write the spec", "Draw the icons", "Plan the launch");
        await AssertOrderAsync(page, "rules-done", "rules-done-order", "Set up CI");
        await Expect(page.Locator("[data-bortable-accepts], [data-bortable-over], [data-bortable-refused]")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task A_full_column_takes_no_more_cards()
    {
        var page = await OpenAsync("/rules");

        // In progress holds two of its three: one more fits - in the slot under the pointer, pushing Review the PR down.
        await DragAsync(page,
            (await BoxAsync(Card(page, "rules-todo", "Write the spec"))).Centre,
            (await BoxAsync(Card(page, "rules-doing", "Review the PR"))).At(0.5, 0.7));
        await AssertOrderAsync(page, "rules-doing", "rules-doing-order", "Build the API", "Write the spec", "Review the PR");

        // Now it is full: it does not even take the drag.
        await PressAsync(page, (await BoxAsync(Card(page, "rules-todo", "Draw the icons"))).Centre);
        await Expect(page.Locator("#rules-todo")).ToHaveAttributeAsync("data-bortable-accepts", "");
        await Expect(page.Locator("#rules-doing")).Not.ToHaveAttributeAsync("data-bortable-accepts", "");
        await MoveAsync(page, (await BoxAsync(Card(page, "rules-doing", "Review the PR"))).Centre);
        await Expect(Placeholder(page, "rules-doing")).ToHaveCountAsync(0);
        await ReleaseAsync(page);

        await AssertOrderAsync(page, "rules-doing", "rules-doing-order", "Build the API", "Write the spec", "Review the PR");
        await AssertOrderAsync(page, "rules-todo", "rules-todo-order", "Draw the icons", "Plan the launch");
    }

    [TestMethod]
    public async Task A_drop_the_server_refuses_slides_back_and_one_it_approves_stays()
    {
        var page = await OpenAsync("/rules");
        await page.Locator("#rules-approve").UncheckAsync();
        await WaitForQuietAsync(page);

        await DragAsync(page,
            (await BoxAsync(Card(page, "rules-doing", "Build the API"))).Centre,
            (await BoxAsync(Card(page, "rules-done", "Set up CI"))).At(0.5, 0.7));
        await Expect(page.Locator("#rules-status")).ToHaveTextAsync("refused Build the API");
        await AssertOrderAsync(page, "rules-doing", "rules-doing-order", "Build the API", "Review the PR");
        await AssertOrderAsync(page, "rules-done", "rules-done-order", "Set up CI");

        await page.Locator("#rules-approve").CheckAsync();
        await WaitForQuietAsync(page);
        await DragAsync(page,
            (await BoxAsync(Card(page, "rules-doing", "Review the PR"))).Centre,
            (await BoxAsync(Card(page, "rules-done", "Set up CI"))).At(0.5, 0.7));
        await Expect(page.Locator("#rules-status")).ToHaveTextAsync("saved Review the PR");
        await AssertOrderAsync(page, "rules-doing", "rules-doing-order", "Build the API");
        // In the slot under the pointer: Set up CI is pushed down.
        await AssertOrderAsync(page, "rules-done", "rules-done-order", "Review the PR", "Set up CI");
    }

    [TestMethod]
    public async Task Buttons_move_items_without_dragging_and_say_so()
    {
        var page = await OpenAsync("/accessibility");

        await page.GetByRole(AriaRole.Button, new() { Name = "Move Search down" }).ClickAsync();
        await WaitForQuietAsync(page);
        await Expect(page.Locator("#a11y-sprint-order")).ToHaveTextAsync("Export to CSV, Search");
        await Expect(page.Locator("[data-bortable-live]")).ToHaveTextAsync("Search moved to position 2 of 2 in Sprint.");

        await page.GetByRole(AriaRole.Button, new() { Name = "Move Audit log to the Sprint" }).ClickAsync();
        await WaitForQuietAsync(page);
        await Expect(page.Locator("#a11y-sprint-order")).ToHaveTextAsync("Export to CSV, Search, Audit log");
        await Expect(page.Locator("#a11y-backlog-order")).ToHaveTextAsync("Sign-up page, Password reset, Dark mode");
        await Expect(page.Locator("#a11y-log")).ToContainTextAsync("[api] Audit log: a11y-backlog -> a11y-sprint#2");
    }
}
