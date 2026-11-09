using System.Text.RegularExpressions;
using Bit.Bheadless.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bheadless.Tests.E2E;

/// <summary>
/// Menus that open on hover (<c>BitBhMenu OpenOnHover</c>), which only a browser can show: the hover opens the menu after
/// its delay without moving the focus, the first key press moves the focus in, the pointer crossing the gap to the menu
/// diagonally keeps it open, leaving closes it after its delay without pulling the focus to the trigger, a click pins it,
/// and once one menu of a bar is open the next trigger hovered takes over.
/// </summary>
[TestClass]
public sealed class MenuHoverTests : DemoTest
{
    private static readonly Regex AnyValue = new(".+");

    // The demos' delays (menubar: the defaults; menu: set on the sample).
    private const int MenubarOpenDelay = 100;
    private const int MenubarCloseDelay = 300;
    private const int MenuCloseDelay = 400;

    /// <summary>The live sample of the example titled <paramref name="title"/>.</summary>
    private static ILocator Sample(IPage page, string title) =>
        page.Locator($"{Content} .bh-example", new PageLocatorOptions { Has = page.Locator("h2", new PageLocatorOptions { HasTextRegex = new Regex($"^\\s*{Regex.Escape(title)}\\s*$") }) })
            .Locator(".bh-example-panel .demo-row").First;

    private static ILocator Trigger(ILocator sample, string name) =>
        sample.Locator("[role=menubar] [role=menuitem]", new LocatorLocatorOptions { HasText = name });

    /// <summary>The menu a trigger controls (its <c>aria-controls</c> while open).</summary>
    private static async Task<ILocator> MenuOfAsync(IPage page, ILocator trigger)
    {
        await Expect(trigger).ToHaveAttributeAsync("aria-controls", AnyValue);
        return page.Locator($"[id='{await trigger.GetAttributeAsync("aria-controls")}']");
    }

    private static Task<bool> NothingFocusedAsync(IPage page) =>
        page.EvaluateAsync<bool>("() => document.activeElement === document.body || document.activeElement === null");

    /// <summary>
    /// Starts a clock that stops when <paramref name="target"/>'s <paramref name="attribute"/> becomes
    /// <paramref name="value"/>; <see cref="ElapsedAsync"/> reads it. Started before the action, it can only measure more
    /// than the component waited - a lower bound that a slow machine never breaks.
    /// </summary>
    private static Task StartClockAsync(ILocator target, string attribute, string value) => target.EvaluateAsync(
        """
        (el, [attribute, value]) => {
          const start = performance.now();
          window.__bhClock = new Promise(resolve => {
            const observer = new MutationObserver(() => {
              if (el.getAttribute(attribute) === value) { observer.disconnect(); resolve(performance.now() - start); }
            });
            observer.observe(el, { attributes: true, attributeFilter: [attribute] });
          });
        }
        """, new object[] { attribute, value });

    private static Task<double> ElapsedAsync(IPage page) => page.EvaluateAsync<double>("() => window.__bhClock");

    /// <summary>Moves the mouse from where it is to <paramref name="to"/> in steps, the way a hand does.</summary>
    private static async Task GlideAsync(IPage page, (float X, float Y) from, (float X, float Y) to, int steps = 10)
    {
        for (var i = 1; i <= steps; i++)
        {
            await page.Mouse.MoveAsync(from.X + (to.X - from.X) * i / steps, from.Y + (to.Y - from.Y) * i / steps);
            await Task.Delay(15);
        }
    }

    private static (float X, float Y) Center(LocatorBoundingBoxResult box) => (box.X + box.Width / 2, box.Y + box.Height / 2);

    /// <summary>Moves the mouse beside <paramref name="menu"/>: off the menu, its trigger and the bar, onto nothing that hovers.</summary>
    private static async Task MoveAwayAsync(IPage page, ILocator menu)
    {
        var box = (await menu.BoundingBoxAsync())!;
        await page.Mouse.MoveAsync(Math.Min(box.X + box.Width + 150, page.ViewportSize!.Width - 5), box.Y + box.Height / 2);
    }

