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

    // ── Drawer snap points ───────────────────────────────────────────────────

    /// <summary>
    /// How much of the panel shows: a bottom sheet's from the bottom of the screen; a side panel's from where it rests when
    /// fully open (its width less how far it is moved towards its edge) - the demo's scroll lock keeps a scrollbar's gutter.
    /// </summary>
    private static Task<double> ShownAsync(ILocator panel) => panel.EvaluateAsync<double>(
        """
        el => {
          const r = el.getBoundingClientRect();
          const moved = new DOMMatrixReadOnly(getComputedStyle(el).transform);
          switch (el.dataset.side) {
            case 'bottom': return innerHeight - r.top;
            case 'top': return r.bottom;
            default: return r.width - Math.abs(moved.m41);
          }
        }
        """);

    /// <summary>axe-core finds nothing in the open drawer, which is portalled out of the page's content.</summary>
    private static async Task ExpectDrawerAccessibleAsync(IPage page)
    {
        await ExpectAccessibleAsync(page);
        var violations = await Axe.ViolationsAsync(page, "[role=dialog]");
        Assert.IsEmpty(violations, $"axe-core found {violations.Length} violation(s) in the drawer on {page.Url}:{Environment.NewLine}{string.Join(Environment.NewLine + Environment.NewLine, violations)}");
    }

    private static Task<double> ShowsAboutAsync(ILocator panel, double size, string expectation)
        => PollAsync(() => ShownAsync(panel), shown => Math.Abs(shown - size) < 2, $"{expectation} ({size}px) showing");

    private static async Task<(double X, double Y)> CenterAsync(ILocator element)
    {
        var box = (await element.BoundingBoxAsync())!;
        return (box.X + box.Width / 2, box.Y + box.Height / 2);
    }

    /// <summary>A drag in frames apart, held still before it is let go: never a flick.</summary>
    private static async Task DragAsync(IPage page, ILocator from, double dx, double dy)
    {
        var (x, y) = await CenterAsync(from);
        await page.Mouse.MoveAsync((float)x, (float)y);
        await page.Mouse.DownAsync();
        for (var i = 1; i <= 10; i++)
        {
            await page.Mouse.MoveAsync((float)(x + dx * i / 10), (float)(y + dy * i / 10));
            await page.WaitForTimeoutAsync(20);
        }
        await page.WaitForTimeoutAsync(250);
        await page.Mouse.UpAsync();
    }

    /// <summary>A short, quick move let go at once.</summary>
    private static async Task FlickAsync(IPage page, ILocator from, double dx, double dy)
    {
        var (x, y) = await CenterAsync(from);
        await page.Mouse.MoveAsync((float)x, (float)y);
        await page.Mouse.DownAsync();
        for (var i = 1; i <= 3; i++)
        {
            await page.Mouse.MoveAsync((float)(x + dx * i / 3), (float)(y + dy * i / 3));
            await page.WaitForTimeoutAsync(16);
        }
        await page.Mouse.UpAsync();
    }

    [TestMethod]
    public async Task Drawer_snap_points_follow_drags_flicks_the_keyboard_and_the_focus()
    {
        var page = await OpenDemoAsync("/drawer");
        var trigger = page.Locator($"{Content} button", new PageLocatorOptions { HasTextRegex = new Regex("^Bottom sheet$") });
        var output = page.Locator($"{Content} .demo-output", new PageLocatorOptions { HasTextString = "Snap point:" });
        var viewport = page.ViewportSize!.Height;
        await trigger.ClickAsync();

        // It opens at the smallest snap point, a 160px peek of a sheet as tall as the screen.
        var panel = page.Locator("[role=dialog]");
        var handle = panel.Locator("[role=slider]");
        await Expect(panel).ToHaveAttributeAsync("data-snap-point", "160px");
        await Expect(handle).ToHaveAttributeAsync("aria-valuenow", "1");
        await Expect(handle).ToHaveAttributeAsync("aria-valuetext", "1 of 3");
        await ShowsAboutAsync(panel, 160, "the peek");
        await ExpectDrawerAccessibleAsync(page);

        // A drag settles on the nearest snap point: 20px short of half the screen is half the screen.
        await DragAsync(page, handle, 0, -(viewport / 2.0 - 160 - 20));
        await Expect(panel).ToHaveAttributeAsync("data-snap-point", "0.5");
        await Expect(output).ToHaveTextAsync("Snap point: 0.5");
        await ShowsAboutAsync(panel, viewport / 2.0, "half the screen");

        // The handle is a slider: the arrow pointing away from the edge opens the sheet further, Enter cycles.
        await handle.FocusAsync();
        await page.Keyboard.PressAsync("End");
        await Expect(panel).ToHaveAttributeAsync("data-snap-point", "1");
        await Expect(handle).ToHaveAttributeAsync("aria-valuetext", "3 of 3");
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(panel).ToHaveAttributeAsync("data-snap-point", "0.5");
        await page.Keyboard.PressAsync("Home");
        await Expect(panel).ToHaveAttributeAsync("data-snap-point", "160px");
        await page.Keyboard.PressAsync("Enter");
        await Expect(panel).ToHaveAttributeAsync("data-snap-point", "0.5");
        await page.Keyboard.PressAsync("ArrowUp");
        await Expect(panel).ToHaveAttributeAsync("data-snap-point", "1");
        await ExpectDrawerAccessibleAsync(page);

        // Tab into what the peek hides: the sheet opens far enough to show it.
        await page.Keyboard.PressAsync("Home");
        await Expect(panel).ToHaveAttributeAsync("data-snap-point", "160px");
        await ShowsAboutAsync(panel, 160, "the peek");
        await page.Keyboard.PressAsync("Tab");
        await Expect(panel).Not.ToHaveAttributeAsync("data-snap-point", "160px");
        await PollAsync(() => page.EvaluateAsync<bool>(
            "() => { const r = document.activeElement.getBoundingClientRect(); return r.top >= 0 && r.bottom <= innerHeight + 1; }"),
            true, "the focused element on screen");
        await Expect(output).Not.ToHaveTextAsync("Snap point: 160px");

        // A flick moves on past the snap point a drag of that length would settle back on.
        await handle.FocusAsync();
        await page.Keyboard.PressAsync("Home");
        await ShowsAboutAsync(panel, 160, "the peek");
        await FlickAsync(page, handle, 0, -90);
        await Expect(panel).Not.ToHaveAttributeAsync("data-snap-point", "160px");

        // Dragged below the smallest snap point, past a quarter of it, the sheet closes.
        await handle.FocusAsync();
        await page.Keyboard.PressAsync("Home");
        await ShowsAboutAsync(panel, 160, "the peek");
        await DragAsync(page, handle, 0, 100);
        await Expect(panel).ToBeHiddenAsync();
        await Expect(trigger).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Drawer_snap_points_work_from_the_side_too()
    {
        var page = await OpenDemoAsync("/drawer");
        var trigger = page.Locator($"{Content} button", new PageLocatorOptions { HasTextRegex = new Regex("^Side panel$") });
        await trigger.ClickAsync();

        var panel = page.Locator("[role=dialog]");
        var handle = panel.Locator("[role=slider]");
        await Expect(panel).ToHaveAttributeAsync("data-side", "right");
        await Expect(panel).ToHaveAttributeAsync("data-snap-point", "160px");
        await Expect(handle).ToHaveAttributeAsync("aria-orientation", "horizontal");
        await ShowsAboutAsync(panel, 160, "the peek");

        // Left - away from the right edge - opens it further; a drag to the left settles on the nearest snap point.
        await handle.FocusAsync();
        await page.Keyboard.PressAsync("ArrowLeft");
        await Expect(panel).ToHaveAttributeAsync("data-snap-point", "360px");
        await ShowsAboutAsync(panel, 360, "the second snap point");
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(panel).ToHaveAttributeAsync("data-snap-point", "160px");
        await ShowsAboutAsync(panel, 160, "the peek");
        await DragAsync(page, handle, -180, 0);
        await Expect(panel).ToHaveAttributeAsync("data-snap-point", "360px");
        await ShowsAboutAsync(panel, 360, "the second snap point");
        await ExpectDrawerAccessibleAsync(page);

        // Escape still closes it from any snap point.
        await page.Keyboard.PressAsync("Escape");
        await Expect(panel).ToBeHiddenAsync();
        await Expect(trigger).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Drawer_snap_points_in_the_Tailwind_demo()
    {
        var page = await OpenDemoAsync("/tw/drawer");
        var trigger = page.Locator($"{Content} button", new PageLocatorOptions { HasTextRegex = new Regex("^Open trip$") });
        await trigger.ClickAsync();

        var panel = page.Locator("[role=dialog]");
        var handle = panel.Locator("[role=slider]");
        await Expect(panel).ToHaveAttributeAsync("data-snap-point", "160px");
        await ShowsAboutAsync(panel, 160, "the peek");
        await ExpectDrawerAccessibleAsync(page);

        await handle.FocusAsync();
        await page.Keyboard.PressAsync("ArrowUp");
        await Expect(panel).ToHaveAttributeAsync("data-snap-point", "0.5");
        await ShowsAboutAsync(panel, page.ViewportSize!.Height / 2.0, "half the screen");
        await page.Keyboard.PressAsync("End");
        await Expect(panel).ToHaveAttributeAsync("data-snap-point", "1");
        await ExpectDrawerAccessibleAsync(page);

        await page.Keyboard.PressAsync("Escape");
        await Expect(panel).ToBeHiddenAsync();
        await Expect(trigger).ToBeFocusedAsync();
    }
}
