using System.Text.RegularExpressions;
using Bit.Bdnd.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bdnd.Tests.E2E;

/// <summary>
/// A thousand rows in <c>Virtualize</c>: a pointer drag held near the bottom scrolls the list, the dragged row's
/// component leaves the rendered range and the drag goes on, and the rows rendered under the pointer take the drop;
/// a keyboard drag walks the list a row at a time, scrolling as it goes.
/// </summary>
[TestClass]
public sealed class VirtualizedTests : BdndTest
{
    private static ILocator Row(IPage page, string name) =>
        page.Locator(".virtual-row:not([data-bit-bdnd-preview])", new() { HasTextRegex = new Regex($"^\\s*{name}\\s*$") });

    private static ILocator Status(IPage page) => page.Locator(".virtual-status");

    [TestMethod]
    public async Task A_pointer_drag_scrolls_the_list_and_drops_on_a_row_rendered_on_the_way()
    {
        var page = await OpenAsync("/sortable/virtualized");
        var list = await BoxAsync(page.Locator(".virtual-list"));
        var from = (await BoxAsync(Row(page, "Row 1"))).Centre;

        await page.Mouse.MoveAsync((float)from.X, (float)from.Y);
        await page.Mouse.DownAsync();
        await MoveAsync(page, from.Offset(60, 30));
        // PreviewAxis="Vertical": the copy went down with the pointer, not sideways
        await EventuallyAsync(async () =>
        {
            var copy = await page.Locator("[data-bit-bdnd-preview]").BoundingBoxAsync();
            return copy is not null && Math.Abs(copy.X + copy.Width / 2 - from.X) < 1.5 && Math.Abs(copy.Y + copy.Height / 2 - from.Y - 30) < 1.5;
        }, "the copy follows the pointer down and keeps to the vertical axis");
        // held near the bottom of the list, the drag scrolls it until rows far below were rendered
        var nearBottom = new Point(list.Centre.X, list.Bottom - 6);
        var deadline = DateTime.UtcNow.AddSeconds(20);
        for (var round = 0; await Row(page, "Row 40").CountAsync() == 0; round++)
        {
            Assert.IsTrue(DateTime.UtcNow < deadline, "the list never scrolled to Row 40");
            await MoveAsync(page, nearBottom.Offset(round % 2, 0), steps: 1);
            await page.WaitForTimeoutAsync(100);
        }
        // the dragged row was scrolled out of the rendered range: its component is gone, the drag and its copy are not
        await Expect(Row(page, "Row 1")).ToHaveCountAsync(0);
        await Expect(page.Locator("[data-bit-bdnd-preview]")).ToBeVisibleAsync();

        // the middle of the list stops the scrolling; the row there takes the drop
        await MoveAsync(page, list.Centre);
        await WaitForQuietAsync(page, 150);
        await MoveAsync(page, list.Centre.Offset(0, 1), steps: 1);
        var slot = page.Locator(".virtual-slot[data-bit-bdnd-edge]");
        // read once the list has settled: the same row and edge twice, a quiet moment apart
        (int Row, string? Edge) hovered = default, again = (-1, null);
        while (hovered != again)
        {
            hovered = again;
            await WaitForQuietAsync(page, 150);
            await Expect(slot).ToHaveCountAsync(1);
            again = (int.Parse((await slot.InnerTextAsync()).Trim()["Row ".Length..]), await slot.GetAttributeAsync("data-bit-bdnd-edge"));
        }
        var (target, edge) = hovered;
        await page.Mouse.UpAsync();

        // Row 1 leaves place 1: after Row k it is at place k, before it at k - 1
        var place = edge == "bottom" ? target : target - 1;
        await Expect(Status(page)).ToHaveTextAsync($"Row 1 moved to place {place}.");
        Assert.IsGreaterThan(20, target, "the drop landed on a row rendered while the list scrolled");
        await Expect(page.Locator("[data-bit-bdnd-preview]")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task A_keyboard_drag_walks_the_list_past_the_rendered_rows()
    {
        var page = await OpenAsync("/sortable/virtualized");
        var live = page.Locator("#bit-bdnd-live-region");
        await WaitForBdndAsync(page.MainFrame);
        await Row(page, "Row 1").FocusAsync();

        await page.Keyboard.PressAsync("Space");
        await Expect(live).ToContainTextAsync("Picked up Row 1.");
        for (var i = 2; i <= 24; i++)
        {
            await page.Keyboard.PressAsync("ArrowDown");
            await Expect(live).ToHaveTextAsync($"Row 1 is after Row {i}.");
            // at a person's pace: the rows ahead render as the list scrolls (a key pressed before they have finds
            // nothing further - the README says so)
            await WaitForQuietAsync(page, 100);
        }
        // Row 1 was scrolled out of the rendered range on the way
        await Expect(Row(page, "Row 1")).ToHaveCountAsync(0);
        await page.Keyboard.PressAsync("Space");

        await Expect(Status(page)).ToHaveTextAsync("Row 1 moved to place 24.");
        await EventuallyAsync(() => Row(page, "Row 1").EvaluateAsync<bool>("el => document.activeElement === el"),
            "the row rendered at its new place takes the focus");
    }
}