    [TestMethod]
    public async Task A_hover_opens_the_menu_after_its_delay_without_moving_the_focus_and_the_first_key_moves_it_in()
    {
        var page = await OpenDemoAsync("/menubar");
        var sample = Sample(page, "Open on hover");
        await sample.ScrollIntoViewIfNeededAsync();
        await page.EvaluateAsync("() => document.activeElement?.blur()");
        var trigger = Trigger(sample, "Products");

        await StartClockAsync(trigger, "aria-expanded", "true");
        await trigger.HoverAsync();
        var opened = await ElapsedAsync(page);
        Assert.IsGreaterThanOrEqualTo(MenubarOpenDelay - 20, opened, $"The menu opened {opened:0} ms after the hover, before its delay.");

        var menu = await MenuOfAsync(page, trigger);
        await Expect(menu).ToBeVisibleAsync();
        await Expect(menu).ToHaveAttributeAsync("data-state", "open");
        Assert.IsTrue(await NothingFocusedAsync(page), $"The hover moved the focus to {await FocusedAsync(page)}.");
        await Expect(menu).Not.ToHaveAttributeAsync("aria-activedescendant", AnyValue);
        await ExpectAccessibleAsync(page);

        // Nothing has the focus: the first key press moves it into the menu and acts there.
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(menu).ToBeFocusedAsync();
        await Expect(menu).ToHaveAttributeAsync("aria-activedescendant", AnyValue);
        var first = menu.Locator($"[id='{await menu.GetAttributeAsync("aria-activedescendant")}']");
        await Expect(first).ToHaveTextAsync("Analytics");
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(menu.Locator("[data-highlighted]")).ToHaveTextAsync("Automation");

        // The keyboard has it now: the pointer leaving does not close it, and Escape gives the focus to the trigger.
        await MoveAwayAsync(page, menu);
        await Task.Delay(MenubarCloseDelay * 2);
        await Expect(menu).ToBeVisibleAsync();
        await page.Keyboard.PressAsync("Escape");
        await Expect(menu).ToBeHiddenAsync();
        await Expect(trigger).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task The_pointer_crossing_diagonally_to_the_menu_keeps_it_open_and_leaving_closes_it_after_its_delay()
    {
        var page = await OpenDemoAsync("/menubar");
        var sample = Sample(page, "Open on hover");
        await sample.ScrollIntoViewIfNeededAsync();
        await page.EvaluateAsync("() => document.activeElement?.blur()");
        var trigger = Trigger(sample, "Products");
        await trigger.HoverAsync();
        var menu = await MenuOfAsync(page, trigger);
        await Expect(menu).ToBeVisibleAsync();

        // From the middle of the trigger down and to the right, over the gap, to the far end of an item.
        var from = Center((await trigger.BoundingBoxAsync())!);
        var item = menu.Locator("[role=menuitem]", new LocatorLocatorOptions { HasText = "Automation" });
        var box = (await item.BoundingBoxAsync())!;
        await GlideAsync(page, from, (box.X + box.Width * 0.85f, box.Y + box.Height / 2));

        await Task.Delay(MenubarCloseDelay * 2);
        await Expect(menu).ToBeVisibleAsync();
        await Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");
        // The pointer highlights the item without focusing the menu.
        await Expect(item).ToHaveAttributeAsync("data-highlighted", "");
        Assert.IsTrue(await NothingFocusedAsync(page), $"Hovering an item moved the focus to {await FocusedAsync(page)}.");

        // Out of the menu, well clear of the bar: it closes after its delay and gives no focus to the trigger.
        await StartClockAsync(trigger, "aria-expanded", "false");
        await MoveAwayAsync(page, menu);
        var closed = await ElapsedAsync(page);
        Assert.IsGreaterThanOrEqualTo(MenubarCloseDelay - 20, closed, $"The menu closed {closed:0} ms after the pointer left it, before its delay.");
        await Expect(menu).ToBeHiddenAsync();
        Assert.IsTrue(await NothingFocusedAsync(page), $"Closing the menu moved the focus to {await FocusedAsync(page)}.");
    }

    [TestMethod]
    public async Task A_submenu_the_pointer_opens_and_leaves_gives_no_focus_to_the_menu()
    {
        var page = await OpenDemoAsync("/menubar");
        var sample = Sample(page, "Open on hover");
        await sample.ScrollIntoViewIfNeededAsync();
        await page.EvaluateAsync("() => document.activeElement?.blur()");
        var trigger = Trigger(sample, "Products");
        await trigger.HoverAsync();
        var menu = await MenuOfAsync(page, trigger);
        await Expect(menu).ToBeVisibleAsync();

        var subTrigger = menu.Locator("[role=menuitem]", new LocatorLocatorOptions { HasText = "Integrations" });
        await GlideAsync(page, Center((await trigger.BoundingBoxAsync())!), Center((await subTrigger.BoundingBoxAsync())!));
        var sub = await MenuOfAsync(page, subTrigger);
        await Expect(sub).ToBeVisibleAsync();
        await ExpectAccessibleAsync(page);

        // Back up to another item: the submenu closes after its grace period, and the menu still has no focus...
        var item = menu.Locator("[role=menuitem]", new LocatorLocatorOptions { HasText = "Analytics" });
        await GlideAsync(page, Center((await subTrigger.BoundingBoxAsync())!), Center((await item.BoundingBoxAsync())!), steps: 4);
        await Expect(sub).ToBeHiddenAsync();
        await Expect(menu).ToBeVisibleAsync();
        Assert.IsTrue(await NothingFocusedAsync(page), $"Closing the submenu moved the focus to {await FocusedAsync(page)}.");

        // ...so it still closes when the pointer leaves it.
        await MoveAwayAsync(page, menu);
        await Expect(menu).ToBeHiddenAsync();
        Assert.IsTrue(await NothingFocusedAsync(page), $"Closing the menu moved the focus to {await FocusedAsync(page)}.");
    }

    [TestMethod]
    public async Task Once_a_menu_of_the_bar_is_open_hovering_the_next_trigger_switches_without_moving_the_focus()
    {
        var page = await OpenDemoAsync("/menubar");
        var sample = Sample(page, "Open on hover");
        await sample.ScrollIntoViewIfNeededAsync();
        await page.EvaluateAsync("() => document.activeElement?.blur()");
        var products = Trigger(sample, "Products");
        var resources = Trigger(sample, "Resources");

        await products.HoverAsync();
        var productsMenu = await MenuOfAsync(page, products);
        await Expect(productsMenu).ToBeVisibleAsync();

        await resources.HoverAsync();
        var resourcesMenu = await MenuOfAsync(page, resources);
        await Expect(resourcesMenu).ToBeVisibleAsync();
        await Expect(productsMenu).ToBeHiddenAsync();
        await Expect(products).ToHaveAttributeAsync("aria-expanded", "false");
        Assert.IsTrue(await NothingFocusedAsync(page), $"Switching menus moved the focus to {await FocusedAsync(page)}.");
        // The bar's tab stop stays where the focus last was.
        await Expect(products).ToHaveAttributeAsync("tabindex", "0");
        await ExpectAccessibleAsync(page);
    }

    [TestMethod]
    public async Task A_click_on_the_trigger_of_a_hover_opened_menu_keeps_it_open_and_gives_it_the_focus()
    {
        var page = await OpenDemoAsync("/menubar");
        var sample = Sample(page, "Open on hover");
        await sample.ScrollIntoViewIfNeededAsync();
        var trigger = Trigger(sample, "Resources");
        await trigger.HoverAsync();
        var menu = await MenuOfAsync(page, trigger);
        await Expect(menu).ToBeVisibleAsync();

        await trigger.ClickAsync();
        await Expect(menu).ToBeFocusedAsync();
        await MoveAwayAsync(page, menu);
        await Task.Delay(MenubarCloseDelay * 2);
        await Expect(menu).ToBeVisibleAsync();
        await Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");

        await page.Keyboard.PressAsync("Escape");
        await Expect(menu).ToBeHiddenAsync();
        await Expect(trigger).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task A_hover_leaves_the_focus_in_the_field_being_typed_in()
    {
        var page = await OpenDemoAsync("/menu");
        var sample = Sample(page, "Open on hover");
        await sample.ScrollIntoViewIfNeededAsync();
        var field = sample.Locator("input");
        var button = sample.Locator("button[aria-haspopup=menu]");
        await field.ClickAsync();
        await page.Keyboard.TypeAsync("ab");

        await button.HoverAsync();
        await Expect(button).ToHaveAttributeAsync("aria-expanded", "true");
        var menu = page.Locator($"[id='{await button.GetAttributeAsync("aria-controls")}']");
        await Expect(menu).ToBeVisibleAsync();

        // The keys are the field's: typing goes on, and an arrow key does not move into the menu.
        await page.Keyboard.TypeAsync("c");
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(field).ToBeFocusedAsync();
        await Expect(field).ToHaveValueAsync("abc");
        await Expect(menu).Not.ToHaveAttributeAsync("aria-activedescendant", AnyValue);

        await MoveAwayAsync(page, menu);
        await Expect(menu).ToBeHiddenAsync(new LocatorAssertionsToBeHiddenOptions { Timeout = MenuCloseDelay + ExpectTimeout });
        await Expect(field).ToBeFocusedAsync();
    }
}
