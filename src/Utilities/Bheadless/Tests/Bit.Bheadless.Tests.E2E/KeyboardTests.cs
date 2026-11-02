using System.Text.RegularExpressions;
using Bit.Bheadless.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bheadless.Tests.E2E;

/// <summary>
/// Keyboard and focus behaviour that only a real browser can verify: focus movement, focus traps and restoration,
/// inert backgrounds, scroll locking, positioning - plus axe-core on the open state of each overlay.
/// </summary>
[TestClass]
public sealed class KeyboardTests : DemoTest
{
    private static readonly Regex AnyValue = new(".+");

    [TestMethod]
    public async Task Dialog_traps_focus_makes_the_page_inert_and_restores_focus()
    {
        var page = await OpenDemoAsync("/dialog");
        var trigger = page.Locator("main button", new PageLocatorOptions { HasTextRegex = new Regex("open", RegexOptions.IgnoreCase) }).First;
        await trigger.FocusAsync();
        await page.Keyboard.PressAsync("Enter");

        var dialog = page.Locator("[role=dialog]").First;
        await Expect(dialog).ToBeVisibleAsync();
        await Expect(dialog).ToHaveAttributeAsync("aria-modal", "true");
        Task<bool> FocusInDialog() => page.EvaluateAsync<bool>("() => !!document.activeElement.closest('[role=dialog]')");
        await PollAsync(FocusInDialog, true, "the focus inside the dialog");
        await PollAsync(() => page.Locator("[inert]").CountAsync(), count => count > 0, "inert elements behind the dialog");
        await PollAsync(() => page.EvaluateAsync<string>("() => getComputedStyle(document.body).overflow"), "hidden", "the body's overflow");

        for (var i = 0; i < 6; i++) await page.Keyboard.PressAsync("Tab");
        Assert.IsTrue(await FocusInDialog(), "Tab moved the focus out of the dialog.");
        await ExpectAccessibleAsync(page);

        await page.Keyboard.PressAsync("Escape");
        await Expect(dialog).ToBeHiddenAsync();
        await Expect(trigger).ToBeFocusedAsync();
        await Expect(page.Locator("[inert]")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Menu_opens_with_ArrowDown_navigates_and_returns_focus_on_Escape()
    {
        var page = await OpenDemoAsync("/menu");
        var button = page.Locator("main button[aria-haspopup=menu]").First;
        await button.FocusAsync();
        await page.Keyboard.PressAsync("ArrowDown");

        var menu = page.Locator("[role=menu]:visible").First;
        await Expect(menu).ToBeVisibleAsync();
        await Expect(menu).ToBeFocusedAsync();
        await Expect(menu).ToHaveAttributeAsync("aria-activedescendant", AnyValue);
        var first = await menu.GetAttributeAsync("aria-activedescendant");
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(menu).Not.ToHaveAttributeAsync("aria-activedescendant", first!);
        await ExpectAccessibleAsync(page);

        await page.Keyboard.PressAsync("Escape");
        await Expect(menu).ToBeHiddenAsync();
        await Expect(button).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Listbox_arrows_move_the_active_option_and_Enter_selects()
    {
        var page = await OpenDemoAsync("/listbox");
        var button = page.Locator("main [role=combobox]").First;
        await button.FocusAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(button).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(button).ToHaveAttributeAsync("aria-activedescendant", AnyValue);
        var first = await button.GetAttributeAsync("aria-activedescendant");
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(button).Not.ToHaveAttributeAsync("aria-activedescendant", first!);
        var active = await button.GetAttributeAsync("aria-activedescendant");
        var text = (await page.Locator($"[id=\"{active}\"]").InnerTextAsync()).Trim();
        await ExpectAccessibleAsync(page);

        await page.Keyboard.PressAsync("Enter");
        await Expect(button).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(button).ToContainTextAsync(text);
        await Expect(button).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Combobox_typing_filters_and_highlights_and_Enter_selects()
    {
        var page = await OpenDemoAsync("/combobox");
        var input = page.Locator("main input[role=combobox]").First;
        await input.ClickAsync();
        await input.PressSequentiallyAsync("ap", new LocatorPressSequentiallyOptions { Delay = 50 });
        await Expect(input).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(input).ToHaveAttributeAsync("aria-activedescendant", AnyValue);
        var active = await input.GetAttributeAsync("aria-activedescendant");
        var text = (await page.Locator($"[id=\"{active}\"]").InnerTextAsync()).Trim();
        await ExpectAccessibleAsync(page);

        await page.Keyboard.PressAsync("Enter");
        await Expect(input).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(input).ToHaveValueAsync(text);
    }

    [TestMethod]
    public async Task Tabs_arrow_keys_move_the_selection_without_scrolling_the_page()
    {
        var page = await OpenDemoAsync("/tabs");
        // Pinned by id: the selector would follow the selection.
        var id = await page.Locator("main [role=tab][aria-selected=true]").First.GetAttributeAsync("id");
        var tab = page.Locator($"[id=\"{id}\"]");
        await tab.FocusAsync();
        var before = await page.EvaluateAsync<double>("() => window.scrollY");
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(tab).Not.ToBeFocusedAsync();
        var now = await FocusedAsync(page);
        Assert.AreEqual("tab", now?.Role);
        await Expect(page.Locator($"[id=\"{now!.Id}\"]")).ToHaveAttributeAsync("aria-selected", "true");
        Assert.AreEqual(before, await page.EvaluateAsync<double>("() => window.scrollY"), "the page scrolled");
    }

    [TestMethod]
    public async Task Popover_opens_on_click_closes_on_Escape_and_restores_focus()
    {
        var page = await OpenDemoAsync("/popover");
        var button = page.Locator("main button[aria-expanded]").First;
        await button.ClickAsync();
        await Expect(button).ToHaveAttributeAsync("aria-expanded", "true");
        await ExpectAccessibleAsync(page);
        await page.Keyboard.PressAsync("Escape");
        await Expect(button).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(button).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Tooltip_shows_on_hover_and_hides_on_Escape()
    {
        var page = await OpenDemoAsync("/tooltip");
        var trigger = page.Locator("main [aria-describedby]").First;
        await trigger.HoverAsync();
        await Expect(page.Locator("[role=tooltip]:visible")).ToHaveCountAsync(1, new LocatorAssertionsToHaveCountOptions { Timeout = 3_000 });
        await page.Keyboard.PressAsync("Escape");
        await Expect(page.Locator("[role=tooltip]:visible")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Alert_dialog_ignores_outside_clicks_and_closes_on_Escape()
    {
        var page = await OpenDemoAsync("/alert-dialog");
        var trigger = page.Locator("main button[aria-haspopup=dialog]").First;
        await trigger.ClickAsync();
        var dialog = page.Locator("[role=alertdialog]");
        await Expect(dialog).ToBeVisibleAsync();
        await page.Mouse.ClickAsync(3, 3);
        await Expect(dialog).ToBeVisibleAsync();
        await ExpectAccessibleAsync(page);
        await page.Keyboard.PressAsync("Escape");
        await Expect(dialog).ToBeHiddenAsync();
        await Expect(trigger).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Radio_group_arrows_move_the_focus_and_check()
    {
        var page = await OpenDemoAsync("/radiogroup");
        var id = await page.Locator("main [role=radio][tabindex=\"0\"]").First.GetAttributeAsync("id");
        var radio = page.Locator($"[id=\"{id}\"]");
        await radio.FocusAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(radio).Not.ToBeFocusedAsync();
        var now = await FocusedAsync(page);
        Assert.AreEqual("radio", now?.Role);
        await Expect(page.Locator($"[id=\"{now!.Id}\"]")).ToHaveAttributeAsync("aria-checked", "true");
    }

    [TestMethod]
    public async Task Checkbox_toggles_with_Space_but_not_Enter()
    {
        var page = await OpenDemoAsync("/checkbox");
        var box = page.Locator("main [role=checkbox]:not([disabled])").First;
        await box.FocusAsync();
        var initial = await box.GetAttributeAsync("aria-checked");
        await page.Keyboard.PressAsync("Enter");
        await Expect(box).ToHaveAttributeAsync("aria-checked", initial!);
        await page.Keyboard.PressAsync(" ");
        await Expect(box).Not.ToHaveAttributeAsync("aria-checked", initial!);
    }

    [TestMethod]
    public async Task Tree_arrows_move_the_focus_and_Right_expands()
    {
        var page = await OpenDemoAsync("/tree");
        var tree = page.Locator("main [role=tree]").First;
        var id = await tree.Locator("[role=treeitem][tabindex=\"0\"]").First.GetAttributeAsync("id");
        await page.Locator($"[id=\"{id}\"]").FocusAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(page.Locator($"[id=\"{id}\"]")).Not.ToBeFocusedAsync();
        Assert.AreEqual("treeitem", (await FocusedAsync(page))?.Role);

        var collapsedId = await tree.Locator("[role=treeitem][aria-expanded=false]").First.GetAttributeAsync("id");
        await page.Locator($"[id=\"{collapsedId}\"]").FocusAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(page.Locator($"[id=\"{collapsedId}\"]")).ToHaveAttributeAsync("aria-expanded", "true");
        await ExpectAccessibleAsync(page);
    }

    [TestMethod]
    public async Task Command_palette_opens_with_Ctrl_K_filters_and_restores_focus()
    {
        var page = await OpenDemoAsync("/command");
        var before = await page.EvaluateAsync<string?>("() => document.activeElement?.tagName");
        await page.Keyboard.PressAsync("Control+k");
        var dialog = page.Locator("[role=dialog]");
        await Expect(dialog).ToBeVisibleAsync();
        var input = dialog.Locator("input[role=combobox]");
        await Expect(input).ToBeFocusedAsync();
        await input.PressSequentiallyAsync("se", new LocatorPressSequentiallyOptions { Delay = 30 });
        await Expect(input).ToHaveAttributeAsync("aria-activedescendant", AnyValue);
        await ExpectAccessibleAsync(page);
        await page.Keyboard.PressAsync("Escape");
        await Expect(dialog).ToBeHiddenAsync();
        Assert.AreEqual(before, await page.EvaluateAsync<string?>("() => document.activeElement?.tagName"), "the focus was not restored");
    }

    [TestMethod]
    public async Task A_letter_shortcut_without_Shift_leaves_the_shifted_letter_alone()
    {
        var page = await OpenDemoAsync("/command");

        // The letter's case is compared away, so Shift has to be part of the match: "mod+k" must not also take
        // Ctrl+Shift+K. A shortcut that matches calls preventDefault on the keydown, synchronously.
        Assert.IsFalse(await ShortcutTakesAsync(page, shift: true), "Ctrl+Shift+K was taken by the Ctrl+K shortcut");
        Assert.IsTrue(await ShortcutTakesAsync(page, shift: false), "Ctrl+K was not taken");
        await Expect(page.Locator("[role=dialog]")).ToBeVisibleAsync();
    }

    private static Task<bool> ShortcutTakesAsync(IPage page, bool shift)
        => page.EvaluateAsync<bool>("""
            shift => {
                const e = new KeyboardEvent('keydown', { key: shift ? 'K' : 'k', code: 'KeyK', ctrlKey: true, shiftKey: shift, bubbles: true, cancelable: true });
                document.body.dispatchEvent(e);
                return e.defaultPrevented;
            }
            """, shift);

    [TestMethod]
    public async Task Slider_arrow_keys_change_the_value_and_Home_End_jump()
    {
        var page = await OpenDemoAsync("/slider");
        var thumb = page.Locator("main [role=slider]:not([aria-disabled=true])").First;
        await thumb.FocusAsync();
        var min = await thumb.GetAttributeAsync("aria-valuemin");
        var max = await thumb.GetAttributeAsync("aria-valuemax");
        await page.Keyboard.PressAsync("Home");
        await Expect(thumb).ToHaveAttributeAsync("aria-valuenow", min!);
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(thumb).Not.ToHaveAttributeAsync("aria-valuenow", min!);
        await page.Keyboard.PressAsync("End");
        await Expect(thumb).ToHaveAttributeAsync("aria-valuenow", max!);
    }

    [TestMethod]
    public async Task Sortable_keyboard_reordering_is_announced()
    {
        var page = await OpenDemoAsync("/sortable");
        var handle = page.Locator("main [data-bit-bh-sortable-handle]").First;
        var list = handle.Locator("xpath=ancestor::ul[1]");
        var firstText = (await list.Locator("> li").First.InnerTextAsync()).Trim();
        var liveRegion = page.Locator("[data-bit-bh-live-region]");
        await handle.FocusAsync();
        await page.Keyboard.PressAsync(" ");
        await Expect(liveRegion).ToContainTextAsync(new Regex("Picked up"));
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync(" ");
        await Expect(liveRegion).ToContainTextAsync(new Regex("Dropped"));
        Assert.AreEqual(firstText, (await list.Locator("> li").Nth(1).InnerTextAsync()).Trim());
    }

    [TestMethod]
    public async Task Toasts_are_announced_and_F8_moves_the_focus_to_them()
    {
        var page = await OpenDemoAsync("/toast");
        await page.Locator("main button", new PageLocatorOptions { HasText = "Show toast" }).ClickAsync();
        var region = page.Locator("[role=region][aria-label^=\"Notifications\"]").First;
        await Expect(region.Locator("li")).ToHaveCountAsync(1);
        await Expect(page.Locator("[data-bit-bh-live-region]")).Not.ToHaveTextAsync("");
        await page.Keyboard.PressAsync("F8");
        await PollAsync(() => page.EvaluateAsync<bool>("() => !!document.activeElement.closest('[role=region]')"), true, "the focus inside the notifications region");
        await ExpectAccessibleAsync(page);
    }
}
