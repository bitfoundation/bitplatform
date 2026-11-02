using Bit.Bheadless.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bheadless.Tests.E2E;

/// <summary>
/// What the unit suite cannot prove, since bUnit stands in for the browser: the owned DOM events
/// (<c>bitbhkeydown</c> / <c>bitbhclick</c>) the JS initializer registers with Blazor actually fire in a running app,
/// and a secondary press in a context menu's trigger moves the open menu instead of dismissing it.
/// </summary>
[TestClass]
public sealed class OwnedEventTests : DemoTest
{
    [TestMethod]
    public async Task Grid_cells_move_focus_and_activate_through_the_owned_events()
    {
        var page = await OpenDemoAsync("/grid");
        var grid = page.Locator("main table[aria-label=Planets]");
        var header = grid.Locator("th[role=columnheader]").First;
        var firstBodyCell = grid.Locator("tbody [role=rowheader]").First;

        // Arrow keys reach the cells only through bitbhkeydown: a registration that never happened leaves them dead.
        await firstBodyCell.FocusAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        await PollAsync(() => page.EvaluateAsync<string>("() => document.activeElement.getAttribute('role')"), "gridcell", "the focused element's role");
        await page.Keyboard.PressAsync("ArrowUp");
        await PollAsync(() => page.EvaluateAsync<string>("() => document.activeElement.getAttribute('role')"), "columnheader", "the focused element's role");

        // Enter and a click activate a header (its OnActivate sorts), through bitbhkeydown and bitbhclick.
        await header.FocusAsync();
        var initial = await header.GetAttributeAsync("aria-sort");
        await page.Keyboard.PressAsync("Enter");
        var afterEnter = await PollAsync(() => header.GetAttributeAsync("aria-sort"), sort => sort != initial, "the sort to change on Enter");
        await header.ClickAsync();
        await PollAsync(() => header.GetAttributeAsync("aria-sort"), sort => sort != afterEnter, "the sort to change on a click");
    }

    [TestMethod]
    public async Task A_right_click_in_the_trigger_of_an_open_context_menu_moves_it_without_closing_it()
    {
        var page = await OpenDemoAsync("/context-menu");
        var trigger = page.Locator("main .cm-trigger").First;
        await trigger.ClickAsync(new LocatorClickOptions { Button = MouseButton.Right, Position = new Position { X = 10, Y = 10 } });

        var menu = page.Locator("[role=menu]:visible").First;
        await Expect(menu).ToBeVisibleAsync();
        var before = await menu.BoundingBoxAsync();

        // Watch the menu from here on: dismissing and reopening it would pass through data-state="closed".
        await menu.EvaluateAsync("""
            el => {
              window.__bitBhClosed = false;
              new MutationObserver(() => { if (el.getAttribute('data-state') !== 'open' || el.hidden) window.__bitBhClosed = true; })
                .observe(el, { attributes: true, attributeFilter: ['data-state', 'hidden'] });
            }
            """);
        var box = await trigger.BoundingBoxAsync();
        await trigger.ClickAsync(new LocatorClickOptions
        {
            Button = MouseButton.Right,
            Position = new Position { X = Math.Max(20, box!.Width - 20), Y = Math.Max(20, box.Height - 20) },
        });

        await PollAsync(async () => (await menu.BoundingBoxAsync())?.X, x => x != before?.X, "the menu's position to change");
        Assert.IsFalse(await page.EvaluateAsync<bool>("() => window.__bitBhClosed"), "The menu closed and reopened instead of moving.");
        await Expect(menu).ToBeVisibleAsync();
    }
}
