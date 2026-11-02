using Bit.Bdnd.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bdnd.Tests.E2E;

/// <summary>
/// The drag events page: the provider's <c>OnDragStart</c> and <c>OnDragEnd</c>, the trays' <c>OnEnter</c>,
/// <c>OnLeave</c> and <c>OnDrop</c> are logged as a drag goes through them, the drop's result makes an undo history, a
/// closed (<c>Disabled</c>) tray takes nothing, and the page reads the drag state from outside the provider.
/// </summary>
[TestClass]
public sealed class DragEventsTests : BdndTest
{
    private static ILocator Tray(IPage page, string name) =>
        page.Locator(".events-tray", new() { Has = page.Locator("h2", new() { HasText = name }) });

    private static ILocator Chip(IPage page, string name) =>
        page.Locator(".events-chip:not([data-bit-bdnd-preview])", new() { HasText = name });

    private static ILocator Log(IPage page) => page.Locator(".events-log li");

    [TestMethod]
    public async Task Every_event_of_a_drag_is_logged_and_undo_puts_the_move_back()
    {
        var page = await OpenAsync("/other/drag-events");
        var today = Tray(page, "Today");
        await today.ScrollIntoViewIfNeededAsync();

        await DragAsync(page, Chip(page, "Red"), today, async () =>
        {
            await Expect(today).ToHaveAttributeAsync("data-bit-bdnd-over", "");
            // read outside the provider, through the manager the page owns
            await Expect(page.Locator(".events-now")).ToHaveTextAsync("Dragging Red - Native");
        });

        await Expect(today.Locator(".events-chip")).ToHaveTextAsync(["Red"]);
        await Expect(page.Locator(".events-now")).ToHaveTextAsync("Not dragging");
        var log = await Log(page).AllTextContentsAsync();
        CollectionAssert.Contains(log.ToArray(), "OnDragStart: Red (type \"chip\"), Native - the browser's own drag and drop.");
        CollectionAssert.Contains(log.ToArray(), "OnLeave: Red left Inbox.");
        CollectionAssert.Contains(log.ToArray(), "OnEnter: Red is over Today.");
        CollectionAssert.AreEqual(new[]
        {
            "OnDrop: Today took Red and published the result \"Red from Inbox to Today\".",
            "OnDragEnd: DidDrop is true, DropResult is \"Red from Inbox to Today\".",
        }, log.TakeLast(2).ToArray(), string.Join(" | ", log));

        await page.GetByRole(AriaRole.Button, new() { Name = "Undo" }).ClickAsync();
        await Expect(Tray(page, "Inbox").Locator(".events-chip")).ToHaveTextAsync(["Red", "Green", "Blue"]);
        await Expect(Log(page).Last).ToHaveTextAsync("Undo: Red went back to Inbox.");
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Undo" })).ToBeDisabledAsync();
    }

    [TestMethod]
    public async Task A_closed_tray_takes_nothing()
    {
        var page = await OpenAsync("/other/drag-events");
        var today = Tray(page, "Today");
        await today.ScrollIntoViewIfNeededAsync();
        await today.GetByLabel("Closed").CheckAsync();

        await StartDragAsync(page, Chip(page, "Green"));
        await Expect(Tray(page, "Later")).ToHaveAttributeAsync("data-bit-bdnd-can-drop", "");
        await Expect(today).Not.ToHaveAttributeAsync("data-bit-bdnd-can-drop", "");
        await DragToAsync(page, (await BoxAsync(today)).Centre);

        await Expect(today.Locator(".events-chip")).ToHaveCountAsync(0);
        await Expect(Log(page).Last).ToHaveTextAsync("OnDragEnd: DidDrop is false, DropResult is null.");
        Assert.IsFalse((await Log(page).AllTextContentsAsync()).Any(line => line.Contains("Today", StringComparison.Ordinal)),
            "a closed tray reported the drag");
    }
}
