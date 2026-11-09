using System.Text.RegularExpressions;
using Bit.Bheadless.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bheadless.Tests.E2E;

/// <summary>
/// The navigation menu and the drawer in a real browser: focus moving into and out of contents (also through a
/// shared viewport), hover with intent, the viewport's measured size, the indicator, a drawer's focus trap and its
/// swipe to dismiss - plus axe-core on the open states.
/// </summary>
[TestClass]
public sealed class NavigationMenuAndDrawerTests : DemoTest
{
    private static Task<string?> ActiveTextAsync(IPage page)
        => page.EvaluateAsync<string?>("() => document.activeElement?.textContent?.trim() ?? null");

    [TestMethod]
    public async Task Navigation_menu_ArrowDown_enters_the_content_and_Escape_returns_to_the_trigger()
    {
        var page = await OpenDemoAsync("/navigation-menu");
        var nav = page.Locator($"{Content} nav[aria-label='Products (example)']");
        var trigger = nav.Locator("button", new LocatorLocatorOptions { HasText = "Products" });
        await trigger.FocusAsync();
        await page.Keyboard.PressAsync("ArrowDown");

        await Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");
        var content = page.Locator($"[id='{await trigger.GetAttributeAsync("aria-controls")}']");
        await Expect(content).ToBeVisibleAsync();
        await PollAsync(() => ActiveTextAsync(page), text => text?.StartsWith("Analytics", StringComparison.Ordinal) == true, "the first link focused");
        await page.Keyboard.PressAsync("ArrowDown");
        await PollAsync(() => ActiveTextAsync(page), text => text?.StartsWith("Automation", StringComparison.Ordinal) == true, "the next link focused");
        await ExpectAccessibleAsync(page);

        await page.Keyboard.PressAsync("Escape");
        await Expect(content).ToBeHiddenAsync();
        await Expect(trigger).ToBeFocusedAsync();

        // Arrow keys move along the bar; Enter toggles; Tab from the open trigger enters its content.
        await page.Keyboard.PressAsync("ArrowRight");
        var resources = nav.Locator("button", new LocatorLocatorOptions { HasText = "Resources" });
        await Expect(resources).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Enter");
        await Expect(resources).ToHaveAttributeAsync("aria-expanded", "true");
        await page.Keyboard.PressAsync("Tab");
        await PollAsync(() => ActiveTextAsync(page), "Guides", "the focused link");
    }

    [TestMethod]
    public async Task Navigation_menu_viewport_takes_the_content_and_keeps_the_Tab_order()
    {
        var page = await OpenDemoAsync("/navigation-menu");
        var nav = page.Locator($"{Content} nav[aria-label='Site (viewport example)']");
        var learn = nav.Locator("button", new LocatorLocatorOptions { HasText = "Learn" });
        var overview = nav.Locator("button", new LocatorLocatorOptions { HasText = "Overview" });
        var viewport = nav.Locator(".navigation-menu-viewport");
        var indicator = nav.Locator(".navigation-menu-indicator");

        // Hover with intent.
        await learn.HoverAsync();
        await Expect(learn).ToHaveAttributeAsync("aria-expanded", "true", new LocatorAssertionsToHaveAttributeOptions { Timeout = 3_000 });
        await Expect(viewport).ToBeVisibleAsync();
        var learnContent = page.Locator($"[id='{await learn.GetAttributeAsync("aria-controls")}']");
        // The content was moved into the viewport, which took its size; the indicator shows.
        var viewportId = await viewport.GetAttributeAsync("id");
        await PollAsync(() => learnContent.EvaluateAsync<bool>("(el, id) => document.getElementById(id).contains(el)", viewportId), true, "the content inside the viewport");
        await PollAsync(() => viewport.EvaluateAsync<double>("el => parseFloat(el.style.getPropertyValue('--bit-bh-navigation-menu-viewport-width')) || 0"), w => w > 50, "the viewport's measured width");
        await Expect(indicator).ToHaveAttributeAsync("data-state", "visible");

        // Moving to the next trigger switches at once, and the new content comes from the end.
        await overview.HoverAsync();
        await Expect(overview).ToHaveAttributeAsync("aria-expanded", "true");
        var overviewContent = page.Locator($"[id='{await overview.GetAttributeAsync("aria-controls")}']");
        await Expect(overviewContent).ToHaveAttributeAsync("data-motion", "from-end");
        await ExpectAccessibleAsync(page);

        // Keyboard: Tab from the open trigger goes into the content in the viewport, Tab from its last link to the next item.
        await page.Mouse.MoveAsync(1, 1);
        await Expect(overview).ToHaveAttributeAsync("aria-expanded", "false", new LocatorAssertionsToHaveAttributeOptions { Timeout = 3_000 });
        await learn.FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await Expect(learn).ToHaveAttributeAsync("aria-expanded", "true");
        await page.Keyboard.PressAsync("Tab");
        await PollAsync(() => ActiveTextAsync(page), text => text?.StartsWith("Introduction", StringComparison.Ordinal) == true, "the first link of the content");
        for (var i = 0; i < 3; i++) await page.Keyboard.PressAsync("Tab");
        await PollAsync(() => ActiveTextAsync(page), text => text?.StartsWith("Testing", StringComparison.Ordinal) == true, "the last link of the content");
        await page.Keyboard.PressAsync("Tab");
        await Expect(overview).ToBeFocusedAsync();
        // Leaving the content closed it.
        await Expect(learn).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [TestMethod]
    public async Task Drawer_traps_focus_and_restores_it_and_a_swipe_dismisses_it()
    {
        var page = await OpenDemoAsync("/drawer");
        var trigger = page.Locator($"{Content} button", new PageLocatorOptions { HasTextRegex = new Regex("^Open sheet$") }).First;
        await trigger.ClickAsync();

        var panel = page.Locator("[role=dialog]");
        await Expect(panel).ToBeVisibleAsync();
        await Expect(panel).ToHaveAttributeAsync("aria-modal", "true");
        await Expect(panel).ToHaveAttributeAsync("data-side", "bottom");
        await PollAsync(() => page.EvaluateAsync<bool>("() => !!document.activeElement.closest('[role=dialog]')"), true, "the focus inside the drawer");
        await PollAsync(() => page.Locator("[inert]").CountAsync(), count => count > 0, "inert elements behind the drawer");
        await page.Keyboard.PressAsync("Escape");
        await Expect(panel).ToBeHiddenAsync();
        await Expect(trigger).ToBeFocusedAsync();

        // A short drag on the handle settles back; a long one dismisses.
        await trigger.ClickAsync();
        await Expect(panel).ToBeVisibleAsync();
        await page.WaitForTimeoutAsync(400); // the enter animation
        var handle = page.Locator("[data-bit-bh-drawer-handle]");
        var box = (await handle.BoundingBoxAsync())!;
        var x = box.X + box.Width / 2;
        var y = box.Y + box.Height / 2;

        await page.Mouse.MoveAsync(x, y);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(x, y + 10, new MouseMoveOptions { Steps = 5 });
        await Expect(panel).ToHaveAttributeAsync("data-swiping", "");
        await page.Mouse.UpAsync();
        await Expect(panel).Not.ToHaveAttributeAsync("data-swiping", "");
        await page.WaitForTimeoutAsync(400);
        await Expect(panel).ToBeVisibleAsync();

        await page.Mouse.MoveAsync(x, y);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(x, y + 400, new MouseMoveOptions { Steps = 20 });
        var progress = await panel.EvaluateAsync<double>("el => parseFloat(el.style.getPropertyValue('--bit-bh-drawer-swipe-progress'))");
        Assert.IsGreaterThan(0.25, progress);
        await page.Mouse.UpAsync();
        await Expect(panel).ToBeHiddenAsync();
        await Expect(trigger).ToBeFocusedAsync();
    }
}
